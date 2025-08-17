-- PostgreSQL Test User Seeding Script  
-- WitchCityRope Docker Authentication System
--
-- This script creates test users for development environment ONLY
-- Production environments should NOT execute this script
--
-- SECURITY NOTE: This script contains hardcoded test passwords
-- It should only run in development containers with proper isolation

-- ============================================================================
-- 1. ENVIRONMENT SAFETY CHECK
-- ============================================================================

-- Only run in development environment
DO $$
BEGIN
    -- Check if this is a development database
    IF current_database() NOT LIKE '%dev%' AND current_database() NOT LIKE '%test%' THEN
        RAISE EXCEPTION 'SECURITY: Test user seeding blocked - not a development/test database: %', current_database();
    END IF;
    
    RAISE NOTICE 'Development environment confirmed: %', current_database();
END
$$;

-- ============================================================================
-- 2. ASP.NET CORE IDENTITY TABLES CHECK
-- ============================================================================

-- Wait for EF Core to create Identity tables before seeding
-- This script runs before EF migrations, so it creates a placeholder function
-- The actual seeding will be handled by the API on startup

-- Create a function that will be called by the API to seed test data
CREATE OR REPLACE FUNCTION seed_test_users()
RETURNS TEXT AS $$
DECLARE
    result_text TEXT := '';
BEGIN
    -- Check if Identity tables exist
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables 
        WHERE table_schema = 'auth' AND table_name = 'Users'
    ) THEN
        RETURN 'Identity tables not yet created by EF Core migrations';
    END IF;

    -- Check if test users already exist
    IF EXISTS (SELECT 1 FROM auth."Users" WHERE "Email" LIKE '%@example.com') THEN
        RETURN 'Test users already exist';
    END IF;

    -- Since this runs before EF Core creates tables, we'll create a seeding script
    -- that the API can execute after migrations run
    
    result_text := 'Test user seeding function created - will be executed by API after migrations';
    
    RETURN result_text;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- 3. TEST USER DATA PREPARATION
-- ============================================================================

-- Create a table to store test user data that will be processed by the API
CREATE TABLE IF NOT EXISTS public."TestUserSeeds" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Email" TEXT NOT NULL,
    "SceneName" TEXT NOT NULL,
    "PronouncedName" TEXT NOT NULL,
    "Pronouns" TEXT NOT NULL,
    "Role" TEXT NOT NULL,
    "PasswordHash" TEXT NOT NULL, -- This will be computed by the API using proper hashing
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsVetted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DateOfBirth" TIMESTAMPTZ NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Insert test user seeds (API will process these into actual users)
INSERT INTO public."TestUserSeeds" (
    "Email", "SceneName", "PronouncedName", "Pronouns", "Role", 
    "PasswordHash", "IsActive", "IsVetted", "DateOfBirth"
) VALUES 
    -- Test Admin User
    (
        'testuser@example.com',
        'TestAdmin',
        'Test Admin',
        'they/them',
        'Admin',
        'Test1234', -- Plain text - will be hashed by API
        TRUE,
        TRUE,
        '1990-01-01 00:00:00+00'::TIMESTAMPTZ
    ),
    -- Test Member User  
    (
        'member@example.com',
        'TestMember',
        'Test Member',
        'she/her',
        'Member',
        'Test1234', -- Plain text - will be hashed by API
        TRUE,
        FALSE,
        '1992-06-15 00:00:00+00'::TIMESTAMPTZ
    ),
    -- Test Vetted Member
    (
        'vetted@example.com',
        'TestVetted',
        'Test Vetted',
        'he/him',
        'VettedMember', 
        'Test1234', -- Plain text - will be hashed by API
        TRUE,
        TRUE,
        '1988-03-22 00:00:00+00'::TIMESTAMPTZ
    ),
    -- Test Teacher
    (
        'teacher@example.com',
        'TestTeacher',
        'Test Teacher',
        'she/her',
        'Teacher',
        'Test1234', -- Plain text - will be hashed by API
        TRUE,
        TRUE,
        '1985-11-10 00:00:00+00'::TIMESTAMPTZ
    ),
    -- Test Guest User
    (
        'guest@example.com',
        'TestGuest',
        'Test Guest',
        'they/them',
        'Guest',
        'Test1234', -- Plain text - will be hashed by API
        TRUE,
        FALSE,
        '1995-08-30 00:00:00+00'::TIMESTAMPTZ
    )
ON CONFLICT ("Email") DO NOTHING;

-- Grant permissions on test seed table
GRANT SELECT, DELETE ON public."TestUserSeeds" TO witchcityrope_app;

-- ============================================================================
-- 4. TEST EVENT DATA
-- ============================================================================

