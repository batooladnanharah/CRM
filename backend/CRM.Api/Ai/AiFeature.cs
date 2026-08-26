using System.Text.Json.Serialization;

namespace CRM.Api.Ai;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AiFeature
{
    TicketSummary,
    TicketCategorization,
    SuggestedReply,
    SuggestedSolution,
    Chatbot,
}
