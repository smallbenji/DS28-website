CREATE TABLE ds28.historic_presignup (
    group_id INTEGER NOT NULL REFERENCES ds28.scout_group(id) ON DELETE RESTRICT,
    camp_year INTEGER NOT NULL CHECK (camp_year BETWEEN 1900 AND 9999),
    category VARCHAR(16) NOT NULL CHECK (category IN ('family', 'beaver', 'wolf', 'junior', 'trop', 'senior', 'rover', 'leader')),
    participant_count INTEGER NOT NULL CHECK (participant_count >= 0),
    guest_count INTEGER NOT NULL CHECK (guest_count >= 0),
    source_document_id TEXT NOT NULL,
    source_sha256 VARCHAR(64) NOT NULL CHECK (source_sha256 ~ '^[0-9a-f]{64}$'),
    imported_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (group_id, camp_year, category),
    UNIQUE (camp_year, source_document_id, category)
);

CREATE TABLE ds28.historic_signup (
    group_id INTEGER NOT NULL REFERENCES ds28.scout_group(id) ON DELETE RESTRICT,
    camp_year INTEGER NOT NULL CHECK (camp_year BETWEEN 1900 AND 9999),
    category VARCHAR(16) NOT NULL CHECK (category IN ('family', 'beaver', 'wolf', 'junior', 'trop', 'senior', 'rover', 'leader')),
    participant_count INTEGER NOT NULL CHECK (participant_count >= 0),
    day_index INTEGER NOT NULL CHECK (day_index BETWEEN 0 AND 8),
    source_document_id TEXT NOT NULL,
    source_sha256 VARCHAR(64) NOT NULL CHECK (source_sha256 ~ '^[0-9a-f]{64}$'),
    imported_at TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (group_id, camp_year, category, day_index),
    UNIQUE (camp_year, source_document_id, category, day_index)
);
