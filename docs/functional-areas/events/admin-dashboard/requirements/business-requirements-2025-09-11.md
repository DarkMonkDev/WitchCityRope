# Business Requirements: Admin Events Dashboard Enhancement
<!-- Last Updated: 2025-09-11 -->
<!-- Version: 1.0 -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Draft -->

## Executive Summary
The Admin Events Dashboard enhancement transforms the current card-based event management interface into a streamlined table view with advanced filtering, real-time search, and improved navigation. This enhancement addresses admin user feedback requesting simplified event management with better visibility into capacity and ticket sales.

## Business Context

### Problem Statement
The current admin events dashboard uses a card-based layout that, while visually appealing, creates inefficiencies for administrators managing multiple events:
- Difficult to quickly compare events at a glance
- No ability to sort by critical fields like date or capacity
- Limited filtering options for finding specific event types
- No search functionality for rapid event location
- Past events visibility requires scrolling through mixed content
- Action buttons scattered across individual cards reduce workflow efficiency

### Business Value
- **Improved Administrative Efficiency**: 60% reduction in time to locate and manage events
- **Better Resource Planning**: Clear capacity visualization helps optimize event scheduling  
- **Enhanced Event Oversight**: Sortable columns and filtering provide comprehensive event portfolio view
- **Streamlined Operations**: Single-click navigation to event editing reduces friction
- **Improved Decision Making**: Real-time capacity indicators support better event management

### Success Metrics
- Average time to find specific event reduced from 45 seconds to 15 seconds
- Event management task completion rate increased by 40%
- User satisfaction score improved from 3.2/5 to 4.5/5 for dashboard usability
- Reduction in support tickets related to event finding/management by 50%

## Data Structure Requirements

### Event Dashboard Data
Based on existing EventDto structure from API:
- id: string (GUID, required)
- title: string (required, max 200 characters)
- startDateTime: DateTime (required, ISO 8601)
- endDateTime: DateTime (required, ISO 8601)  
- capacity: number (required, min 1)
- currentAttendees: number (required, default 0)
- status: EventStatus enum (Published, Draft, Cancelled)
- eventType: EventType enum (Class, Social, Workshop, Performance)
- venueId: string (optional)
- venueName: string (optional)

### Business Rules
1. **Event Display Logic**: Only show events in Published status by default
2. **Past Events**: Events are considered past when endDateTime < current time
3. **Capacity Visualization**: Show current/max attendees with visual progress indicator
4. **Time Display**: Format as "HH:MM AM/PM - HH:MM AM/PM" pattern
5. **Date Sorting**: Default sort by startDateTime ascending (upcoming events first)
6. **Search Scope**: Search applies to event title only, case-insensitive
7. **Filter Persistence**: Filter states persist during session but reset on page reload

## User Stories

### Story 1: Table View Navigation
**As an** Admin
**I want to** view events in a sortable table format
**So that** I can quickly compare multiple events and manage them efficiently

**Acceptance Criteria:**
- Given I'm on the admin events dashboard
- When the page loads
- Then I see events displayed in a table with columns: Date, Event Title, Time, Capacity/Tickets
- And Date and Event Title columns are sortable (indicated by visual cues)
- And clicking a table row navigates to the event edit page
- And the table shows clear visual hierarchy with alternating row colors

### Story 2: Event Type Filtering  
**As an** Admin
**I want to** filter events by type using clickable filter words
**So that** I can focus on specific categories of events

**Acceptance Criteria:**
- Given I'm viewing the events dashboard
- When I see filter controls at the top
- Then I see clickable words "Social" and "Class" 
- And both filters can be independently selected/deselected
- And selected filters are visually highlighted
- And the events table updates in real-time based on selected filters
- And "All" state shows all event types when no specific filters are selected

### Story 3: Past Events Visibility Control
**As an** Admin  
**I want to** toggle visibility of past events
**So that** I can focus on upcoming events or review historical data as needed

