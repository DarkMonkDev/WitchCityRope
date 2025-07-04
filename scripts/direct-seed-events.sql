-- Direct event seeding with error checking
-- Add events one by one

-- Event 1: Introduction to Rope Safety
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") 
VALUES (
    'e1111111-1111-1111-1111-111111111111'::uuid, 
    'Introduction to Rope Safety', 
    'Perfect for beginners! Learn the fundamentals of rope safety.',
    NOW() + INTERVAL '5 days',
    NOW() + INTERVAL '5 days' + INTERVAL '2 hours',
    30,
    'Workshop',
    'The Rope Space - Main Room',
    true,
    NOW(),
    NOW(),
    '[{"Amount": 45.00, "Currency": "USD"}]'
)
ON CONFLICT ("Id") DO NOTHING;

-- Event 3: Suspension Intensive
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") 
VALUES (
    'e3333333-3333-3333-3333-333333333333'::uuid,
    'Suspension Intensive Workshop',
    'Take your skills to new heights!',
    NOW() + INTERVAL '12 days',
    NOW() + INTERVAL '12 days' + INTERVAL '5 hours',
    20,
    'Workshop',
    'The Rope Space - Main Room',
    true,
    NOW(),
    NOW(),
    '[{"Amount": 95.00, "Currency": "USD"}]'
)
ON CONFLICT ("Id") DO NOTHING;

-- Event 4: Performance Night
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") 
VALUES (
    'e4444444-4444-4444-4444-444444444444'::uuid,
    'Rope Performance Night',
    'An evening of rope art and performance.',
    NOW() + INTERVAL '20 days',
    NOW() + INTERVAL '20 days' + INTERVAL '3 hours',
    100,
    'Performance',
    'Salem Theater - Main Stage',
    true,
    NOW(),
    NOW(),
    '[{"Amount": 25.00, "Currency": "USD"}]'
)
ON CONFLICT ("Id") DO NOTHING;

-- Event 5: Virtual Workshop
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") 
VALUES (
    'e5555555-5555-5555-5555-555555555555'::uuid,
    'Virtual Rope Workshop: Self-Tying',
    'Learn self-tying from home.',
    NOW() + INTERVAL '30 days',
    NOW() + INTERVAL '30 days' + INTERVAL '2 hours',
    100,
    'Virtual',
    'Online - Zoom',
    true,
    NOW(),
    NOW(),
    '[{"Amount": 25.00, "Currency": "USD"}]'
)
ON CONFLICT ("Id") DO NOTHING;

-- Event organizers
INSERT INTO "EventOrganizers" ("EventId", "UserId", "Role", "CreatedAt")
SELECT 
    e."Id",
    (SELECT "Id" FROM "Users" LIMIT 1),
    'Primary',
    NOW()
FROM "Events" e
WHERE NOT EXISTS (SELECT 1 FROM "EventOrganizers" WHERE "EventId" = e."Id");

-- Show results
SELECT COUNT(*) as total_events FROM "Events";
SELECT "Title", "EventType", "StartDate" FROM "Events" ORDER BY "StartDate";