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

CREATE INDEX idx_activity_activity_team_id
    ON activity(activity_team_id);

CREATE INDEX idx_scout_group_id
    ON scout(group_id);

CREATE INDEX idx_patrol_group_id
    ON patrol(group_id);

CREATE INDEX idx_patrol_membership_patrol_id
    ON patrol_membership(patrol_id);