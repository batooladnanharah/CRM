using CRM.Api.Auth;
using CRM.Api.Tickets;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Sla;

public static class SlaPolicyEndpoints
{
    public static void MapSlaPolicyEndpoints(this IEndpointRouteBuilder app)
    {
        // SLA policy CRUD is admin-only — agents consume the computed SLA
        // snapshot on tickets but never manage the policies themselves.
        var policies = app.MapGroup("/api/sla/policies")
            .RequireAuthorization("AdminOnly")
            .WithTags("SlaPolicies");

        // Separate group (not nested under /policies) — this is an
        // automation/ops control, not a policy resource.
        var sla = app.MapGroup("/api/sla")
            .RequireAuthorization("AdminOnly")
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
        .WithName("ListSlaPolicies");

        policies.MapGet("/{id:guid}", async (Guid id, TicketDbContext db) =>
        {
            var entity = await db.SlaPolicies.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            return entity is null ? Results.NotFound() : Results.Ok(ToResponse(entity));
        })
        .WithName("GetSlaPolicy");

        policies.MapPost("/", async (CreateSlaPolicyRequest request, TicketDbContext db) =>
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

            return Results.Created($"/api/sla/policies/{entity.Id}", ToResponse(entity));
        })
        .WithName("CreateSlaPolicy");

        policies.MapPut("/{id:guid}", async (Guid id, UpdateSlaPolicyRequest request, TicketDbContext db) =>
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

            return Results.Ok(ToResponse(entity));
        })
        .WithName("UpdateSlaPolicy");

        // Bypasses the SlaAutomationHostedService timer — used by tests/ops to
        // trigger an evaluation cycle deterministically and on demand.
        sla.MapPost("/evaluate-now", async (ISlaEvaluator evaluator, CancellationToken ct) =>
        {
            var evaluatedCount = await evaluator.EvaluateAllOpenAsync(ct);
            return Results.Ok(new { evaluatedCount });
        })
        .WithName("EvaluateSlaNow");

        policies.MapDelete("/{id:guid}", async (Guid id, TicketDbContext db) =>
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

            return Results.NoContent();
        })
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

        if (firstResponseMinutes <= 0)
        {
            return Results.BadRequest(new ErrorResponse("FirstResponseMinutes must be greater than zero."));
        }
        if (resolutionMinutes <= 0)
        {
            return Results.BadRequest(new ErrorResponse("ResolutionMinutes must be greater than zero."));
        }

        return null;
    }

    private static SlaPolicyResponse ToResponse(SlaPolicy p) => new(
        p.Id, p.Name, p.Channel, p.Priority, p.FirstResponseMinutes, p.ResolutionMinutes,
        p.IsDefault, p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc);
}
