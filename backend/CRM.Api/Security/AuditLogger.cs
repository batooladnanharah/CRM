using System.Security.Claims;
using System.Text.Json;
using CRM.Api.Auth;

namespace CRM.Api.Security;

public sealed class AuditLogger(AuthDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditLogger
{
    private const int MaxPayloadJsonLength = 4096;

    public async Task WriteAsync(
        string action, string? targetType = null, string? targetId = null, object? payload = null,
        CancellationToken ct = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var principal = httpContext?.User;

        // No actor claims exist for an anonymous request (e.g. a login
        // attempt) — the caller passes the attempted identity as the target
        // instead, so the entry stays meaningful.
        Guid? actorUserId = null;
        if (Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedActorId))
        {
            actorUserId = parsedActorId;
        }
        var actorEmail = principal?.FindFirstValue(ClaimTypes.Email);

        var ipAddress = httpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? httpContext?.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext?.Request.Headers.UserAgent.FirstOrDefault();

        string? payloadJson = null;
        if (payload is not null)
        {
            payloadJson = JsonSerializer.Serialize(payload);
            if (payloadJson.Length > MaxPayloadJsonLength)
            {
                payloadJson = payloadJson[..MaxPayloadJsonLength] + "...(truncated)";
            }
        }

        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = DateTime.UtcNow,
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            PayloadJson = payloadJson,
        });

        await db.SaveChangesAsync(ct);
    }
}
