ALTER TABLE ds28.org_position
    ADD COLUMN pa_for_position_id INTEGER
        REFERENCES ds28.org_position(id)
        ON DELETE CASCADE;

CREATE UNIQUE INDEX uq_org_position_pa_for_position_id
    ON ds28.org_position(pa_for_position_id);