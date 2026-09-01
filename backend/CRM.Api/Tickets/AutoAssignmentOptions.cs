namespace CRM.Api.Tickets;

// Mirrors the SlaAutomationOptions binding pattern (see CRM.Api.Sla.SlaAutomationOptions):
// a single bool switch bound from configuration, defaulting to enabled.
public sealed class AutoAssignmentOptions
{
    public const string SectionName = "TicketAutoAssignment";

    public bool Enabled { get; set; } = true;

    // Future-proofing only — "LowestWorkload" is the only value TicketAssignmentService
    // implements (CRM-62 MVP). Not read/branched on anywhere yet.
    public string Strategy { get; set; } = "LowestWorkload";
}
