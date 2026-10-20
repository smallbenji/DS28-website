DO $transition$
DECLARE
    target_table text;
    identity_sequence text;
    source_table text;
    source_sequence text;
    source_last_id bigint;
    source_sequence_called boolean;
    highest_id bigint;
BEGIN
    IF to_regclass('public."__EFMigrationsHistory"') IS NULL THEN
        RETURN;
    END IF;

    INSERT INTO ds28.scout_group (id, district, name)
        SELECT s."Id",
            CASE WHEN upper(btrim(s."District")) = '' THEN 'DANEHOF' ELSE upper(btrim(s."District")) END,
            s."Name"
        FROM public."Groups" s;

    INSERT INTO ds28.scout (id, birthday, gender, group_id, name)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", (s."Birthday" AT TIME ZONE 'UTC')::date, upper(s."Gender"), s."GroupId", s."Name"
        FROM public."Scouts" s;

    INSERT INTO ds28.patrol (id, group_id, name)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."GroupId", s."Name"
        FROM public."Patrols" s;

    INSERT INTO ds28.patrol_membership (id, is_patrol_leader, joined_date, patrol_id, scout_id)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."IsPatrolLeader", s."JoinedDate", s."PatrolId", s."ScoutId"
        FROM public."PatrolMemberships" s;

    INSERT INTO ds28.group_pre_signup (id, beaver, group_id, junior, leader, rover, senior, trop, wolf)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."Beaver", s."GroupId", s."Junior", s."Leader", s."Rover", s."Senior", s."Trop", s."Wolf"
        FROM public."GroupPreSignups" s;

    INSERT INTO ds28.asp_net_roles (id, concurrency_stamp, name, normalized_name)
        SELECT s."Id", s."ConcurrencyStamp", s."Name", s."NormalizedName"
        FROM public."AspNetRoles" s;

    INSERT INTO ds28.asp_net_users (
        id, access_failed_count, concurrency_stamp, email,
        email_confirmed, first_name, group_id, has_enabled_authenticator,
        last_name, lockout_enabled, lockout_end, normalized_email,
        normalized_user_name, password_hash, phone_number, phone_number_confirmed,
        security_stamp, two_factor_enabled, user_name)
        SELECT
            s."Id", s."AccessFailedCount", s."ConcurrencyStamp", s."Email",
            s."EmailConfirmed", s."FirstName", s."GroupId", s."HasEnabledAuthenticator",
            s."LastName", s."LockoutEnabled", s."LockoutEnd", s."NormalizedEmail",
            s."NormalizedUserName", s."PasswordHash", s."PhoneNumber", s."PhoneNumberConfirmed",
            s."SecurityStamp", s."TwoFactorEnabled", s."UserName"
        FROM public."AspNetUsers" s;

    INSERT INTO ds28.asp_net_role_claims (id, claim_type, claim_value, role_id)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."ClaimType", s."ClaimValue", s."RoleId"
        FROM public."AspNetRoleClaims" s;

    INSERT INTO ds28.asp_net_user_claims (id, claim_type, claim_value, user_id)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."ClaimType", s."ClaimValue", s."UserId"
        FROM public."AspNetUserClaims" s;

    INSERT INTO ds28.asp_net_user_logins (login_provider, provider_key, provider_display_name, user_id)
        SELECT s."LoginProvider", s."ProviderKey", s."ProviderDisplayName", s."UserId"
        FROM public."AspNetUserLogins" s;

    INSERT INTO ds28.asp_net_user_roles (user_id, role_id)
        SELECT s."UserId", s."RoleId"
        FROM public."AspNetUserRoles" s;

    INSERT INTO ds28.asp_net_user_tokens (user_id, login_provider, name, value)
        SELECT s."UserId", s."LoginProvider", s."Name", s."Value"
        FROM public."AspNetUserTokens" s;

    INSERT INTO ds28.asp_net_user_passkeys (credential_id, user_id, data)
        SELECT s."CredentialId", s."UserId", s."Data"
        FROM public."AspNetUserPasskeys" s;

    INSERT INTO ds28.activity_team (id, name)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."Name"
        FROM public."ActivityTeams" s;

    INSERT INTO ds28.activity (id, activity_team_id, name)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."ActivityTeamId", s."Name"
        FROM public."Activities" s;

    INSERT INTO ds28.activity_budget (activity_id, budget)
        SELECT "Id", "Budget_Budget" FROM public."Activities"
        WHERE "Budget_Budget" IS NOT NULL;

    INSERT INTO ds28.catalog_data (id, description, name, summary, activity_id)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."Description", s."Name", s."Summary", a."Id"
        FROM public."CatalogData" s
        JOIN public."Activities" a ON a."CatalogId" = s."Id";

    INSERT INTO ds28.activity_category (id, name)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."Name"
        FROM public."ActivityCategories" s;

    INSERT INTO ds28.catalog_data_category (catalog_data_id, activity_category_id)
        SELECT s."CatalogDataId", s."CategoriesId"
        FROM public."ActivityCategoryCatalogData" s;

    INSERT INTO ds28.activity_team_membership (id, activity_team_id, is_admin, user_id)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."ActivityTeamId", s."IsAdmin", s."UserId"
        FROM public."ActivityTeamMemberships" s;

    INSERT INTO ds28.material (id, name, price, url)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."Name", s."Price", s."Url"
        FROM public."Materials" s;

    INSERT INTO ds28.material_order (id, activity_id, material_id, use_date, quantity)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."ActivityId", s."MaterialId", s."OrderedToDate", s."Quantity"
        FROM public."MaterialOrders" s;

    INSERT INTO ds28.email_outbox (
        id, attempts, body, correlation_id,
        created_at, event_type, failed_at, last_error,
        locked_at, locked_by, next_attempt_at, sent_at,
        subject, to_email, user_id)
        OVERRIDING SYSTEM VALUE
        SELECT
            s."Id", s."Attempts", s."Body", s."CorrelationId",
            s."CreatedAt", s."EventType", s."FailedAt", s."LastError",
            s."LockedAt", s."LockedBy", s."NextAttemptAt", s."SentAt",
            s."Subject", s."ToEmail", s."UserId"
        FROM public."EmailOutbox" s;

    DELETE FROM ds28.registration_settings;
    INSERT INTO ds28.registration_settings (id, is_pre_signup_open, is_signup_open)
        SELECT s."Id", s."IsPreSignupOpen", s."IsSignupOpen"
        FROM public."RegistrationSettings" s;

    INSERT INTO ds28.user_invitation (id, activity_team_id, email, group_id, invitation_id, is_admin, roles, used)
        OVERRIDING SYSTEM VALUE
        SELECT s."Id", s."ActivityTeamId", s."Email", s."GroupId", s."InvitationId", s."IsAdmin", s."Roles", s."Used"
        FROM public."Invitations" s;

    INSERT INTO ds28.open_iddict_applications (
        id, application_type, client_id, client_secret,
        client_type, concurrency_token, consent_type, display_name,
        display_names, json_web_key_set, permissions, post_logout_redirect_uris,
        properties, redirect_uris, requirements, settings)
        SELECT
            s."Id", s."ApplicationType", s."ClientId", s."ClientSecret",
            s."ClientType", s."ConcurrencyToken", s."ConsentType", s."DisplayName",
            s."DisplayNames", s."JsonWebKeySet", s."Permissions", s."PostLogoutRedirectUris",
            s."Properties", s."RedirectUris", s."Requirements", s."Settings"
        FROM public."OpenIddictApplications" s;

    INSERT INTO ds28.open_iddict_scopes (
        id, concurrency_token, description, descriptions,
        display_name, display_names, name, properties,
        resources)
        SELECT
            s."Id", s."ConcurrencyToken", s."Description", s."Descriptions",
            s."DisplayName", s."DisplayNames", s."Name", s."Properties",
            s."Resources"
        FROM public."OpenIddictScopes" s;

    INSERT INTO ds28.open_iddict_authorizations (
        id, application_id, concurrency_token, creation_date,
        properties, scopes, status, subject,
        type)
        SELECT
            s."Id", s."ApplicationId", s."ConcurrencyToken", s."CreationDate",
            s."Properties", s."Scopes", s."Status", s."Subject",
            s."Type"
        FROM public."OpenIddictAuthorizations" s;

    INSERT INTO ds28.open_iddict_tokens (
        id, application_id, authorization_id, concurrency_token,
        creation_date, expiration_date, payload, properties,
        redemption_date, reference_id, status, subject,
        type)
        SELECT
            s."Id", s."ApplicationId", s."AuthorizationId", s."ConcurrencyToken",
            s."CreationDate", s."ExpirationDate", s."Payload", s."Properties",
            s."RedemptionDate", s."ReferenceId", s."Status", s."Subject",
            s."Type"
        FROM public."OpenIddictTokens" s;

    FOR target_table, source_table IN
        SELECT * FROM (VALUES
            ('scout', 'Scouts'),
            ('patrol', 'Patrols'),
            ('patrol_membership', 'PatrolMemberships'),
            ('group_pre_signup', 'GroupPreSignups'),
            ('asp_net_role_claims', 'AspNetRoleClaims'),
            ('asp_net_user_claims', 'AspNetUserClaims'),
            ('activity_team', 'ActivityTeams'),
            ('activity', 'Activities'),
            ('catalog_data', 'CatalogData'),
            ('activity_category', 'ActivityCategories'),
            ('activity_team_membership', 'ActivityTeamMemberships'),
            ('material', 'Materials'),
            ('material_order', 'MaterialOrders'),
            ('email_outbox', 'EmailOutbox'),
            ('user_invitation', 'Invitations')
        ) AS identity_tables(target_name, source_name)
    LOOP
        identity_sequence := pg_get_serial_sequence('ds28.' || target_table, 'id');
        source_sequence := pg_get_serial_sequence(format('public.%I', source_table), 'Id');
        EXECUTE format('SELECT max(id) FROM ds28.%I', target_table) INTO highest_id;
        EXECUTE format('SELECT last_value, is_called FROM %s', source_sequence::regclass)
            INTO source_last_id, source_sequence_called;
        PERFORM setval(identity_sequence::regclass, greatest(coalesce(highest_id, 1), source_last_id),
            source_sequence_called OR coalesce(highest_id >= source_last_id, false));
    END LOOP;
END
$transition$;
