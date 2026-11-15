# Historiske tilmeldinger direkte fra Firebase

`DS.RegistrationImporter` følger med Website-imaget i `/app/importer`.
Den læser `Grupper`, `Forhaand` og `Patruljer` direkte fra Firestore og behandler
data i hukommelsen. Der er ingen indbygget datafil, eksportfil eller Python-trin.
Importen køres manuelt og starter aldrig automatisk ved deploy.

## Credentials

Angiv en lokal JSON-fil med `--firebase-credentials` eller sæt
`GOOGLE_APPLICATION_CREDENTIALS` til filens sti. Filen skal være tilgængelig inde
i containeren. Montér den som en read-only secret i dit deploymiljø; credentials
skal ikke tilføjes til repository eller image.

To formater understøttes:

- En Google service-account-nøgle i JSON-format, som indeholder `type` lig med
  `service_account`, `project_id`, `client_email` og `private_key`. Den skal have
  læseadgang til den gamle Firestore-database. Der bruges Google OAuth med
  datastore-scope; importeren udfører kun GET-kald til Firestore.
- En Firebase-klientkonfiguration med `projectId` og `apiKey`. Eksempel med
  pladsholder: `{"projectId":"fynsdistrikt","apiKey":"DIN_API_KEY"}`. Det er
  konfigurationsværdierne fra det gamle projekts `src/firebase.tsx`, skrevet som
  JSON; TypeScript-filen læses ikke direkte. Denne nøgle giver ikke i sig selv
  adgang: eksisterende Firebase-regler skal tillade læsning af alle tre samlinger.

Projekt-id hentes fra credentials-filen. Nøgler, tokens og Firestore-fejlsvar
udskrives ikke og gemmes ikke i DS28-databasen. Credentials med andre typer,
herunder refresh-token-filer, afvises.

