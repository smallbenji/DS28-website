# Tavler og kort

## Status

Implementeret. Tavlerne er HQ's projektstyringsværktøj og bruges også til
udviklingsarbejdet på DS28 IT-tavlen. MCP og CLI står i [mcp.md](mcp.md), og
arbejdsgangen for kort i udviklingen står i [AGENTS.md](../../AGENTS.md).

## Nuværende adfærd

### Model

- Kernen er generisk og kender ikke aktiviteter, materialer eller tilmelding:
  `board`, `board_column`, `work_item` og `board_card`. Skemaet står i
  [database.md](../database.md).
- Et arbejdskort (`work_item`) ejes ét sted: titel, beskrivelse (HTML),
  ansvarlige brugere, deadline, prioritet og farve. Kolonnen for ansvarlig
  stilling findes stadig i API og database, men vises og redigeres ikke i
  kortdialogen (se beslutningen 2026-11-24). Kortet kan placeres på flere tavler med én `board_card` pr. tavle. Titel,
  beskrivelse, farve og ansvarlige er fælles på tværs af placeringer.
- Status hører til placeringen: kortets kolonne på den tavle. En placering er
  færdig, når kolonnen er markeret som færdig-kolonne (`is_done_column`).
  Samme kort kan være i gang på én tavle og færdigt på en anden.
- Kortnummeret `DS-<id>` er `work_item.id` og vises på tavlen og i
  kortdialogen. Nye kort får nummer, når de er oprettet.
- Fjernes kortet fra sin sidste tavle, soft-slettes det. Nye tavler får
  kolonnerne Backlog, I gang og Færdig, og nøglen udledes af titlen.
- `work_item_link` kan pege på domæneobjekter (`entity_type`, `entity_id`),
  men ingen kilde er koblet på, og kort seedes ikke fra domænet.

### Adgang

- HQ-flisen, ruterne `/board` og `/board/:id` og tavler i søgningen kræver
  `BoardView`, som alle stabsgrupper har undtagen `FoodAdmin`. `User` og
  `McpUser` har den ikke.
- API'et kræver login og medlemskab. SysAdmin og CampAdmin har læse- og
  redigeringsadgang til alle tavler (`BoardActor.CanSeeAllBoards`).
  `BoardViewAll` beskriver den globale adgang i matricen og bruges til
  notifikationer; selve kontrollen sker på grupperne.
- Alle indloggede med adgang kan oprette en tavle og bliver selv medlem. Alle
  medlemmer har lige adgang til kort, kolonner, indstillinger, sletning og
  invitation. Der er ingen tavleadministrator eller læserolle.
- Invitation tilføjer eksisterende brugere direkte uden mail. Medlemmer kan
  fjerne andre og forlade tavlen, men det sidste medlem kan ikke fjernes
  (409). Personens ansvar for kort bevares.
- Samme kontrol gælder læsning, ændringer, placeringer og brugersøgning.
  Navne og placeringer på skjulte tavler sendes ikke med kortene. Medlemskab
  af én tavle giver ikke adgang til et delt korts andre tavler.

### Tavlevisning

- Kolonner er hvide flader med skygge på grå baggrund og en fast topkant: grå
  for almindelige kolonner, grøn for færdig-kolonnen.
- Kolonnemenuen »…« har rediger, WIP-grænse, færdig-kolonne, klap sammen og
  slet. Antal kort bliver gult over WIP-grænsen. Sammenklappede kolonner huskes
  i browseren (`ds28.board.collapsedColumns`).
- Tavlehovedet har én »Tavle«-menu (ny kolonne, indstillinger, medlemmer, tags,
  farver og på udviklertavler epics) og »Tilføj kort« som primær knap. Op til
  tre medlemsavatarer med profilbillede åbner medlemsdialogen.
- Kortet viser farven som en stribe i venstre side, tags ved DS-nummeret,
  relativ deadline (rød over, gul i dag og i morgen, dato i færdig-kolonner),
  prioritet som tekst, ansvarlige med klarmelding (»k/n«), antal PR-links og
  »Venter på …«.
- Hurtig oprettelse nederst i kolonnen opretter et kort med kun titel med
  Enter, og feltet bliver stående til næste kort.
