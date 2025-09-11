# Database to Backend Developer Handoff - Event Session Matrix

**Date**: 2025-01-20  
**Phase**: Phase 2 - Event Session Matrix Backend  
**From**: Database Designer  
**To**: Backend Developer  
**Status**: Ready for Implementation

## Handoff Summary

The database schema for the Event Session Matrix system is complete and ready for backend implementation. This document provides critical implementation notes, priorities, and gotchas to ensure successful development.

## 🚨 CRITICAL IMPLEMENTATION NOTES

### 1. MANDATORY: Read Entity Framework Patterns First

**BEFORE starting ANY implementation work:**
- **MUST READ**: `/docs/standards-processes/development-standards/entity-framework-patterns.md`
- **CRITICAL**: All DateTime values MUST be UTC - use `DateTime.UtcNow` and `DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)`
- **CRITICAL**: Use correct DbContext name: `WitchCityRopeDbContext` (NOT `WitchCityRopeDbContext`)
- **CRITICAL**: Initialize all entity IDs in constructors: `Id = Guid.NewGuid()`

### 2. PostgreSQL-Specific Patterns

**DateTime Handling:**
```csharp
// ❌ WRONG - Will cause "Cannot write DateTime with Kind=Unspecified" error
StartDateTime = new DateTime(2025, 1, 20, 19, 0, 0);

// ✅ CORRECT - Always specify UTC
StartDateTime = new DateTime(2025, 1, 20, 19, 0, 0, DateTimeKind.Utc);
// OR
StartDateTime = DateTime.UtcNow;
```

**Entity Construction Pattern:**
```csharp
public EventSession(Event @event, string sessionIdentifier, /* other params */)
{
    Id = Guid.NewGuid(); // CRITICAL: Must set this first!
    Event = @event ?? throw new ArgumentNullException(nameof(@event));
    EventId = @event.Id;
    // ... other assignments
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

### 3. Migration Generation MUST Follow This Process

```bash
# 1. FIRST: Ensure all code compiles
dotnet build src/WitchCityRope.Infrastructure/

# 2. THEN: Generate migration using provided script
./scripts/generate-migration.sh AddEventSessionMatrix

# 3. VERIFY: Check migration file for correct table creation order
# 4. TEST: Run migration on test database first
```

## Implementation Priority Order

### Phase 1: Core Entities (Day 1-2)
1. **Create TicketTypeEnum** - Simple enum for Single/Couples
2. **Create EventSession Entity** - Core session management
3. **Create EventTicketType Entity** - Ticket type with pricing
4. **Create TicketTypeSessionInclusion** - Junction table entity
5. **Create EF Core Configurations** - Database mappings

### Phase 2: Database Migration (Day 2)
1. **Generate and Review Migration** - Use provided script
2. **Test Migration on Local Database** - Verify schema creation
3. **Update DbContext** - Add new DbSets and configurations
4. **Test Entity Creation** - Basic CRUD operations

### Phase 3: Service Layer (Day 3-4)
1. **Create EventSessionService** - CRUD operations for sessions
2. **Create EventTicketTypeService** - CRUD operations for ticket types
3. **Enhance EventService** - Integration with sessions and ticket types
4. **Create DTOs** - API response objects matching frontend interfaces

### Phase 4: API Endpoints (Day 4-5)
1. **EventSessions Controller** - CRUD endpoints
2. **EventTicketTypes Controller** - CRUD endpoints  
3. **Enhanced Events Controller** - Include sessions and ticket types
4. **Integration Testing** - End-to-end API tests

## Key Design Decisions Made

### 1. Entity Relationships
- **Event → EventSessions**: One-to-Many (Event can have multiple sessions)
- **Event → EventTicketTypes**: One-to-Many (Event can have multiple ticket types)
- **EventTicketType ↔ EventSession**: Many-to-Many via junction table (Ticket can include multiple sessions, session can be in multiple tickets)
- **Registration → EventTicketType**: Many-to-One nullable (Registration can specify which ticket type was purchased)

### 2. Session Identifier Pattern
- Format: "S1", "S2", "S3", etc.
- Unique per event (not globally unique)
- Used by frontend for display and user reference
- Auto-generated in service layer

### 3. Pricing Strategy
- Min/Max price range supports sliding scale pricing
- Stored as decimal(10,2) for financial accuracy
- Currency handling maintained through existing Money value object pattern

### 4. Capacity Management
- Each session has its own capacity
- Total event capacity = sum of session capacities (business rule, not enforced at DB level)
- Registrations count against specific sessions through ticket type inclusions

## Database Schema Quick Reference

### New Tables Created
1. **EventSessions** - Individual sessions within an event
2. **EventTicketTypes** - Different ticket options with pricing
3. **TicketTypeSessionInclusions** - Junction table for ticket ↔ session relationships

### Enhanced Tables
1. **Registrations** - Added `EventTicketTypeId` foreign key (nullable)

### Key Indexes Created
```sql
-- Performance-critical indexes
IX_EventSessions_EventId_IsActive        -- Active sessions per event
IX_EventTicketTypes_EventId_IsActive     -- Active ticket types per event  
IX_TicketTypeSessionInclusions_EventTicketTypeId  -- Session lookups
UQ_EventSessions_EventId_SessionIdentifier       -- Session uniqueness
```

## API Design Expectations

### Frontend Interface Compatibility

The frontend components expect these interfaces:

```typescript
// From EventSessionsGrid.tsx
interface EventSession {
  id: string;
  sessionIdentifier: string; // S1, S2, S3, etc.
  name: string;
  date: string;
  startTime: string;
  endTime: string;
  capacity: number;
  registeredCount: number;
}

