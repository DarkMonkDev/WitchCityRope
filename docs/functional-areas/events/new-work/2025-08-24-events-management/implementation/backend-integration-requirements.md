# Backend Integration Requirements: Events Management System
<!-- Last Updated: 2025-08-25 -->
<!-- Version: 1.0 -->
<!-- Author: Git Manager Agent -->
<!-- Status: Ready for API Development -->

## Executive Summary

This document specifies the backend API integration requirements for the Events Management System, translating the business requirements and UI wireframes into comprehensive REST API specifications. The system implements an Event Session Matrix architecture with 20+ endpoints across 4 functional areas, complete data models for NSwag auto-generation, and production-ready security and compliance requirements.

## API Architecture Overview

### Core Architectural Patterns

**1. Vertical Slice Architecture**
- Each functional area (Events, Sessions, TicketTypes, Tickets) has dedicated endpoint grouping
- Consistent error handling and response patterns across all endpoints
- Direct Entity Framework integration following established WitchCityRope patterns

**2. Event Session Matrix Model**
```
Event (1) → EventSessions (N) ← TicketTypeSessionInclusions → EventTicketTypes (N) ← Tickets (N)
```
- Events contain multiple sessions (S1, S2, S3 format)
- Ticket types reference multiple sessions via junction table
- Complex availability calculations based on session-specific capacity
- Real-time capacity validation for ticket purchases

**3. RESTful API Standards**
- HTTP verbs: GET (read), POST (create), PUT (update), DELETE (remove)
- Consistent status codes: 200 (success), 201 (created), 400 (validation), 404 (not found), 409 (conflict)
- JSON request/response format with standardized error responses
- UTC timestamps in ISO 8601 format for all DateTime fields

## REST API Endpoint Specifications

### 1. Events Management Endpoints (6 endpoints)

#### `GET /api/events`
**Purpose**: List all published events with basic information
**Response**: `List<EventSummaryDto>`
**Query Parameters**:
- `eventType` (optional): Filter by "Class" or "SocialEvent"
- `showPast` (optional): Include past events (default: false)
- `organizerId` (optional): Filter by specific organizer

```json
{
  "events": [
    {
      "eventId": "uuid",
      "title": "Advanced Rope Workshop",
      "shortDescription": "Learn advanced techniques...",
      "eventType": "Class",
      "startDate": "2025-09-15T18:00:00Z",
      "endDate": "2025-09-15T21:00:00Z",
      "venue": "Main Studio",
      "totalCapacity": 20,
      "availableSpots": 5,
      "hasMultipleSessions": true,
      "sessionCount": 2,
      "lowestPrice": 45.00,
      "highestPrice": 75.00
    }
  ]
}
```

#### `GET /api/events/{eventId}`
**Purpose**: Get complete event details including sessions and ticket types
**Response**: `EventDetailsDto`
**Includes**: Sessions array, TicketTypes array, organizers, venue details

```json
{
  "eventId": "uuid",
  "title": "3-Day Advanced Workshop Series",
  "shortDescription": "Brief description",
  "fullDescription": "Complete HTML description",
  "eventType": "Class",
  "venue": {
    "name": "Main Studio",
    "address": "123 Salem St",
    "capacity": 25
  },
  "organizers": [
    {
      "userId": "uuid",
      "displayName": "Organizer Name",
      "role": "Primary"
    }
  ],
  "sessions": [
    {
      "sessionId": "S1",
      "sessionName": "Foundation Day",
      "sessionDate": "2025-09-15T18:00:00Z",
      "startTime": "18:00:00",
      "endTime": "21:00:00",
      "capacity": 20,
      "currentRegistrations": 15,
      "availableSpots": 5
    },
    {
      "sessionId": "S2",
      "sessionName": "Advanced Techniques",
      "sessionDate": "2025-09-16T18:00:00Z",
      "startTime": "18:00:00",
      "endTime": "21:00:00",
      "capacity": 20,
      "currentRegistrations": 12,
      "availableSpots": 8
    }
  ],
  "ticketTypes": [
    {
      "ticketTypeId": "uuid",
      "name": "Full Series",
      "description": "Access to all workshop sessions",
      "includedSessions": ["S1", "S2"],
      "price": 120.00,
      "memberPrice": 100.00,
      "maxQuantity": 20,
      "availableQuantity": 5,
      "salesEndDate": "2025-09-15T12:00:00Z",
      "isAvailable": true,
      "constraintReason": null
    },
    {
      "ticketTypeId": "uuid",
      "name": "Day 1 Only",
      "description": "Foundation Day session only",
      "includedSessions": ["S1"],
      "price": 65.00,
      "memberPrice": 55.00,
      "maxQuantity": 20,
      "availableQuantity": 5,
      "salesEndDate": "2025-09-15T12:00:00Z",
      "isAvailable": true,
      "constraintReason": null
    }
  ]
}
```

