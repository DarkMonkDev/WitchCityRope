# Existing Events API Analysis for Event Session Matrix Migration

**Date**: 2025-08-24  
**Author**: Backend Developer  
**Status**: Analysis Complete - Ready for Implementation Planning

## Executive Summary

This analysis examines the current Events API implementation to identify what needs to be updated for the new Event Session Matrix architecture. The current system uses a simple single-event model with registration-based ticketing. The new system requires a complex multi-session, multi-ticket-type architecture.

## Current API Structure Analysis

### 1. Entity Architecture

#### Current Event Entity (`src/WitchCityRope.Core/Entities/Event.cs`)

**✅ STRENGTHS - Can Be Reused:**
- Well-structured domain entity with proper encapsulation
- Good validation and business rules implementation
- Proper DateTime handling for UTC/PostgreSQL compatibility
- Solid audit trail (CreatedAt, UpdatedAt)
- Publishing workflow (IsPublished flag)
- Capacity management foundation
- Organizer relationship management

**❌ LIMITATIONS - Needs Major Updates:**
- **Single capacity model**: Only tracks `Capacity` at event level, no session-based capacity
- **Simple pricing**: Uses `IReadOnlyCollection<Money> PricingTiers` (sliding scale) vs multi-ticket types
- **No session support**: Missing entire session entity concept
- **Registration-centric**: Built around direct event registrations, not ticket purchases

#### Current Registration Entity (`src/WitchCityRope.Core/Entities/Registration.cs`)

**✅ STRENGTHS - Partially Reusable:**
- Good payment integration via Payment entity
- Proper status workflow (Pending → Confirmed → CheckedIn)
- Cancellation and refund logic
- Emergency contact and dietary restriction support
- Confirmation code generation

**❌ MAJOR ARCHITECTURAL MISMATCH:**
- **Direct Event relationship**: `EventId` directly links to Event, no ticket type concept
- **Single price selection**: `SelectedPrice` from sliding scale vs ticket type pricing
- **No session awareness**: Cannot handle multi-session events
- **RSVP vs Ticket confusion**: Acts as both RSVP and ticket purchase

#### Current Payment Entity (`src/WitchCityRope.Core/Entities/Payment.cs`)

**✅ EXCELLENT - Fully Reusable:**
- Robust payment status workflow
- Proper refund handling
- Transaction ID tracking
- Amount validation
- This entity can be used as-is with new ticket system

### 2. API Implementation Analysis

#### Current API Structure

**Old MVC Controller** (`apps/api/Controllers/EventsController.cs`):
- ❌ **ARCHIVED**: File explicitly states it's been migrated and is non-functional

**New Minimal API** (`apps/api/Features/Events/Endpoints/EventEndpoints.cs`):
- ✅ **Simple read endpoints**: GET /api/events, GET /api/events/{id}
- ✅ **Fallback data mechanism**: Provides hardcoded events when database is empty
- ✅ **Proper error handling**: Returns appropriate HTTP status codes
- ❌ **Read-only**: No CREATE, UPDATE, DELETE operations
- ❌ **Simple DTO**: Only basic event information, no session or ticket data

**EventService** (`apps/api/Features/Events/Services/EventService.cs`):
- ✅ **Good patterns**: Direct Entity Framework access, proper async/await
- ✅ **Performance optimized**: Uses AsNoTracking for read queries
- ✅ **Proper logging**: Structured logging with context
- ❌ **Limited functionality**: Only GetPublishedEventsAsync and GetEventAsync
- ❌ **Simple queries**: No complex capacity or availability calculations

**EventDto** (`apps/api/Features/Events/Models/EventDto.cs`):
- ✅ **Basic structure**: Id, Title, Description, StartDate, Location
- ❌ **Missing critical fields**: No EndDate, Capacity, EventType, Sessions, TicketTypes
- ❌ **No availability info**: Missing available spots, ticket pricing, session details

### 3. Database Configuration Analysis

#### Current Database Models

**New API Event Model** (`apps/api/Models/Event.cs`):
- ✅ **PostgreSQL optimized**: Proper UUID, TIMESTAMPTZ handling
- ✅ **Basic fields present**: Title, Description, StartDate, EndDate, Capacity
- ❌ **Simple pricing**: JSON string `PricingTiers` vs proper ticket type tables
- ❌ **No session support**: Single event model only

**Core Domain Event** (`src/WitchCityRope.Core/Entities/Event.cs`):
- ✅ **Rich domain model**: Full business logic implementation
- ✅ **Proper relationships**: Registration collection, Organizer collection
- ❌ **Single-session assumption**: No concept of multiple sessions within event

**ApplicationDbContext** (`apps/api/Data/ApplicationDbContext.cs`):
- ✅ **Well configured**: Proper PostgreSQL schema mapping, UTC handling
- ✅ **Identity integration**: Auth tables in separate schema
- ❌ **Missing tables**: No Session, TicketType, or Ticket tables
- ❌ **Simple Event mapping**: Only basic event fields configured

## Event Session Matrix Requirements Gap Analysis

### Missing Core Entities

**❌ EventSession Entity**: 
- Purpose: Atomic capacity units (S1, S2, S3)
- Fields: SessionName, SessionDate, StartTime, EndTime, Capacity
- Relationships: Belongs to Event, Referenced by TicketTypeSessionInclusions

