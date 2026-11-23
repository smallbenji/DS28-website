# Filer, billeder og Google Drev

## Status

Implementeret: fil-lager til profil- og katalogbilleder og aktivitetsfiler i
Google Drev via bruger-OAuth. Ikke bygget: frontend til katalogbilleder,
skabeloner til Drev-filer og omdøbning, flytning eller sletning af Drev-filer
fra HQ.

## Nuværende adfærd

### Billeder

- `FileService` (`src/DS.Application/Services/Files`) håndterer upload,
  læsning og sletning gennem `IDataDbContext` og `IFileStorage`. Metadata
  ligger i `stored_file` (se [database.md](../database.md)); filerne ligger på
  lokal disk i Docker-volumen `files` (`/app/files`, `DS__FileStoragePath`)
  under nøgler, der ikke kan pege uden for lagerroden. `IFileStorage` gør det
  muligt at skifte til fx S3 senere.
- Upload gemmer den rå fil midlertidigt med status `Pending` og enqueuer
  `FileProcessingJobs.ConvertToWebp`. Jobbet retter EXIF-orientering,
  centercropper profilbilleder til 1:1, skalerer til højst 2000 px, fjerner
  metadata og koder WebP (kvalitet 80). Rækken bliver `Ready`, og originalen
  slettes. Ved fejl bliver den `Failed`, og Hangfire genprøver (30/120/300 s,
  3 forsøg). Biblioteket er SkiaSharp.
- Endpoints under `/api/v1/files`: `POST /{purpose}` (`catalog` eller
  `profile`), `GET /public/{publicId}` (anonym, kun offentlige og `Ready`),
  `GET /{publicId}` (login) og `DELETE /{id}` (uploader eller SysAdmin).
- Katalogbilleder er offentlige via et ugætteligt `public_id`. Profilbilleder
  kræver login.
- Et billede knyttes med `ImageReferenceDto`. En aktivitets
  `catalog_data.image_file_id` sættes kun til et `Ready` katalogbillede
  uploadet af samme bruger. `PUT /api/v1/me/profile-picture` sætter eget
  profilbillede; `null` rydder det. `PUT /api/v1/user/{id}/profile-picture`
  kræver `UsersEditProfilePicture` (kun SysAdmin), og billedet skal være
  uploadet af den handlende bruger.
- Profil og brugerstyring beskærer med `ProfilePictureUpload.vue` og
  venter på `Ready` ved at polle fil-URL'en (`waitUntilReady` i
  `src/DS.Frontend/src/Services/FileService.ts`). `UserAvatar.vue` viser
  initialer, når der ikke er et billede.

### Google Drev for aktiviteter

- En admin forbinder én Google-konto i lejrindstillingerne
  (`/admin/camp-settings`; `/admin/google` sender videre). Filer oprettes på
  vegne af den konto, som ejer dem. Refresh-tokenet gemmes krypteret med Data
  Protection i `google_connection`. Indstillinger: `DS:GoogleClientId`,
  `DS:GoogleClientSecret`, `DS:GoogleRedirectUri` og
  `DS:GoogleDriveBaseFolderId`. Scopet er `https://www.googleapis.com/auth/drive`.
- `GoogleApiController`: `GET /api/v1/google/status` kan læses af alle;
  `connect`, `callback` og `disconnect` kræver `AdminAccess`. Connect bruger en
  kortlivet state-cookie som CSRF-værn.
- Hver aktivitet får en undermappe under basemappen, mærket med
  `appProperties.activity_id` og gemt i `activity_drive_folder`, så mappen kan
  flyttes frit i Drev. Uden gemt ID søges der på mærket i hele Drev. Er mappen
  slettet, glemmes ID'et, og visningen bliver tom; næste oprettelse laver en
  ny. Visning opretter aldrig en mappe.
- Fanen Filer viser den aktuelle mappe live fra Drev (30 sekunders cache, ryddes
  ved oprettelse) med brødkrumme, mapper øverst og alle filtyper. Rækker
  beriges med `activity_file`, så "Oprettet af" vises for filer og mapper
  oprettet fra HQ. Filrækker åbner Google i en ny fane; mapperækker går ind i
  mappen i HQ.
