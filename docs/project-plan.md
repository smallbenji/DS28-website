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

- Systemansvarlig: Benjamin Falch, +45 25755838.
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

### Auditlog – nice to have

Det skal eventuelt være muligt at se, hvem der har ændret hvad. Den planlagte
adgang er begrænset til systemadministrator og lejrchef.

## Teknologi og planlagt drift

- ASP.NET Core og Vue/Vite.
- ASP.NET Core Identity og OpenIddict til identitet og login.
- Buefy til frontend-komponenter.
- Hosting: Smallhosting.
- Services: Grafana og Prometheus.
- CMS-valget var eksempelvis WordPress.

Dette afsnit dokumenterer planen og bekræfter ikke den aktuelle hosting eller
driftsopsætning.

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
  HQ. Her ligger en Buefy-toggle til at åbne og lukke forhåndstilmeldingen.
  Den nuværende adgang er givet til systemadministrator og lejrchef gennem
  `PreSignupManage`.
- Åben/lukket gemmes i databasen og styres manuelt, uafhængigt af planens datoer.
  Indstillingen starter som åben ved migrering for at bevare den hidtidige adgang.
- Den foreløbige fortolkning af lukket forhåndstilmelding er, at både nye
  tilmeldinger og ændringer af deltagerantal blokeres i backend. Eksisterende
  tilmeldinger kan fortsat ses. Om eksisterende grupper skal kunne ændre deres
  tal efter lukning, er endnu ikke bekræftet af Benjamin.

Nye beslutninger kan føjes til dette afsnit, så den oprindelige plan fortsat
kan skelnes fra senere valg.
