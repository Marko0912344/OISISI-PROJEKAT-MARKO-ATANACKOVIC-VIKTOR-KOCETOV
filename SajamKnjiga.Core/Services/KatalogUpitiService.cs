using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;

namespace SajamKnjiga.Core.Services;

/// <summary>
/// Pretraga, numericko/tekstualno sortiranje i povezani upiti iz PDF-a
/// (8.19-8.21, 8.27-8.31). Nema zavisnost od Console ili WPF-a.
/// Paginacija se izvrsava tek POSLE filtriranja i sortiranja.
/// </summary>
public sealed class KatalogUpitiService
{
    public const int VelicinaStranice = 16;

    private readonly XmlDao<Autor> _autori = new("Autori.xml");
    private readonly XmlDao<Knjiga> _knjige = new("Knjige.xml");
    private readonly XmlDao<Posetilac> _posetioci = new("Posetioci.xml");
    private readonly XmlDao<Izdavac> _izdavaci = new("Izdavaci.xml");
    private readonly XmlDao<Kupovina> _kupovine = new("Kupovine.xml");
    private readonly XmlStorage<StavkaListeZelja> _zelje = new("StavkeListeZelja.xml");
    private readonly AutorKnjigaService _autorKnjiga = new();

    // Koristimo javne staticke filtre i u celom sistemu i u testovima.
    public static List<Posetilac> FiltrirajPosetioce(IEnumerable<Posetilac> posetioci, string? upit)
    {
        string[] reci = Parsiraj(upit, 3);
        if (reci.Length == 0) return posetioci.ToList();
        return posetioci.Where(p => reci.Length switch
        {
            1 => Sadrzi(p.Prezime, reci[0]),
            2 => Sadrzi(p.Prezime, reci[0]) && Sadrzi(p.Ime, reci[1]),
            3 => Sadrzi(p.BrojClanskeKarte, reci[0]) &&
                 Sadrzi(p.Ime, reci[1]) && Sadrzi(p.Prezime, reci[2]),
            _ => false
        }).ToList();
    }

    /// <summary>
    /// Pretraga autora po prezimenu ili kombinaciji prezime, ime.
    /// </summary>
    public static List<Autor> FiltrirajAutore(IEnumerable<Autor> autori, string? upit)
    {
        string[] reci = Parsiraj(upit, 2);
        if (reci.Length == 0) return autori.ToList();
        return autori.Where(a =>
            Sadrzi(a.Prezime, reci[0]) &&
            (reci.Length == 1 || Sadrzi(a.Ime, reci[1]))).ToList();
    }

    /// <summary>
    /// Pretraga knjige po nazivu ili ISBN-u, neosetljiva na velicinu slova.
    /// </summary>
    public static List<Knjiga> FiltrirajKnjige(IEnumerable<Knjiga> knjige, string? upit)
    {
        if (string.IsNullOrWhiteSpace(upit)) return knjige.ToList();
        string kriterijum = upit.Trim();
        return knjige.Where(k =>
            Sadrzi(k.Naziv, kriterijum) || Sadrzi(k.ISBN, kriterijum)).ToList();
    }

    private static bool Sadrzi(string? izvor, string deo) =>
        (izvor ?? "").Contains(deo, StringComparison.OrdinalIgnoreCase);

    // Razdvoji upit po zarezima i odbij suvisne/prazne kriterijume.
    private static string[] Parsiraj(string? upit, int maksimum)
    {
        if (string.IsNullOrWhiteSpace(upit)) return Array.Empty<string>();
        string[] reci = upit.Split(',', StringSplitOptions.TrimEntries);
        if (reci.Length > maksimum || reci.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"Neispravna pretraga: unesi od 1 do {maksimum} dela razdvojena zarezom.");
        return reci;
    }

    private static string Polje(string? tekst) => (tekst ?? "").Trim().ToLowerInvariant();

    private static IOrderedEnumerable<T> Tekst<T>(IEnumerable<T> izvor, Func<T, string> kljuc, bool opadajuce) =>
        opadajuce
            ? izvor.OrderByDescending(kljuc, StringComparer.CurrentCultureIgnoreCase)
            : izvor.OrderBy(kljuc, StringComparer.CurrentCultureIgnoreCase);

