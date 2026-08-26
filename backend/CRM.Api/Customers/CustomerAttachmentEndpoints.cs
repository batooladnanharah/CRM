using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.Customers.Attachments;
using CRM.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CRM.Api.Customers;

public static class CustomerAttachmentEndpoints
{
    public static void MapCustomerAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        // Same policy as notes/interactions — admin/agent only, never the customer role.
        var attachments = app.MapGroup("/api/customers/{customerId:guid}/attachments")
            .RequireAuthorization(Permissions.CustomersManage)
            .WithTags("CustomerAttachments");

        attachments.MapGet("/", async (Guid customerId, CustomerDbContext db, AuthDbContext authDb) =>
        {
            var customerExists = await db.Customers.AsNoTracking().AnyAsync(c => c.Id == customerId);
            if (!customerExists)
            {
                return Results.NotFound();
            }

            var entities = await db.CustomerAttachments
                .AsNoTracking()
                .Where(a => a.CustomerId == customerId)
                .OrderByDescending(a => a.CreatedAtUtc)
                .ThenByDescending(a => a.Id)
                .ToListAsync();

            var responses = await ToResponsesAsync(entities, authDb);
            return Results.Ok(responses);
        })
        .WithName("ListCustomerAttachments");

        attachments.MapPost("/", async (
            Guid customerId, HttpRequest request, CustomerDbContext db, AuthDbContext authDb,
            IFileStorage storage, IOptions<AttachmentsOptions> attachmentsOptions,
            ClaimsPrincipal principal, ILogger<Program> log, IAuditLogger auditLogger, CancellationToken ct) =>
        {
            var options = attachmentsOptions.Value;

            var customerExists = await db.Customers.AsNoTracking().AnyAsync(c => c.Id == customerId, ct);
            if (!customerExists)
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

            // Never trust the client path — only the leaf name, sanitized.
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

            var storageKey = $"{customerId}/{Guid.NewGuid():N}{extension}";
            var uploadedBy = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await using var stream = file.OpenReadStream();
            try
            {
                await storage.SaveAsync(stream, storageKey, ct);
            }
            catch (StorageException ex)
            {
                log.LogError(ex, "Failed to save attachment to storage for customer {CustomerId}", customerId);
                return Results.Json(new ErrorResponse("Could not save the attachment. Please try again."),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var entity = new CustomerAttachment
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                UploadedByUserId = uploadedBy,
                OriginalFileName = originalFileName,
                StorageKey = storageKey,
                ContentType = file.ContentType,
                FileSize = file.Length,
                CreatedAtUtc = DateTime.UtcNow,
            };

            try
            {
                db.CustomerAttachments.Add(entity);
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Storage already has the file but the DB row failed — delete it to avoid an orphan.
                await storage.DeleteAsync(storageKey, ct);
                log.LogError(ex, "Failed to persist attachment metadata for customer {CustomerId}", customerId);
                return Results.Json(new ErrorResponse("Could not save the attachment. Please try again."),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            log.LogInformation(
                "customer_attachment create customerId={CustomerId} attachmentId={AttachmentId} actor={ActorId}",
                customerId, entity.Id, uploadedBy);
            await auditLogger.WriteAsync(
                AuditActions.CustomerAttachmentAdded, targetType: "customer", targetId: customerId.ToString(), ct: ct);

            var response = (await ToResponsesAsync([entity], authDb))[0];
            return Results.Created($"/api/customers/{customerId}/attachments/{entity.Id}", response);
        })
        .WithName("UploadCustomerAttachment");

        attachments.MapGet("/{attachmentId:guid}/download", async (
            Guid customerId, Guid attachmentId, CustomerDbContext db, IFileStorage storage,
            ILogger<Program> log, CancellationToken ct) =>
        {
            var entity = await db.CustomerAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.CustomerId == customerId, ct);
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
        .WithName("DownloadCustomerAttachment");

        attachments.MapDelete("/{attachmentId:guid}", async (
            Guid customerId, Guid attachmentId, CustomerDbContext db, IFileStorage storage,
            ClaimsPrincipal principal, ILogger<Program> log, IAuditLogger auditLogger, CancellationToken ct) =>
        {
            var entity = await db.CustomerAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.CustomerId == customerId, ct);
            if (entity is null)
            {
                return Results.NotFound();
            }

            db.CustomerAttachments.Remove(entity);
            await db.SaveChangesAsync(ct);

            await storage.DeleteAsync(entity.StorageKey, ct);

            var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            log.LogInformation(
                "customer_attachment delete customerId={CustomerId} attachmentId={AttachmentId} actor={ActorId}",
                customerId, attachmentId, actorId);
            await auditLogger.WriteAsync(
                AuditActions.CustomerAttachmentRemoved, targetType: "customer", targetId: customerId.ToString(), ct: ct);

            return Results.NoContent();
        })
        .WithName("DeleteCustomerAttachment");
    }

    private static async Task<List<CustomerAttachmentResponse>> ToResponsesAsync(
        IReadOnlyList<CustomerAttachment> entities, AuthDbContext authDb)
    {
        var uploaderIds = entities.Select(a => a.UploadedByUserId).Distinct().ToList();
        var uploaderNames = await authDb.Users
            .AsNoTracking()
            .Where(u => uploaderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        return entities
            .Select(a => new CustomerAttachmentResponse(
                a.Id,
                a.CustomerId,
                a.OriginalFileName,
                a.ContentType,
                a.FileSize,
                a.UploadedByUserId,
                uploaderNames.GetValueOrDefault(a.UploadedByUserId, string.Empty),
                a.CreatedAtUtc))
            .ToList();
    }
}
