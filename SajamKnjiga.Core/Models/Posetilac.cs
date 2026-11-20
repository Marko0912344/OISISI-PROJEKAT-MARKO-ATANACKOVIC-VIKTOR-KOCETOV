using System;
using System.Collections.Generic;
using System.Xml.Serialization;
namespace SajamKnjiga.Core.Models;

/// <summary>
/// Model posetioca sa clanstvom, ocenama, kupljenim knjigama i listom zelja.
/// </summary>
public class Posetilac : IEntitet
{
    public int Id { get; set; }
    public string Ime { get; set; } = "";
    public string Prezime { get; set; } = "";
    public DateTime DatumRodjenja { get; set; }
    public string Telefon { get; set; } = "";
    public string Email { get; set; } = "";
    public string BrojClanskeKarte { get; set; } = "";
    public int TrenutnaGodinaClanstva { get; set; }
    public StatusPosetioca Status { get; set; }
    public double ProsecnaOcenaRecenzija { get; set; }
    // Povezani podaci se rekonstruisu preko identifikatora posle ucitavanja.
    public int AdresaId { get; set; }
    // XmlIgnore sprecava duple zapise kupovina, zelja i adresa u XML-u.
    [XmlIgnore] public Adresa? Adresa { get; set; }
    [XmlIgnore] public List<Kupovina> Kupovine { get; set; } = new();
    [XmlIgnore] public List<Knjiga> ListaZelja { get; set; } = new();

    public Posetilac() { }

    public Posetilac(string ime, string prezime, DateTime rodjenje, Adresa adresa,
        string telefon, string email, string karta, int godina, StatusPosetioca status)
    {
        Ime = ime;
        Prezime = prezime;
        DatumRodjenja = rodjenje;
        Adresa = adresa;
        AdresaId = adresa.Id;
        Telefon = telefon;
        Email = email;
        BrojClanskeKarte = karta;
        TrenutnaGodinaClanstva = godina;
        Status = status;
    }

    public override string ToString() => $"{Ime} {Prezime}";
}
