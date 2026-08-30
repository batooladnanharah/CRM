using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.KnowledgeBase;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class KnowledgeBaseSearchEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _defaultCategoryId;

    public KnowledgeBaseSearchEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
        _defaultCategoryId = SeedCategory("Default Search Category");
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

    private async Task<HttpClient> AuthenticatedClientAsync(
        string email = CustomWebApplicationFactory.AdminEmail,
        string password = CustomWebApplicationFactory.AdminPassword)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private object ArticlePayload(
        string title, string slug, string body = "Article body.", string[]? tags = null, string? status = null,
        Guid? categoryId = null)
        => new
        {
            title, slug, body, tags = tags ?? Array.Empty<string>(), status,
            categoryId = categoryId ?? _defaultCategoryId,
        };

    private async Task<KnowledgeBaseArticleResponse> CreateArticleAsync(HttpClient adminClient, object payload)
    {
        var response = await adminClient.PostAsJsonAsync("/api/knowledge-base/articles", payload);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>())!;
    }

    [Fact]
    public async Task Search_ByTitle_ReturnsMatchingArticle()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(
            admin, ArticlePayload("Resetting Your Password Title", "search-title-match", status: "Published"));

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=Resetting%20Your");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.Contains(result!.Items, i => i.Title == "Resetting Your Password Title");
    }

    [Fact]
    public async Task Search_ByContent_ReturnsMatchingArticle()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(
            admin,
            ArticlePayload(
                "Unrelated Content Title", "search-content-match",
                "The body contains zzyxwquark somewhere in the middle.", status: "Published"));

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=zzyxwquark");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.Contains(result!.Items, i => i.Title == "Unrelated Content Title");
    }

    [Fact]
    public async Task Search_ByCategoryName_ReturnsMatchingArticle()
    {
        var admin = await AuthenticatedClientAsync();
        var categoryId = SeedCategory("Zqxyloop Billing Category");
        await CreateArticleAsync(
            admin,
            ArticlePayload(
                "Nothing Special Title", "search-category-match", "Nothing special body.",
                status: "Published", categoryId: categoryId));

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=Zqxyloop");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.Contains(result!.Items, i => i.Title == "Nothing Special Title");
        Assert.Equal("Zqxyloop Billing Category", result.Items.First(i => i.Title == "Nothing Special Title").Category.Name);
    }

    [Fact]
    public async Task Search_IsCaseInsensitive()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(
            admin, ArticlePayload("Password Reset Case Test", "search-case-insensitive", status: "Published"));

        foreach (var q in new[] { "PASSWORD", "password", "Password" })
        {
            var response = await admin.GetAsync($"/api/knowledge-base/articles/search?q={q}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
            Assert.Contains(result!.Items, i => i.Title == "Password Reset Case Test");
        }
    }

    [Fact]
    public async Task Search_FiltersByCategoryId()
    {
        var admin = await AuthenticatedClientAsync();
        var otherCategoryId = SeedCategory("Other Search Filter Category");
        await CreateArticleAsync(
            admin, ArticlePayload("Filter Match In Default", "search-filter-default", "shared-term-filter body", status: "Published"));
        await CreateArticleAsync(
            admin, ArticlePayload("Filter Match In Other", "search-filter-other", "shared-term-filter body",
                status: "Published", categoryId: otherCategoryId));

        var response = await admin.GetAsync($"/api/knowledge-base/articles/search?q=shared-term-filter&categoryId={otherCategoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.Contains(result!.Items, i => i.Title == "Filter Match In Other");
        Assert.DoesNotContain(result.Items, i => i.Title == "Filter Match In Default");
    }

    [Fact]
    public async Task Search_Paginates_And_ReportsTotalCount()
    {
        var admin = await AuthenticatedClientAsync();
        for (var i = 0; i < 5; i++)
        {
            await CreateArticleAsync(
                admin, ArticlePayload($"Pagination Term Article {i}", $"search-paginate-{i}", status: "Published"));
        }

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=Pagination%20Term&page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.Equal(2, result!.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.True(result.TotalCount >= 5);
    }

    [Fact]
    public async Task Search_RejectsEmptyQuery_With400()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("query_too_short", body!.Message);
    }

    [Fact]
    public async Task Search_RejectsQueryBelowMinimumLength_With400()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=a");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("query_too_short", body!.Message);
    }

    [Fact]
    public async Task Search_RejectsQueryAboveMaximumLength_With400()
    {
        var admin = await AuthenticatedClientAsync();
        var longQuery = new string('a', 201);

        var response = await admin.GetAsync($"/api/knowledge-base/articles/search?q={longQuery}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("query_too_long", body!.Message);
    }

    [Fact]
    public async Task Search_OrdersByTitleThenCategoryThenContentThenPublishedAt()
    {
        var admin = await AuthenticatedClientAsync();
        var term = "rankquark";

        // Content-only match.
        var contentOnly = await CreateArticleAsync(
            admin, ArticlePayload("Content Only Title", "search-rank-content", $"has {term} in the body", status: "Published"));
        // Title match.
        var titleMatch = await CreateArticleAsync(
            admin, ArticlePayload($"Title With {term} In It", "search-rank-title", "plain body", status: "Published"));
        // Category-name match (term appears in category name, not title/content).
        var categoryId = SeedCategory($"Category {term} Name");
        var categoryOnly = await CreateArticleAsync(
            admin, ArticlePayload("Category Only Title", "search-rank-category", "plain body",
                status: "Published", categoryId: categoryId));

        var response = await admin.GetAsync($"/api/knowledge-base/articles/search?q={term}&pageSize=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        var ids = result!.Items.Select(i => i.Id).ToList();

        Assert.True(ids.IndexOf(titleMatch.Id) < ids.IndexOf(categoryOnly.Id));
        Assert.True(ids.IndexOf(categoryOnly.Id) < ids.IndexOf(contentOnly.Id));
    }

    [Fact]
    public async Task Search_AgentWithDraftPermission_ReturnsDrafts_When_IncludeDraftsTrue()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(
            admin, ArticlePayload("Draft Visible To Admin", "search-drafts-admin", "unique-draft-visible-term body"));

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=unique-draft-visible-term&includeDrafts=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.Contains(result!.Items, i => i.Title == "Draft Visible To Admin");
        Assert.Equal("Draft", result.Items.First(i => i.Title == "Draft Visible To Admin").Status);
    }

    [Fact]
    public async Task Search_AgentWithoutDraftPermission_DoesNotReturnDrafts_EvenWhen_IncludeDraftsTrue()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(
            admin, ArticlePayload("Draft Hidden From Agent", "search-drafts-agent", "unique-draft-hidden-term body"));

        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await agent.GetAsync("/api/knowledge-base/articles/search?q=unique-draft-hidden-term&includeDrafts=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.DoesNotContain(result!.Items, i => i.Title == "Draft Hidden From Agent");
    }

    [Fact]
    public async Task Search_EscapesLikeMetaCharacters()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(
            admin, ArticlePayload("Fifty Percent Off Deal", "search-meta-percent", "Get 50% off this week only.", status: "Published"));
        await CreateArticleAsync(
            admin, ArticlePayload("Unrelated Meta Article", "search-meta-unrelated", "No discount mentioned here.", status: "Published"));

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=50%25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.Contains(result!.Items, i => i.Title == "Fifty Percent Off Deal");
        Assert.DoesNotContain(result.Items, i => i.Title == "Unrelated Meta Article");
    }

    [Fact]
    public async Task Search_MatchesArabicQuery()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(
            admin,
            ArticlePayload("إعادة تعيين كلمة المرور", "search-arabic-title", "خطوات إعادة تعيين كلمة المرور بسهولة.",
                status: "Published"));

        var response = await admin.GetAsync(
            "/api/knowledge-base/articles/search?q=" + Uri.EscapeDataString("كلمة المرور"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        Assert.Contains(result!.Items, i => i.Title == "إعادة تعيين كلمة المرور");
    }

    [Fact]
    public async Task Search_NoDuplicateIds_WhenTitleAndContentBothMatch()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(
            admin, ArticlePayload("Duplicate Match Term Title", "search-no-duplicate",
                "Body also mentions duplicate match term.", status: "Published"));

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=duplicate%20match%20term");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResponse>();
        var matchCount = result!.Items.Count(i => i.Id == created.Id);
        Assert.Equal(1, matchCount);
    }
}
