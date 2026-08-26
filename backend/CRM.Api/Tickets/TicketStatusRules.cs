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
}
