-- =====================================================
-- WitchCityRope Vertical Slice POC - Database Migration
-- =====================================================
-- Created: 2025-08-16
-- Purpose: Create Events table for technical proof-of-concept
-- Status: Throwaway code for stack validation
-- Target: PostgreSQL 15+

-- =====================================================
-- 1. CREATE EVENTS TABLE
-- =====================================================

CREATE TABLE "Events" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT NOT NULL,
    "StartDate" TIMESTAMPTZ NOT NULL,
    "Location" VARCHAR(200) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =====================================================
-- 2. CREATE INDEXES
-- =====================================================

-- Index for date-based queries (most common use case)
CREATE INDEX "IX_Events_StartDate" ON "Events" ("StartDate");

-- Optional: Index for location queries if needed later
-- CREATE INDEX "IX_Events_Location" ON "Events" ("Location");

-- =====================================================
-- 3. INSERT TEST DATA
-- =====================================================

-- Test events for proof-of-concept validation
INSERT INTO "Events" ("Id", "Title", "Description", "StartDate", "Location", "CreatedAt") VALUES
(
    gen_random_uuid(),
    'Rope Basics Workshop',
    'Learn the fundamentals of rope bondage in a safe, educational environment. Perfect for beginners who want to explore rope play with proper safety techniques.',
    '2025-08-25 14:00:00+00'::timestamptz,
    'Salem Community Center - Room 201',
    NOW()
),
(
    gen_random_uuid(),
    'Advanced Suspension Techniques',
    'Advanced workshop covering suspension safety, rigging points, and dynamic movements. Prerequisites: Basic rope knowledge and comfort with being suspended.',
    '2025-08-30 19:00:00+00'::timestamptz,
    'Studio Space Downtown - Main Floor',
    NOW()
),
(
    gen_random_uuid(),
    'Community Social & Practice',
    'Open practice session for all skill levels. Bring your rope and practice with others in a supportive community environment. Light refreshments provided.',
    '2025-09-05 18:30:00+00'::timestamptz,
    'Salem Arts Collective - Gallery Space',
    NOW()
),
(
    gen_random_uuid(),
    'Photography & Rope Art',
    'Artistic rope photography session with professional photographer. Create beautiful rope art images in a respectful, artistic setting.',
    '2025-09-12 16:00:00+00'::timestamptz,
    'Private Studio - 123 Maple Street',
    NOW()
),
(
    gen_random_uuid(),
    'Shibari History & Philosophy',
    'Educational session exploring the historical and cultural aspects of Japanese rope bondage. Learn about the art, philosophy, and traditional techniques.',
    '2025-09-18 20:00:00+00'::timestamptz,
    'Salem Public Library - Conference Room A',
    NOW()
);

-- =====================================================
-- 4. VERIFICATION QUERIES
-- =====================================================

-- Verify table creation
SELECT table_name, column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'Events' 
ORDER BY ordinal_position;

-- Verify indexes
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'Events';

-- Verify test data
SELECT "Id", "Title", "StartDate", "Location", "CreatedAt"
FROM "Events"
ORDER BY "StartDate";

-- Count total events
SELECT COUNT(*) as total_events FROM "Events";

-- =====================================================
-- 5. ROLLBACK SCRIPT (IF NEEDED)
-- =====================================================

/*
-- Uncomment to rollback changes:

-- Drop indexes
DROP INDEX IF EXISTS "IX_Events_StartDate";

-- Drop table
DROP TABLE IF EXISTS "Events";

-- Verify cleanup
SELECT table_name FROM information_schema.tables 
WHERE table_name = 'Events';
*/

-- =====================================================
-- NOTES FOR DEVELOPERS
-- =====================================================

/*
IMPORTANT NOTES:

1. UTC DateTime Handling:
   - All timestamps use TIMESTAMPTZ for timezone awareness
   - EF Core requires DateTime.UtcNow or DateTimeKind.Utc
   - Example: new DateTime(2025, 8, 25, 14, 0, 0, DateTimeKind.Utc)

2. Case-Sensitive Names:
   - Column names are quoted to match EF Core conventions
   - PostgreSQL will lowercase unquoted identifiers

3. UUID Generation:
   - Uses gen_random_uuid() for PostgreSQL-native UUID generation
   - Maps to Guid type in C#

4. Test Data:
   - Events span future dates for testing upcoming events queries
   - Descriptions are realistic but fictional
   - Locations are placeholder names

5. Proof-of-Concept Scope:
   - This is throwaway code for stack validation
   - Production schema will be more complex with proper relationships
   - No foreign keys or complex constraints intentionally

6. Performance:
   - Basic index on StartDate for common queries
   - Additional indexes can be added based on query patterns

ENTITY FRAMEWORK CORE USAGE:

To use this schema in EF Core:

1. Create Entity:
   public class Event
   {
       public Guid Id { get; set; } = Guid.NewGuid();
       public string Title { get; set; } = string.Empty;
       public string Description { get; set; } = string.Empty;
       public DateTime StartDate { get; set; }
       public string Location { get; set; } = string.Empty;
       public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
   }

2. Configure in DbContext:
   modelBuilder.Entity<Event>(entity =>
   {
       entity.ToTable("Events", "public");
       entity.HasKey(e => e.Id);
       entity.Property(e => e.StartDate).HasColumnType("timestamptz");
       entity.Property(e => e.CreatedAt).HasColumnType("timestamptz");
   });

3. Query Examples:
   // Get upcoming events
   var events = await context.Events
       .AsNoTracking()
       .Where(e => e.StartDate > DateTime.UtcNow)
       .OrderBy(e => e.StartDate)
       .ToListAsync();

TESTING VALIDATION:

After running this migration:

1. Verify connection:
   dotnet ef database update --context ApplicationDbContext

2. Test data retrieval:
   curl http://localhost:5653/api/events

3. Check in React app:
   Navigate to http://localhost:5173 and verify events display

*/