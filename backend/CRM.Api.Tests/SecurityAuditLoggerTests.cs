using System.Security.Claims;
using CRM.Api.Auth;
using CRM.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class SecurityAuditLoggerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SecurityAuditLoggerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private (IAuditLogger Logger, AuthDbContext Db, IServiceScope Scope) CreateLogger(HttpContext? httpContext)
    {
        var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var accessor = new HttpContextAccessorStub(httpContext);
        return (new AuditLogger(db, accessor), db, scope);
    }

    private sealed class HttpContextAccessorStub(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }

    [Fact]
    public async Task WriteAsync_SerializesPayload_AndPersistsEntry()
    {
        var (logger, db, scope) = CreateLogger(httpContext: null);
        using var _ = scope;

        await logger.WriteAsync(
            AuditActions.UserEnabled, targetType: "user", targetId: "abc-123",
            payload: new { before = false, after = true });

        var entry = db.AuditLogs.Single(a => a.TargetId == "abc-123");
        Assert.Equal(AuditActions.UserEnabled, entry.Action);
        Assert.Equal("user", entry.TargetType);
        Assert.Contains("\"before\":false", entry.PayloadJson);
        Assert.Contains("\"after\":true", entry.PayloadJson);
    }

    [Fact]
    public async Task WriteAsync_TruncatesPayload_WhenLargerThan4Kb()
    {
        var (logger, db, scope) = CreateLogger(httpContext: null);
        using var _ = scope;

        var hugeValue = new string('x', 5000);
        await logger.WriteAsync(
            AuditActions.RoleAssigned, targetType: "user", targetId: "big-payload",
            payload: new { note = hugeValue });

        var entry = db.AuditLogs.Single(a => a.TargetId == "big-payload");
        Assert.True(entry.PayloadJson!.Length <= 4096 + "...(truncated)".Length);
        Assert.EndsWith("...(truncated)", entry.PayloadJson);
    }

    [Fact]
    public async Task WriteAsync_ExtractsActorClaims_FromHttpContext()
    {
        var actorId = Guid.NewGuid();
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
            new Claim(ClaimTypes.Email, "actor@crm.local"),
        ], authenticationType: "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        var (logger, db, scope) = CreateLogger(httpContext);
        using var _ = scope;

        await logger.WriteAsync(AuditActions.RoleAssigned, targetType: "user", targetId: "target-1");

        var entry = db.AuditLogs.Single(a => a.TargetId == "target-1");
        Assert.Equal(actorId, entry.ActorUserId);
        Assert.Equal("actor@crm.local", entry.ActorEmail);
    }

    [Fact]
    public async Task WriteAsync_LeavesActorNull_WhenNoHttpContext()
    {
        var (logger, db, scope) = CreateLogger(httpContext: null);
        using var _ = scope;

        await logger.WriteAsync(AuditActions.LoginFailed, targetType: "user", targetId: "anon@crm.local");

        var entry = db.AuditLogs.Single(a => a.TargetId == "anon@crm.local");
        Assert.Null(entry.ActorUserId);
        Assert.Null(entry.ActorEmail);
    }

    [Fact]
    public async Task WriteAsync_FallsBackToRemoteIpAddress_WhenNoForwardedForHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.7");

        var (logger, db, scope) = CreateLogger(httpContext);
        using var _ = scope;

        await logger.WriteAsync(AuditActions.LoginSucceeded, targetType: "user", targetId: "ip-fallback");

        var entry = db.AuditLogs.Single(a => a.TargetId == "ip-fallback");
        Assert.Equal("203.0.113.7", entry.IpAddress);
    }

    [Fact]
    public async Task WriteAsync_PrefersForwardedForHeader_OverRemoteIpAddress()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.7");
        httpContext.Request.Headers["X-Forwarded-For"] = "198.51.100.9";

        var (logger, db, scope) = CreateLogger(httpContext);
        using var _ = scope;

        await logger.WriteAsync(AuditActions.LoginSucceeded, targetType: "user", targetId: "ip-forwarded");

        var entry = db.AuditLogs.Single(a => a.TargetId == "ip-forwarded");
        Assert.Equal("198.51.100.9", entry.IpAddress);
    }
}
