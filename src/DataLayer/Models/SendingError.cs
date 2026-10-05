using System.Xml.Serialization;
using Newtonsoft.Json;

namespace DataLayer.Models;

public class SendingError
{
    public Guid ID { get; set; }
    public Guid MessageID { get; set; }
    public Guid ChannelID { get; set; }
    public string? Details { get; set; }
    public int Type { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual Message? Message { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual Channel? Channel { get; set; }
}
