using System;
using System.Collections.Generic;

namespace CrybbBot.Models;

public sealed class ChannelBundle
{
    public long BundleId { get; set; }

    public List<Channel> Channels { get; set; } = new List<Channel>();

    private string? _alias;
    public string? Alias
    {
        get => string.IsNullOrWhiteSpace(_alias) ? BundleId.ToString() : _alias;
        set => _alias = value;
    }

    public static ChannelBundle Default { get; } = new ChannelBundle(0, Constants.All);

    public ChannelBundle() { }

    public ChannelBundle(long id, string alias) => (BundleId, Alias) = (id, alias);
}
