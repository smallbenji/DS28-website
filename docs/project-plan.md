# DS28 – hjemmesideplan og senere beslutninger

## Status og brug

**Planen er foreløbig og ikke endegyldig, før den er vedtaget.**

Dette er en struktureret gengivelse af den projektplan, Benjamin begyndte
udviklingen ud fra, opdateret med senere beslutninger. Den beskriver formål,
ønskede funktioner og åbne spørgsmål.
Den er ikke en oversigt over færdigimplementerede funktioner. Senere beslutninger
og konkrete opgaver kan ændre planen.

Formålet med den oprindelige plan var at beskrive systemet så grundigt som
muligt, inden udviklingen gik i gang.

- Systemansvarlig: Benjamin Falch
- Foreslået vikar: Milas Holsting. Ikke vedtaget endnu.

## Overordnet idé

Flere mindre systemer skal arbejde sammen om at drive lejren digitalt.
Et samlet dashboard, HQ, skal samle apps, så staben ikke behøver at kende
de enkelte URL'er.

Fælles brugeradministration for grupper og stab bygger på ASP.NET Core Identity
og OpenIddict. Et gruppenummer forbinder en bruger med en spejdergruppe i
tilmeldingsmodulet. Flere brugere skal kunne tilhøre samme spejdergruppe.

Der skal desuden være et materialesystem og en offentlig hjemmeside baseret
på et CMS, så andre end IT kan vedligeholde indhold.

### Applikationsopstart

**Beslutning 2026-10-20 (implementeret):** Frontendens `MeStore.Me` er `null`,
indtil brugerdata er hentet. Komponenter håndterer denne indlæsningstilstand
uden at tildele rettigheder eller læse felter på `null`. Routerens kontrol af
`ActivityAdmin` afventer nu brugerdata ligesom de øvrige adgangskontroller.
`MeService` bevarer sit eksisterende fallback til en ikke-autentificeret bruger.

**Beslutning 2026-10-19 (implementeret):** `DS.Worker` er en selvstændig Generic
Host i samme Docker Compose-projekt som Website. Den starter Hangfire-serveren
med fem jobtråde og `ServerName = Dns.GetHostName()`, dvs. containerens hostname.
Der sættes ikke et fast hostname eller container_name i Compose, så flere
workercontainere kan have hver deres navn. Website enqueuer jobs og viser
dashboardet, men udfører ikke længere jobs. Tilbagevendende jobs registreres
kun i Worker. Begge bruger samme PostgreSQL/Hangfire-lager og delte filvolume.

Worker kører ikke migrationer: før registrering og jobudførelse venter den på,
at alle indlejrede `DS.Migrations.*`-scripts findes i DbUp-journalen. Website
beholder migrationer og rolleseeding. Compose `depends_on` giver startrækkefølgen;
workerens journalcheck sikrer også, at migrationerne er klar. Worker har kun
OpenIddict Core til oprydningsjobbet og ingen HTTP-server eller loginendpoints.
`Containerfile.Worker` bygger worker-imaget lokalt via Compose. Databaseskemaet
er uændret.

**Efterfølgende beslutning 2026-10-19 (implementeret):** Deploy-workflowet bygger
og publicerer både Website og Worker med en matrix. Website bevarer
`ghcr.io/smallbenji/ds28-website`; Worker bruger
`ghcr.io/smallbenji/ds28-website-worker`. Begge bruger samme SemVer-/latest-tags,
`linux/amd64`, GHCR-login og eksisterende tag-/manuelle triggers. Dette erstatter
den oprindelige begrænsning til kun at publicere Website. Compose bygger fortsat
lokalt fra de respektive Containerfiles.

**Rettelse 2026-10-19 (implementeret):** Flytningen fra DS til DS.Domain ændrede
utilsigtet SQL-resource-navne fra `DS.Migrations.*` til `DS.Domain.Migrations.*`.
DbUp identificerer udførte scripts ved disse navne og forsøgte derfor at køre
legacy-overgangen igen på en allerede migreret database. Domain-projektet
fastlægger nu `LogicalName` til de oprindelige `DS.Migrations.*`-navne.
Journalen og SQL-scripts ændres ikke; der slettes eller omdøbes ikke rækker i
en eksisterende database. Migrationernes identitet skal bevares ved fremtidige
projektflytninger. Dette retter resourcenavnene fra den tidligere flytning.

**Beslutning 2026-10-19 (implementeret):** Offentlig brugeroprettelse, glemt
adgangskode og nulstilling af adgangskode håndteres af `AccountService` under
`Application/Services/Accounts`. Auth- og nulstillingscontrollerne beholder
inputvalidering, routes og HTTP-svar; login, 2FA, passkeys og cookies forbliver
i Website. Glemt adgangskode svarer fortsat ens for kendte og ukendte emails.

Klientadministration bruger `IOpenIddictClientService` og resultattyper under
`Application/Services/OpenIddict`, implementeret af `OpenIddictClientService`
i Infrastructure. `OpenIddictClientPolicy` ligger i Application og deles med
autorisationsendpointet. Website beholder antiforgery, cacheforbud og
`OpenIddictManage`-adgangskrav. Validering, descriptorbevarelse, secrets og
fejlsvar er uændrede. Identity-udvidelsen `NotificationUserManager` ligger
under `Infrastructure/Identity` og registreres fortsat som UserManager, så
notifikationer stadig dækker alle Identity-baserede oprettelsesveje.
Infrastructure registreres samlet via `ConfigureDsInfrastructure`, inklusive
de eksisterende jobs. Der ændres ikke databaseskema eller adgangsregler.

**Beslutning 2026-10-20 (implementeret):** Den gentagne DI-opstart er samlet i
`DS.Infrastructure/ConfigureInfrastructure.cs`. Website, Worker og testene
delte tidligere fire næsten identiske kopier af data- og Identity-opsætningen,
og de var begyndt at drive fra hinanden. Der tilføjes fire delte metoder:

- `AddDsDataLayer(connectionString)` registrerer `DataDbContext`
  (Npgsql, snake_case-navngivning, `UseOpenIddict`), `IDataDbContext` og
  OpenIddict Core mod samme DbContext.
- `ConfigureDsIdentityOptions()` samler `IdentityOptions`
  (adgangskodekrav, unik email, `AuthenticatorIssuer`, `SchemaVersion.Version3`).
- `AddDsIdentityCore()` kalder `ConfigureDsIdentityOptions` og registrerer
  `AddIdentityCore<User>().AddRoles<Role>().AddEntityFrameworkStores<DataDbContext>()`.
  Den returnerer `IdentityBuilder`, så en host selv kan kæde fx
  `.AddUserManager<NotificationUserManager>().AddDefaultTokenProviders()`.
- `AddDsHangfireStorage(connectionString)` registrerer Hangfire PostgreSql-lageret
  med schema `hangfire` og `PrepareSchemaIfNecessary = false`.

Website bruger `AddDsDataLayer` og `AddDsHangfireStorage`, beholder sin fulde
`AddIdentity<User, Role>` (cookies, `SignInManager`) og tilføjer
`ConfigureDsIdentityOptions()`. Worker bruger `AddDsDataLayer`,
`ConfigureDsIdentityOptions()` og `AddDsHangfireStorage`. Testene bruger
`AddDsDataLayer` og `AddDsIdentityCore`; `AccountClientChecks` kæder desuden
user manager og token providers på som før. `NotificationUserManager` blev ikke
lagt ind i den fælles `AddDsIdentityCore`, fordi `BoardAccessChecks` bevidst
kører uden den (den kræver `EmailService`, som den test ikke registrerer).

`AddDsIdentityCore` kaldes fortsat før en eventuel host-specifik Identity-kæde,
så `NotificationUserManager` og token providers bevares som UserManager.
Rækkefølgen omkring `AddIdentity`/`ConfigureDsIdentityOptions` er valgt, så de
delte options sættes sidst og ikke overskrives. Routes, skema, adgangsregler og
runtime-adfærd er uændrede.

**Beslutning 2026-10-19 (implementeret):** Profil- og boardarbejdsgange ligger i
Application som `MeService` under `Services/Profile` og `BoardService` under
`Services/Boards`. Website leverer bruger-id og de eksisterende effektive claims
som `MeActor`/`BoardActor`; Application afhænger ikke af HTTP-contexten.
Boardmedlemskab, administratoradgang, synlighed af kortplaceringer,
notifikationsrettigheder og "Vis som"-adfærd bevares. Passkey-mapping ligger ved
profilservicen, og `NotificationPermissions` under `Services/Notifications`.

Dataudtræk bruger `ExportService` og `IDataExport` under `Services/Exports` i
Application. ClosedXML, konkrete udtræk og regnearksformatering ligger under
`Infrastructure/Exports`. Eksportrettigheder kontrolleres fortsat både ved
listevisning og download; API-routes, filnavne og arkdata bevares. Fælles
visningsnavne (`Labels`) ligger i Domain, så Infrastructure kan bruge dem.

Resterende Website-referencer til den tidligere `DS.Domain.Data`-namespace er
opdateret til Infrastructure. SQL-migrationerne ligger fortsat i Domain og
indlæses fra Domain-assemblyet ved opstart og i board-adgangstesten, selvom
`DataDbContext` ligger i Infrastructure. Disse flytninger ændrer ikke skemaet.

**Beslutning 2026-10-19 (implementeret):** `GoogleAuthService` ligger i
Infrastructure under `Google`, med det konkrete Google SDK, Data Protection og
`DataDbContext`. `IGoogleAuthService` ligger i Application under `Services/Google`
og omfatter konfiguration, status og OAuth-forbindelse. SDK-klienten udleveres
gennem `IGoogleDriveClientFactory` i Infrastructure, så Application-interfacet
ikke eksponerer Google SDK-typer. GoogleDriveService bruger denne factory og
er efterfølgende flyttet til Infrastructure. Website registrerer begge auth-interfaces mod samme scoped
GoogleAuthService. OAuth-scope, tokenbeskyttelsens formål, konfiguration og
forbindelsesadfærd er uændrede; der ændres ikke databaseskema.

**Beslutning 2026-10-19 (implementeret):** `GoogleDriveService` ligger under
`Infrastructure/Google`. `IGoogleDriveService`, `DriveFileInfo` og den fælles
`GoogleFileTypes` ligger under `Application/Services/Google`, uden Google
SDK-typer i kontrakten. Website registrerer fortsat implementationen og bruger
Application-interfacet. Mappeafgrænsning, cacheduration, filsortering,
aktiviteternes mappekobling og håndtering af manglende mapper bevares.
Dette erstatter placeringen af GoogleDriveService i Website ovenfor.

**Beslutning 2026-10-19 (implementeret):** `UserApiController` bruger `UserService`
under `Services/Users` i Application til brugeradministration, profilbilleder,
roller, låsning, invitationer og generering af nulstillingslinks. Servicen bruger
`IDataDbContext`, Identity, `EmailService` og `IUserAccessCache`. Controlleren
beholder routes, autorisationsattributter, requestvalidering og HTTP-svar.
Rolletildeling kontrolleres fortsat mod brugerens direkte Identity-roller;
flytningen udvider ikke rettigheder gennem stillinger eller "Vis som".
Nulstillingslinks bruger fortsat requestens scheme og host, leveret af Website;
den tidligere dokumenterede proxybegrænsning er ikke ændret. Databaseskema og
øvrig eksisterende adfærd bevares.

**Beslutning 2026-10-19 (implementeret):** `RegistrationSettingsApiController`
bruger `RegistrationSettingsService` under `Services/Registration` i Application
til opslag og ændring af tilmeldingens åben/lukket-flag via `IDataDbContext`.
Controlleren beholder offentlig læseadgang, `PreSignupManage` ved ændringer,
inputvalidering og HTTP-svar. Delvise opdateringer bevarer det udeladte flag.
Der ændres ikke tilmeldingsregler, datoer eller databaseskema.

**Beslutning 2026-10-19 (implementeret):** `OrgApiController` bruger `OrgService`
under `Services/Organization` i Application. Stillinger, PA'er, indehavere,
invitationer og rettighedskontroller håndteres i servicen via `IDataDbContext`.
Website leverer bruger-id og aktuelle SysAdmin-/OrgView-claims som `OrgActor`,
og beholder requestvalidering og HTTP-svar. Den fælles opslagshjælper
`OrgPositionAccess` ligger også i Application og bruges fortsat af Websites
claims-opbygning. Application rydder rettighedscachen gennem `IUserAccessCache`,
som Website implementerer med den eksisterende `ClaimsTransformer.Evict`.
Eksisterende adgangsregler, PA-arv, cacheinvalidering og databaseskema bevares.

**Beslutning 2026-10-19 (implementeret):** `GroupPreSignupApiController` bruger
`GroupPreSignupService` i Application til gruppeopslag og offentlig
forhåndstilmelding. Transaktion, rækkelåse, kontrol af åben tilmelding,
gruppens første bruger, deltagerantal og håndtering af unikhedsbrud bevares.
Controlleren beholder inputvalidering og kontrollen af, at brugeren er logget
ud, samt HTTP-svar og login efter commit. Routes og databaseskema er uændrede.

**Beslutning 2026-10-19 (implementeret):** `GroupInvitationApiController` bruger
`GroupInvitationService` i Application til opslag og accept. Servicen bevarer
den transaktionelle claim af invitationen, kontrollen af den inviterede bruger,
den atomiske gruppetilknytning og oprettelsen af nye brugere gennem Identity.
Controlleren beholder HTTP-svar og logger først en ny bruger ind, når servicen
har committed transaktionen. Eksisterende routes, svar og regler bevares.
Der ændres ikke databaseskema.

**Beslutning 2026-10-19 (implementeret):** `FileApiController` bruger
`FileService` under `Services/Files` i Application til upload, læsning og sletning
via `IDataDbContext` og `IFileStorage`. Application bestiller billedbehandling
gennem `IFileProcessingQueue`; Infrastructure enqueuer det eksisterende
`FileProcessingJobs.ConvertToWebp` gennem `HangfireFileProcessingQueue`.
Controlleren beholder uploadvalidering, brugeridentifikation, routes,
autorisationsattributter, cache-headere og streaming med range-understøttelse.
Ejerskabskontrollen ved sletning udføres i servicen med bruger-id og den
serverbestemte SysAdmin-status. Eksisterende adfærd og databaseskema bevares.

**Beslutning 2026-10-19 (implementeret):** `InvitationApiController` bruger
`InvitationService` under `Services/Invitations` i Application til at hente og
acceptere invitationer uden gruppetilknytning. Servicen bruger `IDataDbContext`
og Identity til brugeroprettelse, roller, aktivitetsteam og stillinger.
Controlleren beholder HTTP-svar og rydning af Website-lagets rettighedscache
efter succes. Eksisterende validering, rækkefølge og invitationsadfærd bevares;
gruppeinvitationer er fortsat et separat flow. Der ændres ikke databaseskema.

**Rettet 2026-11-21 (DS-64, brugernavn ved invitation):** `InvitationService`
lavede brugernavnet af fornavn og efternavn. Identity tillader ikke æ, ø og å i
brugernavne, så inviterede med danske bogstaver i navnet kunne ikke oprette en
konto, og to personer med samme navn ville støde sammen. Brugernavnet er nu
invitationens email, som i de øvrige oprettelsesflows. Login slår op på email,
så eksisterende brugere med navnebaserede brugernavne påvirkes ikke.

**Beslutning 2026-10-19 (implementeret):** Gruppeadministration, gruppens egen
side med forhåndstilmelding, spejdere og patruljer samt medlemsadministration
ligger i Application som `GroupAdministrationService`, `GroupService`
og `GroupMembersService`. De bruger `IDataDbContext`; interfacet eksponerer
også EF's transaktioner og entity entries, så eksisterende rækkelåse og fjernelse
af gruppetilknytning bevares. Controllerne beholder routes, autorisationskrav,
requestvalidering og oversættelse af serviceresultater til HTTP-svar. Dette er en
flytning af eksisterende adfærd uden nye rettigheder eller skemaændringer.
Det offentlige forhåndstilmeldingsflow ligger efter den efterfølgende flytning
i `GroupPreSignupService` under `Services/Groups`.
Accept af gruppeinvitationer ligger i `GroupInvitationService` under
`Services/Invitations` efter den efterfølgende flytning nedenfor.

Application-services grupperes efter funktion under `Services/Activities`,
`Services/Email` og `Services/Groups`, med tilsvarende namespaces.
`IMailQueue` ligger sammen med mailservicen, og `GroupResult` sammen med
gruppeservices. Det fælles `IDataDbContext` bliver under `Data`.

**Beslutning 2026-10-19 (implementeret):** Service-registrering og tilhørende
konfiguration samles i `DS.Backend/Extensions/ServiceExtensions.cs` og kaldes
via `builder.ConfigureDS()`. `Program.cs` beholder HTTP-pipeline, routing,
DbUp-migrering og rolleseeding. Flytningen ændrer ikke autentifikationsregler
eller databaseskema.

## Roller i den oprindelige plan

| Rolle | Ønsket adgang og ansvar |
| --- | --- |
| Systemadministrator | Adgang til hele systemet og ekstra rettigheder til at ændre kritiske data. |
| Lejrchef | Adgang til hele systemet, men ændringer i kritiske data kræver en systemadministrator. Skal kunne eksportere en liste over registrerede emailadresser. |
| Økonomi | Eksport af forbrug og forventede deltagerantal. Læseadgang til aktiviteter. |
| Materialeansvarlig | Læseadgang til aktiviteter, redigering af materialelister og eksport både pr. aktivitet og som samlet masterark. |
| Aktivitetsansvarlig | Adgang til og mulighed for at ændre alle aktiviteter. |
| Aktivitets-teamleder | Opretter aktiviteter, redigerer næsten alle felter og giver medlemmer adgang. Står som ejer af en aktivitet og tilføjer medlemmer til systemet. Om teamlederen kun må se egne aktiviteter, er uafklaret. |
| Aktivitets-teammedlem | Får adgang af teamlederen til at ændre tildelte aktiviteter. |
| Gruppe | Tilmelder gruppen til lejren, opretter patruljer og tilmelder dem aktiviteter. Brugeren er tilknyttet et gruppenummer. |

Definitionen af "kritiske data" og de præcise felter, en teamleder må ændre,
er ikke fastlagt i planen.

## Funktioner

### CMS og offentlig hjemmeside

Hjemmesiden skal bygges i et CMS, eksempelvis WordPress. Rollebaseret adgang
skal gøre det muligt for PR, IT og eventuelt andre at redigere indhold uden
hjælp fra IT. Formuleringen om, hvem der må "tilgå siden", skal afklares ved
arbejde med offentlig visning kontra adgang til CMS-administrationen.

### Gruppelogin og brugere

I den oprindelige plan modtager grupper en email med et link til
brugeroprettelse. Brugeren kobles automatisk til gruppenummeret. Flere personer
fra samme gruppe skal kunne have hver sin konto i DS_OS.

### Notifikationer – nice to have

**Beslutning 2026-10-19 (implementeret):** `EmailService` i Application bestiller
mailafsendelse gennem `IMailQueue`. Infrastructure implementerer interfacet med
`HangfireMailQueue`, som enqueuer det eksisterende `MailJobs.Send` med `MailData`.
EmailService kender dermed ikke den konkrete jobklasse eller Hangfire-klienten.
Mailindhold, adressevalidering, `NotificationsEnabled` og jobbets retry-regler
er uændrede. Dette præciserer ansvarsfordelingen i den eksisterende Hangfire-løsning.

Brugere skal eventuelt kunne få besked om hændelser, eksempelvis når nogen
opretter en patrulje eller ændrer deltagerantal. Dette blev primært tænkt som
en "Ole-feature"; målgruppe, kanaler og regler er ikke nærmere fastlagt.

**Beslutning 2026-09-29:** Notifikationer starter som **mail**. En `EmailOutbox`
fungerer som transaktionel outbox og en `EmailOutboxWorker` sender fra køen i
baggrunden. Adgang til at se køen (`EmailOutboxView`) er givet til
systemadministrator og lejrchef, via `/admin`.

**Beslutning 2026-10-06:** Køen køres på **Hangfire** (`Hangfire.PostgreSql`,
schema `hangfire`). `EmailOutboxWorker` er erstattet af `EmailOutboxJobs`: et
recurring dispatcher-job der kører hvert 15. sekund og enqueuer ét
`Send`-job pr. due outbox-række, og et send-job der atomisk claim'er rækken,
sender og opdaterer `email_outbox` med samme backoff-, forsøgs- og
dead-letter-semantik som før. `email_outbox` forbliver den transaktionelle
outbox, og transaktionelle mails sendes fortsat synkront. Hangfire-dashboardet
ligger på `/hangfire` og kræver `AdminAccess` (senere `HangfireView`, se DS-83 under Adminområde). Hangfire-schemaet ejes udelukkende
af DbUp: `007_hangfire.sql` installerer Hangfires egne install-scripts med
`PrepareSchemaIfNecessary = false`, så senere Hangfire-opgraderinger tilføjes
som almindelige migrationer. Dette overskriver implementeringsdelen af
beslutningen 2026-09-29; outbox-beslutningen står uændret.

**Beslutning 2026-10-06 (senere samme dag):** Mails enqueueres **direkte**
som Hangfire-jobs. `EmailService.QueueInvitationMail` og
`QueueResetPasswordMail` bygger indholdet, validerer modtageradressen og
kalder `BackgroundJob.Enqueue` med en `MailData`-payload (modtager, emne,
tekst); `MailJobs.Send` sender via SMTP. Invitationer og nulstilling af
adgangskode sendes dermed asynkron: API'et svarer med det samme, og
SMTP-fejl bliver failed Hangfire-jobs med retry (30/60/120/240 s,
5 forsøg) der kan genkøes manuelt i dashboardet. `email_outbox`-tabellen,
dispatcheren, outbox-APIet, admin-siden "Mailkø" og `EmailOutboxView`-
rollen fjernes (`008_drop_email_outbox.sql`); Hangfire-flisen i Admin
erstatter mailkøen, og Vite-dev-proxyen sender `/hangfire` videre til
backenden. `NotificationsEnabled` flyttes til enqueue-trinnet og gate'er
nu også invitationer og nulstilling. Beslutningen overskriver outbox-,
synkron-send- og kø-delen af beslutningerne 2026-09-29 og 2026-10-06
(samme dag); Hangfire som kø-teknologi og dashboardet består.

**Beslutning 2026-10-07:** Hangfire bruges også til planlagt drift: et
daglig recurring-job `prune-openiddict` (`OpenIddictJobs.Prune`, 03:00 UTC)
kalder OpenIddicts `PruneAsync` og fjerner ugyldige eller udløbne rækker fra
`open_iddict_tokens` og `open_iddict_authorizations`. Threshold er syv dage:
rækker der er mindst syv dage gamle slettes først, hvilket både holder
tabellerne små og bevarer et vindue hvor genbrug af et kasseret refresh-token
kan opdages. Uden dette vokser `open_iddict_*`-tabellerne uendeligt, da
applikationen ingen steder sletter udløbne tokens i dag. Jobbet er en udvidelse
af Hangfire-beslutningen 2026-10-06, ikke en overskrivelse, og det kræver ingen
skemaændring.

**Beslutning 2026-10-09:** Brugere kan selv tilvælge notifikationer i
`Profile.vue`, og valgene gemmes pr. bruger i `user_notification_preference`
(`user_id` + `notification_type`). Hvilke typer en bruger må tilvælge er
rollegated: mappingen ligger i `NotificationPermissions`, og `GET`/`PUT`
`/api/v1/me/notifications` markerer kun tilladte typer med
`canSubscribe = true`. Den første type er `NewUser`, som kræver
app-rollen `UsersView` (SysAdmin og CampAdmin). Afsendelsen sker centralt i
`NotificationUserManager`, en `UserManager<User>`-subklasse der overskriver
`CreateAsync(User)`, så alle oprettelsesveje fanges — selvbetjening,
admin-oprettelse, invitation, gruppe-invitation og gruppe-forhåndstilmelding.
Efter et vellykket kald enqueuer `EmailService.QueueNewUserNotificationMail`
en mail til hver bruger med præferencen slået til. Den tidligere faste
modtager `DS__NewUserNotificationEmail` er dermed fjernet. Som de øvrige mails
gate'er `NotificationsEnabled` på enqueue-trinnet.

**Beslutning 2026-11-21 (implementeret):** Brugerstyringen kan også se og
ændre en anden brugers notifikationer, på samme måde som brugeren selv gør det
på profilen. `GET`/`PUT` `/api/v1/user/{id}/notifications` ligger i
`UserApiController` og kræver `UsersView` som de øvrige ændringer af stamdata.
Hvilke typer der kan vælges, afgøres af målbrugerens egne roller, ikke af
administratorens. Logikken deles med `/api/v1/me/notifications` gennem
`NotificationPreferenceService`. Panelet ligger i `UserDetail.vue`.

**Beslutning 2026-11-22 (implementeret, DS-65):** Lejrchefer kan tilvælge en
mail, når der oprettes en ny tavle. Typen `BoardCreated` følger `NewUser`: den
gemmes i `user_notification_preference`, er ikke slået til fra starten,
vælges i profilen eller brugerstyringen ("Ny tavle oprettet") og sendes
gennem `EmailService` og Hangfire-køen, gated af `NotificationsEnabled`.
Afsendelsen sker centralt i `BoardService.CreateBoard`, så både
hjemmesiden, MCP-værktøjerne og andre veje gennem servicen fanges. Mailen
indeholder tavlens navn, opretterens navn og et link til `/board/{id}`.
Rollegatingen i `NotificationPermissions` bruger den nye app-rolle
`BoardViewAll`, som matricen giver SysAdmin og CampAdmin. Rollen beskriver
den eksisterende globale tavleadgang; selve tavleadgangen afgøres fortsat af
gruppetjekket i `BoardActor`, ikke af rollen.

Implementeret standardvalg, der afventer bekræftelse:
- Modtagere er abonnenter, som har adgang til den nye tavle. Da SysAdmin og
  CampAdmin altid ser alle tavler, er det abonnenter med en af de grupper.
  Gruppen tjekkes igen ved afsendelse, så en bruger, der har mistet rollen,
  men stadig har præferencen gemt, ikke får tavlens navn. Andre tavlemedlemmer
  kan ikke tilvælge notifikationen.
- SysAdmin kan også tilvælge den, ligesom ved `NewUser`.
- Opretteren får ikke mail om sin egen tavle.

Ubesluttede punkter:
- Præferencevalget er implementeret for `NewUser` og `BoardCreated`. Flere
  hændelsestyper og tilhørende rollegating er ikke fastlagt. Afmelding sker ved
  at fjerne krydset i profilen eller i brugerstyringen; der findes ingen
  separat afmeldingsmulighed.

### Seneste login

**Beslutning 2026-11-22 (implementeret, DS-84):** Brugerstyringen viser,
hvornår hver bruger sidst loggede ind, så det kan ses, om folk bruger
platformen. Kun ét tidsstempel pr. bruger gemmes (`User.LastLoginAt`), ingen
loginhistorik, af hensyn til dataminimering. `LoginTrackingSignInManager`
opdaterer det ved hvert vellykket login gennem `SignInManager`: adgangskode,
2FA, recovery code, passkey og automatisk login efter invitation eller
forhåndstilmelding. `RefreshSignInAsync` efter profilændringer tæller ikke.
Login-cookien er persistent med sliding expiration, så en aktiv bruger kan
blive logget ind længe uden et nyt login; tidsstemplet viser seneste login,
ikke seneste aktivitet. MCP-adgang med OAuth-refresh-tokens eller personlige
adgangstokens opdaterer det heller ikke. Brugere, der ikke har logget ind
siden ændringen, vises som "Intet login registreret".

**Beslutning 2026-11-22 (implementeret, DS-85):** Fordi seneste login ikke
viser, om en bruger med en levende cookie bruger platformen, gemmes også
`User.LastActiveAt`. `UserActivityMiddleware` kører efter `UseAuthorization`
og opdaterer det ved godkendte requests, uanset om brugeren er logget ind med
cookie, personligt adgangstoken eller MCP-token. Det skrives højst hvert femte
minut pr. bruger (hukommelsescache plus en betingelse i `UPDATE`), så
præcisionen er fem minutter. Et login sætter både `LastLoginAt` og
`LastActiveAt`. Brugerlisten viser "Sidst aktiv"; detaljevisningen viser både
sidst aktiv og seneste login. Kun ét tidsstempel gemmes, ingen aktivitetslog.

### Min profil

**Sikkerhedsrettelse 2026-10-20 (implementeret):** Login- og antiforgery-cookies
bruger altid `Secure`, også i Development; lokal browserudvikling kræver HTTPS
(Vite bruger allerede basic-ssl). Google-forbindelsesloggen indeholder ikke
længere kontoens email. Mailjobbets applikationslogs indeholder ikke modtager,
emne eller exceptiontekst; fejltypen bevares, og fejlen genkastes fortsat til
Hangfire med uændrede retries. Hangfire gemmer fortsat jobpayload og fejldetaljer
og kræver derfor fortsat beskyttet dashboard- og databaseadgang. Frontendens
invitationslog bruger en fast tekst uden invitations-id eller fejlobjekt.

