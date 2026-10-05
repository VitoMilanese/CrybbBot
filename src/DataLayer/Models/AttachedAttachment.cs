using System.Xml.Serialization;
using DataLayer.Enums;
using Newtonsoft.Json;

namespace DataLayer.Models;

public class AttachedAttachment
{
    public Guid ID { get; set; }
    public Guid MessageID { get; set; }
    public AttachmentType Type { get; set; }
    public string Path { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? FieldID { get; set; }
    public string? ContentText { get; set; }
    public byte[]? ContentBin { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual Message? Message { get; set; }
}