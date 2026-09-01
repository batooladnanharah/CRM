using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Uses its own CustomWebApplicationFactory instance (a fresh InMemory database)
// so tests in other ticket test classes don't leak rows into these exact-count
// assertions. Only tests that need a *pristine* ticket table live here.
public class TicketsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task List_ReturnsEmpty_WhenNoTickets()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.NotNull(result);
        Assert.Equal(0, result!.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task List_Returns403_ForCustomerRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task All_Return401_WhenAnonymous()
    {
        var listResponse = await _client.GetAsync("/api/tickets");
        Assert.Equal(HttpStatusCode.Unauthorized, listResponse.StatusCode);

        var createResponse = await _client.PostAsJsonAsync("/api/tickets", new
        {
            customerId = Guid.NewGuid(),
            title = "No auth",
            description = "Should be rejected.",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/tickets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, getResponse.StatusCode);
    }
}

// Own factory instance: create/validation/get-by-id tests don't assert exact
// totals, but still shouldn't share a ticket table with the exact-count classes.
public class TicketsCreateEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsCreateEndpointTests(CustomWebApplicationFactory factory)
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
    public async Task Create_ReturnsCreated_AndPersists()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Ticket Customer", "ticket.customer@example.com");

        var createResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId,
            title = "Cannot log in",
            description = "User reports login failures since this morning.",
            priority = "High",
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);
        var created = await createResponse.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(created);
        Assert.Equal(customerId, created!.CustomerId);
        Assert.Equal("Ticket Customer", created.CustomerName);
        Assert.Equal("Cannot log in", created.Title);
        Assert.Equal(TicketStatus.Open, created.Status);
        Assert.Equal(TicketPriority.High, created.Priority);

        var getResponse = await client.GetAsync($"/api/tickets/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal(created.CustomerName, fetched.CustomerName);
        Assert.Equal(created.Title, fetched.Title);
        Assert.Equal(created.Description, fetched.Description);
    }

    [Fact]
    public async Task Create_DefaultsStatusAndPriority_WhenPriorityOmitted()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Default Priority", "default.priority@example.com");

        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId,
            title = "Needs triage",
            description = "No priority specified.",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(TicketStatus.Open, created!.Status);
        Assert.Equal(TicketPriority.Normal, created.Priority);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenCustomerMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId = Guid.NewGuid(),
            title = "Orphan ticket",
            description = "This customer does not exist.",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("customer_not_found", body!.Message);
    }

    [Fact]
    public async Task Create_Validates_TitleRequiredAndMaxLength()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Title Validation", "title.validation@example.com");

        var missingTitleResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId,
            title = "",
            description = "Some description.",
        });
        Assert.Equal(HttpStatusCode.BadRequest, missingTitleResponse.StatusCode);

        var tooLongTitleResponse = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId,
            title = new string('t', 201),
            description = "Some description.",
        });
        Assert.Equal(HttpStatusCode.BadRequest, tooLongTitleResponse.StatusCode);
    }

    [Fact]
    public async Task Create_Validates_DescriptionMaxLength()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Description Validation", "description.validation@example.com");

        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId,
            title = "Too long description",
            description = new string('d', 4001),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_TicketById_Returns404_WhenMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

// Own factory instance: filter/search assertions use Assert.Single against
// non-overlapping predicates, but still isolated from the other ticket classes
// so unrelated seeded rows can never accidentally match a filter.
public class TicketsListFilterEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsListFilterEndpointTests(CustomWebApplicationFactory factory)
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

    private Guid SeedTicket(
        Guid customerId, string title, string description, DateTime createdAtUtc,
        TicketStatus status = TicketStatus.Open, TicketPriority priority = TicketPriority.Normal)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = description,
            Status = status,
            Priority = priority,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    [Fact]
    public async Task List_SearchesTitleAndDescription_CaseInsensitive()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Search Customer", "search.customer@example.com");
        var now = DateTime.UtcNow;
        SeedTicket(customerId, "Printer is broken", "Office printer jams on every print.", now);
        SeedTicket(customerId, "VPN unreachable", "Cannot connect to the corporate VPN.", now.AddMinutes(1));

        var titleMatch = await client.GetAsync("/api/tickets?search=PRINTER");
        var titleResult = await titleMatch.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Single(titleResult!.Items);
        Assert.Equal("Printer is broken", titleResult.Items[0].Title);

        var descriptionMatch = await client.GetAsync("/api/tickets?search=corporate%20vpn");
        var descriptionResult = await descriptionMatch.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Single(descriptionResult!.Items);
        Assert.Equal("VPN unreachable", descriptionResult.Items[0].Title);
    }

    [Fact]
    public async Task List_FiltersByStatusAndPriority()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Filter Customer", "filter.customer@example.com");
        var now = DateTime.UtcNow;
        SeedTicket(customerId, "Open urgent", "d1", now, TicketStatus.Open, TicketPriority.Urgent);
        SeedTicket(customerId, "Closed low", "d2", now.AddMinutes(1), TicketStatus.Closed, TicketPriority.Low);
        SeedTicket(customerId, "Open low", "d3", now.AddMinutes(2), TicketStatus.Open, TicketPriority.Low);

        var statusResponse = await client.GetAsync("/api/tickets?status=Closed");
        var statusResult = await statusResponse.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Single(statusResult!.Items);
        Assert.Equal("Closed low", statusResult.Items[0].Title);

        var priorityResponse = await client.GetAsync("/api/tickets?priority=Urgent");
        var priorityResult = await priorityResponse.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Single(priorityResult!.Items);
        Assert.Equal("Open urgent", priorityResult.Items[0].Title);

        var combinedResponse = await client.GetAsync("/api/tickets?status=Open&priority=Low");
        var combinedResult = await combinedResponse.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Single(combinedResult!.Items);
        Assert.Equal("Open low", combinedResult.Items[0].Title);
    }
}

