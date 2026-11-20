using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.DAO;

/// <summary>
/// Genericki DAO: osnovne CRUD operacije nad entitetima koji imaju Id.
/// </summary>
public class XmlDao<T> where T : class, IEntitet
{
    private readonly XmlStorage<T> _storage;
    private readonly XmlStorage<IdBrojac> _ids = new("IdBrojaci.xml");
    private static readonly object Sync = new();

    public XmlDao(string fileName) => _storage = new XmlStorage<T>(fileName);

    /// <summary>
    /// Ucitaj sve zapise ovog tipa iz odgovarajuce XML datoteke.
    /// </summary>
    public List<T> UcitajSve() => _storage.Ucitaj();

    /// <summary>
    /// Pronadji entitet po Id-u; null znaci da zapis ne postoji.
    /// </summary>
    public T? PronadjiPoId(int id) =>
        UcitajSve().FirstOrDefault(x => x.Id == id);

    /// <summary>
    /// Dodeli novi ID, sacuvaj entitet i azuriraj trajni brojac.
    /// </summary>
    public T Dodaj(T entitet)
    {
        ArgumentNullException.ThrowIfNull(entitet);

        // Kriticna sekcija sprecava preplitanje ID dodele unutar ovog procesa.
        lock (Sync)
        {
            var lista = _storage.Ucitaj();
            var brojaci = _ids.Ucitaj();
            string tip = typeof(T).Name;

            var brojac = brojaci.FirstOrDefault(b => b.TipEntiteta == tip);
            if (brojac == null)
            {
                brojac = new IdBrojac { TipEntiteta = tip, SledeciId = 1 };
                brojaci.Add(brojac);
            }

            int najveca = lista.Count == 0 ? 0 : lista.Max(x => x.Id);
            // Uzimamo vecu vrednost: sledeci ID iz brojaca ili broj posle maksimuma.
            int id = Math.Max(najveca + 1, brojac.SledeciId);

            if (entitet.Id != 0 && entitet.Id != id)
                throw new InvalidOperationException("ID se dodeljuje automatski.");

            entitet.Id = id;
            brojac.SledeciId = id + 1;

            lista.Add(entitet);
            _storage.Sacuvaj(lista);
            // Brojac se cuva posle entiteta; vise-fajlnu atomarnost obezbedjuje pozivajuca transakcija.
            _ids.Sacuvaj(brojaci);
            return entitet;
        }
    }

    /// <summary>
    /// Zameni postojeci zapis sa istim ID-jem; false ako ga nema.
    /// </summary>
    public bool Izmeni(T entitet)
    {
        ArgumentNullException.ThrowIfNull(entitet);

        lock (Sync)
        {
            var lista = _storage.Ucitaj();
            int index = lista.FindIndex(x => x.Id == entitet.Id);
            if (index < 0) return false;

            lista[index] = entitet;
            _storage.Sacuvaj(lista);
            return true;
        }
    }

    /// <summary>
    /// Obrisi zapis po ID-ju; provere poslovnih zavisnosti su u servisima.
    /// </summary>
    public bool Obrisi(int id)
    {
        lock (Sync)
        {
            var lista = _storage.Ucitaj();
            if (lista.RemoveAll(x => x.Id == id) == 0) return false;

            _storage.Sacuvaj(lista);
            return true;
        }
    }
}
