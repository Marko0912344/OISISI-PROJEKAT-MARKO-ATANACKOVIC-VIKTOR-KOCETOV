
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

        if (string.IsNullOrWhiteSpace(izdavac.Sifra))
        {
            throw new InvalidOperationException(
                "Sifra izdavaca je obavezna.");
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

        if (_izdavaci.Any(i => i.Sifra == izdavac.Sifra))
        {
            throw new InvalidOperationException(
                "Izdavac sa ovom sifrom vec postoji.");
        }

        _izdavaci.Add(izdavac);

        try
        {
            _skladiste.Sacuvaj(_izdavaci);
        }
        catch
        {
            _izdavaci.Remove(izdavac);
            throw;
        }
    }




    public bool Izmeni(Izdavac izmenjeni)
    {
        Izdavac? postojeci = _izdavaci
            .FirstOrDefault(i => i.Id == izmenjeni.Id);

        if (postojeci == null)
            return false;

        if (string.IsNullOrWhiteSpace(izmenjeni.Sifra))
            throw new InvalidOperationException(
                "Sifra izdavaca je obavezna.");

        if (string.IsNullOrWhiteSpace(izmenjeni.Naziv))
            throw new InvalidOperationException(
                "Naziv izdavaca je obavezan.");

        if (_izdavaci.Any(i =>
            i.Id != izmenjeni.Id &&
            i.Sifra == izmenjeni.Sifra))
            throw new InvalidOperationException(
                "Drugi izdavac vec koristi ovu sifru.");

        string staraSifra = postojeci.Sifra;
        string stariNaziv = postojeci.Naziv;
        int? stariSefId = postojeci.SefId;
        Autor? stariSef = postojeci.Sef;
        var stariAutorIds = new List<int>(postojeci.AutorIds);

        postojeci.Sifra = izmenjeni.Sifra;
        postojeci.Naziv = izmenjeni.Naziv;
        postojeci.SefId = izmenjeni.SefId;

        postojeci.Sef = stariSefId == izmenjeni.SefId
            ? stariSef
            : izmenjeni.Sef?.Id == izmenjeni.SefId
                ? izmenjeni.Sef
                : null;

        postojeci.AutorIds = new List<int>(izmenjeni.AutorIds);

        try
        {
            _skladiste.Sacuvaj(_izdavaci);
        }
        catch
        {
            postojeci.Sifra = staraSifra;
            postojeci.Naziv = stariNaziv;
            postojeci.SefId = stariSefId;
            postojeci.Sef = stariSef;
            postojeci.AutorIds = stariAutorIds;
            throw;
        }

        return true;
    }


    public bool Obrisi(int id, KnjigaServis knjigaServis)
    {
        Izdavac? izdavac = _izdavaci
            .FirstOrDefault(i => i.Id == id);

        if (izdavac == null)
        {
            return false;
        }

        if (knjigaServis.ImaKnjigeIzdavaca(id))
        {
            throw new InvalidOperationException(
                "Izdavac ima povezane knjige i ne moze biti obrisan.");
        }

        int indeks = _izdavaci.IndexOf(izdavac);

        _izdavaci.RemoveAt(indeks);

        try
        {
            _skladiste.Sacuvaj(_izdavaci);
        }
        catch
        {
            _izdavaci.Insert(indeks, izdavac);
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

        int? prethodniSefId = izdavac.SefId;
        Autor? prethodniSef = izdavac.Sef;

        izdavac.SefId = autor.Id;
        izdavac.Sef = autor;

        try
        {
            _skladiste.Sacuvaj(_izdavaci);
        }
        catch
        {
            izdavac.SefId = prethodniSefId;
            izdavac.Sef = prethodniSef;

            throw;
        }

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

        try
        {
            _skladiste.Sacuvaj(_izdavaci);
        }
        catch
        {
            izdavac.AutorIds.Remove(autorId);
            throw;
        }

        return true;
    }


    public int GenerisiSledeciId()
    {
        if (_izdavaci.Count == 0)
        {
            return 1;
        }

        return _izdavaci.Max(i => i.Id) + 1;
    }

}
