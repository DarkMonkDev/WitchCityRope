# Phase Transition Handoff: Business Requirements → UI Design
<!-- Last Updated: 2025-09-11 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Phase Transition Summary
**FROM**: Phase 1 - Business Requirements (COMPLETE)
**TO**: Phase 2 - UI Design (STARTING)
**Date**: September 11, 2025
**Project**: Admin Events Dashboard Enhancement

## Phase 1 Deliverables Complete ✅

### Primary Business Requirements Document
- **Location**: `/home/chad/repos/witchcityrope-react/docs/functional-areas/events/admin-dashboard/requirements/business-requirements-2025-09-11.md`
- **Status**: Complete and approved
- **Key Achievement**: Comprehensive requirements with 7 user stories and detailed acceptance criteria
- **Stakeholder Feedback**: Received and incorporated - NO color differentiation for past events

### Critical Stakeholder Requirements Summary
**From approved business requirements:**
- Move "Create New Event" button to same level as page title
- Replace dropdown with two clickable filter words ("Social" and "Class")
- Add search bar for real-time filtering
- Transform from cards to table view layout
- Show capacity/tickets as fraction with progress bar
- **NO special color for past events** (stakeholder confirmed)
- Full page navigation for event editing (not modal)
- Implement breadcrumb navigation

## Critical Resources for UI Designer

### 🎨 EXISTING WIREFRAMES (MUST REFERENCE)
These wireframes provide the design foundation and should guide your work:

**Admin Dashboard Wireframe**:
- **Location**: `/home/chad/repos/witchcityrope-react/docs/functional-areas/events/new-work/2025-08-24-events-management/design/wireframes/admin-events-dashboard.html`
- **Purpose**: Complete HTML wireframe with WitchCityRope design system
- **Key Elements**: Admin header, sidebar navigation, main content area layout
- **Design System**: Burgundy color scheme, Mantine component styling patterns

**Event Form Wireframe**:
- **Location**: `/home/chad/repos/witchcityrope-react/docs/functional-areas/events/new-work/2025-08-24-events-management/design/wireframes/event-form.html`
- **Purpose**: Event creation/editing form design reference
- **Navigation**: Shows breadcrumb patterns and form layout structure

### 🔧 CURRENT IMPLEMENTATION REFERENCE
**React Component Analysis**:
- **Location**: `/home/chad/repos/witchcityrope-react/apps/web/src/pages/admin/AdminEventsPage.tsx`
- **Current State**: Card-based layout using Mantine Grid/Paper components
- **Key Functions**: formatEventDisplay helper, event handlers for edit/delete operations
- **Data Structure**: Uses EventDto from @witchcityrope/shared-types
- **Current Features**: Event creation modal, edit/delete actions, basic event display

## Design Requirements from Business Phase

### Header Controls Layout
1. **Page Title + Create Button**: Position "Create New Event" at same level as "Events Dashboard" title
2. **Filter Controls**: Two clickable words "Social" and "Class" with independent selection states
3. **Search Bar**: Real-time filtering input with search icon and placeholder
4. **Past Events Toggle**: Checkbox/toggle control for historical event visibility

### Table Design Requirements
**Column Structure**:
- Date (sortable) - Format: "MMM DD, YYYY"
- Event Title (sortable) - Primary event name
- Time - Format: "HH:MM AM/PM - HH:MM AM/PM"  
- Capacity/Tickets - Fraction display (4/20) with progress bar

**Visual Design**:
- Alternating row colors for readability
- Hover effects for interactive rows
- Clear typography hierarchy
- Row click navigation (entire row clickable)
- Progress bars for capacity visualization (>80% emphasized)

### Navigation & Interaction
- **Row Click**: Navigate to event edit page (full page, not modal)
- **Copy Functionality**: Action button in table for event duplication
- **Breadcrumbs**: "Dashboard > Events > Edit Event Name" pattern
- **Mobile Responsive**: Horizontal scroll for table on smaller screens

## Design Constraints & Standards

