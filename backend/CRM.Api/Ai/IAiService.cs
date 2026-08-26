namespace CRM.Api.Ai;

public interface IAiService
{
    string ProviderName { get; }
    bool IsAvailable { get; }

    Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken);
}
