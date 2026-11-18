
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
        if (_knjige.Any(k => k.Id == knjiga.Id))
        {
            return false;
        }

        _knjige.Add(knjiga);
        _skladiste.Sacuvaj(_knjige);

        return true;
    }
    public bool Izmeni(Knjiga knjiga)
    {
        Knjiga? postojeca = _knjige.FirstOrDefault(k => k.Id == knjiga.Id);

        if (postojeca == null)
        {
            return false;
        }

        postojeca.ISBN = knjiga.ISBN;
        postojeca.Naziv = knjiga.Naziv;
        postojeca.Zanr = knjiga.Zanr;
        postojeca.GodinaIzdanja = knjiga.GodinaIzdanja;
        postojeca.Cena = knjiga.Cena;
        postojeca.BrojStrana = knjiga.BrojStrana;
        postojeca.IzdavacId = knjiga.IzdavacId;

        _skladiste.Sacuvaj(_knjige);

        return true;
    }
}
