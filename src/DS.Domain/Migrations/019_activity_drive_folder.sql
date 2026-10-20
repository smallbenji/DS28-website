CREATE TABLE ds28.activity_drive_folder (
    activity_id INTEGER PRIMARY KEY
        REFERENCES ds28.activity(id)
        ON DELETE CASCADE,

    drive_folder_id TEXT NOT NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
