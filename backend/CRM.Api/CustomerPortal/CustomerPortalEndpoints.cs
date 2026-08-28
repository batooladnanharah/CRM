using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.KnowledgeBase;
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
    }

    private static CustomerTicketListItemResponse ToListItemResponse(Ticket t) => new(
        t.Id, t.Title, t.Status, t.Priority, t.CreatedAtUtc, t.UpdatedAtUtc);

    private static async Task<CustomerTicketDetailsResponse> ToDetailsResponseAsync(
        Ticket ticket, TicketDbContext db, CancellationToken ct)
    {
        var messages = await db.TicketMessages.AsNoTracking()
            .Where(m => m.TicketId == ticket.Id && !m.IsInternal)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => new CustomerTicketMessageResponse(m.Id, m.Body, m.CreatedAtUtc))
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
