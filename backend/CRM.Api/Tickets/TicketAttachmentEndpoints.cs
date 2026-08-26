using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.Customers.Attachments;
using CRM.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CRM.Api.Tickets;

public static class TicketAttachmentEndpoints
{
    public static void MapTicketAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        // Same policy as the customer attachments group, and reuses the exact
        // same IFileStorage/AttachmentsOptions singleton (shared storage root) —
        // ticket files are keyed under a "tickets/{ticketId}/" prefix so they
        // never collide with customer attachment keys in that shared root.
        var attachments = app.MapGroup("/api/tickets/{ticketId:guid}/attachments")
            .RequireAuthorization("AgentOrAdmin")
            .WithTags("TicketAttachments");

        attachments.MapGet("/", async (Guid ticketId, TicketDbContext db, AuthDbContext authDb) =>
        {
            var ticketExists = await db.Tickets.AsNoTracking().AnyAsync(t => t.Id == ticketId);
            if (!ticketExists)
            {
                return Results.NotFound();
            }

            var entities = await db.TicketAttachments
                .AsNoTracking()
                .Where(a => a.TicketId == ticketId)
                .OrderByDescending(a => a.CreatedAtUtc)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var responses = await ToResponsesAsync(entities, authDb);
            return Results.Ok(responses);
        })
        .WithName("ListTicketAttachments");

        attachments.MapPost("/", async (
            Guid ticketId, HttpRequest request, TicketDbContext db, AuthDbContext authDb,
            IFileStorage storage, IOptions<AttachmentsOptions> attachmentsOptions,
            ClaimsPrincipal principal, ILogger<Program> log, IAuditLogger auditLogger, CancellationToken ct) =>
        {
            var options = attachmentsOptions.Value;

            var ticketExists = await db.Tickets.AsNoTracking().AnyAsync(t => t.Id == ticketId, ct);
            if (!ticketExists)
            {
                return Results.NotFound();
            }

            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new ErrorResponse("Expected a multipart/form-data upload."));
            }

            var form = await request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");

            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new ErrorResponse("No file uploaded."));
            }

            if (file.Length > options.MaxFileSizeBytes)
            {
                return Results.BadRequest(new ErrorResponse(
                    $"File exceeds the maximum allowed size of {options.MaxFileSizeBytes} bytes."));
            }

            if (!options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ErrorResponse("File type is not allowed."));
            }

            var originalFileName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(originalFileName) ||
                originalFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return Results.BadRequest(new ErrorResponse("Invalid file name."));
            }
            if (originalFileName.Length > 255)
            {
                return Results.BadRequest(new ErrorResponse("File name must be 255 characters or fewer."));
            }

            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (!options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ErrorResponse("File extension is not allowed."));
            }

            var storageKey = $"tickets/{ticketId}/{Guid.NewGuid():N}{extension}";
            var uploadedBy = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await using var stream = file.OpenReadStream();
            try
            {
                await storage.SaveAsync(stream, storageKey, ct);
            }
            catch (StorageException ex)
            {
                log.LogError(ex, "Failed to save attachment to storage for ticket {TicketId}", ticketId);
                return Results.Json(new ErrorResponse("Could not save the attachment. Please try again."),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var entity = new TicketAttachment
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                UploadedByUserId = uploadedBy,
                OriginalFileName = originalFileName,
                StorageKey = storageKey,
                ContentType = file.ContentType,
                FileSize = file.Length,
                CreatedAtUtc = DateTime.UtcNow,
            };

            try
            {
                db.TicketAttachments.Add(entity);
                db.TicketHistory.Add(new TicketHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticketId,
                    ChangeType = TicketChangeType.AttachmentAdded,
                    OldValue = null,
                    NewValue = entity.Id.ToString(),
                    ChangedByUserId = uploadedBy,
                    ChangedAtUtc = entity.CreatedAtUtc,
                });
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Storage already has the file but the DB write failed — delete it to avoid an orphan.
                await storage.DeleteAsync(storageKey, ct);
                log.LogError(ex, "Failed to persist attachment metadata for ticket {TicketId}", ticketId);
                return Results.Json(new ErrorResponse("Could not save the attachment. Please try again."),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            log.LogInformation(
                "ticket_attachment create ticketId={TicketId} attachmentId={AttachmentId} actor={ActorId}",
                ticketId, entity.Id, uploadedBy);
            await auditLogger.WriteAsync(
                AuditActions.TicketAttachmentAdded, targetType: "ticket", targetId: ticketId.ToString(), ct: ct);

            var response = (await ToResponsesAsync([entity], authDb))[0];
            return Results.Created($"/api/tickets/{ticketId}/attachments/{entity.Id}", response);
        })
        .WithName("UploadTicketAttachment");

        attachments.MapGet("/{attachmentId:guid}/download", async (
            Guid ticketId, Guid attachmentId, TicketDbContext db, IFileStorage storage,
            ILogger<Program> log, CancellationToken ct) =>
        {
            var entity = await db.TicketAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId, ct);
            if (entity is null)
            {
                return Results.NotFound();
            }

            Stream stream;
            try
            {
                stream = await storage.OpenReadAsync(entity.StorageKey, ct);
            }
            catch (StorageException ex)
            {
                log.LogError(ex, "Failed to open attachment {AttachmentId} from storage", attachmentId);
                return Results.Json(new ErrorResponse("Could not download the attachment. Please try again."),
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }

            return Results.Stream(stream, entity.ContentType, entity.OriginalFileName);
        })
        .WithName("DownloadTicketAttachment");

        attachments.MapDelete("/{attachmentId:guid}", async (
            Guid ticketId, Guid attachmentId, TicketDbContext db, IFileStorage storage,
            ClaimsPrincipal principal, ILogger<Program> log, IAuditLogger auditLogger, CancellationToken ct) =>
        {
            var entity = await db.TicketAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId, ct);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

            db.TicketAttachments.Remove(entity);
            db.TicketHistory.Add(new TicketHistoryEntry
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                ChangeType = TicketChangeType.AttachmentRemoved,
                OldValue = entity.Id.ToString(),
                NewValue = null,
                ChangedByUserId = actorId,
                ChangedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);

            await storage.DeleteAsync(entity.StorageKey, ct);

            log.LogInformation(
                "ticket_attachment delete ticketId={TicketId} attachmentId={AttachmentId} actor={ActorId}",
                ticketId, attachmentId, actorId);
            await auditLogger.WriteAsync(
                AuditActions.TicketAttachmentRemoved, targetType: "ticket", targetId: ticketId.ToString(), ct: ct);

            return Results.NoContent();
        })
        .WithName("DeleteTicketAttachment");
    }

    private static async Task<List<TicketAttachmentResponse>> ToResponsesAsync(
        IReadOnlyList<TicketAttachment> entities, AuthDbContext authDb)
    {
        var uploaderIds = entities.Select(a => a.UploadedByUserId).Distinct().ToList();
        var uploaderNames = await authDb.Users
            .AsNoTracking()
            .Where(u => uploaderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        return entities
            .Select(a => new TicketAttachmentResponse(
                a.Id,
                a.TicketId,
                a.OriginalFileName,
                a.ContentType,
                a.FileSize,
                a.UploadedByUserId,
                uploaderNames.GetValueOrDefault(a.UploadedByUserId, string.Empty),
                a.CreatedAtUtc))
            .ToList();
    }
}
