using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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
    TicketDbContext db, TicketEscalationService escalationService, IEscalationDispatcher escalationDispatcher,
    IOptions<SlaAutomationOptions> options, ILogger<SlaEvaluator> logger) : ISlaEvaluator
{
    private const string AutoEscalationReason = "Automatically escalated due to an SLA breach.";

    public async Task<int> EvaluateAllOpenAsync(CancellationToken ct)
    {
        // Never ToListAsync the whole open-ticket table — page with a bounded
        // Take(BatchSize) so a single cycle's cost stays flat regardless of
        // how many open tickets exist. Oldest-evaluated-first (nulls — never
        // evaluated — sort first) ensures a saturated instance still rotates
        // through the full backlog across successive ticks instead of always
        // re-touching the same head of the table.
        var batchSize = Math.Max(1, options.Value.BatchSize);
        var openTickets = await db.Tickets
            .Where(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed)
            .OrderBy(t => t.SlaLastEvaluatedAtUtc == null ? DateTime.MinValue : t.SlaLastEvaluatedAtUtc)
            .Take(batchSize)
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

        // CRM-63: dispatch escalation-rule notifications for AtRisk/Breached
        // objectives that aren't already completed. Idempotent via
        // EscalationDispatcher's unique-index dedupe, so calling this every
        // tick for a still-AtRisk ticket is safe/cheap (early-outs when no
        // active rule matches).
        if (ticket.FirstRespondedAtUtc is null)
        {
            if (firstResponseStatus == SlaStatus.Breached)
            {
                await escalationDispatcher.DispatchAsync(ticket, SlaObjectiveKind.Response, EscalationTrigger.Breached, ct);
            }
            else if (firstResponseStatus == SlaStatus.AtRisk)
            {
                await escalationDispatcher.DispatchAsync(ticket, SlaObjectiveKind.Response, EscalationTrigger.AtRisk, ct);
            }
        }

        if (ticket.ResolvedAtUtc is null)
        {
            if (resolutionStatus == SlaStatus.Breached)
            {
                await escalationDispatcher.DispatchAsync(ticket, SlaObjectiveKind.Resolution, EscalationTrigger.Breached, ct);
            }
            else if (resolutionStatus == SlaStatus.AtRisk)
            {
                await escalationDispatcher.DispatchAsync(ticket, SlaObjectiveKind.Resolution, EscalationTrigger.AtRisk, ct);
            }
        }

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
