using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.DAO;

// Zajednicki genericki DAO. Koristiti u servisu unutar XmlTransakcija
// kada operacija menja vise datoteka (ukljucujuci IdBrojaci.xml).
public class XmlDao<T> where T : class, IEntitet
{
    private readonly XmlStorage<T> _storage;
    private readonly XmlStorage<IdBrojac> _brojaci = new("IdBrojaci.xml");

    public XmlDao(string fileName) => _storage = new XmlStorage<T>(fileName);
    public List<T> UcitajSve() => _storage.Ucitaj();
    public T? PronadjiPoId(int id) => _storage.Ucitaj().FirstOrDefault(x => x.Id == id);

    public T Dodaj(T entitet)
    {
        ArgumentNullException.ThrowIfNull(entitet);
        if (entitet.Id != 0) throw new InvalidOperationException("Novi zapis mora imati Id=0.");
        List<T> lista = _storage.Ucitaj();
        List<IdBrojac> brojaci = _brojaci.Ucitaj();
        string tip = typeof(T).Name;
        IdBrojac? brojac = brojaci.FirstOrDefault(b => b.TipEntiteta == tip);
        if (brojac == null)
        {
            brojac = new IdBrojac { TipEntiteta = tip };
            brojaci.Add(brojac);
        }
        int maxPostojeci = lista.Count == 0 ? 0 : lista.Max(x => x.Id);
        int id = Math.Max(maxPostojeci + 1, brojac.SledeciId);
        if (id == int.MaxValue) throw new InvalidOperationException("ID brojac je iscrpljen.");
        entitet.Id = id;
        brojac.SledeciId = id + 1;
        // DAO ne moze sam da zastiti oba upisa od prekida; servis koristi
        // XmlTransakcija.Izvrsi sa obe datoteke.
        lista.Add(entitet);
        _storage.Sacuvaj(lista);
        _brojaci.Sacuvaj(brojaci);
        return entitet;
    }

    public bool Izmeni(T entitet)
    {
        ArgumentNullException.ThrowIfNull(entitet);
        var lista = _storage.Ucitaj();
        int index = lista.FindIndex(x => x.Id == entitet.Id);
        if (index < 0) return false;
        lista[index] = entitet;
        _storage.Sacuvaj(lista);
        return true;
    }

    public bool Obrisi(int id)
    {
        var lista = _storage.Ucitaj();
        if (lista.RemoveAll(x => x.Id == id) == 0) return false;
        _storage.Sacuvaj(lista);
        return true;
    }
}
