CREATE TABLE ds28.board (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    key VARCHAR(64) NOT NULL UNIQUE,
    title VARCHAR(256) NOT NULL,
    description TEXT,
    icon VARCHAR(64),
    color VARCHAR(16),
    is_archived BOOLEAN NOT NULL DEFAULT FALSE,
    required_role VARCHAR(64),
    created_by_user_id TEXT
        REFERENCES ds28.asp_net_users(id)
        ON DELETE SET NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE ds28.board_column (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    board_id INTEGER NOT NULL
        REFERENCES ds28.board(id)
        ON DELETE CASCADE,
    title VARCHAR(256) NOT NULL,
    position INTEGER NOT NULL,
    wip_limit INTEGER
        CHECK (wip_limit >= 1),
    is_done_column BOOLEAN NOT NULL DEFAULT FALSE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE (board_id, position)
);

CREATE TABLE ds28.work_item (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title VARCHAR(256) NOT NULL,
    description TEXT,

    assignee_user_id TEXT
        REFERENCES ds28.asp_net_users(id)
        ON DELETE SET NULL,
    owner_position_id INTEGER
        REFERENCES ds28.org_position(id)
        ON DELETE SET NULL,

    due_date DATE,
    priority INTEGER NOT NULL DEFAULT 0,
    is_done BOOLEAN NOT NULL DEFAULT FALSE,

    created_by_user_id TEXT
        REFERENCES ds28.asp_net_users(id)
        ON DELETE SET NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX ix_work_item_assignee_user_id
    ON ds28.work_item(assignee_user_id);

CREATE TABLE ds28.board_card (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    board_id INTEGER NOT NULL
        REFERENCES ds28.board(id)
        ON DELETE CASCADE,
    column_id INTEGER NOT NULL
        REFERENCES ds28.board_column(id)
        ON DELETE RESTRICT,
    work_item_id INTEGER NOT NULL
        REFERENCES ds28.work_item(id)
        ON DELETE CASCADE,
    position INTEGER NOT NULL,

    UNIQUE (work_item_id, board_id)
);

CREATE INDEX ix_board_card_board_id
    ON ds28.board_card(board_id);

CREATE INDEX ix_board_card_column_id
    ON ds28.board_card(column_id);

CREATE TABLE ds28.work_item_link (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    work_item_id INTEGER NOT NULL
        REFERENCES ds28.work_item(id)
        ON DELETE CASCADE,
    entity_type VARCHAR(64) NOT NULL,
    entity_id INTEGER NOT NULL,

    UNIQUE (work_item_id, entity_type, entity_id)
);

CREATE INDEX ix_work_item_link_entity
    ON ds28.work_item_link(entity_type, entity_id);
