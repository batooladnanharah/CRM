namespace CRM.Api.Sla;

// Pure computation helpers — no DB access, so response/list builders can call
// these directly without needing ISlaService injected everywhere.
public static class SlaCalculator
{
    // Fraction of the window elapsed at which an open (not-yet-occurred) SLA
    // clock flips from OnTrack to AtRisk.
    private const double AtRiskThreshold = 0.75;

    public static (DateTime FirstResponseDueAtUtc, DateTime ResolutionDueAtUtc) ComputeDueDates(
        SlaPolicy policy, DateTime fromUtc)
        => (fromUtc.AddMinutes(policy.FirstResponseMinutes), fromUtc.AddMinutes(policy.ResolutionMinutes));

    // startAtUtc is always the ticket's CreatedAtUtc — both the first-response
    // and resolution windows are measured from ticket creation.
    public static SlaStatus ComputeStatus(
        DateTime nowUtc, DateTime? startAtUtc, DateTime? dueAtUtc, DateTime? occurredAtUtc)
    {
        if (dueAtUtc is null || startAtUtc is null)
        {
            return SlaStatus.NotApplicable;
        }

        if (occurredAtUtc is not null)
        {
            return occurredAtUtc <= dueAtUtc ? SlaStatus.Met : SlaStatus.Breached;
        }

        if (nowUtc > dueAtUtc)
        {
            return SlaStatus.Breached;
        }

        var totalWindow = (dueAtUtc.Value - startAtUtc.Value).TotalMinutes;
        if (totalWindow <= 0)
        {
            // Degenerate/misconfigured window (due <= start) — treat as at risk
            // rather than silently reporting OnTrack.
            return SlaStatus.AtRisk;
        }

        var elapsedFraction = (nowUtc - startAtUtc.Value).TotalMinutes / totalWindow;
        return elapsedFraction >= AtRiskThreshold ? SlaStatus.AtRisk : SlaStatus.OnTrack;
    }
}
