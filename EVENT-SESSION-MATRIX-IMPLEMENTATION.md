# Event Session Matrix Domain Models Implementation

## Overview

Successfully implemented the Event Session Matrix architecture domain models to support session-based event ticketing. This enables complex events with multiple sessions (S1, S2, S3, etc.) and flexible ticket types that include different combinations of sessions.

## Created Domain Entities

### 1. EventSession (`/src/WitchCityRope.Core/Entities/EventSession.cs`)
- **Purpose**: Represents individual sessions within an event (e.g., S1, S2, S3)
- **Key Properties**:
  - `SessionIdentifier`: String identifier (S1, S2, S3, etc.)
  - `Name`: Display name for the session
  - `Date`: When the session occurs
  - `StartTime`/`EndTime`: Session timing
  - `Capacity`: Maximum attendees for this session
  - `RegisteredCount`: Current registrations
  - `IsRequired`: Whether session is mandatory

- **Business Rules**:
  - Session capacity must be > 0
  - Start time must be before end time
  - Session identifier must follow S1, S2, S3 format
  - Cannot overlap with other sessions on same date
  - Registered count cannot exceed capacity

### 2. EventTicketType (`/src/WitchCityRope.Core/Entities/EventTicketType.cs`)
- **Purpose**: Represents ticket types with sliding scale pricing and session inclusion
- **Key Properties**:
  - `Name`: Display name (e.g., "Single Person", "Couple")
  - `MinPrice`/`MaxPrice`: Sliding scale pricing range
  - `QuantityAvailable`: Optional quantity limits
  - `SalesEndDate`: When sales end for this ticket type
  - `IsRsvpMode`: Whether payment is required

- **Business Rules**:
  - Must include at least one session
  - Price range validation (min ≤ max, both ≥ 0)
  - Sales dates cannot be in the past
  - Quantity limits must be positive when specified

### 3. EventTicketTypeSession (`/src/WitchCityRope.Core/Entities/EventTicketTypeSession.cs`)
- **Purpose**: Junction entity linking ticket types to specific sessions
- **Key Properties**:
  - `TicketTypeId`: Foreign key to EventTicketType
  - `SessionIdentifier`: Session identifier (S1, S2, etc.)

- **Business Rules**:
  - Each ticket type + session combination must be unique
  - Session identifier must follow proper format

### 4. Updated Event Entity (`/src/WitchCityRope.Core/Entities/Event.cs`)
- **Added Collections**:
  - `Sessions`: Collection of EventSession entities
  - `TicketTypes`: Collection of EventTicketType entities

- **New Business Methods**:
  - `AddSession()`: Validates and adds sessions
  - `RemoveSession()`: Ensures no ticket types reference the session
  - `AddTicketType()`: Validates session references exist
  - `CalculateTicketTypeAvailability()`: Finds minimum capacity across included sessions
  - Session overlap validation
  - Referential integrity enforcement

## Database Configuration

### 1. EventSession Configuration (`EventSessionConfiguration.cs`)
- Table: `EventSessions`
- Primary Key: `Id` (Guid)
- Alternate Key: `(EventId, SessionIdentifier)` for uniqueness
- Foreign Key: `EventId` → `Events.Id` (CASCADE delete)
- Indexes: EventId, Date+StartTime, EventId+SessionIdentifier (unique)
- Check Constraints: Capacity > 0, RegisteredCount ≥ 0, RegisteredCount ≤ Capacity

### 2. EventTicketType Configuration (`EventTicketTypeConfiguration.cs`)
- Table: `EventTicketTypes`
- Primary Key: `Id` (Guid)
- Foreign Key: `EventId` → `Events.Id` (CASCADE delete)
- Indexes: EventId, EventId+Name (unique), EventId+IsActive, SalesEndDate
- Check Constraints: Price validation, quantity validation

### 3. EventTicketTypeSession Configuration (`EventTicketTypeSessionConfiguration.cs`)
- Table: `EventTicketTypeSessions`
- Primary Key: `Id` (Guid)
- Foreign Key: `TicketTypeId` → `EventTicketTypes.Id` (CASCADE delete)
- Indexes: TicketTypeId, SessionIdentifier, TicketTypeId+SessionIdentifier (unique)

