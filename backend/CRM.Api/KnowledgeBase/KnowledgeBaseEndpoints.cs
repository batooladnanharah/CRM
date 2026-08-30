using System.Security.Claims;
using System.Text.RegularExpressions;
using CRM.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.KnowledgeBase;

public static class KnowledgeBaseEndpoints
{
    private static readonly Regex SlugPattern = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    public static void MapKnowledgeBaseEndpoints(this IEndpointRouteBuilder app)
    {
        var articles = app.MapGroup("/api/knowledge-base/articles");

        articles.MapGet("/", async ([AsParameters] KnowledgeBaseListQuery query, KnowledgeBaseDbContext db) =>
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            IQueryable<KnowledgeBaseArticle> filtered = db.Articles.AsNoTracking();

            if (query.Status is not null)
            {
                filtered = filtered.Where(a => a.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.Tag))
            {
                var tag = query.Tag.Trim();
                filtered = filtered.Where(a => a.Tags.Contains(tag));
            }

            if (query.CategoryId is not null)
            {
                filtered = filtered.Where(a => a.CategoryId == query.CategoryId);
            }

            var total = await filtered.CountAsync();
            // Materialize with the category included, then map in memory —
            // ToResponse can't be translated to SQL directly, and the
            // expected row count for an internal help-article table makes
            // this a non-issue (same rationale as the /search endpoint).
            var entities = await filtered
                .Include(a => a.Category)
                .OrderByDescending(a => a.UpdatedAtUtc)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var items = entities.Select(ToResponse).ToList();

            return Results.Ok(new KnowledgeBaseSearchResultResponse(items, total));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseView)
        .WithName("ListKnowledgeBaseArticles")
        .WithTags("KnowledgeBase");

        // CRM-66: full-text search across title, content, and category name,
        // with deterministic relevance ordering and pagination. See
        // KnowledgeBaseSearchService for the shared query/ranking logic used
        // by both this endpoint and the customer-portal search endpoint.
        // Drafts are only included when the caller both requests them
        // (IncludeDrafts=true) and holds the KB manage permission — anyone
        // else transparently gets published-only results even if they asked
        // for drafts.
        articles.MapGet("/search", async (
            [AsParameters] KnowledgeBaseSearchRequestQuery query, KnowledgeBaseDbContext db, ClaimsPrincipal principal,
            CancellationToken ct) =>
        {
            var errorCode = KnowledgeBaseSearchService.ValidateQuery(query.Q, out var trimmedQuery);
            if (errorCode is not null)
            {
                return Results.BadRequest(new ErrorResponse(errorCode));
            }

            var (page, pageSize) = KnowledgeBaseSearchService.ClampPaging(query.Page, query.PageSize);
            var canManageDrafts = principal.HasClaim("permission", Permissions.KnowledgeBaseManage);

            var (items, totalCount) = await KnowledgeBaseSearchService.SearchAsync(
                db,
                new KnowledgeBaseSearchService.Options(
                    trimmedQuery, query.CategoryId, canManageDrafts, query.IncludeDrafts ?? false, page, pageSize),
                ct);

            return Results.Ok(new KnowledgeBaseSearchResponse(items, page, pageSize, totalCount));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseView)
        .WithName("SearchKnowledgeBaseArticles")
        .WithTags("KnowledgeBase");

        articles.MapGet("/by-slug/{slug}", async (string slug, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Articles.AsNoTracking().Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Slug == slug.Trim().ToLowerInvariant());
            return entity is null ? Results.NotFound() : Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseView)
        .WithName("GetKnowledgeBaseArticleBySlug")
        .WithTags("KnowledgeBase");

        articles.MapGet("/{id:guid}", async (Guid id, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Articles.AsNoTracking().Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == id);
            return entity is null ? Results.NotFound() : Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseView)
        .WithName("GetKnowledgeBaseArticle")
        .WithTags("KnowledgeBase");

        articles.MapPost("/", async (
            CreateKnowledgeBaseArticleRequest request, KnowledgeBaseDbContext db, ClaimsPrincipal principal) =>
        {
            var validationError = Validate(
                request.Title, request.Slug, request.Body, request.Tags, request.Status,
                out var title, out var slug, out var body, out var tags, out var status);
            if (validationError is not null)
            {
                return validationError;
            }

            var slugExists = await db.Articles.AsNoTracking().AnyAsync(a => a.Slug == slug);
            if (slugExists)
            {
                return Results.Conflict(new ErrorResponse("slug_conflict"));
            }

            var categoryError = await ValidateCategoryAsync(request.CategoryId, db);
            if (categoryError is not null)
            {
                return categoryError;
            }

            var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var now = DateTime.UtcNow;

            var entity = new KnowledgeBaseArticle
            {
                Id = Guid.NewGuid(),
                Title = title,
                Slug = slug,
                Body = body,
                Tags = tags,
                Status = status,
                AuthorId = actorId,
                CategoryId = request.CategoryId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                PublishedAtUtc = status == KnowledgeBaseArticleStatus.Published ? now : null,
            };

            db.Articles.Add(entity);
            await db.SaveChangesAsync();

            var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == entity.CategoryId);
            return Results.Created($"/api/knowledge-base/articles/{entity.Id}", ToResponse(entity, category));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseManage)
        .WithName("CreateKnowledgeBaseArticle")
        .WithTags("KnowledgeBase");

        articles.MapPut("/{id:guid}", async (
            Guid id, UpdateKnowledgeBaseArticleRequest request, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Articles.Include(a => a.Category).FirstOrDefaultAsync(a => a.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var validationError = Validate(
                request.Title, request.Slug, request.Body, request.Tags, request.Status,
                out var title, out var slug, out var body, out var tags, out var status);
            if (validationError is not null)
            {
                return validationError;
            }

            var slugConflict = await db.Articles.AsNoTracking().AnyAsync(a => a.Slug == slug && a.Id != id);
            if (slugConflict)
            {
                return Results.Conflict(new ErrorResponse("slug_conflict"));
            }

            // Only re-validate the category (exists + active) when it's
            // actually being changed — an article whose category was
            // deactivated after the fact can still be edited without the
            // caller having to switch it to a different category first.
            if (request.CategoryId != entity.CategoryId)
            {
                var categoryError = await ValidateCategoryAsync(request.CategoryId, db);
                if (categoryError is not null)
                {
                    return categoryError;
                }

                entity.CategoryId = request.CategoryId;
                entity.Category = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId);
            }

            entity.Title = title;
            entity.Slug = slug;
            entity.Body = body;
            entity.Tags = tags;

            // Draft/Archived -> Published stamps PublishedAtUtc the first time
            // only; Published -> Draft/Archived never clears it, so the
            // article retains its original first-publish timestamp.
            if (status == KnowledgeBaseArticleStatus.Published && entity.PublishedAtUtc is null)
            {
                entity.PublishedAtUtc = DateTime.UtcNow;
            }

            entity.Status = status;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseManage)
        .WithName("UpdateKnowledgeBaseArticle")
        .WithTags("KnowledgeBase");

        articles.MapDelete("/{id:guid}", async (Guid id, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            db.Articles.Remove(entity);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.KnowledgeBaseManage)
        .WithName("DeleteKnowledgeBaseArticle")
        .WithTags("KnowledgeBase");

        // Dedicated publish/unpublish actions alongside the generic PUT above
        // (kept for callers that already depend on it). Publish always stamps
        // PublishedAtUtc to "now" (even on an already-published article, so a
        // deliberate re-publish refreshes it); unpublish moves the article to
        // Draft and never touches Body/Title/Tags. Idempotent: calling either
        // action from a state that's already the target status still
        // succeeds and returns the current article, matching the pattern
        // used by TicketEndpoints for repeated status transitions.
        articles.MapPost("/{id:guid}/publish", async (Guid id, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Articles.Include(a => a.Category).FirstOrDefaultAsync(a => a.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.Status = KnowledgeBaseArticleStatus.Published;
            entity.PublishedAtUtc = DateTime.UtcNow;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseManage)
        .WithName("PublishKnowledgeBaseArticle")
        .WithTags("KnowledgeBase");

        articles.MapPost("/{id:guid}/unpublish", async (Guid id, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Articles.Include(a => a.Category).FirstOrDefaultAsync(a => a.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            // Status only -> Draft. Content (Title/Body/Tags/Slug) and
            // PublishedAtUtc (first-publish history) are left untouched.
            entity.Status = KnowledgeBaseArticleStatus.Draft;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseManage)
        .WithName("UnpublishKnowledgeBaseArticle")
        .WithTags("KnowledgeBase");
    }

    private static IResult? Validate(
        string? rawTitle, string? rawSlug, string? rawBody, string[]? rawTags, string? rawStatus,
        out string title, out string slug, out string body, out string[] tags,
        out KnowledgeBaseArticleStatus status)
    {
        title = rawTitle?.Trim() ?? string.Empty;
        slug = rawSlug?.Trim().ToLowerInvariant() ?? string.Empty;
        body = rawBody ?? string.Empty;
        tags = (rawTags ?? []).Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
        status = KnowledgeBaseArticleStatus.Draft;

        if (string.IsNullOrEmpty(title))
        {
            return Results.BadRequest(new ErrorResponse("Title is required."));
        }
        if (title.Length > 200)
        {
            return Results.BadRequest(new ErrorResponse("Title must be 200 characters or fewer."));
        }

        if (string.IsNullOrEmpty(slug))
        {
            return Results.BadRequest(new ErrorResponse("Slug is required."));
        }
        if (slug.Length > 200)
        {
            return Results.BadRequest(new ErrorResponse("Slug must be 200 characters or fewer."));
        }
        if (!SlugPattern.IsMatch(slug))
        {
            return Results.BadRequest(new ErrorResponse(
                "Slug must contain only lowercase letters, numbers, and hyphens, and cannot start or end with a hyphen."));
        }

        if (body.Length > 20000)
        {
            return Results.BadRequest(new ErrorResponse("Body must be 20000 characters or fewer."));
        }

        if (tags.Length > 20)
        {
            return Results.BadRequest(new ErrorResponse("A maximum of 20 tags is allowed."));
        }
        if (tags.Any(t => t.Length > 40))
        {
            return Results.BadRequest(new ErrorResponse("Each tag must be 40 characters or fewer."));
        }

        if (!string.IsNullOrWhiteSpace(rawStatus) && !Enum.TryParse(rawStatus, ignoreCase: true, out status))
        {
            return Results.BadRequest(new ErrorResponse(
                $"Unknown status '{rawStatus}'. Allowed values: {string.Join(", ", Enum.GetNames<KnowledgeBaseArticleStatus>())}."));
        }

        return null;
    }

    // Applies to both create (CategoryId always supplied and always
    // re-validated) and update (only called when CategoryId is changing).
    // 404 when the id doesn't exist at all; 422 when it exists but is
    // inactive — distinct from the 400s Validate() returns for malformed
    // input, since the category id itself is syntactically fine.
    private static async Task<IResult?> ValidateCategoryAsync(Guid categoryId, KnowledgeBaseDbContext db)
    {
        if (categoryId == Guid.Empty)
        {
            return Results.BadRequest(new ErrorResponse("CategoryId is required."));
        }

        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId);
        if (category is null)
        {
            return Results.NotFound(new ErrorResponse("Category not found."));
        }
        if (!category.IsActive)
        {
            return Results.Json(
                new ErrorResponse("Cannot assign an inactive category."),
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        return null;
    }

    private static KnowledgeBaseArticleResponse ToResponse(KnowledgeBaseArticle a) => ToResponse(a, a.Category);

    private static KnowledgeBaseArticleResponse ToResponse(KnowledgeBaseArticle a, KnowledgeBaseCategory? category)
    {
        // The referenced category can be null here even though CategoryId is
        // non-null if it was hard-deleted out-of-band (e.g. directly against
        // the DB) — the article stays reachable and simply reports no
        // embedded category rather than failing to load.
        var categoryDto = category is null
            ? null
            : new KnowledgeBaseArticleCategoryDto(category.Id, category.Name, category.IsActive);

        return new KnowledgeBaseArticleResponse(
            a.Id, a.Title, a.Slug, a.Body, a.Tags, a.Status, a.AuthorId, a.CategoryId, categoryDto,
            a.CreatedAtUtc, a.UpdatedAtUtc, a.PublishedAtUtc);
    }
}