- Filtre: søgning på titel og DS-nummer, Mine kort, Overskredet, Venter, tags,
  ansvarlige (inkl. »Ingen ansvarlig«), prioritet og på udviklertavler epic.
  Valg i samme filter kombineres med »eller«, filtre med »og«. Aktive filtre
  vises som mærker. Filtre huskes ikke mellem besøg; kompakt visning huskes i
  browseren. På mobil ligger filtrene i en skuffe.
- Træk og slip bruger native HTML5-drag med indsættelseslinje for kort og
  kolonner. Mens der søges eller filtreres, kan intet flyttes. Der er ingen
  touch- eller tastaturflytning.

### Kortdialogen og kortsiden

- Et kort kan åbnes som dialog på tavlen eller som fuldskærmsside på
  `/board/:id/card/:cardId`. Begge viser `BoardCardDetail.vue` og har samme
  felter og rettigheder. Siden kræver `BoardView` som tavlen, henter tavlen og
  følger dens live opdateringer. Fanetitlen er kortnummer og titel. Findes
  kortet ikke på tavlen, vises en tom side med link til tavlen.
- Dialogens header viser tavle og kortnummer, en knap der kopierer kortets
  adresse, status på den åbne tavle som en knap, der flytter kortet til en
  anden kolonne, »Åbn i fuld skærm«, kortmenuen og luk. Fuld skærm kræver, at
  ændringer er gemt eller annulleret.
- Sidens header viser brødkrummerne Tavler › tavle › kortnummer, »Kopiér link«,
  status og kortmenuen. Relationerne står side om side uden faner. Gem bliver
  på siden, »Fortryd« nulstiller ugemte ændringer, og sletning eller fjernelse
  fra tavlen sender brugeren tilbage til tavlen.
- Hovedkolonnen har titlen øverst og derefter beskrivelsen, som redigeres med
  Nuxt UI's `UEditor` (H1 til H3, fed, kursiv, lister, citat) og vises gennem
  `RichTextViewer`. En tom beskrivelse gemmes som `NULL`.
- I dialogen ligger afhængigheder, PR-links, epic og domænekoblinger i faner under
  beskrivelsen med et tal for antallet. PR-links og epic vises kun på
  udviklertavler, og koblinger kun når der er nogen.
- Sidepanelet viser egenskaberne som rækker med etiket og værdi: ansvarlige,
  prioritet, deadline, tags og farve. Ansvarlige, tags og farve redigeres i en
  popover. Prioritet vises med samme ikoner som på tavlekortene (pil op, streg,
  pil ned). Under egenskaberne står alle kortets tavler i én liste med status
  pr. tavle; fjernelse ligger i hver rækkes menu. På smalle skærme står
  sidepanelet over hovedkolonnen.
- Uden redigeringsadgang vises egenskaberne som tekst, og der er ingen footer.
  Ansvarlige kan stadig åbnes for at klarmelde.
- Kortets felter gemmes med »Gem ændringer« eller Cmd/Ctrl+Enter. Placeringer, status,
  afhængigheder, PR-links, ansvarlige på eksisterende kort og klarmelding
  gemmes straks.
- Tags vælges eller oprettes direkte i feltet (`BoardTagInput.vue`). Nye
  navne sendes som `newTagNames` og gemmes i samme transaktion som kortet.

### Farver og tags

- Hver tavle har sin egen palette af navngivne farver, som medlemmer kan
  ændre. Kortets farve gemmes som hex på `work_item.color`, så palette-
  ændringer ikke påvirker farvede kort. Nye tavler starter med ti farver.
- Tags hører til tavlen: entydigt navn (1 til 64 tegn, uden hensyn til store og
  små bogstaver) og valgfri farve. Tags sættes pr. placering og følger ikke med
  til andre tavler. Ved opdatering bevarer udeladt `tagIds` tags, og `[]`
  rydder dem.

### Ansvarlige og klarmelding

- Et kort har nul eller flere ansvarlige. Tildeling giver ikke adgang.
  Ansvarlige tilføjes og fjernes enkeltvis på eksisterende kort, og
  `assigneeUserIds` ved opdatering er valgfri.
- Hver ansvarlig har en klarmelding (`completedAt`), adskilt fra kortets
  kolonne. Klarmelding blokerer ikke flytning. Alle, der kan redigere kortet,
  kan klarmelde for enhver ansvarlig ved at klikke på cirklen; det gemmes ikke,
  hvem der klarmeldte.

