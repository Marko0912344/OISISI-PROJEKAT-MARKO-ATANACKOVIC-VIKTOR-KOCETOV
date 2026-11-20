using System;
using System.Collections.Generic;
using System.Xml.Serialization;
namespace SajamKnjiga.Core.Models;

/// <summary>
/// Model izdavaca sa poslovnom sifrom i opcionim sefom koji je autor.
/// </summary>
public class Izdavac : IEntitet
{
    public int Id { get; set; }
    public string Sifra { get; set; } = "";
    public string Naziv { get; set; } = "";
    // Sef je autor: u XML se upisuje samo njegov ID.
    public int? SefAutorId { get; set; }
    [XmlIgnore] public Autor? SefAutor { get; set; }
    [XmlIgnore] public List<Autor> Autori { get; set; } = new();
    [XmlIgnore] public List<Knjiga> Knjige { get; set; } = new();

    public Izdavac() { }

    public Izdavac(string sifra, string naziv) => (Sifra, Naziv) = (sifra, naziv);

    public override string ToString() => Naziv;
}
