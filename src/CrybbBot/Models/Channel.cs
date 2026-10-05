using System;

namespace CrybbBot.Models;

public sealed class Channel
{
    public long ChannelId { get; set; }

    private string? _alias;
    public string? Alias
    {
        get => string.IsNullOrWhiteSpace(_alias) ? ChannelId.ToString() : _alias;
        set => _alias = value;
    }

    public Channel() { }

    public Channel(long id, string alias) => (ChannelId, Alias) = (id, alias);
}
