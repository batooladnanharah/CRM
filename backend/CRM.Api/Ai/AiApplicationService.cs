using Microsoft.Extensions.Options;

namespace CRM.Api.Ai;

public sealed class AiApplicationService(
    IAiService aiService,
    ITicketAiContextBuilder contextBuilder,
    IOptions<AiOptions> options,
    ILogger<AiApplicationService> logger)
{
    public async Task<AiResponse?> SummariseTicketAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var context = await contextBuilder.BuildAsync(ticketId, cancellationToken);
        if (context is null)
        {
            return null;
        }

        // Summary context includes internal notes — unlike a future category
        // suggestion, summarising benefits from an agent's internal-only
        // observations, and this endpoint is staff-only to begin with.
        var promptContext = new Dictionary<string, string>
        {
            ["Subject"] = context.Subject,
            ["Description"] = context.Description,
            ["Status"] = context.Status,
            ["Priority"] = context.Priority,
        };
        for (var i = 0; i < context.Messages.Count; i++)
        {
            promptContext[$"Message{i}"] = context.Messages[i].Body;
        }

        var userInput = string.Join(
            "\n",
            context.Messages.Select(m => m.Body).Prepend(context.Description));

        var request = new AiRequest(
            AiFeature.TicketSummary, AiPromptTemplates.TicketSummary, userInput, promptContext);

        return await GenerateAsync(AiFeature.TicketSummary, request, cancellationToken);
    }

    // TODO(follow-up AI feature stories): add SuggestTicketCategoryAsync (blocked —
    // the Ticket domain has no persisted category concept yet; see CRM-69 report),
    // SuggestReplyAsync, SuggestSolutionAsync, and a Chatbot entry point, each
    // calling GenerateAsync with its own AiFeature/AiPromptTemplates constant.

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
}
