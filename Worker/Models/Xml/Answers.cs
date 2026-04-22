using System.Xml.Serialization;

namespace Worker.Models.Xml;

[XmlRoot(ElementName = "Answers")]
public class Answers
{
    [XmlElement(ElementName = "words")]
    public List<string> Words { get; set; } = new();
}