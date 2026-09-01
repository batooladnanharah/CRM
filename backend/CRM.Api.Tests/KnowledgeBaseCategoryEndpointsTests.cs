using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.KnowledgeBase;
using CRM.Api.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class KnowledgeBaseCategoryEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public KnowledgeBaseCategoryEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(
        string email = CustomWebApplicationFactory.AdminEmail,
        string password = CustomWebApplicationFactory.AdminPassword)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private static object CategoryPayload(string name, string? description = null, bool? isActive = null)
        => new { name, description, isActive };

    private async Task<KnowledgeBaseCategoryResponse> CreateCategoryAsync(HttpClient adminClient, object payload)
    {
        var response = await adminClient.PostAsJsonAsync("/api/knowledge-base/categories", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KnowledgeBaseCategoryResponse>())!;
    }

    [Fact]
    public async Task Create_ReturnsCreated_ForAdmin()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/categories", CategoryPayload("Billing"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseCategoryResponse>();
        Assert.Equal("Billing", body!.Name);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task Create_WritesAuditLogEntry()
    {
        var admin = await AuthenticatedClientAsync();

        var created = await CreateCategoryAsync(admin, CategoryPayload("Audited Category"));

        using var scope = _factory.Services.CreateScope();
        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var entry = authDb.AuditLogs.Single(a => a.TargetId == created.Id.ToString()
            && a.Action == AuditActions.KnowledgeBaseCategoryCreated);
        Assert.Equal("knowledgeBaseCategory", entry.TargetType);
    }

    [Fact]
    public async Task SetStatus_Deactivate_WritesRemovedAuditLogEntry()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateCategoryAsync(admin, CategoryPayload("Audited Deactivate Category"));

        var response = await admin.PatchAsync(
            $"/api/knowledge-base/categories/{created.Id}/status", JsonContent.Create(new { isActive = false }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var entry = authDb.AuditLogs.Single(a => a.TargetId == created.Id.ToString()
            && a.Action == AuditActions.KnowledgeBaseCategoryRemoved);
        Assert.Equal("knowledgeBaseCategory", entry.TargetType);
    }

    [Fact]
    public async Task Create_Returns403_ForAgent()
    {
        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await agent.PostAsJsonAsync(
            "/api/knowledge-base/categories", CategoryPayload("Agent Cannot Create"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_ForAnonymous()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/knowledge-base/categories", CategoryPayload("Anon Category"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenNameDuplicatesDifferentCase()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateCategoryAsync(admin, CategoryPayload("Shipping"));

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/categories", CategoryPayload("SHIPPING"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenNameMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/categories", CategoryPayload(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenNameWhitespace()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/categories", CategoryPayload("   "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenNameTooLong()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/categories", CategoryPayload(new string('a', 121)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_UpdatesNameAndDescription_AndBumpsUpdatedAt()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateCategoryAsync(admin, CategoryPayload("Original Name", "Original description"));

        var response = await admin.PutAsJsonAsync(
            $"/api/knowledge-base/categories/{created.Id}",
            new { name = "Renamed", description = "Updated description" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseCategoryResponse>();
        Assert.Equal("Renamed", body!.Name);
        Assert.Equal("Updated description", body.Description);
        Assert.True(body.UpdatedAtUtc >= created.UpdatedAtUtc);
    }

    [Fact]
    public async Task Update_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PutAsJsonAsync(
            $"/api/knowledge-base/categories/{Guid.NewGuid()}", new { name = "Missing", description = (string?)null });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchStatus_TogglesIsActive()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateCategoryAsync(admin, CategoryPayload("Toggle Me"));
        Assert.True(created.IsActive);

        var deactivate = await admin.PatchAsync(
            $"/api/knowledge-base/categories/{created.Id}/status", JsonContent.Create(new { isActive = false }));
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        var deactivated = await deactivate.Content.ReadFromJsonAsync<KnowledgeBaseCategoryResponse>();
        Assert.False(deactivated!.IsActive);

        var reactivate = await admin.PatchAsync(
            $"/api/knowledge-base/categories/{created.Id}/status", JsonContent.Create(new { isActive = true }));
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
        var reactivated = await reactivate.Content.ReadFromJsonAsync<KnowledgeBaseCategoryResponse>();
        Assert.True(reactivated!.IsActive);
    }

    [Fact]
    public async Task List_ReturnsAll_ByDefault()
    {
        var admin = await AuthenticatedClientAsync();
        var active = await CreateCategoryAsync(admin, CategoryPayload($"List Active {Guid.NewGuid()}"));
        var created = await CreateCategoryAsync(admin, CategoryPayload($"List Inactive {Guid.NewGuid()}"));
        await admin.PatchAsync(
            $"/api/knowledge-base/categories/{created.Id}/status", JsonContent.Create(new { isActive = false }));

        var response = await admin.GetAsync("/api/knowledge-base/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseCategoryListEnvelope>();
        Assert.Contains(body!.Items, c => c.Id == active.Id);
        Assert.Contains(body.Items, c => c.Id == created.Id);
    }

    [Fact]
    public async Task List_ActiveOnly_FiltersOutInactive()
    {
        var admin = await AuthenticatedClientAsync();
        var active = await CreateCategoryAsync(admin, CategoryPayload($"ActiveOnly Active {Guid.NewGuid()}"));
        var inactive = await CreateCategoryAsync(admin, CategoryPayload($"ActiveOnly Inactive {Guid.NewGuid()}"));
        await admin.PatchAsync(
            $"/api/knowledge-base/categories/{inactive.Id}/status", JsonContent.Create(new { isActive = false }));

        var response = await admin.GetAsync("/api/knowledge-base/categories?activeOnly=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseCategoryListEnvelope>();
        Assert.Contains(body!.Items, c => c.Id == active.Id);
        Assert.DoesNotContain(body.Items, c => c.Id == inactive.Id);
    }
}

internal sealed record KnowledgeBaseCategoryListEnvelope(List<KnowledgeBaseCategoryResponse> Items);
