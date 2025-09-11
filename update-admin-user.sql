-- Update admin user with Identity fields (without password hash - we'll use API to set that)
UPDATE public."Users" 
SET 
  "UserName" = 'admin@witchcityrope.com',
  "NormalizedUserName" = UPPER('admin@witchcityrope.com'),
  "NormalizedEmail" = UPPER('admin@witchcityrope.com'),
  "EmailConfirmed" = true,
  "SecurityStamp" = gen_random_uuid()::text,
  "ConcurrencyStamp" = gen_random_uuid()::text,
  "LockoutEnabled" = true,
  "AccessFailedCount" = 0,
  "TwoFactorEnabled" = false,
  "PhoneNumberConfirmed" = false
WHERE "Email" = 'admin@witchcityrope.com';

-- Also update other test users
UPDATE public."Users" 
SET 
  "UserName" = "Email",
  "NormalizedUserName" = UPPER("Email"),
  "NormalizedEmail" = UPPER("Email"),
  "EmailConfirmed" = true,
  "SecurityStamp" = gen_random_uuid()::text,
  "ConcurrencyStamp" = gen_random_uuid()::text,
  "LockoutEnabled" = true,
  "AccessFailedCount" = 0,
  "TwoFactorEnabled" = false,
  "PhoneNumberConfirmed" = false
WHERE "UserName" IS NULL;