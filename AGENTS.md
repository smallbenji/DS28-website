# Project conventions

## Project purpose and planning context

DS28 supports the digital operation of Distriktssommerlejr 2028: group
registration, activities, materials, finances, and a shared HQ dashboard.

Read [the project plan](docs/project-plan.md) when working on domain
behavior, permissions, registration dates, or architecture. It is a short
index: the user's original draft, one list of open questions, and a table that
points to the file for each area (`docs/architecture.md`, `docs/security.md`
and `docs/features/*.md`). Read the index first, then only the files for the
areas the task touches. The plan is **a draft, not an adopted specification or
a request to implement every listed feature**.

- Use the current user request and explicit subsequent decisions to determine
  scope. Use the draft to understand intent, not to silently expand a task.
- Inspect the implementation before assuming that a planned feature exists.
  When the plan and code differ, distinguish intended behavior from current
  behavior and surface discrepancies relevant to the task.
- Keycloak has been explicitly dropped from the plan. ASP.NET Core Identity
  and OpenIddict are the current authentication foundation
  (`src/DS.Backend/Extensions/ServiceExtensions.cs`). Do not reintroduce
  Keycloak based on old planning material.
- Current global permissions are defined in `src/DS.Domain/Roles.cs`. `AppGroups`
  denotes global identity roles, not scout groups. The draft role descriptions
  are not evidence that those permissions are already implemented.
- Scout group membership currently uses `User.Group`, with no group owner or
  group administrator distinction. Group members have equal group-management
  access. Do not infer a group-admin hierarchy from activity-team leadership.
- The draft dates and the meaning of registration "closing" are provisional.
  Do not introduce hard-coded deadlines or enforce those dates just because
  they appear in the plan.
- Notifications and audit logs are nice-to-have features; invoicing and several
  role details remain open questions. Keep that uncertainty visible.

## Repository map

| Path | Contents |
|---|---|
| `src/DS.Backend` | Website host: controllers, auth, MCP endpoint, `Program.cs` |
| `src/DS.Application` | Services, form templates, seeders |
| `src/DS.Infrastructure` | EF `DataDbContext`, entity configuration, Identity, OpenIddict, Google, exports, Hangfire jobs |
| `src/DS.Domain` | Models, DTOs, `Roles.cs`, `DSVersion.cs`, DbUp scripts in `Migrations/*.sql` |
| `src/DS.Worker` | Hangfire worker host |
| `src/DS.RegistrationImporter` | One-off import of historic registrations |
| `src/DS.Frontend` | Live Vue, Nuxt UI and Tailwind frontend |
| `src/DS.Frontend.Archive` | Archived Buefy frontend, not built or served |
| `tools/ds28-mcp` | MCP server and CLI for DS28 cards |
| `tools/historic_registration` | Scripts for the 2024 registration import |

| Document | Use it for |
|---|---|
| [`docs/project-plan.md`](docs/project-plan.md) | Plan index: original draft, open questions, decision table |
| [`docs/architecture.md`](docs/architecture.md) | Projects, layers, DI, Worker, migration rules, frontend build |
| [`docs/security.md`](docs/security.md) | Identity, OpenIddict, antiforgery, access tokens, hardening |
| [`docs/features/`](docs/features/) | One file per feature area: behavior, decisions, open questions |
| [`docs/database.md`](docs/database.md) | Current database schema |
| [`docs/design-guide.md`](docs/design-guide.md) | Archived frontend only |
| [`README.md`](README.md) | Running locally, Worker, code graph |

## Keep the project plan current

Updating the plan is part of completing work that establishes or changes a
project decision. Do this in the same task, without waiting for a separate
documentation request.

- Record a decision in the file for its area: `docs/architecture.md` for
  projects, layers, jobs, migrations rules and build, `docs/security.md` for
  authentication and hardening, and `docs/features/<area>.md` for features.
  Create a new feature file when no existing area fits.
- Feature files use the same sections: `Status`, `Nuværende adfærd` (present
  tense, no change log), `Beslutninger` (one bullet each with date, DS card,
  decision and what it supersedes), `Åbne spørgsmål` and `Kodehenvisninger`
  (paths that exist).
