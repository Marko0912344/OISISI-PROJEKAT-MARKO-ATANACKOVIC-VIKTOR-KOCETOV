
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
        _skladiste.Sacuvaj(_kupovine);
    }

    public bool Izmeni(Kupovina izmenjenaKupovina)
    {
        Kupovina? postojeca = _kupovine
            .FirstOrDefault(k => k.Id == izmenjenaKupovina.Id);

        if (postojeca == null)
        {
            return false;
        }

        if (izmenjenaKupovina.Ocena < 1 || izmenjenaKupovina.Ocena > 5)
        {
            throw new InvalidOperationException(
                "Ocena mora biti izmedju 1 i 5.");
        }

        postojeca.Ocena = izmenjenaKupovina.Ocena;
        postojeca.Komentar = izmenjenaKupovina.Komentar;
        postojeca.Ocena = izmenjenaKupovina.Ocena;
        postojeca.Komentar = izmenjenaKupovina.Komentar;

        _skladiste.Sacuvaj(_kupovine);

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

        _kupovine.Remove(kupovina);
        _skladiste.Sacuvaj(_kupovine);

        return true;
    }

}
