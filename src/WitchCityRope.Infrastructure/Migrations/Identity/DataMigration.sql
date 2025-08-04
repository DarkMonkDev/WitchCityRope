-- =====================================================
-- Data Migration Script: Migrate existing data to Identity tables
-- =====================================================
-- This script migrates existing user data from the old schema to the new Identity tables
-- Run this AFTER applying the AddIdentityTables migration
-- 
-- IMPORTANT: 
-- 1. Back up your database before running this script
-- 2. Run in a transaction to ensure data consistency
-- 3. Verify data integrity after migration

BEGIN TRANSACTION;

-- Step 1: Migrate Users from public.Users to auth.Users
-- Note: We'll need to handle password hashes separately
INSERT INTO auth."Users" (
    "Id",
    "UserName",
    "NormalizedUserName",
    "Email",
    "NormalizedEmail",
    "EmailConfirmed",
    "PasswordHash",
    "SecurityStamp",
    "ConcurrencyStamp",
    "PhoneNumber",
    "PhoneNumberConfirmed",
    "TwoFactorEnabled",
    "LockoutEnd",
    "LockoutEnabled",
    "AccessFailedCount",
    "EncryptedLegalName",
    "SceneName",
    "EmailValue",
    "EmailDisplayValue",
    "DateOfBirth",
    "Role",
    "IsActive",
    "IsVetted",
    "PronouncedName",
    "Pronouns",
    "CreatedAt",
    "UpdatedAt",
    "LastLoginAt",
    "FailedLoginAttempts",
    "LockedOutUntil",
    "LastPasswordChangeAt",
    "EmailVerificationToken",
    "EmailVerificationTokenCreatedAt"
)
SELECT 
    u."Id",
    COALESCE(u."Email", u."SceneName") AS "UserName", -- Use email as username, fallback to scene name
    UPPER(COALESCE(u."Email", u."SceneName")) AS "NormalizedUserName",
    u."Email",
    UPPER(u."Email") AS "NormalizedEmail",
    COALESCE(ua."EmailVerified", false) AS "EmailConfirmed",
    ua."PasswordHash",
    gen_random_uuid()::text AS "SecurityStamp", -- Generate new security stamp
    CONCAT(EXTRACT(EPOCH FROM NOW())::BIGINT, '-', gen_random_uuid()) AS "ConcurrencyStamp",
    NULL AS "PhoneNumber",
    false AS "PhoneNumberConfirmed",
    COALESCE(ua."IsTwoFactorEnabled", false) AS "TwoFactorEnabled",
    CASE 
        WHEN ua."LockedOutUntil" IS NOT NULL AND ua."LockedOutUntil" > NOW() 
        THEN ua."LockedOutUntil" AT TIME ZONE 'UTC'
        ELSE NULL 
    END AS "LockoutEnd",
    true AS "LockoutEnabled", -- Enable lockout for all users
    COALESCE(ua."FailedLoginAttempts", 0) AS "AccessFailedCount",
    u."EncryptedLegalName",
    u."SceneName",
    u."Email" AS "EmailValue",
    u."Email" AS "EmailDisplayValue",
    u."DateOfBirth",
    u."Role"::text,
    u."IsActive",
    u."IsVetted",
    COALESCE(u."PronouncedName", ua."PronouncedName", u."SceneName") AS "PronouncedName",
    COALESCE(u."Pronouns", ua."Pronouns", '') AS "Pronouns",
    u."CreatedAt",
    u."UpdatedAt",
    ua."LastLoginAt",
    COALESCE(ua."FailedLoginAttempts", 0) AS "FailedLoginAttempts",
    ua."LockedOutUntil",
    ua."LastPasswordChangeAt",
    ua."EmailVerificationToken",
    ua."EmailVerificationTokenCreatedAt"
FROM public."Users" u
LEFT JOIN public."UserAuthentications" ua ON u."Id" = ua."UserId"
WHERE NOT EXISTS (
    SELECT 1 FROM auth."Users" au WHERE au."Id" = u."Id"
);

-- Step 2: Assign roles to users based on their Role enum value
-- Map UserRole enum to Identity roles
INSERT INTO auth."UserRoles" ("UserId", "RoleId")
SELECT 
    u."Id" AS "UserId",
    CASE u."Role"
        WHEN 'Attendee' THEN '11111111-1111-1111-1111-111111111111'::uuid
        WHEN 'Member' THEN '22222222-2222-2222-2222-222222222222'::uuid
        WHEN 'Organizer' THEN '33333333-3333-3333-3333-333333333333'::uuid
        WHEN 'Admin' THEN '55555555-5555-5555-5555-555555555555'::uuid
        WHEN 'VettingTeam' THEN '44444444-4444-4444-4444-444444444444'::uuid -- Map to Moderator
        WHEN 'SafetyTeam' THEN '44444444-4444-4444-4444-444444444444'::uuid -- Map to Moderator
        ELSE '11111111-1111-1111-1111-111111111111'::uuid -- Default to Attendee
    END AS "RoleId"
FROM auth."Users" u
WHERE NOT EXISTS (
    SELECT 1 FROM auth."UserRoles" ur 
    WHERE ur."UserId" = u."Id"
);

