DROP INDEX ds28.uq_org_position_pa_for_position_id;

CREATE UNIQUE INDEX uq_org_position_pa_for_position_id
    ON ds28.org_position(pa_for_position_id)
    WHERE deleted_at IS NULL;
