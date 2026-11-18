using System.Globalization;
using System.Xml.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;
using SajamKnjiga.Core.Services;

// OVO JE ODVOJENA TEST APLIKACIJA. Ne pokretati iz SajamKnjiga.Console.
// AppContext.BaseDirectory pokazuje iskljucivo na izlaz projekta Student2.Tests.
string folder = AppContext.BaseDirectory;
if (!folder.Contains("Student2.Tests", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Test je pokrenut sa neocekivane lokacije. Prekidam.");
string data = Path.Combine(folder, "Data");
string trans = Path.Combine(folder, "Transakcije");
if (Directory.Exists(data)) Directory.Delete(data, recursive: true);
if (Directory.Exists(trans)) Directory.Delete(trans, recursive: true);
Directory.CreateDirectory(data);

int testovi = 0;
void Potvrdi(bool uslov, string opis)
{
    if (!uslov) throw new Exception("NEUSPEH: " + opis);
    Console.WriteLine("PROLAZ: " + opis);
    testovi++;
}
void OcekivanoOdbijeno(Action akcija, string opis)
{
    bool odbijeno = false;
    try { akcija(); }
    catch (InvalidOperationException) { odbijeno = true; }
    Potvrdi(odbijeno, opis);
}

// Minimalni zajednicki XML podaci: ne uvozimo niti pisemo klase Studenta 1.
new XDocument(new XElement("ArrayOfPosetilac",
    new XElement("Posetilac", new XElement("Id", 10),
        new XElement("AdresaId", 0), new XElement("ProsecnaOcena", 0))))
    .Save(Path.Combine(data, "Posetioci.xml"));
new XDocument(new XElement("ArrayOfIzdavac",
    new XElement("Izdavac", new XElement("Id", 5), new XElement("Naziv", "Test Izdavac"),
        new XElement("SefId", 1))))
    .Save(Path.Combine(data, "Izdavaci.xml"));

var aserv = new Student2AutoriService();
var kserv = new Student2KnjigeService();
var kupovine = new Student2KupovineService();
Autor a1 = aserv.Dodaj(new Autor {
    Ime = "Ana", Prezime = "Petrovic", DatumRodjenja = new DateTime(1990, 1, 1),
    Telefon = "060123", Email = "ana@example.test", BrojLicneKarte = "LK-01", GodineIskustva = 7
}, new Adresa("Glavna", "1", "Novi Sad", "Srbija"));
Autor a2 = aserv.Dodaj(new Autor {
    Ime = "Marko", Prezime = "Markovic", DatumRodjenja = new DateTime(1994, 2, 2),
    Telefon = "060456", Email = "marko@example.test", BrojLicneKarte = "LK-02", GodineIskustva = 2
}, new Adresa("Druga", "2", "Beograd", "Srbija"));
Potvrdi(a1.Id == 1 && a2.Id == 2, "Trajni ID-jevi autora");
Potvrdi(aserv.Nadji(a1.Id)?.Adresa?.Grad == "Novi Sad", "Rekonstruisana adresa autora");
Potvrdi(aserv.Pretrazi("petr, ana").Count == 1, "Pretraga prezime, ime");
Potvrdi(aserv.Upit("mar", "ime", false).Count == 1, "Kombinovana pretraga i sortiranje autora");
OcekivanoOdbijeno(() => aserv.Dodaj(new Autor {
    Ime = "Drugi", Prezime = "Autor", DatumRodjenja = DateTime.Today,
    Telefon = "060", Email = "drugi@example.test", BrojLicneKarte = "LK-01", GodineIskustva = 1
}, new Adresa("A", "3", "Grad", "Drzava")), "Duplirana licna karta se odbija");

var dao = new XmlDao<Knjiga>("Knjige.xml");
Knjiga DodajKnjigu(string isbn, string naziv, int? izdavacId) =>
    XmlTransakcija.Izvrsi(new[] { "Knjige.xml", "IdBrojaci.xml" }, () =>
        dao.Dodaj(new Knjiga {
            ISBN = isbn, Naziv = naziv, Zanr = "Roman", GodinaIzdanja = 2025,
            Cena = 700, BrojStrana = 200, IzdavacId = izdavacId
        }));
Knjiga b1 = DodajKnjigu("ISBN-01", "Prva knjiga", 5);
Knjiga b2 = DodajKnjigu("ISBN-02", "Druga knjiga", 5);
Knjiga b3 = DodajKnjigu("ISBN-03", "Treca knjiga", null);
Knjiga b4 = DodajKnjigu("ISBN-04", "Cetvrta knjiga", null);

Potvrdi(kserv.DodajAutora(a1.Id, b1.Id), "Povezivanje autora i knjige");
Potvrdi(kserv.DodajAutora(a1.Id, b2.Id), "Vise knjiga istog autora");
Potvrdi(kserv.DodajAutora(a2.Id, b3.Id), "Veza sa drugim autorom");
Potvrdi(kserv.DodajAutora(a2.Id, b4.Id), "Veza sa cetvrtom knjigom");
Potvrdi(kserv.UkloniAutora(a2.Id, b4.Id) &&
    kserv.Nadji(b4.Id)!.Autori.Count == 0,
    "Uklanjanje autora moze ostaviti prazno polje (sekcija 8.25)");
Potvrdi(kserv.Sve().Single(k => k.Id == b1.Id).Autori.Single().Id == a1.Id,
    "Rekonstruisana lista autora knjige");
Potvrdi(aserv.Svi().Single(a => a.Id == a1.Id).Knjige.Count == 2,
    "Rekonstruisana lista knjiga autora");
Potvrdi(kserv.Upit("knjiga", "cena", true).Count == 4,
    "Kombinovana pretraga i sortiranje knjiga");
Potvrdi(kserv.KnjigeIzdavaca(5).Count == 2, "Knjige izdavaca");
Potvrdi(kserv.PretraziAutoreIzdavaca(5, "pet").Count == 1,
    "Pretraga autora povezanih sa izdavacem");

// Sef je autor knjiga b1 i b2: jedna veza moze biti uklonjena,
// a poslednja kod tog izdavaca mora ostati.
Potvrdi(kserv.UkloniAutora(a1.Id, b1.Id), "Dozvoljeno uklanjanje autora sa knjige");
OcekivanoOdbijeno(() => kserv.UkloniAutora(a1.Id, b2.Id),
    "Zabrana uklanjanja poslednje knjige sefa kod izdavaca");
Potvrdi(kserv.DodajAutora(a1.Id, b1.Id), "Ponovno povezivanje autora");
Autor izmena = aserv.Nadji(a1.Id)!;
izmena.GodineIskustva = 4;
OcekivanoOdbijeno(() => aserv.Izmeni(izmena),
    "Sef izdavaca ne moze imati ispod 5 godina iskustva");
OcekivanoOdbijeno(() => aserv.Obrisi(a1.Id),
    "Zabrana brisanja autora koji je sef ili ima knjige");

var zelje = new XmlStorage<StavkaListeZelja>("StavkeListeZelja.xml");
zelje.Sacuvaj(new List<StavkaListeZelja> {
    new() { PosetilacId = 10, KnjigaId = b3.Id }
});
Potvrdi(kserv.ListaZelja(10).Single().Id == b3.Id, "Prikaz liste zelja");
Potvrdi(kserv.PretraziAutoreZaZelje(10, "mark").Single().Id == a2.Id,
    "Autori sa liste zelja i pretraga unutar rezultata");
Potvrdi(kserv.UkloniSaListeZelja(10, b3.Id), "Uklanjanje sa liste zelja");
Potvrdi(kserv.ListaZelja(10).Count == 0, "Zelja ostaje uklonjena na disku");
zelje.Sacuvaj(new List<StavkaListeZelja> {
    new() { PosetilacId = 10, KnjigaId = b3.Id }
});

int kupovinaId = kupovine.Evidentiraj(10, b3.Id, DateTime.Today, 5, "Odlicna knjiga");
Potvrdi(kupovinaId >= 1 && kserv.ListaZelja(10).Count == 0,
    "Kupovina premesta knjigu iz liste zelja");
Potvrdi(kserv.Nadji(b3.Id)!.IdKupaca.Contains(10),
    "Knjiga zna koji posetilac ju je kupio (ID)");
XDocument doc = XDocument.Load(Path.Combine(data, "Posetioci.xml"));
string prosek = doc.Root!.Elements().Single().Elements().Single(e =>
    e.Name.LocalName == "ProsecnaOcena").Value;
Potvrdi(double.Parse(prosek, CultureInfo.InvariantCulture) == 5,
    "Prosecna ocena posetioca uskladjena sa kupovinom");
OcekivanoOdbijeno(() => kserv.Obrisi(b3.Id),
    "Kupljena knjiga ne moze da se obrise");
Potvrdi(kserv.Obrisi(b1.Id),
    "Knjiga moze da se obrise kada sef ima drugu kod istog izdavaca");
OcekivanoOdbijeno(() => kserv.Obrisi(b2.Id),
    "Poslednja knjiga sefa kod izdavaca ne moze da se obrise");

// Obican izuzetak unutar transakcije mora da vrati prethodnu XML listu.
var skladisteVeza = new XmlStorage<AutorKnjiga>("AutorKnjiga.xml");
int pre = skladisteVeza.Ucitaj().Count;
OcekivanoOdbijeno(() => XmlTransakcija.Izvrsi<bool>(
    new[] { "AutorKnjiga.xml" }, () =>
    {
        var kopija = skladisteVeza.Ucitaj();
        kopija.Add(new AutorKnjiga { AutorId = 999, KnjigaId = 999 });
        skladisteVeza.Sacuvaj(kopija);
        throw new InvalidOperationException("Namerni pad transakcije.");
    }), "Namerni prekid obicne transakcije");
Potvrdi(skladisteVeza.Ucitaj().Count == pre, "Rollback je vratio prethodni XML");

Potvrdi(XmlTransakcija.OporaviPrekinute() >= 0,
    "Mehanizam oporavka moze bezbedno da se pozove");
Console.WriteLine($"\nUSPESNO: {testovi} provera. Podaci su iskljucivo u: {data}");
