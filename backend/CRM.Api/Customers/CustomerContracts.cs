namespace CRM.Api.Customers;

public record CustomerListItem(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? Company,
    DateTime CreatedAtUtc);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record CustomerListQuery(
    string? Search,
    string? SortBy,       // "fullName" | "email" | "company" | "createdAtUtc"
    string? SortDir,      // "asc" | "desc"
    int Page = 1,
    int PageSize = 25,
    string? Company = null);

public record CreateCustomerRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Company);

public record UpdateCustomerRequest(
    string FullName,
    string Email,
    string? Phone,
    string? Company);

public record CustomerInteractionDto(
    Guid Id,
    string Type,
    string Summary,
    DateTime OccurredAt,
    string? ActorName,
    Guid? ActorId,
    Guid? TicketId);

public record CustomerInteractionsQuery(
    int Page = 1,
    int PageSize = 20);

public record CreateCustomerNoteRequest(string Content);

public record UpdateCustomerNoteRequest(string Content);

public record CustomerNoteResponse(
    Guid Id,
    Guid CustomerId,
    Guid AuthorId,
    string AuthorDisplayName,
    string Content,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

// StorageKey is intentionally never included here.
public record CustomerAttachmentResponse(
    Guid Id,
    Guid CustomerId,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    Guid UploadedByUserId,
    string UploadedByDisplayName,
    DateTime CreatedAtUtc);
