namespace CRM.Api.Ai;

/// <summary>
/// Development-only implementation. Returns deterministic mock content, clearly
/// labelled as such. Never presented as real AI output.
/// </summary>
public sealed class DevelopmentAiService : IAiService
{
    public string ProviderName => "Development";
    public bool IsAvailable => true;

    public Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(new AiResponse(false, null, ProviderName, "development-mock", "Cancelled"));
        }

        var content = request.Feature switch
        {
            AiFeature.TicketSummary => $"Development summary: {Truncate(SummaryInput(request), 200)}",
            AiFeature.TicketCategorization => "Development category: General",
            AiFeature.SuggestedReply =>
                "Development suggested reply: Thank you for contacting support. We are looking into your request.",
            AiFeature.SuggestedSolution =>
                "Development suggested solution: Please try restarting the affected component.",
            AiFeature.Chatbot => "Development chatbot: This is a mock response.",
            _ => "Development response: (unrecognised feature).",
        };

        return Task.FromResult(new AiResponse(true, content, ProviderName, "development-mock", null));
    }

    private static string SummaryInput(AiRequest request) =>
        !string.IsNullOrWhiteSpace(request.UserInput)
            ? request.UserInput
            : request.Context.GetValueOrDefault("Subject", string.Empty);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
