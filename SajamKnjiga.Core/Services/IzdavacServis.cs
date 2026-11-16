
using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.Data;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Services;

public class IzdavacServis
{
    private readonly XmlSkladiste<Izdavac> _skladiste;
    private readonly List<Izdavac> _izdavaci;

    public IzdavacServis(string putanja)
    {
        _skladiste = new XmlSkladiste<Izdavac>(putanja);
        _izdavaci = _skladiste.Ucitaj();
    }

    public List<Izdavac> PrikaziSve()
    {
        return new List<Izdavac>(_izdavaci);
    }

    public void Dodaj(Izdavac izdavac)
    {
        if (_izdavaci.Any(i => i.Id == izdavac.Id))
        {
            throw new InvalidOperationException(
                "Izdavac sa ovim ID-jem vec postoji.");
        }

        _izdavaci.Add(izdavac);
        _skladiste.Sacuvaj(_izdavaci);
    }

    public bool Izmeni(Izdavac izmenjeni)
    {
        Izdavac? postojeci = _izdavaci
            .FirstOrDefault(i => i.Id == izmenjeni.Id);

        if (postojeci == null)
            return false;

        int indeks = _izdavaci.IndexOf(postojeci);
        _izdavaci[indeks] = izmenjeni;

        _skladiste.Sacuvaj(_izdavaci);

        return true;
    }

    public bool Obrisi(int id)
    {
        Izdavac? izdavac = _izdavaci
            .FirstOrDefault(i => i.Id == id);

        if (izdavac == null)
            return false;

        _izdavaci.Remove(izdavac);
        _skladiste.Sacuvaj(_izdavaci);

        return true;
    }

    public bool PostaviSefa(int izdavacId, int autorId, AutorServis autorServis)
    {
        Izdavac? izdavac = _izdavaci
            .FirstOrDefault(i => i.Id == izdavacId);

        if (izdavac == null)
        {
            throw new InvalidOperationException(
                "Izdavac ne postoji.");
        }

        Autor? autor = autorServis.PronadjiPoId(autorId);

        if (autor == null)
        {
            throw new InvalidOperationException(
                "Autor ne postoji.");
        }

        if (autor.GodineIskustva < 5)
        {
            throw new InvalidOperationException(
                "Sef mora imati najmanje 5 godina iskustva.");
        }

        izdavac.SefId = autor.Id;
        izdavac.Sef = autor;

        _skladiste.Sacuvaj(_izdavaci);

        return true;
    }

}
