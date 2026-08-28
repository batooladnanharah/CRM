using CRM.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.KnowledgeBase;

public static class KnowledgeBaseCategoryEndpoints
{
    public static void MapKnowledgeBaseCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var categories = app.MapGroup("/api/knowledge-base/categories");

        categories.MapGet("/", async (bool? activeOnly, KnowledgeBaseDbContext db) =>
        {
            IQueryable<KnowledgeBaseCategory> query = db.Categories.AsNoTracking();
            if (activeOnly == true)
            {
                query = query.Where(c => c.IsActive);
            }

            var items = await query
                .OrderBy(c => c.Name)
                .Select(c => ToResponse(c))
                .ToListAsync();

            return Results.Ok(items);
        })
        .RequireAuthorization(Permissions.KnowledgeBaseCategoriesView)
        .WithName("ListKnowledgeBaseCategories")
        .WithTags("KnowledgeBaseCategories");

        categories.MapGet("/{id:guid}", async (Guid id, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            return entity is null ? Results.NotFound() : Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseCategoriesView)
        .WithName("GetKnowledgeBaseCategory")
        .WithTags("KnowledgeBaseCategories");

        categories.MapPost("/", async (CreateKnowledgeBaseCategoryRequest request, KnowledgeBaseDbContext db) =>
        {
            var validationError = Validate(request.Name, request.Description, out var name, out var description);
            if (validationError is not null)
            {
                return validationError;
            }

            var nameLower = name.ToLowerInvariant();
            var duplicate = await db.Categories.AsNoTracking()
                .AnyAsync(c => c.Name.ToLower() == nameLower);
            if (duplicate)
            {
                return Results.Conflict(new ErrorResponse("A category with this name already exists."));
            }

            var now = DateTime.UtcNow;
            var entity = new KnowledgeBaseCategory
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                IsActive = request.IsActive ?? true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            db.Categories.Add(entity);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Defense-in-depth against a concurrent create racing the
                // upfront duplicate check between the AnyAsync above and this
                // SaveChangesAsync — the unique index is the real guard.
                return Results.Conflict(new ErrorResponse("A category with this name already exists."));
            }

            return Results.Created($"/api/knowledge-base/categories/{entity.Id}", ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseCategoriesManage)
        .WithName("CreateKnowledgeBaseCategory")
        .WithTags("KnowledgeBaseCategories");

        categories.MapPut("/{id:guid}", async (
            Guid id, UpdateKnowledgeBaseCategoryRequest request, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var validationError = Validate(request.Name, request.Description, out var name, out var description);
            if (validationError is not null)
            {
                return validationError;
            }

            var nameLower = name.ToLowerInvariant();
            var duplicate = await db.Categories.AsNoTracking()
                .AnyAsync(c => c.Id != id && c.Name.ToLower() == nameLower);
            if (duplicate)
            {
                return Results.Conflict(new ErrorResponse("A category with this name already exists."));
            }

            entity.Name = name;
            entity.Description = description;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new ErrorResponse("A category with this name already exists."));
            }

            return Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseCategoriesManage)
        .WithName("UpdateKnowledgeBaseCategory")
        .WithTags("KnowledgeBaseCategories");

        categories.MapPatch("/{id:guid}/status", async (
            Guid id, SetKnowledgeBaseCategoryStatusRequest request, KnowledgeBaseDbContext db) =>
        {
            var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.IsActive = request.IsActive;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.KnowledgeBaseCategoriesManage)
        .WithName("SetKnowledgeBaseCategoryStatus")
        .WithTags("KnowledgeBaseCategories");
    }

    private static IResult? Validate(
        string? rawName, string? rawDescription, out string name, out string? description)
    {
        name = rawName?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(rawDescription) ? null : rawDescription.Trim();

        if (string.IsNullOrEmpty(name))
        {
            return Results.BadRequest(new ErrorResponse("Name is required."));
        }
        if (name.Length > 120)
        {
            return Results.BadRequest(new ErrorResponse("Name must be 120 characters or fewer."));
        }
        if (description is { Length: > 1000 })
        {
            return Results.BadRequest(new ErrorResponse("Description must be 1000 characters or fewer."));
        }

        return null;
    }

    internal static KnowledgeBaseCategoryResponse ToResponse(KnowledgeBaseCategory c) => new(
        c.Id, c.Name, c.Description, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc);
}
