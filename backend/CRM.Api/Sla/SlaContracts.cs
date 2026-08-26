using CRM.Api.Tickets;

namespace CRM.Api.Sla;

public record SlaPolicyResponse(
    Guid Id,
    string Name,
    string? Channel,
    TicketPriority Priority,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    bool IsDefault,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateSlaPolicyRequest(
    string Name,
    string? Channel,
    string Priority,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    bool IsDefault,
    bool IsActive);

public record UpdateSlaPolicyRequest(
    string Name,
    string? Channel,
    string Priority,
    int FirstResponseMinutes,
    int ResolutionMinutes,
    bool IsDefault,
    bool IsActive);

public record TicketSlaSnapshotResponse(
    Guid? PolicyId,
    DateTime? FirstResponseDueAtUtc,
    DateTime? ResolutionDueAtUtc,
    DateTime? FirstRespondedAtUtc,
    DateTime? ResolvedAtUtc,
    SlaStatus FirstResponseStatus,
    SlaStatus ResolutionStatus,
    DateTime? FirstResponseBreachedAtUtc,
    DateTime? ResolutionBreachedAtUtc,
    DateTime? SlaLastEvaluatedAtUtc,
    DateTime? SlaAutoEscalatedAtUtc);
