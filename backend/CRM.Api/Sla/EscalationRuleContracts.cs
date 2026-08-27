namespace CRM.Api.Sla;

public sealed record EscalationRuleDto(
    Guid Id, string Name, EscalationTrigger Trigger,
    bool NotifyAgent, bool NotifyManager, bool IsActive,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record CreateEscalationRuleRequest(
    string Name, EscalationTrigger Trigger,
    bool NotifyAgent, bool NotifyManager, bool IsActive);

public sealed record UpdateEscalationRuleRequest(
    string Name, EscalationTrigger Trigger,
    bool NotifyAgent, bool NotifyManager, bool IsActive);
