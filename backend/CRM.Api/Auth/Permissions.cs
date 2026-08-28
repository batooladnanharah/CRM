namespace CRM.Api.Auth;

// Single source of truth for named permissions. Each constant maps 1:1 to the
// access level an existing named authorization policy ("AdminOnly" /
// "AgentOrAdmin" / "CustomerPortal") already grants at a given route group —
// this catalogue does not change who can do what, it gives the existing
// three-tier access model named, per-capability policies instead of bare role
// checks, so authorization intent reads at the route and survives future role
// changes without touching every endpoint.
public static class Permissions
{
    public const string CustomersManage = "customers.manage";

    public const string TicketsManage = "tickets.manage";
    public const string TicketsEscalate = "tickets.escalate";

    public const string QuickRepliesView = "quickReplies.view";
    public const string QuickRepliesManage = "quickReplies.manage";

    public const string KnowledgeBaseView = "kb.view";
    public const string KnowledgeBaseManage = "kb.manage";

    public const string KnowledgeBaseCategoriesView = "kb.categories.view";
    public const string KnowledgeBaseCategoriesManage = "kb.categories.manage";

    public const string CommunicationChannelsView = "channels.view";
    public const string CommunicationChannelsManage = "channels.manage";

    public const string SlaManage = "sla.manage";
    public const string ManageSlaEscalationRules = "sla.escalation.manage";
    public const string ReportsView = "reports.view";
    public const string SecurityAdmin = "security.admin";
    public const string PortalAccess = "portal.access";

    public static IReadOnlyCollection<string> All { get; } =
    [
        CustomersManage,
        TicketsManage, TicketsEscalate,
        QuickRepliesView, QuickRepliesManage,
        KnowledgeBaseView, KnowledgeBaseManage,
        KnowledgeBaseCategoriesView, KnowledgeBaseCategoriesManage,
        CommunicationChannelsView, CommunicationChannelsManage,
        SlaManage, ManageSlaEscalationRules, ReportsView, SecurityAdmin, PortalAccess,
    ];
}
