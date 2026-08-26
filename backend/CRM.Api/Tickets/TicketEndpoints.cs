using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Security;
using CRM.Api.Sla;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Tickets;

public static class TicketEndpoints
{
    public static void MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var tickets = app.MapGroup("/api/tickets");

        tickets.MapGet("/", async (
            [AsParameters] TicketListQuery query, TicketDbContext db, CustomerDbContext customerDb) =>
        {
            // Page/PageSize are clamped, never rejected — same convention as the
            // customers list endpoint.
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            IQueryable<Ticket> filtered = db.Tickets;

            var term = query.Search?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(term))
            {
                filtered = filtered.Where(t =>
                    t.Title.ToLower().Contains(term) || t.Description.ToLower().Contains(term));
            }

            if (query.Status is not null)
            {
                filtered = filtered.Where(t => t.Status == query.Status);
            }

            if (query.Priority is not null)
            {
                filtered = filtered.Where(t => t.Priority == query.Priority);
            }

            if (query.AssigneeId is not null)
            {
                filtered = filtered.Where(t => t.AssigneeUserId == query.AssigneeId);
            }

            if (query.UpdatedSince is not null)
            {
                filtered = filtered.Where(t => t.UpdatedAtUtc >= query.UpdatedSince);
            }

            var totalCount = await filtered.CountAsync();

            var entities = await filtered
                .OrderByDescending(t => t.UpdatedAtUtc)
                .ThenByDescending(t => t.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = await ToListItemsAsync(entities, customerDb);
            return Results.Ok(new PagedResult<TicketListItem>(items, page, pageSize, totalCount));
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("ListTickets")
        .WithTags("Tickets")
        .Produces<PagedResult<TicketListItem>>();

        tickets.MapPost("/", async (
            CreateTicketRequest request, TicketDbContext db, CustomerDbContext customerDb, AuthDbContext authDb,
            TicketCreationService creationService, IAuditLogger auditLogger) =>
        {
            var title = request.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(title))
            {
                return Results.BadRequest(new ErrorResponse("Title is required."));
            }
            if (title.Length > 200)
            {
                return Results.BadRequest(new ErrorResponse("Title must be 200 characters or fewer."));
            }

            var description = request.Description?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(description))
            {
                return Results.BadRequest(new ErrorResponse("Description is required."));
            }
            if (description.Length > 4000)
            {
                return Results.BadRequest(new ErrorResponse("Description must be 4000 characters or fewer."));
            }

            var customer = await customerDb.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId);
            if (customer is null)
            {
                return Results.BadRequest(new ErrorResponse("customer_not_found"));
            }

            var entity = await creationService.CreateAsync(
                request.CustomerId, title, description, request.Priority ?? TicketPriority.Normal,
                CancellationToken.None);

            await auditLogger.WriteAsync(
                AuditActions.TicketCreated, targetType: "ticket", targetId: entity.Id.ToString());

            var response = await ToResponseAsync(entity, customer.FullName, authDb);
            return Results.Created($"/api/tickets/{entity.Id}", response);
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("CreateTicket")
        .WithTags("Tickets")
        .Produces<TicketResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        tickets.MapGet("/{id:guid}", async (Guid id, TicketDbContext db, CustomerDbContext customerDb, AuthDbContext authDb) =>
        {
            var entity = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            // Two DbContexts, one request: tickets and customers are separate bounded
            // contexts backed by the same physical database, so this is two queries
            // stitched in-memory rather than a relational join. No transactional
            // consistency is required for a read.
            var customer = await customerDb.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.CustomerId);

            var response = await ToResponseAsync(entity, customer?.FullName ?? string.Empty, authDb);
            return Results.Ok(response);
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("GetTicket")
        .WithTags("Tickets")
        .Produces<TicketResponse>()
        .Produces(StatusCodes.Status404NotFound);

        tickets.MapPut("/{id:guid}/assignment", async (
            Guid id, AssignTicketRequest request, TicketDbContext db, CustomerDbContext customerDb,
            AuthDbContext authDb, ClaimsPrincipal principal, ILogger<Program> log, IAuditLogger auditLogger) =>
        {
            var entity = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            if (request.AgentUserId is not null)
            {
                var agent = await authDb.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == request.AgentUserId);
                if (agent is null || !agent.Roles.Contains(Roles.Agent))
                {
                    return Results.BadRequest(new ErrorResponse("invalid_agent"));
                }
            }

            if (entity.AssigneeUserId != request.AgentUserId)
            {
                var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
                db.TicketHistory.Add(new TicketHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    TicketId = entity.Id,
                    ChangeType = TicketChangeType.Assignment,
                    OldValue = entity.AssigneeUserId?.ToString(),
                    NewValue = request.AgentUserId?.ToString(),
                    ChangedByUserId = actorId,
                    ChangedAtUtc = DateTime.UtcNow,
                });

                entity.AssigneeUserId = request.AgentUserId;
                entity.UpdatedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync();

                log.LogInformation(
                    "ticket_assignment ticketId={TicketId} agentId={AgentId} actor={ActorId}",
                    entity.Id, request.AgentUserId, actorId);
                await auditLogger.WriteAsync(
                    AuditActions.TicketAssigned, targetType: "ticket", targetId: entity.Id.ToString());
            }

            var customer = await customerDb.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.CustomerId);
            return Results.Ok(await ToResponseAsync(entity, customer?.FullName ?? string.Empty, authDb));
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("AssignTicket")
        .WithTags("Tickets")
        .Produces<TicketResponse>()
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        tickets.MapPut("/{id:guid}/status", async (
            Guid id, ChangeTicketStatusRequest request, TicketDbContext db, CustomerDbContext customerDb,
            AuthDbContext authDb, ClaimsPrincipal principal, ILogger<Program> log, IAuditLogger auditLogger) =>
        {
            var entity = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            if (!Enum.TryParse<TicketStatus>(request.Status, ignoreCase: true, out var newStatus))
            {
                return Results.BadRequest(new ErrorResponse(
                    $"Unknown status '{request.Status}'. Allowed values: {string.Join(", ", Enum.GetNames<TicketStatus>())}."));
            }

            if (entity.Status != newStatus)
            {
                if (!TicketStatusRules.IsAllowedTransition(entity.Status, newStatus))
                {
                    return Results.BadRequest(new ErrorResponse(
                        $"Cannot transition ticket status from {entity.Status} to {newStatus}."));
                }

                var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
                db.TicketHistory.Add(new TicketHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    TicketId = entity.Id,
                    ChangeType = TicketChangeType.Status,
                    OldValue = entity.Status.ToString(),
                    NewValue = newStatus.ToString(),
                    ChangedByUserId = actorId,
                    ChangedAtUtc = DateTime.UtcNow,
                });

                entity.Status = newStatus;
                entity.UpdatedAtUtc = DateTime.UtcNow;

                // Resolved and Closed both count as "resolved" for SLA
                // purposes — only stamp once, the first time either is reached.
                if (entity.ResolvedAtUtc is null &&
                    newStatus is TicketStatus.Resolved or TicketStatus.Closed)
                {
                    entity.ResolvedAtUtc = entity.UpdatedAtUtc;
                }

                await db.SaveChangesAsync();

                log.LogInformation(
                    "ticket_status_change ticketId={TicketId} newStatus={NewStatus} actor={ActorId}",
                    entity.Id, newStatus, actorId);
                await auditLogger.WriteAsync(
                    AuditActions.TicketStatusChanged, targetType: "ticket", targetId: entity.Id.ToString());
            }

            var customer = await customerDb.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.CustomerId);
            return Results.Ok(await ToResponseAsync(entity, customer?.FullName ?? string.Empty, authDb));
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("ChangeTicketStatus")
        .WithTags("Tickets")
        .Produces<TicketResponse>()
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        tickets.MapPut("/{id:guid}/priority", async (
            Guid id, ChangeTicketPriorityRequest request, TicketDbContext db, CustomerDbContext customerDb,
            AuthDbContext authDb, ClaimsPrincipal principal, ILogger<Program> log, ISlaService slaService,
            IAuditLogger auditLogger) =>
        {
            var entity = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            if (!Enum.TryParse<TicketPriority>(request.Priority, ignoreCase: true, out var newPriority))
            {
                return Results.BadRequest(new ErrorResponse(
                    $"Unknown priority '{request.Priority}'. Allowed values: {string.Join(", ", Enum.GetNames<TicketPriority>())}."));
            }

            if (entity.Priority != newPriority)
            {
                var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var now = DateTime.UtcNow;
                db.TicketHistory.Add(new TicketHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    TicketId = entity.Id,
                    ChangeType = TicketChangeType.Priority,
                    OldValue = entity.Priority.ToString(),
                    NewValue = newPriority.ToString(),
                    ChangedByUserId = actorId,
                    ChangedAtUtc = now,
                });

                entity.Priority = newPriority;
                entity.UpdatedAtUtc = now;

                // Re-resolve the policy for the new priority and recompute
                // only the due dates whose clock hasn't already stopped —
                // a priority bump after the first reply must not move the
                // first-response deadline that's already been met/breached.
                var oldPolicyId = entity.SlaPolicyId;
                var policy = await slaService.ResolvePolicyAsync(newPriority, channel: null, CancellationToken.None);
                entity.SlaPolicyId = policy?.Id;

                if (policy is not null)
                {
                    var (firstResponseDueAtUtc, resolutionDueAtUtc) =
                        SlaCalculator.ComputeDueDates(policy, entity.CreatedAtUtc);

                    if (entity.FirstRespondedAtUtc is null)
                    {
                        entity.FirstResponseDueAtUtc = firstResponseDueAtUtc;
                    }
                    if (entity.ResolvedAtUtc is null)
                    {
                        entity.ResolutionDueAtUtc = resolutionDueAtUtc;
                    }
                }
                else
                {
                    if (entity.FirstRespondedAtUtc is null)
                    {
                        entity.FirstResponseDueAtUtc = null;
                    }
                    if (entity.ResolvedAtUtc is null)
                    {
                        entity.ResolutionDueAtUtc = null;
                    }
                }

                if (oldPolicyId != entity.SlaPolicyId)
                {
                    db.TicketHistory.Add(new TicketHistoryEntry
                    {
                        Id = Guid.NewGuid(),
                        TicketId = entity.Id,
                        ChangeType = TicketChangeType.SlaRecalculated,
                        OldValue = oldPolicyId?.ToString(),
                        NewValue = entity.SlaPolicyId?.ToString(),
                        ChangedByUserId = actorId,
                        ChangedAtUtc = now,
                    });
                }

                await db.SaveChangesAsync();

                log.LogInformation(
                    "ticket_priority_change ticketId={TicketId} newPriority={NewPriority} actor={ActorId}",
                    entity.Id, newPriority, actorId);
                await auditLogger.WriteAsync(
                    AuditActions.TicketPriorityChanged, targetType: "ticket", targetId: entity.Id.ToString());
            }

            var customer = await customerDb.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.CustomerId);
            return Results.Ok(await ToResponseAsync(entity, customer?.FullName ?? string.Empty, authDb));
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("ChangeTicketPriority")
        .WithTags("Tickets")
        .Produces<TicketResponse>()
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        tickets.MapGet("/{id:guid}/history", async (Guid id, TicketDbContext db, AuthDbContext authDb) =>
        {
            var ticketExists = await db.Tickets.AsNoTracking().AnyAsync(t => t.Id == id);
            if (!ticketExists)
            {
                return Results.NotFound();
            }

            var entries = await db.TicketHistory
                .AsNoTracking()
                .Where(h => h.TicketId == id)
                .OrderByDescending(h => h.ChangedAtUtc)
                .ThenByDescending(h => h.Id)
                .ToListAsync();

            var changerIds = entries.Select(h => h.ChangedByUserId).Distinct().ToList();
            var changerNames = await authDb.Users
                .AsNoTracking()
                .Where(u => changerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Name);

            var responses = entries
                .Select(h => new TicketHistoryEntryResponse(
                    h.Id,
                    h.ChangeType,
                    h.OldValue,
                    h.NewValue,
                    h.Reason,
                    h.ChangedByUserId,
                    changerNames.GetValueOrDefault(h.ChangedByUserId, string.Empty),
                    h.ChangedAtUtc,
                    h.ChangedByUserId == TicketEscalationService.SystemActorId))
                .ToList();

            return Results.Ok(responses);
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("GetTicketHistory")
        .WithTags("Tickets")
        .Produces<List<TicketHistoryEntryResponse>>()
        .Produces(StatusCodes.Status404NotFound);

        tickets.MapGet("/eligible-agents", async (AuthDbContext authDb) =>
        {
            var agents = await authDb.Users
                .AsNoTracking()
                .Where(u => u.IsActive && u.Roles.Contains(Roles.Agent))
                .OrderBy(u => u.Name)
                .Select(u => new EligibleAgentResponse(u.Id, u.Name, u.Email))
                .ToListAsync();

            return Results.Ok(agents);
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("GetEligibleAgents")
        .WithTags("Tickets")
        .Produces<List<EligibleAgentResponse>>();

        tickets.MapPost("/{id:guid}/escalate", async (
            Guid id, EscalateTicketRequest request, TicketDbContext db, CustomerDbContext customerDb,
            AuthDbContext authDb, ClaimsPrincipal principal, ILogger<Program> log,
            TicketEscalationService escalationService, IAuditLogger auditLogger) =>
        {
            var entity = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var reason = request.Reason?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(reason))
            {
                return Results.BadRequest(new ErrorResponse("A reason is required to escalate a ticket."));
            }
            if (reason.Length > 500)
            {
                return Results.BadRequest(new ErrorResponse("Reason must be 500 characters or fewer."));
            }

            var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var priorityBeforeEscalation = entity.Priority;

            var result = await escalationService.EscalateAsync(
                entity, reason, actorId, DateTime.UtcNow, CancellationToken.None);

            if (!result.Success)
            {
                var message = result.FailureReason == TicketEscalationFailureReason.TicketTerminal
                    ? $"Cannot escalate a {entity.Status} ticket."
                    : "Ticket is already at the highest priority.";
                return Results.BadRequest(new ErrorResponse(message));
            }

            log.LogInformation(
                "ticket_escalate ticketId={TicketId} fromPriority={FromPriority} toPriority={ToPriority} actor={ActorId}",
                entity.Id, priorityBeforeEscalation, entity.Priority, actorId);
            await auditLogger.WriteAsync(
                AuditActions.TicketEscalated, targetType: "ticket", targetId: entity.Id.ToString());

            var customer = await customerDb.Customers.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == entity.CustomerId);
            return Results.Ok(await ToResponseAsync(entity, customer?.FullName ?? string.Empty, authDb));
        })
        .RequireAuthorization("AdminOnly")
        .WithName("EscalateTicket")
        .WithTags("Tickets")
        .Produces<TicketResponse>()
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<TicketResponse> ToResponseAsync(
        Ticket entity, string customerName, AuthDbContext authDb)
    {
        string? assigneeName = null;
        if (entity.AssigneeUserId is not null)
        {
            assigneeName = await authDb.Users.AsNoTracking()
                .Where(u => u.Id == entity.AssigneeUserId)
                .Select(u => u.Name)
                .FirstOrDefaultAsync();
        }

        return new TicketResponse(
            entity.Id,
            entity.CustomerId,
            customerName,
            entity.Title,
            entity.Description,
            entity.Status,
            entity.Priority,
            entity.AssigneeUserId,
            assigneeName,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            BuildSlaSnapshot(entity));
    }

    private static async Task<List<TicketListItem>> ToListItemsAsync(
        IReadOnlyList<Ticket> entities, CustomerDbContext customerDb)
    {
        var customerIds = entities.Select(t => t.CustomerId).Distinct().ToList();
        var customerNames = await customerDb.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.FullName);

        return entities
            .Select(t => new TicketListItem(
                t.Id,
                t.CustomerId,
                customerNames.GetValueOrDefault(t.CustomerId, string.Empty),
                t.Title,
                t.Status,
                t.Priority,
                t.AssigneeUserId,
                t.CreatedAtUtc,
                t.UpdatedAtUtc,
                BuildSlaSnapshot(t)))
            .ToList();
    }

    // Pure computation over already-loaded columns — no extra query per
    // ticket, so list responses stay N+1-free.
    private static TicketSlaSnapshotResponse BuildSlaSnapshot(Ticket entity)
    {
        var now = DateTime.UtcNow;
        var firstResponseStatus = SlaCalculator.ComputeStatus(
            now, entity.CreatedAtUtc, entity.FirstResponseDueAtUtc, entity.FirstRespondedAtUtc);
        var resolutionStatus = SlaCalculator.ComputeStatus(
            now, entity.CreatedAtUtc, entity.ResolutionDueAtUtc, entity.ResolvedAtUtc);

        return new TicketSlaSnapshotResponse(
            entity.SlaPolicyId,
            entity.FirstResponseDueAtUtc,
            entity.ResolutionDueAtUtc,
            entity.FirstRespondedAtUtc,
            entity.ResolvedAtUtc,
            firstResponseStatus,
            resolutionStatus,
            entity.FirstResponseBreachedAtUtc,
            entity.ResolutionBreachedAtUtc,
            entity.SlaLastEvaluatedAtUtc,
            entity.SlaAutoEscalatedAtUtc);
    }
}
