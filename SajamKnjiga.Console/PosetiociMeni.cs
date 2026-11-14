
using SajamKnjiga.ConsoleApp;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Services;
using System;
using System.IO;
using static SajamKnjiga.Core.Models.Posetilac;

namespace SajamKnjiga.ConsoleApp;

public class PosetiociMeni : IMeniModul
{
    private readonly PosetilacServis _servis;

    public string Naziv => "Posetioci";

    public PosetiociMeni()
    {
        string putanja = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "posetioci.xml"
        );

        _servis = new PosetilacServis(putanja);
    }

    public void Pokreni()
    {
        while (true)
        {
            System.Console.WriteLine("\n--- POSETIOCI ---");
            System.Console.WriteLine("1. Prikazi sve posetioce");
            System.Console.WriteLine("2. Dodaj posetioca");
            System.Console.WriteLine("3. Izmeni posetioca");
            System.Console.WriteLine("4. Obrisi posetioca");
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
        var posetioci = _servis.PrikaziSve();

        if (posetioci.Count == 0)
        {
            System.Console.WriteLine("Nema posetilaca.");
            return;
        }

        foreach (var p in posetioci)
        {
            System.Console.WriteLine(
                $"{p.Id} - {p.Ime} {p.Prezime} | {p.Email} | {p.Status}"
            );
        }
    }



    private void Dodaj()
    {
        System.Console.Write("ID: ");
        if (!int.TryParse(System.Console.ReadLine(), out int id))
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        System.Console.Write("Ime: ");
        string ime = System.Console.ReadLine() ?? "";

        System.Console.Write("Prezime: ");
        string prezime = System.Console.ReadLine() ?? "";

        System.Console.Write("Email: ");
        string email = System.Console.ReadLine() ?? "";

        System.Console.Write("Telefon: ");
        string telefon = System.Console.ReadLine() ?? "";

        System.Console.Write("Datum rodjenja (dd.MM.yyyy): ");
        if (!DateTime.TryParseExact(
            System.Console.ReadLine(),
            "dd.MM.yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out DateTime datumRodjenja))
        {
            System.Console.WriteLine("Neispravan datum rodjenja.");
            return;
        }

        System.Console.Write("Broj clanske karte: ");
        string brojKarte = System.Console.ReadLine() ?? "";

        System.Console.Write("Godina clanstva: ");
        if (!int.TryParse(System.Console.ReadLine(), out int godina))
        {
            System.Console.WriteLine("Neispravna godina.");
            return;
        }

        System.Console.Write("Status (R - Redovan, V - VIP): ");
        string statusUnos = (System.Console.ReadLine() ?? "")
            .Trim().ToUpperInvariant();

        if (statusUnos != "R" && statusUnos != "V")
        {
            System.Console.WriteLine("Neispravan status.");
            return;
        }

        Posetilac novi = new Posetilac
        {
            Id = id,
            Ime = ime,
            Prezime = prezime,
            Email = email,
            Telefon = telefon,
            DatumRodjenja = datumRodjenja,
            BrojClanskeKarte = brojKarte,
            GodinaClanstva = godina,
            Status = statusUnos == "V"
                ? StatusPosetioca.VIP
                : StatusPosetioca.Redovan
        };

        try
        {
            _servis.Dodaj(novi);
            System.Console.WriteLine("Posetilac je uspesno dodat.");
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine(ex.Message);
        }
    }

    private void Izmeni()
    {
        System.Console.Write("ID posetioca: ");
        if (!int.TryParse(System.Console.ReadLine(), out int id))
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        var postojeci = _servis.PrikaziSve()
            .Find(p => p.Id == id);

        if (postojeci == null)
        {
            System.Console.WriteLine("Posetilac nije pronadjen.");
            return;
        }

        System.Console.Write("Novo ime: ");
        string ime = System.Console.ReadLine() ?? "";

        System.Console.Write("Novo prezime: ");
        string prezime = System.Console.ReadLine() ?? "";

        Posetilac izmenjeni = new Posetilac
        {
            Id = id,
            Ime = ime,
            Prezime = prezime,
            Email = postojeci.Email,
            DatumRodjenja = postojeci.DatumRodjenja,
            Telefon = postojeci.Telefon,
            BrojClanskeKarte = postojeci.BrojClanskeKarte,
            GodinaClanstva = postojeci.GodinaClanstva,
            Status = postojeci.Status,
            ProsecnaOcena = postojeci.ProsecnaOcena,
            AdresaId = postojeci.AdresaId
        };

        if (_servis.Izmeni(izmenjeni))
            System.Console.WriteLine("Podaci su izmenjeni.");
    }

    private void Obrisi()
    {
        System.Console.Write("ID posetioca: ");
        if (!int.TryParse(System.Console.ReadLine(), out int id))
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        if (_servis.Obrisi(id))
            System.Console.WriteLine("Posetilac je obrisan.");
        else
            System.Console.WriteLine("Posetilac nije pronadjen.");
    }
}
