using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Sla;

public interface ISlaEvaluator
{
    // Exposed for direct unit testing — the caller supplies the ticket
    // (already tracked by the DbContext this evaluator was constructed with)
    // and is responsible for nothing else; this persists its own changes.
    Task<bool> EvaluateAsync(Ticket ticket, DateTime nowUtc, CancellationToken ct);

    Task<int> EvaluateAllOpenAsync(CancellationToken ct);
}

// Evaluates the SLA lifecycle for open tickets on each automation tick.
// Reuses SlaCalculator for all due-date/status math — never reimplements it.
// A breach is recorded (and escalation attempted) at most once per due-type;
// re-running this over the same ticket is a no-op beyond bumping
// SlaLastEvaluatedAtUtc. Note: a later policy change (e.g. via priority
// change) can move the due dates themselves, but an already-persisted
// *BreachedAtUtc timestamp is never rewritten or cleared.
public sealed class SlaEvaluator(
    TicketDbContext db, TicketEscalationService escalationService, ILogger<SlaEvaluator> logger) : ISlaEvaluator
{
    private const string AutoEscalationReason = "Automatically escalated due to an SLA breach.";

    public async Task<int> EvaluateAllOpenAsync(CancellationToken ct)
    {
        var openTickets = await db.Tickets
            .Where(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed)
            .ToListAsync(ct);

        var nowUtc = DateTime.UtcNow;
        var evaluatedCount = 0;

        foreach (var ticket in openTickets)
        {
            try
            {
                await EvaluateAsync(ticket, nowUtc, ct);
                evaluatedCount++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "sla_evaluation_failed ticketId={TicketId}", ticket.Id);
            }
        }

        return evaluatedCount;
    }

    public async Task<bool> EvaluateAsync(Ticket ticket, DateTime nowUtc, CancellationToken ct)
    {
        var breachedNow = false;

        var firstResponseStatus = SlaCalculator.ComputeStatus(
            nowUtc, ticket.CreatedAtUtc, ticket.FirstResponseDueAtUtc, ticket.FirstRespondedAtUtc);
        var resolutionStatus = SlaCalculator.ComputeStatus(
            nowUtc, ticket.CreatedAtUtc, ticket.ResolutionDueAtUtc, ticket.ResolvedAtUtc);

        if (firstResponseStatus == SlaStatus.Breached && ticket.FirstResponseBreachedAtUtc is null)
        {
            ticket.FirstResponseBreachedAtUtc = nowUtc;
            db.TicketHistory.Add(NewBreachEntry(ticket.Id, "FirstResponse", nowUtc));
            breachedNow = true;
        }

        if (resolutionStatus == SlaStatus.Breached && ticket.ResolutionBreachedAtUtc is null)
        {
            ticket.ResolutionBreachedAtUtc = nowUtc;
            db.TicketHistory.Add(NewBreachEntry(ticket.Id, "Resolution", nowUtc));
            breachedNow = true;
        }

        ticket.SlaLastEvaluatedAtUtc = nowUtc;

        if (breachedNow && ticket.SlaAutoEscalatedAtUtc is null)
        {
            ticket.SlaAutoEscalatedAtUtc = nowUtc;
            await escalationService.EscalateAsync(
                ticket, AutoEscalationReason, TicketEscalationService.SystemActorId, nowUtc, ct);
        }

        // EscalateAsync (when invoked) already saved its own changes on this
        // same tracked entity/context; calling SaveChangesAsync again here is
        // then a no-op. When escalation was not attempted, or failed
        // validation (e.g. already Urgent) without saving, this call is what
        // actually persists the breach fields/history recorded above.
        await db.SaveChangesAsync(ct);

        return breachedNow;
    }

    private static TicketHistoryEntry NewBreachEntry(Guid ticketId, string dueType, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        ChangeType = TicketChangeType.SlaBreached,
        OldValue = null,
        NewValue = dueType,
        ChangedByUserId = TicketEscalationService.SystemActorId,
        ChangedAtUtc = nowUtc,
    };
}
