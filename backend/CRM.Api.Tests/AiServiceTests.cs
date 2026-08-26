using CRM.Api.Ai;
using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Tests;

public class AiServiceTests
{
    private static readonly IReadOnlyDictionary<string, string> EmptyContext =
        new Dictionary<string, string>();

    [Theory]
    [InlineData(AiFeature.TicketSummary)]
    [InlineData(AiFeature.TicketCategorization)]
    [InlineData(AiFeature.SuggestedReply)]
    [InlineData(AiFeature.SuggestedSolution)]
    [InlineData(AiFeature.Chatbot)]
    public async Task GenerateAsync_returns_success_and_labelled_content_for_every_feature(AiFeature feature)
    {
        var service = new DevelopmentAiService();

        var response = await service.GenerateAsync(
            new AiRequest(feature, "instruction", "some ticket text", EmptyContext), CancellationToken.None);

        Assert.True(response.Success);
        Assert.Equal("Development", response.Provider);
        Assert.NotNull(response.Content);
        Assert.StartsWith("Development", response.Content);
    }

    [Fact]
    public async Task GenerateAsync_with_pre_cancelled_token_returns_Cancelled_without_throwing()
    {
        var service = new DevelopmentAiService();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var response = await service.GenerateAsync(
            new AiRequest(AiFeature.TicketSummary, "instruction", "text", EmptyContext), cts.Token);

        Assert.False(response.Success);
        Assert.Equal("Cancelled", response.ErrorCode);
    }

    [Fact]
    public async Task GenerateAsync_TicketSummary_truncates_to_200_characters()
    {
        var service = new DevelopmentAiService();
        var longInput = new string('x', 500);

        var response = await service.GenerateAsync(
            new AiRequest(AiFeature.TicketSummary, "instruction", longInput, EmptyContext), CancellationToken.None);

        Assert.NotNull(response.Content);
        Assert.True(response.Content!.Length <= "Development summary: ".Length + 200);
    }
}

public class TicketAiContextBuilderTests
{
    private static TicketDbContext MakeDb() =>
        new(new DbContextOptionsBuilder<TicketDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Ticket MakeTicket() => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Title = "Cannot log in",
        Description = "Login failing since this morning.",
        Status = TicketStatus.Open,
        Priority = TicketPriority.Normal,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
    };

    [Fact]
    public async Task BuildAsync_returns_null_for_a_missing_ticket()
    {
        await using var db = MakeDb();
        var builder = new TicketAiContextBuilder(db);

        var context = await builder.BuildAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(context);
    }

    [Fact]
    public async Task BuildAsync_only_exposes_subject_description_status_priority_and_message_bodies()
    {
        await using var db = MakeDb();
        var ticket = MakeTicket();
        db.Tickets.Add(ticket);
        db.TicketMessages.Add(new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = Guid.NewGuid(),
            Body = "Have you tried resetting your password?",
            IsInternal = false,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var builder = new TicketAiContextBuilder(db);
        var context = await builder.BuildAsync(ticket.Id, CancellationToken.None);

        Assert.NotNull(context);
        Assert.Equal(ticket.Title, context!.Subject);
        Assert.Equal(ticket.Description, context.Description);
        Assert.Equal("Open", context.Status);
        Assert.Equal("Normal", context.Priority);
        Assert.Single(context.Messages);
        Assert.Equal("Have you tried resetting your password?", context.Messages[0].Body);

        // TicketAiContext has no field for credentials/tokens/customer contact
        // details at all — there is nothing to accidentally leak through it.
        var properties = typeof(TicketAiContext).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain(properties, name =>
            name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("ApiKey", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Email", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Phone", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task BuildAsync_includes_internal_notes_in_the_message_list()
    {
        await using var db = MakeDb();
        var ticket = MakeTicket();
        db.Tickets.Add(ticket);
        db.TicketMessages.Add(new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = Guid.NewGuid(),
            Body = "Internal-only observation for the summary.",
            IsInternal = true,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var builder = new TicketAiContextBuilder(db);
        var context = await builder.BuildAsync(ticket.Id, CancellationToken.None);

        Assert.Contains(context!.Messages, m => m.IsInternal && m.Body == "Internal-only observation for the summary.");
    }

    [Fact]
    public async Task BuildAsync_truncates_to_the_most_recent_30_messages()
    {
        await using var db = MakeDb();
        var ticket = MakeTicket();
        db.Tickets.Add(ticket);
        var baseTime = DateTime.UtcNow;
        for (var i = 0; i < 35; i++)
        {
            db.TicketMessages.Add(new TicketMessage
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                AuthorUserId = Guid.NewGuid(),
                Body = $"Message {i}",
                IsInternal = false,
                CreatedAtUtc = baseTime.AddMinutes(i),
            });
        }
        await db.SaveChangesAsync();

        var builder = new TicketAiContextBuilder(db);
        var context = await builder.BuildAsync(ticket.Id, CancellationToken.None);

        Assert.Equal(30, context!.Messages.Count);
        Assert.Equal("Message 5", context.Messages[0].Body);
        Assert.Equal("Message 34", context.Messages[^1].Body);
    }

    [Fact]
    public async Task BuildAsync_truncates_an_individual_message_to_2000_characters()
    {
        await using var db = MakeDb();
        var ticket = MakeTicket();
        db.Tickets.Add(ticket);
        db.TicketMessages.Add(new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorUserId = Guid.NewGuid(),
            Body = new string('x', 3000),
            IsInternal = false,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var builder = new TicketAiContextBuilder(db);
        var context = await builder.BuildAsync(ticket.Id, CancellationToken.None);

        Assert.Equal(2000, context!.Messages[0].Body.Length);
    }
}
