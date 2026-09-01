using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using CRM.Api.Auth;
using CRM.Api.Customers;
using CRM.Api.Ai;
using CRM.Api.CommunicationChannels;
using CRM.Api.CustomerPortal;
using CRM.Api.Customers.Attachments;
using CRM.Api.Email;
using CRM.Api.KnowledgeBase;
using CRM.Api.Notifications;
using CRM.Api.QuickReplies;
using CRM.Api.Reports;
using CRM.Api.Security;
using CRM.Api.Sla;
using CRM.Api.Tickets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "Jwt:Key is not configured. Set it via `dotnet user-secrets set \"Jwt:Key\" \"<32+ char secret>\"` in Development, " +
        "or via the Jwt__Key environment variable in other environments.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "crm-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "crm-web";

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CrmDb")));
builder.Services.AddDbContext<CustomerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CrmDb")));
builder.Services.AddDbContext<TicketDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CrmDb")));
builder.Services.AddDbContext<QuickReplyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CrmDb")));
builder.Services.AddDbContext<CommunicationChannelsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CrmDb")));
builder.Services.AddDbContext<KnowledgeBaseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CrmDb")));
builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CrmDb")));

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<ISlaService, SlaService>();
builder.Services.AddScoped<TicketEscalationService>();
builder.Services.AddScoped<ISlaEvaluator, SlaEvaluator>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IManagerResolver, ManagerResolver>();
builder.Services.AddScoped<IEscalationDispatcher, EscalationDispatcher>();
builder.Services.AddScoped<TicketCreationService>();
builder.Services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();
builder.Services.AddScoped<ICurrentCustomerAccessor, CurrentCustomerAccessor>();
builder.Services.AddScoped<ReportsService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

builder.Services.Configure<SlaAutomationOptions>(
    builder.Configuration.GetSection(SlaAutomationOptions.SectionName));
builder.Services.Configure<AutoAssignmentOptions>(
    builder.Configuration.GetSection(AutoAssignmentOptions.SectionName));
if (builder.Configuration.GetValue("Sla:Enabled", true))
{
    builder.Services.AddHostedService<SlaAutomationHostedService>();
}

// Shared by both customer and ticket attachments: same physical storage root,
// same size/type limits. Ticket attachment keys are prefixed "tickets/{ticketId}/"
// (see TicketAttachmentEndpoints.cs) so they never collide with customer
// attachment keys, which are prefixed by a bare customerId.
builder.Services.Configure<AttachmentsOptions>(builder.Configuration.GetSection(AttachmentsOptions.SectionName));
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
var emailProvider = builder.Configuration["Email:Provider"] ?? "Development";
if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, DevelopmentEmailService>();
}

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
var aiEnabled = builder.Configuration.GetValue("AI:Enabled", false);
var aiProvider = builder.Configuration["AI:Provider"];
if (aiEnabled && string.IsNullOrWhiteSpace(aiProvider))
{
    // Must not throw — the application always has to start. Fall back to the
    // safe, no-external-dependency development provider.
    Console.Error.WriteLine(
        "Warning: AI:Enabled is true but AI:Provider is not set; falling back to the Development provider.");
    builder.Services.AddSingleton<IAiService, DevelopmentAiService>();
}
else if (aiEnabled && !string.Equals(aiProvider, "Development", StringComparison.OrdinalIgnoreCase))
{
    // No real provider SDK exists yet — report the configured name so /api/ai/status
    // is honest about what was requested, but never attempt to call it.
    Console.Error.WriteLine(
        $"Warning: AI provider '{aiProvider}' is configured but no implementation is available; " +
        "falling back to Development provider with IsAvailable=false.");
    builder.Services.AddSingleton<IAiService>(new UnimplementedProviderAiService(aiProvider!));
}
else
{
    builder.Services.AddSingleton<IAiService, DevelopmentAiService>();
}
builder.Services.AddScoped<ITicketAiContextBuilder, TicketAiContextBuilder>();
builder.Services.AddScoped<AiApplicationService>();

