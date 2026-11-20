
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.Data;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Services;

public class AdresaServis
{
    private readonly XmlSkladiste<Adresa> _skladiste;
    private readonly List<Adresa> _adrese;

    public AdresaServis(string putanja)
    {
        _skladiste = new XmlSkladiste<Adresa>(putanja);
        _adrese = _skladiste.Ucitaj();
    }

    public List<Adresa> PrikaziSve()
    {
        return new List<Adresa>(_adrese);
    }

    public void Dodaj(Adresa adresa)
    {
        if (_adrese.Any(a => a.Id == adresa.Id))
        {
            throw new System.InvalidOperationException(
                "Adresa sa ovim ID-jem vec postoji.");
        }

        _adrese.Add(adresa);
        _skladiste.Sacuvaj(_adrese);
    }

    public bool Izmeni(Adresa izmenjena)
    {
        Adresa? postojeca = _adrese
            .FirstOrDefault(a => a.Id == izmenjena.Id);

        if (postojeca == null)
            return false;

        int indeks = _adrese.IndexOf(postojeca);

        _adrese[indeks] = izmenjena;

        _skladiste.Sacuvaj(_adrese);

        return true;
    }

    public bool Obrisi(int id)
    {
        Adresa? adresa = _adrese
            .FirstOrDefault(a => a.Id == id);

        if (adresa == null)
            return false;

        _adrese.Remove(adresa);
        _skladiste.Sacuvaj(_adrese);

        return true;
    }


}
