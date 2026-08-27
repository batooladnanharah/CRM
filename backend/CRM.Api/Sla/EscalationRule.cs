using System.Text.Json.Serialization;

namespace CRM.Api.Sla;

public sealed class EscalationRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public EscalationTrigger Trigger { get; set; }
    public bool IsActive { get; set; } = true;
    public bool NotifyAgent { get; set; }
    public bool NotifyManager { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// String-serialized in JSON (both request and response bodies), matching the
// convention used by TicketStatus/TicketPriority/SlaStatus — request bodies
// send "AtRisk"/"Breached", not raw int values.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EscalationTrigger
{
    AtRisk = 1,
    Breached = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SlaObjectiveKind
{
    Response = 1,
    Resolution = 2
}