CodeQL-alerts #86, #87 og #88 er vurderet som falske positiver: de markerede
inputguards afviser manglende input med HTTP 400, ikke et succesfuldt login eller
en sikkerhedsændring. Token-/adgangskodekontrol kan ikke omgås gennem disse
guards. Valideringen bevares; alerts er ikke automatisk afvist på GitHub.

**Opfølgning 2026-10-20 (implementeret):** Website refererer eksplicit til
`System.Security.Cryptography.Xml` 10.0.13 frem for OpenIddicts transitive version
10.0.10. Kun pakkens pruning-advarsel `NU1510` undertrykkes: referencen er
tilsigtet, selvom Website også har ASP.NET Core-frameworkreferencen.
Sårbarhedsadvarsler undertrykkes ikke. SDK'et kan prune pakken i forbrugende
testprojekter, som da bruger den delte ASP.NET Core-runtime. Version 10.0.13
retter CVE-2026-96958, CVE-2026-96959 og CVE-2026-96960 (XML denial of service).
Den installerede .NET-runtime skal desuden opdateres til mindst 10.0.13;
pakkeopdateringen erstatter ikke en runtimeopdatering. Containerne bruger
flydende `aspnet:10.0`-tags og skal genbygges med opdaterede base-images og
genudrulles. Der ændres ikke databaseskema eller autentifikationsflow.

**Beslutning 2026-10-19 (implementeret):** Min profil viser brugerens email
som skrivebeskyttet tekst under Profil og sikkerhed. Email medsendes i
`GET /api/v1/me`; der tilføjes ingen mulighed for at ændre den i profilen.

### Adminområde

**Beslutning 2026-10-19 (implementeret):** OpenIddict-klienter administreres på
`/admin/openiddict`, med en flise under Admin → System. Den nye approlle
`OpenIddictManage` gives kun til SysAdmin, ikke lejrchef. API og frontend bruger
samme adgangskrav, også under "Vis som".

- Første version omfatter liste, oprettelse, redigering og sletning samt
  rotation af client secret. Felterne er Client ID, navn, public/confidential,
  redirect-URL'er, scopes (`openid`, `profile`, `email`, `roles`), PKCE og
  loginrettigheder (`AppRoles`, se den efterfølgende beslutning nedenfor).
  Public-klienter kræver PKCE; nye confidential-klienter foreslår også PKCE.
- Nye klienter bruger serverens eksisterende authorization-code-flow og implicit
  consent, da der ikke findes et samtykketrin. Andre grants og nye scopes
  aktiveres ikke i denne første version; DS-36 nedenfor udvider senere med
  refresh-grant og `offline_access`. Redirect-URL'er kræver HTTPS, undtagen HTTP på loopback, og
  tillader ikke wildcard, fragment eller brugernavn/adgangskode.
- Secrets genereres kryptografisk på serveren, hashes af OpenIddict og vises
  kun i svaret ved oprettelse, rotation eller skift fra public til confidential.
  Listen indeholder hverken secret eller hash. Almindelig redigering bevarer
  eksisterende secret; skift til public fjerner det. Rotation erstatter straks
  det gamle secret og kræver opdatering af klientapplikationen.
- Redigering indlæser den eksisterende OpenIddict-descriptor og bevarer øvrige
  indstillinger, herunder consent, avancerede permissions/scopes, requirements,
  logout-URL'er, nøgler og øvrige properties. Kun loginrettighederne har en
  redigerbar property i denne version.
- Sletning og rotation kræver bekræftelse i brugerfladen. Sletning sker gennem
  OpenIddict og fjerner også klientens gemte tokens og autorisationer.
  Ændringsendpoints har antiforgery-validering; svar fra klient-API'et må ikke
  caches. Der tilføjes ingen tabeller, kolonner eller migrationer.

**Beslutning 2026-10-19 (efterfølgende, implementeret):** Klienters loginadgang
styres af valgte **`AppRoles`-rettigheder**, ikke `AppGroups`-brugerroller.
Brugerroller er bundter af rettigheder. Dette erstatter den fælles,
hardkodede WordPress-begrænsning fra klientadministrationens første version.

- Hver klient vælger én eller flere rettigheder fra backendens aktuelle
  `AppRoles`. Brugeren skal have **mindst én** af dem (OR). Valget giver ikke
  brugeren nye rettigheder. SysAdmin har ingen særskilt bypass af klientkravet.
- Oprettelse og redigering afviser tomme valg, ukendte rettigheder og
  `AppGroups`-navne. Nye klienter har intet forhåndsvalgt adgangskrav og kan
  ikke gemmes, før administratoren har valgt mindst én rettighed.
- Kravet gemmes som et JSON-array i klientens eksisterende `properties` under
  `ds28:required_app_roles`. Der kræves ingen skemaændring eller migration.
  Klienter uden nøglen beholder `WordPressEditor` ELLER `WordPressAdmin`;
  de vises med dette valg i brugerfladen. Ved næste gemning bliver valget
  eksplicit. En eksisterende, men tom eller ugyldig property afviser login
  frem for at åbne adgang eller falde tilbage til WordPress.
- `/connect/authorize` kontrollerer kravet mod brugerens effektive rettigheder,
  inklusive stillinger, PA-arv og "Vis som". Ved indløsning af authorization
  code genkontrolleres klientens aktuelle krav mod brugerens aktuelle
  Identity-principal (reelle roller og stillinger; server-til-server-kaldet
  er ikke afhængigt af browserens "Vis som"-cookie). Fjernede rettigheder eller
  ændrede klientkrav kan dermed afvise en allerede udstedt code.
- Der ændres ikke på allerede udstedte tokens, klientapplikationernes egne
  sessioner eller indholdet i eksisterende claims/userinfo. `userinfo`-svarets
  `roles`-felt er fortsat WordPress-specifikt; generel udlevering af flere
  rettigheder er ikke del af denne beslutning.

**Beslutning 2026-09-29:** Der oprettes et samlet adminområde på `/admin` med
sektioner, som kræver `AppRoles.AdminAccess`. Rollen er tildelt
systemadministrator og lejrchef. Hvert element i adminområdet gater fortsat på
sin egen rolle; adgang til siden og adgang til den enkelte funktion er to
separate ting.

HQ viser kun én "Admin"-flis i stedet for de enkelte administrationsfliser.
Aktivitetsmodulet og Wordpress-login bliver stående på HQ, fordi de også er
tilgængelige for roller uden for systemadministrator og lejrchef.

**Beslutning 2026-09-29:** Adminområdet har en sidebar med tilmeldingsstatistik,
der sammenligner forhåndstilmelding og endelig tilmelding mod et måltal.
Måltallet kom oprindeligt fra det hardkodede `ParticipantData`
(`TotalUniqueParticipants`, 285); det er erstattet af DS-71 nedenfor.

**Beslutning 2026-11-21 (DS-71, implementeret):** Måltallene hentes fra de
importerede historiske tilmeldinger i stedet for et hardkodet tal. Det faste
tal brugte samme mål for begge tilmeldinger og var et tal uden ledere, mens de
nuværende tal tæller ledere med. `ParticipantData` og `ParticipantStatsDto`
er fjernet, da de ikke længere bruges.

- Lejråret er det seneste `camp_year` i `historic_presignup` eller
  `historic_signup`. Det bestemmes af dataene, ikke af
  `DS__HistoricRegistration__Year`.
- Målet for forhåndstilmelding er summen af `participant_count` i
  `historic_presignup` for lejråret, inklusive ledere og uden venskabsgruppens
  `guest_count`, da nuværende forhåndstilmelding ikke har gæster.
- Målet for endelig tilmelding er summen af `participant_count` i
  `historic_signup` for dag 0 (patruljesummer, inklusive ledere). Dag 1–8
  summeres ikke, fordi det ville tælle samme deltager flere gange.
- Uden historik for lejråret har tilmeldingen intet mål: API'et returnerer
  `target` og `targetYear` som `null` og status `no-target`, og siden viser
  tallet uden bjælke frem for et opdigtet mål.
- Ved en lokal import af 2024 med produktionsopsætningen giver det 427 for
  forhåndstilmelding (15 grupper) og 385 for endelig tilmelding (19 grupper).
- Uafklaret: om nuværende endelig tilmelding (rækker i `Scout`) omfatter
  ledere på samme måde som dag 0 i historikken. Målet er aggregerede
  tilmeldinger, ikke et verificeret antal unikke personer.

Tærsklerne er: grøn når målet er nået, gul når mindst halvdelen er nået, rød
under halvdelen. Tallet er hærdet som halvdelen af målet, så en tærskel ikke
skal vedligeholdes separat.

De to tal har forskellige kilder og er ikke koblet til hinanden:
- Forhåndstilmelding summerer `GroupPreSignup` for alle grupper og aldersgrupper,
  altså gruppernes erklærede antal.
- Endelig tilmelding tæller rækker i `Scout`, altså navngivne deltagere.

Uafklaret: sidebaren skal på sigende og senere rumme de åb/luk-funktioner, der
hidtil ligger på `/camp-settings`.

**Beslutning 2026-11-21 (DS-70, implementeret):** Sidebaren viser under
tilmeldingsstatistikken også antal brugere og antal grupper i systemet, hentet
fra `GET /api/v1/admin/system-counts` (kræver `AdminAccess`). Det er rene
optællinger uden persondata. Slettede grupper tælles ikke med, fordi grupper
er soft-deleted og filtreres globalt.

**Beslutning 2026-11-22 (DS-83, implementeret):** Baggrundsjob vises på en
indbygget side i HQ på `/admin/jobs`, med en flise "Baggrundsjob" under
Admin → System, der erstatter Hangfire-flisen. Siden bygger på Hangfires
monitoring-API gennem `BackgroundJobService` og
`GET /api/v1/background-jobs`, uden nye afhængigheder eller skemaændringer.
Den viser nøgletal, fejlede jobs med detaljer, forsøg og stacktrace,
tilbagevendende jobs, seneste aktivitet (polling hvert 5. sekund, på pause
når fanen er skjult) og en jobhistorik over afsluttede jobs med filter på
status og jobtype, søgning på job-id og sideinddeling.

- Jobargumenter og fejltekster maskeres på serveren, før de sendes til
  browseren: mailadresser og navne vises maskeret, og mailens indhold,
  adgangskoder, tokens og hemmeligheder vises kun som skjulte med størrelse.
  Hangfire gemmer fortsat payload og fejldetaljer i klartekst i
  `hangfire`-schemaet.
- Jobhistorikken går 24 timer tilbage, som er Hangfires standard for udløb af
  afsluttede jobs. Udløbstiden hæves ikke, af hensyn til dataminimering.
  Fejlede jobs udløber ikke og bliver liggende, til de genkøres eller slettes.
- `HangfireView` giver læseadgang til siden og dashboardet. Handlinger
  ("Prøv igen", "Prøv alle igen", "Slet" og "Kør nu") kræver den nye approlle
  `HangfireManage`, som kun SysAdmin har. API'et håndhæver rollen, og
  knapperne vises ikke uden den.
- `/hangfire` bevares som nødudgang, men er read-only for brugere uden
  `HangfireManage` (`IsReadOnlyFunc`). Dashboardet viser fortsat argumenter i
  klartekst. Det overskriver den tidligere praksis, hvor `HangfireView` også
  kunne slette, genkøre og trigge jobs i dashboardet.

### Rollevisning (SysAdmin-impersonation)

**Beslutning 2026-10-12:** En systemadministrator kan skifte til en anden
`AppGroups`-gruppe ("Vis som …") for at se og fornemme, hvad den pågældende rolle
har adgang til – både til egen kontrol og som demo over for udvalgene. Det er
ægte impersonation, ikke kun en UI-preview: den valgte gruppe anvendes server-side
i hele appen, så `[Authorize(Roles=…)]`, `IsInRole` (herunder HQ-fliser,
Hangfire og eksport) og `/api/v1/me` alle opfører sig som den valgte gruppe.

- Skifteren ligger i brugermenuen i `Back.vue` ("Vis som …") og vises kun for
  SysAdmin. Alle `AppGroups` er valgbare.
- Mekanismen: `ClaimsTransformer` erstatter på hvert request gruppe-/approlclaims
  med en "act-as"-gruppe fra en HttpOnly-, SameSite=Strict- og
  DataProtection-krypteret cookie (`DS.Impersonation`), som sættes af
  `POST /api/v1/impersonation/start` og fjernes af `stop`. De reelle grupper
  lagres som `ds28:imp:origin`-claims, så start/stop altid kan verificeres mod
  den ægte SysAdmin-identitet, uanset hvad der impersoneres.
- Fra 2026-10-14 anvendes cookien kun, hvis brugerens reelle grupper fortsat
  indeholder `SysAdmin`; mister man rollen, holder en allerede udstedt cookie op
  med at virke, i stedet for at virke til den udløber.
- En aktiv visning vises i selve brugermenuen i `Back.vue`: mens man viser
  som en rolle, står der "Viser som …" med rollens titel plus et "Stop visning"-
  punkt. Der er bevidst ikke et globalt topbanner.
- Cookies er kun til "rolleoverlay" – identiteten (user-id, navn, billede, 2FA-krav)
  ændres ikke. Fra 2026-10-18 scopes tavler efter medlemskab og de effektive
  SysAdmin/CampAdmin-claims: "Vis som" en anden rolle fjerner derfor den globale
  tavleadgang, men bevarer den virkelige brugers personlige medlemskaber.
  En Lejrchef-impersonation har fortsat adgang til alle tavler.

Afsluttet beslutning: brugeren ønskede eksplicit **ægte impersonation** frem for en
frontend-only preview; valgte **alle grupper** som target og **brugermenuen** som
placering.

### Stillinger og organisationsdiagram

**Beslutning 2026-10-12 (implementeret).** Benjamin ønsker et
organisationsdiagram bygget ud fra brugerne. I stedet for at koble personer
direkte sammen er noderne **stillinger**: en stilling har en titel, ligger under
en anden stilling og kan beklædes af flere brugere, ligesom en bruger kan
beklæde flere stillinger.

Diagrammet omfatter kun brugere, der beklæder en stilling. Brugere der
udelukkende er knyttet til en spejdergruppe (`User.Group`, `AppGroups.User`)
indgår ikke i hierarkiet, og spejdergrupper er ikke noder i træet. En bruger med
både gruppetilknytning og en stilling optræder alene på grund af stillingen.

Begreberne holdes adskilt:

- `AppRoles` er fortsat de fine-grained rettigheder.
- `AppGroups` er fortsat kode-ejede adgangsbundter og Identity-rolle-enheden.
  Gruppernes danske visningstitler (`GetGroupTitle`) flyttes i stedet til
  stillingen, så `AppGroups` kun betyder adgang.
- En stilling er data-ejet: titel, placering i træet og en liste af `AppGroups`
  (0..N). To stillinger kan dele samme gruppe, og stillingens titel behøver ikke
  matche gruppens.

Model (implementeret):

| Tabel | Indhold |
| --- | --- |
| `org_position` | `id`, `title`, `parent_position_id` (nullable), `pa_for_position_id` (nullable, unik), `is_system`, `color` (nullable) |
| `org_position_group` | `position_id`, `group_name` (et `AppGroups`-navn) |
| `org_position_holder` | `position_id`, `user_id` |
| `user_invitation` | nyt `org_position_id` (nullable) |

Adgang opløses **dynamisk**: effektive grupper = direkte tildelte Identity-roller
∪ `AppGroups` fra alle stillinger brugeren beklæder. Beklæder brugeren en
personlig assistent-stilling, arves desuden `AppGroups` fra den stilling,
assistenten er knyttet til (`pa_for_position_id`). Opløsningen sker i
`ClaimsTransformer` og `AppClaimsPrincipalFactory` via `OrgPositionAccess`; en
ændring af en stillings grupper slår dermed straks igennem for alle indehavere,
inkl. dens personlige assistent, og `user-roles:{id}`-cachen skal ryddes for de
berørte brugere. Direkte rolletildeling via `UserApiController` består uændret.

Autoritet følger træet: at beklæde en stilling giver ret til at oprette
børnestillinger under den og til at invitere eller tildele i det undertræ.
Rodstillinger (`parent_position_id` er null) oprettes kun af SysAdmin.

**Invariant:** ingen må tildele flere rettigheder, end man selv besidder — med
SysAdmin som undtagelse. Når en ikke-SysAdmin sætter en stillings `AppGroups`
eller inviterer til en stilling, skal de tildelte grupper være en delmængde af
ens egne effektive grupper (direkte roller ∪ egne stillinger, inkl. arvede
grupper via en personlig assistent). Det forhindrer, at
en indehaver eskalerer ved at oprette et barn med flere rettigheder og invitere
sig selv, og spejler `CanAssignRole`. En SysAdmin kan derimod give en stilling
vilkårlige `AppGroups`, også flere end forældrestillingen har; stillinger er
derfor ikke strukturelt begrænset til deres forælder.

SysAdmin er en reserveret systemstilling (`is_system`): den kan ikke slettes,
omdøbes eller tømmes for sin sidste indehaver, og dens grupper kan ikke
redigeres. Kun en nuværende SysAdmin må tildele eller fjerne den. Den kan
derimod flyttes i træet af en SysAdmin, fx så den organisatorisk placeres under
lejrchefen; men uanset hvor den ligger, forbliver det kun en nuværende SysAdmin,
der må tildele eller fjerne dens indehavere. Direkte
`AppGroups.SysAdmin`-tildeling ad den eksisterende beskyttede vej forbliver
roden af tillid, så en fejlredigeret stilling ikke kan låse systemet ude.

Implementeringsnoter (2026-10-12):
- Nye `AppRoles`: `OrgView` og `OrgManage`. De tildeles via matricen til alle
  grupper undtagen `FoodAdmin` og `User`.
  Visning af `/api/v1/org` kræver `OrgView`; mutationer afgøres af
  stillingsautoriteten.
- Ny `AppGroups.PersonalAssistant` ("Personlig assistent") til personlige
  assistenter i organisationen. Den kan gives til brugere og stillinger og
  giver `OrgView` (ikke `OrgManage`).
- En personlig assistent er en fast stabfunktion knyttet til én stilling: den
  oprettes som en stilling med titel "Personlig assistent", gruppen
  `PersonalAssistant` og `pa_for_position_id` sat til ejerstillingen. Der kan
  være maksimalt én assistent pr. stilling; assistenten vises som en egen kasse
  med stiplet kant ved siden af ejerstillingen. Assistenten kan ikke have
  understillinger, kan ikke ændres eller flyttes, og slettes sammen med
  ejerstillingen (`ON DELETE CASCADE`). Den kan dog give invitationer og
  beklædes af brugere.
- Assistenten arver ejerstillingens `AppGroups` i sin effektive adgang. Når en
  stilling får ændret sine grupper, ryddes derfor også rolle-cachen for dens
  personlige assistents indehavere.
- Stillinger kan farves med en fast palette (hex-værdi på `org_position.color`).
  Farven vises som en lys baggrundstone på kortet og vælges i draweren eller
  ved oprettelse. Personlige assistenter har ingen egen farve, men arver
  ejerstillingens tone.
- `AppGroups.SysAdmin` må ikke indgå i en almindelig stillings grupper; den
  findes kun på systemstillingen.
- En stilling med understillinger eller indehavere kan ikke slettes, før den er
  tømt.
- En stillingsinvitation gælder kun nye brugere; eksisterende brugere tilknyttes
  stillingen direkte. Ved accept oprettes `org_position_holder`, og rollernes
  cache ryddes.
- `MeDto.Title` viser stillingens titel, hvis brugeren beklæder en stilling, og
  falder ellers tilbage til `ResolveGroupTitle`.
- Brugeradministrationen viser en brugers stillinger via `UserDto.Positions`
  (stillings-ID og titel).
- En stilling kan flyttes til en anden overstilling via
  `PUT /api/v1/org/{id}/parent`. Flytning kræver autoritet over både den
  nuværende og den nye overstilling; en rodstilling (ny overstilling = null)
  kræver SysAdmin. Også systemstillingen kan flyttes, men kun af en SysAdmin.
  Der er cirkelkontrol, så en stilling ikke kan sættes under sig selv eller
  under en af sine egne understillinger. Grupperne ændres ikke ved flytning, så
  adgangen påvirkes ikke. Overstillingen vælges med træk og slip eller i
  skuffens fane Struktur (se DS-89 nedenfor).
- Autoritet følger hele undertræet rekursivt: at beklæde en overstilling giver
  håndteringsret til stillinger på alle niveauer under den. `CanManageParentAsync`
  og frontend-guardet i `orgAccess.ts` accepterer en indehaver af en hvilken som
  helst stilling i forældrekæden, ikke kun af den umiddelbare overstilling.
  SysAdmin kan håndtere hele træet; rodstillinger håndteres kun af SysAdmin.
- Diagrammet tegnes af en rekursiv `OrgNode.vue`; der bruges ingen ekstern
  org-chart-komponent. Siden ligger på `/org` med en "Organisation"-flis på
  hjemmesiden for brugere med `OrgView` og er ikke længere en del af
  adminområdet.

**Beslutning 2026-11-22 (DS-89, implementeret):** Organisationssiden er lavet
om efter en mockup, som Benjamin har godkendt. Ændringen er kun i frontend og
bruger de eksisterende API-kald. Rettighederne i `orgAccess.ts` er uændrede.
Dette erstatter den tidligere beskrivelse af én samlet skuffe og en flyt-modal.

- En understilling oprettes direkte fra noden: et plus under kortet åbner en
  lille formular med titel, farve og grupper. Plusset vises kun, hvor brugeren
  må oprette understillinger. Efter oprettelse markeres den nye stilling, og
  beskeden har en genvej til at tilføje personer.
- Skuffen (`OrgPositionDrawer.vue`) er delt i fanerne Stilling (titel, farve,
  grupper), Personer (indehavere, invitation, personlig assistent) og Struktur
  (overordnet stilling og understillinger). Sletning ligger i en menu i
  skuffens top og er slået fra med en forklaring, når stillingen har
  understillinger eller personer, så det matcher backendens regel. Toppen
  viser stillingens placering i træet som links.
- Flytning sker både med træk og slip i diagram og liste og med en søgbar
  trævælger under Struktur. Begge tilbyder kun mål, brugeren har autoritet
  over, og kan ikke flytte en stilling under sig selv eller sine
  understillinger. En flytning kan fortrydes fra beskeden.
- Til store diagrammer er der søgning på stilling eller person (inklusive
  personlige assistenter), zoom og "tilpas til vinduet", sammenfoldning af
  enkelte eller alle grene og en listevisning. Søgning folder automatisk de
  grene ud, der indeholder et resultat.
- På smalle skærme åbner siden i listevisning, skuffen glider op nedefra, og
  træk og slip er slået fra. Flytning sker der via vælgeren.
- Logikken er samlet i `lib/org/orgTree.ts` (træopbygning, forfædre,
  efterkommere, søgning) og `lib/org/orgChartContext.ts` (fælles tilstand for
  søgning, sammenfoldning, oprettelse og træk, delt via provide/inject).

Ubesluttet efter DS-89: om brugerne finder listevisningen tilstrækkelig på
mobil, og om valget mellem diagram og liste skal huskes.
- Migration `011_org_positions.sql` seeder systemstillingen
  "Systemadministrator" med gruppen `SysAdmin`.

Ubesluttede punkter:
- Skal tildeling af en stilling kunne give adgang til at redigere selve træet
  uden `OrgView`/`OrgManage`, altså udelukkende ud fra placeringen?
- Skal en stilling kunne beklædes af en bruger uden en tilknyttet konto?
- Er der behov for at vise stillinger uden `AppGroups` (rene
  organisationsnoder)?

### Aktiviteter

**Beslutning 2026-10-16 (implementeret):** Aktivitetsdetaljen starter med et
aktivitetsdashboard frem for en formular. Overblikket viser katalogtitel,
resumé og et uddrag af beskrivelsen, tildelt budget som dansk kronebeløb samt
op til seks direkte genveje til filer og mapper i aktivitetens hovedmappe.
Overblik, Katalog og Filer tilgås via kompakte vandrette faner. Navnet
redigeres direkte ved sidetitlen via blyanten; Anvend overfører navnet til
den lokale formular, og Annuller lukker navnefeltet uden at ændre navnet.
Der er ingen separat Aktivitet-fane. Budgettet er fortsat skrivebeskyttet
og vises kompakt ved sidetitlen. Katalogkortet viser, om resumé eller
beskrivelse mangler, med en genvej til redigering; dette ændrer ikke
reglerne for udfyldningsstatus. Filgenveje vises som en kompakt liste ved
siden af kataloget og under kataloget på smalle skærme. Den tidligere venstremenu og det deaktiverede
budgetinput er erstattet af dette layout.

Navn og katalogtekst redigeres lokalt, indtil brugeren vælger "Gem aktivitet".
Knappen og en besked om ikke-gemte ændringer vises kun, når disse felter er
ændret, også hvis brugeren skifter tilbage til overblikket. Filer og mapper
oprettes fortsat med øjeblikkelig lagring. Overblikkets filliste har særskilte
tilstande for indlæsning, fejl, manglende Google-forbindelse og ingen filer.
Der ændres ikke på rettigheder eller reglerne for udfyldningsstatus.
**Opgaver på aktivitetsdashboardet udskydes** efter brugerens ønske; der
implementeres ingen kobling til tavlekort i denne ændring.


- Registrerede aktiviteter kan udgives i et katalog.
- Grupper kan tilmelde sig, når aktivitetstilmeldingen åbner.
- En patrulje må ikke tilmeldes aktiviteter på dage, hvor den ikke er på lejren.
- Aktivitetens materialebehov og øvrige metadata registreres her og bruges af
  de andre dele af systemet.

**Beslutning 2026-10-16 (implementeret):** Aktivitetsoversigten udbygges til
arbejdsbord for aktivitetsadministratorer med indsendelse, godkendelse og
begrundet tilbagesendelse. Dette udvider aktivitetsdashboardet ovenfor.
Godkendelse og katalogudgivelse er adskilt: der implementeres ingen
udgivelseshandling eller automatisk offentliggørelse i denne version.

- Statusforløbet er `Draft` (Kladde) → `Submitted` (Afventer godkendelse) →
  `Approved` (Godkendt). En indsendt aktivitet kan i stedet sendes retur som
  `ChangesRequested` (Ændringer ønskes) og senere genindsendes.
- Indsendelse kræver navn, katalognavn, resumé og beskrivelse. Serveren
  validerer kravene. Filer er valgfrie. Den tidligere enkle kontrol af, om
  noget katalogindhold er tilføjet, er fortsat kun en indholdsindikator:
  mærket hedder nu "Indhold tilføjet" og er ikke en godkendelse.
- Godkendelse og tilbagesendelse kræver den eksisterende globale approlle
  `ActivityAdmin`. Der er ingen ændring i rollematricen. Indsendelse kræver
  samme teamadgang som redigering: et teammedlemskab med `IsAdmin` eller
  global `ActivityAdmin`. Øvrige teammedlemmer har læseadgang og kan se
  status og feedback; de får ingen nye redigeringsrettigheder.
- Tilbagesendelse kræver en begrundelse på højst 4.000 tegn. Seneste
  vurdering gemmes med tidspunkt, bruger og feedback. Feedback bevares
  under rettelser og genindsendelse og ryddes ved godkendelse. Der er endnu
  ingen fuld historik eller notifikationsmail.
- En faktisk ændring af navn eller katalogindhold (også katalogbillede)
  sender en godkendt eller indsendt aktivitet tilbage til Kladde og rydder
  indsendelsestidspunktet. Der kræves derefter ny eksplicit indsendelse.
  Rettelser i en tilbagesendt aktivitet bevarer status Ændringer ønskes.
  En gemning uden indholdsændringer bevarer status. Ændringer i Google-filer
  påvirker ikke godkendelsen; den gælder oplysningerne i HQ.
- `review_version` kontrolleres ved gemning og alle statusændringer, også
  som database-concurrency-token. En gammel version giver HTTP 409 frem
  for at overskrive eller godkende indhold, der er ændret af en anden.
- Adminoversigten viser tællere for afventende, tilbagesendte, godkendte og
  alle aktiviteter samt søgning, team- og statusfiltre. Listen viser
  aktivitet, team, godkendelsesstatus, budget og indsendelsesdato. Teamets
  egen liste viser også den nye godkendelsesstatus.
- Aktivitetens dashboard har et godkendelsespanel med indsend/genindsend og
  for administratorer Godkend/Send retur. Ikke-gemte lokale ændringer skal
  gemmes før en statusændring. Kataloget og dokumentlinks bruges til
  gennemgang. Eksisterende aktiviteter starter som Kladde.
- Budget er skrivebeskyttet i denne arbejdsgang og indgår ikke i det nye
  aktivitetsopdaterings-DTO. Den tidligere generelle aktivitetsopdatering
  kunne også ændre budgettet; dette er erstattet af indholdsredigering.
  Et egentligt interface til budgettildeling er fortsat udskudt, ligesom
  massehandlinger, fuld historik og notifikationer. Opgaver på aktivitetens
  dashboard er fortsat udskudt.

Implementeringsnoter:
- `021_activity_review.sql` tilføjer status, versionskontrol og seneste
  vurdering på `activity`. Der ændres ikke på budget- eller katalogskemaet.