// Kestrel's default MaxRequestBodySize (30MB) already comfortably covers the
// configured attachment size limit plus multipart overhead; raise it only if
// a deployment configures a larger MaxFileSizeBytes than that default allows.
var maxAttachmentBytes = builder.Configuration.GetValue<long?>("Attachments:MaxFileSizeBytes");
if (maxAttachmentBytes is > 0)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        var withOverhead = maxAttachmentBytes.Value + 1024 * 1024;
        if (options.Limits.MaxRequestBodySize is null || options.Limits.MaxRequestBodySize < withOverhead)
        {
            options.Limits.MaxRequestBodySize = withOverhead;
        }
    });
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// Named role policies for minimal-API routes. No admin/agent-only business
// endpoint exists yet; future stories gate their routes with
// .RequireAuthorization("AdminOnly") / .RequireAuthorization("AgentOrAdmin").
// A bare .RequireAuthorization() (no policy name) remains "any authenticated user".
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole(Roles.Admin));
    options.AddPolicy("AgentOrAdmin", p => p.RequireRole(Roles.Admin, Roles.Agent));
    options.AddPolicy("CustomerPortal", p => p.RequireRole(Roles.Customer));

    // Named permission policies (RBAC — CRM-81). One policy per Permissions.All
    // entry; a request must carry a "permission" claim (issued per the user's
    // role via RolePermissions — see JwtTokenService.cs) with that exact value.
    foreach (var permission in Permissions.All)
    {
        options.AddPolicy(permission, p => p.RequireClaim("permission", permission));
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Also exposed in "Testing" (the environment CustomWebApplicationFactory
// uses) so ExternalApiIntegrationTests can assert the document is served —
// the schema carries no data, only route/type metadata, so this is safe to
// widen beyond Development. Still gated off Production.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

// Registered before UseAuthorization so its `await next()` wraps the
// authorization + endpoint execution — by the time control returns here,
// Response.StatusCode already reflects whatever RequireAuthorization decided.
// Only 403 is audited (401 is unauthenticated noise from crawlers/expired
// tokens, not a meaningful access-denied event). RBAC (CRM-81) broadened this
// from /api/admin-only to every route, since permission policies now protect
// most of the API surface, not just the admin group. Reuses the existing
// AuditActions.AccessDenied constant rather than adding a duplicate
// "PermissionDenied" constant for the same event.
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
    {
        // ActorUserId/ActorEmail are already captured by AuditLogger itself
        // from the ambient HttpContext — only the route-specific detail
        // (method) needs to be passed explicitly here.
        var auditLogger = context.RequestServices.GetRequiredService<IAuditLogger>();
        await auditLogger.WriteAsync(
            AuditActions.AccessDenied, targetType: "route", targetId: context.Request.Path,
            payload: new { method = context.Request.Method }, ct: context.RequestAborted);
    }
});

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    db.Database.Migrate();

    if (!db.Users.Any())
    {
        var seedPassword = app.Configuration["Seed:AgentPassword"];
        if (string.IsNullOrWhiteSpace(seedPassword))
        {
            app.Logger.LogWarning(
                "Skipping development seed user: set \"Seed:AgentPassword\" via `dotnet user-secrets` to seed agent@crm.local.");
        }
        else
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var seedUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "agent@crm.local",
                Name = "Demo Agent",
                Roles = ["agent"],
                IsActive = true,
            };
            seedUser.PasswordHash = hasher.HashPassword(seedUser, seedPassword);
            db.Users.Add(seedUser);
            db.SaveChanges();
        }
    }

    if (!db.Users.Any(u => u.Roles.Contains(Roles.Admin)))
    {
        var adminEmail = app.Configuration["Seed:AdminEmail"];
        var adminPassword = app.Configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            app.Logger.LogWarning(
                "Skipping development seed admin: set \"Seed:AdminEmail\" and \"Seed:AdminPassword\" via `dotnet user-secrets` to seed a default admin user.");
        }
        else
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Email = adminEmail.Trim().ToLowerInvariant(),
                Name = "Default Admin",
                Roles = [Roles.Admin],
                IsActive = true,
            };
            adminUser.PasswordHash = hasher.HashPassword(adminUser, adminPassword);
            db.Users.Add(adminUser);
            db.SaveChanges();
        }
    }

    var customerDb = scope.ServiceProvider.GetRequiredService<CustomerDbContext>();
    customerDb.Database.Migrate();

    if (!customerDb.Customers.Any())
    {
        var now = DateTime.UtcNow;
        customerDb.Customers.AddRange(
            new Customer { Id = Guid.NewGuid(), FullName = "Alice Johnson", Email = "alice.johnson@example.com", Phone = "+1-555-0101", Company = "Acme Corp", CreatedAtUtc = now },
            new Customer { Id = Guid.NewGuid(), FullName = "Bob Martinez", Email = "bob.martinez@example.com", Phone = "+1-555-0102", Company = "Globex", CreatedAtUtc = now },
            new Customer { Id = Guid.NewGuid(), FullName = "Carla Nguyen", Email = "carla.nguyen@example.com", Phone = "+1-555-0103", Company = "Initech", CreatedAtUtc = now },
            new Customer { Id = Guid.NewGuid(), FullName = "David Smith", Email = "david.smith@example.com", Phone = "+1-555-0104", Company = "Umbrella", CreatedAtUtc = now },
            new Customer { Id = Guid.NewGuid(), FullName = "Elena Petrova", Email = "elena.petrova@example.com", Phone = "+1-555-0105", Company = "Soylent", CreatedAtUtc = now });
        customerDb.SaveChanges();
    }

    if (!customerDb.CustomerInteractions.Any())
    {
        var now = DateTime.UtcNow;
        foreach (var customer in customerDb.Customers)
        {
            customerDb.CustomerInteractions.AddRange(
                new CustomerInteraction
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Type = CustomerInteractionType.TicketCreated,
                    Summary = "Ticket created: \"Cannot access account\"",
                    OccurredAt = now.AddDays(-4),
                    ActorName = "Demo Agent",
                    CreatedAtUtc = now,
                },
                new CustomerInteraction
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Type = CustomerInteractionType.CustomerMessage,
                    Summary = "Customer replied with account details.",
                    OccurredAt = now.AddDays(-3),
                    ActorName = customer.FullName,
                    CreatedAtUtc = now,
                },
                new CustomerInteraction
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Type = CustomerInteractionType.AgentReply,
                    Summary = "Agent reset the password and confirmed access.",
                    OccurredAt = now.AddDays(-2),
                    ActorName = "Demo Agent",
                    CreatedAtUtc = now,
                },
                new CustomerInteraction
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Type = CustomerInteractionType.Email,
                    Summary = "Follow-up satisfaction survey sent.",
                    OccurredAt = now.AddDays(-1),
                    CreatedAtUtc = now,
                });
        }
        customerDb.SaveChanges();
    }

    var ticketDb = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
    ticketDb.Database.Migrate();

    if (!ticketDb.EscalationRules.Any())
    {
        var now = DateTimeOffset.UtcNow;
        ticketDb.EscalationRules.AddRange(
            new EscalationRule
            {
                Id = Guid.NewGuid(),
                Name = "Notify agent when at risk",
                Trigger = EscalationTrigger.AtRisk,
                IsActive = true,
                NotifyAgent = true,
                NotifyManager = false,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new EscalationRule
            {
                Id = Guid.NewGuid(),
                Name = "Notify agent and manager on breach",
                Trigger = EscalationTrigger.Breached,
                IsActive = true,
                NotifyAgent = true,
                NotifyManager = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        ticketDb.SaveChanges();
    }

    const string SlaSeedPolicyName = "Default (dev seed)";
    var seedPolicy = ticketDb.SlaPolicies.FirstOrDefault(p => p.Name == SlaSeedPolicyName);
    if (seedPolicy is null)
    {
        var nowUtc = DateTime.UtcNow;
        seedPolicy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Name = SlaSeedPolicyName,
            Channel = null,
            Priority = TicketPriority.Normal,
            FirstResponseMinutes = 30,
            ResolutionMinutes = 60,
            IsDefault = !ticketDb.SlaPolicies.Any(),
            IsActive = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
        ticketDb.SlaPolicies.Add(seedPolicy);
        ticketDb.SaveChanges();
    }

    const string SlaSmokeTestTicketTitle = "Cannot access dashboard (SLA smoke-test seed)";
    if (!ticketDb.Tickets.Any(t => t.Title == SlaSmokeTestTicketTitle))
    {
        var seedAgent = db.Users.FirstOrDefault(u => u.Roles.Contains(Roles.Agent));
        var seedCustomer = customerDb.Customers.FirstOrDefault();
        if (seedAgent is not null && seedCustomer is not null)
        {
            // Created far enough in the past that both the first-response and
            // resolution windows are already overdue — the SLA worker's very
            // first tick after startup will observe a Breached status for
            // this ticket and fire the seeded escalation rules immediately,
            // rather than requiring the smoke tester to wait out a real SLA
            // window.
            var createdAtUtc = DateTime.UtcNow.AddMinutes(-120);
            ticketDb.Tickets.Add(new Ticket
            {
                Id = Guid.NewGuid(),
                CustomerId = seedCustomer.Id,
                Title = SlaSmokeTestTicketTitle,
                Description = "Seeded ticket already past its SLA window so escalation rules fire on the first worker tick.",
                Status = TicketStatus.Open,
                Priority = TicketPriority.Normal,
                AssigneeUserId = seedAgent.Id,
                CreatedAtUtc = createdAtUtc,
                UpdatedAtUtc = createdAtUtc,
                SlaPolicyId = seedPolicy.Id,
                FirstResponseDueAtUtc = createdAtUtc.AddMinutes(seedPolicy.FirstResponseMinutes),
                ResolutionDueAtUtc = createdAtUtc.AddMinutes(seedPolicy.ResolutionMinutes),
            });
            ticketDb.SaveChanges();
        }
    }

    var quickReplyDb = scope.ServiceProvider.GetRequiredService<QuickReplyDbContext>();
    quickReplyDb.Database.Migrate();

    var communicationChannelsDb = scope.ServiceProvider.GetRequiredService<CommunicationChannelsDbContext>();
    communicationChannelsDb.Database.Migrate();

    var knowledgeBaseDb = scope.ServiceProvider.GetRequiredService<KnowledgeBaseDbContext>();
    knowledgeBaseDb.Database.Migrate();

    var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    notificationsDb.Database.Migrate();

    if (!notificationsDb.Notifications.Any())
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var seededUser in db.Users)
        {
            var isAdmin = seededUser.Roles.Contains(Roles.Admin);
            notificationsDb.Notifications.AddRange(
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = seededUser.Id,
                    Type = NotificationType.SlaAtRisk,
                    Title = "SLA At Risk",
                    Message = isAdmin
                        ? "A ticket assigned in your team is approaching its response SLA."
                        : "A ticket assigned to you is approaching its response SLA.",
                    IsRead = false,
                    CreatedAt = now.AddHours(-2),
                },
                new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = seededUser.Id,
                    Type = NotificationType.SlaBreached,
                    Title = "SLA Breached",
                    Message = isAdmin
                        ? "A ticket in your team has breached its resolution SLA."
                        : "A ticket assigned to you has breached its resolution SLA.",
                    IsRead = true,
                    CreatedAt = now.AddDays(-1),
                    ReadAt = now.AddHours(-20),
                });
        }
        notificationsDb.SaveChanges();
    }
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

