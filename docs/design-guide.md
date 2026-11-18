# DS28 HQ – designguide

Guiden beskriver, hvordan brugerfladen i `src/DS.Frontend` skal bygges, så nye
skærme ligner de eksisterende. Den bygger på de mønstre, som allerede er mest
udbredt i koden (november 2026). Hvor koden i dag er uensartet, står det under
[Kendte afvigelser](#kendte-afvigelser): ny kode følger guiden, og gammel kode
rettes, når man alligevel arbejder i den.

Kort sagt:

1. Brug Buefy-komponenten, før du skriver din egen.
2. Brug de delte layoutkomponenter (`ManagementWrapper`, `Sidebar`, `Workspace`).
3. Brug Bulmas hjælpeklasser til afstand og tekst i stedet for egen CSS.
4. Al tekst er på dansk, kort og handlingsorienteret.

## Grundlag

| Del | Valg |
| --- | --- |
| Framework | Vue 3 med `<script setup lang="ts">` |
| Komponentbibliotek | Buefy 3 (Bulma) – importeres pr. komponent: `import { BButton, BField } from 'buefy'` |
| Ikoner | Font Awesome 7, solid (`fas`) er standard |
| Styles | SCSS i komponentens `<style lang="scss">` |
| Skrift | `"Segoe UI", Tahoma, Geneva, Verdana, sans-serif` (sat i `index.html`) |
| Tema | Kun lyst tema (`data-theme="light"`) |

Tilføj ikke et nyt UI-bibliotek eller CSS-framework uden en beslutning i
[projektplanen](project-plan.md).

## Farver

Brug Bulmas semantiske farver via `type="is-…"` frem for egne hex-koder.

| Type | Betydning | Brug til |
| --- | --- | --- |
| `is-primary` | Hovedhandling | Opret, tilføj, gem, bekræft i formularer |
| `is-success` | Gennemført / positiv status | Toasts efter gem, status-tags |
| `is-danger` | Destruktiv eller fejl | Slet, fjern, fejl-toasts, fejlnotifikationer |
| `is-warning` | Kræver opmærksomhed | Valideringsbeskeder, advarsler |
| `is-info` | Neutral information | Forklarende notifikationer |
| `is-light` | Nedtonet variant | Kombineres, f.eks. `is-danger is-light` til mindre farlige sekundærhandlinger |

Faste farver i layoutet:

| Rolle | Værdi |
| --- | --- |
| Sidebaggrund | `#d4d4d4` |
| Flader (kort, sidebar, workspace) | `#fff` |
| Hover på rækker | `#f7f8fa` |
| Valgt element i liste | baggrund `#ececec`, kant `#696969` |
| Sekundær tekst | `#6b7280` (eller klassen `has-text-grey`) |
| Overskrift i tom tilstand | `#4b5563` |
| Ikon i tom tilstand | `#cbd5e1` |
| Tynde kanter og skillelinjer | `rgba(0, 0, 0, 0.1)` (tabeller: `0.08`) |
| Fokusring og link-blå | `#3273dc` |

Skal du bruge en farve, som ikke står her, så genbrug Bulmas klasser
(`has-text-*`, `has-background-*`) før du indfører en ny hex-kode. Tavlernes
brugervalgte farver (`orgPalette.ts`, tavlepaletten) er data, ikke en del af
UI-paletten.

## Typografi

Brug Bulmas klasser, ikke egne skriftstørrelser.

| Element | Klasse |
| --- | --- |
| Sidetitel (i topbaren via `Back`) | `title is-5` – sættes automatisk fra routerens `pageTitle` |
| Overskrift i hero / workspace | `title is-3` med `subtitle is-6` |
| Sektionsoverskrift | `title is-5` eller `panel-heading` / `card-header-title` |
| Hjælpetekst og metadata | `is-size-7 has-text-grey` |
| ID'er, koder, tokens | `is-family-monospace` |

Brug `<h1>` én gang pr. side (det gør `Back`), og lad overskrifter følge
hinanden i rækkefølge.

## Afstand, kanter og skygger

- Afstand følger Bulmas skala: `mb-2`, `mb-3`, `mb-4`, `mt-4`, `px-4` osv.
  Standard mellem blokke er `mb-4`.
- Intern polstring i flader er `1rem`. Mellemrum mellem flader er `1rem`.
- Hjørneradius er `10px` på store flader (sidebar, workspace, kort, topbar) og
  `4px` på små elementer (knapper, fokusmarkering).
- Skygge på kort: klassen `ui-card` (`0 2px 8px rgba(0, 0, 0, 0.06)`). Brug ikke
  nye skyggevarianter.

## Sidelayout

Hver side har en route med `meta.pageTitle` og `meta.pageIcon`. Så viser `App.vue`
automatisk topbaren (`Back`) med tilbageknap, titel, søgning og brugermenu.

```ts
{
    path: '/admin/groups/:id?',
    component: () => import('@/Views/Groups.vue'),
    meta: { requiresGroupsData: true, pageTitle: "Gruppestyring", pageIcon: "users-gear" }
}
```

Vælg en af tre sidetyper:

### 1. Liste og detalje (administration)

Bruges når man vælger et element i en liste og redigerer det. Reference:
`Views/Groups.vue`.

```vue
<ManagementWrapper :class="{ 'has-selection': selected != null }">
    <Sidebar>
        <SidebarHeader>
            <BInput v-model="searchQuery" icon="magnifying-glass" placeholder="Søg på navn" />
        </SidebarHeader>
        <SidebarContent>
            <!-- én *SidebarBox pr. element -->
        </SidebarContent>
        <SidebarFooter>
            <!-- opret-knap -->
        </SidebarFooter>
    </Sidebar>
    <Workspace :filled="selected != null">
        <button class="mobile-back" @click="clearSelection">
            <font-awesome-icon icon="arrow-left" />
            <span>Tilbage</span>
        </button>
        <WorkspaceContent>
            <!-- paneler i columns -->
        </WorkspaceContent>
        <WorkspaceFooter>
            <BButton type="is-primary" @click="save">Gem</BButton>
        </WorkspaceFooter>
    </Workspace>
</ManagementWrapper>
```

- Det valgte element styres af routen (`router.replace`), så det kan linkes og
  genindlæses.
- Søgefeltet i sidebaren kan fokuseres med Ctrl/Cmd+K.
- Under 768 px vises enten listen eller detaljen, og `mobile-back` fører tilbage.
- Når intet er valgt, er workspacet stiplet. Brug evt. `workspace-empty` til at
  forklare, hvad man skal gøre.

### 2. Flisemenu (oversigt)

Forsiden og adminområdet bruger `.grid` med `.link-box`-fliser: et stort ikon og
en kort tekst. Eksterne links får ikonet `up-right-from-square` i klassen
`external-icon`. Under 1024 px bliver fliserne til en liste med pil.

### 3. Indholdsside

Enkle sider (profil, indstillinger, formularer) lægger indholdet i `ui-card`,
`card` eller `box` med `1rem` margin. Brug `columns is-desktop`, hvis der er to
spalter, så de stables på mindre skærme.

## Komponenter

### Knapper

Brug altid `BButton`.

| Situation | Udformning |
| --- | --- |
| Hovedhandling | `type="is-primary"`, evt. `icon-left` |
| Sekundær handling (Annuller, Luk) | Ingen `type` |
| Destruktiv handling | `type="is-danger"`, `icon-left="trash"` når det er slet |
| Mindre destruktiv (fjern, log ud) | `type="is-danger is-light"` |
| Handlinger i tabeller og lister | `size="is-small"` |
| Gem i `WorkspaceFooter` | `type="is-primary"`, label `Gem` |

- Højst én `is-primary` pr. område.
- Grupér flere knapper i `<div class="buttons">`. I en modal-footer står
  sekundær til venstre og primær til højre.
- Deaktivér knappen og brug `:loading`, mens et kald kører, så der ikke kan
  klikkes to gange.
- Ikonknapper uden tekst skal have `aria-label`.

Almindelige ikoner: `plus` (opret/tilføj), `pen` (redigér), `trash` (slet),
`copy` (kopiér), `magnifying-glass` (søg), `arrow-left` (tilbage), `check`
(godkend), `ban` (afvis), `up-right-from-square` (eksternt link). Nye ikoner skal
registreres i `library.add(...)` i `main.ts`.

### Formularer

```vue
<BField label="Navn" :type="errors.name ? 'is-danger' : ''" :message="errors.name">
    <BInput v-model="form.name" placeholder="Gruppens navn" />
</BField>
```

- Ét `BField` pr. felt med `label` over feltet. Brug ikke kun placeholder som
  label.
- Placeholder viser et eksempel (`F.eks. 1`) eller en handling (`Vælg distrikt...`).
- `BSelect` og `BInput` i fuld bredde får `expanded`.
- Datoer vælges med `BDatepicker`, flere værdier med `BTaginput`, ja/nej med
  `BSwitch` (tilstand) eller `BCheckbox` (samtykke/valg).
- Valider på klienten før kaldet og vis fejlen ved feltet. Til korte formularer
  i modaler er en `is-warning`-toast (`Udfyld venligst …`) acceptabel.
- Flertrinsforløb bruger `BSteps` / `BStepItem` (se gruppetilmeldingen).

### Paneler og kort

- Grupper relaterede felter i et Bulma-`panel` med `panel-heading`, og læg
  indholdet i `panel-body` med `1rem` polstring. Det er standarden i
  workspacet.
- Brug `card` med `card-header-title` til selvstændige blokke, f.eks. på
  gruppesiden.

### Lister i sidebaren

Hvert element er en `*SidebarBox`-komponent med:

- navn i fed (`font-weight: 600`, `1rem`)
- én linje metadata (`0.8rem`, `#64748b`)
- kant `1px solid rgba(0, 0, 0, 0.1)`, radius `10px`, `margin-bottom: 1rem`
- valgt tilstand via klassen `active`

Genbrug en eksisterende `*SidebarBox` som skabelon i stedet for at opfinde en ny.

### Tabeller

- Data med sortering, paginering eller kolonner: `BTable` med `BTableColumn`.
- Enkle, klikbare lister: `table.ui-table` (hover-effekt og pointer er indbygget).
- Kolonneoverskrifter med stort begyndelsesbogstav og resten med små.

### Modaler

Brug `BModal` med `has-modal-card` og Bulmas modal-card-struktur:

```vue
<BModal v-model="open" has-modal-card>
    <div class="modal-card">
        <header class="modal-card-head">
            <p class="modal-card-title">Opret ny gruppe</p>
            <button type="button" class="delete" aria-label="Luk" @click="open = false" />
        </header>
        <section class="modal-card-body">
            <!-- felter -->
        </section>
        <footer class="modal-card-foot">
            <div class="buttons">
                <BButton @click="open = false">Annuller</BButton>
                <BButton type="is-primary" :loading="saving" @click="create">Opret gruppe</BButton>
            </div>
        </footer>
    </div>
</BModal>
```

- Titlen er en handling: »Opret ny gruppe«, »Redigér aktivitet«.
- Knappen gentager handlingen: »Opret gruppe«, ikke »OK«.
- Nulstil formularen, når modalen åbnes.

### Bekræftelser

Destruktive handlinger bekræftes med Buefys dialog:

```ts
const Dialog = useDialog();

Dialog.confirm({
    title: 'Slet kort',
    message: `Er du sikker på, at du vil slette "${card.title}"?`,
    confirmText: 'Slet',
    cancelText: 'Annuller',
    type: 'is-danger',
    onConfirm: () => { void remove(); }
});
```

- Titlen er handlingen, `confirmText` er verbet (`Slet`, `Fjern`), `cancelText`
  er altid `Annuller`.
- Fortæl konsekvensen, hvis den ikke er åbenlys (»Kortet bevares på de øvrige
  tavler«).
- Brug aldrig browserens `window.confirm`.

### Feedback

| Situation | Komponent |
| --- | --- |
| Resultat af en handling | `useToast()` → `Toast.open({ message, type })` |
| Vedvarende besked på siden | `BNotification` med `:closable="false"` |
| Indlæsning | `:loading` på knappen, eller `BLoading` over området |
| Ingen data | `.empty-state` med ikon, `empty-state-title` og en kort forklaring |

Toast-typer: `is-success` efter gem/opret/slet, `is-danger` ved fejl,
`is-warning` ved manglende input. Fejlbeskeder siger, hvad der gik galt, når vi
ved det (»Der skete en fejl under upload af billedet«), og falder ellers tilbage
til »Der skete en fejl«.

### Tags

`BTag` / `.tag` til status og kategorier. Brug de semantiske typer fra
[Farver](#farver), og `is-light` når der står mange tags samlet.

## Tekst og sprog

- Al UI-tekst er på dansk med æ, ø og å.
- Knapper er verber i bydeform: »Gem«, »Opret gruppe«, »Tilføj ny gruppe«,
  »Slet«, »Annuller«, »Luk«.
- Overskrifter og knapper har kun stort begyndelsesbogstav: »Opret ny gruppe«,
  ikke »Opret Ny Gruppe«.
- Bekræftelser: »Gruppen er oprettet«, »Linket er kopieret!«.
- Validering: »Udfyld venligst …«.
- Brug »…« (ellipse) i menupunkter, der åbner et valg, f.eks. »Vis som …«.
- Brug fagordene fra lejren konsekvent: gruppe, patrulje, spejder, team,
  aktivitet, tavle, kort.

## Responsivt design

Brug kun disse brudpunkter:

| Brudpunkt | Effekt |
| --- | --- |
| `max-width: 1024px` | Fliser bliver til liste, `columns is-desktop` stables |
| `max-width: 768px` | Liste/detalje viser én ting ad gangen, sidebaren fylder bredden |

Test hver ny skærm i både desktopbredde og ca. 375 px.

## Tilgængelighed

- Alt klikbart er en `button` eller et link, ikke en `div` med `@click`. Er det
  ikke muligt, skal elementet have `role`, `tabindex="0"` og tastaturhåndtering.
- Synlig fokus: `outline: 2px solid #3273dc; outline-offset: 2px` på
  `:focus-visible`. Fjern aldrig fokusmarkeringen uden at erstatte den.
- Ikonknapper har `aria-label`. Rent dekorative ikoner skjules for skærmlæsere.
- Animationer pakkes ind i `@media (prefers-reduced-motion: no-preference)`.
- Farve må ikke være den eneste bærer af information. Kombinér med tekst eller
  ikon.
- Skjult tekst til skærmlæsere bruger `is-sr-only`.

## Delte klasser

Defineret globalt i `App.vue` og må bruges overalt:

| Klasse | Formål |
| --- | --- |
| `ui-card` | Hvid flade med radius og skygge |
| `ui-table` | Enkel, klikbar tabel |
| `empty-state`, `empty-state-title` | Tom tilstand i et område |
| `workspace-empty`, `-title`, `-subtitle` | Tom tilstand i et helt workspace |
| `grid`, `link-box` | Flisemenu |
| `compact-scroll` | Smal scrollbar |

Skal et mønster bruges i mere end én komponent, så læg det her eller lav en
komponent i `Components/`, i stedet for at kopiere CSS.

## Egen CSS

- Klassenavne følger komponenten: `.group-sidebar-box`, `.group-sidebar-box-name`
  (BEM-lignende med `&-name` i SCSS).
- Brug ikke inline `style="…"`.
- Brug ikke `!important` for at overskrive Buefy. Find en klasse eller prop i
  stedet.
- Komponentstyles er i dag globale (ikke `scoped`), så klassenavne skal være
  unikke og have komponentens navn som præfiks.

## Tjekliste før pull request

- [ ] Routen har `pageTitle` og `pageIcon`.
- [ ] Siden bruger en af de tre sidetyper.
- [ ] Kun Buefy-komponenter og Bulma-klasser, hvor de findes.
- [ ] Ingen nye hex-farver, brudpunkter eller skygger uden for guiden.
- [ ] Højst én primær knap pr. område, og destruktive handlinger bekræftes.
- [ ] Toast efter gem, opret og slet, både ved succes og fejl.
- [ ] Teksten er dansk og følger [Tekst og sprog](#tekst-og-sprog).
- [ ] Testet med tastatur og i mobilbredde.
- [ ] `npm run build` i `src/DS.Frontend` lykkes.

## Kendte afvigelser

Koden følger ikke guiden alle steder endnu. Ret det, når du alligevel arbejder i
filen, og lav ikke store omskrivninger kun for at følge guiden.

- **Gem-knapper:** nogle bruger `is-success` (f.eks. `WorkspaceFooter` i
  `Groups.vue`). Standarden er `is-primary`.
- **Blå:** både `#3273dc`, `#485fc7` og `rgb(59, 130, 246)` bruges til fokus og
  hover. Standarden er `#3273dc`.
- **Grå:** der findes over 20 nærtbeslægtede grå hex-koder. Ny kode bruger
  værdierne i [Farver](#farver) eller Bulmas `has-text-grey*`.
- **Modaler:** flere modaler mangler `modal-card-foot` og har knappen i
  `modal-card-body` med inline `style` (f.eks. `GroupsCreateGroup.vue`).
- **Brudpunkt 600 px:** ét sted bruges `max-width: 600px`. Brug 768 px.
- **Globale styles:** `WorkspaceFooter.vue` definerer `.flex` igen, selvom den
  allerede findes i `App.vue`.
- **Farvevariabler:** farverne er hardkodet i hver komponent. En fælles
  SCSS-fil med variabler er en mulig forbedring, men ikke besluttet.
