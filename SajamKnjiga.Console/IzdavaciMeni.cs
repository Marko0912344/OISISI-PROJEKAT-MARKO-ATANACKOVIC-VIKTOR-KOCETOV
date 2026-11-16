
using System;
using System.IO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Services;

namespace SajamKnjiga.ConsoleApp;

public class IzdavaciMeni : IMeniModul
{
    private readonly IzdavacServis _servis;

    public string Naziv => "Izdavaci";

    public IzdavaciMeni()
    {
        string putanja = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "izdavaci.xml"
        );

        _servis = new IzdavacServis(putanja);
    }

    public void Pokreni()
    {
        while (true)
        {
            System.Console.WriteLine("\n--- IZDAVACI ---");
            System.Console.WriteLine("1. Prikazi sve izdavace");
            System.Console.WriteLine("2. Dodaj izdavaca");
            System.Console.WriteLine("3. Izmeni izdavaca");
            System.Console.WriteLine("4. Obrisi izdavaca");
            System.Console.WriteLine("5. Postavi sefa izdavaca");
            System.Console.WriteLine("6. Dodaj autora");
            System.Console.WriteLine("0. Povratak");

            System.Console.Write("Izbor: ");
            string? izbor = System.Console.ReadLine();

            switch (izbor)
            {
                case "1":
                    Prikazi();
                    break;
                case "2":
                    Dodaj();
                    break;
                case "3":
                    Izmeni();
                    break;
                case "4":
                    Obrisi();
                    break;
                case "6":
                    DodajAutora();
                    break;
                case "5":
                    PostaviSefa();
                    break;
                case "0":
                    return;
                default:
                    System.Console.WriteLine("Nepostojeca opcija.");
                    break;
            }
        }
    }

    private void Prikazi()
    {
        var izdavaci = _servis.PrikaziSve();

        if (izdavaci.Count == 0)
        {
            System.Console.WriteLine("Nema izdavaca.");
            return;
        }

        foreach (var i in izdavaci)
        {
            System.Console.WriteLine(
                $"ID: {i.Id} | Naziv: {i.Naziv} | Sef ID: {i.SefId}"
            );
        }
    }

    private void Dodaj()
    {
        System.Console.Write("ID izdavaca: ");

        if (!int.TryParse(System.Console.ReadLine(), out int id))
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        System.Console.Write("Naziv izdavaca: ");
        string naziv = System.Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(naziv))
        {
            System.Console.WriteLine("Naziv je obavezan.");
            return;
        }

        Izdavac novi = new Izdavac
        {
            Id = id,
            Naziv = naziv
        };

        try
        {
            _servis.Dodaj(novi);
            System.Console.WriteLine("Izdavac je dodat.");
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine(ex.Message);
        }
    }

    private void Izmeni()
    {
        System.Console.Write("ID izdavaca: ");

        if (!int.TryParse(System.Console.ReadLine(), out int id))
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        var postojeci = _servis.PrikaziSve()
            .Find(i => i.Id == id);

        if (postojeci == null)
        {
            System.Console.WriteLine("Izdavac nije pronadjen.");
            return;
        }

        System.Console.Write("Novi naziv: ");
        string naziv = System.Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(naziv))
        {
            System.Console.WriteLine("Naziv ne sme biti prazan.");
            return;
        }

        Izdavac izmenjeni = new Izdavac
        {
            Id = postojeci.Id,
            Naziv = naziv,
            SefId = postojeci.SefId
        };

        if (_servis.Izmeni(izmenjeni))
            System.Console.WriteLine("Izdavac je izmenjen.");
    }

    private void Obrisi()
    {
        System.Console.Write("ID izdavaca: ");

        if (!int.TryParse(System.Console.ReadLine(), out int id))
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        if (_servis.Obrisi(id))
            System.Console.WriteLine("Izdavac je obrisan.");
        else
            System.Console.WriteLine("Izdavac nije pronadjen.");
    }

    private void PostaviSefa()
    {
        System.Console.Write("ID izdavaca: ");

        if (!int.TryParse(System.Console.ReadLine(), out int izdavacId))
        {
            System.Console.WriteLine("Neispravan ID izdavaca.");
            return;
        }

        System.Console.Write("ID autora koji ce biti sef: ");

        if (!int.TryParse(System.Console.ReadLine(), out int autorId))
        {
            System.Console.WriteLine("Neispravan ID autora.");
            return;
        }

        string putanja = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "autori.xml"
        );

        AutorServis autorServis = new AutorServis(putanja);

        try
        {
            _servis.PostaviSefa(izdavacId, autorId, autorServis);

            System.Console.WriteLine(
                "Sef izdavaca je uspesno postavljen.");
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine(ex.Message);
        }
    }

    private void DodajAutora()
    {
        string putanja = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "autori.xml"
        );

        AutorServis autorServis = new AutorServis(putanja);

        System.Console.Write("ID autora: ");

        if (!int.TryParse(System.Console.ReadLine(), out int id))
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        System.Console.Write("Ime autora: ");
        string ime = System.Console.ReadLine() ?? "";

        System.Console.Write("Prezime autora: ");
        string prezime = System.Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(ime) ||
            string.IsNullOrWhiteSpace(prezime))
        {
            System.Console.WriteLine("Ime i prezime su obavezni.");
            return;
        }

        System.Console.Write("Godine iskustva: ");

        if (!int.TryParse(
            System.Console.ReadLine(),
            out int godineIskustva))
        {
            System.Console.WriteLine("Neispravan broj godina.");
            return;
        }

        Autor autor = new Autor
        {
            Id = id,
            Ime = ime,
            Prezime = prezime,
            GodineIskustva = godineIskustva
        };

        try
        {
            autorServis.Dodaj(autor);
            System.Console.WriteLine("Autor je uspesno dodat.");
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine(ex.Message);
        }
    }

}
