using CRM.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.CommunicationChannels;

public static class CommunicationChannelEndpoints
{
    public static void MapCommunicationChannelEndpoints(this IEndpointRouteBuilder app)
    {
        var channels = app.MapGroup("/api/channels");

        channels.MapGet("/", async (CommunicationChannelsDbContext db) =>
        {
            var items = await db.Channels
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new ChannelResponse(c.Id, c.Name, c.Type, c.IsEnabled, c.CreatedAtUtc, c.UpdatedAtUtc))
                .ToListAsync();

            return Results.Ok(items);
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("ListCommunicationChannels")
        .WithTags("CommunicationChannels");

        channels.MapPost("/", async (CreateChannelRequest request, CommunicationChannelsDbContext db) =>
        {
            var name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                return Results.BadRequest(new ErrorResponse("Name is required."));
            }
            if (name.Length > 200)
            {
                return Results.BadRequest(new ErrorResponse("Name must be 200 characters or fewer."));
            }

            if (!Enum.TryParse<ChannelType>(request.Type, ignoreCase: true, out var type))
            {
                return Results.BadRequest(new ErrorResponse(
                    $"Unknown channel type '{request.Type}'. Allowed values: {string.Join(", ", Enum.GetNames<ChannelType>())}."));
            }

            if (await db.Channels.AnyAsync(c => c.Type == type && c.Name == name))
            {
                return Results.Json(new ErrorResponse("A channel with this name and type already exists."),
                    statusCode: StatusCodes.Status409Conflict);
            }

            var now = DateTime.UtcNow;
            var entity = new Channel
            {
                Id = Guid.NewGuid(),
                Name = name,
                Type = type,
                IsEnabled = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            try
            {
                db.Channels.Add(entity);
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Race-safety net: the pre-check above can miss a concurrent insert;
                // the unique index on (Type, Name) is the source of truth.
                return Results.Json(new ErrorResponse("A channel with this name and type already exists."),
                    statusCode: StatusCodes.Status409Conflict);
            }

            var response = new ChannelResponse(
                entity.Id, entity.Name, entity.Type, entity.IsEnabled, entity.CreatedAtUtc, entity.UpdatedAtUtc);
            return Results.Created($"/api/channels/{entity.Id}", response);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("CreateCommunicationChannel")
        .WithTags("CommunicationChannels");

        channels.MapGet("/{id:guid}", async (Guid id, CommunicationChannelsDbContext db) =>
        {
            var entity = await db.Channels.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var response = new ChannelResponse(
                entity.Id, entity.Name, entity.Type, entity.IsEnabled, entity.CreatedAtUtc, entity.UpdatedAtUtc);
            return Results.Ok(response);
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("GetCommunicationChannel")
        .WithTags("CommunicationChannels");

        channels.MapPut("/{id:guid}", async (
            Guid id, UpdateChannelRequest request, CommunicationChannelsDbContext db) =>
        {
            var entity = await db.Channels.FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var name = request.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(name))
            {
                return Results.BadRequest(new ErrorResponse("Name is required."));
            }
            if (name.Length > 200)
            {
                return Results.BadRequest(new ErrorResponse("Name must be 200 characters or fewer."));
            }

            if (await db.Channels.AnyAsync(c => c.Id != id && c.Type == entity.Type && c.Name == name))
            {
                return Results.Json(new ErrorResponse("A channel with this name and type already exists."),
                    statusCode: StatusCodes.Status409Conflict);
            }

            entity.Name = name;
            entity.IsEnabled = request.IsEnabled;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var response = new ChannelResponse(
                entity.Id, entity.Name, entity.Type, entity.IsEnabled, entity.CreatedAtUtc, entity.UpdatedAtUtc);
            return Results.Ok(response);
        })
        .RequireAuthorization("AdminOnly")
        .WithName("UpdateCommunicationChannel")
        .WithTags("CommunicationChannels");

        channels.MapDelete("/{id:guid}", async (Guid id, CommunicationChannelsDbContext db) =>
        {
            var entity = await db.Channels.FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var hasEmails = await db.EmailMessages.AsNoTracking().AnyAsync(m => m.ChannelId == id);
            if (hasEmails)
            {
                return Results.Json(new ErrorResponse("Cannot delete a channel that has received emails."),
                    statusCode: StatusCodes.Status409Conflict);
            }

            db.Channels.Remove(entity);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization("AdminOnly")
        .WithName("DeleteCommunicationChannel")
        .WithTags("CommunicationChannels");

        channels.MapGet("/{id:guid}/emails", async (Guid id, CommunicationChannelsDbContext db) =>
        {
            var channelExists = await db.Channels.AsNoTracking().AnyAsync(c => c.Id == id);
            if (!channelExists)
            {
                return Results.NotFound();
            }

            var items = await db.EmailMessages
                .AsNoTracking()
                .Where(m => m.ChannelId == id)
                .OrderByDescending(m => m.ReceivedAtUtc)
                .ThenByDescending(m => m.Id)
                .Take(100)
                .Select(m => new EmailMessageResponse(
                    m.Id, m.ChannelId, m.FromAddress, m.ToAddress, m.Subject, m.Body, m.ReceivedAtUtc, m.TicketId))
                .ToListAsync();

            return Results.Ok(items);
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("ListChannelEmails")
        .WithTags("CommunicationChannels");

        // Internal-only ingestion endpoint for tests / manual seeding — no
        // real SMTP/IMAP integration exists yet (out of scope for this story).
        channels.MapPost("/{id:guid}/emails/ingest", async (
            Guid id, IngestEmailRequest request, CommunicationChannelsDbContext db) =>
        {
            var channelExists = await db.Channels.AsNoTracking().AnyAsync(c => c.Id == id);
            if (!channelExists)
            {
                return Results.NotFound();
            }

            var fromAddress = request.FromAddress?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(fromAddress))
            {
                return Results.BadRequest(new ErrorResponse("FromAddress is required."));
            }

            var toAddress = request.ToAddress?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(toAddress))
            {
                return Results.BadRequest(new ErrorResponse("ToAddress is required."));
            }

            var entity = new EmailMessage
            {
                Id = Guid.NewGuid(),
                ChannelId = id,
                FromAddress = fromAddress,
                ToAddress = toAddress,
                Subject = request.Subject?.Trim() ?? string.Empty,
                Body = request.Body ?? string.Empty,
                ReceivedAtUtc = request.ReceivedAtUtc ?? DateTime.UtcNow,
                TicketId = request.TicketId,
            };

            db.EmailMessages.Add(entity);
            await db.SaveChangesAsync();

            var response = new EmailMessageResponse(
                entity.Id, entity.ChannelId, entity.FromAddress, entity.ToAddress, entity.Subject, entity.Body,
                entity.ReceivedAtUtc, entity.TicketId);
            return Results.Created($"/api/channels/{id}/emails/{entity.Id}", response);
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("IngestChannelEmail")
        .WithTags("CommunicationChannels");
    }
}
