using System.Xml.Serialization;
using Newtonsoft.Json;

namespace DataLayer.Models;

public class AttachedChannel
{
    public Guid ID { get; set; }
    public Guid MessageID { get; set; }
    public Guid ChannelID { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual Message? Message { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual Channel? Channel { get; set; }
}
