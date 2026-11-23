CREATE TABLE ds28.camp_day (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    date DATE NOT NULL,
    half VARCHAR(16) NOT NULL CONSTRAINT ck_camp_day_half CHECK (half IN ('First', 'Second', 'Both')),
    CONSTRAINT uq_camp_day_date UNIQUE (date)
);
