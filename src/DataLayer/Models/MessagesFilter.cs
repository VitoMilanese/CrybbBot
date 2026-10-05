namespace DataLayer.Models;

public class MessagesFilter
{
    public enum AttachmentsType : byte
    {
        None = 0b_000,
        Image = 0b_001,
        File = 0b_010,
        ImageAndFile = 0b_011,
        FileReference = 0b_100,
        ImageAndFileReference = 0b_101,
        FileAndFileReference = 0b_110,
        All = 0b_111
    }

    public DateTime? ScheduledFrom { get; set; }
    public DateTime? ScheduledTo { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public bool Ascending { get; set; }
    public bool? Scheduled { get; set; }
    public bool? Sent { get; set; }
    public bool? Errors { get; set; }
    public bool? Attachments { get; set; }
    public AttachmentsType AttType { get; set; } = AttachmentsType.All;
    public bool ContainingText { get; set; }
    public string? Text { get; set; }
    public bool SearchByChannels { get; set; }
    public List<Guid>? Bundles { get; set; }
    public List<Guid>? Channels { get; set; }

    public static MessagesFilter Default => new MessagesFilter
    {
        CreatedFrom = DateTime.Today.AddMonths(-1),
        CreatedTo = DateTime.Today.AddDays(1),
        ScheduledFrom = null,
        ScheduledTo = null,
        Ascending = true,
        Scheduled = null,
        Sent = null,
        Errors = null,
        Attachments = null,
        AttType = AttachmentsType.All,
        ContainingText = false,
        Text = null,
        SearchByChannels = false
    };

    public bool SelectAll => !(Scheduled.HasValue || Sent.HasValue || Errors.HasValue || Attachments.HasValue || (ContainingText && !string.IsNullOrWhiteSpace(Text)));
}