### 4. Updated DbContext (`WitchCityRopeDbContext.cs`)
- Added `DbSet<EventSession>`, `DbSet<EventTicketType>`, `DbSet<EventTicketTypeSession>`
- Applied all configurations
- Updated audit trail for new entities

## Key Design Decisions

### 1. Session Identifiers as Strings
- Used string identifiers (S1, S2, S3) instead of integer IDs for better business readability
- Enables natural sorting and display
- Validates format during creation

### 2. Junction Table Approach
- EventTicketTypeSession provides many-to-many relationship flexibility
- Enables complex ticket configurations (e.g., ticket includes S1+S3 but not S2)
- Maintains referential integrity

### 3. Sliding Scale Pricing
- Min/Max price range supports sliding scale model
- Individual ticket selection happens at registration time
- Maintains business model flexibility

### 4. Capacity Calculation
- Ticket type availability = minimum capacity across all included sessions
- Prevents overbooking complex ticket combinations
- Real-time availability checking

## Business Logic Highlights

### Session Management
```csharp
// Add session with overlap validation
event.AddSession(session);

// Remove session with referential integrity check
event.RemoveSession("S1"); // Throws if ticket types reference S1
```

### Ticket Type Configuration
```csharp
// Create ticket type
var ticketType = new EventTicketType(
    eventId, "Weekend Pass", "Both S1 and S2", 
    minPrice: 75m, maxPrice: 150m
);

// Configure included sessions
ticketType.AddSession("S1");
ticketType.AddSession("S2");

// Add to event (validates sessions exist)
event.AddTicketType(ticketType);
```

### Availability Calculation
```csharp
// Calculate ticket availability
var availability = event.CalculateTicketTypeAvailability(ticketType);
// Returns minimum available spots across all included sessions
```

## Testing Verification

Successfully tested domain model creation and basic business logic:
- ✅ EventSession creation with validation
- ✅ EventTicketType creation with pricing validation
- ✅ Session inclusion in ticket types
- ✅ Availability calculation
- ✅ Business rule enforcement

## Integration Points

### With Registration System
- Registration entities can track which sessions user is registered for
- Capacity tracking per session enables accurate availability
- Supports complex scenarios (user registered for S1 but not S2)

### With Payment System
- Sliding scale pricing integrates with existing payment architecture
- RSVP mode supports free/social events
- Quantity limits enable tiered pricing strategies

### With Frontend
- Session identifiers (S1, S2, S3) provide natural UI labeling
- Ticket type configuration supports flexible UI presentation
- Availability calculation enables real-time capacity display

## Next Steps

1. **Database Migration**: Create and run migrations for new entities
2. **Service Layer**: Implement service layer for session management
3. **API Endpoints**: Create REST endpoints for session-based events
4. **Frontend Integration**: Update React components for session selection
5. **Testing**: Complete integration and end-to-end testing

## Files Created/Modified

### Created Files:
- `src/WitchCityRope.Core/Entities/EventSession.cs`
- `src/WitchCityRope.Core/Entities/EventTicketType.cs`
- `src/WitchCityRope.Core/Entities/EventTicketTypeSession.cs`
- `src/WitchCityRope.Infrastructure/Data/Configurations/EventSessionConfiguration.cs`
- `src/WitchCityRope.Infrastructure/Data/Configurations/EventTicketTypeConfiguration.cs`
- `src/WitchCityRope.Infrastructure/Data/Configurations/EventTicketTypeSessionConfiguration.cs`

### Modified Files:
- `src/WitchCityRope.Core/Entities/Event.cs` - Added session/ticket type collections and business methods
- `src/WitchCityRope.Infrastructure/Data/WitchCityRopeDbContext.cs` - Added new DbSets and configurations

## Architectural Compliance

- ✅ **Domain-Driven Design**: Rich domain models with business logic
- ✅ **SOLID Principles**: Single responsibility, proper encapsulation
- ✅ **Clean Architecture**: Core entities independent of infrastructure
- ✅ **Entity Framework Patterns**: Proper configuration and relationships
- ✅ **Testing Ready**: Domain models testable in isolation
- ✅ **Database Design**: Normalized structure with integrity constraints