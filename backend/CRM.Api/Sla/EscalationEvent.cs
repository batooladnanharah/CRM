namespace CRM.Api.Sla;

public sealed class EscalationEvent
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid RuleId { get; set; }
    public EscalationTrigger Trigger { get; set; }
    public SlaObjectiveKind Objective { get; set; }
    public DateTimeOffset ExecutedAt { get; set; }

    // Whether a notification recipient was actually resolved/notified for
    // each side of the rule — tracked so the ticket-details escalation
    // surface can honestly report the outcome (see EscalationDispatcher).
    public bool AgentNotified { get; set; }
    public bool ManagerNotified { get; set; }
}
