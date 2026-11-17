# DEMO podaci — iskljucivo za testiranje

Ovaj folder sadrzi devet XML fajlova sa **fiktivnim** primerima za testiranje.

**Nemojte direktno pokretati aplikaciju nad DemoPodaci**: runtime fajlovi se citaju iz `SajamKnjiga.Console/bin/Debug/net10.0/Data`.

## Ucitavanje

1. Zaustaviti konzolnu aplikaciju.
2. Napraviti rezervnu kopiju postojeceg runtime foldera `Data` (i transakcionog dnevnika ako postoji).
3. Kopirati svih devet `.xml` fajlova iz `DemoPodaci` u runtime `Data` folder.
4. Ponovo pokrenuti konzolnu aplikaciju.

Ove podatke je dozvoljeno resetovati prostim ponovnim kopiranjem. Ne izvrsavati Git commit nad runtime `Data` folderom.

## Ogranicenja

Format XML-a odgovara demonstracionim modelima koji se trenutno koriste u grani Studenta 2. Nakon integracije sa drugim studentom treba proveriti kompatibilnost sa zajednickim modelima kupovine, posetioca i izdavaca.
