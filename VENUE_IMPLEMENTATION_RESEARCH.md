# Venue Implementation Research - WitchCityRope Codebase

**Date**: November 2, 2025
**Status**: CURRENT STATE ANALYSIS - No dedicated Venue entity exists yet
**Findings**: Venues are currently implemented as simple string values in the Location field

---

## EXECUTIVE SUMMARY

The WitchCityRope codebase currently uses a **STRING-BASED venue system** where:
- Venues are stored as plain text in the Event's `Location` field
- Frontend has **hardcoded mock venue options** (4 venues) 
- No dedicated Venue entity/database table exists
- No venue API endpoints exist (venues are not managed separately)
- Venues are **NOT fully integrated** - form field exists but data management is minimal

### Current Implementation Status
- Backend: Location = string (Event.Location property)
- Frontend: Hardcoded dropdown with 4 venue options
- Database: No Venue table, no migrations
- API: No venue endpoints (Location is just an Event property)

---

## 1. DATABASE SCHEMA

### Current State
**NO VENUE TABLE EXISTS** - Venues are managed as part of Event entity

#### Event Entity Schema
**File**: `/home/chad/repos/witchcityrope/apps/api/Models/Event.cs`

```csharp
public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? ShortDescription { get; set; }
    public string Description { get; set; }
    public string? Policies { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Capacity { get; set; }
    public EventType EventType { get; set; }
    public string Location { get; set; }  // <-- VENUES ARE HERE (just a string)
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<Session> Sessions { get; set; }
    public ICollection<TicketType> TicketTypes { get; set; }
    public ICollection<VolunteerPosition> VolunteerPositions { get; set; }
    public ICollection<ApplicationUser> Organizers { get; set; }
    public ICollection<EventParticipation> EventParticipations { get; set; }
}
```

### Database Migrations
**Location**: `/home/chad/repos/witchcityrope/apps/api/Migrations/`

**Key Migrations**:
- `20251024232104_InitialMigration.cs` - Initial schema (2674 lines) - NO Venue table
- `20251026035926_AddPricingTypeToTicketType.cs` - Pricing updates
- `20251026043704_RemovePricingTiersFromEvents.cs` - Event pricing refactoring
- `20251026044614_AddSlidingScalePricingToTicketTypes.cs` - Ticket pricing model
- Plus 10+ other migrations for various features

**FINDING**: None of these migrations create a Venue table

### DbContext Configuration
**File**: `/home/chad/repos/witchcityrope/apps/api/Data/ApplicationDbContext.cs` (Lines 346-413)

```csharp
// Event entity configuration - Location is just a required string property
modelBuilder.Entity<Event>(entity =>
{
    entity.ToTable("Events", "public");
    entity.HasKey(e => e.Id);
    
    entity.Property(e => e.Location)
          .IsRequired();
    
    // ... other properties ...
});
```

**NO DbSet<Venue>** exists in ApplicationDbContext

---

## 2. BACKEND API

### Current API Endpoints
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Endpoints/EventEndpoints.cs`

#### Existing Endpoints:
1. **GET /api/events** - Get all events (with optional admin access)
   - Returns: `ApiResponse<List<EventDto>>`
   - Includes: Location field (as string)
   - No venue-specific filters

2. **GET /api/events/{id}** - Get single event by ID
   - Returns: `ApiResponse<EventDto>`
   - Includes: Location field (as string)

3. **POST /api/events** - Create new event (implied in codebase)
4. **PUT /api/events/{id}** - Update event (implied in codebase)
5. **DELETE /api/events/{id}** - Delete event (implied in codebase)

### NO VENUE-SPECIFIC ENDPOINTS

There are **NO endpoints** for:
- GET /api/venues - List all venues
- GET /api/venues/{id} - Get single venue
- POST /api/venues - Create venue
- PUT /api/venues/{id} - Update venue
- DELETE /api/venues/{id} - Delete venue

### Request/Response DTOs

#### EventDto
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/EventDto.cs`

```csharp
public class EventDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string? ShortDescription { get; set; }
    public string Description { get; set; }
    public string? Policies { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Location { get; set; }  // <-- Venue info here
    public string EventType { get; set; }
    public int Capacity { get; set; }
    public bool IsPublished { get; set; }
    public int RegistrationCount { get; set; }
    public int CurrentRSVPs { get; set; }
    public int CurrentTickets { get; set; }
    public List<SessionDto> Sessions { get; set; }
    public List<TicketTypeDto> TicketTypes { get; set; }
    public List<VolunteerPositionDto> VolunteerPositions { get; set; }
    public List<string> TeacherIds { get; set; }
}
```

