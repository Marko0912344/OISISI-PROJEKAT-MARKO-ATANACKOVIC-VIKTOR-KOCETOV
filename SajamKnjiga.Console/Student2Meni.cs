using System.Globalization;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Services;

namespace SajamKnjiga.ConsoleApp;

public sealed class Student2Meni : IMeniModul
{
    private readonly Student2AutoriService _autori = new();
    private readonly Student2KnjigeService _knjige = new();
    private readonly Student2KupovineService _kupovine = new();
    public string Naziv => "Student 2 - autori, knjige, veze i pretraga";

    public void Pokreni()
    {
        while (true)
        {
            Console.WriteLine("\n=== STUDENT 2 / KONZOLNA APLIKACIJA ===");
            Console.WriteLine("1. Prikaz autora         2. Dodaj autora");
            Console.WriteLine("3. Izmeni autora         4. Obrisi autora");
            Console.WriteLine("5. Pretrazi autore       6. Sortiraj autore");
            Console.WriteLine("7. Prikaz knjiga         8. Obrisi knjigu");
            Console.WriteLine("9. Pretrazi knjige      10. Sortiraj knjige");
            Console.WriteLine("11. Dodaj autora knjizi 12. Ukloni autora sa knjige");
            Console.WriteLine("13. Lista zelja         14. Ukloni knjigu iz zelja");
            Console.WriteLine("15. Autori iz zelja     16. Knjige izdavaca");
            Console.WriteLine("17. Autori izdavaca     18. Evidentiraj kupovinu");
            Console.WriteLine("19. Prikaz adresa       20. Dodaj adresu");
            Console.WriteLine("21. Izmeni adresu       22. Obrisi adresu");
            Console.WriteLine("0. Povratak");
            Console.Write("Izbor: ");
            var izbor = Console.ReadLine()?.Trim();
            if (izbor == "0") return;
            try
            {
                switch (izbor)
                {
                    case "1": IspisiAutore(_autori.Svi()); break;
                    case "2": DodajAutora(); break;
                    case "3": IzmeniAutora(); break;
                    case "4": Console.WriteLine(_autori.Obrisi(Broj("ID autora: ")) ? "Obrisan." : "Nije pronadjen."); break;
                    case "5": IspisiAutore(_autori.Pretrazi(Tekst("Prezime ili prezime, ime: "))); break;
                    case "6": IspisiAutore(_autori.Sortiraj(Tekst("Polje (ime/prezime/email): "), DaNe("Opadajuce? (d/n): "))); break;
                    case "7": IspisiKnjige(_knjige.Sve()); break;
                    case "8": ObrisiKnjigu(); break;
                    case "9": IspisiKnjige(_knjige.Pretrazi(Tekst("Naziv ili ISBN: "))); break;
                    case "10": IspisiKnjige(_knjige.Sortiraj(Tekst("Polje (isbn/naziv/cena/godina/zanr): "), DaNe("Opadajuce? (d/n): "))); break;
                    case "11": Console.WriteLine(_knjige.DodajAutora(Broj("Autor ID: "), Broj("Knjiga ID: ")) ? "Veza dodata." : "Veza vec postoji."); break;
                    case "12": Console.WriteLine(_knjige.UkloniAutora(Broj("Autor ID: "), Broj("Knjiga ID: ")) ? "Veza uklonjena." : "Veza ne postoji."); break;
                    case "13": IspisiKnjige(_knjige.ListaZelja(Broj("Posetilac ID: "))); break;
                    case "14": UkloniZelju(); break;
                    case "15": IspisiAutore(_knjige.AutoriZaZelje(Broj("Posetilac ID: "))); break;
                    case "16": IspisiKnjige(_knjige.KnjigeIzdavaca(Broj("Izdavac ID: "))); break;
                    case "17": IspisiAutore(_knjige.AutoriIzdavaca(Broj("Izdavac ID: "))); break;
                    case "18": EvidentirajKupovinu(); break;
                    case "19": PrikaziStrane(_autori.SveAdrese(), a => $"{a.Id,4} | {a.Ulica} {a.Broj}, {a.Grad}, {a.Drzava}"); break;
                    case "20": DodajAdresu(); break;
                    case "21": IzmeniAdresu(); break;
                    case "22": Console.WriteLine(_autori.ObrisiAdresu(Broj("ID adrese: ")) ? "Obrisana." : "Ne postoji."); break;
                    default: Console.WriteLine("Nepoznata opcija."); break;
                }
            }
            catch (Exception e) { Console.WriteLine("Operacija nije izvrsena: " + e.Message); }
            Console.Write("Enter za nastavak..."); Console.ReadLine();
        }
    }