### Afhængigheder

- Et kort kan vente på andre kort (`work_item_dependency`). Relationen hører
  til kortet og gælder på tværs af tavler. »Blokerer« er samme relation set
  fra det andet kort.
- Et kort, man venter på, er færdigt, når alle dets placeringer på
  ikke-slettede tavler står i en færdig-kolonne. Afhængigheder giver kun en
  markering og spærrer ingen flytning.
- `BoardService` afviser afhængighed af kortet selv og ringe. Skjulte kort
  vises kun med nummer og færdig-status. Oprettelse kræver, at man kan
  redigere det ventende kort og se det andet. Soft delete fjerner
  afhængighederne.
- Kortsøgningen (`GET /api/v1/boards/cards/search`) finder ikke-slettede kort
  på synlige tavler, højst 10.

### Live opdatering

- En åben tavle opdaterer sig gennem SignalR på `/api/v1/boards/hub`.
  `BoardService` udsender hændelser gennem `IBoardEventPublisher`, så ændringer
  fra MCP og andre brugere også vises. Hubben bruger samme synlighed som API'et.
- Frontenden genindlæser tavlen samlet med kort forsinkelse, springer over
  under træk og genopretter forbindelsen automatisk. Kortdialogen beholder
  sine egne felter; sidste gemning vinder.
- Der er ingen backplane, så live opdatering forudsætter én Website-instans og
  at proxyen videresender WebSockets.

### Udviklertavler

Kun den effektive SysAdmin kan slå udviklerfunktionen til i
tavleindstillingerne. Deaktivering skjuler data uden at slette dem og afviser
ændringer.

- **PR-links:** HTTPS-links til konkrete pull requests på `github.com`,
  normaliseret og uden dubletter, pr. placering.
- **Epics:** et kort markeret som epic (`board_card.is_epic`); andre kort på
  samme tavle kan høre til én epic (`epic_board_card_id`). Kun ét niveau, og
  epic og kort skal være på samme tavle. Fjernes markeringen eller epic'en,
  slippes kortene. Serveren beregner fremdrift (færdige af alle). Epics står i
  de normale kolonner og flyttes manuelt til Færdig. Tavle-menuen har et
  overblik over epics, og kortdialogen viser epic'ens kort.
- DS28 IT-tavlen er inddelt i epics efter område (DS-17, DS-107, DS-108,
  DS-110 til DS-115). Inddelingen kan ændres på tavlen uden ny beslutning.

## Beslutninger

- 2026-10-14: HQ får et generisk kanban med arbejdskort, der kan placeres på
  flere tavler og kobles til domæner via `work_item_link`.
  `IWorkItemSource` og seed fra domænet er ikke bygget.
- 2026-10-16: Færdig er placeringsbestemt via færdig-kolonner. Afløser et
  globalt `work_item.is_done`.
- 2026-10-16: Kort på flere tavler med status pr. placering. Kortet åbnes i en
  dialog med rig tekst og DOMPurify.
- 2026-10-17: Kortnummer `DS-<id>` afledt af `work_item.id`.
- 2026-10-18: Medlemsbaseret tavleadgang. Afløser `required_role`,
  `BoardsView` og `BoardsManage`.
- 2026-10-18: Palette pr. tavle, og kortets farve er fælles hex. Afløser en
  fast palette med seks farver.
- 2026-10-19: Indsættelseslinje ved træk og slip.
- 2026-10-21: Tavler skjules for brugere uden en stabsgruppe med `BoardView`
  (flise, ruter, søgning). Ikke registreret som beslutning dengang; se åbne
  spørgsmål.
- 2026-10-21: Flere ansvarlige pr. kort. Afløser én ansvarlig bruger.
- 2026-11-15, DS-27 og DS-35: Tags pr. tavle og oprettelse direkte i
  kortdialogen i samme transaktion. Release-tags sættes manuelt.
- 2026-11-15: Live opdatering med SignalR i stedet for polling.
- 2026-11-19: Afhængigheder mellem kort og nyt layout af kortdialogen.
- 2026-11-21, DS-56 til DS-62: Redesign af tavlevisningen efter godkendt
  mockup. Sammenklapning huskes kun i browseren, et kort må oprettes med kun
  titel, og det sidste medlem kan ikke forlade tavlen.
