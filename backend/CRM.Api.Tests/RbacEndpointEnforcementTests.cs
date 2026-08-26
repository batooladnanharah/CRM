using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;

namespace CRM.Api.Tests;

// One representative protected route per permission, exercised by a fresh
// user seeded with exactly one role — proves the permission claim (not just
// the old role literal) is what the policy is actually keyed on, end to end
// through real HTTP requests rather than the in-process policy evaluation
// covered by AuthorizationPolicyTests.cs.
public class RbacEndpointEnforcementTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RbacEndpointEnforcementTests(CustomWebApplicationFactory factory)
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

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, true)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword, true)]
    [InlineData(CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword, false)]
    public async Task TicketsManage_ListTickets_AllowsAdminAndAgent_DeniesCustomer(
        string email, string password, bool allowed)
    {
        var client = await AuthenticatedClientAsync(email, password);

        var response = await client.GetAsync("/api/tickets");

        Assert.Equal(
            allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, true)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword, false)]
    public async Task TicketsEscalate_OnlyAdmin(string email, string password, bool allowed)
    {
        var client = await AuthenticatedClientAsync(email, password);

        // A non-existent ticket still exercises the policy first — a 403 for
        // Agent proves the permission check runs before the 404 the handler
        // would otherwise return; Admin reaching 404 (not 403) proves the
        // policy let the request through.
        var response = await client.PostAsJsonAsync($"/api/tickets/{Guid.NewGuid()}/escalate", new { reason = "x" });

        if (allowed)
        {
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, true)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword, false)]
    public async Task SlaManage_OnlyAdmin(string email, string password, bool allowed)
    {
        var client = await AuthenticatedClientAsync(email, password);

        var response = await client.GetAsync("/api/sla/policies");

        Assert.Equal(
            allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, true)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword, false)]
    public async Task ReportsView_OnlyAdmin(string email, string password, bool allowed)
    {
        var client = await AuthenticatedClientAsync(email, password);

        var response = await client.GetAsync("/api/reports/summary");

        Assert.Equal(
            allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, true)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword, true)]
    [InlineData(CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword, false)]
    public async Task KnowledgeBaseView_AllowsAdminAndAgent_DeniesCustomer(
        string email, string password, bool allowed)
    {
        var client = await AuthenticatedClientAsync(email, password);

        var response = await client.GetAsync("/api/knowledge-base/articles");

        Assert.Equal(
            allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, true)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword, false)]
    public async Task KnowledgeBaseManage_OnlyAdmin(string email, string password, bool allowed)
    {
        var client = await AuthenticatedClientAsync(email, password);

        var response = await client.PostAsJsonAsync(
            "/api/knowledge-base/articles", new { title = "x", body = "x", category = "x" });

        if (allowed)
        {
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, true)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword, false)]
    public async Task CommunicationChannelsManage_OnlyAdmin(string email, string password, bool allowed)
    {
        var client = await AuthenticatedClientAsync(email, password);

        var response = await client.PostAsJsonAsync(
            "/api/channels", new { name = "x", type = "Email" });

        if (allowed)
        {
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, true)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword, false)]
    public async Task QuickRepliesManage_OnlyAdmin(string email, string password, bool allowed)
    {
        var client = await AuthenticatedClientAsync(email, password);

        var response = await client.PostAsJsonAsync("/api/quick-replies", new { title = "x", content = "x" });

        if (allowed)
        {
            Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        }
        else
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword, true)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword, false)]
    [InlineData(CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword, false)]
    public async Task SecurityAdmin_OnlyAdmin(string email, string password, bool allowed)
    {
        var client = await AuthenticatedClientAsync(email, password);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(
            allowed ? HttpStatusCode.OK : HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task PortalAccess_AllowsALinkedCustomerUser()
    {
        // PortalCustomerEmail is seeded with a real linked CustomerId (unlike
        // CustomerRoleEmail, which is deliberately mis-provisioned — see
        // CustomWebApplicationFactory) so this exercises the policy allowing
        // the request through, not a downstream 403 from the missing link.
        _factory.SeedPortalCustomers();
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.PortalCustomerEmail, CustomWebApplicationFactory.PortalCustomerPassword);

        var response = await client.GetAsync("/api/customer/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword)]
    [InlineData(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword)]
    public async Task PortalAccess_DeniesStaffRoles(string email, string password)
    {
        var client = await AuthenticatedClientAsync(email, password);

        var response = await client.GetAsync("/api/customer/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
