namespace CRM.Api.QuickReplies;

public class QuickReply
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // No cross-context navigation to the creator — same style as CustomerNote.AuthorId.
    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
