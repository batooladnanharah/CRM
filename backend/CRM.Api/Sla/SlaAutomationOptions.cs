namespace CRM.Api.Sla;

public sealed class SlaAutomationOptions
{
    public const string SectionName = "Sla";

    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 60;

    // Upper bound on how many open tickets a single evaluation cycle touches —
    // prevents an unbounded ToListAsync/foreach over the whole open-ticket
    // table as the dataset grows. Tickets are ordered oldest-evaluated-first
    // (nulls first) so a busy instance still makes forward progress across
    // ticks rather than starving the same tail of rows every time.
    public int BatchSize { get; set; } = 200;
}
