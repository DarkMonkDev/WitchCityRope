# TDD Implementation Plan: Events Management System
<!-- Last Updated: 2025-10-05 -->
<!-- Version: 1.1 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Updated with User Clarifications -->

## Document Change History

### Version 1.1 (2025-10-05)
**Critical Updates Applied:**
- **Check-in System**: Moved to Phase 1/2, retitled as "Staff-Assisted Check-In" (not kiosk)
- **RSVP+Tickets**: Updated social events to support BOTH as separate, parallel actions
- **Event Images**: Removed from scope, marked as deferred
- **Questions Answered**: Business logic and UI/UX clarifications resolved
- **Phase Priorities**: Reorganized to reflect actual business requirements

### Version 1.0 (2025-09-06)
- Initial TDD implementation plan

## Executive Summary

This detailed Test-Driven Development (TDD) implementation plan completes the Events Management System by leveraging existing research, wireframes, and API code. The plan follows a TDD-first approach where tests are written before implementation, ensuring clear requirements and preventing feature drift.

**Key Context**: We are building upon substantial existing work:
- ✅ Complete wireframes and designs exist
- ✅ Backend API has 3 working GET endpoints
- ✅ Frontend integration demo is functional
- ✅ Business requirements and functional specs complete (v3.1)
- ✅ TDD test infrastructure already created (50 tests)

**CRITICAL CLARIFICATIONS (2025-10-05):**
- ✅ Social events support BOTH RSVP (free) AND ticket purchases (separate actions)
- ✅ Check-in is staff-assisted (NOT kiosk mode) and required for Phase 1
- ✅ Teachers CANNOT edit events (only view)
- ✅ Event images DEFERRED to future phase
- ✅ Email automation CONFIRMED in-scope

## Current State Analysis

### What We Have (Assets to Leverage)

#### Backend API (Partially Complete)
- ✅ **EventsManagementService**: Working GET endpoints
- ✅ **Event, EventDto**: Basic entity and data transfer objects
- ✅ **Database Integration**: PostgreSQL with proper UTC handling
- ✅ **Unit Tests**: Test infrastructure established
- ❌ **Missing**: CREATE/UPDATE/DELETE endpoints, Session/TicketType/RSVP entities

#### Frontend Integration (Demo Ready)
- ✅ **TypeScript Types**: Generated from C# DTOs via NSwag
- ✅ **API Service**: TanStack Query integration working
- ✅ **Demo Page**: http://localhost:5173/admin/events-management-api-demo
- ❌ **Missing**: Full UI components, admin dashboard, public pages, check-in interface

#### Design System (Complete)
- ✅ **Wireframes**: Complete admin, public, and check-in interfaces
- ✅ **Event Session Matrix**: Architecture fully documented
- ✅ **Business Requirements**: All user stories and acceptance criteria (v3.1)
- ✅ **Functional Specifications**: Technical implementation details

#### Test Infrastructure (TDD Ready)
- ✅ **50 TDD Tests Created**: All written first, currently failing
- ✅ **Backend Unit Tests**: 22 tests for Event Session Matrix logic
- ✅ **React Component Tests**: 20 tests for UI components
- ✅ **Integration Tests**: 8 tests for end-to-end workflows

### Missing Implementation (Focus Areas)

#### Backend Missing Pieces
1. **Event Session Matrix Entities**: EventSession, EventTicketType, TicketTypeSessionInclusions
2. **RSVP Entity**: EventRsvp with separate tracking from tickets
3. **CRUD API Endpoints**: POST/PUT/DELETE for events, sessions, tickets, RSVPs
4. **Complex Availability Calculations**: Multi-session capacity logic
5. **Dual Registration System**: Social event RSVP + ticket handling (separate actions)
6. **Check-in System**: Staff-assisted check-in with RSVP and ticket verification

#### Frontend Missing Pieces
1. **Admin Dashboard**: Events list with filters and actions
2. **Event Form**: Tabbed interface (Basic Info, Tickets/Orders, Emails, Volunteers) - NO images
3. **Public Events Page**: Card/list view with filtering - NO images
4. **Event Details Page**: Registration interface with RSVP AND ticket options for social events
5. **Check-in Interface**: Staff-assisted check-in (Phase 1 priority)

