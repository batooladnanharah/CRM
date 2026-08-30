using Microsoft.EntityFrameworkCore;

namespace CRM.Api.KnowledgeBase;

// Shared query composition for the agent (/api/knowledge-base/articles/search)
// and customer-portal (/api/customer/knowledge-base/search) search endpoints,
// so the matching/ordering/pagination rules can never drift between the two
// callers. Case-insensitive matching is done with ToLower()/Contains() rather
// than EF.Functions.ILike: EF Core's InMemory provider (used by the test
// suite) cannot translate ILike, while ToLower()/Contains() translates on
// both Npgsql (as LOWER(...) LIKE ...) and InMemory, and EF Core already
// escapes LIKE metacharacters (%, _) for the argument of a translated
// Contains() call, so no manual escaping is required here.
internal static class KnowledgeBaseSearchService
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    public sealed record Options(
        string Query,
        Guid? CategoryId,
        bool CanSeeDrafts,
        bool IncludeDrafts,
        int Page,
        int PageSize);

    public static async Task<(IReadOnlyList<KnowledgeBaseSearchItemDto> Items, int TotalCount)> SearchAsync(
        KnowledgeBaseDbContext db, Options options, CancellationToken ct = default)
    {
        var term = options.Query.Trim().ToLowerInvariant();

        IQueryable<KnowledgeBaseArticle> baseQuery = db.Articles.AsNoTracking().Include(a => a.Category);

        // Drafts are only ever visible when the caller both has draft
        // permission AND explicitly opted in; every other case (portal
        // callers always, agents without permission, agents who didn't ask)
        // is forced to published + active-category only.
        var showDrafts = options.CanSeeDrafts && options.IncludeDrafts;
        if (!showDrafts)
        {
            baseQuery = baseQuery.Where(a =>
                a.Status == KnowledgeBaseArticleStatus.Published && a.Category != null && a.Category.IsActive);
        }

        if (options.CategoryId is not null)
        {
            baseQuery = baseQuery.Where(a => a.CategoryId == options.CategoryId);
        }

        var matched = baseQuery.Where(a =>
            a.Title.ToLower().Contains(term) ||
            a.Body.ToLower().Contains(term) ||
            (a.Category != null && a.Category.Name.ToLower().Contains(term)));

        var totalCount = await matched.CountAsync(ct);

        var entities = await matched.ToListAsync(ct);

        var ordered = entities
            .Select(a => new
            {
                Article = a,
                TitleMatch = a.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                CategoryMatch = a.Category is not null &&
                    a.Category.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
                ContentMatch = a.Body.Contains(term, StringComparison.OrdinalIgnoreCase) ? 1 : 0,
            })
            .OrderByDescending(x => x.TitleMatch)
            .ThenByDescending(x => x.CategoryMatch)
            .ThenByDescending(x => x.ContentMatch)
            .ThenByDescending(x => x.Article.PublishedAtUtc)
            .ThenByDescending(x => x.Article.Id)
            .Skip((options.Page - 1) * options.PageSize)
            .Take(options.PageSize)
            .Select(x => ToDto(x.Article, showDrafts))
            .ToList();

        return (ordered, totalCount);
    }

    public static (int Page, int PageSize) ClampPaging(int? page, int? pageSize) =>
        (Math.Max(page ?? 1, 1), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));

    // Trims the raw query and validates length. Returns null when valid;
    // otherwise the stable error code the caller should return as a 400.
    public static string? ValidateQuery(string? rawQuery, out string trimmed)
    {
        trimmed = rawQuery?.Trim() ?? string.Empty;
        if (trimmed.Length < 2)
        {
            return "query_too_short";
        }
        if (trimmed.Length > 200)
        {
            return "query_too_long";
        }
        return null;
    }

    private static KnowledgeBaseSearchItemDto ToDto(KnowledgeBaseArticle a, bool includeStatus)
    {
        var category = a.Category is null
            ? new KnowledgeBaseSearchCategoryDto(a.CategoryId, string.Empty)
            : new KnowledgeBaseSearchCategoryDto(a.Category.Id, a.Category.Name);

        return new KnowledgeBaseSearchItemDto(
            a.Id,
            a.Title,
            category,
            KnowledgeBaseExcerpt.Build(a.Body),
            includeStatus ? a.Status.ToString() : null);
    }
}
