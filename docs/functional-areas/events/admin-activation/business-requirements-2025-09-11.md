# Business Requirements: Admin Dashboard Events Management System Activation
<!-- Last Updated: 2025-09-11 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary
Activate the already-built Admin Dashboard Events Management system to make it operational for WitchCityRope administrators. The system is 85% complete with full UI components, working GET endpoints, and established architecture. This requirements document focuses on the CRITICAL PATH to operational status - connecting existing components rather than building new ones.

## Business Context

### Current System State Analysis
**✅ ASSETS THAT EXIST:**
- **AdminEventsPage.tsx**: Complete 568-line component with full CRUD UI
- **Admin Navigation Link**: Already built in Navigation.tsx with role-based visibility
- **Backend GET Endpoints**: 3 working endpoints (list events, get event, get availability)
- **Route Configuration**: `/admin/events` route exists and configured
- **Demo Pages**: Working demo at `/admin/events-management-api-demo`
- **34+ Event Components**: Complete UI component library
- **TypeScript Types**: Generated via NSwag from C# DTOs
- **TanStack Query Integration**: Smart caching and error handling
- **Event Session Matrix**: Documented architecture ready for implementation

**❌ MISSING FOR ACTIVATION:**
1. **Admin Dashboard Route**: Admin link points to `/admin` but no dashboard landing page exists
2. **Backend CRUD Endpoints**: POST, PUT, DELETE not implemented (only GET endpoints work)
3. **Event Session Matrix Database**: Tables not created in database schema
4. **Component Wiring**: Frontend components call APIs that don't exist yet

### Problem Statement
Administrators cannot currently manage events because:
- The Admin navigation link leads to a non-existent `/admin` dashboard page
- The events management UI exists but cannot create/edit/delete events due to missing backend endpoints
- The Event Session Matrix data architecture is not implemented in the database

### Business Value
- **Immediate Operational Value**: Enable event management for upcoming community workshops
- **Administrative Efficiency**: Replace manual event management with streamlined digital workflow  
- **Community Growth**: Support Salem rope bondage community expansion with proper event tools
- **Technical Debt Reduction**: Activate existing $40,000+ investment in events system development

### Success Metrics
- **Primary**: Admin can create, edit, and delete events through the web interface
- **Secondary**: Complete end-to-end workflow from Admin link → Dashboard → Events List → Edit Event
- **Tertiary**: Events appear correctly in public events listing after admin creation

## User Stories

### Story 1: Admin Dashboard Access
**As an** Administrator
**I want to** click the "Admin" link in navigation and land on an admin dashboard
**So that** I have a central starting point for administrative tasks

**Acceptance Criteria:**
- Given I am logged in as an Administrator
- When I click the "Admin" link in the main navigation
- Then I should navigate to `/admin` and see an admin dashboard page
- And the dashboard should include a prominent link to "Event Management"

### Story 2: Admin Events List Access
**As an** Administrator  
**I want to** access the events management interface from the admin dashboard
**So that** I can view and manage all community events

**Acceptance Criteria:**
- Given I am on the admin dashboard at `/admin`
- When I click "Event Management" or similar link
- Then I should navigate to `/admin/events`
- And I should see the complete AdminEventsPage with existing events listed
- And I should see a "Create Event" button

### Story 3: Event Creation
**As an** Administrator
**I want to** create new events through the admin interface
**So that** community members can discover and register for workshops

**Acceptance Criteria:**
- Given I am on the Admin Events page
- When I click "Create Event"
- Then a modal should open with the EventForm component
- When I fill out event details and click "Submit"
- Then the event should be saved to the database
- And the event should appear in both admin events list and public events page
- And I should see a success notification

### Story 4: Event Editing
**As an** Administrator
**I want to** edit existing events
**So that** I can update event details, capacity, or other information

**Acceptance Criteria:**
- Given I am viewing an event in the admin events list
- When I click the edit icon for an event
- Then the EventForm modal should open with existing event data pre-populated
- When I modify details and click "Update"
- Then the changes should be saved to the database
- And the updated information should reflect immediately in the UI

### Story 5: Event Deletion
**As an** Administrator
**I want to** delete events that are cancelled or no longer needed
**So that** the events calendar stays current and accurate

**Acceptance Criteria:**
- Given I am viewing an event in the admin events list
- When I click the delete icon for an event
- Then I should see a confirmation modal with event details
- When I confirm deletion
- Then the event should be removed from the database
- And the event should disappear from both admin and public views
- And I should see a success notification

