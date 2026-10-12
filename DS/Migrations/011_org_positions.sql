CREATE TABLE ds28.org_position (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title VARCHAR(256) NOT NULL,
    parent_position_id INTEGER
        REFERENCES ds28.org_position(id)
        ON DELETE RESTRICT,
    is_system BOOLEAN NOT NULL DEFAULT FALSE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX ix_org_position_parent_position_id
    ON ds28.org_position(parent_position_id);

CREATE TABLE ds28.org_position_group (
    position_id INTEGER NOT NULL
        REFERENCES ds28.org_position(id)
        ON DELETE CASCADE,
    group_name VARCHAR(64) NOT NULL,

    PRIMARY KEY (position_id, group_name)
);

CREATE TABLE ds28.org_position_holder (
    position_id INTEGER NOT NULL
        REFERENCES ds28.org_position(id)
        ON DELETE CASCADE,
    user_id TEXT NOT NULL
        REFERENCES ds28.asp_net_users(id)
        ON DELETE CASCADE,

    PRIMARY KEY (position_id, user_id)
);

CREATE INDEX ix_org_position_holder_user_id
    ON ds28.org_position_holder(user_id);

ALTER TABLE ds28.user_invitation
    ADD COLUMN org_position_id INTEGER
        REFERENCES ds28.org_position(id)
        ON DELETE RESTRICT;

INSERT INTO ds28.org_position (title, parent_position_id, is_system)
VALUES ('Systemadministrator', NULL, TRUE);

INSERT INTO ds28.org_position_group (position_id, group_name)
SELECT id, 'SysAdmin'
FROM ds28.org_position
WHERE is_system = TRUE;
