using System;
using System.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services
{
    /// <summary>
    /// Poslovne provere pri izmeni postojecih entiteta.
    /// Ne zavisi od konzole i moze se koristiti i u WPF aplikaciji.
    /// Knjige ostaju u KnjigaPravilaService, a postavljanje sefa
    /// izdavaca u IzdavacService.
    /// </summary>
    public class KatalogIzmenaService
    {
        private readonly XmlDao<Adresa> _adrese = new("Adrese.xml");
        private readonly XmlDao<Autor> _autori = new("Autori.xml");
        private readonly XmlDao<Posetilac> _posetioci = new("Posetioci.xml");
        private readonly XmlDao<Izdavac> _izdavaci = new("Izdavaci.xml");
        private readonly XmlDao<Kupovina> _kupovine = new("Kupovine.xml");

        /// <summary>
        /// Izmeni autora uz proveru jedinstvenosti LK i ogranicenja za sefa.
        /// </summary>
        public bool IzmeniAutora(Autor autor)
        {
            ArgumentNullException.ThrowIfNull(autor);
            ZahtevajPostojanje(_autori.PronadjiPoId(autor.Id), "Autor");

            ValidacijaOsoba.ProveriAutora(autor);

            if (autor.GodineIskustva < 5 &&
                _izdavaci.UcitajSve().Any(i => i.SefAutorId == autor.Id))
                throw new InvalidOperationException(
                    "Autor je sef izdavaca i mora imati najmanje 5 godina iskustva.");

            ProveriAdresu(autor.AdresaId);
            string broj = autor.BrojLicneKarte.Trim();
            if (_autori.UcitajSve().Any(a => a.Id != autor.Id &&
                string.Equals(a.BrojLicneKarte?.Trim(), broj,
                    StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    "Broj licne karte vec pripada drugom autoru.");

            autor.BrojLicneKarte = broj;
            return _autori.Izmeni(autor);
        }

        /// <summary>
        /// Izmeni posetioca uz proveru clanske karte i podataka o osobi.
        /// </summary>
        public bool IzmeniPosetioca(Posetilac posetilac)
        {
            ArgumentNullException.ThrowIfNull(posetilac);
            Posetilac postojeci = _posetioci.PronadjiPoId(posetilac.Id)
                ?? throw new InvalidOperationException("Posetilac ne postoji.");

            ValidacijaOsoba.ProveriPosetioca(posetilac);
            ProveriAdresu(posetilac.AdresaId);

            string karta = posetilac.BrojClanskeKarte.Trim();
            if (_posetioci.UcitajSve().Any(p => p.Id != posetilac.Id &&
                string.Equals(p.BrojClanskeKarte?.Trim(), karta,
                    StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    "Broj clanske karte vec pripada drugom posetiocu.");

            posetilac.BrojClanskeKarte = karta;
            // Prosecna ocena pripada kupovinama, ne rucnoj izmeni posetioca.
            posetilac.ProsecnaOcenaRecenzija = postojeci.ProsecnaOcenaRecenzija;
            return _posetioci.Izmeni(posetilac);
        }

        /// <summary>
        /// Izmeni izdavaca i proveri sifru i uslove za sefa.
        /// </summary>
        public bool IzmeniIzdavaca(Izdavac izdavac)
        {
            ArgumentNullException.ThrowIfNull(izdavac);
            Izdavac postojeci = _izdavaci.PronadjiPoId(izdavac.Id)
                ?? throw new InvalidOperationException("Izdavac ne postoji.");

            ProveriTekst(izdavac.Sifra, "Sifra izdavaca");
            ProveriTekst(izdavac.Naziv, "Naziv izdavaca");

            // Sef se menja iskljucivo kroz IzdavacService.PostaviSefaIzdavaca.
            if (izdavac.SefAutorId != postojeci.SefAutorId)
                throw new InvalidOperationException(
                    "Sef izdavaca se postavlja kroz posebnu poslovnu operaciju.");

            string sifra = izdavac.Sifra.Trim();
            if (_izdavaci.UcitajSve().Any(i => i.Id != izdavac.Id &&
                string.Equals(i.Sifra?.Trim(), sifra,
                    StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException(
                    "Sifra izdavaca vec pripada drugom izdavacu.");

            izdavac.Sifra = sifra;
            return _izdavaci.Izmeni(izdavac);
        }

        /// <summary>
        /// Izmeni postojecu adresu i proveri obavezna polja.
        /// </summary>
        public bool IzmeniAdresu(Adresa adresa)
        {
            ArgumentNullException.ThrowIfNull(adresa);
            ZahtevajPostojanje(_adrese.PronadjiPoId(adresa.Id), "Adresa");
            ProveriTekst(adresa.Ulica, "Ulica");
            ProveriTekst(adresa.Broj, "Kucni broj");
            ProveriTekst(adresa.Grad, "Grad");
            ProveriTekst(adresa.Drzava, "Drzava");
            return _adrese.Izmeni(adresa);
        }

        /// <summary>
        /// Menjaj dozvoljene detalje kupovine i azuriraj prosek ocena.
        /// </summary>
        public bool IzmeniKupovinu(Kupovina kupovina)
        {
            ArgumentNullException.ThrowIfNull(kupovina);
            return XmlTransakcija.Izvrsi(
                new[] { "Kupovine.xml", "Posetioci.xml" },
                () =>
                {
                    Kupovina postojeca = _kupovine.PronadjiPoId(kupovina.Id)
                        ?? throw new InvalidOperationException("Kupovina ne postoji.");

                    // Posetilac i knjiga su deo identiteta ove kupovine;
                    // njihovu zamenu treba obraditi kao drugu poslovnu operaciju.
                    if (kupovina.PosetilacId != postojeca.PosetilacId ||
                        kupovina.KnjigaId != postojeca.KnjigaId)
                        throw new InvalidOperationException(
                            "Posetilac i knjiga postojece kupovine ne mogu se menjati.");

                    if (kupovina.Ocena < 1 || kupovina.Ocena > 5)
                        throw new InvalidOperationException("Ocena mora biti od 1 do 5.");

                    if (kupovina.DatumKupovine == default ||
                        kupovina.DatumKupovine.Date > DateTime.Today)
                        throw new InvalidOperationException("Datum kupovine nije ispravan.");

                    kupovina.Komentar ??= string.Empty;
                    kupovina.CenaPriKupovini = postojeca.CenaPriKupovini;
                    if (!_kupovine.Izmeni(kupovina)) return false;

                    Posetilac posetilac = _posetioci.PronadjiPoId(kupovina.PosetilacId)
                        ?? throw new InvalidOperationException("Posetilac ne postoji.");
                    var kupovinePosetioca = _kupovine.UcitajSve()
                        .Where(k => k.PosetilacId == kupovina.PosetilacId).ToList();
                    posetilac.ProsecnaOcenaRecenzija = kupovinePosetioca.Count == 0
                        ? 0 : kupovinePosetioca.Average(k => k.Ocena);
                    if (!_posetioci.Izmeni(posetilac))
                        throw new InvalidOperationException("Prosecna ocena nije sacuvana.");
                    return true;
                });
        }

        /// <summary>
        /// Iskljucivo za stare kupovine bez sacuvane cene: istorijski iznos
        /// unosi se iz poznate evidencije, NIKADA se ne pogadja iz danasnje cene.
        /// </summary>
        public bool EvidentirajIstorijskuCenu(int kupovinaId, decimal stvarnoPlaceno)
        {
            if (stvarnoPlaceno < 0)
                throw new InvalidOperationException("Istorijska cena ne moze biti negativna.");
            return XmlTransakcija.Izvrsi(new[] { "Kupovine.xml" }, () =>
            {
                Kupovina kupovina = _kupovine.PronadjiPoId(kupovinaId)
                    ?? throw new InvalidOperationException("Kupovina ne postoji.");
                if (kupovina.CenaPriKupovini.HasValue)
                    throw new InvalidOperationException(
                        "Kupovina vec ima istorijsku cenu. Izmena nije dozvoljena.");
                kupovina.CenaPriKupovini = stvarnoPlaceno;
                return _kupovine.Izmeni(kupovina);
            });
        }

        // Obezbedi da povezana adresa zaista postoji.
        private void ProveriAdresu(int id)
        {
            if (id <= 0 || _adrese.PronadjiPoId(id) == null)
                throw new InvalidOperationException("Izabrana adresa ne postoji.");
        }

        // Proveri da tekstualno polje nije prazno.
        private static void ProveriTekst(string? vrednost, string polje)
        {
            if (string.IsNullOrWhiteSpace(vrednost))
                throw new InvalidOperationException($"Polje '{polje}' je obavezno.");
        }

        // Datum rodjenja ne sme biti nepoznat ili u buducnosti.
        private static void ProveriDatumRodjenja(DateTime datum)
        {
            if (datum == default || datum.Date > DateTime.Today)
                throw new InvalidOperationException("Datum rodjenja nije ispravan.");
        }

        // Pre izmene proveri da li entitet postoji.
        private static void ZahtevajPostojanje(object? entitet, string naziv)
        {
            if (entitet == null)
                throw new InvalidOperationException($"{naziv} ne postoji.");
        }
    }
}
