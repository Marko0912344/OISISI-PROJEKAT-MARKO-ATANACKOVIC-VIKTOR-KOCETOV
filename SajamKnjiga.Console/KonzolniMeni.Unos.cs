using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;
using SajamKnjiga.Core.Services;

namespace SajamKnjiga.ConsoleApp
{
    // Deo konzolnog menija: unos.
    public partial class KonzolniMeni
    {
        // Enter ostavlja staru tekstualnu vrednost.
        private static string IzmeniTekst(string naziv, string staraVrednost)
        {
            Console.Write($"{naziv} [{staraVrednost}]: ");
            string unos = Console.ReadLine()?.Trim() ?? "";
            return unos.Length == 0 ? staraVrednost : unos;
        }

        // Prikazi staru vrednost i proveri novi broj.
        private static int IzmeniBroj(string naziv, int stari, int min, int max = int.MaxValue)
        {
            Console.Write($"{naziv} [{stari}] (Enter = isto): ");
            string unos = Console.ReadLine()?.Trim() ?? "";
            if (unos.Length == 0) return stari;
            if (!int.TryParse(unos, out int broj) || broj < min || broj > max)
                throw new ArgumentException($"{naziv} mora biti broj od {min} do {max}.");
            return broj;
        }

        // Parsiraj novi datum ili zadrzi stari.
        private static DateTime IzmeniDatum(string naziv, DateTime stari)
        {
            Console.Write($"{naziv} [{stari:dd.MM.yyyy}] (Enter = isto): ");
            string unos = Console.ReadLine()?.Trim() ?? "";
            if (unos.Length == 0) return stari;
            if (!DateTime.TryParseExact(unos, "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d) ||
                d > DateTime.Today)
                throw new ArgumentException("Datum nije ispravan.");
            return d;
        }

        // Omoguci promenu cene uz proveru decimalnog unosa.
        private static decimal IzmeniCenu(decimal stara)
        {
            Console.Write($"Cena [{stara}] (Enter = isto): ");
            string unos = Console.ReadLine()?.Trim() ?? "";
            if (unos.Length == 0) return stara;
            if (!decimal.TryParse(unos.Replace(',', '.'),
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out decimal d) || d < 0)
                throw new ArgumentException("Cena nije ispravna.");
            return d;
        }

        // Procitaj dozvoljeni status posetioca R ili V.
        private static StatusPosetioca UnesiStatus()
        {
            while (true)
            {
                Console.Write("Status (R = redovan, V = VIP): ");
                string unos = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "";
                if (unos == "R") return StatusPosetioca.R;
                if (unos == "V") return StatusPosetioca.V;
                Console.WriteLine("Unesi R ili V.");
            }
        }

        // Pronadji posetioca po ID-ju za ostale operacije.
        private Posetilac? PronadjiPovezanogPosetioca(int id)
        {
            var (posetioci, _, _) = _posetilacService.UcitajPovezanePodatke();
            return posetioci.FirstOrDefault(p => p.Id == id);
        }

        // Ponovi unos dok obavezno polje ne bude popunjeno.
        private static string UnesiObavezanTekst(string poruka)
        {
            while (true)
            {
                Console.Write(poruka);
                string unos = Console.ReadLine()?.Trim() ?? "";
                if (unos.Length > 0) return unos;
                Console.WriteLine("Ovo polje je obavezno.");
            }
        }

        // Zatrazi pozitivan identifikator.
        private static int UnesiId(string poruka)
        {
            return UnesiBroj(poruka, 1);
        }

        // Proveri numericki unos u trazenom rasponu.
        private static int UnesiBroj(
            string poruka, int minimum, int maksimum = int.MaxValue)
        {
            while (true)
            {
                Console.Write(poruka);
                if (int.TryParse(Console.ReadLine(), out int broj) &&
                    broj >= minimum && broj <= maksimum)
                {
                    return broj;
                }

                Console.WriteLine(
                    $"Unesi ceo broj od {minimum} do {maksimum}.");
            }
        }

        // Procitaj decimalnu cenu uz proveru granica.
        private static decimal UnesiCenu()
        {
            while (true)
            {
                Console.Write("Cena (npr. 1250,50): ");
                string unos = (Console.ReadLine() ?? "").Trim();
                string normalizovano = unos.Replace(',', '.');

                if (decimal.TryParse(normalizovano,
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out decimal cena) &&
                    cena >= 0)
                {
                    return cena;
                }

                Console.WriteLine("Unesi nenegativnu cenu, bez separatora hiljada.");
            }
        }

        // Procitaj datum u trazenom formatu.
        private static DateTime UnesiObavezanDatum(string poruka)
        {
            while (true)
            {
                Console.Write(poruka);
                string unos = Console.ReadLine()?.Trim() ?? "";

                if (DateTime.TryParseExact(unos, "dd.MM.yyyy",
                    CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out DateTime datum) && datum <= DateTime.Today)
                {
                    return datum;
                }

                Console.WriteLine("Unesi datum u formatu dd.MM.yyyy, ne u buducnosti.");
            }
        }

        // Ocena kupovine mora biti od 1 do 5.
        private static int UnesiOcenu()
        {
            return UnesiBroj("Ocena (1-5): ", 1, 5);
        }

        // Procitaj datum kupovine.
        private static DateTime UnesiDatum()
        {
            Console.Write("Datum kupovine dd.MM.yyyy (Enter = danas): ");
            string unos = Console.ReadLine()?.Trim() ?? "";
            if (unos.Length == 0) return DateTime.Today;

            if (DateTime.TryParseExact(unos, "dd.MM.yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None,
                out DateTime datum))
            {
                return datum;
            }

            throw new ArgumentException("Datum nije u formatu dd.MM.yyyy.");
        }

        // Trazi eksplicitnu potvrdu pre osetljive operacije.
        private static bool Potvrdi(string poruka)
        {
            Console.Write($"{poruka} (DA/NE): ");
            return string.Equals(Console.ReadLine()?.Trim(), "DA",
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
