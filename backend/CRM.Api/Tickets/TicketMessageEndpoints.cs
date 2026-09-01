using System.Net.Mail;
using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.CommunicationChannels;
using CRM.Api.Customers;
using CRM.Api.Email;
using CRM.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CRM.Api.Tickets;

public static class TicketMessageEndpoints
{
    public static void MapTicketMessageEndpoints(this IEndpointRouteBuilder app)
    {
        // Messages are staff-only (internal notes and agent-authored public
        // replies) — same AgentOrAdmin policy as the customer notes/attachments
        // groups, never the customer role.
        var messages = app.MapGroup("/api/tickets/{ticketId:guid}/messages")
            .RequireAuthorization(Permissions.TicketsManage)
            .WithTags("TicketMessages");

        messages.MapGet("/", async (
            Guid ticketId, [AsParameters] TicketMessagesQuery query, TicketDbContext db, AuthDbContext authDb,
            CommunicationChannelsDbContext channelsDb, CustomerDbContext customerDb) =>
        {
            var ticketExists = await db.Tickets.AsNoTracking().AnyAsync(t => t.Id == ticketId);
            if (!ticketExists)
            {
                return Results.NotFound();
            }

            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            var messagesQuery = db.TicketMessages.AsNoTracking().Where(m => m.TicketId == ticketId);
            var totalCount = await messagesQuery.CountAsync();

            var entities = await messagesQuery
                .OrderByDescending(m => m.CreatedAtUtc)
                .ThenByDescending(m => m.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = await ToResponsesAsync(entities, db, authDb, channelsDb, customerDb);
            return Results.Ok(new PagedResult<TicketMessageResponse>(items, page, pageSize, totalCount));
        })
        .WithName("ListTicketMessages");

        messages.MapPost("/", async (
            Guid ticketId, CreateTicketMessageRequest request, TicketDbContext db, AuthDbContext authDb,
            CustomerDbContext customerDb, CommunicationChannelsDbContext channelsDb, IEmailService emailService,
            IOptions<EmailOptions> emailOptions,
            ClaimsPrincipal principal, ILogger<Program> log, IAuditLogger auditLogger) =>
        {
            var body = request.Body?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(body))
            {
                return Results.BadRequest(new ErrorResponse("Body is required."));
            }
            if (body.Length > 5000)
            {
                return Results.BadRequest(new ErrorResponse("Body must be 5000 characters or fewer."));
            }

            var mentionedUserIds = (request.MentionedUserIds ?? [])
                .Distinct()
                .ToList();

            if (mentionedUserIds.Count > 0 && !request.IsInternal)
            {
                return Results.BadRequest(new ErrorResponse("Mentions are only allowed on internal notes."));
            }

            var channel = MessageChannel.Web;
            if (!string.IsNullOrWhiteSpace(request.Channel) &&
                !Enum.TryParse(request.Channel, ignoreCase: true, out channel))
            {
                return Results.BadRequest(new ErrorResponse("Unrecognized channel."));
            }

            if (channel == MessageChannel.Email && request.IsInternal)
            {
                return Results.BadRequest(new ErrorResponse("Internal notes cannot be sent via email."));
            }

            var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket is null)
            {
                return Results.NotFound();
            }

            Customer? customer = null;
            string? emailSubject = null;
            if (channel == MessageChannel.Email)
            {
                customer = await customerDb.Customers.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == ticket.CustomerId);
                if (customer is null)
                {
                    return Results.NotFound();
                }

                if (string.IsNullOrWhiteSpace(customer.Email) || !MailAddress.TryCreate(customer.Email, out _))
                {
                    return Results.BadRequest(new ErrorResponse("customer_email_missing_or_invalid"));
                }

                emailSubject = ticket.Title.StartsWith("Re: ", StringComparison.Ordinal)
                    ? ticket.Title
                    : $"Re: {ticket.Title}";
            }

            if (mentionedUserIds.Count > 0)
            {
                // Every mentioned id must resolve to an active user holding a
                // CRM role (admin/agent) — never the customer role.
                var validUserIds = await authDb.Users
                    .AsNoTracking()
                    .Where(u => mentionedUserIds.Contains(u.Id) && u.IsActive &&
                        (u.Roles.Contains(Roles.Admin) || u.Roles.Contains(Roles.Agent)))
                    .Select(u => u.Id)
                    .ToListAsync();

                var invalidIds = mentionedUserIds.Except(validUserIds).ToList();
                if (invalidIds.Count > 0)
                {
                    return Results.BadRequest(new ErrorResponse(
                        $"Cannot mention unknown, inactive, or non-CRM user(s): {string.Join(", ", invalidIds)}."));
                }
            }

            var authorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var now = DateTime.UtcNow;

            var entity = new TicketMessage
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                AuthorUserId = authorId,
                Body = body,
                IsInternal = request.IsInternal,
                Channel = channel,
                CreatedAtUtc = now,
            };

