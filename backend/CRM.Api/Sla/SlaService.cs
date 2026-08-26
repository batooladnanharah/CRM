using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Sla;

public interface ISlaService
{
    Task<SlaPolicy?> ResolvePolicyAsync(TicketPriority priority, string? channel, CancellationToken ct);
}

public sealed class SlaService(TicketDbContext db) : ISlaService
{
    // Resolution order: exact (priority, channel) match -> (priority, any
    // channel) -> the single active default policy -> none.
    public async Task<SlaPolicy?> ResolvePolicyAsync(TicketPriority priority, string? channel, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(channel))
        {
            var exactMatch = await db.SlaPolicies.AsNoTracking()
                .FirstOrDefaultAsync(p => p.IsActive && p.Priority == priority && p.Channel == channel, ct);
            if (exactMatch is not null)
            {
                return exactMatch;
            }
        }

        var anyChannelMatch = await db.SlaPolicies.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsActive && p.Priority == priority && p.Channel == null, ct);
        if (anyChannelMatch is not null)
        {
            return anyChannelMatch;
        }

        return await db.SlaPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.IsActive && p.IsDefault, ct);
    }
}
