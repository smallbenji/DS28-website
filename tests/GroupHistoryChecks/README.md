# Group history checks

Run with .NET 10:

```sh
dotnet run --project tests/GroupHistoryChecks/GroupHistoryChecks.csproj --disable-build-servers
```

Uses an isolated EF Core in-memory database and the application's Identity
configuration. Checks the combined controller/action authorization policy,
group/year scoping, separate current and guest counts, missing attendance,
empty/unknown groups, and exclusion of source metadata and shared-DTO history.
No production database or imported registration files are needed.

These checks do not exercise HTTP middleware, PostgreSQL translation, or browser
rendering.
