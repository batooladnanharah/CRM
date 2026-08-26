using CRM.Api.Auth;
using CRM.Api.Sla;
using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Reports;

// Read-only aggregation over existing tables — no new DbContext, no
// migration. Reuses SlaCalculator.ComputeStatus (the same classifier
// SlaEvaluator uses) rather than re-deriving the at-risk threshold here.
public sealed class ReportsService(TicketDbContext ticketDb, AuthDbContext authDb)
{
    private const int TopAgentCount = 10;

    private static readonly TicketStatus[] OpenStatuses = [TicketStatus.Open, TicketStatus.InProgress];
    private static readonly TicketStatus[] ResolvedStatuses = [TicketStatus.Resolved, TicketStatus.Closed];

    public async Task<ReportsSummaryResponse> GetSummaryAsync(CancellationToken ct)
    {
        // Every aggregate below is derived from this same in-memory snapshot —
        // one round trip, rather than a separate COUNT/AVG query per metric.
        // SlaCalculator.ComputeStatus is a pure C# method, so it cannot be
        // pushed into a SQL translation anyway; aggregating in memory here
        // is both simpler and unavoidable for the SLA classification step.
        var tickets = await ticketDb.Tickets.AsNoTracking().ToListAsync(ct);

        var volume = new TicketVolumeResponse(
            tickets.Count,
            tickets.Count(t => OpenStatuses.Contains(t.Status)),
            tickets.Count(t => ResolvedStatuses.Contains(t.Status)));

        var statusDistribution = Enum.GetValues<TicketStatus>()
            .Select(status => new StatusCountResponse(status, tickets.Count(t => t.Status == status)))
            .ToList();

        var agentPerformance = await BuildAgentPerformanceAsync(tickets, ct);
        var slaPerformance = BuildSlaPerformance(tickets);
        var resolution = BuildResolutionMetrics(tickets);

        return new ReportsSummaryResponse(volume, statusDistribution, agentPerformance, slaPerformance, resolution);
    }

    private async Task<IReadOnlyList<AgentPerformanceResponse>> BuildAgentPerformanceAsync(
        IReadOnlyList<Ticket> tickets, CancellationToken ct)
    {
        var topAssignees = tickets
            .Where(t => t.AssigneeUserId is not null)
            .GroupBy(t => t.AssigneeUserId!.Value)
            .Select(g => new { AgentId = g.Key, TicketCount = g.Count() })
            .OrderByDescending(g => g.TicketCount)
            .Take(TopAgentCount)
            .ToList();

        var agentIds = topAssignees.Select(a => a.AgentId).ToList();
        var agentNames = await authDb.Users.AsNoTracking()
            .Where(u => agentIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        return topAssignees
            .Select(a => new AgentPerformanceResponse(
                a.AgentId,
                // A deleted/disabled agent's row is never dropped silently —
                // it still counts toward performance, just under a fallback label.
                agentNames.GetValueOrDefault(a.AgentId, "(unknown)"),
                a.TicketCount))
            .ToList();
    }

    private static SlaPerformanceResponse BuildSlaPerformance(IReadOnlyList<Ticket> tickets)
    {
        var nowUtc = DateTime.UtcNow;
        var withinSla = 0;
        var atRisk = 0;
        var breached = 0;

        // Only tickets with an SLA policy actually applied are "evaluated" —
        // a ticket that never matched any policy has no due dates and would
        // otherwise misleadingly inflate or deflate the denominator.
        foreach (var ticket in tickets.Where(t => t.SlaPolicyId is not null))
        {
            var status = SlaCalculator.ComputeStatus(
                nowUtc, ticket.CreatedAtUtc, ticket.ResolutionDueAtUtc, ticket.ResolvedAtUtc);

            switch (status)
            {
                case SlaStatus.Met:
                case SlaStatus.OnTrack:
                    withinSla++;
                    break;
                case SlaStatus.AtRisk:
                    atRisk++;
                    break;
                case SlaStatus.Breached:
                    breached++;
                    break;
                case SlaStatus.NotApplicable:
                default:
                    // Shouldn't occur once SlaPolicyId is set (due dates are
                    // stamped at the same time), but never silently count it.
                    break;
            }
        }

        var totalEvaluated = withinSla + atRisk + breached;
        return new SlaPerformanceResponse(
            totalEvaluated, withinSla, atRisk, breached,
            PercentOf(withinSla, totalEvaluated), PercentOf(atRisk, totalEvaluated), PercentOf(breached, totalEvaluated));
    }

    private static ResolutionMetricsResponse BuildResolutionMetrics(IReadOnlyList<Ticket> tickets)
    {
        var resolvedTickets = tickets
            .Where(t => ResolvedStatuses.Contains(t.Status) && t.ResolvedAtUtc is not null)
            .ToList();

        double? averageResolutionMinutes = resolvedTickets.Count > 0
            ? resolvedTickets.Average(t => (t.ResolvedAtUtc!.Value - t.CreatedAtUtc).TotalMinutes)
            : null;

        return new ResolutionMetricsResponse(resolvedTickets.Count, averageResolutionMinutes);
    }

    private static int PercentOf(int count, int total) =>
        total == 0 ? 0 : (int)Math.Round(count * 100.0 / total, MidpointRounding.AwayFromZero);
}