- `POST /api/v1/activity/activity/{id}/submit`, `/approve` og
  `/request-changes` modtager den forventede `reviewVersion`; den sidste
  handling modtager også `feedback`. `PUT` til aktiviteten kræver nu også
  `reviewVersion`. Reviewfelter kan ikke sættes gennem den almindelige
  indholdsopdatering.

### Økonomi

- Tildel aktiviteter budgetter og eksporter forbrug.
- Eksporter deltagerantal pr. dag og pr. gruppe.
- Muligheden for fakturering til grupper skal undersøges; den er ikke besluttet.

**Beslutning 2026-11-21 (DS-17, ikke implementeret):** Økonomimodulet skal
afløse det regneark, lejrchefen i dag fører manuelt til budget og regnskab.
Formålet er struktureret data, der senere kan bruges til automatisering, ikke
et regneark bygget ind i HQ. Beslutningen erstatter den tidligere afgrænsning
til aktivitetsbudgetter og udtræk af forbrug. Arbejdet er delt op i DS-72 til
DS-82 med afhængigheder på tavlen. DS-17 er samlekort.

- Data er et træ med Indtægter og Udgifter øverst og kategorier og
  underkategorier i flere niveauer, som brugerne selv opretter, som i 24. Fx
  Udgifter, Materialer, de enkelte aktiviteter, eller Administration,
  Administrationsmateriale. Kategorier er ikke hardkodet.
- Hver post har budget og realiseret, ét tal hver i første version, som
  `decimal` i DKK. Et tomt beløb er ikke det samme som 0. Summer beregnes op
  gennem træet og gemmes ikke. En post kan kobles til en aktivitet.
- `activity_budget` flyttes over i træet, så aktivitetens budget kun har én
  kilde. Budgetændringer må ikke påvirke aktivitetens review eller indhold.
- Der laves ingen import af regnearket. Lejrchefen flytter tallene over manuelt
  for at lære systemet, så redigering skal være hurtig (inline, tastatur,
  flytning og sortering).
- Lejrchef og kasserer får nye roller `FinanceView` og `FinanceManage`.
  Kassereren får også `ActivityView` og `ExportsView`, men ikke
  `ActivityAdmin`.
- Der indtastes deltagerpriser for hel uge, halv uge og dag. Deltagerbetaling
  beregnes som indtægt ud fra tilmeldte deltagere og priserne.
- Kassereren skal i HQ kunne se, hvad hver gruppe skal faktureres for: antal
  pr. pristype og beløb pr. gruppe, uden navne på spejdere. Excel-udtræk af
  faktureringen og selve faktureringen er ude af scope foreløbig. Det afløser
  punktet om at undersøge fakturering til grupper.
- Det skal registreres i tilmeldingen, hvilke dage hver deltager er på lejren
  (DS-72). Det er første kort, fordi deltagerbetaling og fakturering bygger på
  det. Tabellen `scout_signup` findes, men bruges ikke.
- Materialesystemet skal senere fodre forventet materialeforbrug automatisk ind
  i økonomien (DS-82) og kræver, at `Material.Price` skiftes fra `double` til
  `decimal`.
- Ude af scope: bogføring, bilag, moms, bankafstemning, betalinger, udlæg og
  refusion samt selve faktureringen.

Stadig åbent: reglen fra deltagelsesdage til pris (DS-79/DS-80), om ledere og
hjælpere registreres og betaler (DS-97), om der er særlige priser for ledere
eller aldersgrupper, og kassererens øvrige behov (DS-4).

**Beslutning 2026-11-23 (DS-72 og DS-96, implementeret, se implementeringsnoterne):** Deltagelsesdage
registreres i den endelige tilmelding. Beslutningen præciserer punktet om
deltagelsesdage ovenfor.

- Kun spejdere registreres med dage i første version. Ledere og hjælpere er
  skubbet til DS-97. Modellen skal kunne udvides med en deltagertype uden at
  ændre tabellerne for dage.
- Enheden er hele dage. Der registreres ingen ankomst- eller
  afrejsetidspunkter.
- Lejrens dage er data i en ny tabel `camp_day`, som lejrchefen redigerer på
  `/camp-settings` med `PreSignupManage` (DS-96). Hver dag hører til 1.
  halvdel, 2. halvdel eller begge, så en skiftedag kan være med i begge
  halvdele. En dag med tilmeldinger kan ikke slettes, men datoen kan rettes.
  Datoerne hardkodes ikke.
- Spejderens dage gemmes i en ny tabel `scout_camp_day` med fremmednøgle til
  `camp_day`. Den ubrugte `scout_signup` med fri dato fjernes i en senere
  release (expand/contract).
- En ny spejder har ingen dage. En spejder uden dage betyder altid "mangler at
  blive udfyldt"; der findes ingen tilstand for "0 dage", og en spejder, der
  ikke deltager, slettes. Det følger princippet om, at tomt ikke er 0.
- På `/group/final-signup` vælges Hel uge, 1. halvdel, 2. halvdel eller
  enkelte dage pr. spejder, også ved oprettelse og for flere spejdere eller en
  hel patrulje ad gangen. Manglende dage vises som advarsel øverst på siden,
  som mærkat pr. spejder og som status på `/group`, med en knap, der giver alle
  uden dage samme periode.
- Spejdere uden dage tæller ikke med i deltagerbetaling og
  faktureringsoversigt, men vises som "N spejdere mangler dage" pr. gruppe
  (DS-80, DS-81).
- Lukket endelig tilmelding låser ikke dagene. Hvad lukket tilmelding betyder,
  afklares på DS-98.
- Hensyn, ikke beslutning: aktivitetstilmeldingen skal senere kunne tjekke, om
  spejdere er på lejren. DS-72 garanterer kun, at data kan besvare, hvem der er
  tilmeldt en given dato, og hvem der mangler dage, med datoer regnet i
  Europe/Copenhagen. Om aktivitetstilmeldingen blokerer, advarer eller kun
  tilmelder de tilstedeværende, og hvordan spejdere uden dage håndteres der,
  er ikke besluttet. Reglen om, at en patrulje ikke må tilmeldes på dage, hvor
  den ikke er på lejren, står derfor uændret som hensigt.

### Materialer

- Ved oprettelse af materialelister vises materialer, som andre aktiviteter
  allerede har bestilt.
- Materialer reserveres kun de dage, hvor aktiviteten har moduler.
- Genbrug på tværs af aktiviteter skal reducere indkøb, eksempelvis fra 15 til
  10 økser, når behovene ikke overlapper.
- Materialer kan markeres som stationære og dermed undtages fra turnusordningen.
- Materialeansvarlige skal hver aften kunne se, hvordan materialer skal flyttes,
  så aktiviteter kan starte igen næste morgen.
- Den første udskrift af materialelister tager udgangspunkt i dag 1.

### Filopbevaring

**Beslutning 2026-10-09:** Der indføres et generelt fil-lager til billeder.
Katalogbilleder (aktivitetsbilleder) er **offentlige** og betjenes uden login via
et ugætteligt `public_id`. **Profilbilleder** er **bag login**. Billeder
konverteres **kun** til WebP; originalfilen gemmes ikke efter konverteringen.
Profilbilleder centercroppes til **1:1**. Konverteringen sker asynkront i et
**Hangfire-job** (`FileProcessingJobs.ConvertToWebp`), ikke synkront under
upload, så et stort eller langsomt billede ikke blokerer API-kaldet.

Upload gemmer den rå fil midlertidigt, opretter rækken i `stored_file` med
`status = 'Pending'` og enqueuer et konverteringsjob. Jobbet læser den
midlertidige fil, retter EXIF-orientering, centercropper ved profilformål,
skalerer til maks. 2000 px, fjerner metadata og koder WebP (kvalitet 80).
Derefter opdateres rækken til `Ready` med `width`, `height`, `size_bytes`,
`sha256` og `storage_key`, og den midlertidige fil slettes. Fejler jobbet,
sættes `status = 'Failed'` med fejlteksten og Hangfire genprøver
(30/120/300 s, 3 forsøg).

Lagringen er **lokal disk** via et Docker-volume (`files` → `/app/files`,
`DS__FileStoragePath`) indtil videre. `IFileStorage` abstraherer gem/hent/slet,
så en senere S3-implementering kan indsættes uden at ændre kaldstederne.
Metadata (ejer, størrelse, mål, hash, oprindeligt navn, formål) ligger i
`stored_file`; selve filerne ligger på disken under nøgler, der ikke kan pege
uden for lagerroden.

Billedbiblioteket er **SkiaSharp** (MIT, licensfri). ImageSharp blev fravalgt,
fordi 3.x/4.x kræver en licensnøgle for Release-builds, hvilket ville bryde
containerbygget uden en registreret nøgle.

Endpoints under `/api/v1/files`: `POST /{purpose}` (multipart, `catalog` eller
`profile`), `GET /public/{publicId}` (anonym, kun offentlige og `Ready`),
`GET /{publicId}` (login), `DELETE /{id}` (uploader eller systemadministrator).

Et uploadet billede refereres med `ImageReferenceDto` (`public_id` + `url`). I
`ActivityDto` bæres referencen af `CatalogDataDto.Image`, og ved opdatering af
en aktivitet sættes `catalog_data.image_file_id`, når billedet findes, er
`Ready`, har formålet `CatalogImage` og er uploadet af samme bruger. `PUT
/api/v1/me/profile-picture` sætter på samme måde
`asp_net_users.profile_picture_file_id` (billedet skal være `ProfilePicture`,
`Ready` og uploadet af brugeren selv); et `null`-billede rydder feltet.
`MeDto.ProfilePicture` returnerer den aktuelle reference. Personalet kan rette
en anden brugers billede via `PUT /api/v1/user/{id}/profile-picture`, som kræver
den særskilte `AppRoles.UsersEditProfilePicture` (kun tildelt SysAdmin): samme
validering, men det uploadede billede skal være uploadet af den handlede bruger,
eftersom uploaderen er den, der knytter billedet til. `UsersView` alene giver
altså kun visning, ikke ret til at ændre billedet.

Frontend: `Profile.vue` har et Buefy-uploadfelt til profilbilledet. Filen
uploades til `/api/v1/files/profile`, der ventes på, at baggrundsjobbet sætter
den til `Ready` (ved at polle den private fil-URL), og referencen knyttes
efterfølgende via `PUT /api/v1/me/profile-picture`. Billedet vises som en rund
avatar, og et "Fjern"-knap kalder samme endpoint med `null`. Profilbilledet
vises også i brugeradministrationen: `GET /api/v1/user` leverer `UserDto` med
`ProfilePicture`, og `UserAvatar.vue` tegner det i listen
(`UserSidebarBox.vue`) og i detaljevisningen (`User.vue`), med initialer som
fallback når der ikke er noget billede. `UserMetadata.vue` har desuden et
uploadfelt (gated med `<Can role="UsersEditProfilePicture">`), der sætter den
valgte brugers billede med det samme via `PUT /api/v1/user/{id}/profile-picture`.

- Profilbilleder beskæres før upload i både egen profil og brugeradministration.
  Vue Advanced Cropper (MIT, Vue 3) erstatter den hjemmelavede beskæring og giver
  lokal forhåndsvisning med træk/touch, zoom, rotation og spejling i et kvadratisk
  udsnit. Knapper giver også tastaturbetjening af placering og billedværktøjer.
  Først ved »Beskær og upload« sendes udsnittet som et 512 × 512 PNG-billede;
  annullering ændrer ikke det eksisterende billede. Serverens billedbehandling og
  adgangskontrol er uændret.

Headerens brugermenu (`Back.vue`/`UserMenuTrigger.vue`) viser nu profilbilledet,
navnet og brugerens gruppetitel i stedet for mailen. Titlen afledes i
`AppAccess.ResolveGroupTitle` af den første matchende gruppe i
`AppGroups`-enumets rækkefølge (SysAdmin, CampAdmin, EventAdmin, ActivityAdmin,
ActivityUser, FinanceAdmin, PRAdmin, FoodAdmin, User), og hver gruppe har en
dansk visningstitel i `AppAccess.GetGroupTitle` (fx CampAdmin = "Lejrchef",
ActivityUser = "Aktivitets-teammedlem", User = "Deltager"). `MeDto.Title`
leverer titlen til frontend. Enum-rækkefølgen er bevidst den prioriterede
rækkefølge: en bruger med flere grupper ser titlen for den første i rækkefølgen.

Ubesluttede punkter:
- Et katalogbillede kan endnu ikke ryddes via aktivitetsopdateringen: sendes
  `Image` ikke, bevares det eksisterende billede.
- Hvem må uploade katalogbilleder? I dag følger upload blot login, og koblingen
  kræver kun, at uploaderen er den samme bruger.
- Skal sletning nægtes, så længe et billede er i brug af en aktivitet eller
  profil?
- Der er endnu ingen frontend til katalogbilleder (kun profilbilleder), og
  `waitUntilReady` i `FileService` poller fil-endpointet, fordi der ikke findes
  et status-endpoint.

### Google Drive-filer

**Beslutning 2026-10-15:** Aktivitetsteams kan oprette Google Docs-dokumenter
til en aktivitet direkte fra HQ. Dokumenterne oprettes via **bruger-OAuth**: en
HQ-administrator forbinder én gang en Google-konto på `/admin/google`, og
dokumenterne oprettes derefter på vegne af den konto. Ejerskabet følger den
forbundne Google-bruger — ikke en service account og ikke den HQ-bruger, der
klikker "Opret". Refresh-tokenet gemmes krypteret med ASP.NET Core Data
Protection i `google_connection`. Indstillingerne er `DS:GoogleClientId`,
`DS:GoogleClientSecret`, `DS:GoogleRedirectUri` og `DS:GoogleDriveBaseFolderId`
(lejrmappen).

**Beslutning 2026-10-15 (udvidet):** Oprettelse omfatter Docs, Sheets og
Slides, og fanen viser hele aktivitetens mappe i Drive — ikke kun de filer, der
er oprettet fra HQ. Listen hentes live fra Drive med 30 sekunders cache i
`IMemoryCache`, som ryddes ved oprettelse, så den, der lige har oprettet en fil,
ser den med det samme. Et planlagt Hangfire-syncjob blev overvejet og fravalgt
for nu: et fanebesøg koster ét Drive-kald, hvilket er langt under kvoten, og et
job ville give en forsinkelse og selv skulle håndtere omdøbninger og
sletninger. Jobbet bliver først relevant ved søgning på tværs af aktiviteter
eller notifikationer.

**Verificeret 2026-10-15 (service account):** En service account med
skriverettighed på en mappe i et almindeligt My Drive kan oprette **undermapper**
(mapper optager ingen lagerplads), men **ikke** dokumenter eller andre filer.
Google svarer `403 storageQuotaExceeded` med beskeden "Service Accounts do not
have storage quota. Leverage shared drives, or use OAuth delegation instead",
fordi den oprettede fil ville blive ejet af service accounten, som ikke har
lagerplads.

**Beslutning 2026-10-15 (ændret):** Service account med domain-wide delegation
blev først valgt, men er droppet, fordi det kræver en
Workspace-superadministrator, som ikke er tilgængelig. Delt drev blev også
overvejet og droppet, fordi det ville kræve, at lejrmappen flyttes, og fordi
organisationen kan spærre for eksterne medlemmer. Bruger-OAuth er valgt, fordi
det kun kræver adgang til Google Cloud-projektet (til OAuth-klienten), som
projektet allerede administrerer. Service account-nøglen,
`DS:GoogleServiceAccountPath` og `DS:GoogleDriveImpersonateUser` er derfor ikke
længere i brug.

**Verificeret 2026-10-15 (bruger-OAuth):** Forbindelsen blev oprettet mod
lejrmappen, og et dokument oprettet fra HQ åbner direkte i Google Docs.

**Beslutning 2026-10-15 (ændret):** Den indlejrede visning er droppet igen.
Den krævede, at browseren var logget på en Google-konto med adgang til filen;
filerne ejes af den forbundne Google-bruger og deles ikke med HQ-brugerne, så
andre brugere fik Googles login-/adgangsside i stedet for selve dokumentet.
Fanen viser derfor kun en liste, hvor hver række åbner filen i Google i en ny
fane. `PreviewUrl` er fjernet fra både `GoogleFileTypes`, `ActivityFileDto` og
frontend-typen.

**Beslutning 2026-10-16 (udvidet):** Mapper er tilføjet som en fjerde `kind`
(`folder`), så aktivitetens filer kan organiseres. Der kan oprettes mapper i
aktivitetsmappen og i dens undermapper, og fanen viser den aktuelle mappe med en
brødkrumme til at gå op igen. Antallet af niveauer er ikke begrænset i sig selv,
men valideringen af, at en vist eller angivet mappe hører til aktiviteten, går
opad i forældrekæden og opgiver efter 20 led. Mapper gemmes i `activity_file` på
samme måde som filer, så "Oprettet af" også vises for mapper.

- Ved oprettelse sørger `GoogleDriveService` for en undermappe pr. aktivitet
  under basemappen. Mappen mærkes med `appProperties.activity_id`, og ID'et
  gemmes i `activity_drive_folder`, så mappen kan flyttes frit i Drev uden at
  forbindelsen brydes. Efterfølgende opslag bruger det gemte ID og rører ikke
  basemappen.
- Findes der intet gemt ID — fx for aktiviteter, der fik en mappe før denne
  ændring — findes mappen ved at søge på `appProperties.activity_id` i hele
  Drev, uafhængigt af hvor mappen ligger, og ID'et gemmes, så det kun sker én
  gang. Basemappen bruges derfor kun, når der skal oprettes en ny mappe.
- På visningsstien oprettes der ikke en mappe; mangler den helt, er listen tom.
- Er en gemt mappe slettet eller flyttet til papirkurven i Drev, glemmer HQ
  ID'et: visningen bliver tom i stedet for at fejle, og næste oprettelse danner
  en ny mappe.
- Filerne er tomme Google-filer med aktivitetens navn som standardtitel, mens
  mapper hedder "Ny mappe", hvis navnet udelades. `kind` er `doc`, `sheet`,
  `slides` eller `folder` og oversættes til MIME-typen
  `application/vnd.google-apps.document|spreadsheet|presentation|folder`.
  Skabeloner, der kopieres og udfyldes ved oprettelsen, er en senere udvidelse;
  `activity_file` har derfor ingen skabelonreference.
- Listen sammensættes af indholdet i den aktuelle mappe og beriges med rækkerne i
  `activity_file` (matchet på `drive_file_id`), så "Oprettet af" kun vises for
  filer og mapper oprettet fra HQ. Titlen kommer fra Drive, fordi filer kan
  omdøbes der. Mapper vises øverst med mappeikon, og andre filtyper end Docs,
  Sheets og Slides vises også, med et generisk ikon og link til Drive.
- Rettigheder følger aktiviteten: oprettelse kræver samme adgang som
  redigering af aktiviteten (`HasAccessAsync(teamId, true)`), og læsning følger
  visningsadgang. Endpoints er `GET /api/v1/activity/activity/{id}/files` og
  `POST /api/v1/activity/activity/{id}/file/add`; begge tager en valgfri
  `folderId` i forespørgslen, hvor udeladt betyder aktivitetsmappen. En
  `folderId`, der ikke ligger i aktivitetsmappen, giver `404`, så en klient ikke
  kan læse eller skrive i vilkårlige Drev-mapper.
- I fanen åbner en filrække filen i Google i en ny fane, mens en mapperække går
  ind i mappen i HQ. Brødkrummen viser placeringen og kan bruges til at gå op
  igen. Fanemarkøren for "Filer" tændes ud fra aktivitetsmappen, ikke den viste
  undermappe.
- Selve Google-forbindelsen styres af `GoogleApiController`.
  `GET /api/v1/google/status` kan læses af alle brugere, mens
  `GET /api/v1/google/connect`, `GET /api/v1/google/callback` og
  `POST /api/v1/google/disconnect` kræver `AdminAccess`. Connect bruger en
  kortlivet state-cookie som CSRF-værn, og OAuth-scopet er
  `https://www.googleapis.com/auth/drive`.
- Er Google-integrationen ikke konfigureret, svarer både listen og oprettelsen
  `503`. Er den konfigureret, men uden en forbundet Google-konto, svarer begge
  også `503`. Svarer Google ikke inden for 20 sekunder, eller fejler kaldet,
  svarer de `502`.
- Frontend viser en "Filer"-fane i aktivitetsdetaljen med en liste over
  aktivitetens filer i en tabel over hele fanens bredde med typeikon, titel,
  "Oprettet af" og "Sidst ændret". Filnavnet er et link til Google i en ny
  fane, hvor både visning og redigering foregår. Oprettelsen ligger i en
  separat, kompakt formular med besked om, at filer gemmes straks; knappen
  "Gem aktivitet" gemmer aktivitetens øvrige oplysninger. Udfyldningsstatus
  angiver, om navn eller Katalog mangler indhold. Dette ændrer ikke
  reglerne for, hvornår en aktivitet regnes som udfyldt.
- Forbindelsen oprettes og afbrydes på `/admin/google`, som er linket fra
  adminområdet. Fanen viser en advarsel og skjuler opret-formularen, indtil der
  er en forbundet konto.
- Filer slettes ikke i Drive, når aktiviteten slettes; rækken i `activity_file`
  soft-slettes/cascades, så Drive er kilden til sandhed for selve filen.
- Drive-klienten er `Google.Apis.Drive.v3`. Pakken trækker
  `System.Security.Cryptography.Pkcs`, hvis standardversion (10.0.0) havde kendte
  sårbarheder; den er derfor pinnet til 10.0.13 i `DS.Backend.csproj`, så
  restore er uden advisories.

Ubesluttede punkter:
- Om OAuth-klient-ID, klienthemmelighed og redirect-URI i containeren skal
  komme fra miljøvariabler eller mount i stedet for `.env`/repoet.
- Hvilke skabeloner der skal kunne oprettes ud fra, og hvordan felter udfyldes.
- Om filer og mapper skal kunne omdøbes, slettes eller flyttes fra HQ.
- Om Drive-mappen skal omdøbes, når aktiviteten omdøbes i HQ; navnet sættes i
  dag kun ved oprettelsen.
- Om filer skal deles med aktivitetsteamet i Google (fx "alle med linket"), så
  linket virker for brugere uden adgang til den forbundne Google-konto, og
  hvilke adgangsregler der i så fald gælder.
- Om der skal vises advarsel, når refresh-tokenet er udløbet eller tilbagekaldt,
  så forbindelsen kan fornyes i tide.
- Om der senere skal tilføjes et syncjob, så søgning på tværs af aktiviteter og
  notifikationer kan bygges på databasen i stedet for Drive.

### Dataudtræk

Planen under Økonomi nævner eksport af deltagerantal pr. dag og pr. gruppe, men
beskriver ikke hvordan udtræk skal defineres eller hvem der må hente dem.

**Beslutning 2026-09-29:** Dataudtræk defineres udelukkende i backend. Hvert
udtræk er en `DataExport`-klasse under `DS.Backend/Exports/`, som registreres som
scoped service i `ServiceExtensions.AddDSServices`. Frontend henter listen fra `GET /api/v1/exports`
og renderer den, så et nyt udtræk kræver ingen frontend-ændring. Siden ligger
på `/exports` og linkes fra adminområdet.

- Første udtræk er grupper med distrikt og forhåndstilmeldingens syv
  aldersgrupper plus en sumkolonne. Grupper uden forhåndstilmelding får tomme
  felter, så de kan skelnes fra grupper der har tilmeldt nul.
- Filer genereres som `.xlsx` med ClosedXML. Ark får frosne overskrifter,
  autofilter og tilpassede kolonnebredder via `ExportSheetExtensions`.
- Adgang kræver `ExportsView`, som er tildelt systemadministrator og lejrchef.
  Hvert enkelt udtræk kræver desuden sin egen rolle, i dag `GroupsView`, og den
  kontrolleres både i listen og ved download, så en nøgle ikke kan gættes.
- Der er ikke bygget udtræk for spejdere, brugere, aktiviteter, patruljer eller
  materialer. De blev gennemgået, men valgt ud i første version. Spejdere og
  brugere ville indeholde persondata og kræver en egen afvejning.
- `ParticipantData` var hardkodet placeholder-data og blev ikke eksporteret,
  fordi tallene ikke var rigtige. Den er fjernet med DS-71.
- Der logges ikke, hvem der har hentet et udtræk. Det aktuelle udtræk indeholder
  kun tal, men kommende udtræk med persondata bør overveje det.
- Backend manglede hidtil danske visningsnavne på enums; de ligger nu i
  `DS.Backend/Labels.cs`. Frontend har stadig egne kopier i `group.types.ts` og
  `GroupsScouts.vue`, som på sigt bør læse fra den fælles kilde.

### Auditlog – nice to have

Det skal eventuelt være muligt at se, hvem der har ændret hvad. Den planlagte
adgang er begrænset til systemadministrator og lejrchef.

**Beslutning 2026-09-29:** `AuditLogView` er nu tildelt systemadministrator og
lejrchef, så adgangen matcher den planlagte. Auditloggen er endnu hverken
implementeret eller synlig i adminområdet.

### Projektstyring og Kanban-board

**Beslutning (DS-50, godkendt og implementeret):** Kun den effektive
SysAdmin-rolle kan slå udviklerfunktionen til eller fra i tavleindstillingerne.
Nye og eksisterende tavler har funktionen slået fra som standard.

- Brugere med redigeringsadgang til en udviklertavle kan tilføje og fjerne
  flere GitHub-PR-links enkeltvis i kortdialogen eller via `add_pull_request`
  og `remove_pull_request` i både remote- og stdio-MCP. `get_board` viser links.
- Kortdialogens PR-sektion åbner linkfeltet med »Tilføj« og lukker det efter
  tilføjelse (DS-51).
- Links er afgrænset til tavleplaceringen. De skjules ved deaktivering og
  kommer tilbage ved genaktivering. Under deaktivering afvises ændringer.
- Serveren accepterer eksplicitte HTTPS-links til konkrete PR'er på
  `github.com`, normaliserer dem og afviser dubletter. Ændringer opdaterer
  den berørte tavle via SignalR.

**Beslutning 2026-11-23 (DS-99, database, service og API implementeret):** En
epic er et almindeligt kort, der er markeret som epic på en udviklertavle.
Andre kort på samme tavle kan høre til én epic. Epics afløser »samlekort«, der
samler kort med afhængigheder. UI (DS-100), filter (DS-101) og MCP (DS-102) er
implementeret, se nedenfor.

- **Model:** Markeringen og relationen hører til tavleplaceringen
  (`board_card.is_epic` og `board_card.epic_board_card_id`), ligesom PR-links,
  og findes derfor kun på udviklertavler. Et delt kort kan være epic på én
  tavle og ikke på en anden.
- **Regler i `BoardService`** (så de også gælder for MCP): et kort hører til
  højst én epic; kun ét niveau, så en epic ikke kan høre til en epic, og et
  kort, der hører til en epic, ikke kan markeres som epic; epic og kort skal
  ligge på samme tavle; målet skal være markeret som epic; et kort kan ikke være
  sin egen epic. Fjernes markeringen, slippes kortene i samme transaktion. Når
  en epic fjernes fra tavlen eller soft-slettes, slipper databasens `ON DELETE
  SET NULL` kortene i samme sætning. Kontrollerne er ikke beskyttet mod to
  samtidige ændringer, ligesom ringkontrollen for afhængigheder.
- **Udviklerfunktion og adgang:** Ændringer kræver redigeringsadgang til tavlen
  og en udviklertavle; ellers afvises de som PR-links. Under deaktivering
  bevares data, men skjules i API'et.
- **Kolonner og færdig:** Epics står i tavlens normale kolonner som en start
  (besluttet 2026-11-23). En epic flyttes manuelt til Færdig; serveren flytter
  eller spærrer ikke. Afhængigheder betyder fortsat rækkefølge og er uændrede.
- **Fremdrift:** Serveren beregner for hver epic antal kort på tavlen, der
  hører til det, og hvor mange af dem der står i en færdig-kolonne på samme
  tavle.

