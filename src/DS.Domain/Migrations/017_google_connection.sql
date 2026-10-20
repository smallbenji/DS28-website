CREATE TABLE ds28.google_connection (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    account_email TEXT NOT NULL,

    refresh_token TEXT NOT NULL,

    connected_by_user_id TEXT
        REFERENCES ds28.asp_net_users(id)
        ON DELETE SET NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
