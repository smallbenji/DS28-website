ALTER TABLE ds28.board ADD COLUMN is_developer_board BOOLEAN NOT NULL DEFAULT FALSE;

CREATE TABLE ds28.board_card_pull_request (
    board_card_id INTEGER NOT NULL REFERENCES ds28.board_card(id) ON DELETE CASCADE,
    url VARCHAR(512) NOT NULL,
    PRIMARY KEY (board_card_id, url)
);
