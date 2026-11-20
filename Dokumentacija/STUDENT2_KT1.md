# Student 2 — prva kontrolna tacka (konzola)

## Obuhvat

Ovaj dokument prati rad na grani `student2-rad` timskog OISISI projekta.
WPF interfejs nije deo prve kontrolne tacke.

## Organizacija

- `SajamKnjiga.Core/Models/`: autor, knjiga, adresa i pomocne klase veza.
- `SajamKnjiga.Core/DAO/`: genericki pristup entitetima.
- `SajamKnjiga.Core/Persistence/`: XML datoteke, ID brojevi i oporavak transakcija.
- `SajamKnjiga.Core/Services/Student2*`: poslovna logika Studenta 2.
- `SajamKnjiga.Console/Student2Meni.cs`: konzolne opcije Studenta 2.
- `Testovi/Student2.Tests/`: izdvojeni automatizovani testovi.
- `DemoPodaci/`: fiktivni XML primeri za rucno testiranje.

## Provere

Pre objavljivanja nove revizije pokrenuti:

```powershell
dotnet build .\SajamKnjigaBeta.slnx
dotnet run --project .\Testovi\Student2.Tests\Student2.Tests.csproj
git diff --check
git status --short
```

U korisnickom interfejsu proveriti prikaz/CRUD autora, knjiga i veze autor–knjiga, pretragu/sortiranje/paginaciju, zelje, kupovine i posebne upite.

## Integracija sa Studentom 1

- Ne menjati koleginu granu niti prenositi njegov kod u ovu granu bez dogovora.
- Uskladiti modele `Posetilac`, `Kupovina`, `Izdavac` i XML polja pre spajanja.
- Zajednicki Build i test spojenog programa su posebna faza.

Rezultat automatskog testiranja na korisnikovom racunaru: **33/33 provera proslo 17.11.2026**. To nije dokaz da je zajednicka integracija gotova.
