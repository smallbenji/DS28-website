CREATE SCHEMA IF NOT EXISTS ds28;

DO $transition$
DECLARE
    source_table text;
    expected_migrations text[] := ARRAY[
        '20260513212452_AddingGroup',
        '20260520160713_AddingInvitation',
        '20260520161646_AddingInvitationEmail',
        '20260520163904_AddingInvitationEmailFix',
        '20260620120154_AddingPatrols',
        '20260624092643_AddingScouts',
        '20260731130529_activities',
        '20260731202121_activity_teams',
        '20260802160317_AddIdentity',
        '20260802172333_AddingUserNames',
        '20260803005858_AddOpenIddict',
        '20260803213458_addingGroupToUser',
        '20260803214706_removingGroupNumber',
        '20260803223801_addingDistricts',
        '20260808002403_AddPasskeyTables',
        '20260810173929_userIdToUser',
        '20260810203603_ActivityInviteTeam',
        '20260812114019_addHasEnabledAuthenticatorToUSer',
        '20260814223028_AddGroupPreSignup',
        '20260915104743_AddGroupInvitations',
        '20260915114433_AddRegistrationSettings',
        '20260921090323_AddSignupSetting',
        '20260929112910_AddEmailOutbox'
    ];
    applied_migrations text[];
    legacy_tables text[] := ARRAY[
        'Groups',
        'Scouts',
        'Patrols',
        'PatrolMemberships',
        'GroupPreSignups',
        'AspNetRoles',
        'AspNetUsers',
        'AspNetRoleClaims',
        'AspNetUserClaims',
        'AspNetUserLogins',
        'AspNetUserRoles',
        'AspNetUserTokens',
        'AspNetUserPasskeys',
        'ActivityTeams',
        'Activities',
        'CatalogData',
        'ActivityCategories',
        'ActivityCategoryCatalogData',
        'ActivityTeamMemberships',
        'Materials',
        'MaterialOrders',
        'EmailOutbox',
        'RegistrationSettings',
        'Invitations',
        'OpenIddictApplications',
        'OpenIddictScopes',
        'OpenIddictAuthorizations',
        'OpenIddictTokens'
    ];
    new_tables text[] := ARRAY[
        'scout_group',
        'scout',
        'patrol',
        'patrol_membership',
        'group_pre_signup',
        'scout_signup',
        'material',
        'material_order',
        'email_outbox',
        'registration_settings',
        'user_invitation',
        'scout_activity_timeslot',
        'open_iddict_applications',
        'open_iddict_scopes',
        'open_iddict_authorizations',
        'open_iddict_tokens',
        'activity_team',
        'activity',
        'activity_budget',
        'catalog_data',
        'activity_category',
        'activity_timeslots',
        'catalog_data_category',
        'activity_team_membership',
        'asp_net_roles',
        'asp_net_users',
        'asp_net_role_claims',
        'asp_net_user_claims',
        'asp_net_user_logins',
        'asp_net_user_roles',
        'asp_net_user_tokens',
        'asp_net_user_passkeys'
    ];
