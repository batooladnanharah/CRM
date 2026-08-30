namespace CRM.Api.KnowledgeBase;

// Minimal embedded category shape — enough for the article list/detail UI to
// render a name/badge and know whether the category is still active, without
// pulling in Description/timestamps that belong to the category endpoints.
public record KnowledgeBaseArticleCategoryDto(Guid Id, string Name, bool IsActive);

public record KnowledgeBaseArticleResponse(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string[] Tags,
    KnowledgeBaseArticleStatus Status,
    Guid AuthorId,
    Guid CategoryId,
    KnowledgeBaseArticleCategoryDto? Category,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? PublishedAtUtc);

public record CreateKnowledgeBaseArticleRequest(
    string Title,
    string Slug,
    string Body,
    string[]? Tags,
    string? Status,
    Guid CategoryId);

public record UpdateKnowledgeBaseArticleRequest(
    string Title,
    string Slug,
    string Body,
    string[]? Tags,
    string? Status,
    Guid CategoryId);

public record KnowledgeBaseListQuery(
    KnowledgeBaseArticleStatus? Status,
    string? Tag,
    Guid? CategoryId,
    int Page = 1,
    int PageSize = 20);

public record KnowledgeBaseSearchResultResponse(
    IReadOnlyList<KnowledgeBaseArticleResponse> Items,
    int Total);

// --- Full-text search (title + content + category name) contracts (CRM-66) ---
// Distinct from KnowledgeBaseSearchResultResponse above, which remains the
// response shape for the plain article LIST endpoint (status/tag/category
// filters only, no relevance ranking or excerpt).

public record KnowledgeBaseSearchRequestQuery(
    string? Q,
    Guid? CategoryId,
    bool? IncludeDrafts,
    int? Page,
    int? PageSize);

public record KnowledgeBaseSearchCategoryDto(Guid Id, string Name);

public record KnowledgeBaseSearchItemDto(
    Guid Id,
    string Title,
    KnowledgeBaseSearchCategoryDto Category,
    string Excerpt,
    string? Status);

public record KnowledgeBaseSearchResponse(
    IReadOnlyList<KnowledgeBaseSearchItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
