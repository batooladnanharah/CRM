namespace CRM.Api.KnowledgeBase;

public record KnowledgeBaseCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateKnowledgeBaseCategoryRequest(string Name, string? Description, bool? IsActive);

public record UpdateKnowledgeBaseCategoryRequest(string Name, string? Description);

public record SetKnowledgeBaseCategoryStatusRequest(bool IsActive);
