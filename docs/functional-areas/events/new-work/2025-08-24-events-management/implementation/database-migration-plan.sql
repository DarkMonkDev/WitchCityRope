-- =====================================================================
-- WitchCityRope Event Session Matrix Migration Plan
-- =====================================================================
-- Date: 2025-08-25
-- Description: Migration script for implementing the Event Session Matrix
-- Architecture: Migrates from simple Event->Registration model to 
--               Event->Sessions->TicketTypes->Orders model
-- 
-- CRITICAL PATTERNS APPLIED:
-- - All DateTime fields use 'timestamptz' (PostgreSQL UTC requirement)
-- - All GUIDs use 'uuid' type with gen_random_uuid() defaults
-- - Proper foreign key constraints with CASCADE/RESTRICT as appropriate
-- - Named constraints for PostgreSQL compatibility
-- - Proper indexing for performance
-- - Transaction boundaries for safety
-- =====================================================================

BEGIN;

-- =====================================================================
-- PHASE 1: CREATE NEW TABLES
-- =====================================================================

-- NEW: EventSessions table - atomic capacity units
CREATE TABLE "EventSessions" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL,
    "SessionIdentifier" varchar(10) NOT NULL, -- S1, S2, S3, etc.
    "Name" varchar(100) NOT NULL, -- "Day 1 Workshop", "Saturday Session"
    "Date" date NOT NULL,
    "StartTime" time NOT NULL,
    "EndTime" time NOT NULL,
    "Capacity" integer NOT NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    
    -- Foreign key constraints
    CONSTRAINT "FK_EventSessions_Events" FOREIGN KEY ("EventId") 
        REFERENCES "Events"("Id") ON DELETE CASCADE,
    
    -- Business rule constraints
    CONSTRAINT "CHK_EventSessions_Capacity" CHECK ("Capacity" > 0),
    CONSTRAINT "CHK_EventSessions_Times" CHECK ("StartTime" < "EndTime"),
    
    -- Unique constraints
    CONSTRAINT "UQ_EventSessions_EventId_SessionIdentifier" 
        UNIQUE ("EventId", "SessionIdentifier"),
    CONSTRAINT "UQ_EventSessions_EventId_Name" 
        UNIQUE ("EventId", "Name")
);

-- NEW: EventTicketTypes table - session bundles with pricing
CREATE TABLE "EventTicketTypes" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "EventId" uuid NOT NULL,
    "Name" varchar(100) NOT NULL, -- "Full Series Pass", "Friday Only"
    "Type" varchar(20) NOT NULL, -- "Single" or "Couples"
    "MinPrice" decimal(10,2) NOT NULL, -- Sliding scale minimum
    "MaxPrice" decimal(10,2) NOT NULL, -- Sliding scale maximum
    "QuantityAvailable" integer, -- NULL = unlimited
    "SalesEndDate" timestamptz, -- When ticket sales end
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "SortOrder" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    
    -- Foreign key constraints
    CONSTRAINT "FK_EventTicketTypes_Events" FOREIGN KEY ("EventId") 
        REFERENCES "Events"("Id") ON DELETE CASCADE,
    
    -- Business rule constraints
    CONSTRAINT "CHK_EventTicketTypes_MinPrice" CHECK ("MinPrice" >= 0),
    CONSTRAINT "CHK_EventTicketTypes_MaxPrice" CHECK ("MaxPrice" >= "MinPrice"),
    CONSTRAINT "CHK_EventTicketTypes_Quantity" CHECK ("QuantityAvailable" > 0 OR "QuantityAvailable" IS NULL),
    CONSTRAINT "CHK_EventTicketTypes_Type" CHECK ("Type" IN ('Single', 'Couples')),
    
    -- Unique constraints
    CONSTRAINT "UQ_EventTicketTypes_EventId_Name" 
        UNIQUE ("EventId", "Name")
);

-- NEW: EventTicketTypeSessions junction table - which sessions each ticket includes
CREATE TABLE "EventTicketTypeSessions" (
    "TicketTypeId" uuid NOT NULL,
    "SessionId" uuid NOT NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    
    -- Composite primary key
    PRIMARY KEY ("TicketTypeId", "SessionId"),
    
    -- Foreign key constraints
    CONSTRAINT "FK_EventTicketTypeSessions_TicketType" 
        FOREIGN KEY ("TicketTypeId") REFERENCES "EventTicketTypes"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_EventTicketTypeSessions_Session" 
        FOREIGN KEY ("SessionId") REFERENCES "EventSessions"("Id") ON DELETE CASCADE
);

