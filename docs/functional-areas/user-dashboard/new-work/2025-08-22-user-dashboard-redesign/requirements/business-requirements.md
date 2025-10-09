# Business Requirements: User Dashboard Redesign (CONSOLIDATED)
<!-- Last Updated: 2025-10-08 -->
<!-- Version: 4.0 - CONSOLIDATION UPDATE: Simplified Two-Page Structure -->
<!-- Owner: Business Requirements Agent -->
<!-- Status: Consolidated -->

## 🚨 CONSOLIDATION UPDATE 🚨

**Major simplification based on October 2025 stakeholder feedback:**
- **REMOVED**: Right-side menu/status area entirely
- **TWO PRIMARY PAGES**: Dashboard Landing (`/dashboard`) + Events Page (`/dashboard/events`)
- **Dashboard Landing**: Welcome message, upcoming events (3-5 cards), quick shortcuts
- **Events Page**: Full event history with tabs (Upcoming/Past/Cancelled)
- **Integration**: Uses existing EventDetailPage for event actions
- **Maintained**: Edge-to-edge layout, left nav, simple functionality focus

This is a **simple website with simple functionality** - not complex design.

## Architecture Discovery Results

### Documents Reviewed:
- **domain-layer-architecture.md**: Lines 725-997 - Found complete NSwag implementation for TypeScript type generation
- **DTO-ALIGNMENT-STRATEGY.md**: Lines 85-213 - Confirmed API as source of truth, NSwag auto-generation requirement
- **migration-plan.md**: Lines 1-100 - Found React infrastructure status (complete) and Mantine v7 selection
- **functional-area-master-index.md**: User Dashboard area exists with current active work path identified
- **business-requirements-lessons-learned.md**: Critical validation requirements for DTO specification standards

### Legacy Wireframes Reviewed:
- **user-dashboard-visual.html**: Main dashboard layout with prominent event display and welcome section
- **member-security-settings-visual.html**: Security settings with sidebar navigation showing Profile/Membership/Security/Notifications structure
- **member-my-tickets-visual.html**: Events page with filter tabs for Upcoming/Past/Cancelled events
- **member-profile-settings-visual.html**: Profile editing interface with sidebar navigation
- **member-membership-settings.html**: Membership status and management interface
- **user-menu-wireframes.md**: Complete navigation patterns for authenticated and unauthenticated states

### Existing Solutions Found:
- **NSwag Type Generation**: Complete TypeScript interface generation from C# DTOs (lines 725-774)
- **Authentication System**: Complete React implementation (Milestone achieved 2025-08-19)
- **UI Framework**: Mantine v7 validated and integrated (ADR-004, Infrastructure complete 2025-08-18)
- **State Management**: TanStack Query v5 + Zustand patterns proven in authentication milestone
- **Design System v7**: Integrated on homepage with Mantine v7 components
- **EventDetailPage**: Existing event detail page with action buttons (View Details integration point)

### Verification Statement:
Confirmed existing solutions for all technical foundations. No manual DTO creation required - NSwag generates all TypeScript types. React infrastructure and design systems are production-ready. Legacy wireframes provide general direction - creating simplified version, not exact copies. EventDetailPage already exists for event detail actions.

## Executive Summary

The User Dashboard Redesign project creates a **simple, two-page dashboard** replacing the legacy Blazor Server implementation. The dashboard features a **left-mounted menu** with 5 sections (Dashboard, Events, Profile, Security, Membership) and **edge-to-edge layout**. The **Dashboard Landing page** (`/dashboard`) shows a welcome message and upcoming events preview (3-5 cards max) with quick shortcuts. The **Events Page** (`/dashboard/events`) provides full event history with tabs for filtering. All event actions navigate to the existing EventDetailPage. The primary focus is **functionality over complex design**.

## Business Context

### Problem Statement

The current Blazor Server dashboard needs a simple React implementation with clear separation between dashboard overview and event management. Users need quick access to upcoming events on their dashboard landing while maintaining full event history access on a dedicated events page. **Previous designs had too many elements** - this consolidation creates two focused pages: a welcoming dashboard with preview, and a comprehensive events page for history.

### Business Value

