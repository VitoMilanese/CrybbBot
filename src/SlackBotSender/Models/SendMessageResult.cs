namespace SlackBotSender.Models
{
    public class SendMessageResult
    {
        public string ChannelId { get; set; } = string.Empty;
        public Enums.SendMessageResult Result { get; set; } = Enums.SendMessageResult.None;
        public Enums.SendMessageErrorType ErrorType { get; set; } = Enums.SendMessageErrorType.None;
        public string? Error { get; set; }
    }
}