    private void DodajAdresu()
    {
        var adresa = new Adresa(Tekst("Ulica: "), Tekst("Broj: "),
            Tekst("Grad: "), Tekst("Drzava: "));
        if (DaNe("Sacuvati adresu? (d/n): "))
            Console.WriteLine("Adresa ID: " + _autori.DodajAdresu(adresa).Id);
    }

    private void IzmeniAdresu()
    {
        Adresa a = _autori.NadjiAdresu(Broj("ID adrese: "))
            ?? throw new InvalidOperationException("Adresa ne postoji.");
        a.Ulica = IzmeniTekst("Ulica", a.Ulica);
        a.Broj = IzmeniTekst("Broj", a.Broj);
        a.Grad = IzmeniTekst("Grad", a.Grad);
        a.Drzava = IzmeniTekst("Drzava", a.Drzava);
        if (DaNe("Sacuvati izmene? (d/n): "))
            Console.WriteLine(_autori.IzmeniAdresu(a) ? "Sacuvano." : "Nije sacuvano.");
    }

    private void DodajAutora()
    {
        var a = new Autor
        {
            Ime = Tekst("Ime: "), Prezime = Tekst("Prezime: "),
            DatumRodjenja = Datum("Datum rodjenja (dd.MM.yyyy): "),
            Telefon = Tekst("Telefon: "), Email = Tekst("Email: "),
            BrojLicneKarte = Tekst("Broj licne karte: "),
            GodineIskustva = Broj("Godine iskustva: ", 0)
        };
        Console.WriteLine("--- Adresa ---");
        var adr = new Adresa(Tekst("Ulica: "), Tekst("Broj: "),
            Tekst("Grad: "), Tekst("Drzava: "));
        if (DaNe("Sacuvati autora? (d/n): "))
            Console.WriteLine("Sacuvan autor ID: " + _autori.Dodaj(a, adr).Id);
    }

    private void IzmeniAutora()
    {
        Autor a = _autori.Nadji(Broj("ID autora: ")) ?? throw new InvalidOperationException("Autor ne postoji.");
        Console.WriteLine("Pritisni Enter za zadrzavanje stare vrednosti.");
        a.Ime = IzmeniTekst("Ime", a.Ime);
        a.Prezime = IzmeniTekst("Prezime", a.Prezime);
        a.Telefon = IzmeniTekst("Telefon", a.Telefon);
        a.Email = IzmeniTekst("Email", a.Email);
        a.BrojLicneKarte = IzmeniTekst("Broj LK", a.BrojLicneKarte);
        Console.Write($"Datum rodjenja [{a.DatumRodjenja:dd.MM.yyyy}] (Enter = isto): ");
        string? datumUnos = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(datumUnos))
        {
            if (!DateTime.TryParseExact(datumUnos, "dd.MM.yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime novDatum) || novDatum.Date > DateTime.Today)
                throw new ArgumentException("Datum rodjenja nije ispravan.");
            a.DatumRodjenja = novDatum;
        }
        Console.Write($"Iskustvo [{a.GodineIskustva}] (Enter = isto): ");
        string? iskustvo = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(iskustvo))
        {
            if (!int.TryParse(iskustvo, out int v) || v < 0)
                throw new ArgumentException("Neispravno iskustvo.");
            a.GodineIskustva = v;
        }
        Adresa adresa = _autori.NadjiAdresu(a.AdresaId)
            ?? throw new InvalidOperationException("Adresa autora nije pronadjena.");
        Console.WriteLine("--- Adresa autora (Enter = isto) ---");
        adresa.Ulica = IzmeniTekst("Ulica", adresa.Ulica);
        adresa.Broj = IzmeniTekst("Broj", adresa.Broj);
        adresa.Grad = IzmeniTekst("Grad", adresa.Grad);
        adresa.Drzava = IzmeniTekst("Drzava", adresa.Drzava);
        if (DaNe("Sacuvati izmene? (d/n): "))
            Console.WriteLine(_autori.IzmeniSaAdresom(a, adresa) ? "Sacuvano." : "Nije sacuvano.");
    }

