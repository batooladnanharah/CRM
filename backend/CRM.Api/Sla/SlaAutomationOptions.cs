namespace CRM.Api.Sla;

public sealed class SlaAutomationOptions
{
    public const string SectionName = "Sla";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;
}