- **Clear Purpose Separation**: Dashboard for overview, Events page for history
- **Reduced Clutter**: Dashboard shows only 3-5 upcoming events, not full list
- **Quick Navigation**: Shortcuts on dashboard for common actions
- **Comprehensive History**: Dedicated events page with all participation data
- **Existing Integration**: Uses EventDetailPage for event actions (proven pattern)
- **Simple Design**: Edge-to-edge layout, no right-side menu, clean interface

### Success Metrics

- **Performance**: Dashboard load time <1.5 seconds, Events page <2 seconds
- **Usability**: Users find upcoming events immediately on dashboard
- **Navigation**: <2 clicks to reach any event detail or account setting
- **Simplicity**: Zero UI complexity complaints
- **Functionality**: All event history accessible via tabs
- **Mobile Usage**: Clean responsive experience on all devices

## User Stories

### Story 1: Dashboard Landing - Welcome and Upcoming Events Preview
**As a** member with upcoming events
**I want to** see a welcoming dashboard with my next few events
**So that** I can quickly check my immediate commitments

**Acceptance Criteria:**
- Given I log into my dashboard at `/dashboard`
- When the Dashboard Landing page loads
- Then I see "Welcome back, [Scene Name]" at the top
- And I see a "My Upcoming Events" section below the welcome
- And the section shows 3-5 upcoming events I've RSVP'd to or purchased tickets for
- And each event card displays: event name, date, time, location, participation status
- And each event card has a "View Details" button that navigates to EventDetailPage
- And if I have more than 5 upcoming events, I see a "View All Events" link to `/dashboard/events`
- And if I have no upcoming events, I see "No upcoming events scheduled"
- And the content extends edge-to-edge (no floating boxes)
- And there is NO right-side menu or status area

**Data Requirements:**
- User's upcoming events (RSVP'd or ticketed) ordered by event date (soonest first)
- Limited to 5 most recent upcoming events for dashboard preview
- Participation status: "Registered", "Ticketed", "Waitlisted", etc.
- Event basic info: name, date, time, location

### Story 2: Dashboard Landing - Quick Action Shortcuts
**As any** authenticated user
**I want** quick access to common account actions
**So that** I don't need to navigate through multiple menus

**Acceptance Criteria:**
- Given I am on the Dashboard Landing page
- When I scroll below the upcoming events section
- Then I see a "Quick Actions" section
- And I see shortcut buttons for:
  - "Edit Profile" → navigates to `/dashboard/profile`
  - "Security Settings" → navigates to `/dashboard/security`
  - "Membership Status" → navigates to `/dashboard/membership`
- And shortcuts are displayed in a simple row or grid (edge-to-edge)
- And shortcuts have clear icons and labels
- And shortcuts work on mobile with proper touch targets

### Story 3: Events Page - Full Event History with Tabs
**As a** member who has attended multiple events
**I want to** view all my event participation history
**So that** I can review past events and manage future ones

**Acceptance Criteria:**
- Given I click "Events" in the left navigation or "View All Events" from dashboard
- When I navigate to `/dashboard/events`
- Then I see the Events Page with three filter tabs:
  - "Upcoming" (default active tab)
  - "Past"
  - "Cancelled"
- And the Upcoming tab shows all events I'm registered for or have tickets for
- And events are ordered by date (soonest first for Upcoming, most recent first for Past)
- And each event card displays: name, date, time, location, participation status
- And each event card has a "View Details" button that navigates to EventDetailPage
- And the Past tab shows events that have already occurred
- And the Cancelled tab shows events I was registered for but were cancelled
- And the layout is edge-to-edge (no floating boxes)
- And there is NO right-side menu or status area

**Data Requirements:**
- All user event participations with full history
- Event status: upcoming, past (completed), cancelled
- Participation status for each event
- Sorting: upcoming by date ascending, past by date descending
- All event basic info: name, date, time, location, description

### Story 4: Events Page - Tab Filtering Behavior
**As a** user viewing my events
**I want** clear tab filtering
**So that** I can easily find the events I'm looking for

