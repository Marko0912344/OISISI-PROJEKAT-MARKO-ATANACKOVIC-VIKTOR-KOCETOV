// Prosireni scenariji poslovnih pravila, validacija i oporavka transakcija.
using SajamKnjiga.Core.DAO;
using SajamKnjiga.Core.Models;
using SajamKnjiga.Core.Persistence;
using SajamKnjiga.Core.Services;

int broj = 0;
void Check(bool ok, string naziv)
{
    if (!ok) throw new InvalidOperationException("NEUSPESNO: " + naziv);
    broj++;
    Console.WriteLine("PASS " + naziv);
}
void Throws<TException>(Action operation, string naziv) where TException : Exception
{
    try { operation(); }
    catch (TException) { Check(true, naziv); return; }
    throw new InvalidOperationException("Ocekivan izuzetak " + typeof(TException).Name + ": " + naziv);
}

try
{
    // Strogo izolovani podaci unutar output foldera ovog testnog projekta.
    foreach (string folder in new[] { "Data", "Transakcije" })
    {
        string putanja = Path.Combine(AppContext.BaseDirectory, folder);
        if (Directory.Exists(putanja)) Directory.Delete(putanja, recursive: true);
    }
    XmlTransakcija.OporaviPrekinute();
    var prazni = new List<Posetilac>();
    Check(KatalogUpitiService.BrojStrana(0) == 1, "Prazna lista ima jednu vidljivu stranicu");
    Check(KatalogUpitiService.BrojStrana(16) == 1, "16 elemenata staje u jednu stranu");
    Check(KatalogUpitiService.BrojStrana(17) == 2, "17 elemenata daje dve strane");
    var dvadeset = Enumerable.Range(1, 20).ToList();
    Check(KatalogUpitiService.Stranica(dvadeset, 1).Count == 16, "Prva stranica = 16 redova");
    Check(KatalogUpitiService.Stranica(dvadeset, 2).SequenceEqual(new[] {17,18,19,20}), "Druga stranica = 4 reda");
    Throws<ArgumentOutOfRangeException>(() => KatalogUpitiService.Stranica(dvadeset, 3), "Nevalidna strana se odbija");

    var testPosetioci = new List<Posetilac>
    {
        new() { Id=1, Ime="Marko", Prezime="Markovic", BrojClanskeKarte="SK-1-2025", Status=StatusPosetioca.R, TrenutnaGodinaClanstva=2025, ProsecnaOcenaRecenzija=4.25 },
        new() { Id=2, Ime="Mara", Prezime="Petrovic", BrojClanskeKarte="SK-2-2024", Status=StatusPosetioca.V, TrenutnaGodinaClanstva=2024, ProsecnaOcenaRecenzija=5.0 },
        new() { Id=3, Ime="Jovan", Prezime="Markovic", BrojClanskeKarte="SK-3-2026", Status=StatusPosetioca.R, TrenutnaGodinaClanstva=2026, ProsecnaOcenaRecenzija=3.5 }
    };
    Check(KatalogUpitiService.FiltrirajPosetioce(testPosetioci,"mArKoV").Count == 2, "Posetioci - jedna rec trazi po prezimenu");
    Check(KatalogUpitiService.FiltrirajPosetioce(testPosetioci,"Markovic, Mark").Single().Id == 1, "Posetioci - dve reci prezime/ime");
    Check(KatalogUpitiService.FiltrirajPosetioce(testPosetioci,"sk-1, mark, MAR").Single().Id == 1, "Posetioci - tri reci karta/ime/prezime");
    Throws<ArgumentException>(() => KatalogUpitiService.FiltrirajPosetioce(testPosetioci,"test, , test"), "Pretraga sa praznim delom se odbija");
    Check(KatalogUpitiService.SortirajPosetioce(testPosetioci,"ocena",true).First().Id==2, "Prosecna ocena se sortira numericki");
    Check(KatalogUpitiService.SortirajPosetioce(testPosetioci,"godina",false).First().Id==2, "Godina clanstva se sortira numericki");
    Check(KatalogUpitiService.SortirajPosetioce(testPosetioci,"karta",false).First().Id==1, "Clanska karta se sortira tekstualno");
    Check(KatalogUpitiService.SortirajPosetioce(testPosetioci,"status",true).First().Id==2, "Status moze opadajuce");

    var testAutori = new List<Autor>
    {
        new() { Id=1, Ime="Milica", Prezime="Petrovic", Email="m@example.com" },
        new() { Id=2, Ime="Marko", Prezime="Petrovic", Email="a@example.com" }
    };
    Check(KatalogUpitiService.FiltrirajAutore(testAutori,"petrovic").Count==2, "Autori - jedna rec");
    Check(KatalogUpitiService.FiltrirajAutore(testAutori,"petrovic, mil").Single().Id==1, "Autori - dve reci");
    Check(KatalogUpitiService.SortirajAutore(testAutori,"email",false).First().Id==2, "Autori - email sortiranje");
    Throws<ArgumentException>(() => KatalogUpitiService.FiltrirajAutore(testAutori,"a,b,c"), "Autori - vise od dva dela se odbija");

    var testKnjige = new List<Knjiga>
    {
        new() { Id=1, ISBN="978-A", Naziv="Zima", Cena=200m, GodinaIzdanja=2022 },
        new() { Id=2, ISBN="978-B", Naziv="Leto", Cena=35m, GodinaIzdanja=2019 }
    };
    Check(KatalogUpitiService.FiltrirajKnjige(testKnjige,"978-b").Single().Id==2, "Knjige - deo ISBN-a");
    Check(KatalogUpitiService.FiltrirajKnjige(testKnjige,"let").Single().Id==2, "Knjige - deo naziva");
    Check(KatalogUpitiService.SortirajKnjige(testKnjige,"cena",false).First().Id==2, "Knjige - numericko sortiranje cene");
    Check(KatalogUpitiService.SortirajKnjige(testKnjige,"godina",true).First().Id==1, "Knjige - numericko sortiranje godine");

    // Validacija ISBN-a: standardni ISBN-10/13, kontrolna cifra, format, razmaci.
    Check(IsbnPravila.Normalizuj("0-306-40615-2") == "0306406152", "ISBN-10 kontrolna cifra");
    Check(IsbnPravila.Normalizuj("978-0-306-40615-7") == "9780306406157", "ISBN-13 kontrolna cifra");
    Throws<InvalidOperationException>(() => IsbnPravila.Normalizuj("9780306406158"), "ISBN-13 pogresna kontrolna cifra");
    Throws<InvalidOperationException>(() => IsbnPravila.Normalizuj("0-306-40615-3"), "ISBN-10 pogresna kontrolna cifra");
    Throws<InvalidOperationException>(() => IsbnPravila.Normalizuj("978-B"), "ISBN ne prima proizvoljan tekst");

    // Nije dovoljan samo neprazan unos: validacija mora vaziti i direktno u Core-u.
    var ispravanAutor = new Autor
    {
        Ime="Test", Prezime="Autor", DatumRodjenja=new DateTime(1990,1,1),
        Telefon="060 111 222", Email="test.autor@example.com", BrojLicneKarte="TEST-LK-2026",
        GodineIskustva=6
    };
    var ispravanPosetilac = new Posetilac
    {
        Ime="Test", Prezime="Posetilac", DatumRodjenja=new DateTime(2000,1,1),
        Telefon="060 333 444", Email="test.posetilac@example.com", BrojClanskeKarte="TEST-2026",
        TrenutnaGodinaClanstva=2026, Status=StatusPosetioca.R
    };
    ValidacijaOsoba.ProveriAutora(ispravanAutor);
    ValidacijaOsoba.ProveriPosetioca(ispravanPosetilac);
    Check(true,"Core prihvata validne osobe");
    ispravanAutor.Email="nije-email";
    Throws<InvalidOperationException>(()=>ValidacijaOsoba.ProveriAutora(ispravanAutor),"Autor - nevalidan email");
    ispravanAutor.Email="test.autor@example.com";
    ispravanAutor.DatumRodjenja=DateTime.Today.AddDays(1);
    Throws<InvalidOperationException>(()=>ValidacijaOsoba.ProveriAutora(ispravanAutor),"Autor - datum rodjenja u buducnosti");
    ispravanAutor.DatumRodjenja=new DateTime(1990,1,1);
    ispravanAutor.Telefon=" ";
    Throws<InvalidOperationException>(()=>ValidacijaOsoba.ProveriAutora(ispravanAutor),"Autor - telefon obavezan");
    ispravanAutor.Telefon="060 111 222";
    ispravanPosetilac.Status=(StatusPosetioca)999;
    Throws<InvalidOperationException>(()=>ValidacijaOsoba.ProveriPosetioca(ispravanPosetilac),"Posetilac - enum status R/V");
    ispravanPosetilac.Status=StatusPosetioca.R;
    ispravanPosetilac.TrenutnaGodinaClanstva=DateTime.Today.Year+1;
    Throws<InvalidOperationException>(()=>ValidacijaOsoba.ProveriPosetioca(ispravanPosetilac),"Posetilac - buduca godina clanstva");
    ispravanPosetilac.TrenutnaGodinaClanstva=DateTime.Today.Year;
    ispravanPosetilac.Email="pogresan-email";
    Throws<InvalidOperationException>(()=>ValidacijaOsoba.ProveriPosetioca(ispravanPosetilac),"Posetilac - nevalidan email");
    ispravanPosetilac.Email="test.posetilac@example.com";

    // Import proverene solo DEMO datoteke, bez dodirivanja radnih podataka korisnika.
    var razmena = new RazmenaPodatakaService();
    var demo = razmena.ProcitajPaket(Path.Combine(AppContext.BaseDirectory,"DEMO_SajamKnjiga.xml"));
    razmena.Uvezi(demo);
    var q = new KatalogUpitiService();

    // Provere na servisima, ne samo na pomocnim validatorima.
    var unos = new KatalogUnosService();
    var adresa = new Adresa("Test ulica", "1", "Novi Sad", "Srbija");
    ispravanAutor.Email="bez-et";
    Throws<InvalidOperationException>(()=>unos.DodajAutora(ispravanAutor, adresa),
        "Dodavanje autora kroz Core odbija nevalidan email");
    ispravanAutor.Email="test.autor@example.com";
    ispravanPosetilac.Status=(StatusPosetioca)999;
    Throws<InvalidOperationException>(()=>unos.DodajPosetioca(ispravanPosetilac, adresa),
        "Dodavanje posetioca kroz Core odbija nevalidan status");
    ispravanPosetilac.Status=StatusPosetioca.R;
    Check(new XmlDao<Adresa>("Adrese.xml").UcitajSve().Count == demo.Adrese.Count,
        "Nevalidne osobe ne ostavljaju nove adrese");

    var izmenaOsoba = new KatalogIzmenaService();
    var autorZaIzmenu = new XmlDao<Autor>("Autori.xml").PronadjiPoId(1)!;
    string emailPre = autorZaIzmenu.Email;
    autorZaIzmenu.Email="nevalidan";
    Throws<InvalidOperationException>(()=>izmenaOsoba.IzmeniAutora(autorZaIzmenu),
        "Izmena autora kroz Core odbija nevalidan email");
    Check(new XmlDao<Autor>("Autori.xml").PronadjiPoId(1)!.Email == emailPre,
        "Nevalidna izmena nije sacuvana");
    var posetilacZaIzmenu = new XmlDao<Posetilac>("Posetioci.xml").PronadjiPoId(1)!;
    posetilacZaIzmenu.TrenutnaGodinaClanstva = DateTime.Today.Year + 1;
    Throws<InvalidOperationException>(()=>izmenaOsoba.IzmeniPosetioca(posetilacZaIzmenu),
        "Izmena posetioca kroz Core odbija buducu godinu");

    var knjigaPravila = new KnjigaPravilaService();
    var knjigaZaIzmenu = new XmlDao<Knjiga>("Knjige.xml").PronadjiPoId(2)!;
    knjigaZaIzmenu.ISBN="9781234560010";  // isti ISBN kao knjiga 1
    Throws<InvalidOperationException>(()=>knjigaPravila.IzmeniKnjigu(knjigaZaIzmenu),
        "Izmena knjige odbija duplirani ISBN");
    knjigaZaIzmenu.ISBN="9781234560028"; // neispravna kontrolna cifra
    Throws<InvalidOperationException>(()=>knjigaPravila.IzmeniKnjigu(knjigaZaIzmenu),
        "Izmena knjige odbija neispravnu kontrolnu cifru");
    var knjigaZaUnos = new Knjiga
    { ISBN="9781234560011", Naziv="Nevalidna", Zanr="Roman", GodinaIzdanja=2020, Cena=100m, BrojStrana=100 };
    Throws<InvalidOperationException>(()=>unos.DodajKnjigu(knjigaZaUnos, 1, new[] {1}),
        "Dodavanje knjige odbija neispravnu kontrolnu cifru");

    // Namerni neuspeh POSLE prvog XML upisa: transakcija mora vratiti oba fajla.
    string dataFolder = Path.Combine(AppContext.BaseDirectory,"Data");
    string fajlKnjige = Path.Combine(dataFolder,"Knjige.xml");
    string fajlPosetioci = Path.Combine(dataFolder,"Posetioci.xml");
    byte[] preKnjige = File.ReadAllBytes(fajlKnjige);
    byte[] prePosetioci = File.ReadAllBytes(fajlPosetioci);
    Throws<InvalidOperationException>(()=>XmlTransakcija.Izvrsi<int>(
        new[]{"Knjige.xml", "Posetioci.xml"}, () =>
        {
            File.WriteAllText(fajlKnjige,"NEUSPESAN UPIS");
            File.WriteAllText(fajlPosetioci,"NEUSPESAN UPIS 2");
            throw new InvalidOperationException("Simulirani pad druge faze upisa");
        }), "Transakcija propagira simulirani neuspeh");
    Check(File.ReadAllBytes(fajlKnjige).SequenceEqual(preKnjige) &&
          File.ReadAllBytes(fajlPosetioci).SequenceEqual(prePosetioci),
        "Rollback vraca oba XML fajla u izvorno stanje");
    Check(q.Posetioci("savic, viktor", "prezime", false).Single().Id==1, "Integrisana pretraga posetioca");
    Check(q.Autori("petrovic, milica", "ime", false).Single().Id==1, "Integrisana pretraga autora");
    Check(q.Knjige("astronomije", "naziv", false).Single().Id==4, "Integrisana pretraga knjige");
    Check(q.KnjigeAutora(1).Select(k=>k.Id).OrderBy(x=>x).SequenceEqual(new[]{1,2}), "8.21 - knjige autora");
    Check(q.PosetiociKojiZeleAutora(1).Select(p=>p.Id).OrderBy(x=>x).SequenceEqual(new[]{1,3}), "8.27 - deduplikovani posetioci autora");
    Check(q.PosetiociKojiZeleAutora(1,"popovic, luka").Single().Id==3, "8.27 - pretraga unutar posetilaca autora");
    Check(q.AutoriKojePosetilacZeli(1).Select(a=>a.Id).OrderBy(x=>x).SequenceEqual(new[]{1,3}), "8.28 - autori sa liste zelja");
    Check(q.AutoriKojePosetilacZeli(1,"markovic").Single().Id==3, "8.28 - pretraga unutar autora sa liste zelja");
    var parZelja = q.UporediDveKnjige(2,4);
    Check(parZelja.ObeNaListiZelja.Select(p=>p.Id).SequenceEqual(new[]{1}), "8.29 - obe knjige na listi zelja");
    var parKupovina = q.UporediDveKnjige(1,3);
    Check(parKupovina.KupiliPrvuNeDrugu.Select(p=>p.Id).SequenceEqual(new[]{1}), "8.29 - kupili prvu, ne drugu");
    Throws<ArgumentException>(() => q.UporediDveKnjige(1,1), "8.29 - knjige moraju biti razlicite");
    Check(q.KnjigeIzdavaca(1).Count==3, "8.30 - knjige izdavaca");
    Check(q.AutoriIzdavaca(1).Select(a=>a.Id).OrderBy(x=>x).SequenceEqual(new[]{1,2,3}), "8.31 - deduplikovani autori izdavaca");
    Check(q.AutoriIzdavaca(1,"jovanovic").Single().Id==2, "8.31 - pretraga autora izdavaca");
    var pre = q.KupljeneKnjige(1);
    Check(pre.Kupovine.Count==1 && pre.UkupnaPotrosnja is null && pre.ProsecnaOcena==5d,
        "8.13 - stara kupovina bez cene NE prikazuje izmisljenu potrosnju");
    Check(new KatalogIzmenaService().EvidentirajIstorijskuCenu(1,1290m),
        "Dokumentovana istorijska cena moze se uneti za stari zapis");
    Check(q.KupljeneKnjige(1).UkupnaPotrosnja==1290m,
        "8.13 - precizna potrosnja nakon dopune istorijske cene");
    Throws<InvalidOperationException>(()=>new KatalogIzmenaService().EvidentirajIstorijskuCenu(1,2000m),
        "Cena stare kupovine ne moze se ponovo prepisati");
    Throws<InvalidOperationException>(()=>new KatalogIzmenaService().EvidentirajIstorijskuCenu(2,-1m),
        "Istorijska cena ne sme biti negativna");
    Check(new KatalogIzmenaService().EvidentirajIstorijskuCenu(2,0m),
        "Besplatna kupljena knjiga moze imati istorijsku cenu 0");
    Check(q.KupljeneKnjige(2).UkupnaPotrosnja == 0m,
        "Nulta dokumentovana cena ne zamenjuje se trenutnom cenom");
    var izmenaKupovine = new KatalogIzmenaService();
    var staraKupovina = new XmlDao<Kupovina>("Kupovine.xml").PronadjiPoId(1)!;
    staraKupovina.Ocena = 3;
    Check(izmenaKupovine.IzmeniKupovinu(staraKupovina), "Izmena ocene postojece kupovine");
    Check(q.KupljeneKnjige(1).ProsecnaOcena == 3d &&
          new XmlDao<Posetilac>("Posetioci.xml").PronadjiPoId(1)!.ProsecnaOcenaRecenzija == 3d,
          "Prosek se menja zajedno sa ocenom kupovine");
    staraKupovina.Ocena = 5;
    Check(izmenaKupovine.IzmeniKupovinu(staraKupovina), "Vracanje ocene kupovine");
    var posetilacServis = new PosetilacService();
    var nova = posetilacServis.EvidentirajKupovinu(1,4,DateTime.Today,4,"Spec test");
    Check(nova.CenaPriKupovini==1890m, "Cena kupovine se cuva pri evidentiranju");
    var sadasnja = new XmlDao<Knjiga>("Knjige.xml").PronadjiPoId(4)!;
    sadasnja.Cena = 3890m;
    new KnjigaPravilaService().IzmeniKnjigu(sadasnja);
    Check(q.KupljeneKnjige(1).UkupnaPotrosnja==3180m,
        "Istorijska potrosnja ostaje ista posle izmene cene knjige");
    Check(posetilacServis.PonistiKupovinu(1,4), "Ponistavanje kupovine");
    Check(q.KupljeneKnjige(1).UkupnaPotrosnja==1290m,
        "Potrosnja se azurira nakon ponistavanja kupovine");
    var veliki = razmena.ProcitajPaket(Path.Combine(AppContext.BaseDirectory, "DEMO_SajamKnjiga_VELIKI.xml"));
    razmena.Uvezi(veliki);
    Check(q.Posetioci("", "karta", false).Count == 240, "Veliki demo - 240 posetilaca");
    Check(q.Autori("", "prezime", false).Count == 240, "Veliki demo - 240 autora");
    Check(q.Knjige("", "cena", false).Count == 320, "Veliki demo - 320 knjiga");
    Check(KatalogUpitiService.BrojStrana(q.Knjige("", "naziv", false).Count) == 20,
        "Veliki demo - knjige imaju 20 stranica");
    var sortirane = q.Knjige("", "cena", false);
    var zadnja = KatalogUpitiService.Stranica(sortirane, 20);
    Check(zadnja.Count == 16 && zadnja.All(k => k.Cena >= sortirane[303].Cena),
        "Veliki demo - poslednja stranica sortiranja je ispravna");

    // Dodatne negativne provere nad velikim paketom (bez upisa ako nije validan).
    var velikiNeispravan = razmena.ProcitajPaket(Path.Combine(
        AppContext.BaseDirectory, "DEMO_SajamKnjiga_VELIKI.xml"));
    velikiNeispravan.Kupovine[0].CenaPriKupovini = -10m;
    Throws<InvalidDataException>(()=>razmena.Uvezi(velikiNeispravan),
        "Uvoz odbija negativnu istorijsku cenu bez izmene podataka");
    Check(q.Knjige("", "cena", false).Count == 320,
        "Nevalidan uvoz nije promenio radni katalog");

    // Sef izdavaca 1 u malom DEMO-u ima knjige 1 i 2.
    // Kada uklonimo jednu vezu, drugu ne smemo ukloniti (ostao bi bez knjige).
    razmena.Uvezi(demo);
    var vezePravila = new KnjigaPravilaService();
    Check(vezePravila.UkloniAutoraSaKnjige(1,1),
        "Uklanjanje veze je dozvoljeno kada sef ima drugu knjigu");
    Throws<InvalidOperationException>(()=>vezePravila.UkloniAutoraSaKnjige(1,2),
        "Ne moze se ukloniti poslednja knjiga sefa izdavaca");
    Check(new XmlStorage<AutorKnjiga>("AutorKnjiga.xml").Ucitaj()
            .Any(v=>v.AutorId==1 && v.KnjigaId==2),
        "Zabranjena izmena nije uklonila poslednju vezu");

    Console.WriteLine($"USPEH: svih {broj} specifikacionih provera proslo.");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    Environment.ExitCode=1;
}
