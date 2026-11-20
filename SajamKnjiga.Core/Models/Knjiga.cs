using System;
using System.Collections.Generic;
using System.Xml.Serialization;
namespace SajamKnjiga.Core.Models;

/// <summary>
/// Model knjige sa ISBN-om, izdavacem i povezanim autorima i posetiocima.
/// </summary>
public class Knjiga : IEntitet
{
    public int Id { get; set; }
    public string ISBN { get; set; } = "";
    public string Naziv { get; set; } = "";
    public string Zanr { get; set; } = "";
    public int GodinaIzdanja { get; set; }
    public decimal Cena { get; set; }
    public int BrojStrana { get; set; }
    // Vezni kljucevi su serijalizovani; povezani objekti su samo za prikaz.
    public int? IzdavacId { get; set; }
    [XmlIgnore] public Izdavac? Izdavac { get; set; }
    [XmlIgnore] public List<Autor> Autori { get; set; } = new();
    [XmlIgnore] public List<Posetilac> PosetiociKojiSuKupili { get; set; } = new();
    [XmlIgnore] public List<Posetilac> PosetiociNaListiZelja { get; set; } = new();

    public override string ToString() => $"{Naziv} ({ISBN})";
}
