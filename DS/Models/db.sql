CREATE TABLE activity_team (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE activity (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT
        NOT NULL,

    activity_team_id INTEGER
        NOT NULL
        REFERENCES activity_team(id)
        ON DELETE RESTRICT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE activity_budget (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    budget INTEGER NOT NULL DEFAULT 0
        CHECK (budget >= 0),
    activity_id INTEGER NOT NULL UNIQUE
        REFERENCES activity(id)
        ON DELETE CASCADE
);

CREATE TABLE catalog_data (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT,
    summary TEXT,
    description TEXT,

    activity_id INTEGER NOT NULL UNIQUE
        REFERENCES activity(id)
        ON DELETE CASCADE
);

CREATE TABLE activity_category (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE activity_timeslots (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    activity_id INTEGER NOT NULL
        REFERENCES activity(id)
        ON DELETE RESTRICT,
    start_time TIMESTAMPTZ NOT NULL,
    duration INTEGER NOT NULL
        CHECK (duration >= 1)
);

CREATE INDEX idx_activity_timeslot_activity_id
    ON activity_timeslot(activity_id);

CREATE TABLE catalog_data_category (
    catalog_data_id INTEGER NOT NULL
        REFERENCES catalog_data(id)
        ON DELETE CASCADE,
    
    activity_category_id INTEGER NOT NULL
        REFERENCES activity_category(id)
        ON DELETE CASCADE,

    PRIMARY KEY (catalog_data_id, activity_category_id)
);

CREATE TABLE scout_group (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    district TEXT NOT NULL
        CHECK (district IN ('DANEHOF', 'FIONIA')),
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE scout (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL,
    birthday DATE NOT NULL,

    gender TEXT NOT NULL
        CHECK (gender IN ('MALE', 'FEMALE')),
    
    group_id INTEGER NOT NULL
        REFERENCES scout_group(id)
        ON DELETE RESTRICT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE patrol (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL,

    group_id INTEGER NOT NULL
        REFERENCES scout_group(id)
        ON DELETE CASCADE
);

CREATE TABLE patrol_membership (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    scout_id INTEGER NOT NULL
        REFERENCES scout(id)
        ON DELETE CASCADE,
    
    patrol_id INTEGER NOT NULL
        REFERENCES patrol(id)
        ON DELETE CASCADE,

    joined_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_patrol_leader BOOLEAN NOT NULL DEFAULT FALSE,

    UNIQUE (scout_id, patrol_id)
);

CREATE TABLE group_pre_signup (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    group_id INTEGER NOT NULL UNIQUE
        REFERENCES scout_group(id)
        ON DELETE CASCADE,

    beaver  INTEGER NOT NULL DEFAULT 0 CHECK (beaver >= 0),
    wolf    INTEGER NOT NULL DEFAULT 0 CHECK (wolf >= 0),
    junior  INTEGER NOT NULL DEFAULT 0 CHECK (junior >= 0),
    trop    INTEGER NOT NULL DEFAULT 0 CHECK (trop >= 0),
    senior  INTEGER NOT NULL DEFAULT 0 CHECK (senior >= 0),
    rover   INTEGER NOT NULL DEFAULT 0 CHECK (rover >= 0),
    leader  INTEGER NOT NULL DEFAULT 0 CHECK (leader >= 0)
);

CREATE TABLE registration_settings (
    id INTEGER PRIMARY KEY DEFAULT 1
        CHECK (id = 1),

    is_pre_signup_open BOOLEAN NOT NULL DEFAULT FALSE,
    is_signup_open BOOLEAN NOT NULL DEFAULT FALSE
);

INSERT INTO registration_settings DEFAULT VALUES;

CREATE INDEX idx_activity_activity_team_id
    ON activity(activity_team_id);

CREATE INDEX idx_scout_group_id
    ON scout(group_id);

CREATE INDEX idx_patrol_group_id
    ON patrol(group_id);

CREATE INDEX idx_patrol_membership_patrol_id
    ON patrol_membership(patrol_id);

CREATE TABLE material (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL,
    price NUMERIC(10, 2) NOT NULL DEFAULT 0
        CHECK (price >= 0),
    url TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE material_order (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    activity_id INTEGER NOT NULL
        REFERENCES activity(id)
        ON DELETE RESTRICT,
    material_id INTEGER NOT NULL
        REFERENCES material(id)
        ON DELETE RESTRICT,
    quantity INTEGER NOT NULL DEFAULT 1
        CHECK (quantity > 0),
    use_date TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_material_order_activity_id
    ON material_order(activity_id);

CREATE INDEX idx_material_order_material_id
    ON material_order(material_id);

CREATE TABLE email_outbox (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    event_type TEXT NOT NULL,
    correlation_id UUID,
    user_id TEXT NOT NULL,
    to_email TEXT NOT NULL,

    subject TEXT NOT NULL,
    body TEXT NOT NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    sent_at TIMESTAMPTZ,
    next_attempt_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    attempts INTEGER NOT NULL DEFAULT 0
        CHECK (attempts >= 0),

    last_error TEXT,
    failed_at TIMESTAMPTZ,

    locked_at TIMESTAMPTZ,
    locked_by TEXT
);

CREATE TABLE scout_activity_timeslot (
    activity_timeslot_id INTEGER NOT NULL
        REFERENCES activity_timeslot(id)
        ON DELETE CASCADE,
    
    scout_id INTEGER NOT NULL
        REFERENCES scout(id)
        ON DELETE CASCADE,

    PRIMARY KEY (activity_timeslot_id, scout_id)
);

CREATE INDEX idx_scout_activity_timeslot_scout_id
    ON scout_activity_timeslot(scout_id);

CREATE TABLE scout_signup (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    day DATE NOT NULL,
    scout_id INTEGER NOT NULL
        REFERENCES scout(id)
        ON DELETE CASCADE,
    
    UNIQUE (scout_id, day)
);

CREATE TABLE asp_net_roles (
    id TEXT PRIMARY KEY,
    name VARCHAR(256),
    normalized_name VARCHAR(256),
    concurrency_stamp TEXT
);

CREATE UNIQUE INDEX idx_asp_net_roles_normalized_name
    ON asp_net_roles(normalized_name);

CREATE TABLE asp_net_users (
    id TEXT PRIMARY KEY,

    user_name VARCHAR(256),
    normalized_user_name VARCHAR(256),

    email VARCHAR(256),
    normalized_email VARCHAR(256),
    email_confirmed BOOLEAN NOT NULL,

    password_hash TEXT,
    security_stamp TEXT,
    concurrency_stamp TEXT,

    phone_number VARCHAR(256),
    phone_number_confirmed BOOLEAN NOT NULL,

    two_factor_enabled BOOLEAN NOT NULL,

    lockout_end TIMESTAMPTZ,
    lockout_enabled BOOLEAN NOT NULL,
    access_failed_count INTEGER NOT NULL,

    first_name TEXT,
    last_name TEXT,

    group_id INTEGER
        REFERENCES scout_group(id)
        ON DELETE RESTRICT,

    has_enabled_authenticator BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX idx_asp_net_users_normalized_email
    ON asp_net_users(normalized_email);

CREATE UNIQUE INDEX idx_asp_net_users_normalized_user_name
    ON asp_net_users(normalized_user_name);

CREATE INDEX idx_asp_net_users_group_id
    ON asp_net_users(group_id);

CREATE TABLE asp_net_role_claims (
    id INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,

    role_id TEXT NOT NULL
        REFERENCES asp_net_roles(id)
        ON DELETE CASCADE,

    claim_type TEXT,
    claim_value TEXT
);

CREATE INDEX idx_asp_net_role_claims_role_id
    ON asp_net_role_claims(role_id);

CREATE TABLE asp_net_user_claims (
    id INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,

    user_id TEXT NOT NULL
        REFERENCES asp_net_users(id)
        ON DELETE CASCADE,

    claim_type TEXT,
    claim_value TEXT
);

CREATE INDEX idx_asp_net_user_claims_user_id
    ON asp_net_user_claims(user_id);


CREATE TABLE asp_net_user_logins (
    login_provider VARCHAR(128) NOT NULL,
    provider_key VARCHAR(128) NOT NULL,
    provider_display_name TEXT,

    user_id TEXT NOT NULL
        REFERENCES asp_net_users(id)
        ON DELETE CASCADE,

    PRIMARY KEY (login_provider, provider_key)
);

CREATE INDEX idx_asp_net_user_logins_user_id
    ON asp_net_user_logins(user_id);

CREATE TABLE asp_net_user_roles (
    user_id TEXT NOT NULL
        REFERENCES asp_net_users(id)
        ON DELETE CASCADE,

    role_id TEXT NOT NULL
        REFERENCES asp_net_roles(id)
        ON DELETE CASCADE,

    PRIMARY KEY (user_id, role_id)
);

CREATE INDEX idx_asp_net_user_roles_role_id
    ON asp_net_user_roles(role_id);

CREATE TABLE asp_net_user_tokens (
    user_id TEXT NOT NULL
        REFERENCES asp_net_users(id)
        ON DELETE CASCADE,

    login_provider VARCHAR(128) NOT NULL,
    name VARCHAR(128) NOT NULL,
    value TEXT,

    PRIMARY KEY (user_id, login_provider, name)
);

CREATE TABLE asp_net_user_passkeys (
    credential_id BYTEA PRIMARY KEY,

    user_id TEXT NOT NULL
        REFERENCES asp_net_users(id)
        ON DELETE CASCADE,

    data JSONB NOT NULL
);

CREATE INDEX idx_asp_net_user_passkeys_user_id
    ON asp_net_user_passkeys(user_id);

CREATE TABLE user_invitation (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    invitation_id UUID NOT NULL UNIQUE,
    email TEXT NOT NULL,
    roles TEXT[] NOT NULL DEFAULT '{}',
    used BOOLEAN NOT NULL DEFAULT FALSE,
    activity_team_id INTEGER
        REFERENCES activity_team(id)
        ON DELETE RESTRICT,
    is_admin BOOLEAN NOT NULL DEFAULT FALSE,
    group_id INTEGER
        REFERENCES scout_group(id)
        ON DELETE RESTRICT,
    
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE activity_team_membership (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id TEXT NOT NULL
        REFERENCES asp_net_users(id)
        ON DELETE CASCADE,
    activity_team_id INTEGER NOT NULL
        REFERENCES activity_team(id)
        ON DELETE CASCADE,
    
    is_admin BOOLEAN NOT NULL DEFAULT FALSE,

    UNIQUE (user_id, activity_team_id)
);

CREATE TABLE open_iddict_applications (
    id text NOT NULL,
    application_type character varying(50),
    client_id character varying(100),
    client_secret text,
    client_type character varying(50),
    concurrency_token character varying(50),
    consent_type character varying(50),
    display_name text,
    display_names text,
    json_web_key_set text,
    permissions text,
    post_logout_redirect_uris text,
    properties text,
    redirect_uris text,
    requirements text,
    settings text,
    CONSTRAINT pk_open_iddict_applications PRIMARY KEY (id)
);

CREATE TABLE open_iddict_scopes (
    id text NOT NULL,
    concurrency_token character varying(50),
    description text,
    descriptions text,
    display_name text,
    display_names text,
    name character varying(200),
    properties text,
    resources text,
    CONSTRAINT pk_open_iddict_scopes PRIMARY KEY (id)
);

CREATE TABLE open_iddict_authorizations (
    id text NOT NULL,
    application_id text,
    concurrency_token character varying(50),
    creation_date timestamp with time zone,
    properties text,
    scopes text,
    status character varying(50),
    subject character varying(400),
    type character varying(50),
    CONSTRAINT pk_open_iddict_authorizations PRIMARY KEY (id),
    CONSTRAINT fk_open_iddict_authorizations_open_iddict_applications_applica FOREIGN KEY (application_id) REFERENCES open_iddict_applications (id)
);

CREATE TABLE open_iddict_tokens (
    id text NOT NULL,
    application_id text,
    authorization_id text,
    concurrency_token character varying(50),
    creation_date timestamp with time zone,
    expiration_date timestamp with time zone,
    payload text,
    properties text,
    redemption_date timestamp with time zone,
    reference_id character varying(100),
    status character varying(50),
    subject character varying(400),
    type character varying(150),
    CONSTRAINT pk_open_iddict_tokens PRIMARY KEY (id),
    CONSTRAINT fk_open_iddict_tokens_open_iddict_applications_application_id FOREIGN KEY (application_id) REFERENCES open_iddict_applications (id),
    CONSTRAINT fk_open_iddict_tokens_open_iddict_authorizations_authorization FOREIGN KEY (authorization_id) REFERENCES open_iddict_authorizations (id)
);

CREATE UNIQUE INDEX ix_open_iddict_applications_client_id ON open_iddict_applications (client_id);

CREATE INDEX ix_open_iddict_authorizations_application_id_status_subject_ty ON open_iddict_authorizations (application_id, status, subject, type);

CREATE UNIQUE INDEX ix_open_iddict_scopes_name ON open_iddict_scopes (name);

CREATE INDEX ix_open_iddict_tokens_application_id_status_subject_type ON open_iddict_tokens (application_id, status, subject, type);

CREATE INDEX ix_open_iddict_tokens_authorization_id ON open_iddict_tokens (authorization_id);

CREATE UNIQUE INDEX ix_open_iddict_tokens_reference_id ON open_iddict_tokens (reference_id);
