using CRM.Api.Auth;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Sla;

public interface IManagerResolver
{
    Task<IReadOnlyList<Guid>> ResolveManagersAsync(Guid ticketId, CancellationToken ct);
}

// Resolves the recipient(s) for "notify manager" escalations.
//
// TODO: department manager (CRM-63) — Ticket has no DepartmentId field yet
// and this codebase has no Department entity, so the department->manager
// resolution step from the story cannot be implemented. Falls straight to
// the role-based approach: this codebase also has no "Manager" role (only
// Admin/Agent/Customer — see Roles.cs), so "manager" here means Admin.
public sealed class ManagerResolver(AuthDbContext authDb) : IManagerResolver
{
    public async Task<IReadOnlyList<Guid>> ResolveManagersAsync(Guid ticketId, CancellationToken ct)
    {
        var admins = await authDb.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Roles.Contains(Roles.Admin))
            .Select(u => u.Id)
            .ToListAsync(ct);

        return admins;
    }
}
