-- Check if the event exists in the database
SELECT 
    "Id",
    "Title",
    "IsPublished",
    "StartDate",
    "CreatedAt"
FROM "Events"
WHERE "Id" = '79272089-a274-45a4-ae08-a31f5edb0965';

-- List all events to see what's actually in the database
SELECT 
    "Id",
    "Title",
    "IsPublished",
    "StartDate"
FROM "Events"
ORDER BY "CreatedAt" DESC
LIMIT 10;

-- Check database name
SELECT current_database();