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

    // CRM-62 — simple available/unavailable toggle used by automatic ticket
    // assignment eligibility (TicketAssignmentService). Deliberately not a
    // calendar/shift/presence model — see the story's "Agent Availability"
    // section. Defaults to true so existing agents remain eligible without
    // an explicit opt-in after this column is added.
    public bool IsAvailable { get; set; } = true;
}
