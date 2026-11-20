using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services
{
    /// <summary>
    /// Poslovna pravila koja se odnose na izmenu knjige i uklanjanje
    /// njene veze sa autorom. Ne koristi Console i moze se pozvati iz WPF-a.
    /// </summary>
    public class KnjigaPravilaService
    {
        private readonly XmlDao<Knjiga> _knjige = new("Knjige.xml");
        private readonly XmlDao<Autor> _autori = new("Autori.xml");
        private readonly XmlDao<Izdavac> _izdavaci = new("Izdavaci.xml");
        private readonly XmlStorage<AutorKnjiga> _veze =
            new("AutorKnjiga.xml");

        // Menja podatke knjige, uz provere poslovnih ogranicenja.
        public bool IzmeniKnjigu(Knjiga izmenjena)
        {
            ArgumentNullException.ThrowIfNull(izmenjena);

            Knjiga prethodna = _knjige.PronadjiPoId(izmenjena.Id)
                ?? throw new InvalidOperationException("Knjiga ne postoji.");

            string isbn = IsbnPravila.Normalizuj(izmenjena.ISBN);

            if (string.IsNullOrWhiteSpace(izmenjena.Naziv) ||
                string.IsNullOrWhiteSpace(izmenjena.Zanr))
                throw new InvalidOperationException(
                    "Naziv i zanr knjige su obavezni.");

            if (izmenjena.GodinaIzdanja < 1 ||
                izmenjena.GodinaIzdanja > 9999 ||
                izmenjena.Cena < 0 || izmenjena.BrojStrana < 1)
                throw new InvalidOperationException(
                    "Godina, cena ili broj strana nisu ispravni.");

            List<Knjiga> sveKnjige = _knjige.UcitajSve();

            if (sveKnjige.Any(k =>
                k.Id != izmenjena.Id &&
                string.Equals(IsbnPravila.Normalizuj(k.ISBN), isbn,
                    StringComparison.Ordinal)))
                throw new InvalidOperationException(
                    "Knjiga sa ovim ISBN-om vec postoji.");

            if (izmenjena.IzdavacId is int noviIzdavacId &&
                _izdavaci.PronadjiPoId(noviIzdavacId) == null)
                throw new InvalidOperationException(
                    "Novi izdavac ne postoji.");

            // Ako knjiga odlazi od starog izdavaca, njegov sef
            // mora imati barem jos jednu svoju knjigu kod njega.
            if (prethodna.IzdavacId != izmenjena.IzdavacId &&
                prethodna.IzdavacId is int stariIzdavacId)
            {
                Izdavac stariIzdavac =
                    _izdavaci.PronadjiPoId(stariIzdavacId)
                    ?? throw new InvalidOperationException(
                        "Prethodni izdavac ne postoji.");

                if (stariIzdavac.SefAutorId is int sefId)
                {
                    List<AutorKnjiga> veze = _veze.Ucitaj();
                    bool sefPotpisaoOvu = veze.Any(v =>
                        v.AutorId == sefId &&
                        v.KnjigaId == izmenjena.Id);

                    if (sefPotpisaoOvu)
                    {
                        HashSet<int> ostaleKnjige = sveKnjige
                            .Where(k => k.Id != izmenjena.Id &&
                                k.IzdavacId == stariIzdavacId)
                            .Select(k => k.Id)
                            .ToHashSet();

                        bool imaDrugu = veze.Any(v =>
                            v.AutorId == sefId &&
                            ostaleKnjige.Contains(v.KnjigaId));

                        if (!imaDrugu)
                            throw new InvalidOperationException(
                                "Promena izdavaca bi ostavila njegovog " +
                                "sefa bez objavljene knjige.");
                    }
                }
            }

            izmenjena.ISBN = isbn;
            return _knjige.Izmeni(izmenjena);
        }

        // Uklanja vezu autora i knjige bez brisanja ijednog entiteta.
        // Knjiga mora zadrzati barem jednog autora.
        public bool UkloniAutoraSaKnjige(int autorId, int knjigaId)
        {
            if (_autori.PronadjiPoId(autorId) == null)
                throw new InvalidOperationException("Autor ne postoji.");

            Knjiga knjiga = _knjige.PronadjiPoId(knjigaId)
                ?? throw new InvalidOperationException("Knjiga ne postoji.");

            List<AutorKnjiga> veze = _veze.Ucitaj();
            int brojVeza = veze.Count(v =>
                v.AutorId == autorId && v.KnjigaId == knjigaId);

            if (brojVeza == 0)
                return false;

            if (brojVeza > 1)
                throw new InvalidOperationException(
                    "Otkrivene su duplirane veze autor-knjiga.");

            int ostalihAutora = veze
                .Where(v => v.KnjigaId == knjigaId &&
                    v.AutorId != autorId)
                .Select(v => v.AutorId)
                .Distinct()
                .Count();

            // Sekcija 8.25 dozvoljava da polje Autor privremeno bude prazno.
            // Ostaje zabrana ako bi time sef izdavaca ostao bez knjige.

            if (knjiga.IzdavacId is int izdavacId)
            {
                Izdavac izdavac = _izdavaci.PronadjiPoId(izdavacId)
                    ?? throw new InvalidOperationException(
                        "Izdavac ove knjige ne postoji.");

                if (izdavac.SefAutorId == autorId)
                {
                    HashSet<int> drugeKnjige = _knjige.UcitajSve()
                        .Where(k => k.Id != knjigaId &&
                            k.IzdavacId == izdavacId)
                        .Select(k => k.Id)
                        .ToHashSet();

                    bool sefImaDrugu = veze.Any(v =>
                        v.AutorId == autorId &&
                        drugeKnjige.Contains(v.KnjigaId));

                    if (!sefImaDrugu)
                        throw new InvalidOperationException(
                            "Autor je sef izdavaca i ostao bi bez " +
                            "objavljene knjige kod tog izdavaca.");
                }
            }

            veze.RemoveAll(v =>
                v.AutorId == autorId && v.KnjigaId == knjigaId);
            _veze.Sacuvaj(veze);
            return true;
        }
    }
}
