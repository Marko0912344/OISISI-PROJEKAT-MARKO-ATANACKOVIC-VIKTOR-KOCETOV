
using System;
using System.IO;
using System.Runtime.Intrinsics.Arm;
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
            System.Console.WriteLine("7. Povezi autora sa izdavacem");
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
                case "7":
                    PoveziAutora();
                    break;
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

        string putanjaKnjige = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "knjige.xml"
        );

        KnjigaServis knjigaServis = new KnjigaServis(putanjaKnjige);
        var sveKnjige = knjigaServis.PrikaziSve();

        foreach (var i in izdavaci)
        {
            System.Console.WriteLine("\n------------------------------");
            System.Console.WriteLine($"ID: {i.Id}");
            System.Console.WriteLine($"Sifra: {i.Sifra}");
            System.Console.WriteLine($"Naziv: {i.Naziv}");

            System.Console.WriteLine(
                $"Sef ID: {(i.SefId.HasValue ? i.SefId.Value.ToString() : "Nije postavljen")}"
            );

            string autori = i.AutorIds != null && i.AutorIds.Count > 0
                ? string.Join(", ", i.AutorIds)
                : "Nema povezanih autora";

            System.Console.WriteLine($"Autori (ID): {autori}");

            var knjigeIzdavaca = sveKnjige
                .Where(k => k.IzdavacId == i.Id)
                .ToList();

            System.Console.WriteLine("Knjige izdavaca:");

            if (knjigeIzdavaca.Count == 0)
            {
                System.Console.WriteLine("  Nema knjiga.");
            }
            else
            {
                foreach (var knjiga in knjigeIzdavaca)
                {
                    System.Console.WriteLine(
                        $"  - ID: {knjiga.Id}, " +
                        $"Naziv: {knjiga.Naziv}, " +
                        $"ISBN: {knjiga.ISBN}"
                    );
                }
            }
        }

        System.Console.WriteLine("------------------------------");
    }





    private void Dodaj()
    {
        int id = _servis.GenerisiSledeciId();

        System.Console.WriteLine($"Automatski dodeljen ID izdavaca: {id}");

        System.Console.Write("Sifra izdavaca: ");
        string sifra = (System.Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(sifra))
        {
            System.Console.WriteLine("Sifra izdavaca je obavezna.");
            return;
        }

        System.Console.Write("Naziv izdavaca: ");
        string naziv = (System.Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(naziv))
        {
            System.Console.WriteLine("Naziv je obavezan.");
            return;
        }

        System.Console.Write("ID sefa izdavaca (Enter ako nema): ");
        string unosSefa = System.Console.ReadLine() ?? "";

        int? sefId = null;

        if (!string.IsNullOrWhiteSpace(unosSefa))
        {
            if (!int.TryParse(unosSefa, out int idSefa) || idSefa <= 0)
            {
                System.Console.WriteLine("Neispravan ID sefa.");
                return;
            }

            sefId = idSefa;
        }

        List<int> autorIds = new List<int>();

        System.Console.Write("ID-jevi autora (odvojeni zarezom, Enter ako nema): ");
        string unosAutora = System.Console.ReadLine() ?? "";

        if (!string.IsNullOrWhiteSpace(unosAutora))
        {
            foreach (string deo in unosAutora.Split(','))
            {
                if (!int.TryParse(deo.Trim(), out int autorId) || autorId <= 0)
                {
                    System.Console.WriteLine("Neispravan ID autora.");
                    return;
                }

                if (!autorIds.Contains(autorId))
                    autorIds.Add(autorId);
            }
        }

        string putanjaAutori = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "autori.xml"
        );

        AutorServis autorServis = new AutorServis(putanjaAutori);

        if (sefId.HasValue && !autorServis.MozeBitiSef(sefId.Value))
        {
            System.Console.WriteLine(
                "Sef mora biti postojeci autor sa najmanje 5 godina iskustva.");
            return;
        }

        foreach (int autorId in autorIds)
        {
            if (autorServis.PronadjiPoId(autorId) == null)
            {
                System.Console.WriteLine($"Autor sa ID-jem {autorId} ne postoji.");
                return;
            }
        }

        Izdavac novi = new Izdavac
        {
            Id = id,
            Sifra = sifra,
            Naziv = naziv,
            SefId = sefId,
            AutorIds = autorIds
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

        if (!int.TryParse(System.Console.ReadLine(), out int id) || id <= 0)
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        var postojeci = _servis.PrikaziSve()
            .FirstOrDefault(i => i.Id == id);

        if (postojeci == null)
        {
            System.Console.WriteLine("Izdavac nije pronadjen.");
            return;
        }

        System.Console.Write($"Nova sifra ({postojeci.Sifra}): ");
        string sifra = (System.Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(sifra))
            sifra = postojeci.Sifra;

        if (string.IsNullOrWhiteSpace(sifra))
        {
            System.Console.WriteLine("Sifra izdavaca je obavezna.");
            return;
        }

        System.Console.Write($"Novi naziv ({postojeci.Naziv}): ");
        string naziv = (System.Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(naziv))
            naziv = postojeci.Naziv;

        System.Console.Write(
            $"Novi ID sefa ({postojeci.SefId?.ToString() ?? "nema"}) " +
            "[Enter = bez promene, 0 = ukloni]: ");

        string unosSefa = (System.Console.ReadLine() ?? "").Trim();

        int? sefId = postojeci.SefId;

        if (unosSefa == "0")
        {
            sefId = null;
        }
        else if (!string.IsNullOrWhiteSpace(unosSefa))
        {
            if (!int.TryParse(unosSefa, out int noviSefId) || noviSefId <= 0)
            {
                System.Console.WriteLine("Neispravan ID sefa.");
                return;
            }

            sefId = noviSefId;
        }

        var autorIds = new List<int>(
            postojeci.AutorIds ?? new List<int>());

        System.Console.Write(
            "Novi ID-jevi autora (odvojeni zarezom) " +
            "[Enter = bez promene, 0 = ukloni sve]: ");

        string unosAutora = (System.Console.ReadLine() ?? "").Trim();

        if (unosAutora == "0")
        {
            autorIds.Clear();
        }
        else if (!string.IsNullOrWhiteSpace(unosAutora))
        {
            var noviAutori = new List<int>();

            foreach (string deo in unosAutora.Split(','))
            {
                if (!int.TryParse(deo.Trim(), out int autorId) || autorId <= 0)
                {
                    System.Console.WriteLine("Neispravan ID autora.");
                    return;
                }

                if (!noviAutori.Contains(autorId))
                    noviAutori.Add(autorId);
            }

            autorIds = noviAutori;
        }

        string putanjaAutori = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "autori.xml"
        );

        AutorServis autorServis = new AutorServis(putanjaAutori);

        if (sefId.HasValue && !autorServis.MozeBitiSef(sefId.Value))
        {
            System.Console.WriteLine(
                "Sef mora biti postojeci autor sa najmanje 5 godina iskustva.");
            return;
        }

        foreach (int autorId in autorIds)
        {
            if (autorServis.PronadjiPoId(autorId) == null)
            {
                System.Console.WriteLine($"Autor sa ID-jem {autorId} ne postoji.");
                return;
            }
        }

        var izmenjeni = new Izdavac
        {
            Id = postojeci.Id,
            Sifra = sifra,
            Naziv = naziv,
            SefId = sefId,
            AutorIds = autorIds
        };

        try
        {
            if (_servis.Izmeni(izmenjeni))
                System.Console.WriteLine("Izdavac je uspesno izmenjen.");
            else
                System.Console.WriteLine("Izmena nije uspela.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine(
                "Greska prilikom izmene izdavaca: " + ex.Message);
        }
    }


    private void Obrisi()
    {
        System.Console.Write("ID izdavaca: ");

        if (!int.TryParse(System.Console.ReadLine(), out int id) || id <= 0)
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        var izdavac = _servis.PrikaziSve()
            .FirstOrDefault(i => i.Id == id);

        if (izdavac == null)
        {
            System.Console.WriteLine("Izdavac nije pronadjen.");
            return;
        }

        string putanjaKnjige = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "knjige.xml"
        );

        KnjigaServis knjigaServis = new KnjigaServis(putanjaKnjige);

        if (knjigaServis.ImaKnjigeIzdavaca(id))
        {
            System.Console.WriteLine(
                "Izdavac ima povezane knjige i ne moze biti obrisan.");
            return;
        }

        System.Console.Write("Da li sigurno zelis da obrises izdavaca? (D/N): ");

        string odgovor = (System.Console.ReadLine() ?? "")
            .Trim()
            .ToUpperInvariant();

        if (odgovor != "D")
        {
            System.Console.WriteLine("Brisanje je otkazano.");
            return;
        }

        if (_servis.Obrisi(id,knjigaServis))
            System.Console.WriteLine("Izdavac je uspesno obrisan.");
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

    private void PoveziAutora()
    {
        System.Console.Write("ID izdavaca: ");

        if (!int.TryParse(System.Console.ReadLine(), out int izdavacId)
            || izdavacId <= 0)
        {
            System.Console.WriteLine("Neispravan ID izdavaca.");
            return;
        }

        System.Console.Write("ID autora: ");

        if (!int.TryParse(System.Console.ReadLine(), out int autorId)
            || autorId <= 0)
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
            _servis.PoveziAutora(izdavacId, autorId, autorServis);

            System.Console.WriteLine(
                "Autor je uspesno povezan sa izdavacem.");
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine(ex.Message);
        }
    }

}
