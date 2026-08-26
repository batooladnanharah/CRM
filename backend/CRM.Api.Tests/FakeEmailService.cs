using CRM.Api.Email;

namespace CRM.Api.Tests;

// Test double for IEmailService — defaults to succeeding so most tests don't
// need to configure it; ShouldFail flips it to simulate a provider failure.
public sealed class FakeEmailService : IEmailService
{
    public bool ShouldFail { get; set; }
    public List<EmailSendRequest> SentRequests { get; } = [];

    public Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);

        if (ShouldFail)
        {
            return Task.FromResult(new EmailSendResult(false, null, "provider_down", "Simulated provider failure."));
        }

        return Task.FromResult(new EmailSendResult(true, "fake-" + Guid.NewGuid().ToString("N"), null, null));
    }
}
