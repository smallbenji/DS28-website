DO
$$
    BEGIN
        IF NOT EXISTS(
            SELECT schema_name
            FROM information_schema.schemata
            WHERE schema_name = 'hangfire'
            )
        THEN
            EXECUTE 'CREATE SCHEMA "hangfire";';
        END IF;

    END
$$;

SET search_path = 'hangfire';
--
-- Table structure for table `Schema`
--

CREATE TABLE IF NOT EXISTS "schema"
(
    "version" INT NOT NULL,
    PRIMARY KEY ("version")
);


DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 3) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

INSERT INTO "schema"("version")
VALUES ('1');

--
-- Table structure for table `Counter`
--

CREATE TABLE IF NOT EXISTS "counter"
(
    "id"       SERIAL       NOT NULL,
    "key"      VARCHAR(100) NOT NULL,
    "value"    SMALLINT     NOT NULL,
    "expireat" TIMESTAMP    NULL,
    PRIMARY KEY ("id")
);

DO
$$
    BEGIN
        BEGIN
            CREATE INDEX "ix_hangfire_counter_key" ON "counter" ("key");
        EXCEPTION
            WHEN duplicate_table THEN RAISE NOTICE 'INDEX ix_hangfire_counter_key already exists.';
        END;
    END;
$$;

--
-- Table structure for table `Hash`
--

CREATE TABLE IF NOT EXISTS "hash"
(
    "id"       SERIAL       NOT NULL,
    "key"      VARCHAR(100) NOT NULL,
    "field"    VARCHAR(100) NOT NULL,
    "value"    TEXT         NULL,
    "expireat" TIMESTAMP    NULL,
    PRIMARY KEY ("id"),
    UNIQUE ("key", "field")
);


--
-- Table structure for table `Job`
--

CREATE TABLE IF NOT EXISTS "job"
(
    "id"             SERIAL      NOT NULL,
    "stateid"        INT         NULL,
    "statename"      VARCHAR(20) NULL,
    "invocationdata" TEXT        NOT NULL,
    "arguments"      TEXT        NOT NULL,
    "createdat"      TIMESTAMP   NOT NULL,
    "expireat"       TIMESTAMP   NULL,
    PRIMARY KEY ("id")
);

DO
$$
    BEGIN
        BEGIN
            CREATE INDEX "ix_hangfire_job_statename" ON "job" ("statename");
        EXCEPTION
            WHEN duplicate_table THEN RAISE NOTICE 'INDEX "ix_hangfire_job_statename" already exists.';
        END;
    END;
$$;

--
-- Table structure for table `State`
--

CREATE TABLE IF NOT EXISTS "state"
(
    "id"        SERIAL       NOT NULL,
    "jobid"     INT          NOT NULL,
    "name"      VARCHAR(20)  NOT NULL,
    "reason"    VARCHAR(100) NULL,
    "createdat" TIMESTAMP    NOT NULL,
    "data"      TEXT         NULL,
    PRIMARY KEY ("id"),
    FOREIGN KEY ("jobid") REFERENCES "job" ("id") ON UPDATE CASCADE ON DELETE CASCADE
);

DO
$$
    BEGIN
        BEGIN
            CREATE INDEX "ix_hangfire_state_jobid" ON "state" ("jobid");
        EXCEPTION
            WHEN duplicate_table THEN RAISE NOTICE 'INDEX "ix_hangfire_state_jobid" already exists.';
        END;
    END;
$$;



--
-- Table structure for table `JobQueue`
--

CREATE TABLE IF NOT EXISTS "jobqueue"
(
    "id"        SERIAL      NOT NULL,
    "jobid"     INT         NOT NULL,
    "queue"     VARCHAR(20) NOT NULL,
    "fetchedat" TIMESTAMP   NULL,
    PRIMARY KEY ("id")
);

DO
$$
    BEGIN
        BEGIN
            CREATE INDEX "ix_hangfire_jobqueue_queueandfetchedat" ON "jobqueue" ("queue", "fetchedat");
        EXCEPTION
            WHEN duplicate_table THEN RAISE NOTICE 'INDEX "ix_hangfire_jobqueue_queueandfetchedat" already exists.';
        END;
    END;
$$;


--
-- Table structure for table `List`
--

CREATE TABLE IF NOT EXISTS "list"
(
    "id"       SERIAL       NOT NULL,
    "key"      VARCHAR(100) NOT NULL,
    "value"    TEXT         NULL,
    "expireat" TIMESTAMP    NULL,
    PRIMARY KEY ("id")
);


--
-- Table structure for table `Server`
--

