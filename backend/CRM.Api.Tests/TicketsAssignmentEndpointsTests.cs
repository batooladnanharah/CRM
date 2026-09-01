using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class TicketsAssignmentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsAssignmentEndpointsTests(CustomWebApplicationFactory factory)
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

    private Guid CreateTicket(Guid customerId, Guid? assigneeUserId = null)
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
            AssigneeUserId = assigneeUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    private int HistoryCount(Guid ticketId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.TicketHistory.Count(h => h.TicketId == ticketId);
    }

    private Guid UserIdByEmail(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.Users.Single(u => u.Email == email).Id;
    }

    [Fact]
    public async Task Put_Assignment_ToEligibleAgent_ReturnsOk_UpdatesTicket_AndCreatesHistoryRow()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Assign Customer", "assign.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var agentId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);

        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/assignment", new { agentUserId = agentId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(agentId, ticket!.AssigneeUserId);
        Assert.Equal("Second Agent", ticket.AssigneeDisplayName);
        Assert.Equal(1, HistoryCount(ticketId));
    }

    [Fact]
    public async Task Put_Assignment_ToNonAgent_Returns400()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Bad Agent Customer", "bad.agent.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var customerRoleUserId = UserIdByEmail(CustomWebApplicationFactory.CustomerRoleEmail);

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticketId}/assignment", new { agentUserId = customerRoleUserId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("invalid_agent", body!.Message);
        Assert.Equal(0, HistoryCount(ticketId));
    }

    [Fact]
    public async Task Put_Assignment_WithNull_Unassigns_AndRecordsNullNewValue()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Unassign Customer", "unassign.customer@example.com");
        var agentId = UserIdByEmail(CustomWebApplicationFactory.ActiveEmail);
        var ticketId = CreateTicket(customerId, agentId);

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticketId}/assignment", new { agentUserId = (Guid?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Null(ticket!.AssigneeUserId);
        Assert.Equal(1, HistoryCount(ticketId));
    }

    [Fact]
    public async Task Put_Assignment_ToSameAgent_ReturnsOk_WithoutNewHistoryRow()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Reassign Customer", "reassign.customer@example.com");
        var agentId = UserIdByEmail(CustomWebApplicationFactory.ActiveEmail);
        var ticketId = CreateTicket(customerId, agentId);

        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/assignment", new { agentUserId = agentId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, HistoryCount(ticketId));
    }

    [Fact]
    public async Task Put_Assignment_Returns404_WhenTicketMissing()
    {
        var client = await AuthenticatedClientAsync();
        var agentId = UserIdByEmail(CustomWebApplicationFactory.ActiveEmail);

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/assignment", new { agentUserId = agentId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Assignment_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/assignment", new { agentUserId = (Guid?)null });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_Assignment_Returns403_ForCustomerRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/assignment", new { agentUserId = (Guid?)null });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // CRM-62 regression: automatic assignment (TicketCreationService) must not
    // break the pre-existing TKT-005 manual reassignment endpoint — a ticket
    // that was auto-assigned at creation can still be manually reassigned to
    // a different eligible agent afterwards.
    [Fact]
    public async Task Put_Assignment_StillWorks_AfterAutoAssignmentAtCreation()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Auto Then Manual Co", "auto.then.manual@example.com");

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId,
            title = "Auto then manual",
            description = "Auto-assigned first, then manually reassigned.",
            priority = "Normal",
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>();

        var newAgentId = UserIdByEmail(CustomWebApplicationFactory.SecondAgentEmail);
        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{created!.Id}/assignment", new { agentUserId = newAgentId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(newAgentId, ticket!.AssigneeUserId);
    }
}