#### `POST /api/events`
**Purpose**: Create new event (Event Organizers only)
**Request**: `CreateEventRequest`
**Response**: `EventDetailsDto`
**Authorization**: Role-based (EventOrganizer, Admin)

#### `PUT /api/events/{eventId}`
**Purpose**: Update event details (Event Organizers only)
**Request**: `UpdateEventRequest`
**Response**: `EventDetailsDto`
**Authorization**: Role-based (EventOrganizer, Admin)

#### `DELETE /api/events/{eventId}`
**Purpose**: Delete event and all related data
**Response**: `204 No Content`
**Authorization**: Role-based (EventOrganizer, Admin)
**Business Rules**: Cannot delete events with confirmed registrations

#### `GET /api/events/{eventId}/availability`
**Purpose**: Real-time availability calculation for ticket types
**Response**: `AvailabilityDto`
**Use Case**: Frontend polling for live availability updates

### 2. Event Sessions Management Endpoints (5 endpoints)

#### `GET /api/events/{eventId}/sessions`
**Purpose**: List all sessions for specific event
**Response**: `List<EventSessionDto>`

#### `POST /api/events/{eventId}/sessions`
**Purpose**: Add new session to event
**Request**: `CreateSessionRequest`
**Response**: `EventSessionDto`
**Business Rules**: Auto-generates next S# ID (S1, S2, S3, etc.)

#### `PUT /api/events/{eventId}/sessions/{sessionId}`
**Purpose**: Update session details
**Request**: `UpdateSessionRequest`
**Response**: `EventSessionDto`
**Business Rules**: Cannot reduce capacity below current registrations

#### `DELETE /api/events/{eventId}/sessions/{sessionId}`
**Purpose**: Delete session
**Response**: `204 No Content`
**Business Rules**: Cannot delete sessions with confirmed registrations or referenced by ticket types

#### `GET /api/sessions/{sessionId}/registrations`
**Purpose**: Get all registrations for specific session
**Response**: `List<SessionRegistrationDto>`
**Authorization**: Event Organizers and Admins only

### 3. Ticket Types Management Endpoints (5 endpoints)

#### `GET /api/events/{eventId}/ticket-types`
**Purpose**: List all ticket types for event with availability
**Response**: `List<TicketTypeDto>`

#### `POST /api/events/{eventId}/ticket-types`
**Purpose**: Create new ticket type
**Request**: `CreateTicketTypeRequest`
**Response**: `TicketTypeDto`

#### `PUT /api/events/{eventId}/ticket-types/{ticketTypeId}`
**Purpose**: Update ticket type details
**Request**: `UpdateTicketTypeRequest`
**Response**: `TicketTypeDto`

#### `DELETE /api/events/{eventId}/ticket-types/{ticketTypeId}`
**Purpose**: Delete ticket type
**Response**: `204 No Content`
**Business Rules**: Cannot delete ticket types with sold tickets

#### `GET /api/ticket-types/{ticketTypeId}/availability`
**Purpose**: Check real-time availability for specific ticket type
**Response**: `TicketTypeAvailabilityDto`
**Includes**: Session-specific constraints and availability calculations

### 4. Ticket Purchase and Management Endpoints (4 endpoints)

#### `POST /api/events/{eventId}/tickets/purchase`
**Purpose**: Purchase tickets for event
**Request**: `PurchaseTicketsRequest`
**Response**: `PurchaseConfirmationDto`
**Business Rules**: 
- Validates session availability for all included sessions
- Creates Payment record
- Sends confirmation email
- Atomically updates capacity counts

```json
{
  "ticketTypeId": "uuid",
  "quantity": 2,
  "attendees": [
    {
      "name": "John Doe",
      "email": "john@example.com",
      "dietaryRestrictions": "Vegetarian",
      "emergencyContact": "Jane Doe (555-1234)"
    }
  ],
  "paymentMethod": "CreditCard",
  "paymentToken": "payment-processor-token"
}
```

#### `GET /api/users/{userId}/tickets`
**Purpose**: List user's purchased tickets
**Response**: `List<UserTicketDto>`
**Authorization**: User can view own tickets, organizers can view all

