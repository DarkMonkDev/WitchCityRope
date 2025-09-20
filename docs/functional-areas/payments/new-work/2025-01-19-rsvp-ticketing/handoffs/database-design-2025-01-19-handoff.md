# Database Designer Handoff: RSVP and Ticketing System
<!-- Date: 2025-01-19 -->
<!-- Version: 1.0 -->
<!-- From: Database Designer Agent -->
<!-- To: Backend Developer Agent -->
<!-- Status: Complete -->

## Executive Summary

Database design phase for the RSVP and Ticketing System is **COMPLETE**. This handoff provides the backend developer with comprehensive database schema, Entity Framework models, and implementation guidance for building the participation tracking and payment processing system.

## Deliverables Completed

### 1. Complete Database Schema Design
**Location**: `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/database-design.md`

**Key Components**:
- **5 New Tables**: EventParticipations, TicketPurchases, ParticipationHistory, PaymentTransactions, enhanced TicketTypes
- **Comprehensive Indexes**: 15+ performance-optimized indexes including partial and GIN indexes
- **Business Rule Constraints**: Database-level validation for data integrity
- **Audit Trail System**: Complete change tracking with JSONB metadata
- **Capacity Management**: Real-time participation counting functions

### 2. Entity Framework Core Models
**Location**: `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/design/ef-core-models.md`

**Key Components**:
- **4 Entity Classes**: EventParticipation, TicketPurchase, ParticipationHistory, PaymentTransaction
- **3 Enumerations**: ParticipationType, ParticipationStatus, PaymentStatus
- **4 Configuration Classes**: Complete EF Core configurations with PostgreSQL optimizations
- **DbContext Integration**: ApplicationDbContext updates with UTC handling

## Critical Implementation Requirements

### 🚨 MANDATORY: Entity ID Pattern (CRITICAL)
**NEVER initialize entity IDs in properties**:
```csharp
// ✅ CORRECT - Simple property, EF handles generation
public class EventParticipation
{
    public Guid Id { get; set; }  // Let Entity Framework manage ID lifecycle
}

// ❌ NEVER DO THIS - Breaks EF Core ID generation
public class EventParticipation
{
    public Guid Id { get; set; } = Guid.NewGuid();  // THIS CAUSES UPDATE instead of INSERT!
}
```

### 🚨 MANDATORY: UTC DateTime Handling
**All DateTime properties MUST use UTC for PostgreSQL compatibility**:
```csharp
// ✅ CORRECT - Proper UTC handling
builder.Property(e => e.CreatedAt)
       .IsRequired()
       .HasColumnType("timestamptz");

// In entity constructor:
CreatedAt = DateTime.UtcNow;
```

### 🚨 MANDATORY: PostgreSQL Patterns
- **JSONB Configuration**: Use GIN indexes for metadata queries
- **Check Constraints**: Enforce business rules at database level
- **Partial Indexes**: Optimize for specific query patterns
- **snake_case Naming**: Follow PostgreSQL conventions

## Database Architecture Overview

### Core Entity Relationships
```
Events 1--N EventParticipations N--1 ApplicationUsers
EventParticipations 1--1 TicketPurchases N--1 TicketTypes
EventParticipations 1--N ParticipationHistory
TicketPurchases 1--N PaymentTransactions
```

### Business Logic Implementation
1. **Single Participation Rule**: One participation per user per event (unique constraint)
2. **RSVP vs Ticket**: ParticipationType enum distinguishes free RSVPs from paid tickets
3. **Status Tracking**: Active, Cancelled, Refunded, Waitlisted states with audit trail
4. **Payment Integration**: Links to existing PayPal webhook infrastructure
5. **Capacity Management**: Real-time counting functions for event capacity

## Migration Strategy

### Step 1: Create Migration
```bash
dotnet ef migrations add AddRsvpTicketingSystem --project apps/api --startup-project apps/api
```

### Step 2: Review Generated Migration
- Verify all constraints are properly named
- Check index creation statements
- Validate foreign key relationships
- Ensure UTC timestamptz columns

### Step 3: Apply to Development
```bash
dotnet ef database update --project apps/api --startup-project apps/api
```

### Step 4: Generate Production Script
```bash
dotnet ef migrations script --project apps/api --startup-project apps/api --idempotent --output migration-scripts/rsvp-ticketing-system.sql
```

## Performance Optimization

### Critical Indexes Implemented
```sql
-- Real-time capacity queries
CREATE INDEX "IX_EventParticipations_EventId_Status"
    ON "EventParticipations"("EventId", "Status") WHERE "Status" = 1;

-- User dashboard queries
CREATE INDEX "IX_EventParticipations_UserId_Status"
    ON "EventParticipations"("UserId", "Status") WHERE "Status" = 1;

-- Payment processing queries
CREATE INDEX "IX_TicketPurchases_PaymentId"
    ON "TicketPurchases"("PaymentId");

-- JSONB metadata queries
CREATE INDEX "IX_EventParticipations_Metadata_Gin"
    ON "EventParticipations" USING GIN ("Metadata");
```

### Expected Query Performance
- Participation Status Check: < 50ms
- Event Capacity Check: < 100ms
- User Dashboard Load: < 200ms
- Admin Participant List: < 500ms