## Phase-Based Implementation Plan (UPDATED)

### Phase 1: Foundation & Route Setup (Est: 4-6 hours)
**Objective**: Establish basic navigation and authentication for events system

#### Tests to Write First (TDD)
```typescript
// /apps/web/src/pages/events/__tests__/routes.test.tsx
describe('Events Routes', () => {
  it('should navigate to public events page', () => {});
  it('should require admin role for admin dashboard', () => {});
  it('should require Event Organizer or Admin for check-in', () => {});
  it('should redirect unauthenticated users appropriately', () => {});
  it('should prevent Teachers from accessing edit routes', () => {});
});
```

#### Implementation Tasks
1. **Route Configuration**
   - Add `/events` route (public events list)
   - Add `/admin/events` route (admin dashboard)
   - Add `/admin/events/create` and `/admin/events/{id}/edit` routes
   - Add `/admin/events/{id}/check-in` route (staff-assisted check-in)
   - Add authentication guards with role checks

2. **Basic Page Components** (Stub Implementation)
   ```typescript
   // /apps/web/src/pages/events/PublicEventsPage.tsx
   // /apps/web/src/pages/events/AdminDashboardPage.tsx
   // /apps/web/src/pages/events/EventFormPage.tsx
   // /apps/web/src/pages/events/CheckInPage.tsx (NEW - Phase 1)
   ```

3. **Navigation Integration**
   - Update main navigation to include Events
   - Add admin navigation links
   - Add check-in access link for Event Organizers
   - Test navigation with existing test accounts
   - Verify Teacher role restrictions

#### Success Criteria
- [ ] All routes accessible and render basic components
- [ ] Authentication working with test accounts
- [ ] Teacher role correctly restricted from edit routes
- [ ] Check-in route accessible to Event Organizers and Admins
- [ ] Navigation tests passing
- [ ] No TypeScript compilation errors

#### Questions for User (ANSWERED 2025-10-05)
1. ✅ Should public events be accessible without login? **YES**
2. ✅ Which roles should have access to admin events dashboard? **Event Organizers and Admins**
3. ✅ Should we display different navigation based on user role? **YES, Teachers see view-only**

### Phase 2: Event Session Matrix Backend + RSVP System (Est: 14-18 hours)
**Objective**: Implement core Event Session Matrix entities, RSVP system, and CRUD operations

**UPDATED:** Now includes RSVP entity and dual-path registration logic

#### Tests Already Written (50 TDD Tests)
All tests exist in:
- `/tests/WitchCityRope.Api.Tests/Features/Events/EventSessionTests.cs` (22 tests)
- `/tests/integration/events/EventSessionMatrixIntegrationTests.cs` (8 tests)

