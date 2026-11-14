
using System;
using System.Xml.Serialization;

namespace SajamKnjiga.Core.Models;

public class Kupovina : IEntitet
{
    public int Id { get; set; }

    public int PosetilacId { get; set; }

    public int KnjigaId { get; set; }

    public DateTime DatumKupovine { get; set; }

    public int Ocena { get; set; }

    public string Komentar { get; set; } = "";

    [XmlIgnore]
    public Posetilac? Posetilac { get; set; }

    [XmlIgnore]
    public Knjiga? Knjiga { get; set; }

    public Kupovina()
    {
    }
}
