namespace CRM.Api.Tickets;

public class TicketAttachment
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    // No cross-context navigation to the uploader — same style as CustomerAttachment.
    public Guid UploadedByUserId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
