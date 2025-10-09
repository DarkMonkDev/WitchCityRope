# User Dashboard Redesign - Approved Design Document

<!-- Last Updated: 2025-10-09 -->
<!-- Version: 1.0 -->
<!-- Owner: UI Design Team -->
<!-- Status: APPROVED FOR IMPLEMENTATION -->

## 🎯 APPROVAL STATUS

**Date Approved**: October 9, 2025
**Approved Wireframe**: `dashboard-wireframe-v4-iteration.html`
**Status**: ✅ **APPROVED FOR IMPLEMENTATION**
**Approval Authority**: Project Owner

---

## 📋 EXECUTIVE SUMMARY

This document serves as the **single source of truth** for all implementation work on the User Dashboard redesign. The v4 iteration wireframe has been approved as the final design, and all development work MUST match this specification exactly.

**Key Design Philosophy**: This is a **user dashboard** (not a public sales page). It focuses on the user's registered events and profile management with a clean, modern interface.

---

## 🎨 DESIGN SUMMARY

### Core Structure
- **2-Tab Layout**:
  - **My Events** (default tab) - User's registered events
  - **Profile Settings** (secondary tab) - User profile management

### Conditional Elements
- **Vetting Status Alert Box**: Shows only for non-vetted users (4 status types)
- **Past Events**: Hidden by default with checkbox toggle to show
- **View Toggle**: Grid (card) view and Table (list) view options

### Navigation Updates
- **Top Utility Bar**: Added "Edit Profile" link (before Logout)
- **Dashboard Title**: Displays as "[User's Name] Dashboard" (e.g., "ShadowKnot Dashboard")
- **Edit Profile Button**: Appears on dashboard page for quick access

---

## 🔑 KEY FEATURES FOR IMPLEMENTATION

### 1. Navigation Enhancement
**Requirements**:
- Add "Edit Profile" link to utility bar (positioned before "Logout")
- Link should navigate to Profile Settings tab
- Maintain existing About, Contact, and Logout links
- Visual consistency with existing navigation styling

**Implementation Notes**:
```html
<!-- Utility Bar Structure -->
<a href="#">About</a>
<a href="#">Contact</a>
<a href="#profile">Edit Profile</a> <!-- NEW -->
<a href="#logout">Logout</a>
```

### 2. Page Title and Profile Access
**Requirements**:
- Title format: `{FirstName} Dashboard` (e.g., "ShadowKnot Dashboard")
- "Edit Profile" button positioned to the right of the title
- Button uses secondary button styling (cream background, rose-gold border)

**Data Binding**:
```typescript
// Get user's first name from auth context
const { user } = useAuth();
const dashboardTitle = `${user.firstName} Dashboard`;
```

### 3. Vetting Status Alert Box
**Requirements**:
- Display conditionally based on vetting status
- **Show for**: Pending, Approved, On Hold, Denied
- **Hide for**: Vetted (no alert needed)

**Alert Variants**:

