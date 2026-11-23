# Arkitektur

## Status

Implementeret. Filen beskriver projekternes opdeling, opstart, fælles
mønstre, baggrundsjob, migrationsregler og frontend-build. Databaseskemaet står
i [database.md](database.md), og deploy og udgivelse i afsnittet Deployment i
[AGENTS.md](../AGENTS.md).

## Nuværende adfærd

### Projekter og lag

| Projekt | Ansvar |
| --- | --- |
| `src/DS.Domain` | Modeller, DTO'er, `Roles.cs`, `Labels.cs`, `DSVersion.cs` og DbUp-scripts i `Migrations/*.sql`. Afhænger ikke af de andre projekter. |
| `src/DS.Application` | Services grupperet efter funktion under `Services/<Område>` (fx `Boards`, `Groups`, `Users`, `Profile`, `Organization`, `Exports`, `Google`), `IDataDbContext` under `Data`, `ServiceResult`, formularskabeloner og seeders. Kender hverken HTTP-contexten, SignalR, Hangfire-klienten eller Google SDK-typer. |
| `src/DS.Infrastructure` | `DataDbContext`, entity-konfiguration, Identity-udvidelser, OpenIddict-klientservice, Google-klienter, ClosedXML-udtræk og Hangfire-jobs. |
| `src/DS.Backend` (Website) | Controllers, autentifikation, MCP-endpoint, SignalR-hub, `ClaimsTransformer`, HTTP-pipeline, DbUp-migrering og rolleseeding. |
| `src/DS.Worker` | Generic Host, der udfører Hangfire-jobs. |
| `src/DS.RegistrationImporter` | Import af historiske tilmeldinger, se [historic-registration.md](features/historic-registration.md). |
| `src/DS.Frontend` | Vue, Nuxt UI og Tailwind, serveret på `/`. |

Controllers beholder routes, autorisationsattributter, inputvalidering og
oversættelse til HTTP-svar. Arbejdsgange ligger i Application-services, som
får brugerens id og effektive rettigheder som en actor-record fra Website (fx
`BoardActor`, `MeActor`, `OrgActor`, `SearchActor`). Application bestiller
sideeffekter gennem interfaces, som Infrastructure eller Website implementerer:
`IMailQueue` (`HangfireMailQueue`), `IFileProcessingQueue`
(`HangfireFileProcessingQueue`), `IBoardEventPublisher` (SignalR i Website) og
`IUserAccessCache` (rydder `ClaimsTransformer`-cachen).

### Opstart og DI

- `builder.ConfigureDS()` i `src/DS.Backend/Extensions/ServiceExtensions.cs`
  registrerer Websites services og konfiguration. `Program.cs` beholder
  HTTP-pipelinen, routing, DbUp-migrering og rolleseeding.
- `src/DS.Infrastructure/ConfigureInfrastructure.cs` samler den fælles
  opsætning, som Website og Worker deler:
  - `AddDsDataLayer(connectionString)` registrerer `DataDbContext` (Npgsql,
    snake_case, `UseOpenIddict`), `IDataDbContext` og OpenIddict Core.
  - `ConfigureDsIdentityOptions()` sætter adgangskodekrav, unik email,
    `AuthenticatorIssuer` og `SchemaVersion.Version3`.
  - `AddDsIdentityCore()` registrerer Identity Core med roller og EF-lager og
    returnerer `IdentityBuilder`, så en host selv kan kæde videre.
  - `AddDsHangfireStorage(connectionString)` registrerer PostgreSQL-lageret
    med schema `hangfire` og `PrepareSchemaIfNecessary = false`.
  - `ConfigureDsInfrastructure` registrerer de øvrige Infrastructure-services
    og jobs.
- Website bruger `AddDsDataLayer`, `AddDsHangfireStorage`, sin fulde
  `AddIdentity<User, Role>` med `NotificationUserManager` og
  `LoginTrackingSignInManager` og til sidst `ConfigureDsIdentityOptions()`, så
  de delte options ikke overskrives. Worker bruger `AddDsDataLayer`,
  `ConfigureDsIdentityOptions()` og `AddDsHangfireStorage`.

### Delt resultattype

Services returnerer `ServiceResult` eller `ServiceResult<T>` fra
`src/DS.Application/Services/ServiceResult.cs` med et `ServiceOutcome` og et
valgfrit `Error` (`object`, så Identity-fejl kan sendes med). `ServiceResult<T>`
har en implicit konvertering fra den ikke-generiske type. Controllers kalder
`this.ToActionResult(result)` eller `this.ToActionResult(result, noContent: true)`
fra `src/DS.Backend/Extensions/ControllerResultExtensions.cs`:
`Success` giver `Ok`/`NoContent`, `Invalid` `BadRequest(Error)`, `NotFound`
`NotFound(Error)`, `Forbidden` `Forbid`, `Conflict` `Conflict(Error)`, og et
ukendt udfald giver 500. Domænespecifikke `*Result`-typer bruges ikke.