// From EventTicketTypesGrid.tsx  
interface EventTicketType {
  id: string;
  name: string;
  type: 'Single' | 'Couples';
  sessionIdentifiers: string[]; // ['S1', 'S2', 'S3']
  minPrice: number;
  maxPrice: number;
  quantityAvailable?: number;
  salesEndDate?: string;
}
```

### Required API Endpoints

**EventSessions:**
- `GET /api/events/{eventId}/sessions` - Get all sessions for event
- `POST /api/events/{eventId}/sessions` - Create new session
- `PUT /api/events/{eventId}/sessions/{sessionId}` - Update session
- `DELETE /api/events/{eventId}/sessions/{sessionId}` - Delete session

**EventTicketTypes:**
- `GET /api/events/{eventId}/ticket-types` - Get all ticket types for event
- `POST /api/events/{eventId}/ticket-types` - Create new ticket type
- `PUT /api/events/{eventId}/ticket-types/{ticketTypeId}` - Update ticket type
- `DELETE /api/events/{eventId}/ticket-types/{ticketTypeId}` - Delete ticket type

## Critical Business Rules to Implement

### 1. Session Management
- Session identifier must be unique per event (S1, S2, S3, etc.)
- Session start time must be < end time
- Session capacity must be > 0
- Cannot delete sessions with existing registrations

### 2. Ticket Type Management  
- Min price must be >= 0
- Max price must be >= min price
- Quantity available must be > 0 or null (unlimited)
- Cannot delete ticket types with existing registrations

### 3. Session Inclusions
- Ticket type and all included sessions must belong to same event
- Cannot create circular references
- Must include at least one session per ticket type

### 4. Registration Integration
- When creating registration with ticket type, validate ticket type belongs to event
- Calculate session-specific registration counts for capacity management
- Handle quantity limits per ticket type

## Testing Requirements

### Unit Tests Required
```csharp
[Test] public void EventSession_Constructor_SetsUtcDateTimes()
[Test] public void EventSession_Constructor_ThrowsOnInvalidDateRange()
[Test] public void EventTicketType_Constructor_ValidatesPriceRange()
[Test] public void TicketTypeSessionInclusion_PreventsDuplicates()
```

### Integration Tests Required
```csharp
[Test] public void CanCreateEventWithSessionsAndTicketTypes()
[Test] public void CanQuerySessionsWithRegistrationCounts()
[Test] public void CanQueryTicketTypesWithIncludedSessions()
[Test] public void CascadeDeleteWorksCorrectly()
```

### API Tests Required
```csharp
[Test] public void POST_EventSessions_CreatesSessionSuccessfully()
[Test] public void GET_EventSessions_ReturnsCorrectFormat()
[Test] public void PUT_TicketTypes_UpdatesSessionInclusions()
[Test] public void DELETE_Session_FailsWithActiveRegistrations()
```

## Common Pitfalls to Avoid

### 1. DateTime Issues
```csharp
// ❌ WRONG - Unspecified DateTimeKind fails in PostgreSQL
session.StartDateTime = DateTime.Parse(request.StartTime);

// ✅ CORRECT - Always ensure UTC
session.StartDateTime = DateTime.SpecifyKind(DateTime.Parse(request.StartTime), DateTimeKind.Utc);
```

### 2. Entity Construction  
```csharp
// ❌ WRONG - Missing Id initialization causes Guid.Empty conflicts
public EventSession(Event @event, string sessionIdentifier)
{
    Event = @event;
    SessionIdentifier = sessionIdentifier;
    // Missing: Id = Guid.NewGuid();
}

// ✅ CORRECT - Always set Id first
public EventSession(Event @event, string sessionIdentifier)
{
    Id = Guid.NewGuid();  // CRITICAL!
    Event = @event;
    SessionIdentifier = sessionIdentifier;
}
```

### 3. Navigation Property Updates
```csharp
// ❌ WRONG - EF Core won't track this properly
ticketType.SessionInclusions.Add(inclusion);

// ✅ CORRECT - Add to context directly
_context.TicketTypeSessionInclusions.Add(inclusion);
```

### 4. Query Performance
```csharp
// ❌ WRONG - N+1 query problem
var sessions = await _context.EventSessions.Where(s => s.EventId == eventId).ToListAsync();
foreach(var session in sessions)
{
    var count = await _context.Registrations.CountAsync(r => r.EventTicketType.SessionInclusions.Any(si => si.EventSessionId == session.Id));
}

