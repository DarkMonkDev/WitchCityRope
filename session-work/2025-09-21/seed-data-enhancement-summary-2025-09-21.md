# Seed Data Enhancement Summary - September 21, 2025

## Overview
Updated the database seeder to improve testing of RSVP/ticket functionality based on user requirements.

## Changes Made

### 1. Event Count Reduction
**Previous**: 12 events (10 upcoming, 2 past)
**New**: 8 events (6 upcoming, 2 past)

### 2. Event Mix Improvement
**Previous**: 9 upcoming classes, 1 upcoming social event
**New**: 3 upcoming classes, 3 upcoming social events

**Upcoming Classes (3)**:
- Introduction to Rope Safety (in 7 days, capacity 20)
- Suspension Basics (in 14 days, capacity 12)
- Advanced Floor Work (in 21 days, capacity 10)

**Upcoming Social Events (3)**:
- Community Rope Jam (in 28 days, capacity 40)
- Rope Social & Discussion (in 35 days, capacity 30)
- New Members Meetup (in 42 days, capacity 25)

**Past Events (2)**:
- Beginner Rope Circle (7 days ago, capacity 20)
- Rope Fundamentals Series (14 days ago, capacity 15)

### 3. EventParticipation Records Added
Created new `SeedEventParticipationsAsync()` method that adds:

**Social Events RSVPs**:
- Community Rope Jam: 5 RSVPs
- New Members Meetup: 8 RSVPs
- Rope Social & Discussion: 6 RSVPs
- Past/Other events: 3 RSVPs each

**Class Event Tickets**:
- Introduction to Rope Safety: 5 tickets sold
- Suspension Basics: 4 tickets sold
- Advanced Floor Work: 3 tickets sold
- Past classes: 2 tickets sold each

### 4. Varied Purchase Dates
- RSVPs: Created 1-10 days ago
- Ticket purchases: Created 1-20 days ago
- Realistic staggered participation timing

## Files Modified

### `/apps/api/Services/SeedDataService.cs`
- Reduced event count from 12 to 8 events
- Added more social events (3 instead of 1)
- Added `SeedEventParticipationsAsync()` method
- Updated method order to include EventParticipations seeding
- Added proper imports for EventParticipation entities
- Updated documentation comments

### `/apps/api/Services/ISeedDataService.cs`
- Added `SeedEventParticipationsAsync()` method signature
- Added comprehensive documentation for the new method

## Database Impact

### New EventParticipation Records
The seed data now creates realistic participation records:
- **RSVPs**: ParticipationType.RSVP with Status.Active
- **Tickets**: ParticipationType.Ticket with Status.Active
- **Metadata**: JSON with purchase amount and payment method for tickets
- **Timestamps**: Realistic CreatedAt/UpdatedAt dates

### Capacity Testing Scenarios
The new seed data provides better testing scenarios:
- **High attendance**: New Members Meetup (8/25 = 32%)
- **Medium attendance**: Community Rope Jam (5/40 = 12.5%)
- **Low attendance**: Rope Social & Discussion (6/30 = 20%)
- **Class variations**: 25-50% capacity across different class types

## Testing Benefits

### RSVP System Testing
- Multiple social events with varying RSVP counts
- Tests capacity display: "5/40 attendees" format
- Tests different social event types and capacities

### Ticket System Testing
- Varied ticket sales across class difficulty levels
- Tests purchase metadata and payment tracking
- Tests capacity management for paid events

### Historical Data Testing
- Past events with RSVPs/tickets for filtering tests
- Tests date-based queries and historical views

## Database Reseeding Instructions

### Method 1: Full Database Reset (Recommended)
```bash
# Stop the API container
docker-compose down api

# Reset the database (this will drop and recreate all data)
./scripts/reset-database.sh

# Start the API container (will auto-seed with new data)
docker-compose up api -d

# Verify seeding in logs
docker-compose logs api | grep -i seed
```

### Method 2: Manual Seeding Trigger
If the database already has data and auto-seeding is skipped:

```bash
# Connect to the API container
docker-compose exec api bash

# Run the seeding endpoint (if available)
curl -X POST http://localhost:5655/api/admin/seed-data

# Or use the database seeder tool
dotnet run --project tools/DatabaseSeeder
```

### Method 3: SQL Reset (Advanced)
```sql
-- Connect to PostgreSQL and clear existing data
DELETE FROM "EventParticipations";
DELETE FROM "TicketPurchases";
DELETE FROM "Sessions";
DELETE FROM "TicketTypes";
DELETE FROM "VolunteerPositions";
DELETE FROM "Events";
DELETE FROM "AspNetUsers" WHERE "Email" LIKE '%@witchcityrope.com';

-- Restart API to trigger auto-seeding
```

## Verification Steps

After reseeding, verify the changes:

### 1. Event Count Check
```bash
# Should show 8 events total
curl http://localhost:5655/api/events | jq length
```

### 2. Social Events Check
```bash
# Should show 3 upcoming social events
curl http://localhost:5655/api/events | jq '[.[] | select(.eventType == "Social" and .startDate > now)]'
```

### 3. RSVP Count Check
```bash
# Community Rope Jam should show 5/40 capacity
curl http://localhost:5655/api/events | jq '.[] | select(.title | contains("Community Rope Jam")) | {title, currentAttendees: .currentAttendeeCount, capacity}'
```

### 4. Participation Records Check
```bash
# Should show EventParticipation records
curl http://localhost:5655/api/admin/participations | jq length
```

## Expected Frontend Impact

### Event Lists
- Cleaner event lists with fewer items for easier testing
- Better mix of event types for UI component testing

### RSVP/Ticket Buttons
- Social events show "RSVP" with current counts
- Class events show "Buy Tickets" with current sales

### Capacity Displays
- Progress bars show varied fill levels
- Different capacity scenarios for UI testing

### Event Details
- Participation lists show realistic user data
- Purchase history shows varied payment methods and dates

## Rollback Instructions

If the changes need to be reverted:

1. Restore the previous version:
```bash
git checkout HEAD~1 -- apps/api/Services/SeedDataService.cs apps/api/Services/ISeedDataService.cs
```

2. Reset database with old seed data:
```bash
./scripts/reset-database.sh
docker-compose up api -d
```

## Notes for Test Executor

This enhancement provides much better data for testing:
- **RSVP functionality**: Multiple social events with realistic attendance
- **Ticket purchasing**: Varied sales across different class types
- **Capacity management**: Different fill levels for progress bar testing
- **Historical data**: Past events for date filtering tests
- **User scenarios**: Multiple test users with varied participation patterns

The seed data is now more focused and provides better coverage of the RSVP/ticket functionality while being easier to understand during testing.