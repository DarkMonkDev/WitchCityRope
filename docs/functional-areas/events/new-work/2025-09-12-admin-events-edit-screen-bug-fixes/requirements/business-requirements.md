# Business Requirements: Admin Events Edit Screen Bug Fixes (TDD Implementation)
<!-- Last Updated: 2025-09-12 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Ready for TDD Development -->

## Executive Summary
Critical bug fixes for the Admin Events Edit Screen to ensure proper session management, ticket creation, and volunteer position functionality using Test-Driven Development (TDD) approach. These fixes are essential for the Events Management system to function as designed and enable proper event administration workflow.

## Business Context
### Problem Statement
The Admin Events Edit Screen has multiple critical bugs preventing Event Organizers from properly configuring events:
1. Session creation fails completely, blocking all downstream functionality
2. Ticket creation is blocked due to session dependency
3. Volunteer position management has multiple broken workflows
4. UI inconsistency between different management sections
5. Missing seed data prevents proper testing and demonstration

### Business Value
- Unblocks Event Organizer workflow for complete event setup
- Enables proper session-based event management as designed
- Provides consistent UI experience across all management tabs
- Allows comprehensive testing with realistic data
- Restores confidence in admin tools functionality

### Success Metrics
- 100% of session creation operations succeed without page refresh
- 100% of ticket types can be created and associated with sessions
- 100% of volunteer position CRUD operations function correctly
- 95% reduction in user interface inconsistencies
- Complete seed data coverage for all event-related entities

## User Stories

### Story 1: Session Management Bug Fixes
**As an** Event Organizer  
**I want to** create and manage event sessions without page refresh issues  
**So that** I can build the foundation for ticket types and scheduling

**Acceptance Criteria:**
- Given I am in the Admin Events Edit Screen Tickets/Orders tab
- When I click "Add Session" and fill out the modal form
- Then the session is saved via API without full page refresh
- And the session appears in the Event Sessions table immediately
- And the session gets proper S# ID assignment (S1, S2, S3)
- And I can edit existing sessions through the Edit button
- And the Edit modal pre-populates with current session data
- And session updates save without page refresh
- And I can delete sessions with confirmation dialog

**Test Requirements (TDD):**
```typescript
// Write these failing tests FIRST
describe('Session Management', () => {
  it('should create session via API without page refresh')
  it('should update sessions grid immediately after creation')
  it('should assign S# IDs automatically (S1, S2, S3)')
  it('should load existing session data into edit modal')
  it('should save session updates without page refresh')
  it('should delete sessions with confirmation')
})
```

### Story 2: Ticket Creation Dependency Resolution
**As an** Event Organizer  
**I want to** create ticket types after sessions exist  
**So that** I can offer registration options for the event

**Acceptance Criteria:**
- Given I have created at least one event session (S1, S2, etc.)
- When I access ticket type management
- Then I can create ticket types and associate them with sessions
- And the Sessions Included dropdown shows only current event sessions
- And ticket types display session associations in S# format
- And ticket capacity validation works against session capacity
- And I can create multiple ticket types for different session combinations

**Test Requirements (TDD):**
```typescript
// Write these failing tests FIRST
describe('Ticket Type Management', () => {
  it('should enable ticket creation when sessions exist')
  it('should populate session dropdown with event-specific sessions only')
  it('should save ticket-session associations correctly')
  it('should validate ticket capacity against session capacity')
  it('should display session associations in S# format')
})
```

### Story 3: Volunteer Position Management Bug Fixes
**As an** Event Organizer  
**I want to** properly manage volunteer positions with session assignments  
**So that** I can coordinate staffing across multi-session events

**Acceptance Criteria:**
- Given I am in the Volunteers/Staff tab
- When I view volunteer positions, the Sessions dropdown shows only current event sessions (not all platform sessions)
- And when I click "Add Position" the form processes and updates the grid
- And when I click "Edit" on a position, the form pre-populates with position data
- And the position form saves changes without page refresh
- And the positions grid updates immediately after save/edit operations
- And I can delete positions with confirmation dialog

**Test Requirements (TDD):**
```typescript
// Write these failing tests FIRST
describe('Volunteer Position Management', () => {
  it('should show only event-specific sessions in dropdown')
  it('should process Add Position form and update grid')
  it('should load position data into edit form correctly')
  it('should save position changes without page refresh')
  it('should update grid immediately after changes')
  it('should delete positions with confirmation')
})
```

### Story 4: UI Consistency Implementation
**As an** Event Organizer  
**I want to** consistent UI patterns across all management tabs  
**So that** I have predictable and intuitive interaction patterns

