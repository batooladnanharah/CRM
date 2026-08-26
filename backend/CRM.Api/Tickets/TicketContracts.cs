using CRM.Api.Sla;

namespace CRM.Api.Tickets;

public record CreateTicketRequest(
    Guid CustomerId,
    string Title,
    string Description,
    TicketPriority? Priority);

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
    TicketSlaSnapshotResponse Sla);

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
    Guid AuthorUserId,
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
