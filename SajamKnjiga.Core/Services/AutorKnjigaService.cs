using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services;

/// <summary>
/// Cuvanje i ucitavanje M:N veza autora i knjiga.
/// </summary>
public class AutorKnjigaService
{
    private readonly XmlStorage<AutorKnjiga> _veze = new("AutorKnjiga.xml");
    private readonly XmlDao<Autor> _autori = new("Autori.xml");
    private readonly XmlDao<Knjiga> _knjige = new("Knjige.xml");
    private readonly XmlDao<Adresa> _adrese = new("Adrese.xml");

    /// <summary>
    /// Dodaj M:N vezu samo ako oba entiteta postoje i veza nije duplirana.
    /// </summary>
    public bool DodajAutoraNaKnjigu(int autorId, int knjigaId)
    {
        if (_autori.PronadjiPoId(autorId) == null)
            throw new InvalidOperationException("Autor ne postoji.");

        if (_knjige.PronadjiPoId(knjigaId) == null)
            throw new InvalidOperationException("Knjiga ne postoji.");

        var veze = _veze.Ucitaj();
        if (veze.Any(v => v.AutorId == autorId && v.KnjigaId == knjigaId))
            return false;

        veze.Add(new AutorKnjiga { AutorId = autorId, KnjigaId = knjigaId });
        _veze.Sacuvaj(veze);
        return true;
    }

    /// <summary>
    /// Rekonstruisi navigacione kolekcije autora i knjiga iz ID veza i adresa.
    /// </summary>
    public (List<Autor> Autori, List<Knjiga> Knjige) UcitajPovezanePodatke()
    {
        var autori = _autori.UcitajSve();
        var knjige = _knjige.UcitajSve();
        var veze = _veze.Ucitaj();

        var adrese = _adrese.UcitajSve().ToDictionary(x => x.Id);
        var mapaA = autori.ToDictionary(x => x.Id);
        var mapaK = knjige.ToDictionary(x => x.Id);

        foreach (var a in autori)
            if (adrese.TryGetValue(a.AdresaId, out var adresa)) a.Adresa = adresa;

        // Svaki zapis veze povezuje postojece objekte u obe navigacione liste.
        foreach (var v in veze)
        {
            if (!mapaA.TryGetValue(v.AutorId, out var a) ||
                !mapaK.TryGetValue(v.KnjigaId, out var k))
                throw new InvalidOperationException("Veza upucuje na nepostojeci entitet.");

            if (!a.Knjige.Any(x => x.Id == k.Id)) a.Knjige.Add(k);
            if (!k.Autori.Any(x => x.Id == a.Id)) k.Autori.Add(a);
        }

        return (autori, knjige);
    }
}
