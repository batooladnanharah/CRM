using CRM.Api.Sla;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Tickets;

public enum TicketEscalationFailureReason
{
    TicketTerminal,
    AlreadyAtMaxPriority,
}

public sealed record TicketEscalationResult(bool Success, TicketEscalationFailureReason? FailureReason);

// Shared by the manual /escalate endpoint and the SLA automation worker so
// both produce an identical audit trail — only the actor and reason differ.
public sealed class TicketEscalationService(TicketDbContext db, ISlaService slaService)
{
    // Sentinel "user" id for automation-originated history entries — there is
    // no real system user row in AuthDbContext, and ChangedByUserId stays
    // non-nullable to avoid touching every other history-writing call site.
    public static readonly Guid SystemActorId = Guid.Empty;

    public async Task<TicketEscalationResult> EscalateAsync(
        Ticket entity, string reason, Guid actorUserId, DateTime nowUtc, CancellationToken ct)
    {
        // Resolved/Closed tickets are terminal for escalation purposes even
        // though Resolved can still transition back to Open via the status
        // endpoint — reopen the ticket first, then escalate.
        if (entity.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            return new TicketEscalationResult(false, TicketEscalationFailureReason.TicketTerminal);
        }

        if (entity.Priority == TicketPriority.Urgent)
        {
            return new TicketEscalationResult(false, TicketEscalationFailureReason.AlreadyAtMaxPriority);
        }

        var oldPriority = entity.Priority;
        var newPriority = (TicketPriority)((int)oldPriority + 1);

        db.TicketHistory.Add(new TicketHistoryEntry
        {
            Id = Guid.NewGuid(),
            TicketId = entity.Id,
            ChangeType = TicketChangeType.Escalated,
            OldValue = oldPriority.ToString(),
            NewValue = newPriority.ToString(),
            Reason = reason,
            ChangedByUserId = actorUserId,
            ChangedAtUtc = nowUtc,
        });

        entity.Priority = newPriority;
        entity.UpdatedAtUtc = nowUtc;

        // Same SLA recompute rule as the dedicated priority-change endpoint:
        // re-resolve the policy for the new priority, but never move a due
        // date whose clock has already stopped.
        var oldPolicyId = entity.SlaPolicyId;
        var policy = await slaService.ResolvePolicyAsync(newPriority, channel: null, ct);
        entity.SlaPolicyId = policy?.Id;

        if (policy is not null)
        {
            var (firstResponseDueAtUtc, resolutionDueAtUtc) =
                SlaCalculator.ComputeDueDates(policy, entity.CreatedAtUtc);

            if (entity.FirstRespondedAtUtc is null)
            {
                entity.FirstResponseDueAtUtc = firstResponseDueAtUtc;
            }
            if (entity.ResolvedAtUtc is null)
            {
                entity.ResolutionDueAtUtc = resolutionDueAtUtc;
            }
        }
        else
        {
            if (entity.FirstRespondedAtUtc is null)
            {
                entity.FirstResponseDueAtUtc = null;
            }
            if (entity.ResolvedAtUtc is null)
            {
                entity.ResolutionDueAtUtc = null;
            }
        }

        if (oldPolicyId != entity.SlaPolicyId)
        {
            db.TicketHistory.Add(new TicketHistoryEntry
            {
                Id = Guid.NewGuid(),
                TicketId = entity.Id,
                ChangeType = TicketChangeType.SlaRecalculated,
                OldValue = oldPolicyId?.ToString(),
                NewValue = entity.SlaPolicyId?.ToString(),
                ChangedByUserId = actorUserId,
                ChangedAtUtc = nowUtc,
            });
        }

        await db.SaveChangesAsync(ct);
        return new TicketEscalationResult(true, null);
    }
}
