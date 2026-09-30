CREATE TABLE email_outbox (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,

    event_type TEXT NOT NULL,
    correlation_id UUID,
    user_id TEXT NOT NULL,
    to_email TEXT NOT NULL,

    subject TEXT NOT NULL,
    body TEXT NOT NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    sent_at TIMESTAMPTZ,
    next_attempt_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    attempts INTEGER NOT NULL DEFAULT 0
        CHECK (attempts >= 0),

    last_error TEXT,
    failed_at TIMESTAMPTZ,

    locked_at TIMESTAMPTZ,
    locked_by TEXT
);

CREATE TABLE registration_settings (
    id INTEGER PRIMARY KEY DEFAULT 1
        CHECK (id = 1),

    is_pre_signup_open BOOLEAN NOT NULL DEFAULT FALSE,
    is_signup_open BOOLEAN NOT NULL DEFAULT FALSE
);

INSERT INTO registration_settings DEFAULT VALUES;

CREATE TABLE user_invitation (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    invitation_id UUID NOT NULL UNIQUE,
    email TEXT NOT NULL,
    roles TEXT[] NOT NULL DEFAULT '{}',
    used BOOLEAN NOT NULL DEFAULT FALSE,
    activity_team_id INTEGER
        REFERENCES activity_team(id)
        ON DELETE RESTRICT,
    is_admin BOOLEAN NOT NULL DEFAULT FALSE,
    group_id INTEGER
        REFERENCES scout_group(id)
        ON DELETE RESTRICT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE scout_activity_timeslot (
    activity_timeslot_id INTEGER NOT NULL
        REFERENCES activity_timeslot(id)
        ON DELETE CASCADE,

    scout_id INTEGER NOT NULL
        REFERENCES scout(id)
        ON DELETE CASCADE,

    PRIMARY KEY (activity_timeslot_id, scout_id)
);

CREATE INDEX idx_scout_activity_timeslot_scout_id
    ON scout_activity_timeslot(scout_id);