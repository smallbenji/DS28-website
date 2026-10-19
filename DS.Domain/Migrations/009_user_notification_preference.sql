CREATE TABLE ds28.user_notification_preference (
    user_id TEXT NOT NULL
        REFERENCES ds28.asp_net_users(id)
        ON DELETE CASCADE,

    notification_type VARCHAR(64) NOT NULL,

    PRIMARY KEY (user_id, notification_type)
);
