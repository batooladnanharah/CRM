using CRM.Api.Ai;

namespace CRM.Api.Tests;

// Test double for IAiService — defaults to succeeding so most tests don't need to
// configure it. ShouldThrow/Delay let tests exercise AiApplicationService's
// timeout/error handling without depending on real elapsed time.
public sealed class FakeAiService : IAiService
{
    public string ProviderName => "Development";
    public bool IsAvailable { get; set; } = true;
    public bool ShouldThrow { get; set; }
    public bool EmptyContent { get; set; }
    public TimeSpan? Delay { get; set; }
    public List<AiRequest> Requests { get; } = [];

    public async Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (Delay is not null)
        {
            await Task.Delay(Delay.Value, cancellationToken);
        }

        if (ShouldThrow)
        {
            throw new InvalidOperationException("Simulated AI provider failure.");
        }

        if (EmptyContent)
        {
            return new AiResponse(true, string.Empty, ProviderName, "development-mock", null);
        }

        return new AiResponse(true, $"Development summary: {request.UserInput}", ProviderName, "development-mock", null);
    }
}
