using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class TicketsPriorityEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsPriorityEndpointsTests(CustomWebApplicationFactory factory)
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

    [Theory]
    [InlineData("Low")]
    [InlineData("Normal")]
    [InlineData("High")]
    [InlineData("Urgent")]
    public async Task Put_Priority_AcceptsEachAllowedValue(string priority)
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer($"Priority Customer {priority}", $"priority.customer.{priority.ToLowerInvariant()}@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/priority", new { priority });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(priority, ticket!.Priority.ToString());
    }

    [Fact]
    public async Task Put_Priority_UnknownValue_Returns400()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Unknown Priority Customer", "unknown.priority.customer@example.com");
        var ticketId = CreateTicket(customerId);

        var response = await client.PutAsJsonAsync($"/api/tickets/{ticketId}/priority", new { priority = "critical" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Priority_Returns404_WhenTicketMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/priority", new { priority = "High" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Priority_Returns403_ForCustomerRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{Guid.NewGuid()}/priority", new { priority = "High" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
