-- Force seed database with all required data
-- This script will add missing users and all events

BEGIN;

-- First check what admin user exists
DO $$
DECLARE
    admin_id UUID;
    admin_email TEXT;
BEGIN
    -- Get the existing admin user
    SELECT "Id", "Email" INTO admin_id, admin_email FROM "Users" WHERE "Email" LIKE '%admin%' LIMIT 1;
    
    IF admin_id IS NOT NULL THEN
        RAISE NOTICE 'Found existing admin user: % with ID: %', admin_email, admin_id;
        
        -- Make sure this user has authentication
        IF NOT EXISTS (SELECT 1 FROM "UserAuthentications" WHERE "UserId" = admin_id) THEN
            INSERT INTO "UserAuthentications" (
                "Id", "UserId", "PasswordHash", "TwoFactorSecret", "IsTwoFactorEnabled", 
                "LastPasswordChangeAt", "FailedLoginAttempts", "LockedOutUntil", "CreatedAt", "UpdatedAt"
            ) VALUES (
                gen_random_uuid(), admin_id, '$2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu', 
                NULL, false, NOW(), 0, NULL, NOW(), NOW()
            );
            RAISE NOTICE 'Added authentication for admin user';
        END IF;
    END IF;
END $$;

-- Add missing users if they don't exist
INSERT INTO "Users" (
    "Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", 
    "IsActive", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "IsVetted"
)
SELECT * FROM (VALUES
    ('b2222222-2222-2222-2222-222222222222'::uuid, 'VGVhY2hlciBVc2Vy', 'TeacherRope', 'teacher@witchcityrope.com', '1985-05-15'::timestamp, 'Teacher', true, NOW(), NOW(), 'TeacherRope', 'she/her', true),
    ('c3333333-3333-3333-3333-333333333333'::uuid, 'VmV0dGVkIE1lbWJlcg==', 'VettedMember', 'vetted@witchcityrope.com', '1992-08-20'::timestamp, 'VettedMember', true, NOW(), NOW(), 'VettedMember', 'he/him', true),
    ('d4444444-4444-4444-4444-444444444444'::uuid, 'UmVndWxhciBNZW1iZXI=', 'RegularMember', 'member@witchcityrope.com', '1995-12-10'::timestamp, 'Member', true, NOW(), NOW(), 'RegularMember', 'she/they', false),
    ('e5555555-5555-5555-5555-555555555555'::uuid, 'T3JnYW5pemVyIFVzZXI=', 'EventOrganizer', 'organizer@witchcityrope.com', '1988-11-05'::timestamp, 'Moderator', true, NOW(), NOW(), 'EventOrganizer', 'they/them', true)
) AS new_users("Id", "EncryptedLegalName", "SceneName", "Email", "DateOfBirth", "Role", "IsActive", "CreatedAt", "UpdatedAt", "PronouncedName", "Pronouns", "IsVetted")
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Email" = new_users."Email");

-- Add authentication for all users that don't have it
INSERT INTO "UserAuthentications" (
    "Id", "UserId", "PasswordHash", "TwoFactorSecret", "IsTwoFactorEnabled", 
    "LastPasswordChangeAt", "FailedLoginAttempts", "LockedOutUntil", "CreatedAt", "UpdatedAt"
)
SELECT 
    gen_random_uuid(),
    u."Id",
    '$2a$11$FPxQ3mhOsiJ9KiWa4RG4Ye0w5P3G0YeZv2gvZ9.KzQq3KoJCS0oAu', -- Test123!
    NULL,
    false,
    NOW(),
    0,
    NULL,
    NOW(),
    NOW()
FROM "Users" u
WHERE NOT EXISTS (SELECT 1 FROM "UserAuthentications" WHERE "UserId" = u."Id");

-- Now add events
INSERT INTO "Events" (
    "Id", "Title", "Description", "StartDate", "EndDate", "Capacity", 
    "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers"
) VALUES
-- Workshop 1: Introduction to Rope Safety (5 days from now)
('e1111111-1111-1111-1111-111111111111'::uuid, 
 'Introduction to Rope Safety', 
 'Perfect for beginners! Learn the fundamentals of rope safety, basic knots, and communication techniques in a supportive environment.',
 NOW() + INTERVAL '5 days' + INTERVAL '14 hours',
 NOW() + INTERVAL '5 days' + INTERVAL '16 hours',
 30,
 'Workshop',
 'The Rope Space - Main Room',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 45.00, "Currency": "USD", "Description": "Standard"}, {"Amount": 35.00, "Currency": "USD", "Description": "Member"}, {"Amount": 25.00, "Currency": "USD", "Description": "Student"}]'),

