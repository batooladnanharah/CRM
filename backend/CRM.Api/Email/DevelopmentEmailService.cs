namespace CRM.Api.Email;

/// <summary>
/// Development-only implementation. Does not send email. Do not use in production.
/// </summary>
public sealed class DevelopmentEmailService(ILogger<DevelopmentEmailService> logger) : IEmailService
{
    public Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "development_email_send to={ToAddress} subject={Subject} ticketMessageId={TicketMessageId}",
            request.ToAddress, request.Subject, request.TicketMessageId);

        return Task.FromResult(new EmailSendResult(true, "dev-" + Guid.NewGuid().ToString("N"), null, null));
    }
}