Se også [Firestores autentificeringsdokumentation](https://firebase.google.com/docs/firestore/use-rest-api).

## Kørsel efter deploy

Website skal først have kørt DbUp-migrationerne. Importeren bruger
`DS__ConnectionString` fra den kørende HQ-container. Eksemplerne antager, at
credentials-filen er monteret på `/run/secrets/historic-firebase.json`.

Start med en prøvekørsel:

```sh
docker compose exec -T hq dotnet importer/DS.RegistrationImporter.dll --firebase-credentials /run/secrets/historic-firebase.json --year 2024
```

Den viser kildeprojekt, kontrolsum, gruppematch, totaler og fejl. Returkode 0
betyder gyldig prøvekørsel; returkode 1 betyder, at en fejl eller uafklaret dublet
blokerer importen. Ingen data gemmes uden `--apply`.

Grupper matches på præcist navn blandt ikke-slettede DS28-grupper. Der bruges
ikke fuzzy matching, og ingen grupper oprettes. Hvis et navn er ændret eller
ikke er entydigt, kan koblingen angives med en JSON-fil fra gammelt gruppenummer
til eksisterende DS28-gruppe-id. Eksempel med illustrative id'er:

```json
{"1234": 17, "5678": 29}
```

```sh
docker compose cp historic-group-map.json hq:/tmp/historic-group-map.json
docker compose exec -T hq dotnet importer/DS.RegistrationImporter.dll --firebase-credentials /run/secrets/historic-firebase.json --year 2024 --group-map /tmp/historic-group-map.json
```

Flere gamle grupper må ikke pege på samme DS28-gruppe. Et forkert manuelt id
eller et id for en slettet gruppe afvises.

## Uafklaret dublet: Svendborg

Ved undersøgelsen 14. november 2026 indeholdt Firebase to dokumenter for Svendborg:

| Firestore-dokument | Junior | Trop | Ledere | Total |
| --- | ---: | ---: | ---: | ---: |
| `KoukBj10vHYgkqsgB3nR` | 5 | 2 | 3 | 10 |
| `aPJv57QJMp5Qx84Y08Dw` | 5 | 4 | 3 | 12 |

Der er ikke valgt en vinder. Importeren blokerer, indtil dubletten er afklaret.
Når et dokument er bekræftet som overflødigt, kan det udelades med
`--exclude-signup-document <dokument-id>`. Flaget kan gentages. Dokumenterne i
Firebase slettes eller ændres ikke.

Kør prøvekørslen med de valgte flag og derefter samme kommando med `--apply`:

```sh
docker compose exec -T hq dotnet importer/DS.RegistrationImporter.dll --firebase-credentials /run/secrets/historic-firebase.json --year 2024 --exclude-signup-document <bekræftet-overflødigt-dokument-id>
docker compose exec -T hq dotnet importer/DS.RegistrationImporter.dll --firebase-credentials /run/secrets/historic-firebase.json --year 2024 --exclude-signup-document <bekræftet-overflødigt-dokument-id> --apply
```

Er der også brug for `--group-map`, skal det flag bruges i begge kommandoer.
Hver kørsel læser Firebase igen. Kildekontrolsummen beregnes af de normaliserede
data, kildeprojekt og lejrår; den ændres, hvis dette grundlag ændrer sig mellem
prøvekørsel og import. Der gemmes ingen kopi af kildedata på disk.

`--apply` sletter og genindsætter kun det valgte lejrårs historiske rækker i
begge tabeller. Tag databasebackup før første produktionsimport. Genkørsel med
samme kildedata og flag giver samme deltagerdata uden dubletter;
importtidspunktet opdateres. Andre lejrår og aktive tilmeldinger berøres ikke.
Fejl ruller hele transaktionen tilbage. Samtidige importer af samme år
serialiseres med en PostgreSQL advisory lock; en serialization failure kræver
genkørsel. Firebase skal være tilgængelig ved både prøvekørsel og import.

## Testgrupper og andre bevidste udeladelser

Ved undersøgelsen indeholdt `TestGruppe`, gammelt gruppenummer `123456`,
43 forhåndstilmeldte og 1 endeligt tilmeldt. Den er ikke automatisk udeladt.
Brug `--exclude-group-number 123456` for at udelade dens rækker fra begge
historiktabeller. Flaget kan gentages og kombineres med de øvrige flag.
Et ukendt gruppenummer afvises. Udeladte grupper kræver ikke match i DS28.

## Databetydning

- Grupper, antal, kilde-id'er, lejrår, lejrdage, kontrolsum og importtidspunkt
  gemmes i DS28. Kontaktmails bruges kun i hukommelsen til at forbinde de gamle
  tilmeldinger med deres kildegrupper. Login, rettigheder, mails,
  fritekstkommentarer og patruljenavne importeres ikke.
- Forhåndstal og venskabsgruppers antal gemmes særskilt. Gæster lægges ikke
  automatisk til værtsgruppens antal.
- Tomme endelige dokumenter uden gruppekobling udelades kun, hvis de har nul
  deltagere inklusive ledere. Et ikke-tomt dokument uden kobling afvises.
- Dag 0 er summen af registrerede patruljestørrelser for grenen, ikke summen
  af fremmødedagene og ikke dokumentation for unikke personer.
- Dag 1–8 er lørdag start, søndag, mandag, tirsdag, onsdag, torsdag, fredag og
  lørdag slut. Dagstallene viser tilmeldt fremmøde, ikke faktisk indtjekning.
- Særskilte ledertal uden dagsoplysninger får kun dag 0. Manglende dagsrækker
  betyder ukendt fremmøde. Data med både lederpatruljer og særskilte
  ledertotaler afvises, fordi overlap ikke kan afgøres automatisk.
- Tomme tekstfelter for antal fortolkes som nul, som i det gamle eksportflow.
  Andre ugyldige tal og manglende/ugyldige patruljedage afvises.
- Ingen sammenligningsside er tilføjet. `ParticipantData` og det nuværende
  dashboard er fortsat uændrede og bruger ikke de nye tabeller endnu.

## Verifikation

Kør source- og normaliseringskontroller med syntetiske data, uden Firebase- eller
databaseadgang:

```sh
dotnet run --project tests/HistoricRegistrationChecks/HistoricRegistrationChecks.csproj --disable-build-servers
```

Kør også databasekontrollerne i en automatisk oprettet, isoleret PostgreSQL-container.
Scriptet kræver Docker og OpenSSL og genererer et tilfældigt testpassword ved hver kørsel:

```sh
bash tests/HistoricRegistrationChecks/run.sh
```

Kontrollerne dækker credentials-formater, paginering, bearer-token, ugyldige data,
ukendt lederfremmøde, kildefejl uden credential-læk, migrationen, dubletblokering,
prøvekørsel, genimport, gruppeudeladelse og transaktionel tilbagerulning.
Ingen rigtige historiske data eller credentials er inkluderet i testprojektet.
