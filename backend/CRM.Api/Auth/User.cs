namespace CRM.Api.Auth;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;   // unique, lower-cased
    public string Name { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Links a Customer-role user to the Customer row they portal-log-in as.
    // Nullable so existing agent/admin users (and mis-provisioned customer
    // accounts) are unaffected; no cross-context FK, since Customer lives in
    // CustomerDbContext (same physical database, separate bounded context).
    public Guid? CustomerId { get; set; }
}
