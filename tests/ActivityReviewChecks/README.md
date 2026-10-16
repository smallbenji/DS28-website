# Activity review checks

Run from the repository root with .NET 10 and Docker available:

```sh
./tests/ActivityReviewChecks/run.sh
```

The runner starts a disposable PostgreSQL 16 container, runs the project's
embedded migrations, verifies the review migration on an existing activity,
and exercises the workflow, required fields, feedback, budget preservation,
optimistic concurrency and authorization. The container is removed on exit.
The executable returns a nonzero exit code if any check fails. It adds no test
framework dependencies.
