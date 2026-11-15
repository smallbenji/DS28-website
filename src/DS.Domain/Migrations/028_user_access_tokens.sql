CREATE TABLE ds28.user_access_token (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id TEXT NOT NULL REFERENCES ds28.asp_net_users(id) ON DELETE CASCADE,
    name VARCHAR(64) NOT NULL CHECK (length(btrim(name)) > 0),
    token_hash CHAR(64) NOT NULL UNIQUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    revoked_at TIMESTAMPTZ
);

CREATE INDEX ix_user_access_token_user_id ON ds28.user_access_token(user_id);