#### `PUT /api/tickets/{ticketId}/cancel`
**Purpose**: Cancel ticket and request refund
**Request**: `CancelTicketRequest`
**Response**: `CancellationDto`
**Business Rules**: Refund rules based on event policies

#### `PUT /api/tickets/{ticketId}/checkin`
**Purpose**: Check in ticket holder at event
**Request**: `CheckInRequest`
**Response**: `CheckInDto`
**Authorization**: Kiosk mode or Event Organizers

## Data Models for NSwag Generation

### Core Event Models

```csharp
public class EventDetailsDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; }
    public string ShortDescription { get; set; }
    public string FullDescription { get; set; }
    public EventType EventType { get; set; } // Enum: Class, SocialEvent
    public string PoliciesProcedures { get; set; }
    public VenueDto Venue { get; set; }
    public List<EventOrganizerDto> Organizers { get; set; }
    public List<EventSessionDto> Sessions { get; set; }
    public List<TicketTypeDto> TicketTypes { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class EventSessionDto
{
    public string SessionId { get; set; } // S1, S2, S3 format
    public string SessionName { get; set; }
    public DateTime SessionDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int Capacity { get; set; }
    public int CurrentRegistrations { get; set; }
    public int AvailableSpots { get; set; }
    public bool IsActive { get; set; }
}

public class TicketTypeDto
{
    public Guid TicketTypeId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> IncludedSessions { get; set; } // ["S1", "S2"]
    public decimal Price { get; set; }
    public decimal? MemberPrice { get; set; }
    public int MaxQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTime? SalesStartDate { get; set; }
    public DateTime SalesEndDate { get; set; }
    public bool IsAvailable { get; set; }
    public string? ConstraintReason { get; set; }
}
```

### Request/Response Models

```csharp
public class CreateEventRequest
{
    [Required, StringLength(200)]
    public string Title { get; set; }
    
    [Required, StringLength(500)]
    public string ShortDescription { get; set; }
    
    public string FullDescription { get; set; }
    
    [Required]
    public EventType EventType { get; set; }
    
    public string PoliciesProcedures { get; set; }
    
    [Required]
    public Guid VenueId { get; set; }
    
    public List<Guid> OrganizerIds { get; set; }
}

public class PurchaseTicketsRequest
{
    [Required]
    public Guid TicketTypeId { get; set; }
    
    [Range(1, 10)]
    public int Quantity { get; set; }
    
    [Required]
    public List<AttendeeDto> Attendees { get; set; }
    
    [Required]
    public string PaymentMethod { get; set; }
    
    public string PaymentToken { get; set; }
}

public class AttendeeDto
{
    [Required, StringLength(100)]
    public string Name { get; set; }
    
    [Required, EmailAddress]
    public string Email { get; set; }
    
    public string DietaryRestrictions { get; set; }
    
    public string EmergencyContact { get; set; }
}
```

### Complex Availability Models

```csharp
public class AvailabilityDto
{
    public Guid EventId { get; set; }
    public List<SessionAvailabilityDto> Sessions { get; set; }
    public List<TicketTypeAvailabilityDto> TicketTypes { get; set; }
    public DateTime LastCalculated { get; set; }
}

public class SessionAvailabilityDto
{
    public string SessionId { get; set; }
    public int Capacity { get; set; }
    public int Sold { get; set; }
    public int Available { get; set; }
    public bool HasWaitlist { get; set; }
    public int WaitlistCount { get; set; }
}

public class TicketTypeAvailabilityDto
{
    public Guid TicketTypeId { get; set; }
    public bool IsAvailable { get; set; }
    public int MaxPurchasable { get; set; }
    public string ConstraintReason { get; set; }
    public List<string> LimitingSessionIds { get; set; }
}
```

## Business Logic Requirements

### 1. Complex Availability Calculations

**Session-Based Capacity Tracking**:
```csharp
public class AvailabilityCalculationService
{
    public async Task<int> CalculateAvailableQuantityForTicketType(Guid ticketTypeId)
    {
        // 1. Get ticket type and included sessions
        // 2. For each session, calculate remaining capacity
        // 3. Return minimum available across all sessions
        // 4. Consider existing ticket sales and reservations
    }
    
    public async Task<bool> ValidatePurchaseCapacity(Guid ticketTypeId, int quantity)
    {
        // Atomic validation to prevent overselling
        // Must be called within transaction scope
    }
}
```

### 2. Multi-Session Purchase Workflow

