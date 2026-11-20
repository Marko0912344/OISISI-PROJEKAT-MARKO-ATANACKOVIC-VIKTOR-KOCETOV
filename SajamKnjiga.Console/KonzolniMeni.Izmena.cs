using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;
using SajamKnjiga.Core.Services;

namespace SajamKnjiga.ConsoleApp
{
    // Deo konzolnog menija: izmena.
    public partial class KonzolniMeni
    {
        // Ucitaj postojeci zapis; Enter zadrzava stare vrednosti.
        private void IzmeniAutora()
        {
            int id = UnesiId("ID autora: ");
            Autor a = _autori.PronadjiPoId(id)
                ?? throw new InvalidOperationException("Autor ne postoji.");
            Console.WriteLine("Enter zadrzava postojecu vrednost.");
            a.Ime = IzmeniTekst("Ime", a.Ime);
            a.Prezime = IzmeniTekst("Prezime", a.Prezime);
            a.DatumRodjenja = IzmeniDatum("Datum rodjenja", a.DatumRodjenja);
            a.Telefon = IzmeniTekst("Telefon", a.Telefon);
            a.Email = IzmeniTekst("Email", a.Email);
            string noviBroj = IzmeniTekst("Broj licne karte", a.BrojLicneKarte);
            a.BrojLicneKarte = noviBroj;
            int iskustvo = IzmeniBroj("Godine iskustva", a.GodineIskustva, 0);
            a.GodineIskustva = iskustvo;
            int novaAdresa = IzmeniBroj("Adresa ID", a.AdresaId, 1);
            a.AdresaId = novaAdresa;
            if (!Potvrdi("Sacuvati izmene autora?")) return;
            _katalogIzmena.IzmeniAutora(a);
            Console.WriteLine("Autor izmenjen.");
        }

        // Izmeni atribute knjige uz poslovne provere.
        private void IzmeniKnjigu()
        {
            int id = UnesiId("ID knjige: ");
            Knjiga k = _knjige.PronadjiPoId(id)
                ?? throw new InvalidOperationException("Knjiga ne postoji.");

            Console.WriteLine("Enter zadrzava postojecu vrednost.");
            k.ISBN = IzmeniTekst("ISBN", k.ISBN);
            k.Naziv = IzmeniTekst("Naziv", k.Naziv);
            k.Zanr = IzmeniTekst("Zanr", k.Zanr);
            k.GodinaIzdanja = IzmeniBroj(
                "Godina izdanja", k.GodinaIzdanja, 1, 9999);
            k.Cena = IzmeniCenu(k.Cena);
            k.BrojStrana = IzmeniBroj("Broj strana", k.BrojStrana, 1);

            Console.Write($"Izdavac ID [{k.IzdavacId?.ToString() ?? "nema"}] " +
                "(Enter = ne menjaj): ");
            string izdUnos = Console.ReadLine()?.Trim() ?? "";
            if (izdUnos.Length > 0)
            {
                if (!int.TryParse(izdUnos, out int noviIzdavacId) ||
                    noviIzdavacId <= 0)
                    throw new ArgumentException("Neispravan ID izdavaca.");

                k.IzdavacId = noviIzdavacId;
            }

            if (!Potvrdi("Sacuvati izmene knjige?")) return;

            // Poslovne provere sada pripadaju Core servisu, ne meniju.
            _knjigaPravila.IzmeniKnjigu(k);
            Console.WriteLine(
                "Knjiga izmenjena. Autori se menjaju kroz Poslovne operacije.");
        }

