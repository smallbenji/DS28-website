# Database diagram

Schema `ds28` (PostgreSQL). Alle tabeller ligger i schema `ds28` og bruger
snake_case. Kilden er `DS/Data/DataDbContext.cs`, `DS/Data/Configurations/*.cs`
og `DS/Migrations/*.sql`.

## Domene

```mermaid
erDiagram
    scout_group {
        int id PK
        text name
        text district "DANEHOF | FIONIA"
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at
    }

    scout {
        int id PK
        text name
        date birthday
        text gender "MALE | FEMALE"
        int group_id FK "RESTRICT"
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at
    }

    patrol {
        int id PK
        text name
        int group_id FK "CASCADE"
    }

    patrol_membership {
        int id PK
        int scout_id FK "CASCADE"
        int patrol_id FK "CASCADE"
        timestamptz joined_date
        bool is_patrol_leader
    }

    group_pre_signup {
        int id PK
        int group_id FK "CASCADE, UNIQUE"
        int beaver
        int wolf
        int junior
        int trop
        int senior
        int rover
        int leader
    }

    scout_signup {
        int id PK
        date day
        int scout_id FK "CASCADE, UNIQUE(scout_id, day)"
    }

    activity_team {
        int id PK
        text name
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at
    }

    activity {
        int id PK
        text name
        int activity_team_id FK "RESTRICT"
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at
    }

    activity_budget {
        int id PK
        int budget "CHECK >= 0"
        int activity_id FK "CASCADE, UNIQUE"
    }

    catalog_data {
        int id PK
        text name
        text summary
        text description
        int activity_id FK "CASCADE, UNIQUE"
    }

    activity_category {
        int id PK
        text name "UNIQUE"
    }

    catalog_data_category {
        int catalog_data_id PK,FK "CASCADE"
        int activity_category_id PK,FK "CASCADE"
    }

    activity_timeslots {
        int id PK
        int activity_id FK "RESTRICT"
        timestamptz start_time
        int duration "CHECK >= 1"
    }

    scout_activity_timeslot {
        int activity_timeslot_id PK,FK "CASCADE"
        int scout_id PK,FK "CASCADE"
    }

    activity_team_membership {
        int id PK
        text user_id FK "CASCADE"
        int activity_team_id FK "CASCADE"
        bool is_admin
    }

    material {
        int id PK
        text name
        numeric price "CHECK >= 0"
        text url
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at
    }

    material_order {
        int id PK
        int activity_id FK "RESTRICT"
        int material_id FK "RESTRICT"
        int quantity "CHECK > 0"
        timestamptz use_date
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at
    }

    scout_group ||--o{ scout : "har medlemmer"
    scout_group ||--o{ patrol : "har patruljer"
    scout_group ||--o| group_pre_signup : "har forudtilmelding"
    scout_group ||--o{ asp_net_users : "har brugere"
    scout_group ||--o{ user_invitation : "har invitationer"
    scout ||--o{ patrol_membership : "er med i"
    patrol ||--o{ patrol_membership : "har medlemmer"
    scout ||--o{ scout_signup : "tilmelder sig"
    scout ||--o{ scout_activity_timeslot : "tilmelder sig"
    activity_team ||--o{ activity : "arrangerer"
    activity_team ||--o{ activity_team_membership : "har medlemmer"
    activity ||--o| activity_budget : "har budget"
    activity ||--o| catalog_data : "har katalogdata"
    activity ||--o{ activity_timeslots : "har tidspunkter"
    activity ||--o{ material_order : "bestiller"
    activity_timeslots ||--o{ scout_activity_timeslot : "tilmeldes af"
    catalog_data ||--o{ catalog_data_category : "kategoriseres af"
    activity_category ||--o{ catalog_data_category : "indeholder"
    material ||--o{ material_order : "bestilles i"
```

## Brugere og roller

