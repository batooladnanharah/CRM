namespace CRM.Api.Customers;

public enum CustomerInteractionType
{
    TicketCreated,
    CustomerMessage,
    AgentReply,
    StatusChange,
    Assignment,
    InternalNote,
    Email,
    WhatsApp,
    LiveChat,
    Sms,
    WebForm,
}

public class CustomerInteraction
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public CustomerInteractionType Type { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }        // UTC
    public Guid? ActorId { get; set; }              // nullable — system-generated events have no actor
    public string? ActorName { get; set; }          // denormalised for MVP; avoids a cross-DbContext join to Auth.Users

    // No FK — there is no Tickets table yet (no ticket module exists in the repo).
    public Guid? TicketId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
