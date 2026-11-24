ALTER TABLE ds28.work_item DROP CONSTRAINT IF EXISTS work_item_owner_position_id_fkey;

ALTER TABLE ds28.work_item DROP COLUMN IF EXISTS owner_position_id;
