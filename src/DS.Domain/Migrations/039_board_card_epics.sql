ALTER TABLE ds28.board_card ADD COLUMN is_epic BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE ds28.board_card ADD COLUMN epic_board_card_id INTEGER NULL REFERENCES ds28.board_card(id) ON DELETE SET NULL;

ALTER TABLE ds28.board_card ADD CONSTRAINT ck_board_card_epic_not_self CHECK (epic_board_card_id <> id);

CREATE INDEX ix_board_card_epic_board_card_id ON ds28.board_card(epic_board_card_id);
