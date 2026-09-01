namespace CRM.Api.Notifications;

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? TicketId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

public enum NotificationType
{
    SlaAtRisk = 1,
    SlaBreached = 2,

    // CRM-74 — a customer posted a reply via the customer portal. Sent to
    // the ticket's assignee (if any); no notification when unassigned.
    CustomerReplied = 3
}