-- NEW: Orders table - replaces direct Registration->Event relationship
CREATE TABLE "Orders" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "EventId" uuid NOT NULL,
    "OrderNumber" varchar(50) NOT NULL,
    "Status" varchar(20) NOT NULL DEFAULT 'Pending', -- Pending, Confirmed, Cancelled
    "TotalAmount" decimal(10,2) NOT NULL,
    "PaymentIntentId" varchar(100),
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamptz NOT NULL DEFAULT NOW(),
    
    -- Foreign key constraints
    CONSTRAINT "FK_Orders_Events" FOREIGN KEY ("EventId") 
        REFERENCES "Events"("Id") ON DELETE RESTRICT,
    -- Note: UserId references will be added after User table analysis
    
    -- Business rule constraints
    CONSTRAINT "CHK_Orders_Status" CHECK ("Status" IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT "CHK_Orders_TotalAmount" CHECK ("TotalAmount" >= 0),
    
    -- Unique constraints
    CONSTRAINT "UQ_Orders_OrderNumber" UNIQUE ("OrderNumber")
);

-- NEW: OrderItems table - individual ticket purchases within an order
CREATE TABLE "OrderItems" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "OrderId" uuid NOT NULL,
    "TicketTypeId" uuid NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" decimal(10,2) NOT NULL, -- Actual price paid (from sliding scale)
    "TotalPrice" decimal(10,2) NOT NULL,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    
    -- Foreign key constraints
    CONSTRAINT "FK_OrderItems_Orders" FOREIGN KEY ("OrderId") 
        REFERENCES "Orders"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_OrderItems_TicketTypes" FOREIGN KEY ("TicketTypeId") 
        REFERENCES "EventTicketTypes"("Id") ON DELETE RESTRICT,
    
    -- Business rule constraints
    CONSTRAINT "CHK_OrderItems_Quantity" CHECK ("Quantity" > 0),
    CONSTRAINT "CHK_OrderItems_UnitPrice" CHECK ("UnitPrice" >= 0),
    CONSTRAINT "CHK_OrderItems_TotalPrice" CHECK ("TotalPrice" = "UnitPrice" * "Quantity")
);

-- NEW: SessionAttendances table - tracks who's attending which sessions
CREATE TABLE "SessionAttendances" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "SessionId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "OrderItemId" uuid NOT NULL,
    "IsCheckedIn" boolean NOT NULL DEFAULT FALSE,
    "CheckInTime" timestamptz,
    "CreatedAt" timestamptz NOT NULL DEFAULT NOW(),
    
    -- Foreign key constraints
    CONSTRAINT "FK_SessionAttendances_Sessions" FOREIGN KEY ("SessionId") 
        REFERENCES "EventSessions"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_SessionAttendances_OrderItems" FOREIGN KEY ("OrderItemId") 
        REFERENCES "OrderItems"("Id") ON DELETE CASCADE,
    -- Note: UserId references will be added after User table analysis
    
    -- Unique constraints - one attendance record per user per session
    CONSTRAINT "UQ_SessionAttendances_SessionId_UserId" 
        UNIQUE ("SessionId", "UserId")
);

-- =====================================================================
-- PHASE 2: CREATE PERFORMANCE INDEXES
-- =====================================================================

-- EventSessions indexes
CREATE INDEX "IX_EventSessions_EventId" ON "EventSessions"("EventId");
CREATE INDEX "IX_EventSessions_Date" ON "EventSessions"("Date");
CREATE INDEX "IX_EventSessions_EventId_Date" ON "EventSessions"("EventId", "Date");

-- EventTicketTypes indexes
CREATE INDEX "IX_EventTicketTypes_EventId" ON "EventTicketTypes"("EventId");
CREATE INDEX "IX_EventTicketTypes_IsActive" ON "EventTicketTypes"("IsActive");
CREATE INDEX "IX_EventTicketTypes_SalesEndDate" ON "EventTicketTypes"("SalesEndDate");
CREATE INDEX "IX_EventTicketTypes_EventId_IsActive" ON "EventTicketTypes"("EventId", "IsActive");

-- EventTicketTypeSessions indexes
CREATE INDEX "IX_EventTicketTypeSessions_SessionId" ON "EventTicketTypeSessions"("SessionId");

-- Orders indexes
CREATE INDEX "IX_Orders_UserId" ON "Orders"("UserId");
CREATE INDEX "IX_Orders_EventId" ON "Orders"("EventId");
CREATE INDEX "IX_Orders_Status" ON "Orders"("Status");
CREATE INDEX "IX_Orders_CreatedAt" ON "Orders"("CreatedAt");
CREATE INDEX "IX_Orders_UserId_Status" ON "Orders"("UserId", "Status");

