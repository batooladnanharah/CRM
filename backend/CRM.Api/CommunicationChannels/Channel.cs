using System.Text.Json.Serialization;

namespace CRM.Api.CommunicationChannels;

public class Channel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ChannelType Type { get; set; }
    public bool IsEnabled { get; set; } = true;

    // No optimistic concurrency token yet — last write wins. Acceptable for
    // this MVP scaffolding story; revisit if concurrent edits become common.
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChannelType { Email = 1 }
