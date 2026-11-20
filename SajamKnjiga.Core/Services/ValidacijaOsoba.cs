using System;
using System.Text.RegularExpressions;
using SajamKnjiga.Core.Models;

namespace SajamKnjiga.Core.Services;

/// <summary>
/// Centralizovane provere licnih podataka. Poziva ih Core i pri dodavanju
/// i pri izmeni, bez obzira da li je klijent Console ili buduci WPF.
/// PDF ne propisuje strogi telefonski format ni minimalnu starost.
/// </summary>
public static class ValidacijaOsoba
{
    private static readonly Regex EmailObrazac = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>
    /// Proveri identitet, kontakt, datum rodjenja i iskustvo autora.
    /// </summary>
    public static void ProveriAutora(Autor autor)
    {
        ArgumentNullException.ThrowIfNull(autor);
        ObavezanTekst(autor.Ime, "Ime autora");
        ObavezanTekst(autor.Prezime, "Prezime autora");
        ProveriDatum(autor.DatumRodjenja);
        ObavezanTekst(autor.Telefon, "Telefon autora");
        ProveriEmail(autor.Email);
        ObavezanTekst(autor.BrojLicneKarte, "Broj licne karte");
        if (autor.GodineIskustva < 0)
            throw new InvalidOperationException("Godine iskustva ne mogu biti negativne.");
    }

    /// <summary>
    /// Proveri identitet, kontakt, clanstvo i dozvoljeni enum status.
    /// </summary>
    public static void ProveriPosetioca(Posetilac posetilac)
    {
        ArgumentNullException.ThrowIfNull(posetilac);
        ObavezanTekst(posetilac.Ime, "Ime posetioca");
        ObavezanTekst(posetilac.Prezime, "Prezime posetioca");
        ProveriDatum(posetilac.DatumRodjenja);
        ObavezanTekst(posetilac.Telefon, "Telefon posetioca");
        ProveriEmail(posetilac.Email);
        ObavezanTekst(posetilac.BrojClanskeKarte, "Broj clanske karte");
        if (posetilac.TrenutnaGodinaClanstva < 1900 ||
            posetilac.TrenutnaGodinaClanstva > DateTime.Today.Year)
            throw new InvalidOperationException("Godina clanstva ne sme biti u buducnosti.");
        if (!Enum.IsDefined(typeof(StatusPosetioca), posetilac.Status))
            throw new InvalidOperationException("Status mora biti R ili V.");
    }

    // Obavezno tekstualno polje ne sme biti prazno.
    private static void ObavezanTekst(string? vrednost, string naziv)
    {
        if (string.IsNullOrWhiteSpace(vrednost))
            throw new InvalidOperationException($"Polje '{naziv}' je obavezno.");
    }

    // Datum rodjenja mora biti popunjen i ne sme biti u buducnosti.
    private static void ProveriDatum(DateTime datum)
    {
        if (datum == default || datum.Date > DateTime.Today)
            throw new InvalidOperationException("Datum rodjenja nije ispravan.");
    }

    // Proveri osnovni format e-mail adrese.
    private static void ProveriEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !EmailObrazac.IsMatch(email.Trim()))
            throw new InvalidOperationException("Email adresa nije ispravna.");
    }
}
