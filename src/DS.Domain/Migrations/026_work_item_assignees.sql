CREATE TABLE ds28.work_item_assignee (
    work_item_id INTEGER NOT NULL REFERENCES ds28.work_item(id) ON DELETE CASCADE,
    user_id TEXT NOT NULL REFERENCES ds28.asp_net_users(id) ON DELETE CASCADE,
    PRIMARY KEY (work_item_id, user_id)
);

CREATE INDEX ix_work_item_assignees_user_id ON ds28.work_item_assignee(user_id);

INSERT INTO ds28.work_item_assignee (work_item_id, user_id)
SELECT id, assignee_user_id FROM ds28.work_item WHERE assignee_user_id IS NOT NULL;

ALTER TABLE ds28.work_item DROP COLUMN assignee_user_id;
