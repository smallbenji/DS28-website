ALTER TABLE ds28.activity_document RENAME TO activity_file;

ALTER INDEX ds28.activity_document_pkey RENAME TO activity_file_pkey;

ALTER INDEX ds28.idx_activity_document_activity_id RENAME TO idx_activity_file_activity_id;

ALTER TABLE ds28.activity_file RENAME CONSTRAINT activity_document_activity_id_fkey TO activity_file_activity_id_fkey;

ALTER TABLE ds28.activity_file RENAME CONSTRAINT activity_document_created_by_user_id_fkey TO activity_file_created_by_user_id_fkey;
