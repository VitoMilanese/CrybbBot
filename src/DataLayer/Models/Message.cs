using System.Xml.Serialization;
using Newtonsoft.Json;

namespace DataLayer.Models;

public class Message
{
    public Guid ID { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime InsertDT { get; set; }
    public DateTime? ScheduleDT { get; set; }
    public DateTime? SendingDT { get; set; }
    public bool Managed { get; set; }
    public DateTime? LastUpdateDT { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual List<AttachedBundle>? Bundles { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual List<AttachedChannel>? Channels { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual List<AttachedAttachment>? Attachments { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual List<SendingError>? SendingErrors { get; set; }
}
