namespace CRM.Api.Security;

// This admin surface manages a single primary role per user — User.Roles is
// technically a list (a handful of seeded fixtures hold more than one), but
// the assign-role endpoint below always replaces it with exactly one value.
// Role here is that first entry, falling back to "" for a roleless account.
public sealed record AdminUserListItem(
    Guid Id, string Email, string Name, string Role, bool IsDisabled, DateTime CreatedAtUtc);

public sealed record AdminUserDetail(
    Guid Id, string Email, string Name, string Role, bool IsDisabled, Guid? CustomerId, DateTime CreatedAtUtc);

public sealed record AdminUserListQuery(
    string? Search, string? Role, bool? Disabled, int Page = 1, int PageSize = 25);

public sealed record AssignRoleRequest(string Role, Guid? CustomerId = null);

public sealed record RoleSummary(string Name, IReadOnlyCollection<string> Permissions);

public sealed record AdminCreateUserRequest(string Email, string Password, string Name, string Role, Guid? CustomerId = null);

public sealed record AdminUpdateUserRequest(string Email, string Name, Guid? CustomerId = null);

public sealed record AuditLogEntryResponse(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    string? ActorEmail,
    string Action,
    string? TargetType,
    string? TargetId,
    string? IpAddress,
    string? PayloadJson);

public sealed record AuditLogQuery(
    Guid? ActorId, string? TargetId, string? Action, DateTime? From, DateTime? To,
    int Page = 1, int PageSize = 25);
