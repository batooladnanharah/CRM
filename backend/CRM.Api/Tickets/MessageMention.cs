namespace CRM.Api.Tickets;

public class MessageMention
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }

    // No cross-context navigation to the mentioned user — same style as
    // TicketMessage.AuthorUserId; display name resolved via AuthDbContext at read time.
    public Guid UserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
