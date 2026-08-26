namespace CRM.Api.Security;

// Immutable by convention — nothing in this codebase ever updates or deletes
// an AuditLog row; only IAuditLogger ever writes one.
public sealed class AuditLog
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    // Null when the action has no authenticated actor yet (e.g. a login
    // attempt, successful or failed) — the target identifies who acted on
    // themselves in that case.
    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }

    public string Action { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Small JSON snapshot (<= 4 KB, truncated by AuditLogger) — never the
    // password hash or any other secret.
    public string? PayloadJson { get; set; }
}
