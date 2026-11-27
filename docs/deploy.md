# Deploy til produktion

HQ kører på `main-server` som Docker Compose bag Tailscale. Deploy sker gennem
branchen `prod`: en pull request fra `master` til `prod` er beslutningen om at
deploye, og et merge starter `.github/workflows/deploy.yml`.

## Beslutning

**2026-11-22 (DS-24, implementeret, ikke kørt i produktion):** `prod` er
produktionsbranchen, og et pull request fra `master` til `prod` er
deploybeslutningen. Det afløser reglen om `master` som eneste lange branch og
manuelt skubbede SemVer-tags fra 2026-11-15 samt den tag-triggede publicering
fra 2026-10-19. Workflowet tagger hvert `prod`-push automatisk og deployer det
byggede image gennem GitHub Environment `production`.

## Sådan virker det

1. Arbejdet merges til `master` som altid.
2. Der åbnes en pull request fra `master` til `prod`. `prod-source.yml` afviser
   pull requests fra andre branches.
3. Et merge til `prod` starter Deploy-workflowet:
   - **Fastlæg version** læser `DSVersion.Version` (fx `0.3.1`) og tagger
     merge-committen. Første deploy af en version får `v0.3.1`, de næste
     `v0.3.1+1`, `v0.3.1+2` osv. Tagget skubbes af workflowet, ikke af en person.
   - **Byg** bygger Website og Worker én gang med den fulde version som
     `DS_BUILD_VERSION` og publicerer dem til GHCR som `0.3.1_2` og `latest`.
     Docker-tags må ikke indeholde `+`, derfor `_`.
   - **Deploy** joiner tailnettet som en kortlivet node med `tag:ci`, SSH'er til
     `main-server` og kører `deploy/deploy.sh` med image-tagget. Scriptet tager et
     databasedump, henter de fastlåste images og genstarter Website og Worker.
   - Til sidst kontrollerer jobbet, at `/api/version` svarer med den nye version.

### Hverdagsbrug

| Situation | Hvad du gør |
| --- | --- |
| Lille rettelse uden nyt versionsnummer | PR fra `master` til `prod`. Bliver fx `v0.3.1+3`. |
| Ny version | Bump `Version` i `src/DS.Domain/DSVersion.cs` på `master`, derefter PR til `prod`. Bliver `v0.4.0`. |
| Rollback | Actions → Deploy → Run workflow på `prod` med et eksisterende tag, fx `v0.3.1+2`. Der bygges ikke; det eksisterende image deployes. |
| Hotfix | Altid via `master` og derefter PR til `prod`. Commit aldrig direkte på `prod`. |

Rul ikke tilbage ved at revertere merge-committen på `prod`. Git betragter så
ændringerne som allerede merget, og næste PR fra `master` tager dem ikke med
igen. Brug manuel rollback og ret fejlen på `master`.

Migrationer skal fortsat være bagudkompatible i én udgivelse (expand/contract),
så rollback til det forrige tag virker. DbUp kan ikke rulle en migration tilbage;
dumpet er sikkerhedsnettet.

## Engangsopsætning

Opsætningen skal være på plads, **før** branchen `prod` oprettes, fordi
oprettelsen i sig selv udløser et deploy.

### 1. Tailscale

1. Tilføj tagget `tag:ci` i tailnettets policy med dig selv som ejer, og giv det
   kun adgang til SSH på `main-server`, fx:

   ```json
   "tagOwners": { "tag:ci": ["autogroup:admin"] },
   "grants": [
     { "src": ["tag:ci"], "dst": ["main-server"], "ip": ["tcp:22"] }
   ]
   ```

   Brug `main-server`s tailnet-IP eller et host-alias i `dst`, alt efter hvordan
   policyen i forvejen er skrevet.
2. Opret en OAuth-client under Settings → OAuth clients med skrivescope
   `auth_keys` og tagget `tag:ci`.
3. Deploy bruger almindelig OpenSSH over tailnettet, ikke Tailscale SSH. Den
   tvungne kommando i `authorized_keys` nedenfor virker kun med OpenSSH.

### 2. main-server

1. Opret en bruger til deploy, fx `ds28deploy`, og giv den adgang til Docker.
   Medlemskab af `docker`-gruppen svarer reelt til root, så nøglen begrænses
   med en tvungen kommando.
2. Kopiér filerne fra repoet:

   ```bash
   sudo install -d -o ds28deploy /opt/ds28
   sudo install -m 755 deploy/deploy.sh /opt/ds28/deploy.sh
   sudo install -m 600 -o ds28deploy deploy/deploy.env.example /opt/ds28/deploy.env
   ```

   Ret `deploy.env`: `COMPOSE_DIR` er mappen med serverens compose-fil,
   `DB_CONTAINER`, `DB_NAME` og `DB_USER` peger på PostgreSQL, og `BACKUP_DIR`
   er hvor dumps gemmes. `SERVICES` er compose-servicenavnene for Website og
   Worker.
3. Serverens compose-fil skal bruge den fastlåste version:

   ```yaml
   services:
     hq:
       image: ghcr.io/smallbenji/ds28-website:${DS28_VERSION:?}
     worker:
       image: ghcr.io/smallbenji/ds28-website-worker:${DS28_VERSION:?}
   ```

   Repoets `docker-compose.yaml` bygger fra kildekode og bruges ikke af deploy.
   Kører du compose manuelt på serveren, så sæt versionen fra den seneste
   deploy: `DS28_VERSION=$(cat .ds28-version) docker compose up -d`.
4. Er GHCR-pakkerne private, så log brugeren ind én gang med
   `docker login ghcr.io` og et token med `read:packages`.
5. Generér et nøglepar kun til deploy, og læg den offentlige nøgle i
   `~ds28deploy/.ssh/authorized_keys` med en tvungen kommando:

   ```text
   restrict,command="/opt/ds28/deploy.sh" ssh-ed25519 AAAA... ds28-github-deploy
   ```

   Nøglen kan så kun køre deploy-scriptet, og scriptet afviser alt, der ikke
   ligner et image-tag.

### 3. GitHub

1. Settings → Environments → `production`:
   - Deployment branches: kun `prod`.
   - Secrets: `TS_OAUTH_CLIENT_ID`, `TS_OAUTH_SECRET`, `DEPLOY_SSH_KEY` (den
     private nøgle) og `DEPLOY_KNOWN_HOSTS` (output af
     `ssh-keyscan <tailnet-navn>` for `main-server`).
   - Variables: `DEPLOY_HOST` (tailnet-navn eller -IP) og `DEPLOY_USER`.
   - Required reviewers er valgfrit. Merge af PR'en er allerede godkendelsen.
2. Opret branchen `prod` fra `master`. Det starter første deploy.
3. Opret et ruleset for `prod`:
   - kræv pull request
   - kræv statuschecket **Kilde er master**
   - bloker force-push og sletning
4. Kontrollér, at ingen tag-ruleset forhindrer GitHub Actions i at skubbe
   `v*`-tags.

## Databasedumps

Hvert deploy gemmer et dump i `BACKUP_DIR` som
`ds28-<tidspunkt>-before-<image-tag>.dump` i `pg_dump`s custom-format. Dumps
slettes ikke automatisk. Gendannelse sker med `pg_restore` mod en stoppet
applikation.

## Åbne spørgsmål

- Tailscale-policy, OAuth-client, serverbruger, serverens compose-fil, secrets
  og ruleset for `prod` er ikke opsat endnu.
- Oprydning i gamle databasedumps er ikke automatiseret.