CREATE TABLE IF NOT EXISTS "server"
(
    "id"            VARCHAR(50) NOT NULL,
    "data"          TEXT        NULL,
    "lastheartbeat" TIMESTAMP   NOT NULL,
    PRIMARY KEY ("id")
);


--
-- Table structure for table `Set`
--

CREATE TABLE IF NOT EXISTS "set"
(
    "id"       SERIAL       NOT NULL,
    "key"      VARCHAR(100) NOT NULL,
    "score"    FLOAT8       NOT NULL,
    "value"    TEXT         NOT NULL,
    "expireat" TIMESTAMP    NULL,
    PRIMARY KEY ("id"),
    UNIQUE ("key", "value")
);


--
-- Table structure for table `JobParameter`
--

CREATE TABLE IF NOT EXISTS "jobparameter"
(
    "id"    SERIAL      NOT NULL,
    "jobid" INT         NOT NULL,
    "name"  VARCHAR(40) NOT NULL,
    "value" TEXT        NULL,
    PRIMARY KEY ("id"),
    FOREIGN KEY ("jobid") REFERENCES "job" ("id") ON UPDATE CASCADE ON DELETE CASCADE
);

DO
$$
    BEGIN
        BEGIN
            CREATE INDEX "ix_hangfire_jobparameter_jobidandname" ON "jobparameter" ("jobid", "name");
        EXCEPTION
            WHEN duplicate_table THEN RAISE NOTICE 'INDEX "ix_hangfire_jobparameter_jobidandname" already exists.';
        END;
    END;
$$;

CREATE TABLE IF NOT EXISTS "lock"
(
    "resource" VARCHAR(100) NOT NULL,
    UNIQUE ("resource")
);

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 3 WHERE "version" = 1;

SET search_path = 'hangfire';
--
-- Table structure for table `Schema`
--

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 4) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

ALTER TABLE "counter"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;
ALTER TABLE "lock"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;
ALTER TABLE "hash"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;
ALTER TABLE "job"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;
ALTER TABLE "jobparameter"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;
ALTER TABLE "jobqueue"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;
ALTER TABLE "list"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;
ALTER TABLE "server"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;
ALTER TABLE "set"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;
ALTER TABLE "state"
    ADD COLUMN "updatecount" integer NOT NULL DEFAULT 0;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 4 WHERE "version" = 3;

SET search_path = 'hangfire';
--
-- Table structure for table `Schema`
--

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 5) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

ALTER TABLE "server"
    ALTER COLUMN "id" TYPE VARCHAR(100);

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 5 WHERE "version" = 4;

SET search_path = 'hangfire';
--
-- Adds indices, greatly speeds-up deleting old jobs.
--

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 6) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;


DO
$$
    BEGIN
        BEGIN
            CREATE INDEX "ix_hangfire_counter_expireat" ON "counter" ("expireat");
        EXCEPTION
            WHEN duplicate_table THEN RAISE NOTICE 'INDEX ix_hangfire_counter_expireat already exists.';
        END;
    END;
$$;

DO
$$
    BEGIN
        BEGIN
            CREATE INDEX "ix_hangfire_jobqueue_jobidandqueue" ON "jobqueue" ("jobid", "queue");
        EXCEPTION
            WHEN duplicate_table THEN RAISE NOTICE 'INDEX "ix_hangfire_jobqueue_jobidandqueue" already exists.';
        END;
    END;
$$;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 6 WHERE "version" = 5;

SET search_path = 'hangfire';
--
-- Table structure for table `Schema`
--

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 7) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

ALTER TABLE "lock"
    ADD COLUMN acquired timestamp without time zone;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 7 WHERE "version" = 6;

SET search_path = 'hangfire';
--
-- Table structure for table `Schema`
--

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 8) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

ALTER TABLE "counter"
    ALTER COLUMN value TYPE bigint;
ALTER TABLE "counter"
    DROP COLUMN updatecount RESTRICT;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 8 WHERE "version" = 7;

SET search_path = 'hangfire';
--
-- Table structure for table `Schema`
--

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 9) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

ALTER TABLE "lock"
    ALTER COLUMN "resource" TYPE TEXT;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 9 WHERE "version" = 8;

SET search_path = 'hangfire';
--
-- Table structure for table `Schema`
--

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 10) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

ALTER TABLE "jobqueue"
    ALTER COLUMN "queue" TYPE TEXT;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 10 WHERE "version" = 9;

SET search_path = 'hangfire';

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 11) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

ALTER TABLE "counter"
    ALTER COLUMN id TYPE BIGINT;
