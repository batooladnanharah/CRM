using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CRM.Api.Ai;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var ai = app.MapGroup("/api/ai").WithTags("Ai");

        ai.MapGet("/status", (IAiService aiService, IOptions<AiOptions> options) =>
        {
            var enabled = options.Value.Enabled;
            var response = new AiStatusResponse(
                enabled,
                enabled ? aiService.ProviderName : null,
                enabled && aiService.IsAvailable);
            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("GetAiStatus")
        .Produces<AiStatusResponse>();

        // Same authorization as ticket reads (TicketEndpoints.cs GET /{id}) — this
        // codebase has no additional per-ticket ownership check beyond the
        // AgentOrAdmin role, so reusing the policy is sufficient to match behaviour.
        ai.MapPost("/tickets/{ticketId:guid}/summary", async (
            Guid ticketId, IOptions<AiOptions> options, IAiService aiService,
            AiApplicationService applicationService, CancellationToken cancellationToken) =>
        {
            if (!options.Value.Enabled || !aiService.IsAvailable)
            {
                return Results.Json(
                    new AiUnavailableResponse("AiUnavailable"), statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var response = await applicationService.SummariseTicketAsync(ticketId, cancellationToken);
            if (response is null)
            {
                return Results.NotFound();
            }

            if (!response.Success)
            {
                return Results.Json(
                    new AiUnavailableResponse("ai.provider_failed"), statusCode: StatusCodes.Status502BadGateway);
            }

            return Results.Ok(response);
        })
        .RequireAuthorization("AgentOrAdmin")
        .WithName("SummariseTicket")
        .Produces<AiResponse>()
        .Produces(StatusCodes.Status404NotFound)
        .Produces<AiUnavailableResponse>(StatusCodes.Status502BadGateway)
        .Produces<AiUnavailableResponse>(StatusCodes.Status503ServiceUnavailable);

        return app;
    }
}
