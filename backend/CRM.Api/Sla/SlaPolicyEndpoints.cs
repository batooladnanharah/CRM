using CRM.Api.Auth;
using CRM.Api.Security;
using CRM.Api.Tickets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Sla;

public static class SlaPolicyEndpoints
{
    public static void MapSlaPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        // CRM-60: reads require only SlaPolicyRead (Agent + Admin); mutations
        // require SlaManage (Admin only in this codebase's role model, which
        // has no separate "Manager" role — see RolePermissions.cs).
        var policies = app.MapGroup("/api/sla/policies")
            .WithTags("SlaPolicies");

        // Separate group (not nested under /policies) — this is an
        // automation/ops control, not a policy resource.
        var sla = app.MapGroup("/api/sla")
            .RequireAuthorization(Permissions.SlaManage)
            .WithTags("SlaPolicies");

        policies.MapGet("/", async (TicketDbContext db) =>
        {
            var items = await db.SlaPolicies
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => ToResponse(p))
                .ToListAsync();

            return Results.Ok(items);
        })
        .RequireAuthorization(Permissions.SlaPolicyRead)
        .WithName("ListSlaPolicies");

        policies.MapGet("/{id:guid}", async (Guid id, TicketDbContext db) =>
        {
            var entity = await db.SlaPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            return entity is null ? Results.NotFound() : Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.SlaPolicyRead)
        .WithName("GetSlaPolicy");

        policies.MapPost("/", async (CreateSlaPolicyRequest request, TicketDbContext db, IAuditLogger auditLogger) =>
        {
            var validationError = Validate(
                request.Name, request.Priority, request.FirstResponseMinutes, request.ResolutionMinutes,
                out var name, out var priority);
            if (validationError is not null)
            {
                return validationError;
            }

            var now = DateTime.UtcNow;
            var entity = new SlaPolicy
            {
                Id = Guid.NewGuid(),
                Name = name,
                Channel = string.IsNullOrWhiteSpace(request.Channel) ? null : request.Channel.Trim(),
                Priority = priority,
                FirstResponseMinutes = request.FirstResponseMinutes,
                ResolutionMinutes = request.ResolutionMinutes,
                IsDefault = request.IsDefault,
                IsActive = request.IsActive,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            if (request.IsDefault)
            {
                // Clear any existing default(s) and insert the new policy in
                // the same SaveChangesAsync call — this is the atomic unit on
                // both Npgsql and the EF Core InMemory test provider (which
                // does not support explicit transactions).
                await ClearExistingDefaultsAsync(db);
            }

            db.SlaPolicies.Add(entity);
            await db.SaveChangesAsync();

            await auditLogger.WriteAsync(
                AuditActions.SlaPolicyCreated, targetType: "slaPolicy", targetId: entity.Id.ToString());

            return Results.Created($"/api/sla/policies/{entity.Id}", ToResponse(entity));
        })
        .RequireAuthorization(Permissions.SlaManage)
        .WithName("CreateSlaPolicy");

        policies.MapPut("/{id:guid}", async (Guid id, UpdateSlaPolicyRequest request, TicketDbContext db, IAuditLogger auditLogger) =>
        {
            var entity = await db.SlaPolicies.FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var validationError = Validate(
                request.Name, request.Priority, request.FirstResponseMinutes, request.ResolutionMinutes,
                out var name, out var priority);
            if (validationError is not null)
            {
                return validationError;
            }

            if (request.IsDefault && !entity.IsDefault)
            {
                await ClearExistingDefaultsAsync(db, excludeId: id);
            }

            entity.Name = name;
            entity.Channel = string.IsNullOrWhiteSpace(request.Channel) ? null : request.Channel.Trim();
            entity.Priority = priority;
            entity.FirstResponseMinutes = request.FirstResponseMinutes;
            entity.ResolutionMinutes = request.ResolutionMinutes;
            entity.IsDefault = request.IsDefault;
            entity.IsActive = request.IsActive;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await auditLogger.WriteAsync(
                AuditActions.SlaPolicyUpdated, targetType: "slaPolicy", targetId: entity.Id.ToString());

            return Results.Ok(ToResponse(entity));
        })
        .RequireAuthorization(Permissions.SlaManage)
        .WithName("UpdateSlaPolicy");

        // CRM-60: dedicated status toggle for activate/deactivate and
        // set-default flows without resending the full policy payload.
        // Deactivating the current default clears IsDefault (rather than
        // rejecting) and reports a warning so the frontend can toast it.
        policies.MapPatch("/{id:guid}/status", async (
            Guid id, UpdateSlaPolicyStatusRequest request, TicketDbContext db, IAuditLogger auditLogger) =>
        {
            var entity = await db.SlaPolicies.FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            var wasActive = entity.IsActive;
            var wasDefault = entity.IsDefault;

            var warnings = new List<string>();
            var wantsDefault = request.IsDefault ?? entity.IsDefault;

            if (!request.IsActive && (request.IsDefault ?? entity.IsDefault))
            {
                // Deactivating a default policy clears the default flag
                // instead of rejecting the request.
                wantsDefault = false;
                warnings.Add("sla.defaultCleared");
            }

            if (wantsDefault && !entity.IsDefault)
            {
                await ClearExistingDefaultsAsync(db, excludeId: id);
            }

            entity.IsActive = request.IsActive;
            entity.IsDefault = wantsDefault;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            // Mirrors SecurityAdminEndpoints' enable/disable pair: report the
            // post-toggle active state as its own action, then a separate
            // entry when the default flag newly changed.
            if (entity.IsActive != wasActive)
            {
                await auditLogger.WriteAsync(
                    entity.IsActive ? AuditActions.SlaPolicyActivated : AuditActions.SlaPolicyDeactivated,
                    targetType: "slaPolicy", targetId: entity.Id.ToString());
            }
            if (entity.IsDefault && !wasDefault)
            {
                await auditLogger.WriteAsync(
                    AuditActions.SlaPolicyDefaultSet, targetType: "slaPolicy", targetId: entity.Id.ToString());
            }

            return Results.Ok(new UpdateSlaPolicyStatusResponse(ToResponse(entity), warnings));
        })
        .RequireAuthorization(Permissions.SlaManage)
        .WithName("UpdateSlaPolicyStatus");

        // Bypasses the SlaAutomationHostedService timer — used by tests/ops to
        // trigger an evaluation cycle deterministically and on demand.
        sla.MapPost("/evaluate-now", async (ISlaEvaluator evaluator, CancellationToken ct) =>
        {
            var evaluatedCount = await evaluator.EvaluateAllOpenAsync(ct);
            return Results.Ok(new { evaluatedCount });
        })
        .WithName("EvaluateSlaNow");

        policies.MapDelete("/{id:guid}", async (Guid id, TicketDbContext db, IAuditLogger auditLogger) =>
        {
            var entity = await db.SlaPolicies.FirstOrDefaultAsync(p => p.Id == id);
            if (entity is null)
            {
                return Results.NotFound();
            }

            // Soft delete only — tickets may already reference this policy's
            // Id via Ticket.SlaPolicyId, and that reference must stay valid.
            entity.IsActive = false;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await auditLogger.WriteAsync(
                AuditActions.SlaPolicyRemoved, targetType: "slaPolicy", targetId: entity.Id.ToString());

            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.SlaManage)
        .WithName("DeleteSlaPolicy");
    }

    private static async Task ClearExistingDefaultsAsync(TicketDbContext db, Guid? excludeId = null)
    {
        var currentDefaults = await db.SlaPolicies
            .Where(p => p.IsDefault && p.Id != excludeId)
            .ToListAsync();

        foreach (var policy in currentDefaults)
        {
            policy.IsDefault = false;
            policy.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private static IResult? Validate(
        string? rawName, string rawPriority, int firstResponseMinutes, int resolutionMinutes,
        out string name, out TicketPriority priority)
    {
        name = rawName?.Trim() ?? string.Empty;
        priority = default;

        if (string.IsNullOrEmpty(name))
        {
            return Results.BadRequest(new ErrorResponse("Name is required."));
        }
        if (name.Length > 200)
        {
            return Results.BadRequest(new ErrorResponse("Name must be 200 characters or fewer."));
        }

        if (!Enum.TryParse(rawPriority, ignoreCase: true, out priority))
        {
            return Results.BadRequest(new ErrorResponse(
                $"Unknown priority '{rawPriority}'. Allowed values: {string.Join(", ", Enum.GetNames<TicketPriority>())}."));
        }

        // 525600 minutes = 1 year; guards against pathological values that
        // would otherwise silently push due-dates decades into the future.
        const int MaxMinutes = 525600;

        if (firstResponseMinutes <= 0)
        {
            return Results.BadRequest(new ErrorResponse("FirstResponseMinutes must be greater than zero."));
        }
        if (firstResponseMinutes > MaxMinutes)
        {
            return Results.BadRequest(new ErrorResponse("errors.targetTooLarge"));
        }
        if (resolutionMinutes <= 0)
        {
            return Results.BadRequest(new ErrorResponse("ResolutionMinutes must be greater than zero."));
        }
        if (resolutionMinutes > MaxMinutes)
        {
            return Results.BadRequest(new ErrorResponse("errors.targetTooLarge"));
        }
        if (resolutionMinutes < firstResponseMinutes)
        {
            return Results.BadRequest(new ErrorResponse("errors.resolutionLessThanResponse"));
        }

        return null;
    }

    private static SlaPolicyResponse ToResponse(SlaPolicy p) => new(
        p.Id, p.Name, p.Channel, p.Priority, p.FirstResponseMinutes, p.ResolutionMinutes,
        p.IsDefault, p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc);
}
