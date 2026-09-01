using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.KnowledgeBase;
using CRM.Api.Notifications;
using CRM.Api.Security;
using CRM.Api.Tickets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.CustomerPortal;

public static class CustomerPortalEndpoints
{
    private const int RecentTicketsCount = 5;

    public static void MapCustomerPortalEndpoints(this IEndpointRouteBuilder app)
    {
        var customer = app.MapGroup("/api/customer")
            .RequireAuthorization(Permissions.PortalAccess)
            .WithTags("CustomerPortal");

        customer.MapGet("/dashboard", async (
            ClaimsPrincipal principal, ICurrentCustomerAccessor accessor, TicketDbContext db, CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            var tickets = db.Tickets.AsNoTracking().Where(t => t.CustomerId == customerId);

            var openCount = await tickets.CountAsync(t => t.Status == TicketStatus.Open, ct);
            var pendingCount = await tickets.CountAsync(t => t.Status == TicketStatus.InProgress, ct);
            var resolvedCount = await tickets.CountAsync(
                t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed, ct);

            var recentTickets = await tickets
                .OrderByDescending(t => t.UpdatedAtUtc)
                .ThenByDescending(t => t.Id)
                .Take(RecentTicketsCount)
                .Select(t => ToListItemResponse(t))
                .ToListAsync(ct);

            return Results.Ok(new CustomerDashboardResponse(openCount, pendingCount, resolvedCount, recentTickets));
        })
        .WithName("GetPortalDashboard");

        customer.MapGet("/tickets", async (
            ClaimsPrincipal principal, ICurrentCustomerAccessor accessor, TicketDbContext db, CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            var items = await db.Tickets.AsNoTracking()
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.UpdatedAtUtc)
                .ThenByDescending(t => t.Id)
                .Select(t => ToListItemResponse(t))
                .ToListAsync(ct);

            return Results.Ok(items);
        })
        .WithName("ListPortalTickets");

        customer.MapGet("/tickets/{id:guid}", async (
            Guid id, ClaimsPrincipal principal, ICurrentCustomerAccessor accessor, TicketDbContext db,
            CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            // Never distinguish "doesn't exist" from "belongs to someone
            // else" — both return 404 so URL tampering can't confirm another
            // customer's ticket id exists.
            var entity = await db.Tickets.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId, ct);
            if (entity is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(await ToDetailsResponseAsync(entity, db, ct));
        })
        .WithName("GetPortalTicket");

        // Portal reply — the customer-authored counterpart to
        // TicketMessageEndpoints.CreateTicketMessage. Deliberately not the
        // same endpoint/policy: that one is gated to Permissions.TicketsManage
        // (staff only) and must stay that way; this one is gated to
        // Permissions.PortalAccess via the enclosing MapGroup and always
        // writes IsInternal = false, AuthorCustomerId = the resolved caller,
        // AuthorUserId = null.
        customer.MapPost("/tickets/{id:guid}/messages", async (
            Guid id, CustomerCreateTicketMessageRequest request, ClaimsPrincipal principal,
            ICurrentCustomerAccessor accessor, TicketDbContext db, INotificationService notifications,
            IAuditLogger auditLogger, CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            var body = request.Body?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(body))
            {
                return Results.BadRequest(new ErrorResponse("Body is required."));
            }
            // Same limit as TicketMessage.Body (TicketDbContext.OnModelCreating,
            // and the staff-side check in TicketMessageEndpoints) — one ceiling
            // for the column, enforced at every write path.
            if (body.Length > 5000)
            {
                return Results.BadRequest(new ErrorResponse("Body must be 5000 characters or fewer."));
            }

            // Same "never disclose existence" rule as GetPortalTicket above —
            // foreign-owned and missing tickets both 404.
            var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId, ct);
            if (ticket is null)
            {
                return Results.NotFound();
            }

