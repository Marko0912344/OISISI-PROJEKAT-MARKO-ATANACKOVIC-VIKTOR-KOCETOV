using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services
{
    /// <summary>
    /// Centralizovan unos entiteta. Jedna korisnicka operacija moze da
    /// promeni vise XML datoteka, zato se cuva kao jedna XML transakcija.
    /// Ne sadrzi Console logiku i moze se koristiti i iz WPF projekta.
    /// </summary>
    public class KatalogUnosService
    {
        private readonly XmlDao<Adresa> _adrese = new("Adrese.xml");
        private readonly XmlDao<Autor> _autori = new("Autori.xml");
        private readonly XmlDao<Knjiga> _knjige = new("Knjige.xml");
        private readonly XmlDao<Posetilac> _posetioci = new("Posetioci.xml");
        private readonly XmlDao<Izdavac> _izdavaci = new("Izdavaci.xml");
        private readonly XmlStorage<AutorKnjiga> _autorKnjiga =
            new("AutorKnjiga.xml");

        /// <summary>
        /// Proveri polja adrese i dodaj novu adresu kroz genericki DAO.
        /// </summary>
        public Adresa DodajAdresu(Adresa adresa)
        {
            ArgumentNullException.ThrowIfNull(adresa);

            // U jednoj logickoj operaciji upisujemo vise povezanih XML datoteka.
            return XmlTransakcija.Izvrsi(
                new[] { "Adrese.xml", "IdBrojaci.xml" },
                () =>
                {
                    ProveriNovuAdresu(adresa);
                    return _adrese.Dodaj(adresa);
                });
        }

        /// <summary>
        /// Sacuvaj autora i njegovu adresu kao jednu logicku transakciju.
        /// </summary>
        public Autor DodajAutora(Autor autor, Adresa adresa)
        {
            ArgumentNullException.ThrowIfNull(autor);
            ArgumentNullException.ThrowIfNull(adresa);

            return XmlTransakcija.Izvrsi(
                new[] { "Adrese.xml", "Autori.xml", "IdBrojaci.xml" },
                () =>
                {
                    ProveriNovuAdresu(adresa);
                    ProveriNovEntitet(autor.Id, "Autor");
                    ValidacijaOsoba.ProveriAutora(autor);

                    string lk = autor.BrojLicneKarte.Trim();
                    if (_autori.UcitajSve().Any(a =>
                        string.Equals(a.BrojLicneKarte?.Trim(), lk,
                            StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException(
                            "Autor sa ovim brojem licne karte vec postoji.");

                    _adrese.Dodaj(adresa);
                    autor.Adresa = adresa;
                    autor.AdresaId = adresa.Id;
                    autor.BrojLicneKarte = lk;
                    return _autori.Dodaj(autor);
                });
        }

        /// <summary>
        /// Sacuvaj posetioca i njegovu adresu uz jedinstvenu clansku kartu.
        /// </summary>
        public Posetilac DodajPosetioca(Posetilac posetilac, Adresa adresa)
        {
            ArgumentNullException.ThrowIfNull(posetilac);
            ArgumentNullException.ThrowIfNull(adresa);

            return XmlTransakcija.Izvrsi(
                new[] { "Adrese.xml", "Posetioci.xml", "IdBrojaci.xml" },
                () =>
                {
                    ProveriNovuAdresu(adresa);
                    ProveriNovEntitet(posetilac.Id, "Posetilac");
                    ValidacijaOsoba.ProveriPosetioca(posetilac);

                    string karta = posetilac.BrojClanskeKarte.Trim();
                    if (_posetioci.UcitajSve().Any(p =>
                        string.Equals(p.BrojClanskeKarte?.Trim(), karta,
                            StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException(
                            "Posetilac sa ovom clanskom kartom vec postoji.");

                    _adrese.Dodaj(adresa);
                    posetilac.Adresa = adresa;
                    posetilac.AdresaId = adresa.Id;
                    posetilac.BrojClanskeKarte = karta;
                    // Novi posetilac jos nema recenzije; prosek nije slobodno polje.
                    posetilac.ProsecnaOcenaRecenzija = 0;
                    return _posetioci.Dodaj(posetilac);
                });
        }

        /// <summary>
        /// Dodaj izdavaca uz jedinstvenu sifru i dozvoljene pocetne veze.
        /// </summary>
        public Izdavac DodajIzdavaca(Izdavac izdavac)
        {
            ArgumentNullException.ThrowIfNull(izdavac);

            return XmlTransakcija.Izvrsi(
                new[] { "Izdavaci.xml", "IdBrojaci.xml" },
                () =>
                {
                    ProveriNovEntitet(izdavac.Id, "Izdavac");
                    ProveriObavezanTekst(izdavac.Sifra, "Sifra izdavaca");
                    ProveriObavezanTekst(izdavac.Naziv, "Naziv izdavaca");
                    string sifra = izdavac.Sifra.Trim();
                    if (_izdavaci.UcitajSve().Any(i =>
                        string.Equals(i.Sifra?.Trim(), sifra,
                            StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException(
                            "Izdavac sa ovom sifrom vec postoji.");

                    izdavac.Sifra = sifra;
                    return _izdavaci.Dodaj(izdavac);
                });
        }

        /// <summary>
        /// Validiraj ISBN, izdavaca i autore i sacuvaj knjigu sa M:N vezama.
        /// </summary>
        public Knjiga DodajKnjigu(
            Knjiga knjiga, int izdavacId, IEnumerable<int> autorIds)
        {
            ArgumentNullException.ThrowIfNull(knjiga);
            ArgumentNullException.ThrowIfNull(autorIds);

            // Kopiramo izbor da se ne menja tokom operacije.
            int[] odabraniAutori = autorIds.ToArray();

            return XmlTransakcija.Izvrsi(
                new[] { "Knjige.xml", "AutorKnjiga.xml", "IdBrojaci.xml" },
                () =>
                {
                    ProveriNovEntitet(knjiga.Id, "Knjiga");
                    ProveriObavezanTekst(knjiga.ISBN, "ISBN");
                    ProveriObavezanTekst(knjiga.Naziv, "Naziv knjige");
                    ProveriObavezanTekst(knjiga.Zanr, "Zanr knjige");
                    if (knjiga.GodinaIzdanja < 1 ||
                        knjiga.GodinaIzdanja > 9999 ||
                        knjiga.BrojStrana < 1 || knjiga.Cena < 0)
                        throw new InvalidOperationException(
                            "Godina izdanja, broj strana ili cena nisu ispravni.");

                    string isbn = IsbnPravila.Normalizuj(knjiga.ISBN);
                    if (_knjige.UcitajSve().Any(k =>
                        string.Equals(IsbnPravila.Normalizuj(k.ISBN), isbn,
                            StringComparison.Ordinal)))
                        throw new InvalidOperationException(
                            "Knjiga sa ovim ISBN-om vec postoji.");

                    if (_izdavaci.PronadjiPoId(izdavacId) == null)
                        throw new InvalidOperationException(
                            "Izabrani izdavac ne postoji.");

                    if (odabraniAutori.Length == 0 ||
                        odabraniAutori.Any(id => id <= 0) ||
                        odabraniAutori.Distinct().Count() != odabraniAutori.Length)
                        throw new InvalidOperationException(
                            "Izaberi bar jednog autora, bez ponavljanja.");

                    HashSet<int> postojeciAutori = _autori.UcitajSve()
                        .Select(a => a.Id).ToHashSet();
                    if (odabraniAutori.Any(id => !postojeciAutori.Contains(id)))
                        throw new InvalidOperationException(
                            "Jedan od izabranih autora ne postoji.");

                    knjiga.ISBN = isbn;
                    knjiga.IzdavacId = izdavacId;
                    _knjige.Dodaj(knjiga);

                    List<AutorKnjiga> veze = _autorKnjiga.Ucitaj();
                    foreach (int autorId in odabraniAutori)
                    {
                        veze.Add(new AutorKnjiga
                        {
                            AutorId = autorId,
                            KnjigaId = knjiga.Id
                        });
                    }
                    _autorKnjiga.Sacuvaj(veze);
                    return knjiga;
                });
        }

        // Adresa mora imati obavezne podatke pre cuvanja.
        private static void ProveriNovuAdresu(Adresa adresa)
        {
            ProveriNovEntitet(adresa.Id, "Adresa");
            ProveriObavezanTekst(adresa.Ulica, "Ulica");
            ProveriObavezanTekst(adresa.Broj, "Kucni broj");
            ProveriObavezanTekst(adresa.Grad, "Grad");
            ProveriObavezanTekst(adresa.Drzava, "Drzava");
        }

        // Novom entitetu ID treba da dodeli DAO, ne pozivalac.
        private static void ProveriNovEntitet(int id, string naziv)
        {
            if (id != 0)
                throw new InvalidOperationException(
                    $"Novi entitet ({naziv}) ne sme vec imati ID.");
        }

        // Odbij prazan ili beli tekst u obaveznom polju.
        private static void ProveriObavezanTekst(string? vrednost,
            string naziv)
        {
            if (string.IsNullOrWhiteSpace(vrednost))
                throw new InvalidOperationException(
                    $"Polje '{naziv}' je obavezno.");
        }
    }
}
