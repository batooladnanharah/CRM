namespace CRM.Api.Ai;

/// <summary>
/// Reports the configured provider name so status endpoints are honest about what
/// was requested, while never attempting to call an unimplemented external SDK.
/// </summary>
public sealed class UnimplementedProviderAiService(string providerName) : IAiService
{
    public string ProviderName { get; } = providerName;
    public bool IsAvailable => false;

    public Task<AiResponse> GenerateAsync(AiRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new AiResponse(false, null, ProviderName, null, "ProviderNotImplemented"));
}
