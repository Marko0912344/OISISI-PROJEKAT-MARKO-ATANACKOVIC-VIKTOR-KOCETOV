using System;
using System.Linq;

namespace SajamKnjiga.Core.Services;

/// <summary>
/// ISBN-10 (modulo 11, zavrsno X) i ISBN-13 (EAN, modulo 10).
/// Razmaci i crtice su dozvoljeni pri unosu; u XML cuvamo kanonski ISBN.
/// </summary>
public static class IsbnPravila
{
    /// <summary>
    /// Ukloni razmake/crtice i prihvati samo validan ISBN-10 ili ISBN-13.
    /// </summary>
    public static string Normalizuj(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new InvalidOperationException("ISBN je obavezan.");
        string cist = new string(isbn.Where(c => c != '-' && !char.IsWhiteSpace(c))
            .ToArray()).ToUpperInvariant();
        bool validan = cist.Length switch
        {
            10 => ProveriIsbn10(cist),
            13 => ProveriIsbn13(cist),
            _ => false
        };
        if (!validan)
            throw new InvalidOperationException("ISBN mora biti validan ISBN-10 ili ISBN-13 sa kontrolnom cifrom.");
        return cist;
    }

    // Proveri ISBN-10 kontrolnu cifru pomocu modula 11.
    private static bool ProveriIsbn10(string isbn)
    {
        if (!isbn.Take(9).All(c => c >= '0' && c <= '9')) return false;
        if (!(isbn[9] >= '0' && isbn[9] <= '9') && isbn[9] != 'X') return false;
        int zbir = 0;
        for (int i = 0; i < 9; i++) zbir += (10 - i) * (isbn[i] - '0');
        zbir += isbn[9] == 'X' ? 10 : isbn[9] - '0';
        return zbir % 11 == 0;
    }

    // Proveri ISBN-13 kontrolnu cifru sa tezinskim faktorima 1 i 3.
    private static bool ProveriIsbn13(string isbn)
    {
        if (!isbn.All(c => c >= '0' && c <= '9')) return false;
        // ISBN-13 koristi EAN prefikse 978 i 979 (ne svaki EAN-13).
        if (!isbn.StartsWith("978", StringComparison.Ordinal) &&
            !isbn.StartsWith("979", StringComparison.Ordinal)) return false;
        int zbir = 0;
        for (int i = 0; i < 13; i++)
            zbir += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return zbir % 10 == 0;
    }
}
