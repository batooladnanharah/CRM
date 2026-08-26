using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.QuickReplies;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class QuickRepliesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public QuickRepliesEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<QuickReplyResponse> CreateQuickReplyAsync(HttpClient adminClient, string title, string content)
    {
        var response = await adminClient.PostAsJsonAsync("/api/quick-replies", new { title, content });
        return (await response.Content.ReadFromJsonAsync<QuickReplyResponse>())!;
    }

    [Fact]
    public async Task Get_QuickReplies_ReturnsArray_ForAgent()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/quick-replies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<QuickReplyResponse>>();
        Assert.NotNull(items);
    }

    [Fact]
    public async Task Post_QuickReply_Returns403_ForAgent()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/quick-replies", new { title = "Greeting", content = "Hello there!" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_QuickReply_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/quick-replies", new { title = "Greeting", content = "Hello there!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_QuickReply_CreatesAndReturns201_ForAdmin()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await client.PostAsJsonAsync(
            "/api/quick-replies", new { title = "Password Reset", content = "Here are the password reset steps..." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<QuickReplyResponse>();
        Assert.Equal("Password Reset", body!.Title);
        Assert.Equal("Here are the password reset steps...", body.Content);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task Post_QuickReply_Returns400_WhenTitleEmpty()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await client.PostAsJsonAsync("/api/quick-replies", new { title = "", content = "Content" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_QuickReply_Returns400_WhenContentEmpty()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await client.PostAsJsonAsync("/api/quick-replies", new { title = "Title", content = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_QuickReply_Returns400_WhenTitleTooLong()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await client.PostAsJsonAsync(
            "/api/quick-replies", new { title = new string('t', 121), content = "Content" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_QuickReply_Returns400_WhenContentTooLong()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await client.PostAsJsonAsync(
            "/api/quick-replies", new { title = "Title", content = new string('c', 4001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_QuickReply_UpdatesFields_ForAdmin()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var created = await CreateQuickReplyAsync(admin, "Original Title", "Original content");

        var response = await admin.PutAsJsonAsync(
            $"/api/quick-replies/{created.Id}", new { title = "Updated Title", content = "Updated content", isActive = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<QuickReplyResponse>();
        Assert.Equal("Updated Title", body!.Title);
        Assert.Equal("Updated content", body.Content);
        Assert.False(body.IsActive);
    }

    [Fact]
    public async Task Put_QuickReply_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.PutAsJsonAsync(
            $"/api/quick-replies/{Guid.NewGuid()}", new { title = "Title", content = "Content", isActive = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_QuickReply_Returns403_ForAgent()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var created = await CreateQuickReplyAsync(admin, "Agent Cannot Edit", "Content");

        var agent = await AuthenticatedClientAsync();
        var response = await agent.PutAsJsonAsync(
            $"/api/quick-replies/{created.Id}", new { title = "Hacked", content = "Content", isActive = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_QuickReply_RemovesIt_ForAdmin_AndExcludesFromSubsequentList()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var created = await CreateQuickReplyAsync(admin, "Delete Me Unique Title Xyzzy", "Content");

        var deleteResponse = await admin.DeleteAsync($"/api/quick-replies/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await admin.GetAsync("/api/quick-replies?search=Xyzzy");
        var items = await listResponse.Content.ReadFromJsonAsync<List<QuickReplyResponse>>();
        Assert.DoesNotContain(items!, q => q.Id == created.Id);
    }

    [Fact]
    public async Task Delete_QuickReply_Returns403_ForAgent()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var created = await CreateQuickReplyAsync(admin, "Agent Cannot Delete", "Content");

        var agent = await AuthenticatedClientAsync();
        var response = await agent.DeleteAsync($"/api/quick-replies/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_QuickReply_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await admin.DeleteAsync($"/api/quick-replies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_QuickReplies_SearchFiltersByTitleOrContent()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        await CreateQuickReplyAsync(admin, "Unique Password Reset Reply", "Steps to reset your password.");
        await CreateQuickReplyAsync(admin, "Unrelated Reply", "Nothing to do with the search term.");

        var response = await admin.GetAsync("/api/quick-replies?search=password");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<QuickReplyResponse>>();
        Assert.Contains(items!, q => q.Title == "Unique Password Reset Reply");
        Assert.DoesNotContain(items!, q => q.Title == "Unrelated Reply");
    }
}
