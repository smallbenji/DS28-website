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
konfiguration samles i `DS.Website/Extensions/ServiceExtensions.cs` og kaldes
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
ligger på `/hangfire` og kræver `AdminAccess`. Hangfire-schemaet ejes udelukkende
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
`canSubscribe = true`. Den første og eneste type er `NewUser`, som kræver
app-rollen `UsersView` (SysAdmin og CampAdmin). Afsendelsen sker centralt i
`NotificationUserManager`, en `UserManager<User>`-subklasse der overskriver
`CreateAsync(User)`, så alle oprettelsesveje fanges — selvbetjening,
admin-oprettelse, invitation, gruppe-invitation og gruppe-forhåndstilmelding.
Efter et vellykket kald enqueuer `EmailService.QueueNewUserNotificationMail`
en mail til hver bruger med præferencen slået til. Den tidligere faste
modtager `DS__NewUserNotificationEmail` er dermed fjernet. Som de øvrige mails
gate'er `NotificationsEnabled` på enqueue-trinnet.

Ubesluttede punkter:
- Præferencevalget er kun implementeret for `NewUser`. Flere hændelsestyper og
  tilhørende rollegating er ikke fastlagt. Afmelding sker ved at fjerne krydset
  i profilen; der findes ingen separat afmeldingsmulighed.

### Min profil

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
  aktiveres ikke. Redirect-URL'er kræver HTTPS, undtagen HTTP på loopback, og
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
der sammenligner forhåndstilmelding og endelig tilmelding mod måletallet fra
`DS/ParticipantData.cs` (`TotalUniqueParticipants`, 285).

Tærsklerne er: grøn når målet er nået, gul når mindst halvdelen er nået, rød
under halvdelen. Tallet er hærdet som halvdelen af målet, så en tærskel ikke
skal vedligeholdes separat.

De to tal har forskellige kilder og er ikke koblet til hinanden:
- Forhåndstilmelding summerer `GroupPreSignup` for alle grupper og aldersgrupper,
  altså gruppernes erklærede antal.
- Endelig tilmelding tæller rækker i `Scout`, altså navngivne deltagere.

Uafklaret: sidebaren skal på sigende og senere rumme de åb/luk-funktioner, der
hidtil ligger på `/camp-settings`.

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
  adgangen påvirkes ikke. Overstillingen vælges i en flyt-modal, hvor
  understillinger af den aktuelle stilling er filtreret fra.
- Autoritet følger hele undertræet rekursivt: at beklæde en overstilling giver
  håndteringsret til stillinger på alle niveauer under den. `CanManageParentAsync`
  og frontend-guardet i `orgAccess.ts` accepterer en indehaver af en hvilken som
  helst stilling i forældrekæden, ikke kun af den umiddelbare overstilling.
  SysAdmin kan håndtere hele træet; rodstillinger håndteres kun af SysAdmin.
- Diagrammet tegnes af en rekursiv `OrgNode.vue`; der bruges ingen ekstern
  org-chart-komponent. Hver node er et klikbart kort, og redigering, grupper,
  indehavere, invitation, flytning, oprettelse af understillinger og sletning
  sker i en fælles skuffe (`OrgDrawer.vue`) baseret på `BSidebar`, der glider
  ind fra højre, i stedet for knapper på hver node. Siden ligger på `/org` med
  en "Organisation"-flis på hjemmesiden for brugere med `OrgView` og er ikke
  længere en del af adminområdet.
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
  sårbarheder; den er derfor pinnet til 10.0.13 i `DS.Website.csproj`, så
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
udtræk er en `DataExport`-klasse under `DS.Website/Exports/`, som registreres som
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
- `ParticipantData` er hardkodet placeholder-data og eksporteres ikke, fordi
  tallene ikke er rigtige.
- Der logges ikke, hvem der har hentet et udtræk. Det aktuelle udtræk indeholder
  kun tal, men kommende udtræk med persondata bør overveje det.
- Backend manglede hidtil danske visningsnavne på enums; de ligger nu i
  `DS.Website/Labels.cs`. Frontend har stadig egne kopier i `group.types.ts` og
  `GroupsScouts.vue`, som på sigt bør læse fra den fælles kilde.

### Auditlog – nice to have

Det skal eventuelt være muligt at se, hvem der har ændret hvad. Den planlagte
adgang er begrænset til systemadministrator og lejrchef.

**Beslutning 2026-09-29:** `AuditLogView` er nu tildelt systemadministrator og
lejrchef, så adgangen matcher den planlagte. Auditloggen er endnu hverken
implementeret eller synlig i adminområdet.

### Projektstyring og Kanban-board

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
  særskilt læserolle. Sletning følger samme medlemsadgang.
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
  følger indholdets højde med "Tilføj kort" direkte under kortene.
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

Ubesluttede punkter:
- Hvem må rydde døde links, og skal et kort blokere sletning af et
  domæneobjekt eller blot miste linket?
- Skal et board senere kunne generere kort automatisk fra en forespørgsel
  ("smart board"), fx aktiviteter uden budget, via domænehændelser/Hangfire?

## Sikkerhed og auth-hærdning

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
- Buefy til frontend-komponenter.
- Hosting: Smallhosting.
- Services: Grafana og Prometheus.
- CMS-valget var eksempelvis WordPress.

Dette afsnit dokumenterer planen og bekræfter ikke den aktuelle hosting eller
driftsopsætning.

### Frontend-build og typekontrol

**Midlertidig beslutning 2026-10-02:** `npm run build` i `DS.Website/js`
kører kun `vite build` og udelader `vue-tsc -b`. Årsagen er en
afhængighedskonflikt: `vue-tsc` 3.x løser `typescript/lib/tsc`, og
TypeScript 7 eksporterer ikke længere det subpath, så buildet fejlede med
`ERR_PACKAGE_PATH_NOT_EXPORTED`, før Vite overhovedet kørte.

Det er en midlertidig løsning, ikke en permanent afvejning af typekontrol.
Frontendens typer kontrolleres derfor ikke længere som del af buildet.
`vue-tsc` er fortsat en devDependency, og scriptet `npm run typecheck`
(`vue-tsc -b`) er tilføjet som en manuel kontrol – men det fejler stadig med
samme `ERR_PACKAGE_PATH_NOT_EXPORTED`, ind afhængighedskonflikten er løst.
Beslutningen skal genovervejes, når `vue-tsc` understøtter TypeScript 7;
alternativt kan TypeScript nedgraderes til 5.x og typekontrollen føjes tilbage
i buildet.

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
`updated_at` ved opdateringer og soft-delete-filtrering er ikke implementeret.
Outboxens dengang eksisterende indeks på næste forsøg og id blev bevaret ud
over de indekser, SQL-planen angiver; tabellen er senere fjernet
(beslutning 2026-10-06). EF genererer fortsat egne indeks- og
constraint-navne samt konventionsbaserede fremmednøgleindekser.

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
- Frontend bruger Vue/Vite og Buefy.
- Globale roller og rettigheder ligger i `DS.Website/Roles.cs`. Planens roller
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
