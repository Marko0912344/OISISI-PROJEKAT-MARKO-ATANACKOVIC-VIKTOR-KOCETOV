using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services
{
    /// <summary>
    /// Provera zavisnosti i bezbedno brisanje povezanih entiteta.
    /// </summary>
    public class KontrolisanoBrisanjeService
    {
        private readonly XmlDao<Adresa> _adreseDao;
        private readonly XmlDao<Posetilac> _posetiociDao;
        private readonly XmlDao<Autor> _autoriDao;
        private readonly XmlDao<Knjiga> _knjigeDao;
        private readonly XmlDao<Kupovina> _kupovineDao;
        private readonly XmlDao<Izdavac> _izdavaciDao;

        private readonly XmlStorage<StavkaListeZelja> _zeljeStorage;
        private readonly XmlStorage<AutorKnjiga> _autorKnjigaStorage;

        public KontrolisanoBrisanjeService()
        {
            _adreseDao = new XmlDao<Adresa>("Adrese.xml");
            _posetiociDao = new XmlDao<Posetilac>("Posetioci.xml");
            _autoriDao = new XmlDao<Autor>("Autori.xml");
            _knjigeDao = new XmlDao<Knjiga>("Knjige.xml");
            _kupovineDao = new XmlDao<Kupovina>("Kupovine.xml");
            _izdavaciDao = new XmlDao<Izdavac>("Izdavaci.xml");
            _zeljeStorage = new XmlStorage<StavkaListeZelja>(
                "StavkeListeZelja.xml");
            _autorKnjigaStorage = new XmlStorage<AutorKnjiga>(
                "AutorKnjiga.xml");
        }

        /// <summary>
        /// Proveri kupovine i zelje povezane sa posetiocem.
        /// </summary>
        public ProveraBrisanja ProveriBrisanjePosetioca(int id)
        {
            ProveraBrisanja rezultat = new();

            if (_posetiociDao.PronadjiPoId(id) == null)
            {
                rezultat.Razlozi.Add("Posetilac ne postoji.");
                return rezultat;
            }

            if (_zeljeStorage.Ucitaj().Any(z => z.PosetilacId == id))
            {
                rezultat.Razlozi.Add(
                    "Posetilac ima knjige na listi zelja.");
            }

            if (_kupovineDao.UcitajSve().Any(k => k.PosetilacId == id))
            {
                rezultat.Razlozi.Add(
                    "Posetilac ima evidentirane kupovine.");
            }

            return rezultat;
        }

        /// <summary>
        /// Proveri povezane knjige i ulogu sefa izdavaca.
        /// </summary>
        public ProveraBrisanja ProveriBrisanjeAutora(int id)
        {
            ProveraBrisanja rezultat = new();

            if (_autoriDao.PronadjiPoId(id) == null)
            {
                rezultat.Razlozi.Add("Autor ne postoji.");
                return rezultat;
            }

            if (_autorKnjigaStorage.Ucitaj().Any(v => v.AutorId == id))
            {
                rezultat.Razlozi.Add(
                    "Autor je povezan sa jednom ili vise knjiga.");
            }

            if (_izdavaciDao.UcitajSve().Any(i => i.SefAutorId == id))
            {
                rezultat.Razlozi.Add("Autor je sef izdavaca.");
            }

            return rezultat;
        }

        /// <summary>
        /// Proveri kupovine, zelje i poslednju knjigu sefa izdavaca.
        /// </summary>
        public ProveraBrisanja ProveriBrisanjeKnjige(int id)
        {
            ProveraBrisanja rezultat = new();
            Knjiga? knjiga = _knjigeDao.PronadjiPoId(id);

            if (knjiga == null)
            {
                rezultat.Razlozi.Add("Knjiga ne postoji.");
                return rezultat;
            }

            if (_zeljeStorage.Ucitaj().Any(z => z.KnjigaId == id))
            {
                rezultat.Razlozi.Add(
                    "Knjiga se nalazi na listi zelja posetioca.");
            }

            if (_kupovineDao.UcitajSve().Any(k => k.KnjigaId == id))
            {
                rezultat.Razlozi.Add(
                    "Knjiga ima evidentirane kupovine.");
            }

            if (knjiga.IzdavacId.HasValue)
            {
                int izdavacId = knjiga.IzdavacId.Value;
                Izdavac? izdavac = _izdavaciDao.PronadjiPoId(izdavacId);

                if (izdavac == null)
                {
                    rezultat.Razlozi.Add(
                        "Knjiga upucuje na nepostojeceg izdavaca.");
                }
                else if (izdavac.SefAutorId is int sefId)
                {
                    List<AutorKnjiga> veze = _autorKnjigaStorage.Ucitaj();

                    bool sefJeAutorOveKnjige = veze.Any(v =>
                        v.AutorId == sefId && v.KnjigaId == id);

                    if (sefJeAutorOveKnjige)
                    {
                        HashSet<int> drugeKnjigeIzdavaca =
                            _knjigeDao.UcitajSve()
                            .Where(k => k.Id != id &&
                                k.IzdavacId == izdavacId)
                            .Select(k => k.Id)
                            .ToHashSet();

                        bool sefImaDruguKnjigu = veze.Any(v =>
                            v.AutorId == sefId &&
                            drugeKnjigeIzdavaca.Contains(v.KnjigaId));

                        if (!sefImaDruguKnjigu)
                        {
                            rezultat.Razlozi.Add(
                                "Brisanje bi ostavilo sefa " +
                                "izdavaca bez objavljene knjige.");
                        }
                    }
                }
            }

            return rezultat;
        }

        // Brisanje izdavaca nije dozvoljeno dok ima povezane knjige.
        public ProveraBrisanja ProveriBrisanjeIzdavaca(int id)
        {
            ProveraBrisanja rezultat = new();
            if (_izdavaciDao.PronadjiPoId(id) == null)
            {
                rezultat.Razlozi.Add("Izdavac ne postoji.");
                return rezultat;
            }

            if (_knjigeDao.UcitajSve().Any(k => k.IzdavacId == id))
                rezultat.Razlozi.Add(
                    "Izdavac ima povezane knjige. Brisanje nije dozvoljeno.");

            return rezultat;
        }

        // Adresa moze biti deljena izmedju vise autora i posetilaca.
        public ProveraBrisanja ProveriBrisanjeAdrese(int id)
        {
            ProveraBrisanja rezultat = new();
            if (_adreseDao.PronadjiPoId(id) == null)
            {
                rezultat.Razlozi.Add("Adresa ne postoji.");
                return rezultat;
            }

            if (_autoriDao.UcitajSve().Any(a => a.AdresaId == id))
                rezultat.Razlozi.Add("Adresu koristi najmanje jedan autor.");

            if (_posetiociDao.UcitajSve().Any(p => p.AdresaId == id))
                rezultat.Razlozi.Add("Adresu koristi najmanje jedan posetilac.");

            return rezultat;
        }

        /// <summary>
        /// Obrisi izdavaca samo ako je provera brisanja dozvolila.
        /// </summary>
        public bool ObrisiIzdavaca(int id)
        {
            ZahtevajDozvolu(ProveriBrisanjeIzdavaca(id));
            return _izdavaciDao.Obrisi(id);
        }

        /// <summary>
        /// Obrisi adresu samo ako je niko ne koristi.
        /// </summary>
        public bool ObrisiAdresu(int id)
        {
            ZahtevajDozvolu(ProveriBrisanjeAdrese(id));
            return _adreseDao.Obrisi(id);
        }

        // Prekini operaciju i prikazi razloge neuspele provere.
        private static void ZahtevajDozvolu(ProveraBrisanja provera)
        {
            if (!provera.Dozvoljeno)
            {
                throw new InvalidOperationException(
                    string.Join(Environment.NewLine, provera.Razlozi));
            }
        }

        /// <summary>
        /// Bezbedno obrisi posetioca posle provere referenci.
        /// </summary>
        public bool ObrisiPosetioca(int id)
        {
            ZahtevajDozvolu(ProveriBrisanjePosetioca(id));
            return _posetiociDao.Obrisi(id);
        }

        /// <summary>
        /// Bezbedno obrisi autora posle provere referenci.
        /// </summary>
        public bool ObrisiAutora(int id)
        {
            ZahtevajDozvolu(ProveriBrisanjeAutora(id));
            return _autoriDao.Obrisi(id);
        }

        // Brisanje knjige menja DVA XML fajla:
        // veze autora i knjiga, zatim spisak knjiga.
        public bool ObrisiKnjigu(int id)
        {
            return XmlTransakcija.Izvrsi(
                new[] { "AutorKnjiga.xml", "Knjige.xml" },
                () =>
                {
                    ZahtevajDozvolu(ProveriBrisanjeKnjige(id));

                    List<AutorKnjiga> veze = _autorKnjigaStorage.Ucitaj();
                    int uklonjeno = veze.RemoveAll(v => v.KnjigaId == id);

                    if (uklonjeno > 0)
                    {
                        _autorKnjigaStorage.Sacuvaj(veze);
                    }

                    if (!_knjigeDao.Obrisi(id))
                    {
                        throw new InvalidOperationException(
                            "Knjiga nije mogla biti obrisana.");
                    }

                    return true;
                });
        }
    }
}
