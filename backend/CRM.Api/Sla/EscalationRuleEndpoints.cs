using CRM.Api.Auth;
using CRM.Api.Security;
using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Sla;

public static class EscalationRuleEndpoints
{
    public static void MapEscalationRuleEndpoints(this IEndpointRouteBuilder app)
    {
        // GETs are management-oriented (Admin only in this codebase — no
        // Manager role exists; see RolePermissions.cs). Non-GET endpoints
        // require the dedicated ManageSlaEscalationRules permission.
        var reads = app.MapGroup("/api/sla/escalation-rules")
            .RequireAuthorization("AdminOnly")
            .WithTags("EscalationRules");

        var writes = app.MapGroup("/api/sla/escalation-rules")
            .RequireAuthorization(Permissions.ManageSlaEscalationRules)
            .WithTags("EscalationRules");

        reads.MapGet("/", async (TicketDbContext db) =>
        {
            var items = await db.EscalationRules
                .AsNoTracking()
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => ToDto(r))
                .ToListAsync();

            return Results.Ok(items);
        })
        .WithName("ListEscalationRules");

        reads.MapGet("/{id:guid}", async (Guid id, TicketDbContext db) =>
        {
            var entity = await db.EscalationRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            return entity is null ? Results.NotFound() : Results.Ok(ToDto(entity));
        })
        .WithName("GetEscalationRule");

        writes.MapPost("/", async (CreateEscalationRuleRequest request, TicketDbContext db, IAuditLogger auditLogger) =>
        {
            var (validationError, name, trigger) = await Validate(request.Name, request.Trigger, db, null);
            if (validationError is not null)
            {
                return validationError;
            }

            var now = DateTimeOffset.UtcNow;
            var entity = new EscalationRule
            {
                Id = Guid.NewGuid(),
                Name = name,
                Trigger = trigger,
                NotifyAgent = request.NotifyAgent,
                NotifyManager = request.NotifyManager,
                IsActive = request.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
            };

            db.EscalationRules.Add(entity);
            await db.SaveChangesAsync();

            await auditLogger.WriteAsync(
                AuditActions.EscalationRuleCreated, targetType: "escalationRule", targetId: entity.Id.ToString());

            return Results.Created($"/api/sla/escalation-rules/{entity.Id}", ToDto(entity));
        })
        .WithName("CreateEscalationRule");

        writes.MapPut("/{id:guid}", async (
            Guid id, UpdateEscalationRuleRequest request, TicketDbContext db, IAuditLogger auditLogger) =>
        {
            var entity = await db.EscalationRules.FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var (validationError, name, trigger) = await Validate(request.Name, request.Trigger, db, id);
            if (validationError is not null)
            {
                return validationError;
            }

            entity.Name = name;
            entity.Trigger = trigger;
            entity.NotifyAgent = request.NotifyAgent;
            entity.NotifyManager = request.NotifyManager;
            entity.IsActive = request.IsActive;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            await auditLogger.WriteAsync(
                AuditActions.EscalationRuleUpdated, targetType: "escalationRule", targetId: entity.Id.ToString());

            return Results.Ok(ToDto(entity));
        })
        .WithName("UpdateEscalationRule");

        writes.MapPatch("/{id:guid}/activate", async (Guid id, TicketDbContext db, IAuditLogger auditLogger) =>
        {
            var entity = await db.EscalationRules.FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.IsActive = true;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            await auditLogger.WriteAsync(
                AuditActions.EscalationRuleActivated, targetType: "escalationRule", targetId: entity.Id.ToString());

            return Results.Ok(ToDto(entity));
        })
        .WithName("ActivateEscalationRule");

        writes.MapPatch("/{id:guid}/deactivate", async (Guid id, TicketDbContext db, IAuditLogger auditLogger) =>
        {
            var entity = await db.EscalationRules.FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            entity.IsActive = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            await auditLogger.WriteAsync(
                AuditActions.EscalationRuleDeactivated, targetType: "escalationRule", targetId: entity.Id.ToString());

            return Results.Ok(ToDto(entity));
        })
        .WithName("DeactivateEscalationRule");

        writes.MapDelete("/{id:guid}", async (Guid id, TicketDbContext db, IAuditLogger auditLogger) =>
        {
            var entity = await db.EscalationRules.FirstOrDefaultAsync(r => r.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            db.EscalationRules.Remove(entity);
            await db.SaveChangesAsync();

            await auditLogger.WriteAsync(
                AuditActions.EscalationRuleRemoved, targetType: "escalationRule", targetId: entity.Id.ToString());

            return Results.NoContent();
        })
        .WithName("DeleteEscalationRule");
    }

    private static async Task<(IResult? Error, string Name, EscalationTrigger Trigger)> Validate(
        string? rawName, EscalationTrigger trigger, TicketDbContext db, Guid? excludeId)
    {
        var name = rawName?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            return (Results.BadRequest(new ErrorResponse("Name is required.")), name, trigger);
        }
        if (name.Length > 128)
        {
            return (Results.BadRequest(new ErrorResponse("Name must be 128 characters or fewer.")), name, trigger);
        }

        if (!Enum.IsDefined(trigger))
        {
            return (Results.BadRequest(new ErrorResponse("Unknown trigger. Allowed values: AtRisk, Breached.")), name, trigger);
        }

        var duplicate = await db.EscalationRules
            .AsNoTracking()
            .Where(r => r.Id != excludeId)
            .AnyAsync(r => r.Name.ToLower() == name.ToLower());
        if (duplicate)
        {
            return (Results.Conflict(new ErrorResponse($"An escalation rule named '{name}' already exists.")), name, trigger);
        }

        return (null, name, trigger);
    }

    private static EscalationRuleDto ToDto(EscalationRule r) => new(
        r.Id, r.Name, r.Trigger, r.NotifyAgent, r.NotifyManager, r.IsActive, r.CreatedAt, r.UpdatedAt);
}
