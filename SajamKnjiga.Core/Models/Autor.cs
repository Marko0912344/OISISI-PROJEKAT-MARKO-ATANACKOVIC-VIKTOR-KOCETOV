using System;
using System.Collections.Generic;
using System.Xml.Serialization;
namespace SajamKnjiga.Core.Models;

/// <summary>
/// Model autora: licni podaci, iskustvo i veze prema adresi i knjigama.
/// </summary>
public class Autor : IEntitet
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";
    public string Prezime { get; set; } = "";
    public DateTime DatumRodjenja { get; set; }
    public string Telefon { get; set; } = "";
    public string Email { get; set; } = "";
    public string BrojLicneKarte { get; set; } = "";
    public int GodineIskustva { get; set; }
    // U XML-u cuvamo ID adrese, a ne celu ugnjezdenu adresu.
    public int AdresaId { get; set; }
    // Navigacione reference se popunjavaju iz povezanih XML datoteka.
    [XmlIgnore] public Adresa? Adresa { get; set; }
    [XmlIgnore] public List<Knjiga> Knjige { get; set; } = new();

    public Autor() { }

    public Autor(string ime, string prezime, DateTime datumRodjenja, Adresa adresa,
        string telefon, string email, string brojLicneKarte, int godineIskustva)
    {
        Ime = ime;
        Prezime = prezime;
        DatumRodjenja = datumRodjenja;
        Adresa = adresa;
        AdresaId = adresa.Id;
        Telefon = telefon;
        Email = email;
        BrojLicneKarte = brojLicneKarte;
        GodineIskustva = godineIskustva;
    }

    public override string ToString() => $"{Ime} {Prezime}";
}
