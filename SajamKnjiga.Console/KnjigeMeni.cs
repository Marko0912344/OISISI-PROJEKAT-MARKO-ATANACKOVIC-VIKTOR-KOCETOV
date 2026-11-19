using SajamKnjiga.Core.Services;
using System.IO;
namespace SajamKnjiga.ConsoleApp;

public class KnjigeMeni : IMeniModul
{
    public string Naziv => "Knjige";


    private readonly KnjigaServis _servis =
        new KnjigaServis(
            Path.Combine(
                AppContext.BaseDirectory,
                "Podaci",
                "knjige.xml"
            )
        );


    public void Pokreni()
    {
        while (true)
        {
            Console.WriteLine("\n=== KNJIGE ===");
            Console.WriteLine("4. Obrisi knjigu");
            Console.WriteLine("3. Izmeni knjigu");
            Console.WriteLine("2. Dodaj knjigu");
            Console.WriteLine("1. Prikazi knjige");
            Console.WriteLine("0. Nazad");
            Console.Write("Izbor: ");

            string? izbor = Console.ReadLine();

            if (izbor == "0")
                return;

            if (izbor == "1")
            {
                foreach (var knjiga in _servis.PrikaziSve())
                {
                    Console.WriteLine($"\nID: {knjiga.Id}");
                    Console.WriteLine($"ISBN: {knjiga.ISBN}");
                    Console.WriteLine($"Naziv: {knjiga.Naziv}");
                    Console.WriteLine($"Zanr: {knjiga.Zanr}");
                    Console.WriteLine($"Godina izdanja: {knjiga.GodinaIzdanja}");
                    Console.WriteLine($"Cena: {knjiga.Cena} RSD");
                    Console.WriteLine($"Broj strana: {knjiga.BrojStrana}");
                    Console.WriteLine($"Izdavac ID: {knjiga.IzdavacId}");
                }
            }

            else if (izbor == "2")
            {
                try
                {
                    Dodaj();
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (izbor == "3")
            {
                try
                {
                    Izmeni();
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else if (izbor == "4")
            {
                try
                {
                    Obrisi();
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Nepostojeca opcija.");
            }
        }
    }




    private void Dodaj()
    {
        int id = _servis.GenerisiSledeciId();

        Console.WriteLine($"Automatski dodeljen ID knjige: {id}");

        Console.Write("ISBN: ");
        string isbn = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(isbn))
        {
            Console.WriteLine("ISBN je obavezan.");
            return;
        }

        Console.Write("Naziv: ");
        string naziv = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(naziv))
        {
            Console.WriteLine("Naziv knjige je obavezan.");
            return;
        }

        Console.Write("Zanr: ");
        string zanr = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrWhiteSpace(zanr))
        {
            Console.WriteLine("Zanr je obavezan.");
            return;
        }

        Console.Write("Godina izdanja: ");
        if (!int.TryParse(Console.ReadLine(), out int godina) ||
            godina < 1 || godina > DateTime.Now.Year)
        {
            Console.WriteLine("Neispravna godina izdanja.");
            return;
        }

        Console.Write("Cena: ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal cena) ||
            cena < 0)
        {
            Console.WriteLine("Neispravna cena.");
            return;
        }

        Console.Write("Broj strana: ");
        if (!int.TryParse(Console.ReadLine(), out int brojStrana) ||
            brojStrana <= 0)
        {
            Console.WriteLine("Neispravan broj strana.");
            return;
        }

        Console.Write("ID izdavaca (Enter ako nema): ");
        string? unosIzdavaca = Console.ReadLine();

        int? izdavacId = null;

        if (!string.IsNullOrWhiteSpace(unosIzdavaca))
        {
            if (!int.TryParse(unosIzdavaca, out int idIzdavaca) ||
                idIzdavaca <= 0)
            {
                Console.WriteLine("Neispravan ID izdavaca.");
                return;
            }

            string putanjaIzdavaca = Path.Combine(
                AppContext.BaseDirectory,
                "Podaci",
                "izdavaci.xml"
            );

            IzdavacServis izdavacServis =
                new IzdavacServis(putanjaIzdavaca);

            bool postojiIzdavac = izdavacServis.PrikaziSve()
                .Any(i => i.Id == idIzdavaca);

            if (!postojiIzdavac)
            {
                Console.WriteLine("Izdavac sa tim ID-jem ne postoji.");
                return;
            }

            izdavacId = idIzdavaca;
        }

        var knjiga = new SajamKnjiga.Core.Models.Knjiga
        {
            Id = id,
            ISBN = isbn,
            Naziv = naziv,
            Zanr = zanr,
            GodinaIzdanja = godina,
            Cena = cena,
            BrojStrana = brojStrana,
            IzdavacId = izdavacId
        };

        try
        {
            if (_servis.Dodaj(knjiga))
            {
                Console.WriteLine("Knjiga je uspesno dodata.");
            }
            else
            {
                Console.WriteLine("Dodavanje knjige nije uspelo.");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void Izmeni()
    {
        Console.Write("Unesi ID knjige za izmenu: ");

        if (!int.TryParse(Console.ReadLine(), out int id) || id <= 0)
        {
            Console.WriteLine("Neispravan ID.");
            return;
        }

        var knjiga = _servis.PrikaziSve()
            .FirstOrDefault(k => k.Id == id);

        if (knjiga == null)
        {
            Console.WriteLine("Knjiga nije pronadjena.");
            return;
        }

        Console.WriteLine(
            "\nUnesi nove podatke. Pritisni Enter da zadrzis staru vrednost.");

        Console.Write($"Novi ISBN ({knjiga.ISBN}): ");
        string? isbn = Console.ReadLine();

        Console.Write($"Novi naziv ({knjiga.Naziv}): ");
        string? naziv = Console.ReadLine();

        Console.Write($"Novi zanr ({knjiga.Zanr}): ");
        string? zanr = Console.ReadLine();

        Console.Write($"Nova godina izdanja ({knjiga.GodinaIzdanja}): ");
        string? unosGodine = Console.ReadLine();

        int godina = knjiga.GodinaIzdanja;

        if (!string.IsNullOrWhiteSpace(unosGodine))
        {
            if (!int.TryParse(unosGodine, out godina) ||
                godina <= 0 || godina > DateTime.Now.Year)
            {
                Console.WriteLine("Neispravna godina izdanja.");
                return;
            }
        }

        Console.Write($"Nova cena ({knjiga.Cena}): ");
        string? unosCene = Console.ReadLine();

        decimal cena = knjiga.Cena;

        if (!string.IsNullOrWhiteSpace(unosCene))
        {
            if (!decimal.TryParse(unosCene, out cena) || cena < 0)
            {
                Console.WriteLine("Neispravna cena.");
                return;
            }
        }

        Console.Write($"Novi broj strana ({knjiga.BrojStrana}): ");
        string? unosStrana = Console.ReadLine();

        int brojStrana = knjiga.BrojStrana;

        if (!string.IsNullOrWhiteSpace(unosStrana))
        {
            if (!int.TryParse(unosStrana, out brojStrana) || brojStrana <= 0)
            {
                Console.WriteLine("Neispravan broj strana.");
                return;
            }
        }

        Console.Write(
            $"Novi ID izdavaca ({knjiga.IzdavacId?.ToString() ?? "nema"}): ");

        string? unosIzdavaca = Console.ReadLine();

        int? izdavacId = knjiga.IzdavacId;

        if (!string.IsNullOrWhiteSpace(unosIzdavaca))
        {
            if (!int.TryParse(unosIzdavaca, out int noviIzdavacId) ||
                noviIzdavacId <= 0)
            {
                Console.WriteLine("Neispravan ID izdavaca.");
                return;
            }

            string putanjaIzdavaci = System.IO.Path.Combine(
                AppContext.BaseDirectory, "Podaci", "izdavaci.xml");

            var izdavacServis =
                new SajamKnjiga.Core.Services.IzdavacServis(putanjaIzdavaci);

            if (!izdavacServis.PrikaziSve()
                .Any(i => i.Id == noviIzdavacId))
            {
                Console.WriteLine("Izdavac sa tim ID-jem ne postoji.");
                return;
            }

            izdavacId = noviIzdavacId;
        }

        var izmenjena = new SajamKnjiga.Core.Models.Knjiga
        {
            Id = knjiga.Id,
            ISBN = string.IsNullOrWhiteSpace(isbn) ? knjiga.ISBN : isbn.Trim(),
            Naziv = string.IsNullOrWhiteSpace(naziv) ? knjiga.Naziv : naziv.Trim(),
            Zanr = string.IsNullOrWhiteSpace(zanr) ? knjiga.Zanr : zanr.Trim(),
            GodinaIzdanja = godina,
            Cena = cena,
            BrojStrana = brojStrana,
            IzdavacId = izdavacId
        };

        if (_servis.Izmeni(izmenjena))
            Console.WriteLine("Knjiga je uspesno izmenjena.");
        else
            Console.WriteLine("Izmena nije uspela.");
    }
    private void Obrisi()
    {
        Console.Write("Unesi ID knjige za brisanje: ");

        if (!int.TryParse(Console.ReadLine(), out int id) || id <= 0)
        {
            Console.WriteLine("Neispravan ID knjige.");
            return;
        }

        var knjiga = _servis.PrikaziSve()
            .FirstOrDefault(k => k.Id == id);

        if (knjiga == null)
        {
            Console.WriteLine("Knjiga nije pronadjena.");
            return;
        }

        Console.WriteLine($"Knjiga: {knjiga.Naziv}");
        Console.Write("Da li sigurno zelis da obrises knjigu? (D/N): ");

        string? potvrda = Console.ReadLine();

        if (!string.Equals(potvrda, "D",
            StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Brisanje je otkazano.");
            return;
        }

        string putanjaKupovine = Path.Combine(
            AppContext.BaseDirectory, "Podaci", "kupovine.xml");

        string putanjaListaZelja = Path.Combine(
            AppContext.BaseDirectory, "Podaci", "listaZelja.xml");

        var kupovinaServis = new KupovinaServis(putanjaKupovine);
        var listaZeljaServis = new ListaZeljaServis(putanjaListaZelja);

        bool obrisana = _servis.Obrisi(
            id, kupovinaServis, listaZeljaServis);

        if (obrisana)
            Console.WriteLine("Knjiga je uspesno obrisana.");
        else
            Console.WriteLine("Brisanje nije uspelo.");
    }
}