    private static IOrderedEnumerable<T> Broj<T, TBroj>(IEnumerable<T> izvor, Func<T, TBroj> kljuc, bool opadajuce) =>
        opadajuce ? izvor.OrderByDescending(kljuc) : izvor.OrderBy(kljuc);

    /// <summary>
    /// Sortiranje posetilaca po sest trazenih atributa u oba smera.
    /// </summary>
    public static List<Posetilac> SortirajPosetioce(IEnumerable<Posetilac> posetioci, string polje, bool opadajuce)
    {
        IOrderedEnumerable<Posetilac> sortirano = Polje(polje) switch
        {
            "karta" or "clanskakarta" => Tekst(posetioci, p => p.BrojClanskeKarte, opadajuce),
            "ime" => Tekst(posetioci, p => p.Ime, opadajuce),
            "prezime" => Tekst(posetioci, p => p.Prezime, opadajuce),
            "godina" or "clanstvo" => Broj(posetioci, p => p.TrenutnaGodinaClanstva, opadajuce),
            "status" => Broj(posetioci, p => p.Status, opadajuce),
            "ocena" or "prosek" => Broj(posetioci, p => p.ProsecnaOcenaRecenzija, opadajuce),
            _ => throw new ArgumentException("Sortiranje posetilaca: karta, ime, prezime, godina, status, ocena.")
        };
        // Sekundarno sortiranje po ID-ju daje stabilan redosled kod jednakih vrednosti.
        return sortirano.ThenBy(p => p.Id).ToList();
    }

    /// <summary>
    /// Sortiranje autora po imenu, prezimenu ili e-mail adresi.
    /// </summary>
    public static List<Autor> SortirajAutore(IEnumerable<Autor> autori, string polje, bool opadajuce)
    {
        IOrderedEnumerable<Autor> sortirano = Polje(polje) switch
        {
            "ime" => Tekst(autori, a => a.Ime, opadajuce),
            "prezime" => Tekst(autori, a => a.Prezime, opadajuce),
            "email" or "e-mail" => Tekst(autori, a => a.Email, opadajuce),
            _ => throw new ArgumentException("Sortiranje autora: ime, prezime, email.")
        };
        return sortirano.ThenBy(a => a.Id).ToList();
    }

    /// <summary>
    /// Sortiranje knjiga po ISBN-u, nazivu, ceni, godini i zanru.
    /// </summary>
    public static List<Knjiga> SortirajKnjige(IEnumerable<Knjiga> knjige, string polje, bool opadajuce)
    {
        IOrderedEnumerable<Knjiga> sortirano = Polje(polje) switch
        {
            "isbn" => Tekst(knjige, k => k.ISBN, opadajuce),
            "naziv" => Tekst(knjige, k => k.Naziv, opadajuce),
            "cena" => Broj(knjige, k => k.Cena, opadajuce),
            "godina" => Broj(knjige, k => k.GodinaIzdanja, opadajuce),
            "zanr" => Tekst(knjige, k => k.Zanr, opadajuce),
            _ => throw new ArgumentException("Sortiranje knjiga: isbn, naziv, cena, godina, zanr.")
        };
        return sortirano.ThenBy(k => k.Id).ToList();
    }

    /// <summary>
    /// Slozi filtriranje i sortiranje posetilaca.
    /// </summary>
    public List<Posetilac> Posetioci(string? pretraga, string sortPolje, bool opadajuce) =>
        SortirajPosetioce(FiltrirajPosetioce(_posetioci.UcitajSve(), pretraga), sortPolje, opadajuce);

    /// <summary>
    /// Slozi filtriranje i sortiranje autora.
    /// </summary>
    public List<Autor> Autori(string? pretraga, string sortPolje, bool opadajuce) =>
        SortirajAutore(FiltrirajAutore(_autorKnjiga.UcitajPovezanePodatke().Autori, pretraga), sortPolje, opadajuce);

