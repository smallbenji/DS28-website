CREATE TABLE ds28.stored_file (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    public_id UUID NOT NULL UNIQUE,

    original_name TEXT,
    content_type VARCHAR(128) NOT NULL,

    size_bytes BIGINT,
    sha256 VARCHAR(64),
    width INTEGER,
    height INTEGER,

    storage_key TEXT,

    status VARCHAR(16) NOT NULL DEFAULT 'Pending'
        CHECK (status IN ('Pending', 'Processing', 'Ready', 'Failed')),

    error TEXT,

    purpose VARCHAR(32) NOT NULL
        CHECK (purpose IN ('CatalogImage', 'ProfilePicture')),

    is_public BOOLEAN NOT NULL DEFAULT FALSE,

    uploaded_by_user_id TEXT
        REFERENCES ds28.asp_net_users(id)
        ON DELETE SET NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_stored_file_status
    ON ds28.stored_file(status);

CREATE INDEX idx_stored_file_uploaded_by_user_id
    ON ds28.stored_file(uploaded_by_user_id);

ALTER TABLE ds28.catalog_data
    ADD COLUMN image_file_id INTEGER
        REFERENCES ds28.stored_file(id)
        ON DELETE SET NULL;

ALTER TABLE ds28.asp_net_users
    ADD COLUMN profile_picture_file_id INTEGER
        REFERENCES ds28.stored_file(id)
        ON DELETE SET NULL;
