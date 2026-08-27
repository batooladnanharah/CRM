using CRM.Api.Ai;
using CRM.Api.Auth;
using CRM.Api.CommunicationChannels;
using CRM.Api.Customers;
using CRM.Api.Customers.Attachments;
using CRM.Api.Email;
using CRM.Api.KnowledgeBase;
using CRM.Api.Notifications;
using CRM.Api.QuickReplies;
using CRM.Api.Tickets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CRM.Api.Tests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ActiveEmail = "active@crm.local";
    public const string ActivePassword = "Correct#Passw0rd!";
    public const string InactiveEmail = "inactive@crm.local";
    public const string InactivePassword = "Correct#Passw0rd!";
    public const string AdminEmail = "admin@crm.local";
    public const string AdminPassword = "Correct#Passw0rd!";
    public const string MultiRoleEmail = "admin-agent@crm.local";
    public const string MultiRolePassword = "Correct#Passw0rd!";
    public const string CustomerRoleEmail = "customer-role@crm.local";
    public const string CustomerRolePassword = "Correct#Passw0rd!";
    public const string PortalCustomerEmail = "portal-customer@crm.local";
    public const string PortalCustomerPassword = "Correct#Passw0rd!";
    public const string OtherPortalCustomerEmail = "other-portal-customer@crm.local";
    public const string OtherPortalCustomerPassword = "Correct#Passw0rd!";
    public const string SecondAgentEmail = "second-agent@crm.local";
    public const string SecondAgentPassword = "Correct#Passw0rd!";

    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly string _customerDbName = Guid.NewGuid().ToString();
    private readonly string _ticketDbName = Guid.NewGuid().ToString();
    private readonly string _quickReplyDbName = Guid.NewGuid().ToString();
    private readonly string _communicationChannelsDbName = Guid.NewGuid().ToString();
    private readonly string _knowledgeBaseDbName = Guid.NewGuid().ToString();
    private readonly string _notificationsDbName = Guid.NewGuid().ToString();

    public CustomWebApplicationFactory()
    {
        // Program.cs fails fast if Jwt:Key is missing; user-secrets are only loaded
        // in the Development environment, so tests supply the key via an
        // environment variable instead (read unconditionally by WebApplication.CreateBuilder).
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-at-least-32-characters-long");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Program.cs only registers SlaAutomationHostedService when
        // Sla:Enabled is true (read before the host is built) — override it
        // here so the timer never runs during tests and evaluate-now stays
        // the only deterministic trigger.
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sla:Enabled"] = "false",
            });
        });

        builder.ConfigureServices(services =>
        {
            // EF Core 8+ aggregates ALL registered IDbContextOptionsConfiguration<T>
            // entries when building DbContextOptions<T>, so removing just the
            // DbContextOptions<AuthDbContext> descriptor is not enough to drop the
            // original UseNpgsql(...) configuration added by Program.cs.
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<AuthDbContext>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<AuthDbContext>));

            services.AddDbContext<AuthDbContext>(options => options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<DbContextOptions<CustomerDbContext>>();
            services.RemoveAll<CustomerDbContext>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<CustomerDbContext>));

            services.AddDbContext<CustomerDbContext>(options => options.UseInMemoryDatabase(_customerDbName));

            services.RemoveAll<DbContextOptions<TicketDbContext>>();
            services.RemoveAll<TicketDbContext>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<TicketDbContext>));

            services.AddDbContext<TicketDbContext>(options => options.UseInMemoryDatabase(_ticketDbName));

            services.RemoveAll<DbContextOptions<QuickReplyDbContext>>();
            services.RemoveAll<QuickReplyDbContext>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<QuickReplyDbContext>));

            services.AddDbContext<QuickReplyDbContext>(options => options.UseInMemoryDatabase(_quickReplyDbName));

            services.RemoveAll<DbContextOptions<CommunicationChannelsDbContext>>();
            services.RemoveAll<CommunicationChannelsDbContext>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<CommunicationChannelsDbContext>));

            services.AddDbContext<CommunicationChannelsDbContext>(
                options => options.UseInMemoryDatabase(_communicationChannelsDbName));

            services.RemoveAll<DbContextOptions<KnowledgeBaseDbContext>>();
            services.RemoveAll<KnowledgeBaseDbContext>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<KnowledgeBaseDbContext>));

            services.AddDbContext<KnowledgeBaseDbContext>(
                options => options.UseInMemoryDatabase(_knowledgeBaseDbName));

            services.RemoveAll<DbContextOptions<NotificationsDbContext>>();
            services.RemoveAll<NotificationsDbContext>();
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<NotificationsDbContext>));

            services.AddDbContext<NotificationsDbContext>(
                options => options.UseInMemoryDatabase(_notificationsDbName));

            // Real LocalFileStorage would write under App_Data on disk; tests use
            // an in-memory double instead so attachment tests never touch disk.
            services.RemoveAll<IFileStorage>();
            services.AddSingleton<IFileStorage, InMemoryFileStorage>();

            // Real DevelopmentEmailService only logs; tests use FakeEmailService
            // (singleton, so tests can flip ShouldFail and inspect SentRequests)
            // instead of asserting on log output.
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, FakeEmailService>();

            // Always swap in FakeAiService regardless of which branch Program.cs's
            // AI:Provider factory picked, so tests control IsAvailable/ShouldThrow
            // deterministically the same way FakeEmailService overrides IEmailService.
            services.RemoveAll<IAiService>();
            services.AddSingleton<IAiService, FakeAiService>();
        });
    }

    public void SeedUsers()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        if (db.Users.Any())
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var active = new User
        {
            Id = Guid.NewGuid(),
            Email = ActiveEmail,
            Name = "Active Agent",
            IsActive = true,
            Roles = ["agent"],
        };
        active.PasswordHash = hasher.HashPassword(active, ActivePassword);

        var inactive = new User
        {
            Id = Guid.NewGuid(),
            Email = InactiveEmail,
            Name = "Inactive Agent",
            IsActive = false,
            Roles = ["agent"],
        };
        inactive.PasswordHash = hasher.HashPassword(inactive, InactivePassword);

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = AdminEmail,
            Name = "Default Admin",
            IsActive = true,
            Roles = [Roles.Admin],
        };
        admin.PasswordHash = hasher.HashPassword(admin, AdminPassword);

        var multiRole = new User
        {
            Id = Guid.NewGuid(),
            Email = MultiRoleEmail,
            Name = "Admin Agent",
            IsActive = true,
            Roles = [Roles.Admin, Roles.Agent],
        };
        multiRole.PasswordHash = hasher.HashPassword(multiRole, MultiRolePassword);

        var customerRole = new User
        {
            Id = Guid.NewGuid(),
            Email = CustomerRoleEmail,
            Name = "Portal Customer",
            IsActive = true,
            Roles = [Roles.Customer],
        };
        customerRole.PasswordHash = hasher.HashPassword(customerRole, CustomerRolePassword);

        var secondAgent = new User
        {
            Id = Guid.NewGuid(),
            Email = SecondAgentEmail,
            Name = "Second Agent",
            IsActive = true,
            Roles = ["agent"],
        };
        secondAgent.PasswordHash = hasher.HashPassword(secondAgent, SecondAgentPassword);

        db.Users.AddRange(active, inactive, admin, multiRole, customerRole, secondAgent);
        db.SaveChanges();
    }

    // Distinct from CustomerRoleEmail (a Customer-role user with no
    // CustomerId link, used to test the "mis-provisioned account" 403 path).
    // These two are linked to their own Customer row via User.CustomerId, so
    // tests can exercise the happy path and cross-customer isolation.
    public (Guid PortalCustomerId, Guid OtherCustomerId) SeedPortalCustomers()
    {
        using var authScope = Services.CreateScope();
        var authDb = authScope.ServiceProvider.GetRequiredService<AuthDbContext>();

        var existingPortalUser = authDb.Users.FirstOrDefault(u => u.Email == PortalCustomerEmail);
        var existingOtherUser = authDb.Users.FirstOrDefault(u => u.Email == OtherPortalCustomerEmail);
        if (existingPortalUser?.CustomerId is not null && existingOtherUser?.CustomerId is not null)
        {
            return (existingPortalUser.CustomerId.Value, existingOtherUser.CustomerId.Value);
        }

        using var customerScope = Services.CreateScope();
        var customerDb = customerScope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        var now = DateTime.UtcNow;

        var portalCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Portal Customer Co",
            Email = "portal-customer-co@example.com",
            CreatedAtUtc = now,
        };
        var otherCustomer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Other Portal Customer Co",
            Email = "other-portal-customer-co@example.com",
            CreatedAtUtc = now,
        };
        customerDb.Customers.AddRange(portalCustomer, otherCustomer);
        customerDb.SaveChanges();

        var hasher = authScope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var portalUser = new User
        {
            Id = Guid.NewGuid(),
            Email = PortalCustomerEmail,
            Name = "Portal Customer User",
            IsActive = true,
            Roles = [Roles.Customer],
            CustomerId = portalCustomer.Id,
        };
        portalUser.PasswordHash = hasher.HashPassword(portalUser, PortalCustomerPassword);

        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = OtherPortalCustomerEmail,
            Name = "Other Portal Customer User",
            IsActive = true,
            Roles = [Roles.Customer],
            CustomerId = otherCustomer.Id,
        };
        otherUser.PasswordHash = hasher.HashPassword(otherUser, OtherPortalCustomerPassword);

        authDb.Users.AddRange(portalUser, otherUser);
        authDb.SaveChanges();

        return (portalCustomer.Id, otherCustomer.Id);
    }

    public void SeedCustomers()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
        if (db.Customers.Any())
        {
            return;
        }

        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        db.Customers.AddRange(
            new Customer
            {
                Id = Guid.NewGuid(),
                FullName = "Alice Johnson",
                Email = "alice.johnson@example.com",
                Phone = "+1-555-0101",
                Company = "Acme Corp",
                CreatedAtUtc = baseTime,
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                FullName = "Bob Martinez",
                Email = "bob.martinez@example.com",
                Phone = "+1-555-0102",
                Company = "Globex",
                CreatedAtUtc = baseTime.AddDays(1),
            },
            new Customer
            {
                Id = Guid.NewGuid(),
                FullName = "100% Discount Co",
                Email = "wildcard@example.com",
                Phone = null,
                Company = "Under_score Inc",
                CreatedAtUtc = baseTime.AddDays(2),
            });
        db.SaveChanges();
    }
}