-- OrderItems indexes
CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems"("OrderId");
CREATE INDEX "IX_OrderItems_TicketTypeId" ON "OrderItems"("TicketTypeId");

-- SessionAttendances indexes
CREATE INDEX "IX_SessionAttendances_SessionId" ON "SessionAttendances"("SessionId");
CREATE INDEX "IX_SessionAttendances_UserId" ON "SessionAttendances"("UserId");
CREATE INDEX "IX_SessionAttendances_OrderItemId" ON "SessionAttendances"("OrderItemId");
CREATE INDEX "IX_SessionAttendances_IsCheckedIn" ON "SessionAttendances"("IsCheckedIn");

-- =====================================================================
-- PHASE 3: EVENTS TABLE MODIFICATIONS
-- =====================================================================

-- Add new fields to Events table for enhanced functionality
ALTER TABLE "Events" 
ADD COLUMN "RequiresVetting" boolean NOT NULL DEFAULT FALSE,
ADD COLUMN "RegistrationOpenDate" timestamptz,
ADD COLUMN "RegistrationCloseDate" timestamptz,
ADD COLUMN "MaxAttendees" integer, -- Overall event capacity limit
ADD COLUMN "AllowWaitlist" boolean NOT NULL DEFAULT FALSE;

-- Add constraints for new fields
ALTER TABLE "Events" 
ADD CONSTRAINT "CHK_Events_MaxAttendees" CHECK ("MaxAttendees" > 0 OR "MaxAttendees" IS NULL);

-- Create indexes for new fields
CREATE INDEX "IX_Events_RequiresVetting" ON "Events"("RequiresVetting");
CREATE INDEX "IX_Events_RegistrationOpenDate" ON "Events"("RegistrationOpenDate");
CREATE INDEX "IX_Events_RegistrationCloseDate" ON "Events"("RegistrationCloseDate");

-- =====================================================================
-- PHASE 4: DATA MIGRATION FROM EXISTING REGISTRATIONS
-- =====================================================================

-- Migration strategy for existing events:
-- 1. Create default sessions for existing events
-- 2. Create default ticket types 
-- 3. Migrate existing registrations to new order system
-- 4. Preserve all existing registration data

-- Step 4.1: Create default sessions for existing published events
INSERT INTO "EventSessions" (
    "EventId", 
    "SessionIdentifier", 
    "Name", 
    "Date", 
    "StartTime", 
    "EndTime", 
    "Capacity"
)
SELECT 
    e."Id",
    'S1',
    e."Title", -- Use event title as session name
    e."StartDate"::date,
    e."StartDate"::time,
    e."EndDate"::time,
    e."Capacity"
FROM "Events" e
WHERE e."IsPublished" = TRUE;

-- Step 4.2: Create default ticket types for migrated sessions
-- Single person tickets with sliding scale from existing PricingTiers
INSERT INTO "EventTicketTypes" (
    "EventId",
    "Name",
    "Type", 
    "MinPrice",
    "MaxPrice",
    "QuantityAvailable"
)
SELECT 
    e."Id",
    'General Admission',
    'Single',
    -- Extract min/max from existing PricingTiers JSON
    -- This is a simplified version - actual implementation would parse JSON
    0.00, -- MinPrice - would be parsed from PricingTiers
    50.00, -- MaxPrice - would be parsed from PricingTiers
    e."Capacity"
FROM "Events" e
WHERE e."IsPublished" = TRUE;

-- Step 4.3: Link ticket types to sessions (all tickets include the default session)
INSERT INTO "EventTicketTypeSessions" ("TicketTypeId", "SessionId")
SELECT 
    tt."Id",
    es."Id"
FROM "EventTicketTypes" tt
JOIN "EventSessions" es ON tt."EventId" = es."EventId"
WHERE es."SessionIdentifier" = 'S1'; -- Default session

-- Step 4.4: Migrate existing registrations to orders
-- Note: This preserves registration IDs as order numbers for traceability
INSERT INTO "Orders" (
    "Id",
    "UserId", 
    "EventId",
    "OrderNumber",
    "Status",
    "TotalAmount"
)
SELECT 
    r."Id", -- Preserve registration ID as order ID
    r."UserId",
    r."EventId", 
    CONCAT('REG-', r."Id"::text), -- Convert registration ID to order number
    CASE 
        WHEN r."Status" = 'Confirmed' THEN 'Confirmed'
        WHEN r."Status" = 'Cancelled' THEN 'Cancelled'
        ELSE 'Pending'
    END,
    COALESCE(r."SelectedPriceAmount", 0.00)
