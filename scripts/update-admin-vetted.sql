-- Update admin user to be vetted in the current database
-- This ensures admin@witchcityrope.com is marked as a vetted member

UPDATE auth."Users"
SET "IsVetted" = true
WHERE "Email" = 'admin@witchcityrope.com';

-- Verify the update
SELECT "Id", "Email", "SceneName", "IsVetted", "UserName"
FROM auth."Users"
WHERE "Email" = 'admin@witchcityrope.com';