    /// <summary>
    /// Slozi filtriranje i sortiranje knjiga.
    /// </summary>
    public List<Knjiga> Knjige(string? pretraga, string sortPolje, bool opadajuce) =>
        SortirajKnjige(FiltrirajKnjige(_autorKnjiga.UcitajPovezanePodatke().Knjige, pretraga), sortPolje, opadajuce);

    /// <summary>
    /// Izracunaj broj strana, ukljucujuci jednu praznu pocetnu stranu.
    /// </summary>
    public static int BrojStrana(int brojElemenata, int poStrani = VelicinaStranice)
    {
        if (poStrani <= 0) throw new ArgumentOutOfRangeException(nameof(poStrani));
        if (brojElemenata < 0) throw new ArgumentOutOfRangeException(nameof(brojElemenata));
        return Math.Max(1, (int)((long)brojElemenata + poStrani - 1) / poStrani);
    }

    /// <summary>
    /// Vrati samo zapise trazene strane POSLE filtriranja i sortiranja.
    /// </summary>
    public static List<T> Stranica<T>(IReadOnlyList<T> rezultati, int brojStrane, int poStrani = VelicinaStranice)
    {
        ArgumentNullException.ThrowIfNull(rezultati);
        int ukupno = BrojStrana(rezultati.Count, poStrani);
        if (brojStrane < 1 || brojStrane > ukupno)
            throw new ArgumentOutOfRangeException(nameof(brojStrane));
        // Skip preskace prethodne strane, Take uzima samo ovu stranu.
        return rezultati.Skip((brojStrane - 1) * poStrani).Take(poStrani).ToList();
    }

    /// <summary>
    /// Nadji sve knjige za jednog autora (PDF 8.21).
    /// </summary>
    public List<Knjiga> KnjigeAutora(int autorId)
    {
        ProveriAutora(autorId);
        var (_, knjige) = _autorKnjiga.UcitajPovezanePodatke();
        return knjige.Where(k => k.Autori.Any(a => a.Id == autorId))
            .OrderBy(k => k.Naziv, StringComparer.CurrentCultureIgnoreCase).ThenBy(k => k.Id).ToList();
    }

    /// <summary>
    /// Nadji jedinstvene posetioce koji zele neku knjigu autora (8.27).
    /// </summary>
    public List<Posetilac> PosetiociKojiZeleAutora(int autorId, string? pretraga = null)
    {
        var trazeneKnjige = KnjigeAutora(autorId).Select(k => k.Id).ToHashSet();
        var posetilacIds = _zelje.Ucitaj().Where(z => trazeneKnjige.Contains(z.KnjigaId))
            .Select(z => z.PosetilacId).ToHashSet();
        return SortirajPosetioce(FiltrirajPosetioce(_posetioci.UcitajSve()
            .Where(p => posetilacIds.Contains(p.Id)), pretraga), "prezime", false);
    }

    /// <summary>
    /// Nadji jedinstvene autore knjiga sa liste zelja posetioca (8.28).
    /// </summary>
    public List<Autor> AutoriKojePosetilacZeli(int posetilacId, string? pretraga = null)
    {
        ProveriPosetioca(posetilacId);
        var zeljeneKnjige = _zelje.Ucitaj().Where(z => z.PosetilacId == posetilacId)
            .Select(z => z.KnjigaId).ToHashSet();
        var (autori, knjige) = _autorKnjiga.UcitajPovezanePodatke();
        var autorIds = knjige.Where(k => zeljeneKnjige.Contains(k.Id))
            .SelectMany(k => k.Autori).Select(a => a.Id).ToHashSet();
        return SortirajAutore(FiltrirajAutore(
            autori.Where(a => autorIds.Contains(a.Id)), pretraga), "prezime", false);
    }

