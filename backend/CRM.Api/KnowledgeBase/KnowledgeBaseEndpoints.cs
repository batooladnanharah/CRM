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

            var total = await filtered.CountAsync();
            var items = await filtered
                .OrderByDescending(a => a.UpdatedAtUtc)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => ToResponse(a))
                .ToListAsync();

            return Results.Ok(new KnowledgeBaseSearchResultResponse(items, total));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseView)
        .WithName("ListKnowledgeBaseArticles")
        .WithTags("KnowledgeBase");

        // Ranking (title match first, then most-recently-updated) requires a
        // conditional ordering that isn't reliably translatable by every EF
        // Core provider, so the matched set is materialized first and then
        // ordered/paged in memory — the expected row count for an internal
        // help-article table makes this a non-issue.
        articles.MapGet("/search", async ([AsParameters] KnowledgeBaseSearchQuery query, KnowledgeBaseDbContext db) =>
        {
            var term = query.Q?.Trim();
            if (string.IsNullOrEmpty(term) || term.Length < 2)
            {
                return Results.BadRequest(new ErrorResponse("q_too_short"));
            }

            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var termLower = term.ToLowerInvariant();

            IQueryable<KnowledgeBaseArticle> filtered = db.Articles.AsNoTracking().Where(a =>
                a.Title.ToLower().Contains(termLower) || a.Body.ToLower().Contains(termLower));

            if (query.Status is not null)
            {
                filtered = filtered.Where(a => a.Status == query.Status);
            }

            if (!string.IsNullOrWhiteSpace(query.Tag))
            {
                var tag = query.Tag.Trim();
                filtered = filtered.Where(a => a.Tags.Contains(tag));
            }

            var matches = await filtered.ToListAsync();
            var ranked = matches
                .OrderBy(a => a.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenByDescending(a => a.UpdatedAtUtc)
                .ToList();

            var total = ranked.Count;
            var items = ranked
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToResponse)
                .ToList();

            return Results.Ok(new KnowledgeBaseSearchResultResponse(items, total));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseView)
        .WithName("SearchKnowledgeBaseArticles")
        .WithTags("KnowledgeBase");

        articles.MapGet("/by-slug/{slug}", async (string slug, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Articles.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Slug == slug.Trim().ToLowerInvariant());
            return entity is null ? Results.NotFound() : Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseView)
        .WithName("GetKnowledgeBaseArticleBySlug")
        .WithTags("KnowledgeBase");

        articles.MapGet("/{id:guid}", async (Guid id, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
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
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                PublishedAtUtc = status == KnowledgeBaseArticleStatus.Published ? now : null,
            };

            db.Articles.Add(entity);
            await db.SaveChangesAsync();

            return Results.Created($"/api/knowledge-base/articles/{entity.Id}", ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseManage)
        .WithName("CreateKnowledgeBaseArticle")
        .WithTags("KnowledgeBase");

        articles.MapPut("/{id:guid}", async (
            Guid id, UpdateKnowledgeBaseArticleRequest request, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Articles.FirstOrDefaultAsync(a => a.Id == id);
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

    private static KnowledgeBaseArticleResponse ToResponse(KnowledgeBaseArticle a) => new(
        a.Id, a.Title, a.Slug, a.Body, a.Tags, a.Status, a.AuthorId, a.CreatedAtUtc, a.UpdatedAtUtc, a.PublishedAtUtc);
}