-- Social Event: March Rope Jam (8 days from now)
('e2222222-2222-2222-2222-222222222222'::uuid,
 'March Rope Jam',
 'Monthly practice space for vetted members. Bring your rope and practice partners for a social evening of rope bondage in a safe, monitored environment.',
 NOW() + INTERVAL '8 days' + INTERVAL '19 hours',
 NOW() + INTERVAL '8 days' + INTERVAL '22 hours',
 60,
 'Social',
 'The Rope Space - All Rooms',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 15.00, "Currency": "USD", "Description": "Standard"}, {"Amount": 10.00, "Currency": "USD", "Description": "Member"}]'),

-- Workshop 2: Suspension Intensive (12 days from now)
('e3333333-3333-3333-3333-333333333333'::uuid,
 'Suspension Intensive Workshop',
 'Take your skills to new heights! This intensive workshop covers suspension basics, safety protocols, and hands-on practice with experienced instructors.',
 NOW() + INTERVAL '12 days' + INTERVAL '13 hours',
 NOW() + INTERVAL '12 days' + INTERVAL '18 hours',
 20,
 'Workshop',
 'The Rope Space - Main Room',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 95.00, "Currency": "USD", "Description": "Standard"}, {"Amount": 85.00, "Currency": "USD", "Description": "Member"}, {"Amount": 75.00, "Currency": "USD", "Description": "Early Bird"}]'),

-- Performance: Rope Performance Night (20 days from now)
('e4444444-4444-4444-4444-444444444444'::uuid,
 'Rope Performance Night',
 'An evening of rope art and performance. Watch skilled riggers and models showcase their artistry. Open to all skill levels to watch and learn.',
 NOW() + INTERVAL '20 days' + INTERVAL '20 hours',
 NOW() + INTERVAL '20 days' + INTERVAL '23 hours',
 100,
 'Performance',
 'Salem Theater - Main Stage',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 25.00, "Currency": "USD", "Description": "General Admission"}, {"Amount": 35.00, "Currency": "USD", "Description": "VIP Seating"}]'),

-- Virtual Workshop (30 days from now)
('e5555555-5555-5555-5555-555555555555'::uuid,
 'Virtual Rope Workshop: Self-Tying',
 'Learn the art of self-tying from the comfort of your home. This online workshop covers basic self-bondage techniques and safety.',
 NOW() + INTERVAL '30 days' + INTERVAL '19 hours',
 NOW() + INTERVAL '30 days' + INTERVAL '21 hours',
 100,
 'Virtual',
 'Online - Zoom',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 25.00, "Currency": "USD", "Description": "Standard"}, {"Amount": 15.00, "Currency": "USD", "Description": "Member"}]'),

-- Workshop 3: Rope Fundamentals (18 days from now)
('e6666666-6666-6666-6666-666666666666'::uuid,
 'Rope Fundamentals: Floor Work',
 'Master the basics of floor-based rope bondage. Perfect for beginners or those wanting to refine their fundamental skills.',
 NOW() + INTERVAL '18 days' + INTERVAL '14 hours',
 NOW() + INTERVAL '18 days' + INTERVAL '16 hours',
 30,
 'Workshop',
 'The Rope Space - Main Room',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 45.00, "Currency": "USD", "Description": "Standard"}, {"Amount": 35.00, "Currency": "USD", "Description": "Member"}]'),

-- Social Event: Monthly Rope Social (25 days from now)
('e7777777-7777-7777-7777-777777777777'::uuid,
 'Monthly Rope Social',
 'Casual social gathering for all skill levels. Practice, learn from others, or just hang out with the rope community. Light refreshments provided.',
 NOW() + INTERVAL '25 days' + INTERVAL '18 hours',
 NOW() + INTERVAL '25 days' + INTERVAL '21 hours',
 80,
 'Social',
 'The Rope Space - All Rooms',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 0.00, "Currency": "USD", "Description": "Free Event"}]'),

-- Play Party (28 days from now)
('e8888888-8888-8888-8888-888888888888'::uuid,
 'Rope Play Party',
 'Monthly play party for vetted members. Dungeon monitors on duty. BYOB and snacks to share. Must be 21+.',
 NOW() + INTERVAL '28 days' + INTERVAL '20 hours',
 NOW() + INTERVAL '29 days' + INTERVAL '2 hours',
 50,
 'PlayParty',
 'Private Venue (address provided after RSVP)',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 25.00, "Currency": "USD", "Description": "Standard"}, {"Amount": 15.00, "Currency": "USD", "Description": "Member"}]'),

