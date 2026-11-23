# Historiske tilmeldinger fra 2024

## Status

Implementeret. Importen er klargjort og konfigureret i produktion. Ved
seneste registrering var produktionsimporten ikke kørt; kontrollér
tabellerne, før du antager andet.

## Nuværende adfærd

### Import

- Importen omfatter kun forhåndstilmeldinger og endelige tilmeldinger fra det
  gamle Fynsdistrikt-projekt i Firestore. Den gemmes i `historic_presignup` og
  `historic_signup`, koblet til eksisterende DS28-grupper. Aktive
  tilmeldinger ændres ikke. Lejrår, gren, kilde-id, kontrolsum og
  importtidspunkt bevares.
- `DS.RegistrationImporter` følger med Website- og Worker-imaget. En bruger med
  `AdminAccess` starter importen som et on-demand Worker-job fra
  lejrindstillingerne. Jobbet bruger `--apply` og erstatter det konfigurerede
  lejrår transaktionelt, så genkørsel ikke giver dubletter. Kommandolinjen i
  Website-imaget kan bruges til prøvekørsel.
- Konfiguration: `DS__HistoricRegistration__FirebaseCredentials`,
  `DS__HistoricRegistration__Year`, valgfrit `__GroupMap`,
  `__ExcludeSignupDocuments` og `__ExcludeGroupNumbers`. Credentials og
  gruppekort kan være JSON-indhold eller en filsti. Start afvises, hvis
  credentials eller lejrår mangler. Credentials og datafiler medfølger ikke
  image eller repository.
- Importeren læser direkte fra Firestore med service-account-nøgler eller
  Firebase-klient-JSON og normaliserer i hukommelsen.
- Grupper matches på entydigt navn eller via `GroupMap` fra gammelt
  gruppenummer til DS28-id. Ingen grupper oprettes.
- Forhåndstal og venskabsgruppetal gemmes pr. gren. Endelige tal gemmes pr.
  gren med dag 0 som patruljesummer og dag 1 til 8 som tilmeldt fremmøde.
  Manglende dagsrækker er ukendt, ikke nul. Familiespejdere er en egen gren.
- Dubletter vælges ikke automatisk; et dokument udelades eksplicit.

### Visning

- Gruppestyringen viser for `AdminAccess` én kompakt tabel med forhåndstal,
  endelige patruljesummer (dag 0), ændring og totaler. Venskabsgruppetal kan
  tilvælges, og fremmøde for dag 1 til 8 er foldet sammen. Manglende tal vises
  som ukendt, en total kun når alle viste grene har tal, og der summeres aldrig
  på tværs af fremmødedage.
- `GET /api/v1/groups/{id}/registration-history` kræver `GroupsView` og
  `AdminAccess` og returnerer kun antal, gren, lejrår og dagsindeks. Gruppens
  egne endpoints indeholder ingen historik.
- Adminområdets måltal bruger de importerede tal, se
  [admin-exports.md](admin-exports.md).

### Produktionsdata

- `FirebaseCredentials` og `Year=2024` er sat i produktion. Kildens
  gruppenumre er identiske med DS28's gruppe-id'er, så `GroupMap` kobler de 19
  grupper med data direkte på nummer. Landenavne (1001 til 1017 og 1111) og
  `TestGruppe` (123456) udelades via `ExcludeGroupNumbers`.
- Svendborgs oprindelige endelige tilmelding fra 26. maj 2024 (10 deltagere)
  udelades via dokument-id, så opdateringen fra 10. juli (12 deltagere)
  importeres.
- Gruppe 3607 (Sct. Knuds, FIONIA) er oprettet i DS28, fordi kilden har 13
  endelige deltagere til den. En ny import kræver, at gruppen findes.
- Kørsel, databetydning og dubletter er beskrevet i
  [importvejledningen](../../tools/historic_registration/README.md).

## Beslutninger

- 2026-11-14: Første import omfatter kun forhånds- og endelige tilmeldinger i
  separate historiktabeller.
- 2026-11-15: Importen læser direkte fra Firestore. Et indbygget JSON-udtræk
  og et separat Python-eksporttrin er fravalgt.
- 2026-11-15: Importen konfigureres med miljøvariabler og køres som Worker-job
  startet fra Admin.
- 2026-11-15, DS-22 og DS-32: Historikken vises i gruppestyringen i én kompakt
  tabel uden nuværende forhåndstilmelding. DS-32 afløser DS-22's visning.
- 2026-11-21, DS-70: Synkroniseringsknappen flyttes til lejrindstillingerne.

## Åbne spørgsmål

- Øvrige gamle datatyper er uden for første import.
- Skal gruppe 3607 beholdes, hvis den kun findes for historikkens skyld?
- Summerne er ikke et verificeret antal unikke personer.

## Kodehenvisninger

- `src/DS.RegistrationImporter/Program.cs`
- `src/DS.RegistrationImporter/FirebaseSource.cs`
- `src/DS.RegistrationImporter/RegistrationNormalizer.cs`
- `src/DS.Infrastructure/Jobs/HistoricRegistrationSyncJob.cs`
- `src/DS.Domain/HistoricRegistrationSettings.cs`
- `src/DS.Domain/Migrations/027_historic_registration.sql`
- `src/DS.Backend/Controllers/ApiControllers/GroupsApiController.cs`
- `src/DS.Frontend/src/Components/Groups/GroupRegistrationHistory.vue`
- `src/DS.Frontend/src/Components/HistoricRegistrationSync.vue`
- `tools/historic_registration/README.md`
