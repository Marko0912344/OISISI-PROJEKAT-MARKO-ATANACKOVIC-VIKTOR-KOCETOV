
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SajamKnjiga.Core.Models;

public class Izdavac : IEntitet
{
    public int Id { get; set; }

    public string Naziv { get; set; } = "";

    public int? SefId { get; set; }

    [XmlIgnore]
    public Autor? Sef { get; set; }

    [XmlIgnore]
    public List<Autor> Autori { get; set; } = new();

    [XmlIgnore]
    public List<Knjiga> Knjige { get; set; } = new();

    public Izdavac()
    {
    }
}