var auth = app.MapGroup("/api/auth");

auth.MapPost("/login", async (LoginRequest req, AuthDbContext db,
    IPasswordHasher<User> hasher, JwtTokenService tokens, ILogger<Program> log, IAuditLogger auditLogger) =>
{
    if (string.IsNullOrWhiteSpace(req.Email))
        return Results.BadRequest(new ErrorResponse("Email is required."));
    if (string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new ErrorResponse("Password is required."));
    if (!IsEmail(req.Email))
        return Results.BadRequest(new ErrorResponse("Invalid email format."));

    var email = req.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

    // Single generic failure path — do not leak which check failed to the
    // HTTP response. The audit log is admin-only and may safely record the
    // specific reason (unknown_user / wrong_password / inactive).
    if (user is null || !user.IsActive ||
        hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password)
            == PasswordVerificationResult.Failed)
    {
        log.LogInformation("Failed login for {Email}", email); // never log password
        var reason = user is null ? "unknown_user" : !user.IsActive ? "inactive" : "wrong_password";
        await auditLogger.WriteAsync(
            AuditActions.LoginFailed, targetType: "user", targetId: user?.Id.ToString() ?? email,
            payload: new { email, reason });
        return Results.Json(new ErrorResponse("Invalid email or password."),
            statusCode: StatusCodes.Status401Unauthorized);
    }

    var token = tokens.Issue(user);
    log.LogInformation("Successful login for {UserId}", user.Id);
    await auditLogger.WriteAsync(
        AuditActions.LoginSucceeded, targetType: "user", targetId: user.Id.ToString(), payload: new { email });
    return Results.Ok(new LoginResponse(
        new AuthUserDto(user.Id, user.Name, user.Email, user.Roles, RolePermissions.ForRoles(user.Roles).ToList()),
        token));
})
.AllowAnonymous();