**Acceptance Criteria:**
- Given I am on the Events Page
- When I click the "Upcoming" tab
- Then I see only events with future dates that I'm registered for
- When I click the "Past" tab
- Then I see only events that have already occurred
- When I click the "Cancelled" tab
- Then I see only events that were cancelled
- And the active tab is clearly highlighted
- And tab switches happen without full page reload
- And empty states show helpful messages: "No upcoming events", "No past events", "No cancelled events"

### Story 5: EventDetailPage Integration
**As a** user viewing an event card
**I want to** click "View Details" to see full event information
**So that** I can access all event actions in one place

**Acceptance Criteria:**
- Given I see an event card on Dashboard Landing or Events Page
- When I click the "View Details" button
- Then I navigate to the EventDetailPage for that event
- And EventDetailPage shows all event information
- And EventDetailPage provides actions like: Cancel RSVP, View Ticket, Add to Calendar, etc.
- And I can return to my dashboard or events page using browser back or navigation
- And navigation state is preserved (dashboard vs events page context)

**Integration Requirements:**
- EventDetailPage already exists - reuse existing component
- Pass event ID to EventDetailPage for loading
- EventDetailPage handles all event-specific actions
- No duplication of event action logic

### Story 6: Left Navigation - Five-Section Menu
**As any** authenticated user
**I want** a simple left-mounted menu
**So that** I can access all dashboard sections

**Acceptance Criteria:**
- Given I am on any dashboard page
- When I view the left navigation
- Then I see exactly 5 sections:
  - "Dashboard" → `/dashboard` (landing page)
  - "Events" → `/dashboard/events` (full event history)
  - "Profile" → `/dashboard/profile`
  - "Security" → `/dashboard/security`
  - "Membership" → `/dashboard/membership`
- And the current page is clearly highlighted
- And the menu is mounted to the left side (not floating)
- And the menu works on mobile with basic responsive collapse
- And there are NO multiple layers or background boxes

### Story 7: Mobile Responsive Experience
**As any** user accessing via mobile device
**I want** a simple responsive dashboard
**So that** I can use all features on mobile

**Acceptance Criteria:**
- Given I access the dashboard on mobile
- When I view the Dashboard Landing page
- Then the welcome message, events, and shortcuts are readable
- And event cards stack vertically on mobile
- And "View Details" buttons have proper touch targets
- When I access the Events Page on mobile
- Then tabs are accessible and functional
- And event cards remain readable in stacked layout
- And the left menu collapses to a mobile menu (hamburger or similar)
- And all functionality works without complex mobile patterns

## Business Rules

### Dashboard Landing Page Rules
1. **Limited Event Preview**: Maximum 5 upcoming events on dashboard landing
2. **Chronological Order**: Events sorted by date (soonest first)
3. **User Events Only**: Show only events user has RSVP'd to or purchased tickets for
4. **View All Link**: If more than 5 upcoming events, show "View All Events" link
5. **Empty State**: Clear message if user has no upcoming events
6. **Quick Shortcuts**: Three primary shortcuts: Profile, Security, Membership
7. **Edge-to-Edge**: All content spans full page width
8. **No Right Menu**: Right-side menu/status area completely removed

### Events Page Rules
1. **Three Tabs Required**: Upcoming, Past, Cancelled
2. **Default Tab**: "Upcoming" tab active on page load
3. **Full History**: Show all user event participations, not limited to 5
4. **Status-Based Filtering**: Events filtered by tab selection
5. **Upcoming Sort**: Soonest events first (ascending date)
6. **Past Sort**: Most recent events first (descending date)
7. **Empty States**: Clear messages for each empty tab
8. **Real-Time Data**: No caching, immediate updates
9. **Edge-to-Edge**: All content spans full page width
10. **No Right Menu**: Right-side menu/status area completely removed

### EventDetailPage Integration Rules
1. **Single Source**: EventDetailPage handles all event-specific actions
2. **Navigation**: "View Details" button navigates to EventDetailPage
3. **No Duplication**: Don't recreate event actions on dashboard/events pages
4. **Context Preservation**: User can navigate back to origin (dashboard vs events)
5. **Existing Component**: Reuse existing EventDetailPage, no new implementation

