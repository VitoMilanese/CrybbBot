using System.Xml.Serialization;
using Newtonsoft.Json;

namespace DataLayer.Models;

public class AttachedBundle
{
    public Guid ID { get; set; }
    public Guid MessageID { get; set; }
    public Guid BundleID { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual Message? Message { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public virtual Bundle? Bundle { get; set; }
}
