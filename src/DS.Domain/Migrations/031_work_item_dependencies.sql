CREATE TABLE ds28.work_item_dependency (
    work_item_id INTEGER NOT NULL REFERENCES ds28.work_item(id) ON DELETE CASCADE,
    depends_on_work_item_id INTEGER NOT NULL REFERENCES ds28.work_item(id) ON DELETE CASCADE,
    PRIMARY KEY (work_item_id, depends_on_work_item_id),
    CONSTRAINT ck_work_item_dependency_not_self CHECK (work_item_id <> depends_on_work_item_id)
);

CREATE INDEX ix_work_item_dependency_depends_on_work_item_id ON ds28.work_item_dependency(depends_on_work_item_id);