#### Additional Tests Needed
```csharp
// /tests/WitchCityRope.Api.Tests/Features/Events/RsvpTests.cs (NEW)
- CreateRsvp_ForSocialEvent_SucceedsWithoutTicket()
- CreateRsvp_AndPurchaseTicket_TrackedSeparately()
- CreateTicket_WithoutRsvp_AllowedForSocialEvent()
- GetAttendeeStatus_ShowsBothRsvpAndTicket()
- CheckIn_WithRsvpOnly_AllowsTicketPurchaseAtDoor()
```

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

   // /src/WitchCityRope.Core/Entities/EventRsvp.cs (NEW)
   public class EventRsvp
   {
       public Guid Id { get; private set; }
       public Guid EventId { get; private set; }
       public Guid MemberId { get; private set; }
       public DateTime RsvpDate { get; private set; }
       public bool HasTicket { get; private set; } // Tracks if also purchased ticket
       public RsvpStatus Status { get; private set; } // Confirmed, Cancelled, CheckedIn
       // IMPORTANT: RSVP and ticket are tracked separately
   }
   ```

2. **Database Schema Updates**
   ```sql
   -- New migration: Add Event Session Matrix + RSVP tables
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

   CREATE TABLE EventRsvps (
       Id UUID PRIMARY KEY,
       EventId UUID NOT NULL REFERENCES Events(Id) ON DELETE CASCADE,
       MemberId UUID NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
       RsvpDate TIMESTAMPTZ NOT NULL DEFAULT NOW(),
       HasTicket BOOLEAN NOT NULL DEFAULT FALSE,
       Status VARCHAR(20) NOT NULL CHECK (Status IN ('Confirmed', 'Cancelled', 'CheckedIn')),
       CreatedAt TIMESTAMPTZ NOT NULL DEFAULT NOW(),
       UNIQUE(EventId, MemberId)
   );
   ```

3. **API Endpoints** (Make failing tests pass)
   ```csharp
   // /apps/api/Features/Events/Endpoints/EventManagementEndpoints.cs
   POST /api/events - Create event with sessions and ticket types
   PUT /api/events/{id} - Update event (Event Organizers and Admins only)
   DELETE /api/events/{id} - Delete event (Event Organizers and Admins only)

   POST /api/events/{id}/sessions - Add session to event
   PUT /api/sessions/{id} - Update session
   DELETE /api/sessions/{id} - Delete session

   POST /api/events/{id}/ticket-types - Create ticket type
   PUT /api/ticket-types/{id} - Update ticket type
   DELETE /api/ticket-types/{id} - Delete ticket type

   // NEW: RSVP endpoints for social events
   POST /api/events/{id}/rsvp - Create RSVP (separate from ticket purchase)
   DELETE /api/events/{id}/rsvp - Cancel RSVP
   GET /api/events/{id}/rsvps - Get RSVP list (Event Organizers only)
   GET /api/events/{id}/attendee-status - Get member's RSVP + ticket status
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
       // - SocialEvent_RsvpsAndTickets_TrackedSeparately()
   }
   ```

#### Success Criteria
- [ ] All 22 backend unit tests passing
- [ ] All 8 integration tests passing
- [ ] All new RSVP tests passing
- [ ] Database migrations run successfully
- [ ] API endpoints return proper DTOs
- [ ] Complex availability calculations working per test specs
- [ ] RSVP and ticket tracking separate for social events
- [ ] Teacher role prevented from PUT/DELETE operations

#### Dependencies
- Existing Event entity (✅ available)
- PostgreSQL database (✅ configured)
- Entity Framework Core (✅ configured)
- Role-based authorization (✅ available)

### Phase 3: Staff-Assisted Check-In System (Est: 8-10 hours)
**Objective**: Implement staff-assisted check-in interface with RSVP and ticket verification

**MOVED FROM PHASE 5 - Now Phase 1 Priority**

#### Tests to Write First (TDD)
```typescript
// /apps/web/src/pages/events/__tests__/CheckInInterface.test.tsx
describe('Staff-Assisted Check-In Interface', () => {
  it('should require Event Organizer or Admin role', () => {});
  it('should load attendee list with RSVP and ticket status', () => {});
  it('should search attendees by name', () => {});
  it('should show separate indicators for RSVP vs ticket', () => {});
  it('should allow ticket purchase at door for RSVP-only attendees', () => {});
  it('should check in attendees with proper status updates', () => {});
  it('should log all check-in actions with staff attribution', () => {});
});
```

#### Implementation Tasks

1. **Check-In Backend Service**
   ```csharp
   // Backend: /apps/api/Features/Events/Services/CheckInService.cs
   public class CheckInService
   {
       Task<AttendeeListDto> GetAttendeeList(Guid eventId);
       Task<CheckInResultDto> CheckInAttendee(Guid eventId, Guid attendeeId, string staffUserId);
       Task<TicketPurchaseDto> PurchaseTicketAtDoor(Guid eventId, Guid memberId, decimal amount);
       Task<List<CheckInAuditLogDto>> GetCheckInLogs(Guid eventId);
   }
   ```

2. **Check-In Interface**
   ```typescript
   // /apps/web/src/pages/events/CheckInInterface.tsx
   // Based on wireframe: check-in-interface.html

   interface CheckInInterfaceProps {
     eventId: string;
   }

   interface AttendeeStatus {
     id: string;
     name: string;
     hasRsvp: boolean;
     hasTicket: boolean;
     isCheckedIn: boolean;
     checkInTime?: string;
   }
   ```

3. **Check-In Components**
   ```typescript
   // /apps/web/src/components/events/AttendeeList.tsx
   // Shows all expected attendees with status indicators

   // /apps/web/src/components/events/CheckInModal.tsx
   interface CheckInModalProps {
     attendee: AttendeeDto;
     onCheckIn: (checkInData: CheckInRequestDto) => void;
     onPurchaseTicketAtDoor: (purchaseData: DoorPurchaseDto) => void;
     onCancel: () => void;
   }
   ```

4. **Status Indicators**
   - RSVP only (no ticket): Yellow badge "RSVP"
   - Ticket purchased: Green badge "TICKET"
   - Both RSVP and ticket: Two badges "RSVP" + "TICKET"
   - Checked in: Blue badge "CHECKED IN"

#### Success Criteria
- [ ] Check-in interface accessible to Event Organizers and Admins
- [ ] Attendee list shows all expected participants
- [ ] Separate status indicators for RSVP and ticket
- [ ] Staff can check in attendees with status updates
- [ ] Staff can process ticket purchases at door
- [ ] All check-in actions logged with staff attribution
- [ ] Mobile responsive for tablet use at check-in desk

#### Dependencies
- Phase 2 backend RSVP and ticket entities (required)
- Authentication system with role checking (✅ available)
- Event attendee data (from tickets and RSVPs)

### Phase 4: Admin Event Management UI (Est: 16-20 hours)
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
     onLaunchCheckIn: (id: string) => void; // NEW
   }
   ```

