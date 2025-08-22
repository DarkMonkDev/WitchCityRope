-- PostgreSQL Schema Preparation Script
-- WitchCityRope Docker Authentication System
-- 
-- This script prepares the database schema for ASP.NET Core Identity
-- and application-specific tables before EF Core migrations run
-- 
-- NOTE: The actual Identity tables will be created by EF Core migrations
-- This script only prepares the environment and creates supporting objects

-- ============================================================================
-- 1. SCHEMA PREPARATION
-- ============================================================================

-- Ensure schemas exist (redundant check from previous script)
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS public;

-- ============================================================================
-- 2. CUSTOM TYPES AND DOMAINS
-- ============================================================================

-- Create custom types for application-specific data

-- User role enumeration (matches ApplicationUser.Role property)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'user_role_enum') THEN
        CREATE TYPE user_role_enum AS ENUM (
            'Guest',
            'Attendee', 
            'Member',
            'VettedMember',
            'Teacher',
            'Admin'
        );
        COMMENT ON TYPE user_role_enum IS 'User role hierarchy for WitchCityRope application';
    END IF;
END
$$;

-- Event type enumeration (matches Event.EventType property)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'event_type_enum') THEN
        CREATE TYPE event_type_enum AS ENUM (
            'Workshop',
            'Performance',
            'Social',
            'Private',
            'Special'
        );
        COMMENT ON TYPE event_type_enum IS 'Event categories for WitchCityRope events';
    END IF;
END
$$;

-- ============================================================================
-- 3. UTILITY FUNCTIONS
-- ============================================================================

-- Function to ensure DateTime values are UTC (supports EF Core UTC handling)
CREATE OR REPLACE FUNCTION ensure_utc_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    -- Ensure CreatedAt is UTC on INSERT
    IF TG_OP = 'INSERT' THEN
        NEW."CreatedAt" = (NEW."CreatedAt" AT TIME ZONE 'UTC');
        NEW."UpdatedAt" = (NEW."UpdatedAt" AT TIME ZONE 'UTC');
    END IF;
    
    -- Ensure UpdatedAt is UTC on UPDATE
    IF TG_OP = 'UPDATE' THEN
        NEW."UpdatedAt" = (NEW."UpdatedAt" AT TIME ZONE 'UTC');
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION ensure_utc_timestamp() IS 'Trigger function to ensure all timestamps are stored as UTC';

-- ============================================================================
-- 4. PERFORMANCE OPTIMIZATION OBJECTS
-- ============================================================================

-- Create partial indexes that will be useful after EF Core creates tables
-- These will be created in a separate script after tables exist

-- ============================================================================
-- 5. AUDIT AND MONITORING SETUP
-- ============================================================================

-- Create audit log table for security monitoring
CREATE TABLE IF NOT EXISTS public."AuditLog" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "UserId" UUID NULL,
    "Action" TEXT NOT NULL,
    "TableName" TEXT NOT NULL,
    "RecordId" TEXT NULL,
    "Changes" JSONB NULL,
    "IPAddress" INET NULL,
    "UserAgent" TEXT NULL,
    "Timestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Success" BOOLEAN NOT NULL DEFAULT TRUE,
    "ErrorMessage" TEXT NULL
);

-- Index for audit log queries
CREATE INDEX IF NOT EXISTS "IX_AuditLog_Timestamp" ON public."AuditLog" ("Timestamp" DESC);
CREATE INDEX IF NOT EXISTS "IX_AuditLog_UserId" ON public."AuditLog" ("UserId") WHERE "UserId" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_AuditLog_Action" ON public."AuditLog" ("Action");

COMMENT ON TABLE public."AuditLog" IS 'Security audit log for user actions and system events';

-- ============================================================================
-- 6. APPLICATION CONFIGURATION TABLE
-- ============================================================================

-- Create configuration table for runtime application settings
CREATE TABLE IF NOT EXISTS public."ApplicationConfiguration" (
    "Key" TEXT PRIMARY KEY,
    "Value" TEXT NOT NULL,
    "Description" TEXT NULL,
    "IsEncrypted" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE public."ApplicationConfiguration" IS 'Runtime configuration settings for WitchCityRope application';

-- Insert default configuration values
INSERT INTO public."ApplicationConfiguration" ("Key", "Value", "Description", "IsEncrypted") 
VALUES 
    ('SystemTimezone', 'UTC', 'System timezone for all operations', FALSE),
    ('EmailVerificationRequired', 'true', 'Whether email verification is required for new users', FALSE),
    ('MaxLoginAttempts', '5', 'Maximum login attempts before account lockout', FALSE),
    ('PasswordMinLength', '8', 'Minimum password length requirement', FALSE),
    ('SessionTimeoutMinutes', '60', 'Session timeout in minutes', FALSE)
ON CONFLICT ("Key") DO NOTHING;

-- ============================================================================
-- 7. DEVELOPMENT DATA PREPARATION
-- ============================================================================

-- Create a table to track which initialization scripts have run
CREATE TABLE IF NOT EXISTS public."InitializationLog" (
    "ScriptName" TEXT PRIMARY KEY,
    "ExecutedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "Success" BOOLEAN NOT NULL DEFAULT TRUE,
    "Notes" TEXT NULL
);

COMMENT ON TABLE public."InitializationLog" IS 'Tracks database initialization script execution';

-- Log this script execution
INSERT INTO public."InitializationLog" ("ScriptName", "Notes") 
VALUES ('02-create-schema.sql', 'Schema preparation and utility objects created')
ON CONFLICT ("ScriptName") DO UPDATE SET 
    "ExecutedAt" = NOW(),
    "Notes" = EXCLUDED."Notes";

-- ============================================================================
-- 8. PERMISSIONS FOR APPLICATION USER
-- ============================================================================

-- Grant permissions on utility objects to application user
GRANT SELECT, INSERT, UPDATE, DELETE ON public."AuditLog" TO witchcityrope_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON public."ApplicationConfiguration" TO witchcityrope_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON public."InitializationLog" TO witchcityrope_app;

-- Grant usage on sequences (for UUID generation and any future sequences)
GRANT USAGE ON ALL SEQUENCES IN SCHEMA public TO witchcityrope_app;
GRANT USAGE ON ALL SEQUENCES IN SCHEMA auth TO witchcityrope_app;

-- ============================================================================
-- 9. VALIDATION AND REPORTING
-- ============================================================================

DO $$
BEGIN
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'PostgreSQL Schema Preparation Complete';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Custom Types: user_role_enum, event_type_enum';
    RAISE NOTICE 'Utility Functions: ensure_utc_timestamp()';
    RAISE NOTICE 'Audit Tables: AuditLog, ApplicationConfiguration, InitializationLog';
    RAISE NOTICE 'Ready for EF Core Identity table creation';
    RAISE NOTICE '============================================================================';
END
$$;

\echo 'Schema preparation script completed successfully'