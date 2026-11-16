
using System;
using System.IO;
using SajamKnjiga.Core.Services;
using System.Linq;

namespace SajamKnjiga.ConsoleApp;

public class KupovineMeni : IMeniModul
{
    private readonly KupovinaServis _servis;

    public string Naziv => "Kupovine";

    public KupovineMeni()
    {
        string putanja = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "kupovine.xml"
        );

        _servis = new KupovinaServis(putanja);
    }

    public void Pokreni()
    {
        while (true)
        {
            System.Console.WriteLine("\n--- KUPOVINE ---");
            System.Console.WriteLine("1. Prikazi sve kupovine");
            System.Console.WriteLine("2. Dodaj kupovinu");
            System.Console.WriteLine("0. Povratak");
            System.Console.Write("Izbor: ");

            string? izbor = System.Console.ReadLine();

            switch (izbor)
            {
                case "1":
                    Prikazi();
                    break;

                case "2":
                    Dodaj();
                    break;
                case "0":
                    return;

                default:
                    System.Console.WriteLine("Nepoznata opcija.");
                    break;
            }
        }
    }

    private void Prikazi()
    {
        var kupovine = _servis.PrikaziSve();

        if (kupovine.Count == 0)
        {
            System.Console.WriteLine("Nema evidentiranih kupovina.");
            return;
        }

        foreach (var kupovina in kupovine)
        {
            System.Console.WriteLine(
                $"ID: {kupovina.Id} | " +
                $"Posetilac: {kupovina.PosetilacId} | " +
                $"Knjiga: {kupovina.KnjigaId} | " +
                $"Ocena: {kupovina.Ocena}");
        }
    }

    private void Dodaj()
    {
        System.Console.Write("ID kupovine: ");
        if (!int.TryParse(System.Console.ReadLine(), out int id) || id <= 0)
        {
            System.Console.WriteLine("Neispravan ID kupovine.");
            return;
        }

        System.Console.Write("ID posetioca: ");
        if (!int.TryParse(System.Console.ReadLine(), out int posetilacId) || posetilacId <= 0)
        {
            System.Console.WriteLine("Neispravan ID posetioca.");
            return;
        }

        string putanjaPosetioci = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "posetioci.xml"
        );

        PosetilacServis posetilacServis =
            new PosetilacServis(putanjaPosetioci);

        bool postojiPosetilac = posetilacServis
            .PrikaziSve()
            .Any(p => p.Id == posetilacId);

        if (!postojiPosetilac)
        {
            System.Console.WriteLine(
                "Posetilac sa unetim ID-jem ne postoji.");
            return;
        }

        System.Console.Write("ID knjige: ");
        if (!int.TryParse(System.Console.ReadLine(), out int knjigaId) || knjigaId <= 0)
        {
            System.Console.WriteLine("Neispravan ID knjige.");
            return;
        }

        string putanjaKnjige = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "knjige.xml"
        );

        KnjigaServis knjigaServis = new KnjigaServis(putanjaKnjige);

        if (!knjigaServis.PostojiKnjiga(knjigaId))
        {
            System.Console.WriteLine(
                "Knjiga sa unetim ID-jem ne postoji.");
            return;
        }

        System.Console.Write("Ocena (1-5): ");
        if (!int.TryParse(System.Console.ReadLine(), out int ocena) ||
            ocena < 1 || ocena > 5)
        {
            System.Console.WriteLine("Ocena mora biti od 1 do 5.");
            return;
        }

        System.Console.Write("Komentar: ");
        string komentar = System.Console.ReadLine() ?? "";

        var kupovina = new SajamKnjiga.Core.Models.Kupovina
        {
            Id = id,
            PosetilacId = posetilacId,
            KnjigaId = knjigaId,
            DatumKupovine = DateTime.Today,
            Ocena = ocena,
            Komentar = komentar
        };

        try
        {
            _servis.Dodaj(kupovina);
            System.Console.WriteLine("Kupovina je uspesno dodata.");
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine(ex.Message);
        }
    }

}
