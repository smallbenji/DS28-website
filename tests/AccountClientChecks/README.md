# Account and client checks

Run `bash tests/AccountClientChecks/run.sh` from the repository root with
.NET 10 and Docker available. The runner creates and removes a disposable
PostgreSQL 16 database and never uses the application database. Mail is captured
in memory, without SMTP or Hangfire execution.

Checks cover Identity registration through Infrastructure's notification manager,
new-user notifications, known and unknown reset emails, invalid and valid reset
tokens, live HTTP checks for missing/invalid/valid reset-link CSRF tokens,
the shared non-cacheable token endpoint, global PUT/DELETE protection, and
rejection of a request token without its cookie. The HTTP host uses Website's
actual MVC registration. Client checks cover validation, duplicate client IDs, secret generation and rotation,
preservation of unrelated descriptor settings, public-client PKCE and deletion.