        // Izmeni podatke posetioca bez menjanja njegovog ID-ja.
        private void IzmeniPosetioca()
        {
            int id = UnesiId("ID posetioca: ");
            Posetilac p = _posetioci.PronadjiPoId(id)
                ?? throw new InvalidOperationException("Posetilac ne postoji.");
            Console.WriteLine("Enter zadrzava postojecu vrednost.");
            p.Ime = IzmeniTekst("Ime", p.Ime);
            p.Prezime = IzmeniTekst("Prezime", p.Prezime);
            p.DatumRodjenja = IzmeniDatum("Datum rodjenja", p.DatumRodjenja);
            p.Telefon = IzmeniTekst("Telefon", p.Telefon);
            p.Email = IzmeniTekst("Email", p.Email);
            string karta = IzmeniTekst("Broj clanske karte", p.BrojClanskeKarte);
            p.BrojClanskeKarte = karta;
            p.TrenutnaGodinaClanstva = IzmeniBroj(
                "Godina clanstva", p.TrenutnaGodinaClanstva, 1, 9999);
            Console.Write($"Status [{p.Status}] R/V (Enter = isto): ");
            string statusUnos = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "";
            if (statusUnos.Length > 0)
            {
                if (statusUnos != "R" && statusUnos != "V")
                    throw new ArgumentException("Status mora biti R ili V.");
                p.Status = statusUnos == "R" ? StatusPosetioca.R : StatusPosetioca.V;
            }
            int novaAdresa = IzmeniBroj("Adresa ID", p.AdresaId, 1);
            p.AdresaId = novaAdresa;
            if (!Potvrdi("Sacuvati izmene posetioca?")) return;
            _katalogIzmena.IzmeniPosetioca(p);
            Console.WriteLine("Posetilac izmenjen.");
        }

        // Izmeni poslovne podatke izdavaca.
        private void IzmeniIzdavaca()
        {
            int id = UnesiId("ID izdavaca: ");
            Izdavac i = _izdavaci.PronadjiPoId(id)
                ?? throw new InvalidOperationException("Izdavac ne postoji.");
            string sifra = IzmeniTekst("Sifra", i.Sifra);
            i.Sifra = sifra;
            i.Naziv = IzmeniTekst("Naziv", i.Naziv);
            if (!Potvrdi("Sacuvati izmene izdavaca?")) return;
            _katalogIzmena.IzmeniIzdavaca(i);
            Console.WriteLine("Izdavac izmenjen. Sef se postavlja kroz Poslovne operacije.");
        }

        // Izmeni postojecu adresu.
        private void IzmeniAdresu()
        {
            int id = UnesiId("ID adrese: ");
            Adresa a = _adrese.PronadjiPoId(id)
                ?? throw new InvalidOperationException("Adresa ne postoji.");
            Console.WriteLine("PAZNJA: ako vise entiteta koristi istu adresu, " +
                "izmena ce se videti kod svih.");
            a.Ulica = IzmeniTekst("Ulica", a.Ulica);
            a.Broj = IzmeniTekst("Broj", a.Broj);
            a.Grad = IzmeniTekst("Grad", a.Grad);
            a.Drzava = IzmeniTekst("Drzava", a.Drzava);
            if (!Potvrdi("Sacuvati izmene adrese?")) return;
            _katalogIzmena.IzmeniAdresu(a);
            Console.WriteLine("Adresa izmenjena.");
        }

        // Izmeni dozvoljene detalje kupovine, ukljucujuci ocenu.
        private void IzmeniKupovinu()
        {
            int id = UnesiId("ID kupovine: ");
            Kupovina k = _kupovine.PronadjiPoId(id)
                ?? throw new InvalidOperationException("Kupovina ne postoji.");
            Console.WriteLine($"Posetilac ID {k.PosetilacId}, Knjiga ID {k.KnjigaId}");
            Console.WriteLine("Veze se ne menjaju kroz ovu opciju.");
            k.DatumKupovine = IzmeniDatum("Datum kupovine", k.DatumKupovine);
            k.Ocena = IzmeniBroj("Ocena", k.Ocena, 1, 5);
            Console.Write($"Komentar [{k.Komentar}] (Enter = isto): ");
            string komentar = Console.ReadLine() ?? "";
            if (komentar.Length > 0) k.Komentar = komentar;
            if (!Potvrdi("Sacuvati izmene kupovine?")) return;
            _katalogIzmena.IzmeniKupovinu(k);
            Console.WriteLine("Kupovina izmenjena.");
        }
    }
}
