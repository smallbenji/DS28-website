# Adgang og brugere

## Status

Implementeret: globale roller, brugerstyring, invitationer, "Vis som",
profil, seneste login og aktivitet samt mailnotifikationer for `NewUser` og
`BoardCreated`. Notifikationer er fortsat en nice-to-have med kun to typer.
Stillinger står i [organization.md](organization.md), login og tokens i
[security.md](../security.md).

## Nuværende adfærd

### Roller og rettigheder

- `src/DS.Domain/Roles.cs` er kilden til de globale roller. `AppGroups` er
  Identity-roller og adgangsbundter (SysAdmin, CampAdmin, EventAdmin,
  ActivityAdmin, ActivityUser, FinanceAdmin, PRAdmin, FoodAdmin,
  PersonalAssistant, User, McpUser). `AppRoles` er de fine-grained
  rettigheder, og `AppAccess.Matrix` fordeler dem på grupperne.
  `AppAccess.AssignableGroups` og `CanAssignRole` afgør, hvem der må tildele
  hvilke grupper.
- `AppGroups` betyder global adgang, ikke spejdergruppe. Spejdergruppe-
  medlemskab er `User.Group`. Alle medlemmer af en gruppe har samme adgang til
  forhåndstilmelding og gruppens brugere; der er ingen gruppeejer eller
  gruppeadministrator. Aktivitets-teamlederrollen giver ikke en lederrolle i
  spejdergrupper.
- Effektive grupper er direkte Identity-roller plus grupper fra stillinger,
  se [organization.md](organization.md). `ClaimsTransformer` bygger claims
  pr. request og cacher dem pr. bruger (`user-roles:{id}`).
- `McpUser` er en særskilt gruppe uden rettigheder i matricen. Den tildeles
  eksplicit og kan ikke arves gennem stillinger, se [mcp.md](mcp.md).
- `AuditLogView` er tildelt SysAdmin og CampAdmin, men auditloggen findes
  ikke, se [admin-exports.md](admin-exports.md).

### Brugerstyring

- `/admin/user/:id?` bruger `UserService`
  (`src/DS.Application/Services/Users`) gennem `UserApiController`: liste med
  søgning, detaljer, oprettelse, invitation, roller, stillinger, låsning,
  nulstillingslink, sletning, profilbillede, notifikationer og adgangstokens.
  Rolletildeling tjekkes mod brugerens direkte Identity-roller, ikke mod
  stillinger eller "Vis som". Stamdata gemmes fra en kladde med »Gem«.
- Invitationer uden gruppetilknytning accepteres gennem `InvitationService`
  (`src/DS.Application/Services/Invitations`), som opretter brugeren med
  roller, aktivitetsteam og stilling. Brugernavnet er invitationens email,
  som i de øvrige oprettelsesflows. Gruppeinvitationer er et separat flow, se
  [groups-registration.md](groups-registration.md).
- Glemt adgangskode er selvbetjent på `/forgot-password` og sender et link til
  `/reset-password/{brugerId}?token=...`. Detaljer i
  [security.md](../security.md).

### "Vis som" (impersonation)

- En SysAdmin kan vælge en vilkårlig `AppGroups`-gruppe i brugermenuen nederst
  i sidebaren ("Vis som …"). Den valgte gruppe anvendes server-side i hele appen,
  så `[Authorize(Roles=…)]`, `IsInRole`, HQ-fliser, Hangfire, eksport,
  søgning og `/api/v1/me` opfører sig som gruppen.
- Identiteten (bruger-id, navn, billede, 2FA-krav) ændres ikke. Tavleadgang
  følger de effektive SysAdmin/CampAdmin-claims, så "Vis som" en anden rolle
  fjerner den globale tavleadgang, men bevarer personlige medlemskaber.
- Under visning viser sidebaren et gult banner med rollen og »Stop visning«.
  Efter start og stop sendes brugeren til forsiden.
- Endpoints: `POST /api/v1/impersonation/start` og `stop`. Cookie og
  sikkerhedskontrol står i [security.md](../security.md).

### Profil

- `/profile` viser email som skrivebeskyttet tekst (fra `GET /api/v1/me`),
  stamdata, adgangskode, profilbillede, notifikationer, MCP-forbindelse,
  totrinsbekræftelse, recovery codes, passkeys og adgangstokens.
- Profilbilleder beskæres i browseren med `vue-advanced-cropper`
  (`ProfilePictureUpload.vue`) og uploades som 512 x 512 PNG. Lagring og
  billedbehandling står i [files-google-drive.md](files-google-drive.md).
- Brugermenuen viser profilbillede, navn og titel. `MeDto.Title` er
  stillingens titel, hvis brugeren beklæder en stilling, og ellers
  `AppAccess.ResolveGroupTitle`: titlen på den første gruppe i
  `AppGroups`-enumets rækkefølge, som derfor er en prioriteret rækkefølge.

### Seneste login og aktivitet

- `User.LastLoginAt` sættes af `LoginTrackingSignInManager` ved hvert
  vellykket login (adgangskode, 2FA, recovery code, passkey og automatisk login
  efter invitation eller forhåndstilmelding), ikke ved `RefreshSignInAsync`
  eller token-adgang.
