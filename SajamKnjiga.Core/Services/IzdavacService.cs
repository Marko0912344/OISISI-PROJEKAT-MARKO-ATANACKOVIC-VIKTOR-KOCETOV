using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Services;

/// <summary>
/// Upravljanje knjigama izdavaca, njihovim autorima i sefom.
/// </summary>
public class IzdavacService
{
    private readonly XmlDao<Autor> _autori = new("Autori.xml");
    private readonly XmlDao<Izdavac> _izdavaci = new("Izdavaci.xml");
    private readonly XmlDao<Knjiga> _knjige = new("Knjige.xml");
    private readonly AutorKnjigaService _veze = new();

    /// <summary>
    /// Promeni izdavaca knjige uz pravila koja stite postojece veze.
    /// </summary>
    public bool DodeliKnjiguIzdavacu(int knjigaId, int izdavacId)
    {
        var k = _knjige.PronadjiPoId(knjigaId) ??
            throw new InvalidOperationException("Knjiga ne postoji.");

        if (_izdavaci.PronadjiPoId(izdavacId) == null)
            throw new InvalidOperationException("Izdavac ne postoji.");

        if (k.IzdavacId == izdavacId) return false;
        k.IzdavacId = izdavacId;
        return new KnjigaPravilaService().IzmeniKnjigu(k);
    }

    /// <summary>
    /// Sef mora imati >= 5 godina iskustva i knjigu kod odabranog izdavaca.
    /// </summary>
    public bool PostaviSefaIzdavaca(int izdavacId, int autorId)
    {
        var izdavac = _izdavaci.PronadjiPoId(izdavacId) ??
            throw new InvalidOperationException("Izdavac ne postoji.");
        var autor = _autori.PronadjiPoId(autorId) ??
            throw new InvalidOperationException("Autor ne postoji.");

        if (autor.GodineIskustva < 5)
            throw new InvalidOperationException("Sef mora imati najmanje 5 godina iskustva.");

        var (_, knjige) = _veze.UcitajPovezanePodatke();
        // Zahtev PDF 8.26: sef mora zaista imati knjigu kod tog izdavaca.
        if (!knjige.Any(k => k.IzdavacId == izdavacId &&
            k.Autori.Any(a => a.Id == autorId)))
            throw new InvalidOperationException("Sef mora biti autor knjige kod tog izdavaca.");

        if (izdavac.SefAutorId == autorId) return false;
        izdavac.SefAutorId = autorId;
        return _izdavaci.Izmeni(izdavac);
    }

    /// <summary>
    /// Rekonstruisi knjige, autore i sefa izdavaca preko ID veza.
    /// </summary>
    public List<Izdavac> UcitajPovezanePodatke()
    {
        var izdavaci = _izdavaci.UcitajSve();
        var (autori, knjige) = _veze.UcitajPovezanePodatke();
        var mapaA = autori.ToDictionary(a => a.Id);

        foreach (var i in izdavaci)
        {
            i.Knjige = knjige.Where(k => k.IzdavacId == i.Id).ToList();
            i.Autori = i.Knjige.SelectMany(k => k.Autori).DistinctBy(a => a.Id).ToList();

            if (i.SefAutorId is int id && mapaA.TryGetValue(id, out var a))
                i.SefAutor = a;

            foreach (var k in i.Knjige) k.Izdavac = i;
        }

        return izdavaci;
    }
}
