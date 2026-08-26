namespace CRM.Api.Email;

public interface IEmailService
{
    Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken);
}

public sealed record EmailSendRequest(
    string ToAddress,
    string? ToName,
    string Subject,
    string Body,
    Guid TicketMessageId);

public sealed record EmailSendResult(
    bool Success,
    string? ProviderMessageId,
    string? ErrorCode,
    string? ErrorMessage);
