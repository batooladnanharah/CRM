namespace CRM.Api.Tickets;

public interface ITicketAssignmentService
{
    // Returns the selected agent's user id, or null when auto-assignment is
    // disabled or no eligible agent exists. Never throws for "no agent found" —
    // only for genuine infrastructure failures, which callers (TicketCreationService)
    // must catch so ticket creation itself never fails.
    Task<Guid?> TryAutoAssignAsync(Ticket ticket, CancellationToken ct);
}
