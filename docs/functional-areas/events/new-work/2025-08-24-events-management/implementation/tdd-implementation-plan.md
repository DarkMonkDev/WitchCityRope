# TDD Implementation Plan: Events Management System
<!-- Last Updated: 2025-09-06 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Implementation Ready -->

## Executive Summary

This detailed Test-Driven Development (TDD) implementation plan completes the Events Management System by leveraging existing research, wireframes, and API code. The plan follows a TDD-first approach where tests are written before implementation, ensuring clear requirements and preventing feature drift.

**Key Context**: We are building upon substantial existing work:
- ✅ Complete wireframes and designs exist
- ✅ Backend API has 3 working GET endpoints
- ✅ Frontend integration demo is functional
- ✅ Business requirements and functional specs complete
- ✅ TDD test infrastructure already created (50 tests)

## Current State Analysis

### What We Have (Assets to Leverage)

#### Backend API (Partially Complete)
- ✅ **EventsManagementService**: Working GET endpoints
- ✅ **Event, EventDto**: Basic entity and data transfer objects
- ✅ **Database Integration**: PostgreSQL with proper UTC handling
- ✅ **Unit Tests**: Test infrastructure established
- ❌ **Missing**: CREATE/UPDATE/DELETE endpoints, Session/TicketType entities

#### Frontend Integration (Demo Ready)
- ✅ **TypeScript Types**: Generated from C# DTOs via NSwag
- ✅ **API Service**: TanStack Query integration working
- ✅ **Demo Page**: http://localhost:5173/admin/events-management-api-demo
- ❌ **Missing**: Full UI components, admin dashboard, public pages

#### Design System (Complete)
- ✅ **Wireframes**: Complete admin, public, and check-in interfaces
- ✅ **Event Session Matrix**: Architecture fully documented
- ✅ **Business Requirements**: All user stories and acceptance criteria
- ✅ **Functional Specifications**: Technical implementation details

#### Test Infrastructure (TDD Ready)
- ✅ **50 TDD Tests Created**: All written first, currently failing
- ✅ **Backend Unit Tests**: 22 tests for Event Session Matrix logic
- ✅ **React Component Tests**: 20 tests for UI components
- ✅ **Integration Tests**: 8 tests for end-to-end workflows

### Missing Implementation (Focus Areas)

#### Backend Missing Pieces
1. **Event Session Matrix Entities**: EventSession, EventTicketType, TicketTypeSessionInclusions
2. **CRUD API Endpoints**: POST/PUT/DELETE for events, sessions, tickets
3. **Complex Availability Calculations**: Multi-session capacity logic
4. **RSVP vs Ticket System**: Social event vs class event handling
5. **Check-in System**: Kiosk mode and attendee check-in

#### Frontend Missing Pieces
1. **Admin Dashboard**: Events list with filters and actions
2. **Event Form**: Tabbed interface (Basic Info, Tickets/Orders, Emails, Volunteers)
3. **Public Events Page**: Card/list view with filtering
4. **Event Details Page**: Registration interface with ticket selection
5. **Check-in Interface**: Kiosk mode for event check-in

## Phase-Based Implementation Plan

### Phase 1: Foundation & Route Setup (Est: 4-6 hours)
**Objective**: Establish basic navigation and authentication for events system

#### Tests to Write First (TDD)
```typescript
// /apps/web/src/pages/events/__tests__/routes.test.tsx
describe('Events Routes', () => {
  it('should navigate to public events page', () => {});
  it('should require admin role for admin dashboard', () => {});
  it('should redirect unauthenticated users appropriately', () => {});
});
```

#### Implementation Tasks
1. **Route Configuration**
   - Add `/events` route (public events list)
   - Add `/admin/events` route (admin dashboard)
   - Add `/admin/events/create` and `/admin/events/{id}/edit` routes
   - Add authentication guards

2. **Basic Page Components** (Stub Implementation)
   ```typescript
   // /apps/web/src/pages/events/PublicEventsPage.tsx
   // /apps/web/src/pages/events/AdminDashboardPage.tsx
   // /apps/web/src/pages/events/EventFormPage.tsx
   ```

3. **Navigation Integration**
   - Update main navigation to include Events
   - Add admin navigation links
   - Test navigation with existing test accounts

