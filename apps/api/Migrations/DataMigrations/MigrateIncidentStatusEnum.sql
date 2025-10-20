-- ========================================
-- INCIDENT STATUS ENUM DATA MIGRATION
-- ========================================
-- This script migrates existing IncidentStatus values from 4-stage to 5-stage enum
-- CRITICAL: Run this AFTER applying the UpdateIncidentReportingSchema migration
-- ========================================
-- Migration Mapping:
--   Old Status          → New Status
--   -----------------------------------------
--   New (1)            → ReportSubmitted (1)  [NO CHANGE]
--   InProgress (2)     → InformationGathering (2)  [NO CHANGE]
--   Resolved (3)       → Closed (5)  [LOSSY - maps to Closed]
--   Archived (4)       → Closed (5)  [LOSSY - maps to Closed]
-- ========================================

BEGIN TRANSACTION;

-- Log migration start
DO $$
BEGIN
    RAISE NOTICE 'Starting IncidentStatus enum migration...';
END $$;

-- Create system notes for all existing incidents
-- This provides audit trail for the migration
INSERT INTO "IncidentNotes" (
    "Id",
    "IncidentId",
    "Content",
    "Type",
    "IsPrivate",
    "AuthorId",
    "CreatedAt"
)
SELECT
    gen_random_uuid(),
    "Id",
    'Status migrated from legacy system during October 2025 schema update.',
    2,  -- System note type
    false,
    NULL,  -- System-generated note
    NOW()
FROM "SafetyIncidents";

-- Migrate status values
-- NOTE: Values 1 and 2 remain unchanged, only 3 and 4 need migration
UPDATE "SafetyIncidents"
SET "Status" = CASE
    WHEN "Status" = 1 THEN 1  -- New → ReportSubmitted (unchanged)
    WHEN "Status" = 2 THEN 2  -- InProgress → InformationGathering (unchanged)
    WHEN "Status" = 3 THEN 5  -- Resolved → Closed (LOSSY)
    WHEN "Status" = 4 THEN 5  -- Archived → Closed (LOSSY)
    ELSE "Status"             -- Preserve any other values (should not exist)
END,
"UpdatedAt" = NOW()
WHERE "Status" IN (3, 4);  -- Only update Resolved and Archived

-- Verify migration
DO $$
DECLARE
    v_count INTEGER;
    v_migrated INTEGER;
    v_total INTEGER;
BEGIN
    -- Check for any status values outside new range (1-5)
    SELECT COUNT(*) INTO v_count
    FROM "SafetyIncidents"
    WHERE "Status" NOT IN (1, 2, 3, 4, 5);

    IF v_count > 0 THEN
        RAISE EXCEPTION 'Migration failed: % incidents have invalid status values', v_count;
    END IF;

    -- Get migration counts
    SELECT COUNT(*) INTO v_migrated
    FROM "SafetyIncidents"
    WHERE "Status" = 5;  -- New Closed status

    SELECT COUNT(*) INTO v_total
    FROM "SafetyIncidents";

    -- Log migration completion
    RAISE NOTICE 'Migration completed successfully.';
    RAISE NOTICE 'Total incidents: %', v_total;
    RAISE NOTICE 'Migrated to Closed status: %', v_migrated;
END $$;

-- Show migration results
SELECT
    'Migration Results' as description,
    COUNT(*) as total_incidents,
    SUM(CASE WHEN "Status" = 1 THEN 1 ELSE 0 END) as report_submitted_count,
    SUM(CASE WHEN "Status" = 2 THEN 1 ELSE 0 END) as information_gathering_count,
    SUM(CASE WHEN "Status" = 3 THEN 1 ELSE 0 END) as reviewing_report_count,
    SUM(CASE WHEN "Status" = 4 THEN 1 ELSE 0 END) as on_hold_count,
    SUM(CASE WHEN "Status" = 5 THEN 1 ELSE 0 END) as closed_count
FROM "SafetyIncidents";

COMMIT;

-- ========================================
-- POST-MIGRATION VERIFICATION
-- ========================================
-- Run these queries after migration to verify success:
--
-- 1. Check status distribution:
--    SELECT "Status", COUNT(*) FROM "SafetyIncidents" GROUP BY "Status";
--
-- 2. Verify system notes created:
--    SELECT COUNT(*) FROM "IncidentNotes" WHERE "Type" = 2;
--
-- 3. Check for invalid statuses:
--    SELECT COUNT(*) FROM "SafetyIncidents" WHERE "Status" NOT IN (1,2,3,4,5);
--    -- Should return 0
-- ========================================
