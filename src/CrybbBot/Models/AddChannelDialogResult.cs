namespace CrybbBot.Models;

public sealed class AddChannelDialogResult
{
    public bool Yes { get; init; }
    public string ChannelId { get; init; } = string.Empty;
    public string? Alias { get; init; }
}
