namespace CrybbBot.Models;

public sealed class Note
{
    public long Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public byte[]? ImageBytes { get; set; }
}