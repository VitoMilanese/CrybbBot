namespace SlackBotSender.Enums
{
    public enum AttachmentType
    {
        Unknown = 0,
        Image = 1,
        File = 2,
        FileReference = 3
    }

    public enum SendMessageResult
    {
        None = 0,
        Sent = 1,
        Failed = 2
    }

    public enum SendMessageErrorType
    {
        None = 0,
        Unknown = 1,
        UploadingAttachment = 2,
        SendingMessage = 3
    }
}