*Implementeret:* Migration `039_board_card_epics.sql`. API'et har `PUT` og
`DELETE /api/v1/boards/{id}/cards/{workItemId}/epic` til at markere og
afmarkere en epic, `PUT /api/v1/boards/{id}/cards/{workItemId}/epic-link` med
`epicWorkItemId` (DS-nummer) til at lægge kortet under en epic og `DELETE
.../epic-link` til at fjerne det igen. Gentagne kald er idempotente. Tavlens
kort har `isEpic`, `epicCardId` (epic'ens DS-nummer) og for epics
`epicProgress` med `done` og `total`; felterne er kun udfyldt på
udviklertavler. Ændringer opdaterer den berørte tavle via SignalR. Frontenden
har kun typer og metoder i `BoardService.ts` (`setEpicMarking`, `setCardEpic`,
`removeCardEpic`), ingen UI.

*Implementeret (DS-102, `src/DS.Backend/Mcp/BoardTools.cs`,
`tools/ds28-mcp/server.mjs`, `tools/ds28-mcp/lib.mjs`):* Remote- og stdio-MCP
har samme parametre og tekst. `get_board` viser »epic: 3 af 7 færdige« på en
epic og »epic: DS-17 <titel>« på et kort under en epic. `create_card` har
`isEpic` og `epicCardId`, som sættes efter oprettelsen; `update_card` har
`isEpic` (true/false, udeladt bevarer) og `epicCardId` (DS-nummer, `0` fjerner
kortet fra sin epic, udeladt/null bevarer). Værktøjerne afviser epic-parametre
på tavler uden udviklerfunktioner og kontrollerer på forhånd, at målet er en
epic på samme tavle, så et kort ikke oprettes halvt; serverens øvrige regler og
fejltekster gælder uændret. Agenter lægger nye kort under en eksisterende epic
efter kortforløbet i `AGENTS.md`.

*Implementeret (DS-100, `BoardCard.vue`, `BoardCardEpic.vue`,
`BoardCardModal.vue`, `BoardDetail.vue`):* På udviklertavler viser et kort, der
hører til en epic, et lille mærke med epic'ens titel (opslag blandt tavlens kort
via `epicCardId`, ellers DS-nummeret). En epic har mærket »Epic« og viser »X af Y
færdige« med en lille fremdriftslinje, i kompakt visning kun teksten. Når alle
kort er færdige (`done === total > 0`), bliver teksten grøn med flueben. I
kortdialogen har eksisterende kort på udviklertavler en sektion »Epic«: en
kontakt markerer eller afmarkerer kortet som epic og skjules, når kortet selv
hører til en epic. Afmarkering af en epic med kort kræver bekræftelse. Et
almindeligt kort kan vælge en af tavlens andre epics eller fjernes fra sin epic.
En epic viser sine kort med DS-nummer, titel, kolonne og færdig-status. De kort
og epic'en på et kort kan klikkes for at åbne det kort i samme dialog; har
dialogen ugemte ændringer, afvises skiftet med en besked. Serverens fejlbeskeder
vises som toast. Uden redigeringsadgang vises oplysningerne uden kontrolelementer.

*Implementeret (DS-101, `lib/board/boardFilters.ts`, `BoardFilterPanel.vue`,
`BoardFilterBar.vue`):* Filterpanelet har et »Epic«-filter, der kun vises på
udviklertavler med mindst én epic. Valgmulighederne er tavlens epics (DS-nummer
og titel, sorteret efter titel) samt »Ingen epic«, med antal kort pr. valg.
Vælges en epic, vises både epic'en selv og de kort, der hører til den. »Ingen
epic« viser kort, der hverken er epic eller hører til en. Flere valg kombineres
med »eller« og med andre filtre med »og«, som i DS-88. Aktive valg vises som
mærker, der kan fjernes, tæller med i antallet af filtre, ryddes af »Ryd
filtre« og »Fjern alle« og findes også i mobilskuffen. Filteret huskes ikke
mellem besøg, og kort kan ikke flyttes, mens der filtreres. Afmarkeres eller
fjernes en valgt epic, fjernes den fra valget; forsvinder tavlens sidste epic,
ryddes hele epic-filteret.

**Beslutning 2026-11-21 (godkendt og implementeret i DS-56 til DS-62, redesign af kanban-visningen):**
Tavlevisningen skal redesignes ud fra et klikbart mockup, som brugeren har
godkendt 2026-11-21, inklusive mobilvisningen. Mockuppet blev først lavet som en
statisk HTML-side og derefter i den daværende beta-frontend med Nuxt UI og
testdata. Det er kun til lokal visning og skal ikke merges.

**Beslutning 2026-11-21:** Redesignet blev bygget i beta-frontenden, som samme
dag blev den officielle frontend i `src/DS.Frontend` (se
[Frontend med Nuxt UI](#frontend-med-nuxt-ui)). Redesignet er derfor
tavlevisningen i den nuværende frontend. Den tidligere regel om, at
`src/DS.Frontend` ikke måtte ændres, er afløst og gælder ikke længere.
Arbejdet er delt i kort på DS28 IT-tavlen: DS-56 kort, DS-57 filterlinje,
DS-58 tavlehoved, DS-59 kolonnemenu, WIP og sammenklapning, DS-60 hurtig
oprettelse, DS-61 API til medlemmer og DS-62 medlemsdialog (venter på DS-61).

- **Kolonner (besluttet 2026-11-20):** Kolonnerne er hvide flader med skygge
  på en grå tavlebaggrund, med en 4 px farvet kant i toppen og en skillelinje
  under kolonneoverskriften. Kantens farve er fast: grå for almindelige
  kolonner og grøn for færdig-kolonnen. Den er ikke valgfri pr. kolonne, så
  kolonner får ingen ny farve i databasen. Frontenden har dette udseende.
- **Kolonnemenu:** Én »…«-menu erstatter rediger- og sletknapperne: omdøb,
  WIP-grænse, færdig-kolonne, klap sammen og slet. WIP-grænsen vises som en
  bjælke og med tekst, når den overskrides. Kolonner kan klappes sammen til
  en smal lodret strimmel. **Beslutning 2026-11-21:** Sammenklapningen huskes
  kun i browseren (localStorage), ikke pr. bruger på serveren.
  *Implementeret (DS-59, `BoardColumn.vue`):* »…«-menuen har »Rediger
  kolonne …«, en WIP-undermenu (ingen, 2, 3, 4, 5, 6, 8, 10 eller »Anden …«,
  som åbner kolonnedialogen), »Færdig-kolonne« som afkrydsning, »Klap sammen«
  og »Slet kolonne …«. WIP-grænse og færdig-markering gemmes straks gennem den
  eksisterende kolonneopdatering. Antal kort bliver gult, når grænsen er
  overskredet. Sammenklappede kolonner gemmes som kolonne-id'er under
  `ds28.board.collapsedColumns`; strimlen viser titel, antal og færdig-ikon, er
  selv en knap, der folder ud, og et kort, der slippes på den, lægges sidst.
  Brugere uden redigeringsadgang kan kun klappe sammen.
- **Tavlehoved:** Tavlens handlinger er samlet i én »Tavle«-menu med »Tilføj
  kort« som eneste primære knap. Medlemsavatarerne åbner medlemmer og
  invitation. Titel, filterlinje og handlinger står på én linje på brede
  skærme.
  *Implementeret (DS-58, `BoardDetail.vue`):* Knappen »Kolonne« er
  fjernet fra topbaren; »Ny kolonne …« ligger i »Tavle«-menuen sammen med
  Indstillinger, Medlemmer og invitation, Tags og Farver. »Tilføj kort« åbner
  kortdialogen i den første kolonne, der ikke er en færdig-kolonne. Højst tre
  medlemsavatarer vises fra 768 px og åbner medlemsdialogen, og listen hentes
  igen, når dialogen lukkes. På mobil vises »Tavle« og »Tilføj kort« kun som
  ikoner, og mærket »Udviklertavle« skjules. Titel og filterlinje står stadig på
  hver sin linje, fordi frontenden bruger Nuxt UI's navbar og toolbar.
  *Implementeret (DS-87):* Medlemsavatarerne i tavlehovedet og i
  medlemsdialogen (medlemsliste og brugersøgning) viser brugerens
  profilbillede og falder kun tilbage til initialer, når der ikke er et klar
  billede. `BoardMemberDto` har fået `ProfilePicture` (`ImageReferenceDto`) med
  samme regel som `UserSummaryDto`: kun filer med status `Ready`, der ikke er
  slettet.
- **Filterlinje:** Søgning og hurtigfiltre (Mine kort, Overskredet, Venter).
  Tags ligger i deres egen »Tags«-dropdown med afkrydsning og antal kort pr.
  tag, og de valgte vises som mærker, der kan fjernes. Flere tags kan
  kombineres. Det erstatter dagens filter, hvor man kun kan vælge ét tag.
  Desuden kan man vælge mellem detaljeret og kompakt visning.
  *Implementeret (DS-57, `BoardFilterBar.vue`, `lib/board/boardFilters.ts`):*
  Søgning matcher titel og DS-nummer (»DS-12«, »ds12« eller »12«). »Mine kort«
  bruger den aktuelle brugers id. »Overskredet« og »Venter« medtager ikke kort i
  en færdig-kolonne. Flere tags kombineres med »eller«, og alle filtre
  kombineres med »og«. Mens der søges eller filtreres, kan kort og kolonner ikke
  flyttes, og »Fjern alle« rydder både søgning og filtre. Kompakt visning skjuler
  beskrivelse og »Venter på«-mærket og huskes i browseren (localStorage). På
  mobil ligger filtrene i en skuffe bag »Filtre«, og tavlens beskrivelse skjules.
  *Udvidet (DS-88, `BoardFilterPanel.vue`):* Man kan filtrere på ansvarlige
  (en eller flere personer samt »Ingen ansvarlig«) og på prioritet (Høj,
  Normal, Lav). Flere valg inden for samme filter kombineres med »eller«.
  Listen over ansvarlige bygges af de personer, der er tildelt kort på
  tavlen, med den aktuelle bruger øverst. På brede skærme erstatter én
  »Filtre«-knap med en popover rækken af hurtigfiltre og »Tags«-dropdownen;
  aktive filtre vises som mærker, der kan fjernes, ved siden af knappen
  sammen med »Ryd filtre«. Mobilskuffen har de samme filtre. Filtrene huskes
  ikke mellem besøg, ligesom i DS-57. Filtre på forfaldsdato og kolonne er
  ikke med.
- **Kort:** Kortets farve vises som en stribe i venstre side i stedet for at
  tone hele kortet. Deadline vises relativt (»I dag«, »3 dage over«), og
  prioritet får tekst. Antal PR-links vises som et tal.
  *Implementeret (DS-56, `BoardCard.vue`):* Farvestriben erstatter den tonede
  baggrund, og tags står ved siden af DS-nummeret. Deadline vises som »N dage
  over« (rød), »I dag« eller »I morgen« (gul) og ellers som kort dato; i en
  færdig-kolonne altid som dato. Den fulde dato står i tooltip. Prioritet vises
  som »Høj« eller »Lav«, og antal PR-links vises med et PR-ikon.
- **Hurtig oprettelse:** Et kort kan oprettes direkte i kolonnen med Enter.
  **Beslutning 2026-11-21:** Et kort må oprettes med kun en titel.
  *Implementeret (DS-60, `BoardColumn.vue`):* »Tilføj kort« nederst i
  kolonnen åbner et felt i stedet for kortdialogen. Enter eller »Opret kort«
  opretter kortet med kun titel i den kolonne, og feltet bliver stående med
  fokus, så flere kort kan oprettes i træk. Esc eller »Annuller« lukker feltet.
  Fejler oprettelsen, beholdes titlen, og der vises en fejl. Kortdialogen bruges
  stadig fra »Tilføj kort« i tavlehovedet og til at udfylde de øvrige felter.
- **Mobil:** Topbaren viser kun titel, »Tavle«-menuen som ikon og »+«.
  Filtrene samles bag en »Filtre«-knap, der åbner en skuffe nedefra med
  hurtigfiltre, tags, visning, »Ryd filtre« og »Vis N kort«. Kolonnerne fylder
  næsten skærmbredden og snapper på plads.
- **Medlemmer og invitation:** Søgningen viser resultater, mens man skriver,
  uden »Søg«-knap, og en bruger tilføjes med pil op/ned og Enter.
  Medlemslisten viser avatar, navn, mail og antal kort, personen er ansvarlig
  for. Den aktuelle bruger står øverst som »Dig«, og listen kan filtreres. Et
  medlem kan fjernes, og man kan selv forlade tavlen, begge med en bekræftelse
  i rækken. Det følger beslutningen fra 2026-10-18 om, at alle medlemmer har
  lige adgang, og indfører ingen roller.
  **Beslutning 2026-11-21:** Det sidste medlem kan ikke forlade eller fjernes
  fra tavlen, heller ikke af SysAdmin eller CampAdmin. Når et medlem fjernes,
  bevares personens ansvar for kortene.
  *API implementeret (DS-61):* `DELETE /api/v1/boards/{id}/members/{userId}`
  fjerner et medlem, også den aktuelle bruger selv (»Forlad tavle«). Samme
  adgang som invitation: medlemmer samt SysAdmin og CampAdmin. Ukendt medlem
  giver 404, og det sidste medlem giver 409 med »Tavlen skal have mindst ét
  medlem.«. Fjernelsen opdaterer tavlen live. `GET .../members` returnerer nu
  `assignedCardCount`: antal ikke-slettede kort på tavlen, som medlemmet er
  ansvarlig for. Brugersøgningen returnerer ikke antallet. Begge frontends har
  fået `removeMember` og feltet i typerne; ingen skemaændring.
  *Dialog implementeret (DS-62, `BoardMembersModal.vue`):* Søgningen
  starter fra 2 tegn med en kort forsinkelse, og kun det seneste svar vises.
  Pil op/ned og Enter tilføjer den markerede bruger. Medlemslisten viser
  »Ansvarlig for N kort« eller »Ingen kort«, og der vises et filterfelt, når
  tavlen har flere end 5 medlemmer. Fjern-knappen og »Forlad tavle« er
  deaktiveret, når der kun er ét medlem. Efter »Forlad tavle« lukkes dialogen,
  og brugeren sendes til tavleoversigten.

Kræver backend: »Mine kort« kan bygges med den aktuelle brugers id fra
`MeStore`. Fjernelse af medlemmer, »Forlad tavle« og antal kort pr. medlem
findes i API'et fra DS-61.

**Beslutning 2026-11-19 (implementeret, afhængigheder mellem kort):**
Et kort kan vente på andre kort. Det er særligt tænkt til udviklingsarbejde,
hvor fx et frontendkort venter på et backendkort. Alle tre dele er
implementeret: database, service, API og kortsøgning (del 1), frontend (del 2)
og MCP (del 3).

*Implementeret i del 3:* Både remote-MCP (`BoardTools.cs`) og stdio-MCP
(`tools/ds28-mcp`) har `search_cards`, `add_dependency` og
`remove_dependency`. `get_board` viser "venter på" og "blokerer" pr. kort med
DS-nummer, titel og færdig-status; skjulte kort vises som "(skjult kort)".
`remove_dependency` fjerner relationen mellem to kort uanset retning.

**Beslutning 2026-11-19 (implementeret, nyt layout af kortdialogen):** Kortdialogen
har fået et roligere layout ud fra et godkendt mockup. Status på den åbne tavle
står som en pille i headeren og skiftes derfra. Tags står over titlen, og
beskrivelsen har en ramme og en "Rediger"-knap. Sidepanelet er en liste over
egenskaber (Ansvarlige, Stilling, Deadline, Prioritet og Farve) med én række
pr. felt, og klik på en række åbner redigeringen i en dropdown
(`BoardCardProperty.vue`). Ændringer gemmes stadig først med "Gem ændringer",
og footeren viser "Ugemte ændringer", når formularen afviger fra det gemte kort.
"Også på andre tavler" viser kun de øvrige placeringer; at fjerne kortet fra
den åbne tavle ligger i •••-menuen. Koblinger vises som små kort. Den
skrivebeskyttede visning bruger samme layout uden redigering. Felter, API,
rettigheder og gemmelogik er uændrede. På mobil står sidepanelet efter
indholdet, og statuspillen bliver i headeren.

*Implementeret i del 2:* Kortdialogen har sektionen "Afhængigheder"
(`BoardCardDependencies.vue`) under beskrivelsen for eksisterende kort, også i
skrivebeskyttet visning. Den viser grupperne "Venter på" med antal færdige og
"Blokerer" med DS-nummer, titel og status pr. kort; skjulte kort vises som "Kort
på en tavle, du ikke har adgang til". "Tilføj" åbner kortsøgningen, og fjernelse
bekræftes. Tilføjelse og fjernelse gemmes med det samme, ligesom tavleplaceringer.
Statusteksten for et kort, der ikke er færdigt, er kolonnen på dets første
synlige placering, der ikke er færdig. Tavlekortet viser et gult mærke "Venter på
DS-<id>" eller "Venter på N kort", undtagen i færdig-kolonner.

*Implementeret i del 1:* Migration `031_work_item_dependencies.sql`. API'et
har `POST /api/v1/boards/{id}/cards/{workItemId}/dependencies` med
`dependsOnWorkItemId`, `DELETE
/api/v1/boards/{id}/cards/{workItemId}/dependencies/{otherWorkItemId}`, som
fjerner relationen mellem de to kort uanset retning, og `GET
/api/v1/boards/cards/search?search=` (højst 10 resultater). Tavlens kort har
`waitingOn` og `blocking` med `id`, `isVisible`, `title`, `isDone` og
synlige `placements`. Et kort, man venter på, og som ikke findes eller er
skjult, afvises med samme besked, så id'er ikke kan sonderes. Placeringer på
slettede tavler tæller ikke med i færdig-reglen; et kort uden placeringer på
ikke-slettede tavler tæller som færdigt. Tavler opdateres live, når et
relateret kort ændres, flyttes til en anden kolonne, deles, fjernes eller
slettes, når en kolonnes færdig-markering ændres, og når en tavle slettes.
Ringkontrollen er ikke beskyttet mod to samtidige oprettelser.

- **Model:** En ny tabel `work_item_dependency` gemmer "kort A venter på kort
  B" med fremmednøgler til begge `work_item` og et unikt par. Relationen hører
  til kortet, ikke til placeringen, og gælder derfor på tværs af tavler.
  "Blokerer" er samme relation set fra det andet kort. Der er kun denne ene
  relationstype. `work_item_link` (Koblinger) genbruges ikke, fordi den mangler
  fremmednøgle, retning og synlighedskontrol og ikke kan ændres efter
  oprettelse.
- **Færdig:** Et kort, man venter på, er færdigt, når alle dets placeringer
  står i en færdig-kolonne (`is_done_column`), også placeringer på tavler, som
  brugeren ikke kan se. Serveren beregner det og sender kun ja/nej til
  klienten.
- **Håndhævelse:** Afhængigheder giver kun en markering. Træk og slip,
  statusskift pr. tavle og MCP's `move_card` spærres ikke.
- **Regler:** `BoardService` afviser afhængighed af kortet selv og ringe, både
  direkte og indirekte, så reglen også gælder for MCP.
- **Synlighed og adgang:** Et kort, man venter på, men ikke må se, vises kun
  med nummer og færdig-status, uden titel, tavle eller kolonne, så skjulte
  tavler fortsat ikke afsløres. For at oprette en afhængighed skal brugeren
  kunne redigere det ventende kort og se det andet kort. For at fjerne en
  afhængighed er det nok at kunne redigere en af de to sider.
- **Sletning:** Når et kort soft-slettes, slettes dets afhængigheder i samme
  transaktion.
- **Tavlen:** Kortet viser "Venter på DS-<id>" eller "Venter på N kort", så
  længe mindst ét ikke er færdigt. Markeringen skjules på placeringer, der selv
  står i en færdig-kolonne, ligesom overskreden deadline. "Blokerer" vises kun
  i kortdialogen. Ændringer i et kort, man venter på, sendes gennem
  `IBoardEventPublisher` til de tavler, der viser ventende kort, så de
  opdateres live.
- **Kortdialogen:** En sektion med "Venter på" og "Blokerer" for eksisterende
  kort, ikke under oprettelse. Man tilføjer via ét søgefelt, der forstår
  "DS-138", "138" og titel. Det kræver et nyt søge-endpoint for kort, som kun
  finder ikke-slettede kort på tavler, brugeren kan se.
- **Ikke med i første version:** notifikationer, når et kort ikke længere
  venter, kort i den fælles søgning (`SearchMenu`), andre relationstyper og
  gendannelse af slettede kort.
- **Opdeling:** 1) database, service, API og kortsøgning (opdaterer også
  `docs/database.md`), 2) frontend i kortdialog og på tavlen, 3) MCP-værktøjer
  (`add_dependency`, `remove_dependency`, `search_cards` og "venter på" i
  `get_board`, i både `BoardTools.cs` og `tools/ds28-mcp/server.mjs`). 1 merges
  før 2 og 3. Et nyt layout af kortdialogen er et separat, uafhængigt kort.

**Beslutning 2026-11-15 (implementeret, live opdatering):** En åben tavle
opdaterer sig selv, når nogen opretter, flytter, redigerer eller sletter kort,
kolonner, farver eller tavleindstillinger — også når ændringen kommer fra MCP
eller en anden bruger. Kun tavler er omfattet; tavleoversigten og resten af HQ
opfriskes fortsat ved sideindlæsning.

- Live opdatering bruger SignalR på `/api/v1/boards/hub` i stedet for polling,
  så ændringer vises med det samme uden fast baggrundstrafik. Klienten bruger
  den eksisterende cookie-session; hubben kræver login.
- Hændelsen udsendes fra `BoardService` gennem `IBoardEventPublisher`, ikke fra
  controlleren. Både cookie-kald fra browseren og Bearer-kald fra MCP går gennem
  servicelaget og udløser derfor samme hændelse. Website implementerer
  publiceringen med SignalR; Application kender ikke SignalR.
- Hver tavle er sin egen gruppe. Klienten melder sig ind med `JoinBoard`, og
  hubben bruger samme synlighedsregel som API'et (`VisibleBoards`), så en
  bruger kun får ændringer for tavler, den må se. Skjulte tavlers id spredes
  ikke.
- Frontend abonnerer kun på den tavle, der er åben, og genindlæser tavlen
  (samlet med kort forsinkelse) i stedet for at flette ændringerne ind. Egne
  handlinger genindlæser fortsat straks efter eget kald. Genindlæsning springes
  over, mens et træk og slip er i gang, så trækket ikke afbrydes.
- Kortets fælles felter — titel, beskrivelse, farve og ansvarlige — er delt på
  tværs af tavleplaceringer, så en kortændring sendes til alle tavler, der viser
  kortet. Tilføjelse eller fjernelse af en placering opdaterer også de øvrige
  tavler, der viser kortets placeringer.
- **Samtidig redigering:** kortdialogen beholder sine egne felter, så en
  indkommende ændring ikke overskriver det, brugeren er ved at skrive.
  Sidste gemning vinder, fordi API'et ikke har versionskontrol; der vises
  ingen konfliktadvarsel i denne version. Slettes kortet under redigering,
  fejler gemningen med den eksisterende fejlmeddelelse.
- Falder forbindelsen, genoprettes den automatisk, og klienten melder sig ind i
  gruppen igen. Kan den ikke oprettes, virker tavlen som før uden live
  opdatering.
- Der tilføjes ingen backplane, fordi der kun kører én Website-instans. Ved
  flere instanser kræver SignalR en backplane. Rettigheder og databaseskema er
  uændrede; live opdatering kræver, at den eksisterende reverse proxy videresender
  WebSocket-opgraderinger.

**Beslutning 2026-10-21 (implementeret):** Et arbejdskort kan have nul eller
flere ansvarlige brugere. Dette erstatter den tidligere enkelte ansvarlige
bruger; den valgfrie ansvarlige stilling er uændret. Ansvarlige tilhører kortet
og er fælles på tværs af tavleplaceringer. Tildeling giver ikke medlemskab eller
adgang til tavler, og de eksisterende adgangsregler er uændrede.

- Kortdialogen kan tilføje flere brugere og fjerne dem enkeltvis. Valgene gemmes
  med kortet, og Annuller ændrer ikke de gemte tildelinger. Tavlekortet viser
  profilbillede eller initialer for hver ansvarlig med navnet i et tooltip.
- API'et modtager `assigneeUserIds`, normaliserer og deduplikerer ID'er og
  afviser ukendte brugere. En tom liste fjerner alle ansvarlige. Svar bruger
  `assignees` med ID, fornavn, efternavn og profilbillede, uden telefonnummer
  eller øvrige administrationsoplysninger. DS-40 har siden gjort listen
  valgfri ved opdatering og tilføjet klarmelding pr. ansvarlig.
- `026_work_item_assignees.sql` flytter eksisterende tildelinger til
  `work_item_assignee` og fjerner den gamle enkeltbruger-kolonne.

**Beslutning 2026-11-21 (DS-40, implementeret i denne ændring, ikke udgivet):**
Hver ansvarlig på et kort kan klarmelde sin egen del. Klarmelding er en status
pr. ansvarlig og er adskilt fra kortets kolonne og udgivelsesstatus: `Ikke
udgivet` betyder stadig merget og `Færdig` deployet.

- Kun den ansvarlige selv kan klarmelde eller trække sin klarmelding tilbage
  (`PUT`/`DELETE /api/v1/boards/{id}/cards/{workItemId}/completion`). Der er
  ingen administratoradgang til at klarmelde for andre i første version.
- Klarmelding blokerer ikke flytning af kortet; alle behøver ikke at have
  klarmeldt, før kortet flyttes videre.
- Fjernes en ansvarlig, slettes klarmeldingen med tildelingen. Tilføjes
  personen igen, starter vedkommende som ikke klarmeldt. Redigering af kortet
  i øvrigt bevarer eksisterende klarmeldinger.
- Ansvarlige på eksisterende kort tilføjes og fjernes enkeltvis
  (`POST /cards/{workItemId}/assignees` og `DELETE
  /cards/{workItemId}/assignees/{userId}`), så samtidige ændringer ikke
  overskriver hinandens ansvarlige. `assigneeUserIds` ved opdatering af et kort
  er nu valgfri: udeladt bevarer listen, en liste erstatter den. Kortdialogen
  bruger enkelt-endpoints for eksisterende kort og sender kun listen ved
  oprettelse.
- Svar bruger `WorkItemAssigneeDto` med `completedAt` ud over brugerfelterne.
  Kortdialogen viser status pr. ansvarlig og knappen »Klarmeld min del«;
  tavlekortet viser »k/n« og markerer klarmeldte avatarer.
- MCP viser »(klarmeldt)« ved ansvarlige i `get_board` og har værktøjet
  `set_completion`. `update_card` sender kun ansvarlige, når de angives.

**Beslutning 2026-10-19 (implementeret, træk og slip):** Tavlen viser en blå
indsættelseslinje før eller efter det kort, musen peger på. Den øverste og
nederste halvdel vælger placeringen, og kortet indsættes samme sted som
markeringen. Drop på kolonnens frie område viser en markering efter sidste
kort med teksten "Slip kortet her", også i tomme kolonner. Kortet, der
trækkes, dæmpes visuelt.

Kolonneflytning markerer venstre eller højre kant efter musens placering og
indsætter kolonnen på den markerede side. Kolonner kan også slippes over
andre kolonners kort. Drop i mellemrummet uden for kolonnerne flytter ikke
noget. Markeringerne fjernes ved drop, afsluttet/afbrudt træk eller tab af
vinduesfokus. Native HTML5-drag beholdes; der tilføjes ikke touch- eller
tastaturbaseret flytning i denne ændring. Rettigheder og databaseskema er
uændrede.

**Beslutning 2026-10-18 (implementeret, kortfarver, udvidet samme dag):** Hver
tavle har sin egen palette af navngivne kortfarver. Dette erstatter den først
implementerede fælles palette med seks faste farver.

- Tavlens "Farver"-dialog lader alle medlemmer samt SysAdmin/CampAdmin tilføje,
  omdøbe, ændre og slette palettefarver. Navnet er påkrævet, højst 64 tegn,
  og farven vælges frit med en farvevælger (`#rrggbb`). Der er ingen fast grænse
  på antallet af farver. Paletten starter med ti farver: blå, grøn, gul, rød,
  lilla, turkis, orange, pink, grå og brun. Hele paletten kan også tømmes.
- Farven vælges fra den aktuelle tavles palette i kortdialogen og gemmes først
  med "Opret kort"/"Gem ændringer". Annuller gemmer ikke kortets farvevalg.
  Paletteændringer har deres egne eksplicitte gemme-/slettehandlinger.
- **Kortets farve forbliver fælles på tværs af tavler** efter brugerens
  udtrykkelige valg: `work_item.color` gemmer selve hex-værdien, ikke en reference
  til en palettefarve. Palettens navn er lokalt for tavlen og er ikke en label
  på kortet. Omdøbning, farveændring eller sletning i paletten påvirker ikke
  allerede farvede kort. En eksisterende farve uden for den aktuelle palette
  kan bevares eller fjernes i kortdialogen; adgang til andre tavlers paletter
  gives ikke gennem et delt kort.
- Kortet viser farven som en lys baggrundstone for at bevare læsbar tekst,
  også når brugeren vælger en mørk farve. Farven er rent visuel og ændrer
  ikke prioritet, status eller adgang. "Ingen farve" viser et hvidt kort.
- `023_work_item_color.sql` tilføjer nullable `work_item.color`;
  `024_board_colors.sql` tilføjer `board_color`, seeder eksisterende tavlers
  paletter og udvider kortets CHECK-constraint til vilkårlig lowercase
  sekscifret hex. Eksisterende kortfarver bevares. API'et normaliserer hex og
  afviser andre farveformater. Nye tavler får samme standardpalette i kode.

**Beslutning 2026-10-18 (implementeret):** Tavleadgang er medlemsbaseret i
stedet for rollegated. Dette afløser `required_role`, `BoardsView` og
`BoardsManage` samt adgangsbeskrivelserne i noterne fra 2026-10-14–16 nedenfor.

- Alle indloggede brugere kan åbne tavleoversigten og oprette en tavle.
  Opretteren bliver automatisk medlem. Almindelige brugere ser kun tavler,
  de er medlem af; SysAdmin og CampAdmin har læse- og redigeringsadgang til alle
  tavler. Andre globale roller giver ikke længere tavleadgang i sig selv.
