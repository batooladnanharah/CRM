using System.Security.Claims;
using CRM.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class AuthorizationPolicyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthorizationPolicyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static ClaimsPrincipal PrincipalWithRoles(params string[] roles)
    {
        var identity = new ClaimsIdentity(
            roles.Select(role => new Claim(ClaimTypes.Role, role)),
            authenticationType: "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    private async Task<bool> EvaluateAsync(string policyName, ClaimsPrincipal principal)
    {
        using var scope = _factory.Services.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var result = await authorizationService.AuthorizeAsync(principal, policyName);
        return result.Succeeded;
    }

    [Fact]
    public async Task AdminOnlyPolicy_RejectsPrincipalWithoutAdminRole()
    {
        var principal = PrincipalWithRoles(Roles.Agent, Roles.Customer);

        var succeeded = await EvaluateAsync("AdminOnly", principal);

        Assert.False(succeeded);
    }

    [Fact]
    public async Task AdminOnlyPolicy_AcceptsPrincipalWithAdminRole()
    {
        var principal = PrincipalWithRoles(Roles.Admin, Roles.Agent);

        var succeeded = await EvaluateAsync("AdminOnly", principal);

        Assert.True(succeeded);
    }

    [Theory]
    [InlineData(new[] { "agent" }, true)]
    [InlineData(new[] { "admin" }, true)]
    [InlineData(new[] { "admin", "agent" }, true)]
    [InlineData(new[] { "customer" }, false)]
    public async Task AgentOrAdminPolicy_AcceptsEitherRole(string[] roles, bool expected)
    {
        var principal = PrincipalWithRoles(roles);

        var succeeded = await EvaluateAsync("AgentOrAdmin", principal);

        Assert.Equal(expected, succeeded);
    }

    [Fact]
    public async Task CustomerPortalPolicy_AcceptsPrincipalWithCustomerRole()
    {
        var principal = PrincipalWithRoles(Roles.Customer);

        var succeeded = await EvaluateAsync("CustomerPortal", principal);

        Assert.True(succeeded);
    }

    [Fact]
    public async Task CustomerPortalPolicy_RejectsAgentRole()
    {
        var principal = PrincipalWithRoles(Roles.Agent);

        var succeeded = await EvaluateAsync("CustomerPortal", principal);

        Assert.False(succeeded);
    }

    [Fact]
    public async Task CustomerPortalPolicy_RejectsAdminRole()
    {
        var principal = PrincipalWithRoles(Roles.Admin);

        var succeeded = await EvaluateAsync("CustomerPortal", principal);

        Assert.False(succeeded);
    }

    [Fact]
    public async Task CustomerPortalPolicy_RejectsAdminAndAgentRoles()
    {
        var principal = PrincipalWithRoles(Roles.Admin, Roles.Agent);

        var succeeded = await EvaluateAsync("CustomerPortal", principal);

        Assert.False(succeeded);
    }
}
