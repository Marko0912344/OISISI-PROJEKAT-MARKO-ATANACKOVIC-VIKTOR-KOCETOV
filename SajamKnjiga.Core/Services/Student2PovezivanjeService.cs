using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services;

/// <summary>
/// U jednom prolazu rekonstruiše veze autora, adresa i knjiga iz ID-jeva.
/// Navigacione kolekcije su XmlIgnore, pa nema kruzne serijalizacije.
/// Podaci o posetiocima se iznose kao ID-jevi dok kolegin model nije spojen.
/// </summary>
internal static class Student2PovezivanjeService
{
    public static (List<Autor> Autori, List<Knjiga> Knjige) Ucitaj()
    {
        var autori = new XmlStorage<Autor>("Autori.xml").Ucitaj();
        var knjige = new XmlStorage<Knjiga>("Knjige.xml").Ucitaj();
        var adrese = new XmlStorage<Adresa>("Adrese.xml").Ucitaj();
        var veze = new XmlStorage<AutorKnjiga>("AutorKnjiga.xml").Ucitaj();
        var zelje = new XmlStorage<StavkaListeZelja>("StavkeListeZelja.xml").Ucitaj();
        var kupovine = CitacVeza.Kupovine();

        var autoriId = autori.ToDictionary(a => a.Id);
        var knjigeId = knjige.ToDictionary(k => k.Id);
        var adreseId = adrese.ToDictionary(a => a.Id);

        foreach (var a in autori)
        {
            a.Knjige = new List<Knjiga>();
            a.Adresa = adreseId.GetValueOrDefault(a.AdresaId);
        }
        foreach (var k in knjige)
        {
            k.Autori = new List<Autor>();
            k.IdKupaca = new List<int>();
            k.IdPosetilacaSaZeljom = new List<int>();
        }
        foreach (var veza in veze.DistinctBy(v => (v.AutorId, v.KnjigaId)))
        {
            if (autoriId.TryGetValue(veza.AutorId, out var a) &&
                knjigeId.TryGetValue(veza.KnjigaId, out var k))
            {
                a.Knjige.Add(k);
                k.Autori.Add(a);
            }
        }
        foreach (var grupa in zelje.GroupBy(z => z.KnjigaId))
        {
            if (knjigeId.TryGetValue(grupa.Key, out var k))
                k.IdPosetilacaSaZeljom = grupa.Select(z => z.PosetilacId)
                    .Where(id => id > 0).Distinct().ToList();
        }
        foreach (var grupa in kupovine.GroupBy(z => z.KnjigaId))
        {
            if (knjigeId.TryGetValue(grupa.Key, out var k))
                k.IdKupaca = grupa.Select(z => z.PosetilacId)
                    .Where(id => id > 0).Distinct().ToList();
        }
        return (autori, knjige);
    }
}