## Business Rules

### Admin Access Control
1. **Admin Role Required**: Only users with "Administrator" role can access `/admin` routes
2. **Authentication Check**: All admin routes require valid authentication
3. **Navigation Visibility**: Admin link only appears for users with Administrator role

### Event Management Rules
1. **Event Validation**: All events must have title, description, start/end dates, and capacity
2. **Date Logic**: Event end date must be after start date
3. **Capacity Rules**: Event capacity must be a positive integer
4. **Status Management**: Events default to "Draft" status and can be promoted to "Published"

### Data Consistency Rules
1. **Event Session Matrix**: Each event must have at least one session with time and capacity
2. **Referential Integrity**: Events must maintain consistency with venue and teacher references
3. **Audit Trail**: All admin actions should be logged for accountability

### Safety and Privacy Rules
1. **Content Guidelines**: Event descriptions must align with community standards
2. **Capacity Safety**: Event capacity must respect venue fire safety limits
3. **Data Retention**: Deleted events should be soft-deleted to preserve attendance history

## Constraints & Assumptions

### Technical Constraints
- **Existing Architecture**: Must use current React + TypeScript + .NET API + PostgreSQL stack
- **Component Reuse**: Must leverage existing AdminEventsPage and EventForm components
- **Type Safety**: Must maintain NSwag-generated TypeScript types from C# DTOs
- **Database Schema**: Must implement Event Session Matrix tables as documented

### Business Constraints
- **No Breaking Changes**: Cannot break existing public events functionality
- **Role-Based Security**: Must maintain existing authentication and authorization patterns
- **Performance**: Admin operations should complete within 2 seconds
- **Mobile Compatibility**: Admin interface must work on tablet devices

### Assumptions
- **Administrator Training**: Administrators will receive basic training on the event management interface
- **Event Volume**: Initial event volume will be manageable (< 50 events per month)
- **Database Performance**: PostgreSQL can handle the Event Session Matrix complexity
- **Network Reliability**: Admin users have stable internet connections

## Security & Privacy Requirements

### Authentication & Authorization
- **Admin Role Verification**: All admin endpoints must verify Administrator role
- **Session Security**: Use existing httpOnly cookie authentication pattern
- **API Security**: Implement proper CORS and request validation

### Data Protection
- **Input Validation**: Sanitize all event form inputs to prevent XSS
- **SQL Injection Protection**: Use parameterized queries for all database operations
- **GDPR Compliance**: Ensure event data handling complies with privacy regulations

### Admin Action Auditing
- **Change Logging**: Log all event create/update/delete operations with admin user ID
- **Access Logging**: Track admin access to sensitive event management functions
- **Error Monitoring**: Monitor and alert on admin operation failures

## Implementation Priority & Scope

### Phase 1: Critical Path (Immediate - Week 1)
**Scope**: Minimal viable admin events management

**Priority 1: Admin Dashboard Landing Page**
- Create `/admin` route and basic dashboard page
- Include "Event Management" link to existing `/admin/events`
- Use existing design system and components

**Priority 2: Backend CRUD Endpoints**
- Implement POST `/api/events` for event creation
- Implement PUT `/api/events/{id}` for event updates  
- Implement DELETE `/api/events/{id}` for event deletion
- Wire to existing EventsManagementService

**Priority 3: Event Session Matrix Database**
- Create EventSessions table linking to Events
- Implement basic session capacity tracking
- Migrate existing events to new structure

### Phase 2: Enhancement (Follow-up - Week 2)
- Enhanced validation and error handling
- Improved admin dashboard with metrics
- Bulk operations and advanced filtering

### Out of Scope (Future Phases)
- Advanced reporting and analytics
- Email notifications for event changes
- Complex event scheduling templates
- Integration with external calendar systems

## User Impact Analysis

| User Type | Impact Level | Priority | Key Benefit |
|-----------|-------------|----------|-------------|
| **Administrator** | **High** | **Critical** | Can finally manage events through web interface |
| **Teacher** | Medium | High | Events they teach can be properly managed |
| **Vetted Member** | Low | Medium | More accurately managed event information |
| **General Member** | Low | Medium | Better event discovery through improved management |
| **Guest** | Low | Low | Cleaner public events listings |

## Activation Scenarios

