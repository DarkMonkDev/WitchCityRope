# User Dashboard Redesign - Business Requirements
## Version 6.0 - APPROVED DESIGN (October 9, 2025)

**Status**: ✅ APPROVED FOR IMPLEMENTATION
**Approved Wireframe**: dashboard-wireframe-v4-iteration.html
**Approval Date**: October 9, 2025
**Last Updated**: 2025-10-09
**Owner**: Business Requirements Agent

---

## 🎯 VERSION 6.0 - APPROVED DESIGN ALIGNMENT

This version aligns with the **approved wireframe v4 iteration** which represents the final design specification. All implementation work must match this wireframe exactly.

**Major Changes from v5.0 to v6.0:**
1. **Simplified Navigation**: From 3-tab layout to 2-page structure (My Events + Profile Settings)
2. **Dashboard + Events Merged**: Single "My Events" page combines dashboard preview and full event list
3. **Vetting Status Alert**: Conditional alert box with 4 status messages (Pending, Approved, On Hold, Denied)
4. **Edit Profile Access**: Added to top utility bar and dashboard page title bar
5. **Event Filtering Simplified**: "Show Past Events" checkbox instead of Past/Cancelled tabs
6. **User Dashboard Focus**: Events display shows user's registered events ONLY (no pricing, capacity, or sales elements)
7. **Cancelled Events Removed**: No separate cancelled events section
8. **Status Badges Updated**: RSVP Confirmed, Ticket Purchased, Attended (displayed based on registration state)

---

## Architecture Discovery Results

### Documents Reviewed:
- **domain-layer-architecture.md**: Lines 725-997 - Found complete NSwag implementation for TypeScript type generation
- **DTO-ALIGNMENT-STRATEGY.md**: Lines 85-213 - Confirmed API as source of truth, NSwag auto-generation requirement
- **migration-plan.md**: Lines 1-100 - Found React infrastructure status (complete) and Mantine v7 selection
- **functional-area-master-index.md**: User Dashboard area exists with current active work path identified
- **business-requirements-lessons-learned.md**: Critical validation requirements for DTO specification standards
- **APPROVED-DESIGN.md**: Version 1.0 - Final approved design specification for v4 wireframe

### Existing Solutions Found:
- **NSwag Type Generation**: Complete TypeScript interface generation from C# DTOs (lines 725-774)
- **Authentication System**: Complete React implementation (Milestone achieved 2025-08-19)
- **UI Framework**: Mantine v7 validated and integrated (ADR-004, Infrastructure complete 2025-08-18)
- **State Management**: TanStack Query v5 + Zustand patterns proven in authentication milestone
- **Design System v7**: Integrated on homepage with Mantine v7 components
- **EventDetailPage**: Existing event detail page with action buttons (View Details integration point)

### Verification Statement:
Confirmed existing solutions for all technical foundations. No manual DTO creation required - NSwag generates all TypeScript types. React infrastructure and design systems are production-ready. Approved wireframe v4 provides exact specification for implementation.

---

## Executive Summary

The User Dashboard Redesign project creates a **simple, two-page dashboard** aligned with the **approved wireframe v4 iteration**. The dashboard features:

- **My Events Page** (`/dashboard`): User's registered events with conditional vetting alert, "Show Past Events" toggle, and grid/table view options
- **Profile Settings Page** (`/dashboard/profile-settings`): Consolidated profile editing with tabbed structure (Personal, Social, Security, Vetting)
- **Edit Profile Access**: Available via top utility bar (before Logout) and dashboard page title bar button
- **Vetting Status Alert**: Conditional display for Pending, Approved, On Hold, Denied statuses (hidden when Vetted)
- **User-Focused Event Display**: Shows only user's registered events with status badges (NO pricing, capacity, or sales elements)
- **Simplified Filtering**: "Show Past Events" checkbox (unchecked by default) instead of complex tab navigation
- **View Toggle**: Switch between Card View (grid) and List View (table)

The design prioritizes **functionality over complex design** and focuses on the user's registered events and profile management.

---

## Business Context

### Problem Statement

Users need a clean dashboard that shows their registered events and allows easy profile management. The dashboard must be a **user dashboard** (not a public sales page), focusing on events the user has already committed to. Users need quick access to profile editing from multiple entry points. Non-vetted users need clear visibility of their vetting status with appropriate next steps.

### Business Value

