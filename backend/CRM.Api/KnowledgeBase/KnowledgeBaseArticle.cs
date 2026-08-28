using System.Text.Json.Serialization;

namespace CRM.Api.KnowledgeBase;

public class KnowledgeBaseArticle
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public KnowledgeBaseArticleStatus Status { get; set; } = KnowledgeBaseArticleStatus.Draft;

    // No cross-context navigation to the author — same style as QuickReply.CreatedByUserId.
    public Guid AuthorId { get; set; }

    public Guid CategoryId { get; set; }
    public KnowledgeBaseCategory? Category { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Set the first time an article transitions into Published; never cleared
    // afterward (Published -> Draft/Archived retains first-publish history).
    public DateTime? PublishedAtUtc { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KnowledgeBaseArticleStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}
