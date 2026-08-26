using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.CustomerPortal;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class CustomerPortalEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _portalCustomerId;
    private readonly Guid _otherCustomerId;

    public CustomerPortalEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        (_portalCustomerId, _otherCustomerId) = factory.SeedPortalCustomers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(
        string email = CustomWebApplicationFactory.PortalCustomerEmail,
        string password = CustomWebApplicationFactory.PortalCustomerPassword)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateTicketForCustomer(
        Guid customerId, string title = "Sample ticket", TicketStatus status = TicketStatus.Open)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = "Sample description",
            Status = status,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    private void AddMessage(Guid ticketId, string body, bool isInternal)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.TicketMessages.Add(new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorUserId = Guid.NewGuid(),
            Body = body,
            IsInternal = isInternal,
            CreatedAtUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    private void AddHistoryEntry(Guid ticketId, TicketChangeType changeType, string? oldValue, string? newValue)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.TicketHistory.Add(new TicketHistoryEntry
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ChangeType = changeType,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedByUserId = Guid.NewGuid(),
            ChangedAtUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_Tickets_ReturnsOnlyOwnTickets()
    {
        var client = await AuthenticatedClientAsync();
        CreateTicketForCustomer(_portalCustomerId, "My Ticket");
        CreateTicketForCustomer(_otherCustomerId, "Not My Ticket");

        var response = await client.GetAsync("/api/customer/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<CustomerTicketListItemResponse>>();
        Assert.Contains(items!, t => t.Title == "My Ticket");
        Assert.DoesNotContain(items!, t => t.Title == "Not My Ticket");
    }

    [Fact]
    public async Task Get_TicketById_ReturnsOwnTicket()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Details Ticket");

        var response = await client.GetAsync($"/api/customer/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();
        Assert.Equal("Details Ticket", body!.Title);
    }

    [Fact]
    public async Task Get_TicketById_Returns404_ForAnotherCustomersTicket()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_otherCustomerId, "Not Mine");

        var response = await client.GetAsync($"/api/customer/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_TicketById_Returns404_ForNonExistentTicket()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/customer/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Tickets_IgnoresBodyCustomerId_AndUsesAuthenticatedIdentity()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/customer/tickets", new
        {
            title = "New Portal Ticket",
            description = "Something is broken.",
            customerId = _otherCustomerId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();

        var ownList = await client.GetAsync("/api/customer/tickets");
        var ownItems = await ownList.Content.ReadFromJsonAsync<List<CustomerTicketListItemResponse>>();
        Assert.Contains(ownItems!, t => t.Id == body!.Id);
    }

    [Fact]
    public async Task Post_Tickets_PersistsTicketWithTitleAndDescription()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/customer/tickets", new
        {
            title = "Persisted Ticket",
            description = "Full description here.",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();
        Assert.Equal("Persisted Ticket", body!.Title);
        Assert.Equal("Full description here.", body.Description);
        Assert.Equal(TicketStatus.Open, body.Status);

        var refetch = await client.GetAsync($"/api/customer/tickets/{body.Id}");
        Assert.Equal(HttpStatusCode.OK, refetch.StatusCode);
    }

    [Fact]
    public async Task Get_Tickets_Returns403_WhenUserHasNoCustomerLink()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.GetAsync("/api/customer/tickets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AgentToken_Cannot_Access_CustomerPortal()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await client.GetAsync("/api/customer/tickets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CustomerToken_Cannot_Access_InternalTicketsApi()
    {
        var client = await AuthenticatedClientAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/tickets")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync(
                "/api/tickets", new { customerId = _portalCustomerId, title = "T", description = "D" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden, (await client.GetAsync($"/api/tickets/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden, (await client.GetAsync($"/api/tickets/{Guid.NewGuid()}/history")).StatusCode);
    }

    [Fact]
    public async Task TicketDetailsResponse_Excludes_InternalMessages_And_NonStatusHistory()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Filtered Ticket");
        AddMessage(ticketId, "Public reply to the customer.", isInternal: false);
        AddMessage(ticketId, "Internal note not for the customer.", isInternal: true);
        AddHistoryEntry(ticketId, TicketChangeType.Status, "Open", "InProgress");
        AddHistoryEntry(ticketId, TicketChangeType.Assignment, null, Guid.NewGuid().ToString());

        var response = await client.GetAsync($"/api/customer/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();
        Assert.Single(body!.Messages);
        Assert.Equal("Public reply to the customer.", body.Messages[0].Body);
        Assert.Single(body.History);
        Assert.Equal("InProgress", body.History[0].NewValue);
    }
}

// Isolated from CustomerPortalEndpointsTests: dashboard counts are exact
// totals across all of the current customer's tickets, so this must not
// share a fixture/DB with tests that create their own tickets for the same
// seeded portal customer (the well-known shared-fixture isolation issue).
public class CustomerPortalDashboardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _portalCustomerId;
    private readonly Guid _otherCustomerId;

    public CustomerPortalDashboardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        (_portalCustomerId, _otherCustomerId) = factory.SeedPortalCustomers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.PortalCustomerEmail,
            password = CustomWebApplicationFactory.PortalCustomerPassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateTicketForCustomer(Guid customerId, string title, TicketStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = "Sample description",
            Status = status,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    [Fact]
    public async Task Get_Dashboard_ReturnsCountsForCurrentCustomerOnly()
    {
        var client = await AuthenticatedClientAsync();
        CreateTicketForCustomer(_portalCustomerId, "Own Open", TicketStatus.Open);
        CreateTicketForCustomer(_portalCustomerId, "Own Pending", TicketStatus.InProgress);
        CreateTicketForCustomer(_portalCustomerId, "Own Resolved", TicketStatus.Resolved);
        CreateTicketForCustomer(_otherCustomerId, "Someone Else's Open", TicketStatus.Open);

        var response = await client.GetAsync("/api/customer/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerDashboardResponse>();
        Assert.Equal(1, body!.OpenCount);
        Assert.Equal(1, body.PendingCount);
        Assert.Equal(1, body.ResolvedCount);
        Assert.All(body.RecentTickets, t => Assert.DoesNotContain("Someone Else's", t.Title));
    }
}
