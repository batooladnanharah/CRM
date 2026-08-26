namespace CRM.Api.QuickReplies;

public record QuickReplyResponse(
    Guid Id,
    string Title,
    string Content,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateQuickReplyRequest(string Title, string Content);

public record UpdateQuickReplyRequest(string Title, string Content, bool IsActive);
