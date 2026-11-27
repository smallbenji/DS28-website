# DS28 - projektplan og beslutningsindeks

## Status og brug

**Planen er foreløbig og ikke endegyldig, før den er vedtaget.**

Dette dokument er indgangen til projektets planlægning. Det gengiver den
projektplan, Benjamin begyndte udviklingen ud fra, og peger på de filer, hvor
senere beslutninger, nuværende adfærd og åbne spørgsmål står. Det er et udkast,
ikke en vedtaget specifikation og ikke en bestilling på at bygge alle nævnte
funktioner.

- Den aktuelle opgave og eksplicitte senere beslutninger afgør scope. Det
  oprindelige udkast nedenfor viser hensigten, men udvider ikke en opgave.
- Kontrollér altid koden, før du antager, at en planlagt funktion findes. Når
  plan og kode afviger, skelnes mellem tilsigtet og nuværende adfærd.
- Beslutninger står i filen for deres område, se
  [beslutningsindekset](#beslutningsindeks). Hver fil adskiller nuværende
  adfærd, beslutninger og åbne spørgsmål.
- Databaseskemaet står i [database.md](database.md), og arbejdsgangen for
  kort, pull requests og udgivelse står i [AGENTS.md](../AGENTS.md).
- Git-historikken bevarer tidligere versioner af planen, herunder afløste
  beslutninger og ændringslog.

Systemansvarlig: Benjamin Falch. Foreslået vikar: Milas Holsting, ikke vedtaget.

## Formål

Flere mindre systemer skal arbejde sammen om at drive Distriktssommerlejr 2028
digitalt. Et samlet dashboard, HQ, samler apps, så staben ikke behøver at
kende de enkelte URL'er.

Fælles brugeradministration for grupper og stab bygger på ASP.NET Core Identity
og OpenIddict. Keycloak indgik i det oprindelige udkast, men er droppet efter
Benjamins beslutning og er ikke en kommende integration. Et gruppenummer
forbinder en bruger med en spejdergruppe, og flere brugere kan tilhøre samme
gruppe.

## Det oprindelige udkast

Afsnittet gengiver planen, som den så ud, før udviklingen gik i gang. Det er
ikke en beskrivelse af det, der er bygget. Se feature-filerne for nuværende
adfærd.

### Roller

| Rolle | Ønsket adgang og ansvar |
| --- | --- |
| Systemadministrator | Adgang til hele systemet og ekstra rettigheder til at ændre kritiske data. |
| Lejrchef | Adgang til hele systemet, men ændringer i kritiske data kræver en systemadministrator. Skal kunne eksportere en liste over registrerede emailadresser. |
| Økonomi | Eksport af forbrug og forventede deltagerantal. Læseadgang til aktiviteter. |
| Materialeansvarlig | Læseadgang til aktiviteter, redigering af materialelister og eksport både pr. aktivitet og som samlet masterark. |
| Aktivitetsansvarlig | Adgang til og mulighed for at ændre alle aktiviteter. |
| Aktivitets-teamleder | Opretter aktiviteter, redigerer næsten alle felter og giver medlemmer adgang. Står som ejer af en aktivitet og tilføjer medlemmer til systemet. |
| Aktivitets-teammedlem | Får adgang af teamlederen til at ændre tildelte aktiviteter. |
| Gruppe | Tilmelder gruppen til lejren, opretter patruljer og tilmelder dem aktiviteter. Brugeren er tilknyttet et gruppenummer. |

De implementerede roller og rettigheder står i `src/DS.Domain/Roles.cs` og
er beskrevet i [Adgang og brugere](features/access-and-users.md). Udkastets
rollebeskrivelser er ikke bevis for, at rettighederne findes.

### Funktioner

- **CMS og offentlig hjemmeside:** hjemmesiden bygges i et CMS, eksempelvis
  WordPress, med rollebaseret adgang, så PR, IT og eventuelt andre kan
  redigere indhold uden hjælp fra IT.
- **Gruppelogin:** grupper modtager en email med et link til
  brugeroprettelse, og brugeren kobles automatisk til gruppenummeret. Flere
  personer fra samme gruppe kan have hver sin konto.
- **Notifikationer (nice to have):** brugere kan eventuelt få besked om
  hændelser, fx når nogen opretter en patrulje eller ændrer deltagerantal.
- **Aktiviteter:** aktiviteter kan udgives i et katalog, grupper tilmelder
  sig, når aktivitetstilmeldingen åbner, og en patrulje må ikke tilmeldes på
  dage, hvor den ikke er på lejren. Materialebehov og metadata registreres her.
- **Økonomi:** budgetter til aktiviteter, eksport af forbrug og eksport af
  deltagerantal pr. dag og pr. gruppe. Fakturering til grupper skulle
  undersøges.
- **Materialer:** ved oprettelse af materialelister vises materialer, andre
  aktiviteter har bestilt. Materialer reserveres kun på dage med moduler, og
  genbrug på tværs af aktiviteter skal reducere indkøb (fx fra 15 til 10
  økser). Stationære materialer undtages fra turnus. Materialeansvarlige skal
  hver aften kunne se, hvordan materialer flyttes til næste morgen, og første
  udskrift tager udgangspunkt i dag 1.
- **Auditlog (nice to have):** se, hvem der har ændret hvad, for
  systemadministrator og lejrchef.

### Teknologi og drift

ASP.NET Core og Vue/Vite, ASP.NET Core Identity og OpenIddict. Udkastet nævner
hosting hos Smallhosting og Grafana og Prometheus som services. Det er ikke
bekræftet som den aktuelle drift. Den nuværende opsætning står i
[architecture.md](architecture.md) og i afsnittet Deployment i
[AGENTS.md](../AGENTS.md).

### Foreløbig tidsplan

| Dato | Milepæl |
| --- | --- |
| 18. oktober 2027 | Forhåndstilmelding åbner. Den oprindelige tekst angiver samtidig "(nov)". |
| 30. november 2027 | Forhåndstilmelding lukker. |
| 1. marts 2028 | Endelig tilmelding åbner. |
| 31. marts 2028 | Endelig tilmelding "lukker". |
| 1. april 2028 | Aktivitetstilmelding åbner. |

Datoerne er foreløbige og styrer ikke adgang i koden. Tilmeldinger åbnes og
lukkes manuelt med flag, se [Grupper og tilmelding](features/groups-registration.md).

## Åbne spørgsmål

Spørgsmålene er samlet her i kort form. Detaljerne står i den linkede fil.

**Planlægning og domæne**

- Hvad betyder "kritiske data", og hvilke felter må en aktivitets-teamleder
  ændre? Må teamlederen kun se egne aktiviteter?
  ([activities.md](features/activities.md))
- Hvad betyder det, at en tilmelding "lukker", og hvad må grupper stadig
  gøre? 18. oktober eller "(nov)" for forhåndstilmelding?
  ([groups-registration.md](features/groups-registration.md))
- Skal flaget for endelig tilmelding gate siden `/group/final-signup`?
  ([groups-registration.md](features/groups-registration.md))
- Hvad betyder lukket endelig aktivitetstilmelding (DS-92), og bekræftes
  standardvalgene for de nye flag?
  ([groups-registration.md](features/groups-registration.md))
- Regler fra deltagelsesdage til pris, ledere og hjælperes deltagelse og
  kassererens øvrige behov (DS-79, DS-80, DS-97, DS-4).
  ([finance.md](features/finance.md))
- Hvem må tilgå og redigere den offentlige hjemmeside/CMS'et? (oprindeligt
  udkast)

