namespace CRM.Api.CommunicationChannels;

public record ChannelResponse(
    Guid Id,
    string Name,
    ChannelType Type,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreateChannelRequest(string Name, string Type);

public record UpdateChannelRequest(string Name, bool IsEnabled);

public record EmailMessageResponse(
    Guid Id,
    Guid ChannelId,
    string FromAddress,
    string ToAddress,
    string Subject,
    string Body,
    DateTime ReceivedAtUtc,
    Guid? TicketId);

public record IngestEmailRequest(
    string FromAddress,
    string ToAddress,
    string Subject,
    string Body,
    DateTime? ReceivedAtUtc,
    Guid? TicketId);