**Purchase Transaction Requirements**:
1. **Begin database transaction**
2. **Validate availability** for all included sessions
3. **Reserve capacity** (temporary hold)
4. **Process payment** via external processor
5. **Confirm tickets** and update capacity
6. **Send confirmation email**
7. **Commit transaction** or rollback on failure

### 3. Real-Time Availability Updates

**Performance Requirements**:
- Availability calculations must complete in <200ms
- Support concurrent purchase attempts without overselling
- Cache expensive calculations with 30-second TTL
- Use database-level constraints for atomicity

## Security and Authorization Requirements

### 1. Role-Based Access Control

**Event Organizer Permissions**:
- Full CRUD access to all events
- Can create/modify sessions and ticket types
- Access to registration and financial data
- Can generate kiosk mode tokens

**Teacher Permissions**:
- Read-only access to assigned events
- Cannot modify event details
- Contact Event Organizers for changes

**Member Permissions**:
- View published events based on vetting level
- Purchase tickets for appropriate events
- View own registration history
- Cancel registrations within refund window

**Admin Permissions**:
- All Event Organizer permissions
- System administration capabilities
- User role management

### 2. Kiosk Mode Security

**Token-Based Authentication**:
```csharp
public class KioskTokenDto
{
    public string Token { get; set; }
    public string StationName { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid EventId { get; set; }
    public string EventCode { get; set; }
}
```

**Security Requirements**:
- Cryptographically secure token generation (32-byte random)
- Time-bound expiration (4-8 hours maximum)
- Device binding through browser fingerprinting
- Limited scope (check-in operations only)
- Audit logging for all actions
- Immediate revocation capability

### 3. Payment Security

**PCI Compliance Requirements**:
- **ZERO credit card data storage** in WitchCityRope database
- All payment processing via external PCI-compliant processors
- Payment tokens stored only for refund processing
- Immediate disposal of sensitive payment data
- Encrypted transmission of payment information
- Audit trail for all financial transactions

### 4. Data Protection

**Privacy Requirements**:
- Member contact information limited to Event Organizers and Admins
- Anonymization options for sensitive events
- GDPR compliance for data retention and deletion
- Secure session management for kiosk mode
- Activity logging for compliance audits

## Performance Requirements

### 1. Response Time Targets

- **Event List (GET /api/events)**: < 100ms
- **Event Details (GET /api/events/{id})**: < 200ms
- **Availability Check**: < 150ms
- **Ticket Purchase**: < 500ms (excluding payment processor time)
- **Complex Availability Calculation**: < 200ms

### 2. Capacity Management

**Concurrent Purchase Handling**:
- Support 50+ concurrent users purchasing tickets
- Prevent overselling through database constraints
- Graceful handling of capacity conflicts
- Real-time availability updates during peak sales

**Database Optimization**:
- Proper indexing on EventId, SessionId, TicketTypeId
- Efficient queries using `AsNoTracking()` for read operations
- Connection pooling optimized for concurrent access
- Query optimization for complex availability calculations

### 3. Caching Strategy

**Cache Layers**:
- Event details: 5-minute TTL (updates rare)
- Availability calculations: 30-second TTL (updates frequent)
- Static data (venues, organizers): 15-minute TTL
- Session-specific data: 1-minute TTL during active sales

## Integration Points

### 1. Authentication System Integration

**Existing Auth Requirements**:
- JWT token validation for all protected endpoints
- Role-based authorization using existing user roles
- Session management for kiosk mode
- Integration with existing ApplicationUser entity

### 2. Payment Processor Integration

**External Payment Processing**:
- Stripe or similar PCI-compliant processor
- Webhook handling for payment confirmation
- Refund processing automation
- Payment status synchronization

### 3. Email System Integration

**Notification Requirements**:
- Confirmation emails for ticket purchases
- Reminder emails before events
- Cancellation and refund notifications
- Session-specific email targeting (S1, S2, S3)

### 4. Frontend Integration Points

**React Component Requirements**:
- Event listing with real-time availability
- Complex ticket type selection interface
- Session-aware purchase workflow
- Admin dashboard for event management
- Kiosk mode interface for check-ins

## Error Handling and Validation

### 1. Validation Rules

**Event Creation Validation**:
- Title: Required, 3-200 characters
- Dates: StartDate before EndDate, future dates only
- Capacity: Minimum 1, maximum 500 per session
- Organizer: Must be valid Event Organizer role

**Ticket Purchase Validation**:
- Availability: Real-time capacity checking
- Payment: Valid payment method and token
- User eligibility: Vetting level requirements
- Quantity limits: Maximum 10 tickets per purchase

