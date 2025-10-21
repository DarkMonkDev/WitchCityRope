# Volunteer Positions Admin UX Enhancement - Implementation Plan

**Date**: 2025-10-21
**Developer**: React Developer Agent
**Working Folder**: `/docs/functional-areas/volunteers/admin-ux-enhancement-2025-10-21/`

## Overview
Major UX overhaul to replace modal-based editing with inline editing and add member assignment functionality.

## Key Changes

### 1. Table Updates
- ✅ Remove Edit button column
- ✅ Combine "Needed" and "Assigned" columns into single "Needed / Filled" column using CapacityDisplay pattern
- ✅ Add "Visibility" badge column (Public/Private based on `isPublicFacing`)
- ✅ Make rows clickable to expand inline edit area

### 2. Remove Modal
- ✅ Deprecate `VolunteerPositionFormModal.tsx`
- ✅ Extract reusable form logic

### 3. Inline Edit Area
- ✅ Create `VolunteerPositionInlineForm.tsx` component
- ✅ Appears below "Add New Position" button
- ✅ Opens in create mode (blank) or edit mode (populated)
- ✅ Uses Mantine Collapse for smooth animation
- ✅ Form fields match current modal fields

### 4. Member Assignment Section
- ✅ Create `AssignedMembersSection.tsx` component
- ✅ Only shows when editing existing position
- ✅ Table with columns: Scene Name, Email, FetLife, Discord, Remove
- ✅ Search input with live filtering
- ✅ Add member functionality

## Component Architecture

```
VolunteerPositionsGrid (updated)
├── VolunteerPositionsTable (new - extracted)
│   ├── Rows with CapacityDisplay
│   └── Visibility badge column
├── Button: "Add New Position"
├── Collapse: Inline edit area
│   ├── VolunteerPositionInlineForm (new)
│   │   ├── Form inputs
│   │   └── Save/Cancel/Delete buttons
│   └── AssignedMembersSection (new - only in edit mode)
│       ├── AssignedMembersTable (new)
│       │   └── Rows with member data + Remove
│       └── MemberSearchInput (new)
│           └── Select with live search
```

## API Endpoints Needed

### Existing (verify)
- GET `/api/events/{eventId}/volunteer-positions` - List positions
- POST `/api/volunteer-positions` - Create position
- PUT `/api/volunteer-positions/{id}` - Update position
- DELETE `/api/volunteer-positions/{id}` - Delete position

### May Need (document if missing)
- GET `/api/volunteer-positions/{id}/signups` - Get assigned members
- POST `/api/volunteer-positions/{id}/signups` - Assign member
- DELETE `/api/volunteer-signups/{signupId}` - Remove member assignment
- GET `/api/users/search?q={query}` - Search members

## Implementation Steps

1. ✅ Read all reference files and understand patterns
2. ✅ Create new component files
3. ✅ Update VolunteerPositionsGrid with new table structure
4. ✅ Implement inline edit functionality
5. ✅ Add member assignment section
6. ✅ Update EventForm.tsx to use new components
7. ✅ Test all functionality
8. ✅ Update lessons learned if patterns discovered

## Files to Create

- `/apps/web/src/components/events/VolunteerPositionInlineForm.tsx`
- `/apps/web/src/components/events/AssignedMembersSection.tsx`
- `/apps/web/src/components/events/AssignedMembersTable.tsx`
- `/apps/web/src/components/events/MemberSearchInput.tsx`

## Files to Modify

- `/apps/web/src/components/events/VolunteerPositionsGrid.tsx` - Major refactor
- `/apps/web/src/components/events/EventForm.tsx` - Update state management
- `/apps/web/src/components/events/VolunteerPositionFormModal.tsx` - Deprecate

## Testing Checklist

- [ ] No edit buttons in table
- [ ] "Needed / Filled" column matches CapacityDisplay styling
- [ ] Visibility badge shows Public/Private correctly
- [ ] Clicking row expands inline edit
- [ ] "Add New Position" opens blank inline form
- [ ] Form uses approved input styling
- [ ] Only one edit area open at a time
- [ ] Smooth collapse/expand animations
- [ ] Assigned members table matches volunteer positions table styling
- [ ] Member search works with live filtering
- [ ] Can remove members from position
- [ ] Can add members to position
- [ ] Save/Cancel/Delete buttons work correctly
- [ ] All API calls use proper error handling

## Notes

- Use mock data for member assignment if API endpoints don't exist
- Document missing API endpoints clearly
- Follow Mantine patterns consistently
- Ensure proper TypeScript typing throughout
