CREATE TABLE ds28.open_iddict_applications (
    id TEXT NOT NULL PRIMARY KEY,
    application_type CHARACTER VARYING(50),
    client_id CHARACTER VARYING(100),
    client_secret TEXT,
    client_type CHARACTER VARYING(50),
    concurrency_token CHARACTER VARYING(50),
    consent_type CHARACTER VARYING(50),
    display_name TEXT,
    display_names TEXT,
    json_web_key_set TEXT,
    permissions TEXT,
    post_logout_redirect_uris TEXT,
    properties TEXT,
    redirect_uris TEXT,
    requirements TEXT,
    settings TEXT
);

CREATE TABLE ds28.open_iddict_scopes (
    id TEXT NOT NULL PRIMARY KEY,
    concurrency_token CHARACTER VARYING(50),
    description TEXT,
    descriptions TEXT,
    display_name TEXT,
    display_names TEXT,
    name CHARACTER VARYING(200),
    properties TEXT,
    resources TEXT
);

CREATE TABLE ds28.open_iddict_authorizations (
    id TEXT NOT NULL PRIMARY KEY,
    application_id TEXT
        REFERENCES ds28.open_iddict_applications (id),
    concurrency_token CHARACTER VARYING(50),
    creation_date TIMESTAMPTZ,
    properties TEXT,
    scopes TEXT,
    status CHARACTER VARYING(50),
    subject CHARACTER VARYING(400),
    type CHARACTER VARYING(50)
);

CREATE TABLE ds28.open_iddict_tokens (
    id TEXT NOT NULL PRIMARY KEY,
    application_id TEXT
        REFERENCES ds28.open_iddict_applications (id),
    authorization_id TEXT
        REFERENCES ds28.open_iddict_authorizations (id),
    concurrency_token CHARACTER VARYING(50),
    creation_date TIMESTAMPTZ,
    expiration_date TIMESTAMPTZ,
    payload TEXT,
    properties TEXT,
    redemption_date TIMESTAMPTZ,
    reference_id CHARACTER VARYING(100),
    status CHARACTER VARYING(50),
    subject CHARACTER VARYING(400),
    type CHARACTER VARYING(150)
);

CREATE UNIQUE INDEX ix_open_iddict_applications_client_id
    ON ds28.open_iddict_applications (client_id);

CREATE INDEX ix_open_iddict_authorizations_application_id_status_subject_ty
    ON ds28.open_iddict_authorizations (application_id, status, subject, type);

CREATE UNIQUE INDEX ix_open_iddict_scopes_name
    ON ds28.open_iddict_scopes (name);

CREATE INDEX ix_open_iddict_tokens_application_id_status_subject_type
    ON ds28.open_iddict_tokens (application_id, status, subject, type);

CREATE INDEX ix_open_iddict_tokens_authorization_id
    ON ds28.open_iddict_tokens (authorization_id);

CREATE UNIQUE INDEX ix_open_iddict_tokens_reference_id
    ON ds28.open_iddict_tokens (reference_id);
