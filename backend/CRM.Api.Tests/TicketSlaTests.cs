using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Sla;
using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Split into several IClassFixture<CustomWebApplicationFactory> classes rather
// than one — each class gets its own factory/in-memory DB instance, so SLA
// policies seeded by one test never leak into policy-resolution assertions in
// another (the well-known shared-fixture isolation issue in this test suite).

public class TicketSlaPolicyResolutionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketSlaPolicyResolutionTests(CustomWebApplicationFactory factory)
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

    private SlaPolicy SeedPolicy(
        string name, TicketPriority priority, int firstResponseMinutes, int resolutionMinutes,
        bool isDefault = false, string? channel = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();

        var now = DateTime.UtcNow;
        var policy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Channel = channel,
            Priority = priority,
            FirstResponseMinutes = firstResponseMinutes,
            ResolutionMinutes = resolutionMinutes,
            IsDefault = isDefault,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.SlaPolicies.Add(policy);
        db.SaveChanges();
        return policy;
    }

    [Fact]
    public async Task Post_Ticket_WithMatchingExactPriorityPolicy_StampsDueDates()
    {
        var client = await AuthenticatedClientAsync();
        var policy = SeedPolicy("Exact Match High Policy", TicketPriority.High, 30, 240);
        var customerId = CreateCustomer("Exact Match Customer", "exact.match.customer@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId, title = "Title", description = "Description", priority = "High" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(policy.Id, body!.Sla.PolicyId);
        Assert.NotNull(body.Sla.FirstResponseDueAtUtc);
        Assert.NotNull(body.Sla.ResolutionDueAtUtc);
    }

    [Fact]
    public async Task Post_Ticket_WithNoMatchingPolicy_FallsBackToDefault()
    {
        var client = await AuthenticatedClientAsync();
        var defaultPolicy = SeedPolicy("Fallback Default Policy", TicketPriority.Low, 60, 480, isDefault: true);
        var customerId = CreateCustomer("Fallback Default Customer", "fallback.default.customer@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId, title = "Title", description = "Description", priority = "Urgent" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(defaultPolicy.Id, body!.Sla.PolicyId);
    }

    [Fact]
    public async Task Post_Ticket_WithNoMatchingPolicyAndNoDefault_LeavesSlaUnset()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("No Policy Customer", "no.policy.customer@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId, title = "Title", description = "Description", priority = "Normal" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Null(body!.Sla.PolicyId);
        Assert.Null(body.Sla.FirstResponseDueAtUtc);
        Assert.Equal(SlaStatus.NotApplicable, body.Sla.FirstResponseStatus);
        Assert.Equal(SlaStatus.NotApplicable, body.Sla.ResolutionStatus);
    }
}

public class TicketSlaFirstResponseTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketSlaFirstResponseTests(CustomWebApplicationFactory factory)
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

    private void SeedPolicy(string name, TicketPriority priority, int firstResponseMinutes, int resolutionMinutes)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        db.SlaPolicies.Add(new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Priority = priority,
            FirstResponseMinutes = firstResponseMinutes,
            ResolutionMinutes = resolutionMinutes,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        db.SaveChanges();
    }

    private Ticket GetTicket(Guid ticketId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.Tickets.AsNoTracking().Single(t => t.Id == ticketId);
    }

    [Fact]
    public async Task Post_FirstMessage_StampsFirstRespondedAtUtc_AndYieldsMetStatus()
    {
        var client = await AuthenticatedClientAsync();
        SeedPolicy("First Response Policy", TicketPriority.Normal, 60, 480);
        var customerId = CreateCustomer("First Response Customer", "first.response.customer@example.com");

        var created = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId, title = "Title", description = "Description", priority = "Normal" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>();

        var messageResponse = await client.PostAsJsonAsync(
            $"/api/tickets/{ticket!.Id}/messages", new { body = "Responding now.", isInternal = false });
        Assert.Equal(HttpStatusCode.Created, messageResponse.StatusCode);

        var entity = GetTicket(ticket.Id);
        Assert.NotNull(entity.FirstRespondedAtUtc);

        var refetch = await client.GetAsync($"/api/tickets/{ticket.Id}");
        var refetchBody = await refetch.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(SlaStatus.Met, refetchBody!.Sla.FirstResponseStatus);
    }

    [Fact]
    public async Task Post_InternalNote_DoesNotStampFirstRespondedAtUtc_OrCompleteResponseSla()
    {
        var client = await AuthenticatedClientAsync();
        SeedPolicy("Internal Note Policy", TicketPriority.Normal, 60, 480);
        var customerId = CreateCustomer("Internal Note Customer", "internal.note.customer@example.com");

        var created = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId, title = "Title", description = "Description", priority = "Normal" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>();

        var messageResponse = await client.PostAsJsonAsync(
            $"/api/tickets/{ticket!.Id}/messages", new { body = "Internal note only.", isInternal = true });
        Assert.Equal(HttpStatusCode.Created, messageResponse.StatusCode);

        var entity = GetTicket(ticket.Id);
        Assert.Null(entity.FirstRespondedAtUtc);

        var refetch = await client.GetAsync($"/api/tickets/{ticket.Id}");
        var refetchBody = await refetch.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotEqual(SlaStatus.Met, refetchBody!.Sla.FirstResponseStatus);

        // A subsequent customer-visible reply must still be able to complete
        // the response SLA — the internal note must not have permanently
        // blocked it.
        await client.PostAsJsonAsync(
            $"/api/tickets/{ticket.Id}/messages", new { body = "Actual reply.", isInternal = false });
        var afterPublicReply = GetTicket(ticket.Id);
        Assert.NotNull(afterPublicReply.FirstRespondedAtUtc);
    }

    [Fact]
    public async Task Post_SecondMessage_DoesNotOverwriteFirstRespondedAtUtc()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Second Message Customer", "second.message.customer@example.com");

        var created = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId, title = "Title", description = "Description", priority = "Normal" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>();

        await client.PostAsJsonAsync(
            $"/api/tickets/{ticket!.Id}/messages", new { body = "First reply.", isInternal = false });
        var firstRespondedAt = GetTicket(ticket.Id).FirstRespondedAtUtc;

        await client.PostAsJsonAsync(
            $"/api/tickets/{ticket.Id}/messages", new { body = "Second reply.", isInternal = false });
        var afterSecond = GetTicket(ticket.Id).FirstRespondedAtUtc;

        Assert.Equal(firstRespondedAt, afterSecond);
    }
}

