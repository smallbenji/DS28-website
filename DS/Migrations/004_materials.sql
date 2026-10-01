CREATE TABLE ds28.material (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name TEXT NOT NULL,
    price NUMERIC(10, 2) NOT NULL DEFAULT 0
        CHECK (price >= 0),
    url TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TABLE ds28.material_order (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    activity_id INTEGER NOT NULL
        REFERENCES ds28.activity(id)
        ON DELETE RESTRICT,
    material_id INTEGER NOT NULL
        REFERENCES ds28.material(id)
        ON DELETE RESTRICT,
    quantity INTEGER NOT NULL DEFAULT 1
        CHECK (quantity > 0),
    use_date TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE INDEX idx_material_order_activity_id
    ON ds28.material_order(activity_id);

CREATE INDEX idx_material_order_material_id
    ON ds28.material_order(material_id);
