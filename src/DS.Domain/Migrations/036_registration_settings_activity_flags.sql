ALTER TABLE ds28.registration_settings
    ADD COLUMN is_activity_wish_round_open BOOLEAN NOT NULL DEFAULT FALSE,
    ADD COLUMN is_activity_signup_open BOOLEAN NOT NULL DEFAULT FALSE;
