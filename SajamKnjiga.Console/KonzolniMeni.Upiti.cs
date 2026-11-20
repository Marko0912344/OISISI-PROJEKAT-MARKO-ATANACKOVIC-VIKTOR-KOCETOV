using System;
using System.Collections.Generic;
using System.Linq;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Services;

namespace SajamKnjiga.ConsoleApp
{
    /// <summary>
    /// Konzolni prikaz zahteva 8.19-8.21 i 8.27-8.31.
    /// Svi poslovni upiti zive u Core/KatalogUpitiService.cs.
    /// </summary>
    // Deo konzolnog menija: upiti.
    public partial class KonzolniMeni
    {
        private readonly KatalogUpitiService _upiti = new();

        // Meni za pretragu, sortiranje, paginaciju i povezane upite.
        private void MeniUpiti()
        {
            while (true)
            {
                Console.WriteLine("\n====== PRETRAGA, SORTIRANJE I POSLOVNI UPITI ======");
                Console.WriteLine("1. Posetioci - pretraga, sortiranje, stranice");
                Console.WriteLine("2. Autori - pretraga, sortiranje, stranice");
                Console.WriteLine("3. Knjige - pretraga, sortiranje, stranice");
                Console.WriteLine("4. Knjige koje je autor napisao (8.21)");
                Console.WriteLine("5. Posetioci koji zele knjige autora (8.27)");
                Console.WriteLine("6. Autori sa liste zelja posetioca (8.28)");
                Console.WriteLine("7. Poredjenje posetilaca prema dve knjige (8.29)");
                Console.WriteLine("8. Knjige izdavaca (8.30)");
                Console.WriteLine("9. Autori izdavaca (8.31)");
                Console.WriteLine("10. Kupljene knjige i ukupna potrosnja (8.13)");
                Console.WriteLine("11. Unos poznate istorijske cene stare kupovine");
                Console.WriteLine("0. Povratak");
                Console.Write("Izbor: ");

                string? izbor = Console.ReadLine()?.Trim();
                if (izbor == "0" || izbor is null) return;
                try
                {
                    switch (izbor)
                    {
                        case "1": PregledPosetilaca(); break;
                        case "2": PregledAutora(); break;
                        case "3": PregledKnjiga(); break;
                        case "4": PrikaziKnjigeAutora(); break;
                        case "5": PrikaziPosetioceAutora(); break;
                        case "6": PrikaziAutorePosetioca(); break;
                        case "7": PorediDveKnjige(); break;
                        case "8": PrikaziKnjigeIzdavaca(); break;
                        case "9": PrikaziAutoreIzdavaca(); break;
                        case "10": PrikaziKupljeneSaPotrosnjom(); break;
                        case "11": DopuniIstorijskuCenu(); break;
                        default: Console.WriteLine("Nepostojeca opcija."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Upit nije izvrsen: " + ex.Message);
                }
            }
        }

        // Prazan upit znaci prikaz svih rezultata.
        private static string OpcionaPretraga(string opis)
        {
            Console.Write(opis + " (Enter = svi): ");
            return Console.ReadLine()?.Trim() ?? "";
        }

        // Izaberi rastuci ili opadajuci redosled.
        private static bool SmerSortiranja()
        {
            Console.Write("Opadajuce sortiranje? (d/N): ");
            return string.Equals(Console.ReadLine()?.Trim(), "d", StringComparison.OrdinalIgnoreCase);
        }

        // Izaberi atribut sortiranja, uz podrazumevanu vrednost.
        private static string PoljeSortiranja(string opcije, string podrazumevano)
        {
            Console.Write($"Sortiraj po ({opcije}), Enter = {podrazumevano}: ");
            string? polje = Console.ReadLine()?.Trim();
            return string.IsNullOrWhiteSpace(polje) ? podrazumevano : polje;
        }

        // Pretrazi i sortiraj posetioce pre prikaza strane.
        private void PregledPosetilaca()
        {
            string upit = OpcionaPretraga("Pretraga: prezime / prezime, ime / karta, ime, prezime");
            string polje = PoljeSortiranja("karta/ime/prezime/godina/status/ocena", "prezime");
            PrikaziPosetioce(_upiti.Posetioci(upit, polje, SmerSortiranja()));
        }

        // Pretrazi i sortiraj autore pre prikaza strane.
        private void PregledAutora()
        {
            string upit = OpcionaPretraga("Pretraga: prezime / prezime, ime");
            string polje = PoljeSortiranja("ime/prezime/email", "prezime");
            PrikaziAutore(_upiti.Autori(upit, polje, SmerSortiranja()));
        }

        // Pretrazi i sortiraj knjige pre prikaza strane.
        private void PregledKnjiga()
        {
            string upit = OpcionaPretraga("Pretraga: deo naziva ili ISBN-a");
            string polje = PoljeSortiranja("isbn/naziv/cena/godina/zanr", "naziv");
            PrikaziKnjige(_upiti.Knjige(upit, polje, SmerSortiranja()));
        }

        // Izlistaj knjige koje je napisao izabrani autor.
        private void PrikaziKnjigeAutora()
        {
            int autorId = UnesiId("ID autora: ");
            PrikaziKnjige(_upiti.KnjigeAutora(autorId));
        }

        // Posetioci koji zele knjige izabranog autora.
        private void PrikaziPosetioceAutora()
        {
            int autorId = UnesiId("ID autora: ");
            string pretraga = OpcionaPretraga("Pretraga medju posetiocima (1/2/3 dela)");
            PrikaziPosetioce(_upiti.PosetiociKojiZeleAutora(autorId, pretraga));
        }

        // Autori knjiga iz liste zelja posetioca.
        private void PrikaziAutorePosetioca()
        {
            int posetilacId = UnesiId("ID posetioca: ");
            string pretraga = OpcionaPretraga("Pretraga medju autorima (prezime / prezime, ime)");
            PrikaziAutore(_upiti.AutoriKojePosetilacZeli(posetilacId, pretraga));
        }

        // Uporedi zelje i kupovine posetilaca za dve knjige.
        private void PorediDveKnjige()
        {
            int prva = UnesiId("ID prve knjige: ");
            int druga = UnesiId("ID druge (razlicite) knjige: ");
            string pretraga = OpcionaPretraga("Pretraga unutar rezultata (1/2/3 dela)");
            var (obe, prvaNeDruga) = _upiti.UporediDveKnjige(prva, druga, pretraga);
            Console.WriteLine("\n--- POSETIOCI KOJI OBE KNJIGE IMAJU NA LISTI ZELJA ---");
            PrikaziPosetioce(obe);
            Console.WriteLine("\n--- KUPILI PRVU, ALI NISU KUPILI DRUGU ---");
            PrikaziPosetioce(prvaNeDruga);
        }

        // Prikazi knjige izabranog izdavaca.
        private void PrikaziKnjigeIzdavaca()
        {
            int id = UnesiId("ID izdavaca: ");
            PrikaziKnjige(_upiti.KnjigeIzdavaca(id));
        }

        // Prikazi jedinstvene autore izabranog izdavaca.
        private void PrikaziAutoreIzdavaca()
        {
            int id = UnesiId("ID izdavaca: ");
            string pretraga = OpcionaPretraga("Pretraga medju autorima (prezime / prezime, ime)");
            PrikaziAutore(_upiti.AutoriIzdavaca(id, pretraga));
        }

        // Prikazi kupljene knjige i poznatu istorijsku potrosnju.
        private void PrikaziKupljeneSaPotrosnjom()
        {
            int id = UnesiId("ID posetioca: ");
            var (kupovine, ukupno, prosek) = _upiti.KupljeneKnjige(id);
            var knjige = _knjige.UcitajSve().ToDictionary(k => k.Id);
            PrikaziStranice(kupovine, k =>
                $"Kupovina {k.Id,4} | Knjiga: {(knjige.TryGetValue(k.KnjigaId, out var knjiga) ? knjiga.Naziv : "?")} | " +
                $"Datum: {k.DatumKupovine:dd.MM.yyyy} | Ocena: {k.Ocena} | " +
                $"Placeno: {(k.CenaPriKupovini.HasValue ? k.CenaPriKupovini.Value.ToString("F2") + " RSD" : "ISTORIJSKA CENA NEPOZNATA")}");
            Console.WriteLine($"Prosecna ocena: {prosek:F2}; ukupna potrosnja: " +
                (ukupno.HasValue ? $"{ukupno.Value:F2} RSD" : "NIJE MOGUCE TACNO IZRACUNATI"));
            if (kupovine.Any(k => !k.CenaPriKupovini.HasValue))
                Console.WriteLine("Stare kupovine nemaju sacuvanu cenu. Koristi stavku 11 za unos proverene istorijske cene.");
        }

        // Dopuni stvarno placeni iznos za kupovinu koja ga nema.
        private void DopuniIstorijskuCenu()
        {
            int kupovinaId = UnesiId("ID stare kupovine: ");
            Kupovina kupovina = _kupovine.PronadjiPoId(kupovinaId)
                ?? throw new InvalidOperationException("Kupovina ne postoji.");
            if (kupovina.CenaPriKupovini.HasValue)
                throw new InvalidOperationException("Kupovina vec ima sacuvanu istorijsku cenu.");

            Console.WriteLine("Unesi iskljucivo dokumentovani iznos placen pri toj kupovini.");
            decimal cena = UnesiCenu();
            if (!Potvrdi($"Potvrditi istorijski iznos {cena:F2} RSD za kupovinu {kupovinaId}?"))
                return;
            _katalogIzmena.EvidentirajIstorijskuCenu(kupovinaId, cena);
            Console.WriteLine("Istorijski iznos je sacuvan. Podatak je nepovratan kroz ovaj meni.");
        }

        // Prikaz rezultata posetilaca kroz paginaciju.
        private void PrikaziPosetioce(IReadOnlyList<Posetilac> rezultati)
        {
            var adrese = _adrese.UcitajSve().ToDictionary(a => a.Id);
            PrikaziStranice(rezultati, p =>
                $"{p.Id,4} | {p.BrojClanskeKarte,-17} | {p.Ime} {p.Prezime} | " +
                $"Status: {p.Status} | God: {p.TrenutnaGodinaClanstva} | " +
                $"Ocena: {p.ProsecnaOcenaRecenzija:F2} | " +
                $"{(adrese.TryGetValue(p.AdresaId, out var adresa) ? adresa.ToString() : "Nema adrese")}");
        }

        // Prikaz rezultata autora kroz paginaciju.
        private static void PrikaziAutore(IReadOnlyList<Autor> rezultati) =>
            PrikaziStranice(rezultati, a =>
                $"{a.Id,4} | {a.Ime} {a.Prezime} | {a.Email} | " +
                $"{a.Telefon} | {a.DatumRodjenja:dd.MM.yyyy} | " +
                $"{(a.Adresa is null ? "Nema adrese" : a.Adresa.ToString())}");

        // Prikaz rezultata knjiga kroz paginaciju.
        private static void PrikaziKnjige(IReadOnlyList<Knjiga> rezultati) =>
            PrikaziStranice(rezultati, k =>
                $"{k.Id,4} | {k.ISBN,-18} | {k.Naziv} | " +
                $"{k.GodinaIzdanja} | {k.Zanr} | {k.Cena:F2} RSD | " +
                $"Izdavac ID: {k.IzdavacId?.ToString() ?? "-"}");

        private static void PrikaziStranice<T>(IReadOnlyList<T> rezultati, Func<T, string> red)
        {
            int ukupno = KatalogUpitiService.BrojStrana(rezultati.Count);
            int strana = 1;
            while (true)
            {
                Console.WriteLine($"\nUkupno: {rezultati.Count}, strana {strana}/{ukupno} (najvise 16 redova)");
                foreach (var element in KatalogUpitiService.Stranica(rezultati, strana))
                    Console.WriteLine(red(element));
                if (rezultati.Count == 0) Console.WriteLine("Nema rezultata.");
                Console.Write("n = sledeca, p = prethodna, q/Enter = nazad: ");
                string? naredba = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (naredba == "n" && strana < ukupno) strana++;
                else if (naredba == "p" && strana > 1) strana--;
                else if (naredba is "q" or "" or null) return;
                else Console.WriteLine("Nema trazene stranice.");
            }
        }
    }
}