            db.TicketMessages.Add(entity);

            foreach (var userId in mentionedUserIds)
            {
                db.MessageMentions.Add(new MessageMention
                {
                    Id = Guid.NewGuid(),
                    MessageId = entity.Id,
                    UserId = userId,
                    CreatedAtUtc = now,
                });
            }

            db.TicketHistory.Add(new TicketHistoryEntry
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                ChangeType = TicketChangeType.MessageAdded,
                OldValue = null,
                NewValue = entity.Id.ToString(),
                ChangedByUserId = authorId,
                ChangedAtUtc = now,
            });

            // This endpoint only creates staff-authored messages (customer-authored
            // replies go through CustomerPortalEndpoints, see AuthorCustomerId). Only a
            // customer-visible reply (IsInternal == false) counts as the first
            // response for SLA purposes — internal notes must not stamp it.
            if (!request.IsInternal && ticket.FirstRespondedAtUtc is null)
            {
                ticket.FirstRespondedAtUtc = now;
                ticket.UpdatedAtUtc = now;
            }

            await db.SaveChangesAsync();

            if (channel == MessageChannel.Email)
            {
                var metadata = new EmailMessageMetadata
                {
                    Id = Guid.NewGuid(),
                    TicketMessageId = entity.Id,
                    FromAddress = emailOptions.Value.FromAddress,
                    ToAddress = customer!.Email,
                    Subject = emailSubject!,
                    DeliveryStatus = EmailDeliveryStatus.Pending,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                channelsDb.EmailMessageMetadata.Add(metadata);
                await channelsDb.SaveChangesAsync();

                EmailSendResult sendResult;
                try
                {
                    sendResult = await emailService.SendAsync(
                        new EmailSendRequest(customer.Email, customer.FullName, emailSubject!, body, entity.Id),
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Email provider failed for ticket {TicketId}", ticketId);
                    sendResult = new EmailSendResult(false, null, "provider_exception", "The email provider failed.");
                }

                if (sendResult.Success)
                {
                    metadata.DeliveryStatus = EmailDeliveryStatus.Sent;
                    metadata.ProviderMessageId = sendResult.ProviderMessageId;
                    metadata.SentAt = DateTimeOffset.UtcNow;
                    await channelsDb.SaveChangesAsync();

                    db.TicketHistory.Add(new TicketHistoryEntry
                    {
                        Id = Guid.NewGuid(),
                        TicketId = ticketId,
                        ChangeType = TicketChangeType.EmailSent,
                        OldValue = null,
                        NewValue = $"Email sent to {customer.FullName}",
                        ChangedByUserId = authorId,
                        ChangedAtUtc = DateTimeOffset.UtcNow.UtcDateTime,
                    });
                    await db.SaveChangesAsync();

                    log.LogInformation(
                        "ticket_message create ticketId={TicketId} messageId={MessageId} actor={ActorId} mentions={MentionCount} channel={Channel}",
                        ticketId, entity.Id, authorId, mentionedUserIds.Count, channel);
                    await auditLogger.WriteAsync(
                        AuditActions.TicketMessageAdded, targetType: "ticket", targetId: ticketId.ToString());

                    var successResponse = (await ToResponsesAsync([entity], db, authDb, channelsDb, customerDb))[0];
                    return Results.Created($"/api/tickets/{ticketId}/messages/{entity.Id}", successResponse);
                }

                metadata.DeliveryStatus = EmailDeliveryStatus.Failed;
                metadata.LastError = sendResult.ErrorMessage ?? sendResult.ErrorCode;
                await channelsDb.SaveChangesAsync();

                log.LogError(
                    "email_delivery_failed ticketId={TicketId} messageId={MessageId} errorCode={ErrorCode}",
                    ticketId, entity.Id, sendResult.ErrorCode);

                return Results.Json(
                    new EmailDeliveryFailureResponse("Unable to send email. Please try again.", entity.Id),
                    statusCode: StatusCodes.Status502BadGateway);
            }

            log.LogInformation(
                "ticket_message create ticketId={TicketId} messageId={MessageId} actor={ActorId} mentions={MentionCount} channel={Channel}",
                ticketId, entity.Id, authorId, mentionedUserIds.Count, channel);
            await auditLogger.WriteAsync(
                AuditActions.TicketMessageAdded, targetType: "ticket", targetId: ticketId.ToString());

            var response = (await ToResponsesAsync([entity], db, authDb, channelsDb, customerDb))[0];
            return Results.Created($"/api/tickets/{ticketId}/messages/{entity.Id}", response);
        })
        .WithName("CreateTicketMessage");
    }

    private static async Task<List<TicketMessageResponse>> ToResponsesAsync(
        IReadOnlyList<TicketMessage> messages, TicketDbContext db, AuthDbContext authDb,
        CommunicationChannelsDbContext channelsDb, CustomerDbContext customerDb)
    {
        var authorIds = messages.Where(m => m.AuthorUserId is not null)
            .Select(m => m.AuthorUserId!.Value).Distinct().ToList();
        var authorNames = await authDb.Users
            .AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        var authorCustomerIds = messages.Where(m => m.AuthorCustomerId is not null)
            .Select(m => m.AuthorCustomerId!.Value).Distinct().ToList();
        var authorCustomerNames = await customerDb.Customers
            .AsNoTracking()
            .Where(c => authorCustomerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.FullName);

        var messageIds = messages.Select(m => m.Id).ToList();
        var mentionsByMessageId = (await db.MessageMentions
            .AsNoTracking()
            .Where(m => messageIds.Contains(m.MessageId))
            .ToListAsync())
            .GroupBy(m => m.MessageId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(m => m.UserId).ToList());

        var deliveryStatusByMessageId = await channelsDb.EmailMessageMetadata
            .AsNoTracking()
            .Where(m => messageIds.Contains(m.TicketMessageId))
            .ToDictionaryAsync(m => m.TicketMessageId, m => m.DeliveryStatus);

        return messages
            .Select(m => new TicketMessageResponse(
                m.Id,
                m.TicketId,
                m.AuthorUserId,
                m.AuthorCustomerId,
                m.AuthorUserId is { } userId
                    ? authorNames.GetValueOrDefault(userId, string.Empty)
                    : m.AuthorCustomerId is { } customerId
                        ? authorCustomerNames.GetValueOrDefault(customerId, string.Empty)
                        : string.Empty,
                m.Body,
                m.IsInternal,
                mentionsByMessageId.GetValueOrDefault(m.Id, Array.Empty<Guid>()),
                m.Channel.ToString(),
                deliveryStatusByMessageId.TryGetValue(m.Id, out var status) ? status.ToString() : null,
                m.CreatedAtUtc))
            .ToList();
    }
}

public record TicketMessagesQuery(int Page = 1, int PageSize = 20);

public record EmailDeliveryFailureResponse(string Message, Guid MessageId);
