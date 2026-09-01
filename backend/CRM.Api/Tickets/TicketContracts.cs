using CRM.Api.Sla;

namespace CRM.Api.Tickets;

public record CreateTicketRequest(
    Guid CustomerId,
    string Title,
    string Description,
    TicketPriority? Priority,
    // CRM-62 — honoured only when the caller holds the manual-assignment
    // permission (currently: the Admin role — see TicketEndpoints.MapPost("/")).
    // Otherwise this is silently ignored and automatic assignment runs instead;
    // ticket creation never fails or errors because of this field.
    Guid? AssignedAgentId = null);

public record TicketResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    Guid? AssigneeUserId,
    string? AssigneeDisplayName,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    TicketSlaSnapshotResponse Sla,
    IReadOnlyList<TicketEscalationResponse> Escalations,
    bool AutoAssigned);

public record TicketEscalationResponse(
    bool AgentNotified, bool ManagerNotified, string Trigger, string Objective);

public record TicketListItem(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string Title,
    TicketStatus Status,
    TicketPriority Priority,
    Guid? AssigneeUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    TicketSlaSnapshotResponse Sla);

public record TicketListQuery(
    string? Search,
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? AssigneeId,
    DateTime? UpdatedSince,
    int Page = 1,
    int PageSize = 20);

public record AssignTicketRequest(Guid? AgentUserId);

public record ChangeTicketStatusRequest(string Status);

public record ChangeTicketPriorityRequest(string Priority);

public record EligibleAgentResponse(Guid Id, string DisplayName, string Email);

public record TicketHistoryEntryResponse(
    Guid Id,
    TicketChangeType ChangeType,
    string? OldValue,
    string? NewValue,
    string? Reason,
    Guid ChangedByUserId,
    string ChangedByDisplayName,
    DateTime ChangedAtUtc,
    bool IsSystemActor);

public record CreateTicketMessageRequest(
    string Body,
    bool IsInternal,
    IReadOnlyList<Guid>? MentionedUserIds,
    string? Channel,
    string? SubjectOverride);

public record TicketMessageResponse(
    Guid Id,
    Guid TicketId,
    Guid? AuthorUserId,
    Guid? AuthorCustomerId,
    string AuthorDisplayName,
    string Body,
    bool IsInternal,
    IReadOnlyList<Guid> MentionedUserIds,
    string Channel,
    string? EmailDeliveryStatus,
    DateTime CreatedAtUtc);

// StorageKey is intentionally never included here.
public record TicketAttachmentResponse(
    Guid Id,
    Guid TicketId,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    Guid UploadedByUserId,
    string UploadedByDisplayName,
    DateTime CreatedAtUtc);

public record EscalateTicketRequest(string Reason);
