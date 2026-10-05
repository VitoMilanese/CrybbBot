using System.Xml.Serialization;
using Newtonsoft.Json;

namespace DataLayer.Models;

public class Channel
{
    public Guid ID { get; set; }
    public string SlackChannelID { get; set; } = string.Empty;
    public string? Alias { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual bool CanBeDeleted { get; set; } = true;

    [XmlIgnore]
    [JsonIgnore]
    public virtual List<AttachedChannel>? AttachedChannels { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual List<SendingError>? SendingErrors { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual List<BundleChannelMapping>? BundleChannelMappings { get; set; }
}
