using CrybbBot.ViewModels;

namespace CrybbBot.Models;

public sealed class EditChannelDialogResult
{
    public bool Yes { get; init; }
    public string? NewId { get; init; }
    public string? OldId { get; init; }
    public string? NewAlias { get; init; }
    public string? OldAlias { get; init; }
    public ChannelInBundleWithFlagViewModel[]? Bundles { get; init; }
}