#### Success Criteria
- [ ] All routes accessible and render basic components
- [ ] Authentication working with test accounts
- [ ] Navigation tests passing
- [ ] No TypeScript compilation errors

#### Questions for User
1. Should public events be accessible without login?
2. Which roles should have access to admin events dashboard?
3. Should we display different navigation based on user role?

### Phase 2: Event Session Matrix Backend (Est: 12-16 hours)
**Objective**: Implement core Event Session Matrix entities and CRUD operations

#### Tests Already Written (50 TDD Tests)
All tests exist in:
- `/tests/WitchCityRope.Api.Tests/Features/Events/EventSessionTests.cs` (22 tests)
- `/tests/integration/events/EventSessionMatrixIntegrationTests.cs` (8 tests)

#### Implementation Tasks

1. **Core Entities** (Based on existing tests)
   ```csharp
   // /src/WitchCityRope.Core/Entities/EventSession.cs
   public class EventSession
   {
       public Guid Id { get; private set; }
       public Guid EventId { get; private set; }
       public int SessionNumber { get; private set; } // 1, 2, 3 for S1, S2, S3
       public string Name { get; private set; }
       public DateTime StartDateTime { get; private set; }
       public DateTime EndDateTime { get; private set; }
       public int Capacity { get; private set; }
       // Business logic methods per test specs
   }

   // /src/WitchCityRope.Core/Entities/EventTicketType.cs
   public class EventTicketType
   {
       public Guid Id { get; private set; }
       public Guid EventId { get; private set; }
       public string Name { get; private set; }
       public string Type { get; private set; } // 'Single' | 'Couples'
       public decimal MinPrice { get; private set; }
       public decimal MaxPrice { get; private set; }
       public int Quantity { get; private set; }
       public DateTime SalesEndDate { get; private set; }
       public IReadOnlyList<Guid> SessionIds { get; private set; }
       // Capacity calculation methods per test specs
   }
   ```

2. **Database Schema Updates**
   ```sql
   -- New migration: Add Event Session Matrix tables
   CREATE TABLE EventSessions (
       Id UUID PRIMARY KEY,
       EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
       SessionNumber INTEGER NOT NULL,
       Name VARCHAR(100) NOT NULL,
       StartDateTime TIMESTAMPTZ NOT NULL,
       EndDateTime TIMESTAMPTZ NOT NULL,
       Capacity INTEGER NOT NULL CHECK (Capacity > 0),
       CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
   );

   CREATE TABLE EventTicketTypes (
       Id UUID PRIMARY KEY,
       EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
       Name VARCHAR(100) NOT NULL,
       Type VARCHAR(20) NOT NULL CHECK (Type IN ('Single', 'Couples')),
       MinPrice DECIMAL(10,2) NOT NULL,
       MaxPrice DECIMAL(10,2) NOT NULL,
       Quantity INTEGER NOT NULL,
       SalesEndDate DATE NOT NULL,
       IsActive BOOLEAN NOT NULL DEFAULT TRUE,
       CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW()
   );

   CREATE TABLE TicketTypeSessionInclusions (
       Id UUID PRIMARY KEY,
       TicketTypeId UUID NOT NULL REFERENCES EventTicketTypes(Id) ON DELETE CASCADE,
       SessionId UUID NOT NULL REFERENCES EventSessions(Id) ON DELETE CASCADE,
       UNIQUE(TicketTypeId, SessionId)
   );
   ```

3. **API Endpoints** (Make failing tests pass)
   ```csharp
   // /apps/api/Features/Events/Endpoints/EventManagementEndpoints.cs
   POST /api/events - Create event with sessions and ticket types
   PUT /api/events/{id} - Update event
   DELETE /api/events/{id} - Delete event
   
   POST /api/events/{id}/sessions - Add session to event
   PUT /api/sessions/{id} - Update session
   DELETE /api/sessions/{id} - Delete session
   
   POST /api/events/{id}/ticket-types - Create ticket type
   PUT /api/ticket-types/{id} - Update ticket type
   DELETE /api/ticket-types/{id} - Delete ticket type
   ```

