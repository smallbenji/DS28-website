# Project conventions

## Project purpose and planning context

DS28 supports the digital operation of Distriktssommerlejr 2028: group
registration, activities, materials, finances, and a shared HQ dashboard.

Read [the project plan](docs/project-plan.md) when working on domain
behavior, permissions, registration dates, or architecture. It records the
user's starting point, subsequent decisions, and open questions; it is **a draft, not an adopted
specification or a request to implement every listed feature**.

- Use the current user request and explicit subsequent decisions to determine
  scope. Use the draft to understand intent, not to silently expand a task.
- Inspect the implementation before assuming that a planned feature exists.
  When the plan and code differ, distinguish intended behavior from current
  behavior and surface discrepancies relevant to the task.
- Keycloak has been explicitly dropped from the plan. ASP.NET Core Identity
  and OpenIddict are the current authentication foundation
  (`DS.Backend/Extensions/ServiceExtensions.cs`). Do not reintroduce Keycloak based on old planning
  material.
- Current global permissions are defined in `DS.Backend/Roles.cs`. `AppGroups`
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

## Keep the project plan current

Updating `docs/project-plan.md` is part of completing work that establishes or
changes a project decision. Do this in the same task, without waiting for a
separate documentation request.

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
- Record the reasoning in `docs/project-plan.md` instead, which is where
  decisions belong.
- An exception is a short note on a non-obvious security invariant, where a
  future change would otherwise silently introduce a bug. Keep it to one line.

## C# controller style

Use `DS.Backend/Controllers/ApiControllers/ActivityApiController.cs` as the
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
(`DS.Backend/Program.cs`).

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
- **Start**: when you begin working on a card, first move it to `I gang` and
  assign the person doing the work.
- **Pull request**: title it `[DS-<number>]: <short description>` and link it
  on the card with `add_pull_request`. Link every pull request that belongs to
  the card.
- **Merged**: when the pull request is approved and merged, move the card to
  `Ikke udgivet`.
- **Released**: when a version tag containing the change is deployed, move
  the released cards to `Færdig`. `Færdig` means released, not merely merged.
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
- The release workflow builds and publishes both Website and Worker using a
  matrix, with the same version tags. Worker uses the `-worker` image suffix.

The project uses GitHub Actions for CI/CD (see `.github/workflows/deploy.yml`).

- **Trigger**: Runs only on semantic version git tags matching `v[0-9]+.[0-9]+.[0-9]+*` (e.g., `v0.1.21`, `v1.2.3-rc1`). Also supports manual runs via `workflow_dispatch`.
- **Registry**: Images are published to GitHub Container Registry (`ghcr.io`) as `ghcr.io/smallbenji/ds28-website` and `ghcr.io/smallbenji/ds28-website-worker`.
- **Build**: Uses Docker Buildx with Docker driver and a matrix for `Containerfile.Website` and `Containerfile.Worker` in the repo root (`context: .`), targeting `linux/amd64`.
- **Tags**: Automatically generated from the SemVer tag - exact (`{{version}}`), minor (`{{major}}.{{minor}}`), major (`{{major}}`), and `latest` (for all semver tag pushes).
- **Push**: Images are pushed on tag triggers (manual runs still build but the workflow pushes as configured; see workflow for details).

To deploy a new version: bump the version in `src/DS.Domain/DSVersion.cs`, create a commit with message "Bump version", tag that commit with `vX.Y.Z`, and push the commit and the tag. The workflow triggers on tag pushes; pushes to master are allowed but the deploy only runs for semver tags. The current version is exposed via the API (`/api/version`) from `DSVersion`.

- Production at `hq.distriktssommerlejr.dk` is the only environment. There is no
  staging host. Keep `master` as the only long-lived branch; a tag is the
  release, and there is no separate production or release branch.
- Deploy the exact image built for a tag. Do not rebuild for production.
- Deploying to production is a deliberate step, not a side effect of pushing a
  tag, and is gated on approval of the `production` GitHub Environment.
- Database migrations must stay backwards compatible for one release
  (expand/contract: add, use, then remove), so the previous tag can be
  redeployed if a release has to be rolled back.
- Take a database dump before deploying a tag that contains a migration.

## Verification

- Review formatting against this section and use
  `ActivityApiController.cs` for patterns not specified here; an automatic
  formatter alone does not establish compliance with the user's preferred
  style.
- Run `git diff --check` after edits.
- For C# changes, build with
  `dotnet build DS.Backend/DS.Backend.csproj --no-restore --disable-build-servers -m:1`.
- For frontend changes, run `npm run build` in `src/DS.Frontend`. If both builds
  are needed, finish the frontend build first: it replaces assets consumed by
  the backend build.
- Do not add tests solely for formatting changes.
