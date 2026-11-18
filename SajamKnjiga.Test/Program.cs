
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Services;
using System;
using System.IO;
using static SajamKnjiga.Core.Models.Posetilac;

// Koristimo poseban XML fajl za ovaj test
string putanja = Path.Combine(
    Path.GetTempPath(),
    "SajamKnjigaTest",
    "crud_posetioci.xml"
);

// Brisemo prethodne test podatke
if (File.Exists(putanja))
{
    File.Delete(putanja);
}

PosetilacServis servis = new PosetilacServis(putanja);

// 1. DODAVANJE
Posetilac posetilac = new Posetilac
{
    Id = 1,
    Ime = "Marko",
    Prezime = "Markovic",
    Email = "marko@example.com",
    BrojClanskeKarte = "SK-001",
    GodinaClanstva = 2026,
    Status = StatusPosetioca.Redovan
};

servis.Dodaj(posetilac);

Console.WriteLine("1. DODAVANJE");
Console.WriteLine("Posetilac je dodat.");

// 2. PRIKAZ
Console.WriteLine("\n2. PRIKAZ");

foreach (Posetilac p in servis.PrikaziSve())
{
    Console.WriteLine(p.Id + " - " + p.Ime + " " + p.Prezime);
}

// 3. IZMENA
Console.WriteLine("\n3. IZMENA");

posetilac.Ime = "Nikola";
servis.Izmeni(posetilac);

foreach (Posetilac p in servis.PrikaziSve())
{
    Console.WriteLine(p.Id + " - " + p.Ime + " " + p.Prezime);
}

// 4. BRISANJE
Console.WriteLine("\n4. BRISANJE");

var kupovinaServis =
    new KupovinaServis("Podaci/kupovine.xml");

var listaZeljaServis =
    new ListaZeljaServis("Podaci/listaZelja.xml");

servis.Obrisi(1, kupovinaServis, listaZeljaServis);
