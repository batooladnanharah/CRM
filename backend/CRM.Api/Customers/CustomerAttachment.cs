namespace CRM.Api.Customers;

public class CustomerAttachment
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }

    // No navigation property to Customer, and no cross-context navigation to the
    // uploading user — same one-directional FK style as CustomerNote/CustomerInteraction.
    public Guid UploadedByUserId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