-- Workshop 4: Rope and Sensation (15 days from now)
('e9999999-9999-9999-9999-999999999999'::uuid,
 'Rope and Sensation Play',
 'Explore the intersection of rope bondage and sensation play. Learn how to incorporate different sensations safely into your rope practice.',
 NOW() + INTERVAL '15 days' + INTERVAL '14 hours',
 NOW() + INTERVAL '15 days' + INTERVAL '17 hours',
 24,
 'Workshop',
 'The Rope Space - Lounge',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 55.00, "Currency": "USD", "Description": "Standard"}, {"Amount": 40.00, "Currency": "USD", "Description": "Member"}]'),

-- Conference (45 days from now)
('e0000000-0000-0000-0000-000000000000'::uuid,
 'New England Rope Intensive',
 '3-day intensive rope bondage conference featuring workshops, performances, and vendor market. Guest instructors from around the world.',
 NOW() + INTERVAL '45 days' + INTERVAL '17 hours',
 NOW() + INTERVAL '48 days' + INTERVAL '14 hours',
 200,
 'Conference',
 'Salem Convention Center',
 true,
 NOW(),
 NOW(),
 '[{"Amount": 250.00, "Currency": "USD", "Description": "Full Conference"}, {"Amount": 200.00, "Currency": "USD", "Description": "Early Bird"}, {"Amount": 100.00, "Currency": "USD", "Description": "Single Day"}]');

-- Add event organizers
DO $$
DECLARE
    admin_id UUID;
    teacher_id UUID;
    organizer_id UUID;
BEGIN
    -- Get user IDs
    SELECT "Id" INTO admin_id FROM "Users" WHERE "Email" = 'admin@witchcityrope.com' LIMIT 1;
    SELECT "Id" INTO teacher_id FROM "Users" WHERE "Email" = 'teacher@witchcityrope.com' LIMIT 1;
    SELECT "Id" INTO organizer_id FROM "Users" WHERE "Email" = 'organizer@witchcityrope.com' LIMIT 1;
    
    -- Use the first available user if specific ones don't exist
    IF admin_id IS NULL THEN
        SELECT "Id" INTO admin_id FROM "Users" WHERE "Role" IN ('Admin', 'Administrator') LIMIT 1;
    END IF;
    
    IF admin_id IS NULL THEN
        SELECT "Id" INTO admin_id FROM "Users" LIMIT 1;
    END IF;
    
    -- If we still don't have IDs, use the admin for all
    IF teacher_id IS NULL THEN teacher_id := admin_id; END IF;
    IF organizer_id IS NULL THEN organizer_id := admin_id; END IF;
    
    -- Insert event organizers
    INSERT INTO "EventOrganizers" ("EventId", "UserId", "Role", "CreatedAt") VALUES
    ('e1111111-1111-1111-1111-111111111111', admin_id, 'Primary', NOW()),
    ('e2222222-2222-2222-2222-222222222222', teacher_id, 'Primary', NOW()),
    ('e3333333-3333-3333-3333-333333333333', teacher_id, 'Primary', NOW()),
    ('e4444444-4444-4444-4444-444444444444', organizer_id, 'Primary', NOW()),
    ('e5555555-5555-5555-5555-555555555555', admin_id, 'Primary', NOW()),
    ('e6666666-6666-6666-6666-666666666666', teacher_id, 'Primary', NOW()),
    ('e7777777-7777-7777-7777-777777777777', admin_id, 'Primary', NOW()),
    ('e8888888-8888-8888-8888-888888888888', organizer_id, 'Primary', NOW()),
    ('e9999999-9999-9999-9999-999999999999', teacher_id, 'Primary', NOW()),
    ('e0000000-0000-0000-0000-000000000000', admin_id, 'Primary', NOW());
END $$;

COMMIT;

-- Verify the results
SELECT 'Data seeding complete!' as status;
SELECT 'Users:' as table_name, COUNT(*) as count FROM "Users"
UNION ALL
SELECT 'Events:', COUNT(*) FROM "Events"
UNION ALL
SELECT 'EventOrganizers:', COUNT(*) FROM "EventOrganizers"
UNION ALL
SELECT 'UserAuthentications:', COUNT(*) FROM "UserAuthentications";

-- Show sample events
SELECT "Title", "EventType", "StartDate", "Location" 
FROM "Events" 
ORDER BY "StartDate" 
LIMIT 5;