// ✅ CORRECT - Single query with aggregation  
var sessions = await _context.EventSessions
    .Where(s => s.EventId == eventId)
    .Select(s => new {
        Session = s,
        RegisteredCount = s.TicketTypeInclusions
            .SelectMany(tsi => tsi.EventTicketType.Registrations)
            .Count(r => r.Status == RegistrationStatus.Confirmed)
    }).ToListAsync();
```

## Validation Rules Reference

### EventSession Validation
- `SessionIdentifier`: Required, max 10 chars, unique per event
- `Name`: Required, max 200 chars
- `StartDateTime`: Required, must be UTC, must be < EndDateTime
- `EndDateTime`: Required, must be UTC, must be > StartDateTime  
- `Capacity`: Required, must be > 0

### EventTicketType Validation
- `Name`: Required, max 200 chars
- `TicketType`: Required, must be 'Single' or 'Couples'
- `MinPrice`: Required, must be >= 0
- `MaxPrice`: Required, must be >= MinPrice
- `QuantityAvailable`: Optional, if set must be > 0
- `SalesEndDateTime`: Optional, if set must be UTC

### TicketTypeSessionInclusion Validation
- Both `EventTicketTypeId` and `EventSessionId` must exist
- Must be unique combination
- Ticket type and session must belong to same event

## Files to Create/Modify

### New Entity Files
- `/src/WitchCityRope.Core/Entities/EventSession.cs`
- `/src/WitchCityRope.Core/Entities/EventTicketType.cs`
- `/src/WitchCityRope.Core/Entities/TicketTypeSessionInclusion.cs`
- `/src/WitchCityRope.Core/Enums/TicketTypeEnum.cs`

### EF Configuration Files
- `/src/WitchCityRope.Infrastructure/Data/Configurations/EventSessionConfiguration.cs`
- `/src/WitchCityRope.Infrastructure/Data/Configurations/EventTicketTypeConfiguration.cs`
- `/src/WitchCityRope.Infrastructure/Data/Configurations/TicketTypeSessionInclusionConfiguration.cs`

### Files to Modify
- `/src/WitchCityRope.Infrastructure/Data/WitchCityRopeDbContext.cs` - Add DbSets
- `/src/WitchCityRope.Infrastructure/Data/Configurations/RegistrationConfiguration.cs` - Add EventTicketTypeId FK
- `/src/WitchCityRope.Core/Entities/Registration.cs` - Add EventTicketTypeId property

### Service Layer Files (Create)
- `/src/WitchCityRope.Api/Features/Events/Services/EventSessionService.cs`
- `/src/WitchCityRope.Api/Features/Events/Services/EventTicketTypeService.cs`
- `/src/WitchCityRope.Api/Features/Events/Models/EventSessionDto.cs`
- `/src/WitchCityRope.Api/Features/Events/Models/EventTicketTypeDto.cs`

## Success Criteria

### Definition of Done
- [ ] All entities compile and pass unit tests
- [ ] Migration runs successfully on fresh database
- [ ] API endpoints return data matching frontend interfaces
- [ ] Integration tests pass with PostgreSQL
- [ ] No N+1 query issues in session/ticket type lookups
- [ ] Cascade deletes work correctly
- [ ] All business rule validations implemented
- [ ] DateTime values stored as UTC in database

### Performance Benchmarks
- Session lookup for event: < 50ms
- Ticket type lookup with sessions: < 100ms
- Registration count aggregation: < 200ms
- Bulk session creation (10 sessions): < 500ms

## Questions for Implementation

### Immediate Decisions Needed
1. **Session Identifier Generation**: Should "S1", "S2", etc. be auto-generated or user-provided?
2. **Bulk Operations**: Do we need bulk create/update endpoints for sessions?
3. **Validation Timing**: Should complex cross-entity validations happen in entities or services?
4. **Soft Delete Strategy**: Use IsActive flags or separate soft delete implementation?

### Future Considerations  
1. **Event Templates**: Should we support saving session/ticket configurations as templates?
2. **Session Dependencies**: Will sessions ever have prerequisites (e.g., Session 2 requires Session 1)?
3. **Dynamic Pricing**: Will we need time-based pricing changes beyond sales end dates?
4. **Session Transfers**: Will users be able to transfer between sessions?

## Next Steps

1. **Immediate**: Review this handoff document and database design
2. **Day 1**: Implement core entities and EF configurations
3. **Day 2**: Generate and test database migration
4. **Day 3**: Implement service layer with business rules
5. **Day 4**: Create API endpoints matching frontend interfaces
6. **Day 5**: Integration testing and performance validation

## Contact Information

**Database Designer**: Available for questions about schema design decisions, PostgreSQL specifics, or EF Core patterns.
**Frontend Team**: Confirm API interface requirements before implementation.
**Test Team**: Coordinate on integration test scenarios and performance benchmarks.

---

**REMINDER**: Read the Entity Framework patterns document first - it will save hours of debugging PostgreSQL DateTime issues and entity construction problems!