# DS28 HQ

HQ til Distriktssommerlejr 2028: gruppetilmelding, aktiviteter, materialer,
økonomi, tavler og et fælles dashboard. Produktion kører på
`hq.distriktssommerlejr.dk`.

Projektets opbygning, konventioner og arbejdsgang for kort og pull requests står
i [AGENTS.md](AGENTS.md). Beslutninger og åbne spørgsmål står i
[projektplanen](docs/project-plan.md), og databaseskemaet i
[docs/database.md](docs/database.md).

## Kør lokalt

Kræver .NET 10 (`mise.toml`), Node 24 (`volta` i `src/DS.Frontend/package.json`)
og en PostgreSQL-database.

1. Kopiér `.env.example` til `.env` og udfyld mindst `ASPNETCORE_URLS`,
   `DS__ConnectionString` og `DS__FileStoragePath`. Frontendens dev-server
   forventer backenden på `https://localhost:44310`.
2. Start Website. Databasen migreres med DbUp ved opstart:

   ```sh
   dotnet run --project src/DS.Backend/DS.Backend.csproj
   ```

   I VS Code kan du i stedet bruge launch-konfigurationen "Launch Website", som
   læser `.env`.
3. Start frontenden, som proxyer `/api` til backenden:

   ```sh
   cd src/DS.Frontend
   npm install
   npm run dev
   ```

`npm run build` typetjekker og bygger frontenden til
`src/DS.Backend/wwwroot/dist`, hvor Website serverer den på `/`.

## Hangfire-worker

`DS.Worker` udfører baggrundsjob. Website sætter kun job i kø og viser
Hangfire-dashboardet.

```sh
dotnet run --project src/DS.Worker/DS.Worker.csproj
```

Workeren skal bruge samme `DS__ConnectionString` og `DS__FileStoragePath` som
Website og læser SMTP-indstillinger fra samme `DS`-sektion. Den venter på
Websites databasemigreringer, før den registrerer tilbagevendende job og starter
Hangfire, og bruger maskinens hostname som servernavn.

`docker compose up --build hq worker` bygger begge services. Compose læser
konfiguration fra `.env` og deler volumen `files` mellem dem. Workeren har
ingen HTTP-port.

## Kodegraf til agenter

[Graphify](https://graphify.com) bygger en lokal kodegraf, så en agent kan
spørge til strukturen i stedet for at læse filer. Outputtet i `graphify-out/`
er genereret og ignoreres af git.

```sh
uv tool install graphifyy     # eller: pipx install graphifyy
graphify install --platform opencode
graphify extract . --code-only
graphify cluster-only .
```

`--code-only` springer den semantiske gennemgang af dokumenter over og kræver
derfor ingen API-nøgle til en sprogmodel. Opdatér grafen efter kodeændringer
med `graphify update .`, og kør `graphify extract . --code-only --force`, når
filer er slettet. Spørg grafen med `graphify query "<spørgsmål>"` med engelske
symbolnavne, eller åbn `graphify-out/graph.html`.

`graphify hook install` genopbygger grafen i baggrunden efter commit, checkout,
merge og rebase. Hooks ligger i `.git/hooks` og gælder kun den lokale maskine.
