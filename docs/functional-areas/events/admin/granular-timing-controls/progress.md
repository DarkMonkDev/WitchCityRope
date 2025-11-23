# Granular Event Timing Controls - Progress Tracker
<!-- Last Updated: 2025-11-22 -->
<!-- Version: 1.2 -->
<!-- Status: Production - Bug Fix Complete -->

## Feature Overview

**Feature Name**: Granular Event Timing Controls
**Work Type**: Feature Development
**Start Date**: 2025-11-18
**Target Completion**: TBD (8-10 days estimated)

**Business Value**: Replace global "Pre-Start Buffer" system setting with per-event, granular timing controls for RSVP, Ticket, and Volunteer registration/cancellation windows.

## Quality Gates

Based on Feature Development work type:

| Gate | Target | Status |
|------|--------|--------|
| **Requirements** | 95% | ⏸️ Not Started |
| **Design** | 90% | ⏸️ Not Started |
| **Implementation** | 85% | ⏸️ Not Started |
| **Testing** | 100% | ⏸️ Not Started |

## Phase Progress

### Phase 1: Requirements & Planning ✅ COMPLETE

**Status**: ✅ **COMPLETE** (2025-11-18)
**Duration**: 1 day
**Completion**: 100%

**Deliverables**:
- [x] Implementation Plan created
- [x] Database Designer Handoff created
- [x] Backend Developer Handoff created
- [x] React Developer Handoff created
- [x] UI Designer Handoff created
- [x] Test Developer Handoff created
- [x] Progress Tracker created (this document)

**Documents Created**:
- `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/implementation-plan.md`
- `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/handoffs/database-designer-handoff.md`
- `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/handoffs/backend-developer-handoff.md`
- `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/handoffs/react-developer-handoff.md`
- `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/handoffs/ui-designer-handoff.md`
- `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/handoffs/test-developer-handoff.md`
- `/home/chad/repos/witchcityrope/docs/functional-areas/events/admin/granular-timing-controls/progress.md`

**Key Decisions**:
1. ✅ Window Independence: Registration and cancellation windows completely independent
2. ✅ Post-Event Limits: Fixed maximum of -24 hours (24 hours after event)
3. ✅ RSVP/Ticket Settings: Shared settings (one set of windows for both)
4. ✅ Time Units: Hours only (decimal allowed, 0.5 = 30 minutes)
5. ✅ NULL Defaults: NULL fields = no restriction (backward compatible)

### Phase 2: Database Design ⏸️ NOT STARTED

**Status**: ⏸️ **NOT STARTED**
**Assigned To**: database-designer agent
**Estimated Duration**: 1 day
**Completion**: 0%

**Tasks**:
- [ ] Create migration adding 6 nullable decimal columns to Events table
- [ ] Add 6 check constraints enforcing -24 minimum
- [ ] Add descriptive database comments
- [ ] Create migration rollback script
- [ ] Test migration on local dev database
- [ ] Test migration on staging database
- [ ] Verify existing events unaffected (all NULL values)
- [ ] Update Event.cs entity class with 6 properties
- [ ] Create migration tests

**Deliverables**:
- Migration: Add Event Timing Control columns
- Entity: Updated Event.cs
- Tests: Migration correctness tests

**Blockers**: None

### Phase 3: Backend Implementation ⏸️ NOT STARTED

**Status**: ⏸️ **NOT STARTED**
**Assigned To**: backend-developer agent
**Estimated Duration**: 2-3 days
**Completion**: 0%

**Tasks**:
- [ ] Create EventActionType enum
- [ ] Refactor TimeZoneService.IsRegistrationOpenAsync → IsActionAllowedAsync
- [ ] Update AttendanceService (3 enforcement points)
- [ ] Update VolunteerService (2 new enforcement points)
- [ ] Create new volunteer cancel endpoint (POST /api/volunteer-signups/{id}/cancel)
- [ ] Update EventDto with 6 timing properties
- [ ] Create comprehensive unit tests (95%+ coverage)
- [ ] Create integration tests (100% pass required)
- [ ] Test backward compatibility (NULL fields)

**Deliverables**:
- Service: TimeZoneService.IsActionAllowedAsync method
- Service: VolunteerService.CancelVolunteerSignup method
- Endpoint: POST /api/volunteer-signups/{signupId}/cancel
- DTO: Updated EventDto
- Tests: Unit + Integration test suites

**Blockers**: Depends on Phase 2 (database migration)

### Phase 4: UI Design ⏸️ NOT STARTED

