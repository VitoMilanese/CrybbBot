using System.Xml.Serialization;
using Newtonsoft.Json;

namespace DataLayer.Models;

public class Bundle
{
    public Guid ID { get; set; }
    public string? Alias { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual List<BundleChannelMapping>? BundleChannelMappings { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual List<Channel>? Channels => BundleChannelMappings?.Where(p => p.Channel != null)?.Select(p => p.Channel!)?.ToList();
}