public class TicketSlaResolutionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketSlaResolutionTests(CustomWebApplicationFactory factory)
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

    private Ticket GetTicket(Guid ticketId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.Tickets.AsNoTracking().Single(t => t.Id == ticketId);
    }

    [Fact]
    public async Task Put_Status_ToResolved_StampsResolvedAtUtc()
    {
        var client = await AuthenticatedClientAsync();
        var customerId = CreateCustomer("Resolved Customer", "resolved.customer@example.com");

        var created = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId, title = "Title", description = "Description", priority = "Normal" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>();

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket!.Id}/status", new { status = "Resolved" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entity = GetTicket(ticket.Id);
        Assert.NotNull(entity.ResolvedAtUtc);
    }
}

// Each of these two tests seeds its own High/Normal priority policies, and
// resolution picks the first exact-priority match with no defined order — so
// even within one class, the two tests must not share a fixture/DB.
public class TicketSlaPriorityRecomputeBeforeResponseTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketSlaPriorityRecomputeBeforeResponseTests(CustomWebApplicationFactory factory)
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

    private SlaPolicy SeedPolicy(string name, TicketPriority priority, int firstResponseMinutes, int resolutionMinutes)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        var policy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Priority = priority,
            FirstResponseMinutes = firstResponseMinutes,
            ResolutionMinutes = resolutionMinutes,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.SlaPolicies.Add(policy);
        db.SaveChanges();
        return policy;
    }

    private Ticket GetTicket(Guid ticketId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.Tickets.AsNoTracking().Single(t => t.Id == ticketId);
    }

    private int HistoryCount(Guid ticketId, TicketChangeType changeType)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.TicketHistory.Count(h => h.TicketId == ticketId && h.ChangeType == changeType);
    }

    [Fact]
    public async Task Put_Priority_BeforeFirstResponse_RecomputesBothDueDates_AndWritesHistory()
    {
        var client = await AuthenticatedClientAsync();
        SeedPolicy("Normal Priority Policy", TicketPriority.Normal, 60, 480);
        var highPolicy = SeedPolicy("High Priority Policy", TicketPriority.High, 15, 120);
        var customerId = CreateCustomer("Priority Recompute Customer", "priority.recompute.customer@example.com");

        var created = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId, title = "Title", description = "Description", priority = "Normal" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>();
        var originalFirstResponseDue = ticket!.Sla.FirstResponseDueAtUtc;

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}/priority", new { priority = "High" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(highPolicy.Id, body!.Sla.PolicyId);
        Assert.NotEqual(originalFirstResponseDue, body.Sla.FirstResponseDueAtUtc);
        Assert.Equal(1, HistoryCount(ticket.Id, TicketChangeType.SlaRecalculated));
    }
}

