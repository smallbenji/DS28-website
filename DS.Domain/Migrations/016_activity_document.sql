CREATE TABLE ds28.activity_document (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    activity_id INTEGER NOT NULL
        REFERENCES ds28.activity(id)
        ON DELETE CASCADE,

    drive_file_id TEXT NOT NULL,

    title TEXT NOT NULL,

    created_by_user_id TEXT
        REFERENCES ds28.asp_net_users(id)
        ON DELETE SET NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_activity_document_activity_id
    ON ds28.activity_document(activity_id);
