# Security alert checks

Run from the repository root (no database required):

```sh
dotnet run --project tests/SecurityAlertChecks/SecurityAlertChecks.csproj
```

Checks CodeQL #86–#88: null bodies, missing/blank credentials and rejected
credentials return HTTP 400 without enabling/disabling 2FA, resetting a key or
issuing a successful login. Nonempty credentials must reach verification.
Checks #89–#90: application and antiforgery cookies require HTTPS in both
Development and Production.

The bypass alerts are false positives in the input guards, not justification
to remove validation. These checks do not replace end-to-end Identity tests.