public class TicketSlaPriorityRecomputeAfterResponseTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketSlaPriorityRecomputeAfterResponseTests(CustomWebApplicationFactory factory)
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

    private void SeedPolicy(string name, TicketPriority priority, int firstResponseMinutes, int resolutionMinutes)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        db.SlaPolicies.Add(new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Name = name,
            Priority = priority,
            FirstResponseMinutes = firstResponseMinutes,
            ResolutionMinutes = resolutionMinutes,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        db.SaveChanges();
    }

    private Ticket GetTicket(Guid ticketId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.Tickets.AsNoTracking().Single(t => t.Id == ticketId);
    }

    [Fact]
    public async Task Put_Priority_AfterFirstResponse_DoesNotMoveFirstResponseDueAtUtc()
    {
        var client = await AuthenticatedClientAsync();
        SeedPolicy("Normal Priority Policy 2", TicketPriority.Normal, 60, 480);
        SeedPolicy("High Priority Policy 2", TicketPriority.High, 15, 120);
        var customerId = CreateCustomer("Post Response Priority Customer", "post.response.priority.customer@example.com");

        var created = await client.PostAsJsonAsync(
            "/api/tickets", new { customerId, title = "Title", description = "Description", priority = "Normal" });
        var ticket = await created.Content.ReadFromJsonAsync<TicketResponse>();

        await client.PostAsJsonAsync(
            $"/api/tickets/{ticket!.Id}/messages", new { body = "Already responded.", isInternal = false });
        var entityAfterResponse = GetTicket(ticket.Id);
        var firstResponseDueBefore = entityAfterResponse.FirstResponseDueAtUtc;
        var resolutionDueBefore = entityAfterResponse.ResolutionDueAtUtc;

        var response = await client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}/priority", new { priority = "High" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entityAfterPriorityChange = GetTicket(ticket.Id);
        Assert.Equal(firstResponseDueBefore, entityAfterPriorityChange.FirstResponseDueAtUtc);
        Assert.NotEqual(resolutionDueBefore, entityAfterPriorityChange.ResolutionDueAtUtc);
    }
}

public class TicketSlaEvaluateNowEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TicketSlaEvaluateNowEndpointTests(CustomWebApplicationFactory factory)
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

    private Guid CreateBreachedTicket(Guid customerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var created = DateTime.UtcNow.AddHours(-2);
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Sample ticket",
            Description = "Sample description",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = created,
            UpdatedAtUtc = created,
            FirstResponseDueAtUtc = created.AddHours(1),
            ResolutionDueAtUtc = created.AddHours(8),
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    [Fact]
    public async Task Post_EvaluateNow_Returns401_WhenUnauthenticated()
    {
        var response = await _client.PostAsync("/api/sla/evaluate-now", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_EvaluateNow_Returns403_ForAgent()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsync("/api/sla/evaluate-now", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_EvaluateNow_ForAdmin_BreachesTicket_AndWritesAutoEscalationHistory()
    {
        var admin = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminEmail, CustomWebApplicationFactory.AdminPassword);
        var customerId = CreateCustomer("Evaluate Now Customer", "evaluate.now.customer@example.com");
        var ticketId = CreateBreachedTicket(customerId);

        var response = await admin.PostAsync("/api/sla/evaluate-now", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var ticketResponse = await admin.GetAsync($"/api/tickets/{ticketId}");
        var ticket = await ticketResponse.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(ticket!.Sla.FirstResponseBreachedAtUtc);
        Assert.NotNull(ticket.Sla.SlaAutoEscalatedAtUtc);
        Assert.Equal(TicketPriority.High, ticket.Priority);

        var historyResponse = await admin.GetAsync($"/api/tickets/{ticketId}/history");
        var entries = await historyResponse.Content.ReadFromJsonAsync<List<TicketHistoryEntryResponse>>();
        Assert.Contains(entries!, e => e.ChangeType == TicketChangeType.SlaBreached && e.IsSystemActor);
        Assert.Contains(entries!, e => e.ChangeType == TicketChangeType.Escalated && e.IsSystemActor);
    }
}