BEGIN
    IF EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'ds28_legacy_v016') THEN
        RAISE EXCEPTION 'Legacy archive already exists; migrate the previously converted database separately instead of copying stale data';
    END IF;

    FOREACH source_table IN ARRAY new_tables LOOP
        IF to_regclass(format('public.%I', source_table)) IS NOT NULL THEN
            RAISE EXCEPTION 'New-format table public.% already exists; migration from an already converted public schema requires separate handling', source_table;
        END IF;
    END LOOP;

    IF to_regclass('public."__EFMigrationsHistory"') IS NULL THEN
        FOREACH source_table IN ARRAY legacy_tables LOOP
            IF to_regclass(format('public.%I', source_table)) IS NOT NULL THEN
                RAISE EXCEPTION 'Legacy table % exists without EF migration history', source_table;
            END IF;
        END LOOP;
        RETURN;
    END IF;

    SELECT array_agg("MigrationId" ORDER BY "MigrationId") INTO applied_migrations
    FROM public."__EFMigrationsHistory";
    IF applied_migrations IS DISTINCT FROM expected_migrations
       AND applied_migrations IS DISTINCT FROM array_replace(expected_migrations,
           '20260929112910_AddEmailOutbox', '20260929075734_AddEmailOutbox') THEN
        RAISE EXCEPTION 'Expected all v0.1.6 EF migrations and no additional migrations; found %', applied_migrations;
    END IF;

    FOREACH source_table IN ARRAY new_tables LOOP
        IF to_regclass(format('ds28.%I', source_table)) IS NOT NULL THEN
            RAISE EXCEPTION 'Target table ds28.% already exists while legacy history is present; refusing to merge data', source_table;
        END IF;
    END LOOP;

    FOREACH source_table IN ARRAY legacy_tables LOOP
        EXECUTE format('LOCK TABLE public.%I IN ACCESS EXCLUSIVE MODE', source_table);
    END LOOP;

    IF EXISTS (
        SELECT 1
        FROM public."Groups"
        WHERE upper(btrim("District")) <> '' AND upper(btrim("District")) NOT IN ('DANEHOF', 'FIONIA')
    ) THEN
        RAISE EXCEPTION 'Legacy group districts must be empty, DANEHOF or FIONIA; resolve other values before migration';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM (VALUES
            ('Id', 'int4', 'NO'),
            ('EventType', 'text', 'YES'),
            ('CorrelationId', 'uuid', 'YES'),
            ('UserId', 'text', 'YES'),
            ('ToEmail', 'text', 'YES'),
            ('Subject', 'text', 'YES'),
            ('Body', 'text', 'YES'),
            ('CreatedAt', 'timestamptz', 'NO'),
            ('SentAt', 'timestamptz', 'YES'),
            ('NextAttemptAt', 'timestamptz', 'NO'),
            ('Attempts', 'int4', 'NO'),
            ('LastError', 'text', 'YES'),
            ('FailedAt', 'timestamptz', 'YES'),
            ('LockedAt', 'timestamptz', 'YES'),
            ('LockedBy', 'text', 'YES')
        ) AS expected(column_name, udt_name, is_nullable)
        FULL JOIN (
            SELECT column_name, udt_name, is_nullable
            FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = 'EmailOutbox'
        ) actual USING (column_name)
        WHERE expected.udt_name IS DISTINCT FROM actual.udt_name
           OR expected.is_nullable IS DISTINCT FROM actual.is_nullable
    ) THEN
        RAISE EXCEPTION 'EmailOutbox columns do not match the supported v0.1.6 schema; migration stopped without changing data';
    END IF;

    IF EXISTS (
        SELECT 1 FROM public."EmailOutbox" WHERE "Attempts" < 0
    ) THEN
        RAISE EXCEPTION 'Email outbox attempts cannot be negative; resolve these rows before migration';
    END IF;

    IF EXISTS (
        SELECT 1 FROM public."CatalogData" c
        LEFT JOIN public."Activities" a ON a."CatalogId" = c."Id"
        GROUP BY c."Id" HAVING count(a."Id") <> 1
    ) THEN
        RAISE EXCEPTION 'Each legacy catalog must belong to exactly one activity; resolve orphaned or shared catalogs before migration';
    END IF;

    IF EXISTS (
        SELECT 1 FROM public."Materials"
        WHERE "Price"::numeric IS DISTINCT FROM "Price"::numeric(10, 2)
           OR "Price"::text IN ('NaN', 'Infinity', '-Infinity')
    ) THEN
        RAISE EXCEPTION 'Material prices cannot be represented exactly as numeric(10, 2); resolve these prices before migration';
    END IF;

    IF EXISTS (
        SELECT 1 FROM public."Materials" WHERE "Price" < 0
    ) THEN
        RAISE EXCEPTION 'Material prices cannot be negative; resolve these prices before migration';
    END IF;

    IF EXISTS (
        SELECT 1 FROM public."MaterialOrders" WHERE "Quantity" <= 0
    ) THEN
        RAISE EXCEPTION 'Material order quantities must be greater than zero; resolve these orders before migration';
    END IF;

    IF EXISTS (
        SELECT 1 FROM public."Activities" WHERE "Budget_Budget" < 0
    ) THEN
        RAISE EXCEPTION 'Activity budgets cannot be negative; resolve these budgets before migration';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM public."GroupPreSignups"
        WHERE least("Beaver", "Wolf", "Junior", "Trop", "Senior", "Rover", "Leader") < 0
    ) THEN
        RAISE EXCEPTION 'Group pre-signup counts cannot be negative; resolve these rows before migration';
    END IF;

    IF (SELECT count(*) FROM public."RegistrationSettings") <> 1
       OR NOT EXISTS (SELECT 1 FROM public."RegistrationSettings" WHERE "Id" = 1) THEN
        RAISE EXCEPTION 'Expected exactly one legacy registration settings row with Id 1';
    END IF;

END
$transition$;
