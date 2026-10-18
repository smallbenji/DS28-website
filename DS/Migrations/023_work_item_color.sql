ALTER TABLE ds28.work_item
    ADD COLUMN color VARCHAR(7)
    CHECK (color IN ('#dbeafe', '#dcfce7', '#fef3c7', '#fee2e2', '#ede9fe', '#ccfbf1'));