### Navigation Rules
1. **5-Section Menu**: Dashboard, Events, Profile, Security, Membership only
2. **Left-Mounted**: Menu attached to left side, not floating
3. **Current Highlighting**: Clear indication of current page
4. **Simple Mobile**: Basic responsive collapse, no complex patterns
5. **Consistent Across Pages**: Same navigation on all dashboard pages

### Design Simplicity Rules
1. **NO Floating Boxes**: All content extends across entire page width
2. **NO Right-Side Menu**: No status area, notifications panel, or right sidebar
3. **NO Multiple Layers**: Simple, flat design without background containers
4. **NO Over-Engineering**: This is a simple website - keep functionality simple
5. **Edge-to-Edge Layout**: Content spans full width like main website

## Constraints & Assumptions

### Page Structure Constraints
- **Two Primary Pages**: Dashboard Landing + Events Page only (plus Profile, Security, Membership)
- **EventDetailPage Reuse**: Must use existing EventDetailPage, not create new one
- **No Right Menu**: Right-side content completely eliminated from design
- **Left Nav Only**: Single navigation source (left menu)
- **Tab-Based Filtering**: Events page uses tabs, not complex filters

### Technical Constraints
- **React Architecture**: Must use React + TypeScript + Vite stack
- **UI Framework**: Must use Mantine v7 components (simple ones)
- **Type Safety**: Must use NSwag generated types for event data
- **Authentication**: Must integrate with existing auth system
- **Routing**: React Router for page navigation and tab state
- **State Management**: TanStack Query for event data fetching

### Business Constraints
- **Keep It Simple**: This is a simple website with simple functionality
- **No Over-Complication**: Most templates are too complex for our needs
- **Functionality Priority**: Working features over complex design
- **Existing Patterns**: Leverage EventDetailPage pattern already proven
- **Clean Interface**: Minimal UI without multiple sections competing

### Assumptions
- **EventDetailPage Exists**: Existing page handles all event actions
- **API Available**: User events API provides participation history
- **Status Data**: Event participation status available from API
- **Filtering Backend**: API can filter events by status (upcoming/past/cancelled)
- **Performance**: API response times support <2 second page load

## Security & Privacy Requirements

### Authentication & Authorization
- **Session Management**: Use existing httpOnly cookie + JWT authentication
- **Role Verification**: Verify user identity for event data access
- **Session Timeout**: 2-hour inactivity timeout
- **User Data Isolation**: Users see only their own events and data

### Data Protection
- **PII Handling**: Encrypt data in transit and at rest
- **Privacy Controls**: Respect user privacy settings for event data
- **Data Minimization**: Only fetch necessary event data per page
- **Event Privacy**: Respect event visibility settings (public/vetted-only)

## Data Structure Requirements

### Dashboard Landing Page Data
**Upcoming Events Preview DTO:**
- userId: Guid (required)
- upcomingEvents: List<EventSummaryDto> (required, max 5 items)
  - eventId: Guid (required)
  - eventName: string (required, max 200 characters)
  - eventDate: DateTime (required, ISO 8601 format)
  - eventTime: string (required, time format)
  - location: string (required, max 300 characters)
  - participationStatus: string (required, enum: "Registered", "Ticketed", "Waitlisted")
- hasMoreEvents: boolean (required) - indicates if user has >5 upcoming events
- totalUpcomingCount: int (required) - total upcoming events count

### Events Page Data
**Full Event History DTO:**
- userId: Guid (required)
- events: List<EventParticipationDto> (required)
  - eventId: Guid (required)
  - eventName: string (required, max 200 characters)
  - eventDate: DateTime (required, ISO 8601 format)
  - eventTime: string (required, time format)
  - location: string (required, max 300 characters)
  - participationStatus: string (required, enum)
  - eventStatus: string (required, enum: "Upcoming", "Past", "Cancelled")
  - registrationDate: DateTime (required, ISO 8601 format)
- upcomingCount: int (required)
- pastCount: int (required)
- cancelledCount: int (required)

### Business Rules for Data
- Event dates in UTC, display in user's local timezone
- Participation status from predefined enum
- Event status calculated based on event date vs current date
- Sort order: upcoming ascending by date, past descending by date

## Examples/Scenarios

### Scenario 1: New User First Dashboard Login
**Context**: User logs in for the first time, has registered for 2 upcoming events

