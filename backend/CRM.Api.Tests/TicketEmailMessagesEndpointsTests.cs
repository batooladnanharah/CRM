using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.CommunicationChannels;
using CRM.Api.Customers;
using CRM.Api.Email;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class TicketEmailMessagesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketEmailMessagesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
        FakeService.ShouldFail = false;
        FakeService.SentRequests.Clear();
    }

    private FakeEmailService FakeService =>
        (FakeEmailService)_factory.Services.GetRequiredService<IEmailService>();

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

    private Guid CreateCustomer(string fullName, string? email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email ?? string.Empty,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    private Guid CreateTicket(Guid customerId, string title = "Sample ticket")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
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

    private EmailMessageMetadata? MetadataForMessage(Guid messageId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CommunicationChannelsDbContext>();
        return db.EmailMessageMetadata.FirstOrDefault(m => m.TicketMessageId == messageId);
    }

    [Fact]
    public async Task Post_email_message_as_agent_returns_201_and_records_sent_metadata()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Email Customer", "customer@example.com");
        var ticketId = CreateTicket(customerId, "Cannot access account");

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Following up on your issue.",
            isInternal = false,
            channel = "Email",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<TicketMessageResponse>();
        Assert.Equal("Email", message!.Channel);
        Assert.Equal("Sent", message.EmailDeliveryStatus);

        var metadata = MetadataForMessage(message.Id);
        Assert.NotNull(metadata);
        Assert.Equal(EmailDeliveryStatus.Sent, metadata!.DeliveryStatus);
        Assert.Equal("customer@example.com", metadata.ToAddress);
        Assert.Equal("Re: Cannot access account", metadata.Subject);
    }

    [Fact]
    public async Task Post_email_message_as_unauthorized_user_returns_403()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.PostAsJsonAsync($"/api/tickets/{Guid.NewGuid()}/messages", new
        {
            body = "Hello",
            isInternal = false,
            channel = "Email",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_email_message_when_customer_has_no_email_returns_400()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("No Email Customer", null);
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Hello",
            isInternal = false,
            channel = "Email",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_email_message_with_empty_content_returns_400()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Empty Content Customer", "empty@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "   ",
            isInternal = false,
            channel = "Email",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_email_message_calls_email_service_exactly_once_with_derived_subject_and_recipient()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Derived Subject Customer", "derived@example.com");
        var ticketId = CreateTicket(customerId, "Password reset request");

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Here are the steps.",
            isInternal = false,
            channel = "Email",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(FakeService.SentRequests);
        var sent = FakeService.SentRequests[0];
        Assert.Equal("derived@example.com", sent.ToAddress);
        Assert.Equal("Re: Password reset request", sent.Subject);
    }

    [Fact]
    public async Task Post_email_message_creates_ticket_message_with_channel_Email()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Channel Customer", "channel@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Body",
            isInternal = false,
            channel = "Email",
        });

        var message = await response.Content.ReadFromJsonAsync<TicketMessageResponse>();
        Assert.Equal("Email", message!.Channel);
    }

    [Fact]
    public async Task Post_email_message_provider_failure_returns_502_and_metadata_status_Failed()
    {
        FakeService.ShouldFail = true;
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Failure Customer", "failure@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "This will fail to send.",
            isInternal = false,
            channel = "Email",
        });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var failure = await response.Content.ReadFromJsonAsync<EmailDeliveryFailureResponse>();
        Assert.NotEqual(Guid.Empty, failure!.MessageId);

        var metadata = MetadataForMessage(failure.MessageId);
        Assert.NotNull(metadata);
        Assert.Equal(EmailDeliveryStatus.Failed, metadata!.DeliveryStatus);
    }

    [Fact]
    public async Task Post_email_message_provider_failure_response_body_does_not_contain_smtp_password()
    {
        FakeService.ShouldFail = true;
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Secret Safe Customer", "secretsafe@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "This will fail to send.",
            isInternal = false,
            channel = "Email",
        });

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Smtp", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_email_message_success_appends_ticket_history_entry_EmailSent()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("History Customer", "history@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/messages", new
        {
            body = "Body",
            isInternal = false,
            channel = "Email",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, HistoryCount(ticketId, TicketChangeType.EmailSent));
    }

    [Fact]
    public async Task Post_message_with_no_channel_still_defaults_to_Web_and_does_not_call_email_service()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Regression Customer", "regression@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/messages", new { body = "Plain web reply", isInternal = false });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var message = await response.Content.ReadFromJsonAsync<TicketMessageResponse>();
        Assert.Equal("Web", message!.Channel);
        Assert.Null(message.EmailDeliveryStatus);
        Assert.Empty(FakeService.SentRequests);
    }
}
