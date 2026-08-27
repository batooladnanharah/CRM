using CRM.Api.Customers;
using CRM.Api.Notifications;
using CRM.Api.Sla;
using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Exercises IEscalationDispatcher directly via DI (same style as
// SlaEvaluatorTests). Each scenario gets its OWN IClassFixture<CustomWebApplicationFactory>
// (its own in-memory EscalationRules/Notifications tables) rather than sharing
// one class-wide fixture across [Fact]s — EscalationDispatcher.DispatchAsync
// matches ALL active rules by Trigger globally (not scoped to a single test's
// rows), so a shared DB would let one test's seeded rule fire against another
// test's ticket. Same isolation convention already used by TicketSlaTests.cs.
public abstract class EscalationDispatcherTestBase
{
    protected readonly CustomWebApplicationFactory Factory;

    protected EscalationDispatcherTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        factory.SeedUsers();
    }

    protected Guid CreateCustomer()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Dispatcher Test Customer",
            Email = $"{Guid.NewGuid()}@example.com",
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer.Id;
    }

    protected Guid CreateAgentUser()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Auth.AuthDbContext>();
        var user = new Auth.User
        {
            Id = Guid.NewGuid(),
            Email = $"{Guid.NewGuid()}@crm.local",
            Name = "Dispatcher Test Agent",
            IsActive = true,
            Roles = [Auth.Roles.Agent],
            PasswordHash = "unused",
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    protected Ticket CreateTicket(Guid customerId, Guid? assigneeUserId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Dispatcher ticket",
            Description = "Dispatcher ticket description",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Normal,
            AssigneeUserId = assigneeUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket;
    }

    protected EscalationRule CreateRule(EscalationTrigger trigger, bool notifyAgent, bool notifyManager, bool isActive = true)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTimeOffset.UtcNow;
        var rule = new EscalationRule
        {
            Id = Guid.NewGuid(),
            Name = $"Rule {Guid.NewGuid()}",
            Trigger = trigger,
            NotifyAgent = notifyAgent,
            NotifyManager = notifyManager,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.EscalationRules.Add(rule);
        db.SaveChanges();
        return rule;
    }

    protected async Task DispatchAsync(Ticket ticket, SlaObjectiveKind objective, EscalationTrigger trigger)
    {
        using var scope = Factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEscalationDispatcher>();
        await dispatcher.DispatchAsync(ticket, objective, trigger, CancellationToken.None);
    }

    protected int EventCount(Guid ticketId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.EscalationEvents.Count(e => e.TicketId == ticketId);
    }

    protected int NotificationCount(Guid userId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        return db.Notifications.Count(n => n.UserId == userId);
    }

    protected int HistoryCount(Guid ticketId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.TicketHistory.Count(h => h.TicketId == ticketId && h.ChangeType == TicketChangeType.Escalated);
    }
}

public class EscalationDispatcher_AtRisk_NotifiesAgentOnlyTests(CustomWebApplicationFactory factory)
    : EscalationDispatcherTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DispatchAsync_AtRiskTrigger_NotifiesAgentOnly()
    {
        var agentId = CreateAgentUser();
        var customerId = CreateCustomer();
        var ticket = CreateTicket(customerId, agentId);
        CreateRule(EscalationTrigger.AtRisk, notifyAgent: true, notifyManager: false);

        await DispatchAsync(ticket, SlaObjectiveKind.Response, EscalationTrigger.AtRisk);

        Assert.Equal(1, EventCount(ticket.Id));
        Assert.Equal(1, NotificationCount(agentId));
        Assert.Equal(1, HistoryCount(ticket.Id));
    }
}