ALTER TABLE "hash"
    ALTER COLUMN id TYPE BIGINT;
ALTER TABLE "job"
    ALTER COLUMN id TYPE BIGINT;
ALTER TABLE "job"
    ALTER COLUMN stateid TYPE BIGINT;
ALTER TABLE "state"
    ALTER COLUMN id TYPE BIGINT;
ALTER TABLE "state"
    ALTER COLUMN jobid TYPE BIGINT;
ALTER TABLE "jobparameter"
    ALTER COLUMN id TYPE BIGINT;
ALTER TABLE "jobparameter"
    ALTER COLUMN jobid TYPE BIGINT;
ALTER TABLE "jobqueue"
    ALTER COLUMN id TYPE BIGINT;
ALTER TABLE "jobqueue"
    ALTER COLUMN jobid TYPE BIGINT;
ALTER TABLE "list"
    ALTER COLUMN id TYPE BIGINT;
ALTER TABLE "set"
    ALTER COLUMN id TYPE BIGINT;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 11 WHERE "version" = 10;

SET search_path = 'hangfire';

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 12) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

ALTER TABLE "counter"
    ALTER COLUMN "key" TYPE TEXT;
ALTER TABLE "hash"
    ALTER COLUMN "key" TYPE TEXT;
ALTER TABLE "hash"
    ALTER COLUMN field TYPE TEXT;
ALTER TABLE "job"
    ALTER COLUMN statename TYPE TEXT;
ALTER TABLE "list"
    ALTER COLUMN "key" TYPE TEXT;
ALTER TABLE "server"
    ALTER COLUMN id TYPE TEXT;
ALTER TABLE "set"
    ALTER COLUMN "key" TYPE TEXT;
ALTER TABLE "jobparameter"
    ALTER COLUMN "name" TYPE TEXT;
ALTER TABLE "state"
    ALTER COLUMN "name" TYPE TEXT;
ALTER TABLE "state"
    ALTER COLUMN reason TYPE TEXT;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 12 WHERE "version" = 11;

SET search_path = 'hangfire';



DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 13) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

CREATE INDEX IF NOT EXISTS jobqueue_queue_fetchat_jobId ON jobqueue USING btree (queue asc, fetchedat asc nulls last, jobid asc);

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 13 WHERE "version" = 12;

SET search_path = 'hangfire';

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 14) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

do
$$
    DECLARE
    BEGIN
        EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".job_id_seq AS bigint MAXVALUE 9223372036854775807');
    EXCEPTION
        WHEN syntax_error THEN
            EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".job_id_seq MAXVALUE 9223372036854775807');
    END;
$$;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 14 WHERE "version" = 13;

SET search_path = 'hangfire';

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 15) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

CREATE INDEX ix_hangfire_job_expireat ON "job" (expireat);
CREATE INDEX ix_hangfire_list_expireat ON "list" (expireat);
CREATE INDEX ix_hangfire_set_expireat ON "set" (expireat);
CREATE INDEX ix_hangfire_hash_expireat ON "hash" (expireat);

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 15 WHERE "version" = 14;

SET search_path = 'hangfire';

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 16) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

-- Note: job_id_seq is already bigint as per migration script v14
DO
$$
    DECLARE
    BEGIN
        EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".counter_id_seq AS bigint MAXVALUE 9223372036854775807');
        EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".hash_id_seq AS bigint MAXVALUE 9223372036854775807');
        EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".jobparameter_id_seq AS bigint MAXVALUE 9223372036854775807');
        EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".jobqueue_id_seq AS bigint MAXVALUE 9223372036854775807');
        EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".list_id_seq AS bigint MAXVALUE 9223372036854775807');
        EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".set_id_seq AS bigint MAXVALUE 9223372036854775807');
        EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".state_id_seq AS bigint MAXVALUE 9223372036854775807');
    EXCEPTION
        WHEN syntax_error THEN
            EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".counter_id_seq MAXVALUE 9223372036854775807');
            EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".hash_id_seq MAXVALUE 9223372036854775807');
            EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".jobparameter_id_seq MAXVALUE 9223372036854775807');
            EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".jobqueue_id_seq MAXVALUE 9223372036854775807');
            EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".list_id_seq MAXVALUE 9223372036854775807');
            EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".set_id_seq MAXVALUE 9223372036854775807');
            EXECUTE ('ALTER SEQUENCE "' || 'hangfire' || '".state_id_seq MAXVALUE 9223372036854775807');
    END
$$;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 16 WHERE "version" = 15;

SET search_path = 'hangfire';

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 17) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