**Step-by-step walkthrough**:
1. User authenticates and navigates to `/dashboard`
2. Dashboard Landing page loads showing "Welcome back, [Scene Name]"
3. "My Upcoming Events" section shows 2 event cards
4. Each card displays event name, date, time, location, "Registered" status
5. Each card has "View Details" button
6. No "View All Events" link shown (only 2 events, less than 5)
7. Quick Actions section shows Edit Profile, Security Settings, Membership shortcuts
8. Left navigation shows Dashboard (highlighted), Events, Profile, Security, Membership
9. No right-side menu or floating elements

**Success Criteria**: Clean welcome experience with immediate event visibility

### Scenario 2: Active User with Many Events
**Context**: User has 12 upcoming events and 25 past events

**Step-by-step walkthrough**:
1. User logs into `/dashboard`
2. Dashboard shows "Welcome back, [Scene Name]"
3. "My Upcoming Events" shows 5 most recent upcoming event cards
4. "View All Events" link appears below the 5 cards
5. User clicks "View All Events"
6. Navigates to `/dashboard/events` with "Upcoming" tab active
7. Events page shows all 12 upcoming events in cards
8. User clicks "Past" tab
9. Tab content updates to show all 25 past events (most recent first)
10. User clicks "View Details" on an event
11. Navigates to EventDetailPage for that event
12. User can use browser back to return to Events page with Past tab still active

**Success Criteria**: Smooth navigation between preview and full history, tab state preserved

### Scenario 3: Events Page Tab Navigation
**Context**: User wants to review their event participation history

**Step-by-step walkthrough**:
1. User clicks "Events" in left navigation
2. Events page loads at `/dashboard/events` with "Upcoming" tab active
3. Page shows 8 upcoming events
4. User clicks "Past" tab
5. Content updates to show 15 past events without page reload
6. User clicks "Cancelled" tab
7. Content shows 2 cancelled events
8. User clicks "Upcoming" tab again
9. Returns to 8 upcoming events
10. Tab highlighting clearly shows active tab throughout

**Success Criteria**: Fast tab switching, clear filtering, no page reloads

### Scenario 4: Mobile Dashboard Experience
**Context**: User accesses dashboard on mobile phone

**Step-by-step walkthrough**:
1. User logs in on mobile device
2. Dashboard Landing loads with mobile layout
3. Welcome message displays clearly
4. Event cards stack vertically (one per row)
5. "View Details" buttons have large touch targets
6. Quick Actions stack vertically or wrap appropriately
7. Left menu collapses to hamburger menu
8. User taps hamburger to see 5 navigation sections
9. User taps "Events" section
10. Events page loads with tabs accessible at top
11. User swipes or taps tabs to filter events
12. Event cards remain readable and functional

**Success Criteria**: All functionality works on mobile without complexity

### Scenario 5: EventDetailPage Integration
**Context**: User wants to see full event details and take action

**Step-by-step walkthrough**:
1. User is on Dashboard Landing and sees upcoming event card
2. User clicks "View Details" button on event card
3. Browser navigates to EventDetailPage at `/events/{eventId}`
4. EventDetailPage loads full event information
5. EventDetailPage shows action buttons: Cancel RSVP, Add to Calendar, etc.
6. User completes action (e.g., cancels RSVP)
7. User clicks browser back button
8. Returns to Dashboard Landing page
9. Event is removed from upcoming events (real-time update)
10. Dashboard reflects updated event list

**Success Criteria**: Seamless integration, no duplicate functionality, state updates

## User Impact Analysis

| User Type | Dashboard Landing Impact | Events Page Impact | Priority |
|-----------|--------------------------|-------------------|----------|
| **Admin** | Quick view of next admin events + shortcuts | Full admin event history with tabs | High |
| **Teacher** | Preview of teaching schedule + shortcuts | All teaching events organized by status | High |
| **Vetted Member** | Upcoming classes/workshops + shortcuts | Complete participation history | High |
| **General Member** | Upcoming public events + shortcuts | Public event participation history | Medium |
| **Guest** | Limited dashboard access | Limited events access | Low |