**Adgang og brugere**

- Bekræftelse af DS-65's standardvalg for "Ny tavle oprettet" og flere
  notifikationstyper. ([access-and-users.md](features/access-and-users.md))
- Uafklarede punkter om stillinger: redigering af træet ud fra placering,
  stillinger uden konto eller uden grupper, mobilvisning.
  ([organization.md](features/organization.md))
- Rollevalget for admins visning og tilbagekaldelse af adgangstokens (DS-91),
  og om Bearer-adgang skal kræve `McpUser`. ([security.md](security.md))
- PKCE pr. klient, rate limit-grænsen og adgangskodepolitikken.
  ([security.md](security.md))
- Om kortbeskrivelser skal renses for HTML i backenden.
  ([security.md](security.md))

**Funktioner**

- Board: oprydning af døde domænelinks, "smart boards", visning af hvem der
  klarmeldte for en anden, og om kortdialogen også skal ændre adressen.
  ([boards.md](features/boards.md))
- Google Drev: konfiguration, skabeloner, omdøbning, deling og udløbne tokens.
  ([files-google-drive.md](features/files-google-drive.md))
- Katalogbilleder: rydning, uploadrettigheder og sletning af billeder i brug.
  ([files-google-drive.md](features/files-google-drive.md))
- Admin: om sidebaren skal rumme åbn/luk-funktionerne, om endelig tilmelding
  tæller ledere, og om deltagerudtrækket med navne er godkendt.
  ([admin-exports.md](features/admin-exports.md))
