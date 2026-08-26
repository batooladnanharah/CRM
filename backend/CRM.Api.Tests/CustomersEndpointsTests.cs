using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;

namespace CRM.Api.Tests;

public class CustomersEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomersEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        factory.SeedCustomers();
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

    [Fact]
    public async Task List_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsPagedResults_WithDefaults()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.Page);
        Assert.Equal(25, result.PageSize);
        Assert.True(result.TotalCount >= 3);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task List_AppliesSearch_CaseInsensitive()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers?search=ALICE");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal("Alice Johnson", result.Items[0].FullName);
    }

    [Fact]
    public async Task List_AppliesSearch_ToEmailAndPhone()
    {
        var client = await AuthenticatedClientAsync();

        var emailResponse = await client.GetAsync("/api/customers?search=ALICE.JOHNSON@EXAMPLE.COM");
        var emailResult = await emailResponse.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Single(emailResult!.Items);

        var phoneResponse = await client.GetAsync("/api/customers?search=555-0102");
        var phoneResult = await phoneResponse.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Single(phoneResult!.Items);
        Assert.Equal("Bob Martinez", phoneResult.Items[0].FullName);
    }

    [Fact]
    public async Task List_WhitespaceSearch_ReturnsAll()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers?search=%20%20");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, result!.TotalCount);
    }

    [Fact]
    public async Task List_AppliesExactCompanyFilter()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers?company=Acme%20Corp");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(result!.Items);
        Assert.Equal("Alice Johnson", result.Items[0].FullName);
    }

    [Fact]
    public async Task List_AppliesSearch_LiteralPercentAndOnlySearchesSupportedFields()
    {
        var client = await AuthenticatedClientAsync();

        var percentResponse = await client.GetAsync("/api/customers?search=100%25");
        var percentResult = await percentResponse.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Equal(HttpStatusCode.OK, percentResponse.StatusCode);
        Assert.Single(percentResult!.Items);
        Assert.Equal("100% Discount Co", percentResult.Items[0].FullName);

        var underscoreResponse = await client.GetAsync("/api/customers?search=under_score");
        var underscoreResult = await underscoreResponse.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Equal(HttpStatusCode.OK, underscoreResponse.StatusCode);
        Assert.Empty(underscoreResult!.Items);
    }

    [Fact]
    public async Task List_SortsByAllowedColumn_Asc_And_Desc()
    {
        var client = await AuthenticatedClientAsync();

        var ascResponse = await client.GetAsync("/api/customers?sortBy=fullName&sortDir=asc&pageSize=100");
        var ascResult = await ascResponse.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Equal(HttpStatusCode.OK, ascResponse.StatusCode);
        var ascNames = ascResult!.Items.Select(i => i.FullName).ToList();
        Assert.Equal(ascNames.OrderBy(n => n, StringComparer.Ordinal), ascNames);

        var descResponse = await client.GetAsync("/api/customers?sortBy=fullName&sortDir=desc&pageSize=100");
        var descResult = await descResponse.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Equal(HttpStatusCode.OK, descResponse.StatusCode);
        var descNames = descResult!.Items.Select(i => i.FullName).ToList();
        Assert.Equal(descNames.OrderByDescending(n => n, StringComparer.Ordinal), descNames);
    }

    [Fact]
    public async Task List_Rejects_InvalidSortBy_With400()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers?sortBy=notAColumn");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_Rejects_InvalidSortDir_With400()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers?sortDir=sideways");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_ClampsPageBelowOne_ToOne()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers?page=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Equal(1, result!.Page);
    }

    [Fact]
    public async Task List_ClampsPageSize_ToMax100()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers?pageSize=500");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Equal(100, result!.PageSize);
    }

    [Fact]
    public async Task List_ClampsPageSize_ToMin1()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/customers?pageSize=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItem>>();
        Assert.Equal(1, result!.PageSize);
    }
}

