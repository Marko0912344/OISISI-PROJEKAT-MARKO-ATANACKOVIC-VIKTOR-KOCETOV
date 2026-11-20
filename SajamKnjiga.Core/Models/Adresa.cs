using System;
namespace SajamKnjiga.Core.Models;

/// <summary>
/// Model adrese koji se koristi i za autore i za posetioce.
/// </summary>
public class Adresa : IEntitet
{
    public int Id { get; set; }
    public string Ulica { get; set; } = "";
    public string Broj { get; set; } = "";
    public string Grad { get; set; } = "";
    public string Drzava { get; set; } = "";

    public Adresa() { }

    public Adresa(string ulica, string broj, string grad, string drzava)
        => (Ulica, Broj, Grad, Drzava) = (ulica, broj, grad, drzava);

    public override string ToString() => $"{Ulica} {Broj}, {Grad}, {Drzava}";
}
