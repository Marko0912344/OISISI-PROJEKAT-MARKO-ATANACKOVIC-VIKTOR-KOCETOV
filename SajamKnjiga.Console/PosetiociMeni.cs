
using SajamKnjiga.ConsoleApp;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Services;
using System;
using System.IO;
using System.Linq; 
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

        string putanjaAdrese = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "adrese.xml"
        );

        AdresaServis adresaServis = new AdresaServis(putanjaAdrese);
        var adrese = adresaServis.PrikaziSve();

        foreach (var p in posetioci)
        {
            Adresa? adresa = adrese
                .FirstOrDefault(a => a.Id == p.AdresaId);

            System.Console.WriteLine("------------------------------");
            System.Console.WriteLine($"ID: {p.Id}");
            System.Console.WriteLine($"Ime: {p.Ime}");
            System.Console.WriteLine($"Prezime: {p.Prezime}");
            System.Console.WriteLine($"Datum rodjenja: {p.DatumRodjenja:dd.MM.yyyy}");
            System.Console.WriteLine($"Telefon: {p.Telefon}");
            System.Console.WriteLine($"Email: {p.Email}");
            System.Console.WriteLine($"Broj clanske karte: {p.BrojClanskeKarte}");
            System.Console.WriteLine($"Godina clanstva: {p.GodinaClanstva}");
            System.Console.WriteLine($"Status: {p.Status}");

            string putanjaKupovine = Path.Combine(
                AppContext.BaseDirectory,
                "Podaci",
                "kupovine.xml"
            );

            KupovinaServis kupovinaServis =
                new KupovinaServis(putanjaKupovine);

            var kupovine = kupovinaServis.KupovinePosetioca(p.Id);

            if (kupovine.Count > 0)
            {
                double prosecnaOcena = kupovine.Average(k => k.Ocena);
                System.Console.WriteLine($"Prosecna ocena: {prosecnaOcena:F2}");
            }
            else
            {
                System.Console.WriteLine("Prosecna ocena: Nema ocena");
            }


            if (adresa != null)
            {
                System.Console.WriteLine($"Adresa: {adresa}");
            }
            else
            {
                System.Console.WriteLine("Adresa: Nije uneta");
            }
        }

        System.Console.WriteLine("------------------------------");
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

        System.Console.Write("Ulica: ");
        string ulica = System.Console.ReadLine() ?? "";

        System.Console.Write("Broj: ");
        string broj = System.Console.ReadLine() ?? "";

        System.Console.Write("Grad: ");
        string grad = System.Console.ReadLine() ?? "";

        System.Console.Write("Drzava: ");
        string drzava = System.Console.ReadLine() ?? "";

        if (string.IsNullOrWhiteSpace(ulica) ||
            string.IsNullOrWhiteSpace(broj) ||
            string.IsNullOrWhiteSpace(grad) ||
            string.IsNullOrWhiteSpace(drzava))
        {
            System.Console.WriteLine("Svi podaci adrese su obavezni.");
            return;
        }

        string putanjaAdrese = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "adrese.xml"
        );

        AdresaServis adresaServis = new AdresaServis(putanjaAdrese);

        int noviAdresaId = adresaServis.PrikaziSve()
            .Select(a => a.Id)
            .DefaultIfEmpty(0)
            .Max() + 1;

        Adresa novaAdresa = new Adresa
        {
            Id = noviAdresaId,
            Ulica = ulica,
            Broj = broj,
            Grad = grad,
            Drzava = drzava
        };

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
            : StatusPosetioca.Redovan,

            AdresaId = novaAdresa.Id,
            Adresa = novaAdresa
        };


        try
        {
            if (_servis.PrikaziSve().Any(p => p.Id == novi.Id))
            {
                System.Console.WriteLine(
                    "Posetilac sa ovim ID-jem vec postoji.");
                return;
            }

            adresaServis.Dodaj(novaAdresa);
            _servis.Dodaj(novi);

            System.Console.WriteLine(
                "Posetilac i njegova adresa su uspesno dodati.");
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

            if (datum.Date > DateTime.Today)
            {
                System.Console.WriteLine(
                    "Datum rodjenja ne moze biti u buducnosti.");
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

            if (godina < 1900 || godina > DateTime.Now.Year)
            {
                System.Console.WriteLine(
                    "Godina clanstva nije ispravna.");
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


        string putanjaAdrese = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "adrese.xml"
        );

        AdresaServis adresaServis = new AdresaServis(putanjaAdrese);

        Adresa? postojecaAdresa = adresaServis.PrikaziSve()
            .FirstOrDefault(a => a.Id == postojeci.AdresaId);

        if (postojecaAdresa != null)
        {
            System.Console.WriteLine("Izmena adrese (Enter za zadrzavanje):");

            System.Console.Write($"Ulica ({postojecaAdresa.Ulica}): ");
            string ulica = System.Console.ReadLine() ?? "";

            System.Console.Write($"Broj ({postojecaAdresa.Broj}): ");
            string broj = System.Console.ReadLine() ?? "";

            System.Console.Write($"Grad ({postojecaAdresa.Grad}): ");
            string grad = System.Console.ReadLine() ?? "";

            System.Console.Write($"Drzava ({postojecaAdresa.Drzava}): ");
            string drzava = System.Console.ReadLine() ?? "";

            Adresa izmenjenaAdresa = new Adresa
            {
                Id = postojecaAdresa.Id,
                Ulica = string.IsNullOrWhiteSpace(ulica)
                    ? postojecaAdresa.Ulica : ulica,
                Broj = string.IsNullOrWhiteSpace(broj)
                    ? postojecaAdresa.Broj : broj,
                Grad = string.IsNullOrWhiteSpace(grad)
                    ? postojecaAdresa.Grad : grad,
                Drzava = string.IsNullOrWhiteSpace(drzava)
                    ? postojecaAdresa.Drzava : drzava
            };

            adresaServis.Izmeni(izmenjenaAdresa);
        }

        else
        {
            System.Console.WriteLine(
                "Posetilac nema adresu. Da li zelis da je dodas? (D/N)");

            string odgovor = (System.Console.ReadLine() ?? "")
                .Trim().ToUpperInvariant();

            if (odgovor == "D")
            {
                System.Console.Write("Ulica: ");
                string ulica = System.Console.ReadLine() ?? "";

                System.Console.Write("Broj: ");
                string broj = System.Console.ReadLine() ?? "";

                System.Console.Write("Grad: ");
                string grad = System.Console.ReadLine() ?? "";

                System.Console.Write("Drzava: ");
                string drzava = System.Console.ReadLine() ?? "";

                if (string.IsNullOrWhiteSpace(ulica) ||
                    string.IsNullOrWhiteSpace(broj) ||
                    string.IsNullOrWhiteSpace(grad) ||
                    string.IsNullOrWhiteSpace(drzava))
                {
                    System.Console.WriteLine(
                        "Svi podaci adrese su obavezni.");
                    return;
                }

                int noviId = adresaServis.PrikaziSve()
                    .Select(a => a.Id)
                    .DefaultIfEmpty(0)
                    .Max() + 1;

                Adresa novaAdresa = new Adresa
                {
                    Id = noviId,
                    Ulica = ulica,
                    Broj = broj,
                    Grad = grad,
                    Drzava = drzava
                };

                adresaServis.Dodaj(novaAdresa);

                postojeci.AdresaId = novaAdresa.Id;
                postojeci.Adresa = novaAdresa;

                _servis.Izmeni(postojeci);
            }
        }


    }

    private void Obrisi()
    {
        System.Console.Write("ID posetioca: ");

        if (!int.TryParse(System.Console.ReadLine(), out int id) || id <= 0)
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        Posetilac? posetilac = _servis.PrikaziSve()
            .FirstOrDefault(p => p.Id == id);

        if (posetilac == null)
        {
            System.Console.WriteLine("Posetilac nije pronadjen.");
            return;
        }

        System.Console.Write("Da li sigurno zelis da obrises posetioca? (D/N): ");
        string odgovor = (System.Console.ReadLine() ?? "")
            .Trim().ToUpperInvariant();

        if (odgovor != "D")
        {
            System.Console.WriteLine("Brisanje je otkazano.");
            return;
        }

        string putanjaKupovine = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "kupovine.xml"
        );

        KupovinaServis kupovinaServis =
            new KupovinaServis(putanjaKupovine);

        if (kupovinaServis.KupovinePosetioca(id).Any())
        {
            System.Console.WriteLine(
                "Posetilac ima evidentirane kupovine i ne moze biti obrisan.");
            return;
        }

        int adresaId = posetilac.AdresaId;

        bool obrisan;

        try
        {
            obrisan = _servis.Obrisi(id, kupovinaServis);
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine(ex.Message);
            return;
        }


        if (!obrisan)
        {
            System.Console.WriteLine("Brisanje nije uspelo.");
            return;
        }

        if (adresaId > 0)
        {
            bool koristiDrugi = _servis.PrikaziSve()
                .Any(p => p.AdresaId == adresaId);

            if (!koristiDrugi)
            {
                string putanjaAdrese = Path.Combine(
                    AppContext.BaseDirectory,
                    "Podaci",
                    "adrese.xml"
                );

                AdresaServis adresaServis =
                    new AdresaServis(putanjaAdrese);

                adresaServis.Obrisi(adresaId);
            }
        }

        System.Console.WriteLine("Posetilac je obrisan.");
    }


}