**Impact Summary**:
- All authenticated users benefit from two-page structure
- Dashboard provides quick overview without overwhelming
- Events page provides comprehensive history management
- Clear separation reduces cognitive load
- Integration with EventDetailPage provides consistent UX

## Questions for Product Manager

### Answered (October 2025 Consolidation):
1. **Q**: Should dashboard show preview or full event list?
   **A**: Preview only (3-5 events max) with "View All" link to Events page

2. **Q**: What belongs on Events page vs Dashboard?
   **A**: Dashboard = welcome + preview + shortcuts; Events page = full history with tabs

3. **Q**: Keep right-side menu?
   **A**: No, completely removed - left nav only

4. **Q**: How to handle event actions?
   **A**: Use existing EventDetailPage, "View Details" button on all event cards

5. **Q**: Tab structure for Events page?
   **A**: Three tabs - Upcoming (default), Past, Cancelled

6. **Q**: Quick action shortcuts needed?
   **A**: Yes - Edit Profile, Security Settings, Membership Status

## Quality Gate Checklist (95% Required)

- [x] **Two-page structure defined**: Dashboard Landing + Events Page clearly specified
- [x] **Right-side menu removed**: No status area, notifications, or right sidebar
- [x] **Dashboard Landing specified**: Welcome, upcoming events preview (3-5), shortcuts
- [x] **Events Page specified**: Full history with Upcoming/Past/Cancelled tabs
- [x] **EventDetailPage integration**: "View Details" button navigates to existing page
- [x] **Quick shortcuts defined**: Profile, Security, Membership shortcuts on dashboard
- [x] **Left navigation maintained**: 5-section menu (Dashboard, Events, Profile, Security, Membership)
- [x] **Edge-to-edge layout preserved**: No floating boxes, simple flat design
- [x] **Tab filtering specified**: Three tabs with clear filtering behavior
- [x] **Mobile experience defined**: Responsive layout with simple patterns
- [x] **User stories complete**: All major user flows documented with acceptance criteria
- [x] **Business rules documented**: Clear rules for both pages and navigation
- [x] **Data structures specified**: DTOs for dashboard preview and events history
- [x] **Integration requirements**: EventDetailPage reuse, no duplication
- [x] **Empty states handled**: Clear messages for no events in each context
- [x] **Security requirements**: Authentication, data protection, privacy
- [x] **Success metrics defined**: Performance, usability, navigation targets
- [x] **Examples provided**: 5 comprehensive scenarios covering all use cases
- [x] **Simplification maintained**: Functionality over complex design

## Implementation Readiness Verification

### Consolidation Confirmed ✅
- **Two-Page Structure**: Dashboard Landing for overview, Events Page for history
- **Right Menu Removed**: Clean single-navigation design (left only)
- **EventDetailPage Integration**: Existing component reused for event actions
- **Tab-Based Filtering**: Simple three-tab structure for events
- **Quick Shortcuts**: Three essential shortcuts on dashboard landing
- **Preview Limitation**: Maximum 5 events on dashboard, full list on events page

### Technical Foundation ✅
- **React Infrastructure**: Available for two-page implementation
- **Mantine v7**: Provides tabs, cards, navigation components
- **React Router**: Page navigation and tab state management
- **TanStack Query**: Event data fetching and caching
- **NSwag Types**: API type generation for event DTOs
- **EventDetailPage**: Existing component ready for integration

### Business Requirements Met ✅
- **Stakeholder Consolidation**: Right menu removed, two focused pages created
- **Simple Functionality**: No over-engineering, clear purpose per page
- **Existing Patterns**: Leverages proven EventDetailPage pattern
- **Clean Interface**: Edge-to-edge, no floating elements, single navigation
- **Future Flexibility**: Foundation allows enhancement without breaking changes

---

*This consolidated business requirements document defines a simple two-page dashboard structure: a welcoming Dashboard Landing with upcoming events preview and quick shortcuts, and a comprehensive Events Page with full history and tab-based filtering. The design eliminates the right-side menu entirely, uses existing EventDetailPage for event actions, and maintains the simplified edge-to-edge approach. All stakeholder consolidation feedback has been incorporated.*

*Next Phase*: UI design for Dashboard Landing page and Events Page layouts, including EventDetailPage integration points and tab component specifications.
