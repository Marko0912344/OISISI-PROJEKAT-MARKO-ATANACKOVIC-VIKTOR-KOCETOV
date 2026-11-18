
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
        if (izdavac.Id <= 0)
        {
            throw new InvalidOperationException(
                "ID izdavaca mora biti pozitivan broj.");
        }

        if (string.IsNullOrWhiteSpace(izdavac.Naziv))
        {
            throw new InvalidOperationException(
                "Naziv izdavaca je obavezan.");
        }

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

        postojeci.Naziv = izmenjeni.Naziv;
        postojeci.SefId = izmenjeni.SefId;
        postojeci.AutorIds = new List<int>(izmenjeni.AutorIds);

        _skladiste.Sacuvaj(_izdavaci);

        return true;
    }

    public bool Obrisi(int id, KnjigaServis knjigaServis)
    {
        Izdavac? izdavac = _izdavaci
            .FirstOrDefault(i => i.Id == id);

        if (izdavac == null)
            return false;

        if (knjigaServis.ImaKnjigeIzdavaca(id))
        {
            throw new InvalidOperationException(
                "Izdavac ima povezane knjige i ne moze biti obrisan.");
        }

        _izdavaci.Remove(izdavac);

        try
        {
            _skladiste.Sacuvaj(_izdavaci);
        }
        catch
        {
            _izdavaci.Add(izdavac);
            throw;
        }

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

    public bool PoveziAutora(int izdavacId, int autorId, AutorServis autorServis)
    {
        Izdavac? izdavac = _izdavaci
            .FirstOrDefault(i => i.Id == izdavacId);

        if (izdavac == null)
        {
            throw new InvalidOperationException("Izdavac ne postoji.");
        }

        Autor? autor = autorServis.PronadjiPoId(autorId);

        if (autor == null)
        {
            throw new InvalidOperationException("Autor ne postoji.");
        }

        if (izdavac.AutorIds.Contains(autorId))
        {
            throw new InvalidOperationException(
                "Autor je vec povezan sa ovim izdavacem.");
        }

        izdavac.AutorIds.Add(autorId);

        _skladiste.Sacuvaj(_izdavaci);

        return true;
    }

}