FROM "Registrations" r;

-- Step 4.5: Create order items for migrated registrations
INSERT INTO "OrderItems" (
    "OrderId",
    "TicketTypeId", 
    "Quantity",
    "UnitPrice",
    "TotalPrice"
)
SELECT 
    o."Id",
    tt."Id", -- Link to the General Admission ticket type
    1, -- One ticket per registration
    o."TotalAmount",
    o."TotalAmount"
FROM "Orders" o
JOIN "EventTicketTypes" tt ON o."EventId" = tt."EventId" 
WHERE tt."Name" = 'General Admission';

-- Step 4.6: Create session attendance records
INSERT INTO "SessionAttendances" (
    "SessionId",
    "UserId",
    "OrderItemId",
    "IsCheckedIn",
    "CheckInTime"
)
SELECT 
    es."Id",
    o."UserId",
    oi."Id",
    CASE WHEN r."CheckedInAt" IS NOT NULL THEN TRUE ELSE FALSE END,
    r."CheckedInAt"
FROM "Orders" o
JOIN "OrderItems" oi ON o."Id" = oi."OrderId"
JOIN "EventSessions" es ON o."EventId" = es."EventId"
JOIN "Registrations" r ON o."Id" = r."Id" -- Orders preserve registration IDs
WHERE es."SessionIdentifier" = 'S1'; -- Default session

-- =====================================================================
-- PHASE 5: ADD FOREIGN KEY REFERENCES TO USERS
-- =====================================================================

-- Add foreign key constraints for User references
-- Note: This assumes Users table exists - adjust based on actual schema
ALTER TABLE "Orders" 
ADD CONSTRAINT "FK_Orders_Users" FOREIGN KEY ("UserId") 
    REFERENCES "Users"("Id") ON DELETE RESTRICT;

ALTER TABLE "SessionAttendances" 
ADD CONSTRAINT "FK_SessionAttendances_Users" FOREIGN KEY ("UserId") 
    REFERENCES "Users"("Id") ON DELETE RESTRICT;

-- =====================================================================
-- PHASE 6: UPDATE EXISTING REGISTRATIONS TABLE (OPTIONAL)
-- =====================================================================

-- Option A: Add migration tracking to preserve legacy data
ALTER TABLE "Registrations" 
ADD COLUMN "MigratedToOrderId" uuid,
ADD CONSTRAINT "FK_Registrations_Orders" FOREIGN KEY ("MigratedToOrderId") 
    REFERENCES "Orders"("Id") ON DELETE SET NULL;

-- Update migration tracking
UPDATE "Registrations" r
SET "MigratedToOrderId" = r."Id"  -- Orders preserved registration IDs
WHERE EXISTS (SELECT 1 FROM "Orders" o WHERE o."Id" = r."Id");

-- Option B: Create archive table for historical data (recommended for production)
CREATE TABLE "Registrations_Archive" AS 
SELECT *, NOW() as "ArchivedAt" FROM "Registrations";

-- =====================================================================
-- DATA VALIDATION QUERIES
-- =====================================================================

-- Verify migration integrity
-- These queries should be run after migration to ensure data consistency

-- 1. Verify all events have at least one session
SELECT 
    e."Id", 
    e."Title",
    COUNT(es."Id") as session_count
FROM "Events" e
LEFT JOIN "EventSessions" es ON e."Id" = es."EventId"
WHERE e."IsPublished" = TRUE
GROUP BY e."Id", e."Title"
HAVING COUNT(es."Id") = 0;
-- Should return no rows

-- 2. Verify all ticket types have at least one session
SELECT 
    tt."Id",
    tt."Name", 
    COUNT(ttses."SessionId") as session_count
FROM "EventTicketTypes" tt
LEFT JOIN "EventTicketTypeSessions" ttses ON tt."Id" = ttses."TicketTypeId"
GROUP BY tt."Id", tt."Name"
HAVING COUNT(ttses."SessionId") = 0;
-- Should return no rows

-- 3. Verify order totals match order items
SELECT 
    o."Id",
    o."TotalAmount",
    SUM(oi."TotalPrice") as calculated_total
FROM "Orders" o
JOIN "OrderItems" oi ON o."Id" = oi."OrderId"
GROUP BY o."Id", o."TotalAmount"
HAVING o."TotalAmount" != SUM(oi."TotalPrice");
-- Should return no rows

-- 4. Verify session attendance counts don't exceed capacity
SELECT 
    es."Id",
    es."Name",
    es."Capacity",
    COUNT(sa."Id") as attendance_count