**Status**: ⏸️ **NOT STARTED**
**Assigned To**: ui-designer agent
**Estimated Duration**: 1-2 days
**Completion**: 0%

**Tasks**:
- [ ] Create wireframe: RSVP/Tickets tab settings collapsed
- [ ] Create wireframe: RSVP/Tickets tab settings expanded
- [ ] Create wireframe: Volunteers tab settings expanded
- [ ] Create wireframe: User volunteer assignment with cancel button
- [ ] Create wireframe: Cancel confirmation dialog
- [ ] Create wireframe: Error state - cancellation window closed
- [ ] Create component specifications document
- [ ] Document interaction specifications
- [ ] Create accessibility checklist

**Deliverables**:
- 6 wireframes (PNG format)
- Component specifications
- Interaction specifications
- Accessibility checklist

**Blockers**: None (can run parallel with Phase 3)

### Phase 5: Frontend Implementation ⏸️ NOT STARTED

**Status**: ⏸️ **NOT STARTED**
**Assigned To**: react-developer agent
**Estimated Duration**: 2-3 days
**Completion**: 0%

**Tasks**:
- [ ] Regenerate TypeScript types from updated EventDto
- [ ] Add RSVP/Tickets timing settings section to EventForm
- [ ] Add Volunteers timing settings section to EventForm
- [ ] Implement timing settings toggle (show/hide)
- [ ] Implement NumberInput components with validation
- [ ] Add cancel button to UserVolunteerShifts
- [ ] Create volunteerApi.cancelVolunteerSignup method
- [ ] Implement form validation (< -24 rejection)
- [ ] Create component tests
- [ ] Test with NULL values (backward compatibility)

**Deliverables**:
- Component: EventForm timing settings sections
- Component: UserVolunteerShifts cancel button
- API: volunteerApi.cancelVolunteerSignup
- Types: Regenerated TypeScript types
- Tests: Component test suite

**Blockers**: Depends on Phase 3 (backend API) and Phase 4 (UI design)

### Phase 6: E2E Testing ⏸️ NOT STARTED

**Status**: ⏸️ **NOT STARTED**
**Assigned To**: test-developer agent
**Estimated Duration**: 2 days
**Completion**: 0%

**Tasks**:
- [ ] Create admin timing settings E2E tests (6 tests)
- [ ] Create user RSVP timing E2E tests (6 tests)
- [ ] Create user cancellation timing E2E tests (6 tests)
- [ ] Create user volunteer timing E2E tests (6 tests)
- [ ] Run full E2E suite
- [ ] Achieve 100% E2E pass rate
- [ ] Document any issues found

**Deliverables**:
- E2E Tests: Admin configuration flows
- E2E Tests: User timing flows (RSVP, cancel, volunteer)
- Test Report: E2E validation results

**Blockers**: Depends on Phase 5 (frontend implementation)

### Phase 7: Cleanup & Finalization ⏸️ NOT STARTED

**Status**: ⏸️ **NOT STARTED**
**Assigned To**: backend-developer agent + git-manager agent
**Estimated Duration**: 1 day
**Completion**: 0%

**Tasks**:
- [ ] Delete PreStartBufferMinutes from Settings table
- [ ] Remove UI for global setting from admin settings page
- [ ] Update admin documentation
- [ ] Verify no references to old setting in codebase
- [ ] Update PROGRESS.md
- [ ] Update functional-area-master-index.md
- [ ] Update file registry
- [ ] Create git commit
- [ ] Deploy to staging
- [ ] Deploy to production

**Deliverables**:
- Migration: Remove PreStartBufferMinutes setting
- Documentation: Updated admin guides
- Git: Commit with all changes
- Deployment: Staging + production releases

**Blockers**: Depends on Phase 6 (E2E testing complete)

## Agent Assignments

| Phase | Agent | Status | Start Date | End Date |
|-------|-------|--------|------------|----------|
| Phase 1: Requirements | Business Requirements | ✅ Complete | 2025-11-18 | 2025-11-18 |
| Phase 2: Database | database-designer | ⏸️ Not Started | TBD | TBD |
| Phase 3: Backend | backend-developer | ⏸️ Not Started | TBD | TBD |
| Phase 4: UI Design | ui-designer | ⏸️ Not Started | TBD | TBD |
| Phase 5: Frontend | react-developer | ⏸️ Not Started | TBD | TBD |
| Phase 6: Testing | test-developer | ⏸️ Not Started | TBD | TBD |
| Phase 7: Finalization | backend-developer + git-manager | ⏸️ Not Started | TBD | TBD |