### 2. Error Response Format

**Standardized Error Response**:
```json
{
  "error": {
    "code": "INSUFFICIENT_CAPACITY",
    "message": "Not enough spots available for selected ticket type",
    "details": {
      "ticketTypeId": "uuid",
      "requestedQuantity": 3,
      "availableQuantity": 1,
      "limitingSessionId": "S2"
    },
    "timestamp": "2025-08-25T10:30:00Z"
  }
}
```

### 3. Business Rule Violations

**Common Conflict Scenarios**:
- Insufficient session capacity for ticket type
- Sales period expired for ticket type
- User lacks required vetting level
- Payment processing failure
- Concurrent purchase conflicts

## Testing Requirements

### 1. Unit Test Coverage

**Required Test Categories**:
- Availability calculation accuracy
- Session capacity constraint validation
- Ticket purchase workflow edge cases
- Role-based authorization scenarios
- Payment integration mocking

### 2. Integration Test Requirements

**TestContainers Integration**:
- Real PostgreSQL testing (NO in-memory database)
- Complete purchase workflow testing
- Concurrent purchase conflict testing
- Session capacity boundary testing
- Complex availability calculation validation

### 3. Performance Testing

**Load Testing Scenarios**:
- 100 concurrent users browsing events
- 50 simultaneous ticket purchases
- Peak availability checking load
- Database constraint stress testing

## Migration and Deployment

### 1. Database Migration Strategy

**Phased Migration Approach**:
1. Create new tables alongside existing Registration table
2. Implement new API endpoints while maintaining backward compatibility
3. Migrate existing registrations to new ticket system
4. Update frontend to use new endpoints
5. Deprecate old Registration-based system

### 2. Backward Compatibility

**Transition Period Requirements**:
- Maintain existing `/api/events` endpoint during migration
- Gradual rollout of new functionality
- Data integrity validation during transition
- Rollback capability if issues arise

### 3. Production Deployment

**Deployment Checklist**:
- Database migrations tested in staging environment
- Payment processor integration validated
- Security audit completed
- Performance benchmarks met
- Monitoring and alerting configured
- Backup and recovery procedures tested

## Success Metrics and Monitoring

### 1. Technical Metrics

**Performance Monitoring**:
- API response times by endpoint
- Database query performance
- Payment processing success rate
- Concurrent user capacity
- Error rates by endpoint

### 2. Business Metrics

**Operational Success**:
- Event creation efficiency (time to publish)
- Ticket sales conversion rate
- Registration accuracy (no overselling incidents)
- User satisfaction with purchase process
- Administrative time savings

### 3. Monitoring and Alerting

**Critical Alerts**:
- Payment processing failures
- Overselling incidents (should never occur)
- API response time degradation
- Database connectivity issues
- Security token violations

## Implementation Readiness

### Quality Gate Checklist

- [x] **API Contract Complete**: All 20+ endpoints specified with request/response models
- [x] **Data Models Defined**: Complete DTOs for NSwag auto-generation
- [x] **Business Logic Documented**: Complex availability calculations specified
- [x] **Security Requirements**: PCI compliance, kiosk mode, role-based access
- [x] **Performance Targets**: Response time and concurrency requirements defined
- [x] **Integration Points**: Authentication, payment, email, frontend specified
- [x] **Error Handling**: Standardized error responses and validation rules
- [x] **Testing Strategy**: Unit, integration, and performance test requirements
- [x] **Migration Plan**: Database changes and deployment strategy documented
- [x] **Success Metrics**: Technical and business monitoring requirements

### Next Phase Deliverables

1. **Entity Models**: EventSession, EventTicketType, TicketTypeSessionInclusions, Ticket entities
2. **Database Migrations**: Create new tables with proper relationships and constraints
3. **Business Logic Services**: AvailabilityCalculationService, TicketPurchaseService
4. **API Implementation**: All 20+ endpoints with full business logic
5. **Security Implementation**: Kiosk mode tokens, role-based authorization
6. **Payment Integration**: External processor integration with webhook handling
7. **Test Suite**: Comprehensive test coverage with TestContainers
8. **Frontend Integration**: Updated React components for new API structure

---

**Status**: Backend Integration Requirements Complete - Ready for API Development Phase  
**Estimated Development Time**: 6-8 weeks for complete implementation  
**Risk Level**: High (architectural changes) - Medium (individual endpoints)  
**Dependencies**: Database migration approval, payment processor selection, frontend coordination