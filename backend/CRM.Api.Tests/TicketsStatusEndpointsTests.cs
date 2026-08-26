using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class TicketsStatusEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsStatusEndpointsTests(CustomWebApplicationFactory factory)
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

    private Guid CreateTicket(Guid customerId, TicketStatus status)
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
            Priority = TicketPriority.Normal,
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

    [Fact]
    public async Task Put_Status_LegalTransition_ReturnsOk_AndCreatesHistoryRow()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Status Customer", "status.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Open);

        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/status", new { status = "InProgress" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(TicketStatus.InProgress, ticket!.Status);
        Assert.Equal(1, HistoryCount(ticketId));
    }

    [Fact]
    public async Task Put_Status_IllegalTransition_Returns400_WithoutWrite()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Illegal Transition Customer", "illegal.transition.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Closed);

        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/status", new { status = "Open" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, HistoryCount(ticketId));
    }

    [Fact]
    public async Task Put_Status_UnknownValue_Returns400_WithAllowedValuesInMessage()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Unknown Status Customer", "unknown.status.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Open);

        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/status", new { status = "Bogus" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Contains("Open", body!.Message);
        Assert.Contains("Closed", body.Message);
    }

    [Fact]
    public async Task Put_Status_SameValue_ReturnsOk_WithoutNewHistoryRow()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Same Status Customer", "same.status.customer@example.com");
        var ticketId = CreateTicket(customerId, TicketStatus.Open);

        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/status", new { status = "Open" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, HistoryCount(ticketId));
    }

    [Fact]
    public async Task Put_Status_Returns404_WhenTicketMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/status", new { status = "Open" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Status_Returns403_ForCustomerRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/status", new { status = "Open" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
