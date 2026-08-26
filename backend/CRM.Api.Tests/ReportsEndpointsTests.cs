using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Reports;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Each scenario below gets its own IClassFixture<CustomWebApplicationFactory>
// class rather than sharing one — the summary endpoint aggregates over ALL
// tickets in the database, so exact-count assertions cannot tolerate the
// usual shared-fixture cross-test contamination.

public class ReportsSummaryTicketVolumeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsSummaryTicketVolumeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.AdminEmail,
            password = CustomWebApplicationFactory.AdminPassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateCustomer()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Volume Customer",
            Email = $"volume-{Guid.NewGuid()}@example.com",
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    private void CreateTicket(Guid customerId, TicketStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Sample ticket",
            Description = "Sample description",
            Status = status,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ResolvedAtUtc = status is TicketStatus.Resolved or TicketStatus.Closed ? now : null,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Summary_ReturnsTicketVolumeCounts()
    {
        var customerId = CreateCustomer();
        CreateTicket(customerId, TicketStatus.Open);
        CreateTicket(customerId, TicketStatus.InProgress);
        CreateTicket(customerId, TicketStatus.Resolved);
        CreateTicket(customerId, TicketStatus.Closed);

        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReportsSummaryResponse>();
        Assert.Equal(4, body!.TicketVolume.Total);
        Assert.Equal(2, body.TicketVolume.Open);
        Assert.Equal(2, body.TicketVolume.Resolved);
    }
}

public class ReportsSummaryStatusDistributionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsSummaryStatusDistributionTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.AdminEmail,
            password = CustomWebApplicationFactory.AdminPassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task Summary_ReturnsStatusDistribution_WithZeroesForMissingStatuses()
    {
        // No tickets are created in this test — every status must still
        // appear in the distribution with a zero count.
        var client = await AdminClientAsync();

        var response = await client.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReportsSummaryResponse>();
        Assert.Equal(4, body!.StatusDistribution.Count);
        Assert.All(body.StatusDistribution, s => Assert.Equal(0, s.Count));
        Assert.Contains(body.StatusDistribution, s => s.Status == TicketStatus.Open);
        Assert.Contains(body.StatusDistribution, s => s.Status == TicketStatus.InProgress);
        Assert.Contains(body.StatusDistribution, s => s.Status == TicketStatus.Resolved);
        Assert.Contains(body.StatusDistribution, s => s.Status == TicketStatus.Closed);
    }
}

public class ReportsSummaryAgentPerformanceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsSummaryAgentPerformanceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.AdminEmail,
            password = CustomWebApplicationFactory.AdminPassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateCustomer()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Agent Perf Customer",
            Email = $"agent-perf-{Guid.NewGuid()}@example.com",
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    private void CreateTicket(Guid customerId, Guid? assigneeUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Sample ticket",
            Description = "Sample description",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            AssigneeUserId = assigneeUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        db.SaveChanges();
    }

    private Guid CreateAgent(string name, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = new User { Id = Guid.NewGuid(), Email = email, Name = name, PasswordHash = "x", IsActive = true, Roles = [Roles.Agent] };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    [Fact]
    public async Task Summary_ReturnsAgentPerformance_OrderedByTicketCountDesc_Top10()
    {
        var customerId = CreateCustomer();
        var agents = Enumerable.Range(0, 11)
            .Select(i => CreateAgent($"Agent {i}", $"agent-perf-{i}-{Guid.NewGuid()}@example.com"))
            .ToList();

        // Agent[0] gets the most tickets (11), Agent[1] one fewer, etc., so
        // the least-loaded (the 12th, agent index 10 with 1 ticket) falls
        // outside the top-10 cutoff.
        for (var i = 0; i < agents.Count; i++)
        {
            var ticketCount = agents.Count - i;
            for (var j = 0; j < ticketCount; j++)
            {
                CreateTicket(customerId, agents[i]);
            }
        }
        CreateTicket(customerId, assigneeUserId: null);

        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReportsSummaryResponse>();
        Assert.Equal(10, body!.AgentPerformance.Count);
        Assert.DoesNotContain(body.AgentPerformance, a => a.AgentId == agents[10]);
        Assert.Equal(agents[0], body.AgentPerformance[0].AgentId);
        Assert.Equal(11, body.AgentPerformance[0].TicketCount);
        Assert.True(
            body.AgentPerformance.Zip(body.AgentPerformance.Skip(1))
                .All(pair => pair.First.TicketCount >= pair.Second.TicketCount));
    }

}

public class ReportsSummaryUnknownAgentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsSummaryUnknownAgentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.AdminEmail,
            password = CustomWebApplicationFactory.AdminPassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateCustomer()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Unknown Agent Customer",
            Email = $"unknown-agent-{Guid.NewGuid()}@example.com",
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    private void CreateTicket(Guid customerId, Guid? assigneeUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Sample ticket",
            Description = "Sample description",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            AssigneeUserId = assigneeUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Summary_FallsBackToUnknown_ForDeletedAgent()
    {
        var customerId = CreateCustomer();
        var missingAgentId = Guid.NewGuid();
        CreateTicket(customerId, missingAgentId);

        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/reports/summary");

        var body = await response.Content.ReadFromJsonAsync<ReportsSummaryResponse>();
        var row = Assert.Single(body!.AgentPerformance, a => a.AgentId == missingAgentId);
        Assert.Equal("(unknown)", row.DisplayName);
    }
}

public class ReportsSummarySlaPerformanceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsSummarySlaPerformanceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.AdminEmail,
            password = CustomWebApplicationFactory.AdminPassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateCustomer()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "SLA Report Customer",
            Email = $"sla-report-{Guid.NewGuid()}@example.com",
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    private void CreateTicket(
        Guid customerId, Guid? slaPolicyId, DateTime createdAtUtc, DateTime? resolutionDueAtUtc,
        DateTime? resolvedAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Sample ticket",
            Description = "Sample description",
            Status = resolvedAtUtc is not null ? TicketStatus.Resolved : TicketStatus.Open,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            SlaPolicyId = slaPolicyId,
            ResolutionDueAtUtc = resolutionDueAtUtc,
            ResolvedAtUtc = resolvedAtUtc,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Summary_ReturnsSlaPercentages_MatchingSlaEvaluatorClassification()
    {
        var customerId = CreateCustomer();
        var now = DateTime.UtcNow;

        // Met: resolved before the due date.
        CreateTicket(
            customerId, Guid.NewGuid(), createdAtUtc: now.AddHours(-3),
            resolutionDueAtUtc: now.AddHours(-1), resolvedAtUtc: now.AddHours(-2));

        // Breached: still open, past its due date.
        CreateTicket(
            customerId, Guid.NewGuid(), createdAtUtc: now.AddHours(-3),
            resolutionDueAtUtc: now.AddHours(-1), resolvedAtUtc: null);

        // OnTrack: still open, well before due date (elapsed fraction low).
        CreateTicket(
            customerId, Guid.NewGuid(), createdAtUtc: now,
            resolutionDueAtUtc: now.AddHours(8), resolvedAtUtc: null);

        // No SLA policy applied at all — must be excluded entirely.
        CreateTicket(
            customerId, slaPolicyId: null, createdAtUtc: now, resolutionDueAtUtc: null, resolvedAtUtc: null);

        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReportsSummaryResponse>();
        Assert.Equal(3, body!.SlaPerformance.TotalEvaluated);
        Assert.Equal(2, body.SlaPerformance.WithinSla);
        Assert.Equal(0, body.SlaPerformance.AtRisk);
        Assert.Equal(1, body.SlaPerformance.Breached);
        Assert.Equal(67, body.SlaPerformance.WithinSlaPercent);
        Assert.Equal(33, body.SlaPerformance.BreachedPercent);
    }

    [Fact]
    public async Task Summary_ReturnsZeroPercentages_WhenNoTicketsHaveAnSlaPolicy()
    {
        var customerId = CreateCustomer();
        CreateTicket(customerId, slaPolicyId: null, createdAtUtc: DateTime.UtcNow, resolutionDueAtUtc: null, resolvedAtUtc: null);

        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/reports/summary");

        var body = await response.Content.ReadFromJsonAsync<ReportsSummaryResponse>();
        Assert.Equal(0, body!.SlaPerformance.TotalEvaluated);
        Assert.Equal(0, body.SlaPerformance.WithinSlaPercent);
        Assert.Equal(0, body.SlaPerformance.AtRiskPercent);
        Assert.Equal(0, body.SlaPerformance.BreachedPercent);
    }
}

public class ReportsSummaryResolutionMetricsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsSummaryResolutionMetricsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.AdminEmail,
            password = CustomWebApplicationFactory.AdminPassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateCustomer()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Resolution Customer",
            Email = $"resolution-{Guid.NewGuid()}@example.com",
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    private void CreateTicket(Guid customerId, TicketStatus status, DateTime createdAtUtc, DateTime? resolvedAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Sample ticket",
            Description = "Sample description",
            Status = status,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            ResolvedAtUtc = resolvedAtUtc,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Summary_ReturnsAverageResolutionMinutes_OnlyResolvedTickets()
    {
        var customerId = CreateCustomer();
        var now = DateTime.UtcNow;

        CreateTicket(customerId, TicketStatus.Resolved, now.AddMinutes(-60), now);
        CreateTicket(customerId, TicketStatus.Closed, now.AddMinutes(-120), now);
        CreateTicket(customerId, TicketStatus.Open, now.AddMinutes(-500), null);

        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReportsSummaryResponse>();
        Assert.Equal(2, body!.Resolution.ResolvedTicketCount);
        Assert.NotNull(body.Resolution.AverageResolutionMinutes);
        Assert.Equal(90, body.Resolution.AverageResolutionMinutes!.Value, 3);
    }

    [Fact]
    public async Task Summary_ReturnsNullAverage_WhenNoResolvedTickets()
    {
        var customerId = CreateCustomer();
        CreateTicket(customerId, TicketStatus.Open, DateTime.UtcNow, null);

        var client = await AdminClientAsync();
        var response = await client.GetAsync("/api/reports/summary");

        var body = await response.Content.ReadFromJsonAsync<ReportsSummaryResponse>();
        Assert.Equal(0, body!.Resolution.ResolvedTicketCount);
        Assert.Null(body.Resolution.AverageResolutionMinutes);
    }
}

public class ReportsSummaryAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReportsSummaryAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    [Fact]
    public async Task Summary_ReturnsForbidden_ForNonAdminUser()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await client.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Summary_ReturnsForbidden_ForCustomerRole()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Summary_ReturnsUnauthorized_ForAnonymous()
    {
        var response = await _client.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Summary_ReturnsOk_ForAdminUser()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);

        var response = await client.GetAsync("/api/reports/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
