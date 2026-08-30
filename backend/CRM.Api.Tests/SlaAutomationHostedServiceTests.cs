using CRM.Api.Customers;
using CRM.Api.Sla;
using CRM.Api.Tickets;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

// Exercises ISlaEvaluator.EvaluateAllOpenAsync — the method the hosted
// service's PeriodicTimer loop calls every tick (see
// SlaAutomationHostedService.ExecuteAsync). The hosted service itself is
// disabled in tests (Sla:Enabled=false, see CustomWebApplicationFactory) so
// timing never leaks into the suite; what's tested here is the paging/error
// tolerance behaviour the service depends on.
public class SlaAutomationHostedServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SlaAutomationHostedServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
    }

    private WebApplicationFactory<Program> WithBatchSize(int batchSize) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) => configBuilder.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Sla:BatchSize"] = batchSize.ToString() }));
        });

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

    private void SeedOpenTickets(WebApplicationFactory<Program> factory, Guid customerId, int count)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var created = DateTime.UtcNow.AddHours(-1);

        for (var i = 0; i < count; i++)
        {
            db.Tickets.Add(new Ticket
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Title = $"Batch ticket {i}",
                Description = "Sample description",
                Status = TicketStatus.Open,
                Priority = TicketPriority.Normal,
                CreatedAtUtc = created,
                UpdatedAtUtc = created,
                // No SlaPolicyId/due dates — these tickets are "no SLA"
                // (NotApplicable), so evaluation is a cheap no-op beyond
                // stamping SlaLastEvaluatedAtUtc; only the paging behaviour
                // is under test here, not breach/escalation logic.
            });
        }

        db.SaveChanges();
    }

    private int CountEvaluated(WebApplicationFactory<Program> factory, Guid customerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        return db.Tickets.Count(t => t.CustomerId == customerId && t.SlaLastEvaluatedAtUtc != null);
    }

    [Fact]
    public async Task EvaluateAllOpenAsync_MoreTicketsThanBatchSize_OnlyEvaluatesBatchSizePerTick()
    {
        const int batchSize = 5;
        var factory = WithBatchSize(batchSize);
        var customerId = CreateCustomer(factory, "Batch Customer", "batch.customer@example.com");
        SeedOpenTickets(factory, customerId, batchSize + 1);

        using var scope = factory.Services.CreateScope();
        var evaluator = scope.ServiceProvider.GetRequiredService<ISlaEvaluator>();
        var evaluatedCount = await evaluator.EvaluateAllOpenAsync(CancellationToken.None);

        Assert.Equal(batchSize, evaluatedCount);
        Assert.Equal(batchSize, CountEvaluated(factory, customerId));
    }

    [Fact]
    public async Task EvaluateAllOpenAsync_SecondTick_MakesForwardProgressOnRemainingTickets()
    {
        const int batchSize = 5;
        var factory = WithBatchSize(batchSize);
        var customerId = CreateCustomer(factory, "Rotation Customer", "rotation.customer@example.com");
        SeedOpenTickets(factory, customerId, batchSize * 2);

        using (var scope = factory.Services.CreateScope())
        {
            var evaluator = scope.ServiceProvider.GetRequiredService<ISlaEvaluator>();
            await evaluator.EvaluateAllOpenAsync(CancellationToken.None);
        }

        Assert.Equal(batchSize, CountEvaluated(factory, customerId));

        using (var scope = factory.Services.CreateScope())
        {
            var evaluator = scope.ServiceProvider.GetRequiredService<ISlaEvaluator>();
            await evaluator.EvaluateAllOpenAsync(CancellationToken.None);
        }

        // The first batch (already stamped with a recent SlaLastEvaluatedAtUtc)
        // sorts after the still-unevaluated (null) tail, so the second tick
        // must have picked up the remaining, previously-untouched tickets.
        Assert.Equal(batchSize * 2, CountEvaluated(factory, customerId));
    }

    [Fact]
    public async Task EvaluateAllOpenAsync_ResolvedTickets_AreNeverIncludedInABatch()
    {
        const int batchSize = 10;
        var factory = WithBatchSize(batchSize);
        var customerId = CreateCustomer(factory, "Resolved Exclusion Customer", "resolved.exclusion.customer@example.com");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
            var created = DateTime.UtcNow.AddHours(-1);
            db.Tickets.Add(new Ticket
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Title = "Already resolved",
                Description = "Sample description",
                Status = TicketStatus.Resolved,
                Priority = TicketPriority.Normal,
                CreatedAtUtc = created,
                UpdatedAtUtc = created,
                ResolvedAtUtc = created.AddMinutes(5),
            });
            db.SaveChanges();
        }

        using var scope2 = factory.Services.CreateScope();
        var evaluator = scope2.ServiceProvider.GetRequiredService<ISlaEvaluator>();
        var evaluatedCount = await evaluator.EvaluateAllOpenAsync(CancellationToken.None);

        Assert.Equal(0, evaluatedCount);
        Assert.Equal(0, CountEvaluated(factory, customerId));
    }
}
