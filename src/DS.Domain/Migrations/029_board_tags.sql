CREATE TABLE ds28.board_tag (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    board_id INTEGER NOT NULL REFERENCES ds28.board(id) ON DELETE CASCADE,
    name VARCHAR(64) NOT NULL CHECK (length(btrim(name)) > 0),
    color VARCHAR(7) CHECK (color ~ '^#[0-9a-f]{6}$'),
    CONSTRAINT ak_board_tag_board_id_id UNIQUE (board_id, id)
);
CREATE UNIQUE INDEX ux_board_tag_board_name ON ds28.board_tag (board_id, lower(name));

ALTER TABLE ds28.board_card ADD CONSTRAINT ak_board_card_board_id_id UNIQUE (board_id, id);

CREATE TABLE ds28.board_card_tag (
    board_id INTEGER NOT NULL,
    board_card_id INTEGER NOT NULL,
    board_tag_id INTEGER NOT NULL,
    PRIMARY KEY (board_card_id, board_tag_id),
    FOREIGN KEY (board_id, board_card_id) REFERENCES ds28.board_card(board_id, id) ON DELETE CASCADE,
    FOREIGN KEY (board_id, board_tag_id) REFERENCES ds28.board_tag(board_id, id) ON DELETE CASCADE
);
CREATE INDEX ix_board_card_tag_board_id_board_card_id ON ds28.board_card_tag(board_id, board_card_id);
CREATE INDEX ix_board_card_tag_board_id_board_tag_id ON ds28.board_card_tag(board_id, board_tag_id);
