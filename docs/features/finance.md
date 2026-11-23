# Økonomi

## Status

Besluttet, ikke bygget (DS-17). Første forudsætning, deltagelsesdage (DS-72),
er implementeret og beskrevet i [groups-registration.md](groups-registration.md).
Aktiviteternes budget ligger i dag i `activity_budget` og er skrivebeskyttet i
HQ.

## Nuværende adfærd

- Hver aktivitet kan have ét budget i `activity_budget`, som vises
  skrivebeskyttet på aktivitetsdashboardet. Der er ingen brugerflade til at
  tildele budgetter.
- Spejdernes deltagelsesdage registreres i den endelige tilmelding.
- Der er ingen økonomiroller, poster, priser eller faktureringsoversigt.
  `FinanceAdmin` ("Kasserer") findes som gruppe, men har ingen
  økonomirettigheder.
- `Material.Price` er `double` i modellen og gemmes som `numeric(10, 2)`.

## Beslutninger

- Oprindeligt udkast: tildel aktiviteter budgetter, eksportér forbrug og
  deltagerantal pr. dag og gruppe, og undersøg fakturering til grupper.
- 2026-11-21, DS-17: Økonomimodulet skal afløse lejrchefens manuelle regneark
  til budget og regnskab med struktureret data, ikke et regneark i HQ. Afløser
  afgrænsningen til aktivitetsbudgetter og udtræk af forbrug. Arbejdet er delt i
  DS-72 til DS-82.
  - Data er et træ med Indtægter og Udgifter øverst og brugeroprettede
    kategorier og underkategorier i flere niveauer. Kategorier hardkodes ikke.
  - Hver post har budget og realiseret som `decimal` i DKK. Tomt er ikke 0.
    Summer beregnes op gennem træet og gemmes ikke. En post kan kobles til en
    aktivitet.
  - `activity_budget` flyttes ind i træet, så aktivitetens budget har én
    kilde. Budgetændringer må ikke påvirke aktivitetens godkendelse.
  - Ingen import af regnearket. Lejrchefen flytter tallene manuelt, så
    redigering skal være hurtig (inline, tastatur, flytning og sortering).
  - Nye roller `FinanceView` og `FinanceManage` til lejrchef og kasserer.
    Kassereren får også `ActivityView` og `ExportsView`, ikke `ActivityAdmin`.
  - Deltagerpriser for hel uge, halv uge og dag. Deltagerbetaling beregnes som
    indtægt ud fra tilmeldte deltagere og priser.
  - Kassereren skal se, hvad hver gruppe skal faktureres for: antal pr.
    pristype og beløb pr. gruppe uden spejdernes navne. Excel-udtræk og selve
    faktureringen er ude af scope foreløbig. Afløser punktet om at undersøge
    fakturering.
  - Materialesystemet skal senere fodre forventet forbrug ind i økonomien
    (DS-82) og kræver, at `Material.Price` skiftes til `decimal`.
  - Ude af scope: bogføring, bilag, moms, bankafstemning, betalinger, udlæg,
    refusion og fakturering.
- 2026-11-23, DS-72: Spejdere uden deltagelsesdage tæller ikke med i
  deltagerbetaling og faktureringsoversigt, men vises som "N spejdere mangler
  dage" pr. gruppe (DS-80, DS-81).

## Åbne spørgsmål

- Reglen fra deltagelsesdage til pris (DS-79, DS-80).
- Registreres og betaler ledere og hjælpere (DS-97)?
- Særlige priser for ledere eller aldersgrupper?
- Kassererens øvrige behov (DS-4).

## Kodehenvisninger

- `src/DS.Domain/Migrations/003_activity.sql`
- `src/DS.Domain/Migrations/038_scout_camp_days.sql`
- `src/DS.Domain/Roles.cs`
- `src/DS.Application/Services/Activities/ActivityService.cs`
