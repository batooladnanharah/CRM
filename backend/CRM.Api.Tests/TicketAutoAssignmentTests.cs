using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Tickets;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CRM.Api.Tests;

// CRM-62 — SLA-003 Automatic Ticket Assignment. Each test seeds its own
// agents/tickets directly against the in-memory DbContexts (bypassing the
// manual-assignment endpoint) so workload/eligibility scenarios are exact,
// then exercises the real POST /api/tickets endpoint end to end.
public class TicketAutoAssignmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TicketAutoAssignmentTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        // Deliberately NOT calling factory.SeedUsers() here: its baseline
        // agents (ActiveEmail, SecondAgentEmail — both active & available)
        // would silently become eligible auto-assignment candidates in every
        // test that shares this fixture, corrupting the exact workload/
        // eligibility scenarios each test constructs below. Only the caller
        // needed to authenticate is ever seeded (SeedAdminOnly/SeedCaller).
    }

    private async Task<HttpClient> AuthenticatedClientAsync(
        WebApplicationFactory<Program> factory,
        string email = CustomWebApplicationFactory.AdminEmail,
        string password = CustomWebApplicationFactory.AdminPassword)
    {
        var anonymous = factory.CreateClient();
        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateCustomer(WebApplicationFactory<Program> factory, string fullName, string email)
    {
        using var scope = factory.Services.CreateScope();
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

    private Guid SeedAgent(
        WebApplicationFactory<Program> factory, string name, string email,
        bool isActive = true, bool isAvailable = true, bool asAgent = true)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            IsActive = isActive,
            IsAvailable = isAvailable,
            Roles = asAgent ? [Roles.Agent] : [],
        };
        user.PasswordHash = hasher.HashPassword(user, "Correct#Passw0rd!");
        db.Users.Add(user);
        db.SaveChanges();
        return user.Id;
    }

    private void SeedTicketFor(
        WebApplicationFactory<Program> factory, Guid customerId, Guid? assigneeUserId, TicketStatus status)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.Tickets.Add(new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = "Seed ticket",
            Description = "Seed description",
            Status = status,
            Priority = TicketPriority.Normal,
            AssigneeUserId = assigneeUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    private void SeedActiveTickets(WebApplicationFactory<Program> factory, Guid customerId, Guid agentId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            SeedTicketFor(factory, customerId, agentId, TicketStatus.Open);
        }
    }

    private int HistoryCountForAction(WebApplicationFactory<Program> factory, Guid ticketId, string reasonPrefix)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.TicketHistory.Count(h =>
            h.TicketId == ticketId && h.ChangeType == TicketChangeType.Assignment
            && h.Reason != null && h.Reason.StartsWith(reasonPrefix));
    }

    // Seeds only the Admin caller (not the full baseline fixture set) — used
    // for the two tests that need a derived WithWebHostBuilder() factory,
    // whose runtime type is not CustomWebApplicationFactory so SeedUsers()
    // isn't reachable there.
    private void SeedAdminOnly(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        if (db.Users.Any(u => u.Email == CustomWebApplicationFactory.AdminEmail))
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = CustomWebApplicationFactory.AdminEmail,
            Name = "Default Admin",
            IsActive = true,
            Roles = [Roles.Admin],
        };
        admin.PasswordHash = hasher.HashPassword(admin, CustomWebApplicationFactory.AdminPassword);
        db.Users.Add(admin);
        db.SaveChanges();
    }

    private static async Task<HttpResponseMessage> PostCreateTicketAsync(
        HttpClient client, Guid customerId, Guid? assignedAgentId = null)
    {
        return await client.PostAsJsonAsync("/api/tickets", new
        {
            customerId,
            title = "Cannot log in",
            description = "User reports being unable to sign in.",
            priority = "Normal",
            assignedAgentId,
        });
    }

    [Fact]
    public async Task AutoAssignmentEnabled_AssignsAgentWithLowestWorkload()
    {
        await using var factory = new CustomWebApplicationFactory();
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "Lowest Workload Co", "lowest.workload@example.com");

        var agentA = SeedAgent(factory, "Agent A", "agent.a.lowest@crm.local");
        var agentB = SeedAgent(factory, "Agent B", "agent.b.lowest@crm.local");
        var agentC = SeedAgent(factory, "Agent C", "agent.c.lowest@crm.local");
        SeedActiveTickets(factory, customerId, agentA, 2);
        SeedActiveTickets(factory, customerId, agentB, 5);
        SeedActiveTickets(factory, customerId, agentC, 3);

        var response = await PostCreateTicketAsync(client, customerId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(agentA, ticket!.AssigneeUserId);
        Assert.True(ticket.AutoAssigned);
    }

    [Fact]
    public async Task AutoAssignmentDisabled_LeavesTicketUnassigned()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TicketAutoAssignment:Enabled"] = "false",
                });
            });
        });
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "Disabled Auto Co", "disabled.auto@example.com");
        SeedAgent(factory, "Idle Agent", "idle.agent.disabled@crm.local");

        var response = await PostCreateTicketAsync(client, customerId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Null(ticket!.AssigneeUserId);
        Assert.False(ticket.AutoAssigned);
    }

    [Fact]
    public async Task NoEligibleAgents_ReturnsCreatedTicketUnassigned()
    {
        await using var factory = new CustomWebApplicationFactory();
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "No Agents Co", "no.agents@example.com");
        SeedAgent(factory, "Inactive Only", "inactive.only@crm.local", isActive: false);
        SeedAgent(factory, "Unavailable Only", "unavailable.only@crm.local", isAvailable: false);

        var response = await PostCreateTicketAsync(client, customerId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Null(ticket!.AssigneeUserId);
        Assert.False(ticket.AutoAssigned);
    }

    [Fact]
    public async Task DepartmentFiltering_ExcludesOtherDepartments()
    {
        // No DepartmentId exists anywhere on Ticket or User in this codebase
        // (grepped before implementing) — department matching is a documented
        // no-op per the story's edge cases. This test asserts the workload
        // rule still governs selection when no department concept exists,
        // i.e. auto-assignment is not accidentally disabled by the absence
        // of a department field.
        await using var factory = new CustomWebApplicationFactory();
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "Dept Co", "dept.co@example.com");
        var supportAgent = SeedAgent(factory, "Support Agent", "support.agent.dept@crm.local");

        var response = await PostCreateTicketAsync(client, customerId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(supportAgent, ticket!.AssigneeUserId);
    }

    [Fact]
    public async Task InactiveAgentsExcluded()
    {
        await using var factory = new CustomWebApplicationFactory();
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "Inactive Excl Co", "inactive.excl@example.com");
        SeedAgent(factory, "Inactive Agent", "inactive.excl.agent@crm.local", isActive: false);
        var activeAgent = SeedAgent(factory, "Active Agent", "active.excl.agent@crm.local");

        var response = await PostCreateTicketAsync(client, customerId);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(activeAgent, ticket!.AssigneeUserId);
    }

    [Fact]
    public async Task UnavailableAgentsExcluded()
    {
        await using var factory = new CustomWebApplicationFactory();
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "Unavailable Excl Co", "unavailable.excl@example.com");
        SeedAgent(factory, "Unavailable Agent", "unavailable.excl.agent@crm.local", isAvailable: false);
        var availableAgent = SeedAgent(factory, "Available Agent", "available.excl.agent@crm.local");

        var response = await PostCreateTicketAsync(client, customerId);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(availableAgent, ticket!.AssigneeUserId);
    }

    [Fact]
    public async Task NonAgentUsersExcluded()
    {
        await using var factory = new CustomWebApplicationFactory();
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "Manager Only Co", "manager.only@example.com");
        var manager = SeedAgent(factory, "Manager No Role", "manager.no.role@crm.local", asAgent: false);
        var agent = SeedAgent(factory, "Real Agent", "real.agent.norole@crm.local");
        // Give the non-agent "manager" zero tickets and the real agent one —
        // if role filtering were broken, lowest-workload would wrongly prefer
        // the manager (0 < 1).
        SeedActiveTickets(factory, customerId, agent, 1);

        var response = await PostCreateTicketAsync(client, customerId);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(agent, ticket!.AssigneeUserId);
        Assert.NotEqual(manager, ticket.AssigneeUserId);
    }

    [Fact]
    public async Task TieBreaker_UsesStableUserIdOrder()
    {
        // Verified across 3 independent factories/databases (not 3 tickets in
        // the same run) — creating a ticket changes the winner's workload, so
        // asserting repeatedly against the same seeded pair would just prove
        // the *next* tie is broken differently, not that ties are deterministic.
        for (var run = 0; run < 3; run++)
        {
            await using var factory = new CustomWebApplicationFactory();
            SeedAdminOnly(factory);
            var client = await AuthenticatedClientAsync(factory);
            var customerId = CreateCustomer(factory, $"Tie Break Co {run}", $"tie.break.{run}@example.com");

            var agent1 = SeedAgent(factory, "Tie Agent 1", $"tie.agent.1.{run}@crm.local");
            var agent2 = SeedAgent(factory, "Tie Agent 2", $"tie.agent.2.{run}@crm.local");
            SeedActiveTickets(factory, customerId, agent1, 3);
            SeedActiveTickets(factory, customerId, agent2, 3);
            var expected = agent1 < agent2 ? agent1 : agent2;

            var response = await PostCreateTicketAsync(client, customerId);
            var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
            Assert.Equal(expected, ticket!.AssigneeUserId);
        }
    }

    [Fact]
    public async Task ResolvedAndClosedTicketsExcludedFromWorkload()
    {
        await using var factory = new CustomWebApplicationFactory();
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "Resolved Excl Co", "resolved.excl@example.com");

        var busyOnPaper = SeedAgent(factory, "Busy On Paper", "busy.paper@crm.local");
        for (var i = 0; i < 5; i++)
        {
            SeedTicketFor(factory, customerId, busyOnPaper, TicketStatus.Resolved);
        }
        SeedTicketFor(factory, customerId, busyOnPaper, TicketStatus.Closed);

        var trulyBusy = SeedAgent(factory, "Truly Busy", "truly.busy@crm.local");
        SeedActiveTickets(factory, customerId, trulyBusy, 1);

        var response = await PostCreateTicketAsync(client, customerId);

        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(busyOnPaper, ticket!.AssigneeUserId);
    }

    [Fact]
    public async Task HistoryEntryWrittenForAutoAssignment()
    {
        await using var factory = new CustomWebApplicationFactory();
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "History Co", "history.auto@example.com");
        SeedAgent(factory, "History Agent", "history.agent@crm.local");

        var response = await PostCreateTicketAsync(client, customerId);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();

        Assert.Equal(1, HistoryCountForAction(factory, ticket!.Id, "AutoAssigned"));
    }

    [Fact]
    public async Task ClientProvidedAssigneeIgnoredWithoutPermission()
    {
        await using var factory = new CustomWebApplicationFactory();
        // The caller carries the "agent" role only — not Admin — so the
        // manual-assignment permission for create-time override is denied.
        // isAvailable: false keeps this caller itself out of the auto-assignment
        // candidate pool, so it can't accidentally "win" lowest-workload and
        // mask whether the override was actually ignored.
        const string callerEmail = "caller.noperm@crm.local";
        const string callerPassword = "Correct#Passw0rd!";
        SeedAgent(factory, "Caller Agent", callerEmail, isAvailable: false);
        var client = await AuthenticatedClientAsync(factory, callerEmail, callerPassword);
        var customerId = CreateCustomer(factory, "No Permission Co", "no.permission@example.com");
        var targetAgent = SeedAgent(factory, "Target Agent", "target.agent.noperm@crm.local");
        var lowerWorkloadAgent = SeedAgent(factory, "Lower Workload Agent", "lower.workload.noperm@crm.local");
        SeedActiveTickets(factory, customerId, targetAgent, 5);

        var response = await PostCreateTicketAsync(client, customerId, assignedAgentId: targetAgent);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotEqual(targetAgent, ticket!.AssigneeUserId);
        Assert.Equal(lowerWorkloadAgent, ticket.AssigneeUserId);
        Assert.True(ticket.AutoAssigned);
    }

    [Fact]
    public async Task ClientProvidedAssigneeHonouredWithPermission()
    {
        await using var factory = new CustomWebApplicationFactory();
        SeedAdminOnly(factory);
        // AdminEmail carries the Admin role, which this implementation treats
        // as the "manager" tier permitted to override assignment at creation.
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "With Permission Co", "with.permission@example.com");
        var targetAgent = SeedAgent(factory, "Chosen Agent", "chosen.agent.perm@crm.local");
        var lowerWorkloadAgent = SeedAgent(factory, "Lower Workload Agent2", "lower.workload.perm@crm.local");
        SeedActiveTickets(factory, customerId, targetAgent, 5);

        var response = await PostCreateTicketAsync(client, customerId, assignedAgentId: targetAgent);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(targetAgent, ticket!.AssigneeUserId);
        Assert.NotEqual(lowerWorkloadAgent, ticket.AssigneeUserId);
        Assert.False(ticket.AutoAssigned);
    }

    [Fact]
    public async Task AssignmentServiceThrows_TicketStillCreatedUnassigned()
    {
        await using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITicketAssignmentService>();
                services.AddScoped<ITicketAssignmentService, ThrowingTicketAssignmentService>();
            });
        });
        SeedAdminOnly(factory);
        var client = await AuthenticatedClientAsync(factory);
        var customerId = CreateCustomer(factory, "Throws Co", "throws.auto@example.com");
        SeedAgent(factory, "Would Be Agent", "would.be.agent@crm.local");

        var response = await PostCreateTicketAsync(client, customerId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Null(ticket!.AssigneeUserId);
        Assert.False(ticket.AutoAssigned);
    }

    private sealed class ThrowingTicketAssignmentService : ITicketAssignmentService
    {
        public Task<Guid?> TryAutoAssignAsync(Ticket ticket, CancellationToken ct) =>
            throw new InvalidOperationException("Simulated assignment failure.");
    }
}
