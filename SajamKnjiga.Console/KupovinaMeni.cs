
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

        string putanjaPosetioci = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "posetioci.xml"
        );

        PosetilacServis posetilacServis =
            new PosetilacServis(putanjaPosetioci);

        foreach (var posetilac in posetilacServis.PrikaziSve())
        {
            var kupovine = _servis.KupovinePosetioca(posetilac.Id);

            double prosek = kupovine.Count > 0
                ? kupovine.Average(k => k.Ocena)
                : 0;


            if (posetilac.ProsecnaOcena != prosek)
            {
                posetilac.ProsecnaOcena = prosek;

                bool uspesno = posetilacServis.Izmeni(posetilac);

            
            }

        }

        while (true)
        {
            System.Console.WriteLine("\n--- KUPOVINE ---");
            System.Console.WriteLine("1. Prikazi sve kupovine");
            System.Console.WriteLine("2. Dodaj kupovinu");
            System.Console.WriteLine("3. Obrisi kupovinu");
            System.Console.WriteLine("4. Izmeni kupovinu");
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

                case "3":
                    Obrisi();
                    break;
                case "4":
                    Izmeni();
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
                $"Datum: {kupovina.DatumKupovine:dd.MM.yyyy} | " +
                $"Ocena: {kupovina.Ocena} | " +
                $"Komentar: {kupovina.Komentar}");
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

            var posetilac = posetilacServis.PrikaziSve()
                .FirstOrDefault(p => p.Id == posetilacId);

            if (posetilac != null)
            {
                var kupovine = _servis.KupovinePosetioca(posetilacId);

                posetilac.ProsecnaOcena =
                    kupovine.Average(k => k.Ocena);

                posetilacServis.Izmeni(posetilac);
            }

            System.Console.WriteLine("Kupovina je uspesno dodata.");
        }
        catch (InvalidOperationException ex)
        {
            System.Console.WriteLine(ex.Message);
        }

    }


    private void Obrisi()
    {
        System.Console.Write("ID kupovine za brisanje: ");

        if (!int.TryParse(System.Console.ReadLine(), out int id) || id <= 0)
        {
            System.Console.WriteLine("Neispravan ID kupovine.");
            return;
        }

        var kupovina = _servis.PrikaziSve()
            .FirstOrDefault(k => k.Id == id);

        if (kupovina == null)
        {
            System.Console.WriteLine("Kupovina nije pronadjena.");
            return;
        }

        System.Console.Write("Da li sigurno zelis da obrises kupovinu? (D/N): ");
        string odgovor = (System.Console.ReadLine() ?? "")
            .Trim()
            .ToUpperInvariant();

        if (odgovor != "D")
        {
            System.Console.WriteLine("Brisanje je otkazano.");
            return;
        }

        int posetilacId = kupovina.PosetilacId;

        bool obrisana = _servis.Obrisi(id);

        if (!obrisana)
        {
            System.Console.WriteLine("Kupovina nije pronadjena.");
            return;
        }

        string putanjaPosetioci = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "posetioci.xml"
        );

        PosetilacServis posetilacServis =
            new PosetilacServis(putanjaPosetioci);

        var posetilac = posetilacServis.PrikaziSve()
            .FirstOrDefault(p => p.Id == posetilacId);

        if (posetilac != null)
        {
            var preostaleKupovine = _servis.KupovinePosetioca(posetilacId);

            posetilac.ProsecnaOcena = preostaleKupovine.Count > 0
                ? preostaleKupovine.Average(k => k.Ocena)
                : 0;

            posetilacServis.Izmeni(posetilac);
        }

        System.Console.WriteLine("Kupovina je uspesno obrisana.");
    }

    private void Izmeni()
    {
        System.Console.Write("ID kupovine za izmenu: ");

        if (!int.TryParse(System.Console.ReadLine(), out int id) || id <= 0)
        {
            System.Console.WriteLine("Neispravan ID.");
            return;
        }

        var kupovina = _servis.PrikaziSve()
            .FirstOrDefault(k => k.Id == id);

        if (kupovina == null)
        {
            System.Console.WriteLine("Kupovina nije pronadjena.");
            return;
        }

        System.Console.Write($"Nova ocena (1-5), trenutna {kupovina.Ocena}: ");
        string unosOcene = System.Console.ReadLine() ?? "";

        int novaOcena = kupovina.Ocena;

        if (!string.IsNullOrWhiteSpace(unosOcene))
        {
            if (!int.TryParse(unosOcene, out novaOcena) ||
                novaOcena < 1 || novaOcena > 5)
            {
                System.Console.WriteLine("Ocena mora biti od 1 do 5.");
                return;
            }
        }

        System.Console.Write($"Novi komentar (trenutni: {kupovina.Komentar}): ");
        string noviKomentar = System.Console.ReadLine() ?? "";

        var izmenjena = new SajamKnjiga.Core.Models.Kupovina
        {
            Id = kupovina.Id,
            PosetilacId = kupovina.PosetilacId,
            KnjigaId = kupovina.KnjigaId,
            DatumKupovine = kupovina.DatumKupovine,
            Ocena = novaOcena,
            Komentar = string.IsNullOrWhiteSpace(noviKomentar)
                ? kupovina.Komentar
                : noviKomentar
        };

        bool uspesno = _servis.Izmeni(izmenjena);

        if (!uspesno)
        {
            System.Console.WriteLine("Izmena nije uspela.");
            return;
        }

        string putanjaPosetioci = Path.Combine(
            AppContext.BaseDirectory,
            "Podaci",
            "posetioci.xml"
        );

        PosetilacServis posetilacServis =
            new PosetilacServis(putanjaPosetioci);

        var posetilac = posetilacServis.PrikaziSve()
            .FirstOrDefault(p => p.Id == kupovina.PosetilacId);

        if (posetilac != null)
        {
            var kupovine = _servis.KupovinePosetioca(posetilac.Id);

            posetilac.ProsecnaOcena = kupovine.Count > 0
                ? kupovine.Average(k => k.Ocena)
                : 0;

            posetilacServis.Izmeni(posetilac);
        }

        System.Console.WriteLine("Kupovina je uspesno izmenjena.");
    }


}
