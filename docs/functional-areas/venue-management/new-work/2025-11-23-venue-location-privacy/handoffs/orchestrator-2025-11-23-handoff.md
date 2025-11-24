# Orchestrator Handoff - Venue Location Privacy Feature
**Date**: 2025-11-23
**Orchestrator**: Main Agent
**Feature**: Venue Location Privacy
**Status**: Phase 0 - Handoff Documentation Complete

## Executive Summary

This feature adds a `Location` field to the Venue entity to enable privacy-aware location display. Non-vetted users will see "city, state" on event listings. Vetted users or users who have RSVP'd/purchased tickets will see full venue details (name, directions).

## Feature Requirements

### Database Changes
- Add `Location` field to Venue entity (max 100 characters)
- Field stores "city, state" format (e.g., "Salem, MA")
- Migration required

### Admin Settings UI
- Add "Location (city, state)" field to venue management in Admin Settings
- Label: "Location (city, state)"
- Help text to explain privacy purpose
- Validation: max 100 characters

### Display Logic Rules
**For Event Cards (Public View)**:
- Non-vetted users: Show `Location` field only
- Vetted users: Show `VenueName`

**For Event Details Page**:
- Before RSVP/ticket purchase: Show `Location` field only
- After RSVP/ticket purchase: Show `VenueName` and `Directions`

## Work Phases

### Phase 1: Database Design
**Agent**: database-designer
**Deliverable**: Database schema update with Location field
**Quality Gate**: 90%

### Phase 2: Backend Implementation
**Agent**: backend-developer
**Deliverable**: Updated Venue DTOs, endpoints, and API logic
**Quality Gate**: 85%

### Phase 3: UI Design
**Agent**: ui-designer
**Deliverable**: Admin UI wireframes and location visibility rules documentation
**Quality Gate**: 90%

### Phase 4: Frontend Implementation
**Agent**: react-developer
**Deliverable**: Admin venue management UI + Event card/detail display logic
**Quality Gate**: 85%

### Phase 5: Testing
**Agent**: test-executor
**Deliverable**: All tests passing, privacy rules verified
**Quality Gate**: 100%

## Agent Coordination Plan

### Parallel Execution Opportunities
1. **Phase 1 + Phase 3**: Database design and UI design can run in parallel
2. **Phase 2 depends on Phase 1**: Backend needs database schema
3. **Phase 4 depends on Phase 2 + Phase 3**: Frontend needs API and design
4. **Phase 5 depends on Phase 4**: Testing after implementation

### Agent Handoff Documents
- `database-designer-2025-11-23-handoff.md` - Database schema design
- `backend-developer-2025-11-23-handoff.md` - API implementation
- `ui-designer-2025-11-23-handoff.md` - UI wireframes and rules
- `react-developer-2025-11-23-handoff.md` - Frontend implementation
- `test-executor-2025-11-23-handoff.md` - Testing verification

## Key Technical Details

### Existing Venue Entity
Current fields: Id, Name, Directions, Notes, IsActive, CreatedByUserId, CreatedAt, UpdatedAt

### New Field
- **Name**: Location
- **Type**: string
- **Max Length**: 100
- **Nullable**: Yes (for backward compatibility with existing venues)
- **Purpose**: City and state display for privacy

### API Endpoints to Update
- Admin Venue Settings GET/PUT endpoints
- Event listing endpoints (must include location in DTO)
- Event details endpoints (conditional logic based on user vetting status and participation)

### Frontend Components to Update
- Admin Settings > Venue Management form
- Event cards component (public event listing)
- Event details page (conditional display logic)

## Success Criteria
- [ ] Location field added to database
- [ ] Admin can set location for venues
- [ ] Non-vetted users see only city/state on event cards
- [ ] Vetted users see venue name on event cards
- [ ] Event details show city/state until RSVP/purchase
- [ ] Event details show full venue info after RSVP/purchase
- [ ] All existing tests pass
- [ ] New tests verify privacy rules

## File Paths

**Feature Folder**: `/home/chad/repos/witchcityrope/docs/functional-areas/venue-management/new-work/2025-11-23-venue-location-privacy/`

**Progress Tracking**: `/home/chad/repos/witchcityrope/docs/functional-areas/venue-management/new-work/2025-11-23-venue-location-privacy/progress.md`

**Handoffs**: `/home/chad/repos/witchcityrope/docs/functional-areas/venue-management/new-work/2025-11-23-venue-location-privacy/handoffs/`

## Next Steps
1. Create database-designer handoff and delegate Phase 1
2. Create ui-designer handoff and delegate Phase 3 (parallel)
3. After Phase 1 complete: Delegate Phase 2 (backend-developer)
4. After Phase 2+3 complete: Delegate Phase 4 (react-developer)
5. After Phase 4 complete: Delegate Phase 5 (test-executor)
