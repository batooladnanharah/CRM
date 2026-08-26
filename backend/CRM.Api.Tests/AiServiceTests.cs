using CRM.Api.Ai;

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