4. **Complex Availability Calculations** (Per TDD tests)
   ```csharp
   // /apps/api/Features/Events/Services/AvailabilityCalculationService.cs
   public class AvailabilityCalculationService
   {
       // Implement per existing test specifications:
       // - CalculateAvailability_MultiSessionTicket_ReturnsLimitingSessionCapacity()
       // - CalculateAvailability_WithExistingRegistrations_ConsumesFromAllSessions()
       // - ComplexWorkshop_ThreeDayEvent_SupportsAllTicketCombinations()
   }
   ```

#### Success Criteria
- [ ] All 22 backend unit tests passing
- [ ] All 8 integration tests passing
- [ ] Database migrations run successfully
- [ ] API endpoints return proper DTOs
- [ ] Complex availability calculations working per test specs

#### Dependencies
- Existing Event entity (✅ available)
- PostgreSQL database (✅ configured)
- Entity Framework Core (✅ configured)

### Phase 3: Admin Event Management UI (Est: 16-20 hours)
**Objective**: Build complete admin interface for event creation and management

#### Tests Already Written
React component tests exist in:
- `/apps/web/src/components/events/__tests__/EventSessionForm.test.tsx` (20 tests)

#### Implementation Tasks

1. **Admin Dashboard Page**
   ```typescript
   // /apps/web/src/pages/events/AdminDashboardPage.tsx
   // Based on wireframe: /docs/functional-areas/events/new-work/2025-08-24-events-management/design/wireframes/admin-events-dashboard.html
   
   interface AdminDashboardProps {
     events: EventDto[];
     filters: EventFilters;
     onFilterChange: (filters: EventFilters) => void;
     onCreateEvent: () => void;
     onEditEvent: (id: string) => void;
     onCopyEvent: (id: string) => void;
   }
   ```

2. **Event Form with Tabs** (Based on existing wireframes)
   ```typescript
   // /apps/web/src/pages/events/EventFormPage.tsx
   // Tabbed interface: Basic Info, Tickets/Orders, Emails, Volunteers/Staff
   
   // /apps/web/src/components/events/EventBasicInfoTab.tsx
   // Fields: Event Type, Title, Description, Venue, Teachers
   
   // /apps/web/src/components/events/EventTicketsTab.tsx
   // Event Sessions table + Ticket Types table
   
   // /apps/web/src/components/events/EventEmailsTab.tsx
   // Email template management
   
   // /apps/web/src/components/events/EventVolunteersTab.tsx
   // Volunteer position management
   ```

3. **Session Management Component** (Make React tests pass)
   ```typescript
   // /apps/web/src/components/events/SessionManagement.tsx
   interface SessionManagementProps {
     sessions: EventSessionDto[];
     onAddSession: (session: CreateSessionDto) => void;
     onEditSession: (id: string, session: UpdateSessionDto) => void;
     onDeleteSession: (id: string) => void;
   }
   
   // Must pass all tests in EventSessionForm.test.tsx:
   // - Display all event sessions with capacity information
   // - Add new session with validation
   // - Validate session capacity > 0
   // - Prevent overlapping session times on same date
   ```

4. **Ticket Type Configuration** (Make React tests pass)
   ```typescript
   // /apps/web/src/components/events/TicketTypeConfiguration.tsx
   // Must pass tests:
   // - Display ticket types with session mappings
   // - Create ticket type with session selection
   // - Validate ticket type includes at least one session
   // - Prevent mapping to non-existent sessions
   ```

#### Success Criteria
- [ ] All 20 React component tests passing
- [ ] Admin dashboard displays events in proper table format
- [ ] Event form saves successfully with all tabs
- [ ] Session management works per wireframe specifications
- [ ] Ticket type configuration handles session mappings
- [ ] All accessibility tests passing

#### Dependencies
- Phase 2 backend API endpoints (required)
- Mantine v7 UI components (✅ available)
- TanStack Query (✅ configured)
- Generated TypeScript types (✅ working)

### Phase 4: Public Events Interface (Est: 8-12 hours)
**Objective**: Build public-facing events browsing and registration

