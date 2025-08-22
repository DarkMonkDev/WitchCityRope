-- WitchCityRope ASP.NET Core Identity Tables
-- Manual SQL script for creating Identity schema when EF migrations are not available
-- This creates the basic structure for ASP.NET Core Identity with PostgreSQL

-- Create auth schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS auth;

-- Create AspNetRoles table
CREATE TABLE IF NOT EXISTS auth."Roles" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" TEXT
);

-- Create AspNetUsers table with WitchCityRope custom fields
CREATE TABLE IF NOT EXISTS auth."Users" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserName" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMPTZ,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0,
    
    -- WitchCityRope custom fields
    "SceneName" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastLoginAt" TIMESTAMPTZ,
    "EncryptedLegalName" VARCHAR(500) NOT NULL DEFAULT '',
    "DateOfBirth" TIMESTAMPTZ NOT NULL DEFAULT '1900-01-01',
    "Role" TEXT NOT NULL DEFAULT 'Guest',
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "PronouncedName" VARCHAR(100) NOT NULL DEFAULT '',
    "Pronouns" VARCHAR(50) NOT NULL DEFAULT '',
    "IsVetted" BOOLEAN NOT NULL DEFAULT FALSE,
    "FailedLoginAttempts" INTEGER NOT NULL DEFAULT 0,
    "LockedOutUntil" TIMESTAMPTZ,
    "LastPasswordChangeAt" TIMESTAMPTZ,
    "EmailVerificationToken" TEXT NOT NULL DEFAULT '',
    "EmailVerificationTokenCreatedAt" TIMESTAMPTZ,
    "VettingStatus" INTEGER NOT NULL DEFAULT 0
);

-- Create AspNetUserRoles table
CREATE TABLE IF NOT EXISTS auth."UserRoles" (
    "UserId" UUID NOT NULL,
    "RoleId" UUID NOT NULL,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("UserId") REFERENCES auth."Users"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("RoleId") REFERENCES auth."Roles"("Id") ON DELETE CASCADE
);

-- Create AspNetUserClaims table
CREATE TABLE IF NOT EXISTS auth."UserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    FOREIGN KEY ("UserId") REFERENCES auth."Users"("Id") ON DELETE CASCADE
);

-- Create AspNetUserLogins table
CREATE TABLE IF NOT EXISTS auth."UserLogins" (
    "LoginProvider" VARCHAR(450) NOT NULL,
    "ProviderKey" VARCHAR(450) NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" UUID NOT NULL,
    PRIMARY KEY ("LoginProvider", "ProviderKey"),
    FOREIGN KEY ("UserId") REFERENCES auth."Users"("Id") ON DELETE CASCADE
);

-- Create AspNetUserTokens table
CREATE TABLE IF NOT EXISTS auth."UserTokens" (
    "UserId" UUID NOT NULL,
    "LoginProvider" VARCHAR(450) NOT NULL,
    "Name" VARCHAR(450) NOT NULL,
    "Value" TEXT,
    PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    FOREIGN KEY ("UserId") REFERENCES auth."Users"("Id") ON DELETE CASCADE
);

-- Create AspNetRoleClaims table
CREATE TABLE IF NOT EXISTS auth."RoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" UUID NOT NULL,
    "ClaimType" TEXT,
    "ClaimValue" TEXT,
    FOREIGN KEY ("RoleId") REFERENCES auth."Roles"("Id") ON DELETE CASCADE
);

-- Create Events table in public schema
CREATE TABLE IF NOT EXISTS public."Events" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title" VARCHAR(255) NOT NULL,
    "Description" TEXT NOT NULL,
    "StartDate" TIMESTAMPTZ NOT NULL,
    "EndDate" TIMESTAMPTZ NOT NULL,
    "Capacity" INTEGER NOT NULL,
    "EventType" TEXT NOT NULL,
    "Location" VARCHAR(255) NOT NULL,
    "IsPublished" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "PricingTiers" TEXT NOT NULL DEFAULT '{}'
);

-- Create indexes for performance

-- User indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_SceneName" ON auth."Users" ("SceneName");
CREATE INDEX IF NOT EXISTS "IX_Users_IsActive" ON auth."Users" ("IsActive");
CREATE INDEX IF NOT EXISTS "IX_Users_IsVetted" ON auth."Users" ("IsVetted");
CREATE INDEX IF NOT EXISTS "IX_Users_Role" ON auth."Users" ("Role");
CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON auth."Users" ("NormalizedUserName");
CREATE INDEX IF NOT EXISTS "EmailIndex" ON auth."Users" ("NormalizedEmail");

-- Role indexes
CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON auth."Roles" ("NormalizedName");

-- Event indexes
CREATE INDEX IF NOT EXISTS "IX_Events_StartDate" ON public."Events" ("StartDate");
CREATE INDEX IF NOT EXISTS "IX_Events_IsPublished" ON public."Events" ("IsPublished");
CREATE INDEX IF NOT EXISTS "IX_Events_EventType" ON public."Events" ("EventType");

-- User claim indexes
CREATE INDEX IF NOT EXISTS "IX_UserClaims_UserId" ON auth."UserClaims" ("UserId");

-- User login indexes
CREATE INDEX IF NOT EXISTS "IX_UserLogins_UserId" ON auth."UserLogins" ("UserId");

-- User token indexes
CREATE INDEX IF NOT EXISTS "IX_UserTokens_UserId" ON auth."UserTokens" ("UserId");

-- Role claim indexes
CREATE INDEX IF NOT EXISTS "IX_RoleClaims_RoleId" ON auth."RoleClaims" ("RoleId");

-- Create default roles for WitchCityRope
INSERT INTO auth."Roles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES 
    (gen_random_uuid(), 'Admin', 'ADMIN', gen_random_uuid()::text),
    (gen_random_uuid(), 'Teacher', 'TEACHER', gen_random_uuid()::text),
    (gen_random_uuid(), 'VettedMember', 'VETTEDMEMBER', gen_random_uuid()::text),
    (gen_random_uuid(), 'Member', 'MEMBER', gen_random_uuid()::text),
    (gen_random_uuid(), 'Guest', 'GUEST', gen_random_uuid()::text)
ON CONFLICT ("Id") DO NOTHING;

-- Create migration history table for EF Core compatibility
CREATE TABLE IF NOT EXISTS public."__EFMigrationsHistory" (
    "MigrationId" VARCHAR(150) NOT NULL,
    "ProductVersion" VARCHAR(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Insert initial migration record
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('Manual_Identity_Schema_Creation', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Add constraints for data integrity
ALTER TABLE auth."Users" 
ADD CONSTRAINT IF NOT EXISTS "CK_Users_Email_Format" 
CHECK ("Email" ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$');

ALTER TABLE auth."Users" 
ADD CONSTRAINT IF NOT EXISTS "CK_Users_SceneName_Length" 
CHECK (LENGTH("SceneName") >= 2);

-- Grant permissions
GRANT USAGE ON SCHEMA auth TO postgres;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA auth TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA auth TO postgres;

GRANT USAGE ON SCHEMA public TO postgres;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO postgres;

-- Display completion message
SELECT 'ASP.NET Core Identity tables created successfully' AS status;