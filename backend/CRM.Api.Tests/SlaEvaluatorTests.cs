using CRM.Api.Customers;
using CRM.Api.Sla;
using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Exercises ISlaEvaluator directly via the DI container (same style as
// AuthorizationPolicyTests) rather than through HTTP — these are evaluator
// unit tests, not endpoint integration tests.
public class SlaEvaluatorTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SlaEvaluatorTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
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

    private Guid CreateTicket(
        Guid customerId, DateTime createdAtUtc, DateTime? firstResponseDueAtUtc, DateTime? resolutionDueAtUtc,
        TicketPriority priority = TicketPriority.Normal, Guid? policyId = null)
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
            Priority = priority,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            SlaPolicyId = policyId,
            FirstResponseDueAtUtc = firstResponseDueAtUtc,
            ResolutionDueAtUtc = resolutionDueAtUtc,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
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

    private async Task<bool> EvaluateAsync(Guid ticketId, DateTime nowUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var evaluator = scope.ServiceProvider.GetRequiredService<ISlaEvaluator>();
        var ticket = await db.Tickets.SingleAsync(t => t.Id == ticketId);
        return await evaluator.EvaluateAsync(ticket, nowUtc, CancellationToken.None);
    }

    [Fact]
    public async Task EvaluateAsync_TicketOnTrack_RemainsOnTrack_NoHistoryNoEscalation()
    {
        var customerId = CreateCustomer("On Track Customer", "on.track.customer@example.com");
        var now = DateTime.UtcNow;
        var ticketId = CreateTicket(customerId, now, now.AddHours(1), now.AddHours(8));

        var breached = await EvaluateAsync(ticketId, now);

        Assert.False(breached);
        var entity = GetTicket(ticketId);
        Assert.Null(entity.FirstResponseBreachedAtUtc);
        Assert.Null(entity.ResolutionBreachedAtUtc);
        Assert.Null(entity.SlaAutoEscalatedAtUtc);
        Assert.NotNull(entity.SlaLastEvaluatedAtUtc);
        Assert.Equal(0, HistoryCount(ticketId, TicketChangeType.SlaBreached));
        Assert.Equal(0, HistoryCount(ticketId, TicketChangeType.Escalated));
    }

    [Fact]
    public async Task EvaluateAsync_CrossingResponseDue_SetsBreachedAt_WritesHistory_AndEscalates()
    {
        var customerId = CreateCustomer("Breach Customer", "breach.customer@example.com");
        var created = DateTime.UtcNow.AddHours(-2);
        var ticketId = CreateTicket(
            customerId, created, created.AddHours(1), created.AddHours(8), priority: TicketPriority.Normal);

        var breached = await EvaluateAsync(ticketId, DateTime.UtcNow);

        Assert.True(breached);
        var entity = GetTicket(ticketId);
        Assert.NotNull(entity.FirstResponseBreachedAtUtc);
        Assert.NotNull(entity.SlaAutoEscalatedAtUtc);
        Assert.Equal(TicketPriority.High, entity.Priority);
        Assert.Equal(1, HistoryCount(ticketId, TicketChangeType.SlaBreached));
        Assert.Equal(1, HistoryCount(ticketId, TicketChangeType.Escalated));
    }

    [Fact]
    public async Task EvaluateAsync_ReEvaluatingSameTicket_IsIdempotent()
    {
        var customerId = CreateCustomer("Idempotent Customer", "idempotent.customer@example.com");
        var created = DateTime.UtcNow.AddHours(-2);
        var ticketId = CreateTicket(
            customerId, created, created.AddHours(1), created.AddHours(8), priority: TicketPriority.Normal);

        var firstNow = DateTime.UtcNow;
        var firstBreached = await EvaluateAsync(ticketId, firstNow);
        var afterFirst = GetTicket(ticketId);

        var secondBreached = await EvaluateAsync(ticketId, firstNow.AddMinutes(1));
        var afterSecond = GetTicket(ticketId);

        Assert.True(firstBreached);
        Assert.False(secondBreached);
        Assert.Equal(afterFirst.FirstResponseBreachedAtUtc, afterSecond.FirstResponseBreachedAtUtc);
        Assert.Equal(afterFirst.SlaAutoEscalatedAtUtc, afterSecond.SlaAutoEscalatedAtUtc);
        Assert.Equal(afterFirst.Priority, afterSecond.Priority);
        Assert.Equal(1, HistoryCount(ticketId, TicketChangeType.SlaBreached));
        Assert.Equal(1, HistoryCount(ticketId, TicketChangeType.Escalated));
    }

    [Fact]
    public async Task EvaluateAsync_TicketWithNoPolicy_OnlyUpdatesLastEvaluatedAt()
    {
        var customerId = CreateCustomer("No Policy Customer", "no.policy.evaluator.customer@example.com");
        var created = DateTime.UtcNow.AddDays(-1);
        var ticketId = CreateTicket(customerId, created, firstResponseDueAtUtc: null, resolutionDueAtUtc: null);

        var breached = await EvaluateAsync(ticketId, DateTime.UtcNow);

        Assert.False(breached);
        var entity = GetTicket(ticketId);
        Assert.Null(entity.FirstResponseBreachedAtUtc);
        Assert.Null(entity.ResolutionBreachedAtUtc);
        Assert.Null(entity.SlaAutoEscalatedAtUtc);
        Assert.NotNull(entity.SlaLastEvaluatedAtUtc);
        Assert.Equal(0, HistoryCount(ticketId, TicketChangeType.SlaBreached));
    }

    [Fact]
    public async Task EvaluateAsync_TicketAlreadyAtUrgentPriority_BreachesWithoutEscalating()
    {
        var customerId = CreateCustomer("Urgent Customer", "urgent.evaluator.customer@example.com");
        var created = DateTime.UtcNow.AddHours(-2);
        var ticketId = CreateTicket(
            customerId, created, created.AddHours(1), created.AddHours(8), priority: TicketPriority.Urgent);

        var breached = await EvaluateAsync(ticketId, DateTime.UtcNow);

        Assert.True(breached);
        var entity = GetTicket(ticketId);
        Assert.NotNull(entity.FirstResponseBreachedAtUtc);
        // Escalation was attempted (guard is set regardless of outcome so the
        // evaluator never retries a no-op escalation every tick) but it could
        // not raise priority further, so no Escalated history entry exists.
        Assert.NotNull(entity.SlaAutoEscalatedAtUtc);
        Assert.Equal(TicketPriority.Urgent, entity.Priority);
        Assert.Equal(0, HistoryCount(ticketId, TicketChangeType.Escalated));
    }
}