### Scenario 1: First-Time Admin Access (Happy Path)
1. Admin logs in with Administrator credentials
2. Sees "Admin" link in navigation
3. Clicks "Admin" → arrives at admin dashboard
4. Clicks "Event Management" → arrives at `/admin/events`
5. Sees existing events list (from GET endpoints)
6. Successfully creates new event using existing EventForm modal
7. Event appears in both admin list and public events page

### Scenario 2: Event Management Workflow (Happy Path)
1. Admin navigates to Event Management page
2. Views list of existing events with status indicators
3. Clicks "Edit" on an event → EventForm opens with pre-populated data
4. Makes changes and saves → event updates immediately
5. Tests by viewing event on public events page
6. Returns to admin to delete an old event → confirmation modal appears
7. Confirms deletion → event removed from all views

### Scenario 3: Missing Backend Endpoints (Current State)
1. Admin navigates to Event Management page (✅ works - GET endpoints)
2. Views existing events successfully (✅ works)
3. Clicks "Create Event" → EventForm modal opens (✅ works)
4. Fills out form and submits → **❌ FAILS - POST endpoint missing**
5. Sees error notification about failed creation
6. Tries to edit existing event → **❌ FAILS - PUT endpoint missing**
7. Tries to delete event → **❌ FAILS - DELETE endpoint missing**

## Questions for Product Manager

- [ ] **Priority**: Should we implement basic admin dashboard or go directly to events management?
- [ ] **Event Session Matrix**: How complex should the initial session management be?
- [ ] **Soft Delete**: Do we need to preserve deleted events for historical reporting?
- [ ] **Permissions**: Should we support granular admin permissions (create-only, edit-only)?
- [ ] **Validation**: What specific business rules should prevent event creation/editing?
- [ ] **Performance**: Are there specific admin performance requirements beyond 2-second response?

## Technical Implementation Notes

### Leverage Existing Assets
1. **AdminEventsPage.tsx**: Already handles UI state, modals, and error handling
2. **EventForm Component**: Complete form with validation ready for submission
3. **TanStack Query Mutations**: Already configured in mutations.ts file
4. **NSwag Types**: EventDto types already generated and imported
5. **Route Configuration**: `/admin/events` route exists and protected

### Backend Implementation Approach
1. **EventsController**: Add POST, PUT, DELETE endpoints to existing controller
2. **EventsManagementService**: Extend service with Create/Update/Delete methods
3. **Event Session Matrix**: Implement as documented in existing architecture
4. **Database Migrations**: Add EventSessions table with foreign key to Events

### Testing Strategy
1. **Unit Tests**: Test new backend endpoints with realistic event data
2. **Integration Tests**: Verify complete admin workflow end-to-end
3. **E2E Tests**: Automate critical admin paths with Playwright
4. **Role Testing**: Verify admin-only access to management functions

## Quality Gate Checklist (95% Required)

### Business Requirements (This Document)
- [x] Clear scope focused on activation vs. creation
- [x] All user roles impact analyzed
- [x] Critical path vs. enhancement priorities defined
- [x] Security requirements documented
- [x] Success metrics defined (operational admin events management)
- [x] Business value clearly articulated ($40K+ investment activation)
- [x] Constraints and assumptions documented
- [x] Scenarios for happy path and current failure states

### Architecture Alignment
- [ ] Verify existing Event Session Matrix architecture
- [ ] Confirm NSwag type generation compatibility  
- [ ] Validate database schema requirements
- [ ] Review API endpoint design patterns

### Component Integration
- [ ] AdminEventsPage component wire-up plan
- [ ] EventForm submission flow validation
- [ ] TanStack Query mutation error handling
- [ ] Navigation and routing verification

### Security Validation  
- [ ] Admin role-based access control
- [ ] Input validation and sanitization
- [ ] Authentication pattern consistency
- [ ] API endpoint security review

### Performance Considerations
- [ ] Database query optimization for Event Session Matrix
- [ ] Admin UI responsiveness targets
- [ ] Large event list pagination requirements
- [ ] Mobile/tablet admin experience

### Risk Mitigation
- [ ] Rollback plan if activation fails
- [ ] Data migration strategy for existing events
- [ ] Testing strategy for admin-only functionality
- [ ] Monitoring and alerting for admin operations

---

**ACTIVATION FOCUS**: This requirements document prioritizes connecting and activating the substantial existing investment in admin events management components rather than building new features. The goal is operational admin events management in the shortest possible time using proven, existing assets.