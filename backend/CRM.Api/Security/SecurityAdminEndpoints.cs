using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.Customers;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Security;

public static class SecurityAdminEndpoints
{
    private static readonly string[] AllowedRoles = [Roles.Admin, Roles.Agent, Roles.Customer];

    public static void MapSecurityAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin").RequireAuthorization("AdminOnly").WithTags("SecurityAdmin");

        admin.MapGet("/users", async ([AsParameters] AdminUserListQuery query, AuthDbContext db) =>
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            IQueryable<User> filtered = db.Users.AsNoTracking();

            var term = query.Search?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(term))
            {
                filtered = filtered.Where(u =>
                    u.Email.ToLower().Contains(term) || u.Name.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(query.Role))
            {
                filtered = filtered.Where(u => u.Roles.Contains(query.Role));
            }

            if (query.Disabled is not null)
            {
                filtered = query.Disabled.Value
                    ? filtered.Where(u => !u.IsActive)
                    : filtered.Where(u => u.IsActive);
            }

            var totalCount = await filtered.CountAsync();
            var items = await filtered
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => ToListItem(u))
                .ToListAsync();

            return Results.Ok(new PagedResult<AdminUserListItem>(items, page, pageSize, totalCount));
        })
        .WithName("ListAdminUsers");

        admin.MapGet("/users/{id:guid}", async (Guid id, AuthDbContext db) =>
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            return user is null ? Results.NotFound() : Results.Ok(ToDetail(user));
        })
        .WithName("GetAdminUser");

        admin.MapPut("/users/{id:guid}/role", async (
            Guid id, AssignRoleRequest request, AuthDbContext db, IAuditLogger auditLogger,
            ClaimsPrincipal principal, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null)
            {
                return Results.NotFound();
            }

            var normalizedRole = AllowedRoles.FirstOrDefault(
                r => string.Equals(r, request.Role, StringComparison.OrdinalIgnoreCase));
            if (normalizedRole is null)
            {
                return Results.BadRequest(new ErrorResponse(
                    $"Unknown role '{request.Role}'. Allowed values: {string.Join(", ", AllowedRoles)}."));
            }

            var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (user.Id == actorId)
            {
                return Results.Conflict(new ErrorResponse("cannot_modify_self"));
            }

            var wasAdmin = user.Roles.Contains(Roles.Admin);
            if (wasAdmin && normalizedRole != Roles.Admin && await IsLastActiveAdminAsync(db, user.Id, ct))
            {
                return Results.Conflict(new ErrorResponse("last_admin"));
            }

            var beforeRoles = user.Roles.ToList();
            user.Roles = [normalizedRole];
            await db.SaveChangesAsync(ct);

            await auditLogger.WriteAsync(
                AuditActions.RoleAssigned, targetType: "user", targetId: user.Id.ToString(),
                payload: new { before = beforeRoles, after = user.Roles }, ct: ct);

            return Results.Ok(ToDetail(user));
        })
        .WithName("AssignUserRole");

        admin.MapPost("/users/{id:guid}/disable", async (
            Guid id, AuthDbContext db, IAuditLogger auditLogger, ClaimsPrincipal principal, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null)
            {
                return Results.NotFound();
            }

            var actorId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (user.Id == actorId)
            {
                return Results.Conflict(new ErrorResponse("cannot_modify_self"));
            }

            if (user.IsActive && user.Roles.Contains(Roles.Admin) &&
                await IsLastActiveAdminAsync(db, user.Id, ct))
            {
                return Results.Conflict(new ErrorResponse("last_admin"));
            }

            var wasActive = user.IsActive;
            user.IsActive = false;
            await db.SaveChangesAsync(ct);

            if (wasActive)
            {
                await auditLogger.WriteAsync(
                    AuditActions.UserDisabled, targetType: "user", targetId: user.Id.ToString(), ct: ct);
            }

            return Results.Ok(ToDetail(user));
        })
        .WithName("DisableUser");

        admin.MapPost("/users/{id:guid}/enable", async (
            Guid id, AuthDbContext db, IAuditLogger auditLogger, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null)
            {
                return Results.NotFound();
            }

            var wasActive = user.IsActive;
            user.IsActive = true;
            await db.SaveChangesAsync(ct);

            if (!wasActive)
            {
                await auditLogger.WriteAsync(
                    AuditActions.UserEnabled, targetType: "user", targetId: user.Id.ToString(), ct: ct);
            }

            return Results.Ok(ToDetail(user));
        })
        .WithName("EnableUser");

        admin.MapGet("/audit-log", async ([AsParameters] AuditLogQuery query, AuthDbContext db) =>
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);

            IQueryable<AuditLog> filtered = db.AuditLogs.AsNoTracking();

            if (query.ActorId is not null)
            {
                filtered = filtered.Where(a => a.ActorUserId == query.ActorId);
            }
            if (!string.IsNullOrWhiteSpace(query.TargetId))
            {
                filtered = filtered.Where(a => a.TargetId == query.TargetId);
            }
            if (!string.IsNullOrWhiteSpace(query.Action))
            {
                filtered = filtered.Where(a => a.Action == query.Action);
            }
            if (query.From is not null)
            {
                filtered = filtered.Where(a => a.OccurredAtUtc >= query.From);
            }
            if (query.To is not null)
            {
                filtered = filtered.Where(a => a.OccurredAtUtc <= query.To);
            }

            var totalCount = await filtered.CountAsync();
            var items = await filtered
                .OrderByDescending(a => a.OccurredAtUtc)
                .ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => ToEntryResponse(a))
                .ToListAsync();

            return Results.Ok(new PagedResult<AuditLogEntryResponse>(items, page, pageSize, totalCount));
        })
        .WithName("ListAuditLog");
    }

    // "Last active Admin" is evaluated excluding the target user themselves,
    // so demoting/disabling the sole remaining admin is always blocked while
    // any other active admin exists. In practice, combined with the
    // self-mutation block above and the AdminOnly policy on this whole
    // group, this guard is unreachable through the API as it stands today:
    // the acting admin is necessarily a second active admin distinct from
    // the target once self-mutation is ruled out. It's kept anyway as
    // defensive redundancy against future changes (e.g. a bulk-role-change
    // endpoint, or lifting the self-mutation restriction) that could
    // otherwise strand the system with zero active admins.
    private static Task<bool> IsLastActiveAdminAsync(AuthDbContext db, Guid excludingUserId, CancellationToken ct)
        => db.Users.AsNoTracking().AllAsync(
            u => u.Id == excludingUserId || !u.IsActive || !u.Roles.Contains(Roles.Admin), ct);

    private static AdminUserListItem ToListItem(User u) => new(
        u.Id, u.Email, u.Name, u.Roles.FirstOrDefault() ?? string.Empty, !u.IsActive, u.CreatedAtUtc);

    private static AdminUserDetail ToDetail(User u) => new(
        u.Id, u.Email, u.Name, u.Roles.FirstOrDefault() ?? string.Empty, !u.IsActive, u.CustomerId, u.CreatedAtUtc);

    private static AuditLogEntryResponse ToEntryResponse(AuditLog a) => new(
        a.Id, a.OccurredAtUtc, a.ActorUserId, a.ActorEmail, a.Action, a.TargetType, a.TargetId, a.IpAddress,
        a.PayloadJson);
}