CREATE INDEX IF NOT EXISTS ix_hangfire_set_key_score ON "set" (key, score);

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 17 WHERE "version" = 16;

SET search_path = 'hangfire';

DO
$$
    BEGIN
        IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 18) THEN
            RAISE EXCEPTION 'version-already-applied';
        END IF;
    END
$$;

CREATE TABLE aggregatedcounter (
    "id" bigserial PRIMARY KEY NOT NULL,
    "key" text NOT NULL UNIQUE,
    "value" int8 NOT NULL,
    "expireat" timestamp
);

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 18 WHERE "version" = 17;

SET search_path = 'hangfire';

DO $$
BEGIN
    IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 19) THEN
        RAISE EXCEPTION 'version-already-applied';
END IF;
END $$;

ALTER TABLE "aggregatedcounter" ALTER COLUMN "expireat" TYPE timestamp with time zone;
ALTER TABLE "counter" ALTER COLUMN "expireat" TYPE timestamp with time zone;
ALTER TABLE "hash" ALTER COLUMN "expireat" TYPE timestamp with time zone;
ALTER TABLE "job" ALTER COLUMN "createdat" TYPE timestamp with time zone;
ALTER TABLE "job" ALTER COLUMN "expireat" TYPE timestamp with time zone;
ALTER TABLE "jobqueue" ALTER COLUMN "fetchedat" TYPE timestamp with time zone;
ALTER TABLE "list" ALTER COLUMN "expireat" TYPE timestamp with time zone;
ALTER TABLE "lock" ALTER COLUMN "acquired" TYPE timestamp with time zone;
ALTER TABLE "server" ALTER COLUMN "lastheartbeat" TYPE timestamp with time zone;
ALTER TABLE "set" ALTER COLUMN "expireat" TYPE timestamp with time zone;
ALTER TABLE "state" ALTER COLUMN "createdat" TYPE timestamp with time zone;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 19 WHERE "version" = 18;

SET search_path = 'hangfire';

DO $$
BEGIN
    IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 20) THEN
        RAISE EXCEPTION 'version-already-applied';
END IF;
END $$;

-- Update existing jobs, if any have empty values first
UPDATE "job" SET "invocationdata" = '{}' WHERE "invocationdata" = '';
UPDATE "job" SET "arguments" = '[]' WHERE "arguments" = '';

-- Change the type

ALTER TABLE "job" ALTER COLUMN "invocationdata" TYPE jsonb USING "invocationdata"::jsonb;
ALTER TABLE "job" ALTER COLUMN "arguments" TYPE jsonb USING "arguments"::jsonb;
ALTER TABLE "server" ALTER COLUMN "data" TYPE jsonb USING "data"::jsonb;
ALTER TABLE "state" ALTER COLUMN "data" TYPE jsonb USING "data"::jsonb;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 20 WHERE "version" = 19;

SET search_path = 'hangfire';

DO $$
BEGIN
    IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 21) THEN
        RAISE EXCEPTION 'version-already-applied';
END IF;
END $$;

-- Set REPLICA IDENTITY to allow replication
ALTER TABLE "lock" REPLICA IDENTITY USING INDEX "lock_resource_key";

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 21 WHERE "version" = 20;

SET search_path = 'hangfire';

DO $$
BEGIN
    IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 22) THEN
        RAISE EXCEPTION 'version-already-applied';
END IF;
END $$;

DROP INDEX IF EXISTS jobqueue_queue_fetchat_jobId;
CREATE INDEX IF NOT EXISTS ix_hangfire_jobqueue_fetchedat_queue_jobid ON jobqueue USING btree (fetchedat nulls first, queue, jobid);

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 22 WHERE "version" = 21;

SET search_path = 'hangfire';

DO $$
BEGIN
    IF EXISTS(SELECT 1 FROM "schema" WHERE "version"::integer >= 23) THEN
        RAISE EXCEPTION 'version-already-applied';
    END IF;
END $$;

DROP INDEX IF EXISTS ix_hangfire_job_statename_is_not_null;

DO $$
BEGIN
    IF current_setting('server_version_num')::int >= 110000 THEN
        EXECUTE 'CREATE INDEX ix_hangfire_job_statename_is_not_null ON job USING btree(statename) INCLUDE (id) WHERE statename IS NOT NULL';
    ELSE
		CREATE INDEX ix_hangfire_job_statename_is_not_null ON job USING btree(statename) WHERE statename IS NOT NULL;
    END IF;
END $$;

RESET search_path;

UPDATE "hangfire"."schema" SET "version" = 23 WHERE "version" = 22;