**Acceptance Criteria:**
- Given I navigate between event management tabs
- When I interact with forms and grids
- Then ALL tabs use modal dialogs for add/edit operations (not inline forms)
- And ALL tables follow standardized format: Edit first column, Delete last column
- And volunteer position management uses modal instead of bottom form
- And there is an "Add New Position" button below the volunteer grid
- And ALL modals follow consistent styling and behavior patterns
- And ALL buttons use standardized styling from Design System v7

**Test Requirements (TDD):**
```typescript
// Write these failing tests FIRST
describe('UI Consistency', () => {
  it('should use modals for all add/edit operations')
  it('should follow Edit-first-Delete-last table pattern')
  it('should have Add New Position button below volunteer grid')
  it('should apply consistent modal styling')
  it('should use Design System v7 button styles')
})
```

### Story 5: Comprehensive Seed Data Implementation
**As a** Developer or Event Organizer  
**I want to** complete seed data for events, sessions, tickets, and purchases  
**So that** I can test and demonstrate full system functionality

**Acceptance Criteria:**
- Given the system starts with fresh database
- When seed data is applied
- Then I see realistic events with multiple sessions (S1, S2, S3)
- And each event has various ticket types with different session combinations
- And some ticket types show "sold" quantities for testing
- And volunteer positions exist with session assignments
- And purchased tickets exist showing registration scenarios
- And all data relationships work correctly for complex scenarios

**Test Requirements (TDD):**
```typescript
// Write these failing tests FIRST
describe('Comprehensive Seed Data', () => {
  it('should create events with multiple sessions')
  it('should create ticket types for session combinations')
  it('should create purchased tickets showing sales')
  it('should create volunteer positions with session assignments')
  it('should maintain proper data relationships')
  it('should support complex registration scenarios')
})
```

## Business Rules

### Session Management Rules
1. **API-First Operations**: All session CRUD operations MUST use API calls, never page refresh
2. **S# ID Consistency**: Sessions auto-assigned S1, S2, S3 format, user-editable names
3. **Modal UI Pattern**: All session add/edit operations use modal dialogs, not inline forms
4. **Immediate UI Updates**: Session grid updates immediately after API success response
5. **Validation Requirements**: Session times, dates, and capacity validated before API submission

### Ticket Type Dependency Rules
1. **Session Prerequisite**: Ticket types can only be created when at least one session exists
2. **Event-Scoped Sessions**: Session dropdowns show ONLY sessions for current event, not global sessions
3. **Capacity Validation**: Ticket quantities cannot exceed lowest capacity of associated sessions
4. **S# Format Display**: All session references display in S1, S2, S3 format throughout UI

### Volunteer Position Rules
1. **Event-Scoped Sessions**: Position session dropdowns filtered to current event only
2. **Modal UI Consistency**: Convert bottom form to modal dialog matching other tabs
3. **Session Assignment**: Positions can be assigned to specific sessions (S1, S2) or All sessions
4. **Grid Interaction**: Edit button loads data, form processes correctly without refresh

### UI Consistency Rules
1. **Modal Pattern Standard**: ALL tabs use modals for add/edit operations, NO inline forms
2. **Table Standardization**: ALL tables follow Edit (first column) → Delete (last column) pattern
3. **Button Placement**: "Add New" buttons positioned below their respective grids
4. **Design System v7**: ALL styling uses standardized CSS classes, no custom styling
5. **Visual Feedback**: Loading states, success/error notifications for all API operations

### Data Integrity Rules
1. **Foreign Key Enforcement**: All session-ticket associations must reference valid sessions
2. **Cascade Operations**: Deleting sessions requires handling dependent ticket types and positions
3. **Audit Trail**: All CRUD operations logged for debugging and data integrity
4. **Concurrency Handling**: Prevent simultaneous edits of same entity by multiple users

## Data Structure Requirements