**Acceptance Criteria:**
- Given I'm on the events dashboard
- When I see a "Show Past Events" checkbox/toggle control
- Then checking it includes past events in the table display
- And unchecking it hides past events (default state)
- And past events appear in the same table format without special color differentiation
- And the control state affects both filtered and unfiltered views

### Story 4: Real-Time Search
**As an** Admin
**I want to** search events as I type
**So that** I can rapidly locate specific events without scrolling

**Acceptance Criteria:**
- Given I'm on the events dashboard
- When I type in the search bar
- Then the events table filters in real-time showing matching results
- And search matches against event titles (case-insensitive)
- And clearing the search bar shows all events again
- And search works in combination with type filters and past events toggle

### Story 5: Capacity Visualization
**As an** Admin
**I want to** see capacity information with visual indicators  
**So that** I can quickly identify events approaching capacity limits

**Acceptance Criteria:**
- Given I'm viewing the events table
- When I look at the Capacity column
- Then I see current registrations as a fraction (e.g., "4/20")
- And there's a visual progress bar showing capacity utilization
- And high capacity events (>80%) are visually emphasized
- And sold-out events are clearly marked

### Story 6: Quick Event Actions
**As an** Admin
**I want to** copy existing events and navigate to editing
**So that** I can efficiently create similar events and modify existing ones

**Acceptance Criteria:**
- Given I'm viewing an event row
- When I see the actions column
- Then I see a "Copy" button for each event
- And clicking Copy creates a draft copy of the event with a new date
- And clicking anywhere else on the row navigates to the event edit page
- And the edit navigation opens as a full page (not modal)
- And I can navigate back to dashboard via breadcrumbs

### Story 7: Mobile Responsive Experience
**As an** Admin using a mobile device
**I want to** manage events effectively on smaller screens
**So that** I can perform administrative tasks while away from my computer

**Acceptance Criteria:**
- Given I'm accessing the dashboard on a mobile device
- When the screen width is < 768px
- Then the table remains functional with horizontal scroll if needed
- And critical columns (Date, Title, Actions) remain visible
- And filter controls stack vertically for easier access
- And touch targets are appropriately sized for mobile interaction

## Functional Requirements

### Header Controls
1. **Create New Event Button**: Positioned at same level as page title for prominence
2. **Event Type Filters**: Two clickable words "Social" and "Class" with independent selection
3. **Past Events Toggle**: Checkbox or toggle control with clear labeling
4. **Search Bar**: Real-time filtering input with placeholder text and search icon

### Events Table
1. **Column Structure**: Date (sortable), Event Title (sortable), Time, Capacity/Tickets with visual bar
2. **Row Interaction**: Click anywhere on row navigates to event edit page
3. **Visual Design**: Alternating row colors, hover effects, clear typography hierarchy
4. **Loading States**: Skeleton loader during API calls
5. **Empty States**: Helpful message when no events match current filters

### Navigation & Breadcrumbs
1. **Event Edit Navigation**: Full page edit form (not modal)
2. **Back Navigation**: Clear breadcrumb trail: "Dashboard > Events > Edit Event Name"
3. **Unsaved Changes**: Warning if navigating away from edit form with unsaved changes

## Non-Functional Requirements

### Performance
- Table renders within 200ms for up to 500 events
- Search filtering responds within 100ms of keystroke
- Sort operations complete within 150ms
- API calls use pagination for events > 100 items

### Usability
- Filter states provide immediate visual feedback
- Keyboard navigation support for accessibility
- Consistent with existing WitchCityRope design system
- Responsive design supports 320px to 1920px screen widths

### Data Quality
- All dates display in consistent format (MMM DD, YYYY)
- Time ranges show duration implicitly through start/end times
- Capacity indicators accurately reflect real-time registration data
- Sort operations handle null/undefined values gracefully

## Security & Privacy Requirements
- Admin role verification required for dashboard access
- Event data filtered based on admin permissions
- Copy functionality creates events owned by current admin
- Edit navigation includes CSRF protection
- Search functionality doesn't expose sensitive event details in URLs

## Integration Requirements

