using System;
using System.Xml.Serialization;
namespace SajamKnjiga.Core.Models;

/// <summary>
/// Kupovina povezuje posetioca i knjigu i cuva datum, ocenu i istorijsku cenu.
/// </summary>
public class Kupovina : IEntitet
{
    public int Id { get; set; }
    public int PosetilacId { get; set; }
    public int KnjigaId { get; set; }
    public DateTime DatumKupovine { get; set; }
    public int Ocena { get; set; }
    public string Komentar { get; set; } = "";
    // Cena u trenutku evidentiranja kupovine. Null je dozvoljen radi uvoza starih XML zapisa.
    // Null oznacava da cena stare kupovine nije poznata; ne izmisljamo iznos.
    public decimal? CenaPriKupovini { get; set; }
    [XmlIgnore] public Posetilac? Posetilac { get; set; }
    [XmlIgnore] public Knjiga? Knjiga { get; set; }

    public Kupovina() { }

    public Kupovina(int posetilacId, int knjigaId, DateTime datum, int ocena, string komentar)
    {
        PosetilacId = posetilacId;
        KnjigaId = knjigaId;
        DatumKupovine = datum;
        Ocena = ocena;
        Komentar = komentar;
    }
}