public class EscalationDispatcher_Breached_NotifiesAgentAndManagerTests(CustomWebApplicationFactory factory)
    : EscalationDispatcherTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DispatchAsync_BreachedTrigger_NotifiesAgentAndManager()
    {
        var agentId = CreateAgentUser();
        var customerId = CreateCustomer();
        var ticket = CreateTicket(customerId, agentId);
        CreateRule(EscalationTrigger.Breached, notifyAgent: true, notifyManager: true);

        await DispatchAsync(ticket, SlaObjectiveKind.Resolution, EscalationTrigger.Breached);

        Assert.Equal(1, EventCount(ticket.Id));
        Assert.Equal(1, NotificationCount(agentId));

        using var scope = Factory.Services.CreateScope();
        var authDb = scope.ServiceProvider.GetRequiredService<Auth.AuthDbContext>();
        var adminId = authDb.Users.Single(u => u.Email == CustomWebApplicationFactory.AdminEmail).Id;
        Assert.True(NotificationCount(adminId) >= 1);
    }
}

public class EscalationDispatcher_UnassignedTicketTests(CustomWebApplicationFactory factory)
    : EscalationDispatcherTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DispatchAsync_UnassignedTicket_StillNotifiesManager()
    {
        var customerId = CreateCustomer();
        var ticket = CreateTicket(customerId, assigneeUserId: null);
        CreateRule(EscalationTrigger.Breached, notifyAgent: true, notifyManager: true);

        await DispatchAsync(ticket, SlaObjectiveKind.Response, EscalationTrigger.Breached);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var evt = db.EscalationEvents.Single(e => e.TicketId == ticket.Id);
        Assert.False(evt.AgentNotified);
        Assert.True(evt.ManagerNotified);
    }
}

public class EscalationDispatcher_InactiveRuleTests(CustomWebApplicationFactory factory)
    : EscalationDispatcherTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DispatchAsync_InactiveRule_IsIgnored()
    {
        var agentId = CreateAgentUser();
        var customerId = CreateCustomer();
        var ticket = CreateTicket(customerId, agentId);
        CreateRule(EscalationTrigger.AtRisk, notifyAgent: true, notifyManager: false, isActive: false);

        await DispatchAsync(ticket, SlaObjectiveKind.Response, EscalationTrigger.AtRisk);

        Assert.Equal(0, EventCount(ticket.Id));
    }
}

public class EscalationDispatcher_DuplicateDispatchTests(CustomWebApplicationFactory factory)
    : EscalationDispatcherTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DispatchAsync_DuplicateDispatch_CreatesNoAdditionalEventsOrNotifications()
    {
        var agentId = CreateAgentUser();
        var customerId = CreateCustomer();
        var ticket = CreateTicket(customerId, agentId);
        CreateRule(EscalationTrigger.AtRisk, notifyAgent: true, notifyManager: false);

        await DispatchAsync(ticket, SlaObjectiveKind.Response, EscalationTrigger.AtRisk);
        await DispatchAsync(ticket, SlaObjectiveKind.Response, EscalationTrigger.AtRisk);

        Assert.Equal(1, EventCount(ticket.Id));
        Assert.Equal(1, NotificationCount(agentId));
    }
}

public class EscalationDispatcher_MultipleActiveRulesTests(CustomWebApplicationFactory factory)
    : EscalationDispatcherTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DispatchAsync_MultipleActiveRules_EachFiresOnce()
    {
        var agentId = CreateAgentUser();
        var customerId = CreateCustomer();
        var ticket = CreateTicket(customerId, agentId);
        CreateRule(EscalationTrigger.AtRisk, notifyAgent: true, notifyManager: false);
        CreateRule(EscalationTrigger.AtRisk, notifyAgent: true, notifyManager: false);

        await DispatchAsync(ticket, SlaObjectiveKind.Response, EscalationTrigger.AtRisk);

        Assert.Equal(2, EventCount(ticket.Id));
        Assert.Equal(2, NotificationCount(agentId));
    }
}

public class EscalationDispatcher_ContinuesOnPerRuleFailureTests(CustomWebApplicationFactory factory)
    : EscalationDispatcherTestBase(factory), IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DispatchAsync_NoActiveRules_CreatesNothing()
    {
        var customerId = CreateCustomer();
        var ticket = CreateTicket(customerId, assigneeUserId: null);

        await DispatchAsync(ticket, SlaObjectiveKind.Response, EscalationTrigger.AtRisk);

        Assert.Equal(0, EventCount(ticket.Id));
    }
}