- Alle medlemmer har lige adgang til tavlens kort, kolonner og indstillinger
  samt til at invitere andre. Der indføres ingen tavleadministrator eller
  særskilt læserolle. Sletning følger samme medlemsadgang. Fra 2026-11-21 kan
  medlemmer også fjerne andre medlemmer og forlade tavlen, så længe der er
  mindst ét medlem tilbage (se redesignet af kanban-visningen).
- Invitation gælder **eksisterende brugere** og tilføjer dem direkte uden
  accepttrin eller mail. Nye konti via mailinvitation er ikke del af opgaven.
  Tavlen har en dialog "Medlemmer og invitation" med medlemsliste og søgning
  efter navn/mail. Søgningen kræver adgang til den konkrete tavle og returnerer
  kun id, navn og mail, ikke den fulde administrationsprofil.
- `board_member` gemmer medlemskaber. Migration `022_board_members.sql`
  tilføjer eksisterende tavlers opretter som medlem og fjerner `required_role`.
  Tidligere rolleberettigede brugere bliver ikke automatisk medlemmer;
  opretteren eller en SysAdmin/CampAdmin kan invitere dem. Tavler uden en
  bevaret opretter kan fortsat administreres af SysAdmin/CampAdmin.
- Samme adgangskontrol bruges til læsning, ændringer, kortplaceringer på andre
  tavler og søgning efter brugere. Skjulte tavlers navne og placeringer sendes
  ikke med kortene. Et delt kort redigeres stadig fælles på tværs af dets
  placeringer; medlemskab af én tavle giver ikke adgang til de øvrige tavler.
- `BoardsView` og `BoardsManage` er fjernet fra rollematricen. HQ-flisen og
  frontendruterne kræver nu kun login. Stillingsvælgeren følger fortsat
  organisationens eksisterende adgang og udvider ikke `OrgView`.

**Beslutning 2026-10-17 (implementeret):** Arbejdskort vises med nummeret
`DS-<id>`, hvor `id` er kortets eksisterende `work_item.id` i databasen.
Nummeret vises på tavlens kort og i kortdialogens overskrift og er det samme
på tværs af tavler. Nye kort får først et nummer, når de er oprettet.
Nummeret afledes ved visning og kræver ingen ekstra kolonne eller migration.

**Beslutning 2026-10-14:** HQ skal ikke kun være en database, men også et
projektstyringsværktøj. Der indføres et generisk board/kanban, som kobler sig
til de øvrige domæner gennem én snæver samlingsflade i stedet for at være endnu
et isoleret modul. Beslutningen fastlægger retningen; intet er implementeret
endnu.

- Kernen er generisk og ved intet om aktiviteter, materialer eller tilmelding:
  boards, kolonner og arbejdskort. Kanban er en visning over arbejdskortene.
- Et **arbejdskort** (`work_item`) ejes ét sted: titel, beskrivelse, ansvarlig
  bruger eller stilling, deadline og prioritet. Kortet kan **placeres på flere
  boards** gennem én `board_card`-række pr. board, så et fælles "mine
  opgaver"-overblik bliver muligt på tværs af tavler. Status hører til
  placeringen, ikke til kortet: den er kortets kolonne på det pågældende board,
  og `board_column.is_done_column` markerer de kolonner, der tæller som færdige.
- Koblingen til domænet sker udelukkende via `work_item_link`
  (`entity_type`, `entity_id`). Et kort kan pege på 0..N domæneobjekter, og et
  domæneobjekt kan have 0..N kort. PostgreSQL kan ikke håndhæve polymorfe
  fremmednøgler; oprydning af døde links håndteres i kode, eventuelt af et
  Hangfire-oprydningsjob.
- Integrationen er et backend-interface, `IWorkItemSource`, der registreres som
  scoped service ligesom `DataExport` i `Program.cs`. Et nyt domæne bliver
  dermed linkbart uden ændringer i board-koden.
- **Første version er bevidst minimal:** boards, kolonner, frie kort, træk og
  slip, én kildetype (aktivitet) og rollegated adgang. Kommentarer, checklister,
  labels, vedhæftninger og tidslinje udskydes.
- Et kort **seedes én gang** fra sin kilde og redigeres derefter frit. Der er
  ingen to-vejs synkronisering i første version; status på kortet følger ikke
  automatisk domæneobjektet.
- Board-adgang gates i første version af én påkrævet `AppRole` pr. board, som i
  `DataExports`/Admin. Finere adgang (board × `AppGroups`/stilling/team) kan
  tilføjes senere.
- Nye `AppRoles`: `BoardsView` (se tavler) og `BoardsManage` (oprette og
  administrere tavler). Fordelingen følger matricen: `BoardsManage` til
  SysAdmin, Lejrchef, Eventansvarlig og Aktivitetsansvarlig; `BoardsView`
  derudover til Aktivitets-teammedlem, Kasserer og PR-ansvarlig. Den endelige
  rollefordeling kan justeres senere.

Implementeringsnoter (2026-10-14):
- Migration `015_boards.sql` opretter `board`, `board_column`, `work_item`,
  `board_card` og `work_item_link`; entiteter og `IEntityTypeConfiguration`-
  klasser er tilføjet, og tabellerne er registreret i `DataDbContext`.
- Portalens `BoardApiController` (`/api/v1/boards`) og DTO'er er tilføjet:
  boards (liste/opret/ret/slet), kolonner (opret/ret/slet) og kort
  (opret/ret/flyt/fjern). Visning kræver `BoardsView` plus boardets eventuelle
  `required_role`; ændringer kræver `BoardsManage`.
- Et kort oprettes som et `work_item` med én `board_card`-placering. Fjernes
  kortet fra dets sidste board, soft-slettes `work_item` også. Der er endnu
  ingen UI til at placere samme kort på flere boards.
- Kolonneombytning er løst uden schema-ændring: `board_column` har et unikt krav
  på `(board_id, position)`, så ombytningen sker i to faser inden for én
  transaktion — først midlertidige negative positioner, derefter de endelige.
- Endnu ikke implementeret: `IWorkItemSource` og seed af boards.

Implementeringsnoter (2026-10-15, frontend):
- `/board` viser en liste over tavler med kortantal, og `/board/:id` viser selve
  kanban-tavlen. Begge ruter kræver `BoardsView` og henter data via
  `BoardStore`/`BoardService` efter samme mønster som aktivitetsmodulet.
- Der er tilføjet en HQ-flis "Tavler" (ikon `table-columns`) med
  `RequiredRole = BoardsView`.
- Tavlelisten kan oprette et board ud fra titel, beskrivelse og adgang. Nøglen
  udledes automatisk af titlen (unikgjort med et suffiks ved sammenfald), og nye
  tavler får standardkolonnerne Backlog, I gang og Færdig. Brugeren sendes videre
  til den nye tavle.
- Kanban-visningen kan trække og slippe kort mellem kolonner og omordne inden
  for en kolonne. Flytningen sender den beregnede position til API'et, som selv
  omnummererer. Kolonner redigeres i et sidepanel, kort i en modal, og
  tavleindstillinger (titel, beskrivelse, adgang, arkivering) i en modal.
- Træk og slip er implementeret med native HTML5-drag (ingen ekstra
  afhængighed).
- Visuelt justeret 2026-10-16: tavlen har en lys baggrund, og kolonnerne
  følger indholdets højde op til tavlens synlige højde. Derefter scroller
  kortlisten i hver kolonne, mens overskriften og "Tilføj kort" er synlige.
  Kolonnehandlinger er dæmpede og fremhæves ved hover eller tastaturfokus;
  på touchskærme er de fuldt synlige. Færdig-kolonner markeres med en grøn
  prik i overskriften frem for en hel grøn flade.
- Kortene har lettere titler, mere luft og højst to linjers beskrivelse.
  Metadata samles nederst: prioritet som pil med forklarende tekst ved hover,
  ansvarlig som profilbillede (initialer som fallback) med navn ved hover
  eller tastaturfokus og
  deadline i kort dansk datoformat. Overskredne datoer fremhæves kun for
  kort uden for færdig-kolonner; dagsgrænsen følger Europe/Copenhagen.
- Tavle-API’et medsender den ansvarliges eksisterende profilbillede som
  `assigneeProfilePicture`, når filen er klar og ikke slettet. Kortet bruger
  den eksisterende private fil-URL og viser initialer, hvis billedet mangler
  eller ikke kan indlæses. Dette kræver ingen skemaændring.

Implementeringsnoter (2026-10-16, ansvarlig-vælger):
- Kortets ansvarlige bruger sættes i kortpanelet via en debounceret søgning
  (`GET /api/v1/boards/assignees`). Søgningen begrænses til brugere i de
  grupper, der har `BoardsView`, og kræver selv `BoardsManage`. Ansvarlig
  stilling vælges blandt organisationsstillingerne (`GET /api/v1/org`).
- Brugere med kun `BoardsView` kan fortsat åbne kortpanelet, men i læsetilstand
  uden redigeringsfelter. Flytning af kort med træk og slip er fortsat kun
  muligt for `BoardsManage`.
- Kolonner kan ombyttes med træk i håndtaget i kolonnens top. At slippe på en
  kolonne placerer den før målkolonnen, og at slippe i den tomme del af tavlen
  placerer den sidst. Kun `BoardsManage` kan ombytte kolonner.

**Beslutning 2026-10-16 (implementeret):** Kortet åbnes i en modal i stedet
for et sidepanel, og beskrivelsen redigeres med en rigtig teksteditor i stedet
for et almindeligt tekstfelt. `work_item.description` gemmer dermed HTML.

Implementeringsnoter (2026-10-16, modal og rig tekst):
- Kortpanelet hedder nu `BoardCardModal` og er en `BModal` med plads til
  redigeringsfeltet. Indholdet har titel og beskrivelse til venstre og en fast sidekolonne med
  deadline, prioritet, ansvarlig, stilling og koblinger til højre; på smalle
  skærme stables kolonnerne. Kolonner redigeres fortsat i et sidepanel, da det
  er en kort formular; tavleindstillinger er fortsat en modal.
- Visuelt forslag implementeret 2026-10-15: modalen er op til 64rem bred,
  korttitlen redigeres som et stort titelfelt, og egenskaberne ligger i en lys
  sidekolonne på 16rem. Dette erstatter den tidligere 70/30-fordeling.
  Ansvarlig-søgningen åbnes via "Tilføj ansvarlig"/"Skift ansvarlig".
  Slet ligger i kortmenuen øverst, og bunden har Annuller og eksplicit Gem.
  Dette visuelle forslag er justeret 2026-10-16 efter ønsket om et mere
  minimalistisk udtryk inspireret af Favro: egenskaberne vises uden faste
  feltrammer, med markering ved hover og fokus, og sekundære handlinger er
  diskrete tekstknapper. Beskrivelsen har ingen fast synlig ramme; ramme og
  værktøjslinje vises, når editoren har fokus (inklusive tastaturfokus).
  Gem er fortsat eksplicit, og felter og rettigheder er uændrede.
- Beskrivelsen redigeres med Tiptap (`@tiptap/vue-3` + `starter-kit`):
  overskrifter (H1, H2 og H3 som separate knapper), fed, kursiv, punkt- og
  nummereret liste, citat og fjern formatering. Tiptap er begrænset til
  niveauerne h1–h3, så alt indhold i dokumentet kan slås fra igen fra
  værktøjslinjen. Det er frontendens første editor og første `v-html`.
- HTML gennemvises altid via `RichTextViewer`, som kører DOMPurify før
  `v-html`; Tiptaps output er skemabegrænset, så begge lag begrænser XSS.
  Kortvisningen på tavlen viser beskrivelsen som ren tekst (tags fjernes), så
  kortene forbliver enkle og hurtige.
- En beskrivelse uden tekst gemmes som `NULL` frem for tom HTML.
- Tiptap monteres, når modalen åbnes, og afmonteres igen ved lukning, så
  editoren aldrig sidder skjult i et skjult element.

**Beslutning og implementering (DS-35, 2026-11-15):** Kortpanelet bruger
Buefys `BTaginput` til søgning, valg af flere tags og oprettelse direkte i
feltet. Valgte tags vises med `BoardTagBadge`; de kan fjernes med mus og
Backspace. Nye tags er lokale kladder indtil kortet gemmes, starter uden
farve og kan efterfølgende farves i Tags-dialogen. Annullering opretter intet.

- Brugeren godkendte en udvidelse af frontend-only-scope: kortets eksisterende
  oprettelses-/opdateringskald accepterer nu valgfri `newTagNames`. Tags og
  kort gemmes i ét `SaveChangesAsync`, hvis databaseændringer udføres atomisk
  af EF Core. Det erstatter forslaget om separate `create_tag`-kald og
  efterfølgende oprydning; der slettes aldrig tags som kompensation.
- Navne trimmes og valideres til 1–64 tegn; dubletter samles uden hensyn til
  store/små bogstaver. Eksisterende navne genbruges kun på den aktuelle tavle.
  En samtidig oprettelse med samme navn giver konflikt og kan genforsøges
  uden delvist gemte tags eller kortændringer.
- Udeladt/null `newTagNames` er uændret adfærd. Ved opdatering bevarer udeladt/null
  `tagIds` eksisterende tags og tilføjer eventuelle nye navne; `[]` erstatter
  valget med de nye navne alene (eller rydder det, hvis ingen navne er angivet).
  Andre tavleplaceringers tags bevares. MCP-værktøjernes parametre ændres ikke.
- Ingen databaseændring eller migration. Den eksisterende Tags-dialog og
  tagfiltrering bevares.

**Implementeret (DS-27, 2026-11-15):** Tavler har egne tags med entydigt navn
(op til 64 tegn, uden forskel på store/små bogstaver) og valgfri hex-farve.
Medlemmer samt SysAdmin/CampAdmin kan oprette, redigere og slette tags via
Tags på tavlen. En sletning fjerner tagget fra tavlens kort.

- Et kort kan have flere tags pr. tavleplacering. Tags vælges i kortpanelet og
  vises ved siden af DS-nummeret øverst på tavlekortet. Tags følger ikke med
  til andre tavler, når et kort deles;
  hver tavle har sin egen markering og afslører ingen tags fra skjulte tavler.
- Tagfilteret viser kort med det valgte tag. Flytning af kort og kolonner er
  slået fra under filtrering, så skjulte kort ikke giver en forkert rækkefølge.
  Fjernes et valgt tag, nulstilles filteret til alle kort.
- Tags supplerer `Ikke udgivet` og ændrer ikke arbejdsgangen fra I gang via
  Ikke udgivet til Færdig. Release-tags sættes manuelt i denne version.
  Automatisk kobling til GitHub-merges eller deployede tags er fortsat uafklaret
  og kræver en særskilt integration; der udledes ingen release fra en merge.
- API'et tilbyder oprettelse, opdatering og sletning under
  `/api/v1/boards/{id}/tags`. Tavlesvaret og kortsvaret har `tags`, og
  kortoprettelse/-opdatering accepterer `tagIds`. Ved opdatering bevarer udeladt
  eller null `tagIds` tildelingerne for ældre klienter/MCP, mens `[]` rydder dem.
  Server og sammensatte fremmednøgler afviser tags fra andre tavler. Navne og
  farver på eksisterende tildelinger opdateres gennem deres fælles tagdefinition.
- Ændringer udsender tavlens eksisterende SignalR-hændelse. Migration
  `029_board_tags.sql` tilføjer tabeller og constraints uden at ændre eller
  fjerne eksisterende data, så den tidligere release kan fortsætte uden tags.

**Beslutning 2026-10-16 (implementeret):** Færdig-status er **placeringsbestemt**,
ikke global. Det globale `work_item.is_done` er droppet; "færdig" betyder, at
kortets kolonne på den pågældende tavle er markeret som færdig-kolonne
(`board_column.is_done_column`). Dermed kan samme kort være i gang på én tavle og
færdigt på en anden. Beslutningen afløser det tidligere udkast, hvor et globalt
`is_done`-flag gjaldt på tværs af alle placeringer.

Implementeringsnoter (2026-10-16, status pr. placering):
- Migration `020_drop_work_item_is_done.sql` dropper kolonnen. `work_item` har
  ikke længere et færdig-flag, og `WorkItemDto`/`WorkItemUpdateDto` har ikke
  længere `IsDone`.
- Checkboksen "Færdig" i kortpanelet er fjernet. Man markerer et kort færdigt ved
  at trække det til en færdig-kolonne, og kortet vises gennemstreget i alle
  færdig-kolonner. `BoardColumn` giver kolonnens `isDoneColumn` videre til
  `BoardCard`.
- Færdig-kolonne-markeringen i kolonne-panelet (som hidtil kun styrede farven)
  er nu den funktionelle definition af færdig.

**Beslutning 2026-10-16 (implementeret):** Et kort kan placeres på flere tavler
fra kortpanelet, og en enkelt placering kan fjernes igen uden at slette kortet.
Kortet ejes fortsat ét sted, og hver placering har sin egen kolonne og dermed sin
egen status.

Implementeringsnoter (2026-10-16, kort på flere tavler):
- `WorkItemDto` har nu `placements`: en liste pr. kort med tavle, kolonne og om
  kolonnen er en færdig-kolonne. Listen bygges i `LoadPlacementsAsync` med ét
  opslag for alle kort på tavlen og filtreres på tavlens `required_role` i koden,
  så et kort på en tavle, brugeren ikke har adgang til, ikke afsløres.
- To nye endpoints: `GET /api/v1/boards/{id}/columns` giver en let kolonneliste
  til tavle-vælgeren, og
  `POST /api/v1/boards/{id}/cards/{workItemId}/placements` placerer et
  eksisterende kort på tavlen i en valgt kolonne. At fjerne en enkelt placering
  genbruger `DELETE /api/v1/boards/{id}/cards/{workItemId}`, som allerede
  soft-sletter kortet, hvis det var den sidste placering.
- Placeringen kræver `BoardsManage` og adgang til både den tavle, kortet kommer
  fra, og måltavlen. Den unikke nøgle `(work_item_id, board_id)` afvises i koden
  med en pæn fejl, hvis kortet allerede ligger på tavlen.
- Kortpanelets sidekolonne har en "Tavler og status"-sektion: placeringerne listes med
  tavle og kolonne (færdig-kolonner markeret med flueben), og der kan tilføjes en
  tavle med tilhørende kolonne eller fjernes en enkelt placering. "Fjern" skjules,
  når kortet kun ligger på én tavle; dér bruges "Slet kort". Placering kan først
  ske, når kortet er oprettet. Fjernes kortet fra den tavle, man står på, lukkes
  panelet; fjernes det fra en anden tavle, genindlæses den aktuelle tavle, så
  listen opdateres. Læsetilstand viser "Tavler" som en ren liste, og kun når
  kortet ligger på mere end én tavle.

Implementeringsnote (2026-10-16, statusvælger):
- Hver eksisterende tavleplacering har en statusknap, der åbner tavlens
  kolonner som valgmuligheder. Den aktuelle kolonne markeres, og færdige
  statusser vises med en grøn markering. Valget flytter kortet til slutningen
  af målkolonnen via det eksisterende flytte-API og opdaterer den aktuelle
  tavle uden at lukke kortdialogen. Status på øvrige tavler bevares.
- Status og tavleplaceringer gemmes straks, som UI'et oplyser; Annuller
  fortryder kun ikke-gemte ændringer i kortets øvrige felter. Læsetilstand
  har fortsat ingen redigeringshandlinger. Der er ingen skemaændring.

**Beslutning 2026-11-15 (implementeret):** Et lokalt MCP-værktøj i
`tools/ds28-mcp` giver en AI-agent læse- og skriveadgang til tavlerne gennem
det eksisterende HTTP-API. Værktøjet er udviklingssupport og ændrer ikke
produktets adgangsregler; det bruger det personlige adgangstoken beskrevet
nedenfor.

- MCP-serveren kører lokalt over stdio og er registreret i `opencode.json` og
  `.mcp.json` (Claude Code) med værktøjerne `list_boards`, `get_board`,
  `create_card`, `update_card`, `move_card`, `delete_card`, `create_tag`,
  `update_tag`, `delete_tag`, `search_cards`, `add_dependency`,
  `remove_dependency`, `add_pull_request` og `remove_pull_request`.
  Alle kald går til `/api/v1/boards` med samme
  adgangskontrol som brugerens egen konto, så medlemskab, SysAdmin/CampAdmin og
  API-validering gælder uændret.
- MCP-serveren sender et personligt adgangstoken som `Authorization: Bearer`
  (`DS28_MCP_TOKEN`) i stedet for at logge ind med email og adgangskode.
  Tokenet oprettes og tilbagekaldes under profilen og arver brugerens
  rettigheder. Det ligger i `tools/ds28-mcp/.env`, som er ignoreret af git, med
  `tools/ds28-mcp/.env.example` som skabelon.
- Kort oprettes med titel, kolonne (id eller titel), valgfri beskrivelse,
  prioritet og deadline. Ren tekst laves om til HTML-afsnit, fordi
  `work_item.description` gemmer HTML. `move_card` flytter et kort til en
  anden kolonne på samme tavle og kan angive placering; uden placering havner
  kortet bagerst.
- `update_card` ændrer kun de felter, der angives, og beholder kortets øvrige
  værdier, fordi API'ets opdatering kræver hele kortet. En tom værdi rydder
  beskrivelse, frist, farve eller ansvarlige. Ansvarige angives ved fuldt navn
  eller email og skal matche præcis én bruger, ellers afvises kaldet med
  forslag til, hvem der menes. Farve angives som hex fra tavlens palette, som
  `get_board` viser sammen med hvert korts farve.
- `delete_card` fjerner kortet fra tavlen og sletter det blødt, hvis det var
  kortets sidste tavle.
- Tests: `node --test tools/ds28-mcp/*.test.mjs` dækker hjælpefunktionerne og
  hele forløbet mod en stub-server.

**Implementeret (DS-33, 2026-11-15):** Både stdio- og HTTP-MCP understøtter
DS-27's tavleafgrænsede tags. `get_board` viser tagdefinitionernes id, navn og
valgfri farve samt tag-id'er på hvert kort. `create_card` og `update_card`
accepterer `tagIds` fra samme tavle. Dubletter fjernes, ukendte id'er afvises.
Udeladt/null `tagIds` betyder ingen tags ved oprettelse og bevar ved opdatering;
`[]` rydder kun den aktuelle tavleplacerings tags. Der sættes ikke tags på
andre tavler gennem et delt kort.

- `create_tag` opretter en definition med entydigt navn og valgfri farve og
  returnerer dens id. Det tildeler ikke tagget til kort.
- `update_tag` ændrer navn og/eller farve på definitionen og dermed alle dens
  tildelinger på tavlen. Udeladt/null navn eller farve bevares; tom `color`
  fjerner farven. Et tomt navn afvises. `delete_tag` sletter definitionen og
  dens tildelinger, mens kort og tags på andre tavler bevares.
- Remote-værktøjerne bruger fortsat `BoardService` med den autentificerede
  bruger; stdio bruger de eksisterende HTTP-endpoints med personligt token.
  Adgangskontrol og tavleafgrænsning er uændret. Ingen databaseændring og ingen
  automatisk kobling til GitHub-releases eller deploy.
- Node-tests dækker hjælperne og stdio-protokollen mod en stub-API.
  `BoardAccessChecks` kalder remote-værktøjerne med en test-autentificeret
  bruger mod en isoleret PostgreSQL-database og dækker afgrænsning, CRUD,
  bevar/ryd-semantik og delte kort. OAuth/transportens loginflow er ikke en del
  af denne test.

**Beslutning 2026-11-21 (DS-67, implementeret i denne ændring, ikke udgivet):**
Alle agenter skal kunne oprette, hente og flytte kort på samme måde, også
agenter uden MCP-understøttelse. Kort føres på DS28 IT-tavlen i HQ, ikke i
GitHub Issues eller GitHub Projects.

- `tools/ds28-mcp/cli.mjs` er en CLI til agenter uden MCP. Den starter
  stdio-serveren og kalder dens værktøjer, så CLI og MCP altid har samme
  værktøjer, validering og adgangstoken. Argumenter skrives `navn=værdi` og
  omsættes efter værktøjets skema; heltal accepterer `DS-42`. Uden argumenter
  vises alle værktøjer. Fejl giver exitkode 1. Ingen nye afhængigheder.
- Stdio-serveren er registreret for Claude Code i `.mcp.json` ud over
  `opencode.json`.
- Kortflowet er fastlagt i `AGENTS.md`: nye kort oprettes i `Backlog`, flyttes
  til `I gang` ved start, PR'er linkes med `add_pull_request`, kortet flyttes
  til `Ikke udgivet`, når PR'en er godkendt og merget, og til `Færdig`, når et
  udgivet tag indeholder ændringen. Den person, der har udført arbejdet, sættes
  som ansvarlig ved fuldt navn, aldrig agenten.
- Tests: `node --test tools/ds28-mcp/*.test.mjs` dækker argumentparseren og
  CLI'en mod stub-API'et.

**Beslutning 2026-11-16 (implementeret):** Website udstiller et remote
MCP-endpoint på `/mcp`, så cloud-agenter kan bruge tavlerne. Codex Cloud og
ChatGPT web kan ikke nå en MCP-server, der kører lokalt på udviklerens maskine,
og den eksisterende stdio-server i `tools/ds28-mcp` kan derfor kun bruges
lokalt.

- Endpointet ligger i Website med `ModelContextProtocol.AspNetCore` og kortes
  tolv værktøjer (`list_boards`, `get_board`, `create_card`, `update_card`,
  `move_card`, `delete_card`, `create_tag`, `update_tag`, `delete_tag`,
  `search_cards`, `add_dependency`, `remove_dependency`) i
  `src/DS.Backend/Mcp/BoardTools.cs`. Der
  indføres ingen ekstra tjeneste eller container.
- Værktøjerne kalder `BoardService` direkte med samme `BoardActor` som
  controllerne, så medlemskab, SysAdmin/CampAdmin, validering og
  tavlesynlighed er uændret.
- `/mcp` bruger autentificeringsschemet `Mcp`. Det accepterer både et
  OpenIddict-access-token fra `/connect/token` og et personligt adgangstoken,
  og svarer `401` med `WWW-Authenticate` i stedet for at omdirigere til
  login-siden. Cookielogin bruges ikke på endpointet.
- `/.well-known/oauth-protected-resource` beskriver ressourcen, så en
  MCP-klient kan finde autorisationsserveren. Den bygges ud fra forespørgslens
  origin og indeholder ingen konfiguration.
- Værktøjsnavne, parametre og den danske tekst svarer til stdio-serveren, så en
  agent kan skifte mellem de to uden at lære nye værktøjer.
- `update_card` ændrer kun de angivne felter og læser kortet først, fordi
  `WorkItemUpdateDto` kræver hele kortet; en tom værdi rydder feltet. Ansvarige
  matches på fuldt navn eller email og skal ramme præcis én bruger.
- Endpointet bruger MCP'ens standard-sessioner. Website kører som én instans,
  så der er ikke behov for et delt session-lager, men en genstart afbryder
  igangværende MCP-sessioner.
- Brug i Codex er `[mcp_servers.ds28-boards]` med `url` og
  `bearer_token_env_var`. Codex kan også logge ind via OAuth.
- Den lokale stdio-server består til OpenCode og lokal udvikling og går fortsat
  over HTTP-API'et. Den er ikke nødvendig for cloud-agenter.
- Verifikation: `dotnet build` og de rene hjælpefunktioner i
  `tests/BoardAccessChecks` dækker kolonneopslag, navnematch, korttekst og
  mapping fra `sub` til bruger-id. Selve HTTP-forløbet er ikke kørt mod en
  kørende instans i denne ændring, så første kald efter deploy skal bekræftes
  med `initialize` og `tools/list`.

**Beslutning 2026-11-17 (DS-43, implementeret):** Remote MCP kræver den
særskilte Identity-rolle `McpUser`, som systemadministrator og lejrchef kan
tildele eksplicit i brugeradministrationen. Rollen seedes ved opstart, men
ingen eksisterende brugere får den automatisk — heller ikke administratorer.
Dette erstatter den tidligere adgang alene på grundlag af et gyldigt token.

- Både OAuth og personlige tokens kontrolleres mod den aktuelle direkte
  rolletildeling i databasen ved hver MCP-forespørgsel og hvert værktøjskald.
  Fjernes rollen, afvises også eksisterende tokens og sessioners efterfølgende
  værktøjskald. Låste og slettede brugere afvises.
- Rollen giver ingen ekstra tavlerettigheder og kan ikke tildeles til stillinger
  eller arves gennem organisationsdiagrammet. Den almindelige HTTP-API og den
  lokale stdio-klient, som bruger API'et, er ikke omfattet af remote MCP-porten.
- Profilen viser forbindelsesknapper til Claude og ChatGPT samt kopi af
  MCP-adressen for brugere med rollen. Knapperne åbner en kort vejledning og
  klientens opsætning; installation og login kræver brugerens godkendelse.
  Der loves ikke automatisk installation gennem udokumenterede deep links.
  Eksisterende OAuth-klientregistrering genbruges; DCR/CIMD tilføjes ikke.
