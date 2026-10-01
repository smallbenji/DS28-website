CREATE TABLE ds28.scout_group (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    district TEXT NOT NULL
        CHECK (district IN ('DANEHOF', 'FIONIA')),

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE ds28.scout (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL,
    birthday DATE NOT NULL,

    gender TEXT NOT NULL
        CHECK (gender IN ('MALE', 'FEMALE')),

    group_id INTEGER NOT NULL
        REFERENCES ds28.scout_group(id)
        ON DELETE RESTRICT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE ds28.patrol (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL,

    group_id INTEGER NOT NULL
        REFERENCES ds28.scout_group(id)
        ON DELETE CASCADE
);

CREATE TABLE ds28.patrol_membership (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    scout_id INTEGER NOT NULL
        REFERENCES ds28.scout(id)
        ON DELETE CASCADE,

    patrol_id INTEGER NOT NULL
        REFERENCES ds28.patrol(id)
        ON DELETE CASCADE,

    joined_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_patrol_leader BOOLEAN NOT NULL DEFAULT FALSE,

    UNIQUE (scout_id, patrol_id)
);

CREATE TABLE ds28.group_pre_signup (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    group_id INTEGER NOT NULL UNIQUE
        REFERENCES ds28.scout_group(id)
        ON DELETE CASCADE,

    beaver  INTEGER NOT NULL DEFAULT 0 CHECK (beaver >= 0),
    wolf    INTEGER NOT NULL DEFAULT 0 CHECK (wolf >= 0),
    junior  INTEGER NOT NULL DEFAULT 0 CHECK (junior >= 0),
    trop    INTEGER NOT NULL DEFAULT 0 CHECK (trop >= 0),
    senior  INTEGER NOT NULL DEFAULT 0 CHECK (senior >= 0),
    rover   INTEGER NOT NULL DEFAULT 0 CHECK (rover >= 0),
    leader  INTEGER NOT NULL DEFAULT 0 CHECK (leader >= 0)
);

CREATE TABLE ds28.scout_signup (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    day DATE NOT NULL,
    scout_id INTEGER NOT NULL
        REFERENCES ds28.scout(id)
        ON DELETE CASCADE,

    UNIQUE (scout_id, day)
);