
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.Data;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Services;

public class KnjigaServis
{
    private readonly XmlSkladiste<Knjiga> _skladiste;
    private readonly List<Knjiga> _knjige;

    public KnjigaServis(string putanja)
    {
        _skladiste = new XmlSkladiste<Knjiga>(putanja);
        _knjige = _skladiste.Ucitaj();
    }

    public List<Knjiga> PrikaziSve()
    {
        return new List<Knjiga>(_knjige);
    }

    public bool ImaKnjigeIzdavaca(int izdavacId)
    {
        return _knjige.Any(k => k.IzdavacId == izdavacId);
    }

    public bool PostojiKnjiga(int id)
    {
        return _knjige.Any(k => k.Id == id);
    }


    public bool Dodaj(Knjiga knjiga)
    {
        if (knjiga.Id <= 0)
            throw new InvalidOperationException("Neispravan ID knjige.");

        if (string.IsNullOrWhiteSpace(knjiga.ISBN))
            throw new InvalidOperationException("ISBN je obavezan.");

        if (string.IsNullOrWhiteSpace(knjiga.Naziv))
            throw new InvalidOperationException("Naziv knjige je obavezan.");

        if (string.IsNullOrWhiteSpace(knjiga.Zanr))
            throw new InvalidOperationException("Zanr je obavezan.");

        if (knjiga.GodinaIzdanja <= 0 ||
            knjiga.GodinaIzdanja > DateTime.Now.Year)
            throw new InvalidOperationException("Neispravna godina izdanja.");

        if (knjiga.Cena < 0)
            throw new InvalidOperationException("Cena ne moze biti negativna.");

        if (knjiga.BrojStrana <= 0)
            throw new InvalidOperationException("Neispravan broj strana.");

        if (_knjige.Any(k => k.Id == knjiga.Id))
            throw new InvalidOperationException(
                "Knjiga sa ovim ID-jem vec postoji.");

        knjiga.ISBN = knjiga.ISBN.Trim();
        knjiga.Naziv = knjiga.Naziv.Trim();
        knjiga.Zanr = knjiga.Zanr.Trim();

        if (_knjige.Any(k =>
            string.Equals(k.ISBN, knjiga.ISBN,
                StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                "Knjiga sa ovim ISBN-om vec postoji.");

        _knjige.Add(knjiga);

        try
        {
            _skladiste.Sacuvaj(_knjige);
        }
        catch
        {
            _knjige.Remove(knjiga);
            throw;
        }

        return true;
    }

    public bool Izmeni(Knjiga knjiga)
    {
        Knjiga? postojeca = _knjige
            .FirstOrDefault(k => k.Id == knjiga.Id);

        if (postojeca == null)
            return false;

        if (string.IsNullOrWhiteSpace(knjiga.ISBN) ||
            string.IsNullOrWhiteSpace(knjiga.Naziv) ||
            string.IsNullOrWhiteSpace(knjiga.Zanr))
            throw new InvalidOperationException(
                "ISBN, naziv i zanr su obavezni.");

        if (knjiga.GodinaIzdanja <= 0 ||
            knjiga.GodinaIzdanja > DateTime.Now.Year)
            throw new InvalidOperationException(
                "Neispravna godina izdanja.");

        if (knjiga.Cena < 0 || knjiga.BrojStrana <= 0)
            throw new InvalidOperationException(
                "Neispravna cena ili broj strana.");

        string noviIsbn = knjiga.ISBN.Trim();

        if (_knjige.Any(k =>
            k.Id != knjiga.Id &&
            string.Equals(k.ISBN, noviIsbn,
                StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException(
                "Druga knjiga vec koristi ovaj ISBN.");

        var stara = new Knjiga
        {
            Id = postojeca.Id,
            ISBN = postojeca.ISBN,
            Naziv = postojeca.Naziv,
            Zanr = postojeca.Zanr,
            GodinaIzdanja = postojeca.GodinaIzdanja,
            Cena = postojeca.Cena,
            BrojStrana = postojeca.BrojStrana,
            IzdavacId = postojeca.IzdavacId
        };

        postojeca.ISBN = noviIsbn;
        postojeca.Naziv = knjiga.Naziv.Trim();
        postojeca.Zanr = knjiga.Zanr.Trim();
        postojeca.GodinaIzdanja = knjiga.GodinaIzdanja;
        postojeca.Cena = knjiga.Cena;
        postojeca.BrojStrana = knjiga.BrojStrana;
        postojeca.IzdavacId = knjiga.IzdavacId;

        try
        {
            _skladiste.Sacuvaj(_knjige);
        }
        catch
        {
            postojeca.ISBN = stara.ISBN;
            postojeca.Naziv = stara.Naziv;
            postojeca.Zanr = stara.Zanr;
            postojeca.GodinaIzdanja = stara.GodinaIzdanja;
            postojeca.Cena = stara.Cena;
            postojeca.BrojStrana = stara.BrojStrana;
            postojeca.IzdavacId = stara.IzdavacId;
            throw;
        }

        return true;
    }
    public bool Obrisi(
    int id,
    KupovinaServis kupovinaServis,
    ListaZeljaServis listaZeljaServis)
    {
        Knjiga? knjiga = _knjige.FirstOrDefault(k => k.Id == id);

        if (knjiga == null)
            return false;

        if (kupovinaServis.PrikaziSve()
            .Any(k => k.KnjigaId == id))
        {
            throw new InvalidOperationException(
                "Knjiga ima evidentirane kupovine i ne moze biti obrisana.");
        }

        if (listaZeljaServis.PrikaziSve()
            .Any(s => s.KnjigaId == id))
        {
            throw new InvalidOperationException(
                "Knjiga se nalazi na listi zelja i ne moze biti obrisana.");
        }

        int indeks = _knjige.IndexOf(knjiga);
        _knjige.RemoveAt(indeks);

        try
        {
            _skladiste.Sacuvaj(_knjige);
        }
        catch
        {
            _knjige.Insert(indeks, knjiga);
            throw;
        }

        return true;
    }

}
