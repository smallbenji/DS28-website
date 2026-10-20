# Audit and soft-delete checks

Run `bash tests/AuditSoftDeleteChecks/run.sh` from the repository root with
.NET 10 and Docker available. The runner creates and removes a disposable
PostgreSQL 16 database; it never uses the application database.

Checks cover automatic `created_at`/`updated_at` timestamps on insert and
update, the global `deleted_at` query filter, and `IgnoreQueryFilters` for
retrieving soft-deleted rows. They also verify that `Remove` soft-deletes
entities implementing `ISoftDeleteable` while other entities are still hard
deleted, that deleting a position soft-deletes its personal assistant, that a
position can receive a new assistant after the previous one is soft-deleted,
that scouts belonging to a soft-deleted group leave the participant export, and
that `FindAsync` honors the query filter.
