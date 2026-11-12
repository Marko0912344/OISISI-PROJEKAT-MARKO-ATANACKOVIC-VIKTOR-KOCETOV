using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SajamKnjiga.Core.Persistence
{
    /// <summary>
    /// Dnevnik promena za operacije koje menjaju vise radnih XML datoteka.
    /// Pre pocetka pravi trajne kopije, a neuspesnu ili prekinutu operaciju
    /// vraca na staro stanje. Namenjeno jednoj pokrenutoj instanci aplikacije.
    /// Nije zamena za pravu bazu podataka niti izolaciju konkurentnih upisa.
    /// </summary>
    public static class XmlTransakcija
    {
        private static readonly string DataFolder = Path.Combine(
            AppContext.BaseDirectory, "Data");

        private static readonly string TransakcijeFolder = Path.Combine(
            AppContext.BaseDirectory, "Transakcije");

        // Dopustamo samo poznate radne datoteke. Tako dnevnik ne moze
        // greskom da napravi kopiju proizvoljnog fajla sa diska.
        private static readonly HashSet<string> DozvoljeniFajlovi =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Adrese.xml", "Autori.xml", "Knjige.xml",
                "Posetioci.xml", "Izdavaci.xml", "Kupovine.xml",
                "AutorKnjiga.xml", "StavkeListeZelja.xml",
                "IdBrojaci.xml"
            };

        private const string Manifest = "manifest.txt";
        private const string Spremno = "spremno.ok";
        private const string Potvrdjeno = "potvrdjeno.ok";
        private const string Vraceno = "vraceno.ok";

        /// <summary>
        /// Pozvati na samom pocetku Program.cs, PRE kreiranja menija.
        /// Vraca broj pronadjenih i obradjenih dnevnika transakcija.
        /// </summary>
        public static int OporaviPrekinute()
        {
            using FileStream zakljucavanje = Zakljucaj();
            return OporaviPodZakljucavanjem();
        }

        /// <summary>
        /// Izvrsava poslovnu operaciju uz mogucnost povratka svih navedenih
        /// radnih datoteka ako dodje do izuzetka ili prekida aplikacije.
        /// Sve radne XML datoteke koje operacija MENJA moraju biti navedene.
        /// </summary>
        public static T Izvrsi<T>(
            IEnumerable<string> naziviFajlova,
            Func<T> operacija)
        {
            ArgumentNullException.ThrowIfNull(naziviFajlova);
            ArgumentNullException.ThrowIfNull(operacija);

            string[] fajlovi = naziviFajlova
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (fajlovi.Length == 0 || fajlovi.Any(f =>
                string.IsNullOrWhiteSpace(f) || !DozvoljeniFajlovi.Contains(f)))
            {
                throw new ArgumentException(
                    "Transakcija mora navesti poznate radne XML datoteke.",
                    nameof(naziviFajlova));
            }

            using FileStream zakljucavanje = Zakljucaj();

            // Ako je prethodna operacija prekinuta, prvo je oporavljamo.
            OporaviPodZakljucavanjem();

            string dnevnik = Path.Combine(
                TransakcijeFolder,
                "TX_" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") +
                "_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(dnevnik);

            // 'spremno.ok' se kreira TEK kada je svaka kopija zavrsena.
            // Pre tog trenutka operacija ne sme da menja nijedan radni fajl.
            try
            {
                List<string> manifest = new List<string>();

                foreach (string naziv in fajlovi)
                {
                    string original = Path.Combine(DataFolder, naziv);
                    bool postoji = File.Exists(original);

                    if (postoji)
                    {
                        KopirajTrajno(original, Path.Combine(dnevnik, naziv));
                    }

                    manifest.Add(naziv + "|" + (postoji ? "1" : "0"));
                }

                ZapisiTrajno(Path.Combine(dnevnik, Manifest),
                    string.Join(Environment.NewLine, manifest));
                ZapisiTrajno(Path.Combine(dnevnik, Spremno), "spremno");
            }
            catch
            {
                // Poslovna operacija jos nije pocela: originali su netaknuti.
                PokusajZavrsetakDnevnika(dnevnik);
                throw;
            }

            T rezultat;
            try
            {
                rezultat = operacija();

                // Potvrda tek POSLE svih radnih upisa.
                ZapisiTrajno(Path.Combine(dnevnik, Potvrdjeno), "potvrdjeno");
            }
            catch (Exception uzrok)
            {
                try
                {
                    VratiIzDnevnika(dnevnik);
                    ZapisiTrajno(Path.Combine(dnevnik, Vraceno), "vraceno");
                    PokusajZavrsetakDnevnika(dnevnik);
                }
                catch (Exception greskaVracanja)
                {
                    // Dnevnik cuvamo za sledece pokretanje / rucni oporavak.
                    throw new IOException(
                        "Operacija nije uspela, a vracanje XML datoteka " +
                        "nije zavrseno. Ne nastavljaj rad pre oporavka iz: " + dnevnik,
                        new AggregateException(uzrok, greskaVracanja));
                }

                throw;
            }

            // Ciscenje nije deo poslovne operacije. Eventualna greska pri
            // ciscenju NE SME da pokrene rollback vec potvrdjene transakcije.
            PokusajZavrsetakDnevnika(dnevnik);
            return rezultat;
        }

        private static FileStream Zakljucaj()
        {
            Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory(TransakcijeFolder);

            try
            {
                return new FileStream(
                    Path.Combine(TransakcijeFolder, ".lock"),
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
            }
            catch (IOException ex)
            {
                throw new IOException(
                    "Dnevnik transakcija je zauzet. Proveri da li je " +
                    "program vec pokrenut u drugom prozoru.", ex);
            }
        }

        private static int OporaviPodZakljucavanjem()
        {
            int broj = 0;

            // 'DONE_' direktorijumi su vec bezbedno oznaceni kao zavrseni.
            foreach (string zavrsen in Directory.GetDirectories(
                TransakcijeFolder, "DONE_*"))
            {
                PokusajBrisanjeZavrsenog(zavrsen);
            }

            foreach (string dnevnik in Directory.GetDirectories(
                TransakcijeFolder, "TX_*"))
            {
                bool spremno = File.Exists(Path.Combine(dnevnik, Spremno));
                bool potvrdjeno = File.Exists(Path.Combine(dnevnik, Potvrdjeno));
                bool vraceno = File.Exists(Path.Combine(dnevnik, Vraceno));

                // Bez markera 'spremno' rad sa XML fajlovima nije zapoceo.
                // Sa 'potvrdjeno' svi radni upisi su vec zavrseni.
                if (spremno && !potvrdjeno && !vraceno)
                {
                    VratiIzDnevnika(dnevnik);
                    ZapisiTrajno(Path.Combine(dnevnik, Vraceno), "vraceno");
                }

                PokusajZavrsetakDnevnika(dnevnik);
                broj++;
            }

            return broj;
        }

        private static void VratiIzDnevnika(string dnevnik)
        {
            string putanjaManifesta = Path.Combine(dnevnik, Manifest);
            if (!File.Exists(putanjaManifesta))
            {
                throw new InvalidDataException(
                    "Nedostaje manifest transakcije: " + dnevnik);
            }

            foreach (string red in File.ReadAllLines(putanjaManifesta))
            {
                if (string.IsNullOrWhiteSpace(red)) continue;

                string[] delovi = red.Split('|');
                if (delovi.Length != 2 ||
                    !DozvoljeniFajlovi.Contains(delovi[0]) ||
                    (delovi[1] != "0" && delovi[1] != "1"))
                {
                    throw new InvalidDataException(
                        "Neispravan manifest transakcije: " + dnevnik);
                }

                string original = Path.Combine(DataFolder, delovi[0]);
                string kopija = Path.Combine(dnevnik, delovi[0]);

                if (delovi[1] == "1")
                {
                    if (!File.Exists(kopija))
                    {
                        throw new FileNotFoundException(
                            "Nedostaje rezervna kopija transakcije.", kopija);
                    }

                    // Restauracija preko privremene datoteke na istom disku.
                    string privremena = original + "." +
                        Guid.NewGuid().ToString("N") + ".rollback.tmp";

                    try
                    {
                        KopirajTrajno(kopija, privremena);
                        if (File.Exists(original))
                        {
                            File.Replace(privremena, original, null,
                                ignoreMetadataErrors: true);
                        }
                        else
                        {
                            File.Move(privremena, original);
                        }
                    }
                    finally
                    {
                        if (File.Exists(privremena)) File.Delete(privremena);
                    }
                }
                else if (File.Exists(original))
                {
                    // Fajl nije postojao pre transakcije.
                    File.Delete(original);
                }
            }
        }

        private static void KopirajTrajno(string izvor, string odrediste)
        {
            using FileStream ulaz = new FileStream(
                izvor, FileMode.Open, FileAccess.Read, FileShare.Read);
            using FileStream izlaz = new FileStream(
                odrediste, FileMode.CreateNew, FileAccess.Write, FileShare.None);

            ulaz.CopyTo(izlaz);
            izlaz.Flush(flushToDisk: true);
        }

        private static void ZapisiTrajno(string putanja, string sadrzaj)
        {
            // Atomskim preimenovanjem markera izbegavamo da nedovrsen
            // upis bude protumacen kao uspesna potvrda transakcije.
            string privremena = putanja + "." +
                Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sadrzaj);

                using (FileStream izlaz = new FileStream(
                    privremena, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None))
                {
                    izlaz.Write(bytes, 0, bytes.Length);
                    izlaz.Flush(flushToDisk: true);
                }

                File.Move(privremena, putanja);
            }
            finally
            {
                if (File.Exists(privremena)) File.Delete(privremena);
            }
        }

        private static void PokusajZavrsetakDnevnika(string dnevnik)
        {
            // Prvo PREIMENUJEMO direktorijum, pa ga tek onda brisemo.
            // Ako nestane struja usred brisanja, oporavak vise nece
            // posmatrati ovaj direktorijum kao aktivnu transakciju.
            string odrediste = Path.Combine(
                TransakcijeFolder, "DONE_" + Path.GetFileName(dnevnik));

            try
            {
                if (Directory.Exists(dnevnik))
                {
                    Directory.Move(dnevnik, odrediste);
                }
            }
            catch (IOException)
            {
                // Dnevnik ostaje sa markerom 'potvrdjeno' ili 'vraceno'.
                // Sledeci start ce ponovo pokusati ciscenje.
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            PokusajBrisanjeZavrsenog(odrediste);
        }

        private static void PokusajBrisanjeZavrsenog(string direktorijum)
        {
            try
            {
                if (Directory.Exists(direktorijum))
                {
                    Directory.Delete(direktorijum, recursive: true);
                }
            }
            catch (IOException)
            {
                // Dnevnik je vec oznacen kao dovrsen (DONE_).
            }
            catch (UnauthorizedAccessException)
            {
                // Dnevnik je vec oznacen kao dovrsen (DONE_).
            }
        }
    }
}
