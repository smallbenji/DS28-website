# Grupper og tilmelding

## Status

Implementeret: spejdergrupper, gruppemedlemmer og -invitationer,
forhåndstilmelding, endelig tilmelding med spejdere og patruljer, lejrdage,
deltagelsesdage og manuelle tilmeldingsflag. Besluttet, ikke bygget:
aktivitetsønskerunde og aktivitetstilmelding for grupper.

## Nuværende adfærd

### Grupper og medlemmer

- En bruger knyttes til en spejdergruppe med `User.Group`. Alle medlemmer har
  lige adgang til gruppens forhåndstilmelding, endelige tilmelding og
  brugere. Der er ingen gruppeejer eller gruppeadministrator.
- `/group` viser gruppens navn, ID, distrikt, brugere, antal spejdere,
  patruljer og brugere, status for manglende deltagelsesdage og kort for
  forhåndstilmelding, endelig tilmelding og administration af brugere.
  Aktivitetsønskerunde og aktivitetstilmelding vises som »Ikke tilgængelig
  endnu«.
- `/group/members` viser medlemmer, invitation med email og link, afventende
  invitationer med annullering og fjernelse med bekræftelse. Fjernelse
  ophæver gruppetilknytningen og bevarer kontoen. Et medlem kan ikke fjerne sig
  selv her.
- Gruppeinvitationer sender automatisk en email med et link til
  `/group-invitation/{id}` (bygget af `DS.PublicBaseUrl`), som også kan deles
  manuelt. Accept sker i `GroupInvitationService` med en transaktionel claim af
  invitationen og atomisk gruppetilknytning. Nye brugere logges ind efter
  commit.
- Staben administrerer grupper på `/admin/groups/:id?` (kræver `GroupsView`):
  stamdata, spejdere, patruljer, medlemskaber, gruppens brugere og
  historiske tal (kun `AdminAccess`, se
  [historic-registration.md](historic-registration.md)). Grupper
  soft-slettes.

### Forhåndstilmelding

- Det offentlige flow `/group-pre-signup` starter med et gruppenummer, gemmer
  `GroupPreSignup` for syv aldersgrupper, opretter en bruger og logger ind på
  `/group`. Det sker i en transaktion med rækkelåse.
- Har gruppen allerede en forhåndstilmelding (fx oprettet af staben), men
  ingen bruger, kan flowet oprette gruppens første bruger med tallene
  forudfyldt. Så snart en bruger er tilknyttet, spærres flowet for gruppen.
- `/group/pre-signup` lader gruppens medlemmer redigere tallene. Mangler en
  forhåndstilmelding, oprettes en tom. Brugere uden gruppe får ikke adgang.
- Når forhåndstilmeldingen er lukket, blokerer backenden både nye
  tilmeldinger og ændringer af tal. Eksisterende tilmeldinger kan ses.

### Endelig tilmelding

- `/group/final-signup` opretter og sletter spejdere (navn, fødselsdato, køn)
  og patruljer og tildeler spejdere til patruljer. En spejder kan være i flere
  patruljer. Endpoints under `/api/v1/group/scouts` og `/api/v1/group/patrols`
  kontrollerer, at ressourcen tilhører brugerens gruppe.
- Flaget for endelig tilmelding gater hverken siden eller endpointsene.

### Lejrdage og deltagelsesdage

- Lejrens dage er data i `camp_day` og redigeres i kortet "Lejrdage" på
  `/admin/camp-settings` med `PreSignupManage`. Hver dag har en unik dato og
  hører til 1. halvdel, 2. halvdel eller begge (skiftedag). Ingen datoer er
  hardkodet. `GET /api/v1/camp-days` kræver kun login; ændringer kræver
  `PreSignupManage`. En dublet giver 409, og en dag med tilmeldte spejdere kan
  ikke slettes (409 med antal), men datoen kan rettes.
- Spejderens dage gemmes i `scout_camp_day` og vises som `campDayIds`.
  `PUT /api/v1/group/scouts/days` sætter dagene for en eller flere spejdere og
  kan nøjes med spejdere uden dage (`onlyScoutsWithoutDays`).
- Kun spejdere registreres med dage. Enheden er hele dage. En spejder uden dage
  betyder "mangler at blive udfyldt"; der findes ingen tilstand for 0 dage, og
  en tom liste afvises. En spejder, der ikke deltager, slettes, og så fjernes
  dagene.
- På siden vælges Hel uge, 1. halvdel, 2. halvdel eller Enkelte dage pr.
  spejder, enkeltvis, for alle eller pr. patrulje. Siden viser deltagere pr. dag,
  en advarsel med spejdere uden dage, et filter og en knap, der giver alle uden
  dage samme periode. Den sidste dag under Enkelte dage kan ikke fjernes.