### Fælles gruppeopslag og låse

`src/DS.Application/Services/Groups/GroupLookup.cs` samler gentagne opslag:
`GetUserWithGroupAsync` på `UserManager<User>`, `IsPreSignupOpenAsync`
(tager `FOR SHARE` på indstillingsrækken, når `lockSettings` er sat),
`LockGroupForUpdateAsync` (`FOR UPDATE` på gruppen) og konstanten
`RegistrationSettingsId` (rækken med id 1). Rå låse-SQL skal bruge de
faktiske navne i `ds28`-schemaet (`ds28.registration_settings`,
`ds28.scout_group`), fordi forbindelsens `search_path` ikke indeholder `ds28`.

### Worker og Hangfire

- `DS.Worker` er en selvstændig Generic Host i samme Compose-projekt som
  Website. Den kører Hangfire-serveren med fem jobtråde og
  `ServerName = Dns.GetHostName()`, så flere workercontainere får hver sit
  navn. Compose sætter derfor ikke et fast hostname.
- Website enqueuer jobs og viser dashboardet, men udfører ikke jobs.
  Tilbagevendende jobs registreres kun i Worker
  (`src/DS.Infrastructure/RecurringJobRegistrar.cs`).
- Worker kører ikke migrationer. Før den registrerer og udfører jobs, venter
  den på, at alle indlejrede `DS.Migrations.*`-scripts står i DbUp-journalen
  (`src/DS.Worker/MigrationReadiness.cs`). Den har kun OpenIddict Core til
  oprydningsjobbet og ingen HTTP-server.
- Jobs: mailafsendelse (`MailJobs.Send`, retry 30/60/120/240 s, 5 forsøg),
  billedkonvertering (`FileProcessingJobs.ConvertToWebp`), historisk import
  og det daglige `prune-openiddict` (03:00 UTC), som fjerner ugyldige eller
  udløbne OpenIddict-tokens og -autorisationer, der er mindst syv dage gamle.
- Hangfire-schemaet ejes af DbUp (`007_hangfire.sql`), så Hangfire-opgraderinger
  tilføjes som almindelige migrationer.
- Kørsel lokalt og med Compose er beskrevet i [README.md](../README.md).

### Database og migrationer

Skemaet er beskrevet i [database.md](database.md). Reglerne for ændringer er:

- DbUp ejer al DDL. SQL-scripts ligger i `src/DS.Domain/Migrations`, indlejres
  i Domain-assemblyet og køres af Website ved opstart i én transaktion. En
  fejl stopper opstarten med scriptnavn og fejlbesked. EF-migrations og
  `Database.Migrate()` bruges ikke.
- Skemaændringer tilføjes som nye nummererede scripts. Allerede udførte
  scripts redigeres eller omdøbes ikke.
- DbUp genkender scripts på resource-navnet `DS.Migrations.*`.
  `DS.Domain.csproj` fastlægger `LogicalName`, så navnene bevares, selvom
  projektet er flyttet. Det skal bevares ved fremtidige flytninger.
- Alle tabeller ligger i `ds28`-schemaet, også Identity og OpenIddict, og
  journalen i `ds28.schemaversions`.
- EF-modellen konfigureres med én `IEntityTypeConfiguration<T>` pr. model i
  `src/DS.Infrastructure/Data/Configurations` og `UseSnakeCaseNamingConvention()`.
  Kun afvigelser fra konventionen mappes eksplicit. Bool-felter med
  `DEFAULT FALSE` får `HasSentinel(false)`.
- `DataDbContext` håndhæver soft delete for `ISoftDeleteable` (en `Remove`
  bliver til `deleted_at`, og et globalt query-filter skjuler slettede rækker),
  og `AuditSaveChangesInterceptor` sætter `created_at` og `updated_at`.
  `IgnoreQueryFilters()` bruges kun, hvor slettede rækker skal ses.
- Migrationer skal være bagudkompatible i én udgivelse (expand/contract), så
  forrige version kan deployes igen. Se Deployment i [AGENTS.md](../AGENTS.md).
- Overgangen fra v0.1.6-skemaet i `public` til `ds28` blev gennemført i
  produktion med v0.2.0. Scripts `000_legacy_v016.sql`, `006_copy_v016.sql` og
  `006a_relax_legacy_nullable.sql` er bevaret for journalens skyld. Den
  arkiverede [deployvejledning](archive/deploy-v016-database.md) gælder ikke
  nye deploys.

### Frontend

- `src/DS.Frontend` bruger Vue, Nuxt UI 4, Tailwind CSS 4 og Lucide-ikoner
  (`@iconify-json/lucide`, bundtet ved build). Vite bygger til
  `src/DS.Backend/wwwroot/dist`, og Website serverer appen på `/` med en
  fallback til `index.html`.
- `npm run build` kører `vue-tsc -b` før `vite build`, så typefejl stopper
  buildet, også i containerbygget. TypeScript holdes på `~5.9.3`, som virker
  med den nuværende `vue-tsc`. Typekontrollen må ikke fjernes fra buildet som
  genvej.
