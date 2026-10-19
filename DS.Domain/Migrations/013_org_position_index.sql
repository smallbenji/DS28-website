CREATE INDEX ix_org_position_deleted_at
    ON ds28.org_position(deleted_at)
    WHERE deleted_at IS NULL;