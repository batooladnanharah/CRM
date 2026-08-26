using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Security;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Verifies the story's headline claim: an authorized external application
// can authenticate, read, and write through the CRM API end-to-end using
// only plumbing that already exists (no new endpoints were added for this
// story). Customers + Tickets stand in as the "representative read/write
// module" per the plan.
public class ExternalApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ExternalApiIntegrationTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task Unauthenticated_Read_Returns401()
    {
        var response = await _client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnderPrivileged_AdminRoute_Returns403()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TamperedToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is-not.a-valid-jwt");

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidPayload_ReturnsExistingErrorShape()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        // Malformed create-ticket body: an unknown CustomerId and an empty
        // Title/Description — the handler validates Title first.
        var response = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId = Guid.NewGuid(), title = "", description = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType!.ToString());

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("Title is required.", body!.Message);
    }

    [Fact]
    public async Task ExternalClient_ReadThenWrite_HappyPath()
    {
        // 1. Authenticate via the documented login endpoint.
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = CustomWebApplicationFactory.ActivePassword,
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        // 2. Representative read: list customers.
        var customerId = CreateCustomer("External Flow Customer", "external.flow.customer@example.com");
        var listResponse = await client.GetAsync("/api/customers?search=External Flow Customer");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Contains(page!.Items, c => c.Id == customerId);

        // 3. Representative write: create a ticket.
        var createResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId,
            title = "External client created this ticket",
            description = "Verifying the write path end-to-end.",
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>();

        // 4. Confirm the write persisted via a follow-up read.
        var getResponse = await client.GetAsync($"/api/tickets/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal("External client created this ticket", fetched!.Title);

        // 5. Confirm the mutation produced an audit row (Task 2 of this story).
        using var scope = _factory.Services.CreateScope();
        var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        Assert.Contains(
            authDb.AuditLogs,
            a => a.Action == AuditActions.TicketCreated && a.TargetId == created.Id.ToString());
    }

    [Fact]
    public async Task OpenApi_DocumentIsServed()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(raw));

        using var document = JsonDocument.Parse(raw);
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/customers", out _));
        Assert.True(paths.TryGetProperty("/api/tickets", out _));
    }
}
