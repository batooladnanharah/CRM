namespace CRM.Api.Ai;

public sealed record AiRequest(
    AiFeature Feature,
    string SystemInstruction,
    string UserInput,
    IReadOnlyDictionary<string, string> Context);

public sealed record AiResponse(
    bool Success,
    string? Content,
    string Provider,
    string? Model,
    string? ErrorCode);

public sealed record AiStatusResponse(bool Enabled, string? Provider, bool Available);

public sealed record AiUnavailableResponse(string ErrorCode);
