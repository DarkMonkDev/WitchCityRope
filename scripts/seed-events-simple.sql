-- Delete test event
DELETE FROM "Events" WHERE "Title" = 'Test Event';

-- Add real events
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") VALUES
('e1111111-1111-1111-1111-111111111111', 'Introduction to Rope Safety', 'Perfect for beginners! Learn the fundamentals of rope safety, basic knots, and communication techniques in a supportive environment.', NOW() + INTERVAL '5 days', NOW() + INTERVAL '5 days' + INTERVAL '2 hours', 30, 'Workshop', 'The Rope Space - Main Room', true, NOW(), NOW(), '[{"Amount": 45.00, "Currency": "USD"}, {"Amount": 35.00, "Currency": "USD"}, {"Amount": 25.00, "Currency": "USD"}]');

INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") VALUES
('e2222222-2222-2222-2222-222222222222', 'March Rope Jam', 'Monthly practice space for vetted members. Bring your rope and practice partners for a social evening of rope bondage in a safe, monitored environment.', NOW() + INTERVAL '8 days', NOW() + INTERVAL '8 days' + INTERVAL '3 hours', 60, 'Social', 'The Rope Space - All Rooms', true, NOW(), NOW(), '[{"Amount": 15.00, "Currency": "USD"}, {"Amount": 10.00, "Currency": "USD"}]');

INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") VALUES
('e3333333-3333-3333-3333-333333333333', 'Suspension Intensive Workshop', 'Take your skills to new heights! This intensive workshop covers suspension basics, safety protocols, and hands-on practice with experienced instructors.', NOW() + INTERVAL '12 days', NOW() + INTERVAL '12 days' + INTERVAL '5 hours', 20, 'Workshop', 'The Rope Space - Main Room', true, NOW(), NOW(), '[{"Amount": 95.00, "Currency": "USD"}, {"Amount": 85.00, "Currency": "USD"}]');

INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") VALUES
('e4444444-4444-4444-4444-444444444444', 'Rope Performance Night', 'An evening of rope art and performance. Watch skilled riggers and models showcase their artistry. Open to all skill levels to watch and learn.', NOW() + INTERVAL '20 days', NOW() + INTERVAL '20 days' + INTERVAL '3 hours', 100, 'Performance', 'Salem Theater - Main Stage', true, NOW(), NOW(), '[{"Amount": 25.00, "Currency": "USD"}, {"Amount": 35.00, "Currency": "USD"}]');

INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") VALUES
('e5555555-5555-5555-5555-555555555555', 'Virtual Rope Workshop: Self-Tying', 'Learn the art of self-tying from the comfort of your home. This online workshop covers basic self-bondage techniques and safety.', NOW() + INTERVAL '30 days', NOW() + INTERVAL '30 days' + INTERVAL '2 hours', 100, 'Virtual', 'Online - Zoom', true, NOW(), NOW(), '[{"Amount": 25.00, "Currency": "USD"}, {"Amount": 15.00, "Currency": "USD"}]');

INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") VALUES
('e6666666-6666-6666-6666-666666666666', 'Rope Fundamentals: Floor Work', 'Master the basics of floor-based rope bondage. Perfect for beginners or those wanting to refine their fundamental skills.', NOW() + INTERVAL '18 days', NOW() + INTERVAL '18 days' + INTERVAL '2 hours', 30, 'Workshop', 'The Rope Space - Main Room', true, NOW(), NOW(), '[{"Amount": 45.00, "Currency": "USD"}, {"Amount": 35.00, "Currency": "USD"}]');

INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") VALUES
('e7777777-7777-7777-7777-777777777777', 'Monthly Rope Social', 'Casual social gathering for all skill levels. Practice, learn from others, or just hang out with the rope community. Light refreshments provided.', NOW() + INTERVAL '25 days', NOW() + INTERVAL '25 days' + INTERVAL '3 hours', 80, 'Social', 'The Rope Space - All Rooms', true, NOW(), NOW(), '[{"Amount": 0.00, "Currency": "USD"}]');

INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "EndDate", "Capacity", "EventType", "Location", "IsPublished", "CreatedAt", "UpdatedAt", "PricingTiers") VALUES
('e8888888-8888-8888-8888-888888888888', 'Rope Play Party', 'Monthly play party for vetted members. Dungeon monitors on duty. BYOB and snacks to share. Must be 21+.', NOW() + INTERVAL '28 days', NOW() + INTERVAL '28 days' + INTERVAL '6 hours', 50, 'PlayParty', 'Private Venue (address provided after RSVP)', true, NOW(), NOW(), '[{"Amount": 25.00, "Currency": "USD"}, {"Amount": 15.00, "Currency": "USD"}]');

-- Add event organizers (use the existing user ID)
INSERT INTO "EventOrganizers" ("EventId", "UserId", "Role", "CreatedAt")
SELECT 
    e."Id",
    (SELECT "Id" FROM "Users" LIMIT 1),
    'Primary',
    NOW()
FROM "Events" e
WHERE NOT EXISTS (SELECT 1 FROM "EventOrganizers" WHERE "EventId" = e."Id");