- Sletning af en hel gruppe soft-sletter kun gruppen; spejdernes dage bliver
  liggende og tæller med i sletteblokeringen af en lejrdag.
- Datoer regnes i Europe/Copenhagen. Data kan svare på, hvem der er tilmeldt en
  dato, og hvem der mangler dage.

### Tilmeldingsflag

- `registration_settings` har fire flag, som åbnes og lukkes manuelt på
  `/admin/camp-settings` via `PUT /api/v1/registration-settings` (kræver
  `PreSignupManage`; læsning er offentlig): forhåndstilmelding, endelig
  tilmelding, aktivitetsønskerunde og endelig aktivitetstilmelding. Delvise
  opdateringer bevarer udeladte flag.
- Flagene er uafhængige af datoerne i den foreløbige tidsplan. Nye databaser
  starter med alle flag lukket. Kun forhåndstilmeldingsflaget gater noget i
  dag.

## Beslutninger

- 2026-09-29: Gruppemedlemmer uden forhåndstilmelding får oprettet en tom.
  Det offentlige flow kan oprette en gruppes første bruger én gang.
- 2026-09-30: Nye lejrindstillinger starter med begge tilmeldinger lukket.
  Overgangen fra v0.1.6 bevarede de eksisterende værdier.
- 2026-10-02: Endelig tilmelding med spejdere og patruljer.
- 2026-10-19: Gruppeinvitationer sender automatisk email. Afløser kun manuel
  deling af links.
- 2026-10-19: Gruppeflows flyttes til Application-services uden ændrede regler.
- 2026-11-21, DS-17: Deltagelsesdage registreres i den endelige tilmelding.
- 2026-11-23, DS-72 og DS-96: Lejrdage som data og deltagelsesdage pr. spejder.
  `scout_signup` med fri dato fjernes i en senere udgivelse
  (expand/contract). Valgene om sidste dag og gruppesletning er bekræftet af
  Benjamin 2026-11-23.
- DS-92: Flag for aktivitetsønskerunde og endelig aktivitetstilmelding. De
  gater intet, før flowene er bygget.

## Åbne spørgsmål

- Hvad betyder lukket forhåndstilmelding for eksisterende grupper: må de
  ændre deres tal efter lukning? Den foreløbige fortolkning blokerer ændringer.
- Skal flaget for endelig tilmelding gate `/group/final-signup`, og hvad betyder
  lukket endelig tilmelding (DS-98)? Lukket låser i dag ikke dagene.
- DS-92's standardvalg afventer bekræftelse: samme rolle (`PreSignupManage`),
  uafhængige flag, og betydningen af lukket aktivitetstilmelding.
- Registreres ledere og hjælpere med dage (DS-97)?
- Hvordan aktivitetstilmeldingen bruger deltagelsesdage, og hvordan spejdere
  uden dage håndteres der.
- 18. oktober eller "(nov)" som åbningsdato for forhåndstilmelding, og hvad
  "lukker" betyder for endelig tilmelding.

## Kodehenvisninger

- `src/DS.Application/Services/Groups/GroupService.cs`
- `src/DS.Application/Services/Groups/GroupPreSignupService.cs`
- `src/DS.Application/Services/Groups/GroupMembersService.cs`
- `src/DS.Application/Services/Groups/GroupAdministrationService.cs`
- `src/DS.Application/Services/Groups/GroupLookup.cs`
- `src/DS.Application/Services/Invitations`
- `src/DS.Application/Services/Registration/RegistrationSettingsService.cs`
- `src/DS.Application/Services/Registration/CampDayService.cs`
- `src/DS.Backend/Controllers/ApiControllers/GroupApiController.cs`
- `src/DS.Backend/Controllers/ApiControllers/CampDayApiController.cs`
- `src/DS.Backend/Controllers/ApiControllers/RegistrationSettingsApiController.cs`
- `src/DS.Frontend/src/Views/Group.vue`
- `src/DS.Frontend/src/Views/GroupFinalSignup.vue`
- `src/DS.Frontend/src/Views/GroupPreSignup.vue`
- `src/DS.Frontend/src/Views/GroupPreSignupEdit.vue`
- `src/DS.Frontend/src/Views/GroupMembers.vue`
- `src/DS.Frontend/src/Views/CampSettings.vue`
- `src/DS.Frontend/src/Components/CampDays.vue`
- `src/DS.Frontend/src/lib/campDays.ts`
