namespace CRM.Api.CommunicationChannels;

public class EmailMessage
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }

    // Nullable, unenforced FK to Ticket (a different DbContext) — no
    // cross-context relational constraint, same style as Ticket.CustomerId.
    // A follow-up story should validate this id once channel-to-ticket
    // linkage is actually wired up.
    public Guid? TicketId { get; set; }
}