- **User-Focused Design**: Shows only user's events, not sales-focused public event listings
- **Clear Vetting Visibility**: Conditional alert box provides status-specific guidance
- **Quick Profile Access**: "Edit Profile" available in utility bar and dashboard title bar
- **Simplified Event Filtering**: Single checkbox for past events instead of complex tabs
- **Flexible Views**: Grid (cards) and Table (list) views for different user preferences
- **Mobile Optimized**: Clean responsive experience on all devices
- **Status Clarity**: Clear badges for RSVP Confirmed, Ticket Purchased, Attended
- **Action Buttons**: "View Details" (not "Learn More") aligns with registered event context

### Success Metrics

- **Dashboard Load Time**: Page loads in <2 seconds
- **Profile Access**: "Edit Profile" click-through rate from dashboard >30%
- **Vetting Alert Engagement**: Users with non-vetted status engage with alert CTAs
- **View Toggle Usage**: Users switch between grid/table views based on preference
- **Past Events Filter**: "Show Past Events" toggle usage indicates historical event interest
- **Mobile Usage**: >50% of dashboard access from mobile devices with positive experience
- **Event Detail Navigation**: "View Details" click-through rate from event cards

---

## User Stories

### Story 1: Dashboard Page - My Events with Conditional Vetting Alert
**As a** logged-in user
**I want to** see my registered events and my vetting status
**So that** I can quickly review my upcoming events and understand my membership status

**Acceptance Criteria:**
- Given I log into my dashboard at `/dashboard`
- When the My Events page loads
- Then I see the page title: "[FirstName] Dashboard" (e.g., "ShadowKnot Dashboard")
- And I see an "Edit Profile" button to the right of the page title
- And **IF** my vetting status is NOT "Vetted", I see a vetting status alert box
- And the alert box displays one of four conditional messages:
  - **Pending**: "⏰ Application Under Review - Your membership application is currently under review. We'll notify you via email once it's been reviewed."
  - **Approved**: "✅ Great News! Your Application Has Been Approved - Schedule your vetting interview here to complete your membership." (with link)
  - **On Hold**: "⏸️ Membership On Hold - Your membership is currently on hold. Contact us if you'd like to resume your membership."
  - **Denied**: "❌ Application Not Approved - Your membership application was not approved at this time. Learn about reapplying." (with link)
- And **IF** my vetting status is "Vetted", NO alert box is displayed
- And I see a filter bar with:
  - "Show Past Events" checkbox (unchecked by default)
  - View toggle: "Card View" (default) / "List View"
  - Search input: "Search events..."
- And I see my upcoming registered events displayed in grid view (cards) by default
- And past events are hidden by default (`.hidden` class)
- And each event card shows: event title, date, time, location, status badge
- And each event card has a "View Details" button (NOT "Learn More")
- And **NO** pricing or capacity information is displayed (this is NOT a sales page)

**Data Requirements:**
- User's first name for page title
- User's vetting status: enum("Pending", "Approved", "On Hold", "Denied", "Vetted")
- User's registered events with registration status
- Event basic info: title, date, time, location
- No pricing or capacity data needed

### Story 2: Edit Profile Access - Multiple Entry Points
**As a** logged-in user
**I want to** access my profile settings from the dashboard
**So that** I can quickly edit my profile without searching for navigation

**Acceptance Criteria:**
- Given I am logged into the dashboard
- When I view the top utility bar
- Then I see an "Edit Profile" link positioned before "Logout"
- And the link is styled consistently with other utility bar links
- When I view the dashboard page title bar
- Then I see an "Edit Profile" button to the right of the page title
- And the button uses secondary styling (cream background, rose-gold border)
- When I click either "Edit Profile" link or button
- Then I navigate to `/dashboard/profile-settings`
- And the Profile Settings page loads

**Navigation Structure:**
```
Utility Bar: About | Contact | Edit Profile | Logout
Page Title Bar: [FirstName] Dashboard [Edit Profile Button]
```

### Story 3: Vetting Status Alert - Conditional Display
**As a** non-vetted user
**I want to** see my current vetting status with appropriate next steps
**So that** I understand where I am in the membership process

**Acceptance Criteria:**
- Given my vetting status is "Pending"
- When I view My Events page
- Then I see a blue alert box with clock icon (⏰)
- And the alert displays: "Application Under Review"
- And the message says: "Your membership application is currently under review. We'll notify you via email once it's been reviewed."

- Given my vetting status is "Approved"
- When I view My Events page
- Then I see a green alert box with checkmark icon (✅)
- And the alert displays: "Great News! Your Application Has Been Approved"
- And the message includes a link: "Schedule your vetting interview here to complete your membership."
- And the link navigates to `/vetting/schedule-interview`

- Given my vetting status is "On Hold"
- When I view My Events page
- Then I see an amber alert box with pause icon (⏸️)
- And the alert displays: "Membership On Hold"
- And the message says: "Your membership is currently on hold. Contact us if you'd like to resume your membership."

