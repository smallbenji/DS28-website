# Stillinger og organisationsdiagram

## Status

Implementeret. Siden ligger på `/org` og kræver `OrgView`.

## Nuværende adfærd

### Model og adgang

- Noderne i diagrammet er **stillinger**, ikke personer. En stilling har en
  titel, en overstilling, en valgfri farve og 0..N `AppGroups`, og den kan
  beklædes af flere brugere. En bruger kan beklæde flere stillinger. Tabellerne
  (`org_position`, `org_position_group`, `org_position_holder` og
  `user_invitation.org_position_id`) står i [database.md](../database.md).
- Diagrammet viser kun brugere med en stilling. Spejdergrupper og brugere, der
  kun er knyttet til en spejdergruppe, indgår ikke.
- `AppRoles` er rettigheder, `AppGroups` kode-ejede adgangsbundter, og
  stillingen er data-ejet. To stillinger kan dele en gruppe, og titlen behøver
  ikke matche gruppen.
- Effektive grupper = direkte Identity-roller plus grupper fra alle
  stillinger, brugeren beklæder, opløst dynamisk i `ClaimsTransformer` og
  `AppClaimsPrincipalFactory` via `OrgPositionAccess`. Ændres en stillings
  grupper, ryddes rollecachen for indehaverne og for deres personlige
  assistenter.
- `OrgView` giver læseadgang til `/api/v1/org`. Mutationer afgøres af
  stillingsautoriteten, ikke af `OrgManage` alene.

### Autoritet

- At beklæde en stilling giver ret til at oprette, ændre, invitere og tildele
  i hele undertræet under den, på alle niveauer (`CanManageParentAsync` og
  `orgAccess.ts`). Rodstillinger oprettes og håndteres kun af SysAdmin.
- Invariant: en ikke-SysAdmin kan kun give en stilling grupper eller invitere
  med grupper, som er en delmængde af egne effektive grupper. SysAdmin kan
  give vilkårlige grupper, så en stilling er ikke strukturelt begrænset til sin
  forælder.
- Systemstillingen "Systemadministrator" (`is_system`, seedet med gruppen
  `SysAdmin`) kan ikke slettes, omdøbes, ændre grupper eller miste sin sidste
  indehaver. Kun en SysAdmin kan tildele, fjerne eller flytte den.
  `AppGroups.SysAdmin` kan ikke indgå i andre stillinger. Direkte
  `SysAdmin`-tildeling er fortsat roden af tillid.
- En stilling med understillinger eller indehavere kan ikke slettes.
- Flytning (`PUT /api/v1/org/{id}/parent`) kræver autoritet over både nuværende
  og ny overstilling og har cirkelkontrol. Grupperne ændres ikke ved flytning.

### Personlig assistent

- En personlig assistent er en stilling med titlen "Personlig assistent",
  gruppen `PersonalAssistant` (giver `OrgView`, ikke `OrgManage`) og
  `pa_for_position_id` sat til ejerstillingen. Højst én pr. stilling.
- Assistenten arver ejerstillingens grupper, kan ikke have understillinger,
  kan ikke ændres eller flyttes og soft-slettes sammen med ejerstillingen. Den
  kan beklædes og give invitationer.

### Invitationer og visning

- En stillingsinvitation gælder nye brugere. Eksisterende brugere tilknyttes
  direkte. Ved accept oprettes indehaveren, og rollecachen ryddes.
- `MeDto.Title` viser stillingens titel. Brugerstyringen viser en brugers
  stillinger via `UserDto.Positions`.

### Brugerflade

- Diagrammet tegnes rekursivt af `OrgNode.vue` uden ekstern komponent.
  Stillingens farve vises som en kant i toppen og en svag tone; assistenter
  vises med stiplet kant og arver ejerens tone.
- Et plus under en node opretter en understilling med titel, farve og grupper,
  kun hvor brugeren må.
- Skuffen (`OrgPositionDrawer.vue`) har fanerne Stilling, Personer og Struktur
  og viser stillingens placering som links. Sletning ligger i en menu og er
  slået fra med forklaring, når backendens regel forhindrer den.
- Flytning sker med træk og slip i diagram og liste eller med en søgbar
  trævælger. Kun gyldige mål tilbydes, og flytningen kan fortrydes fra beskeden.
- Søgning på stilling eller person folder grene ud. Der er zoom, "tilpas til
  vinduet", sammenfoldning og en listevisning. På smalle skærme åbner siden i
  listevisning, skuffen glider op nedefra, og træk og slip er slået fra.
- Logikken ligger i `lib/org/orgTree.ts` og `lib/org/orgChartContext.ts`.

## Beslutninger

- 2026-10-12: Organisationsdiagram af stillinger med `AppGroups`, dynamisk
  adgang, autoritet efter træet og invariant om ingen eskalering. Gruppernes
  danske titler flyttes til stillingerne, så `AppGroups` kun betyder adgang.
- 2026-10-12: `OrgView` og `OrgManage` gives til alle grupper undtagen
  `FoodAdmin` og `User`. Ny gruppe `PersonalAssistant`.
- 2026-10-12: Autoritet gælder hele undertræet, ikke kun umiddelbare børn.
  Systemstillingen kan flyttes af SysAdmin.
- 2026-10-19: Stillingslogikken ligger i `OrgService` i Application.
- 2026-10-20: Soft delete af stillinger. Det unikke indeks for assistenter er
  partielt, og `OrgService` soft-sletter assistenten med ejeren, så den
  tidligere `ON DELETE CASCADE` bevares.
- 2026-11-22, DS-89: Ny brugerflade efter godkendt mockup med oprettelse fra
  noden, faner i skuffen, træk og slip og listevisning. Afløser én samlet
  skuffe og en flyt-modal. Rettighederne er uændrede.

## Åbne spørgsmål

- Skal en stilling kunne give ret til at redigere træet ud fra placeringen
  alene, uden `OrgView`/`OrgManage`?
- Skal en stilling kunne beklædes af en person uden konto?
- Er der behov for stillinger uden `AppGroups` som rene organisationsnoder?
- Er listevisningen tilstrækkelig på mobil, og skal valget mellem diagram og
  liste huskes?

## Kodehenvisninger

- `src/DS.Application/Services/Organization/OrgService.cs`
- `src/DS.Application/Services/Organization/OrgPositionAccess.cs`
- `src/DS.Backend/Controllers/ApiControllers/OrgApiController.cs`
- `src/DS.Backend/ClaimsTransformer.cs`
- `src/DS.Domain/Migrations/011_org_positions.sql`
- `src/DS.Frontend/src/Views/OrgChart.vue`
- `src/DS.Frontend/src/Components/Org/OrgNode.vue`
- `src/DS.Frontend/src/Components/Org/OrgPositionDrawer.vue`
- `src/DS.Frontend/src/lib/org/orgAccess.ts`
- `src/DS.Frontend/src/lib/org/orgTree.ts`
- `src/DS.Frontend/src/lib/org/orgChartContext.ts`