- Alle HTTP-kald går gennem `src/DS.Frontend/src/lib/api.ts`: én delt
  `apiClient` (samme origin, `withCredentials`, JSON) med `apiGet`, `apiPost`,
  `apiPut`, `apiDelete`, `safeApiCall`, `safeApiResult`, `safeApiOk`,
  `getErrorMessage` og `getErrorStatus`. Views kalder services i
  `src/DS.Frontend/src/Services`, ikke axios direkte. Hver service bevarer sin
  egen fallback ved fejl. Klienten sender antiforgery-tokenet, se
  [security.md](security.md).
- Containerbygget henter Node fra `docker.io/library/node:24` og .NET fra
  Microsoft Container Registry (`Containerfile.Website`,
  `Containerfile.Worker`).
- `src/DS.Frontend.Archive` er den arkiverede Buefy-frontend. Den bygges og
  serveres ikke.

### Udviklingsværktøjer

- Graphify bygger en lokal kodegraf (`graphify extract . --code-only`) uden
  API-nøgle. Opsætningen står i [README.md](../README.md).
- Repositoryet har ingen `tests/`-mappe. Der verificeres med builds og ved at
  køre appen. Node-tests til MCP-værktøjet ligger ved siden af koden i
  `tools/ds28-mcp`.

## Beslutninger

- 2026-09-30: C#-migrations udfases. DbUp kører SQL-scripts, og
  modelkonfiguration samles i konfigurationsklasser med snake_case. Afløser
  EF-migrations og den samlede `db.sql`.
- 2026-10-01: `ds28` er standardschema for hele modellen. Overgangen fra
  v0.1.6 kopierer fra `public` til `ds28` og lader de gamle tabeller stå.
  Afløser planen om at flytte de gamle tabeller til et arkivschema.
- 2026-10-16: Node-imaget hentes fra Docker Hub i stedet for ECR Public, som
  afviste hentninger med HTTP 429.
- 2026-10-19: Arbejdsgange flyttes fra controllers til Application-services,
  og Infrastructure registreres samlet. Routes, adgang og skema er uændrede.
- 2026-10-19: `DS.Worker` udfører Hangfire-jobs. Website enqueuer kun. Deploy
  publicerer begge images. Afløser, at kun Website blev publiceret.
- 2026-10-19: `LogicalName` fastlåser `DS.Migrations.*`. Retter, at flytningen
  til Domain ændrede resource-navnene, så DbUp ville køre overgangen igen.
- 2026-10-20: Den fælles DI-opstart samles i `ConfigureInfrastructure.cs`.
  Afløser fire næsten ens kopier.
- 2026-10-20: Fælles `ServiceResult` og `ToActionResult`. Afløser
  domænespecifikke resultattyper og private `Respond`-switches.
- 2026-10-20: Fælles frontend-API-klient. Ingen fil uden for `lib/api.ts`
  importerer axios.
- 2026-10-20: Fælles `GroupLookup`. Retter rå låse-SQL, som brugte forkerte
  tabelnavne og fejlede ved hvert kald.
- 2026-10-20: Soft delete og audit håndhæves centralt i `DataDbContext`.
  Afløser manuel sætning i hver service.
- 2026-11-15: Graphify bruges lokalt uden modelbackend.
- 2026-11-17, DS-19: Typekontrol er en del af `npm run build`. Afløser den
  midlertidige udeladelse fra 2026-10-02.
- 2026-11-21: Nuxt UI-frontenden er den eneste frontend, se
  [frontend.md](features/frontend.md).
- 2026-11-23, DS-116: Ingen `tests/`-mappe. Automatiske tests tilføjes kun
  efter aftale.

## Åbne spørgsmål

- Om en senere TypeScript 7-migration giver mening, når værktøjerne er
  modne.
- Ved flere Website-instanser kræver SignalR en backplane, og MCP-sessioner
  et delt lager. Det er ikke nødvendigt med én instans.

## Kodehenvisninger

- `src/DS.Backend/Program.cs`
- `src/DS.Backend/Extensions/ServiceExtensions.cs`
- `src/DS.Backend/Extensions/ControllerResultExtensions.cs`
- `src/DS.Infrastructure/ConfigureInfrastructure.cs`
- `src/DS.Infrastructure/Data/DataDbContext.cs`
- `src/DS.Infrastructure/Data/AuditSaveChangesInterceptor.cs`
- `src/DS.Infrastructure/Data/Configurations`
- `src/DS.Infrastructure/Jobs`
- `src/DS.Application/Services/ServiceResult.cs`
- `src/DS.Application/Services/Groups/GroupLookup.cs`
- `src/DS.Domain/Migrations`
- `src/DS.Domain/DS.Domain.csproj`
- `src/DS.Worker/Program.cs`
- `src/DS.Worker/MigrationReadiness.cs`
- `src/DS.Frontend/src/lib/api.ts`
- `src/DS.Frontend/vite.config.ts`