## Blockers & Issues

### Current Blockers
None (Phase 1 complete, ready to proceed)

### Resolved Blockers
None yet

### Known Issues
None yet

## Decisions Log

| Date | Decision | Rationale | Impact |
|------|----------|-----------|--------|
| 2025-11-18 | Use nullable decimals for timing fields | Supports 0.5 hour increments, NULL = no restriction | Database schema, validation logic |
| 2025-11-18 | -24 hours post-event maximum | Allows day-after cancellations, prevents indefinite timing | Validation constraints |
| 2025-11-18 | Shared RSVP/Ticket settings | Simplifies UI, reduces configuration complexity | Only 4 fields instead of 8 |
| 2025-11-18 | Separate volunteer timing fields | Volunteer workflows different from RSVP/Tickets | 2 additional fields required |
| 2025-11-18 | Collapsible settings sections | Reduces visual clutter for events without timing config | UI pattern, toggle button |

## Key Metrics

### Development Progress
- **Overall Completion**: 14% (1 of 7 phases complete)
- **Documentation**: 100% (all handoff documents complete)
- **Implementation**: 0% (not started)
- **Testing**: 0% (not started)

### Quality Metrics (Targets)
- **Unit Test Coverage**: Target 95%+, Actual: TBD
- **Integration Test Pass Rate**: Target 100%, Actual: TBD
- **E2E Test Pass Rate**: Target 100%, Actual: TBD
- **TypeScript Compilation**: Target 0 errors, Actual: TBD

### Timeline
- **Estimated Total Duration**: 8-10 days
- **Actual Duration So Far**: 1 day
- **Remaining Estimated**: 7-9 days
- **On Track**: ✅ Yes (just started)

## Success Criteria

### Functional Requirements
- [ ] Per-event timing controls working for RSVP
- [ ] Per-event timing controls working for Tickets
- [ ] Per-event timing controls working for Volunteers
- [ ] Registration and cancellation windows independent
- [ ] Post-event limits enforced (-24 max)
- [ ] NULL fields = no restriction (backward compatible)
- [ ] Decimal hour support (0.5 = 30 minutes)
- [ ] User volunteer cancel functionality operational

### Quality Gates
- [ ] 95%+ unit test coverage achieved
- [ ] 100% integration test pass rate
- [ ] 100% E2E test pass rate
- [ ] Zero TypeScript compilation errors
- [ ] Zero breaking changes to existing events
- [ ] Backward compatibility verified

### User Experience
- [ ] Admin can configure timing per event
- [ ] Settings UI intuitive and clear
- [ ] Users see appropriate messages when outside windows
- [ ] Volunteer cancel feature functional
- [ ] Error messages helpful and actionable

### Documentation
- [ ] Implementation plan complete ✅
- [ ] Handoff documents for all agents ✅
- [ ] API documentation updated
- [ ] Admin user guide updated
- [ ] File registry updated
- [ ] Master index updated

### Production Readiness
- [ ] Migration tested on staging
- [ ] Rollback plan documented
- [ ] Performance validated (no degradation)
- [ ] Security review completed
- [ ] Deployment runbook created

## Next Steps

**Immediate Next Action**: Human review and approval of implementation plan

**After Approval**:
1. **Database Designer**: Create migration and entity updates (Phase 2)
2. **Backend Developer**: Implement API changes and tests (Phase 3)
3. **UI Designer**: Create wireframes and component specs (Phase 4, parallel with Phase 3)
4. **React Developer**: Implement UI components and tests (Phase 5)
5. **Test Developer**: Create E2E test suite (Phase 6)
6. **Git Manager**: Finalization and deployment (Phase 7)

## Risk Assessment

### High Risk Items
- **Backward Compatibility**: Existing events without timing config must continue working
  - **Mitigation**: Extensive NULL handling tests, gradual rollout
- **TimeZoneService Refactoring**: Central service affects multiple features
  - **Mitigation**: Comprehensive unit tests, integration tests before deployment

### Medium Risk Items
- **User Confusion**: Complex timing configuration may confuse admins
  - **Mitigation**: Clear UI labels, help text, admin documentation
- **Volunteer Cancel Feature**: New user-facing functionality requires UI/UX validation
  - **Mitigation**: E2E tests, staging validation before production

### Low Risk Items
- **Database Migration**: Standard column additions
  - **Mitigation**: Migration tests, rollback script
- **DTO Changes**: Standard NSwag regeneration
  - **Mitigation**: Type safety validation, compilation checks

