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
    // Deo konzolnog menija: razmena.
    public partial class KonzolniMeni
    {
        // Izvezi jedan XML paket bez menjanja radnih podataka.
        private void IzveziSve()
        {
            Console.WriteLine("\n===== IZVEZI PODATKE =====");
            Console.WriteLine("Podrazumevana putanja:");
            Console.WriteLine(_razmena.PodrazumevanaPutanja);
            Console.Write("Nova putanja (Enter = podrazumevana): ");
            string putanja = Console.ReadLine()?.Trim() ?? "";
            string odrediste = string.IsNullOrWhiteSpace(putanja)
                ? _razmena.PodrazumevanaPutanja : putanja;
            if (System.IO.File.Exists(odrediste) &&
                !Potvrdi("Zbirni XML vec postoji. Prepisati ga?"))
            {
                Console.WriteLine("Izvoz otkazan.");
                return;
            }
            string sacuvano = _razmena.Izvezi(odrediste);
            Console.WriteLine("Podaci su izvezeni u JEDAN XML fajl:");
            Console.WriteLine(sacuvano);
            Console.WriteLine("Radni XML fajlovi nisu promenjeni.");
        }

        // Ucitaj i proveri paket pre zamene postojecih podataka.
        private void UveziSve()
        {
            Console.WriteLine("\n===== UCITAJ PODATKE IZ ZBIRNOG XML-a =====");
            Console.WriteLine("Ova operacija moze ZAMENITI postojece radne podatke.");
            Console.WriteLine("Podrazumevana putanja:");
            Console.WriteLine(_razmena.PodrazumevanaPutanja);
            Console.Write("Putanja do XML fajla (Enter = podrazumevana): ");
            string putanja = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(putanja))
                putanja = _razmena.PodrazumevanaPutanja;

            // Citanje i validacija bez menjanja radnih fajlova.
            SajamPaket paket = _razmena.ProcitajPaket(putanja);
            Console.WriteLine("XML je procitan i prosao provere.");
            Console.WriteLine($"Autori: {paket.Autori.Count}, knjige: {paket.Knjige.Count}");
            Console.WriteLine($"Posetioci: {paket.Posetioci.Count}, izdavaci: {paket.Izdavaci.Count}");
            Console.WriteLine($"Adrese: {paket.Adrese.Count}, kupovine: {paket.Kupovine.Count}");
            Console.WriteLine($"AutorKnjiga veze: {paket.AutorKnjigaVeze.Count}");
            Console.WriteLine($"Lista zelja: {paket.StavkeListeZelja.Count}");
            Console.WriteLine();
            Console.WriteLine("PAZNJA: trenutno stanje ce biti zamenjeno podacima iz ovog XML-a!");
            Console.Write("Za potvrdu upisi tacno ZAMENI: ");
            if (Console.ReadLine()?.Trim() != "ZAMENI")
            {
                Console.WriteLine("Uvoz otkazan. Nista nije menjano.");
                return;
            }

            string rezerva = _razmena.Uvezi(paket);
            Console.WriteLine("Uvoz je uspesan. Podaci su ponovo ucitani.");
            Console.WriteLine("Rezervna kopija prethodnih podataka:");
            Console.WriteLine(rezerva);
        }
    }
}
