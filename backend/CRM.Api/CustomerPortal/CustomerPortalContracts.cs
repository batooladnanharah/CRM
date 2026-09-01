using CRM.Api.Tickets;

namespace CRM.Api.CustomerPortal;

public record CustomerTicketListItemResponse(
    Guid Id,
    string Title,
    TicketStatus Status,
    TicketPriority Priority,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

// Customer-safe DTO. Do NOT add internal fields (assigned agent, SLA,
// escalation, internal notes). SenderType is derived server-side from which
// author field is set on the underlying TicketMessage ("Customer" when
// AuthorCustomerId is set, "Agent" when AuthorUserId is set) — never trust a
// client-supplied sender.
public record CustomerTicketMessageResponse(
    Guid Id,
    string SenderType,
    string Body,
    DateTime CreatedAtUtc);

// Portal reply request body for POST /api/customer/tickets/{id}/messages.
// Body-only — the sending customer is always resolved server-side via
// ICurrentCustomerAccessor, never accepted from the request.
public record CustomerCreateTicketMessageRequest(string Body);

// Only status-change history is customer-visible — see
// CustomerPortalEndpoints.IsPortalVisibleHistoryEntry for the full rationale.
// OldValue/NewValue are the stringified TicketStatus names, matching the
// internal TicketHistoryEntryResponse convention.
public record CustomerTicketHistoryEntryResponse(
    Guid Id,
    string? OldValue,
    string? NewValue,
    DateTime ChangedAtUtc);

// Customer-safe DTO. Do NOT add internal fields (assigned agent, SLA,
// escalation, internal notes, internal priority). Audited against Ticket's
// full field set (AssigneeUserId, SlaPolicyId, FirstResponseDueAtUtc,
// ResolutionDueAtUtc, *BreachedAtUtc, SlaAutoEscalatedAtUtc, AutoAssigned) —
// none of those are exposed here or on CustomerTicketListItemResponse.
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

// Customer-facing knowledge-base article shape: no AuthorId (internal/CRM
// only) and no Status (every article the portal returns is Published by
// definition, so echoing the status back adds nothing).
public record CustomerKnowledgeBaseArticleListItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string[] Tags,
    DateTime PublishedAtUtc);

public record CustomerKnowledgeBaseArticleDetailsResponse(
    Guid Id,
    string Title,
    string Slug,
    string Body,
    string[] Tags,
    DateTime PublishedAtUtc);

public record CustomerKnowledgeBaseArticleListResponse(
    IReadOnlyList<CustomerKnowledgeBaseArticleListItemResponse> Items,
    int Total,
    int Page,
    int PageSize);

// Customer-facing category shape — an ArticleCount of Published articles
// only (Draft/Archived articles are invisible to the portal, so they never
// count) and no IsActive: an inactive category never reaches this list in
// the first place (see ListPortalKnowledgeBaseCategories), so echoing the
// flag back would always read "true" and adds nothing.
public record CustomerKnowledgeBaseCategoryResponse(Guid Id, string Name, string? Description, int ArticleCount);
