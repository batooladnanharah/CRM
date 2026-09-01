using CRM.Api.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CRM.Api.Tickets;

// CRM-62 — Recommended MVP Rule from the intake:
//   1. Match ticket department (no-op today — see the class comment below).
//   2. Find active agents.
//   3. Prefer available agents.
//   4. Select the agent with the fewest active tickets.
//   5. If no agent is available, leave the ticket unassigned.
//
// Department matching is deliberately a no-op: neither Ticket nor User expose
// a DepartmentId anywhere in this codebase (grepped before writing this file),
// and the story explicitly forbids introducing a department system. If/when a
// DepartmentId lands on both sides, add a single .Where(...) clause here.
//
// Workload is counted at the database level (GroupBy/Count translated to SQL
// by EF Core) — ticket rows are never loaded into memory to compute it.
public sealed class TicketAssignmentService(
    AuthDbContext authDb,
    TicketDbContext ticketDb,
    IOptions<AutoAssignmentOptions> options,
    ILogger<TicketAssignmentService> logger) : ITicketAssignmentService
{
    public async Task<Guid?> TryAutoAssignAsync(Ticket ticket, CancellationToken ct)
    {
        if (!options.Value.Enabled)
        {
            return null;
        }

        // AuthDbContext and TicketDbContext are separate DbContext instances (same
        // physical database, separate bounded contexts — same style as every other
        // cross-context lookup in this codebase, e.g. TicketEndpoints.ToResponseAsync).
        // EF Core cannot translate a single query joining IQueryables from two
        // different contexts, so eligibility and workload are queried separately —
        // both still resolved at the database level (no ticket rows loaded to
        // compute counts), only the small eligible-agent-id list is held in memory.
        var eligibleAgentIds = await authDb.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.IsAvailable && u.Roles.Contains(Roles.Agent))
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (eligibleAgentIds.Count == 0)
        {
            logger.LogDebug("auto_assignment_no_eligible_agent ticketId={TicketId}", ticket.Id);
            return null;
        }

        // Database-level GroupBy/Count (EF Core translates this to SQL) over
        // active (non-terminal) tickets assigned to an eligible agent. Agents
        // with zero active tickets simply have no row here — defaulted to 0 below.
        var activeCountsByAgent = await ticketDb.Tickets
            .AsNoTracking()
            .Where(t => t.AssigneeUserId != null && eligibleAgentIds.Contains(t.AssigneeUserId.Value)
                && TicketStatusRules.IsActiveWorkloadStatus(t.Status))
            .GroupBy(t => t.AssigneeUserId!.Value)
            .Select(g => new { AgentId = g.Key, ActiveCount = g.Count() })
            .ToDictionaryAsync(g => g.AgentId, g => g.ActiveCount, ct);

        // Deterministic tie-break: lowest active count, then ascending agent id.
        // No Random/Guid.NewGuid anywhere in the ordering.
        var selected = eligibleAgentIds
            .OrderBy(id => activeCountsByAgent.GetValueOrDefault(id, 0))
            .ThenBy(id => id)
            .First();

        return selected;
    }
}
