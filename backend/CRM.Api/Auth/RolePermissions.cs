namespace CRM.Api.Auth;

// Role -> permission set. Mirrors the access this codebase's three named
// policies (AdminOnly / AgentOrAdmin / CustomerPortal) already grant at every
// route group they protect — see Permissions.cs for the mapping to routes.
// Only Admin, Agent, Customer exist; the codebase (and the SDD, which leaves
// "roles beyond agent" an open question — docs/sdd/areas/10-security-administration.md,
// OQ-02) never defined a fourth role, so none is introduced here.
public static class RolePermissions
{
    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Map =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Roles.Admin] = new HashSet<string>(Permissions.All),
            [Roles.Agent] = new HashSet<string>
            {
                Permissions.CustomersManage,
                Permissions.TicketsManage,
                Permissions.QuickRepliesView,
                Permissions.KnowledgeBaseView,
                Permissions.KnowledgeBaseCategoriesView,
                Permissions.CommunicationChannelsView,
            },
            [Roles.Customer] = new HashSet<string> { Permissions.PortalAccess },
        };

    public static IReadOnlySet<string> For(string role) =>
        Map.TryGetValue(role, out var set) ? set : new HashSet<string>();

    public static IReadOnlySet<string> ForRoles(IEnumerable<string> roles)
    {
        var result = new HashSet<string>();
        foreach (var role in roles)
        {
            result.UnionWith(For(role));
        }
        return result;
    }
}
