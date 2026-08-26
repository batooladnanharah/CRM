using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.CommunicationChannels;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class CommunicationChannelsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommunicationChannelsEndpointsTests(CustomWebApplicationFactory factory)
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

    private async Task<ChannelResponse> CreateChannelAsync(HttpClient adminClient, string name, string type = "Email")
    {
        var response = await adminClient.PostAsJsonAsync("/api/channels", new { name, type });
        return (await response.Content.ReadFromJsonAsync<ChannelResponse>())!;
    }

    [Fact]
    public async Task Post_Channel_Returns201_And_Persists()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync("/api/channels", new { name = "Support Inbox", type = "Email" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChannelResponse>();
        Assert.Equal("Support Inbox", body!.Name);
        Assert.Equal(ChannelType.Email, body.Type);
        Assert.True(body.IsEnabled);

        var getResponse = await admin.GetAsync($"/api/channels/{body.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Post_Channel_DuplicateName_Returns409()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateChannelAsync(admin, "Duplicate Inbox");

        var response = await admin.PostAsJsonAsync(
            "/api/channels", new { name = "Duplicate Inbox", type = "Email" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_Channel_InvalidType_Returns400()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/channels", new { name = "Bad Type Inbox", type = "WhatsApp" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Channel_Returns400_WhenNameEmpty()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync("/api/channels", new { name = "", type = "Email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Channel_Returns403_ForAgent()
    {
        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await agent.PostAsJsonAsync("/api/channels", new { name = "Agent Inbox", type = "Email" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_Channels_ReturnsSeeded()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateChannelAsync(admin, "List Test Inbox Unique Xyzzy");

        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);
        var response = await agent.GetAsync("/api/channels");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<ChannelResponse>>();
        Assert.Contains(items!, c => c.Name == "List Test Inbox Unique Xyzzy");
    }

    [Fact]
    public async Task Get_Channels_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync("/api/channels");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_Channel_Updates_Name_And_IsEnabled()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateChannelAsync(admin, "Original Inbox Name");

        var response = await admin.PutAsJsonAsync(
            $"/api/channels/{created.Id}", new { name = "Renamed Inbox", isEnabled = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ChannelResponse>();
        Assert.Equal("Renamed Inbox", body!.Name);
        Assert.False(body.IsEnabled);
    }

    [Fact]
    public async Task Put_Channel_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PutAsJsonAsync(
            $"/api/channels/{Guid.NewGuid()}", new { name = "Name", isEnabled = true });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Channel_Returns403_ForAgent()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateChannelAsync(admin, "Agent Cannot Edit Inbox");

        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);
        var response = await agent.PutAsJsonAsync(
            $"/api/channels/{created.Id}", new { name = "Hacked", isEnabled = true });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Channel_WithEmails_Returns409()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateChannelAsync(admin, "Channel With Emails");
        var ingest = await admin.PostAsJsonAsync($"/api/channels/{created.Id}/emails/ingest", new
        {
            fromAddress = "customer@example.com",
            toAddress = "support@example.com",
            subject = "Help",
            body = "I need help.",
        });
        Assert.Equal(HttpStatusCode.Created, ingest.StatusCode);

        var response = await admin.DeleteAsync($"/api/channels/{created.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Channel_WithoutEmails_Returns204()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateChannelAsync(admin, "Empty Channel To Delete");

        var response = await admin.DeleteAsync($"/api/channels/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await admin.GetAsync($"/api/channels/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Channel_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.DeleteAsync($"/api/channels/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_Email_Persists_And_DefaultsReceivedAt()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateChannelAsync(admin, "Ingest Default Received Inbox");

        var response = await admin.PostAsJsonAsync($"/api/channels/{created.Id}/emails/ingest", new
        {
            fromAddress = "customer@example.com",
            toAddress = "support@example.com",
            subject = "Password reset",
            body = "Please reset my password.",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EmailMessageResponse>();
        Assert.Equal(created.Id, body!.ChannelId);
        Assert.Equal("customer@example.com", body.FromAddress);
        Assert.True((DateTime.UtcNow - body.ReceivedAtUtc) < TimeSpan.FromMinutes(1));
        Assert.Null(body.TicketId);
    }

    [Fact]
    public async Task Ingest_Email_UsesProvidedReceivedAt_AndTicketId()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateChannelAsync(admin, "Ingest Explicit Received Inbox");
        var ticketId = Guid.NewGuid();
        var receivedAt = new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc);

        var response = await admin.PostAsJsonAsync($"/api/channels/{created.Id}/emails/ingest", new
        {
            fromAddress = "customer@example.com",
            toAddress = "support@example.com",
            subject = "Follow-up",
            body = "Any update?",
            receivedAtUtc = receivedAt,
            ticketId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<EmailMessageResponse>();
        Assert.Equal(receivedAt, body!.ReceivedAtUtc);
        Assert.Equal(ticketId, body.TicketId);
    }

    [Fact]
    public async Task Ingest_Email_Returns404_WhenChannelMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync($"/api/channels/{Guid.NewGuid()}/emails/ingest", new
        {
            fromAddress = "customer@example.com",
            toAddress = "support@example.com",
            subject = "Help",
            body = "Help please.",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Emails_OrdersByReceivedAtDesc_And_LimitedTo100()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateChannelAsync(admin, "Ordering Inbox");
        var baseTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 3; i++)
        {
            var ingest = await admin.PostAsJsonAsync($"/api/channels/{created.Id}/emails/ingest", new
            {
                fromAddress = "customer@example.com",
                toAddress = "support@example.com",
                subject = $"Message {i}",
                body = "Body",
                receivedAtUtc = baseTime.AddHours(i),
            });
            Assert.Equal(HttpStatusCode.Created, ingest.StatusCode);
        }

        var response = await admin.GetAsync($"/api/channels/{created.Id}/emails");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<EmailMessageResponse>>();
        Assert.Equal(3, items!.Count);
        Assert.Equal(["Message 2", "Message 1", "Message 0"], items.Select(m => m.Subject));
    }

    [Fact]
    public async Task Get_Emails_Returns404_WhenChannelMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.GetAsync($"/api/channels/{Guid.NewGuid()}/emails");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unauthenticated_Requests_Return401()
    {
        var listResponse = await _client.GetAsync("/api/channels");
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);

        var postResponse = await _client.PostAsJsonAsync("/api/channels", new { name = "X", type = "Email" });
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);

        var getOneResponse = await _client.GetAsync($"/api/channels/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, getOneResponse.StatusCode);

        var putResponse = await _client.PutAsJsonAsync(
            $"/api/channels/{Guid.NewGuid()}", new { name = "X", isEnabled = true });
        Assert.Equal(HttpStatusCode.Unauthorized, putResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/channels/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, deleteResponse.StatusCode);

        var emailsResponse = await _client.GetAsync($"/api/channels/{Guid.NewGuid()}/emails");
        Assert.Equal(HttpStatusCode.Unauthorized, emailsResponse.StatusCode);

        var ingestResponse = await _client.PostAsJsonAsync(
            $"/api/channels/{Guid.NewGuid()}/emails/ingest",
            new { fromAddress = "a@example.com", toAddress = "b@example.com", subject = "S", body = "B" });
        Assert.Equal(HttpStatusCode.Unauthorized, ingestResponse.StatusCode);
    }
}
