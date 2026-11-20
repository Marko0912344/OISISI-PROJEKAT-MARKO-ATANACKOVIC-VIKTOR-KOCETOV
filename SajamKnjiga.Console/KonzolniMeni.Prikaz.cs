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
    // Deo konzolnog menija: prikaz.
    public partial class KonzolniMeni
    {
        // I administrativni pregled prikazuje najvise 16 zapisa po strani.
        // Stranice se biraju posebno po kategoriji, bez masovnog ispisa.
        private void PrikaziSveEntitete()
        {
            var (autori, knjige) = _autorKnjigaService.UcitajPovezanePodatke();

            Console.WriteLine("\n===== AUTORI (PO PREZIMENU) =====");
            PrikaziStranice(autori.OrderBy(a => a.Prezime,
                StringComparer.CurrentCultureIgnoreCase).ThenBy(a => a.Ime,
                StringComparer.CurrentCultureIgnoreCase).ToList(), a =>
                $"{a.Id} | {a.Ime} {a.Prezime} | " +
                $"Iskustvo: {a.GodineIskustva} | Knjiga: {a.Knjige.Count}");

            Console.WriteLine("\n===== KNJIGE (PO NAZIVU) =====");
            PrikaziStranice(knjige.OrderBy(k => k.Naziv,
                StringComparer.CurrentCultureIgnoreCase).ToList(), k =>
                $"{k.Id} | {k.Naziv} | ISBN: {k.ISBN} | " +
                $"Cena: {k.Cena} | Izdavac ID: {k.IzdavacId} | " +
                $"Autori: {string.Join(", ", k.Autori.Select(a => a.Ime + " " + a.Prezime))}");

            Console.WriteLine("\n===== POSETIOCI (PO PREZIMENU) =====");
            PrikaziStranice(_posetioci.UcitajSve().OrderBy(p => p.Prezime,
                StringComparer.CurrentCultureIgnoreCase).ThenBy(p => p.Ime,
                StringComparer.CurrentCultureIgnoreCase).ToList(), p =>
                $"{p.Id} | {p.Ime} {p.Prezime} | " +
                $"Karta: {p.BrojClanskeKarte} | Status: {p.Status}");

            Console.WriteLine("\n===== IZDAVACI =====");
            PrikaziStranice(_izdavaci.UcitajSve().OrderBy(i => i.Naziv,
                StringComparer.CurrentCultureIgnoreCase).ToList(), i =>
                $"{i.Id} | {i.Sifra} | {i.Naziv} | " +
                $"Sef autor ID: {i.SefAutorId?.ToString() ?? "-"}");

            Console.WriteLine("\n===== ADRESE =====");
            PrikaziStranice(_adrese.UcitajSve().OrderBy(a => a.Id).ToList(), a =>
                $"{a.Id} | {a.Ulica} {a.Broj}, {a.Grad}, {a.Drzava}");

            Console.WriteLine("\n===== KUPOVINE =====");
            PrikaziStranice(_kupovine.UcitajSve().OrderBy(k => k.Id).ToList(), k =>
                $"{k.Id} | Posetilac: {k.PosetilacId} | Knjiga: {k.KnjigaId} | " +
                $"Ocena: {k.Ocena} | Datum: {k.DatumKupovine:dd.MM.yyyy}");
        }

        // Prikazi adresu, kupovine i listu zelja izabranog posetioca.
        private void PrikaziDetaljePosetioca()
        {
            int id = UnesiId("ID posetioca: ");
            Posetilac? p = PronadjiPovezanogPosetioca(id);

            if (p == null)
            {
                Console.WriteLine("Posetilac nije pronadjen.");
                return;
            }

            Console.WriteLine();
            Console.WriteLine($"Posetilac: {p.Ime} {p.Prezime}");
            Console.WriteLine($"Clanska karta: {p.BrojClanskeKarte}");
            Console.WriteLine($"Status: {p.Status}");
            var (_, ukupnaPotrosnja, prosek) = _upiti.KupljeneKnjige(id);
            Console.WriteLine($"Prosecna ocena: {prosek:F2}");
            Console.WriteLine("Ukupna potrosnja: " +
                (ukupnaPotrosnja.HasValue ? $"{ukupnaPotrosnja.Value:F2} RSD"
                    : "NEPOZNATA (nedostaje istorijska cena jedne ili vise kupovina)"));

            Console.WriteLine();
            Console.WriteLine("----- LISTA ZELJA -----");

            foreach (Knjiga k in p.ListaZelja.OrderBy(k => k.Naziv))
            {
                Console.WriteLine($"{k.Id} | {k.Naziv} | {k.Cena}");
            }

            Console.WriteLine();
            Console.WriteLine("----- KUPLJENE KNJIGE -----");

            foreach (Kupovina k in p.Kupovine)
            {
                Console.WriteLine(
                    $"{k.KnjigaId} | {k.Knjiga?.Naziv} | " +
                    $"Ocena: {k.Ocena} | Datum: {k.DatumKupovine:dd.MM.yyyy}"
                );
            }
        }

        // Pokazi razloge za dopusteno ili zabranjeno brisanje.
        private void ProveriBrisanje()
        {
            Console.WriteLine("1. Posetilac");
            Console.WriteLine("2. Autor");
            Console.WriteLine("3. Knjiga");
            Console.WriteLine("4. Izdavac");
            Console.WriteLine("5. Adresa");
            Console.Write("Izaberi tip: ");

            string tip = Console.ReadLine()?.Trim() ?? "";
            if (tip != "1" && tip != "2" && tip != "3" &&
                tip != "4" && tip != "5")
            {
                Console.WriteLine("Nepostojeci tip.");
                return;
            }

            int id = UnesiId("ID entiteta: ");
            ProveraBrisanja rezultat = tip switch
            {
                "1" => _brisanje.ProveriBrisanjePosetioca(id),
                "2" => _brisanje.ProveriBrisanjeAutora(id),
                "3" => _brisanje.ProveriBrisanjeKnjige(id),
                "4" => _brisanje.ProveriBrisanjeIzdavaca(id),
                _ => _brisanje.ProveriBrisanjeAdrese(id)
            };

            Console.WriteLine($"Brisanje dozvoljeno: {rezultat.Dozvoljeno}");
            foreach (string razlog in rezultat.Razlozi)
            {
                Console.WriteLine($"- {razlog}");
            }
            Console.WriteLine("Ovo je samo provera. Nista nije obrisano.");
        }

        // Ispisi kratku statistiku broja zapisa i veza.
        private void PrikaziStanje()
        {
            SajamPaket p = _razmena.PreuzmiTrenutnePodatke();
            Console.WriteLine($"Autori: {p.Autori.Count}; Knjige: {p.Knjige.Count}; " +
                $"Posetioci: {p.Posetioci.Count}");
            Console.WriteLine($"Izdavaci: {p.Izdavaci.Count}; " +
                $"Adrese: {p.Adrese.Count}; Kupovine: {p.Kupovine.Count}");
            Console.WriteLine($"AutorKnjiga veze: {p.AutorKnjigaVeze.Count}; " +
                $"Zelje: {p.StavkeListeZelja.Count}");
            Console.WriteLine("Radni folder: " +
                System.IO.Path.Combine(AppContext.BaseDirectory, "Data"));
        }
    }
}
