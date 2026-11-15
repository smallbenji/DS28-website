# Access token checks

Run `bash tests/AccessTokenChecks/run.sh` from the repository root with .NET 10
and Docker available. The runner creates and removes a disposable PostgreSQL 16
database; it never uses the application database.

Checks cover token creation, that only the SHA-256 hash is stored, expiry and
revocation, rejection for locked users, the bearer authentication handler, and
that cookie writes still require an antiforgery token while bearer writes and
`[IgnoreAntiforgeryToken]` actions do not.
