# Sikkerhed og autentifikation

## Status

Implementeret. Filen er det kanoniske sted for login, OpenIddict,
antiforgery, personlige adgangstokens og hærdning. MCP-adgang står i
[mcp.md](features/mcp.md), og impersonation-adfærd i
[access-and-users.md](features/access-and-users.md).

## Nuværende adfærd

### Identitet og login

- ASP.NET Core Identity og OpenIddict er autentifikationsgrundlaget,
  konfigureret i `src/DS.Backend/Extensions/ServiceExtensions.cs`. Login, 2FA,
  passkeys og cookies ligger i Website. Brugeroprettelse, glemt adgangskode og
  nulstilling ligger i `AccountService`
  (`src/DS.Application/Services/Accounts`).
- Login- og antiforgery-cookies har altid `Secure`, også i Development.
  Lokal browserudvikling kræver derfor HTTPS.
- `PasswordSignInAsync` bruger `lockoutOnFailure: true`, så Identitys
  standard (5 forsøg, 5 minutter) gælder. Politikken `auth` begrænser
  `AuthApiController` til 20 kald pr. minut pr. IP (login, registrering, glemt
  adgangskode og 2FA).
- Adgangskodepolitikken er `RequiredLength = 4` uden kompleksitetskrav.
- Glemt adgangskode svarer ens for kendte og ukendte emails. Linket bruger
  `DS.PublicBaseUrl` og Identitys `DataProtectorTokenProvider` (24 timer) uden
  streng engangsbrug ud over security stamp.
- `UseForwardedHeaders` står først i pipelinen og stoler på alle proxies
  (tomme `KnownProxies` og `KnownIPNetworks`). Det er kun forsvarligt, fordi
  app-porten kun eksponeres gennem Traefik på det interne Docker-netværk.

### OpenIddict-klienter

- Klienter administreres på `/admin/openiddict` med approllen
  `OpenIddictManage` (kun SysAdmin). Felter: Client ID, navn,
  public/confidential, redirect-URL'er, scopes (`openid`, `profile`, `email`,
  `roles`), PKCE og loginrettigheder. Public-klienter kræver PKCE. Nye og
  redigerede klienter får refresh-grant-rettigheden.
- Redirect-URL'er kræver HTTPS, undtagen HTTP på loopback, og tillader ikke
  wildcard, fragment eller brugeroplysninger.
- Secrets genereres på serveren, hashes af OpenIddict og vises kun ved
  oprettelse, rotation eller skift til confidential. Rotation erstatter
  straks det gamle secret. Redigering bevarer den eksisterende descriptor.
  Sletning fjerner også klientens tokens og autorisationer.
- Hver klient kræver mindst én valgt `AppRoles`-rettighed (OR), gemt som
  JSON-array i klientens `properties` under `ds28:required_app_roles`.
  Klienter uden nøglen kræver `WordPressEditor` eller `WordPressAdmin`; en tom
  eller ugyldig værdi afviser login. `/connect/authorize` tjekker kravet mod
  de effektive rettigheder (inklusive stillinger og "Vis som"), og
  tokenudvekslingen tjekker igen mod brugerens reelle roller og stillinger.
- Klienter bruger authorization code med implicit consent og refresh tokens.
  Access tokens lever én time, refresh tokens 14 dage med rotation og glidende
  udløb. Klienten skal bede om `offline_access`. Hver udveksling genlæser
  brugeren og klientens krav, afviser slettede eller låste brugere og tjekker
  security stamp ved refresh. Refresh kan indsnævre scopes, ikke udvide dem.
- `resource` valideres ikke (`DisableResourceValidation`), og
  ressourcerettigheder pr. klient ignoreres (`IgnoreResourcePermissions`),
  fordi der kun er én API og kun operatør-oprettede klienter. Kommer der en
  ressource mere, skal begge dele ændres sammen.
- `userinfo`-svarets `roles` er WordPress-specifikt. DCR og CIMD er ikke
  implementeret.

### Antiforgery

- `CookieAntiforgeryFilter` er den globale MVC-regel: cookie-baserede
  POST, PUT, PATCH og DELETE kræver et gyldigt antiforgery-token. Sikre metoder
  og Bearer-kald springes over, og `[IgnoreAntiforgeryToken]` respekteres.
- `GET /api/v1/antiforgery` udsteder et ikke-cachebart token. Den fælles
  API-klient sender det som `RequestVerificationToken` på ændringskald og
  rydder cachen efter vellykkede ændringskald og ved HTTP 400. Cookien er
  HttpOnly og SameSite=Strict.
- Undtaget efter eksplicit valg: `AuthApiController` (inklusive logout),
  `PasswordResetApiController`, `InvitationApiController`,
  `GroupInvitationApiController` og `GroupPreSignupApiController`. Det er en
  accepteret begrænsning: login-CSRF er muligt, og gruppeinvitationer kan bruge
  en eksisterende session.
- `/connect/authorize`, `/connect/token` og `/connect/userinfo` er undtaget af
  hensyn til OIDC-protokollen. GET-baseret logout og Google-redirect/callback
  er uændrede; callbacken har sin egen state-kontrol.

### Personlige adgangstokens

- Brugere med Identity-rollen `McpUser` kan oprette, se én gang og
  tilbagekalde tokens på profilen (`GET`, `POST /api/v1/access-tokens` kræver
  rollen; `DELETE /api/v1/access-tokens/{id}` gør ikke, så tilbagekaldelse
  aldrig blokeres).
- `user_access_token` gemmer kun en SHA-256-hash, navn, oprettelse, valgfrit
  udløb (standard 90 dage) og tilbagekaldelse. Brug registreres ikke.
