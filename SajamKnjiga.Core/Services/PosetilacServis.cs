
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.Data;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Services;

public class PosetilacServis
{
    private readonly XmlSkladiste<Posetilac> _skladiste;
    private readonly List<Posetilac> _posetioci;

    public PosetilacServis(string putanja)
    {
        _skladiste = new XmlSkladiste<Posetilac>(putanja);
        _posetioci = _skladiste.Ucitaj();
    }

    public List<Posetilac> PrikaziSve()
    {
        return new List<Posetilac>(_posetioci);
    }

    public void Dodaj(Posetilac posetilac)
    {
        if (_posetioci.Any(p => p.Id == posetilac.Id))
        {
            throw new System.InvalidOperationException(
                "Posetilac sa ovim ID-jem vec postoji.");
        }

        _posetioci.Add(posetilac);
        _skladiste.Sacuvaj(_posetioci);
    }

    public bool Obrisi(int id)
    {
        Posetilac? posetilac =
            _posetioci.FirstOrDefault(p => p.Id == id);

        if (posetilac == null)
            return false;

        _posetioci.Remove(posetilac);
        _skladiste.Sacuvaj(_posetioci);

        return true;
    }

    public bool Izmeni(Posetilac izmenjeni)
    {
        Posetilac? postojeci =
            _posetioci.FirstOrDefault(
                p => p.Id == izmenjeni.Id);

        if (postojeci == null)
            return false;

        int indeks = _posetioci.IndexOf(postojeci);
        _posetioci[indeks] = izmenjeni;

        _skladiste.Sacuvaj(_posetioci);

        return true;
    }
}