- Keep the index in `docs/project-plan.md` current: add new files and DS cards
  to the decision table, and add or remove open questions in its list.
- Describe current behavior in place and remove what no longer applies. Git
  history keeps the old text.
- Record decisions about architecture, technologies, permissions, workflows,
  scope, and deadlines, including features or technologies that are dropped.
- Update affected sections and note what an explicit new decision supersedes,
  so old plans are not mistaken for current requirements.
- Keep confirmed decisions, implemented behavior, and unresolved proposals
  clearly distinguished. Do not turn an assumption or suggestion into an
  adopted requirement.
- When implementation changes the behavior described in the plan, update its
  implementation notes to match what was actually completed.
- Update related guidance in `AGENTS.md` if a decision makes it outdated.
- Routine formatting changes and fixes that do not affect the documented plan
  do not require a plan update.

## Keep the database documentation current

Update `docs/database.md` in the same task whenever a change affects the
database: entities, tables, columns, types, indexes, constraints, relationships,
or migrations. Do not wait for a separate documentation request.

- Describe the resulting schema, not the individual migration steps, so the
  document stays valid for a fresh database.
- Cover new and removed tables, columns, and indexes, including columns that
  are nullable, unique, or required.
- Update existing sections in place and remove content that no longer applies,
  instead of appending a history of changes.

## Comments

Do not add code comments unless explicitly asked. This covers `//`, `/* */`,
`<!-- -->`, and XML documentation comments.