- Ingen skemaændring eller migration; rollen bruger eksisterende Identity-tabeller.

**Beslutning 2026-11-16 (implementeret):** MCP-endpointet accepterer
OpenIddict-access-tokens, så ChatGPT-connectoren og Codex kan logge ind i
stedet for at dele et personligt adgangstoken. OpenIddicts eksisterende
autorisationsserver genbruges; der indføres ingen ny identitetsudbyder.

- Årsagen var `invalid_target` (`ID2190`): ChatGPT sender
  `resource=https://hq.distriktssommerlejr.dk/mcp`, og OpenIddict afviser en
  `resource`, der ikke er registreret i serverens options. `resource`
  valideres derfor ikke længere (`DisableResourceValidation`), og tokens
  bindes ikke til en bestemt ressource. Det er acceptabelt, fordi der kun
  findes denne ene API og kun operatør-oprettede klienter; ved en anden
  ressource skal ressourcen registreres og `rsrc:`-rettigheden sættes pr.
  klient i stedet.
- OpenIddict tjekker desuden en `rsrc:`-rettighed pr. klient, og den fejler med
  `invalid_request` (`ID2192`), fordi klienterne oprettes i
  OpenIddict-klienternes administration uden ressourcerettigheder.
  `IgnoreResourcePermissions` slår derfor også det tjek fra, så en ny klient
  virker uden manuelle rettigheder. Beslutningen er den samme afvejning som
  ovenfor og skal ændres sammen med den, hvis der kommer flere ressourcer.
- Access-tokens kan være krypterede (JWE), så schemet forsøger først
  OpenIddict-validering og falder derefter tilbage til opslag af et personligt
  adgangstoken. Det gør valget uafhængigt af tokenformatet.
- Access-tokenets `sub` mappes til bruger-id, så `BoardActor` og
  rolleopfølgningen virker som ved cookie- og tokenlogin.
- Klienter oprettes fortsat manuelt under OpenIddict-klienter med
  connectorens redirect-URI. Dynamisk klientregistrering (DCR) og CIMD er ikke
  implementeret; ChatGPT-connectoren skal derfor have en præregistreret
  klient, og dens redirect-URI skal opdateres, hvis connectoren genskabes.
- Verifikation: `dotnet build`, de rene checks i `tests/BoardAccessChecks` og
  et `initialize`/`tools/list`-kald mod produktion med personligt token.
  OAuth-forløbet er ikke kørt igennem lokalt, fordi der hverken er database
  eller Docker på udviklingsmaskinen.

**Beslutning 2026-11-17 (DS-36, implementeret i denne ændring, ikke udgivet):**
OAuth understøtter refresh tokens til de operatør-oprettede authorization-code-
klienter. Dette udvider den tidligere code-only-afgrænsning; DCR/CIMD indføres
fortsat ikke. Access-tokens lever én time; refresh-tokens lever 14 dage med
OpenIddicts standardrotation og glidende udløb (14 dage fra seneste fornyelse).
Standardvinduet på 30 sekunder til samtidige refresh-kald bevares. Udløbne,
tilbagekaldte og genbrugte tokens uden for vinduet valideres af OpenIddict.

- Serverens discovery og MCP-metadata annoncerer `offline_access`. Klienten
  skal anmode om dette scope ved login for at modtage et refresh-token.
- Nye og redigerede klienter får refresh-grant-rettigheden. Eksisterende
  authorization-code-klienter opgraderes gennem en idempotent datamigration,
  som bevarer øvrige rettigheder og konfiguration. Databaseskemaet er uændret.
- Token-endpointet bevarer de godkendte scopes, også når tokenkaldet udelader
  `scope`. Refresh kan indsnævre scopes, men aldrig udvide dem.
- Hver udveksling genlæser brugeren og klientens adgangskrav, afviser slettede
  eller låste brugere og genopbygger aktuelle brugerclaims. Refresh kontrollerer
  også Identity security stamp, så eksempelvis en nulstilling af adgangskoden
  afviser den gamle forbindelse. Private OpenIddict-claims bevares internt;
  de og security stamp eksponeres ikke i access-/ID-tokens.
- Eksisterende forbindelser skal autoriseres igen med `offline_access` efter
  deploy. Der ændres ikke på allerede udstedte tokens eller personlige tokens.
  ChatGPTs scope-konfiguration skal kontrolleres som del af produktionschecket.
- MCP-sessioners levetid og genoprettelse efter Website-genstart er uændret.
  Refresh løser tokenfornyelsen; det er ikke en løsning på mistet sessionstilstand.
- Klientservicens fælles skrivehjælper bevarer nu det typede svar. Tidligere
  tabte den succesdata, herunder det nye client secret, selv om klienten blev
  gemt. Dette blev afdækket af klienttestene under DS-36.
- Verifikation: `tests/AccountClientChecks/run.sh` tester klientadministration,
  migration af eksisterende klienter og idempotens samt HTTP authorization-code-
  flow med PKCE og gentagen refresh efter access-tokenets udløb. Desuden testes
  scopes, rotation/genbrug, tilbagekaldelse, ændret security stamp, låsning og
  mistet klientadgang. Testene bruger isoleret PostgreSQL og midlertidige
  signerings-/krypteringsnøgler. Backend-build og `git diff --check` er kørt.
  ChatGPT-integration, produktionsdeploy og reconnect efter Website-genstart
  er ikke testet i denne ændring.
- Datamigrationen kræver et databasedump før deploy efter den almindelige
  releaseprocedure. Den er kompatibel med den foregående release, som blot
  ignorerer den ekstra grant-rettighed.

**Beslutning 2026-11-15 (implementeret):** Personlige adgangstokens afløser
email og adgangskode i MCP-værktøjet. En bruger kan oprette, se én gang og
tilbagekalde tokens under profilen; `DS28_MCP_EMAIL`/`DS28_MCP_PASSWORD` er
droppet.

- `user_access_token` gemmer kun en SHA-256-hash af tokenet sammen med navn,
  oprettelsesdato, valgfrit udløb (standard 90 dage) og eventuel
  tilbagekaldelsesdato. Den rå token vises kun ved oprettelsen.
- `AccessTokenApiController` udstiller `GET`, `POST` og `DELETE
  /api/v1/access-tokens` for den indloggede bruger, og profilens side bruger
  dem.
- Et `AccessToken`-authentikationsscheme læser `Authorization: Bearer` og
  sætter brugerens id; `IdentityOrAccessToken` vælger mellem cookie og token.
  Den eksisterende `ClaimsTransformer` tilføjer grupper og stillinger, så
  tavlemedlemskab, SysAdmin/CampAdmin og validering er uændret. Låste brugere
  afvises.
- Tokenet arver brugerens rettigheder. Skal en agent have svagere adgang,
  oprettes en særskilt bruger med eget tavlemedlemskab.
- Den globale `AutoValidateAntiforgeryTokenAttribute` er erstattet af
  `CookieAntiforgeryFilter`: cookie-kald på sikre metoder og Bearer-kald
  springes over, `[IgnoreAntiforgeryToken]` respekteres, og cookie-baserede
  ændringskald kræver fortsat et gyldigt antiforgery-token. Bearer-kald er ikke
  CSRF-udsatte, fordi browseren ikke sender headeren automatisk.
- Tests: `bash tests/AccessTokenChecks/run.sh` dækker oprettelse, hash-lagring,
  udløb, tilbagekaldelse, låste brugere, Bearer-handleren og
  antiforgery-reglerne.

**Beslutning 2026-11-22 (implementeret):** Adgangstokens vises kun for brugere
med rollen `McpUser`. Profilen skjuler token-kortet og henter ikke tokens for
andre brugere, og `GET` og `POST /api/v1/access-tokens` kræver rollen.
Dette erstatter, at alle indloggede brugere kunne oprette tokens.

- `DELETE /api/v1/access-tokens/{id}` kræver ikke rollen, så tilbagekaldelse
  aldrig blokeres.
- Allerede udstedte tokens godkendes fortsat mod den almindelige HTTP-API,
  også hvis brugeren ikke har `McpUser`. Om Bearer-godkendelsen også skal
  kræve rollen, er ubesluttet.

**DS-91 (implementeret, rollevalg afventer bekræftelse):** Under
Brugerstyring (`/admin/user/:id`) kan en admin se en brugers aktive personlige
adgangstokens og tilbagekalde ét eller alle på én gang, med bekræftelse.

- Endpoints i `UserApiController`: `GET /api/v1/user/{id}/access-tokens`,
  `DELETE /api/v1/user/{id}/access-tokens/{tokenId}` og `DELETE
  /api/v1/user/{id}/access-tokens` (tilbagekald alle, svarer med antallet).
  Ukendt bruger giver 404, og et token kan kun tilbagekaldes under den bruger,
  det tilhører.
- Rollevalg: endpointsene kræver `UsersLock` (i dag kun `SysAdmin`) ud over
  controllerens `UsersView`. Det er den strammeste eksisterende rolle i
  brugerstyringen og svarer til at låse en bruger ude. Der er ikke oprettet en
  ny rolle. Sektionen vises kun med `UsersLock`. Valget afventer bekræftelse;
  alternativet er `UsersView`, som også giver `CampAdmin` adgang.
- Kun aktive tokens vises (ikke tilbagekaldt og ikke udløbet), med navn,
  oprettelsesdato og udløb. Tokenværdi og hash returneres aldrig. Tilbagekald
  alle rammer alle ikke-tilbagekaldte tokens, også udløbne.
- "Sidst brugt" vises ikke, fordi `user_access_token` ikke registrerer brug.
  Det kræver en ny kolonne og er ikke en del af DS-91.
- Ikke med (mulige opfølgninger): OAuth-adgang via OpenIddict (autorisationer
  og refresh-tokens), historik over tilbagekaldte og udløbne tokens, og
  automatisk tilbagekaldelse, når en bruger mister `McpUser`.

Ubesluttede punkter:
- Hvem må rydde døde links, og skal et kort blokere sletning af et
  domæneobjekt eller blot miste linket?
- Skal et board senere kunne generere kort automatisk fra en forespørgsel
  ("smart board"), fx aktiviteter uden budget, via domænehændelser/Hangfire?

## Sikkerhed og auth-hærdning

**Rettet 2026-11-20 (XSS i kortforhåndsvisning):** `htmlToPlainText` i
`src/DS.Frontend/src/lib/util.ts` lavede tavlekortenes forhåndsvisning ved at
sætte beskrivelsen som `innerHTML` på et element i dokumentet. Event-handlere
som `onerror` på billeder kørte derfor, og da backenden ikke renser
beskrivelser, kunne et tavlemedlem køre JavaScript hos alle, der åbnede
tavlen, ved at gemme en beskrivelse direkte via API'et. Funktionen bruger nu
`DOMParser`, som ikke kører handlere og giver samme tekst. Den eneste anden
indsættelse af HTML (`RichTextViewer`) renser allerede med DOMPurify.
Ubesluttet: om beskrivelser også skal renses i backenden, så andre klienter end
HQ ikke får rå HTML.

**Beslutning 2026-10-20 (delvist erstattet 2026-11-15):** MVC brugte global
`AutoValidateAntiforgeryTokenAttribute`, så POST, PUT, PATCH og DELETE som
udgangspunkt krævede et gyldigt request-token sammen med antiforgery-cookien.
Den globale attribut er senere erstattet af `CookieAntiforgeryFilter`, som
bevarer kravet for cookie-kald og undtager Bearer-kald; se beslutningen om
adgangstokens.
GET, HEAD, OPTIONS og TRACE valideres ikke af dette filter. Dette udvider den
tidligere beskyttelse, som kun fandtes på OpenIddict-klientadministrationen.

- `GET /api/v1/antiforgery` udsteder et ikke-cachebart token til både anonyme
  og indloggede brugere. Den fælles axios-klient sender det som
  `RequestVerificationToken` på ændringskald til samme origin. Samtidige kald
  deler tokenhentningen; cachen ryddes efter vellykkede ændringskald og ved
  HTTP 400, så login og andre identitetsskift ikke efterlader et gammelt token.
  Afviste ændringskald genafsendes ikke automatisk.
- Antiforgery-cookien er HttpOnly og SameSite=Strict med Secure i produktion;
  lokal udvikling tillader HTTP som den eksisterende login-cookie.
- Det eksisterende token-endpoint under OpenIddict-klientadministrationen
  bevares. Brugeradministrationen har desuden
  `GET /api/v1/user/reset-password-link/antiforgery` med samme adgangskrav
  som oprettelse af nulstillingslinks. Begge udsteder kompatible tokens.
- Efter eksplicit valg er `AuthApiController` (inklusive logout),
  `PasswordResetApiController`, `InvitationApiController`,
  `GroupInvitationApiController` og `GroupPreSignupApiController` undtaget.
  De offentlige flows bevarer deres eksisterende adgangskode-, reset-token-
  og invitationskontroller. Undtagelserne er en accepteret begrænsning, ikke
  en teknisk nødvendighed: også anonyme flows kan bruge antiforgery-tokens,
  login-CSRF er muligt, og gruppeinvitationer kan bruge en eksisterende session.
  Der indføres ikke Origin/Referer-validering som alternativ i denne opgave.
- `/connect/authorize`, `/connect/token` og `/connect/userinfo` er eksplicit
  undtaget fra MVC-antiforgery af hensyn til OIDC-protokollen. OpenIddict
  validerer protokolrequests; tokenudveksling bruger authorization code og
  klientens eksisterende credentials/PKCE-politik, mens userinfo bruger
  OpenIddict-autentifikation frem for Identity-cookien. Passthrough-actions
  kører stadig MVC-filtre; undtagelsen er derfor virksom og bevidst.
- Eksisterende GET-baseret logout og Google OAuth-redirect/callback ændres ikke.
  Google-callback beholder sin state-kontrol; Google-disconnect og
  profilbilledændringer er omfattet af den globale validering.

HTTP-integrationstesten bruger Website's faktiske MVC-registrering og
kontrollerer manglende/ugyldige/gyldige tokens, POST/PUT/DELETE, cacheforbud og
token uden cookie. Ingen databaseskemaændring. CodeQL skal genkøres efter
ændringerne; eksplicit undtagne endpoints kan fortsat give fund, der skal
vurderes særskilt frem for at blive erklæret rettet.

**Beslutning 2026-10-14:** Fire konkrete fund fra en hurtig gennemgang blev
gennemgået. Tre blev rettet, og PKCE blev vurderet og forkastet.

- **Forwarded headers:** `UseForwardedHeaders` kørte efter `UseHttpsRedirection`
  og `UseRouting`, og `KnownProxies`/`KnownIPNetworks` indeholdt kun loopback, så
  Traefiks `X-Forwarded-Proto`/`-For` blev ignoreret. `Request.IsHttps`,
  `Request.Scheme` og `Request.Host` var derfor upålidelige bag proxyen, hvilket
  bl.a. kunne give redirect-loop i `UseHttpsRedirection` og forkerte links i
  admin-genererede nulstillingslinks. Middlewaren er flyttet til toppen af
  pipelinen, og proxyen stoles på ved at tømme `KnownProxies` og
  `KnownIPNetworks`. Det er forsvarligt, fordi app-porten kun eksponeres via
  Traefik på det interne Docker-netværk; eksponeres porten direkte, kan klienter
  spoofe headerne.
- **Login-brute force:** `PasswordSignInAsync` blev kaldt med
  `lockoutOnFailure: false`, så fejlede forsøg aldrig talte, og `IsLockedOut`
  kunne ikke udløses. Slået til, så Identitys standard (5 forsøg / 5 min.)
  gælder. Derudover er der tilføjet rate limiting: en fast 20/min-pr.-IP-politik
  ("auth") på `AuthApiController`, som dækker login, registrering,
  glemt-adgangskode og 2FA. `LockoutEnabled` sættes til `true` af
  `UserManager.CreateAsync` for nye brugere.
- **Impersonation:** `ClaimsTransformer` anvendte impersonation-cookien, så snart
  den kunne dekrypteres, uden at kontrollere den ægte identitet. Cookien
  anvendes nu kun, hvis brugerens reelle grupper indeholder `SysAdmin`.
- **PKCE:** `RequireProofKeyForCodeExchange()` var udkommenteret. Et forsøg på at
  slå kravet til globalt blev forkastet igen, fordi den eneste registrerede
  OIDC-klient (WordPress) ikke sender `code_challenge`, og et globalt krav derfor
  ville bryde WordPress-login. Kravet bør i stedet sættes pr. klient via
  `OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange` på de
  applikationer, der understøtter PKCE, når der kommer flere klienter. Indtil da
  accepteres afvejningen, fordi WordPress-klienten er konfidentiel (hemmelighed),
  så et opsnappet authorization code ikke kan indløses uden den.

Ubesluttede punkter:
- Ingen klient understøtter PKCE i dag. Kravet bør tilføjes på klient-niveau, når
  en ny klient, der kan det, tages i brug.
- Rate-limit-politikken er pr.-IP; brugere bag samme NAT deler kvote. Grænsen
  (20/min) er et forsigtigt udgangspunkt og kan justeres.
- Adgangskodepolitikken (`RequiredLength = 4`, ingen kompleksitetskrav) er
  bevidst uændret her, da den kan være et bevidst valg for deltagere.

## Teknologi og planlagt drift

