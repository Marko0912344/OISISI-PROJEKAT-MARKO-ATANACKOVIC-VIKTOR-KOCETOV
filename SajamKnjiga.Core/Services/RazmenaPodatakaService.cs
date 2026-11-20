using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services
{
    // Radi sa jednim prenosivim XML fajlom. Ne menja nacin rada
    // postojecih DAO i XmlStorage klasa.
    /// <summary>
    /// Uvoz i izvoz celog sajma kroz SajamPaket uz proveru veza.
    /// </summary>
    public class RazmenaPodatakaService
    {
        private readonly XmlStorage<Adresa> _adrese = new("Adrese.xml");
        private readonly XmlStorage<Autor> _autori = new("Autori.xml");
        private readonly XmlStorage<Knjiga> _knjige = new("Knjige.xml");
        private readonly XmlStorage<Posetilac> _posetioci = new("Posetioci.xml");
        private readonly XmlStorage<Izdavac> _izdavaci = new("Izdavaci.xml");
        private readonly XmlStorage<Kupovina> _kupovine = new("Kupovine.xml");
        private readonly XmlStorage<AutorKnjiga> _veze = new("AutorKnjiga.xml");
        private readonly XmlStorage<StavkaListeZelja> _zelje = new("StavkeListeZelja.xml");
        private readonly XmlStorage<IdBrojac> _brojaci = new("IdBrojaci.xml");

        public string PodrazumevanaPutanja => Path.Combine(
            AppContext.BaseDirectory, "Exports", "SajamKnjiga.xml");

        // Citanje radnih datoteka u jedan objekat.
        public SajamPaket PreuzmiTrenutnePodatke()
        {
            return new SajamPaket
            {
                Adrese = _adrese.Ucitaj(),
                Autori = _autori.Ucitaj(),
                Knjige = _knjige.Ucitaj(),
                Posetioci = _posetioci.Ucitaj(),
                Izdavaci = _izdavaci.Ucitaj(),
                Kupovine = _kupovine.Ucitaj(),
                AutorKnjigaVeze = _veze.Ucitaj(),
                StavkeListeZelja = _zelje.Ucitaj(),
                IdBrojaci = _brojaci.Ucitaj(),
                DatumIzvoza = DateTime.Now,
                Verzija = 1
            };
        }

        // Upisuje jedan kompletan XML. Ne dira radne XML datoteke.
        public string Izvezi(string? putanja = null)
        {
            string odrediste = Path.GetFullPath(
                string.IsNullOrWhiteSpace(putanja)
                    ? PodrazumevanaPutanja : putanja.Trim());

            // Nikada ne dozvoli da zbirni XML prepise jednu od radnih datoteka.
            if (PutanjeRadnihFajlova().Any(f =>
                string.Equals(Path.GetFullPath(f), odrediste,
                    StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "Izvoz ne sme da zameni radnu XML datoteku iz Data foldera.");
            }

            string direktorijum = Path.GetDirectoryName(odrediste)
                ?? throw new InvalidOperationException("Putanja nije ispravna.");
            Directory.CreateDirectory(direktorijum);

            SajamPaket paket = PreuzmiTrenutnePodatke();
            XmlSerializer serializer = new(typeof(SajamPaket));
            string privremena = odrediste + ".tmp";

            try
            {
                using (FileStream tok = new(privremena, FileMode.Create,
                    FileAccess.Write, FileShare.None))
                {
                    serializer.Serialize(tok, paket);
                    tok.Flush(true);
                }

                File.Move(privremena, odrediste, true);
            }
            finally
            {
                if (File.Exists(privremena)) File.Delete(privremena);
            }

            return odrediste;
        }

        // Cita zbirni fajl samo radi pregleda/provere - bez izmene podataka.
        public SajamPaket ProcitajPaket(string putanja)
        {
            if (string.IsNullOrWhiteSpace(putanja))
                throw new ArgumentException("Putanja je obavezna.");

            string punaPutanja = Path.GetFullPath(putanja.Trim().Trim('"'));
            if (!File.Exists(punaPutanja))
                throw new FileNotFoundException("XML datoteka ne postoji.", punaPutanja);

            XmlSerializer serializer = new(typeof(SajamPaket));
            using FileStream tok = File.OpenRead(punaPutanja);
            SajamPaket paket = (SajamPaket?)serializer.Deserialize(tok)
                ?? throw new InvalidDataException("Nije moguce procitati paket.");

            // Ne diramo postojece radne podatke dok paket ne prodje validaciju.
            ProveriPaket(paket);
            return paket;
        }

        // Pre pisanja pravi kopiju svih radnih fajlova. Vraca
        // punu putanju do rezervne kopije.
        public string Uvezi(SajamPaket paket)
        {
            ProveriPaket(paket);
            SrediBrojace(paket);

            string backupDir = Path.Combine(
                AppContext.BaseDirectory,
                "Backups",
                "PreUvoza_" + DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture));

            Directory.CreateDirectory(backupDir);

            // Trajna rezervna kopija ostaje korisniku i posle uvoza.
            // Ne zapocinjemo zamenu ako ova kopija ne uspe.
            string[] fajlovi = PutanjeRadnihFajlova();
            foreach (string fajl in fajlovi.Where(File.Exists))
            {
                File.Copy(fajl, Path.Combine(
                    backupDir, Path.GetFileName(fajl)));
            }

            // Uz rezervnu kopiju, dnevnik transakcije stiti i od
            // prekida programa izmedju upisa u devet XML datoteka.
            XmlTransakcija.Izvrsi(
                new[]
                {
                    "Adrese.xml", "Autori.xml", "Knjige.xml",
                    "Posetioci.xml", "Izdavaci.xml", "Kupovine.xml",
                    "AutorKnjiga.xml", "StavkeListeZelja.xml",
                    "IdBrojaci.xml"
                },
                () =>
                {
                    _adrese.Sacuvaj(paket.Adrese);
                    _autori.Sacuvaj(paket.Autori);
                    _knjige.Sacuvaj(paket.Knjige);
                    _posetioci.Sacuvaj(paket.Posetioci);
                    _izdavaci.Sacuvaj(paket.Izdavaci);
                    _kupovine.Sacuvaj(paket.Kupovine);
                    _veze.Sacuvaj(paket.AutorKnjigaVeze);
                    _zelje.Sacuvaj(paket.StavkeListeZelja);
                    _brojaci.Sacuvaj(paket.IdBrojaci);
                    return true;
                });

            return backupDir;
        }

        // Vrati sve radne putanje koje uvoz ne sme pregaziti pri izvozu.
        private string[] PutanjeRadnihFajlova() => new[]
        {
            _adrese.Putanja, _autori.Putanja, _knjige.Putanja,
            _posetioci.Putanja, _izdavaci.Putanja, _kupovine.Putanja,
            _veze.Putanja, _zelje.Putanja, _brojaci.Putanja
        };

        // Proveri jedinstvenost ID-jeva, ISBN, veze, kupovine, zelje i sefove.
        private static void ProveriPaket(SajamPaket p)
        {
            if (p.Verzija != 1)
                throw new InvalidDataException("Nepodrzana verzija XML paketa.");

            if (p.Adrese == null || p.Autori == null || p.Knjige == null ||
                p.Posetioci == null || p.Izdavaci == null ||
                p.Kupovine == null || p.AutorKnjigaVeze == null ||
                p.StavkeListeZelja == null || p.IdBrojaci == null)
            {
                throw new InvalidDataException("Nedostaje kolekcija u XML paketu.");
            }

            ProveriJedinstveneId(p.Adrese.Select(x => x.Id), "Adrese");
            ProveriJedinstveneId(p.Autori.Select(x => x.Id), "Autori");
            ProveriJedinstveneId(p.Knjige.Select(x => x.Id), "Knjige");
            ProveriJedinstveneId(p.Posetioci.Select(x => x.Id), "Posetioci");
            ProveriJedinstveneId(p.Izdavaci.Select(x => x.Id), "Izdavaci");
            ProveriJedinstveneId(p.Kupovine.Select(x => x.Id), "Kupovine");

            ProveriJedinstveneVrednosti(p.Autori.Select(x => x.BrojLicneKarte), "Broj licne karte");
            // Format i kontrolna cifra ISBN-a, a duplikati i kad imaju crtice/razmake.
            string[] isbns;
            try { isbns = p.Knjige.Select(k => IsbnPravila.Normalizuj(k.ISBN)).ToArray(); }
            catch (InvalidOperationException ex)
            { throw new InvalidDataException("Neispravan ISBN u XML paketu.", ex); }
            ProveriJedinstveneVrednosti(isbns, "ISBN");
            ProveriJedinstveneVrednosti(p.Posetioci.Select(x => x.BrojClanskeKarte), "Clanska karta");
            ProveriJedinstveneVrednosti(p.Izdavaci.Select(x => x.Sifra), "Sifra izdavaca");

            HashSet<int> adrese = p.Adrese.Select(x => x.Id).ToHashSet();
            HashSet<int> autori = p.Autori.Select(x => x.Id).ToHashSet();
            HashSet<int> knjige = p.Knjige.Select(x => x.Id).ToHashSet();
            HashSet<int> posetioci = p.Posetioci.Select(x => x.Id).ToHashSet();
            HashSet<int> izdavaci = p.Izdavaci.Select(x => x.Id).ToHashSet();

            foreach (Autor a in p.Autori)
                if (!adrese.Contains(a.AdresaId))
                    throw new InvalidDataException($"Autor {a.Id} ima nepostojecu adresu.");

            foreach (Posetilac posetilac in p.Posetioci)
                if (!adrese.Contains(posetilac.AdresaId))
                    throw new InvalidDataException($"Posetilac {posetilac.Id} ima nepostojecu adresu.");

            foreach (Knjiga k in p.Knjige)
                if (k.IzdavacId.HasValue && !izdavaci.Contains(k.IzdavacId.Value))
                    throw new InvalidDataException($"Knjiga {k.Id} ima nepostojeceg izdavaca.");

            HashSet<(int, int)> autorKnjigaParovi = new();
            foreach (AutorKnjiga v in p.AutorKnjigaVeze)
            {
                if (!autori.Contains(v.AutorId) || !knjige.Contains(v.KnjigaId))
                    throw new InvalidDataException("AutorKnjiga sadrzi nepostojeci ID.");
                if (!autorKnjigaParovi.Add((v.AutorId, v.KnjigaId)))
                    throw new InvalidDataException("Duplirana veza AutorKnjiga.");
            }

            HashSet<(int, int)> kupovine = new();
            foreach (Kupovina k in p.Kupovine)
            {
                if (!posetioci.Contains(k.PosetilacId) || !knjige.Contains(k.KnjigaId))
                    throw new InvalidDataException("Kupovina ima nepostojeci ID.");
                if (k.Ocena < 1 || k.Ocena > 5)
                    throw new InvalidDataException("Ocena kupovine mora biti od 1 do 5.");
                if (k.CenaPriKupovini is decimal cena && cena < 0)
                    throw new InvalidDataException("Istorijska cena kupovine ne moze biti negativna.");
                if (!kupovine.Add((k.PosetilacId, k.KnjigaId)))
                    throw new InvalidDataException("Duplirana kupovina iste knjige.");
            }

            HashSet<(int, int)> zelje = new();
            foreach (StavkaListeZelja z in p.StavkeListeZelja)
            {
                if (!posetioci.Contains(z.PosetilacId) || !knjige.Contains(z.KnjigaId))
                    throw new InvalidDataException("Lista zelja ima nepostojeci ID.");
                var par = (z.PosetilacId, z.KnjigaId);
                if (!zelje.Add(par))
                    throw new InvalidDataException("Duplirana stavka liste zelja.");
                if (kupovine.Contains(par))
                    throw new InvalidDataException("Knjiga je istovremeno kupljena i u zeljama.");
            }

            foreach (Izdavac i in p.Izdavaci)
            {
                if (!i.SefAutorId.HasValue) continue;
                int sefId = i.SefAutorId.Value;
                Autor? sef = p.Autori.FirstOrDefault(a => a.Id == sefId);
                if (sef == null || sef.GodineIskustva < 5)
                    throw new InvalidDataException($"Sef izdavaca {i.Id} nije validan.");
                bool objavio = p.Knjige.Any(k => k.IzdavacId == i.Id &&
                    autorKnjigaParovi.Contains((sefId, k.Id)));
                if (!objavio)
                    throw new InvalidDataException($"Sef izdavaca {i.Id} nema knjigu kod njega.");
            }

            if (p.IdBrojaci.Any(x => x == null || string.IsNullOrWhiteSpace(x.TipEntiteta) || x.SledeciId <= 0) ||
                p.IdBrojaci.GroupBy(x => x.TipEntiteta, StringComparer.Ordinal).Any(g => g.Count() > 1))
                throw new InvalidDataException("Neispravni ili duplirani ID brojaci.");
        }

        // Svaki ID mora biti pozitivan i jedinstven unutar kolekcije.
        private static void ProveriJedinstveneId(IEnumerable<int> ids, string naziv)
        {
            int[] niz = ids.ToArray();
            if (niz.Any(id => id <= 0) || niz.Distinct().Count() != niz.Length)
                throw new InvalidDataException($"Neispravni ili duplirani ID-jevi: {naziv}.");
        }

        // Zahtevaj neprazne i jedinstvene poslovne kljuceve.
        private static void ProveriJedinstveneVrednosti(IEnumerable<string> vrednosti, string polje)
        {
            string[] niz = vrednosti.Select(x => (x ?? "").Trim()).ToArray();
            if (niz.Any(string.IsNullOrWhiteSpace) ||
                niz.Distinct(StringComparer.OrdinalIgnoreCase).Count() != niz.Length)
                throw new InvalidDataException($"Prazne ili duplirane vrednosti: {polje}.");
        }

        // Posle uvoza uskladi brojac svakog tipa sa najvecim ID-jem.
        private static void SrediBrojace(SajamPaket p)
        {
            OsigurajBrojac(p, "Adresa", p.Adrese.Select(x => x.Id));
            OsigurajBrojac(p, "Autor", p.Autori.Select(x => x.Id));
            OsigurajBrojac(p, "Knjiga", p.Knjige.Select(x => x.Id));
            OsigurajBrojac(p, "Posetilac", p.Posetioci.Select(x => x.Id));
            OsigurajBrojac(p, "Izdavac", p.Izdavaci.Select(x => x.Id));
            OsigurajBrojac(p, "Kupovina", p.Kupovine.Select(x => x.Id));
        }

        // SledeciId mora biti veci od svih uvezenih ID-jeva.
        private static void OsigurajBrojac(SajamPaket p, string tip, IEnumerable<int> ids)
        {
            int maxId = ids.DefaultIfEmpty(0).Max();
            int minimum = checked(maxId + 1);
            IdBrojac? b = p.IdBrojaci.FirstOrDefault(x => x.TipEntiteta == tip);
            if (b == null)
            {
                p.IdBrojaci.Add(new IdBrojac { TipEntiteta = tip, SledeciId = minimum });
            }
            else if (b.SledeciId < minimum)
            {
                b.SledeciId = minimum;
            }
        }
    }
}
