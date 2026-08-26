using System.Security.Claims;
using CRM.Api.Auth;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.CustomerPortal;

public interface ICurrentCustomerAccessor
{
    Task<Guid?> GetCurrentCustomerIdAsync(ClaimsPrincipal principal, CancellationToken ct);
}

// Resolves the Customer a Customer-role JWT is allowed to act as, strictly
// server-side from the authenticated identity — endpoints must never accept
// a customer id from the route/body/query.
public sealed class CurrentCustomerAccessor(AuthDbContext authDb) : ICurrentCustomerAccessor
{
    public async Task<Guid?> GetCurrentCustomerIdAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await authDb.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user?.CustomerId;
    }
}
