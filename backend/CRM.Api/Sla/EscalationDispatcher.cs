using CRM.Api.Auth;
using CRM.Api.Notifications;
using CRM.Api.Tickets;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Sla;

public interface IEscalationDispatcher
{
    Task DispatchAsync(Ticket ticket, SlaObjectiveKind objective, EscalationTrigger trigger, CancellationToken ct);
}

// Fires escalation rules matching a trigger for one ticket/objective. Never
// throws out — every rule is processed independently in a try/catch so one
// bad rule (or a transient DB error) never blocks the SLA worker tick or the
// evaluation of other rules. Idempotent via the unique index on
// EscalationEvent (TicketId, RuleId, Trigger, Objective).
public sealed class EscalationDispatcher(
    TicketDbContext ticketDb,
    AuthDbContext authDb,
    INotificationService notifications,
    IManagerResolver managerResolver,
    ILogger<EscalationDispatcher> logger) : IEscalationDispatcher
{
    public async Task DispatchAsync(Ticket ticket, SlaObjectiveKind objective, EscalationTrigger trigger, CancellationToken ct)
    {
        var rules = await ticketDb.EscalationRules
            .Where(r => r.IsActive && r.Trigger == trigger)
            .ToListAsync(ct);

        if (rules.Count == 0)
        {
            return;
        }

        foreach (var rule in rules)
        {
            try
            {
                await DispatchRuleAsync(ticket, objective, trigger, rule, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex,
                    "escalation_dispatch_failed ticketId={TicketId} ruleId={RuleId} trigger={Trigger} objective={Objective}",
                    ticket.Id, rule.Id, trigger, objective);
            }
        }
    }

    private async Task DispatchRuleAsync(
        Ticket ticket, SlaObjectiveKind objective, EscalationTrigger trigger, EscalationRule rule, CancellationToken ct)
    {
        var alreadyProcessed = await ticketDb.EscalationEvents.AsNoTracking().AnyAsync(
            e => e.TicketId == ticket.Id && e.RuleId == rule.Id && e.Trigger == trigger && e.Objective == objective, ct);
        if (alreadyProcessed)
        {
            return;
        }

        var evt = new EscalationEvent
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            RuleId = rule.Id,
            Trigger = trigger,
            Objective = objective,
            ExecutedAt = DateTimeOffset.UtcNow,
        };

        ticketDb.EscalationEvents.Add(evt);
        try
        {
            await ticketDb.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique-index violation — another tick/replica already recorded
            // this event. Treat as already-processed and stop here.
            ticketDb.Entry(evt).State = EntityState.Detached;
            return;
        }

        string? agentDisplay = null;
        var agentNotified = false;
        var managerNotified = false;

        if (rule.NotifyAgent && ticket.AssigneeUserId is Guid agentId)
        {
            var agent = await authDb.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == agentId, ct);
            if (agent is { IsActive: true })
            {
                agentDisplay = agent.Name;
                await notifications.CreateAsync(BuildNotification(agent.Id, ticket, objective, trigger, forManager: false, agentDisplay), ct);
                agentNotified = true;
            }
        }

        if (rule.NotifyManager)
        {
            var managerIds = await managerResolver.ResolveManagersAsync(ticket.Id, ct);
            foreach (var managerId in managerIds)
            {
                await notifications.CreateAsync(BuildNotification(managerId, ticket, objective, trigger, forManager: true, agentDisplay), ct);
                managerNotified = true;
            }
        }

        evt.AgentNotified = agentNotified;
        evt.ManagerNotified = managerNotified;

        ticketDb.TicketHistory.Add(new TicketHistoryEntry
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangeType = TicketChangeType.Escalated,
            OldValue = null,
            NewValue = $"{trigger}:{objective}",
            Reason = $"SLA escalation rule '{rule.Name}' fired ({trigger} / {objective}).",
            ChangedByUserId = TicketEscalationService.SystemActorId,
            ChangedAtUtc = DateTime.UtcNow,
        });

        await ticketDb.SaveChangesAsync(ct);
    }

    private static Notification BuildNotification(
        Guid recipientUserId, Ticket ticket, SlaObjectiveKind objective, EscalationTrigger trigger,
        bool forManager, string? agentDisplay)
    {
        var (type, title, message) = trigger switch
        {
            EscalationTrigger.AtRisk => (
                NotificationType.SlaAtRisk,
                "SLA At Risk",
                $"Ticket #{ticket.Id} is approaching its {objective} SLA."),
            EscalationTrigger.Breached when forManager => (
                NotificationType.SlaBreached,
                "SLA Breached",
                $"Ticket #{ticket.Id} assigned to {agentDisplay ?? "(unassigned)"} has breached its {objective} SLA."),
            _ => (
                NotificationType.SlaBreached,
                "SLA Breached",
                $"Ticket #{ticket.Id} has exceeded its {objective} SLA."),
        };

        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = recipientUserId,
            Type = type,
            Title = title,
            Message = message,
            TicketId = ticket.Id,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }
}
