using System.Xml.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services;

public class Student2AutoriService
{
    private readonly XmlDao<Autor> _autori = new("Autori.xml");
    private readonly XmlDao<Adresa> _adrese = new("Adrese.xml");
    private readonly XmlStorage<AutorKnjiga> _veze = new("AutorKnjiga.xml");

    public List<Adresa> SveAdrese() => _adrese.UcitajSve();

    public Adresa DodajAdresu(Adresa adresa) =>
        XmlTransakcija.Izvrsi(new[] { "Adrese.xml", "IdBrojaci.xml" }, () =>
        {
            ProveriAdresu(adresa);
            if (adresa.Id != 0)
                throw new InvalidOperationException("Nova adresa mora imati Id=0.");
            return _adrese.Dodaj(adresa);
        });

    public bool IzmeniAdresu(Adresa adresa)
    {
        ProveriAdresu(adresa);
        if (_adrese.PronadjiPoId(adresa.Id) == null) return false;
        return _adrese.Izmeni(adresa);
    }

    public bool ObrisiAdresu(int id)
    {
        if (_autori.UcitajSve().Any(a => a.AdresaId == id) || CitacVeza.PosetilacKoristiAdresu(id))
            throw new InvalidOperationException("Adresa je u upotrebi. Brisanje nije dozvoljeno.");
        return _adrese.Obrisi(id);
    }

    public List<Autor> Svi() => _autori.UcitajSve();
    public Autor? Nadji(int id) => _autori.PronadjiPoId(id);

    public Autor Dodaj(Autor autor, Adresa adresa) =>
        XmlTransakcija.Izvrsi(new[] { "Autori.xml", "Adrese.xml", "IdBrojaci.xml" }, () =>
        {
            ProveriAutora(autor);
            ProveriAdresu(adresa);
            if (autor.Id != 0 || adresa.Id != 0)
                throw new InvalidOperationException("Novi autor i adresa moraju imati Id=0.");
            ProveriDuplikatLK(autor.BrojLicneKarte, 0);
            _adrese.Dodaj(adresa);
            autor.AdresaId = adresa.Id;
            autor.Adresa = adresa;
            return _autori.Dodaj(autor);
        });

    public bool Izmeni(Autor autor)
    {
        ProveriAutora(autor);
        if (_autori.PronadjiPoId(autor.Id) == null)
            throw new InvalidOperationException("Autor ne postoji.");
        if (_adrese.PronadjiPoId(autor.AdresaId) == null)
            throw new InvalidOperationException("Adresa autora ne postoji.");
        ProveriDuplikatLK(autor.BrojLicneKarte, autor.Id);
        if (autor.GodineIskustva < 5 && CitacVeza.SefIzdavaca(autor.Id))
            throw new InvalidOperationException("Sef izdavaca mora imati >= 5 godina iskustva.");
        return _autori.Izmeni(autor);
    }

    public bool IzmeniSaAdresom(Autor autor, Adresa adresa) =>
        XmlTransakcija.Izvrsi(new[] { "Autori.xml", "Adrese.xml" }, () =>
        {
            if (autor.AdresaId != adresa.Id || adresa.Id <= 0)
                throw new InvalidOperationException("Adresa ne pripada autoru.");
            ProveriAdresu(adresa);
            if (!Izmeni(autor)) return false;
            if (!_adrese.Izmeni(adresa))
                throw new InvalidOperationException("Adresa nije sacuvana.");
            return true;
        });

    public Adresa? NadjiAdresu(int id) => _adrese.PronadjiPoId(id);

    public bool Obrisi(int id)
    {
        if (_autori.PronadjiPoId(id) == null) return false;
        if (_veze.Ucitaj().Any(x => x.AutorId == id))
            throw new InvalidOperationException("Autor ima knjige. Brisanje nije dozvoljeno.");
        if (CitacVeza.SefIzdavaca(id))
            throw new InvalidOperationException("Autor je sef izdavaca. Brisanje nije dozvoljeno.");
        return _autori.Obrisi(id);
    }

    public List<Autor> Pretrazi(string upit)
    {
        string[] reci = upit.Split(',', StringSplitOptions.TrimEntries);
        if (reci.Length is < 1 or > 2 || reci.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Unesi prezime ili prezime, ime.");
        return Svi().Where(a =>
            a.Prezime.Contains(reci[0], StringComparison.OrdinalIgnoreCase) &&
            (reci.Length == 1 || a.Ime.Contains(reci[1], StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public List<Autor> Sortiraj(string polje, bool opadajuce)
    {
        var lista = Svi();
        Func<Autor, string> kljuc = polje.ToLowerInvariant() switch
        {
            "ime" => a => a.Ime,
            "prezime" => a => a.Prezime,
            "email" => a => a.Email,
            _ => throw new ArgumentException("Polje: ime, prezime ili email.")
        };
        return (opadajuce
            ? lista.OrderByDescending(kljuc, StringComparer.CurrentCultureIgnoreCase)
            : lista.OrderBy(kljuc, StringComparer.CurrentCultureIgnoreCase)).ToList();
    }

    private void ProveriDuplikatLK(string lk, int osimId)
    {
        if (_autori.UcitajSve().Any(a => a.Id != osimId &&
            a.BrojLicneKarte.Equals(lk, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Broj licne karte vec postoji.");
    }

    private static void ProveriAutora(Autor a)
    {
        if (string.IsNullOrWhiteSpace(a.Ime) || string.IsNullOrWhiteSpace(a.Prezime) ||
            string.IsNullOrWhiteSpace(a.BrojLicneKarte) ||
            string.IsNullOrWhiteSpace(a.Telefon) || string.IsNullOrWhiteSpace(a.Email) ||
            a.DatumRodjenja == default || a.DatumRodjenja.Date > DateTime.Today ||
            a.GodineIskustva < 0)
            throw new ArgumentException("Neispravni ili nepotpuni podaci o autoru.");
    }

    private static void ProveriAdresu(Adresa a)
    {
        if (string.IsNullOrWhiteSpace(a.Ulica) || string.IsNullOrWhiteSpace(a.Broj) ||
            string.IsNullOrWhiteSpace(a.Grad) || string.IsNullOrWhiteSpace(a.Drzava))
            throw new ArgumentException("Adresa mora imati ulicu, broj, grad i drzavu.");
    }
}
