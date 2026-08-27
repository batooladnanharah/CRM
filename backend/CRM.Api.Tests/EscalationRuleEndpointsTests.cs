using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Sla;

namespace CRM.Api.Tests;

public class EscalationRuleEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EscalationRuleEndpointsTests(CustomWebApplicationFactory factory)
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

    private static object RulePayload(
        string name, string trigger = "AtRisk", bool notifyAgent = true, bool notifyManager = false, bool isActive = true)
        => new { name, trigger, notifyAgent, notifyManager, isActive };

    private async Task<HttpClient> AdminClientAsync() =>
        await AuthenticatedClientAsync(CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

    [Fact]
    public async Task Get_Rules_Returns403_ForAgent()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/sla/escalation-rules");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_Rules_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync("/api/sla/escalation-rules");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Rule_CreatesAndReturns201_ForAdmin()
    {
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/sla/escalation-rules", RulePayload("Unique Rule Alpha"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EscalationRuleDto>();
        Assert.Equal("Unique Rule Alpha", body!.Name);
        Assert.Equal(EscalationTrigger.AtRisk, body.Trigger);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task Post_Rule_Returns403_ForAgent()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/sla/escalation-rules", RulePayload("Agent Cannot Create"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Rule_Returns409_ForDuplicateName()
    {
        var admin = await AdminClientAsync();
        await admin.PostAsJsonAsync("/api/sla/escalation-rules", RulePayload("Duplicate Name Rule"));

        var response = await admin.PostAsJsonAsync(
            "/api/sla/escalation-rules", RulePayload("duplicate name rule"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_Rule_Returns400_ForInvalidTrigger()
    {
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/sla/escalation-rules", RulePayload("Bad Trigger Rule", trigger: "Whenever"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Activate_And_Deactivate_ToggleIsActive()
    {
        var admin = await AdminClientAsync();
        var created = await CreateAsync(admin, RulePayload("Toggle Rule", isActive: false));

        var activate = await admin.PatchAsync($"/api/sla/escalation-rules/{created.Id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);
        var activated = await activate.Content.ReadFromJsonAsync<EscalationRuleDto>();
        Assert.True(activated!.IsActive);

        var deactivate = await admin.PatchAsync($"/api/sla/escalation-rules/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        var deactivated = await deactivate.Content.ReadFromJsonAsync<EscalationRuleDto>();
        Assert.False(deactivated!.IsActive);
    }

    [Fact]
    public async Task Delete_Rule_Returns204_ForAdmin()
    {
        var admin = await AdminClientAsync();
        var created = await CreateAsync(admin, RulePayload("Delete Me Rule"));

        var response = await admin.DeleteAsync($"/api/sla/escalation-rules/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var refetched = await admin.GetAsync($"/api/sla/escalation-rules/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, refetched.StatusCode);
    }

    private static async Task<EscalationRuleDto> CreateAsync(HttpClient adminClient, object payload)
    {
        var response = await adminClient.PostAsJsonAsync("/api/sla/escalation-rules", payload);
        return (await response.Content.ReadFromJsonAsync<EscalationRuleDto>())!;
    }
}