FROM "EventSessions" es
LEFT JOIN "SessionAttendances" sa ON es."Id" = sa."SessionId"
GROUP BY es."Id", es."Name", es."Capacity"
HAVING COUNT(sa."Id") > es."Capacity";
-- Should return no rows - if it does, capacity needs adjustment

-- 5. Migration completion summary
SELECT 
    'Events' as table_name, COUNT(*) as total_records FROM "Events"
UNION ALL
SELECT 'EventSessions', COUNT(*) FROM "EventSessions"  
UNION ALL
SELECT 'EventTicketTypes', COUNT(*) FROM "EventTicketTypes"
UNION ALL  
SELECT 'Orders', COUNT(*) FROM "Orders"
UNION ALL
SELECT 'OrderItems', COUNT(*) FROM "OrderItems"
UNION ALL
SELECT 'SessionAttendances', COUNT(*) FROM "SessionAttendances"
UNION ALL
SELECT 'Registrations (Original)', COUNT(*) FROM "Registrations";

COMMIT;

-- =====================================================================
-- ROLLBACK SCRIPT
-- =====================================================================

/*
-- EMERGENCY ROLLBACK (run in separate transaction)
-- WARNING: This will lose all Event Session Matrix data

BEGIN;

-- Drop new tables in reverse dependency order
DROP TABLE IF EXISTS "SessionAttendances" CASCADE;
DROP TABLE IF EXISTS "OrderItems" CASCADE; 
DROP TABLE IF EXISTS "Orders" CASCADE;
DROP TABLE IF EXISTS "EventTicketTypeSessions" CASCADE;
DROP TABLE IF EXISTS "EventTicketTypes" CASCADE;
DROP TABLE IF EXISTS "EventSessions" CASCADE;

-- Remove added columns from Events table
ALTER TABLE "Events" 
DROP COLUMN IF EXISTS "RequiresVetting",
DROP COLUMN IF EXISTS "RegistrationOpenDate", 
DROP COLUMN IF EXISTS "RegistrationCloseDate",
DROP COLUMN IF EXISTS "MaxAttendees",
DROP COLUMN IF EXISTS "AllowWaitlist";

-- Remove migration tracking from Registrations
ALTER TABLE "Registrations" 
DROP COLUMN IF EXISTS "MigratedToOrderId";

-- Drop archive table if created
DROP TABLE IF EXISTS "Registrations_Archive";

COMMIT;
*/

-- =====================================================================
-- POST-MIGRATION TASKS
-- =====================================================================

/*
After running this migration successfully:

1. Update Entity Framework Models:
   - Create EventSession entity with proper configuration
   - Create EventTicketType entity with proper configuration  
   - Create Order and OrderItem entities
   - Create SessionAttendance entity
   - Update Event entity with new properties
   - Configure relationships in DbContext

2. Update Application Services:
   - Create EventSessionService for session management
   - Create TicketTypeService for ticket type management
   - Create OrderService to replace RegistrationService
   - Update capacity calculation logic
   - Update pricing logic for sliding scales

3. Update API Endpoints:
   - Create session management endpoints
   - Create ticket type management endpoints
   - Update event creation/editing endpoints
   - Migrate registration endpoints to order endpoints
   - Add capacity checking endpoints

4. Update Frontend Components:
   - Create session management UI
   - Create ticket type configuration UI  
   - Update event creation/editing forms
   - Update registration/checkout flow
   - Add capacity indicators

5. Testing:
   - Unit tests for all new entities and services
   - Integration tests for migration logic
   - E2E tests for complete ticket purchasing flow
   - Performance tests for capacity calculations

6. Monitoring:
   - Add metrics for order processing
   - Add alerts for capacity violations
   - Monitor session attendance patterns
   - Track ticket type performance
*/

-- =====================================================================
-- CRITICAL PRODUCTION NOTES
-- =====================================================================

/*
BEFORE RUNNING IN PRODUCTION:

1. BACKUP DATABASE: Full backup before migration
2. MAINTENANCE MODE: Put application in maintenance mode
3. VALIDATE DATA: Run all validation queries  
4. MONITOR PERFORMANCE: Watch for slow queries during migration
5. TEST ROLLBACK: Verify rollback script works on copy of production data
6. CAPACITY PLANNING: Ensure database has sufficient space for new tables

MIGRATION TIMELINE ESTIMATE:
- Small database (<1000 events): 5-10 minutes
- Medium database (1000-10000 events): 15-30 minutes  
- Large database (>10000 events): 1-2 hours

RISK MITIGATION:
- Run on staging environment first
- Use read replicas for validation
- Monitor transaction log growth
- Have DBA review before execution
*/