- Der kan oprettes Docs, Sheets, Slides og mapper (`kind`: `doc`, `sheet`,
  `slides`, `folder`) i aktivitetsmappen og dens undermapper. Filer får
  aktivitetens navn som standardtitel, mapper "Ny mappe".
- Rettigheder følger aktiviteten: oprettelse kræver redigeringsadgang, læsning
  visningsadgang. `GET /api/v1/activity/activity/{id}/files` og
  `POST .../file/add` tager en valgfri `folderId`; en mappe uden for
  aktivitetsmappen (kontrolleret op til 20 led) giver 404.
- Ikke konfigureret eller ikke forbundet giver 503; timeout (20 s) eller fejl
  fra Google giver 502. Fanen viser en advarsel uden opret-formular, når der
  ikke er en forbundet konto.
- Filer slettes ikke i Drev, når aktiviteten slettes.
- `IGoogleAuthService` og `IGoogleDriveService` ligger i Application uden
  Google SDK-typer; implementeringerne og `IGoogleDriveClientFactory` ligger i
  `src/DS.Infrastructure/Google`.

## Beslutninger

- 2026-10-09: Generelt fil-lager med WebP-konvertering i et Hangfire-job,
  lokal disk og SkiaSharp. ImageSharp er fravalgt, fordi Release-builds kræver
  licensnøgle.
- 2026-10-15: Google Drev via bruger-OAuth. Service account med
  domain-wide delegation og delt drev er droppet (kræver
  Workspace-superadministrator eller flytning af lejrmappen), og
  `DS:GoogleServiceAccountPath` og `DS:GoogleDriveImpersonateUser` bruges ikke.
  En service account kan ikke eje filer i et almindeligt My Drive.
- 2026-10-15: Docs, Sheets og Slides, og fanen viser hele mappen live med
  cache. Et syncjob er fravalgt indtil videre.
- 2026-10-15: Indlejret visning er droppet, fordi filerne ikke deles med
  HQ-brugerne. Filer åbnes i Google.
- 2026-10-16: Mapper som fjerde type med navigation i undermapper.
- 2026-10-19: Google-services flyttes til Infrastructure med interfaces i
  Application.
- 2026-11-20: Profilbilleder beskæres med `vue-advanced-cropper` før upload.
- 2026-11-20 og DS-70: Google-forbindelsen ligger i lejrindstillingerne.
  Afløser den egne side og flisen "Google Drev".

## Åbne spørgsmål

- Skal OAuth-klient-ID, hemmelighed og redirect-URI komme fra miljøvariabler
  eller mount i stedet for `.env`?
- Hvilke skabeloner skal filer kunne oprettes fra, og hvordan udfyldes felter?
- Skal filer og mapper kunne omdøbes, slettes eller flyttes fra HQ, og skal
  Drev-mappen omdøbes med aktiviteten?
- Skal filer deles med aktivitetsteamet i Google, og med hvilke regler?
- Skal der vises advarsel, når refresh-tokenet er udløbet eller tilbagekaldt?
- Skal der senere være et syncjob til søgning og notifikationer på tværs af
  aktiviteter?
- Katalogbilleder: der er ingen frontend, et billede kan ikke ryddes via
  aktivitetsopdateringen, upload kræver kun login, og et billede i brug kan
  slettes. Der findes ikke et status-endpoint for behandling.

## Kodehenvisninger

- `src/DS.Application/Services/Files/FileService.cs`
- `src/DS.Application/FileStorage/IFileStorage.cs`
- `src/DS.Application/FileStorage/LocalFileStorage.cs`
- `src/DS.Application/FileStorage/WebpImageProcessor.cs`
- `src/DS.Infrastructure/Jobs/FileProcessingJobs.cs`
- `src/DS.Backend/Controllers/ApiControllers/FileApiController.cs`
- `src/DS.Backend/Controllers/ApiControllers/GoogleApiController.cs`
- `src/DS.Application/Services/Google`
- `src/DS.Infrastructure/Google/GoogleAuthService.cs`
- `src/DS.Infrastructure/Google/GoogleDriveService.cs`
- `src/DS.Frontend/src/Services/FileService.ts`
- `src/DS.Frontend/src/Components/User/ProfilePictureUpload.vue`
- `src/DS.Frontend/src/Components/GoogleDriveConnection.vue`
- `src/DS.Frontend/src/Views/ActivityDetail.vue`