#### CreateEventRequest
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/CreateEventRequest.cs`

```csharp
public class CreateEventRequest
{
    [Required]
    public string Title { get; set; }
    public string? ShortDescription { get; set; }
    public string Description { get; set; }
    [Required]
    public DateTime StartDate { get; set; }
    [Required]
    public DateTime EndDate { get; set; }
    public string Location { get; set; }  // <-- Accepts location as string
    public string EventType { get; set; }
    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }
}
```

#### UpdateEventRequest
**File**: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Models/UpdateEventRequest.cs`

```csharp
public class UpdateEventRequest
{
    public string? Title { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Policies { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }  // <-- Can update location
    public int? Capacity { get; set; }
    public bool? IsPublished { get; set; }
    public List<SessionDto>? Sessions { get; set; }
    public List<TicketTypeDto>? TicketTypes { get; set; }
    public List<string>? TeacherIds { get; set; }
    public List<VolunteerPositionDto>? VolunteerPositions { get; set; }
}
```

### Archived DTO Pattern (Historical)
**File**: `/home/chad/repos/witchcityrope/src/_archive/WitchCityRope.Api/Features/Events/DTOs/EventDetailsDto.cs`

Shows what venue management **could** look like (from older codebase):

```csharp
public class EventDetailsDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; }
    // ... other properties ...
    public VenueDto Venue { get; set; } = new();  // Separate venue object
    // ... other properties ...
}

public class VenueDto
{
    public string Name { get; set; }
    public string Address { get; set; }
    public int Capacity { get; set; }
}
```

**IMPORTANT**: This archived pattern suggests venues were considered as separate objects in earlier design but NOT implemented

---

## 3. FRONTEND USAGE

### Frontend Type Definition
**File**: `/home/chad/repos/witchcityrope/apps/web/src/types/Event.ts`

```typescript
export interface Event {
  id: string
  title: string
  shortDescription?: string
  description: string
  startDate: string
  location: string  // <-- Venue is just a string
}
```

### EventForm Component - VENUE FIELD IMPLEMENTATION
**File**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventForm.tsx`

#### Form Data Structure (Lines 64-83):
```typescript
export interface EventFormData {
  // Basic Info
  eventType: 'class' | 'social'
  title: string
  shortDescription: string
  fullDescription: string
  policies: string
  venueId: string  // <-- Venue stored as simple string ID
  teacherIds: string[]
  
  // Status
  status: 'Draft' | 'Published' | 'Cancelled' | 'Completed'
  
  // Sessions and Tickets
  sessions: EventSession[]
  ticketTypes: EventTicketType[]
  
  // Volunteer Positions
  volunteerPositions: VolunteerPosition[]
}
```

#### Hardcoded Venue Options (Lines 201-207):
```typescript
// Mock data for dropdowns
const venues = [
  { value: 'main-studio', label: 'Main Studio' },
  { value: 'meditation-room', label: 'Meditation Room' },
  { value: 'outdoor-space', label: 'Outdoor Space' },
  { value: 'off-site', label: 'Off-site Location' },
]
```

#### Venue Select Component (Lines ~615-620 in form):
```typescript
<Select
  label="Venue"
  placeholder="Select venue..."
  data={venues}  // Hardcoded options
  required
  {...form.getInputProps('venueId')}
/>
```

#### Form Validation (Line 147):
```typescript
venueId: (value) => (!value ? 'Venue selection is required' : null),
```

#### "Add Venue" Button (Line ~616):
- UI button exists but **NO FUNCTIONALITY** - just a placeholder
- No modal or form to add new venues
- No API call to save new venues

### Admin Event Details Page
**File**: `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminEventDetailsPage.tsx`

```typescript
// Extract venue from location field (API returns location as string)
const venueId = event.location || ''

// Map eventType from API response
const eventType = event.eventType === 'Class' ? 'class' : 'social'

// Pass to EventForm
form.setValues({
  venueId, // Now properly extracted from API location field
  // ... other fields ...
})
```

**KEY POINT**: venueId is extracted from the `event.location` field - venues are just strings

### Data Transformation Utilities
**File**: `/home/chad/repos/witchcityrope/apps/web/src/utils/eventDataTransformation.ts`

```typescript
export function convertEventFormDataToUpdateDto(
  eventId: string,
  formData: EventFormData,
  isPublished?: boolean
): UpdateEventDto {
  const updateDto: UpdateEventDto = {
    id: eventId,
  };

  // Handle venue selection
  if (formData.venueId?.trim()) {
    updateDto.location = formData.venueId.trim();  // Venue → Location field
  }

  // ... other field mappings ...

  return updateDto;
}

