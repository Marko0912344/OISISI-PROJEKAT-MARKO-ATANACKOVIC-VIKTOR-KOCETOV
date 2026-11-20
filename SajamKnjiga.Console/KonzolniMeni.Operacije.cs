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
    // Deo konzolnog menija: operacije.
    public partial class KonzolniMeni
    {
        // Knjiga mora postojati i ne sme vec biti kupljena ili pozeljena.
        private void DodajNaListuZelja()
        {
            int posetilacId = UnesiId("ID posetioca: ");
            var dostupne = _posetilacService
                .DostupneKnjigeZaListuZelja(posetilacId);

            if (dostupne.Count == 0)
            {
                Console.WriteLine("Nema knjiga za dodavanje.");
                return;
            }

            Console.WriteLine("Knjige koje se mogu dodati:");
            foreach (Knjiga k in dostupne.OrderBy(k => k.Naziv))
            {
                Console.WriteLine($"{k.Id} | {k.Naziv}");
            }

            int knjigaId = UnesiId("ID knjige: ");
            bool dodata = _posetilacService.DodajNaListuZelja(posetilacId, knjigaId);

            Console.WriteLine(dodata
                ? "Knjiga je dodata."
                : "Knjiga nije dodata (vec postoji ili je kupljena).");
        }

        // Ukloni knjigu iz zelja posle potvrde.
        private void UkloniSaListeZelja()
        {
            int posetilacId = UnesiId("ID posetioca: ");
            Posetilac? p = PronadjiPovezanogPosetioca(posetilacId);

            if (p == null)
            {
                Console.WriteLine("Posetilac ne postoji.");
                return;
            }

            if (p.ListaZelja.Count == 0)
            {
                Console.WriteLine("Lista zelja je prazna.");
                return;
            }

            foreach (Knjiga k in p.ListaZelja.OrderBy(k => k.Naziv))
            {
                Console.WriteLine($"{k.Id} | {k.Naziv}");
            }

            int knjigaId = UnesiId("ID knjige za uklanjanje: ");
            if (!Potvrdi("Ukloniti knjigu sa liste zelja?")) return;

            bool uklonjena = _posetilacService
                .UkloniSaListeZelja(posetilacId, knjigaId);

            Console.WriteLine(uklonjena
                ? "Knjiga je uklonjena."
                : "Knjiga nije bila na listi zelja.");
        }

        // Kupovina nastaje samo iz postojece liste zelja.
        private void EvidentirajKupovinu()
        {
            int posetilacId = UnesiId("ID posetioca: ");
            Posetilac? p = PronadjiPovezanogPosetioca(posetilacId);

            if (p == null || p.ListaZelja.Count == 0)
            {
                Console.WriteLine("Posetilac ne postoji ili nema knjige u zeljama.");
                return;
            }

            Console.WriteLine("Knjige na listi zelja:");
            foreach (Knjiga k in p.ListaZelja.OrderBy(k => k.Naziv))
            {
                Console.WriteLine($"{k.Id} | {k.Naziv} | ISBN: {k.ISBN}");
            }

            int knjigaId = UnesiId("ID knjige za kupovinu: ");
            Knjiga? odabrana = p.ListaZelja.FirstOrDefault(k => k.Id == knjigaId);

            if (odabrana == null)
            {
                Console.WriteLine("Knjiga nije na listi zelja.");
                return;
            }

            Console.WriteLine($"Izabrana knjiga: {odabrana.Naziv}");
            Console.WriteLine($"ISBN: {odabrana.ISBN}");

            DateTime datum = UnesiDatum();
            int ocena = UnesiOcenu();
            Console.Write("Komentar (moze biti prazan): ");
            string komentar = Console.ReadLine() ?? "";

            Kupovina kupovina = _posetilacService.EvidentirajKupovinu(
                posetilacId, knjigaId, datum, ocena, komentar);

            Console.WriteLine($"Kupovina je sacuvana. ID: {kupovina.Id}");
        }

        // Ponistena kupovina vraca knjigu medju zelje.
        private void PonistiKupovinu()
        {
            int posetilacId = UnesiId("ID posetioca: ");
            Posetilac? p = PronadjiPovezanogPosetioca(posetilacId);

            if (p == null || p.Kupovine.Count == 0)
            {
                Console.WriteLine("Posetilac ne postoji ili nema kupovina.");
                return;
            }

            foreach (Kupovina k in p.Kupovine)
            {
                Console.WriteLine($"{k.KnjigaId} | {k.Knjiga?.Naziv}");
            }

            int knjigaId = UnesiId("ID knjige: ");
            if (!Potvrdi("Ponistiti kupovinu?")) return;

            bool uspesno = _posetilacService.PonistiKupovinu(posetilacId, knjigaId);
            Console.WriteLine(uspesno
                ? "Kupovina je ponistena."
                : "Kupovina nije pronadjena.");
        }

        // Unos ID-jeva za dodavanje autorske M:N veze.
        private void PoveziAutoraIKnjigu()
        {
            Console.WriteLine("Autori:");
            foreach (Autor a in _autori.UcitajSve()
                .OrderBy(a => a.Prezime, StringComparer.CurrentCultureIgnoreCase))
            {
                Console.WriteLine($"{a.Id} | {a.Ime} {a.Prezime}");
            }

            Console.WriteLine("Knjige:");
            foreach (Knjiga k in _knjige.UcitajSve()
                .OrderBy(k => k.Naziv, StringComparer.CurrentCultureIgnoreCase))
            {
                Console.WriteLine($"{k.Id} | {k.Naziv}");
            }

            int autorId = UnesiId("ID autora: ");
            int knjigaId = UnesiId("ID knjige: ");

            bool dodato = _autorKnjigaService
                .DodajAutoraNaKnjigu(autorId, knjigaId);

            Console.WriteLine(dodato
                ? "Veza je sacuvana."
                : "Ova veza vec postoji.");
        }

        // Postavi ili promeni izdavaca odabrane knjige.
        private void DodeliKnjiguIzdavacu()
        {
            int knjigaId = UnesiId("ID knjige: ");
            int izdavacId = UnesiId("ID izdavaca: ");
            bool uspesno = _izdavacService.DodeliKnjiguIzdavacu(knjigaId, izdavacId);
            Console.WriteLine(uspesno ? "Knjiga povezana sa izdavacem." :
                "Knjiga je vec kod ovog izdavaca.");
        }

        // Odaberi sefa uz iskustvo i knjigu kod izdavaca.
        private void PostaviSefaIzdavaca()
        {
            int izdavacId = UnesiId("ID izdavaca: ");
            int autorId = UnesiId("ID autora (min. 5 godina iskustva): ");
            bool uspesno = _izdavacService.PostaviSefaIzdavaca(izdavacId, autorId);
            Console.WriteLine(uspesno ? "Sef je postavljen." : "Sef je vec isti autor.");
        }

        // Ukloni autorsku vezu uz proveru poslovnih pravila.
        private void UkloniAutoraSaKnjige()
        {
            int autorId = UnesiId("ID autora: ");
            int knjigaId = UnesiId("ID knjige: ");

            if (!Potvrdi("Ukloniti vezu autora i knjige?")) return;

            // Pravila za poslednjeg autora i sefa izdavaca su u Core-u.
            bool uspesno = _knjigaPravila.UkloniAutoraSaKnjige(
                autorId, knjigaId);

            Console.WriteLine(uspesno
                ? "Veza je uklonjena."
                : "Veza nije pronadjena.");
        }
    }
}
