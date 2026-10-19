# Project DS28

## Overview
This is the primary project repo for DS28 (Distriktssommerlejr 2028). Here we will create many projects which together will ensure that we can get the right data from every place delivered the right places. This project will start August 2026 and go on until the summer 2028. Hence the name ;)

## How to run
1. Open workspace in vscode
2. Launch selected project
3. Go to JS folder of project
4. Run `npm install` & `npm run dev´

## Template project
When starting development on a new application. You need to copy the Template project, then you have everything you need to get started. Remember to change the name of the namespaces.

## Hangfire worker

Start `DS.Worker` alongside Website to execute background jobs:

```sh
dotnet run --project DS.Worker/DS.Worker.csproj
```

Configure `DS__ConnectionString` with the same database as Website and
`DS__FileStoragePath` with access to the same uploaded files. SMTP settings are
read from the same `DS` configuration section. The worker waits for Website's
database migrations before registering recurring jobs and starting Hangfire.
Its Hangfire server name is the host's hostname (`Dns.GetHostName()`).

`docker compose up --build hq worker` builds both services. Compose loads their
configuration from `.env` and shares the `files` volume. The worker has no HTTP
port; Website retains the Hangfire dashboard and enqueues jobs. The current
GHCR workflow publishes both `ghcr.io/smallbenji/ds28-website` and
`ghcr.io/smallbenji/ds28-website-worker` with the same version tags. Compose
builds Worker using `Containerfile.Worker`.

## Services

- Wordpress (The main site)
- HQ
    - The login portal where you can find the services you have permissions to access.
- Material Module
    - The place where you can see which materials the different activities requests and have ordered
- Activity System
    - The place to type in the metadata for every activity
- Economy Module
    - The place to get the correct prices and what to send invoices on and what to pay people
- Group Management
    - The place for the groups to enter how many scouts the're will be