## Security & Compliance

### PCI Compliance Requirements
- **NO CARD DATA**: Only store PayPal order IDs, never credit card numbers
- **Encrypted Storage**: PayPal tokens only, application-level encryption required
- **Audit Trails**: Complete change tracking in ParticipationHistory table
- **Access Control**: Role-based access to participation data

### Data Protection
- **Personal Data**: Participation notes may contain sensitive information
- **Payment Data**: Links to PayPal infrastructure only
- **Audit Logging**: Complete lifecycle tracking for compliance

## Integration Points

### Existing System Integration
1. **PayPal Webhooks**: Links to operational webhook processing infrastructure
2. **Event System**: Integrates with existing Events table and capacity tracking
3. **User System**: Uses ApplicationUser from Identity system
4. **Check-in System**: Complements existing EventAttendees table

### Data Flow Coordination
- **EventParticipations**: Registration and payment tracking
- **EventAttendees**: Check-in and attendance tracking
- **Shared User/Event References**: Coordinated through foreign keys

## Testing Requirements

### Database Testing Patterns
1. **Use TestContainers**: Real PostgreSQL testing, not in-memory provider
2. **UTC DateTime Testing**: Verify all timestamps are UTC compatible
3. **Constraint Testing**: Validate business rules at database level
4. **Performance Testing**: Verify index effectiveness under load

### Critical Test Scenarios
- Unique participation constraint enforcement
- UTC DateTime handling in PostgreSQL
- JSONB metadata storage and querying
- PayPal payment ID tracking
- Audit trail generation

## Backend Development Guidance

### Entity Framework Service Patterns
```csharp
// ✅ CORRECT - Efficient participation queries
var participation = await _context.EventParticipations
    .AsNoTracking()
    .Include(ep => ep.TicketPurchase)
    .FirstOrDefaultAsync(ep => ep.EventId == eventId && ep.UserId == userId && ep.Status == ParticipationStatus.Active);

// ✅ CORRECT - Capacity checking
var currentCount = await _context.EventParticipations
    .CountAsync(ep => ep.EventId == eventId && ep.Status == ParticipationStatus.Active);
```

### Business Logic Implementation
1. **Participation Creation**: Validate capacity before creating
2. **Payment Processing**: Link to PayPal webhook handlers
3. **Cancellation Logic**: Update status and track in audit trail
4. **Refund Processing**: Coordinate with PayPal refund system

## Dependencies and Prerequisites

### Required for Backend Implementation
1. **Event Entity**: Existing Event model (already implemented)
2. **ApplicationUser**: Identity user system (already implemented)
3. **TicketType Entity**: Enhanced with new fields
4. **PayPal Infrastructure**: Operational webhook processing (already implemented)

### Backend Developer Must Read
1. **Entity Framework Patterns**: `/docs/standards-processes/development-standards/entity-framework-patterns.md`
2. **Database Developer Lessons**: `/docs/lessons-learned/database-designer-lessons-learned.md`
3. **Functional Specification**: `/docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/requirements/functional-specification.md`

## Next Phase Requirements

### Backend Developer Tasks
1. **Implement Entity Models**: Copy entity classes from design document
2. **Create EF Configurations**: Implement the 4 configuration classes
3. **Generate Migration**: Create and review EF Core migration
4. **Create Service Layer**: Implement participation and payment services
5. **Integrate PayPal Webhooks**: Link to existing webhook processing
6. **Implement Business Logic**: RSVP creation, ticket purchase, cancellation flows

### Success Criteria for Backend Phase
- [ ] All entity models implemented with correct ID patterns
- [ ] EF Core configurations applied with PostgreSQL optimizations
- [ ] Migration successfully applied to development database
- [ ] Service layer created with proper business logic
- [ ] Integration tests passing with real PostgreSQL
- [ ] PayPal webhook integration functional

## Risk Mitigation

### Critical Patterns to Follow
1. **No ID Initializers**: Use simple `public Guid Id { get; set; }` pattern
2. **UTC DateTime**: All timestamps must be UTC for PostgreSQL
3. **Constraint Naming**: Use explicit constraint names for PostgreSQL
4. **JSONB Handling**: Proper configuration for metadata fields
5. **Performance Indexes**: Implement all specified indexes

### Common Pitfalls to Avoid
- Entity ID initialization causing EF Core update issues
- Non-UTC DateTime causing PostgreSQL compatibility errors
- Missing indexes causing performance degradation
- Improper JSONB configuration breaking metadata queries
- Business rule violations due to missing constraints

## Contact and Support

For questions or clarifications on database design:
- Review database design document for complete specifications
- Check EF Core models document for implementation patterns
- Reference existing PayPal integration for webhook patterns
- Follow established PostgreSQL patterns in lessons learned

## Status: Ready for Backend Implementation

Database design phase is **COMPLETE** and ready for backend developer implementation. All deliverables are comprehensive and include:
- Complete schema with 15+ optimized indexes
- 4 entity models with proper EF Core patterns
- PostgreSQL-specific optimizations
- PCI-compliant payment data handling
- Comprehensive audit trail system
- Integration with existing PayPal infrastructure

**Next Agent**: Backend Developer Agent for service layer implementation.