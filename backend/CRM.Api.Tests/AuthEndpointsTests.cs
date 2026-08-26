using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private int AuditCount(string action, string targetId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.AuditLogs.Count(a => a.Action == action && a.TargetId == targetId);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = CustomWebApplicationFactory.ActivePassword,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Contains("agent", body.User.Roles);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401Generic()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@crm.local",
            password = "whatever-123",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Invalid email or password.", body!.Message);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401Generic()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = "the-wrong-password",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Invalid email or password.", body!.Message);
    }

    [Fact]
    public async Task Login_InactiveUser_Returns401Generic()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.InactiveEmail,
            password = CustomWebApplicationFactory.InactivePassword,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Invalid email or password.", body!.Message);
    }

    [Fact]
    public async Task Login_MissingEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "",
            password = "whatever-123",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Email is required.", body!.Message);
    }

    [Fact]
    public async Task Login_MissingPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = "",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Password is required.", body!.Message);
    }

    [Fact]
    public async Task Login_MalformedEmail_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "not-an-email",
            password = "whatever-123",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Invalid email format.", body!.Message);
    }

    [Fact]
    public async Task Login_ResponseDoesNotContainPasswordHash()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = CustomWebApplicationFactory.ActivePassword,
        });

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"hash\"", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsClaims()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = CustomWebApplicationFactory.ActivePassword,
        });
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<AuthUserDto>();
        Assert.Equal(CustomWebApplicationFactory.ActiveEmail, me!.Email);
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var response = await _client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_Returns204()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = CustomWebApplicationFactory.ActivePassword,
        });
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithMalformedToken_Returns401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_WritesLoginSucceededAuditEntry()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = CustomWebApplicationFactory.ActivePassword,
        });
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.True(AuditCount(AuditActions.LoginSucceeded, body!.User.Id.ToString()) >= 1);
    }

    [Fact]
    public async Task Login_UnknownEmail_WritesLoginFailedAuditEntry()
    {
        await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "unknown-audit@crm.local",
            password = "whatever-123",
        });

        Assert.Equal(1, AuditCount(AuditActions.LoginFailed, "unknown-audit@crm.local"));
    }

    [Fact]
    public async Task Login_WrongPassword_WritesLoginFailedAuditEntry()
    {
        using var scope = _factory.Services.CreateScope();
        var userId = scope.ServiceProvider.GetRequiredService<AuthDbContext>()
            .Users.Single(u => u.Email == CustomWebApplicationFactory.ActiveEmail).Id;

        await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = "the-wrong-password",
        });

        Assert.True(AuditCount(AuditActions.LoginFailed, userId.ToString()) >= 1);
    }

    [Fact]
    public async Task Login_InactiveUser_WritesLoginFailedAuditEntry()
    {
        using var scope = _factory.Services.CreateScope();
        var userId = scope.ServiceProvider.GetRequiredService<AuthDbContext>()
            .Users.Single(u => u.Email == CustomWebApplicationFactory.InactiveEmail).Id;

        await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.InactiveEmail,
            password = CustomWebApplicationFactory.InactivePassword,
        });

        Assert.True(AuditCount(AuditActions.LoginFailed, userId.ToString()) >= 1);
    }
}