- Schemet `AccessToken` læser `Authorization: Bearer`, og
  `IdentityOrAccessToken` vælger mellem cookie og token. `ClaimsTransformer`
  tilføjer grupper og stillinger, så tokenet arver brugerens rettigheder.
  Låste brugere afvises. Skal en agent have mindre adgang, oprettes en
  særskilt bruger.
- Allerede udstedte tokens virker mod den almindelige HTTP-API, også uden
  `McpUser`.
- Under brugerstyringen kan en admin med `UsersLock` se en brugers aktive
  tokens og tilbagekalde ét eller alle (`/api/v1/user/{id}/access-tokens`).
  Tokenværdi og hash returneres aldrig.

### Impersonation

"Vis som" (se [access-and-users.md](features/access-and-users.md)) bruger en
HttpOnly, SameSite=Strict og Data Protection-krypteret cookie
(`DS.Impersonation`). `ClaimsTransformer` anvender den kun, når brugerens
reelle grupper indeholder `SysAdmin`, så en udstedt cookie holder op med at
virke, hvis rollen fjernes. De reelle grupper gemmes som
`ds28:imp:origin`-claims.

### XSS

Kortbeskrivelser er HTML. `RichTextViewer` renser med DOMPurify før `v-html`,
og `htmlToPlainText` i `src/DS.Frontend/src/lib/util.ts` bruger `DOMParser`,
så event-handlere i beskrivelsen ikke kører. Backenden renser ikke
beskrivelser.

### Logning og afhængigheder

- Google-forbindelsesloggen indeholder ikke kontoens email. Mailjobbets logs
  indeholder ikke modtager, emne eller exceptiontekst. Hangfire gemmer fortsat
  payload og fejldetaljer i klartekst, så dashboard og database skal være
  beskyttet.
- `System.Security.Cryptography.Xml` og `System.Security.Cryptography.Pkcs`
  er pinnet til 10.0.13 (CVE-2026-96958, -96959 og -96960 og advisories i
  Pkcs 10.0.0). Kun `NU1510` undertrykkes. Runtimen skal også være mindst
  10.0.13, så containerne skal genbygges med opdaterede base-images.

## Beslutninger

- 2026-10-14: Gennemgang af fire fund. Forwarded headers flyttes først og
  stoler på proxyen, lockout slås til med rate limiting, impersonation kræver
  reel `SysAdmin`. Globalt PKCE-krav forkastes, fordi WordPress-klienten ikke
  sender `code_challenge`.
- 2026-10-19: OpenIddict-klienter administreres i HQ, og loginadgang styres af
  valgte `AppRoles`. Afløser den hardkodede WordPress-begrænsning.
- 2026-10-20: Global antiforgery-validering. Afløser beskyttelse alene på
  klientadministrationen.
- 2026-10-20: Secure cookies altid, ingen persondata i logs, CVE-pins.
  CodeQL-alerts #86, #87 og #88 er vurderet som falske positiver.
- 2026-11-15: Personlige adgangstokens. `CookieAntiforgeryFilter` afløser
  `AutoValidateAntiforgeryTokenAttribute`.
- 2026-11-16: MCP accepterer OpenIddict-tokens uden resource-validering.
- 2026-11-17, DS-36: Refresh tokens og `offline_access`. Udvider den
  tidligere code-only-afgrænsning.
- 2026-11-20: XSS i kortforhåndsvisning rettet med `DOMParser`.
- 2026-11-22: Adgangstokens kræver `McpUser`. Afløser, at alle indloggede
  kunne oprette tokens.
- DS-91: Admins kan se og tilbagekalde andres tokens med `UsersLock`.

## Åbne spørgsmål

- DS-91's rollevalg (`UsersLock`, i dag kun SysAdmin) afventer bekræftelse.
  Alternativet er `UsersView`, som også giver CampAdmin adgang.
- Skal Bearer-godkendelse mod HTTP-API'et også kræve `McpUser`, og skal tokens
  tilbagekaldes automatisk, når rollen fjernes?
- Skal OAuth-autorisationer kunne ses og tilbagekaldes i brugerstyringen, og
  skal "sidst brugt" registreres for tokens?
- PKCE bør kræves pr. klient, når en klient, der kan det, tages i brug.
- Rate limit er pr. IP, så brugere bag samme NAT deler kvote. 20 pr. minut kan
  justeres.
- Adgangskodepolitikken kan være et bevidst valg for deltagere og er ikke
  ændret.
- Skal kortbeskrivelser renses i backenden, så andre klienter ikke får rå HTML?
- Den admin-genererede nulstillingslink-funktion bruger requestens scheme og
  host i stedet for `DS.PublicBaseUrl`, og brugerfladens tekst om engangsbrug
  er upræcis.

## Kodehenvisninger

- `src/DS.Backend/Extensions/ServiceExtensions.cs`
- `src/DS.Backend/Program.cs`
- `src/DS.Backend/Authentication/CookieAntiforgeryFilter.cs`
- `src/DS.Backend/Authentication/AccessTokenAuthenticationHandler.cs`
- `src/DS.Backend/ClaimsTransformer.cs`
- `src/DS.Backend/Impersonation.cs`
- `src/DS.Backend/Controllers/ApiControllers/AccessTokenApiController.cs`
- `src/DS.Backend/Controllers/ApiControllers/UserApiController.cs`
- `src/DS.Application/Services/Accounts`
- `src/DS.Application/Services/OpenIddict`
- `src/DS.Infrastructure/OpenIddict`
- `src/DS.Frontend/src/lib/util.ts`
- `src/DS.Frontend/src/Components/RichText/RichTextViewer.vue`
- `src/DS.Backend/DS.Backend.csproj`
