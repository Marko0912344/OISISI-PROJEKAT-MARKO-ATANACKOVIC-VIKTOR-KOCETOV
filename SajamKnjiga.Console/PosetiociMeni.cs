
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


        if (string.IsNullOrWhiteSpace(ime) ||
            string.IsNullOrWhiteSpace(prezime) ||
            string.IsNullOrWhiteSpace(email))
        {
            System.Console.WriteLine(
                "Ime, prezime i email su obavezni podaci.");

            return;
        }

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

        if (datumRodjenja.Date > DateTime.Today)
        {
            System.Console.WriteLine(
                "Datum rodjenja ne moze biti u buducnosti.");
            return;
        }

        System.Console.Write("Broj clanske karte: ");
        string brojKarte = System.Console.ReadLine() ?? "";


        System.Console.Write("Godina clanstva: ");

        if (!int.TryParse(System.Console.ReadLine(), out int godina))
        {
            System.Console.WriteLine("Godina mora biti broj.");
            return;
        }

        if (godina < 1900 || godina > DateTime.Now.Year)
        {
            System.Console.WriteLine(
                "Godina clanstva nije ispravna.");
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

        System.Console.WriteLine(
            "Pritisni Enter da zadrzis postojecu vrednost.");

        System.Console.Write($"Ime ({postojeci.Ime}): ");
        string ime = System.Console.ReadLine() ?? "";
        if (!string.IsNullOrWhiteSpace(ime))
            postojeci.Ime = ime;

        System.Console.Write($"Prezime ({postojeci.Prezime}): ");
        string prezime = System.Console.ReadLine() ?? "";
        if (!string.IsNullOrWhiteSpace(prezime))
            postojeci.Prezime = prezime;

        System.Console.Write($"Email ({postojeci.Email}): ");
        string email = System.Console.ReadLine() ?? "";
        if (!string.IsNullOrWhiteSpace(email))
            postojeci.Email = email;

        System.Console.Write($"Telefon ({postojeci.Telefon}): ");
        string telefon = System.Console.ReadLine() ?? "";
        if (!string.IsNullOrWhiteSpace(telefon))
            postojeci.Telefon = telefon;

        System.Console.Write(
            $"Datum rodjenja ({postojeci.DatumRodjenja:dd.MM.yyyy}): ");

        string datumUnos = System.Console.ReadLine() ?? "";

        if (!string.IsNullOrWhiteSpace(datumUnos))
        {
            if (!DateTime.TryParseExact(
                datumUnos,
                "dd.MM.yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime datum))
            {
                System.Console.WriteLine("Neispravan datum.");
                return;
            }

            postojeci.DatumRodjenja = datum;
        }

        System.Console.Write(
            $"Broj clanske karte ({postojeci.BrojClanskeKarte}): ");

        string karta = System.Console.ReadLine() ?? "";
        if (!string.IsNullOrWhiteSpace(karta))
            postojeci.BrojClanskeKarte = karta;

        System.Console.Write(
            $"Godina clanstva ({postojeci.GodinaClanstva}): ");

        string godinaUnos = System.Console.ReadLine() ?? "";

        if (!string.IsNullOrWhiteSpace(godinaUnos))
        {
            if (!int.TryParse(godinaUnos, out int godina))
            {
                System.Console.WriteLine("Neispravna godina.");
                return;
            }

            postojeci.GodinaClanstva = godina;
        }

        System.Console.Write(
            $"Status ({postojeci.Status}) [R/V]: ");

        string status = (System.Console.ReadLine() ?? "")
            .Trim().ToUpperInvariant();

        if (status == "R")
            postojeci.Status = StatusPosetioca.Redovan;
        else if (status == "V")
            postojeci.Status = StatusPosetioca.VIP;
        else if (status != "")
        {
            System.Console.WriteLine("Neispravan status.");
            return;
        }

        if (_servis.Izmeni(postojeci))
            System.Console.WriteLine("Podaci su uspesno izmenjeni.");
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
