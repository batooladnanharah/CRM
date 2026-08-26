using System.Text.Json.Serialization;

namespace CRM.Api.Tickets;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;

    // No navigation property — AuthDbContext.Users lives in a separate DbContext
    // (same physical database), same cross-context style as CustomerNote.AuthorId.
    public Guid? AssigneeUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // SLA snapshot — all nullable so existing rows (and tickets with no
    // matching policy) remain valid; computed status surfaces as
    // SlaStatus.NotApplicable when FirstResponseDueAtUtc/ResolutionDueAtUtc
    // are null. No concurrency token yet: last write wins on concurrent
    // priority changes, matching the rest of this entity.
    public Guid? SlaPolicyId { get; set; }
    public DateTime? FirstResponseDueAtUtc { get; set; }
    public DateTime? ResolutionDueAtUtc { get; set; }
    public DateTime? FirstRespondedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }

    // SLA automation bookkeeping (CRM-101) — breach timestamps are set at
    // most once each (guards re-running the evaluator from writing duplicate
    // history/escalations); SlaAutoEscalatedAtUtc guards against escalating
    // the same ticket twice regardless of how many due-dates it breaches.
    // Not rewritten by a later policy change — see SlaEvaluator class comment.
    public DateTime? FirstResponseBreachedAtUtc { get; set; }
    public DateTime? ResolutionBreachedAtUtc { get; set; }
    public DateTime? SlaLastEvaluatedAtUtc { get; set; }
    public DateTime? SlaAutoEscalatedAtUtc { get; set; }
}

// String-serialized in JSON (both request and response bodies) — the frontend
// contract (types/tickets.ts) sends/expects "Open"/"High" etc., not int values.
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketStatus { Open = 0, InProgress = 1, Resolved = 2, Closed = 3 }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }
