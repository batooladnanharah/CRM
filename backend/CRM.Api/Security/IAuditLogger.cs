namespace CRM.Api.Security;

public interface IAuditLogger
{
    Task WriteAsync(
        string action,
        string? targetType = null,
        string? targetId = null,
        object? payload = null,
        CancellationToken ct = default);
}
