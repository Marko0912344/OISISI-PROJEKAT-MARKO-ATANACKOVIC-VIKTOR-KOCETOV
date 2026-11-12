using System.Globalization;
using System.Xml.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services;

/// <summary>
/// Student 2: #upis_kupovine. Upis bez dupliranja klase Kupovina koju
/// implementira Student 1. XML oblik koristi elemente iz PDF/demo seme:
/// Id, PosetilacId, KnjigaId, DatumKupovine, Ocena i Komentar.
/// Nakon spajanja usaglasiti sa tipizovanim modelom Studenta 1.
/// </summary>
public sealed class Student2KupovineService
{
    private readonly XmlDao<Knjiga> _knjige = new("Knjige.xml");
    private readonly XmlStorage<StavkaListeZelja> _zelje = new("StavkeListeZelja.xml");
    private readonly XmlStorage<IdBrojac> _brojaci = new("IdBrojaci.xml");

    public int Evidentiraj(int posetilacId, int knjigaId, DateTime datum,
        int ocena, string komentar)
    {
        if (posetilacId <= 0 || knjigaId <= 0)
            throw new ArgumentException("ID mora biti pozitivan.");
        if (datum.Date > DateTime.Today || datum == default)
            throw new ArgumentException("Datum kupovine nije ispravan.");
        if (ocena < 1 || ocena > 5)
            throw new ArgumentException("Ocena mora biti od 1 do 5.");

        return XmlTransakcija.Izvrsi(
            new[] { "Kupovine.xml", "StavkeListeZelja.xml", "IdBrojaci.xml" },
            () =>
            {
                if (!CitacVeza.PostojiPosetilac(posetilacId))
                    throw new InvalidOperationException("Posetilac ne postoji u zajednickom sistemu.");
                if (_knjige.PronadjiPoId(knjigaId) == null)
                    throw new InvalidOperationException("Knjiga ne postoji.");

                var zelje = _zelje.Ucitaj();
                if (!zelje.Any(z => z.PosetilacId == posetilacId && z.KnjigaId == knjigaId))
                    throw new InvalidOperationException("Knjiga nije na listi zelja posetioca.");

                string putanja = Path.Combine(AppContext.BaseDirectory, "Data", "Kupovine.xml");
                XDocument doc;
                if (File.Exists(putanja))
                    doc = XDocument.Load(putanja);
                else
                    doc = new XDocument(new XElement("ArrayOfKupovina"));
                XElement root = doc.Root ?? throw new InvalidDataException("Kupovine.xml nema koreni element.");
                if (root.Name.LocalName != "ArrayOfKupovina")
                    throw new InvalidDataException("Neusaglasen XML format Kupovine.xml.");

                foreach (XElement zapis in root.Elements())
                {
                    if (Vrednost(zapis, "PosetilacId") == posetilacId &&
                        Vrednost(zapis, "KnjigaId") == knjigaId)
                        throw new InvalidOperationException("Knjiga je vec kupljena.");
                }

                List<IdBrojac> brojaci = _brojaci.Ucitaj();
                IdBrojac? brojac = brojaci.FirstOrDefault(b => b.TipEntiteta == "Kupovina");
                if (brojac == null)
                {
                    brojac = new IdBrojac { TipEntiteta = "Kupovina" };
                    brojaci.Add(brojac);
                }
                int najveci = root.Elements().Select(e => Vrednost(e, "Id")).DefaultIfEmpty(0).Max();
                int id = Math.Max(najveci + 1, brojac.SledeciId);
                if (id == int.MaxValue) throw new InvalidOperationException("ID kupovine je iscrpljen.");

                root.Add(new XElement("Kupovina",
                    new XElement("Id", id),
                    new XElement("PosetilacId", posetilacId),
                    new XElement("KnjigaId", knjigaId),
                    new XElement("DatumKupovine", datum.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)),
                    new XElement("Ocena", ocena),
                    new XElement("Komentar", komentar ?? "")));
                // Datoteka se zamenjuje preko privremene kopije, a kompletna
                // logicka operacija je zasticena dnevnikom XmlTransakcija.
                SacuvajXmlBezbedno(doc, putanja);
                brojac.SledeciId = id + 1;
                _brojaci.Sacuvaj(brojaci);
                zelje.RemoveAll(z => z.PosetilacId == posetilacId && z.KnjigaId == knjigaId);
                _zelje.Sacuvaj(zelje);
                return id;
            });
    }

    private static int Vrednost(XElement zapis, string ime)
    {
        XElement? polje = zapis.Elements().FirstOrDefault(x => x.Name.LocalName == ime);
        return int.TryParse(polje?.Value, out int v) ? v : 0;
    }

    private static void SacuvajXmlBezbedno(XDocument doc, string putanja)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(putanja)!);
        string privremena = putanja + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(privremena, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                doc.Save(stream);
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(putanja))
                File.Replace(privremena, putanja, putanja + ".bak", ignoreMetadataErrors: true);
            else
                File.Move(privremena, putanja);
        }
        finally
        {
            if (File.Exists(privremena))
            {
                try { File.Delete(privremena); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
