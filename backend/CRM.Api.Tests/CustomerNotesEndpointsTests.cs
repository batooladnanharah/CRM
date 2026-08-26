using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Uses its own CustomWebApplicationFactory instance (a fresh InMemory database)
// so notes/customers created here don't affect other test classes.
public class CustomerNotesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerNotesEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task Get_ReturnsNotesNewestFirst_ForAuthorizedAgent()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Newest First", "newest.first.notes@example.com");

        var first = await client.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "First note" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var second = await client.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "Second note" });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);

        var response = await client.GetAsync($"/api/customers/{customerId}/notes");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notes = await response.Content.ReadFromJsonAsync<List<CustomerNoteResponse>>();
        Assert.Equal(["Second note", "First note"], notes!.Select(n => n.Content));
        Assert.Equal("Active Agent", notes![0].AuthorDisplayName);
    }

    [Fact]
    public async Task Get_Returns404_WhenCustomerMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}/notes");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}/notes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Returns403_ForCustomerRoleToken()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail,
            CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}/notes");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_CreatesNote_AssociatesAuthorAndCustomer_ReturnsCreated()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Post Customer", "post.customer.notes@example.com");

        var response = await client.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "Hello" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await response.Content.ReadFromJsonAsync<CustomerNoteResponse>();
        Assert.Equal(customerId, note!.CustomerId);
        Assert.Equal("Hello", note.Content);
        Assert.NotEqual(Guid.Empty, note.AuthorId);
        Assert.Null(note.UpdatedAtUtc);
    }

    [Fact]
    public async Task Post_Returns400_WhenContentEmpty()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Empty Content", "empty.content.notes@example.com");

        var response = await client.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenContentWhitespace()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Whitespace Content", "whitespace.content.notes@example.com");

        var response = await client.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns400_WhenContentTooLong()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Too Long", "too.long.notes@example.com");

        var response = await client.PostAsJsonAsync(
            $"/api/customers/{customerId}/notes", new { content = new string('x', 4001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Returns404_WhenCustomerMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            $"/api/customers/{Guid.NewGuid()}/notes", new { content = "Hello" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_AuthorCanUpdateOwnNote_SetsUpdatedAt()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Update Own", "update.own.notes@example.com");
        var created = await client.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "Original" });
        var note = await created.Content.ReadFromJsonAsync<CustomerNoteResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/customers/{customerId}/notes/{note!.Id}", new { content = "Updated" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CustomerNoteResponse>();
        Assert.Equal("Updated", updated!.Content);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public async Task Put_AdminCanUpdateAnyNote()
    {
        var agentClient = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Admin Update", "admin.update.notes@example.com");
        var created = await agentClient.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "Original" });
        var note = await created.Content.ReadFromJsonAsync<CustomerNoteResponse>();

        var adminClient = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await adminClient.PutAsJsonAsync(
            $"/api/customers/{customerId}/notes/{note!.Id}", new { content = "Updated by admin" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Put_AgentCannotUpdateOthersNote_Returns403()
    {
        var agentClient = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Forbidden Update", "forbidden.update.notes@example.com");
        var created = await agentClient.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "Original" });
        var note = await created.Content.ReadFromJsonAsync<CustomerNoteResponse>();

        var otherAgentClient = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.SecondAgentEmail, CustomWebApplicationFactory.SecondAgentPassword);

        var response = await otherAgentClient.PutAsJsonAsync(
            $"/api/customers/{customerId}/notes/{note!.Id}", new { content = "Hijacked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AuthorCanDeleteOwnNote_Returns204()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Delete Own", "delete.own.notes@example.com");
        var created = await client.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "To delete" });
        var note = await created.Content.ReadFromJsonAsync<CustomerNoteResponse>();

        var response = await client.DeleteAsync($"/api/customers/{customerId}/notes/{note!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AgentCannotDeleteOthersNote_Returns403()
    {
        var agentClient = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Delete Forbidden", "delete.forbidden.notes@example.com");
        var created = await agentClient.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "Protected" });
        var note = await created.Content.ReadFromJsonAsync<CustomerNoteResponse>();

        var otherAgentClient = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.SecondAgentEmail, CustomWebApplicationFactory.SecondAgentPassword);

        var response = await otherAgentClient.DeleteAsync($"/api/customers/{customerId}/notes/{note!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AdminCanDeleteAnyNote()
    {
        var agentClient = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Admin Delete", "admin.delete.notes@example.com");
        var created = await agentClient.PostAsJsonAsync($"/api/customers/{customerId}/notes", new { content = "Deletable" });
        var note = await created.Content.ReadFromJsonAsync<CustomerNoteResponse>();

        var adminClient = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await adminClient.DeleteAsync($"/api/customers/{customerId}/notes/{note!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_WhenNoteMissing()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Missing Note", "missing.note.notes@example.com");

        var response = await client.DeleteAsync($"/api/customers/{customerId}/notes/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Note: cascade delete (Customer -> CustomerNotes) is declared via
    // OnDelete(DeleteBehavior.Cascade) and confirmed in the generated migration's
    // SQL ("ON DELETE CASCADE" in AddCustomerNotes). The EF Core InMemory test
    // provider does not enforce relational FK/cascade semantics, so it cannot be
    // exercised meaningfully here; this is verified against real PostgreSQL via
    // the migration SQL instead of an InMemory-provider test.
}
