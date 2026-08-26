namespace CRM.Api.Ai;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public bool Enabled { get; set; }
    public string? Provider { get; set; } // null | "Development" | future real provider name
    public string? Model { get; set; }
    public string? ApiKey { get; set; } // server-side only, never serialised to any response/log
    public int TimeoutSeconds { get; set; } = 15;
}
