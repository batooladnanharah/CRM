using CRM.Api.Tickets;

namespace CRM.Api.CustomerPortal;

public record CustomerTicketListItemResponse(
    Guid Id,
    string Title,
    TicketStatus Status,
    TicketPriority Priority,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

// No AuthorRole/author identity — every message in this codebase is
// staff-authored (there is no customer- or system-authored message path
// yet), so exposing an author label would always read "agent" and adds
// nothing; omitted rather than inventing a taxonomy the domain doesn't have.
public record CustomerTicketMessageResponse(
    Guid Id,
    string Body,
    DateTime CreatedAtUtc);

// Only status-change history is customer-visible — see
// CustomerPortalEndpoints.IsPortalVisibleHistoryEntry for the full rationale.
// OldValue/NewValue are the stringified TicketStatus names, matching the
// internal TicketHistoryEntryResponse convention.
public record CustomerTicketHistoryEntryResponse(
    Guid Id,
    string? OldValue,
    string? NewValue,
    DateTime ChangedAtUtc);

public record CustomerTicketDetailsResponse(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<CustomerTicketMessageResponse> Messages,
    IReadOnlyList<CustomerTicketHistoryEntryResponse> History);

public record CreateCustomerTicketRequest(string Title, string Description, TicketPriority? Priority);

public record CustomerDashboardResponse(
    int OpenCount,
    int PendingCount,
    int ResolvedCount,
    IReadOnlyList<CustomerTicketListItemResponse> RecentTickets);
