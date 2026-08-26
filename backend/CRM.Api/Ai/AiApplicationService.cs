using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CRM.Api.Ai;

public sealed class AiApplicationService(
    IAiService aiService,
    TicketDbContext ticketDb,
    IOptions<AiOptions> options,
    ILogger<AiApplicationService> logger)
{
    private const int MaxMessages = 20;
    private const int MaxMessageLength = 2000;

    public async Task<AiResponse?> SummariseTicketAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await ticketDb.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        var messages = await ticketDb.TicketMessages
            .AsNoTracking()
            .Where(m => m.TicketId == ticketId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(MaxMessages)
            .OrderBy(m => m.CreatedAtUtc)
            .Select(m => m.Body)
            .ToListAsync(cancellationToken);

        var context = new Dictionary<string, string>
        {
            ["Subject"] = ticket.Title,
            ["Description"] = ticket.Description,
            ["Status"] = ticket.Status.ToString(),
            ["Priority"] = ticket.Priority.ToString(),
        };
        for (var i = 0; i < messages.Count; i++)
        {
            context[$"Message{i}"] = Truncate(messages[i], MaxMessageLength);
        }

        var userInput = string.Join(
            "\n",
            messages.Select(m => Truncate(m, MaxMessageLength)).Prepend(ticket.Description));

        var request = new AiRequest(AiFeature.TicketSummary, AiPromptTemplates.TicketSummary, userInput, context);

        return await GenerateAsync(AiFeature.TicketSummary, request, cancellationToken);
    }

    // TODO(follow-up AI feature stories): add CategoriseTicketAsync, SuggestReplyAsync,
    // SuggestSolutionAsync, and a Chatbot entry point, each calling GenerateAsync with
    // its own AiFeature/AiPromptTemplates constant.

    private async Task<AiResponse> GenerateAsync(AiFeature feature, AiRequest request, CancellationToken callerToken)
    {
        var provider = aiService.ProviderName;
        var model = options.Value.Model;
        var timeout = TimeSpan.FromSeconds(Math.Max(1, options.Value.TimeoutSeconds));

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(callerToken, timeoutCts.Token);

        var startedAt = DateTime.UtcNow;
        try
        {
            var response = await aiService.GenerateAsync(request, linkedCts.Token);

            if (response.Success && string.IsNullOrEmpty(response.Content))
            {
                return response with { Success = false, ErrorCode = "EmptyResponse" };
            }

            return response;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            logger.LogWarning(
                "AI request timed out feature={Feature} provider={Provider} durationMs={DurationMs}",
                feature, provider, (DateTime.UtcNow - startedAt).TotalMilliseconds);
            return new AiResponse(false, null, provider, model, "Timeout");
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "AI request cancelled by caller feature={Feature} provider={Provider}", feature, provider);
            return new AiResponse(false, null, provider, model, "Cancelled");
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                "AI request failed feature={Feature} provider={Provider} error={ErrorMessage}",
                feature, provider, ex.Message);
            return new AiResponse(false, null, provider, model, "ProviderError");
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
