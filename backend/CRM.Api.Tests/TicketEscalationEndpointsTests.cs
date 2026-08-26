using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class TicketEscalationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketEscalationEndpointsTests(CustomWebApplicationFactory factory)
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

    private Guid CreateTicket(Guid customerId, TicketStatus status, TicketPriority priority)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Sample ticket",
            Description = "Sample description",
            Status = status,
            Priority = priority,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    [Fact]
    public async Task Post_Escalate_BumpsPriorityByOneStep_AndWritesHistoryWithReason()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Escalate Customer", "escalate.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Open, TicketPriority.Normal);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/escalate", new { reason = "Customer is a VIP account." });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(TicketPriority.High, ticket!.Priority);

        var history = await client.GetAsync($"/api/tickets/{ticketId}/history");
        var entries = await history.Content.ReadFromJsonAsync<List<TicketHistoryEntryResponse>>();
        Assert.Single(entries!);
        Assert.Equal(TicketChangeType.Escalated, entries![0].ChangeType);
        Assert.Equal("Normal", entries[0].OldValue);
        Assert.Equal("High", entries[0].NewValue);
        Assert.Equal("Customer is a VIP account.", entries[0].Reason);
    }

    [Fact]
    public async Task Post_Escalate_Returns400_WhenTicketClosed()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Closed Customer", "closed.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Closed, TicketPriority.Normal);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/escalate", new { reason = "Too late." });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Escalate_Returns400_WhenTicketResolved()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Resolved Customer", "resolved.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Resolved, TicketPriority.Normal);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/escalate", new { reason = "Reopen first." });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Escalate_Returns400_WhenAlreadyAtMaxPriority()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Max Priority Customer", "max.priority.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Open, TicketPriority.Urgent);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/escalate", new { reason = "Already urgent." });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Escalate_Returns400_WhenReasonMissing()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("No Reason Customer", "no.reason.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Open, TicketPriority.Normal);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/escalate", new { reason = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Escalate_Returns404_WhenTicketMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/escalate", new { reason = "Reason" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Escalate_Returns403_ForAgentRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);
        var customerId = CreateCustomer("Agent Denied Customer", "agent.denied.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Open, TicketPriority.Normal);

        var response = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/escalate", new { reason = "Trying anyway." });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_Escalate_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/escalate", new { reason = "Reason" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Escalate_AfterAutoEscalation_StillSucceeds_AndBothEntriesAppearInOrder()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Auto Then Manual Customer", "auto.then.manual.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Open, TicketPriority.Normal);
        SetBreachedDueDates(ticketId);

        var evaluateNow = await client.PostAsync("/api/sla/evaluate-now", content: null);
        Assert.Equal(HttpStatusCode.OK, evaluateNow.StatusCode);

        var manualEscalate = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/escalate", new { reason = "Escalating further, manually." });
        Assert.Equal(HttpStatusCode.OK, manualEscalate.StatusCode);
        var ticket = await manualEscalate.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(TicketPriority.Urgent, ticket!.Priority);

        var history = await client.GetAsync($"/api/tickets/{ticketId}/history");
        var entries = await history.Content.ReadFromJsonAsync<List<TicketHistoryEntryResponse>>();
        var escalations = entries!.Where(e => e.ChangeType == TicketChangeType.Escalated).ToList();

        Assert.Equal(2, escalations.Count);
        // Newest first: the manual escalation (just performed) precedes the
        // earlier auto-escalation from evaluate-now.
        Assert.False(escalations[0].IsSystemActor);
        Assert.Equal("Escalating further, manually.", escalations[0].Reason);
        Assert.True(escalations[1].IsSystemActor);
    }

    private void SetBreachedDueDates(Guid ticketId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = db.Tickets.Single(t => t.Id == ticketId);
        var created = DateTime.UtcNow.AddHours(-2);
        ticket.CreatedAtUtc = created;
        ticket.FirstResponseDueAtUtc = created.AddHours(1);
        ticket.ResolutionDueAtUtc = created.AddHours(8);
        db.SaveChanges();
    }
}