            // TicketStatusRules: Closed is terminal (no allowed transitions at
            // all), so a reply is always rejected there. Resolved allows
            // Resolved -> Open, which we take as "a customer reply reopens a
            // resolved ticket" — the only other TicketStatusRules-sanctioned
            // outcome for Resolved would be to reject, and reopening is more
            // useful to a customer who still needs help.
            if (ticket.Status == TicketStatus.Closed)
            {
                return Results.Conflict(new ErrorResponse("ticket_closed"));
            }

            var now = DateTime.UtcNow;

            if (ticket.Status == TicketStatus.Resolved &&
                TicketStatusRules.IsAllowedTransition(TicketStatus.Resolved, TicketStatus.Open))
            {
                db.TicketHistory.Add(new TicketHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticket.Id,
                    ChangeType = TicketChangeType.Status,
                    OldValue = ticket.Status.ToString(),
                    NewValue = TicketStatus.Open.ToString(),
                    ChangedByUserId = customerId.Value,
                    ChangedAtUtc = now,
                });
                ticket.Status = TicketStatus.Open;
            }

            var message = new TicketMessage
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                AuthorUserId = null,
                AuthorCustomerId = customerId.Value,
                Body = body,
                IsInternal = false,
                Channel = MessageChannel.Web,
                CreatedAtUtc = now,
            };
            db.TicketMessages.Add(message);

            db.TicketHistory.Add(new TicketHistoryEntry
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                ChangeType = TicketChangeType.MessageAdded,
                OldValue = null,
                NewValue = message.Id.ToString(),
                ChangedByUserId = customerId.Value,
                ChangedAtUtc = now,
            });

            ticket.UpdatedAtUtc = now;

            await db.SaveChangesAsync(ct);

            // Mirrors the intent of the staff-side notification hooks (see
            // Sla/EscalationDispatcher.cs for the established Notification
            // creation pattern) — notify whoever is currently assigned that
            // the customer replied. No assignee yet -> no notification.
            if (ticket.AssigneeUserId is { } assigneeId)
            {
                await notifications.CreateAsync(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = assigneeId,
                    Type = NotificationType.CustomerReplied,
                    Title = "New customer reply",
                    Message = $"The customer replied on \"{ticket.Title}\".",
                    TicketId = ticket.Id,
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow,
                }, ct);
            }

            await auditLogger.WriteAsync(
                AuditActions.TicketMessageAdded, targetType: "ticket", targetId: ticket.Id.ToString(), ct: ct);

            return Results.Created(
                $"/api/customer/tickets/{ticket.Id}/messages/{message.Id}", ToMessageResponse(message));
        })
        .WithName("CreatePortalTicketMessage");

        customer.MapPost("/tickets", async (
            CreateCustomerTicketRequest request, ClaimsPrincipal principal, ICurrentCustomerAccessor accessor,
            TicketDbContext db, TicketCreationService creationService, IAuditLogger auditLogger,
            CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            var title = request.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(title))
            {
                return Results.BadRequest(new ErrorResponse("Title is required."));
            }
            if (title.Length > 200)
            {
                return Results.BadRequest(new ErrorResponse("Title must be 200 characters or fewer."));
            }

            var description = request.Description?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(description))
            {
                return Results.BadRequest(new ErrorResponse("Description is required."));
            }
            if (description.Length > 4000)
            {
                return Results.BadRequest(new ErrorResponse("Description must be 4000 characters or fewer."));
            }

            // customerId is always the resolved current-customer id — any
            // customerId the client might have sent is neither read nor
            // accepted anywhere in this request.
            var entity = await creationService.CreateAsync(
                customerId.Value, title, description, request.Priority ?? TicketPriority.Normal, ct);

            await auditLogger.WriteAsync(
                AuditActions.TicketCreated, targetType: "ticket", targetId: entity.Id.ToString(), ct: ct);

            return Results.Created(
                $"/api/customer/tickets/{entity.Id}", await ToDetailsResponseAsync(entity, db, ct));
        })
        .WithName("CreatePortalTicket");

        // Published-only knowledge-base exposure. Draft/Archived articles
        // and unknown ids are both 404 — never 403 — so a customer probing
        // ids can't tell "not published" from "doesn't exist".
        customer.MapGet("/knowledge-base/articles", async (
            int? page, int? pageSize, ClaimsPrincipal principal, ICurrentCustomerAccessor accessor,
            KnowledgeBaseDbContext kbDb, CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            var resolvedPage = Math.Max(page ?? 1, 1);
            var resolvedPageSize = Math.Clamp(pageSize ?? 20, 1, 100);

            var published = kbDb.Articles.AsNoTracking()
                .Where(a => a.Status == KnowledgeBaseArticleStatus.Published);

            var total = await published.CountAsync(ct);
            var items = await published
                .OrderByDescending(a => a.PublishedAtUtc)
                .ThenByDescending(a => a.Id)
                .Skip((resolvedPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .Select(a => new CustomerKnowledgeBaseArticleListItemResponse(
                    a.Id, a.Title, a.Slug, a.Tags, a.PublishedAtUtc!.Value))
                .ToListAsync(ct);

            return Results.Ok(new CustomerKnowledgeBaseArticleListResponse(
                items, total, resolvedPage, resolvedPageSize));
        })
        .WithName("ListPortalKnowledgeBaseArticles");

        customer.MapGet("/knowledge-base/articles/{id:guid}", async (
            Guid id, ClaimsPrincipal principal, ICurrentCustomerAccessor accessor, KnowledgeBaseDbContext kbDb,
            CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            var entity = await kbDb.Articles.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id && a.Status == KnowledgeBaseArticleStatus.Published, ct);
            if (entity is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new CustomerKnowledgeBaseArticleDetailsResponse(
                entity.Id, entity.Title, entity.Slug, entity.Body, entity.Tags, entity.PublishedAtUtc!.Value));
        })
        .WithName("GetPortalKnowledgeBaseArticle");

        // Active categories only, and only those with at least one Published
        // article — an active-but-empty (or active-but-all-draft) category
        // would otherwise show up as a dead end in the portal's category
        // list. Grouped projection (group-by CategoryId over Published
        // articles, then joined back to active categories) avoids N+1
        // per-category count queries.
        customer.MapGet("/knowledge-base/categories", async (
            ClaimsPrincipal principal, ICurrentCustomerAccessor accessor, KnowledgeBaseDbContext kbDb,
            CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            var publishedCounts = kbDb.Articles.AsNoTracking()
                .Where(a => a.Status == KnowledgeBaseArticleStatus.Published)
                .GroupBy(a => a.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() });

            var items = await kbDb.Categories.AsNoTracking()
                .Where(c => c.IsActive)
                .Join(publishedCounts, c => c.Id, g => g.CategoryId, (c, g) => new { c.Id, c.Name, c.Description, g.Count })
                .Where(r => r.Count > 0)
                .OrderBy(r => r.Name)
                .Select(r => new CustomerKnowledgeBaseCategoryResponse(r.Id, r.Name, r.Description, r.Count))
                .ToListAsync(ct);

            return Results.Ok(items);
        })
        .WithName("ListPortalKnowledgeBaseCategories");

        customer.MapGet("/knowledge-base/categories/{id:guid}/articles", async (
            Guid id, int? page, int? pageSize, ClaimsPrincipal principal, ICurrentCustomerAccessor accessor,
            KnowledgeBaseDbContext kbDb, CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            // Missing id and inactive category both 404 — an inactive
            // category is not customer-visible, matching the article-level
            // "draft/archived and unknown id both 404" convention above.
            var category = await kbDb.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);
            if (category is null)
            {
                return Results.NotFound();
            }

            var resolvedPage = Math.Max(page ?? 1, 1);
            var resolvedPageSize = Math.Clamp(pageSize ?? 20, 1, 100);

            var published = kbDb.Articles.AsNoTracking()
                .Where(a => a.Status == KnowledgeBaseArticleStatus.Published && a.CategoryId == id);

            var total = await published.CountAsync(ct);
            var items = await published
                .OrderByDescending(a => a.PublishedAtUtc)
                .ThenByDescending(a => a.Id)
                .Skip((resolvedPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .Select(a => new CustomerKnowledgeBaseArticleListItemResponse(
                    a.Id, a.Title, a.Slug, a.Tags, a.PublishedAtUtc!.Value))
                .ToListAsync(ct);

            return Results.Ok(new CustomerKnowledgeBaseArticleListResponse(
                items, total, resolvedPage, resolvedPageSize));
        })
        .WithName("ListPortalKnowledgeBaseCategoryArticles");

        // Full-text search scoped to the same visibility rules as every other
        // portal KB route: Published articles in an active category only.
        // IncludeDrafts is intentionally never read from the query string
        // here — portal callers can never see drafts regardless of what they
        // pass — see KnowledgeBaseSearchService for the shared matching and
        // ordering logic used by the CRM-side search endpoint.
        customer.MapGet("/knowledge-base/search", async (
            string? q, Guid? categoryId, int? page, int? pageSize, ClaimsPrincipal principal,
            ICurrentCustomerAccessor accessor, KnowledgeBaseDbContext kbDb, CancellationToken ct) =>
        {
            var customerId = await accessor.GetCurrentCustomerIdAsync(principal, ct);
            if (customerId is null)
            {
                return Results.Forbid();
            }

            var errorCode = KnowledgeBaseSearchService.ValidateQuery(q, out var trimmedQuery);
            if (errorCode is not null)
            {
                return Results.BadRequest(new ErrorResponse(errorCode));
            }

            var (resolvedPage, resolvedPageSize) = KnowledgeBaseSearchService.ClampPaging(page, pageSize);

            var (items, totalCount) = await KnowledgeBaseSearchService.SearchAsync(
                kbDb,
                new KnowledgeBaseSearchService.Options(
                    trimmedQuery, categoryId, CanSeeDrafts: false, IncludeDrafts: false,
                    resolvedPage, resolvedPageSize),
                ct);

            return Results.Ok(new KnowledgeBaseSearchResponse(items, resolvedPage, resolvedPageSize, totalCount));
        })
        .WithName("SearchPortalKnowledgeBase");
    }

    private static CustomerTicketMessageResponse ToMessageResponse(TicketMessage m) => new(
        m.Id, m.AuthorCustomerId != null ? "Customer" : "Agent", m.Body, m.CreatedAtUtc);

    private static CustomerTicketListItemResponse ToListItemResponse(Ticket t) => new(
        t.Id, t.Title, t.Status, t.Priority, t.CreatedAtUtc, t.UpdatedAtUtc);

    private static async Task<CustomerTicketDetailsResponse> ToDetailsResponseAsync(
        Ticket ticket, TicketDbContext db, CancellationToken ct)
    {
        var messages = await db.TicketMessages.AsNoTracking()
            .Where(m => m.TicketId == ticket.Id && !m.IsInternal)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new CustomerTicketMessageResponse(
                m.Id, m.AuthorCustomerId != null ? "Customer" : "Agent", m.Body, m.CreatedAtUtc))
            .ToListAsync(ct);

        var history = await db.TicketHistory.AsNoTracking()
            .Where(h => h.TicketId == ticket.Id && h.ChangeType == TicketChangeType.Status)
            .OrderBy(h => h.ChangedAtUtc)
            .Select(h => new CustomerTicketHistoryEntryResponse(h.Id, h.OldValue, h.NewValue, h.ChangedAtUtc))
            .ToListAsync(ct);

        return new CustomerTicketDetailsResponse(
            ticket.Id, ticket.Title, ticket.Description, ticket.Status, ticket.Priority,
            ticket.CreatedAtUtc, ticket.UpdatedAtUtc, messages, history);
    }
}
