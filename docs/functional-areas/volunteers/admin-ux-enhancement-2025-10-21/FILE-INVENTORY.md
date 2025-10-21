# Volunteer Position UX Enhancement - File Inventory
<!-- Last Updated: 2025-10-21 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Purpose
Complete inventory of all files required for volunteer position UX enhancement on admin event details page.

## Project Context
**Major UX Overhaul**: Removing modal dialog, adding inline editing, member assignment table with live search.

**Key Changes**:
- Remove modal-based position editing
- Add inline table editing for positions
- Add member assignment section with live search
- Reference Tickets/Capacity column styling from events dashboard

## Files to Modify

### 1. Admin Event Details Page
**Path**: `/home/chad/repos/witchcityrope/apps/web/src/pages/admin/AdminEventDetailsPage.tsx`
**Current State**: Displays event form with volunteer positions tab
**Lines**: 419 lines
**Key Features**:
- Event header with publish status toggle
- EventForm component integration
- Modal for status change confirmation
**Changes Needed**:
- May need to pass event data to volunteer positions components
- Coordinate with EventForm for volunteer tab

### 2. Volunteer Position Table Component
**Path**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/VolunteerPositionsGrid.tsx`
**Current State**: Table display with edit/delete buttons, opens modal for editing
**Lines**: 180 lines
**Key Features**:
- Table with Position, Sessions, Time, Description, Needed, Assigned, Delete columns
- "Add New Position" button at bottom
- Opens VolunteerPositionFormModal for editing
- Assignment status badges (red/yellow/green)
**Changes Needed**:
- **REMOVE**: Modal-based editing
- **ADD**: Inline editing for all fields
- **ADD**: Member assignment section below each position

### 3. Volunteer Position Modal Component
**Path**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/VolunteerPositionFormModal.tsx`
**Current State**: Modal dialog for add/edit volunteer position
**Lines**: 207 lines
**Key Features**:
- Fields: Title, Description, Sessions dropdown, Start/End Time, Slots Needed
- Form validation
- Save/Cancel buttons
**Changes Needed**:
- **LIKELY DEPRECATE**: Modal approach being removed
- **EXTRACT**: Form validation logic for inline editing
- **EXTRACT**: Field structure for inline components

### 4. Event Form Component (Container)
**Path**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventForm.tsx`
**Current State**: Multi-tab form with volunteers tab
**Lines**: 200+ lines (partial read)
**Key Features**:
- Tabs: basic-info, sessions, tickets, volunteers, participations, emails
- Contains VolunteerPositionsGrid component
- State management for volunteer positions array
**Changes Needed**:
- Update volunteer positions state management
- May need to pass additional props to volunteer components

## Reference Components

### 5. Capacity Display Component (for styling reference)
**Path**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/CapacityDisplay.tsx`
**Current State**: Shows current/max with progress bar
**Lines**: 57 lines
**Key Features**:
- Format: "currentValue/maxValue"
- Progress bar with color coding (red < 50%, yellow 50-80%, green >= 80%)
- Centered text display
- Accessibility attributes
**Reference For**:
- Assigned/Needed display styling (similar pattern)
- Progress bar visualization for volunteer slots

### 6. Events Table View (for Tickets/Capacity column styling)
**Path**: `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventsTableView.tsx`
**Current State**: Admin events dashboard table
**Lines**: 440 lines
**Key Features**:
- Uses CapacityDisplay component for Tickets/Capacity column
- Column width: `width: '160px', maxWidth: '160px'`
- Centered alignment: `textAlign: 'center'`
- Located at line 382-384
**Reference For**:
- Exact column styling to replicate
- Table layout patterns

## Missing Components (Need to Find/Create)

### 7. Member Search Component
**Status**: NOT FOUND in search
**Needed For**: Live member search for volunteer assignment
**Search Performed**:
- Pattern: `*MemberSearch*`, `*member*search*`
- Result: No existing member search components found
**Action Required**:
- Check if reusable member search exists elsewhere
- May need to create new MemberSearchInput component

## Functional Area Structure

### Current Volunteers Folder
**Base Path**: `/home/chad/repos/witchcityrope/docs/functional-areas/volunteers/`
**Existing Work**:
- `/working/volunteer-signup-ux-redesign-2025-10-20/` - Previous volunteer work

### New Working Folder
**Path**: `/home/chad/repos/witchcityrope/docs/functional-areas/volunteers/admin-ux-enhancement-2025-10-21/`
**Purpose**: Admin volunteer position management UX improvements
**Created**: 2025-10-21

## API Integration Points

### Volunteer Position Types
**Source**: `/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/types/volunteer.types.ts`
**API Client**: `/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/api/volunteerApi.ts`
**Hooks**: `/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/hooks/useVolunteerPositions.ts`

### Additional Components Found
- `/home/chad/repos/witchcityrope/apps/web/src/features/volunteers/components/VolunteerPositionCard.tsx` - Volunteer position display card
- `/home/chad/repos/witchcityrope/apps/web/src/components/members/MemberVolunteerTab.tsx` - Member volunteer information tab

## Next Steps

1. **Business Requirements Agent**: Review this inventory and create requirements document
2. **UI Designer**: Create wireframes for inline editing and member assignment table
3. **React Developer**: Review component architecture and plan refactoring
4. **Test Developer**: Plan test updates for new UX patterns

## File Registry Update Required

All files listed above must be tracked in `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md` when modified.

## Success Criteria

- [ ] All file paths validated and accessible
- [ ] Component dependencies mapped
- [ ] Styling references documented
- [ ] Missing components identified
- [ ] API integration points confirmed
- [ ] Working folder structure created
