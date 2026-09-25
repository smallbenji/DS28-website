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
  (`DS.Website/Program.cs`). Do not reintroduce Keycloak based on old planning
  material.
- Current global permissions are defined in `DS.Website/Roles.cs`. `AppGroups`
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

## C# controller style

Use `DS.Website/Controllers/ApiControllers/ActivityApiController.cs` as the
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
namespace DS.Website.Controllers
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

The frontend uses Vue and Buefy. Use existing Buefy components and surrounding
project patterns for forms, buttons, tables, notifications, and step flows.

## Verification

- Review formatting against this section and use
  `ActivityApiController.cs` for patterns not specified here; an automatic
  formatter alone does not establish compliance with the user's preferred
  style.
- Run `git diff --check` after edits.
- For C# changes, build with
  `dotnet build DS.Website/DS.Website.csproj --no-restore --disable-build-servers -m:1`.
- For frontend changes, run `npm run build` in `DS.Website/js`. If both builds
  are needed, finish the frontend build first: it replaces assets consumed by
  the backend build.
- Do not add tests solely for formatting changes.