-- Step 3: Add user claims for vetted status
INSERT INTO auth."UserClaims" ("UserId", "ClaimType", "ClaimValue")
SELECT 
    u."Id",
    'IsVetted' AS "ClaimType",
    'true' AS "ClaimValue"
FROM auth."Users" u
WHERE u."IsVetted" = true
AND NOT EXISTS (
    SELECT 1 FROM auth."UserClaims" uc 
    WHERE uc."UserId" = u."Id" AND uc."ClaimType" = 'IsVetted'
);

-- Step 4: Add additional role-based claims for special teams
-- VettingTeam claim for users who were in VettingTeam role
INSERT INTO auth."UserClaims" ("UserId", "ClaimType", "ClaimValue")
SELECT 
    u."Id",
    'Team' AS "ClaimType",
    'VettingTeam' AS "ClaimValue"
FROM auth."Users" u
WHERE u."Role" = 'VettingTeam'
AND NOT EXISTS (
    SELECT 1 FROM auth."UserClaims" uc 
    WHERE uc."UserId" = u."Id" AND uc."ClaimType" = 'Team' AND uc."ClaimValue" = 'VettingTeam'
);

-- SafetyTeam claim for users who were in SafetyTeam role
INSERT INTO auth."UserClaims" ("UserId", "ClaimType", "ClaimValue")
SELECT 
    u."Id",
    'Team' AS "ClaimType",
    'SafetyTeam' AS "ClaimValue"
FROM auth."Users" u
WHERE u."Role" = 'SafetyTeam'
AND NOT EXISTS (
    SELECT 1 FROM auth."UserClaims" uc 
    WHERE uc."UserId" = u."Id" AND uc."ClaimType" = 'Team' AND uc."ClaimValue" = 'SafetyTeam'
);

-- Step 5: Update RefreshTokens table to reference new auth.Users table
-- First, add a temporary column to track migration
ALTER TABLE public."RefreshTokens" ADD COLUMN IF NOT EXISTS "MigratedToIdentity" BOOLEAN DEFAULT FALSE;

-- Update foreign key reference (this assumes RefreshTokens will be updated to use auth.Users)
-- This step should be done in application code or a separate migration

-- Step 6: Create a migration audit log
CREATE TABLE IF NOT EXISTS public."IdentityMigrationLog" (
    "Id" SERIAL PRIMARY KEY,
    "TableName" VARCHAR(255) NOT NULL,
    "RecordId" UUID NOT NULL,
    "MigratedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "Status" VARCHAR(50) NOT NULL,
    "Details" TEXT
);

-- Log successful user migrations
INSERT INTO public."IdentityMigrationLog" ("TableName", "RecordId", "Status", "Details")
SELECT 
    'Users',
    u."Id",
    'Success',
    CONCAT('Migrated user ', u."SceneName", ' with role ', u."Role")
FROM auth."Users" u;

-- Step 7: Verify migration integrity
DO $$
DECLARE
    original_count INTEGER;
    migrated_count INTEGER;
BEGIN
    -- Count original users
    SELECT COUNT(*) INTO original_count FROM public."Users";
    
    -- Count migrated users
    SELECT COUNT(*) INTO migrated_count FROM auth."Users";
    
    -- Check if counts match
    IF original_count != migrated_count THEN
        RAISE EXCEPTION 'Migration failed: User count mismatch. Original: %, Migrated: %', 
            original_count, migrated_count;
    END IF;
    
    -- Verify all users have at least one role
    IF EXISTS (
        SELECT 1 FROM auth."Users" u
        WHERE NOT EXISTS (
            SELECT 1 FROM auth."UserRoles" ur WHERE ur."UserId" = u."Id"
        )
    ) THEN
        RAISE EXCEPTION 'Migration failed: Some users do not have roles assigned';
    END IF;
    
    RAISE NOTICE 'Migration verification passed. % users migrated successfully.', migrated_count;
END $$;

-- Step 8: Create views for backward compatibility (optional)
-- This allows existing queries to continue working during transition
CREATE OR REPLACE VIEW public."Users_IdentityView" AS
SELECT 
    u."Id",
    u."EncryptedLegalName",
    u."SceneName",
    u."EmailValue" AS "Email",
    u."DateOfBirth",
    u."Role"::public."UserRole" AS "Role",
    u."IsActive",
    u."IsVetted",
    u."CreatedAt",
    u."UpdatedAt",
    u."PronouncedName",
    u."Pronouns",
    u."EmailConfirmed" AS "EmailVerified",
    u."AccessFailedCount" AS "FailedLoginAttempts",
    u."LockoutEnd" AS "LockedOutUntil",
    u."LastLoginAt",
    u."LastPasswordChangeAt"
FROM auth."Users" u;

COMMIT;

-- =====================================================
-- Post-migration tasks (run these after verifying the migration)
-- =====================================================

-- 1. Update application configuration to use WitchCityRopeIdentityDbContext
-- 2. Update all repositories and services to use Identity APIs
-- 3. Test authentication and authorization thoroughly
-- 4. Once verified, the old tables can be archived or dropped:
--    - DROP TABLE public."Users" CASCADE;
--    - DROP TABLE public."UserAuthentications" CASCADE;
-- 5. Remove the backward compatibility view if no longer needed