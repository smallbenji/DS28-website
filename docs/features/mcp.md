# MCP og CLI til tavler

## Status

Implementeret. Værktøjerne er udviklingssupport og ændrer ikke produktets
adgangsregler. Hvordan agenter bruger kortene i udviklingen (kolonner,
ansvarlige, PR-links), står kun i [AGENTS.md](../../AGENTS.md).

## Nuværende adfærd

### Tre adgangsveje med samme værktøjer

| Vej | Hvor | Godkendelse |
| --- | --- | --- |
| Lokal stdio-server | `tools/ds28-mcp/server.mjs`, registreret i `.mcp.json` og `opencode.json` | Personligt adgangstoken mod HTTP-API'et |
| Remote MCP | `/mcp` i Website (`src/DS.Backend/Mcp/BoardTools.cs`) | OAuth-access-token eller personligt token, kræver `McpUser` |
| CLI | `tools/ds28-mcp/cli.mjs` | Starter stdio-serveren og kalder dens værktøjer |

- Værktøjerne er `list_boards`, `get_board`, `create_card`, `update_card`,
  `move_card`, `delete_card`, `create_tag`, `update_tag`, `delete_tag`,
  `search_cards`, `add_dependency`, `remove_dependency`, `add_pull_request`,
  `remove_pull_request` og `set_completion`. Navne, parametre og dansk tekst er
  ens i remote og stdio, så en agent kan skifte mellem dem.
- Alle veje har samme adgang som brugerens egen konto: medlemskab,
  SysAdmin/CampAdmin og validering gælder uændret. Remote kalder `BoardService`
  direkte med en `BoardActor`; stdio og CLI kalder `/api/v1/boards`.
- Stdio og CLI læser `DS28_MCP_URL` og `DS28_MCP_TOKEN` fra
  `tools/ds28-mcp/.env` (ignoreret af git, skabelon i `.env.example`) eller
  miljøet. CLI-argumenter skrives `navn=værdi`, heltal accepterer `DS-42`, uden
  argumenter vises alle værktøjer, og fejl giver exitkode 1.

### Værktøjernes semantik

- `create_card` tager titel, kolonne (id eller titel), beskrivelse, prioritet,
  deadline, `tagIds`, `isEpic` og `epicCardId`. Ren tekst laves om til
  HTML-afsnit.
- `update_card` ændrer kun angivne felter og læser kortet først. En tom værdi
  rydder beskrivelse, frist, farve eller ansvarlige. Ansvarlige angives ved
  fuldt navn eller email og skal matche præcis én bruger. Ansvarlige sendes
  kun, når de angives. Farve angives som hex fra tavlens palette.
- `tagIds`: udeladt betyder ingen tags ved oprettelse og bevar ved
  opdatering; `[]` rydder den aktuelle placerings tags. `update_tag` med tom
  `color` fjerner farven.
- `move_card` flytter inden for samme tavle, som standard bagerst.
  `delete_card` fjerner placeringen og soft-sletter kortet, hvis det var den
  sidste.
- `get_board` viser kolonner, farver, tags, ansvarlige med »(klarmeldt)«,
  »venter på« og »blokerer« (skjulte kort som »(skjult kort)«), PR-links og
  epics (»epic: 3 af 7 færdige«, »epic: DS-17 <titel>«).
- `set_completion` klarmelder kun ens egen del. `remove_dependency` fjerner
  relationen uanset retning.
- Epic-parametre afvises på tavler uden udviklerfunktion, og målet kontrolleres
  på forhånd, så et kort ikke oprettes halvt.

### Remote MCP og OAuth

- `/mcp` bruger `ModelContextProtocol.AspNetCore` og schemet `Mcp`, som først
  forsøger OpenIddict-validering og derefter opslag af et personligt token.
  Uden gyldigt token svares 401 med `WWW-Authenticate`; cookielogin bruges ikke.
  Tokenets `sub` mappes til bruger-id.
- `/.well-known/oauth-protected-resource` beskriver ressourcen ud fra
  forespørgslens origin.
- Klienter (fx ChatGPT-connectoren og Codex) oprettes manuelt under
  OpenIddict-klienter med connectorens redirect-URI. DCR og CIMD findes ikke.
  Resource-validering er slået fra, se [security.md](../security.md).
  Klienten skal bede om `offline_access` for at få refresh tokens.
- `McpUser` tjekkes mod den direkte rolletildeling i databasen ved hver
  forespørgsel og hvert værktøjskald, så fjernelse også afviser eksisterende
  tokens og sessioner. Låste og slettede brugere afvises. Rollen giver ingen
  tavlerettigheder, tildeles eksplicit af SysAdmin eller lejrchef og kan ikke
  arves gennem stillinger.
- Profilen viser for brugere med rollen forbindelsesknapper til Claude og
  ChatGPT med en kort vejledning og kopi af MCP-adressen.
- MCP-sessioner ligger i hukommelsen. En genstart af Website afbryder dem;
  refresh tokens løser tokenfornyelse, ikke mistet sessionstilstand.

### Tests

`node --test tools/ds28-mcp/*.test.mjs` dækker hjælpefunktioner,
argumentparser, stdio-protokol og CLI mod en stub-API.

## Beslutninger

- 2026-11-15: Lokal stdio-MCP til tavlerne over HTTP-API'et med personligt
  adgangstoken. Afløser login med `DS28_MCP_EMAIL` og `DS28_MCP_PASSWORD`.
- 2026-11-15, DS-33: Tags i både stdio og remote.
- 2026-11-16: Remote MCP på `/mcp` til cloud-agenter i Website, uden ekstra
  tjeneste.
- 2026-11-16: `/mcp` accepterer OpenIddict-tokens, så connectorer kan logge
  ind med OAuth.
- 2026-11-17, DS-43: Remote MCP kræver `McpUser`. Afløser adgang alene ud fra
  et gyldigt token.
- 2026-11-17, DS-36: Refresh tokens for MCP-klienter, se
  [security.md](../security.md).
- 2026-11-21, DS-67: CLI til agenter uden MCP, og stdio registreres også for
  Claude Code. Kort føres på DS28 IT-tavlen, ikke i GitHub Issues eller
  Projects.
- 2026-11-21, DS-40: `set_completion` og »(klarmeldt)« i `get_board`.
- 2026-11-23, DS-102: Epics i MCP.

## Åbne spørgsmål

- Skal ChatGPT-connectorens scope-konfiguration og reconnect efter genstart
  af Website verificeres i produktion?
- Skal `set_completion` kunne klarmelde for andre ansvarlige, som
  kortdialogen kan efter DS-109?

## Kodehenvisninger

- `src/DS.Backend/Mcp/BoardTools.cs`
- `src/DS.Backend/Mcp/McpAuthenticationHandler.cs`
- `src/DS.Backend/Mcp/McpAccessHandler.cs`
- `tools/ds28-mcp/server.mjs`
- `tools/ds28-mcp/lib.mjs`
- `tools/ds28-mcp/cli.mjs`
- `tools/ds28-mcp/.env.example`
- `.mcp.json`
- `opencode.json`
