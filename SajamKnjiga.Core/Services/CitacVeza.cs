using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace SajamKnjiga.Core.Services;

/// <summary>
/// Most prema XML datotekama koje ce, posle integracije, tipizovano
/// obradjivati Student 1. Ne menja njegove modele niti njegove servise.
/// </summary>
internal static class CitacVeza
{
    private static string Putanja(string naziv) =>
        Path.Combine(AppContext.BaseDirectory, "Data", naziv);

    private static List<XElement> Zapisi(string datoteka)
    {
        var putanja = Putanja(datoteka);
        if (!File.Exists(putanja)) return new List<XElement>();
        try
        {
            XDocument doc = XDocument.Load(putanja);
            if (doc.Root is null) throw new InvalidDataException("Nema korena u " + datoteka);
            return doc.Root.Elements().ToList();
        }
        catch (XmlException ex)
        {
            throw new InvalidDataException("Povezani XML nije ispravan: " + datoteka, ex);
        }
    }

    public static int? Broj(XElement zapis, params string[] nazivi)
    {
        foreach (string naziv in nazivi)
        {
            XElement? polje = zapis.Elements().FirstOrDefault(e => e.Name.LocalName == naziv);
            if (polje is null || string.IsNullOrWhiteSpace(polje.Value)) continue;
            if (!int.TryParse(polje.Value, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int vrednost))
                throw new InvalidDataException($"Neispravna vrednost '{naziv}' u povezanom XML-u.");
            return vrednost;
        }
        return null;
    }

    public static bool SefIzdavaca(int autorId) =>
        Zapisi("Izdavaci.xml").Any(i => Broj(i, "SefId", "SefAutorId") == autorId);

    public static bool SefIzdavacaKod(int izdavacId, int autorId) =>
        Zapisi("Izdavaci.xml").Any(i =>
            Broj(i, "Id") == izdavacId &&
            Broj(i, "SefId", "SefAutorId") == autorId);

    public static bool PostojiIzdavac(int izdavacId) =>
        Zapisi("Izdavaci.xml").Any(i => Broj(i, "Id") == izdavacId);

    public static bool PostojiPosetilac(int posetilacId) =>
        Zapisi("Posetioci.xml").Any(p => Broj(p, "Id") == posetilacId);

    public static bool PosetilacKoristiAdresu(int adresaId) =>
        Zapisi("Posetioci.xml").Any(p => Broj(p, "AdresaId") == adresaId);

    public static bool KnjigaKupljena(int knjigaId) =>
        Zapisi("Kupovine.xml").Any(k => Broj(k, "KnjigaId") == knjigaId);

    public static List<(int PosetilacId, int KnjigaId)> Kupovine() =>
        Zapisi("Kupovine.xml")
            .Select(k => (PosetilacId: Broj(k, "PosetilacId") ?? 0,
                          KnjigaId: Broj(k, "KnjigaId") ?? 0))
            .Where(k => k.PosetilacId > 0 && k.KnjigaId > 0)
            .ToList();
}
