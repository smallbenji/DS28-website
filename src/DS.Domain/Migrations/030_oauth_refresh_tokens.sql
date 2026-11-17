UPDATE ds28.open_iddict_applications
SET permissions = (permissions::jsonb || '["gt:refresh_token"]'::jsonb)::text,
    concurrency_token = gen_random_uuid()::text
WHERE permissions::jsonb ? 'gt:authorization_code'
  AND NOT permissions::jsonb ? 'gt:refresh_token';
