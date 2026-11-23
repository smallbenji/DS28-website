# Scout camp day checks

Run `bash tests/ScoutCampDayChecks/run.sh` from the repository root with .NET
10 and Docker available. The runner creates and removes a disposable
PostgreSQL 16 database; it never uses the application database.

Checks cover creating a scout with and without days, that a new scout has no
days by default, replacing a scout's days, setting days for several scouts at
once, filling only scouts without days, rejecting an empty day list, unknown
camp days and scouts from another group, removing a deleted scout's days, that
a camp day with registered scouts cannot be deleted while its date can still
be changed, that a camp day without registrations can be deleted, and the
database constraints on `scout_camp_day`.
