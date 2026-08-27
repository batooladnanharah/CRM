namespace CRM.Api.Notifications;

public sealed record NotificationDto(
    Guid Id, NotificationType Type, string Title, string Message,
    Guid? TicketId, bool IsRead, DateTimeOffset CreatedAt);

public sealed record NotificationListResponse(IReadOnlyList<NotificationDto> Items, int UnreadCount);
