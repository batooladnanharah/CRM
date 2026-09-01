using CRM.Api.Sla;

namespace CRM.Api.Tickets;

// Extracted so the internal create endpoint and the customer-portal create
// endpoint apply identical SLA-resolution/due-date logic — never duplicate it.
// CRM-62: also the single place automatic ticket assignment hooks into, so
// both create paths get it identically (never something the Vue app calls
// as a second, separate "assign" request).
public sealed class TicketCreationService(
    TicketDbContext db,
    ISlaService slaService,
    ITicketAssignmentService assignmentService,
    ILogger<TicketCreationService> logger)
{
    public async Task<Ticket> CreateAsync(
        Guid customerId, string title, string description, TicketPriority priority, CancellationToken ct,
        Guid? requestedAssigneeUserId = null, Guid? actorUserId = null)
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

        if (requestedAssigneeUserId is not null)
        {
            // Caller (TicketEndpoints) has already verified permission and that
            // the target user is a valid agent before passing this through —
            // this path never runs automatic assignment.
            entity.AssigneeUserId = requestedAssigneeUserId;
            entity.AutoAssigned = false;

            db.TicketHistory.Add(new TicketHistoryEntry
            {
                Id = Guid.NewGuid(),
                TicketId = entity.Id,
                ChangeType = TicketChangeType.Assignment,
                OldValue = null,
                NewValue = requestedAssigneeUserId.ToString(),
                ChangedByUserId = actorUserId ?? TicketEscalationService.SystemActorId,
                ChangedAtUtc = entity.CreatedAtUtc,
            });
        }
        else
        {
            // Never let an assignment failure (config, DB, or otherwise) stop
            // ticket creation — the ticket must always be persisted, unassigned
            // if necessary. See CRM-62 "Failure Handling".
            Guid? autoAssignedAgentId = null;
            try
            {
                autoAssignedAgentId = await assignmentService.TryAutoAssignAsync(entity, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex,
                    "auto_assignment_failed ticketId={TicketId} — ticket will be created unassigned",
                    entity.Id);
            }

            if (autoAssignedAgentId is not null)
            {
                entity.AssigneeUserId = autoAssignedAgentId;
                entity.AutoAssigned = true;

                db.TicketHistory.Add(new TicketHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    TicketId = entity.Id,
                    ChangeType = TicketChangeType.Assignment,
                    OldValue = null,
                    NewValue = autoAssignedAgentId.ToString(),
                    Reason = "AutoAssigned:LowestWorkload",
                    ChangedByUserId = TicketEscalationService.SystemActorId,
                    ChangedAtUtc = entity.CreatedAtUtc,
                });
            }
        }

        db.Tickets.Add(entity);
        await db.SaveChangesAsync(ct);

        return entity;
    }
}
