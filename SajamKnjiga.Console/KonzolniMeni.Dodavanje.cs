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
    // Deo konzolnog menija: dodavanje.
    public partial class KonzolniMeni
    {
        // Sve operacije dodavanja idu kroz Core servis i XML transakcije.
        private readonly KatalogUnosService _katalogUnos = new();

        // Zatrazi podatke o autoru i adresi, zatim pozovi servis.
        private void DodajNovogAutora()
        {
            Console.WriteLine();
            Console.WriteLine("===== DODAVANJE NOVOG AUTORA =====");

            string ime = UnesiObavezanTekst("Ime: ");
            string prezime = UnesiObavezanTekst("Prezime: ");
            DateTime datumRodjenja = UnesiObavezanDatum("Datum rodjenja (dd.MM.yyyy): ");
            string telefon = UnesiObavezanTekst("Telefon: ");
            string email = UnesiObavezanTekst("Email: ");
            string brojLicneKarte = UnesiObavezanTekst("Broj licne karte: ");
            int iskustvo = UnesiBroj("Godine iskustva: ", 0);

            // Interni ID je automatski, a broj LK je poslovni jedinstveni podatak.
            bool duplikat = _autori.UcitajSve().Any(a =>
                string.Equals(a.BrojLicneKarte.Trim(), brojLicneKarte,
                    StringComparison.OrdinalIgnoreCase));

            if (duplikat)
            {
                Console.WriteLine("Autor sa ovim brojem licne karte vec postoji.");
                Console.WriteLine("Nista nije sacuvano.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine("--- Adresa autora ---");
            Adresa adresa = UnesiNovuAdresu();

            if (!Potvrdi("Sacuvati autora i njegovu adresu?"))
            {
                Console.WriteLine("Unos je otkazan.");
                return;
            }

            Autor autor = new Autor(
                ime, prezime, datumRodjenja, adresa,
                telefon, email, brojLicneKarte, iskustvo)
            {
                AdresaId = adresa.Id
            };

            _katalogUnos.DodajAutora(autor, adresa);
            Console.WriteLine($"Sacuvan autor ID = {autor.Id}.");
            Console.WriteLine($"Sacuvana adresa ID = {adresa.Id}.");
            Console.WriteLine("Podaci su upisani u Autori.xml i Adrese.xml.");
        }

        // Zatrazi ISBN, izdavaca i autore uz provere u servisnom sloju.
        private void DodajNovuKnjigu()
        {
            Console.WriteLine();
            Console.WriteLine("===== DODAVANJE NOVE KNJIGE =====");

            List<Autor> autori = _autori.UcitajSve();
            List<Izdavac> izdavaci = _izdavaci.UcitajSve();

            if (autori.Count == 0 || izdavaci.Count == 0)
            {
                Console.WriteLine(
                    "Za unos knjige potreban je bar jedan autor i jedan izdavac.");
                return;
            }

            string isbn = UnesiObavezanTekst("ISBN: ");
            bool isbnPostoji = _knjige.UcitajSve().Any(k =>
                string.Equals(k.ISBN.Trim(), isbn, StringComparison.OrdinalIgnoreCase));

            if (isbnPostoji)
            {
                Console.WriteLine("Knjiga sa tim ISBN-om vec postoji.");
                Console.WriteLine("Nista nije sacuvano.");
                return;
            }

            string naziv = UnesiObavezanTekst("Naziv: ");
            string zanr = UnesiObavezanTekst("Zanr: ");
            int godina = UnesiBroj("Godina izdanja: ", 1, 9999);
            decimal cena = UnesiCenu();
            int brojStrana = UnesiBroj("Broj strana: ", 1);

            Console.WriteLine();
            Console.WriteLine("--- Izaberi izdavaca ---");
            foreach (Izdavac i in izdavaci.OrderBy(i => i.Naziv))
            {
                Console.WriteLine($"{i.Id} | {i.Naziv} | Sifra: {i.Sifra}");
            }

            int izdavacId;
            while (true)
            {
                izdavacId = UnesiId("ID izdavaca: ");
                if (izdavaci.Any(i => i.Id == izdavacId)) break;
                Console.WriteLine("Taj izdavac ne postoji. Pokusaj ponovo.");
            }

            Console.WriteLine();
            Console.WriteLine("--- Izaberi jednog ili vise autora ---");
            foreach (Autor a in autori
                .OrderBy(a => a.Prezime, StringComparer.CurrentCultureIgnoreCase))
            {
                Console.WriteLine($"{a.Id} | {a.Ime} {a.Prezime}");
            }

            Console.WriteLine(
                "Unesi ID autora, pa Enter. Za dodatnog autora ponovi unos.");
            Console.WriteLine(
                "Kada zavrsis, pritisni samo Enter (bar jedan autor je obavezan).");

            HashSet<int> odabraniAutori = new();

            while (true)
            {
                Console.Write("ID autora (Enter = kraj): ");
                string unos = Console.ReadLine()?.Trim() ?? "";

                if (unos.Length == 0)
                {
                    if (odabraniAutori.Count > 0) break;
                    Console.WriteLine("Moramo izabrati bar jednog autora.");
                    continue;
                }

                if (!int.TryParse(unos, out int autorId) ||
                    !autori.Any(a => a.Id == autorId))
                {
                    Console.WriteLine("Nepostojeci ID autora.");
                    continue;
                }

                if (!odabraniAutori.Add(autorId))
                {
                    Console.WriteLine("Taj autor je vec izabran.");
                }
                else
                {
                    Console.WriteLine("Autor dodat u izbor.");
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Knjiga: {naziv}");
            Console.WriteLine($"ISBN: {isbn}");
            Console.WriteLine($"Broj autora: {odabraniAutori.Count}");
            Console.WriteLine($"Izdavac ID: {izdavacId}");

            if (!Potvrdi("Sacuvati knjigu i sve veze?"))
            {
                Console.WriteLine("Unos je otkazan.");
                return;
            }

            Knjiga knjiga = new Knjiga
            {
                ISBN = isbn,
                Naziv = naziv,
                Zanr = zanr,
                GodinaIzdanja = godina,
                Cena = cena,
                BrojStrana = brojStrana
            };

            // Knjiga, izdavac i sve veze sa autorima cuvaju se zajedno.
            _katalogUnos.DodajKnjigu(knjiga, izdavacId, odabraniAutori);

            Console.WriteLine($"Sacuvana knjiga ID = {knjiga.Id}.");
            Console.WriteLine("Knjige.xml: podaci o knjizi i IzdavacId.");
            Console.WriteLine("AutorKnjiga.xml: veze sa odabranim autorima.");
        }

        // Zatrazi podatke posetioca i njegovu adresu.
        private void DodajNovogPosetioca()
        {
            Console.WriteLine("\n===== NOVI POSETILAC =====");
            string ime = UnesiObavezanTekst("Ime: ");
            string prezime = UnesiObavezanTekst("Prezime: ");
            DateTime rodjenje = UnesiObavezanDatum("Datum rodjenja dd.MM.yyyy: ");
            string telefon = UnesiObavezanTekst("Telefon: ");
            string email = UnesiObavezanTekst("Email: ");
            string karta = UnesiObavezanTekst("Broj clanske karte: ");
            int godina = UnesiBroj("Godina clanstva: ", 1, 9999);
            StatusPosetioca status = UnesiStatus();

            if (_posetioci.UcitajSve().Any(p =>
                string.Equals(p.BrojClanskeKarte, karta,
                    StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Clanska karta vec postoji.");

            Console.WriteLine("--- Adresa ---");
            Adresa adresa = UnesiNovuAdresu();
            if (!Potvrdi("Sacuvati posetioca i adresu?")) return;
            Posetilac p = new Posetilac(
                ime, prezime, rodjenje, adresa, telefon, email,
                karta, godina, status) { AdresaId = adresa.Id };
            _katalogUnos.DodajPosetioca(p, adresa);
            Console.WriteLine($"Sacuvan posetilac ID = {p.Id}, adresa ID = {adresa.Id}.");
        }

        // Zatrazi sifru i naziv izdavaca.
        private void DodajNovogIzdavaca()
        {
            Console.WriteLine("\n===== NOVI IZDAVAC =====");
            string sifra = UnesiObavezanTekst("Sifra: ");
            string naziv = UnesiObavezanTekst("Naziv: ");
            if (_izdavaci.UcitajSve().Any(i =>
                string.Equals(i.Sifra, sifra,
                    StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Sifra izdavaca vec postoji.");
            if (!Potvrdi("Sacuvati izdavaca?")) return;
            Izdavac i = new Izdavac(sifra, naziv);
            _katalogUnos.DodajIzdavaca(i);
            Console.WriteLine($"Sacuvan izdavac ID = {i.Id}.");
            Console.WriteLine("Sefa postavi kasnije kroz Poslovne operacije, " +
                "kada izdavac dobije knjigu i autora sa >= 5 godina iskustva.");
        }

        // Zatrazi podatke o samostalnoj adresi.
        private void DodajNovuAdresu()
        {
            Console.WriteLine("\n===== NOVA ADRESA =====");
            Adresa a = UnesiNovuAdresu();
            if (!Potvrdi("Sacuvati adresu?")) return;
            _katalogUnos.DodajAdresu(a);
            Console.WriteLine($"Sacuvana adresa ID = {a.Id}.");
        }

        // Pomocna metoda za unos polja adrese.
        private static Adresa UnesiNovuAdresu() => new Adresa(
            UnesiObavezanTekst("Ulica: "),
            UnesiObavezanTekst("Broj: "),
            UnesiObavezanTekst("Grad: "),
            UnesiObavezanTekst("Drzava: "));
    }
}
