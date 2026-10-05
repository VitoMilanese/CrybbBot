using System;

namespace CrybbBot.Models;

public sealed class Message
{
    public Guid ID { get; set; }
    public string Text { get; set; } = string.Empty;
    public Guid? AttachmentsBundleID { get; set; }
    public int AttachmentsNumber { get; set; }
    public Guid? ChannelsBundleID { get; set; }
    public DateTime InsertDT { get; set; }
    public DateTime? ScheduleDT { get; set; }
    public DateTime? SendingDT { get; set; }
    public bool Sent { get; set; }
    public string? SendingError { get; set; }
}
