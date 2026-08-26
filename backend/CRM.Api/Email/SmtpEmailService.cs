namespace CRM.Api.Email;

/// <summary>
/// Skeleton provider proving the <see cref="IEmailService"/> abstraction; not wired
/// to a real SMTP server yet.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    // TODO(COM-002 follow-up): wire System.Net.Mail.SmtpClient using EmailOptions.Smtp.
    public Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("SMTP provider is not configured for this build.");
    }
}
