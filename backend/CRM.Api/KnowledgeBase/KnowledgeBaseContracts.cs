namespace CRM.Api.KnowledgeBase;

public record KnowledgeBaseArticleResponse(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string[] Tags,
    KnowledgeBaseArticleStatus Status,
    Guid AuthorId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? PublishedAtUtc);

public record CreateKnowledgeBaseArticleRequest(
    string Title,
    string Slug,
    string Body,
    string[]? Tags,
    string? Status);

public record UpdateKnowledgeBaseArticleRequest(
    string Title,
    string Slug,
    string Body,
    string[]? Tags,
    string? Status);

public record KnowledgeBaseListQuery(
    KnowledgeBaseArticleStatus? Status,
    string? Tag,
    int Page = 1,
    int PageSize = 20);

public record KnowledgeBaseSearchQuery(
    string? Q,
    string? Tag,
    KnowledgeBaseArticleStatus? Status,
    int Page = 1,
    int PageSize = 20);

public record KnowledgeBaseSearchResultResponse(
    IReadOnlyList<KnowledgeBaseArticleResponse> Items,
    int Total);
