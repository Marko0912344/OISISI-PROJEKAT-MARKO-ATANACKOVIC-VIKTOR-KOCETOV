// Ulazna tacka: oporavak XML transakcija i pokretanje konzolnog menija.
using System;
using SajamKnjiga.ConsoleApp;
using SajamKnjiga.Core.Persistence;

try
{
    // Ovo mora da se desi PRE bilo kakvog rada sa podacima.
    int broj = XmlTransakcija.OporaviPrekinute();

    if (broj > 0)
    {
        Console.WriteLine(
            $"Provereno/oporavljeno dnevnika transakcija: {broj}.");
        Console.WriteLine(
            "Proveri podatke pre nastavljanja rada.");
    }

    KonzolniMeni meni = new KonzolniMeni();
    meni.Pokreni();
}
catch (Exception ex)
{
    Console.WriteLine("Pokretanje je prekinuto: " + ex.Message);
    Console.WriteLine(
        "Nemoj nastavljati rad sa XML datotekama pre otklanjanja greske.");
}