    public (List<Posetilac> ObeNaListiZelja, List<Posetilac> KupiliPrvuNeDrugu) UporediDveKnjige(
        int prvaKnjigaId, int drugaKnjigaId, string? pretraga = null)
    {
        if (prvaKnjigaId == drugaKnjigaId)
            throw new ArgumentException("Izaberi dve razlicite knjige.");
        ProveriKnjigu(prvaKnjigaId);
        ProveriKnjigu(drugaKnjigaId);

        var zelje = _zelje.Ucitaj();
        var obe = zelje.Where(z => z.KnjigaId == prvaKnjigaId).Select(z => z.PosetilacId).ToHashSet();
        obe.IntersectWith(zelje.Where(z => z.KnjigaId == drugaKnjigaId).Select(z => z.PosetilacId));

        var kupovine = _kupovine.UcitajSve();
        var prvuKupili = kupovine.Where(k => k.KnjigaId == prvaKnjigaId)
            .Select(k => k.PosetilacId).ToHashSet();
        prvuKupili.ExceptWith(kupovine.Where(k => k.KnjigaId == drugaKnjigaId)
            .Select(k => k.PosetilacId));

        var posetioci = _posetioci.UcitajSve();
        return (
            SortirajPosetioce(FiltrirajPosetioce(posetioci.Where(p => obe.Contains(p.Id)), pretraga), "prezime", false),
            SortirajPosetioce(FiltrirajPosetioce(posetioci.Where(p => prvuKupili.Contains(p.Id)), pretraga), "prezime", false)
        );
    }

    /// <summary>
    /// Sve knjige koje je izdao izabrani izdavac (8.30).
    /// </summary>
    public List<Knjiga> KnjigeIzdavaca(int izdavacId)
    {
        ProveriIzdavaca(izdavacId);
        var (_, knjige) = _autorKnjiga.UcitajPovezanePodatke();
        return knjige.Where(k => k.IzdavacId == izdavacId)
            .OrderBy(k => k.Naziv, StringComparer.CurrentCultureIgnoreCase).ThenBy(k => k.Id).ToList();
    }

    /// <summary>
    /// Jedinstveni autori knjiga izabranog izdavaca (8.31).
    /// </summary>
    public List<Autor> AutoriIzdavaca(int izdavacId, string? pretraga = null)
    {
        var ids = KnjigeIzdavaca(izdavacId).SelectMany(k => k.Autori)
            .Select(a => a.Id).ToHashSet();
        var (autori, _) = _autorKnjiga.UcitajPovezanePodatke();
        return SortirajAutore(FiltrirajAutore(
            autori.Where(a => ids.Contains(a.Id)), pretraga), "prezime", false);
    }

    // Null ukupna potrosnja znaci da bar jedna stara kupovina nema
    // istorijsku cenu; prikazivanje danasnje cene kao istorijske je pogresno.
    public (List<Kupovina> Kupovine, decimal? UkupnaPotrosnja, double ProsecnaOcena)
        KupljeneKnjige(int posetilacId)
    {
        ProveriPosetioca(posetilacId);
        var kupovine = _kupovine.UcitajSve().Where(k => k.PosetilacId == posetilacId)
            .OrderBy(k => k.DatumKupovine).ThenBy(k => k.Id).ToList();
        decimal? ukupno = kupovine.All(k => k.CenaPriKupovini.HasValue)
            ? kupovine.Sum(k => k.CenaPriKupovini!.Value)
            : null;
        double prosek = kupovine.Count == 0 ? 0d : kupovine.Average(k => k.Ocena);
        return (kupovine, ukupno, prosek);
    }

    // Proveri postojanje izabranog autora pre upita.
    private void ProveriAutora(int id)
    {
        if (_autori.PronadjiPoId(id) == null) throw new InvalidOperationException("Autor ne postoji.");
    }

    // Proveri postojanje izabranog posetioca pre upita.
    private void ProveriPosetioca(int id)
    {
        if (_posetioci.PronadjiPoId(id) == null) throw new InvalidOperationException("Posetilac ne postoji.");
    }

    // Proveri postojanje izabrane knjige pre upita.
    private void ProveriKnjigu(int id)
    {
        if (_knjige.PronadjiPoId(id) == null) throw new InvalidOperationException("Knjiga ne postoji.");
    }

    // Proveri postojanje izabranog izdavaca pre upita.
    private void ProveriIzdavaca(int id)
    {
        if (_izdavaci.PronadjiPoId(id) == null) throw new InvalidOperationException("Izdavac ne postoji.");
    }
}