    private void ObrisiKnjigu()
    {
        int id = Broj("ID knjige: ");
        if (DaNe("Trajno obrisati knjigu? (d/n): "))
            Console.WriteLine(_knjige.Obrisi(id) ? "Obrisana." : "Knjiga ne postoji.");
    }

    private void EvidentirajKupovinu()
    {
        int p = Broj("Posetilac ID: ");
        IspisiKnjige(_knjige.ListaZelja(p));
        int k = Broj("Knjiga ID sa liste zelja: ");
        DateTime datum = Datum("Datum kupovine (dd.MM.yyyy): ");
        int ocena = Broj("Ocena (1-5): ");
        if (ocena > 5) throw new ArgumentException("Ocena mora biti 1-5.");
        Console.Write("Komentar (moze prazno): ");
        string komentar = Console.ReadLine() ?? "";
        if (DaNe("Potvrdi kupovinu? (d/n): "))
            Console.WriteLine("Kupovina ID: " + _kupovine.Evidentiraj(p, k, datum, ocena, komentar));
    }

    private void UkloniZelju()
    {
        int p = Broj("Posetilac ID: "); int k = Broj("Knjiga ID: ");
        if (DaNe("Ukloniti iz liste zelja? (d/n): "))
            Console.WriteLine(_knjige.UkloniSaListeZelja(p, k) ? "Uklonjeno." : "Nije na listi.");
    }

    private static void IspisiAutore(IEnumerable<Autor> autori)
    {
        var list = autori.ToList();
        PrikaziStrane(list, a => $"{a.Id,4} | {a.Prezime,-18} {a.Ime,-15} | LK: {a.BrojLicneKarte,-14} | {a.DatumRodjenja:dd.MM.yyyy} | {a.Email}");
    }
    private static void IspisiKnjige(IEnumerable<Knjiga> knjige)
    {
        var list = knjige.ToList();
        PrikaziStrane(list, k => $"{k.Id,4} | {k.ISBN,-15} | {k.Naziv,-26} | {k.Cena,9} | {k.GodinaIzdanja} | {k.Zanr}");
    }
    private static void PrikaziStrane<T>(List<T> stavke, Func<T, string> red)
    {
        const int poStrani = 16;
        if (stavke.Count == 0) { Console.WriteLine("Nema rezultata."); return; }
        int strana = 0, brojStrana = (stavke.Count + poStrani - 1) / poStrani;
        while (true)
        {
            Console.WriteLine($"\nStrana {strana + 1}/{brojStrana}, ukupno {stavke.Count} rezultata:");
            foreach (T s in stavke.Skip(strana * poStrani).Take(poStrani)) Console.WriteLine(red(s));
            Console.Write("n = sledeca, p = prethodna, Enter = kraj: ");
            string unos = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
            if (unos == "n" && strana < brojStrana - 1) strana++;
            else if (unos == "p" && strana > 0) strana--;
            else if (unos == "") return;
            else Console.WriteLine("Nije moguce preci na trazenu stranu.");
        }
    }

    private static string Tekst(string naslov)
    {
        while (true)
        {
            Console.Write(naslov); string? s = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(s)) return s;
            Console.WriteLine("Polje je obavezno.");
        }
    }
    private static string IzmeniTekst(string naziv, string stara)
    {
        Console.Write($"{naziv} [{stara}]: ");
        string? unos = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(unos) ? stara : unos;
    }
    private static int Broj(string naslov, int min = 1)
    {
        while (true)
        {
            Console.Write(naslov);
            if (int.TryParse(Console.ReadLine(), out int n) && n >= min) return n;
            Console.WriteLine($"Unesi ceo broj od {min} nadalje.");
        }
    }
    private static DateTime Datum(string naslov)
    {
        while (true)
        {
            Console.Write(naslov);
            if (DateTime.TryParseExact(Console.ReadLine(), "dd.MM.yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var datum) && datum.Date <= DateTime.Today) return datum;
            Console.WriteLine("Datum nije ispravan.");
        }
    }
    private static bool DaNe(string poruka)
    {
        Console.Write(poruka);
        return string.Equals(Console.ReadLine()?.Trim(), "d", StringComparison.OrdinalIgnoreCase);
    }
}
