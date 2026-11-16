
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.Data;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Services;

public class AutorServis
{
    private readonly XmlSkladiste<Autor> _skladiste;
    private readonly List<Autor> _autori;

    public AutorServis(string putanja)
    {
        _skladiste = new XmlSkladiste<Autor>(putanja);
        _autori = _skladiste.Ucitaj();
    }

    public List<Autor> PrikaziSve()
    {
        return new List<Autor>(_autori);
    }

    public Autor? PronadjiPoId(int id)
    {
        return _autori.FirstOrDefault(a => a.Id == id);
    }

    public bool MozeBitiSef(int id)
    {
        Autor? autor = PronadjiPoId(id);

        return autor != null && autor.GodineIskustva >= 5;
    }

    public void Dodaj(Autor autor)
    {
        if (_autori.Any(a => a.Id == autor.Id))
        {
            throw new System.InvalidOperationException(
                "Autor sa ovim ID-jem vec postoji.");
        }

        if (autor.GodineIskustva < 0)
        {
            throw new System.InvalidOperationException(
                "Godine iskustva ne mogu biti negativne.");
        }

        _autori.Add(autor);
        _skladiste.Sacuvaj(_autori);
    }


}