### Session Management API
- sessionId: string (auto-generated S# format, required)
- sessionName: string (user-editable, required, 3-100 characters)
- eventId: string (required, foreign key to Event)
- sessionDate: DateTime (required, ISO 8601 format)
- startTime: TimeSpan (required, HH:MM format)
- endTime: TimeSpan (required, HH:MM format, must be after startTime)
- capacityLimit: integer (required, minimum 1, maximum 1000)
- currentRegistrations: integer (calculated, read-only)
- isActive: boolean (required, default true)
- createdAt: DateTime (system-generated)
- updatedAt: DateTime (system-generated)

### Ticket Type Enhanced Data
- ticketTypeId: string (required, UUID)
- ticketTypeName: string (required, 3-100 characters)
- eventId: string (required, foreign key to Event)
- includedSessions: List<string> (required, session IDs in S# format)
- priceIndividual: decimal (required, minimum 0)
- priceCouples: decimal (optional, for couples tickets)
- quantityAvailable: integer (required, minimum 1)
- quantitySold: integer (calculated, read-only)
- salesStartDate: DateTime (required, ISO 8601 format)
- salesEndDate: DateTime (required, ISO 8601 format)
- isActive: boolean (required, default true)

### Volunteer Position Enhanced Data
- positionId: string (required, UUID)
- positionName: string (required, 3-100 characters)
- eventId: string (required, foreign key to Event)
- assignedSessions: List<string> (required, S# format or "All")
- startTime: TimeSpan (required, HH:MM format)
- endTime: TimeSpan (required, HH:MM format)
- description: string (optional, 500 characters max)
- volunteersNeeded: integer (required, minimum 1)
- volunteersAssigned: List<string> (user IDs, managed separately)
- skillsRequired: List<string> (optional, predefined skill list)
- isActive: boolean (required, default true)

### Seed Data Entities
- MultiSessionEvent: 3-day workshop with S1, S2, S3 sessions
- SingleSessionEvent: One-day class with S1 session only
- SocialEvent: Munch with optional ticket for special dinner
- TicketTypes: Full Series, Day 1 Only, Day 2-3, Individual Sessions
- VolunteerPositions: Setup Crew (S1), Safety Monitor (All), Cleanup (S3)
- PurchasedTickets: Various registration scenarios showing sold quantities

## TDD Implementation Requirements

### Test-First Development Mandates
1. **Write Failing Tests First**: Every bug fix starts with failing test that reproduces the issue
2. **Red-Green-Refactor**: Follow strict TDD cycle for all implementations
3. **API Integration Tests**: Mock API responses, test actual integration separately
4. **UI Component Tests**: Test React components with realistic props and state
5. **E2E Scenario Tests**: Full user workflows using Playwright

### Test Coverage Requirements
- **Unit Tests**: 95% coverage for all business logic and API interactions
- **Component Tests**: 90% coverage for UI components and user interactions
- **Integration Tests**: 100% coverage for API-UI data flow scenarios
- **E2E Tests**: 100% coverage for critical user workflows

### Test Structure Standards
```typescript
// Required test structure for each bug fix
describe('Bug Fix: [Description]', () => {
  describe('Current Broken Behavior', () => {
    it('should fail with current implementation')
  })
  
  describe('Fixed Behavior', () => {
    it('should pass after implementation')
    it('should handle edge cases')
    it('should provide proper error handling')
  })
  
  describe('Integration Scenarios', () => {
    it('should work end-to-end in realistic scenarios')
  })
})
```

## Constraints & Assumptions

### Technical Constraints
- **React Architecture**: Must integrate with existing React + TypeScript + Mantine v7 setup
- **API Integration**: Must use existing .NET Minimal API endpoints with proper DTO alignment
- **State Management**: Must use TanStack Query for API state, Zustand for app state
- **NSwag Compliance**: All type definitions auto-generated, no manual DTO interfaces
- **Design System**: Must use Design System v7 components and styling exclusively

### Business Constraints
- **User Experience**: No breaking changes to existing successful workflows
- **Performance**: All operations must complete within 200ms for responsive feel
- **Data Integrity**: Cannot compromise existing event data during bug fixes
- **Security**: All operations must respect existing role-based permissions
- **Accessibility**: Must maintain WCAG 2.1 compliance across all changes

### Development Assumptions
- **TDD Adoption**: Development team trained in Test-Driven Development practices
- **API Stability**: Existing event-related API endpoints are stable and tested
- **Component Library**: Mantine v7 components provide necessary form and modal capabilities
- **Database**: PostgreSQL data relationships properly configured for session-ticket dependencies
- **Testing Infrastructure**: Jest, React Testing Library, and Playwright properly configured

## Security & Privacy Requirements

### API Security
- **Authentication**: All API calls include valid JWT tokens in httpOnly cookies
- **Authorization**: Event management limited to Event Organizers and Admins only
- **Input Validation**: All form inputs validated client-side AND server-side
- **CSRF Protection**: All state-changing operations protected against CSRF attacks
- **SQL Injection**: All database operations use parameterized queries exclusively

### Data Privacy
- **Event Data**: Event configuration visible only to authorized Event Organizers
- **Member Data**: No exposure of sensitive member information in admin interfaces
- **Audit Logging**: All CRUD operations logged with user attribution for accountability
- **Error Handling**: Error messages do not expose sensitive system information

## Compliance Requirements

### Platform Standards
- **WitchCityRope Patterns**: Must follow established event management patterns
- **Community Safety**: Changes must not compromise existing safety and vetting systems
- **Accessibility**: Full WCAG 2.1 AA compliance for all modified interfaces
- **Mobile Responsiveness**: All changes must work properly on mobile devices

### Development Standards
- **Code Quality**: All code must pass ESLint, Prettier, and TypeScript strict checks
- **Documentation**: All complex business logic documented inline and in external docs
- **Version Control**: All changes committed with proper commit message standards
- **Review Process**: All bug fixes require code review before merge

## User Impact Analysis

| User Type | Impact | Priority | Changes |
|-----------|--------|----------|---------|
| **Event Organizers** | Critical Positive - Restores essential functionality | HIGH | Session management works, ticket creation enabled, volunteer management fixed |
| **Admins** | High Positive - Same functionality as Event Organizers | HIGH | Full event administration capabilities restored |
| **Teachers** | Low - Indirect benefit from improved event setup | MEDIUM | Events properly configured when they're assigned to teach |
| **Members** | Medium - Better event experience from proper setup | MEDIUM | Events properly configured with sessions and tickets available |
| **Developers** | High Positive - TDD approach improves code quality | HIGH | Comprehensive tests prevent regression, easier debugging |

## Examples/Scenarios

### Scenario 1: Session Creation Bug Fix
1. **Before Fix**: Event Organizer fills session form → clicks Save → page refreshes → session not created
2. **TDD Process**: Write failing test → implement API integration → test passes → UI updates immediately
3. **After Fix**: Event Organizer fills session form → clicks Save → modal closes → session appears in grid with S1 ID

### Scenario 2: Ticket Creation Dependency Resolution
1. **Before Fix**: Cannot create tickets because sessions required but session creation broken
2. **TDD Process**: Write failing test for session requirement → fix session creation → implement ticket-session relationship
3. **After Fix**: Create sessions S1, S2 → create "Full Weekend" ticket including S1,S2 → create "Saturday Only" ticket for S1

### Scenario 3: Volunteer Position Management Fix
1. **Before Fix**: Sessions dropdown shows all platform sessions → Edit doesn't load data → Add Position does nothing
2. **TDD Process**: Write failing tests for each broken behavior → implement proper filtering and form handling
3. **After Fix**: Sessions dropdown shows only S1, S2, S3 for current event → Edit loads position data → Add creates position

### Scenario 4: UI Consistency Implementation
1. **Before Fix**: Tickets use modals, volunteers use bottom form → inconsistent interaction patterns
2. **TDD Process**: Write tests for modal consistency → convert volunteer form to modal → standardize button placement
3. **After Fix**: All tabs use consistent modal patterns → "Add New Position" button below grid → all tables standardized

### Scenario 5: Complete Event Setup with Seed Data
1. **With Seed Data**: System starts with realistic events already configured
2. **Event 1**: "3-Day Advanced Workshop" with S1, S2, S3 sessions → ticket types for different combinations → volunteer positions assigned
3. **Event 2**: "Beginner Class" with S1 session only → single ticket type → minimal volunteer needs
4. **Testing**: Developers can immediately test all workflows with realistic data

## Questions for Product Manager
- [ ] Should session deletion cascade to remove dependent ticket types and volunteer positions?
- [ ] What should happen when editing session capacity below current ticket sales?
- [ ] Should we implement optimistic UI updates or wait for API confirmation?
- [ ] How should we handle concurrent edits of the same session by multiple users?
- [ ] Should the volunteer position form conversion to modal happen in this bug fix or separate enhancement?

## Quality Gate Checklist (100% Required for TDD)

### Test-Driven Development Requirements
- [ ] All failing tests written before any implementation code
- [ ] Red-Green-Refactor cycle followed for each bug fix
- [ ] Unit tests cover all business logic and API interactions
- [ ] Component tests validate UI behavior with realistic data
- [ ] Integration tests verify API-UI data flow end-to-end
- [ ] E2E tests validate complete user workflows

### Functional Requirements
- [ ] Session creation works without page refresh
- [ ] Session grid updates immediately after API operations
- [ ] S# ID format consistently applied and displayed
- [ ] Ticket creation enabled when sessions exist
- [ ] Session dropdowns filtered to event-specific sessions only
- [ ] Volunteer position CRUD operations function correctly
- [ ] UI consistency implemented across all tabs

### Technical Requirements
- [ ] All API operations use proper error handling
- [ ] NSwag-generated types used exclusively (no manual interfaces)
- [ ] Design System v7 styling applied consistently
- [ ] Modal dialogs replace inline forms for consistency
- [ ] Loading states and user feedback implemented
- [ ] Form validation implemented client-side and server-side

### Data Requirements
- [ ] Comprehensive seed data created for all scenarios
- [ ] Session-ticket relationships properly maintained
- [ ] Volunteer position-session assignments working
- [ ] Cascade operations handled properly
- [ ] Data integrity maintained across all operations

### Quality Assurance
- [ ] Cross-browser testing completed
- [ ] Mobile responsiveness verified
- [ ] Accessibility requirements met (WCAG 2.1 AA)
- [ ] Performance requirements met (<200ms operations)
- [ ] Security requirements validated
- [ ] Code review completed with approval

This comprehensive requirements document ensures that all bugs are fixed using proper TDD methodology while maintaining system integrity and providing excellent user experience for Event Organizers managing complex multi-session events.