using System.Xml.Linq;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services;

// SAMO citanje zavisnosti iz tudjih entiteta; ne definise modele
// Posetilac/Kupovina/Izdavac, niti menja njihove poslovne operacije.
// Kod zajednickog spajanja sa Studentom 1 zameniti tipizovanim servisima.
internal static class CitacVeza
{
    public static bool SefIzdavaca(int autorId) =>
        PostojiOdgovarajucaStavka("Izdavaci.xml", "SefAutorId", autorId);

    public static bool KnjigaKupljena(int knjigaId) =>
        PostojiOdgovarajucaStavka("Kupovine.xml", "KnjigaId", knjigaId);

    public static bool PostojiPosetilac(int posetilacId) =>
        PostojiOdgovarajucaStavka("Posetioci.xml", "Id", posetilacId);

    public static bool PosetilacKoristiAdresu(int adresaId) =>
        PostojiOdgovarajucaStavka("Posetioci.xml", "AdresaId", adresaId);

    private static bool PostojiOdgovarajucaStavka(string datoteka, string polje, int id)
    {
        string putanja = Path.Combine(AppContext.BaseDirectory, "Data", datoteka);
        if (!File.Exists(putanja)) return false;
        try
        {
            XDocument doc = XDocument.Load(putanja);
            return doc.Root?.Elements().Any(z =>
                z.Elements().Any(e => e.Name.LocalName == polje &&
                    int.TryParse(e.Value, out int vrednost) && vrednost == id)) ?? false;
        }
        catch (System.Xml.XmlException ex)
        {
            throw new InvalidDataException("Neispravan povezani XML: " + datoteka, ex);
        }
    }
}
