namespace SlackBotSender
{
    public class ImageAttachment
    {
        public string FileId { get; set; } = string.Empty;
        public string AlternativeText { get; set; } = "image";

        public ImageAttachment()
        {
        }

        public ImageAttachment(string fileId, string alternativeText) => (FileId, AlternativeText) = (fileId, alternativeText);
    }
}
