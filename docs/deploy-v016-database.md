# Overgang fra v0.1.6 til ds28-skemaet

`ds28` er det permanente skema for den nye version: domænetabeller, Identity,
OpenIddict og DbUps `schemaversions`-tabel ligger her. EF har `ds28` som
standardskema. SQL bruger eksplicitte skemanavne og afhænger ikke af
forbindelsens `search_path`.

Overgangen kopierer fra det gamle v0.1.6-skema i `public`. De gamle tabeller,
sekvenser og `__EFMigrationsHistory` bevares på deres nuværende placering.
Der oprettes ikke et `ds28_legacy_v016`-arkiv. Dette erstatter den tidligere
plan om at flytte gamle tabeller til et arkivskema og oprette de nye i public.

## Før deploy

1. Tag en databasebackup, der kan gendannes, og behold den nuværende release.
   Bevar også de eksisterende Data Protection-nøgler og OpenIddict-certifikater.
2. Gendan backuppen i en separat testdatabase og prøv opstart af den nye release
   mod den. Brug testkonfiguration, så mailworker og integrationer ikke sender
   rigtige mails eller tilgår produktion. Kontrollér resultatet og de vigtigste
   flows inden produktionsdeploy.
3. Stop alle gamle appinstanser og andre processer, der skriver i databasen.
   Overgangen kræver nedetid; gammel og ny release må ikke køre samtidig.
4. Deploy med samme databaseforbindelse. Migrationsbrugeren skal kunne oprette
   `ds28`, læse og låse de gamle tabeller og læse deres sekvensværdier.
   Start kun én appinstans under overgangen.

## Understøttet udgangspunkt

Alle migrations i `v0.1.6` skal være kørt. Den sidste migration kan hedde enten
`20260929112910_AddEmailOutbox` som i Git-tagget eller
`20260929075734_AddEmailOutbox` som i den rapporterede database. Resten af
historikken skal matche præcist. `EmailOutbox` kontrolleres for de forventede
kolonner, datatyper og nullability; den tidligere migrationsfil er ikke fundet
i Git-historikken, så vi antager ikke, at dens kode var identisk.

En tom database understøttes også. En database, der allerede er oprettet i
`ds28` gennem de nye DbUp-scripts, genkøres uden at kopiere data igen.

Hvis den tidligere overgang til snake_case-tabeller i `public` allerede er
kørt, eller `ds28_legacy_v016` findes, stopper denne overgang. Det kræver en
særskilt flytning af det aktuelle skema og journalen; gamle data må ikke
kopieres over nyere data. Delvist oprettede måltabeller afvises ligeledes.

## Hvad der sker

DbUp får eksplicit `ds28` som skema og opretter journalen der. Alle ventende
scripts køres i én transaktion. Variabelsubstitution er slået fra, så
PostgreSQLs `$transition$`-blokke behandles som SQL.

- `000_legacy_v016.sql` kontrollerer hele EF-historikken og skemaets
  udgangspunkt. De gamle tabeller låses under kopieringen, men flyttes ikke.
- De seks oprettelsesscripts opretter de nye tabeller i `ds28`.
- `006_copy_v016.sql` kopierer data fra `public` til `ds28` og indstiller
  måltabellernes identity-sekvenser til mindst det højeste eksisterende id og
  den gamle sekvensværdi. De gamle sekvenser ændres ikke.
- `ds28.schemaversions` registrerer scripts med deres resource-navne.
  Ved næste opstart gentages overgangen ikke.

Bruger-id'er, password hashes, security stamps, roller, claims, eksterne
logins, 2FA-hemmeligheder, passkeys og OpenIddict-data kopieres uændret.
Lejrindstillinger og outboxens sendestatus bevares. Budgetter flyttes fra
`Activities.Budget_Budget` til egne rækker. Kataloger kobles til aktiviteter
via de gamle `CatalogId`-værdier. Fødselsdatoer konverteres til
UTC-kalenderdatoer, og kønsværdier konverteres til store bogstaver.

Nye auditfelter får migreringstidspunktet som `created_at` og `updated_at`;
de er ikke historiske oprettelsesdatoer. Eksisterende tidsstempler bevares.

## Hvis overgangen afvises

Appen starter ikke, og transaktionen rulles tilbage. De gamle tabeller og
data i `public` ændres ikke. Fejlen viser scriptnavn og PostgreSQL-fejl.

Nogle nye krav er strengere end i v0.1.6: eksempelvis obligatoriske navne,
unikke medlemskaber og kategorinavne, positive materialemængder,
ikke-negative budgetter og materialers obligatoriske relationer.
Et katalog skal tilhøre præcis én aktivitet. Priser skal kunne repræsenteres
uden afrunding som `numeric(10, 2)`. Uforenelige data skal afklares før et nyt
forsøg; migreringen sletter ikke dubletter eller opfinder manglende værdier.

Efter en afvist overgang kan den gamle release startes igen, når den nye er
stoppet. Efter en vellykket overgang bliver `public` ikke længere opdateret.
Start derfor ikke blot den gamle release som rollback, hvis der er kommet
nye data i `ds28`; de nye skrivninger skal først afstemmes. En verificeret
backup er stadig nødvendig.

## Efter deploy

Kontrollér login, gruppetilknytning, roller, aktivitetsbudgetter og
lejrindstillinger. Kontrollér også OpenIddict-login fra forbundne tjenester
samt 2FA/passkeys, hvis de bruges.

De gamle tabeller i `public` bevares til kontrol og slettes ikke automatisk.
De indeholder stadig persondata og loginhemmeligheder og er ikke en løbende
kopi af det nye skema. Oprydning aftales særskilt efter validering.

## Lokal regressionstest

```sh
python3 tests/migration-verification/run.py
```

Testen kræver .NET 10, Git-tagget `v0.1.6` og PostgreSQL-binærfiler.
På macOS bruges som standard Postgres.app version 18. `POSTGRES_BIN` kan
sættes til en anden mappe med `initdb` og `pg_ctl`.

Testen opretter en midlertidig PostgreSQL-instans uden TCP-lytning. Den bygger
det gamle skema fra Git-historikken og tester begge historikvarianter,
nyinstallation, journalens skema, EF/Identity/OpenIddict i ds28, databevarelse,
identity-sekvenser, genkørsel og rollback. De gamle public-data sammenlignes
før/efter overgang og efter EF-skrivninger i det nye skema.
Projektets databaseforbindelse bruges ikke; testinstansen stoppes og fjernes.
Der er endnu ikke kørt en prøve på produktionsdata.
