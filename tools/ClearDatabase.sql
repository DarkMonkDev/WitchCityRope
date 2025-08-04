-- Script to clear all data from the database (for testing seeding)
-- WARNING: This will delete all data!

-- Disable foreign key checks
SET session_replication_role = 'replica';

-- Delete all data from tables in reverse dependency order
DELETE FROM "IncidentActions";
DELETE FROM "IncidentReviews";
DELETE FROM "IncidentReports";
DELETE FROM "Payments";
DELETE FROM "Registrations";
DELETE FROM "VettingApplications";
DELETE FROM "Events";
DELETE FROM "AspNetUserTokens";
DELETE FROM "AspNetUserLogins";
DELETE FROM "AspNetUserClaims";
DELETE FROM "AspNetRoleClaims";
DELETE FROM "AspNetUserRoles";
DELETE FROM "AspNetUsers";
DELETE FROM "AspNetRoles";

-- Re-enable foreign key checks
SET session_replication_role = 'origin';

-- Show row counts after clearing
SELECT 'Users' as table_name, COUNT(*) as row_count FROM "AspNetUsers"
UNION ALL
SELECT 'Roles', COUNT(*) FROM "AspNetRoles"
UNION ALL
SELECT 'Events', COUNT(*) FROM "Events"
UNION ALL
SELECT 'Registrations', COUNT(*) FROM "Registrations"
UNION ALL
SELECT 'VettingApplications', COUNT(*) FROM "VettingApplications";