- ASP.NET Core og Vue/Vite.
- ASP.NET Core Identity og OpenIddict til identitet og login.
- Nuxt UI 4 og Tailwind CSS 4 til frontend-komponenter (siden 2026-11-21,
  tidligere Buefy), se [Frontend med Nuxt UI](#frontend-med-nuxt-ui).
- Hosting: Smallhosting.
- Services: Grafana og Prometheus.
- CMS-valget var eksempelvis WordPress.

Dette afsnit dokumenterer planen og bekræfter ikke den aktuelle hosting eller
driftsopsætning.

**Beslutning 2026-11-15 (implementeret):** Udviklingen bruger Graphify som
lokalt kodegraf-værktøj. Grafen bygges med `graphify extract . --code-only`, som
kun parser kode lokalt med tree-sitter og derfor hverken kræver API-nøgle eller
sender kode væk fra maskinen. Dokumenter, PDF'er og billeder holdes uden for,
indtil der er taget stilling til en modelbackend. `graphify-out/` er genereret
output og ignoreres af git; grafen skal derfor bygges lokalt og opdateres med
`graphify update .` efter kodeændringer. Agenter får adgang gennem den
installerede OpenCode-skill og den globale plugin-hook. Opsætningen er
beskrevet i `README.md`.

### Delt resultattype fra services til controllers

**Beslutning 2026-10-20:** Services returnerer én fælles resultattype i
`DS.Application/Services/ServiceResult.cs` i stedet for én `*Result`/`*Outcome`
familie pr. domæne:

- `ServiceResult(ServiceOutcome Outcome, object Error = null)` til handlinger
  uden returværdi.
- `ServiceResult<T>(ServiceOutcome Outcome, T Value = default, object Error = null)`
  hvor der findes en typet payload. `Error` er `object`, fordi Identity-baserede
  services sender fejlsamlinger (`IdentityError`) i fejlbodyen.

`ServiceResult<T>` har en implicit konvertering fra den ikke-generiske
`ServiceResult`, så fejlretur i metoder med typet returværdi kan skrives kort
(`return new ServiceResult(ServiceOutcome.NotFound)`). De tidligere
`BoardResult`, `MeResult`, `OrgResult`, `UserResult`, `GroupResult` og
`OpenIddictClientResult` samt deres paralleller er fjernet.

Controllers konverterer resultatet med to udvidelsesmetoder i
`DS.Backend/Extensions/ControllerResultExtensions.cs`, begge med parameteren
`noContent`. De kaldes på controlleren, fx `this.ToActionResult(result)` og
`this.ToActionResult(result, noContent: true)`, og erstatter de tidligere
duplikerede private `Respond`-switches. Den fælles mapping er: `Success` → `Ok`
(eller `NoContent`, når `noContent` er sat), `Invalid` → `BadRequest(Error)`,
`NotFound` → `NotFound(Error)`, `Forbidden` → `Forbid`, `Conflict` →
`Conflict(Error)` og ethvert andet udfald → `500`. Det adskiller sig fra de
gamle controllere, hvor `default` implicit dækkede `Forbidden`/`Conflict`;
begge er nu eksplicitte, og et ukendt udfald giver en fejl i stedet for stilhed.

OpenIddict-hemmeligheden returneres nu som `OpenIddictClientSecretDto` i stedet
for et anonymt `{ clientSecret }`-objekt, så testene kan tjekke typen direkte.
Anonyme payloads i gruppe-services bevares som `T = object` for at holde
JSON-formen uændret.

### Fælles frontend-API-klient

**Beslutning 2026-10-20:** `DS.Backend/js/src/lib/api.ts` samler den gentagne
axios-brug i services: én delt `apiClient` (samme oprindelse,
`withCredentials` og JSON-headere) samt hjælperne `apiGet`/`apiPost`/`apiPut`/
`apiDelete`, `safeApiCall`, `safeApiResult` og `getErrorMessage`.

`safeApiCall` fanger fejl og returnerer en kalderangivet fallback, mens
`getErrorMessage` udtrækker fejlbeskeden fra et axios-svar. Fejlopførslen er
bevidst uændret: hver service beholder sin eksisterende fallback (`[]`, `null`,
`false` osv.), så viewene ikke påvirkes, og metoder der hidtil kastede videre
(fx `BoardService.getMembers`, `FileService`) gør det fortsat.

`BoardService` blev først migreret som pilot; alle øvrige services i
`DS.Backend/js/src/Services` er nu migreret til den fælles klient, så ingen
service importerer `axios` direkte længere. Konverteringshjælperne
`safeApiResult` (fallback `null`) og `safeApiOk` (fallback `false`) dækker de
tilfælde, hvor den oprindelige kode tjekkede `response.status`. `FileService`
bevarer `multipart/form-data`-upload ved at fjerne den delte JSON-header for den
pågældende request.

**Opfølgning 2026-10-20 (samme beslutning):** De views og komponenter, der
hidtil kaldte axios direkte, er flyttet over på services: `Admin.vue`,
`Exports.vue`, `GroupMembers.vue`, `GroupInvitation.vue`, `GroupPreSignupEdit.vue`,
`GroupPreSignup.vue`, `OpenIddictClients.vue` samt `ActivityDetail.vue` og
`ActivityReviewPanel.vue`. Det gav fire nye services – `AdminService`,
`ExportService`, `GroupMembersService` og `GroupInvitationService` – og
`getErrorMessage`/`getErrorStatus` erstatter de lokale fejlbesked-hjælpere
(`errorMessage`, `fail`, `showError`) og de gentagne `axios.isAxiosError`-tjek.

`getErrorStatus` er tilføjet, fordi et par steder skelner mellem en
409-konflikt og andre fejl. De resterende `"/api/v1/..."`-strenge uden for
`Services`/`lib` er bevidst bevaret: de er rene browser-navigationer
(`<img src>` til 2FA-QR og `window.location.href` til Google-connect) og går
ikke gennem axios. Ingen fil uden for `src/lib/api.ts` importerer `axios` nu.

### Fælles gruppeopslag og forhåndstilmeldingslås

**Beslutning 2026-10-20 (implementeret):** De gentagne "hent bruger med gruppe"-
opslag og det rå forhåndstilmeldingslås er samlet i
`DS.Application/Services/Groups/GroupLookup.cs`:

- `GetUserWithGroupAsync(userId)` er en udvidelsesmetode på `UserManager<User>`,
  der laver `Include(u => u.Group)` og `SingleOrDefaultAsync(u => u.Id == userId)`.
  Den erstatter den private `GetCurrentUserAsync` i `GroupMembersService` og de
  fire identiske opslag i `GroupService` (`DeleteScout`, `DeletePatrol`,
  `AddScoutToPatrol`, `RemoveScoutFromPatrol`). De opslag, der har brug for
  ekstra includes (`ThenInclude(g => g.PreSignup/Scouts/Patrols)`), er bevaret
  inline, fordi de er reelt forskellige forespørgsler.
- `IsPreSignupOpenAsync(lockSettings)` tager `FOR SHARE`-låsen på
  indstillingsrækken (når `lockSettings` er sat) og læser
  `IsPreSignupOpen`. Den erstatter den duplikerede sekvens i
  `GroupService.UpdatePreSignup`, `GroupPreSignupService.Lookup` og
  `GroupPreSignupService.Create`.
- `LockGroupForUpdateAsync(groupId)` tager `FOR UPDATE`-låsen på gruppen og
  erstatter de tilsvarende rå kald i `GroupMembersService.RemoveAsync` og
  `GroupPreSignupService.Create`.
- `RegistrationSettingsId` er en delt konstant for den ene indstillingsrække
  med id 1, også brugt af `RegistrationSettingsService`.

**Rettelse af præeksisterende fejl (2026-10-20):** Den rå låse-SQL brugte
PascalCase-navne (`"RegistrationSettings"`, `"Groups"`), men tabellerne ligger i
`ds28`-schemaet med snake_case-navne (`ds28.registration_settings`,
`ds28.scout_group`). Da forbindelsens `search_path` er `"$user", public`, fejlede
begge forespørgsler med `42P01 relation ... does not exist`. Det betød, at
`GroupPreSignupService.Create` og `GroupService.UpdatePreSignup` fejlede hver
gang, de blev kaldt, og at `GroupMembersService.RemoveAsync` fejlede ved selve
låsetrinet. Den fejlende SQL er flyttet uændret gennem den tidligere
controller-til-service-opdeling, så den stammer ikke fra ændringerne her.
`GroupLookup` bruger nu de faktiske skemanavne, og fejlen er bekræftet rettet
end-to-end mod en migreret database. Der er ikke tilføjet en permanent
regressionstest for flowet, da den midlertidige kontrol blev fjernet igen efter
aftale.

### Frontend-build og typekontrol

**Beslutning 2026-11-17 (DS-19, implementeret):** `npm run build` i
`src/DS.Frontend` kører `npm run typecheck && vite build`. Typekontrollen
(`vue-tsc -b`) omfatter både Vue/TypeScript-koden og Vite-konfigurationen og
stopper buildet ved fejl. Det gælder også containerbygget, der bruger samme
npm-script. Dette erstatter den midlertidige beslutning fra 2026-10-02 om at
udelade typekontrol på grund af `ERR_PACKAGE_PATH_NOT_EXPORTED`.

TypeScript er fastholdt på 5.9-patchserien (`~5.9.3`), som virker med den
nuværende `vue-tsc` 3.3.14 og `@vue/tsconfig`. TypeScript 7.0.2 understøttes
ikke af denne checker. TypeScript 7.1 beta og `@vue/content-mapper`
4.0.0-alpha.0 blev afprøvet efter
[artiklen om content mappers](https://alexop.dev/posts/goodbye-vue-tsc-typescript-7-content-mappers/),
men indføres ikke: almindelig npm-installation gav peer-konflikter, og
checkeren gav 603 fejl i den eksisterende frontend, herunder Buefy-attributter
og manglende globale komponenttyper. En senere migration skal vurderes
selvstændigt, når værktøjerne er mere modne; typekontrollen må ikke fjernes
fra buildet som genvej.

De 12 fejl, som den kompatible checker fandt, er rettet uden at svække
kontrollen: organisationsindehavere bruger DTO'ens `id`, `firstName` og
`lastName`, kolonneombytning bruger numeriske id'er, tomme formularværdier
normaliseres til `null`, og tokenfeltets fokus håndteres med et kontrolleret
HTML-input. En ubrugt Vite-import og TypeScript 6-specifikke
`ignoreDeprecations`-indstillinger er fjernet. Serverens adgangsregler og
databaseskema er uændrede.

### Flytning af modelkonfiguration og udfasning af C#-migrations

**Beslutning 2026-09-30:** C#-migrations skal udfases. Som første trin samles
EF Core-modelkonfiguration i `DS/Data/Configurations`, én
`IEntityTypeConfiguration<T>` pr. domænemodel. `DataDbContext` ligger nu i
`DS/Data` og indlæser konfigurationerne efter Identity-konfigurationen.
Nøgler og regler for genererede id'er er flyttet fra modelattributter til
konfigurationsklasserne. De nye klasser følger de påbegyndte konfigurationers
mønster med eksplicitte snake_case-tabelnavne og identity-always for genererede
id'er; gruppe- og indstillings-id'er genereres fortsat ikke.

**Afklaring 2026-09-30:** `DS/Models/db.sql` er planen for de nye tabeller.
Konfigurationerne for eksisterende modeller følger nu denne plan, også for
Identity-tabellerne: kolonnenavne, datatyper, nullability, standardværdier,
unikhed, check constraints og sletteregler. Det erstatter de tidligere
mappings, hvor SQL-planen ændrer dem. Eksempelvis gemmes køn som `MALE` og
`FEMALE`, fødselsdag som `date`, materialepris som `numeric(10, 2)` via en
konvertering fra modellens `double`, og `OrderedToDate` som `use_date`.
Nye lejrindstillinger starter med begge tilmeldinger lukket, som angivet i
SQL-planen; det erstatter den tidligere seed med åben forhåndstilmelding.
Eksisterende databaser er ikke ændret af denne opgave.

Audit- og soft-delete-felterne følger SQL-planens nullable `deleted_at` og
`NOW()`-standarder for oprettelse og opdatering. Automatisk ændring af
`updated_at` ved opdateringer og soft-delete-filtrering var ikke implementeret
på dette tidspunkt; det blev efterfølgende indført, se beslutningen
2026-10-20 nedenfor. Outboxens dengang eksisterende indeks på næste forsøg og id
blev bevaret ud over de indekser, SQL-planen angiver; tabellen er senere fjernet
(beslutning 2026-10-06). EF genererer fortsat egne indeks- og
constraint-navne samt konventionsbaserede fremmednøgleindekser.

**Beslutning 2026-10-20 (implementeret):** Audit og soft delete håndhæves nu
centralt i `DataDbContext` i Infrastructure, så felterne ikke længere sættes
manuelt i hver service. En `ChangeTracker.StateChanged`-handler omdanner
`Remove` af enhver `ISoftDeleteable`-entitet til en soft delete ved at sætte
`deleted_at` og ændre tilstanden til `Modified`, og et globalt query-filter
(`deleted_at IS NULL`) på alle `ISoftDeleteable`-entiteter skjuler slettede
rækker i læsninger. En `SaveChanges`-interceptor
(`AuditSaveChangesInterceptor`) sætter `created_at` og `updated_at` ved insert
og `updated_at` ved opdatering. Entiteter uden `ISoftDeleteable` slettes fortsat
hårdt. Grupper, spejdere, aktiviteter, aktivitetshold og org-stillinger bliver
dermed soft-slettet, hvor de tidligere blev slettet hårdt. Eksisterende
soft-delete-kald i `BoardService` og `FileService` bevares.

Slettede rækker kan fortsat hentes med `IgnoreQueryFilters()`, hvilket bruges i
`BoardService.GenerateKeyAsync` for at undgå at genbruge et `board.key`, der
stadig optager det unikke indeks, og i `MaterialService.GetMaterials` bag
parameteren `includeDeleted`. Fordi et soft-slettet `board` beholder sine
`board_color`-, `board_column`- og `board_card`-rækker, kaskaderer en board-sletning
ikke længere til dem. En ny migration `025_org_pa_soft_delete.sql` gør det unikke
indeks `uq_org_position_pa_for_position_id` partielt (`WHERE deleted_at IS NULL`),
så en ny personlig assistent kan oprettes efter en soft-slettet; `OrgService`
soft-sletter desuden stillingens assistent sammen med ejerstillingen for at
bevare den tidligere `ON DELETE CASCADE`-adfærd. Deltager-eksporten udelader
spejdere i en soft-slettet gruppe.

**Beslutning 2026-09-30:** DbUp håndterer nu SQL-migrations fra
`DS/Migrations`, som erstatter den samlede `DS/Models/db.sql`-fil.
Scripts indlejres i DS-assemblyen og indlæses derfra, så opstart og publicering
ikke afhænger af arbejdsmappe eller løse SQL-filer. Grupper oprettes før
brugere, fordi brugertabellen refererer til `scout_group`.
Timeslot-tabellen hedder `activity_timeslots`, også i indeks og fremmednøgler.
Alle ventende scripts køres i én transaktion. Ved migrationsfejl afbrydes
opstart, og `Database.Migrate()` er fjernet. DbUp registrerer udførte scripts
med deres resource-navne; efter ibrugtagning skal skemaændringer tilføjes som
nye scripts, ikke ved at redigere eller omdøbe allerede udførte scripts.

**Beslutning 2026-10-01:** Produktion kører ifølge Benjamin `v0.1.6`, og
alle gamle EF-migrations er kørt uden fejl. Der er nu implementeret en
overgang for dette skema. Det erstatter den tidligere begrænsning til
nyoprettede databaser. `000_legacy_v016.sql` kontrollerer hele historikken,
låser de gamle tabeller i `public`, inden det nye
skema oprettes i `ds28`. De gamle tabeller flyttes ikke. `006_copy_v016.sql` kopierer data, bevarer id'er og
loginoplysninger, flytter budgetter og katalogrelationer og viderefører
identity-sekvenser. Eksisterende åbne/lukkede tilmeldinger bevares.
Begge scripts indgår i samme DbUp-transaktion som skemaoprettelsen, og
variabelsubstitution er deaktiveret af hensyn til PostgreSQL-blokkene. DbUp-logning går
via `MigrationLog` til ASP.NET-logningen. Fejltekst uden formatargumenter
behandles som ren tekst, så eksempelvis PostgreSQL-arrays med `{...}` ikke
skjuler databasefejlen med en `FormatException`. Opstartsfejlen medtager
scriptnavn og den oprindelige fejlbesked.

Overgangen afviser manglende/ukendte migrations, blandede skemaer og data,
der ikke kan opfylde de nye constraints. Delte eller forældreløse kataloger
og priser, der kræver afrunding, afvises eksplicit. Der slettes ikke gamle
data; de gamle tabeller i `public` bevares til efterkontrol. Nye auditfelter får
migreringstidspunktet, ikke opdigtede historiske datoer. Fremtidige ændringer
skal fortsat tilføjes som nye scripts.

Overgangen er integrationstestet med PostgreSQL og syntetiske data fra det
originale v0.1.6-skema. Den er ikke kørt mod produktionsdata. Deploy kræver
backup, prøvekørsel på en kopi og stop af gamle appinstanser; se
[deployvejledningen](deploy-v016-database.md). Databaser med tidligere
manuelle eller delvise skemaomlægninger kræver særskilt afstemning.

**Beslutning 2026-10-01:** `ds28` er nu standardskemaet for hele den nye
EF-model, inklusive Identity og OpenIddict. DbUp bruger også `ds28`, og
journalen ligger i `ds28.schemaversions`. SQL-skemaoprettelse og datakopiering
bruger eksplicitte skemanavne. Dette erstatter den tidligere overgangsplan,
hvor de gamle tabeller skulle flyttes til `ds28_legacy_v016`, og de nye
oprettes i `public`. Kopieringen går nu fra `public` til `ds28`, mens
kildetabellerne og EF-historikken bliver i `public` uændret. De gamle data
holdes ikke synkroniseret efter overgangen.

Allerede gennemførte omlægninger til snake_case i `public` og eksisterende
arkivskemaer afvises, så nyere data ikke overskrives med en gammel kopi.
En sådan database kræver særskilt afstemning. Nyinstallation, kopiering fra
v0.1.6 og gentagen opstart mod `ds28` er understøttet.

`ActivityTimeslot` og `ScoutSignup` har nu modeller og konfigurationer;
`scout_activity_timeslot` er fortsat uden en model.

Aktivitetsredigering indlæser og opdaterer det eksisterende budget i den
selvstændige `activity_budget`-tabel. Kun aktiviteter uden et budget får
oprettet en ny række, så den unikke `activity_id` bevares.

**Beslutning 2026-09-30:** `EFCore.NamingConventions` aktiveres med
`UseSnakeCaseNamingConvention()` både ved normal opstart og i
`DesignTimeDbContextFactory`. Almindelige kolonnenavne kommer fra konventionen;
kun afvigelser som `OrderedToDate` → `use_date` mappes eksplicit. De eksplicitte
tabelnavne fra SQL-planen bevares. Konventionen gælder også nøgler, indeks og
OpenIddict-kolonner.

**Beslutning 2026-09-30:** OpenIddict-tabellerne er nu også med i `db.sql`
som `open_iddict_applications`, `open_iddict_authorizations`,
`open_iddict_scopes` og `open_iddict_tokens`. Det erstatter den tidligere
beslutning om at bevare OpenIddicts PascalCase-tabelnavne. Fire konfigurationer
under `DS/Data/Configurations` fastlægger tabelnavnene; OpenIddicts egen
konfiguration bevarer feltregler, relationer og concurrency tokens.
SQL-definitionerne er hentet fra den aktuelle EF-model og kontrolleret mod
den slettede `AddOpenIddict`-migration i Git-historikken. De omfatter alle
kolonner, primærnøgler, tre fremmednøgler og seks indeks, heraf tre unikke.
Fremmednøglerne bruger fortsat PostgreSQLs standard `NO ACTION`, uden cascade.
SQL'en opretter nye tabeller og flytter eller omdøber ikke eksisterende data.
Der er ikke kørt SQL mod en database som del af denne opgave.

**Beslutning 2026-10-01:** Boolean-felter med database-standardværdi
konfigureres med `HasSentinel(false)` ved siden af `HasDefaultValueSql("FALSE")`,
fordi EF ellers advarer om manglende sentinel. Sentinel-værdien gør det
eksplicit, at EF udelader kolonnen ved `false` og lader
`DEFAULT FALSE` gælde. Det er den tilsigtede adfærd, fordi
SQL-standarden er `FALSE`: feltet kan derfor ikke indsættes eksplicit som
`false` ad en anden vej, men resultatet er det samme. Det gælder
`ActivityTeamMembership.IsAdmin`, `PatrolMembership.IsPatrolLeader`,
`RegistrationSettings.IsPreSignupOpen` og `IsSignupOpen`,
`User.HasEnabledAuthenticator` samt `UserInvitation.Used` og
`UserInvitation.IsAdmin`. Det ændrer hverken skema eller data, da
DbUp fortsat ejer DDL'en. Identity's øvrige bool-felter har ingen
database-standardværdi og får derfor ingen sentinel.

**Beslutning 2026-10-01:** Et produktionsforsøg på overgangen til
`v0.1.6`-skemaet fejlede i `006_copy_v016.sql`, fordi `scout_group`
afviser andre distrikter end `DANEHOF` og `FIONIA`, mens
`public."Groups"."District"` stadig er tom for grupper oprettet før den
gamle `addingDistricts`-migration. Den tilføjede kolonne med
`defaultValue: ""` gav disse rækker tom tekst, og ingen senere migration
rettede dem. Den gamle enum havde kun `DANEHOF` og `FIONIA`, så den gamle
app behandlede tomme distrikter som `DANEHOF`. Kopieringen normaliserer
derfor tomme distrikter til `DANEHOF` og øvrige værdier til store
bogstaver, på samme måde som kønsværdier allerede blev konverteret.
`000_legacy_v016.sql` afviser distrikter ud over tom, `DANEHOF` og
`FIONIA`.

Samme kørsel afslørede, at syv `CHECK`-krav ikke blev forudgående
kontrolleret: ikke-negative materialpriser, positive materialmængder,
ikke-negative outbox-forsøg, ikke-negative aktivitetsbudgetter og
ikke-negative forhåndstilmeldingstal. De afvises nu i
`000_legacy_v016.sql` med beskeder, der angiver den overtrådte regel, i
stedet for at kopieringen bryder midt i transaktionen. Det er verificeret
ved at køre scripts mod et syntetisk v0.1.6-skema bygget fra
Git-historikken.

**Beslutning 2026-10-01:** Kolonner der var nullable i v0.1.6, er nullable
i `ds28` også. Det gælder navne på grupper, spejdere, patruljer,
materialer, aktiviteter, aktivitetshold og kategorier, outboxens
`event_type`, `user_id`, `to_email`, `subject` og `body`,
invitationsens `email` og `roles`, materialordrernes `activity_id` og
`material_id` samt `activity_team_membership.user_id`. Scriptet
`006a_relax_legacy_nullable.sql` fjerner `NOT NULL`, og de tilsvarende
`.IsRequired()` er fjernet fra konfigurationerne. Prefixet `006a` er
valgt, fordi scriptet skal køre før `006_copy_v016.sql` på en database
hvor overgangen endnu ikke er kørt, men stadig anvendes på en database
hvor `006` allerede er registreret; `DROP NOT NULL` er idempotent.

Dette er en afslappelse af det nye skema og ikke en stramning. Det er
valgt fordi overgangen ellers ville kræve rettelse af data i
produktionsdatabasien. Det er ikke dokumenteret, at v0.1.6 faktisk
indeholdt `NULL` i disse kolonner: v0.1.6 havde `Nullable` slået fra, så
EF gjorde alle `string`-egenskaber nullable medmindre de var markeret
`IsRequired()`. Den gamle kode oprettede heller aldrig outbox-rækker, så
ud af disse kolonner er `email_outbox` antageligt tom (tabellen er siden
fjernet, beslutning 2026-10-06). At slappe `NULL`
er derfor forsigtighed for data, der sandsynligvis ikke findes, og det
svækker blandt andet at `scout_group.name` og `email_outbox.user_id` nu
kan mangle. To unikke krav bliver også svækket, fordi PostgreSQL tillader
flere `NULL`: `activity_category.name` og
`activity_team_membership (user_id, activity_team_id)`.

Konsekvenser der endnu ikke er håndteret. Den reelle risiko er
accept af en migreret invitation: `InvitationApiController` sætter
`newUser.Email = invitation.Email`, hvor `User.Email` er påkrævet, kalder
`FindByEmailAsync(invitation.Email)` og derefter
`AddToRolesAsync(user, invitation.Roles)`. En migreret invitation uden
email eller roller kan derfor give en uventet fejl i en brugers
accept-flow, selv om brugeren er oprettet i mellemtiden. Det er ikke
ændret her.

`EmailService.QueueInvitationMail` kalder
`new MailboxAddress("", invitation.Email)` uden try/catch, så en
ugyldig eller manglende adresse giver en fejl i API-kaldet før
enqueue i stedet for at blive en job-fejl; `MailJobs.Send` giver
heller ikke retry på `ArgumentException` eller `ParseException`.
Metoden har kun `UserApiController` som kalder, og
den sender en invitation den lige har oprettet af validerede input, så
den læser ikke migrerede rækker. Frontendens TypeScript-typer for navne og
invitationsemail er stadig ikke-null. Intet af dette er
ændret her.

## Foreløbig tidsplan

| Dato | Milepæl |
| --- | --- |
| 18. oktober 2027 | Forhåndstilmelding åbner. Den oprindelige tekst angiver samtidig "(nov)". |
| 30. november 2027 | Forhåndstilmelding lukker. |
| 1. marts 2028 | Endelig tilmelding åbner. |
| 31. marts 2028 | Endelig tilmelding "lukker". |
| 1. april 2028 | Aktivitetstilmelding åbner. |

Anførselstegn omkring "lukker" betyder ifølge planen, at tilmeldingen ikke
lukker helt. Hvilke handlinger der fortsat skal være mulige, er ikke beskrevet.
Uoverensstemmelsen mellem 18. oktober og "(nov)" skal også afklares, før datoen
bruges til at styre adgang.

## Senere afklaringer og nuværende implementering

Følgende er konstateret i den aktuelle kode og samtalen, da planen blev føjet
til projektet. Kontrollér altid koden igen ved fremtidige ændringer.

- Keycloak er fjernet fra planen efter Benjamins beslutning. Det er ikke en
  kommende integration eller en åben arkitekturbeslutning. Backend bruger
  ASP.NET Core Identity og OpenIddict.
- Frontend bruger Vue/Vite, Nuxt UI og Tailwind i `src/DS.Frontend`, serveret
  på `/`. Den gamle Buefy-frontend er arkiveret i `src/DS.Frontend.Archive` og
  bygges eller serveres ikke.
- Globale roller og rettigheder ligger i `DS.Domain/Roles.cs`. Planens roller
  er ikke nødvendigvis implementeret med de beskrevne rettigheder.
- Spejdergrupper er adskilt fra globale roller. `User.Group` knytter en bruger
  til en gruppe. Der er ingen særskilt gruppeadministrator eller gruppeejer.
- Gruppemedlemmer har samme adgang til at ændre forhåndstilmelding og
  administrere gruppens medlemmer. Aktivitets-teamlederrollen indebærer ikke
  en tilsvarende lederrolle i spejdergrupper.
- Det implementerede forhåndstilmeldingsflow starter offentligt med et
  gruppenummer, gemmer `GroupPreSignup`, opretter en bruger og logger ind på
  gruppens egen side, `/group`.
- **Beslutning 2026-10-19 (implementeret):** Gruppeinvitationer sender automatisk
  en email med et personligt link via `EmailService.QueueInvitationMail` og
  den eksisterende Hangfire-mailkø. Linket bruger `DS.PublicBaseUrl` og peger på
  `/group-invitation/{id}`. Det kan fortsat vises og deles manuelt. Dette
  erstatter den tidligere beslutning om kun at dele links manuelt uden email.
  Afsendelse kræver som øvrige mails, at `DS.NotificationsEnabled` er slået til.
- Fjernelse af et gruppemedlem ophæver gruppetilknytningen og bevarer kontoen.
  Et medlem kan ikke fjerne sig selv gennem gruppens brugeradministration.
- Lejrindstillinger har en selvstændig side på `/camp-settings`, som åbnes fra
  HQ. Her ligger en Buefy-toggle til at åbne og lukke forhåndstilmeldingen og
  en tilsvarende toggle til at åbne og lukke den endelige tilmelding.
  Den nuværende adgang er givet til systemadministrator og lejrchef gennem
  `PreSignupManage`.
- Åben/lukket gemmes i databasen som `IsPreSignupOpen` og `IsSignupOpen` og
  styres manuelt, uafhængigt af planens datoer. Forhåndstilmeldingen starter
  som åben ved migrering for at bevare den hidtidige adgang, mens den endelige
  tilmelding starter lukket.
- Indstillingen for endelig tilmelding styrer endnu kun selve flaget; den
  gater ikke adgangen til siden `/group/final-signup`.
- **Beslutning 2026-10-02:** Den endelige tilmeldingsside (`/group/final-signup`)
  er implementeret og tilgængelig fra gruppekortet. Siden giver gruppemedlemmer
  mulighed for at oprette spejdere (navn, fødselsdato, køn), oprette patruljer
  og tildele spejdere til patruljer. En spejder kan tilknyttes flere patruljer.
  Endpoints er gruppeafgrænsede under `GET/POST/DELETE /api/v1/group/scouts` og
  `/api/v1/group/patrols` samt `POST /api/v1/group/scouts/add-patrol` og
  `/api/v1/group/scouts/remove-patrol`. Alle endpoints kontrollerer, at den
  pågældende ressource tilhører brugerens gruppe. Åben/lukket-flaget for endelig
  tilmelding håndhæves endnu ikke over for siden; det er et åbent spørgsmål.
- Den foreløbige fortolkning af lukket forhåndstilmelding er, at både nye
  tilmeldinger og ændringer af deltagerantal blokeres i backend. Eksisterende
  tilmeldinger kan fortsat ses. Om eksisterende grupper skal kunne ændre deres
  tal efter lukning, er endnu ikke bekræftet af Benjamin.
- **DS-92 (implementeret, afventer review):** Lejrindstillinger har to nye
  flag, `IsActivityWishRoundOpen` (aktivitetsønskerunde, hvor grupperne ønsker
  aktiviteter) og `IsActivitySignupOpen` (endelig aktivitetstilmelding, hvor
  grupperne tilmelder patruljer til aktiviteter). De gemmes i
  `registration_settings`, starter lukket og åbnes og lukkes manuelt med to
  toggles på `/camp-settings` via det eksisterende
  `PUT /api/v1/registration-settings`. De er uafhængige af datoerne i den
  foreløbige tidsplan, så der indføres ingen hardkodede deadlines. Flagene
  gater endnu intet, fordi ønske- og tilmeldingsflowet ikke er bygget, og
  gruppesiden viser fortsat begge kort som »Ikke tilgængelig endnu«.
  Følgende er forsigtige standardvalg, som afventer Benjamins bekræftelse:
  - Flagene kræver samme adgang som de eksisterende tilmeldingsflag
    (`PreSignupManage`); der er ikke indført en ny rolle.
  - Flagene er uafhængige, og begge kan være åbne samtidig.
  - Det er ikke besluttet, hvad lukket endelig aktivitetstilmelding betyder,
    fx om tilmeldingerne bliver skrivebeskyttede eller skjules. Det afklares,
    når flowet bygges.
- **DS-96 (implementeret, afventer review):** Lejrens dage er data i
  `camp_day` og redigeres i kortet "Lejrdage" på `/camp-settings`, som kræver
  `PreSignupManage`. Hver dag har en unik dato og en halvdel: 1. halvdel, 2.
  halvdel eller begge (skiftedag). Ved tilføjelse foreslås dagen efter den
  sidste lejrdag. `GET /api/v1/camp-days` kræver kun login, fordi grupperne
  skal bruge dagene i DS-72; `POST`, `PUT /{id}` og `DELETE /{id}` kræver
  `PreSignupManage`, og en dublet af en dato giver HTTP 409. En dag med
  tilmeldte spejdere kan ikke slettes (HTTP 409 med antallet), men datoen kan
  rettes (DS-72). Ingen datoer er hardkodet eller seedet.
- **DS-72 (implementeret, afventer review):** Spejderens deltagelsesdage
  gemmes i `scout_camp_day` og vises som `campDayIds` på spejderen i
  `GET /api/v1/group`. `PUT /api/v1/group/scouts/days` sætter dagene for en
  eller flere spejdere i brugerens egen gruppe og erstatter deres tidligere
  dage; med `onlyScoutsWithoutDays` røres spejdere, der allerede har dage,
  ikke. En tom liste af dage afvises, fordi der ikke findes en tilstand for "0
  dage". `POST /api/v1/group/scouts` tager valgfrit `campDayIds`. Når en
  spejder slettes (soft delete), fjernes spejderens dage, så de ikke blokerer
  sletning af en lejrdag. På `/group/final-signup` vælges Hel uge, 1. halvdel,
  2. halvdel eller Enkelte dage pr. spejder, og valget gemmes med det samme.
  Hel uge er alle lejrdage, og en halvdel er dagene med den halvdel eller
  `Both`. Spejdere kan vælges enkeltvis, alle eller pr. patrulje og sættes til
  en periode samlet. Siden viser deltagere pr. dag, en advarsel med navnene på
  spejdere uden dage, et filter, der kun viser dem, og en knap, der giver alle
  uden dage samme periode. Gruppens forside `/group` viser status for
  manglende dage med et link til filteret. Spejder- og patruljekortet står nu
  under hinanden i fuld bredde. Når lejrdagene ikke er oprettet, vises en
  besked i stedet for dagvalget. Følgende valg blev truffet under
  implementeringen og er bekræftet af Benjamin 2026-11-23:
  - Fjernelse af den sidste dag under Enkelte dage afvises med en besked om at
    slette spejderen i stedet.
  - Den administrative sletning af en hel gruppe soft-sletter kun gruppen; dens
    spejderes dage bliver liggende og tæller med i sletteblokeringen af en
    lejrdag.
- Glemt adgangskode er et selvbetjent flow på `/forgot-password`. Brugeren
  indtaster sin email, og backend sender et nulstillingslink på
  `/reset-password/{brugerId}?token=...`. Det er en mail, der sendes
  automatisk. Gruppeinvitationer sender også automatisk email efter
  beslutningen 2026-10-19 ovenfor.
- Endpoints svarer altid ens på en anmodning om nulstillingslink, uanset om
  emailadressen findes, så svaret ikke afslører hvilke brugere der er
  oprettet. Det er kun det konkrete nulstillingslink, der afslører om
  brugeren findes.
- Linket i mailen bygges ud fra `DS.PublicBaseUrl`, ligesom invitationsmailen.
  Den eksisterende admin-genererede nulstillingslink-funktion bruger derimod
  `Request.Scheme` og `Request.Host` og er dermed sårbar bag en proxy.
- Nulstillingslinket er Identity's `DataProtectorTokenProvider` med en
  standardlevetid på 24 timer. Der er ikke implementeret en streng
  engangsbrugsbegrænsning ud over den normale sikkerhedsstempel, så teksten i
  brugeradmin-fladen om at linket kun er gyldigt ét brug er upræcis. Det er
  ikke ændret af denne implementering.
- **Beslutning 2026-09-29:** Hvis en bruger tilknyttet en gruppe tilgår
  forhåndstilmeldingen (`/group/pre-signup`), men der endnu ikke er oprettet en
  forhåndstilmelding for gruppen gennem det offentlige flow, oprettes der
  automatisk en tom `GroupPreSignup` for gruppen, så brugeren alligevel får
  adgang til at se og redigere tallene. Hvis en bruger ikke er tilknyttet en
  gruppe, gives der ikke adgang.
- **Beslutning 2026-09-29:** Hvis en gruppe allerede har en `GroupPreSignup`
  (f.eks. oprettet af staben), men gruppen endnu ikke har nogen oprettet bruger,
  tillader det offentlige forhåndstilmeldingsflow (`/group-pre-signup`), at en
  gruppeleder gennemfører flowet og opretter den første bruger til gruppen.
  Eksisterende forhåndstilmeldte deltagerantal forudfyldes i flowet. Dette kan
  kun ske 1 gang: så snart en bruger er tilknyttet gruppen, spærres det
  offentlige flow for gruppen for at forhindre uautoriseret brugeroprettelse.


Nye beslutninger kan føjes til dette afsnit, så den oprindelige plan fortsat
kan skelnes fra senere valg.


Afklaring 2026-10-01: Den rapporterede database har
`20260929075734_AddEmailOutbox`, mens Git-tagget har
`20260929112910_AddEmailOutbox`. Overgangen accepterer begge konkrete id'er
som alternativer, men ikke begge samtidig eller andre ekstra migrations.
Den oprindelige historik bevares uændret. `EmailOutbox` kontrolleres for det
præcise sæt kolonner, PostgreSQL-datatyper og nullability før overgangen.
Den tidligere migrationsfil er ikke fundet i Git-historikken; understøttelsen
er derfor betinget af skemakontrollen, ikke af en antagelse om identisk kode.


## Containerbyg og publicering

**Beslutning 2026-10-16:** Frontendens build-stage bruger det officielle
`docker.io/library/node:24` direkte fra Docker Hub. Det erstatter
`public.ecr.aws/docker/library/node:24`, fordi ECR Public afviste hentningen
med HTTP 429 (Data limit exceeded) under releasebygget af v0.1.23.
Node-versionen og frontendens buildkommandoer er uændrede. Backendens
.NET 10-images hentes fortsat fra Microsoft Container Registry, og
releaseimages publiceres fortsat i GitHub Container Registry via
versionstags. Et eksisterende versionstag flyttes ikke ved buildrettelser;
rettelsen udgives som en ny patch-version.

**Beslutning 2026-11-15:** Git-arbejdsgangen og udgivelsen er fastlagt. Der
indføres ikke en produktion- eller release-branch.

- `master` er den eneste lange branch, og et SemVer-tag er udgivelsen.
  Produktionsbranche, release-branch og squash-merge bruges ikke.
- Branches hedder `feature/`, `fix/` eller `chore/` efterfulgt af et kort navn.
  Ét kort på DS28 IT svarer til én pull request og én afgrænset ændring, og
  pull requestens titel er `[DS-<nummer>]: <kort beskrivelse>`.
- Produktion på `hq.distriktssommerlejr.dk` er det eneste miljø. Lokal kørsel
  er det eneste testmiljø, indtil der eventuelt oprettes et rigtigt et.
- Det præcise image, der blev bygget til et tag, deployes. Der genbygges ikke
  til produktion, og deploy er et selvstændigt, godkendt trin via GitHub
  Environment `production` frem for en automatisk følge af et tag.
- Migrations skal være bagudkompatible i én udgivelse (expand/contract), så
  det forrige tag kan deployes igen, og der tages en database-dump før et
  deploy med migration. Dette er nødvendigt, fordi DbUp kører fremad og ikke
  kan rulle en migration tilbage.
- Pull requests bygges og testes ikke i CI; kun CodeQL kører. Der bygges og
  testes lokalt efter verifikationsafsnittet, før der merges. Dette er et
  bevidst valg for at holde PR-flowet let, ikke en midlertidig mangel.
- Når arbejdet på et kort starter, tildeler den, der griber kortet, sig selv som
  ansvarlig og flytter kortet til `I gang` på DS28 IT, uanset om kortet kommer
  fra HQ eller oprettes i forbindelse med arbejdet. Det gør det synligt, hvem
  der arbejder på kortet.
- **Præcisering 2026-11-16:** Ved afslutning af en opgave kontrolleres, at den,
  der har udført arbejdet, står med navn som ansvarlig på kortet. Det gælder
  også kort, der oprettes efter arbejdet. Tildelingen bevares efter afslutning,
  merge og udgivelse; øvrige ansvarlige bevares, når en person tilføjes.
  Flere samtidige ansvarlige med individuel klarmelding er bestilt i DS-40.
  Individuel klarmelding og reglerne for fælles færdigmelding er endnu ikke
  implementeret eller endeligt afklaret; udgivelsesstatus er fortsat separat.
- Tavlen får kolonnen `Ikke udgivet` mellem `I gang` og `Færdig`. Et kort
  flyttes dertil, når ændringen er merget til `master`, og videre til `Færdig`,
  når den er med i et tag, der er deployet til produktion. `Færdig` betyder
  dermed udgivet, ikke kun merget.

## Frontend og formularvisning

### Designguide

**Beslutning 2026-11-18:** UI i HQ følger [designguiden](design-guide.md). Den
fastlægger sidetyper, farver, knaptyper, modaler, bekræftelser, feedback,
tekstregler og brudpunkter ud fra de mønstre, der var mest udbredt i koden.
Eksisterende kode er ikke omskrevet; afvigelser er listet i guiden og rettes,
når filerne alligevel ændres. En fælles SCSS-fil med farvevariabler er foreslået,
men ikke besluttet.

### Frontend med Nuxt UI

**Beslutning 2026-11-21 (implementeret):** Beta er nu den officielle
frontend. Den ligger i `src/DS.Frontend`, bygges til `DS.Backend/wwwroot/dist/`
og serveres på `/` uden `/beta`-præfiks. Den gamle Buefy-frontend er flyttet
til `src/DS.Frontend.Archive` og bygges hverken i `Containerfile.Website` eller
serveres af backenden; den bygger kun lokalt til sin egen `dist/`-mappe.
Gamle links til `/beta/*` sendes permanent videre til samme sti uden præfiks
(`Program.cs`). Beta-mærkatet i layoutet og "(beta)" i sidetitlen er fjernet.
Det nedenstående, herunder at kopierede filer skal ændres begge steder og at
designguiden kun gælder Buefy-frontenden, gælder ikke længere. Afsnittet
beskriver ellers fortsat de sider, der nu er den officielle frontend; stierne
er opdateret til at være uden `/beta`. I afsnittet betyder »beta« den frontend,
der nu er `src/DS.Frontend`, og »den nuværende frontend« den arkiverede
Buefy-frontend. `src/DS.Frontend.Beta` findes ikke længere i repositoriet.

**Beslutning 2026-11-20 (implementeret som første skridt):** En ny frontend,
`src/DS.Frontend.Beta`, bygges med Vue, Nuxt UI 4 og Tailwind CSS 4 ved siden
af den nuværende Buefy-frontend. Begrundelsen er, at Nuxt UI ser bedre ud og
har færdige dashboard-komponenter, som gør det nemmere at give hvert modul sit
eget dashboard. Beta er et separat projekt, så Bulma og Tailwind aldrig
indlæses på samme side.

- Services, stores, `lib` og `types` blev kopieret fra den gamle frontend. Da
  den nu er arkiveret, ændres de kun i `src/DS.Frontend`.
- Frontenden bygges til `DS.Backend/wwwroot/dist/` og serveres på `/*`
  (`Program.cs`), som udgangspunkt kun for indloggede brugere. Login,
  registrering, glemt og nulstil adgangskode, invitationer og den offentlige
  forhåndstilmelding serveres også uden login under `/`
  (`AnonymousController`), ligesom de tilsvarende sider i den nuværende
  frontend. Beta-routeren skelner mellem offentlige sider (`meta.public`),
  sider kun for gæster (`meta.guestOnly`, indloggede sendes til forsiden) og
  sider, der kræver login, og sender til `/login` med `returnUrl`.
  Backendens cookie-login sender stadig til den nuværende `/login`, når en
  side under `/` åbnes uden login.
- Ikoner er Lucide via `@iconify-json/lucide` og bundtes ved build, så
  browseren ikke henter ikoner fra Iconifys API.
- Den globale søgning fra `SearchMenu.vue` er i beta Nuxt UI's
  `UDashboardSearch` med en søgeknap øverst i sidebaren. Den viser moduler fra
  HQ-genvejene og søger i tavler, grupper og aktiviteter via `/api/v1/search`.
  Den åbnes med ⌘/Ctrl+K i stedet for ⌘/Ctrl+Space, som Spotlight optager på
  Mac. Listerne i bruger-, gruppe- og teamstyringen har derfor ikke længere
  deres egen ⌘/Ctrl+K-genvej.
- Brugeren nederst i sidebaren åbner en brugermenu med »Min profil«, »Vis som
  …« (kun SysAdmin, under visning »Stop visning«) og »Log ud«, som i den
  nuværende frontends brugermenu. Linket »Til nuværende HQ« i sidebaren er
  fjernet, da det gamle UI er arkiveret. »Vis som …« åbner samme valg af rolle som i dag. Under visning står et gult banner øverst i sidebaren med
  rollen og »Stop visning«. Efter start og stop sendes man til forsiden, så man
  ikke står på en side, rollen ikke har adgang til.
- Beta følger som udgangspunkt systemets lyse eller mørke tema. En menu ved
  brugeren nederst i sidebaren vælger mellem Automatisk (følger systemet),
  Lys og Mørk, og valget gemmes i browseren. Den bruger `useColorMode` fra
  `@vueuse/core`, som Nuxt UI selv bruger, og pakken er derfor skrevet
  eksplicit i beta's `package.json` med samme version.
- Første modul er aktiviteter: `/activity` viser for
  aktivitetsadministratorer nøgletal, filtre og en sorterbar tabel og for
  andre den fulde teamvisning med medlemmer, aktiviteter og oprettelse af
  team. `/activity/:id` er aktivitetsdetaljen med faner, filer i Google
  Drev og godkendelse, og `/activity/teams/:id?` (kræver `ActivityAdmin`)
  er teamstyringen. Aktivitetsdetaljen henter selv aktiviteten, når siden
  åbnes, og datoer på filer vises med `Intl`, fordi `formatDate` i
  `lib/util.ts` viser måneden én for lavt.
- Forsiden `/` viser de samme HQ-genveje som `Home.vue` fra
  `/api/v1/home` samt versionen. Backendens Font Awesome-navne oversættes til
  Lucide-ikoner i `lib/hqShortcuts.ts`. Genveje til moduler i frontenden
  åbner i routeren, og de øvrige åbnes med en almindelig sideindlæsning.
- `/admin` (kræver `AdminAccess`) viser de samme sektioner og
  tilmeldingsstatistik som `Admin.vue` samt antal brugere og grupper. Den
  historiske synkronisering er flyttet til lejrindstillingerne (DS-70). Undersiderne og Hangfire åbner i den nuværende frontend, og
  Hangfire åbner i et nyt vindue som i dag.
- `/admin/user/:id?` er brugerstyringen fra `User.vue`: liste med søgning,
  detaljer, oprettelse, invitation, roller, stillinger, lås, nulstillingslink og
  sletning med samme rolletjek som i dag. Forskelle fra den nuværende side:
  stamdata redigeres i en kladde og gemmes først med »Gem«, så ugemte ændringer
  ikke vises i listen, og fjernelse af roller, låsning og sletning bekræftes i
  en dialog. På mobil åbner brugeren i en skuffe. Upload af profilbillede er
  ikke flyttet; knappen linker til brugeren i den nuværende frontend, mens
  »Fjern billede« virker i beta.
- `/admin/groups/:id?` er gruppestyringen fra `Groups.vue`: liste med
  søgning, oprettelse, stamdata, spejdere med patruljemedlemskaber og
  patruljeledere, patruljer, gruppens brugere og tilmeldingstal fra 2024 (kun
  for `AdminAccess`). Navn og distrikt redigeres i en kladde og gemmes med
  »Gem«. Spejdere, patruljer og medlemskaber gemmes med det samme som i dag, og
  siden læser gruppen fra store efter hver handling i stedet for at rette en
  lokal kopi. Gruppens brugere linker til brugerstyringen i beta.
- `/admin/camp-settings` har de samme to kontakter som `CampSettings.vue`
  til forhåndstilmelding og endelig tilmelding. Beta viser desuden status som
  tekst, en toast ved ændring og en fejlbesked, hvis ændringen ikke kan
  bekræftes. Den nuværende side gemmer fejlen uden at vise den.
- **Beslutning 2026-11-20 (implementeret i beta):** Forbindelsen til Google Drev
  fra `GoogleAdmin.vue` ligger i beta som en sektion i lejrindstillingerne i
  stedet for på en egen side. Siden kræver `PreSignupManage` eller
  `AdminAccess` og viser tilmeldingssektionen for `PreSignupManage` og Google
  Drev for `AdminAccess`, så rollekravene er de samme som i dag.
  `/admin/google` sender videre til lejrindstillingerne. Backenden sender
  brugeren tilbage til `/admin/google` efter forbindelse til Google, som nu
  sender videre til lejrindstillingerne.
- **Beslutning 2026-11-21 (DS-70, implementeret):** Flisen "Google Drev" under
  Admin → System er fjernet, da forbindelsen ligger i lejrindstillingerne.
  Flisen "Lejrindstillinger" vises nu for `PreSignupManage` eller
  `AdminAccess`, så den matcher sidens adgangskrav, og brugere med kun
  `AdminAccess` stadig kan nå Google Drev. Knappen til historisk
  synkronisering er flyttet fra `/admin` til en sektion i lejrindstillingerne
  og vises for `AdminAccess`, som endpointet også kræver.
- `/admin/exports` (kræver `ExportsView`) viser de samme udtræk som
  `Exports.vue` og henter dem som .xlsx via det samme API. Mens et udtræk
  hentes, er de andre knapper låst.
- `/admin/openiddict` (kræver `OpenIddictManage`) har samme funktioner
  og regler som `OpenIddictClients.vue`: oprettelse, redigering, nyt secret og
  sletning, mindst én loginrettighed, PKCE for public-klienter, bekræftelse ved
  skift af klienttype og en secret-dialog, der ikke kan lukkes ved et uheld.
  Klienterne vises som kort i stedet for en bred tabel, og scopes vises på
  kortet.
- `/org` (kræver `OrgView`) viser organisationsdiagrammet fra
  `OrgChart.vue` med samme funktioner i en skuffe: redigering, indehavere,
  invitation, personlig assistent, flytning, understillinger og sletning.
  Rettighedsreglerne (`orgAccess.ts`) og farvepaletten er kopieret uændret.
  Stillingens farve vises som en kant i toppen og en svag tone, så den virker i
  både lyst og mørkt tema, og sletning bekræftes i samme dialog som resten af
  beta. Organisation vises i sidebaren for brugere med `OrgView`.
- `/board` og `/board/:id` (kræver `BoardView`) har hele tavlemodulet
  fra `Board.vue` og `BoardDetail.vue`: oversigt, oprettelse, kolonner, kort,
  træk-og-slip af kort og kolonner, tag-filter, live-opdatering via SignalR,
  kortdialog med beskrivelse, tags, ansvarlige, stilling, deadline, prioritet,
  farve, afhængigheder, pull requests og placeringer på andre tavler samt
  dialoger til kolonner, medlemmer, farver, tags og tavleindstillinger.
  Kolonnerne følger den besluttede del af kanban-redesignet (hvide flader med
  skygge, fast grå eller grøn topkant og skillelinje under overskriften; kort
  med let grå baggrund, der bliver lys ved hover). Redesignets uafklarede
  forslag er ikke lavet, med én undtagelse: tavlens handlinger ligger i
  én »Tavle«-menu, fordi de fem knapper ikke er plads til i dashboard-headeren.
  Det er ikke en beslutning om den nuværende frontend. Beskrivelseseditoren er
  Nuxt UI's `UEditor` (TipTap) med samme formateringsmuligheder som før.
  Kortenes egenskaber er almindelige formularfelter i sidepanelet i stedet for
  dropdowns.
- `/group` viser gruppesiden fra `Group.vue`: gruppens navn, ID og
  distrikt, gruppens brugere og links til forhåndstilmelding, endelig
  tilmelding og administration af brugere, som åbner i den nuværende frontend.
  Aktivitetsønskerunde og aktivitetstilmelding står som ikke tilgængelige, som i
  dag. Beta viser desuden antal spejdere, patruljer og brugere ud fra de data,
  siden allerede henter, og en tom tilstand for brugere uden gruppe.
- `/group/pre-signup` er gruppens redigering af forhåndstilmeldingen fra
  `GroupPreSignupEdit.vue`: de syv grene med tal-felter, total, fortryd og gem,
  og låste felter, når forhåndstilmeldingen er lukket. Den offentlige
  `/group-pre-signup` for grupper, der ikke er logget ind, bliver i den
  nuværende frontend, fordi `/` kræver login.
- `/group/final-signup` er gruppens endelige tilmelding fra
  `GroupFinalSignup.vue`: oprettelse og sletning af spejdere og patruljer og
  tildeling af spejdere til patruljer via gruppens eget API. Gruppen hentes
  igen efter hver handling i stedet for at blive rettet lokalt. Fejl fra
  serveren vises både på siden og som toast, så de også ses, mens en dialog er
  åben. Ligesom i dag tjekker siden ikke, om den endelige tilmelding er åben;
  det afgør backenden.
- `/group/members` er gruppens brugere fra `GroupMembers.vue`: medlemmer,
  invitation med email og link, afventende invitationer med link og
  annullering, og fjernelse af medlemmer med bekræftelse. Invitationslinket kan
  kopieres med en knap. Backenden laver invitationslinket til
  `/group-invitation/:id` i den nuværende frontend; beta har sin egen version
  på `/group-invitation/:id`.
- `/profile` er profilen fra `Profile.vue` med profilbillede (samme
  beskæring med `vue-advanced-cropper`), stamdata, adgangskode, notifikationer,
  MCP, totrinsbekræftelse, recovery codes, passkeys og adgangstokens, og
  `/twofactor-setup` er den tvungne totrinsopsætning. Recovery codes og
  nye adgangstokens vises i dialoger, der ikke kan lukkes ved et uheld. Beta
  rettede to fejl fra den nuværende side: recovery codes ved tilføjelse af en
  passkey blev aldrig vist, og passkey-listen opdaterede sig ikke. Fjernelse af
  passkeys og tilbagekaldelse af tokens bekræftes nu. Brugeren nederst i
  sidebaren linker til profilen, og brugerstyringen kan nu også uploade
  profilbilleder.
- Siderne uden login (`/login`, `/register`,
  `/forgot-password`, `/reset-password/:id`, `/invitation/:id`,
  `/group-invitation/:id`, `/group-pre-signup`) og formularvisningen
  `/forms/:id?` bruger et gæstelayout uden sidebar med en tegnet
  lejrbaggrund (`components/Auth/PrairieBackground.vue`, oprindeligt fra
  `ds-design/login`). Baggrunden er lys i begge temaer, så DS28 HQ-teksten
  over kortet har fast mørk farve.
  *Implementeret (DS-53):* Baggrunden er inline SVG i en komponent i
  stedet for et CSS-baggrundsbillede, så dele af den senere kan gøres
  klikbare (DS-54). »Rullende prærie« betyder en vissen prærie-busk
  (tumbleweed), der ruller hen over sletten som i westernfilm. Det er
  afklaret med Benjamin 2026-11-21, så landskabet står stille. Busken ruller
  fra venstre mod højre med hop, cirka hvert 22. sekund, på alle
  gæstesider. Bålets tre flammelag blafrer hver i sit tempo. Med
  `prefers-reduced-motion` vises busken ikke, og bålet står stille.
  *Implementeret (DS-54):* Et klik på cowboyen på mesaen viser en
  tilfældig western-replik i en taleboble i 4,5 sekunder, aldrig den samme
  to gange i træk. Replikkerne ligger i komponenten. Cowboyen er kun synlig
  på brede skærme, fordi baggrunden beskæres fra siderne på smalle skærme.
  Han er et påskeæg og hverken fokuserbar eller læst op af skærmlæsere,
  fordi hele baggrunden er dekorativ.
  *Implementeret (DS-55):* »Større bjerge« gælder den nære mesa til
  venstre. Ifølge Benjamin (2026-11-21) skulle den være en lille smule
  større. Den er nu skaleret 25 % op fra sin fod i venstre side og har fået
  en klippespids på plateauet, der hælder 7 grader mod højre. Validering af
  `returnUrl` er uændret og sker i backenden. Invitationssiden viste i den
  nuværende frontend formularen, selv når invitationen ikke fandtes, fordi
  tjekket brugte ref-objektet i stedet for dets værdi; beta viser den kun for
  gyldige invitationer.
- **Status 2026-11-20:** Alle sider fra den nuværende frontend findes nu i
  beta, bortset fra Hangfire, som er backendens egen side. Siderne er bygget og
  typetjekket, men ikke testet mod den rigtige backend. Benjamin tester dem
  samlet til sidst.
- **Sikkerhed (fundet 2026-11-20, rettet i begge frontends):** `htmlToPlainText`
  i `lib/util.ts` lavede kortenes forhåndsvisning ved at sætte beskrivelsen
  som `innerHTML` på et element, så event-handlere kørte. Beta bruger
  `DOMParser`, og den nuværende frontend er rettet på samme måde (PR #106);
  se "Rettet 2026-11-20 (XSS i kortforhåndsvisning)" under Sikkerhed og
  auth-hærdning.

Afgjort 2026-11-21: Hele frontenden er flyttet til Nuxt UI (se beslutningen
øverst i afsnittet). Designguiden beskriver den arkiverede Buefy-frontend og
gælder ikke den nye frontend. Ikke besluttet: om det arkiverede UI skal slettes
helt, og om der skal skrives en ny designguide for Nuxt UI-frontenden.

### HQ-favicon

**Beslutning 2026-11-17 (implementeret):** HQ bruger den godkendte cowboyhat
med lejrlogoets blå og brune farver som favicon. Ikonet leveres som ICO med
16, 32 og 48 pixels til både Vue-appen og serverrenderede sider. Det erstatter
Vue-appens reference til en manglende SVG-fil og backendens standardikon.

### Installerbar app (PWA)

**Beslutning 2026-11-22 (implementeret, DS-94):** HQ kan installeres som app
på hjemmeskærmen og skrivebordet via et web app manifest
(`src/DS.Frontend/public/manifest.webmanifest`, serveret fra
`/dist/manifest.webmanifest`). Appen hedder »DS28 HQ«, starter på `/` med
scope `/` og åbner i eget vindue (`standalone`) med temafarven `#4caee7`.
Ikonerne er cowboyhatten fra faviconet, tegnet op i 192 og 512 pixels, en
maskable variant på blå baggrund til Android og et 180 pixels
`apple-touch-icon` til iOS. Manifestet og ikonerne er statiske filer og kræver
ikke login.

Der er bevidst ingen service worker endnu, så appen virker ikke offline og har
ingen push-notifikationer. Chromium-browsere kan stadig tilbyde installation
uden service worker. Offline-understøttelse og push er åbne forslag, ikke
vedtagne krav.

### Navigation og tilgængelighed

**Implementeret:** Søgningen kan åbnes med en synlig »Søg«-knap samt
Ctrl/Mellemrum eller Cmd/Mellemrum. Indlæsningsfejl vises med mulighed for at
prøve igen.

**Besluttet og implementeret:** Søgningen finder ud over moduler også tavler,
grupper og aktiviteter, men kun det, brugeren selv har adgang til. Filtreringen
sker på serveren i `GET /api/v1/search`, som følger de samme regler som de
enkelte moduler:

- Tavler kræver `BoardView`. Lejrchef og SysAdmin ser alle tavler, andre kun
  tavler, de er medlem af.
- Med `GroupsView` findes alle grupper og åbnes i gruppestyringen. Uden den
  findes kun brugerens egen gruppe, som åbnes på `/group`.
- Aktiviteter kræver `ActivityView`. `ActivityAdmin` ser alle aktiviteter, andre
  kun aktiviteter i teams, de er medlem af.

Søgningen starter ved to tegn og viser højst fem resultater pr. type.
Resultaterne grupperes i Moduler, Tavler, Grupper og Aktiviteter, og eksterne
links markeres. Under »Vis som« følger søgningen den valgte rolle ligesom
tavlerne. Senest brugte resultater og beskrivelser af moduler er foreslået,
men ikke besluttet.

Sideskift opdaterer browserens sidetitel og flytter fokus til
hovedindholdet. Gruppens brugerpanel stables under indholdet på smalle skærme.

Gruppesiden viser ikke længere det hardkodede medlemstal 0. Aktivitetsønskerunde
og aktivitetstilmelding vises som »Ikke tilgængelig endnu«, fordi kortene endnu
ikke har destinationer. De tidligere hardkodede novemberdatoer i 2026 er fjernet
fra kortene; de var ikke vedtagne åbningstidspunkter.

### Adgang nægtet

**Besluttet og implementeret (2026-11-15):** Der er én fælles skærm på
`/access-denied`, og det er den eneste sti til den. Cookie-auth'ens
`AccessDeniedPath` peger samme sted, så dubletruten `/AccessDenied` er fjernet.

- Routeren sender til skærmen, når `requiredRole` eller `requiresActivityAdmin`
  ikke er opfyldt. `requiresAuth` tjekkes nu først, så en udlogget bruger mødes af
  login med `returnUrl` i stedet for »Adgang nægtet«. `/activity/teams/:id?`
  kræver derfor også login.
- Svarer API'et 403, sendes brugeren til skærmen med den afviste side i `?from=`.
  Det gælder alle kald gennem den fælles klient, også kald der ellers sluger fejl
  via `safeApiCall`. Skærmen viser stien og tilbyder »Gå tilbage«.
- **Kendt begrænsning:** ethvert 403 udløser skærmen, også baggrundskald. Hvis et
  tilbagevendende kald begynder at give 403, uden at siden er utilgængelig, skal
  det have et opt-out i klienten.
- 401 og manglende login håndteres fortsat af routeren og loginflowet, ikke af
  skærmen.

### Formularskabeloner

**Implementeret (2026-10-22):** Kode-definerede formularskabeloner ligger i
Application under `FormTemplates`. Konkrete skabeloner registreres som
`FormTemplate` i DI og findes via `FormTemplateService` ud fra deres unikke id;
controlleren opretter ikke selv skabeloner. Dublerede id'er afvises ved oprettelse
af servicen.

- `GET /api/v1/forms/{id}` returnerer definitionen eller 404. Den eksisterende
  `/api/v1/forms/test` bevares som alias til `test-form-template`.
- Endpointet er fortsat offentligt og indeholder kun en testskabelon. Adgang til
  fremtidige domænespecifikke skabeloner skal afklares, før de registreres her.
- `src/DS.Frontend` har `FormService` og Pinia-storen `useFormStore` med valgt
  skabelon, loading, fejl og nulstilling. Felttyper sendes som enum-navne i JSON.
  `TemplateForm` renderer sektioner med tekst-, tal- og datofelter via Buefy og
  modtager svar separat med `v-model`. Tal er tal/null; datoer er datostrenge
  (`YYYY-MM-DD`), ikke tidsstempler.
- **UI-udkast (2026-10-23):** `/forms/{id}` (eller `/forms` for testskabelonen)
  viser sektionskort, loading/fejl med genforsøg, rydning af svar og en foldbar
  JSON-visning. Testskabelonen demonstrerer alle tre felttyper. Svar findes kun
  lokalt i siden; siden gør tydeligt opmærksom på, at intet gemmes. Layoutet er
  et udspil, ikke et vedtaget domæneflow. Indsendelse, lagring og servervalidering
  er ikke implementeret; der er ingen påstået gemmehandling.

### Historiske tilmeldinger fra 2024

**Besluttet og klargjort (2026-11-14):** Første historiske import omfatter kun
forhåndstilmeldinger og endelige tilmeldinger fra det gamle Fynsdistrikt-projekt.
Historikken kobles til eksisterende DS28-grupper i to separate tabeller,
`historic_presignup` og `historic_signup`; aktive tilmeldinger ændres ikke.
Lejrår, gren, kilde-id, udtrækskontrolsum og importtidspunkt bevares.

- `DS.RegistrationImporter` følger med Website- og Worker-imaget. Importen køres
  som et on-demand Worker-job, som en bruger med `AdminAccess` (`SysAdmin`)
  starter fra Admin-siden. Jobbet bruger `--apply` og erstatter det konfigurerede
  lejrår transaktionelt; genkørsel giver ikke dubletter. Det er ikke et
  opstartsjob eller et tilbagevendende job. Kommandolinjen i Website-imaget
  bevares til prøvekørsel og fejlsøgning.
- **Ændret beslutning (2026-11-15):** Importen konfigureres med
  `DS__HistoricRegistration__FirebaseCredentials`,
  `DS__HistoricRegistration__Year`, valgfrit
  `DS__HistoricRegistration__GroupMap`,
  `DS__HistoricRegistration__ExcludeSignupDocuments` og
  `DS__HistoricRegistration__ExcludeGroupNumbers`. Credentials og gruppekort
  accepteres både som JSON-indhold i miljøvariablen og som sti til en fil.
  Web-adgangen kræver `AdminAccess`, og endpointet afviser start, hvis
  credentials eller lejrår mangler. Jobbet køres af workeren med samme
  `DS__ConnectionString` som Website; Website sætter det kun i kø.
- **Ændret beslutning:** Et indbygget JSON-udtræk og et separat Python-eksporttrin
  er fravalgt. Importeren læser direkte fra Firestore med en credentials-fil
  angivet ved kørsel (`--firebase-credentials` eller `GOOGLE_APPLICATION_CREDENTIALS`)
  og et eksplicit `--year`. Google service-account-nøgler og Firebase-klient-JSON
  med `projectId`/`apiKey` understøttes; klientnøgler kræver læseadgang gennem
  eksisterende Firebase-regler. Credentials og historiske datafiler medfølger
  ikke image eller repository. Data normaliseres i hukommelsen, og hver kørsel
  læser kilden på ny.
- Grupper matches på entydigt præcist navn eller et eksplicit kort fra gammelt
  gruppenummer til eksisterende DS28-id. Ingen grupper oprettes.
- Forhåndstal og venskabsgruppetal bevares separat pr. gren. Endelige antal
  gemmes pr. gren med dag 0 som patruljesummer og dag 1–8 som tilmeldt fremmøde.
  Særskilte ledere uden dagsoplysninger har kun dag 0; manglende dagsrækker er
  ukendt, ikke nul. Familiespejdere bevares som egen gren.
- Ved undersøgelsen 2026-11-14 havde Firebase 16 forhåndstilmeldinger og 41
  endelige dokumenter. To tomme
  dokumenter uden gruppekobling udelades med registreret begrundelse. De øvrige
  39 vedrører 38 grupper og indeholder råt 285 deltagere uden ledere plus 111
  ledere. Disse summer er ikke et verificeret antal unikke personer.
- **Afklaret (2026-11-15):** Svendborg har to endelige dokumenter med
  henholdsvis 10 og 12 deltagere inklusive ledere. Den opdaterede tilmelding fra
  10. juli 2024 med 12 deltagere importeres; den oprindelige fra 26. maj med 10
  udelades via sit dokument-id. Importen summerer eller vælger ikke automatisk
  mellem dubletter.
- `TestGruppe` (123456) har 43 forhåndstilmeldte og 1 endeligt tilmeldt i
  rådata. Eksplicit udeladelse af et gruppenummer understøttes for begge tabeller;
  testgrupper frasorteres ikke automatisk.
- **Implementeret (DS-22, 2026-11-15):** Gruppevisningen under `/admin/groups`
  viser historiske forhåndstal (deltagere og venskabsgruppe) og endelige tal fra
  2024 pr. gren, med dag 0 adskilt fra dag 1–8. Manglende dagsrækker vises som
  ukendt, ikke nul, og der summeres ikke på tværs af dage. Eksisterende
  gruppeoplysninger vises fortsat ved siden af historikken.
- **Ændret beslutning og implementeret (DS-32, 2026-11-15):** Historikken
  vises i én kompakt tabel med forhåndstal, endelige patruljesummer (dag 0),
  ændring og totaler. Venskabsgruppetal kan tilvælges; dagsfremmøde for dag 1–8
  er foldet sammen fra start. Nuværende forhåndstilmelding er fjernet fra både
  panelet og historik-endpointets DTO/forespørgsel; det erstatter DS-22's visning
  af nuværende tal. Gruppens egen forhåndstilmelding er uændret.
  Manglende tal og ændringer vises som ukendt, og en total vises kun, når alle
  viste grene har tal for den pågældende kolonne. Der summeres aldrig på tværs
  af fremmødedage.
- Historikken hentes særskilt via `GET /api/v1/groups/{id}/registration-history`,
  som kræver både `GroupsView` og `AdminAccess`. Kun antal, gren, lejrår og
  dagsindeks returneres; kildeoplysninger udleveres ikke. Delte gruppe-DTO'er og
  gruppens egne `/api/v1/group`-endpoints indeholder hverken historik,
  historikflag eller tællere. UI'et henter kun historik med `AdminAccess`.
- **Produktionskonfiguration (2026-11-15):** `FirebaseCredentials` og
  `Year=2024` er sat i produktion, så jobbet kan startes fra Admin-siden.
  Kildens gruppenavne mangler endelsen ` Gruppe`, som DS28 bruger, og dens
  gruppenumre er identiske med DS28's gruppe-id'er. `GroupMap` kobler derfor de
  19 grupper, der har data, direkte på nummer. De 18 landenavne (1001–1017 og
  1111) og `TestGruppe` (123456) udelades via `ExcludeGroupNumbers`; landetallene
  er tomme i kilden, og testgruppen er testdata. Gruppe 3607 (Sct. Knuds) er
  oprettet i DS28 med distriktet FIONIA, fordi kilden har 13 endelige deltagere
  til gruppen, og historiktabellerne kræver en eksisterende gruppe. Den fremgår
  derfor af gruppelisten, indtil den eventuelt slettes igen; en ny import kræver,
  at gruppen stadig findes.
- Produktionsimport er ikke udført. Siden DS-71 bruger tilmeldingsstatistikken
  i Admin de importerede tal som måltal, så den viser intet mål, før importen er
  kørt i produktion.
  Øvrige gamle datatyper er uden for denne første import.

Kørsel, databetydning og dubletoplysninger er beskrevet i
[importvejledningen](../tools/historic_registration/README.md).
