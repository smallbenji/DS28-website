# Frontend

## Status

Implementeret. `src/DS.Frontend` (Vue, Nuxt UI 4, Tailwind CSS 4) er den
eneste frontend og serveres på `/`. Build og API-klient står i
[architecture.md](../architecture.md). Konventioner for nyt UI står i afsnittet
Frontend i [AGENTS.md](../../AGENTS.md).

## Nuværende adfærd

### Layout og routing

- Indloggede sider bruger `DashboardLayout.vue` med sidebar, søgning,
  temavalg og brugermenu (»Min profil«, »Vis som …« for SysAdmin og »Log ud«).
  Moduler vises i sidebaren efter rolle.
- Sider uden login (`/login`, `/register`, `/forgot-password`,
  `/reset-password/:id`, `/invitation/:id`, `/group-invitation/:id`,
  `/group-pre-signup`) og `/forms/:id?` bruger `GuestLayout.vue` uden sidebar
  med en tegnet, dekorativ lejrbaggrund (`PrairieBackground.vue`).
  Baggrunden er lys i begge temaer og respekterer `prefers-reduced-motion`.
- Routeren skelner mellem offentlige sider (`meta.public`), sider kun for
  gæster (`meta.guestOnly`) og sider, der kræver login, og sender til `/login`
  med `returnUrl`. `returnUrl` valideres i backenden.
- Website serverer `index.html` som fallback for alle stier.
  `AnonymousController` serverer siderne uden login. Gamle `/beta/*`-links
  sendes permanent videre til samme sti uden præfiks.
- `MeStore.Me` er `null`, indtil brugerdata er hentet. Komponenter og
  routerens adgangskontroller venter på data og læser ikke felter på `null`.
- Temaet følger systemet som standard. Brugeren kan vælge Automatisk, Lys
  eller Mørk; valget gemmes i browseren (`useColorMode`).
- Sideskift sætter sidetitlen (»<side> · DS28 HQ«). Den arkiverede frontend
  flyttede også fokus til hovedindholdet; det gør Nuxt-frontenden ikke.

### Forsiden og søgning

- `/` viser HQ-genvejene fra `/api/v1/home` og versionen. Backendens Font
  Awesome-navne oversættes til Lucide-ikoner i `lib/hqShortcuts.ts`. Interne
  genveje åbner i routeren, eksterne med almindelig sideindlæsning.
- Den globale søgning er Nuxt UI's `UDashboardSearch` (`GlobalSearch.vue`),
  åbnet med søgeknappen i sidebaren eller ⌘/Ctrl+K. Den viser moduler,
  adminsider, tavler, grupper og aktiviteter.
- `GET /api/v1/search` filtrerer på serveren efter modulernes egne regler:
  tavler kræver `BoardView` (SysAdmin og lejrchef ser alle, andre deres egne),
  grupper findes alle med `GroupsView` og ellers kun egen gruppe, og
  aktiviteter kræver `ActivityView` (alle med `ActivityAdmin`, ellers egne
  teams). Søgningen starter ved to tegn, viser højst fem pr. type og følger
  "Vis som".

### Adgang nægtet

- `/access-denied` er den eneste skærm for manglende adgang, også for
  cookie-auth'ens `AccessDeniedPath`.
- Routeren sender dertil, når `requiredRole` eller `requiresActivityAdmin`
  ikke er opfyldt. Login tjekkes først, så en udlogget bruger møder login.
- Et 403 fra API'et gennem den fælles klient sender til skærmen med den afviste
  side i `?from=`, også for kald, der ellers sluger fejl. 401 håndteres af
  login-flowet.

### Installerbar app og ikon

- HQ har et web app manifest (`public/manifest.webmanifest`, serveret fra
  `/dist/manifest.webmanifest`): navn »DS28 HQ«, start og scope `/`,
  `standalone` og temafarve `#4caee7`. Ikonerne er cowboyhatten i 192 og 512
  pixels, en maskable variant og et 180 pixels `apple-touch-icon`. Filerne
  kræver ikke login.
- Faviconet er cowboyhatten som ICO (16, 32 og 48 px), både i appen og på
  serverrenderede sider.
- Der er ingen service worker, så appen virker ikke offline og har ingen push.

## Beslutninger

- 2026-10-20: `MeStore.Me` er `null` under indlæsning, og
  `ActivityAdmin`-kontrollen venter på brugerdata.
- 2026-11-15: Én fælles skærm for adgang nægtet på `/access-denied`. Afløser
  dubletruten `/AccessDenied`.
- 2026-11-17: Cowboyhatten er favicon. Afløser en manglende SVG og
  standardikonet.
- 2026-11-20: En ny frontend med Nuxt UI 4 og Tailwind 4 bygges, fordi Nuxt UI
  har færdige dashboard-komponenter. Ikoner bundtes, så browseren ikke henter
  fra Iconify.
- 2026-11-20: Søgningen åbnes med ⌘/Ctrl+K, fordi ⌘/Ctrl+Mellemrum er optaget af
  Spotlight på Mac. Afløser ⌘/Ctrl+Mellemrum og listernes egne ⌘/Ctrl+K-genveje.
- 2026-11-21: Nuxt UI-frontenden er den officielle frontend i
  `src/DS.Frontend`. Buefy-frontenden er arkiveret i `src/DS.Frontend.Archive`,
  bygges og serveres ikke og ændres ikke. Designguiden gælder kun den
  arkiverede frontend. Afløser beta-frontenden på `/beta` og designguiden som
  regel for HQ (2026-11-18).
- 2026-11-22, DS-94: HQ kan installeres som app via et manifest uden service
  worker.

## Åbne spørgsmål

- Skal det arkiverede frontend slettes helt?
- Skal der skrives en designguide for Nuxt UI-frontenden?
- Ethvert 403 udløser skærmen for adgang nægtet, også baggrundskald. Et kald,
  der begynder at give 403 uden at siden er utilgængelig, kræver et opt-out i
  klienten.
- Senest brugte søgeresultater og beskrivelser af moduler er foreslået, ikke
  besluttet.
- Offline-understøttelse og push-notifikationer er foreslået, ikke besluttet.
- Skal fokus flyttes til hovedindholdet ved sideskift, som i den arkiverede
  frontend?

## Kodehenvisninger

- `src/DS.Frontend/src/router/index.ts`
- `src/DS.Frontend/src/Layouts/DashboardLayout.vue`
- `src/DS.Frontend/src/Layouts/GuestLayout.vue`
- `src/DS.Frontend/src/Components/GlobalSearch.vue`
- `src/DS.Frontend/src/Components/Auth/PrairieBackground.vue`
- `src/DS.Frontend/src/Stores/MeStore.ts`
- `src/DS.Frontend/src/Views/AccessDenied.vue`
- `src/DS.Frontend/src/lib/hqShortcuts.ts`
- `src/DS.Frontend/src/lib/api.ts`
- `src/DS.Frontend/public/manifest.webmanifest`
- `src/DS.Frontend/index.html`
- `src/DS.Backend/Program.cs`
- `src/DS.Backend/Controllers/ApiControllers/SearchApiController.cs`
