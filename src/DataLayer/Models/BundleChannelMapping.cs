using System.Xml.Serialization;
using Newtonsoft.Json;

namespace DataLayer.Models;

public class BundleChannelMapping
{
    public Guid ID { get; set; }
    public Guid BundleID { get; set; }
    public Guid ChannelID { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual Bundle? Bundle { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual Channel? Channel { get; set; }
}