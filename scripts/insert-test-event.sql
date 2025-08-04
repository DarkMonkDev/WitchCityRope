-- Insert the specific test event if it doesn't exist
INSERT INTO "Events" (
    "Id",
    "Title",
    "Description",
    "StartDate",
    "EndDate",
    "Capacity",
    "EventType",
    "Location",
    "PricingTiers",
    "IsPublished",
    "CreatedAt",
    "UpdatedAt",
    "CreatedBy",
    "UpdatedBy",
    "OrganizerIds"
) VALUES (
    '79272089-a274-45a4-ae08-a31f5edb0965'::uuid,
    'Test Event for Browser Testing',
    'This is a test event created specifically for browser automation testing.',
    NOW() + INTERVAL '7 days',
    NOW() + INTERVAL '7 days 2 hours',
    20,
    0, -- Workshop type
    'Test Location, Salem, MA',
    '[{"Amount": 35, "Currency": "USD"}]'::jsonb,
    true,
    NOW(),
    NOW(),
    'SYSTEM',
    'SYSTEM',
    '[]'::jsonb
) ON CONFLICT ("Id") DO UPDATE SET
    "Title" = EXCLUDED."Title",
    "IsPublished" = true,
    "UpdatedAt" = NOW();

-- Verify the event was inserted/updated
SELECT 
    "Id",
    "Title",
    "IsPublished",
    "StartDate",
    "Location"
FROM "Events"
WHERE "Id" = '79272089-a274-45a4-ae08-a31f5edb0965';