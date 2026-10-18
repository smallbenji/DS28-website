# Board access checks

Run `bash tests/BoardAccessChecks/run.sh` from the repository root with .NET 10
and Docker available. The runner creates and removes a disposable PostgreSQL 16
database; it never uses the application database.

Checks cover migration of existing boards, automatic creator membership,
invitation chains, duplicate invitations, administrator access, denied access
for outsiders across board operations, and visibility of shared-card placements.
Card color checks cover migration defaults, preserved colors across upgrades,
named board palette CRUD, board isolation, validation, creation, updates,
clearing, database constraints and shared colors across boards. Palette edits
and deletion must not recolor existing cards.