### Technology Requirements
- **UI Framework**: Mantine v7 components only
- **Styling**: Follow existing WitchCityRope design system
- **Color Scheme**: Burgundy primary (#880124), Rose Gold accents (#B76D75)
- **Typography**: Consistent with current font hierarchy
- **Icons**: Tabler icons (existing import pattern)

### Responsive Design
- **Desktop**: Full table view with all columns
- **Tablet**: Maintain functionality, consider column priority
- **Mobile**: Horizontal scroll acceptable, ensure critical columns visible
- **Touch Targets**: Appropriate sizing for mobile interaction

### Accessibility Standards
- Keyboard navigation support for table interactions
- Clear focus indicators for interactive elements
- Screen reader friendly table structure with proper headers
- Color-blind friendly design (don't rely solely on color for information)

## Key Business Rules for Design

### Event Display Logic
1. **Default View**: Show only published upcoming events
2. **Past Events**: Include when "Show Past Events" toggle is enabled
3. **No Special Coloring**: Past events appear identical to upcoming events
4. **Capacity Indicators**: Visual progress bars with >80% emphasis
5. **Search Scope**: Real-time search on event titles only

### Filter Interaction Patterns
- **Independent Selection**: Social and Class filters work independently
- **Visual States**: Clear active/inactive states for filter words
- **Real-time Updates**: Table updates immediately on filter changes
- **State Persistence**: Filters persist during session, reset on reload

## Data Structure Reference

### EventDto Properties (from API)
```typescript
{
  id: string;              // GUID
  title: string;           // Max 200 characters
  startDateTime: DateTime; // ISO 8601
  endDateTime: DateTime;   // ISO 8601
  capacity: number;        // Min 1
  currentAttendees: number; // Default 0
  status: EventStatus;     // Published, Draft, Cancelled
  eventType: EventType;    // Class, Social, Workshop, Performance
  venueId?: string;        // Optional
  venueName?: string;      // Optional
}
```

## Migration Context

### From Current Card Layout
The existing implementation uses:
- Mantine Grid components for layout
- Paper components for event cards
- Modal for event creation
- Individual card action buttons

### To New Table Layout
Your design should specify:
- Mantine Table component usage
- Header control positioning
- Filter control styling
- Capacity progress bar design
- Row interaction states
- Mobile responsive behavior

## Success Criteria for UI Design Phase

### Design Deliverables Required
1. **Updated Wireframes**: Complete HTML mockups showing table layout
2. **Component Specifications**: Detailed Mantine component usage
3. **Mobile Responsive Design**: Breakpoint behavior specifications
4. **Interactive States**: Hover, active, focus, loading states
5. **Filter Control Design**: Visual states for clickable filter words
6. **Progress Bar Design**: Capacity visualization patterns

### Quality Standards
- Consistent with existing WitchCityRope design system
- Follows Mantine v7 component patterns
- Mobile-first responsive approach
- Accessibility compliance (WCAG 2.1 AA)
- Performance considerations for 500+ events

## Human Review Checkpoint

After completing the UI design phase, your deliverables will go through human review before proceeding to functional specification. Focus on:

1. **Visual Alignment**: Does the design match stakeholder requirements?
2. **Usability**: Are the table interactions intuitive and efficient?
3. **Technical Feasibility**: Can this be implemented with Mantine v7?
4. **Mobile Experience**: Is the responsive design practical?
5. **Design System Consistency**: Does it align with existing UI patterns?

## Next Steps for UI Designer

### Immediate Actions
1. **Review Existing Wireframes**: Study the referenced HTML wireframes thoroughly
2. **Analyze Current Implementation**: Understand the card-to-table transformation needed
3. **Create Table Layout Design**: Focus on header controls and table structure
4. **Design Filter Interactions**: Specify the "Social"/"Class" clickable word behavior
5. **Detail Capacity Visualization**: Design the progress bar integration

### Design Process
1. Start with existing admin-events-dashboard.html as foundation
2. Transform card grid area to table layout
3. Redesign header with inline Create button and filter controls
4. Create detailed component specifications for developers
5. Ensure mobile responsiveness throughout design

### Deliverable Structure
Create your design deliverables at:
```
/docs/functional-areas/events/admin-dashboard/design/
├── ui-design-2025-09-11.md           # Main design document
├── wireframes/
│   ├── admin-table-desktop.html      # Desktop table view
│   ├── admin-table-mobile.html       # Mobile responsive view
│   └── admin-filters-interactions.html # Filter states and interactions
└── specifications/
    └── mantine-components-spec.md    # Detailed component usage
```

## Critical Success Factors

### Must-Have Elements
- Table replaces card layout completely
- Create button moved to title level
- Clickable "Social"/"Class" filter words (not dropdown)
- Real-time search functionality design
- Capacity progress bars integrated in table
- Full-page event edit navigation (not modal)

### Design Excellence Markers
- Seamless transformation from existing design system
- Intuitive filter interaction patterns  
- Efficient admin workflow optimization
- Mobile-responsive table experience
- Performance-conscious design for large datasets

This handoff provides you with all the context, requirements, and resources needed to create exceptional UI designs for the Admin Events Dashboard enhancement. The existing wireframes are your starting point - build upon them to create the table-based interface that meets all stakeholder requirements.

Focus on creating designs that are not only visually appealing but also highly functional for admin users managing multiple events efficiently. Your designs will directly impact the daily workflow of WitchCityRope administrators.