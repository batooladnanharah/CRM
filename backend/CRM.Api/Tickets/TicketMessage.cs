using System.Text.Json.Serialization;

namespace CRM.Api.Tickets;

public class TicketMessage
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }

    // No cross-context navigation to the author — same style as CustomerNote.AuthorId;
    // display name is resolved via a separate AuthDbContext query at read time.
    public Guid AuthorUserId { get; set; }

    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }

    // Defaults to Web for every pre-CRM-50 row and every existing caller that
    // omits the field, so the existing web-reply flow is unaffected.
    public MessageChannel Channel { get; set; } = MessageChannel.Web;

    public DateTime CreatedAtUtc { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MessageChannel { Web = 0, Email = 1 }