| Status | Icon | Color | Title | Message |
|--------|------|-------|-------|---------|
| **Pending** | ⏰ | Blue (#2196F3) | "Application Under Review" | "Your membership application is currently under review. We'll notify you via email once it's been reviewed." |
| **Approved** | ✅ | Green (#4CAF50) | "Great News! Your Application Has Been Approved" | "Schedule your vetting interview here to complete your membership." (with link) |
| **On Hold** | ⏸️ | Amber (#FFC107) | "Membership On Hold" | "Your membership is currently on hold. Contact us if you'd like to resume your membership." |
| **Denied** | ❌ | Red (#F44336) | "Application Not Approved" | "Your membership application was not approved at this time. Learn about reapplying." (with link) |

**Component Logic**:
```typescript
// Only render if NOT vetted
{vettingStatus !== 'Vetted' && (
  <VettingAlert status={vettingStatus} />
)}
```

### 4. Event Display - Grid View (Cards)
**Requirements**:
- Display user's registered events ONLY
- No pricing or capacity information (this is NOT a sales page)
- "View Details" button (NOT "Learn More")
- Status badges show registration state

**Event Card Structure**:
- **Header**: Gradient background with event title
- **Date/Time**: Formatted as "Day, Month Date • Time"
- **Location**: With location pin emoji
- **Status Badge**: Based on registration state
- **Action Button**: "View Details" (secondary styling)

**Status Badges**:
- **RSVP Confirmed** (Blue) - For social events with RSVP
- **Ticket Purchased** (Green) - For paid classes
- **Ticket Purchased (Social Event)** (Green) - For social events with tickets
- **Attended** (Purple) - For past events (when shown)

**Critical Implementation Notes**:
- ❌ **Remove**: Pricing, capacity, "spots available", "Learn More" buttons
- ✅ **Use**: "View Details" button for all events
- ✅ **Show**: Only events the user has registered for
- ✅ **Hide**: Public sales elements completely

### 5. Event Display - Table View (List)
**Requirements**:
- Same data as grid view, different layout
- Columns: Date, Time, Title, Status, Action
- Sortable by date (with sort indicator)
- Clickable rows navigate to event details

**Table Structure**:
```
| Date    | Time    | Event Title                    | Status          | Action       |
|---------|---------|--------------------------------|-----------------|--------------|
| Oct 31  | 7:00 PM | Halloween Rope Social          | RSVP Confirmed  | View Details |
| Nov 8   | 2:00 PM | Single Column Ties Workshop    | Ticket Purchased| View Details |
```

**Styling**:
- Header: Burgundy background (#880124), white text
- Rows: Alternate cream/white backgrounds
- Hover: Light burgundy background
- Status badges: Same as grid view

### 6. Filter and View Controls
**Requirements**:
- **Show Past Events**: Checkbox (unchecked by default)
- **View Toggle**: Card View (default) / List View buttons
- **Search Input**: Filter events by title/date
- All controls in single filter bar

**Behavior**:
- Past events hidden initially (`.hidden` class)
- Checkbox toggle shows/hides past events
- View toggle switches between grid and table
- Search filters visible events in real-time

### 7. Past Events Handling
**Requirements**:
- Past events have `.past-event` class
- Hidden by default with `.hidden` class
- "Show Past Events" checkbox controls visibility
- Past events show "Attended" status badge
- No separate "Cancelled Events" section

**Implementation**:
```typescript
// Toggle past events visibility
const togglePastEvents = (show: boolean) => {
  document.querySelectorAll('.past-event').forEach(event => {
    event.classList.toggle('hidden', !show);
  });
};
```

---

## 🚨 CRITICAL IMPLEMENTATION NOTES

### What to REMOVE (Sales Elements)
These elements exist in the public Events page but MUST NOT appear in the user dashboard:

❌ **Remove Completely**:
- Event pricing (`$45`, `Free`, etc.)
- Capacity information (`12 spots available`)
- "Learn More" buttons
- "Register Now" buttons
- Availability status (these are already registered)
- Sales-focused messaging

### What to USE Instead
✅ **User Dashboard Elements**:
- "View Details" button (replaces "Learn More")
- Registration status badges (replaces pricing)
- User's events only (replaces all available events)
- Focus on event information, not sales

### Data Requirements
**User Dashboard Shows**:
- Events where `user.id` exists in registrations/tickets
- Registration status from database
- Event details (title, date, time, location)
- Vetting status from user profile

**API Endpoints Needed**:
```
GET /api/users/{userId}/events           # User's registered events
GET /api/users/{userId}/vetting-status   # Vetting alert display
GET /api/events/{eventId}                # Event details (for View Details)
```

---

## 📁 REFERENCE FILES

### Approved Design
**Wireframe**: `/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/design/dashboard-wireframe-v4-iteration.html`

### Requirements and Specifications
**Business Requirements**: `/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/requirements/business-requirements.md`

**Functional Specifications**: *To be created by Business Requirements Agent*

### UX Research
**Modern Dashboard Analysis**: `/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/research/modern-dashboard-ux-analysis-2025-10-09.md`

### Implementation Planning
**Implementation Plan**: `/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/implementation-plan.md`

---

## 👥 FOR SUBAGENTS: IMPLEMENTATION REQUIREMENTS

### React Developer
**Primary Responsibility**: Frontend implementation matching wireframe exactly

**Tasks**:
1. Implement 2-tab layout (My Events + Profile Settings)
2. Create VettingAlert component with 4 status variants
3. Build EventCard component (grid view)
4. Build EventTable component (list view)
5. Implement filter controls (past events toggle, view toggle, search)
6. Add "Edit Profile" to utility bar navigation
7. Update page title with user's first name
8. Remove ALL sales elements from event displays

**Critical Rules**:
- ✅ Match approved wireframe pixel-perfect
- ✅ Use "View Details" button (NOT "Learn More")
- ❌ NO pricing or capacity information
- ❌ NO sales-focused elements
- ✅ Show user's registered events ONLY

### Backend Developer
**Primary Responsibility**: API endpoints for user-specific event data

**Tasks**:
1. Create `/api/users/{userId}/events` endpoint
   - Return only events user has registered for
   - Include registration status (RSVP, Ticket, Attended)
   - Filter by date range (upcoming vs past)
2. Create `/api/users/{userId}/vetting-status` endpoint
   - Return current vetting status
   - Include additional context if needed
3. Update `/api/events/{eventId}` for detail view
   - Ensure proper access control
   - Return user's registration status

**Data Models**:
```csharp
public class UserEventDto
{
    public int EventId { get; set; }
    public string Title { get; set; }
    public DateTime StartDate { get; set; }
    public string Location { get; set; }
    public string RegistrationStatus { get; set; } // "RSVP", "Ticket", "Attended"
    public bool IsPastEvent { get; set; }
}
```

### Test Developer
**Primary Responsibility**: E2E tests validating approved design

**Test Scenarios**:
1. **Navigation Tests**
   - Verify "Edit Profile" link in utility bar
   - Confirm dashboard title shows user's first name
   - Test "Edit Profile" button functionality

2. **Vetting Alert Tests**
   - Verify alert shows for Pending status
   - Verify alert shows for Approved with interview link
   - Verify alert shows for On Hold
   - Verify alert shows for Denied with reapply link
   - Verify NO alert for Vetted status

3. **Event Display Tests**
   - Verify grid view shows user's events only
   - Verify NO pricing information visible
   - Verify "View Details" button (NOT "Learn More")
   - Verify correct status badges
   - Verify table view with same data
   - Verify view toggle works correctly

4. **Filter Tests**
   - Verify past events hidden by default
   - Verify "Show Past Events" checkbox works
   - Verify search filters events
   - Verify all filters work in both views

5. **Data Accuracy Tests**
   - Verify only user's registered events shown
   - Verify no public/available events shown
   - Verify registration status accuracy
   - Verify past vs upcoming event filtering

**Test Coverage Requirements**:
- All vetting status variations (5 states)
- Both view modes (grid + table)
- All filter combinations
- Edge cases (no events, all past events, etc.)

### UI Designer
**Primary Responsibility**: Design refinement and asset delivery

**Tasks**:
1. Provide design system specifications:
   - Color values for all status badges
   - Typography specifications
   - Spacing and layout measurements
   - Animation/transition specifications
2. Create additional design assets if needed
3. Review implementation for design accuracy
4. Provide design feedback during implementation

**Quality Assurance**:
- Review implemented UI against wireframe
- Verify color accuracy and consistency
- Validate responsive behavior
- Confirm accessibility standards

---

## ✅ VALIDATION CHECKLIST

Before marking implementation complete, verify:

### Design Accuracy
- [ ] Page title format matches: "{FirstName} Dashboard"
- [ ] "Edit Profile" link added to utility bar (before Logout)
- [ ] "Edit Profile" button positioned on dashboard page
- [ ] Vetting alert box displays for correct statuses
- [ ] Vetting alert box hidden for Vetted users
- [ ] Event cards use gradient headers
- [ ] Status badges use correct colors and labels
- [ ] Table view columns match specification
- [ ] Filter bar layout matches wireframe
- [ ] View toggle works correctly
- [ ] Past events hidden by default

### Sales Element Removal
- [ ] No pricing information visible
- [ ] No capacity/availability information
- [ ] No "Learn More" buttons (replaced with "View Details")
- [ ] No "Register Now" buttons
- [ ] No sales-focused messaging
- [ ] Focus entirely on user's registered events

### Data Accuracy
- [ ] Shows only user's registered events
- [ ] Registration status correct for each event
- [ ] Past events properly identified
- [ ] Vetting status fetched correctly
- [ ] User's first name displayed in title

### Functionality
- [ ] Tab switching works (My Events ↔ Profile Settings)
- [ ] Grid view displays correctly
- [ ] Table view displays correctly
- [ ] View toggle switches between grid/table
- [ ] Past events toggle shows/hides past events
- [ ] Search filters events in real-time
- [ ] "View Details" navigates to event detail page
- [ ] "Edit Profile" navigates to Profile Settings

### Responsive Design
- [ ] Mobile layout works correctly
- [ ] Tablet layout works correctly
- [ ] Desktop layout matches wireframe
- [ ] All interactions work on touch devices

---

## 🚀 IMPLEMENTATION WORKFLOW

### Phase 1: Design Phase (CURRENT)
- ✅ Wireframe v4 approved (October 9, 2025)
- ✅ This approval document created
- ⏳ Functional specifications (next step)

### Phase 2: Implementation Phase
1. Backend Developer: Create API endpoints
2. React Developer: Implement UI components
3. Both: Integrate frontend with backend

### Phase 3: Testing Phase
1. Test Developer: Write E2E test suite
2. All Developers: Bug fixes based on test results
3. UI Designer: Design QA review

### Phase 4: Finalization Phase
1. Complete validation checklist
2. Performance optimization
3. Documentation updates
4. Production deployment

---

## 📞 QUESTIONS OR CLARIFICATIONS

If any aspect of this approved design is unclear:

1. **First**: Reference the approved wireframe at `design/dashboard-wireframe-v4-iteration.html`
2. **Second**: Review business requirements document
3. **Third**: Consult UX research documentation
4. **Last Resort**: Request clarification from project owner

**Remember**: This wireframe (v4) is the final approved design. All implementation work must match it exactly. Do not deviate without explicit approval.

---

## 🏁 SUCCESS CRITERIA

Implementation is complete when:

1. ✅ All elements from wireframe v4 are implemented
2. ✅ Validation checklist is 100% complete
3. ✅ All E2E tests pass
4. ✅ UI Designer approves visual implementation
5. ✅ Project owner approves final product

**This is a user dashboard, not a sales page. Keep focus on user's registered events and profile management.**

---

*End of Approved Design Document*
