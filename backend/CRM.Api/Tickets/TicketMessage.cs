using System.Text.Json.Serialization;

namespace CRM.Api.Tickets;

public class TicketMessage
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    // No cross-context navigation to the author — same style as CustomerNote.AuthorId;
    // display name is resolved via a separate AuthDbContext query at read time.
    // Nullable as of the customer-portal reply feature: exactly one of
    // AuthorUserId (staff) / AuthorCustomerId (customer) is set per message —
    // never both, never neither.
    public Guid? AuthorUserId { get; set; }

    // Set only for customer-portal-authored replies (see CustomerPortalEndpoints
    // message-create path). Null for every staff-authored message (agent replies
    // and internal notes), which is every pre-CRM-74 row.
    public Guid? AuthorCustomerId { get; set; }

    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }

    // Defaults to Web for every pre-CRM-50 row and every existing caller that
    // omits the field, so the existing web-reply flow is unaffected.
    public MessageChannel Channel { get; set; } = MessageChannel.Web;

    public DateTime CreatedAtUtc { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageChannel { Web = 0, Email = 1 }
