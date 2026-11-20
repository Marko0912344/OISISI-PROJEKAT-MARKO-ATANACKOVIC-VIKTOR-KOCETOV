
using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.Data;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Services;

public class KupovinaServis
{
    private readonly XmlSkladiste<Kupovina> _skladiste;
    private readonly List<Kupovina> _kupovine;

    public KupovinaServis(string putanja)
    {
        _skladiste = new XmlSkladiste<Kupovina>(putanja);
        _kupovine = _skladiste.Ucitaj();
    }

    public List<Kupovina> PrikaziSve()
    {
        return new List<Kupovina>(_kupovine);
    }

    public List<Kupovina> KupovinePosetioca(int posetilacId)
    {
        return _kupovine
            .Where(k => k.PosetilacId == posetilacId)
            .ToList();
    }


    public void Dodaj(Kupovina kupovina)
    {
        if (kupovina.Id <= 0 ||
            kupovina.PosetilacId <= 0 ||
            kupovina.KnjigaId <= 0)
        {
            throw new InvalidOperationException(
                "ID kupovine, posetioca i knjige moraju biti pozitivni.");
        }

        if (_kupovine.Any(k => k.Id == kupovina.Id))
        {
            throw new InvalidOperationException(
                "Kupovina sa ovim ID-jem vec postoji.");
        }

        if (kupovina.Ocena < 1 || kupovina.Ocena > 5)
        {
            throw new InvalidOperationException(
                "Ocena mora biti izmedju 1 i 5.");
        }

        _kupovine.Add(kupovina);

        try
        {
            _skladiste.Sacuvaj(_kupovine);
        }
        catch
        {
            _kupovine.Remove(kupovina);
            throw;
        }
    }



    public bool Izmeni(Kupovina izmenjenaKupovina)
    {
        Kupovina? postojeca = _kupovine
            .FirstOrDefault(k => k.Id == izmenjenaKupovina.Id);

        if (postojeca == null)
            return false;

        if (izmenjenaKupovina.Ocena < 1 ||
            izmenjenaKupovina.Ocena > 5)
        {
            throw new InvalidOperationException(
                "Ocena mora biti izmedju 1 i 5.");
        }

        if (izmenjenaKupovina.DatumKupovine == default ||
            izmenjenaKupovina.DatumKupovine.Date > DateTime.Today)
        {
            throw new InvalidOperationException(
                "Datum kupovine nije ispravan.");
        }

        if (postojeca.PosetilacId != izmenjenaKupovina.PosetilacId ||
            postojeca.KnjigaId != izmenjenaKupovina.KnjigaId)
        {
            throw new InvalidOperationException(
                "Promena posetioca ili knjige zahteva proveru povezanih podataka.");
        }

        Kupovina prethodna = new Kupovina
        {
            Id = postojeca.Id,
            PosetilacId = postojeca.PosetilacId,
            KnjigaId = postojeca.KnjigaId,
            DatumKupovine = postojeca.DatumKupovine,
            Ocena = postojeca.Ocena,
            Komentar = postojeca.Komentar
        };

        postojeca.DatumKupovine = izmenjenaKupovina.DatumKupovine;
        postojeca.Ocena = izmenjenaKupovina.Ocena;
        postojeca.Komentar = izmenjenaKupovina.Komentar;

        try
        {
            _skladiste.Sacuvaj(_kupovine);
        }
        catch
        {
            postojeca.DatumKupovine = prethodna.DatumKupovine;
            postojeca.Ocena = prethodna.Ocena;
            postojeca.Komentar = prethodna.Komentar;
            throw;
        }

        return true;
    }

    public bool Obrisi(int id)
    {
        Kupovina? kupovina = _kupovine
            .FirstOrDefault(k => k.Id == id);

        if (kupovina == null)
        {
            return false;
        }

        int indeks = _kupovine.IndexOf(kupovina);

        _kupovine.RemoveAt(indeks);

        try
        {
            _skladiste.Sacuvaj(_kupovine);
        }
        catch
        {
            _kupovine.Insert(indeks, kupovina);
            throw;
        }

        return true;
    }


    public int GenerisiSledeciId()
    {
        if (_kupovine.Count == 0)
        {
            return 1;
        }

        return _kupovine.Max(k => k.Id) + 1;
    }


}
