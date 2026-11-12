using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services;

public class Student2KnjigeService
{
    private readonly XmlDao<Knjiga> _knjige = new("Knjige.xml");
    private readonly XmlDao<Autor> _autori = new("Autori.xml");
    private readonly XmlStorage<AutorKnjiga> _veze = new("AutorKnjiga.xml");
    private readonly XmlStorage<StavkaListeZelja> _zelje = new("StavkeListeZelja.xml");

    public List<Knjiga> Sve() => _knjige.UcitajSve();
    public Knjiga? Nadji(int id) => _knjige.PronadjiPoId(id);

    public List<Knjiga> Pretrazi(string upit)
    {
        if (string.IsNullOrWhiteSpace(upit)) throw new ArgumentException("Unesi kriterijum.");
        return Sve().Where(k => k.Naziv.Contains(upit, StringComparison.OrdinalIgnoreCase) ||
            k.ISBN.Contains(upit, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public List<Knjiga> Sortiraj(string polje, bool opadajuce)
    {
        IEnumerable<Knjiga> q = Sve();
        return polje.ToLowerInvariant() switch
        {
            "isbn" => SortText(q, k => k.ISBN, opadajuce),
            "naziv" => SortText(q, k => k.Naziv, opadajuce),
            "zanr" => SortText(q, k => k.Zanr, opadajuce),
            "cena" => (opadajuce ? q.OrderByDescending(k => k.Cena) : q.OrderBy(k => k.Cena)).ToList(),
            "godina" => (opadajuce ? q.OrderByDescending(k => k.GodinaIzdanja) : q.OrderBy(k => k.GodinaIzdanja)).ToList(),
            _ => throw new ArgumentException("Polje: isbn, naziv, cena, godina ili zanr.")
        };
    }
    private static List<Knjiga> SortText(IEnumerable<Knjiga> q, Func<Knjiga, string> k, bool desc) =>
        (desc ? q.OrderByDescending(k, StringComparer.CurrentCultureIgnoreCase) :
            q.OrderBy(k, StringComparer.CurrentCultureIgnoreCase)).ToList();

    public bool Obrisi(int id) =>
        XmlTransakcija.Izvrsi(new[] { "Knjige.xml", "AutorKnjiga.xml" }, () =>
        {
            Knjiga? knjiga = Nadji(id);
            if (knjiga == null) return false;
            if (_zelje.Ucitaj().Any(z => z.KnjigaId == id) || CitacVeza.KnjigaKupljena(id))
                throw new InvalidOperationException("Knjiga ima zelje ili kupovine.");
            var veze = _veze.Ucitaj();
            if (knjiga.IzdavacId is int izdavacId &&
                veze.Any(v => v.KnjigaId == id && CitacVeza.SefIzdavaca(v.AutorId)))
            {
                // Konzervativna zabrana do integracije tacnih izdavackih podataka.
                throw new InvalidOperationException(
                    "Knjiga ima vezu sa sefom izdavaca: potrebna provera u zajednickom modelu.");
            }
            veze.RemoveAll(x => x.KnjigaId == id);
            _veze.Sacuvaj(veze);
            return _knjige.Obrisi(id);
        });

    public bool DodajAutora(int autorId, int knjigaId)
    {
        if (_autori.PronadjiPoId(autorId) == null || Nadji(knjigaId) == null)
            throw new InvalidOperationException("Autor ili knjiga ne postoji.");
        var veze = _veze.Ucitaj();
        if (veze.Any(v => v.AutorId == autorId && v.KnjigaId == knjigaId)) return false;
        veze.Add(new AutorKnjiga { AutorId = autorId, KnjigaId = knjigaId });
        _veze.Sacuvaj(veze);
        return true;
    }

    public bool UkloniAutora(int autorId, int knjigaId)
    {
        Knjiga knjiga = Nadji(knjigaId) ?? throw new InvalidOperationException("Knjiga ne postoji.");
        var veze = _veze.Ucitaj();
        if (!veze.Any(v => v.AutorId == autorId && v.KnjigaId == knjigaId)) return false;
        if (veze.Count(v => v.KnjigaId == knjigaId) <= 1)
            throw new InvalidOperationException("Knjiga mora imati bar jednog autora.");
        if (knjiga.IzdavacId.HasValue && CitacVeza.SefIzdavaca(autorId))
            throw new InvalidOperationException("Proveriti zavisnost sefa izdavaca pre uklanjanja autora.");
        veze.RemoveAll(v => v.AutorId == autorId && v.KnjigaId == knjigaId);
        _veze.Sacuvaj(veze);
        return true;
    }

    public List<Knjiga> KnjigeIzdavaca(int izdavacId) =>
        Sve().Where(k => k.IzdavacId == izdavacId).ToList();

    public List<Autor> AutoriIzdavaca(int izdavacId)
    {
        var ids = KnjigeIzdavaca(izdavacId).Select(k => k.Id).ToHashSet();
        var autorIds = _veze.Ucitaj().Where(v => ids.Contains(v.KnjigaId))
            .Select(v => v.AutorId).ToHashSet();
        return _autori.UcitajSve().Where(a => autorIds.Contains(a.Id)).ToList();
    }

    public List<Knjiga> ListaZelja(int posetilacId)
    {
        var ids = _zelje.Ucitaj().Where(x => x.PosetilacId == posetilacId)
            .Select(x => x.KnjigaId).ToHashSet();
        return Sve().Where(k => ids.Contains(k.Id)).ToList();
    }

    public bool UkloniSaListeZelja(int posetilacId, int knjigaId)
    {
        if (!CitacVeza.PostojiPosetilac(posetilacId))
            throw new InvalidOperationException("Posetilac ne postoji (ili jos nije integrisan njegov model).");
        var lista = _zelje.Ucitaj();
        if (lista.RemoveAll(x => x.PosetilacId == posetilacId && x.KnjigaId == knjigaId) == 0)
            return false;
        _zelje.Sacuvaj(lista);
        return true;
    }

    public List<Autor> AutoriZaZelje(int posetilacId)
    {
        var knjigaIds = ListaZelja(posetilacId).Select(k => k.Id).ToHashSet();
        var autorIds = _veze.Ucitaj().Where(v => knjigaIds.Contains(v.KnjigaId))
            .Select(v => v.AutorId).ToHashSet();
        return _autori.UcitajSve().Where(a => autorIds.Contains(a.Id)).ToList();
    }
}
