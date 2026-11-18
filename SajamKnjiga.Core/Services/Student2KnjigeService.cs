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

    // Citanje vraca i povezane autore i ID-jeve zainteresovanih/kupaca.
    public List<Knjiga> Sve() => Student2PovezivanjeService.Ucitaj().Knjige;
    public Knjiga? Nadji(int id) => Sve().FirstOrDefault(k => k.Id == id);

    public List<Knjiga> Pretrazi(string upit) =>
        Filtriraj(Sve(), upit, zahtevajKriterijum: true);

    public List<Knjiga> Sortiraj(string polje, bool opadajuce) =>
        SortirajListu(Sve(), polje, opadajuce);

    // Kombinacija iz odeljka 8.20: prvo pretraga, zatim sortiranje;
    // paginacija se primenjuje na dobijenu listu u konzolnom meniju.
    public List<Knjiga> Upit(string? pretraga, string polje, bool opadajuce) =>
        SortirajListu(Filtriraj(Sve(), pretraga, false), polje, opadajuce);

    public static List<Knjiga> Filtriraj(IEnumerable<Knjiga> knjige,
        string? upit, bool zahtevajKriterijum = false)
    {
        if (string.IsNullOrWhiteSpace(upit))
        {
            if (zahtevajKriterijum) throw new ArgumentException("Unesi kriterijum.");
            return knjige.ToList();
        }
        string tekst = upit.Trim();
        return knjige.Where(k =>
            k.Naziv.Contains(tekst, StringComparison.OrdinalIgnoreCase) ||
            k.ISBN.Contains(tekst, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public static List<Knjiga> SortirajListu(IEnumerable<Knjiga> knjige,
        string polje, bool opadajuce)
    {
        IEnumerable<Knjiga> q = knjige;
        return (polje ?? "").Trim().ToLowerInvariant() switch
        {
            "isbn" => SortText(q, k => k.ISBN, opadajuce),
            "naziv" => SortText(q, k => k.Naziv, opadajuce),
            "zanr" => SortText(q, k => k.Zanr, opadajuce),
            "cena" => (opadajuce ? q.OrderByDescending(k => k.Cena) : q.OrderBy(k => k.Cena)).ToList(),
            "godina" => (opadajuce ? q.OrderByDescending(k => k.GodinaIzdanja) : q.OrderBy(k => k.GodinaIzdanja)).ToList(),
            _ => throw new ArgumentException("Polje: isbn, naziv, cena, godina ili zanr.")
        };
    }

    private static List<Knjiga> SortText(IEnumerable<Knjiga> q,
        Func<Knjiga, string> kljuc, bool opadajuce) =>
        (opadajuce
            ? q.OrderByDescending(kljuc, StringComparer.CurrentCultureIgnoreCase)
            : q.OrderBy(kljuc, StringComparer.CurrentCultureIgnoreCase)).ToList();

    public bool Obrisi(int id) =>
        XmlTransakcija.Izvrsi(new[] { "Knjige.xml", "AutorKnjiga.xml" }, () =>
        {
            Knjiga? knjiga = _knjige.PronadjiPoId(id);
            if (knjiga is null) return false;
            if (_zelje.Ucitaj().Any(z => z.KnjigaId == id) || CitacVeza.KnjigaKupljena(id))
                throw new InvalidOperationException("Knjiga je na listi zelja ili je kupljena.");

            var veze = _veze.Ucitaj();
            ProveriSefaNakonUklanjanjaVeza(knjiga, veze,
                veze.Where(v => v.KnjigaId == id).Select(v => v.AutorId).Distinct());

            veze.RemoveAll(x => x.KnjigaId == id);
            _veze.Sacuvaj(veze);
            return _knjige.Obrisi(id);
        });

    public bool DodajAutora(int autorId, int knjigaId)
    {
        if (_autori.PronadjiPoId(autorId) is null || _knjige.PronadjiPoId(knjigaId) is null)
            throw new InvalidOperationException("Autor ili knjiga ne postoji.");
        var veze = _veze.Ucitaj();
        if (veze.Any(v => v.AutorId == autorId && v.KnjigaId == knjigaId)) return false;
        veze.Add(new AutorKnjiga { AutorId = autorId, KnjigaId = knjigaId });
        _veze.Sacuvaj(veze);
        return true;
    }

    public bool UkloniAutora(int autorId, int knjigaId)
    {
        Knjiga knjiga = _knjige.PronadjiPoId(knjigaId) ??
            throw new InvalidOperationException("Knjiga ne postoji.");
        var veze = _veze.Ucitaj();
        if (!veze.Any(v => v.AutorId == autorId && v.KnjigaId == knjigaId)) return false;

        // Specifikacija 8.25 dozvoljava da polje autora bude prazno
        // posle uklanjanja. Ne namecemo zabranu uklanjanja poslednjeg autora.
        // Ali cuvamo uslov da sef izdavaca ima drugu knjigu kod tog izdavaca.
        ProveriSefaNakonUklanjanjaVeza(knjiga, veze, new[] { autorId });
        veze.RemoveAll(v => v.AutorId == autorId && v.KnjigaId == knjigaId);
        _veze.Sacuvaj(veze);
        return true;
    }

    private void ProveriSefaNakonUklanjanjaVeza(Knjiga knjiga,
        List<AutorKnjiga> veze, IEnumerable<int> uklonjeniAutori)
    {
        if (knjiga.IzdavacId is not int izdavacId) return;
        var sveKnjige = _knjige.UcitajSve();
        foreach (int autorId in uklonjeniAutori)
        {
            if (!CitacVeza.SefIzdavacaKod(izdavacId, autorId)) continue;
            bool imaDruguKnjigu = veze.Any(v =>
                v.AutorId == autorId && v.KnjigaId != knjiga.Id &&
                sveKnjige.Any(k => k.Id == v.KnjigaId && k.IzdavacId == izdavacId));
            if (!imaDruguKnjigu)
                throw new InvalidOperationException(
                    "Sef izdavaca mora ostati autor bar jedne knjige kod tog izdavaca.");
        }
    }

    public List<Knjiga> KnjigeIzdavaca(int izdavacId) =>
        Sve().Where(k => k.IzdavacId == izdavacId).ToList();

    public List<Autor> AutoriIzdavaca(int izdavacId) =>
        FiltrirajAutoreIzGrupe(AutoriIzdavacaSvi(izdavacId), null);

    public List<Autor> PretraziAutoreIzdavaca(int izdavacId, string? upit) =>
        FiltrirajAutoreIzGrupe(AutoriIzdavacaSvi(izdavacId), upit);

    private List<Autor> AutoriIzdavacaSvi(int izdavacId)
    {
        var ids = KnjigeIzdavaca(izdavacId).Select(k => k.Id).ToHashSet();
        var autorIds = _veze.Ucitaj().Where(v => ids.Contains(v.KnjigaId))
            .Select(v => v.AutorId).ToHashSet();
        return Student2PovezivanjeService.Ucitaj().Autori
            .Where(a => autorIds.Contains(a.Id)).ToList();
    }

    public List<Knjiga> ListaZelja(int posetilacId)
    {
        if (!CitacVeza.PostojiPosetilac(posetilacId))
            throw new InvalidOperationException("Posetilac ne postoji.");
        var ids = _zelje.Ucitaj().Where(x => x.PosetilacId == posetilacId)
            .Select(x => x.KnjigaId).ToHashSet();
        return Sve().Where(k => ids.Contains(k.Id)).ToList();
    }

    public bool UkloniSaListeZelja(int posetilacId, int knjigaId)
    {
        if (!CitacVeza.PostojiPosetilac(posetilacId))
            throw new InvalidOperationException("Posetilac ne postoji.");
        var lista = _zelje.Ucitaj();
        if (lista.RemoveAll(x => x.PosetilacId == posetilacId && x.KnjigaId == knjigaId) == 0)
            return false;
        _zelje.Sacuvaj(lista);
        return true;
    }

    public List<Autor> AutoriZaZelje(int posetilacId) =>
        PretraziAutoreZaZelje(posetilacId, null);

    public List<Autor> PretraziAutoreZaZelje(int posetilacId, string? upit)
    {
        var knjigaIds = ListaZelja(posetilacId).Select(k => k.Id).ToHashSet();
        var autorIds = _veze.Ucitaj().Where(v => knjigaIds.Contains(v.KnjigaId))
            .Select(v => v.AutorId).ToHashSet();
        return FiltrirajAutoreIzGrupe(
            Student2PovezivanjeService.Ucitaj().Autori
                .Where(a => autorIds.Contains(a.Id)), upit);
    }

    private static List<Autor> FiltrirajAutoreIzGrupe(IEnumerable<Autor> autori, string? upit) =>
        Student2AutoriService.Filtriraj(autori, upit, zahtevajKriterijum: false);
}
