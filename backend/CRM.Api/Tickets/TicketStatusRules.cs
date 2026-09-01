namespace CRM.Api.Tickets;

public static class TicketStatusRules
{
    // Source status -> allowed target statuses. Closed is terminal.
    private static readonly Dictionary<TicketStatus, TicketStatus[]> AllowedTransitions = new()
    {
        [TicketStatus.Open] = [TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Closed],
        [TicketStatus.InProgress] = [TicketStatus.Open, TicketStatus.Resolved, TicketStatus.Closed],
        [TicketStatus.Resolved] = [TicketStatus.Open, TicketStatus.Closed],
        [TicketStatus.Closed] = [],
    };

    public static bool IsAllowedTransition(TicketStatus from, TicketStatus to)
        => from == to || AllowedTransitions[from].Contains(to);

    // Terminal statuses excluded from active-workload counts (CRM-62 auto-assignment)
    // and from anything else that means "still open work" — a single source of
    // truth so no caller re-enumerates TicketStatus itself.
    public static bool IsActiveWorkloadStatus(TicketStatus status)
        => status is TicketStatus.Open or TicketStatus.InProgress;
}
