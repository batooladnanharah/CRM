using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.CustomerPortal;
using CRM.Api.KnowledgeBase;
using CRM.Api.Tickets;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class CustomerPortalEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _portalCustomerId;
    private readonly Guid _otherCustomerId;

    public CustomerPortalEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        (_portalCustomerId, _otherCustomerId) = factory.SeedPortalCustomers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(
        string email = CustomWebApplicationFactory.PortalCustomerEmail,
        string password = CustomWebApplicationFactory.PortalCustomerPassword)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateTicketForCustomer(
        Guid customerId, string title = "Sample ticket", TicketStatus status = TicketStatus.Open)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = "Sample description",
            Status = status,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    private void AddMessage(Guid ticketId, string body, bool isInternal)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.TicketMessages.Add(new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorUserId = Guid.NewGuid(),
            Body = body,
            IsInternal = isInternal,
            CreatedAtUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    private void SetTicketStatus(Guid ticketId, TicketStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = db.Tickets.First(t => t.Id == ticketId);
        ticket.Status = status;
        db.SaveChanges();
    }

    private void SetTicketAssignee(Guid ticketId, Guid assigneeUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = db.Tickets.First(t => t.Id == ticketId);
        ticket.AssigneeUserId = assigneeUserId;
        db.SaveChanges();
    }

    private void AddHistoryEntry(Guid ticketId, TicketChangeType changeType, string? oldValue, string? newValue)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        db.TicketHistory.Add(new TicketHistoryEntry
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            ChangeType = changeType,
            OldValue = oldValue,
            NewValue = newValue,
            ChangedByUserId = Guid.NewGuid(),
            ChangedAtUtc = DateTime.UtcNow,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_Tickets_ReturnsOnlyOwnTickets()
    {
        var client = await AuthenticatedClientAsync();
        CreateTicketForCustomer(_portalCustomerId, "My Ticket");
        CreateTicketForCustomer(_otherCustomerId, "Not My Ticket");

        var response = await client.GetAsync("/api/customer/tickets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<CustomerTicketListItemResponse>>();
        Assert.Contains(items!, t => t.Title == "My Ticket");
        Assert.DoesNotContain(items!, t => t.Title == "Not My Ticket");
    }

    [Fact]
    public async Task Get_TicketById_ReturnsOwnTicket()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Details Ticket");

        var response = await client.GetAsync($"/api/customer/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();
        Assert.Equal("Details Ticket", body!.Title);
    }

    [Fact]
    public async Task Get_TicketById_Returns404_ForAnotherCustomersTicket()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_otherCustomerId, "Not Mine");

        var response = await client.GetAsync($"/api/customer/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_TicketById_Returns404_ForNonExistentTicket()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/customer/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_Tickets_IgnoresBodyCustomerId_AndUsesAuthenticatedIdentity()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/customer/tickets", new
        {
            title = "New Portal Ticket",
            description = "Something is broken.",
            customerId = _otherCustomerId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();

        var ownList = await client.GetAsync("/api/customer/tickets");
        var ownItems = await ownList.Content.ReadFromJsonAsync<List<CustomerTicketListItemResponse>>();
        Assert.Contains(ownItems!, t => t.Id == body!.Id);
    }

    [Fact]
    public async Task Post_Tickets_PersistsTicketWithTitleAndDescription()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/customer/tickets", new
        {
            title = "Persisted Ticket",
            description = "Full description here.",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();
        Assert.Equal("Persisted Ticket", body!.Title);
        Assert.Equal("Full description here.", body.Description);
        Assert.Equal(TicketStatus.Open, body.Status);

        var refetch = await client.GetAsync($"/api/customer/tickets/{body.Id}");
        Assert.Equal(HttpStatusCode.OK, refetch.StatusCode);
    }

    [Fact]
    public async Task Get_Tickets_Returns403_WhenUserHasNoCustomerLink()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);

        var response = await client.GetAsync("/api/customer/tickets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AgentToken_Cannot_Access_CustomerPortal()
    {
        var client = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await client.GetAsync("/api/customer/tickets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CustomerToken_Cannot_Access_InternalTicketsApi()
    {
        var client = await AuthenticatedClientAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/tickets")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync(
                "/api/tickets", new { customerId = _portalCustomerId, title = "T", description = "D" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden, (await client.GetAsync($"/api/tickets/{Guid.NewGuid()}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden, (await client.GetAsync($"/api/tickets/{Guid.NewGuid()}/history")).StatusCode);
    }

    [Fact]
    public async Task TicketDetailsResponse_Excludes_InternalMessages_And_NonStatusHistory()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Filtered Ticket");
        AddMessage(ticketId, "Public reply to the customer.", isInternal: false);
        AddMessage(ticketId, "Internal note not for the customer.", isInternal: true);
        AddHistoryEntry(ticketId, TicketChangeType.Status, "Open", "InProgress");
        AddHistoryEntry(ticketId, TicketChangeType.Assignment, null, Guid.NewGuid().ToString());

        var response = await client.GetAsync($"/api/customer/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();
        Assert.Single(body!.Messages);
        Assert.Equal("Public reply to the customer.", body.Messages[0].Body);
        Assert.Single(body.History);
        Assert.Equal("InProgress", body.History[0].NewValue);
    }

    [Fact]
    public async Task GetTicketDetails_ReturnsCustomerSafeDto_WithNoInternalFields()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Safe DTO Ticket");

        var response = await client.GetAsync($"/api/customer/tickets/{ticketId}");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // The DTO type itself never carries these fields, but this also
        // guards the wire format directly in case a future edit widens the
        // record — see the "Customer-safe DTO" comment on
        // CustomerTicketDetailsResponse in CustomerPortalContracts.cs.
        Assert.DoesNotContain("assigneeUserId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("slaPolicyId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("autoAssigned", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("internalNotes", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTicketDetails_RequiresCustomerAuth()
    {
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Anon Ticket");

        var response = await _client.GetAsync($"/api/customer/tickets/{ticketId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostMessage_CreatesMessage_ForOwnTicket_AsCustomerSender()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Reply Ticket");

        var response = await client.PostAsJsonAsync(
            $"/api/customer/tickets/{ticketId}/messages", new { body = "I need an update please." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerTicketMessageResponse>();
        Assert.Equal("Customer", body!.SenderType);
        Assert.Equal("I need an update please.", body.Body);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var entity = db.TicketMessages.Single(m => m.Id == body.Id);
        Assert.Equal(_portalCustomerId, entity.AuthorCustomerId);
        Assert.Null(entity.AuthorUserId);
        Assert.False(entity.IsInternal);
    }

    [Fact]
    public async Task PostMessage_IgnoresClientSuppliedSender()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Ignores Sender Ticket");

        var response = await client.PostAsJsonAsync($"/api/customer/tickets/{ticketId}/messages", new
        {
            body = "Ignore whatever sender I claim to be.",
            senderCustomerId = _otherCustomerId,
            authorCustomerId = _otherCustomerId,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CustomerTicketMessageResponse>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var entity = db.TicketMessages.Single(m => m.Id == created!.Id);
        Assert.Equal(_portalCustomerId, entity.AuthorCustomerId);
        Assert.NotEqual(_otherCustomerId, entity.AuthorCustomerId);
    }

    [Fact]
    public async Task PostMessage_ReturnsNotFound_ForOtherCustomersTicket()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_otherCustomerId, "Not Mine For Reply");

        var response = await client.PostAsJsonAsync(
            $"/api/customer/tickets/{ticketId}/messages", new { body = "Trying to reply anyway." });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMessage_RejectsEmptyContent()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Empty Body Ticket");

        var response = await client.PostAsJsonAsync(
            $"/api/customer/tickets/{ticketId}/messages", new { body = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMessage_RejectsContentOverMaxLength()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Too Long Ticket");

        var response = await client.PostAsJsonAsync(
            $"/api/customer/tickets/{ticketId}/messages", new { body = new string('a', 5001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMessage_OnClosedTicket_ReturnsConflict_AndCreatesNoMessage()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Closed Ticket", TicketStatus.Closed);

        var response = await client.PostAsJsonAsync(
            $"/api/customer/tickets/{ticketId}/messages", new { body = "Please reopen." });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        Assert.Empty(db.TicketMessages.Where(m => m.TicketId == ticketId));
    }

    // TicketStatusRules allows Resolved -> Open (the only non-terminal
    // transition it defines out of Resolved); CustomerPortalEndpoints takes
    // that as "a customer reply reopens a resolved ticket" rather than
    // rejecting it — see the comment on the POST /messages handler.
    [Fact]
    public async Task PostMessage_OnResolvedTicket_ReopensTicketAndCreatesMessage()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Resolved Ticket", TicketStatus.Resolved);

        var response = await client.PostAsJsonAsync(
            $"/api/customer/tickets/{ticketId}/messages", new { body = "Actually, still broken." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var ticket = db.Tickets.Single(t => t.Id == ticketId);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Single(db.TicketMessages.Where(m => m.TicketId == ticketId));
    }

    [Fact]
    public async Task PostMessage_RequiresCustomerAuth()
    {
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Anon Reply Ticket");

        var response = await _client.PostAsJsonAsync(
            $"/api/customer/tickets/{ticketId}/messages", new { body = "Anonymous attempt." });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTicketDetails_And_PostMessage_RoundTrip_NewReplyAppearsInSubsequentGet()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Round Trip Ticket");

        var initialGet = await client.GetAsync($"/api/customer/tickets/{ticketId}");
        var initialBody = await initialGet.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();
        Assert.Empty(initialBody!.Messages);

        var post = await client.PostAsJsonAsync(
            $"/api/customer/tickets/{ticketId}/messages", new { body = "Following up on this." });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var followUpGet = await client.GetAsync($"/api/customer/tickets/{ticketId}");
        var followUpBody = await followUpGet.Content.ReadFromJsonAsync<CustomerTicketDetailsResponse>();
        Assert.Single(followUpBody!.Messages);
        Assert.Equal("Following up on this.", followUpBody.Messages[0].Body);
        Assert.Equal("Customer", followUpBody.Messages[0].SenderType);
    }

    [Fact]
    public async Task PostMessage_NotifiesAssignedAgent()
    {
        var client = await AuthenticatedClientAsync();
        var ticketId = CreateTicketForCustomer(_portalCustomerId, "Notify Agent Ticket");

        using var setupScope = _factory.Services.CreateScope();
        var authDb = setupScope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var agent = authDb.Users.First(u => u.Email == CustomWebApplicationFactory.ActiveEmail);
        SetTicketAssignee(ticketId, agent.Id);

        var response = await client.PostAsJsonAsync(
            $"/api/customer/tickets/{ticketId}/messages", new { body = "Please look into this." });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var notificationsDb = scope.ServiceProvider.GetRequiredService<CRM.Api.Notifications.NotificationsDbContext>();
        var notification = notificationsDb.Notifications.SingleOrDefault(
            n => n.UserId == agent.Id && n.TicketId == ticketId);
        Assert.NotNull(notification);
        Assert.Equal(CRM.Api.Notifications.NotificationType.CustomerReplied, notification!.Type);
    }

    private Guid EnsureDefaultCategory()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseDbContext>();
        var existing = db.Categories.FirstOrDefault(c => c.Name == "Portal Test Category");
        if (existing is not null)
        {
            return existing.Id;
        }

        var now = DateTime.UtcNow;
        var category = new KnowledgeBaseCategory
        {
            Id = Guid.NewGuid(),
            Name = "Portal Test Category",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.Categories.Add(category);
        db.SaveChanges();
        return category.Id;
    }

    private Guid CreateArticle(
        string title, string slug, KnowledgeBaseArticleStatus status, string body = "Some help content.")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseDbContext>();
        var now = DateTime.UtcNow;
        var entity = new KnowledgeBaseArticle
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Body = body,
            Tags = [],
            Status = status,
            AuthorId = Guid.NewGuid(),
            CategoryId = EnsureDefaultCategory(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PublishedAtUtc = status == KnowledgeBaseArticleStatus.Published ? now : null,
        };
        db.Articles.Add(entity);
        db.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Portal_ListArticles_ReturnsOnlyPublished()
    {
        var client = await AuthenticatedClientAsync();
        CreateArticle("Published Help", "published-help-list", KnowledgeBaseArticleStatus.Published);
        CreateArticle("Draft Help", "draft-help-list", KnowledgeBaseArticleStatus.Draft);
        CreateArticle("Archived Help", "archived-help-list", KnowledgeBaseArticleStatus.Archived);

        var response = await client.GetAsync("/api/customer/knowledge-base/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerKnowledgeBaseArticleListResponse>();
        Assert.Contains(body!.Items, a => a.Title == "Published Help");
        Assert.DoesNotContain(body.Items, a => a.Title == "Draft Help");
        Assert.DoesNotContain(body.Items, a => a.Title == "Archived Help");
    }

    [Fact]
    public async Task Portal_GetArticle_ReturnsPublished()
    {
        var client = await AuthenticatedClientAsync();
        var id = CreateArticle(
            "Getting Started", "getting-started-portal", KnowledgeBaseArticleStatus.Published, "Full body text.");

        var response = await client.GetAsync($"/api/customer/knowledge-base/articles/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerKnowledgeBaseArticleDetailsResponse>();
        Assert.Equal("Getting Started", body!.Title);
        Assert.Equal("Full body text.", body.Body);
    }

    [Fact]
    public async Task Portal_GetArticle_ReturnsNotFoundForDraft()
    {
        var client = await AuthenticatedClientAsync();
        var id = CreateArticle("Draft Article", "draft-article-portal", KnowledgeBaseArticleStatus.Draft);

        var response = await client.GetAsync($"/api/customer/knowledge-base/articles/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Portal_GetArticle_ReturnsNotFoundForArchived()
    {
        var client = await AuthenticatedClientAsync();
        var id = CreateArticle("Archived Article", "archived-article-portal", KnowledgeBaseArticleStatus.Archived);

        var response = await client.GetAsync($"/api/customer/knowledge-base/articles/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Portal_GetArticle_ReturnsNotFoundForMissingId()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/customer/knowledge-base/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

// Isolated from CustomerPortalEndpointsTests: dashboard counts are exact
// totals across all of the current customer's tickets, so this must not
// share a fixture/DB with tests that create their own tickets for the same
// seeded portal customer (the well-known shared-fixture isolation issue).
public class CustomerPortalDashboardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _portalCustomerId;
    private readonly Guid _otherCustomerId;

    public CustomerPortalDashboardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        (_portalCustomerId, _otherCustomerId) = factory.SeedPortalCustomers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.PortalCustomerEmail,
            password = CustomWebApplicationFactory.PortalCustomerPassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid CreateTicketForCustomer(Guid customerId, string title, TicketStatus status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TicketDbContext>();
        var now = DateTime.UtcNow;
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Title = title,
            Description = "Sample description",
            Status = status,
            Priority = TicketPriority.Normal,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.Tickets.Add(ticket);
        db.SaveChanges();
        return ticket.Id;
    }

    [Fact]
    public async Task Get_Dashboard_ReturnsCountsForCurrentCustomerOnly()
    {
        var client = await AuthenticatedClientAsync();
        CreateTicketForCustomer(_portalCustomerId, "Own Open", TicketStatus.Open);
        CreateTicketForCustomer(_portalCustomerId, "Own Pending", TicketStatus.InProgress);
        CreateTicketForCustomer(_portalCustomerId, "Own Resolved", TicketStatus.Resolved);
        CreateTicketForCustomer(_otherCustomerId, "Someone Else's Open", TicketStatus.Open);

        var response = await client.GetAsync("/api/customer/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerDashboardResponse>();
        Assert.Equal(1, body!.OpenCount);
        Assert.Equal(1, body.PendingCount);
        Assert.Equal(1, body.ResolvedCount);
        Assert.All(body.RecentTickets, t => Assert.DoesNotContain("Someone Else's", t.Title));
    }
}

// Isolated from the other CustomerPortal test classes for the same reason as
// CustomerPortalDashboardTests: articles created here don't interact with
// ticket data, but keeping this KB coverage in its own fixture-scoped class
// avoids any future cross-test coupling as the portal KB surface grows.
public class CustomerPortalKnowledgeBaseEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CustomerPortalKnowledgeBaseEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        factory.SeedPortalCustomers();
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.PortalCustomerEmail,
            password = CustomWebApplicationFactory.PortalCustomerPassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private Guid SeedCategory(string name, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseDbContext>();
        var now = DateTime.UtcNow;
        var category = new KnowledgeBaseCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.Categories.Add(category);
        db.SaveChanges();
        return category.Id;
    }

    private Guid SeedArticle(
        string title, string slug, KnowledgeBaseArticleStatus status, string body = "Article body.",
        Guid? categoryId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseDbContext>();
        var now = DateTime.UtcNow;
        var article = new KnowledgeBaseArticle
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            Body = body,
            Tags = [],
            Status = status,
            AuthorId = Guid.NewGuid(),
            CategoryId = categoryId ?? SeedCategory($"Category for {slug}"),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            PublishedAtUtc = status == KnowledgeBaseArticleStatus.Published ? now : null,
        };
        db.Articles.Add(article);
        db.SaveChanges();
        return article.Id;
    }

    [Fact]
    public async Task ListArticles_ReturnsOnlyPublished()
    {
        var client = await AuthenticatedClientAsync();
        SeedArticle("Published FAQ", "portal-published-faq", KnowledgeBaseArticleStatus.Published);
        SeedArticle("Draft FAQ", "portal-draft-faq", KnowledgeBaseArticleStatus.Draft);
        SeedArticle("Archived FAQ", "portal-archived-faq", KnowledgeBaseArticleStatus.Archived);

        var response = await client.GetAsync("/api/customer/knowledge-base/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CustomerKnowledgeBaseArticleListResponse>();
        Assert.Contains(result!.Items, a => a.Slug == "portal-published-faq");
        Assert.DoesNotContain(result.Items, a => a.Slug == "portal-draft-faq");
        Assert.DoesNotContain(result.Items, a => a.Slug == "portal-archived-faq");
    }

    [Fact]
    public async Task GetArticle_ReturnsPublishedArticle()
    {
        var client = await AuthenticatedClientAsync();
        var id = SeedArticle(
            "Getting Started", "portal-getting-started", KnowledgeBaseArticleStatus.Published, "Full body text.");

        var response = await client.GetAsync($"/api/customer/knowledge-base/articles/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerKnowledgeBaseArticleDetailsResponse>();
        Assert.Equal("Getting Started", body!.Title);
        Assert.Equal("Full body text.", body.Body);
    }

    [Fact]
    public async Task GetArticle_Returns404_ForDraftArticle()
    {
        var client = await AuthenticatedClientAsync();
        var id = SeedArticle("Draft Only", "portal-draft-only", KnowledgeBaseArticleStatus.Draft);

        var response = await client.GetAsync($"/api/customer/knowledge-base/articles/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetArticle_Returns404_ForArchivedArticle()
    {
        var client = await AuthenticatedClientAsync();
        var id = SeedArticle("Archived Only", "portal-archived-only", KnowledgeBaseArticleStatus.Archived);

        var response = await client.GetAsync($"/api/customer/knowledge-base/articles/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetArticle_Returns404_ForMissingArticle_SameAsDraft()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/customer/knowledge-base/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListArticles_ReturnsUnauthorized_WhenAnonymous()
    {
        var response = await _client.GetAsync("/api/customer/knowledge-base/articles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AgentToken_Cannot_Access_PortalKnowledgeBase()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = CustomWebApplicationFactory.ActivePassword,
        });
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.Token);

        var response = await client.GetAsync("/api/customer/knowledge-base/articles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListCategories_ReturnsOnlyActiveCategoriesWithPublishedArticles()
    {
        var client = await AuthenticatedClientAsync();

        var withPublished = SeedCategory("Has Published Article");
        SeedArticle("Has Published", "cat-has-published", KnowledgeBaseArticleStatus.Published, categoryId: withPublished);

        var withOnlyDraft = SeedCategory("Has Only Draft");
        SeedArticle("Draft Only Cat", "cat-only-draft", KnowledgeBaseArticleStatus.Draft, categoryId: withOnlyDraft);

        var inactiveWithPublished = SeedCategory("Inactive With Published", isActive: false);
        SeedArticle(
            "Inactive Cat Published", "cat-inactive-published", KnowledgeBaseArticleStatus.Published,
            categoryId: inactiveWithPublished);

        var emptyActive = SeedCategory("Empty Active Category");

        var response = await client.GetAsync("/api/customer/knowledge-base/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<CustomerKnowledgeBaseCategoryResponse>>();
        Assert.Contains(body!, c => c.Id == withPublished && c.ArticleCount == 1);
        Assert.DoesNotContain(body, c => c.Id == withOnlyDraft);
        Assert.DoesNotContain(body, c => c.Id == inactiveWithPublished);
        Assert.DoesNotContain(body, c => c.Id == emptyActive);
    }

    [Fact]
    public async Task ListCategoryArticles_ReturnsOnlyPublishedArticlesInThatCategory()
    {
        var client = await AuthenticatedClientAsync();
        var categoryId = SeedCategory("Category Articles Filter");
        SeedArticle(
            "Published In Category", "cat-articles-published", KnowledgeBaseArticleStatus.Published,
            categoryId: categoryId);
        SeedArticle(
            "Draft In Category", "cat-articles-draft", KnowledgeBaseArticleStatus.Draft, categoryId: categoryId);
        var otherCategoryId = SeedCategory("Other Category For Filter");
        SeedArticle(
            "Published Elsewhere", "cat-articles-elsewhere", KnowledgeBaseArticleStatus.Published,
            categoryId: otherCategoryId);

        var response = await client.GetAsync($"/api/customer/knowledge-base/categories/{categoryId}/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CustomerKnowledgeBaseArticleListResponse>();
        Assert.Contains(body!.Items, a => a.Slug == "cat-articles-published");
        Assert.DoesNotContain(body.Items, a => a.Slug == "cat-articles-draft");
        Assert.DoesNotContain(body.Items, a => a.Slug == "cat-articles-elsewhere");
    }

    [Fact]
    public async Task ListCategoryArticles_Returns404_WhenCategoryMissing()
    {
        var client = await AuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/customer/knowledge-base/categories/{Guid.NewGuid()}/articles");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListCategoryArticles_Returns404_WhenCategoryInactive()
    {
        var client = await AuthenticatedClientAsync();
        var inactiveCategoryId = SeedCategory("Inactive For Articles", isActive: false);

        var response = await client.GetAsync($"/api/customer/knowledge-base/categories/{inactiveCategoryId}/articles");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- CRM-66 portal search ---

    [Fact]
    public async Task Search_ReturnsOnlyPublishedArticlesInActiveCategories()
    {
        var client = await AuthenticatedClientAsync();
        var activeCategoryId = SeedCategory("Active Portal Search Category");
        var inactiveCategoryId = SeedCategory("Inactive Portal Search Category", isActive: false);

        SeedArticle(
            "Portal Search Published Zzynth", "portal-search-published", KnowledgeBaseArticleStatus.Published,
            "zzynth body", categoryId: activeCategoryId);
        SeedArticle(
            "Portal Search Draft Zzynth", "portal-search-draft", KnowledgeBaseArticleStatus.Draft,
            "zzynth body", categoryId: activeCategoryId);
        SeedArticle(
            "Portal Search Inactive Category Zzynth", "portal-search-inactive-cat",
            KnowledgeBaseArticleStatus.Published, "zzynth body", categoryId: inactiveCategoryId);

        var response = await client.GetAsync("/api/customer/knowledge-base/search?q=zzynth");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.Contains(body!.Items, i => i.Title == "Portal Search Published Zzynth");
        Assert.DoesNotContain(body.Items, i => i.Title == "Portal Search Draft Zzynth");
        Assert.DoesNotContain(body.Items, i => i.Title == "Portal Search Inactive Category Zzynth");
    }

    [Fact]
    public async Task Search_IgnoresIncludeDraftsFlag()
    {
        var client = await AuthenticatedClientAsync();
        SeedArticle(
            "Portal Ignores Include Drafts", "portal-search-ignores-include-drafts",
            KnowledgeBaseArticleStatus.Draft, "unique-portal-includedrafts-term body");

        var response = await client.GetAsync(
            "/api/customer/knowledge-base/search?q=unique-portal-includedrafts-term&includeDrafts=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.DoesNotContain(body!.Items, i => i.Title == "Portal Ignores Include Drafts");
    }

    [Fact]
    public async Task Search_StatusFieldIsNull()
    {
        var client = await AuthenticatedClientAsync();
        SeedArticle(
            "Portal Search Status Null", "portal-search-status-null", KnowledgeBaseArticleStatus.Published,
            "unique-portal-status-null-term body");

        var response = await client.GetAsync("/api/customer/knowledge-base/search?q=unique-portal-status-null-term");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        var item = Assert.Single(body!.Items, i => i.Title == "Portal Search Status Null");
        Assert.Null(item.Status);
    }

    [Fact]
    public async Task Search_UnauthenticatedCallerIsRejected()
    {
        var response = await _client.GetAsync("/api/customer/knowledge-base/search?q=anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
