# Camp day checks

Run `bash tests/CampDayChecks/run.sh` from the repository root with .NET 10
and Docker available. The runner creates and removes a disposable PostgreSQL 16
database; it never uses the application database.

Checks cover creating camp days, sorting by date, rejecting a duplicate date
on create and update, validation of missing date and half, updating date and
half, deleting, the database check constraint on `half`, and that changing the
days requires `PreSignupManage` while reading only requires login.
