using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.KnowledgeBase;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class KnowledgeBaseEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _defaultCategoryId;

    public KnowledgeBaseEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
        _defaultCategoryId = SeedCategory("Default Category");
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
        return (await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>())!;
    }

    [Fact]
    public async Task Create_ReturnsCreated_AndPersistsArticle()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles",
            ArticlePayload("Resetting Your Password", "resetting-your-password", "Steps to reset your password."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal("Resetting Your Password", body!.Title);
        Assert.Equal("resetting-your-password", body.Slug);
        Assert.Equal(KnowledgeBaseArticleStatus.Draft, body.Status);
        Assert.Null(body.PublishedAtUtc);

        var getResponse = await admin.GetAsync($"/api/knowledge-base/articles/{body.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_SetsPublishedAtUtc_WhenStatusIsPublished()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles",
            ArticlePayload("Published On Create", "published-on-create", status: "Published"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal(KnowledgeBaseArticleStatus.Published, body!.Status);
        Assert.NotNull(body.PublishedAtUtc);
    }

    [Fact]
    public async Task Create_ReturnsConflict_WhenSlugAlreadyExists()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(admin, ArticlePayload("Original Article", "duplicate-slug"));

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles", ArticlePayload("Another Article", "duplicate-slug"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsValidationProblem_WhenSlugFormatInvalid()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles", ArticlePayload("Bad Slug Article", "Not_A_Valid_Slug!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsValidationProblem_WhenTitleEmpty()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles", ArticlePayload("", "empty-title-slug"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns403_ForAgent()
    {
        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);

        var response = await agent.PostAsJsonAsync(
            "/api/knowledge-base/articles", ArticlePayload("Agent Cannot Create", "agent-cannot-create"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_SetsPublishedAtUtc_WhenTransitioningToPublished()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(admin, ArticlePayload("Draft Article", "draft-article"));
        Assert.Null(created.PublishedAtUtc);

        var response = await admin.PutAsJsonAsync(
            $"/api/knowledge-base/articles/{created.Id}",
            ArticlePayload("Draft Article", "draft-article", status: "Published"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal(KnowledgeBaseArticleStatus.Published, body!.Status);
        Assert.NotNull(body.PublishedAtUtc);
    }

    [Fact]
    public async Task Update_KeepsOriginalPublishedAtUtc_WhenTransitioningPublishedToDraft()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(
            admin, ArticlePayload("Publish Then Revert", "publish-then-revert", status: "Published"));
        var originalPublishedAt = created.PublishedAtUtc;
        Assert.NotNull(originalPublishedAt);

        var response = await admin.PutAsJsonAsync(
            $"/api/knowledge-base/articles/{created.Id}",
            ArticlePayload("Publish Then Revert", "publish-then-revert", status: "Draft"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal(KnowledgeBaseArticleStatus.Draft, body!.Status);
        Assert.Equal(originalPublishedAt, body.PublishedAtUtc);
    }

    [Fact]
    public async Task Update_ReturnsConflict_WhenSlugConflictsWithAnotherArticle()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(admin, ArticlePayload("First Article", "first-article-slug"));
        var second = await CreateArticleAsync(admin, ArticlePayload("Second Article", "second-article-slug"));

        var response = await admin.PutAsJsonAsync(
            $"/api/knowledge-base/articles/{second.Id}",
            ArticlePayload("Second Article", "first-article-slug"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PutAsJsonAsync(
            $"/api/knowledge-base/articles/{Guid.NewGuid()}", ArticlePayload("Missing", "missing-slug"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_FiltersByStatusAndTag()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(
            admin, ArticlePayload("Billing FAQ", "billing-faq-list", tags: ["billing"], status: "Published"));
        await CreateArticleAsync(
            admin, ArticlePayload("Shipping FAQ", "shipping-faq-list", tags: ["shipping"], status: "Draft"));

        var response = await admin.GetAsync("/api/knowledge-base/articles?status=Published&tag=billing");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResultResponse>();
        Assert.Contains(result!.Items, a => a.Slug == "billing-faq-list");
        Assert.DoesNotContain(result.Items, a => a.Slug == "shipping-faq-list");
    }

    [Fact]
    public async Task List_PagesResults()
    {
        var admin = await AuthenticatedClientAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateArticleAsync(admin, ArticlePayload($"Paging Article {i}", $"paging-article-{i}"));
        }

        var response = await admin.GetAsync("/api/knowledge-base/articles?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResultResponse>();
        Assert.Equal(2, result!.Items.Count);
        Assert.True(result.Total >= 3);
    }

    [Fact]
    public async Task Search_ReturnsMatches_RankedByTitleFirst()
    {
        var admin = await AuthenticatedClientAsync();
        await CreateArticleAsync(
            admin, ArticlePayload("Unrelated Title", "body-match-unique-term", "This mentions zzyzx somewhere in the body."));
        await CreateArticleAsync(
            admin, ArticlePayload("Zzyzx Title Match", "title-match-unique-term", "Body has nothing special."));

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=zzyzx");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResultResponse>();
        Assert.Equal(2, result!.Items.Count);
        Assert.Equal("title-match-unique-term", result.Items[0].Slug);
        Assert.Equal("body-match-unique-term", result.Items[1].Slug);
    }

    [Fact]
    public async Task Search_ReturnsBadRequest_WhenQueryTooShort()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.GetAsync("/api/knowledge-base/articles/search?q=a");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_ThenGetReturnsNotFound()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(admin, ArticlePayload("Delete Me", "delete-me-article"));

        var deleteResponse = await admin.DeleteAsync($"/api/knowledge-base/articles/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await admin.GetAsync($"/api/knowledge-base/articles/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.DeleteAsync($"/api/knowledge-base/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Returns403_ForAgent()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(admin, ArticlePayload("Agent Cannot Delete", "agent-cannot-delete"));

        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);
        var response = await agent.DeleteAsync($"/api/knowledge-base/articles/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBySlug_ReturnsArticle_WhenFound()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(admin, ArticlePayload("By Slug Lookup", "by-slug-lookup"));

        var response = await admin.GetAsync("/api/knowledge-base/articles/by-slug/by-slug-lookup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal(created.Id, body!.Id);
    }

    [Fact]
    public async Task GetBySlug_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.GetAsync("/api/knowledge-base/articles/by-slug/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publish_SetsStatusPublishedAndStampsPublishedAtUtc()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(admin, ArticlePayload("Publish Me", "publish-me-article"));
        Assert.Equal(KnowledgeBaseArticleStatus.Draft, created.Status);
        Assert.Null(created.PublishedAtUtc);

        var response = await admin.PostAsync($"/api/knowledge-base/articles/{created.Id}/publish", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal(KnowledgeBaseArticleStatus.Published, body!.Status);
        Assert.NotNull(body.PublishedAtUtc);
    }

    [Fact]
    public async Task Publish_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsync($"/api/knowledge-base/articles/{Guid.NewGuid()}/publish", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publish_Returns403_ForAgent()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(admin, ArticlePayload("Agent Cannot Publish", "agent-cannot-publish"));

        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);
        var response = await agent.PostAsync($"/api/knowledge-base/articles/{created.Id}/publish", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unpublish_SetsStatusDraftAndKeepsContent()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(
            admin, ArticlePayload("Unpublish Me", "unpublish-me-article", "Keep this body.", status: "Published"));
        Assert.Equal(KnowledgeBaseArticleStatus.Published, created.Status);

        var response = await admin.PostAsync($"/api/knowledge-base/articles/{created.Id}/unpublish", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal(KnowledgeBaseArticleStatus.Draft, body!.Status);
        Assert.Equal("Keep this body.", body.Body);
    }

    [Fact]
    public async Task Unpublish_Returns404_WhenMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsync($"/api/knowledge-base/articles/{Guid.NewGuid()}/unpublish", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unpublish_Returns403_ForAgent()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(
            admin, ArticlePayload("Agent Cannot Unpublish", "agent-cannot-unpublish", status: "Published"));

        var agent = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.ActiveEmail, CustomWebApplicationFactory.ActivePassword);
        var response = await agent.PostAsync($"/api/knowledge-base/articles/{created.Id}/unpublish", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unpublish_IsIdempotent_WhenAlreadyDraft()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(admin, ArticlePayload("Already Draft", "already-draft-article"));

        var response = await admin.PostAsync($"/api/knowledge-base/articles/{created.Id}/unpublish", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal(KnowledgeBaseArticleStatus.Draft, body!.Status);
    }

    [Fact]
    public async Task AllEndpoints_ReturnUnauthorized_WhenAnonymous()
    {
        var articleId = Guid.NewGuid();

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.GetAsync("/api/knowledge-base/articles")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.GetAsync("/api/knowledge-base/articles/search?q=test")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.GetAsync($"/api/knowledge-base/articles/{articleId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.GetAsync("/api/knowledge-base/articles/by-slug/anything")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.PostAsJsonAsync(
                "/api/knowledge-base/articles", ArticlePayload("Anon", "anon-slug"))).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.PutAsJsonAsync(
                $"/api/knowledge-base/articles/{articleId}", ArticlePayload("Anon", "anon-slug"))).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.DeleteAsync($"/api/knowledge-base/articles/{articleId}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.PostAsync($"/api/knowledge-base/articles/{articleId}/publish", null)).StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await _client.PostAsync($"/api/knowledge-base/articles/{articleId}/unpublish", null)).StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenCategoryIdMissing()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles",
            new { title = "No Category", slug = "no-category-article", body = "x", tags = Array.Empty<string>() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenCategoryIdEmpty()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles",
            ArticlePayload("Empty Category", "empty-category-article", categoryId: Guid.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns404_WhenCategoryDoesNotExist()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles",
            ArticlePayload("Missing Category", "missing-category-article", categoryId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Returns422_WhenCategoryIsInactive()
    {
        var admin = await AuthenticatedClientAsync();
        var inactiveCategoryId = SeedCategory("Inactive Category", isActive: false);

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles",
            ArticlePayload("Inactive Category Article", "inactive-category-article", categoryId: inactiveCategoryId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmbedsCategory_InResponse()
    {
        var admin = await AuthenticatedClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/knowledge-base/articles",
            ArticlePayload("Embeds Category", "embeds-category-article"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal(_defaultCategoryId, body!.CategoryId);
        Assert.NotNull(body.Category);
        Assert.Equal(_defaultCategoryId, body.Category!.Id);
        Assert.True(body.Category.IsActive);
    }

    [Fact]
    public async Task Update_Returns422_WhenChangingToInactiveCategory()
    {
        var admin = await AuthenticatedClientAsync();
        var created = await CreateArticleAsync(admin, ArticlePayload("Change Category", "change-category-article"));
        var inactiveCategoryId = SeedCategory("Inactive For Update", isActive: false);

        var response = await admin.PutAsJsonAsync(
            $"/api/knowledge-base/articles/{created.Id}",
            ArticlePayload("Change Category", "change-category-article", categoryId: inactiveCategoryId));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Update_Succeeds_WhenCategoryUnchangedEvenIfNowInactive()
    {
        var admin = await AuthenticatedClientAsync();
        var categoryId = SeedCategory("Becomes Inactive");
        var created = await CreateArticleAsync(
            admin, ArticlePayload("Keeps Category", "keeps-category-article", categoryId: categoryId));

        // Deactivate the category after the article was created, then edit
        // the article without changing CategoryId — this must still succeed.
        var deactivate = await admin.PatchAsync(
            $"/api/knowledge-base/categories/{categoryId}/status",
            JsonContent.Create(new { isActive = false }));
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);

        var response = await admin.PutAsJsonAsync(
            $"/api/knowledge-base/articles/{created.Id}",
            ArticlePayload("Keeps Category Renamed", "keeps-category-article", categoryId: categoryId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<KnowledgeBaseArticleResponse>();
        Assert.Equal(categoryId, body!.CategoryId);
        Assert.Equal("Keeps Category Renamed", body.Title);
    }

    [Fact]
    public async Task List_FiltersByCategoryId()
    {
        var admin = await AuthenticatedClientAsync();
        var otherCategoryId = SeedCategory("Other Category For List Filter");
        await CreateArticleAsync(
            admin, ArticlePayload("In Default Category", "in-default-category-list"));
        await CreateArticleAsync(
            admin, ArticlePayload("In Other Category", "in-other-category-list", categoryId: otherCategoryId));

        var response = await admin.GetAsync($"/api/knowledge-base/articles?categoryId={otherCategoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<KnowledgeBaseSearchResultResponse>();
        Assert.Contains(result!.Items, a => a.Slug == "in-other-category-list");
        Assert.DoesNotContain(result.Items, a => a.Slug == "in-default-category-list");
    }
}
