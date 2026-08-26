using System.Text.Json.Serialization;

namespace CRM.Api.CommunicationChannels;

// Outbound delivery tracking for a TicketMessage sent via the Email channel
// (CRM-50). Deliberately separate from EmailMessage, which records *inbound*
// email ingestion tied to a Channel configuration row — a different concept
// with a different lifecycle; folding outbound delivery tracking into it
// would conflate two unrelated flows rather than deduplicate one.
public sealed class EmailMessageMetadata
{
    public Guid Id { get; set; }
    public Guid TicketMessageId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public EmailDeliveryStatus DeliveryStatus { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmailDeliveryStatus { Pending = 0, Sent = 1, Failed = 2 }
