CREATE TABLE ds28.scout_camp_day (
    scout_id INTEGER NOT NULL REFERENCES ds28.scout(id) ON DELETE CASCADE,
    camp_day_id INTEGER NOT NULL REFERENCES ds28.camp_day(id) ON DELETE RESTRICT,
    PRIMARY KEY (scout_id, camp_day_id)
);

CREATE INDEX ix_scout_camp_day_camp_day_id ON ds28.scout_camp_day(camp_day_id);
