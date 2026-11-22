# Background job checks

Run from the repository root with .NET 10:

```sh
dotnet run --project tests/BackgroundJobChecks/BackgroundJobChecks.csproj --disable-build-servers
```

The checks verify that job arguments and error texts shown on `/admin/jobs` are
masked before they leave the server, and that every action on
`BackgroundJobApiController` requires `HangfireManage` while reading requires
`HangfireView`. They need no database and add no test framework dependencies.
The executable returns a nonzero exit code if any check fails.