- Om det arkiverede frontend skal slettes, og om der skal skrives en
  designguide for Nuxt UI-frontenden. ([frontend.md](features/frontend.md))
- Adgang til fremtidige formularskabeloner. ([forms.md](features/forms.md))
- Materialesystemet og auditloggen er ikke bygget. Om og hvornår de skal
  bygges, er ikke besluttet. ([admin-exports.md](features/admin-exports.md))

## Beslutningsindeks

| Område | Fil | DS-kort |
| --- | --- | --- |
| Projekter, lag, DI, Worker, migrationer, frontend-build | [architecture.md](architecture.md) | DS-19, DS-116 |
| Login, OpenIddict, antiforgery, adgangstokens, hærdning | [security.md](security.md) | DS-36, DS-91 |
| Roller, brugere, impersonation, profil, login, notifikationer | [features/access-and-users.md](features/access-and-users.md) | DS-64, DS-65, DS-84, DS-85 |
| Stillinger og organisationsdiagram | [features/organization.md](features/organization.md) | DS-89 |
| Tavler og kort | [features/boards.md](features/boards.md) | DS-27, DS-35, DS-40, DS-50, DS-56 til DS-62, DS-87, DS-88, DS-99 til DS-104, DS-109, DS-119, DS-120, DS-121, DS-122 |
| MCP og CLI til tavler | [features/mcp.md](features/mcp.md) | DS-33, DS-43, DS-67, DS-102 |
| Aktiviteter og godkendelse | [features/activities.md](features/activities.md) | |
| Grupper, forhåndstilmelding, endelig tilmelding, lejrdage | [features/groups-registration.md](features/groups-registration.md) | DS-72, DS-92, DS-96 |
| Filer, billeder og Google Drev | [features/files-google-drive.md](features/files-google-drive.md) | |
| Adminområde, baggrundsjob, dataudtræk, auditlog | [features/admin-exports.md](features/admin-exports.md) | DS-70, DS-71, DS-83 |
| Økonomi | [features/finance.md](features/finance.md) | DS-17, DS-72 til DS-82 |
| Historiske tilmeldinger fra 2024 | [features/historic-registration.md](features/historic-registration.md) | DS-22, DS-32 |
| Formularskabeloner | [features/forms.md](features/forms.md) | |
| Frontend, navigation, søgning, PWA | [features/frontend.md](features/frontend.md) | DS-94 |
| Databaseskema | [database.md](database.md) | DS-117 |
| Kort, pull requests, udgivelse og deploy | [AGENTS.md](../AGENTS.md) | DS-24 |
| Opsætning og drift af produktiondeploy | [deploy.md](deploy.md) | DS-24 |
