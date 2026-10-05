using SlackBotSender.Enums;

namespace SlackBotSender.Models
{
    public sealed class Attachment
    {
        public string FilePath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public AttachmentType Type { get; set; } = AttachmentType.Unknown;

        public string FieldId { get; internal set; } = string.Empty;
        public Dictionary<string, string>? FileIds { get; internal set; }
    }
}
