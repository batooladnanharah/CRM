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
}