-- Create test events for development (after Events table is created by EF)
CREATE TABLE IF NOT EXISTS public."TestEventSeeds" (
    "Id" UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    "Title" TEXT NOT NULL,
    "Description" TEXT NOT NULL,
    "StartDate" TIMESTAMPTZ NOT NULL,
    "EndDate" TIMESTAMPTZ NOT NULL,
    "Capacity" INTEGER NOT NULL,
    "EventType" TEXT NOT NULL,
    "Location" TEXT NOT NULL,
    "IsPublished" BOOLEAN NOT NULL DEFAULT TRUE,
    "PricingTiers" TEXT NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Insert test event data
INSERT INTO public."TestEventSeeds" (
    "Title", "Description", "StartDate", "EndDate", "Capacity", 
    "EventType", "Location", "IsPublished", "PricingTiers"
) VALUES 
    (
        'Introduction to Rope Bondage',
        'Learn the basics of safe rope bondage techniques with experienced instructors.',
        (NOW() + INTERVAL '7 days')::TIMESTAMPTZ,
        (NOW() + INTERVAL '7 days 3 hours')::TIMESTAMPTZ,
        20,
        'Workshop',
        'WitchCity Studio A',
        TRUE,
        '{"General": 45, "Member": 35, "VettedMember": 25}'
    ),
    (
        'Advanced Suspension Techniques',
        'Advanced workshop for experienced rope practitioners focusing on safe suspension.',
        (NOW() + INTERVAL '14 days')::TIMESTAMPTZ,
        (NOW() + INTERVAL '14 days 4 hours')::TIMESTAMPTZ,
        12,
        'Workshop', 
        'WitchCity Studio B',
        TRUE,
        '{"VettedMember": 60, "Teacher": 40}'
    ),
    (
        'Community Social Night',
        'Casual social gathering for community members to connect and network.',
        (NOW() + INTERVAL '21 days')::TIMESTAMPTZ,
        (NOW() + INTERVAL '21 days 4 hours')::TIMESTAMPTZ,
        50,
        'Social',
        'WitchCity Main Hall',
        TRUE,
        '{"Member": 15, "VettedMember": 10, "Guest": 20}'
    )
ON CONFLICT ("Id") DO NOTHING;

-- Grant permissions on test event seeds
GRANT SELECT, DELETE ON public."TestEventSeeds" TO witchcityrope_app;

-- ============================================================================
-- 5. DEVELOPMENT UTILITIES
-- ============================================================================

-- Function to clean up all test data (for development reset)
CREATE OR REPLACE FUNCTION cleanup_test_data()
RETURNS TEXT AS $$
DECLARE
    result_text TEXT := '';
    user_count INTEGER;
    event_count INTEGER;
BEGIN
    -- Count existing test data
    SELECT COUNT(*) INTO user_count FROM auth."Users" WHERE "Email" LIKE '%@example.com';
    SELECT COUNT(*) INTO event_count FROM public."Events" WHERE "Title" LIKE 'Test%' OR "Title" LIKE '%Test%';
    
    -- Delete test users (if Identity tables exist)
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'auth' AND table_name = 'Users') THEN
        DELETE FROM auth."Users" WHERE "Email" LIKE '%@example.com';
    END IF;
    
    -- Delete test events (if Events table exists)
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'Events') THEN
        DELETE FROM public."Events" WHERE "Title" LIKE '%Test%' OR "Title" LIKE 'Introduction%' OR "Title" LIKE 'Advanced%';
    END IF;
    
    -- Clear seed tables
    DELETE FROM public."TestUserSeeds";
    DELETE FROM public."TestEventSeeds";
    
    result_text := format('Cleaned up %s test users and %s test events', user_count, event_count);
    
    -- Log cleanup action
    INSERT INTO public."AuditLog" ("Action", "TableName", "Changes", "Timestamp") 
    VALUES ('TEST_DATA_CLEANUP', 'Multiple', jsonb_build_object('message', result_text), NOW());
    
    RETURN result_text;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- 6. DEVELOPMENT ONLY PERMISSIONS
-- ============================================================================

-- Grant test data cleanup permissions to application user
GRANT EXECUTE ON FUNCTION seed_test_users() TO witchcityrope_app;
GRANT EXECUTE ON FUNCTION cleanup_test_data() TO witchcityrope_app;

-- ============================================================================
-- 7. LOG SEEDING COMPLETION
-- ============================================================================

-- Log test data seeding preparation
INSERT INTO public."InitializationLog" ("ScriptName", "Notes") 
VALUES ('03-seed-test-user.sql', 'Test data seeds prepared - API will process after EF migrations')
ON CONFLICT ("ScriptName") DO UPDATE SET 
    "ExecutedAt" = NOW(),
    "Notes" = EXCLUDED."Notes";

-- ============================================================================
-- 8. VALIDATION AND REPORTING
-- ============================================================================

DO $$
DECLARE
    seed_count INTEGER;
    event_seed_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO seed_count FROM public."TestUserSeeds";
    SELECT COUNT(*) INTO event_seed_count FROM public."TestEventSeeds";
    
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Test Data Seeding Preparation Complete';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'Environment: %', current_database();
    RAISE NOTICE 'User seeds prepared: %', seed_count;
    RAISE NOTICE 'Event seeds prepared: %', event_seed_count;
    RAISE NOTICE 'Functions created: seed_test_users(), cleanup_test_data()';
    RAISE NOTICE '============================================================================';
    RAISE NOTICE 'SECURITY: Test data only for development environment';
    RAISE NOTICE 'API will process seeds after EF Core migrations complete';
    RAISE NOTICE '============================================================================';
END
$$;

\echo 'Test user seeding preparation completed successfully'