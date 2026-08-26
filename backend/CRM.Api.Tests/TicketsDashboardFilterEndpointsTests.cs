using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Covers the assigneeId/updatedSince query filters added for the agent
// dashboard (CRM-97). Each test class below gets its own factory instance (a
// fresh InMemory database) because these assertions rely on exact result
// counts with no other filter narrow enough to isolate them from tickets
// seeded by other test methods sharing the same fixture.

file static class DashboardFilterHelpers
{
    public static async Task<HttpClient> AuthenticatedClientAsync(
        CustomWebApplicationFactory factory, HttpClient anonymousClient,
        string email = CustomWebApplicationFactory.ActiveEmail,
        string password = CustomWebApplicationFactory.ActivePassword)
    {
        var login = await anonymousClient.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    public static Guid CreateCustomer(CustomWebApplicationFactory factory, string fullName, string email)
    {
        using var scope = factory.Services.CreateScope();
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

    public static Guid CreateTicket(
        CustomWebApplicationFactory factory, Guid customerId, string title, Guid? assigneeUserId,
        TicketStatus status, DateTime updatedAtUtc)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = "Seeded for dashboard filter tests.",
            Status = status,
            Priority = TicketPriority.Normal,
            AssigneeUserId = assigneeUserId,
            CreatedAtUtc = updatedAtUtc,
            UpdatedAtUtc = updatedAtUtc,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    public static Guid UserIdByEmail(CustomWebApplicationFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        return db.Users.Single(u => u.Email == email).Id;
    }
}

public class TicketsAssigneeIdFilterEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsAssigneeIdFilterEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_AssigneeIdFilter_ReturnsOnlyThatAgentsTickets()
    {
        var client = await DashboardFilterHelpers.AuthenticatedClientAsync(_factory, _client);
        var customerId = DashboardFilterHelpers.CreateCustomer(
            _factory, "Assignee Filter Customer", "assignee.filter.customer@example.com");
        var agentId = DashboardFilterHelpers.UserIdByEmail(_factory, CustomWebApplicationFactory.ActiveEmail);
        var otherAgentId = DashboardFilterHelpers.UserIdByEmail(_factory, CustomWebApplicationFactory.SecondAgentEmail);
        var now = DateTime.UtcNow;

        DashboardFilterHelpers.CreateTicket(_factory, customerId, "Mine", agentId, TicketStatus.Open, now);
        DashboardFilterHelpers.CreateTicket(_factory, customerId, "Not mine", otherAgentId, TicketStatus.Open, now);
        DashboardFilterHelpers.CreateTicket(_factory, customerId, "Unassigned", null, TicketStatus.Open, now);

        var response = await client.GetAsync($"/api/tickets?assigneeId={agentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Single(result!.Items);
        Assert.Equal("Mine", result.Items[0].Title);
        Assert.Equal(agentId, result.Items[0].AssigneeUserId);
    }
}

public class TicketsUpdatedSinceFilterEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsUpdatedSinceFilterEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_UpdatedSinceFilter_ExcludesOlderTickets()
    {
        var client = await DashboardFilterHelpers.AuthenticatedClientAsync(_factory, _client);
        var customerId = DashboardFilterHelpers.CreateCustomer(
            _factory, "Updated Since Customer", "updated.since.customer@example.com");
        var now = DateTime.UtcNow;

        DashboardFilterHelpers.CreateTicket(_factory, customerId, "Recent", null, TicketStatus.Resolved, now);
        DashboardFilterHelpers.CreateTicket(_factory, customerId, "Old", null, TicketStatus.Resolved, now.AddDays(-10));

        var updatedSince = now.AddDays(-7).ToString("o");
        var response = await client.GetAsync($"/api/tickets?updatedSince={Uri.EscapeDataString(updatedSince)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Single(result!.Items);
        Assert.Equal("Recent", result.Items[0].Title);
    }
}

public class TicketsCombinedDashboardFilterEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsCombinedDashboardFilterEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_AssigneeIdAndUpdatedSince_CombineAsAndFilters()
    {
        var client = await DashboardFilterHelpers.AuthenticatedClientAsync(_factory, _client);
        var customerId = DashboardFilterHelpers.CreateCustomer(
            _factory, "Combined Filter Customer", "combined.filter.customer@example.com");
        var agentId = DashboardFilterHelpers.UserIdByEmail(_factory, CustomWebApplicationFactory.ActiveEmail);
        var now = DateTime.UtcNow;

        DashboardFilterHelpers.CreateTicket(_factory, customerId, "Mine recent resolved", agentId, TicketStatus.Resolved, now);
        DashboardFilterHelpers.CreateTicket(_factory, customerId, "Mine old resolved", agentId, TicketStatus.Resolved, now.AddDays(-10));
        DashboardFilterHelpers.CreateTicket(_factory, customerId, "Someone else recent resolved", null, TicketStatus.Resolved, now);

        var updatedSince = now.AddDays(-7).ToString("o");
        var response = await client.GetAsync(
            $"/api/tickets?assigneeId={agentId}&status=Resolved&updatedSince={Uri.EscapeDataString(updatedSince)}");

        var result = await response.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Single(result!.Items);
        Assert.Equal("Mine recent resolved", result.Items[0].Title);
    }
}
