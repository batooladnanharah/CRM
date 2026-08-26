using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Sla;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class SlaPolicyEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SlaPolicyEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(
        string email = CustomWebApplicationFactory.ActiveEmail,
        string password = CustomWebApplicationFactory.ActivePassword)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private static object PolicyPayload(
        string name, string priority = "High", int firstResponseMinutes = 30, int resolutionMinutes = 240,
        bool isDefault = false, bool isActive = true, string? channel = null)
        => new { name, channel, priority, firstResponseMinutes, resolutionMinutes, isDefault, isActive };

    private async Task<SlaPolicyResponse> CreatePolicyAsync(HttpClient adminClient, object payload)
    {
        var response = await adminClient.PostAsJsonAsync("/api/sla/policies", payload);
        return (await response.Content.ReadFromJsonAsync<SlaPolicyResponse>())!;
    }

    [Fact]
    public async Task Get_Policies_Returns403_ForAgent()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/sla/policies");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_Policies_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync("/api/sla/policies");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Policies_ReturnsArray_ForAdmin()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.GetAsync("/api/sla/policies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<SlaPolicyResponse>>();
        Assert.NotNull(items);
    }

    [Fact]
    public async Task Post_Policy_CreatesAndReturns201_ForAdmin()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync(
            "/api/sla/policies", PolicyPayload("Unique Create Policy Name Xyzzy1"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SlaPolicyResponse>();
        Assert.Equal("Unique Create Policy Name Xyzzy1", body!.Name);
        Assert.Equal(30, body.FirstResponseMinutes);
        Assert.Equal(240, body.ResolutionMinutes);
        Assert.True(body.IsActive);
        Assert.False(body.IsDefault);
    }

    [Fact]
    public async Task Post_Policy_Returns403_ForAgent()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/sla/policies", PolicyPayload("Agent Cannot Create"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Policy_Returns400_WhenNameEmpty()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync("/api/sla/policies", PolicyPayload(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Policy_Returns400_WhenPriorityUnknown()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync(
            "/api/sla/policies", PolicyPayload("Bad Priority Policy", priority: "critical"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Policy_Returns400_WhenFirstResponseMinutesNotPositive()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PostAsJsonAsync(
            "/api/sla/policies", PolicyPayload("Bad Minutes Policy", firstResponseMinutes: 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Policy_SettingDefault_ClearsPreviousDefault()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var first = await CreatePolicyAsync(
            admin, PolicyPayload("First Default Policy Xyzzy2", isDefault: true));
        Assert.True(first.IsDefault);

        var second = await CreatePolicyAsync(
            admin, PolicyPayload("Second Default Policy Xyzzy2", isDefault: true));
        Assert.True(second.IsDefault);

        var refetched = await admin.GetAsync($"/api/sla/policies/{first.Id}");
        var refetchedBody = await refetched.Content.ReadFromJsonAsync<SlaPolicyResponse>();
        Assert.False(refetchedBody!.IsDefault);
    }

    [Fact]
    public async Task Put_Policy_UpdatesFields_ForAdmin()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var created = await CreatePolicyAsync(admin, PolicyPayload("Original Policy Name"));

        var response = await admin.PutAsJsonAsync(
            $"/api/sla/policies/{created.Id}",
            PolicyPayload("Updated Policy Name", priority: "Urgent", firstResponseMinutes: 15, resolutionMinutes: 60));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<SlaPolicyResponse>();
        Assert.Equal("Updated Policy Name", body!.Name);
        Assert.Equal(15, body.FirstResponseMinutes);
        Assert.Equal(60, body.ResolutionMinutes);
    }

    [Fact]
    public async Task Put_Policy_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PutAsJsonAsync(
            $"/api/sla/policies/{Guid.NewGuid()}", PolicyPayload("Missing Policy"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Policy_SettingDefault_ClearsOtherDefault()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var existingDefault = await CreatePolicyAsync(
            admin, PolicyPayload("Existing Default Xyzzy3", isDefault: true));
        var other = await CreatePolicyAsync(admin, PolicyPayload("Other Policy Xyzzy3"));

        var response = await admin.PutAsJsonAsync(
            $"/api/sla/policies/{other.Id}", PolicyPayload("Other Policy Xyzzy3", isDefault: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refetched = await admin.GetAsync($"/api/sla/policies/{existingDefault.Id}");
        var refetchedBody = await refetched.Content.ReadFromJsonAsync<SlaPolicyResponse>();
        Assert.False(refetchedBody!.IsDefault);
    }

    [Fact]
    public async Task Delete_Policy_SoftDeletes_ForAdmin()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var created = await CreatePolicyAsync(admin, PolicyPayload("Delete Me Policy"));

        var deleteResponse = await admin.DeleteAsync($"/api/sla/policies/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var refetched = await admin.GetAsync($"/api/sla/policies/{created.Id}");
        var refetchedBody = await refetched.Content.ReadFromJsonAsync<SlaPolicyResponse>();
        Assert.NotNull(refetchedBody);
        Assert.False(refetchedBody!.IsActive);
    }

    [Fact]
    public async Task Delete_Policy_Returns403_ForAgent()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var created = await CreatePolicyAsync(admin, PolicyPayload("Agent Cannot Delete Policy"));

        var agent = await AuthenticatedClientAsync();
        var response = await agent.DeleteAsync($"/api/sla/policies/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Policy_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.DeleteAsync($"/api/sla/policies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