### API Dependencies
- GET /api/events (with filtering, sorting, pagination parameters)
- POST /api/events (for copy functionality)
- Authentication validation for admin role
- Real-time capacity updates via existing polling or SignalR

### Frontend Dependencies  
- React Router for navigation management
- TanStack Query for data fetching and caching
- Mantine components for UI consistency
- Generated TypeScript types from NSwag pipeline

## Migration Considerations

### From Current Implementation
The existing AdminEventsPage.tsx uses a card-based layout with Mantine Grid components. Migration involves:

1. **Component Replacement**: Replace Grid/Paper components with Table components
2. **Data Processing**: Modify formatEventDisplay helper to support table requirements  
3. **State Management**: Add filter and search state management
4. **Event Handlers**: Update handleEditEvent to use row click navigation
5. **Styling**: Migrate from card-specific styles to table-based design system

### Backwards Compatibility
- Maintain existing API endpoints and data structures
- Preserve event creation and editing workflows
- Keep existing user permissions and role checks
- Maintain mobile responsiveness standards

## Success Criteria Validation

### User Acceptance Testing
- Admin users can complete event location tasks 60% faster
- Filter combinations work as expected in all scenarios
- Mobile users can effectively use table interface
- Search functionality finds relevant events consistently

### Technical Validation
- Table performance meets requirements under load
- Sort operations maintain data integrity
- Filter combinations don't create conflicting states
- Navigation maintains proper browser history

## Dependencies on Existing Systems

### Critical Dependencies
- EventDto structure from API remains stable
- Admin authentication and authorization system
- Existing event creation/editing workflows
- Current venue and capacity management logic

### Nice-to-Have Integrations  
- Real-time updates for capacity changes
- Export functionality for event data
- Bulk operations for multiple events
- Advanced filtering (date ranges, venue, teacher)

## Questions for Product Manager
- [ ] Should table support bulk selection for mass operations?
- [ ] Do we need export functionality (CSV/PDF) for event lists?
- [ ] ~~Should past events have different visual treatment?~~ (Stakeholder confirmed: No special coloring needed)
- [ ] Is there a preference for sort indicator style (arrows vs other icons)?
- [ ] Should search include additional fields beyond event title?
- [ ] Do we need advanced date range filtering for large event portfolios?

## Quality Gate Checklist (95% Required)
- [x] All user roles addressed (Admin focus confirmed)
- [x] Clear acceptance criteria for each story  
- [x] Business value clearly defined with metrics
- [x] Edge cases considered (empty states, mobile, performance)
- [x] Security requirements documented (admin access, CSRF)
- [x] Compliance requirements checked (existing auth system)
- [x] Performance expectations set (response times specified)
- [x] Mobile experience considered (responsive design requirements)
- [x] Examples provided (capacity display, filter interactions)
- [x] Success metrics defined (efficiency gains, satisfaction scores)

## Examples/Scenarios

### Scenario 1: Finding Upcoming Classes
**Context**: Admin needs to check capacity for next week's classes
**Steps**:
1. Admin opens events dashboard (shows current/future events by default)
2. Clicks "Class" filter to hide social events  
3. Views table sorted by date (default) to see chronological listing
4. Scans Capacity column visual indicators to identify high-capacity events
5. Clicks on event row with >80% capacity to edit and monitor

### Scenario 2: Creating Similar Event
**Context**: Admin wants to copy successful workshop for next month
**Steps**:
1. Admin searches for "Advanced Shibari" in search bar
2. Locates desired event in filtered results
3. Clicks "Copy" button in actions column
4. System creates draft copy and navigates to edit form
5. Admin updates date and publishes new event

### Scenario 3: Mobile Event Management
**Context**: Admin needs to check weekend events while out of office
**Steps**:
1. Admin accesses dashboard on mobile device
2. Table displays with horizontal scroll for full column access
3. Uses touch controls to sort by date
4. Checks capacity indicators to identify any issues
5. Taps event row to navigate to mobile-optimized edit form

This requirements document provides the foundation for transforming the Admin Events Dashboard from a card-based interface to an efficient table-driven management tool that meets the specific needs of WitchCityRope administrators.