```mermaid
erDiagram
    asp_net_users {
        text id PK
        varchar user_name
        varchar normalized_user_name UK
        varchar email
        varchar normalized_email
        bool email_confirmed
        text password_hash
        text security_stamp
        text concurrency_stamp
        varchar phone_number
        bool phone_number_confirmed
        bool two_factor_enabled
        timestamptz lockout_end
        bool lockout_enabled
        int access_failed_count
        text first_name
        text last_name
        int group_id FK "RESTRICT"
        bool has_enabled_authenticator
    }

    asp_net_roles {
        text id PK
        varchar name
        varchar normalized_name UK
        text concurrency_stamp
    }

    asp_net_user_roles {
        text user_id PK,FK "CASCADE"
        text role_id PK,FK "CASCADE"
    }

    asp_net_user_claims {
        int id PK
        text user_id FK "CASCADE"
        text claim_type
        text claim_value
    }

    asp_net_user_logins {
        varchar login_provider PK
        varchar provider_key PK
        text provider_display_name
        text user_id FK "CASCADE"
    }

    asp_net_user_tokens {
        text user_id PK,FK "CASCADE"
        varchar login_provider PK
        varchar name PK
        text value
    }

    asp_net_user_passkeys {
        bytea credential_id PK
        text user_id FK "CASCADE"
        jsonb data
    }

    asp_net_role_claims {
        int id PK
        text role_id FK "CASCADE"
        text claim_type
        text claim_value
    }

    asp_net_users ||--o{ asp_net_user_roles : "har roller"
    asp_net_roles ||--o{ asp_net_user_roles : "tildeles"
    asp_net_users ||--o{ asp_net_user_claims : "har claims"
    asp_net_users ||--o{ asp_net_user_logins : "har logins"
    asp_net_users ||--o{ asp_net_user_tokens : "har tokens"
    asp_net_users ||--o{ asp_net_user_passkeys : "har passkeys"
    asp_net_roles ||--o{ asp_net_role_claims : "har claims"
    asp_net_users ||--o{ activity_team_membership : "er medlem af"
    asp_net_users ||--o{ user_invitation : "oprettes via"
```

## Drift og invitationer

```mermaid
erDiagram
    user_invitation {
        int id PK
        uuid invitation_id UK
        text email
        text_array roles
        bool used
        int activity_team_id FK "RESTRICT"
        bool is_admin
        int group_id FK "RESTRICT"
        timestamptz created_at
        timestamptz updated_at
        timestamptz deleted_at
    }

    email_outbox {
        int id PK
        text event_type
        uuid correlation_id
        text user_id
        text to_email
        text subject
        text body
        timestamptz created_at
        timestamptz sent_at
        timestamptz next_attempt_at
        int attempts "CHECK >= 0"
        text last_error
        timestamptz failed_at
        timestamptz locked_at
        text locked_by
    }

    registration_settings {
        int id PK "Singleton, CHECK id = 1"
        bool is_pre_signup_open
        bool is_signup_open
    }
```

`email_outbox.user_id` har ingen foreign key, da en udsendelse kan ske til en
inviteret adresse uden brugerkonto.

## OpenIddict

```mermaid
erDiagram
    open_iddict_applications {
        text id PK
        varchar application_type
        varchar client_id UK
        text client_secret
        varchar client_type
        varchar concurrency_token
        varchar consent_type
        text display_name
        text display_names
        text json_web_key_set
        text permissions
        text post_logout_redirect_uris
        text properties
        text redirect_uris
        text requirements
        text settings
    }

    open_iddict_scopes {
        text id PK
        varchar concurrency_token
        text description
        text descriptions
        text display_name
        text display_names
        varchar name UK
        text properties
        text resources
    }

    open_iddict_authorizations {
        text id PK
        text application_id FK
        varchar concurrency_token
        timestamptz creation_date
        text properties
        text scopes
        varchar status
        varchar subject
        varchar type
    }

    open_iddict_tokens {
        text id PK
        text application_id FK
        text authorization_id FK
        varchar concurrency_token
        timestamptz creation_date
        timestamptz expiration_date
        text payload
        text properties
        timestamptz redemption_date
        varchar reference_id UK
        varchar status
        varchar subject
        varchar type
    }

    open_iddict_applications ||--o{ open_iddict_authorizations : "udsteder"
    open_iddict_applications ||--o{ open_iddict_tokens : "udsteder"
    open_iddict_authorizations ||--o{ open_iddict_tokens : "indeholder"
```

## Bemærkninger

- Soft delete via `deleted_at` på `scout_group`, `scout`, `activity_team`,
  `activity`, `material` og `material_order`.
- Migration `006a_relax_legacy_nullable.sql` gør flere kolonner nullable,
  bl.a. `scout_group.name`, `scout.name`, `activity.name` og
  `email_outbox.to_email`.
- `scout_activity_timeslot` findes i databasen, men har ingen tilsvarende
  entitet i `DataDbContext`.