// Own factory instance: needs an exact, pristine ticket table to assert exact
// pagination counts across 25 seeded rows. See
// TicketsListPaginationBeyondResultsEndpointTests below for the
// beyond-the-last-page case, split into its own class/fixture so the two
// don't share a ticket table.
public class TicketsListPaginationEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsListPaginationEndpointTests(CustomWebApplicationFactory factory)
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

    private void SeedTicket(Guid customerId, string title, DateTime createdAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = "Seeded for pagination.",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task List_Paginates_25Rows()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Pagination Customer", "pagination.customer@example.com");
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 25; i++)
        {
            SeedTicket(customerId, $"Pagination ticket {i}", baseTime.AddMinutes(i));
        }

        var firstPage = await client.GetAsync("/api/tickets?page=1&pageSize=20");
        var firstResult = await firstPage.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Equal(HttpStatusCode.OK, firstPage.StatusCode);
        Assert.Equal(20, firstResult!.Items.Count);
        Assert.Equal(25, firstResult.TotalCount);

        var secondPage = await client.GetAsync("/api/tickets?page=2&pageSize=20");
        var secondResult = await secondPage.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();
        Assert.Equal(HttpStatusCode.OK, secondPage.StatusCode);
        Assert.Equal(5, secondResult!.Items.Count);
        Assert.Equal(25, secondResult.TotalCount);
    }
}

// Own factory instance (separate from TicketsListPaginationEndpointTests, even
// though the setup looks identical): the two classes' [Fact]s previously
// shared one IClassFixture instance and thus one ticket table, so this
// exact-count assertion (5, not 25 + 5 = 30) depended on undefined xUnit test
// ordering. Splitting the class gives each its own fresh in-memory database.
public class TicketsListPaginationBeyondResultsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsListPaginationBeyondResultsEndpointTests(CustomWebApplicationFactory factory)
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

    private void SeedTicket(Guid customerId, string title, DateTime createdAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = "Seeded for pagination.",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task List_ReturnsEmptyItems_WhenPageBeyondResults()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Beyond Page Customer", "beyond.page.customer@example.com");
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 5; i++)
        {
            SeedTicket(customerId, $"Beyond page ticket {i}", baseTime.AddMinutes(i));
        }

        var response = await client.GetAsync("/api/tickets?page=99&pageSize=20");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(result!.Items);
        Assert.Equal(5, result.TotalCount);
    }
}

// Own factory instance: needs an exact, pristine ticket table to assert the
// full, unfiltered list comes back in strict CreatedAtUtc-descending order.
public class TicketsListSortEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketsListSortEndpointTests(CustomWebApplicationFactory factory)
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

    private void SeedTicket(Guid customerId, string title, DateTime createdAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = "Seeded for sort order.",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task List_SortsByCreatedAtDescending()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Sort Customer", "sort.customer@example.com");
        var baseTime = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        SeedTicket(customerId, "Oldest", baseTime);
        SeedTicket(customerId, "Middle", baseTime.AddHours(1));
        SeedTicket(customerId, "Newest", baseTime.AddHours(2));

        var response = await client.GetAsync("/api/tickets?pageSize=100");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TicketListItem>>();

        Assert.Equal(["Newest", "Middle", "Oldest"], result!.Items.Select(i => i.Title));
    }
}