#### Tests to Write First (TDD)
```typescript
// /apps/web/src/pages/events/__tests__/PublicEventsPage.test.tsx
describe('Public Events Page', () => {
  it('should display events in card view by default', () => {});
  it('should switch to list view when toggled', () => {});
  it('should filter events by search term', () => {});
  it('should show member-only events only to logged-in members', () => {});
  it('should display correct availability for multi-session events', () => {});
});

// /apps/web/src/pages/events/__tests__/EventDetailsPage.test.tsx
describe('Event Details Page', () => {
  it('should display event information correctly', () => {});
  it('should show session availability breakdown', () => {});
  it('should allow ticket type selection', () => {});
  it('should prevent selection of unavailable tickets', () => {});
  it('should update purchase button based on selection', () => {});
});
```

#### Implementation Tasks

1. **Public Events List Page**
   ```typescript
   // /apps/web/src/pages/events/PublicEventsPage.tsx
   // Based on wireframe: public-events-list.html
   
   interface PublicEventsPageProps {
     viewMode: 'card' | 'list';
     searchTerm: string;
     filters: PublicEventFilters;
   }
   ```

2. **Event Card Component**
   ```typescript
   // /apps/web/src/components/events/EventCard.tsx
   interface EventCardProps {
     event: EventDto;
     showConstraints: boolean; // "5/20 (Day 2)" for multi-session
   }
   ```

3. **Event Details Page**
   ```typescript
   // /apps/web/src/pages/events/EventDetailsPage.tsx
   // Left column: Event info, sessions, policies
   // Right column: Registration card with ticket selection
   
   interface RegistrationCardProps {
     event: EventDto;
     ticketTypes: TicketTypeDto[];
     availability: EventAvailabilityDto;
     onTicketSelect: (ticketTypeId: string) => void;
   }
   ```

#### Success Criteria
- [ ] Public events page renders in both card and list view
- [ ] Search and filtering work correctly
- [ ] Member-only events respect authentication state
- [ ] Event details page shows proper session information
- [ ] Ticket selection reflects availability constraints
- [ ] Mobile responsive design working

#### Dependencies
- Phase 2 backend availability calculations (required)
- Authentication system (✅ available)
- Event availability API endpoint (✅ working)

### Phase 5: Check-In System (Est: 10-14 hours)
**Objective**: Implement kiosk mode check-in interface

#### Tests to Write First (TDD)
```typescript
// /apps/web/src/pages/events/__tests__/CheckInInterface.test.tsx
describe('Check-In Interface', () => {
  it('should start kiosk session with security token', () => {});
  it('should display session timer and security indicators', () => {});
  it('should search attendees by name', () => {});
  it('should open check-in modal with attendee details', () => {});
  it('should prevent navigation away in kiosk mode', () => {});
  it('should auto-expire session after timeout', () => {});
});
```

#### Implementation Tasks

1. **Kiosk Security System**
   ```csharp
   // Backend: /apps/api/Features/Events/Services/KioskSessionService.cs
   public class KioskSessionService
   {
       Task<KioskSessionDto> StartKioskSession(string stationName, int durationHours);
       Task<bool> ValidateKioskSession(string token);
       Task RevokeKioskSession(string sessionId);
   }
   ```

2. **Check-In Interface**
   ```typescript
   // /apps/web/src/pages/events/CheckInInterface.tsx
   // Based on wireframe: check-in-interface.html
   
   interface CheckInInterfaceProps {
     eventId: string;
     kioskSessionId: string;
     stationName: string;
   }
   ```

3. **Check-In Modal**
   ```typescript
   // /apps/web/src/components/events/CheckInModal.tsx
   interface CheckInModalProps {
     attendee: AttendeeDto;
     onCheckIn: (checkInData: CheckInRequestDto) => void;
     onCancel: () => void;
   }
   ```

#### Success Criteria
- [ ] Kiosk mode starts with secure session
- [ ] Browser navigation locked in kiosk mode
- [ ] Attendee search and check-in working
- [ ] Session timer and auto-expiration working
- [ ] Check-in modal captures all required data
- [ ] Audit trail logging all check-in actions

#### Dependencies
- Phase 2 backend check-in entities (required)
- Authentication system with role checking (✅ available)
- Event attendee data (from ticket purchases)

## Time Estimates & Dependencies

### Total Estimated Time: 40-58 hours

