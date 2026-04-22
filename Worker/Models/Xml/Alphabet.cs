using System.Xml.Serialization;

namespace Worker.Models.Xml;

[XmlRoot(ElementName = "Alphabet")]
public class Alphabet
{
    [XmlElement(ElementName = "symbols")]
    public List<string> Symbols { get; set; } = new();
}
