using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class TicketMessagesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketMessagesEndpointsTests(CustomWebApplicationFactory factory)
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

    private Guid CreateTicket(Guid customerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Sample ticket",
            Description = "Sample description",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    private int HistoryCount(Guid ticketId, TicketChangeType changeType)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.TicketHistory.Count(h => h.TicketId == ticketId && h.ChangeType == changeType);
    }

    private Guid UserIdByEmail(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.Users.Single(u => u.Email == email).Id;
    }

    [Fact]
    public async Task Post_Message_CreatesMessage_ReturnsCreated_AndWritesHistory()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Message Customer", "message.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/messages", new { body = "Looking into this now.", isInternal = true });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<TicketMessageResponse>();
        Assert.Equal(ticketId, message!.TicketId);
        Assert.Equal("Looking into this now.", message.Body);
        Assert.True(message.IsInternal);
        Assert.Equal("Active Agent", message.AuthorDisplayName);
        Assert.Equal(1, HistoryCount(ticketId, TicketChangeType.MessageAdded));
    }

    [Fact]
    public async Task Get_Messages_ReturnsNewestFirst_AndPersistsInternalFlag()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("List Customer", "list.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var first = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/messages", new { body = "First message", isInternal = false });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var second = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/messages", new { body = "Second message", isInternal = true });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var response = await client.GetAsync($"/api/tickets/{ticketId}/messages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<TicketMessageResponse>>();
        Assert.Equal(2, page!.TotalCount);
        Assert.Equal(["Second message", "First message"], page.Items.Select(m => m.Body));
        Assert.False(page.Items[1].IsInternal);
        Assert.True(page.Items[0].IsInternal);
    }

    [Fact]
    public async Task Get_Messages_Returns404_WhenTicketMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}/messages");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Message_Returns400_WhenBodyEmpty()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Empty Body Customer", "empty.body.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/messages", new { body = "   ", isInternal = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Message_Returns400_WhenBodyTooLong()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Too Long Customer", "too.long.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/messages", new { body = new string('x', 5001), isInternal = false });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Message_Returns404_WhenTicketMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/messages", new { body = "Hello", isInternal = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Message_Returns400_WhenChannelUnrecognized()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Bad Channel Customer", "bad.channel.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/messages",
            new { body = "Hello", isInternal = false, channel = "WhatsApp" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Messages_Returns403_ForCustomerRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}/messages");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Message_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/messages", new { body = "Hello", isInternal = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Message_Returns403_ForCustomerRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/messages", new { body = "Hello", isInternal = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_InternalNote_WithValidMentions_PersistsAndReturnsThem()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Mention Customer", "mention.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var secondAgentId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);
        var adminId = UserIdByEmail(CustomWebApplicationFactory.AdminEmail);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Please take a look @agent @admin",
            isInternal = true,
            mentionedUserIds = new[] { secondAgentId, adminId },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<TicketMessageResponse>();
        Assert.Equal(2, message!.MentionedUserIds.Count);
        Assert.Contains(secondAgentId, message.MentionedUserIds);
        Assert.Contains(adminId, message.MentionedUserIds);
    }

    [Fact]
    public async Task Post_InternalNote_MentioningNonExistentUser_Returns400()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Mention Missing Customer", "mention.missing.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Mentioning nobody",
            isInternal = true,
            mentionedUserIds = new[] { Guid.NewGuid() },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InternalNote_MentioningInactiveUser_Returns400()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Mention Inactive Customer", "mention.inactive.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var inactiveId = UserIdByEmail(CustomWebApplicationFactory.InactiveEmail);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Mentioning inactive agent",
            isInternal = true,
            mentionedUserIds = new[] { inactiveId },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InternalNote_MentioningCustomerRoleUser_Returns400()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Mention Customer Role Customer", "mention.customer.role@example.com");
        var ticketId = CreateTicket(customerId);
        var customerRoleId = UserIdByEmail(CustomWebApplicationFactory.CustomerRoleEmail);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Mentioning a customer-role user",
            isInternal = true,
            mentionedUserIds = new[] { customerRoleId },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_InternalNote_DuplicateMention_DedupedToSingleRow()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Mention Dedupe Customer", "mention.dedupe.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var secondAgentId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Duplicate mention",
            isInternal = true,
            mentionedUserIds = new[] { secondAgentId, secondAgentId },
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<TicketMessageResponse>();
        Assert.Single(message!.MentionedUserIds);
    }

    [Fact]
    public async Task Post_Reply_WithMentions_Returns400()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Mention On Reply Customer", "mention.on.reply.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var secondAgentId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "This is a public reply",
            isInternal = false,
            mentionedUserIds = new[] { secondAgentId },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
