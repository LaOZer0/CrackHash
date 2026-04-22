using System.Xml.Serialization;

namespace Manager.Models.Xml;

[XmlRoot(ElementName = "Alphabet")]
public class Alphabet
{
    [XmlElement(ElementName = "symbols")]
    public List<string> Symbols { get; set; } = new();
}