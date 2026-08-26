using CRM.Api.Email;
using Microsoft.Extensions.Logging.Abstractions;

namespace CRM.Api.Tests;

public class DevelopmentEmailServiceTests
{
    [Fact]
    public async Task SendAsync_returns_success_and_does_not_throw()
    {
        var service = new DevelopmentEmailService(NullLogger<DevelopmentEmailService>.Instance);

        var result = await service.SendAsync(
            new EmailSendRequest("customer@example.com", "Customer", "Re: Ticket", "Body", Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.ProviderMessageId);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }
}