**❌ EventTicketType Entity**:
- Purpose: Session bundles with pricing
- Fields: Name, Description, Price, MaxQuantity, SaleStartDate, SaleEndDate
- Relationships: Belongs to Event, References Sessions via junction table

**❌ TicketTypeSessionInclusions Junction Table**:
- Purpose: Defines which sessions each ticket type includes
- Critical for capacity calculations

**❌ Ticket Entity**:
- Purpose: Individual ticket purchases (replaces direct Registration)
- Fields: TicketTypeId, UserId, PurchasePrice, Status
- Relationships: References TicketType, User, Payment

### Missing Business Logic

**❌ Complex Availability Calculations**:
- Current: Simple `Event.GetAvailableSpots()` 
- Needed: Cross-session availability checking for ticket types

**❌ Multi-Session Capacity Management**:
- Current: Single event capacity check
- Needed: Per-session capacity validation with ticket type constraints

**❌ Ticket Type Pricing Logic**:
- Current: Sliding scale selection from fixed tiers
- Needed: Ticket type pricing with time-based discounts, member pricing

**❌ Purchase Workflow**:
- Current: Direct event registration
- Needed: Ticket type selection → Session validation → Purchase → Confirmation

### Migration Strategy Requirements

#### High-Impact Breaking Changes

1. **Registration → Ticket Paradigm Shift**
   - ❌ `Registration.EventId` becomes `Ticket.TicketTypeId`
   - ❌ `Registration.SelectedPrice` becomes `Ticket.PurchasePrice` (from TicketType.Price)
   - ❌ Registration status workflow changes to ticket status workflow

2. **Event Capacity Model**
   - ❌ `Event.Capacity` becomes aggregate of `EventSession.Capacity`
   - ❌ `Event.GetAvailableSpots()` needs complete rewrite
   - ❌ All availability checks need session-aware logic

3. **API Response Changes**
   - ❌ EventDto needs Sessions[] and TicketTypes[] arrays
   - ❌ New endpoints for ticket availability, purchase workflow
   - ❌ Real-time availability calculations for frontend

#### Database Migration Requirements

**New Tables Needed:**
```sql
-- Core Session Matrix Tables
CREATE TABLE EventSessions (...);
CREATE TABLE EventTicketTypes (...);
CREATE TABLE TicketTypeSessionInclusions (...);
CREATE TABLE Tickets (...);  -- Replaces direct Event→Registration

-- Migration Path
-- 1. Create new tables
-- 2. Migrate existing Event→Registration to Event→Session→TicketType→Ticket
-- 3. Update all business logic
-- 4. Drop old direct relationships
```

## Implementation Recommendations

### Phase 1: Foundation (Can Start Immediately)
1. **Create new entities**: EventSession, EventTicketType, TicketTypeSessionInclusions, Ticket
2. **Update ApplicationDbContext**: Add new DbSets and configurations
3. **Database migrations**: Create new tables alongside existing

### Phase 2: Business Logic (Major Effort)
1. **Rewrite availability calculations**: Session-aware capacity checking
2. **Update Event domain model**: Add Sessions collection, deprecate simple capacity
3. **Create ticket purchase workflow**: Replace registration workflow
4. **Update Payment integration**: Link to Ticket instead of Registration

### Phase 3: API Modernization (Breaking Changes)
1. **Expand EventDto**: Add Sessions and TicketTypes data
2. **New endpoints**: Ticket purchase, availability checking, session management
3. **Update EventService**: Complex query methods for availability
4. **Frontend integration**: Update React components for new data model

### Reusable Components

**✅ Keep As-Is:**
- `Payment` entity - perfect for new system
- `User/ApplicationUser` entities - no changes needed
- Authentication/Authorization patterns
- Database connection and UTC handling
- Logging and error handling patterns

**✅ Evolve/Extend:**
- `Event` entity - add Sessions collection, keep metadata
- `EventService` - extend with complex availability methods
- `ApplicationDbContext` - add new entities
- API endpoint patterns - proven vertical slice architecture

**❌ Replace/Rewrite:**
- `Registration` entity - becomes `Ticket` entity
- Simple capacity calculations
- Direct event registration workflow
- EventDto structure

## Risk Assessment

### HIGH RISK
- **Complete workflow change**: Registration → Ticket purchase flow
- **Complex availability logic**: Multi-session, multi-ticket-type calculations
- **Database relationships**: Junction tables and foreign key constraints
- **Frontend integration**: Major React component updates required

### MEDIUM RISK
- **Payment integration**: Should be seamless, but needs testing
- **User experience**: Complex ticket selection UI
- **Performance**: Complex availability queries

### LOW RISK
- **Authentication**: No changes needed
- **Basic CRUD operations**: Standard patterns apply
- **Database infrastructure**: PostgreSQL and EF Core handle complexity well

## Next Steps

1. **Read Event Session Matrix Implementation Guide**: Full technical specifications
2. **Create new entity models**: Start with domain entities
3. **Plan database migration strategy**: Gradual transition plan
4. **Prototype availability calculations**: Critical business logic first
5. **Design API contract**: EventDto with Sessions and TicketTypes

---

**CRITICAL NOTE**: This is a fundamental architectural change, not a simple feature addition. The Registration-based RSVP system becomes a Ticket-based purchase system with complex session dependencies. Plan for significant development time and thorough testing.