using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Ai;
using CRM.Api.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CRM.Api.Tests;

public class AiStatusEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AiStatusEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
    }

    private async Task<HttpClient> AuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var anonymous = factory.CreateClient();
        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new
        {
            email = CustomWebApplicationFactory.ActiveEmail,
            password = CustomWebApplicationFactory.ActivePassword,
        });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private WebApplicationFactory<Program> WithConfig(Dictionary<string, string?> config, bool aiAvailable = true) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) => configBuilder.AddInMemoryCollection(config));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAiService>();
                services.AddSingleton<IAiService>(new FakeAiService { IsAvailable = aiAvailable });
            });
        });

    [Fact]
    public async Task Enabled_with_Development_provider_returns_available_true()
    {
        using var factory = WithConfig(new Dictionary<string, string?>
        {
            ["AI:Enabled"] = "true",
            ["AI:Provider"] = "Development",
        });
        var client = await AuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/ai/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<AiStatusResponse>();
        Assert.True(status!.Enabled);
        Assert.Equal("Development", status.Provider);
        Assert.True(status.Available);
    }

    [Fact]
    public async Task Disabled_returns_enabled_false_provider_null_available_false()
    {
        using var factory = WithConfig(new Dictionary<string, string?>
        {
            ["AI:Enabled"] = "false",
        });
        var client = await AuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/ai/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<AiStatusResponse>();
        Assert.False(status!.Enabled);
        Assert.Null(status.Provider);
        Assert.False(status.Available);
    }

    [Fact]
    public async Task Configured_provider_with_no_SDK_returns_available_false()
    {
        using var factory = WithConfig(
            new Dictionary<string, string?>
            {
                ["AI:Enabled"] = "true",
                ["AI:Provider"] = "OpenAI",
            },
            aiAvailable: false);
        var client = await AuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/ai/status");

        var status = await response.Content.ReadFromJsonAsync<AiStatusResponse>();
        Assert.True(status!.Enabled);
        Assert.False(status.Available);
    }

    [Fact]
    public async Task Response_body_does_not_contain_the_configured_ApiKey()
    {
        using var factory = WithConfig(new Dictionary<string, string?>
        {
            ["AI:Enabled"] = "true",
            ["AI:Provider"] = "Development",
            ["AI:ApiKey"] = "SECRET_KEY_MARKER",
        });
        var client = await AuthenticatedClientAsync(factory);

        var response = await client.GetAsync("/api/ai/status");
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("SECRET_KEY_MARKER", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Anonymous_caller_gets_401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/ai/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
