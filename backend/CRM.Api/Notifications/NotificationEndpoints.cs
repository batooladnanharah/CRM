using System.Security.Claims;
using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Notifications;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications")
            .RequireAuthorization()
            .WithTags("Notifications");

        group.MapGet("/", async (
            int? take, ClaimsPrincipal principal, INotificationService service, TicketDbContext ticketDb,
            CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            var limit = Math.Clamp(take ?? 50, 1, 50);

            var items = await service.ListForUserAsync(userId, limit, ct);

            // Ticket-visibility filter: hide notifications whose referenced
            // ticket no longer exists (TicketEndpoints.cs has no per-role
            // ticket-visibility restriction beyond the TicketsManage
            // permission already required to authenticate into this app, so
            // "still exists" is the only additional check available here).
            var ticketIds = items.Where(n => n.TicketId is not null).Select(n => n.TicketId!.Value).Distinct().ToList();
            var existingTicketIds = ticketIds.Count == 0
                ? []
                : await ticketDb.Tickets.AsNoTracking()
                    .Where(t => ticketIds.Contains(t.Id))
                    .Select(t => t.Id)
                    .ToListAsync(ct);
            var existingSet = existingTicketIds.ToHashSet();

            var visible = items.Where(n => n.TicketId is null || existingSet.Contains(n.TicketId.Value)).ToList();
            var unreadCount = await service.UnreadCountAsync(userId, ct);

            return Results.Ok(new NotificationListResponse(visible.Select(ToDto).ToList(), unreadCount));
        })
        .WithName("ListNotifications");

        group.MapPatch("/{id:guid}/read", async (
            Guid id, ClaimsPrincipal principal, INotificationService service, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            var ok = await service.MarkReadAsync(userId, id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .WithName("MarkNotificationRead");

        group.MapPatch("/read-all", async (
            ClaimsPrincipal principal, INotificationService service, CancellationToken ct) =>
        {
            var userId = GetUserId(principal);
            await service.MarkAllReadAsync(userId, ct);
            return Results.NoContent();
        })
        .WithName("MarkAllNotificationsRead");
    }

    private static Guid GetUserId(ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static NotificationDto ToDto(Notification n) => new(
        n.Id, n.Type, n.Title, n.Message, n.TicketId, n.IsRead, n.CreatedAt);
}
