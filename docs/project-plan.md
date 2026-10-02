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

Brugere skal eventuelt kunne få besked om hændelser, eksempelvis når nogen
opretter en patrulje eller ændrer deltagerantal. Dette blev primært tænkt som
en "Ole-feature"; målgruppe, kanaler og regler er ikke nærmere fastlagt.

**Beslutning 2026-09-29:** Notifikationer starter som **mail**. En `EmailOutbox`
fungerer som transaktionel outbox og en `EmailOutboxWorker` sender fra køen i
baggrunden. Adgang til at se køen (`EmailOutboxView`) er givet til
systemadministrator og lejrchef, via `/admin`.

Ubesluttede punkter:
- Notifikationer skal kun sendes til brugere der har bedt om den pågældende
  hændelsestype. Præferencer er endnu ikke implementeret, og der findes ingen
  afmeldingsmulighed endnu.
- Transaktionelle mails (invitation og adgangskodenulstilling) skal fortsat
  sendes synkront i forbindelse med den handling, brugeren har bedt om. De må
  ikke gå gennem køen, fordi brugeren skal kunne få en fejl, hvis afsendelsen
  mislykkes.
- Der er endnu ingen konkret hændelse, der lægger rækker i køen.

### Adminområde

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

### Aktiviteter

- Registrerede aktiviteter kan udgives i et katalog.
- Grupper kan tilmelde sig, når aktivitetstilmeldingen åbner.
- En patrulje må ikke tilmeldes aktiviteter på dage, hvor den ikke er på lejren.
- Aktivitetens materialebehov og øvrige metadata registreres her og bruges af
  de andre dele af systemet.

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

### Dataudtræk

Planen under Økonomi nævner eksport af deltagerantal pr. dag og pr. gruppe, men
beskriver ikke hvordan udtræk skal defineres eller hvem der må hente dem.

**Beslutning 2026-09-29:** Dataudtræk defineres udelukkende i backend. Hvert
udtræk er en `DataExport`-klasse under `DS.Website/Exports/`, som registreres som
scoped service i `Program.cs`. Frontend henter listen fra `GET /api/v1/exports`
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

## Teknologi og planlagt drift

- ASP.NET Core og Vue/Vite.
- ASP.NET Core Identity og OpenIddict til identitet og login.
- Buefy til frontend-komponenter.
- Hosting: Smallhosting.
- Services: Grafana og Prometheus.
- CMS-valget var eksempelvis WordPress.

Dette afsnit dokumenterer planen og bekræfter ikke den aktuelle hosting eller
driftsopsætning.

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
Outboxens eksisterende indeks på næste forsøg og id er bevaret ud over de
indekser, SQL-planen angiver. EF genererer fortsat egne indeks- og
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
- Gruppeinvitationer oprettes som personlige links, som medlemmer selv deler.
  Der sendes ikke automatisk email i dette flow.
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
  automatisk, i modsætning til gruppeinvitationer ovenfor, hvor Benjamin
  bevidst har valgt, at brugerne selv deler linket. Dette er altså en ny
  afklaring og ikke en udvidelse af invitationflowet.
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
