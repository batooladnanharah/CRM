namespace CRM.Api.Security;

public static class AuditActions
{
    public const string LoginSucceeded = "user.login.succeeded";
    public const string LoginFailed = "user.login.failed";
    public const string RoleAssigned = "user.role.assigned";
    public const string UserDisabled = "user.disabled";
    public const string UserEnabled = "user.enabled";
    public const string UserCreated = "user.created";
    public const string UserUpdated = "user.updated";
    public const string AccessDenied = "security.access.denied";

    // Added by CRM-84 so every endpoint that mutates customer/ticket data is
    // audited, matching the naming convention above (resource.event).
    public const string CustomerCreated = "customer.created";
    public const string CustomerUpdated = "customer.updated";
    public const string CustomerNoteAdded = "customer.note.added";
    public const string CustomerNoteUpdated = "customer.note.updated";
    public const string CustomerNoteRemoved = "customer.note.removed";
    public const string CustomerAttachmentAdded = "customer.attachment.added";
    public const string CustomerAttachmentRemoved = "customer.attachment.removed";
    public const string TicketCreated = "ticket.created";
    public const string TicketAssigned = "ticket.assigned";
    public const string TicketStatusChanged = "ticket.status.changed";
    public const string TicketPriorityChanged = "ticket.priority.changed";
    public const string TicketEscalated = "ticket.escalated";
    public const string TicketMessageAdded = "ticket.message.added";
    public const string TicketAttachmentAdded = "ticket.attachment.added";
    public const string TicketAttachmentRemoved = "ticket.attachment.removed";

    // Added to audit mutations on QuickReplies, KnowledgeBase (articles and
    // categories), CommunicationChannels, and Sla (policies and escalation
    // rules) — string values are wired into frontend i18n labels, do not change.
    public const string QuickReplyCreated = "quickReply.created";
    public const string QuickReplyUpdated = "quickReply.updated";
    public const string QuickReplyRemoved = "quickReply.removed";
    public const string KnowledgeBaseArticleCreated = "knowledgeBase.article.created";
    public const string KnowledgeBaseArticleUpdated = "knowledgeBase.article.updated";
    public const string KnowledgeBaseArticlePublished = "knowledgeBase.article.published";
    public const string KnowledgeBaseArticleArchived = "knowledgeBase.article.archived";
    public const string KnowledgeBaseArticleRemoved = "knowledgeBase.article.removed";
    public const string KnowledgeBaseCategoryCreated = "knowledgeBase.category.created";
    public const string KnowledgeBaseCategoryUpdated = "knowledgeBase.category.updated";
    public const string KnowledgeBaseCategoryRemoved = "knowledgeBase.category.removed";
    public const string CommunicationChannelCreated = "communicationChannel.created";
    public const string CommunicationChannelUpdated = "communicationChannel.updated";
    public const string CommunicationChannelRemoved = "communicationChannel.removed";
    public const string SlaPolicyCreated = "sla.policy.created";
    public const string SlaPolicyUpdated = "sla.policy.updated";
    public const string SlaPolicyActivated = "sla.policy.activated";
    public const string SlaPolicyDeactivated = "sla.policy.deactivated";
    public const string SlaPolicyDefaultSet = "sla.policy.defaultSet";
    public const string SlaPolicyRemoved = "sla.policy.removed";
    public const string EscalationRuleCreated = "sla.escalationRule.created";
    public const string EscalationRuleUpdated = "sla.escalationRule.updated";
    public const string EscalationRuleActivated = "sla.escalationRule.activated";
    public const string EscalationRuleDeactivated = "sla.escalationRule.deactivated";
    public const string EscalationRuleRemoved = "sla.escalationRule.removed";
}
