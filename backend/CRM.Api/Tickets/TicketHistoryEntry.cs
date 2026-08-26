using System.Text.Json.Serialization;

namespace CRM.Api.Tickets;

public sealed class TicketHistoryEntry
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public TicketChangeType ChangeType { get; set; }

    // Stringified previous/new value: a TicketStatus/TicketPriority name, a
    // user id (assignment), a message/attachment id, or null (unassigned / no
    // prior value / not applicable).
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    // Only populated for Escalated entries (the agent-supplied escalation reason).
    public string? Reason { get; set; }

    public Guid ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}

// New members only ever append — existing serialized values (Assignment/Status/
// Priority) must never be renumbered or removed (backward compatibility).
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketChangeType
{
    Assignment = 0,
    Status = 1,
    Priority = 2,
    MessageAdded = 3,
    AttachmentAdded = 4,
    AttachmentRemoved = 5,
    Escalated = 6,
    SlaRecalculated = 7,
    SlaBreached = 8,
}