- Prefer self-documenting code: descriptive names over explanatory comments.
- Record the reasoning in the plan file for the area instead (see
  [Keep the project plan current](#keep-the-project-plan-current)), which is
  where decisions belong.
- An exception is a short note on a non-obvious security invariant, where a
  future change would otherwise silently introduce a bug. Keep it to one line.

## C# controller style

Use `src/DS.Backend/Controllers/ApiControllers/ActivityApiController.cs` as the
general reference for controller formatting. Read it before creating or
substantially editing a controller. The guard style below is authoritative and
supersedes single-return examples in the reference. Use Microsoft C# conventions
where the reference does not establish a convention.

- Use block-scoped namespaces with braces, not file-scoped namespaces.
- Use four spaces per indentation level, including inside the namespace.
- Put opening and closing braces on their own lines for namespaces, classes,
  methods, and multi-statement control-flow blocks.
- Keep an `if` with one short return on one line, for example
  `if (!result.Succeeded) return BadRequest(result.Errors);`. Use braces when
  the body has multiple statements or needs multiple lines for readability.
  Apply the same rule to other single-statement guard clauses.
- Use normal method bodies for controller helpers rather than expression-bodied
  methods, matching the reference controller.
- Keep primary constructors and method signatures on one line when readable.
  Do not automatically put every parameter on a separate line.
- Separate methods and logical sections with one blank line. Keep a query or
  operation and its immediate result check together, as in the reference.
- Use a blank line between independent guard clauses and before the final
  response after completing an operation.
- Keep short conditions and expressions together. Break long LINQ chains at
  method boundaries and long conditions where needed for readability; avoid
  both compressed statements and unnecessary vertical expansion.
- Use multiline object initializers for larger objects, with one property per
  line. Small anonymous response objects may stay on one line.
- Use descriptive PascalCase type/method names and camelCase parameter/local
  names. Preserve existing public names during formatting-only changes.
- Keep route and authorization attributes directly above the class or action.
- Do not change routes, authorization, validation, or business behavior as part
  of a formatting-only task.

Example layout:

```csharp
namespace DS.Backend.Controllers
{
    [Authorize]
    [Route("api/v1/example")]
    public class ExampleApiController(ExampleRepository repository) : Controller
    {
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetExample(int id)
        {
            var example = await repository.GetAsync(id);
            if (example == null) return NotFound();

            return Ok(example);
        }
    }
}
```

## Frontend

The frontend lives in `src/DS.Frontend` and uses Vue, Nuxt UI, and Tailwind,
served at `/`. Use Nuxt UI components and Lucide icons (`i-lucide-*`), and
never Buefy or Bulma. Follow the patterns in surrounding pages and layouts.

`src/DS.Frontend.Archive` is the archived Buefy frontend. It is not built in
`Containerfile.Website` and not served by the backend. Do not change it or
port changes to it. [The design guide](docs/design-guide.md) describes that
archived frontend and does not apply to `src/DS.Frontend`.

Old `/beta/*` links redirect to the same path without the prefix
(`src/DS.Backend/Program.cs`).

## DS28 cards

Work is tracked as cards (`DS-<number>`) on the `DS28 IT` board (id 1) in HQ at
`hq.distriktssommerlejr.dk`. Do not use GitHub issues or GitHub Projects for
cards.

### Access

Use the first option your environment supports. All three call the same HQ
API with the same permissions as the user's own account.

1. **Local MCP server** `tools/ds28-mcp/server.mjs` over stdio. It is
   registered for Claude Code in `.mcp.json` and for opencode in
   `opencode.json`.
2. **Remote MCP** at `https://hq.distriktssommerlejr.dk/mcp`, for cloud agents
   that cannot run a local process. It uses OAuth or a personal access token,
   and the account needs the `McpUser` role.
3. **CLI** for agents without MCP support. It runs the same tools as the local
   server:

   ```bash
   node tools/ds28-mcp/cli.mjs
   node tools/ds28-mcp/cli.mjs get_board boardId=1
   node tools/ds28-mcp/cli.mjs create_card boardId=1 column=Backlog title="Short title" description="What and why"
   node tools/ds28-mcp/cli.mjs move_card boardId=1 cardId=DS-42 column="I gang"
   node tools/ds28-mcp/cli.mjs update_card boardId=1 cardId=DS-42 assignees="Benjamin Falch"
   node tools/ds28-mcp/cli.mjs add_pull_request boardId=1 cardId=DS-42 url=https://github.com/smallbenji/ds28-website/pull/123
   ```

   Without arguments it lists every tool and its parameters. Arguments are
   `name=value`; integers accept `DS-42`, and lists are comma-separated or a
   JSON array. It exits with code 1 and prints the error on failure.

The local server and the CLI read `DS28_MCP_URL` and `DS28_MCP_TOKEN` from
`tools/ds28-mcp/.env` (git-ignored, template in `.env.example`) or from the
environment. The token is a personal access token created under the user's
profile in HQ. Never print, commit, or paste the token.

### Card lifecycle

- **Create** a new card in `Backlog`, with a short Danish title and a
  description of what and why. Create a card for work that has none, even if
  the work is already done.
- **Epics**: an epic is a card marked as epic (`isEpic`) on the developer
  board. When a new card belongs to an existing epic, put it under that epic
  with `epicCardId`. Move an epic to `Færdig` manually once its cards are
  released.
- **Start**: when you begin working on a card, first move it to `I gang` and
  assign the person doing the work.
- **Pull request**: title it `[DS-<number>]: <short description>` and link it
  on the card with `add_pull_request`. Link every pull request that belongs to
  the card.
- **Merged**: when the pull request is approved and merged, move the card to
  `Ikke udgivet`.
- **Released**: when the change is deployed through `prod`, move the released
  cards to `Færdig`. `Færdig` means released, not merely merged.
- Use `Blocked` for a card that cannot proceed, and `Planlægning` for cards
  that still need clarification before they are ready for `Backlog`.

### Assignees

The assignee is the person who performed the work, by full name, for example
`Benjamin Falch` when Benjamin works with an agent. Never assign the agent
itself. `update_card` replaces the assignee list, so read the card with
`get_board` first and include existing assignees. Before finishing a task,
verify the assignment on the card, including cards created after the work, and
keep it through merge and release.

## Git commits and pull requests

- Commit with the user's own git identity as configured in the repository
  (`git config user.name` and `user.email`). Do not change the author or
  committer.
- Do not add `Co-Authored-By` trailers or other AI attribution, such as
  "Generated with Claude Code", to commit messages or pull request
  descriptions.
- Do development work on a branch, not directly on `master`. When the work is
  ready, push the branch and open a pull request against `master`.
- Name branches `feature/<short-name>`, `fix/<short-name>` or
  `chore/<short-name>`. Do not use tool-specific prefixes such as `codex/` or
  `copilot/`.
- One card equals one pull request equals one bounded change. Title the pull
  request `[DS-<number>]: <short description>` and split the work into several
  pull requests rather than growing one.
- Describe what changed, why, how it was tested, and what was not tested.
- Merge with a merge commit, not a squash, and delete the branch afterwards.
- Track every pull request on a DS28 card as described in
  [DS28 cards](#ds28-cards).
- Pull requests are not built in CI; only CodeQL runs. Build and test locally
  according to the verification section below before merging.

## Deployment

- `DS.Worker` executes Hangfire jobs; Website only enqueues jobs and hosts the
  dashboard. Keep recurring-job registration in Worker.
- Compose builds Worker using `Containerfile.Worker`. Both services share the
  database and file volume. Worker waits for Website's DbUp migration journal
  before starting Hangfire and uses the container hostname as its server name.
- The deploy workflow builds and publishes both Website and Worker using a
  matrix, with the same version tags. Worker uses the `-worker` image suffix.

The project uses GitHub Actions for CI/CD (see `.github/workflows/deploy.yml`).
Setup of secrets, the server and branch protection is described in
[the deploy guide](docs/deploy.md).

- **Branches**: `master` is the integration branch and `prod` is what runs in
  production. Feature work merges into `master`; a pull request from `master`
  into `prod` is the deploy decision. `prod-source.yml` rejects pull requests
  into `prod` from any other branch. Never commit directly to `prod`, and land
  hotfixes on `master` first.
- **Versions**: `DSVersion.Version` in `src/DS.Domain/DSVersion.cs` holds the
  manually chosen `X.Y.Z`. Bump it on `master` when a release deserves a new
  number. Every push to `prod` tags the merge commit automatically: `vX.Y.Z`
  for the first deploy of a version, then `vX.Y.Z+1`, `vX.Y.Z+2` for deploys
  without a bump. Do not create release tags by hand.
- **Build version**: the workflow passes the full version as
  `DS_BUILD_VERSION`, which becomes `DSVersion.BuildVersion` and is returned
  by `/api/version`. Local builds fall back to `DSVersion.Version`.
- **Images**: `ghcr.io/smallbenji/ds28-website` and
  `ghcr.io/smallbenji/ds28-website-worker`, tagged with the version (with `+`
  replaced by `_`, since Docker tags cannot contain `+`) and `latest`.
- **Deploy**: the `deploy` job runs in the `production` GitHub Environment,
  joins the tailnet with the Tailscale action and connects over SSH to
  `main-server`, where `deploy/deploy.sh` dumps the database, pulls the pinned
  images and restarts Website and Worker. The job then checks `/api/version`.
- **Rollback**: run the Deploy workflow manually on `prod` with an existing
  tag, for example `v0.3.1+1`. It redeploys that image without building. Do not
  roll back by reverting the merge on `prod`; fix forward on `master`.

- Production at `hq.distriktssommerlejr.dk` is the only environment. There is no
  staging host.
- Deploy the image built for the tagged `prod` commit. Do not rebuild an
  existing version tag.
- Database migrations must stay backwards compatible for one release
  (expand/contract: add, use, then remove), so the previous tag can be
  redeployed if a release has to be rolled back.
- Every deploy takes a database dump on the server before it changes the
  containers.

## Verification

- Review formatting against this section and use
  `ActivityApiController.cs` for patterns not specified here; an automatic
  formatter alone does not establish compliance with the user's preferred
  style.
- Run `git diff --check` after edits.
- For C# changes, build with
  `dotnet build src/DS.Backend/DS.Backend.csproj --no-restore --disable-build-servers -m:1`.
- For frontend changes, run `npm run build` in `src/DS.Frontend`. If both builds
  are needed, finish the frontend build first: it replaces assets consumed by
  the backend build.
- Do not create test projects or test folders such as `tests/` to verify a
  change. Verify with the builds above and by running the app, and ask the user
  before adding automated tests.
