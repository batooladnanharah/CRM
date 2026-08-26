using System.Text.Json.Serialization;
using CRM.Api.Tickets;

namespace CRM.Api.Sla;

public class SlaPolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Loose text filter (e.g. a channel name) — null means "any channel".
    // Tickets don't carry a channel reference yet, so every resolution call
    // passes null; this field exists for forward-compatibility once ticket
    // ↔ channel linkage is wired up in a later story.
    public string? Channel { get; set; }

    public TicketPriority Priority { get; set; }
    public int FirstResponseMinutes { get; set; }
    public int ResolutionMinutes { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SlaStatus { NotApplicable, OnTrack, AtRisk, Breached, Met }
