# Adminområde, baggrundsjob og dataudtræk

## Status

Implementeret: adminområdet med tilmeldingsstatistik, baggrundsjob og
dataudtræk. Foreslået, ikke bygget: auditlog (nice to have).

## Nuværende adfærd

### Adminområdet

- `/admin` kræver `AdminAccess` (SysAdmin og CampAdmin). HQ viser én
  "Admin"-flise. Hvert punkt i adminområdet gater på sin egen rolle, så adgang
  til siden og til den enkelte funktion er adskilt.
- Sektionerne er Konti og grupper (Brugerstyring, Gruppestyring), Tilmelding
  (Lejrindstillinger, vist for `PreSignupManage` eller `AdminAccess`) og
  System (Dataudtræk, Baggrundsjob, OpenIddict-klienter). Google Drev og den
  historiske synkronisering ligger i lejrindstillingerne for `AdminAccess`.
- Aktivitetsmodulet og WordPress-login har egne HQ-fliser, fordi også andre
  roller bruger dem.

### Tilmeldingsstatistik

- Sidebaren sammenligner forhåndstilmelding og endelig tilmelding med et
  måltal. Forhåndstilmelding summerer `GroupPreSignup` for alle grupper og
  aldersgrupper; endelig tilmelding tæller rækker i `Scout`.
- Måltallene kommer fra de importerede historiske tilmeldinger for det seneste
  `camp_year`: summen af `participant_count` i `historic_presignup` (uden
  venskabsgrupper) og i `historic_signup` for dag 0 (patruljesummer). Begge
  tæller ledere med. Uden historik er `target` og `targetYear` `null`, status
  `no-target`, og tallet vises uden bjælke.
- Farver: grøn ved nået mål, gul ved mindst halvdelen, rød under halvdelen.
- Under statistikken vises antal brugere og grupper fra
  `GET /api/v1/admin/system-counts` (`AdminAccess`). Soft-slettede grupper
  tælles ikke.

### Baggrundsjob

- `/admin/jobs` viser Hangfires monitoring-API gennem `BackgroundJobService`
  og `GET /api/v1/background-jobs`: nøgletal, fejlede jobs med detaljer og
  stacktrace, tilbagevendende jobs, seneste aktivitet (polling hvert 5.
  sekund, pause når fanen er skjult) og 24 timers historik med filter, søgning
  og sideinddeling.
- Argumenter og fejltekster maskeres på serveren
  (`BackgroundJobArgumentMasker`): mailadresser og navne maskeres, og
  mailindhold, adgangskoder, tokens og hemmeligheder vises som skjulte.
- `HangfireView` giver læseadgang. »Prøv igen«, »Prøv alle igen«, »Slet« og
  »Kør nu« kræver `HangfireManage` (kun SysAdmin), håndhævet i API'et.
- `/hangfire` er bevaret som nødudgang og er read-only uden `HangfireManage`.
  Dashboardet viser argumenter i klartekst.

### Dataudtræk

- Udtræk defineres kun i backend som `DataExport`-klasser i
  `src/DS.Infrastructure/Exports`, registreret som `IDataExport` i
  `ServiceExtensions.cs`. `ExportService` i Application leverer listen
  (`GET /api/v1/exports`), og frontenden på `/admin/exports` renderer den, så et
  nyt udtræk ikke kræver frontendændringer.
- Adgang kræver `ExportsView` (SysAdmin og CampAdmin), og hvert udtræk kræver
  sin egen rolle, kontrolleret både i listen og ved download.
- Filer er `.xlsx` (ClosedXML) med frosne overskrifter, autofilter og
  kolonnebredder (`ExportSheetExtensions`).
- Udtræk i dag, begge med `GroupsView`:
  - `group-pre-signup`: grupper med distrikt og forhåndstilmeldingens syv
    aldersgrupper plus sum. Grupper uden forhåndstilmelding har tomme felter.
  - `participant-export`: navn og gruppe for alle spejdere i ikke-slettede
    grupper.
- Det logges ikke, hvem der henter et udtræk.
- Danske visningsnavne for enums ligger i `src/DS.Domain/Labels.cs`. Frontenden
  har stadig egne kopier, fx i `src/DS.Frontend/src/types/group.types.ts`.

### Auditlog

`AuditLogView` er tildelt SysAdmin og CampAdmin, men der findes ingen auditlog.

## Beslutninger

- 2026-09-29: Samlet adminområde på `/admin` bag `AdminAccess`, med én
  Admin-flise på HQ.
- 2026-09-29: Tilmeldingsstatistik i sidebaren med tærskler ved mål og
  halvdelen.
- 2026-09-29: Dataudtræk defineres i backend og renderes generisk. Spejdere,
  brugere, aktiviteter, patruljer og materialer blev valgt fra i første version.
- 2026-09-29: `AuditLogView` til SysAdmin og CampAdmin.
- 2026-10-19: `ExportService` i Application, ClosedXML og udtræk i
  Infrastructure, `Labels` i Domain.
- 2026-11-21, DS-70: Antal brugere og grupper i sidebaren. Google Drev og
  historisk synkronisering flyttes til lejrindstillingerne.
- 2026-11-21, DS-71: Måltal fra historiske tilmeldinger. Afløser det
  hardkodede `ParticipantData` (285).
- 2026-11-22, DS-83: Baggrundsjob i HQ med maskering og `HangfireManage`.
  Afløser Hangfire-flisen og at `HangfireView` kunne ændre jobs i dashboardet.

## Åbne spørgsmål

- `participant-export` indeholder spejdernes navne, men første version
  fravalgte udtræk med persondata, og der er ikke registreret en beslutning om
  det. Er udtrækket godkendt, og skal download af persondata logges?
- Omfatter nuværende endelig tilmelding (`Scout`) ledere på samme måde som dag
  0 i historikken? Måltallet er aggregerede tilmeldinger, ikke unikke personer.
- Skal sidebaren også rumme åbn/luk-funktionerne fra lejrindstillingerne?
- Skal frontenden læse enum-labels fra backenden i stedet for egne kopier?
- Auditlog: hvad skal logges, og hvornår skal den bygges?

## Kodehenvisninger

- `src/DS.Backend/Controllers/ApiControllers/AdminApiController.cs`
- `src/DS.Backend/Controllers/ApiControllers/BackgroundJobApiController.cs`
- `src/DS.Backend/Controllers/ApiControllers/ExportsApiController.cs`
- `src/DS.Backend/HangfireDashboardAuthorizationFilter.cs`
- `src/DS.Application/Services/BackgroundJobs/BackgroundJobService.cs`
- `src/DS.Application/Services/BackgroundJobs/BackgroundJobArgumentMasker.cs`
- `src/DS.Application/Services/Exports/ExportService.cs`
- `src/DS.Infrastructure/Exports/DataExport.cs`
- `src/DS.Infrastructure/Exports/GroupPreSignupExport.cs`
- `src/DS.Infrastructure/Exports/ParticipantExport.cs`
- `src/DS.Infrastructure/Exports/ExportSheetExtensions.cs`
- `src/DS.Domain/Labels.cs`
- `src/DS.Frontend/src/Views/Admin.vue`
- `src/DS.Frontend/src/Views/BackgroundJobs.vue`
- `src/DS.Frontend/src/Views/Exports.vue`
