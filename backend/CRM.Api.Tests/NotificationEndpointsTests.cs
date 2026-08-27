using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CRM.Api.Auth;
using CRM.Api.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Api.Tests;

public class NotificationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        factory.SeedUsers();
        _client = factory.CreateClient();
    }

    private async Task<(HttpClient Client, Guid UserId)> AuthenticatedClientAsync(
        string email = CustomWebApplicationFactory.ActiveEmail,
        string password = CustomWebApplicationFactory.ActivePassword)
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return (client, body.User.Id);
    }

    private void SeedNotification(Guid userId, string title = "Test", Guid? ticketId = null, bool isRead = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        db.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = NotificationType.SlaAtRisk,
            Title = title,
            Message = "Message body",
            TicketId = ticketId,
            IsRead = isRead,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_Notifications_Returns401_WhenUnauthenticated()
    {
        var response = await _client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_Notifications_ReturnsOnlyCallersOwnNotifications()
    {
        var (client, userId) = await AuthenticatedClientAsync();
        var (_, otherUserId) = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.SecondAgentEmail, CustomWebApplicationFactory.SecondAgentPassword);

        SeedNotification(userId, "Mine");
        SeedNotification(otherUserId, "Not Mine");

        var response = await client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<NotificationListResponse>();
        Assert.All(body!.Items, item => Assert.NotEqual("Not Mine", item.Title));
        Assert.Contains(body.Items, item => item.Title == "Mine");
    }

    [Fact]
    public async Task Get_Notifications_IgnoresUserIdQueryParam()
    {
        var (client, userId) = await AuthenticatedClientAsync();
        var (_, otherUserId) = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.MultiRoleEmail, CustomWebApplicationFactory.MultiRolePassword);

        SeedNotification(userId, "Real Owner Notification");
        SeedNotification(otherUserId, "Other Owner Notification");

        var response = await client.GetAsync($"/api/notifications?userId={otherUserId}");

        var body = await response.Content.ReadFromJsonAsync<NotificationListResponse>();
        Assert.DoesNotContain(body!.Items, item => item.Title == "Other Owner Notification");
    }

    [Fact]
    public async Task Patch_Read_Returns404_ForNonOwner()
    {
        var (_, ownerId) = await AuthenticatedClientAsync();
        var (otherClient, _) = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.SecondAgentEmail, CustomWebApplicationFactory.SecondAgentPassword);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = ownerId,
            Type = NotificationType.SlaBreached,
            Title = "Owner Only",
            Message = "Message",
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Notifications.Add(notification);
        db.SaveChanges();

        var response = await otherClient.PatchAsync($"/api/notifications/{notification.Id}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Read_Succeeds_ForOwner_AndDecrementsUnreadCount()
    {
        var (client, userId) = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.CustomerRoleEmail, CustomWebApplicationFactory.CustomerRolePassword);
        SeedNotification(userId, "Owned Notification");

        var before = await (await client.GetAsync("/api/notifications")).Content.ReadFromJsonAsync<NotificationListResponse>();
        var notificationId = before!.Items.Single(i => i.Title == "Owned Notification").Id;

        var readResponse = await client.PatchAsync($"/api/notifications/{notificationId}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, readResponse.StatusCode);

        var after = await (await client.GetAsync("/api/notifications")).Content.ReadFromJsonAsync<NotificationListResponse>();
        Assert.Equal(before.UnreadCount - 1, after!.UnreadCount);
    }

    [Fact]
    public async Task Patch_ReadAll_ZeroesUnreadCount()
    {
        var (client, userId) = await AuthenticatedClientAsync(
            CustomWebApplicationFactory.MultiRoleEmail, CustomWebApplicationFactory.MultiRolePassword);
        SeedNotification(userId, "First");
        SeedNotification(userId, "Second");

        var response = await client.PatchAsync("/api/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var after = await (await client.GetAsync("/api/notifications")).Content.ReadFromJsonAsync<NotificationListResponse>();
        Assert.Equal(0, after!.UnreadCount);
    }
}