- 2026-11-21, DS-40: Klarmelding pr. ansvarlig.
- DS-50 og DS-51: Udviklertavler med PR-links, kun SysAdmin slår dem til.
- 2026-11-22, DS-87 og DS-88: Profilbilleder i medlemslister og filtre på
  ansvarlige og prioritet.
- 2026-11-23, DS-99 til DS-104: Epics på udviklertavler med UI, filter, MCP og
  overblik. Afløser samlekort. DS28 IT-tavlen inddeles i epics.
- 2026-11-23, DS-109: Alle med redigeringsadgang kan klarmelde for andre.
  Afløser DS-40's regel om, at kun den ansvarlige selv kan.
- 2026-11-24, DS-119: Nyt layout af kortdialogen efter godkendt mockup: titel
  først, relationer i faner, egenskaber som rækker med popovers og én liste
  over tavler. Afløser layoutet fra 2026-11-19.
- 2026-11-24, DS-120: Hvert kort får en fuldskærmsside på
  `/board/:id/card/:cardId`, som kan deles som link. Dialogen kopierer samme
  adresse, men ændrer ikke selv adressen, når den åbnes.
- 2026-11-24, DS-119 og DS-121: Kort har ikke længere en ansvarlig stilling.
  Afløser "ansvarlig bruger eller stilling" fra 2026-10-14. Feltet blev ikke
  brugt til filtre, overblik eller adgang, og den oprindelige begrundelse er
  ikke dokumenteret. DS-119 fjerner feltet fra kortdialogen og bevarer en
  eksisterende værdi ved gem. DS-121 fjerner det fra API, MCP og database med
  expand/contract.

## Åbne spørgsmål

- `BoardView` gater brugerfladen, men API'et kræver kun login og medlemskab.
  Er det tilsigtet, at en bruger uden `BoardView` kan være medlem af en tavle
  via API eller MCP?
- Skal det registreres og vises, hvem der klarmeldte på vegne af en anden?
- Hvem må rydde døde domænelinks, og skal et kort blokere sletning af et
  domæneobjekt?
- Skal en tavle kunne generere kort automatisk fra en forespørgsel ("smart
  board")?
- Skal adressen også ændres, når kortdialogen åbnes på tavlen (fx
  `?card=142`), eller er fuldskærmssiden nok? DS-120 byggede kun siden.
- Automatisk kobling af release-tags til merges eller deploys er ikke afklaret.
- Ikke med i første version af afhængigheder: notifikationer, andre
  relationstyper og gendannelse af slettede kort. Kommentarer, checklister og
  vedhæftninger er udskudt.
- Samtidige ændringer af afhængigheder og epics er ikke beskyttet mod ringe
  eller dobbelte epic-relationer.

## Kodehenvisninger

- `src/DS.Application/Services/Boards/BoardService.cs`
- `src/DS.Application/Services/Boards/BoardActor.cs`
- `src/DS.Application/Services/Boards/IBoardEventPublisher.cs`
- `src/DS.Backend/Controllers/ApiControllers/BoardApiController.cs`
- `src/DS.Backend/services/BoardHub.cs`
- `src/DS.Backend/services/BoardEventPublisher.cs`
- `src/DS.Domain/Migrations/015_boards.sql`
- `src/DS.Domain/Migrations/039_board_card_epics.sql`
- `src/DS.Frontend/src/Views/Boards.vue`
- `src/DS.Frontend/src/Views/BoardDetail.vue`
- `src/DS.Frontend/src/Views/BoardCardPage.vue`
- `src/DS.Frontend/src/Components/Board/BoardCard.vue`
- `src/DS.Frontend/src/Components/Board/BoardCardDetail.vue`
- `src/DS.Frontend/src/Components/Board/BoardCardModal.vue`
- `src/DS.Frontend/src/Components/Board/BoardColumn.vue`
- `src/DS.Frontend/src/Components/Board/BoardFilterPanel.vue`
- `src/DS.Frontend/src/Components/Board/BoardMembersModal.vue`
- `src/DS.Frontend/src/Components/Board/BoardEpicsModal.vue`
- `src/DS.Frontend/src/Components/Board/BoardTagInput.vue`
- `src/DS.Frontend/src/lib/board/boardFilters.ts`
- `src/DS.Frontend/src/lib/board/boardDrag.ts`
- `src/DS.Frontend/src/lib/board/boardLiveUpdates.ts`
