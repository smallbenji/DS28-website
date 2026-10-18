CREATE TABLE ds28.board_member (
    board_id INTEGER NOT NULL REFERENCES ds28.board(id) ON DELETE CASCADE,
    user_id TEXT NOT NULL REFERENCES ds28.asp_net_users(id) ON DELETE CASCADE,
    PRIMARY KEY (board_id, user_id)
);

CREATE INDEX ix_board_member_user_id ON ds28.board_member(user_id);

INSERT INTO ds28.board_member (board_id, user_id)
SELECT id, created_by_user_id
FROM ds28.board
WHERE created_by_user_id IS NOT NULL;

ALTER TABLE ds28.board DROP COLUMN required_role;
