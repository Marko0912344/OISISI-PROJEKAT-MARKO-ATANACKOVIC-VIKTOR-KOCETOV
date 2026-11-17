using System.Xml.Serialization;
namespace SajamKnjiga.Core.Models;

public class Knjiga : IEntitet
{
    public int Id { get; set; }
    public string ISBN { get; set; } = "";
    public string Naziv { get; set; } = "";
    public string Zanr { get; set; } = "";
    public int GodinaIzdanja { get; set; }
    public decimal Cena { get; set; }
    public int BrojStrana { get; set; }
    public int? IzdavacId { get; set; }
    [XmlIgnore] public List<Autor> Autori { get; set; } = new();
    // Do spajanja sa Studentom 1, posetioci se identifikuju njihovim ID-jevima.
    // Liste se rekonstruišu iz Kupovine.xml i StavkeListeZelja.xml.
    [XmlIgnore] public List<int> IdKupaca { get; set; } = new();
    [XmlIgnore] public List<int> IdPosetilacaSaZeljom { get; set; } = new();
}