- `User.LastActiveAt` sættes af `UserActivityMiddleware` ved godkendte
  requests med cookie, personligt token eller MCP-token, højst hvert femte
  minut pr. bruger.
- Kun ét tidsstempel af hver gemmes, ingen historik. Brugerlisten viser
  "Sidst aktiv", detaljevisningen begge. Brugere uden registrering vises som
  "Intet login registreret".

### Notifikationer

- Al mail sendes asynkront: `EmailService` bygger indhold, validerer adressen
  og enqueuer `MailJobs.Send` gennem `IMailQueue`. `DS.NotificationsEnabled`
  gater alle mails ved enqueue.
- Brugere vælger notifikationer på profilen. Valgene gemmes i
  `user_notification_preference`, og `NotificationPermissions` afgør, hvilke
  typer en bruger må vælge (`canSubscribe`). Brugerstyringen kan se og ændre en
  anden brugers valg (`/api/v1/user/{id}/notifications`, kræver `UsersView`);
  målbrugerens roller afgør de mulige typer. Logikken deles i
  `NotificationPreferenceService`.
- `NewUser` kræver `UsersView` og sendes fra `NotificationUserManager`, som
  overskriver `CreateAsync`, så alle oprettelsesveje fanges.
- `BoardCreated` ("Ny tavle oprettet") kræver `BoardViewAll` og sendes fra
  `BoardService.CreateBoard`, også ved oprettelse via MCP. Mailen har tavlens
  navn, opretterens navn og et link. Rollen tjekkes igen ved afsendelse, og
  opretteren får ikke mail om sin egen tavle.
- Afmelding sker ved at fjerne krydset; der er ingen separat
  afmeldingsmulighed.

## Beslutninger

- 2026-09-29: Notifikationer starter som mail.
- 2026-10-06: Mails enqueues direkte som Hangfire-jobs. Afløser
  `EmailOutbox`, dispatcheren, mailkø-siden og `EmailOutboxView`.
- 2026-10-09: Brugervalgte, rollegatede notifikationer med `NewUser`. Afløser
  den faste modtager `DS__NewUserNotificationEmail`.
- 2026-10-12: Ægte server-side impersonation for alle `AppGroups` fra
  brugermenuen, efter brugerens eksplicitte valg frem for en frontend-preview.
- 2026-10-18: "Vis som" en anden rolle fjerner global tavleadgang, men bevarer
  personlige tavlemedlemskaber.
- 2026-10-19: Brugeradministration, invitationer og konto-flows flyttes til
  Application-services uden ændrede regler.
- 2026-10-19: Email vises skrivebeskyttet på profilen og kan ikke ændres der.
- 2026-11-21: Brugerstyringen kan ændre andres notifikationer.
- 2026-11-21, DS-64: Brugernavnet ved invitation er emailen. Afløser fornavn
  plus efternavn, som fejlede med æ, ø og å og ved ens navne.
- 2026-11-21: Nuxt-frontenden viser et gult banner under "Vis som". Afløser
  beslutningen om ingen global markering i den arkiverede frontend.
- 2026-11-22, DS-65: Notifikationen `BoardCreated` og app-rollen
  `BoardViewAll` (SysAdmin og CampAdmin).
- 2026-11-22, DS-84 og DS-85: Seneste login og seneste aktivitet med ét
  tidsstempel hver af hensyn til dataminimering.

## Åbne spørgsmål

- DS-65's standardvalg afventer bekræftelse: modtagere er abonnenter med
  SysAdmin eller CampAdmin, SysAdmin kan vælge den, og opretteren får ikke
  mail.
- Flere hændelsestyper (fx ny patrulje eller ændret deltagerantal) og deres
  rollegating er ikke fastlagt.
- Migrerede invitationer fra v0.1.6 kan mangle email eller roller, hvilket kan
  give fejl ved accept. Frontendens typer antager stadig ikke-null navne og
  email.
- Hvilke rettigheder de enkelte roller i det oprindelige udkast skal have, er
  ikke afklaret.

## Kodehenvisninger

- `src/DS.Domain/Roles.cs`
- `src/DS.Backend/ClaimsTransformer.cs`
- `src/DS.Backend/AppClaimsPrincipalFactory.cs`
- `src/DS.Backend/Controllers/ApiControllers/UserApiController.cs`
- `src/DS.Backend/Controllers/ApiControllers/ImpersonationApiController.cs`
- `src/DS.Backend/services/UserActivityMiddleware.cs`
- `src/DS.Application/Services/Users`
- `src/DS.Application/Services/Invitations`
- `src/DS.Application/Services/Profile`
- `src/DS.Application/Services/Notifications`
- `src/DS.Application/Services/Email`
- `src/DS.Infrastructure/Identity/NotificationUserManager.cs`
- `src/DS.Infrastructure/Identity/LoginTrackingSignInManager.cs`
- `src/DS.Infrastructure/Jobs/MailJobs.cs`
- `src/DS.Frontend/src/Views/UserManagement.vue`
- `src/DS.Frontend/src/Views/Profile.vue`
- `src/DS.Frontend/src/Components/User/ProfilePictureUpload.vue`
- `src/DS.Frontend/src/Components/ImpersonationModal.vue`
- `src/DS.Frontend/src/Layouts/DashboardLayout.vue`
