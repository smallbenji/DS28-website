ALTER TABLE ds28.activity
    ADD COLUMN review_status VARCHAR(32) NOT NULL DEFAULT 'Draft'
        CONSTRAINT ck_activity_review_status CHECK (review_status IN ('Draft', 'Submitted', 'ChangesRequested', 'Approved')),
    ADD COLUMN review_version INTEGER NOT NULL DEFAULT 0,
    ADD COLUMN submitted_at TIMESTAMPTZ,
    ADD COLUMN reviewed_at TIMESTAMPTZ,
    ADD COLUMN reviewed_by_user_id TEXT REFERENCES ds28.asp_net_users(id) ON DELETE SET NULL,
    ADD COLUMN review_feedback VARCHAR(4000);

CREATE INDEX idx_activity_review_status_submitted_at ON ds28.activity(review_status, submitted_at);
CREATE INDEX idx_activity_reviewed_by_user_id ON ds28.activity(reviewed_by_user_id);