## Bug Fixes

### 2025-11-19: Timing Fields Persistence Bug Fix ✅ COMPLETE

**Issue**: Timing fields displayed correctly and could be edited in the Admin Event Details form, but changes weren't persisting to the database when saved.

**Root Cause**: Two-layer issue:
1. **Frontend Transformation Layer**: `eventDataTransformation.ts` wasn't including timing fields in update payload
2. **Backend DTO Layer**: `UpdateEventRequest.cs` was missing timing field properties
3. **Backend Service Layer**: `EventService.cs` wasn't mapping timing fields to entity before saving

**Fix Details**:
- **Frontend** (`/apps/web/src/utils/eventDataTransformation.ts`):
  - Added timing fields to `convertEventFormDataToUpdateDto()` (lines 95-113)
  - Added timing fields to `getChangedEventFields()` (lines 274-292)

- **Backend** (`/apps/api/Features/Events/Models/UpdateEventRequest.cs`):
  - Added all 6 timing fields as `decimal?` properties

- **Backend** (`/apps/api/Features/Events/Services/EventService.cs`):
  - Added timing field mapping logic to update entity before saving (lines 375-405)

**Testing**: Manual testing confirmed values now persist correctly across page refreshes and database queries.

**Status**: ✅ COMPLETE - Timing fields now fully functional for create, read, and update operations.

### 2025-11-22: Timing Fields Data Loss Bug Fix ✅ COMPLETE

**Date**: 2025-11-22
**Issue**: Admin event timing fields lose data when switching tabs after saving
**Status**: ✅ **COMPLETE**

**Problem**:
- User edits RSVP timing field in RSVP/Tickets tab
- User clicks "Save Timing"
- User switches to Volunteers tab
- **BUG**: Volunteer timing fields are now blank/reset (same issue in reverse)

**Root Cause**:
- Frontend save handlers only sent their respective timing fields (4 for RSVP, 2 for Volunteer)
- Backend detects timing-only updates and updates ALL 6 timing fields
- Missing fields in request interpreted as `null` → data loss when switching tabs

**Solution**:
- Modified `handleSaveRsvpTiming` to send ALL 6 timing fields (added volunteer fields)
- Modified `handleSaveVolunteerTiming` to send ALL 6 timing fields (added RSVP fields)
- `form.values` already contains all 6 fields from initial event data
- Now both save handlers send complete timing data to preserve values in both tabs

**Files Modified**:
- `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventForm.tsx` (lines 1197-1277)

**Testing**: Manual testing confirmed:
- ✅ RSVP Save → Volunteer Check: Volunteer timing fields preserved
- ✅ Volunteer Save → RSVP Check: RSVP timing fields preserved
- ✅ Both Tabs Edit → Both Save: All values persist through both saves
- ✅ Database Verification: All 6 timing fields updated correctly

**Impact**:
- **User Impact**: All timing data now persists correctly → improved UX and data integrity
- **Code Quality**: Complete data sent → follows best practices for partial updates
- **Backend Compatibility**: No backend changes required (already handles correctly)

**Status**: ✅ COMPLETE - Fix implemented, tested, and ready for deployment

## Communication Log

| Date | Type | Participants | Summary |
|------|------|--------------|---------|
| 2025-11-18 | Planning | Business Requirements Agent | Implementation plan and handoff documents created |
| 2025-11-18 | Implementation | Backend Developer, React Developer | Full feature implementation completed |
| 2025-11-19 | Bug Fix | React Developer | Fixed timing fields persistence issue |
| 2025-11-22 | Bug Fix | React Developer | Fixed timing fields data loss when switching tabs |

## Files Created/Modified

See file registry for complete tracking:
- `/home/chad/repos/witchcityrope/docs/architecture/file-registry.md`

**Phase 1 Files**:
- Implementation plan
- 6 handoff documents
- This progress tracker

**Bug Fix Files (2025-11-22)**:
- `/home/chad/repos/witchcityrope/apps/web/src/components/events/EventForm.tsx` (MODIFIED)
- `/home/chad/repos/witchcityrope/session-work/2025-11-22/admin-event-timing-data-loss-fix.md` (CREATED)
- `/home/chad/repos/witchcityrope/test-results/lint-validation-event-form-timing-fix.md` (CREATED)

**Future Files** (to be created in subsequent phases):
- Migration files
- Service files
- Component files
- Test files
- Documentation updates

---

**This progress tracker will be updated after each phase completion. Current status: Phase 1 complete, awaiting human approval to proceed to Phase 2.**
