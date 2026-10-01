CREATE TABLE ds28.activity_team (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE ds28.activity (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT
        NOT NULL,

    activity_team_id INTEGER
        NOT NULL
        REFERENCES ds28.activity_team(id)
        ON DELETE RESTRICT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE ds28.activity_budget (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    budget INTEGER NOT NULL DEFAULT 0
        CHECK (budget >= 0),
    activity_id INTEGER NOT NULL UNIQUE
        REFERENCES ds28.activity(id)
        ON DELETE CASCADE
);

CREATE TABLE ds28.catalog_data (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT,
    summary TEXT,
    description TEXT,

    activity_id INTEGER NOT NULL UNIQUE
        REFERENCES ds28.activity(id)
        ON DELETE CASCADE
);

CREATE TABLE ds28.activity_category (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE ds28.activity_timeslots (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    activity_id INTEGER NOT NULL
        REFERENCES ds28.activity(id)
        ON DELETE RESTRICT,
    start_time TIMESTAMPTZ NOT NULL,
    duration INTEGER NOT NULL
        CHECK (duration >= 1)
);

CREATE INDEX idx_activity_timeslot_activity_id
    ON ds28.activity_timeslots(activity_id);

CREATE TABLE ds28.catalog_data_category (
    catalog_data_id INTEGER NOT NULL
        REFERENCES ds28.catalog_data(id)
        ON DELETE CASCADE,

    activity_category_id INTEGER NOT NULL
        REFERENCES ds28.activity_category(id)
        ON DELETE CASCADE,

    PRIMARY KEY (catalog_data_id, activity_category_id)
);

CREATE TABLE ds28.activity_team_membership (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id TEXT NOT NULL
        REFERENCES ds28.asp_net_users(id)
        ON DELETE CASCADE,
    activity_team_id INTEGER NOT NULL
        REFERENCES ds28.activity_team(id)
        ON DELETE CASCADE,

    is_admin BOOLEAN NOT NULL DEFAULT FALSE,

    UNIQUE (user_id, activity_team_id)
);

CREATE INDEX idx_activity_activity_team_id
    ON ds28.activity(activity_team_id);

CREATE INDEX idx_scout_group_id
    ON ds28.scout(group_id);

CREATE INDEX idx_patrol_group_id
    ON ds28.patrol(group_id);

CREATE INDEX idx_patrol_membership_patrol_id
    ON ds28.patrol_membership(patrol_id);