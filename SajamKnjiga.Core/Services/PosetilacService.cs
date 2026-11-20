using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services
{
    /// <summary>
    /// Upravljanje zeljama, kupovinama i prosecnim ocenama posetilaca.
    /// </summary>
    public class PosetilacService
    {
        private readonly XmlDao<Posetilac> _posetiociDao;
        private readonly XmlDao<Knjiga> _knjigeDao;
        private readonly XmlDao<Kupovina> _kupovineDao;
        private readonly XmlDao<Adresa> _adreseDao;
        private readonly XmlStorage<StavkaListeZelja> _zeljeStorage;

        public PosetilacService()
        {
            _posetiociDao = new XmlDao<Posetilac>("Posetioci.xml");
            _knjigeDao = new XmlDao<Knjiga>("Knjige.xml");
            _kupovineDao = new XmlDao<Kupovina>("Kupovine.xml");
            _adreseDao = new XmlDao<Adresa>("Adrese.xml");
            _zeljeStorage = new XmlStorage<StavkaListeZelja>(
                "StavkeListeZelja.xml");
        }

        // Proveri postojanje i posetioca i knjige pre operacije.
        private void ProveriPostojanje(int posetilacId, int knjigaId)
        {
            if (_posetiociDao.PronadjiPoId(posetilacId) == null)
            {
                throw new InvalidOperationException(
                    "Posetilac ne postoji.");
            }

            if (_knjigeDao.PronadjiPoId(knjigaId) == null)
            {
                throw new InvalidOperationException(
                    "Knjiga ne postoji.");
            }
        }

        // Ove operacije menjaju samo JEDAN XML fajl, pa je
        // dovoljan bezbedan pojedinacni upis kroz XmlStorage<T>.
        public bool DodajNaListuZelja(int posetilacId, int knjigaId)
        {
            ProveriPostojanje(posetilacId, knjigaId);

            List<StavkaListeZelja> zelje = _zeljeStorage.Ucitaj();

            bool vecNaListi = zelje.Any(z =>
                z.PosetilacId == posetilacId && z.KnjigaId == knjigaId);

            bool vecKupljena = _kupovineDao.UcitajSve().Any(k =>
                k.PosetilacId == posetilacId && k.KnjigaId == knjigaId);

            if (vecNaListi || vecKupljena) return false;

            zelje.Add(new StavkaListeZelja
            {
                PosetilacId = posetilacId,
                KnjigaId = knjigaId
            });

            _zeljeStorage.Sacuvaj(zelje);
            return true;
        }

        /// <summary>
        /// Ukloni samo trazenu vezu posetilac-knjiga.
        /// </summary>
        public bool UkloniSaListeZelja(int posetilacId, int knjigaId)
        {
            ProveriPostojanje(posetilacId, knjigaId);

            List<StavkaListeZelja> zelje = _zeljeStorage.Ucitaj();
            int uklonjeno = zelje.RemoveAll(z =>
                z.PosetilacId == posetilacId && z.KnjigaId == knjigaId);

            if (uklonjeno == 0) return false;

            _zeljeStorage.Sacuvaj(zelje);
            return true;
        }

        /// <summary>
        /// Prikazi knjige koje nisu ni kupljene ni na listi zelja.
        /// </summary>
        public List<Knjiga> DostupneKnjigeZaListuZelja(int posetilacId)
        {
            if (_posetiociDao.PronadjiPoId(posetilacId) == null)
            {
                throw new InvalidOperationException("Posetilac ne postoji.");
            }

            // HashSet obezbedjuje brzu proveru i automatski uklanja duplikate ID-jeva.
            HashSet<int> zauzeteKnjige = _zeljeStorage.Ucitaj()
                .Where(z => z.PosetilacId == posetilacId)
                .Select(z => z.KnjigaId)
                .ToHashSet();

            foreach (Kupovina kupovina in _kupovineDao.UcitajSve())
            {
                if (kupovina.PosetilacId == posetilacId)
                {
                    zauzeteKnjige.Add(kupovina.KnjigaId);
                }
            }

            return _knjigeDao.UcitajSve()
                .Where(k => !zauzeteKnjige.Contains(k.Id))
                .ToList();
        }

        // Ova operacija menja TRI XML datoteke:
        // brojac ID-ja, kupovine i listu zelja.
        public Kupovina EvidentirajKupovinu(
            int posetilacId,
            int knjigaId,
            DateTime datumKupovine,
            int ocena,
            string komentar)
        {
            if (ocena < 1 || ocena > 5)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ocena), "Ocena mora biti od 1 do 5.");
            }

            if (datumKupovine == default || datumKupovine.Date > DateTime.Today)
            {
                throw new ArgumentException(
                    "Datum kupovine nije ispravan.", nameof(datumKupovine));
            }

            return XmlTransakcija.Izvrsi(
                new[]
                {
                    "IdBrojaci.xml",
                    "Kupovine.xml",
                    "StavkeListeZelja.xml",
                    "Posetioci.xml"
                },
                () =>
                {
                    ProveriPostojanje(posetilacId, knjigaId);

                    bool vecKupljena = _kupovineDao.UcitajSve().Any(k =>
                        k.PosetilacId == posetilacId &&
                        k.KnjigaId == knjigaId);

                    if (vecKupljena)
                    {
                        throw new InvalidOperationException(
                            "Posetilac je vec kupio ovu knjigu.");
                    }

                    List<StavkaListeZelja> zelje = _zeljeStorage.Ucitaj();
                    List<StavkaListeZelja> odgovarajuce = zelje
                        .Where(z => z.PosetilacId == posetilacId &&
                            z.KnjigaId == knjigaId)
                        .ToList();

                    if (odgovarajuce.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Knjiga nije na listi zelja posetioca.");
                    }

                    if (odgovarajuce.Count != 1)
                    {
                        throw new InvalidOperationException(
                            "Lista zelja sadrzi duplirane veze.");
                    }

                    Kupovina kupovina = new Kupovina(
                        posetilacId, knjigaId, datumKupovine,
                        ocena, komentar ?? string.Empty);

                    // Pamti cenu u trenutku kupovine; kasnija promena cene
                    // ne sme menjati istorijsku ukupnu potrosnju.
                    kupovina.CenaPriKupovini = _knjigeDao.PronadjiPoId(knjigaId)!.Cena;
                    _kupovineDao.Dodaj(kupovina);

                    zelje.Remove(odgovarajuce[0]);
                    _zeljeStorage.Sacuvaj(zelje);
                    OsveziProsecnuOcenu(posetilacId);

                    return kupovina;
                });
        }

        // Ponistavanje menja DVE XML datoteke.
        public bool PonistiKupovinu(int posetilacId, int knjigaId)
        {
            return XmlTransakcija.Izvrsi(
                new[] { "Kupovine.xml", "StavkeListeZelja.xml", "Posetioci.xml" },
                () =>
                {
                    ProveriPostojanje(posetilacId, knjigaId);

                    List<Kupovina> pronadjene = _kupovineDao.UcitajSve()
                        .Where(k => k.PosetilacId == posetilacId &&
                            k.KnjigaId == knjigaId)
                        .ToList();

                    if (pronadjene.Count == 0) return false;

                    if (pronadjene.Count != 1)
                    {
                        throw new InvalidOperationException(
                            "Pronadjene su duplirane kupovine.");
                    }

                    List<StavkaListeZelja> zelje = _zeljeStorage.Ucitaj();

                    if (zelje.Any(z => z.PosetilacId == posetilacId &&
                        z.KnjigaId == knjigaId))
                    {
                        throw new InvalidOperationException(
                            "Knjiga je vec na listi zelja. " +
                            "Podaci nisu konzistentni.");
                    }

                    zelje.Add(new StavkaListeZelja
                    {
                        PosetilacId = posetilacId,
                        KnjigaId = knjigaId
                    });

                    _zeljeStorage.Sacuvaj(zelje);

                    if (!_kupovineDao.Obrisi(pronadjene[0].Id))
                    {
                        throw new InvalidOperationException(
                            "Kupovina nije mogla biti uklonjena.");
                    }
                    OsveziProsecnuOcenu(posetilacId);

                    return true;
                });
        }

        // Izracunaj aritmeticku sredinu ocena svih kupovina posetioca.
        private void OsveziProsecnuOcenu(int posetilacId)
        {
            Posetilac p = _posetiociDao.PronadjiPoId(posetilacId)
                ?? throw new InvalidOperationException("Posetilac ne postoji.");
            var kupovine = _kupovineDao.UcitajSve()
                .Where(k => k.PosetilacId == posetilacId).ToList();
            p.ProsecnaOcenaRecenzija = kupovine.Count == 0
                ? 0 : kupovine.Average(k => k.Ocena);
            _posetiociDao.Izmeni(p);
        }

        // Rekonstrukcija objekata nakon citanja XML-a ostaje nepromenjena.
        public (
            List<Posetilac> Posetioci,
            List<Knjiga> Knjige,
            List<Kupovina> Kupovine
        ) UcitajPovezanePodatke()
        {
            List<Posetilac> posetioci = _posetiociDao.UcitajSve();
            List<Knjiga> knjige = _knjigeDao.UcitajSve();
            List<Kupovina> kupovine = _kupovineDao.UcitajSve();
            List<StavkaListeZelja> zelje = _zeljeStorage.Ucitaj();

            var posetiociPoId = posetioci.ToDictionary(p => p.Id);
            var knjigePoId = knjige.ToDictionary(k => k.Id);
            var adresePoId = _adreseDao.UcitajSve()
                .ToDictionary(a => a.Id);

            foreach (Posetilac posetilac in posetioci)
            {
                if (!adresePoId.TryGetValue(
                    posetilac.AdresaId, out Adresa? adresa))
                {
                    throw new InvalidOperationException(
                        $"Adresa posetioca {posetilac.Id} ne postoji.");
                }

                posetilac.Adresa = adresa;
            }

            HashSet<(int, int)> paroviKupovina = new();

            foreach (Kupovina kupovina in kupovine)
            {
                var par = (kupovina.PosetilacId, kupovina.KnjigaId);

                if (!paroviKupovina.Add(par))
                {
                    throw new InvalidOperationException(
                        "Postoje duplirane kupovine.");
                }

                if (!posetiociPoId.TryGetValue(
                    kupovina.PosetilacId, out Posetilac? posetilac))
                {
                    throw new InvalidOperationException(
                        "Kupovina upucuje na nepostojeceg posetioca.");
                }

                if (!knjigePoId.TryGetValue(
                    kupovina.KnjigaId, out Knjiga? knjiga))
                {
                    throw new InvalidOperationException(
                        "Kupovina upucuje na nepostojecu knjigu.");
                }

                kupovina.Posetilac = posetilac;
                kupovina.Knjiga = knjiga;
                posetilac.Kupovine.Add(kupovina);
                knjiga.PosetiociKojiSuKupili.Add(posetilac);
            }

            HashSet<(int, int)> paroviZelja = new();

            foreach (StavkaListeZelja zelja in zelje)
            {
                var par = (zelja.PosetilacId, zelja.KnjigaId);

                if (!paroviZelja.Add(par))
                {
                    throw new InvalidOperationException(
                        "Postoje duplirane stavke liste zelja.");
                }

                if (paroviKupovina.Contains(par))
                {
                    throw new InvalidOperationException(
                        "Knjiga je istovremeno kupljena " +
                        "i na listi zelja istog posetioca.");
                }

                if (!posetiociPoId.TryGetValue(
                    zelja.PosetilacId, out Posetilac? posetilac))
                {
                    throw new InvalidOperationException(
                        "Lista zelja upucuje na nepostojeceg posetioca.");
                }

                if (!knjigePoId.TryGetValue(
                    zelja.KnjigaId, out Knjiga? knjiga))
                {
                    throw new InvalidOperationException(
                        "Lista zelja upucuje na nepostojecu knjigu.");
                }

                posetilac.ListaZelja.Add(knjiga);
                knjiga.PosetiociNaListiZelja.Add(posetilac);
            }

            return (posetioci, knjige, kupovine);
        }
    }
}