| Phase | Estimated Hours | Dependencies | Blockers |
|-------|----------------|--------------|----------|
| Phase 1: Foundation | 4-6 hours | None | Authentication working |
| Phase 2: Backend Matrix | 12-16 hours | Phase 1 routes | Database access |
| Phase 3: Admin UI | 16-20 hours | Phase 2 APIs | Mantine components |
| Phase 4: Public Interface | 8-12 hours | Phase 2 APIs | Public access permissions |
| Phase 5: Check-In System | 10-14 hours | Phase 2 + attendee data | Security review |

### Critical Path
1. **Phase 1 → Phase 2**: Routes must exist before API integration
2. **Phase 2 → Phase 3**: Backend APIs required for admin UI
3. **Phase 2 → Phase 4**: Availability calculations needed for public interface
4. **Phase 2 + Attendee System → Phase 5**: Check-in needs ticket/registration data

## Implementation Strategy

### Test-Driven Development Approach
1. **Red**: Write failing test first
2. **Green**: Implement minimum code to pass test
3. **Refactor**: Improve code while keeping tests passing

### Existing Test Leverage
We have 50 TDD tests already written:
- **Backend**: 22 unit + 8 integration tests (30 total)
- **Frontend**: 20 React component tests
- **Missing**: Additional E2E tests for full workflows

### Quality Gates
For each phase:
- [ ] All TDD tests passing
- [ ] TypeScript compilation with no errors
- [ ] Accessibility requirements met
- [ ] Mobile responsive design validated
- [ ] Performance targets achieved (<2s load, <200ms API)

## Questions for User Clarification

### Business Logic Questions
1. **RSVP vs Tickets**: Should social events allow both free RSVPs and paid ticket upgrades?
2. **Member Access**: What vetting levels are required for different event types?
3. **Payment Integration**: Phase 1 implementation tracks payments or integrates with payment processor?
4. **Refund Policies**: How should automated refund windows be calculated?

### Technical Questions
5. **Authentication Roles**: Confirm Admin vs EventOrganizer vs Teacher permissions
6. **Kiosk Security**: Required session duration and renewal policies?
7. **Mobile Experience**: Priority for mobile-first design vs desktop-first?
8. **Performance Requirements**: Expected concurrent users and load targets?

### UI/UX Questions
9. **Event Images**: Support for event photos/banners in this phase?
10. **Accessibility**: WCAG compliance level required (AA vs AAA)?
11. **Internationalization**: Support for multiple languages needed?

## Risk Assessment

### High Risk
- **Complex Availability Calculations**: Multi-session, cross-ticket-type capacity logic
- **Kiosk Mode Security**: Browser lockdown and session management
- **Database Performance**: Availability queries under load

### Medium Risk
- **UI Complexity**: Tabbed event form with multiple data grids
- **Mobile Responsiveness**: Complex admin interfaces on small screens
- **Payment Integration**: External payment processor coordination

### Low Risk
- **Basic CRUD Operations**: Standard patterns already established
- **Authentication Integration**: System already working
- **TypeScript Integration**: NSwag pipeline operational

## Success Criteria

### Technical Success
- [ ] All 50+ TDD tests passing
- [ ] Performance targets met (<2s page load, <200ms API)
- [ ] Zero TypeScript compilation errors
- [ ] Accessibility compliance achieved
- [ ] Mobile responsive design validated

### Business Success
- [ ] Complete events workflow: Create → Publish → Register → Check-in
- [ ] Admin dashboard matches wireframe specifications exactly
- [ ] Public interface provides excellent user experience
- [ ] Event Session Matrix architecture fully implemented
- [ ] RSVP and ticketing workflows working correctly

### Quality Success
- [ ] Code review standards met
- [ ] Security validation complete
- [ ] Documentation updated and current
- [ ] User acceptance testing passed
- [ ] Production deployment readiness achieved

## Next Steps

1. **User Review**: Address clarification questions above
2. **Phase 1 Start**: Begin with route setup and basic navigation
3. **TDD Execution**: Run existing tests to confirm they fail appropriately
4. **Implementation**: Follow phase-by-phase plan with TDD approach
5. **Quality Gates**: Validate each phase before proceeding to next

This implementation plan leverages all existing research and assets while following a strict TDD approach to ensure quality and completeness of the Events Management System.