// JWTs are stateless; server-side revocation is out of scope (see follow-up). This endpoint exists for client parity and future audit logging.
auth.MapPost("/logout", () => Results.NoContent())
    .RequireAuthorization()
    .WithName("Logout");

auth.MapGet("/me", (ClaimsPrincipal principal) =>
{
    var id = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var email = principal.FindFirstValue(ClaimTypes.Email)!;
    var name = principal.FindFirstValue("name")!;
    var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    var permissions = principal.FindAll("permission").Select(c => c.Value).ToArray();
    return Results.Ok(new AuthUserDto(id, name, email, roles, permissions));
})
.RequireAuthorization();

app.MapCustomerEndpoints();
app.MapCustomerNoteEndpoints();
app.MapCustomerAttachmentEndpoints();
app.MapTicketEndpoints();
app.MapTicketMessageEndpoints();
app.MapTicketAttachmentEndpoints();
app.MapQuickReplyEndpoints();
app.MapCommunicationChannelEndpoints();
app.MapSlaPolicyEndpoints();
app.MapEscalationRuleEndpoints();
app.MapNotificationEndpoints();
app.MapKnowledgeBaseEndpoints();
app.MapKnowledgeBaseCategoryEndpoints();
app.MapCustomerPortalEndpoints();
app.MapReportsEndpoints();
app.MapSecurityAdminEndpoints();
app.MapAiEndpoints();

app.Run();

static bool IsEmail(string s) => new EmailAddressAttribute().IsValid(s);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
