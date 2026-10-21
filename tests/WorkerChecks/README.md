# Worker checks

Run `bash tests/WorkerChecks/run.sh` with .NET 10 and Docker available. The runner
creates and removes a disposable PostgreSQL database. It launches the real
worker process, verifies that it waits for migrations, applies Website's scripts,
and checks hostname-based Hangfire registration, recurring jobs and execution
of the OpenIddict pruning job. It does not use the application database or SMTP.