- Given my vetting status is "Denied"
- When I view My Events page
- Then I see a red alert box with X icon (❌)
- And the alert displays: "Application Not Approved"
- And the message includes a link: "Your membership application was not approved at this time. Learn about reapplying."
- And the link navigates to `/vetting/reapply`

- Given my vetting status is "Vetted"
- When I view My Events page
- Then NO alert box is displayed

**Alert Styling:**
| Status | Icon | Color | Border Color |
|--------|------|-------|--------------|
| Pending | ⏰ | Blue background (#2196F3 at 10% opacity) | #2196F3 |
| Approved | ✅ | Green background (#4CAF50 at 10% opacity) | #4CAF50 |
| On Hold | ⏸️ | Amber background (#FFC107 at 10% opacity) | #FFC107 |
| Denied | ❌ | Red background (#F44336 at 10% opacity) | #F44336 |

### Story 4: Event Display - User Dashboard Version (Grid View)
**As a** user viewing my events
**I want to** see my registered events in card view
**So that** I can quickly scan my event commitments

**Acceptance Criteria:**
- Given I am on My Events page with Card View active
- When I view my upcoming events
- Then I see event cards displayed in a grid layout
- And each card has a gradient header with event title
- And each card displays:
  - Date/Time: "Day, Month Date • Time" (e.g., "Saturday, October 31 • 7:00 PM")
  - Location: with location pin emoji (📍)
  - Status Badge: based on registration state
  - Action Button: "View Details" (secondary styling)
- And **NO** pricing information is displayed
- And **NO** capacity information is displayed
- And **NO** "Learn More" buttons (replaced with "View Details")
- And status badges display:
  - "RSVP Confirmed" (Blue) - For social events with RSVP
  - "Ticket Purchased" (Green) - For paid classes
  - "Ticket Purchased (Social Event)" (Green) - For social events with tickets
  - "Attended" (Purple) - For past events (when "Show Past Events" is checked)

**Critical Rules:**
- ❌ Remove: Pricing, capacity, "spots available", "Learn More" buttons
- ✅ Use: "View Details" button for all events
- ✅ Show: Only events the user has registered for
- ✅ Hide: Public sales elements completely

### Story 5: Event Display - User Dashboard Version (Table View)
**As a** user viewing my events
**I want to** see my registered events in table view
**So that** I can view more events at once with sortable columns

**Acceptance Criteria:**
- Given I am on My Events page with List View active
- When I view my events
- Then I see events displayed in a table format
- And the table has columns: Date, Time, Event Title, Status, Action
- And the Date column is sortable (with sort indicator ↕)
- And each row displays:
  - Date: "Oct 31" format
  - Time: "7:00 PM" format
  - Event Title: clickable name
  - Status: badge matching grid view styles
  - Action: "View Details" button
- And rows are clickable to navigate to event details
- And rows have hover highlighting
- And the header has burgundy background (#880124) with white text
- And rows alternate cream/white backgrounds
- And **NO** pricing or capacity columns
- And **NO** "Learn More" buttons

**Table Structure:**
```
| Date    | Time    | Event Title                    | Status          | Action       |
|---------|---------|--------------------------------|-----------------|--------------|
| Oct 31  | 7:00 PM | Halloween Rope Social          | RSVP Confirmed  | View Details |
| Nov 8   | 2:00 PM | Single Column Ties Workshop    | Ticket Purchased| View Details |
```

### Story 6: Filter and View Controls
**As a** user managing my event view
**I want to** filter past events and switch between views
**So that** I can customize how I see my events

**Acceptance Criteria:**
- Given I am on My Events page
- When I view the filter bar
- Then I see three controls:
  - "Show Past Events" checkbox (unchecked by default)
  - View toggle: "Card View" (default active) / "List View" buttons
  - Search input: "Search events..." placeholder
- And all controls are in a single filter bar
- When I check "Show Past Events"
- Then past events (with `.past-event` class) become visible
- And past events show "Attended" status badge
- When I uncheck "Show Past Events"
- Then past events are hidden again (`.hidden` class applied)
- When I click "List View" toggle button
- Then the grid view is hidden
- And the table view is displayed
- And the "List View" button is highlighted as active
- When I click "Card View" toggle button
- Then the table view is hidden
- And the grid view is displayed
- And the "Card View" button is highlighted as active
- When I type in the search input
- Then events are filtered in real-time by title/date

**Filter Behavior:**
- Past events hidden by default (`.hidden` class)
- Checkbox toggle shows/hides past events
- View toggle switches between grid and table
- Search filters visible events in real-time
- All filters work in both views

### Story 7: Profile Settings Page - Tabbed Structure
**As a** logged-in user
**I want to** manage my profile settings
**So that** I can update my personal information, social links, security settings, and vetting status

**Acceptance Criteria:**
- Given I navigate to `/dashboard/profile-settings`
- When the Profile Settings page loads
- Then I see a tabbed interface with four tabs:
  - "Personal" (default active tab)
  - "Social"
  - "Security"
  - "Vetting"
- And the Personal tab contains:
  - Scene name field (editable, required, 3-50 characters)
  - Email field (editable, required, valid email format)
  - Pronouns field (editable, optional, dropdown or text)
  - Bio field (editable textarea, optional, max 500 characters)
  - "Save Profile" button
- And the Social tab contains:
  - Social media links (FetLife, Instagram, etc.)
  - "Save Social Links" button
- And the Security tab contains:
  - Current password field (password input, required)
  - New password field (password input, required, minimum 8 characters)
  - Confirm password field (password input, required, must match new password)
  - "Change Password" button
- And the Vetting tab contains:
  - Current vetting status (display only, read-only text)
  - Date of last status update (display only, formatted date)
  - Vetting history (if available)

**Data Requirements:**
- User profile data: sceneName, email, pronouns, bio
- Social links: fetLifeUrl, instagramUrl, etc.
- Password validation rules: minimum 8 characters, complexity requirements
- Vetting status data: currentStatus (string), statusUpdatedAt (DateTime)

### Story 8: Mobile Responsive Experience
**As a** user accessing via mobile device
**I want** a clean responsive dashboard
**So that** I can use all features on mobile

**Acceptance Criteria:**
- Given I access the dashboard on mobile
- When I view the My Events page
- Then the page title and "Edit Profile" button are readable
- And the vetting alert box (if displayed) is readable and properly formatted
- And event cards stack vertically in grid view
- And table view remains scrollable horizontally if needed
- And filter controls stack vertically or wrap appropriately
- And "View Details" buttons have proper touch targets
- And the search input expands on focus
- When I access the Profile Settings page on mobile
- Then tabs are accessible and functional
- And form fields are properly sized for mobile input
- And all functionality works without complex mobile patterns

---

## Business Rules

### Dashboard Page Rules
1. **Page Title Format**: "[FirstName] Dashboard" using user's actual first name
2. **Edit Profile Button**: Positioned to the right of page title, secondary styling
3. **Vetting Alert Display**: Only shown for non-vetted users (Pending, Approved, On Hold, Denied)
4. **Vetted Users**: NO alert box displayed
5. **Alert Styling**: Color-coded by status (Blue, Green, Amber, Red)
6. **Alert Links**: Approved status links to interview scheduling, Denied status links to reapply info
7. **User Events Only**: Show only events user has registered for or purchased tickets for
8. **No Sales Elements**: No pricing, capacity, availability status, or "Learn More" buttons
9. **Status Badges**: RSVP Confirmed, Ticket Purchased, Attended
10. **Past Events Hidden**: Default view hides past events (unchecked checkbox)

### Event Filtering Rules
1. **Show Past Events Toggle**: Checkbox unchecked by default
2. **Past Event Class**: `.past-event` class on past events
3. **Hidden Class**: `.hidden` class applied to past events by default
4. **Checkbox Behavior**: Toggle `.hidden` class on `.past-event` elements
5. **No Cancelled Section**: No separate cancelled events display
6. **View Toggle**: Card View default, List View alternative
7. **Search Filter**: Real-time filtering by event title/date
8. **Filter Persistence**: Filter state maintained during view toggle

### Event Display Rules
1. **User Dashboard Context**: This is NOT a public sales page
2. **Remove Pricing**: NO price information displayed
3. **Remove Capacity**: NO capacity or availability information
4. **View Details Button**: Replaces "Learn More" button on all events
5. **Status Badges**: Based on registration state, not sales state
6. **Grid View Default**: Card view is default display mode
7. **Table View Alternative**: List view for more compact display
8. **Sortable Columns**: Date column sortable in table view
9. **Clickable Rows**: Table rows navigate to event details

### Profile Settings Page Rules
1. **Tabbed Structure**: Personal, Social, Security, Vetting tabs
2. **Personal Tab**: Profile information editing
3. **Social Tab**: Social media links management
4. **Security Tab**: Password change workflow
5. **Vetting Tab**: Read-only vetting status display
6. **Field Validation**: Client-side and server-side validation
7. **Save Actions**: Separate save buttons per tab section
8. **Success Feedback**: Clear confirmation messages after save
9. **Error Handling**: Clear error messages for validation failures

### Navigation Rules
1. **Utility Bar Edit Profile**: Link positioned before "Logout"
2. **Dashboard Edit Profile**: Button positioned right of page title
3. **Profile Settings Navigation**: Both links/buttons navigate to `/dashboard/profile-settings`
4. **Main Navigation**: Dashboard accessible via main nav
5. **Consistent Styling**: Edit Profile links match navigation styling

### Design Simplicity Rules
1. **User Dashboard Focus**: Events user has registered for, not public sales
2. **No Sales Elements**: This is NOT a sales or marketing page
3. **Status Over Price**: Registration status replaces pricing information
4. **Action Alignment**: "View Details" replaces "Learn More" for registered events
5. **Conditional Alerts**: Vetting status alert only for non-vetted users
6. **Clean Interface**: Minimal UI without competing sections
7. **Responsive Design**: Mobile-friendly without over-engineering

---

## Constraints & Assumptions

### Page Structure Constraints
- **Two Primary Pages**: My Events (dashboard) + Profile Settings
- **No Separate Events Page**: Dashboard IS the events page
- **Tabbed Profile Settings**: Four tabs (Personal, Social, Security, Vetting)
- **Conditional Alert**: Vetting alert displayed based on status
- **Filter Controls**: Single filter bar with checkbox, view toggle, search
- **No Cancelled Section**: Cancelled events not displayed separately

### Technical Constraints
- **React Architecture**: Must use React + TypeScript + Vite stack
- **UI Framework**: Must use Mantine v7 components
- **Type Safety**: Must use NSwag generated types for event data
- **Authentication**: Must integrate with existing auth system
- **Routing**: React Router for page navigation and tab state
- **State Management**: TanStack Query for event data fetching
- **Responsive Design**: Mobile-first responsive approach

### Business Constraints
- **User Dashboard Context**: NOT a public sales page
- **Registered Events Focus**: Show only user's events
- **Status-Based Display**: Registration status, not sales status
- **Vetting Visibility**: Clear communication of vetting status
- **Profile Access**: Multiple entry points for profile editing
- **Simple Filtering**: Checkbox for past events, no complex tabs
- **View Options**: Grid and table views for user preference

### Assumptions
- **User Data Available**: User's first name accessible from auth context
- **Vetting Status API**: API provides current vetting status
- **Event Registration Data**: API provides user's registered events with status
- **Profile Data API**: API provides user profile data for settings page
- **Password Change API**: API supports password change workflow
- **Social Links API**: API supports social media links storage
- **Performance**: API response times support <2 second page load

---

## Security & Privacy Requirements

### Authentication & Authorization
- **Session Management**: Use existing httpOnly cookie + JWT authentication
- **Role Verification**: Verify user identity for event data access
- **Session Timeout**: 2-hour inactivity timeout
- **User Data Isolation**: Users see only their own events and data
- **Password Change**: Require current password before allowing change
- **Profile Updates**: Verify user owns profile before allowing edits

### Data Protection
- **PII Handling**: Encrypt data in transit and at rest
- **Privacy Controls**: Respect user privacy settings for event data
- **Data Minimization**: Only fetch necessary event data per page
- **Event Privacy**: Respect event visibility settings (public/vetted-only)
- **Vetting Status Privacy**: Vetting status visible only to user and admins
- **Password Storage**: Hash and salt all passwords using bcrypt

---

## Data Structure Requirements

### My Events Page Data
**User Dashboard DTO:**
- userId: Guid (required)
- firstName: string (required) - for page title
- vettingStatus: string (required, enum: "Pending", "Approved", "On Hold", "Denied", "Vetted")
- registeredEvents: List<UserEventDto> (required)
  - eventId: Guid (required)
  - title: string (required, max 200 characters)
  - startDate: DateTime (required, ISO 8601 format)
  - startTime: string (required, time format)
  - location: string (required, max 300 characters)
  - registrationStatus: string (required, enum: "RSVP Confirmed", "Ticket Purchased", "Attended")
  - isPastEvent: boolean (required)
  - eventType: string (required, e.g., "Social Event", "Workshop", "Class")

### Profile Settings Page Data
**User Profile DTO:**
- userId: Guid (required)
- sceneName: string (required, 3-50 characters, unique)
- email: string (required, valid email format, unique)
- pronouns: string (optional, max 50 characters)
- bio: string (optional, max 500 characters)
- fetLifeUrl: string (optional, valid URL)
- instagramUrl: string (optional, valid URL)
- createdAt: DateTime (required, ISO 8601 format)
- updatedAt: DateTime (required, ISO 8601 format)

**Vetting Status DTO:**
- userId: Guid (required)
- vettingStatus: string (required, enum: "Pending", "Approved", "On Hold", "Denied", "Vetted")
- vettingStatusUpdatedAt: DateTime (required, ISO 8601 format)
- vettingNotes: string (optional, admin-only visibility)

**Password Change DTO:**
- currentPassword: string (required, password input)
- newPassword: string (required, min 8 characters, complexity requirements)
- confirmPassword: string (required, must match newPassword)

### Business Rules for Data
- Event dates in UTC, display in user's local timezone
- Registration status from predefined enum (RSVP Confirmed, Ticket Purchased, Attended)
- Past event determination: event date < current date
- Vetting alert display: status != "Vetted"
- Password must meet complexity requirements: 8+ chars, 1 uppercase, 1 lowercase, 1 number
- Profile updates validate uniqueness of sceneName and email

---

## Examples/Scenarios

### Scenario 1: Vetted User Viewing Dashboard
**Context**: Vetted user logs in with 3 upcoming events and 5 past events

**Step-by-step walkthrough**:
1. User authenticates and navigates to `/dashboard`
2. My Events page loads showing "ShadowKnot Dashboard" as page title
3. "Edit Profile" button displayed to the right of title
4. NO vetting alert box displayed (user is Vetted)
5. Filter bar shows "Show Past Events" checkbox (unchecked by default)
6. View toggle shows "Card View" active, "List View" inactive
7. Grid view displays 3 upcoming event cards
8. Each card shows: title (gradient header), date/time, location, status badge, "View Details" button
9. NO pricing or capacity information visible
10. Past events hidden by default
11. User checks "Show Past Events" checkbox
12. 5 past event cards appear with "Attended" status badge
13. User unchecks checkbox, past events hide again

**Success Criteria**: Clean dashboard, no vetting alert, clear event display, past events toggle works

### Scenario 2: Pending User Viewing Dashboard
**Context**: User with Pending vetting status logs in with 1 upcoming event

**Step-by-step walkthrough**:
1. User authenticates and navigates to `/dashboard`
2. My Events page loads showing "RopeNovice Dashboard" as page title
3. "Edit Profile" button displayed to the right of title
4. Blue vetting alert box displayed at top of page
5. Alert shows clock icon (⏰) and title "Application Under Review"
6. Alert message: "Your membership application is currently under review. We'll notify you via email once it's been reviewed."
7. User sees 1 upcoming event card below the alert
8. Event card shows: title, date, time, location, "RSVP Confirmed" badge, "View Details" button
9. NO pricing or capacity information visible
10. User clicks "Edit Profile" button in title bar
11. Navigates to `/dashboard/profile-settings`

**Success Criteria**: Pending alert displayed correctly, event visible, profile access works

### Scenario 3: Approved User Viewing Dashboard
**Context**: User with Approved status needs to schedule interview

**Step-by-step walkthrough**:
1. User authenticates and navigates to `/dashboard`
2. My Events page loads showing "[FirstName] Dashboard"
3. Green vetting alert box displayed
4. Alert shows checkmark icon (✅) and title "Great News! Your Application Has Been Approved"
5. Alert message includes link: "Schedule your vetting interview here to complete your membership."
6. User clicks the interview scheduling link
7. Navigates to `/vetting/schedule-interview`
8. User completes interview scheduling
9. Returns to dashboard

**Success Criteria**: Approved alert displayed with working link, clear next steps

### Scenario 4: Grid View to Table View Toggle
**Context**: User wants to see more events at once

**Step-by-step walkthrough**:
1. User is on My Events page with Card View active (default)
2. User sees 3 upcoming events displayed as cards in grid layout
3. User clicks "List View" toggle button
4. Grid view hides
5. Table view displays with same 3 events
6. Table shows columns: Date | Time | Event Title | Status | Action
7. Date column shows sort indicator (↕)
8. Each row displays event data in compact format
9. User clicks Date column header to sort
10. Events reorder by date
11. User clicks "Card View" toggle button
12. Table view hides, grid view displays again

**Success Criteria**: Smooth view toggle, data consistency, sortable table

### Scenario 5: Profile Settings - All Tabs
**Context**: User wants to update profile, social links, and password

**Step-by-step walkthrough**:
1. User clicks "Edit Profile" in utility bar
2. Navigates to `/dashboard/profile-settings`
3. Profile Settings page loads with "Personal" tab active
4. User updates scene name and bio
5. User clicks "Save Profile"
6. Success message appears, profile updates
7. User clicks "Social" tab
8. Tab switches to social media links
9. User enters FetLife URL
10. User clicks "Save Social Links"
11. Success message appears, links saved
12. User clicks "Security" tab
13. Tab switches to password change form
14. User enters current password, new password, confirm password
15. User clicks "Change Password"
16. Success message appears, password changed
17. User clicks "Vetting" tab
18. Tab switches to vetting status display
19. User sees read-only vetting status and date

**Success Criteria**: All tabs functional, separate save actions work, data persists

### Scenario 6: Mobile Dashboard Experience
**Context**: User accesses dashboard on mobile device

**Step-by-step walkthrough**:
1. User accesses `/dashboard` on mobile (375px width)
2. Page title "[FirstName] Dashboard" displays at readable size
3. "Edit Profile" button sized for touch target
4. Vetting alert box (if displayed) wraps text appropriately
5. Filter bar controls stack vertically
6. "Show Past Events" checkbox has large touch target
7. View toggle buttons sized for touch
8. Search input expands full width on focus
9. Event cards stack vertically in single column
10. Card content remains readable
11. "View Details" buttons have 44px touch targets
12. User switches to List View
13. Table scrolls horizontally if needed
14. User clicks "Edit Profile"
15. Profile Settings page loads with mobile-friendly tabs
16. Form fields use appropriate mobile keyboards

**Success Criteria**: All functionality works on mobile, proper touch targets, readable text

---

## User Impact Analysis

| User Type | Dashboard Impact | Profile Settings Impact | Priority |
|-----------|------------------|-------------------------|----------|
| **Admin** | View registered events, vetting alert if not vetted | Full profile editing, vetting tab visible | High |
| **Teacher** | Teaching events visible, quick profile access | Profile + social links important | High |
| **Vetted Member** | Clean event view, no vetting alert | Profile editing, password change | High |
| **General Member** | Event view with potential vetting alert | Profile editing, vetting status visible | High |
| **Guest** | Limited access | Basic profile only | Low |

**Impact Summary**:
- All authenticated users see personalized dashboard with their events
- Non-vetted users receive clear vetting status communication
- Multiple entry points for profile editing improve accessibility
- Grid/table view toggle provides user preference flexibility
- Past events toggle keeps interface clean while preserving history access
- Mobile optimization ensures cross-device functionality

---

## Questions for Product Manager

### Resolved (Approved Design v4):
1. **Q**: Should dashboard and events be separate pages?
   **A**: No, merge into single "My Events" page with filter controls

2. **Q**: How should vetting status be communicated?
   **A**: Conditional alert box at top of dashboard with 4 status messages

3. **Q**: Where should "Edit Profile" be accessible?
   **A**: Top utility bar (before Logout) and dashboard title bar button

4. **Q**: How to handle past events?
   **A**: "Show Past Events" checkbox (unchecked by default), no separate tab

5. **Q**: Should pricing be visible on user dashboard?
   **A**: No, this is user dashboard not sales page - no pricing/capacity

6. **Q**: What event action button should be used?
   **A**: "View Details" (not "Learn More") for registered events

### Pending Clarification:
- [ ] Vetting interview scheduling page - existing or new?
- [ ] Reapply information page - existing content or new?
- [ ] Social media link validation - which platforms supported?
- [ ] Past events - include cancelled events or separate?

---

## Quality Gate Checklist (95% Required)

### Version 6.0 Alignment Quality Gates
- [x] **Approved wireframe v4 reviewed**: dashboard-wireframe-v4-iteration.html
- [x] **APPROVED-DESIGN.md reviewed**: Version 1.0 specifications incorporated
- [x] **Two-page structure**: My Events + Profile Settings (not three pages)
- [x] **Dashboard + Events merged**: Single page with filter controls
- [x] **Vetting alert specified**: 4 conditional messages (Pending, Approved, On Hold, Denied)
- [x] **Edit Profile access**: Utility bar link + dashboard title bar button
- [x] **Past events filtering**: "Show Past Events" checkbox instead of tabs
- [x] **User dashboard focus**: No pricing, capacity, or sales elements
- [x] **Event action buttons**: "View Details" not "Learn More"
- [x] **Status badges updated**: RSVP Confirmed, Ticket Purchased, Attended
- [x] **Cancelled events removed**: No separate cancelled section

### User Dashboard Specificity Quality Gates
- [x] **User events only**: Show only registered/ticketed events
- [x] **Remove pricing**: No price information displayed
- [x] **Remove capacity**: No availability information displayed
- [x] **Remove "Learn More"**: Replaced with "View Details"
- [x] **Status over sales**: Registration status not sales status
- [x] **Context clarity**: This is NOT a public sales page

### Profile Settings Quality Gates
- [x] **Tabbed structure**: Personal, Social, Security, Vetting tabs
- [x] **Personal tab fields**: Scene name, email, pronouns, bio
- [x] **Social tab fields**: Social media links
- [x] **Security tab fields**: Current, new, confirm password
- [x] **Vetting tab fields**: Read-only status and date
- [x] **Separate save actions**: Per tab section
- [x] **Validation rules**: Field validation requirements specified

### Original Quality Gates (Still Required)
- [x] **All user roles addressed**: Admin, Teacher, Vetted Member, General Member, Guest
- [x] **Clear acceptance criteria**: All stories have detailed acceptance criteria
- [x] **Business value defined**: Clear value propositions documented
- [x] **Edge cases considered**: Empty states, mobile experience, error handling
- [x] **Security requirements documented**: Authentication, data protection, privacy
- [x] **Performance expectations set**: <2 second page load target
- [x] **Mobile experience considered**: Responsive design requirements specified
- [x] **Examples provided**: 6 comprehensive scenarios covering all use cases
- [x] **Success metrics defined**: Measurable outcomes for dashboard and profile features

---

## Implementation Readiness Verification

### Version 6.0 Design Approved ✅
- **Approved Wireframe**: dashboard-wireframe-v4-iteration.html
- **Approval Document**: APPROVED-DESIGN.md v1.0
- **Approval Date**: October 9, 2025
- **Design Authority**: Project Owner
- **Implementation Status**: Ready for development

### Simplified Structure Confirmed ✅
- **Two Pages**: My Events (dashboard) + Profile Settings
- **Dashboard Merge**: Dashboard preview + full events on single page
- **Navigation Reduction**: Fewer pages, simpler structure
- **Filter Controls**: Single filter bar with checkbox, view toggle, search
- **No Complex Tabs**: Removed Upcoming/Past/Cancelled tabs

### Vetting Alert Enhancement ✅
- **Conditional Display**: Only for non-vetted users
- **4 Status Messages**: Pending, Approved, On Hold, Denied
- **Status-Specific Actions**: Links to interview scheduling, reapply info
- **Visual Coding**: Color-coded alerts (Blue, Green, Amber, Red)
- **Hide When Vetted**: No alert clutter for vetted users

### User Dashboard Context Established ✅
- **Not a Sales Page**: This is user's registered events
- **Remove Sales Elements**: No pricing, capacity, "Learn More"
- **Registration Focus**: Status badges for registration state
- **Action Alignment**: "View Details" for registered events
- **User Events Only**: Filter to user's registrations/tickets

### Technical Foundation ✅
- **React Infrastructure**: Available for implementation
- **Mantine v7**: Provides tabs, cards, tables, modals, alerts
- **React Router**: Page navigation and tab state management
- **TanStack Query**: Event and profile data fetching
- **NSwag Types**: API type generation for all DTOs
- **Responsive Design**: Mobile-first approach with Mantine grid

### Business Requirements Met ✅
- **Approved Design**: All specifications from wireframe v4
- **Stakeholder Feedback**: October 2025 approval incorporated
- **Simple Functionality**: No over-engineering, clear purpose
- **Clean Interface**: User-focused dashboard design
- **Profile Accessibility**: Multiple entry points for editing
- **Vetting Transparency**: Clear status communication

---

## Version History

### Version 6.0 (October 9, 2025) - APPROVED DESIGN ALIGNMENT
**Changes from v5.0:**
- Aligned with approved wireframe v4 iteration
- Simplified navigation from 3-tab to 2-page structure
- Merged dashboard + events into single "My Events" page
- Added conditional vetting status alert box (4 messages)
- Added "Edit Profile" to utility bar and dashboard title bar
- Changed event filtering from tabs to "Show Past Events" checkbox
- Removed cancelled events section
- Updated event display to user dashboard context (no pricing/capacity)
- Changed event action button from "Learn More" to "View Details"
- Updated status badges: RSVP Confirmed, Ticket Purchased, Attended
- Removed all sales-focused elements from event cards

### Version 5.0 (October 2025) - PROFILE CONSOLIDATION
- Consolidated Profile, Security, Membership into single Profile Settings page
- Added prominent social event ticket indicators
- Added membership hold workflow
- Removed waitlist functionality completely
- Reduced navigation to 3 items

### Earlier Versions
- v4.0 and earlier: Initial requirements development and iterations

---

*This business requirements document (v6.0) aligns with the approved wireframe v4 iteration, representing the final design specification for implementation. All development work must match this approved design exactly. The dashboard provides a clean, user-focused interface showing registered events with conditional vetting status alerts, flexible view options, and quick profile access.*

*Next Phase*: UI Designer to create design specifications for components matching approved wireframe exactly, then React Developer to implement according to approved design.*
