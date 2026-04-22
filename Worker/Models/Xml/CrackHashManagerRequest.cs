using System.Xml.Serialization;

namespace Worker.Models.Xml;

[XmlRoot(ElementName = "CrackHashManagerRequest", 
    Namespace = "http://ccfit.nsu.ru/schema/crack-hash-request")]
[XmlType(AnonymousType = true, Namespace = "http://ccfit.nsu.ru/schema/crack-hash-request")]
public class CrackHashManagerRequest
{
    [XmlElement(ElementName = "RequestId", Order = 0)]
    public string RequestId { get; set; } = string.Empty;

    [XmlElement(ElementName = "PartNumber", Order = 1)]
    public int PartNumber { get; set; }

    [XmlElement(ElementName = "PartCount", Order = 2)]
    public int PartCount { get; set; }

    [XmlElement(ElementName = "Hash", Order = 3)]
    public string Hash { get; set; } = string.Empty;

    [XmlElement(ElementName = "MaxLength", Order = 4)]
    public int MaxLength { get; set; }

    [XmlElement(ElementName = "Alphabet", Order = 5)]
    public Alphabet Alphabet { get; set; } = new();
}