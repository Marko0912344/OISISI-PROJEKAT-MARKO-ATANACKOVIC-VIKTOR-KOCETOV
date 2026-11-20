using System.Xml.Serialization;
namespace SajamKnjiga.Core.Models;

public class Adresa : IEntitet
{
    public int Id { get; set; }
    public string Ulica { get; set; } = "";
    public string Broj { get; set; } = "";
    public string Grad { get; set; } = "";
    public string Drzava { get; set; } = "";

    public Adresa() { }
    public Adresa(string ulica, string broj, string grad, string drzava)
    {
        Ulica = ulica; Broj = broj; Grad = grad; Drzava = drzava;
    }
    public override string ToString() => $"{Ulica} {Broj}, {Grad}, {Drzava}";
}
