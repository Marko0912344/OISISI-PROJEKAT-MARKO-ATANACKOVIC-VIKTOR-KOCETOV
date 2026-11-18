
using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.Data;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Services;

public class ListaZeljaServis
{
    private readonly XmlSkladiste<StavkaListeZelja> _skladiste;
    private readonly List<StavkaListeZelja> _stavke;

    public ListaZeljaServis(string putanja)
    {
        _skladiste = new XmlSkladiste<StavkaListeZelja>(putanja);
        _stavke = _skladiste.Ucitaj();
    }

    public List<StavkaListeZelja> PrikaziSve()
    {
        return new List<StavkaListeZelja>(_stavke);
    }

    public List<StavkaListeZelja> ListaPosetioca(int posetilacId)
    {
        return _stavke
            .Where(s => s.PosetilacId == posetilacId)
            .ToList();
    }



    public void Dodaj(
        int posetilacId,
        int knjigaId,
        PosetilacServis posetilacServis,
        KnjigaServis knjigaServis,
        KupovinaServis kupovinaServis)

    {
        if (posetilacId <= 0 || knjigaId <= 0)
        {
            throw new InvalidOperationException(
                "ID posetioca i knjige moraju biti pozitivni.");
        }

        bool postojiPosetilac = posetilacServis
            .PrikaziSve()
            .Any(p => p.Id == posetilacId);

        if (!postojiPosetilac)
        {
            throw new InvalidOperationException(
                "Posetilac sa ovim ID-jem ne postoji.");
        }

        if (!knjigaServis.PostojiKnjiga(knjigaId))
        {
            throw new InvalidOperationException(
                "Knjiga sa ovim ID-jem ne postoji.");
        }

        if (_stavke.Any(s =>
            s.PosetilacId == posetilacId &&
            s.KnjigaId == knjigaId))
        {
            throw new InvalidOperationException(
                "Knjiga je vec na listi zelja ovog posetioca.");
        }

        bool vecKupljena = kupovinaServis
            .KupovinePosetioca(posetilacId)
            .Any(k => k.KnjigaId == knjigaId);

        if (vecKupljena)
        {
            throw new InvalidOperationException(
                "Knjiga je vec kupljena i ne moze biti dodata na listu zelja.");
        }

        _stavke.Add(new StavkaListeZelja
        {
            PosetilacId = posetilacId,
            KnjigaId = knjigaId
        });

        _skladiste.Sacuvaj(_stavke);
    }

    public bool Ukloni(int posetilacId, int knjigaId)
    {
        var stavka = _stavke.FirstOrDefault(s =>
            s.PosetilacId == posetilacId &&
            s.KnjigaId == knjigaId);

        if (stavka == null)
        {
            return false;
        }

        _stavke.Remove(stavka);
        _skladiste.Sacuvaj(_stavke);

        return true;
    }

}