export function hasEventFormDataChanged(
  current: EventFormData,
  initial: EventFormData
): boolean {
  const fieldsToCheck: (keyof EventFormData)[] = [
    'title',
    'shortDescription',
    'fullDescription',
    'policies',
    'venueId',  // <-- Tracked as part of form change detection
    'eventType',
    'teacherIds',
    'sessions',
    'ticketTypes',
    'volunteerPositions'
  ];
  // ...
}
```

### Frontend Pages Using Events
1. **EventsListPage** (`/pages/events/EventsListPage.tsx`) - Displays events to public
2. **EventDetailPage** (`/pages/events/EventDetailPage.tsx`) - Shows event details
3. **AdminEventsPage** (`/pages/admin/AdminEventsPage.tsx`) - Admin event listing
4. **AdminEventDetailsPage** (`/pages/admin/AdminEventDetailsPage.tsx`) - Admin event editor
5. **NewEventPage** (`/pages/admin/NewEventPage.tsx`) - Create new events
6. **EventFormTestPage** (`/pages/EventFormTestPage.tsx`) - Test page for EventForm

---

## 4. DATA SEEDING

### EventSeeder Implementation
**File**: `/home/chad/repos/witchcityrope/apps/api/Services/Seeding/EventSeeder.cs`

#### Current Venue Values in Seed Data:

**Basic Events** (8 total - line 1081):
```csharp
Location = eventType == EventType.Social ? "Community Space" : "Main Workshop Room"
```

This creates:
- Social events → Location: "Community Space"
- Class events → Location: "Main Workshop Room"

**Historical Events** (variable locations - line 1119):
```csharp
Location = location  // Parameter passed in
```

Examples of historical event locations:
- "Studio Space"
- "Salem Community Center"
- "The Gathering Space"

#### Seed Data Details:

**Upcoming Class Events** (3):
1. "Introduction to Rope Safety" - Location: "Main Workshop Room"
2. "Suspension Basics" - Location: "Main Workshop Room"
3. "Advanced Floor Work" - Location: "Main Workshop Room"

**Upcoming Social Events** (3):
1. "Community Rope Jam" - Location: "Community Space"
2. "Rope Social & Discussion" - Location: "Community Space"
3. "New Members Meetup" - Location: "Community Space"

**Past Social Event** (1):
- "Beginner Rope Circle" - Location: Custom (via CreateHistoricalEvent)

**Past Class Event** (1):
- "Rope Fundamentals Series" - Location: Custom (via CreateHistoricalEvent)

#### Seeding Method Signature (Lines 1055-1064):
```csharp
private Event CreateSeedEvent(
    string title,
    int daysFromNow,
    int startHour,
    int capacity,
    EventType eventType,
    decimal price,
    string shortDescription,
    string longDescription,
    string policies)
{
    // ... creates Event with hardcoded locations ...
    Location = eventType == EventType.Social ? "Community Space" : "Main Workshop Room"
}
```

**NO venue management** - locations are hardcoded into event creation

---

## 5. ARCHITECTURE AND PATTERNS

### Current Pattern: String-Based Venues
```
Event.Location (string) ──→ EventDto.location (string) ──→ EventForm.venueId (string)
```

**Flow**:
1. Backend: Store location as plain string in Event table
2. API: Return location as string in EventDto
3. Frontend: Map location string to venueId, display in dropdown
4. Update: Convert venueId back to location string when saving

### Issues with Current Implementation

1. **Hardcoded Options Only** - Frontend has 4 hardcoded venues with no way to add more
2. **No Persistence of Venue Metadata** - Only the venue name is stored, no address, capacity, or other details
3. **No "Add Venue" Functionality** - Button exists but does nothing
4. **Mismatch Between Form and Data** - Form uses venueId but API expects location string
5. **No Venue Management Interface** - No admin page to manage venues
6. **No Validation** - API doesn't validate that location matches available venues
7. **Duplication Risk** - Venue names can be entered differently (typos, inconsistency)

---

## 6. WHAT'S MISSING - GAPS AND NEEDS

### Missing Backend Features
- [ ] Venue entity/model class
- [ ] Venues database table with schema
- [ ] Migration to create Venues table
- [ ] VenueDto for API responses
- [ ] Venue API endpoints (CRUD operations)
- [ ] Venue service layer
- [ ] Foreign key relationship: Event → Venue
- [ ] Venue seeder for sample data
- [ ] Venue validation in event creation/updates
- [ ] Admin controller endpoints for venue management

### Missing Frontend Features
- [ ] Dedicated venue management UI
- [ ] Dynamic venue dropdown (load from API)
- [ ] "Add Venue" modal functionality
- [ ] Venue editing capabilities
- [ ] Venue deletion (with cascade considerations)
- [ ] Venue address/directions display
- [ ] Venue capacity information
- [ ] Map integration for venues
- [ ] Venue search/filter
- [ ] Venue photos/images

### Missing Integration Points
- [ ] Event validation against available venues
- [ ] Venue capacity constraints
- [ ] Venue-specific pricing (if needed)
- [ ] Venue operating hours (if needed)
- [ ] Venue capacity vs Event capacity validation
- [ ] Archived/inactive venues handling

---

## 7. QUICK REFERENCE - FILE LOCATIONS

### Backend Files
| Component | File Path |
|-----------|-----------|
| Event Model | `/apps/api/Models/Event.cs` |
| EventDto | `/apps/api/Features/Events/Models/EventDto.cs` |
| CreateEventRequest | `/apps/api/Features/Events/Models/CreateEventRequest.cs` |
| UpdateEventRequest | `/apps/api/Features/Events/Models/UpdateEventRequest.cs` |
| Event Endpoints | `/apps/api/Features/Events/Endpoints/EventEndpoints.cs` |
| Event Service | `/apps/api/Features/Events/Services/EventService.cs` |
| EventSeeder | `/apps/api/Services/Seeding/EventSeeder.cs` |
| DbContext | `/apps/api/Data/ApplicationDbContext.cs` |
| Migrations | `/apps/api/Migrations/` |

### Frontend Files
| Component | File Path |
|-----------|-----------|
| Event Types | `/apps/web/src/types/Event.ts` |
| EventForm | `/apps/web/src/components/events/EventForm.tsx` |
| EventDto | `/apps/web/src/Features/Events/Models/EventDto.cs` |
| Data Transformation | `/apps/web/src/utils/eventDataTransformation.ts` |
| Admin Events Page | `/apps/web/src/pages/admin/AdminEventsPage.tsx` |
| Admin Event Details | `/apps/web/src/pages/admin/AdminEventDetailsPage.tsx` |
| New Event Page | `/apps/web/src/pages/admin/NewEventPage.tsx` |
| Events List Page | `/apps/web/src/pages/events/EventsListPage.tsx` |
| Event Detail Page | `/apps/web/src/pages/events/EventDetailPage.tsx` |

---

## 8. CURRENT VENUE VALUES IN SYSTEM

### Hardcoded Frontend Venues
```
main-studio → "Main Studio"
meditation-room → "Meditation Room"
outdoor-space → "Outdoor Space"
off-site → "Off-site Location"
```

### Seed Data Venues
```
"Community Space"
"Main Workshop Room"
"Studio Space"
"Salem Community Center"
"The Gathering Space"
```

### MISMATCH ALERT
- Frontend offers: Main Studio, Meditation Room, Outdoor Space, Off-site Location
- Seed data creates events with: Community Space, Main Workshop Room, Studio Space, Salem Community Center, The Gathering Space
- **These don't match!** Frontend dropdown options are different from seeded event locations

---

## CONCLUSION

The venue system in WitchCityRope is **MINIMALLY IMPLEMENTED**:

1. ✅ **Exists**: Location field in Event model and DTOs
2. ✅ **Frontend UI**: Dropdown field with hardcoded options exists
3. ❌ **Dedicated Entity**: No Venue entity or table
4. ❌ **API Endpoints**: No venue management endpoints
5. ❌ **Functional "Add Venue"**: Button exists but no implementation
6. ❌ **Venue Details**: No address, capacity, directions, etc.
7. ❌ **Admin Interface**: No venue management page
8. ❌ **Validation**: No venue validation or constraints

**Current Status**: Venues are simple strings stored in the Event.Location field with a hardcoded frontend dropdown. This is a **PLACEHOLDER IMPLEMENTATION** that needs a complete venue management system to be production-ready.

---

## RECOMMENDATIONS FOR IMPLEMENTATION

To properly implement venues, you would need:

1. **Create Venue Entity** - Define full venue properties (name, address, capacity, directions, etc.)
2. **Database Migration** - Create Venues table and establish Event → Venue relationship
3. **API Endpoints** - Build full CRUD venue endpoints
4. **Frontend Components** - Create venue management UI in admin section
5. **Validation** - Add business logic to validate event venues
6. **Integration** - Update all event creation/editing flows to use venue IDs
7. **Seeding** - Create actual venue seeding (separate from event seeding)

See `/docs/functional-areas/events/` for proper architectural documentation.
