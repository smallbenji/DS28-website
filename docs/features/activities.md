# Aktiviteter

## Status

Implementeret: aktivitetsteams, aktivitetsdashboard, katalogtekst, filer i
Google Drev og intern godkendelse. Besluttet, ikke bygget: udgivelse i
kataloget, gruppernes aktivitetsønsker og -tilmelding, opgaver på
aktivitetsdashboardet og interface til budgettildeling.

## Nuværende adfærd

### Teams og adgang

- Aktiviteter tilhører et aktivitetsteam. Et teammedlemskab med `IsAdmin` eller
  den globale `ActivityAdmin` giver redigeringsadgang. Øvrige teammedlemmer har
  læseadgang og kan se status og feedback.
- `/activity` viser for `ActivityAdmin` et arbejdsbord med nøgletal (afventende,
  tilbagesendte, godkendte, alle), søgning, team- og statusfiltre og en
  sorterbar liste med team, status, budget og indsendelsesdato. Andre ser
  deres teams med medlemmer, aktiviteter og oprettelse af team.
  `/activity/teams/:id?` er teamstyringen og kræver `ActivityAdmin`.

### Aktivitetsdashboard

- `/activity/:id` har fanerne Overblik, Katalog og Filer. Overblikket viser
  katalogtitel, resumé, uddrag af beskrivelsen, tildelt budget i kroner og op
  til seks genveje til filer og mapper i aktivitetens Drev-mappe.
- Navnet redigeres ved sidetitlen. Navn og katalogtekst redigeres lokalt og
  gemmes med »Gem aktivitet«, som kun vises ved ændringer. Filer og mapper
  gemmes straks, se [files-google-drive.md](files-google-drive.md).
- Mærket »Indhold tilføjet« eller »Mangler indhold« er kun en
  indholdsindikator, ikke en godkendelse.
- Budgettet er skrivebeskyttet og kan ikke ændres gennem
  aktivitetsopdateringen.

### Godkendelse

- Statusforløbet er `Draft` (Kladde), `Submitted` (Afventer godkendelse) og
  `Approved` (Godkendt). En indsendt aktivitet kan sendes retur som
  `ChangesRequested` (Ændringer ønskes) og genindsendes.
- Indsendelse kræver navn, katalognavn, resumé og beskrivelse, valideret på
  serveren, og samme adgang som redigering. Godkendelse og tilbagesendelse
  kræver `ActivityAdmin`. Tilbagesendelse kræver en begrundelse på højst 4.000
  tegn.
- Seneste vurdering gemmes med tidspunkt, bruger og feedback. Feedback
  bevares under rettelser og ryddes ved godkendelse.
- En faktisk ændring af navn eller katalogindhold (også katalogbillede) sender
  en indsendt eller godkendt aktivitet tilbage til Kladde. Rettelser i en
  tilbagesendt aktivitet bevarer Ændringer ønskes. Ændringer i Google-filer
  påvirker ikke godkendelsen.
- `review_version` er concurrency-token. En forældet version giver 409 ved
  gemning og statusændringer. Reviewfelter kan ikke sættes gennem den
  almindelige opdatering.
- Godkendelse udgiver ikke aktiviteten i kataloget.
- Endpoints: `POST /api/v1/activity/activity/{id}/submit`, `/approve` og
  `/request-changes` med `reviewVersion` (og `feedback`); `PUT` på aktiviteten
  kræver også `reviewVersion`.

## Beslutninger

- 2026-10-15: Filer i Google Drev pr. aktivitet, se
  [files-google-drive.md](files-google-drive.md).
- 2026-10-16: Aktivitetsdetaljen starter med et dashboard med faner. Afløser
  venstremenuen og det deaktiverede budgetinput.
- 2026-10-16: Indsendelse, godkendelse og begrundet tilbagesendelse.
  Godkendelse og katalogudgivelse er adskilt. Budget fjernes fra den
  almindelige aktivitetsopdatering.
- 2026-10-16: Opgaver på aktivitetsdashboardet udskydes efter brugerens ønske.
- 2026-11-21, DS-17: Aktivitetens budget skal flyttes ind i økonomitræet, se
  [finance.md](finance.md).

## Åbne spørgsmål

- Udgivelse i kataloget og offentlig visning er ikke besluttet.
- Hvilke felter må en aktivitets-teamleder ændre, og må teamlederen kun se
  egne aktiviteter?
- Hvordan aktivitetstilmeldingen skal bruge lejrdage (blokere, advare eller kun
  tilmelde tilstedeværende), se [groups-registration.md](groups-registration.md).
  Hensigten om, at en patrulje ikke tilmeldes på dage, hvor den ikke er på
  lejren, står uændret.
- Massehandlinger, fuld historik og notifikationsmail ved godkendelse er
  udskudt.

## Kodehenvisninger

- `src/DS.Application/Services/Activities/ActivityService.cs`
- `src/DS.Application/Services/Activities/ActivityTeamService.cs`
- `src/DS.Backend/Controllers/ApiControllers/ActivityApiController.cs`
- `src/DS.Domain/Migrations/021_activity_review.sql`
- `src/DS.Frontend/src/Views/ActivityDashboard.vue`
- `src/DS.Frontend/src/Views/ActivityDetail.vue`
- `src/DS.Frontend/src/Views/TeamManagement.vue`
- `src/DS.Frontend/src/Components/Activity/ActivityReviewPanel.vue`
- `src/DS.Frontend/src/lib/activityReview.ts`
