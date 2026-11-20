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
    // Deo konzolnog menija: cs.
    public partial class KonzolniMeni
    {
        private readonly XmlDao<Adresa> _adrese = new("Adrese.xml");
        private readonly XmlDao<Autor> _autori = new("Autori.xml");
        private readonly XmlDao<Knjiga> _knjige = new("Knjige.xml");
        private readonly XmlDao<Posetilac> _posetioci = new("Posetioci.xml");
        private readonly XmlDao<Izdavac> _izdavaci = new("Izdavaci.xml");
        private readonly XmlDao<Kupovina> _kupovine = new("Kupovine.xml");
        private readonly PosetilacService _posetilacService = new();
        private readonly AutorKnjigaService _autorKnjigaService = new();
        private readonly IzdavacService _izdavacService = new();
        private readonly KnjigaPravilaService _knjigaPravila = new();
        private readonly KatalogIzmenaService _katalogIzmena = new();
        private readonly KontrolisanoBrisanjeService _brisanje = new();
        private readonly RazmenaPodatakaService _razmena = new();

        /// <summary>
        /// Glavna petlja konzole: izbor opcije, poziv funkcije i obrada greske.
        /// </summary>
        public void Pokreni()
        {
            Console.OutputEncoding = Encoding.UTF8;
            while (true)
            {
                PrikaziGlavniMeni();
                string izbor = Console.ReadLine()?.Trim() ?? "";
                if (izbor == "0")
                {
                    Console.WriteLine("Program je zavrsen.");
                    return;
                }

                try
                {
                    switch (izbor)
                    {
                        case "1": MeniDodaj(); break;
                        case "2": MeniIzmeni(); break;
                        case "3": MeniObrisi(); break;
                        case "4": UveziSve(); break;
                        case "5": IzveziSve(); break;
                        case "6": MeniPregled(); break;
                        case "7": MeniPoslovneOperacije(); break;
                        case "8": MeniUpiti(); break;
                        default: Console.WriteLine("Nepostojeca opcija."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("Operacija nije izvrsena: " + ex.Message);
                }

                Console.WriteLine();
                Console.Write("Pritisni Enter za povratak u glavni meni...");
                Console.ReadLine();
            }
        }

        // Ispisi pocetne opcije bez pozivanja poslovne logike.
        private static void PrikaziGlavniMeni()
        {
            Console.WriteLine();
            Console.WriteLine("============================================");
            Console.WriteLine("             SAJAM KNJIGA - OISISI");
            Console.WriteLine("============================================");
            Console.WriteLine("1. DODAJ entitet");
            Console.WriteLine("2. IZMENI entitet");
            Console.WriteLine("3. OBRISI entitet (kontrolisano)");
            Console.WriteLine("4. UCITAJ PODATKE iz zbirnog XML-a (UVOZ)");
            Console.WriteLine("5. IZVEZI PODATKE u jedan XML");
            Console.WriteLine("6. PREGLED podataka");
            Console.WriteLine("7. POSLOVNE OPERACIJE");
            Console.WriteLine("8. PRETRAGA, SORTIRANJE, STRANICE I POSEBNI UPITI");
            Console.WriteLine("0. IZLAZ");
            Console.WriteLine("--------------------------------------------");
            Console.Write("Izaberi opciju: ");
        }

        // Usmeri unos korisnika na dodavanje trazenog tipa entiteta.
        private void MeniDodaj()
        {
            Console.WriteLine("\n===== DODAJ =====");
            Console.WriteLine("1. Autor");
            Console.WriteLine("2. Knjiga");
            Console.WriteLine("3. Posetilac");
            Console.WriteLine("4. Izdavac");
            Console.WriteLine("5. Adresa");
            Console.WriteLine("6. Kupovina sa liste zelja");
            Console.WriteLine("0. Nazad");
            Console.Write("Izbor: ");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": DodajNovogAutora(); break;
                case "2": DodajNovuKnjigu(); break;
                case "3": DodajNovogPosetioca(); break;
                case "4": DodajNovogIzdavaca(); break;
                case "5": DodajNovuAdresu(); break;
                case "6": EvidentirajKupovinu(); break;
                case "0": return;
                default: Console.WriteLine("Nepostojeca opcija."); break;
            }
        }

        // Usmeri unos korisnika na izmenu odgovarajuceg entiteta.
        private void MeniIzmeni()
        {
            Console.WriteLine("\n===== IZMENI =====");
            Console.WriteLine("1. Autor");
            Console.WriteLine("2. Knjiga");
            Console.WriteLine("3. Posetilac");
            Console.WriteLine("4. Izdavac");
            Console.WriteLine("5. Adresa");
            Console.WriteLine("6. Kupovina (ocena, datum, komentar)");
            Console.WriteLine("0. Nazad");
            Console.Write("Izbor: ");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": IzmeniAutora(); break;
                case "2": IzmeniKnjigu(); break;
                case "3": IzmeniPosetioca(); break;
                case "4": IzmeniIzdavaca(); break;
                case "5": IzmeniAdresu(); break;
                case "6": IzmeniKupovinu(); break;
                case "0": return;
                default: Console.WriteLine("Nepostojeca opcija."); break;
            }
        }

        // Potvrdi brisanje i pozovi servis koji proverava zavisnosti.
        private void MeniObrisi()
        {
            Console.WriteLine("\n===== OBRISI (KONTROLISANO) =====");
            Console.WriteLine("1. Autor");
            Console.WriteLine("2. Knjiga");
            Console.WriteLine("3. Posetilac");
            Console.WriteLine("4. Izdavac");
            Console.WriteLine("5. Adresa");
            Console.WriteLine("6. Ponisti kupovinu (vraca u zelje)");
            Console.WriteLine("0. Nazad");
            Console.Write("Izbor: ");
            string? izbor = Console.ReadLine()?.Trim();
            if (izbor == "0") return;
            if (izbor == "6") { PonistiKupovinu(); return; }
            if (izbor != "1" && izbor != "2" && izbor != "3" &&
                izbor != "4" && izbor != "5")
            {
                Console.WriteLine("Nepostojeca opcija.");
                return;
            }
            int id = UnesiId("ID za brisanje: ");
            if (!Potvrdi("Trajno obrisati izabrani entitet?")) return;

            switch (izbor)
            {
                case "1":
                    Console.WriteLine(_brisanje.ObrisiAutora(id)
                        ? "Autor je obrisan." : "Autor nije obrisan.");
                    break;
                case "2":
                    Console.WriteLine(_brisanje.ObrisiKnjigu(id)
                        ? "Knjiga i njene veze su obrisane." : "Knjiga nije obrisana.");
                    break;
                case "3":
                    Console.WriteLine(_brisanje.ObrisiPosetioca(id)
                        ? "Posetilac je obrisan." : "Posetilac nije obrisan.");
                    break;
                case "4":
                    Console.WriteLine(_brisanje.ObrisiIzdavaca(id)
                        ? "Izdavac je obrisan." : "Izdavac nije obrisan.");
                    break;
                case "5":
                    Console.WriteLine(_brisanje.ObrisiAdresu(id)
                        ? "Adresa je obrisana." : "Adresa nije obrisana.");
                    break;
            }
        }

        // Ponudi preglede podataka i vezanih detalja.
        private void MeniPregled()
        {
            Console.WriteLine("\n===== PREGLED =====");
            Console.WriteLine("1. Svi entiteti (sortirano za prikaz)");
            Console.WriteLine("2. Detalji posetioca");
            Console.WriteLine("3. Trenutno stanje radnih XML fajlova");
            Console.WriteLine("0. Nazad");
            Console.Write("Izbor: ");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": PrikaziSveEntitete(); break;
                case "2": PrikaziDetaljePosetioca(); break;
                case "3": PrikaziStanje(); break;
                case "0": return;
                default: Console.WriteLine("Nepostojeca opcija."); break;
            }
        }

        // Ponudi zelje, kupovine i veze autora, knjiga i izdavaca.
        private void MeniPoslovneOperacije()
        {
            Console.WriteLine("\n===== POSLOVNE OPERACIJE =====");
            Console.WriteLine("1. Dodaj knjigu na listu zelja");
            Console.WriteLine("2. Ukloni knjigu sa liste zelja");
            Console.WriteLine("3. Evidentiraj kupovinu");
            Console.WriteLine("4. Ponisti kupovinu");
            Console.WriteLine("5. Povezi autora i knjigu");
            Console.WriteLine("6. Ukloni autora sa knjige");
            Console.WriteLine("7. Dodeli knjigu izdavacu");
            Console.WriteLine("8. Postavi sefa izdavaca");
            Console.WriteLine("9. Proveri brisanje (bez brisanja)");
            Console.WriteLine("0. Nazad");
            Console.Write("Izbor: ");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": DodajNaListuZelja(); break;
                case "2": UkloniSaListeZelja(); break;
                case "3": EvidentirajKupovinu(); break;
                case "4": PonistiKupovinu(); break;
                case "5": PoveziAutoraIKnjigu(); break;
                case "6": UkloniAutoraSaKnjige(); break;
                case "7": DodeliKnjiguIzdavacu(); break;
                case "8": PostaviSefaIzdavaca(); break;
                case "9": ProveriBrisanje(); break;
                case "0": return;
                default: Console.WriteLine("Nepostojeca opcija."); break;
            }
        }
    }
}
