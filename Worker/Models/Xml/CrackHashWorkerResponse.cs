using System.Xml.Serialization;

namespace Worker.Models.Xml;

[XmlRoot(ElementName = "CrackHashWorkerResponse", 
    Namespace = "http://ccfit.nsu.ru/schema/crack-hash-response")]
[XmlType(AnonymousType = true, Namespace = "http://ccfit.nsu.ru/schema/crack-hash-response")]
public class CrackHashWorkerResponse
{
    [XmlElement(ElementName = "RequestId", Order = 0)]
    public string RequestId { get; set; } = string.Empty;

    [XmlElement(ElementName = "PartNumber", Order = 1)]
    public int PartNumber { get; set; }

    [XmlElement(ElementName = "Answers", Order = 2)]
    public Answers Answers { get; set; } = new();
}