using CRM.Api.Sla;

namespace CRM.Api.Tickets;

// Extracted so the internal create endpoint and the customer-portal create
// endpoint apply identical SLA-resolution/due-date logic — never duplicate it.
public sealed class TicketCreationService(TicketDbContext db, ISlaService slaService)
{
    public async Task<Ticket> CreateAsync(
        Guid customerId, string title, string description, TicketPriority priority, CancellationToken ct)
    {
        var entity = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = description,
            Status = TicketStatus.Open,
            Priority = priority,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        // Tickets don't carry a channel reference yet, so resolution is
        // always by priority alone (channel: null) — see SlaPolicy.Channel.
        var policy = await slaService.ResolvePolicyAsync(entity.Priority, channel: null, ct);
        if (policy is not null)
        {
            var (firstResponseDueAtUtc, resolutionDueAtUtc) =
                SlaCalculator.ComputeDueDates(policy, entity.CreatedAtUtc);
            entity.SlaPolicyId = policy.Id;
            entity.FirstResponseDueAtUtc = firstResponseDueAtUtc;
            entity.ResolutionDueAtUtc = resolutionDueAtUtc;
        }

        db.Tickets.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity;
    }
}
