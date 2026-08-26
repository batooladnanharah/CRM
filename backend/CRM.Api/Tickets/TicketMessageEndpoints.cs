using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Tickets;

public static class TicketMessageEndpoints
{
    public static void MapTicketMessageEndpoints(this IEndpointRouteBuilder app)
    {
        // Messages are staff-only (internal notes and agent-authored public
        // replies) — same AgentOrAdmin policy as the customer notes/attachments
        // groups, never the customer role.
        var messages = app.MapGroup("/api/tickets/{ticketId:guid}/messages")
            .RequireAuthorization("AgentOrAdmin")
            .WithTags("TicketMessages");

        messages.MapGet("/", async (
            Guid ticketId, [AsParameters] TicketMessagesQuery query, TicketDbContext db, AuthDbContext authDb) =>
        {
            var ticketExists = await db.Tickets.AsNoTracking().AnyAsync(t => t.Id == ticketId);
            if (!ticketExists)
            {
                return Results.NotFound();
            }

            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var messagesQuery = db.TicketMessages.AsNoTracking().Where(m => m.TicketId == ticketId);
            var totalCount = await messagesQuery.CountAsync();

            var entities = await messagesQuery
                .OrderByDescending(m => m.CreatedAtUtc)
                .ThenByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = await ToResponsesAsync(entities, db, authDb);
            return Results.Ok(new PagedResult<TicketMessageResponse>(items, page, pageSize, totalCount));
        })
        .WithName("ListTicketMessages");

        messages.MapPost("/", async (
            Guid ticketId, CreateTicketMessageRequest request, TicketDbContext db, AuthDbContext authDb,
            ClaimsPrincipal principal, ILogger<Program> log, IAuditLogger auditLogger) =>
        {
            var body = request.Body?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(body))
            {
                return Results.BadRequest(new ErrorResponse("Body is required."));
            }
            if (body.Length > 5000)
            {
                return Results.BadRequest(new ErrorResponse("Body must be 5000 characters or fewer."));
            }

            var mentionedUserIds = (request.MentionedUserIds ?? [])
                .Distinct()
                .ToList();

            if (mentionedUserIds.Count > 0 && !request.IsInternal)
            {
                return Results.BadRequest(new ErrorResponse("Mentions are only allowed on internal notes."));
            }

            var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket is null)
            {
                return Results.NotFound();
            }

            if (mentionedUserIds.Count > 0)
            {
                // Every mentioned id must resolve to an active user holding a
                // CRM role (admin/agent) — never the customer role.
                var validUserIds = await authDb.Users
                    .AsNoTracking()
                    .Where(u => mentionedUserIds.Contains(u.Id) && u.IsActive &&
                        (u.Roles.Contains(Roles.Admin) || u.Roles.Contains(Roles.Agent)))
                    .Select(u => u.Id)
                    .ToListAsync();

                var invalidIds = mentionedUserIds.Except(validUserIds).ToList();
                if (invalidIds.Count > 0)
                {
                    return Results.BadRequest(new ErrorResponse(
                        $"Cannot mention unknown, inactive, or non-CRM user(s): {string.Join(", ", invalidIds)}."));
                }
            }

            var authorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var now = DateTime.UtcNow;

            var entity = new TicketMessage
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                AuthorUserId = authorId,
                Body = body,
                IsInternal = request.IsInternal,
                CreatedAtUtc = now,
            };

            db.TicketMessages.Add(entity);

            foreach (var userId in mentionedUserIds)
            {
                db.MessageMentions.Add(new MessageMention
                {
                    Id = Guid.NewGuid(),
                    MessageId = entity.Id,
                    UserId = userId,
                    CreatedAtUtc = now,
                });
            }

            db.TicketHistory.Add(new TicketHistoryEntry
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                ChangeType = TicketChangeType.MessageAdded,
                OldValue = null,
                NewValue = entity.Id.ToString(),
                ChangedByUserId = authorId,
                ChangedAtUtc = now,
            });

            // Every ticket message is staff-authored (there is no customer- or
            // system-authored message path in this codebase yet), so the
            // first message on a ticket is, by construction, the first agent
            // response — stamp the SLA first-response clock right here.
            if (ticket.FirstRespondedAtUtc is null)
            {
                ticket.FirstRespondedAtUtc = now;
                ticket.UpdatedAtUtc = now;
            }

            await db.SaveChangesAsync();

            log.LogInformation(
                "ticket_message create ticketId={TicketId} messageId={MessageId} actor={ActorId} mentions={MentionCount}",
                ticketId, entity.Id, authorId, mentionedUserIds.Count);
            await auditLogger.WriteAsync(
                AuditActions.TicketMessageAdded, targetType: "ticket", targetId: ticketId.ToString());

            var response = (await ToResponsesAsync([entity], db, authDb))[0];
            return Results.Created($"/api/tickets/{ticketId}/messages/{entity.Id}", response);
        })
        .WithName("CreateTicketMessage");
    }

    private static async Task<List<TicketMessageResponse>> ToResponsesAsync(
        IReadOnlyList<TicketMessage> messages, TicketDbContext db, AuthDbContext authDb)
    {
        var authorIds = messages.Select(m => m.AuthorUserId).Distinct().ToList();
        var authorNames = await authDb.Users
            .AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var messageIds = messages.Select(m => m.Id).ToList();
        var mentionsByMessageId = (await db.MessageMentions
            .AsNoTracking()
            .Where(m => messageIds.Contains(m.MessageId))
            .ToListAsync())
            .GroupBy(m => m.MessageId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(m => m.UserId).ToList());

        return messages
            .Select(m => new TicketMessageResponse(
                m.Id,
                m.TicketId,
                m.AuthorUserId,
                authorNames.GetValueOrDefault(m.AuthorUserId, string.Empty),
                m.Body,
                m.IsInternal,
                mentionsByMessageId.GetValueOrDefault(m.Id, Array.Empty<Guid>()),
                m.CreatedAtUtc))
            .ToList();
    }
}

public record TicketMessagesQuery(int Page = 1, int PageSize = 20);
