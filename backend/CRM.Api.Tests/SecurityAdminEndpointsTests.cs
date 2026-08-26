using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Each scenario below that depends on an exact admin/user count gets its own
// IClassFixture<CustomWebApplicationFactory> class — SeedUsers() already
// seeds two admin-role accounts (AdminEmail, MultiRoleEmail), so tests that
// assert "last admin" behaviour must control the full admin population
// themselves rather than share a fixture with anything else.

public class SecurityAdminUsersListTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityAdminUsersListTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task Get_Users_Returns200_ForAdmin()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResultOfAdminUserListItem>();
        Assert.Contains(body!.Items, u => u.Email == CustomWebApplicationFactory.AdminEmail);
    }

    [Fact]
    public async Task Get_Users_Returns403_ForAgent()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_Users_Returns401_ForAnonymous()
    {
        var response = await _client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Users_FiltersByRoleAndDisabled()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await client.GetAsync("/api/admin/users?role=agent&disabled=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResultOfAdminUserListItem>();
        Assert.Contains(body!.Items, u => u.Email == CustomWebApplicationFactory.InactiveEmail);
        Assert.DoesNotContain(body.Items, u => u.Email == CustomWebApplicationFactory.ActiveEmail);
    }

    [Fact]
    public async Task Get_UserById_Returns404_WhenMissing()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await client.GetAsync($"/api/admin/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class SecurityAdminRoleChangeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityAdminRoleChangeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid UserIdByEmail(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.Users.Single(u => u.Email == email).Id;
    }

    private int AuditCount(string action, string targetId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.AuditLogs.Count(a => a.Action == action && a.TargetId == targetId);
    }

    private Guid CreateFreshAgentUser(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var user = new User { Id = Guid.NewGuid(), Email = email, Name = "Fresh Agent", IsActive = true, Roles = ["agent"] };
        user.PasswordHash = hasher.HashPassword(user, "Correct#Passw0rd!");
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    private Guid CreateCustomer(string fullName, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    [Fact]
    public async Task Put_Role_ToCustomer_RequiresCustomerId()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var targetId = CreateFreshAgentUser("role-to-customer-no-link@crm.local");

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{targetId}/role", new { role = "customer" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("customer_id_required", body!.Message);
    }

    [Fact]
    public async Task Put_Role_ToCustomer_WithCustomerId_LinksTheCustomerRecord()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var targetId = CreateFreshAgentUser("role-to-customer-linked@crm.local");
        var customerId = CreateCustomer("Role Change Customer Co", "role-change@example.com");

        var response = await admin.PutAsJsonAsync(
            $"/api/admin/users/{targetId}/role", new { role = "customer", customerId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserDetail>();
        Assert.Equal("customer", body!.Role);
        Assert.Equal(customerId, body.CustomerId);
    }

    [Fact]
    public async Task Put_Role_Succeeds_AndWritesAuditEntry()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var targetId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{targetId}/role", new { role = "admin" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserDetail>();
        Assert.Equal("admin", body!.Role);
        Assert.Equal(1, AuditCount(AuditActions.RoleAssigned, targetId.ToString()));
    }

    [Fact]
    public async Task Put_Role_Returns409_ForSelfMutation()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var selfId = UserIdByEmail(CustomWebApplicationFactory.AdminEmail);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{selfId}/role", new { role = "agent" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("cannot_modify_self", body!.Message);
    }

    [Fact]
    public async Task Put_Role_Returns400_ForInvalidRole()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var targetId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{targetId}/role", new { role = "superuser" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Role_Returns404_WhenTargetMissing()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PutAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/role", new { role = "agent" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Role_Returns403_ForAgent()
    {
        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);
        var targetId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var response = await agent.PutAsJsonAsync($"/api/admin/users/{targetId}/role", new { role = "admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public class SecurityAdminDisableEnableTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityAdminDisableEnableTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid UserIdByEmail(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.Users.Single(u => u.Email == email).Id;
    }

    [Fact]
    public async Task Post_Disable_BlocksSubsequentLogin()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var targetId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var disable = await admin.PostAsync($"/api/admin/users/{targetId}/disable", content: null);
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

        var loginAttempt = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.SecondAgentEmail,
            password = CustomWebApplicationFactory.SecondAgentPassword,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginAttempt.StatusCode);
    }

    [Fact]
    public async Task Post_Enable_AllowsLoginAgain()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var targetId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        await admin.PostAsync($"/api/admin/users/{targetId}/disable", content: null);
        var enable = await admin.PostAsync($"/api/admin/users/{targetId}/enable", content: null);
        Assert.Equal(HttpStatusCode.OK, enable.StatusCode);

        var loginAttempt = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.SecondAgentEmail,
            password = CustomWebApplicationFactory.SecondAgentPassword,
        });

        Assert.Equal(HttpStatusCode.OK, loginAttempt.StatusCode);
    }

    [Fact]
    public async Task Post_Disable_Returns409_ForSelfMutation()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var selfId = UserIdByEmail(CustomWebApplicationFactory.AdminEmail);

        var response = await admin.PostAsync($"/api/admin/users/{selfId}/disable", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}

public class SecurityAdminAuditLogTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityAdminAuditLogTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task Get_AuditLog_FiltersByAction_AndPaginates()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        // Two failed logins produce two AuditActions.LoginFailed entries
        // scoped to this test's own factory instance.
        await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = "nobody-audit@crm.local", password = "whatever" });
        await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = "nobody-audit-2@crm.local", password = "whatever" });

        var response = await admin.GetAsync(
            $"/api/admin/audit-log?action={AuditActions.LoginFailed}&page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResultOfAuditLogEntryResponse>();
        Assert.Single(body!.Items);
        Assert.True(body.TotalCount >= 2);
        Assert.All(body.Items, e => Assert.Equal(AuditActions.LoginFailed, e.Action));
    }

    [Fact]
    public async Task Get_AuditLog_FiltersByTargetId()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = CustomWebApplicationFactory.ActivePassword,
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var response = await admin.GetAsync($"/api/admin/audit-log?targetId={loginBody!.User.Id}");

        var body = await response.Content.ReadFromJsonAsync<PagedResultOfAuditLogEntryResponse>();
        Assert.Contains(body!.Items, e => e.TargetId == loginBody.User.Id.ToString());
    }

    [Fact]
    public async Task Get_AuditLog_FiltersByDateRange()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var farFuture = DateTime.UtcNow.AddYears(10).ToString("O");
        var response = await admin.GetAsync($"/api/admin/audit-log?from={farFuture}");

        var body = await response.Content.ReadFromJsonAsync<PagedResultOfAuditLogEntryResponse>();
        Assert.Empty(body!.Items);
    }

    [Fact]
    public async Task Get_AuditLog_Returns403_ForAgent()
    {
        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await agent.GetAsync("/api/admin/audit-log");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public class SecurityAccessDeniedAuditTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityAccessDeniedAuditTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task AgentHittingAdminUsers_Writes_AccessDeniedAuditEntry()
    {
        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await agent.GetAsync("/api/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        Assert.Contains(db.AuditLogs, a => a.Action == AuditActions.AccessDenied && a.TargetId == "/api/admin/users");
    }
}

public class SecurityAdminCreateUserTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityAdminCreateUserTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private int AuditCount(string action, string targetId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.AuditLogs.Count(a => a.Action == action && a.TargetId == targetId);
    }

    private Guid CreateCustomer(string fullName, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    [Fact]
    public async Task Post_CreateUser_WithCustomerRole_RequiresCustomerId()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            email = "portal.nolink@crm.local",
            password = "Correct#Passw0rd!",
            name = "No Link",
            role = "customer",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("customer_id_required", body!.Message);
    }

    [Fact]
    public async Task Post_CreateUser_WithCustomerRole_Returns400_WhenCustomerIdDoesNotExist()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            email = "portal.badlink@crm.local",
            password = "Correct#Passw0rd!",
            name = "Bad Link",
            role = "customer",
            customerId = Guid.NewGuid(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("customer_not_found", body!.Message);
    }

    [Fact]
    public async Task Post_CreateUser_WithCustomerRole_LinksTheCustomerRecord()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var customerId = CreateCustomer("Portal Customer Co", "portal.link@example.com");

        var response = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            email = "portal.link@crm.local",
            password = "Correct#Passw0rd!",
            name = "Portal Link",
            role = "customer",
            customerId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserDetail>();
        Assert.Equal(customerId, body!.CustomerId);

        // The new account can now reach the customer portal (the regression this
        // guards against — see CurrentCustomerAccessor.cs).
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = "portal.link@crm.local", password = "Correct#Passw0rd!" });
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        var portalClient = _factory.CreateClient();
        portalClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody!.Token);
        var dashboard = await portalClient.GetAsync("/api/customer/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
    }

    [Fact]
    public async Task Post_CreateUser_PersistsUser_AndWritesAudit()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            email = "new.agent@crm.local",
            password = "Correct#Passw0rd!",
            name = "New Agent",
            role = "agent",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserDetail>();
        Assert.Equal("new.agent@crm.local", body!.Email);
        Assert.Equal("agent", body.Role);
        Assert.False(body.IsDisabled);
        Assert.Equal(1, AuditCount(AuditActions.UserCreated, body.Id.ToString()));

        // The created user can log in with the supplied password immediately.
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = "new.agent@crm.local", password = "Correct#Passw0rd!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Post_CreateUser_Returns409_OnDuplicateEmail()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = "Correct#Passw0rd!",
            name = "Duplicate",
            role = "agent",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("duplicate_email", body!.Message);
    }

    [Fact]
    public async Task Post_CreateUser_Returns400_OnInvalidRole()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            email = "invalid.role@crm.local",
            password = "Correct#Passw0rd!",
            name = "Invalid Role",
            role = "superuser",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("invalid_role", body!.Message);
    }

    [Fact]
    public async Task Post_CreateUser_Returns400_OnWeakPassword()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/admin/users", new
        {
            email = "weak.password@crm.local",
            password = "short",
            name = "Weak Password",
            role = "agent",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("weak_password", body!.Message);
    }

    [Fact]
    public async Task Post_CreateUser_Returns403_ForAgent()
    {
        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await agent.PostAsJsonAsync("/api/admin/users", new
        {
            email = "blocked@crm.local",
            password = "Correct#Passw0rd!",
            name = "Blocked",
            role = "agent",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public class SecurityAdminUpdateUserTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SecurityAdminUpdateUserTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid UserIdByEmail(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.Users.Single(u => u.Email == email).Id;
    }

    private int AuditCount(string action, string targetId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.AuditLogs.Count(a => a.Action == action && a.TargetId == targetId);
    }

    private Guid CreateCustomer(string fullName, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    private Guid? CustomerIdOf(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.Users.Single(u => u.Id == userId).CustomerId;
    }

    [Fact]
    public async Task Put_UpdateUser_RelinksCustomerId_ForACustomerRoleUser()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var (portalCustomerId, _) = _factory.SeedPortalCustomers();
        var targetId = UserIdByEmail(CustomWebApplicationFactory.PortalCustomerEmail);
        var newCustomerId = CreateCustomer("Relinked Customer Co", "relinked@example.com");

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{targetId}", new
        {
            email = CustomWebApplicationFactory.PortalCustomerEmail,
            name = "Portal Customer User",
            customerId = newCustomerId,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserDetail>();
        Assert.Equal(newCustomerId, body!.CustomerId);
        Assert.NotEqual(portalCustomerId, body.CustomerId);
    }

    [Fact]
    public async Task Put_UpdateUser_IgnoresCustomerId_ForANonCustomerRoleUser()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var targetId = UserIdByEmail(CustomWebApplicationFactory.InactiveEmail);
        var someCustomerId = CreateCustomer("Ignored Customer Co", "ignored@example.com");

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{targetId}", new
        {
            email = CustomWebApplicationFactory.InactiveEmail,
            name = "Inactive Agent",
            customerId = someCustomerId,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(CustomerIdOf(targetId));
    }

    [Fact]
    public async Task Put_UpdateUser_UpdatesFields_AndWritesAudit()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var targetId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{targetId}", new
        {
            email = "renamed.agent@crm.local",
            name = "Renamed Agent",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AdminUserDetail>();
        Assert.Equal("renamed.agent@crm.local", body!.Email);
        Assert.Equal("Renamed Agent", body.Name);
        Assert.Equal(1, AuditCount(AuditActions.UserUpdated, targetId.ToString()));
    }

    [Fact]
    public async Task Put_UpdateUser_Returns409_OnDuplicateEmail()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var targetId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{targetId}", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            name = "Second Agent",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("duplicate_email", body!.Message);
    }

    [Fact]
    public async Task Put_UpdateUser_Returns404_WhenTargetMissing()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{Guid.NewGuid()}", new
        {
            email = "nobody@crm.local",
            name = "Nobody",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_UpdateUser_Returns403_ForAgent()
    {
        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);
        var targetId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var response = await agent.PutAsJsonAsync($"/api/admin/users/{targetId}", new
        {
            email = "blocked@crm.local",
            name = "Blocked",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public sealed record PagedResultOfAdminUserListItem(
    IReadOnlyList<AdminUserListItem> Items, int Page, int PageSize, int TotalCount);

public sealed record PagedResultOfAuditLogEntryResponse(
    IReadOnlyList<AuditLogEntryResponse> Items, int Page, int PageSize, int TotalCount);
