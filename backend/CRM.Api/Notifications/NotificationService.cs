using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Notifications;

public sealed class NotificationService(NotificationsDbContext db) : INotificationService
{
    public async Task CreateAsync(Notification notification, CancellationToken ct)
    {
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Notification>> ListForUserAsync(Guid userId, int take, CancellationToken ct)
    {
        return await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken ct) =>
        db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct)
    {
        var entity = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct);
        if (entity is null)
        {
            return false;
        }

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            entity.ReadAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct)
    {
        var unread = await db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        if (unread.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return unread.Count;
    }
}