// Uses its own CustomWebApplicationFactory instance (a fresh InMemory database)
// so that customers created here don't leak into CustomersEndpointsTests' exact
// TotalCount assertions against the shared seed data.
public class CustomersCreateEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomersCreateEndpointTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task Post_Customer_ReturnsCreated_WhenValid()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/customers", new
        {
            fullName = "New Customer",
            email = "new.customer@example.com",
            phone = "+1-555-0199",
            company = "New Co",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<CustomerListItem>();
        Assert.NotNull(body);
        Assert.Equal("New Customer", body!.FullName);
        Assert.Equal("new.customer@example.com", body.Email);
    }

    [Fact]
    public async Task Post_Customer_Returns400_WhenFullNameMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/customers", new
        {
            fullName = "",
            email = "missing.name@example.com",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Customer_Returns400_WhenEmailInvalid()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/customers", new
        {
            fullName = "Bad Email",
            email = "not-an-email",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_Customer_Returns409_WhenDuplicateEmail_CaseInsensitive()
    {
        var client = await AuthenticatedClientAsync();

        var first = await client.PostAsJsonAsync("/api/customers", new
        {
            fullName = "Duplicate One",
            email = "duplicate.check@example.com",
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/customers", new
        {
            fullName = "Duplicate Two",
            email = "DUPLICATE.CHECK@EXAMPLE.COM",
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Post_Customer_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsJsonAsync("/api/customers", new
        {
            fullName = "No Token",
            email = "no.token@example.com",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Customer_Returns403_ForCustomerRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail,
            CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.PostAsJsonAsync("/api/customers", new
        {
            fullName = "Blocked",
            email = "blocked@example.com",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

// Uses its own CustomWebApplicationFactory instance (a fresh InMemory database)
// so GET-by-id/PUT tests don't leak rows into CustomersEndpointsTests' exact
// TotalCount assertions against the shared seed data.
public class CustomersEditEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomersEditEndpointTests(CustomWebApplicationFactory factory)
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

    private async Task<CustomerListItem> CreateCustomerAsync(HttpClient client, string fullName, string email)
    {
        var response = await client.PostAsJsonAsync("/api/customers", new { fullName, email });
        var body = await response.Content.ReadFromJsonAsync<CustomerListItem>();
        return body!;
    }

    [Fact]
    public async Task Get_CustomerById_ReturnsOk_WhenFound()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateCustomerAsync(client, "Find Me", "find.me@example.com");

        var response = await client.GetAsync($"/api/customers/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerListItem>();
        Assert.Equal(created.Id, body!.Id);
    }

    [Fact]
    public async Task Get_CustomerById_Returns404_WhenMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_CustomerById_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_Customer_ReturnsOk_WhenValid()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateCustomerAsync(client, "Original Name", "original@example.com");

        var response = await client.PutAsJsonAsync($"/api/customers/{created.Id}", new
        {
            fullName = "Updated Name",
            email = "updated@example.com",
            phone = "+1-555-0200",
            company = "Updated Co",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerListItem>();
        Assert.Equal("Updated Name", body!.FullName);
        Assert.Equal("updated@example.com", body.Email);
        Assert.Equal("Updated Co", body.Company);
    }

    [Fact]
    public async Task Put_Customer_Returns404_WhenMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync($"/api/customers/{Guid.NewGuid()}", new
        {
            fullName = "Nobody",
            email = "nobody@example.com",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_Customer_Returns400_WhenFullNameMissing()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateCustomerAsync(client, "Someone", "someone@example.com");

        var response = await client.PutAsJsonAsync($"/api/customers/{created.Id}", new
        {
            fullName = "",
            email = "someone@example.com",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Customer_Returns400_WhenEmailInvalid()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateCustomerAsync(client, "Someone", "someone2@example.com");

        var response = await client.PutAsJsonAsync($"/api/customers/{created.Id}", new
        {
            fullName = "Someone",
            email = "not-an-email",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_Customer_Returns409_WhenEmailBelongsToAnotherCustomer()
    {
        var client = await AuthenticatedClientAsync();
        await CreateCustomerAsync(client, "First", "first@example.com");
        var second = await CreateCustomerAsync(client, "Second", "second@example.com");

        var response = await client.PutAsJsonAsync($"/api/customers/{second.Id}", new
        {
            fullName = "Second",
            email = "FIRST@EXAMPLE.COM",
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_Customer_AllowsUnchangedEmail()
    {
        var client = await AuthenticatedClientAsync();
        var created = await CreateCustomerAsync(client, "Same Email", "same.email@example.com");

        var response = await client.PutAsJsonAsync($"/api/customers/{created.Id}", new
        {
            fullName = "Same Email Updated",
            email = "same.email@example.com",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Put_Customer_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PutAsJsonAsync($"/api/customers/{Guid.NewGuid()}", new
        {
            fullName = "No Token",
            email = "no.token2@example.com",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_Customer_Returns403_ForCustomerRole()
    {
        var adminClient = await AuthenticatedClientAsync();
        var created = await CreateCustomerAsync(adminClient, "Protected", "protected@example.com");

        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail,
            CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.PutAsJsonAsync($"/api/customers/{created.Id}", new
        {
            fullName = "Blocked",
            email = "blocked2@example.com",
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
