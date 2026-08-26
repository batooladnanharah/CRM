using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Customers;

public static class CustomerNoteEndpoints
{
    public static void MapCustomerNoteEndpoints(this IEndpointRouteBuilder app)
    {
        // Internal notes are never exposed to the customer role — reuse the
        // existing AgentOrAdmin policy (admin/agent only) rather than a new one.
        var notes = app.MapGroup("/api/customers/{customerId:guid}/notes")
            .RequireAuthorization(Permissions.CustomersManage)
            .WithTags("CustomerNotes");

        notes.MapGet("/", async (Guid customerId, CustomerDbContext db, AuthDbContext authDb) =>
        {
            var customerExists = await db.Customers.AsNoTracking().AnyAsync(c => c.Id == customerId);
            if (!customerExists)
            {
                return Results.NotFound();
            }

            var entities = await db.CustomerNotes
                .AsNoTracking()
                .Where(n => n.CustomerId == customerId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .ThenByDescending(n => n.Id)
                .ToListAsync();

            var responses = await ToResponsesAsync(entities, authDb);
            return Results.Ok(responses);
        })
        .WithName("ListCustomerNotes");

        notes.MapPost("/", async (
            Guid customerId, CreateCustomerNoteRequest request,
            CustomerDbContext db, AuthDbContext authDb, ClaimsPrincipal principal, ILogger<Program> log,
            IAuditLogger auditLogger) =>
        {
            var content = request.Content?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(content))
            {
                return Results.BadRequest(new ErrorResponse("Content is required."));
            }
            if (content.Length > 4000)
            {
                return Results.BadRequest(new ErrorResponse("Content must be 4000 characters or fewer."));
            }

            var customerExists = await db.Customers.AsNoTracking().AnyAsync(c => c.Id == customerId);
            if (!customerExists)
            {
                return Results.NotFound();
            }

            var authorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var entity = new CustomerNote
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                AuthorId = authorId,
                Content = content,
                CreatedAtUtc = DateTime.UtcNow,
            };

            db.CustomerNotes.Add(entity);
            await db.SaveChangesAsync();

            log.LogInformation(
                "customer_note create customerId={CustomerId} noteId={NoteId} actor={ActorId}",
                customerId, entity.Id, authorId);
            await auditLogger.WriteAsync(
                AuditActions.CustomerNoteAdded, targetType: "customer", targetId: customerId.ToString());

            var response = (await ToResponsesAsync([entity], authDb))[0];
            return Results.Created($"/api/customers/{customerId}/notes/{entity.Id}", response);
        })
        .WithName("CreateCustomerNote");

        notes.MapPut("/{noteId:guid}", async (
            Guid customerId, Guid noteId, UpdateCustomerNoteRequest request,
            CustomerDbContext db, AuthDbContext authDb, ClaimsPrincipal principal, ILogger<Program> log,
            IAuditLogger auditLogger) =>
        {
            var entity = await db.CustomerNotes
                .FirstOrDefaultAsync(n => n.Id == noteId && n.CustomerId == customerId);
            if (entity is null)
            {
                return Results.NotFound();
            }

            if (!CanModify(entity, principal))
            {
                return Results.Forbid();
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

            entity.Content = content;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            log.LogInformation(
                "customer_note update customerId={CustomerId} noteId={NoteId} actor={ActorId}",
                customerId, entity.Id, actorId);
            await auditLogger.WriteAsync(
                AuditActions.CustomerNoteUpdated, targetType: "customer", targetId: customerId.ToString());

            var response = (await ToResponsesAsync([entity], authDb))[0];
            return Results.Ok(response);
        })
        .WithName("UpdateCustomerNote");

        notes.MapDelete("/{noteId:guid}", async (
            Guid customerId, Guid noteId,
            CustomerDbContext db, ClaimsPrincipal principal, ILogger<Program> log, IAuditLogger auditLogger) =>
        {
            var entity = await db.CustomerNotes
                .FirstOrDefaultAsync(n => n.Id == noteId && n.CustomerId == customerId);
            if (entity is null)
            {
                return Results.NotFound();
            }

            if (!CanModify(entity, principal))
            {
                return Results.Forbid();
            }

            db.CustomerNotes.Remove(entity);
            await db.SaveChangesAsync();

            var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            log.LogInformation(
                "customer_note delete customerId={CustomerId} noteId={NoteId} actor={ActorId}",
                customerId, noteId, actorId);
            await auditLogger.WriteAsync(
                AuditActions.CustomerNoteRemoved, targetType: "customer", targetId: customerId.ToString());

            return Results.NoContent();
        })
        .WithName("DeleteCustomerNote");
    }

    private static bool CanModify(CustomerNote note, ClaimsPrincipal user)
        => user.IsInRole(Roles.Admin)
           || (Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) && uid == note.AuthorId);

    private static async Task<List<CustomerNoteResponse>> ToResponsesAsync(
        IReadOnlyList<CustomerNote> notes, AuthDbContext authDb)
    {
        var authorIds = notes.Select(n => n.AuthorId).Distinct().ToList();
        var authorNames = await authDb.Users
            .AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        return notes
            .Select(n => new CustomerNoteResponse(
                n.Id,
                n.CustomerId,
                n.AuthorId,
                authorNames.GetValueOrDefault(n.AuthorId, string.Empty),
                n.Content,
                n.CreatedAtUtc,
                n.UpdatedAtUtc))
            .ToList();
    }
}
