namespace CRM.Api.Tickets;

public class TicketMessage
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    // No cross-context navigation to the author — same style as CustomerNote.AuthorId;
    // display name is resolved via a separate AuthDbContext query at read time.
    public Guid AuthorUserId { get; set; }

    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