2. **Event Form with Tabs** (Based on existing wireframes)
   ```typescript
   // /apps/web/src/pages/events/EventFormPage.tsx
   // Tabbed interface: Basic Info, Tickets/Orders, Emails, Volunteers/Staff

   // /apps/web/src/components/events/EventBasicInfoTab.tsx
   // Fields: Event Type, Title, Description, Venue, Teachers
   // NO image upload (deferred feature)

   // /apps/web/src/components/events/EventTicketsTab.tsx
   // Event Sessions table + Ticket Types table
   // For social events: RSVP tracking + ticket sales

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

5. **RSVP Management for Social Events (NEW)**
   ```typescript
   // /apps/web/src/components/events/RsvpManagement.tsx
   interface RsvpManagementProps {
     eventId: string;
     rsvps: EventRsvpDto[];
     onViewRsvp: (rsvpId: string) => void;
   }

   // Shows:
   // - Total RSVPs (separate from ticket sales)
   // - RSVPs with tickets vs RSVP-only
   // - RSVP status (Confirmed, Checked In, Cancelled)
   ```

#### Success Criteria
- [ ] All 20 React component tests passing
- [ ] Admin dashboard displays events in proper table format
- [ ] Event form saves successfully with all tabs
- [ ] Basic Info tab does NOT include image upload
- [ ] Session management works per wireframe specifications
- [ ] Ticket type configuration handles session mappings
- [ ] Social events show RSVP management separate from tickets
- [ ] Check-in button launches check-in interface
- [ ] All accessibility tests passing
- [ ] Teachers cannot access edit functionality

#### Dependencies
- Phase 2 backend API endpoints (required)
- Phase 3 check-in interface (for launch button)
- Mantine v7 UI components (✅ available)
- TanStack Query (✅ configured)
- Generated TypeScript types (✅ working)

### Phase 5: Public Events Interface (Est: 10-14 hours)
**Objective**: Build public-facing events browsing and registration with RSVP + ticket options

#### Tests to Write First (TDD)
```typescript
// /apps/web/src/pages/events/__tests__/PublicEventsPage.test.tsx
describe('Public Events Page', () => {
  it('should display events in card view by default', () => {});
  it('should NOT show event images (deferred feature)', () => {});
  it('should switch to list view when toggled', () => {});
  it('should filter events by search term', () => {});
  it('should show member-only events only to logged-in members', () => {});
  it('should display correct availability for multi-session events', () => {});
});

