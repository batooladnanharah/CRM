using System.Security.Claims;
using CRM.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.QuickReplies;

public static class QuickReplyEndpoints
{
    public static void MapQuickReplyEndpoints(this IEndpointRouteBuilder app)
    {
        var quickReplies = app.MapGroup("/api/quick-replies");

        quickReplies.MapGet("/", async (string? search, QuickReplyDbContext db) =>
        {
            IQueryable<QuickReply> filtered = db.QuickReplies.AsNoTracking().Where(q => q.IsActive);

            var term = search?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(term))
            {
                filtered = filtered.Where(q =>
                    q.Title.ToLower().Contains(term) || q.Content.ToLower().Contains(term));
            }

            var items = await filtered
                .OrderBy(q => q.Title)
                .Select(q => new QuickReplyResponse(
                    q.Id, q.Title, q.Content, q.IsActive, q.CreatedAtUtc, q.UpdatedAtUtc))
                .ToListAsync();

            return Results.Ok(items);
        })
        .RequireAuthorization(Permissions.QuickRepliesView)
        .WithName("ListQuickReplies")
        .WithTags("QuickReplies");

        quickReplies.MapPost("/", async (
            CreateQuickReplyRequest request, QuickReplyDbContext db, ClaimsPrincipal principal) =>
        {
            var title = request.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(title))
            {
                return Results.BadRequest(new ErrorResponse("Title is required."));
            }
            if (title.Length > 120)
            {
                return Results.BadRequest(new ErrorResponse("Title must be 120 characters or fewer."));
            }

            var content = request.Content?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(content))
            {
                return Results.BadRequest(new ErrorResponse("Content is required."));
            }
            if (content.Length > 4000)
            {
                return Results.BadRequest(new ErrorResponse("Content must be 4000 characters or fewer."));
            }

            var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var now = DateTime.UtcNow;

            var entity = new QuickReply
            {
                Id = Guid.NewGuid(),
                Title = title,
                Content = content,
                IsActive = true,
                CreatedByUserId = actorId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            db.QuickReplies.Add(entity);
            await db.SaveChangesAsync();

            var response = new QuickReplyResponse(
                entity.Id, entity.Title, entity.Content, entity.IsActive, entity.CreatedAtUtc, entity.UpdatedAtUtc);
            return Results.Created($"/api/quick-replies/{entity.Id}", response);
        })
        .RequireAuthorization(Permissions.QuickRepliesManage)
        .WithName("CreateQuickReply")
        .WithTags("QuickReplies");

        quickReplies.MapPut("/{id:guid}", async (
            Guid id, UpdateQuickReplyRequest request, QuickReplyDbContext db) =>
        {
            var entity = await db.QuickReplies.FirstOrDefaultAsync(q => q.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var title = request.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(title))
            {
                return Results.BadRequest(new ErrorResponse("Title is required."));
            }
            if (title.Length > 120)
            {
                return Results.BadRequest(new ErrorResponse("Title must be 120 characters or fewer."));
            }

            var content = request.Content?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(content))
            {
                return Results.BadRequest(new ErrorResponse("Content is required."));
            }
            if (content.Length > 4000)
            {
                return Results.BadRequest(new ErrorResponse("Content must be 4000 characters or fewer."));
            }

            entity.Title = title;
            entity.Content = content;
            entity.IsActive = request.IsActive;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var response = new QuickReplyResponse(
                entity.Id, entity.Title, entity.Content, entity.IsActive, entity.CreatedAtUtc, entity.UpdatedAtUtc);
            return Results.Ok(response);
        })
        .RequireAuthorization(Permissions.QuickRepliesManage)
        .WithName("UpdateQuickReply")
        .WithTags("QuickReplies");

        quickReplies.MapDelete("/{id:guid}", async (Guid id, QuickReplyDbContext db) =>
        {
            var entity = await db.QuickReplies.FirstOrDefaultAsync(q => q.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            // Hard delete for MVP simplicity — matches how the ticket module
            // deletes secondary rows (e.g. ticket attachments).
            db.QuickReplies.Remove(entity);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.QuickRepliesManage)
        .WithName("DeleteQuickReply")
        .WithTags("QuickReplies");
    }
}
