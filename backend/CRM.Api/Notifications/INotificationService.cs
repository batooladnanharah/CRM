namespace CRM.Api.Notifications;

public interface INotificationService
{
    Task CreateAsync(Notification notification, CancellationToken ct);
    Task<IReadOnlyList<Notification>> ListForUserAsync(Guid userId, int take, CancellationToken ct);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken ct);
    Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct);
}
