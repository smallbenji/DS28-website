ALTER TABLE ds28.work_item DROP CONSTRAINT work_item_color_check;
ALTER TABLE ds28.work_item ADD CONSTRAINT work_item_color_check
    CHECK (color ~ '^#[0-9a-f]{6}$');

CREATE TABLE ds28.board_color (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    board_id INTEGER NOT NULL REFERENCES ds28.board(id) ON DELETE CASCADE,
    name VARCHAR(64) NOT NULL CHECK (length(btrim(name)) > 0),
    color VARCHAR(7) NOT NULL CHECK (color ~ '^#[0-9a-f]{6}$')
);

CREATE INDEX ix_board_color_board_id ON ds28.board_color(board_id);

INSERT INTO ds28.board_color (board_id, name, color)
SELECT board.id, palette.name, palette.color
FROM ds28.board
CROSS JOIN (VALUES
    ('Blå', '#dbeafe'), ('Grøn', '#dcfce7'), ('Gul', '#fef3c7'),
    ('Rød', '#fee2e2'), ('Lilla', '#ede9fe'), ('Turkis', '#ccfbf1'),
    ('Orange', '#ffedd5'), ('Pink', '#fce7f3'), ('Grå', '#e5e7eb'),
    ('Brun', '#e7d5c5')
) AS palette(name, color)
ORDER BY board.id, palette.name;
