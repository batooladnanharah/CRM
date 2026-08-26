using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class TicketsHistoryEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsHistoryEndpointsTests(CustomWebApplicationFactory factory)
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

    private Guid CreateAgentUser(string name, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            IsActive = true,
            Roles = [Roles.Agent],
        };
        user.PasswordHash = hasher.HashPassword(user, "Correct#Passw0rd!");
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    [Fact]
    public async Task Get_History_ReturnsEmpty_WhenNoChanges()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("No History Customer", "no.history.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.GetAsync($"/api/tickets/{ticketId}/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entries = await response.Content.ReadFromJsonAsync<List<TicketHistoryEntryResponse>>();
        Assert.Empty(entries!);
    }

    [Fact]
    public async Task Get_History_Returns404_WhenTicketMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_History_ReturnsThreeMixedChanges_NewestFirst_WithDisplayNames()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Mixed History Customer", "mixed.history.customer@example.com");
        var ticketId = CreateTicket(customerId);
        var agentId = await GetActiveAgentIdAsync();

        var assign = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/assignment", new { agentUserId = agentId });
        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);

        var status = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/status", new { status = "InProgress" });
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);

        var priority = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/priority", new { priority = "High" });
        Assert.Equal(HttpStatusCode.OK, priority.StatusCode);

        var response = await client.GetAsync($"/api/tickets/{ticketId}/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entries = await response.Content.ReadFromJsonAsync<List<TicketHistoryEntryResponse>>();
        Assert.Equal(3, entries!.Count);
        Assert.Equal(
            [TicketChangeType.Priority, TicketChangeType.Status, TicketChangeType.Assignment],
            entries.Select(e => e.ChangeType));
        Assert.All(entries, e => Assert.Equal("Active Agent", e.ChangedByDisplayName));
    }

    [Fact]
    public async Task Get_History_RoundTripsUnicodeDisplayName()
    {
        var agentId = CreateAgentUser("سارة أحمد", "sara.ahmed@example.com");
        var client = await AuthenticatedClientAsync(CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);
        var agentClient = await AuthenticatedClientAsync("sara.ahmed@example.com", "Correct#Passw0rd!");
        var customerId = CreateCustomer("Unicode History Customer", "unicode.history.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await agentClient.PutAsJsonAsync(
            $"/api/tickets/{ticketId}/status", new { status = "InProgress" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await client.GetAsync($"/api/tickets/{ticketId}/history");
        var entries = await history.Content.ReadFromJsonAsync<List<TicketHistoryEntryResponse>>();

        Assert.Single(entries!);
        Assert.Equal("سارة أحمد", entries![0].ChangedByDisplayName);
        Assert.Equal(agentId, entries[0].ChangedByUserId);
    }

    [Fact]
    public async Task Get_EligibleAgents_ReturnsOnlyActiveAgentRoleUsers()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/tickets/eligible-agents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var agents = await response.Content.ReadFromJsonAsync<List<EligibleAgentResponse>>();
        var names = agents!.Select(a => a.DisplayName).ToList();

        Assert.Contains("Active Agent", names);
        Assert.Contains("Second Agent", names);
        Assert.Contains("Admin Agent", names); // multi-role admin+agent user
        Assert.DoesNotContain("Inactive Agent", names); // IsActive == false
        Assert.DoesNotContain("Default Admin", names); // admin-only, no agent role
        Assert.DoesNotContain("Portal Customer", names); // customer role only
    }

    [Fact]
    public async Task Get_History_ReturnsNewEventKinds_AlongsideExistingOnes()
    {
        var client = await AuthenticatedClientAsync();
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var customerId = CreateCustomer("New Kinds Customer", "new.kinds.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var message = await client.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/messages", new { body = "Investigating now.", isInternal = true });
        Assert.Equal(HttpStatusCode.Created, message.StatusCode);

        var escalate = await admin.PostAsJsonAsync(
            $"/api/tickets/{ticketId}/escalate", new { reason = "Customer is a VIP account." });
        Assert.Equal(HttpStatusCode.OK, escalate.StatusCode);

        var response = await client.GetAsync($"/api/tickets/{ticketId}/history");
        var entries = await response.Content.ReadFromJsonAsync<List<TicketHistoryEntryResponse>>();

        Assert.Equal(2, entries!.Count);
        Assert.Equal(
            [TicketChangeType.Escalated, TicketChangeType.MessageAdded],
            entries.Select(e => e.ChangeType));
        Assert.Equal("Customer is a VIP account.", entries[0].Reason);
        Assert.Null(entries[1].Reason);
    }

    private async Task<Guid> GetActiveAgentIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == CustomWebApplicationFactory.ActiveEmail);
        return user.Id;
    }
}
