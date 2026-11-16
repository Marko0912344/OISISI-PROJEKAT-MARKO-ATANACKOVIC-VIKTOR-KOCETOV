
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

    public bool PostojiKnjiga(int id)
    {
        return _knjige.Any(k => k.Id == id);
    }
}
