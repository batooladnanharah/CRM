namespace CRM.Api.Ai;

// Prompt text kept out of endpoint/service files so a real provider adapter
// can consume the same instructions later without re-plumbing call sites.
public static class AiPromptTemplates
{
    public const string TicketSummary =
        "Summarise this support ticket conversation for an agent in 2-3 sentences. " +
        "Focus on the customer's issue and current state; do not invent facts not present in the context.";

    public const string TicketCategorization =
        "Categorise this support ticket into a single short category label based on its subject and description.";

    public const string SuggestedReply =
        "Draft a short, professional reply an agent could send to the customer, addressing their issue.";

    public const string SuggestedSolution =
        "Suggest a concise, actionable troubleshooting step for this ticket's issue.";

    public const string Chatbot =
        "Respond helpfully and concisely to the user's message in the context of this support conversation.";
}
