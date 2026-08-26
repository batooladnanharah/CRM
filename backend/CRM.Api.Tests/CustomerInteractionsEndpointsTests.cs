using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Uses its own CustomWebApplicationFactory instance (a fresh InMemory database)
// so interaction rows created here don't affect other test classes.
public class CustomerInteractionsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerInteractionsEndpointsTests(CustomWebApplicationFactory factory)
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

    private void AddInteraction(Guid customerId, DateTime occurredAt, string summary = "Summary")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        db.CustomerInteractions.Add(new CustomerInteraction
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Type = CustomerInteractionType.CustomerMessage,
            Summary = summary,
            OccurredAt = occurredAt,
            CreatedAtUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_Interactions_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}/interactions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Interactions_UnknownCustomer_Returns404()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}/interactions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Interactions_InvalidGuidRoute_Returns404()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers/not-a-guid/interactions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Interactions_EmptyHistory_ReturnsOkEmptyPage()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Empty History", "empty.history@example.com");

        var response = await client.GetAsync($"/api/customers/{customerId}/interactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CustomerInteractionDto>>();
        Assert.Empty(body!.Items);
        Assert.Equal(0, body.TotalCount);
    }

    [Fact]
    public async Task Get_Interactions_ReturnsItemsNewestFirst()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Newest First", "newest.first@example.com");
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        AddInteraction(customerId, baseTime, "Oldest");
        AddInteraction(customerId, baseTime.AddDays(2), "Newest");
        AddInteraction(customerId, baseTime.AddDays(1), "Middle");

        var response = await client.GetAsync($"/api/customers/{customerId}/interactions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CustomerInteractionDto>>();
        Assert.Equal(["Newest", "Middle", "Oldest"], body!.Items.Select(i => i.Summary));
    }

    [Fact]
    public async Task Get_Interactions_SameTimestamp_OrdersByIdDescending()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Same Timestamp", "same.timestamp@example.com");
        var occurredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Guid firstId, secondId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
            var first = new CustomerInteraction
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Type = CustomerInteractionType.CustomerMessage,
                Summary = "First",
                OccurredAt = occurredAt,
                CreatedAtUtc = DateTime.UtcNow,
            };
            var second = new CustomerInteraction
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Type = CustomerInteractionType.CustomerMessage,
                Summary = "Second",
                OccurredAt = occurredAt,
                CreatedAtUtc = DateTime.UtcNow,
            };
            db.CustomerInteractions.AddRange(first, second);
            db.SaveChanges();
            firstId = first.Id;
            secondId = second.Id;
        }

        var response = await client.GetAsync($"/api/customers/{customerId}/interactions");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CustomerInteractionDto>>();

        var expectedFirst = firstId.CompareTo(secondId) > 0 ? "First" : "Second";
        Assert.Equal(expectedFirst, body!.Items[0].Summary);
    }

    [Fact]
    public async Task Get_Interactions_Pagination_ReturnsCorrectSliceAndTotal()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Paged Customer", "paged.customer@example.com");
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 25; i++)
        {
            AddInteraction(customerId, baseTime.AddMinutes(i), $"Item {i}");
        }

        var response = await client.GetAsync($"/api/customers/{customerId}/interactions?page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CustomerInteractionDto>>();
        Assert.Equal(10, body!.Items.Count);
        Assert.Equal(25, body.TotalCount);
        Assert.Equal(2, body.Page);
    }

    [Fact]
    public async Task Get_Interactions_ClampsPageSize_ToMax100()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Clamp Customer", "clamp.customer@example.com");

        var response = await client.GetAsync($"/api/customers/{customerId}/interactions?pageSize=500");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CustomerInteractionDto>>();
        Assert.Equal(100, body!.PageSize);
    }

    [Fact]
    public async Task Get_Interactions_ClampsPageBelowOne_ToOne()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Clamp Page Customer", "clamp.page.customer@example.com");

        var response = await client.GetAsync($"/api/customers/{customerId}/interactions?page=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CustomerInteractionDto>>();
        Assert.Equal(1, body!.Page);
    }
}