// /apps/web/src/pages/events/__tests__/EventDetailsPage.test.tsx
describe('Event Details Page', () => {
  it('should display event information correctly', () => {});
  it('should show session availability breakdown', () => {});
  it('should show BOTH RSVP and ticket options for social events', () => {});
  it('should track RSVP and ticket purchase separately', () => {});
  it('should allow ticket type selection', () => {});
  it('should prevent selection of unavailable tickets', () => {});
  it('should update purchase button based on selection', () => {});
  it('should NOT show image upload (deferred)', () => {});
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
     // NO imageUrl prop (deferred feature)
   }
   ```

3. **Event Details Page**
   ```typescript
   // /apps/web/src/pages/events/EventDetailsPage.tsx
   // Left column: Event info, sessions, policies (NO images)
   // Right column: Registration card with RSVP AND ticket options

   interface RegistrationCardProps {
     event: EventDto;
     ticketTypes: TicketTypeDto[];
     availability: EventAvailabilityDto;
     userRsvpStatus: RsvpStatusDto | null;
     userTicketStatus: TicketStatusDto | null;
     onRsvp: () => void; // NEW
     onCancelRsvp: () => void; // NEW
     onTicketSelect: (ticketTypeId: string) => void;
   }
   ```

4. **Social Event Registration UI (NEW)**
   ```typescript
   // /apps/web/src/components/events/SocialEventRegistration.tsx
   // Shows TWO separate sections:
   // 1. RSVP section (free, confirms attendance)
   // 2. Ticket purchase section (paid, separate from RSVP)

   interface SocialEventRegistrationProps {
     event: EventDto;
     hasRsvp: boolean;
     hasTicket: boolean;
     onRsvp: () => void;
     onCancelRsvp: () => void;
     onPurchaseTicket: (ticketTypeId: string) => void;
   }
   ```

#### Success Criteria
- [ ] Public events page renders in both card and list view
- [ ] Event cards do NOT show images (deferred feature)
- [ ] Search and filtering work correctly
- [ ] Member-only events respect authentication state
- [ ] Event details page shows proper session information
- [ ] Social events show BOTH RSVP and ticket purchase options
- [ ] RSVP and ticket status tracked separately
- [ ] Ticket selection reflects availability constraints
- [ ] Mobile responsive design working

#### Dependencies
- Phase 2 backend availability calculations (required)
- Phase 2 RSVP endpoints (required)
- Authentication system (✅ available)
- Event availability API endpoint (✅ working)

## Time Estimates & Dependencies (UPDATED)

### Total Estimated Time: 48-68 hours

| Phase | Estimated Hours | Dependencies | Blockers | Priority |
|-------|----------------|--------------|----------|----------|
| Phase 1: Foundation | 4-6 hours | None | Authentication working | HIGH |
| Phase 2: Backend Matrix + RSVP | 14-18 hours | Phase 1 routes | Database access | HIGH |
| Phase 3: Check-In System | 8-10 hours | Phase 2 APIs | Staff roles | HIGH |
| Phase 4: Admin UI | 16-20 hours | Phase 2 + 3 | Mantine components | MEDIUM |
| Phase 5: Public Interface | 10-14 hours | Phase 2 APIs | Public access | MEDIUM |

### Critical Path (UPDATED)
1. **Phase 1 → Phase 2**: Routes must exist before API integration
2. **Phase 2 → Phase 3**: RSVP and ticket entities required for check-in
3. **Phase 2 + 3 → Phase 4**: Backend APIs and check-in needed for admin UI
4. **Phase 2 → Phase 5**: Availability calculations and RSVP needed for public interface

## Implementation Strategy

### Test-Driven Development Approach
1. **Red**: Write failing test first
2. **Green**: Implement minimum code to pass test
3. **Refactor**: Improve code while keeping tests passing

### Existing Test Leverage
We have 50 TDD tests already written:
- **Backend**: 22 unit + 8 integration tests (30 total)
- **Frontend**: 20 React component tests
- **Missing**: Additional E2E tests for full workflows, RSVP tests

### Quality Gates
For each phase:
- [ ] All TDD tests passing
- [ ] TypeScript compilation with no errors
- [ ] Accessibility requirements met
- [ ] Mobile responsive design validated
- [ ] Performance targets achieved (<2s load, <200ms API)
- [ ] Role-based access control validated

## Questions for User Clarification (ANSWERED 2025-10-05)

### Business Logic Questions
1. ✅ **RSVP vs Tickets**: Should social events allow both free RSVPs and paid ticket upgrades?
   **ANSWER:** YES - Social events support BOTH RSVP (free) and ticket purchases as SEPARATE actions. A member can: RSVP only, purchase ticket only, OR both RSVP and purchase ticket. Tickets can be purchased ahead of time OR at door during check-in.

2. ✅ **Member Access**: What vetting levels are required for different event types?
   **ANSWER:** Per existing vetting system - vetted members have access to member-only events.

3. ✅ **Payment Integration**: Phase 1 implementation tracks payments or integrates with payment processor?
   **ANSWER:** Payment tracking with external payment processing (existing pattern).

4. ✅ **Refund Policies**: How should automated refund windows be calculated?
   **ANSWER:** Per existing refund policy rules.

### Technical Questions
5. ✅ **Authentication Roles**: Confirm Admin vs EventOrganizer vs Teacher permissions
   **ANSWER:**
   - Event Organizers and Admins: Full edit access
   - Teachers: View only, NO editing capabilities, must contact organizers for changes

6. ✅ **Check-In System**: Required session duration and renewal policies?
   **ANSWER:** Staff-assisted check-in with standard staff login authentication (NOT kiosk mode with special sessions)

7. ✅ **Mobile Experience**: Priority for mobile-first design vs desktop-first?
   **ANSWER:** Mobile responsive (standard approach).

8. ✅ **Performance Requirements**: Expected concurrent users and load targets?
   **ANSWER:** Standard performance targets (<2s page load, <200ms API).

### UI/UX Questions
9. ✅ **Event Images**: Support for event photos/banners in this phase?
   **ANSWER:** NO - Defer event image upload functionality to future phase. Remove from current scope.

10. ✅ **Accessibility**: WCAG compliance level required (AA vs AAA)?
    **ANSWER:** Standard WCAG AA compliance.

11. ✅ **Internationalization**: Support for multiple languages needed?
    **ANSWER:** Not in current phase.

## Risk Assessment

### High Risk (UPDATED)
- **Complex Availability Calculations**: Multi-session, cross-ticket-type capacity logic
- **Dual Registration System**: RSVP and ticket tracking as separate but related entities
- **Database Performance**: Availability queries under load with RSVPs + tickets

### Medium Risk
- **UI Complexity**: Tabbed event form with multiple data grids
- **Mobile Responsiveness**: Complex admin interfaces on small screens
- **Check-In Workflow**: Door payment processing during check-in

### Low Risk
- **Basic CRUD Operations**: Standard patterns already established
- **Authentication Integration**: System already working
- **TypeScript Integration**: NSwag pipeline operational
- **Teacher Restrictions**: Simple role-based access control

## Success Criteria

### Technical Success
- [ ] All 50+ TDD tests passing
- [ ] Additional RSVP tests passing
- [ ] Performance targets met (<2s page load, <200ms API)
- [ ] Zero TypeScript compilation errors
- [ ] Accessibility compliance achieved
- [ ] Mobile responsive design validated
- [ ] Role-based access control working (Teacher restrictions)

### Business Success
- [ ] Complete events workflow: Create → Publish → Register (RSVP/Ticket) → Check-in
- [ ] Admin dashboard matches wireframe specifications exactly (no images)
- [ ] Public interface provides excellent user experience (no images)
- [ ] Event Session Matrix architecture fully implemented
- [ ] RSVP and ticketing workflows working correctly as SEPARATE actions
- [ ] Staff-assisted check-in operational with door payment support

### Quality Success
- [ ] Code review standards met
- [ ] Security validation complete
- [ ] Documentation updated and current
- [ ] User acceptance testing passed
- [ ] Production deployment readiness achieved

## Next Steps

1. ✅ **User Review**: Clarifications received and documented
2. **Phase 1 Start**: Begin with route setup and basic navigation (including check-in route)
3. **TDD Execution**: Run existing tests to confirm they fail appropriately
4. **Phase 2 Implementation**: Backend with RSVP system
5. **Phase 3 Implementation**: Staff-assisted check-in interface
6. **Quality Gates**: Validate each phase before proceeding to next

This implementation plan leverages all existing research and assets while following a strict TDD approach to ensure quality and completeness of the Events Management System. All user clarifications from 2025-10-05 have been incorporated.
