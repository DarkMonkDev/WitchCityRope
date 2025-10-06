# Events Business Requirements Clarifications - 2025-10-05

## Executive Summary

This document captures critical requirement clarifications provided by the user that require immediate updates to the Events Management System business requirements. These clarifications correct misunderstandings and ensure the implementation matches actual business needs.

## Critical Requirement Changes

### 1. RSVP + Tickets for Social Events (CRITICAL CORRECTION)

**Previous Understanding (INCORRECT):**
- Social events: RSVP-based registration (free)
- Classes: Ticket-based registration (paid)

**Corrected Understanding:**
- **Social events**: Users can BOTH:
  - RSVP (free, confirms attendance intent)
  - Purchase tickets (ahead of time OR at the door during check-in)
- **RSVPs and ticket purchases are SEPARATE actions** for social events
- Both must be tracked and managed independently

**Impact:**
- Social events need BOTH RSVP table AND ticket purchase table
- Check-in system must handle both RSVP confirmation and ticket purchase
- Business logic must support dual registration paths

**Files Affected:**
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/business-requirements.md` - Lines 196-201
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/implementation/tdd-implementation-plan.md` - Lines 487

### 2. Check-in System (IN SCOPE - Phase 1)

**Previous Understanding (PARTIALLY INCORRECT):**
- Check-in system referenced as "kiosk mode"
- Unclear if in Phase 1 scope

**Corrected Understanding:**
- **Staff-assisted check-in** (NOT self-service kiosk)
- **IN SCOPE for Phase 1** - REQUIRED feature
- Staff member checks everyone in when they arrive
- Check-in screen is REQUIRED for Phase 1
- The method for bringing up the check-in screen is in scope

**Impact:**
- Check-in interface is Phase 1 priority
- Focus on staff workflow, not self-service
- Must support both RSVP confirmation and ticket purchase at check-in

**Files Affected:**
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/business-requirements.md` - Lines 133-147 (Story 8)
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/implementation/tdd-implementation-plan.md` - Lines 381-444 (Phase 5)

### 3. Teacher Permissions (CONFIRMED)

**Previous Understanding:** Unclear if teachers could edit events

**Confirmed Understanding:**
- **Teachers CANNOT edit events**
- Only Administrators and Event Organizers can edit events
- Teachers must contact Event Organizers for any changes needed

**Impact:**
- Explicitly document teacher limitations
- No edit permissions in teacher role
- Clear UI messaging about request process

**Files Affected:**
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/business-requirements.md` - Lines 214-218

### 4. Email Automation (CONFIRMED)

**Previous Understanding:** Email automation was included but priority unclear

**Confirmed Understanding:**
- **Automated emails ARE part of the event process**
- Keep all automated email features as designed
- No changes needed to email functionality

**Impact:**
- No changes required
- Maintain current email automation design

**Files Affected:**
- None - current implementation correct

### 5. Event Images (DEFERRED)

**Previous Understanding:** Image upload included in requirements

**Corrected Understanding:**
- **Defer event image upload functionality to future phase**
- Remove from Phase 1 scope
- Mark as future enhancement

**Impact:**
- Remove image upload from current requirements
- Document as deferred feature
- Update implementation plan

**Files Affected:**
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/business-requirements.md` - Any image references
- `/docs/functional-areas/events/new-work/2025-08-24-events-management/implementation/tdd-implementation-plan.md` - Line 499

## Requirement Updates Required

### Business Requirements Document Updates

1. **Event Types and RSVP Rules (Lines 196-201)**
   - Update to clarify social events support BOTH RSVP and tickets
   - Specify that RSVPs and tickets are separate actions
   - Document check-in workflow for both paths

2. **Story 8: Check-in System (Lines 133-147)**
   - Change title from "Kiosk Mode Check-In System" to "Staff-Assisted Check-In System"
   - Clarify this is Phase 1, not kiosk self-service
   - Update acceptance criteria for staff workflow
   - Remove kiosk mode security requirements

3. **Teacher Permissions (Lines 214-218)**
   - Already correct, no changes needed
   - Verify language is explicit about "NO editing capabilities"

4. **Event Images**
   - Add to Constraints section as "DEFERRED"
   - Remove any image upload requirements from current scope

### Implementation Plan Updates

1. **Phase 5: Check-In System (Lines 381-444)**
   - Move to Phase 1 or Phase 2
   - Retitle as "Staff-Assisted Check-In"
   - Update time estimates
   - Adjust dependencies

2. **Business Logic Questions (Line 487)**
   - Answer Question 1: YES, social events allow both free RSVPs and ticket purchases (separate actions)
   - Clarify tickets can be purchased ahead OR at door during check-in

3. **UI/UX Questions (Line 499)**
   - Answer Question 9: Event images DEFERRED to future phase

## Implementation Impact Analysis

### High Impact Changes

1. **Social Event Registration Logic**
   - Database schema must support both RSVP and ticket records
   - UI must present both options clearly
   - Business logic must handle dual registration paths
   - Check-in must verify both RSVP status and ticket purchase

2. **Check-in System Priority**
   - Move from Phase 5 to Phase 1/2
   - Focus on staff workflow instead of kiosk security
   - Simplify authentication requirements
   - Support RSVP confirmation + ticket purchase at door

### Medium Impact Changes

1. **Event Images Removal**
   - Remove from current phase requirements
   - Update UI designs to not include image upload
   - Document as future enhancement

### Low Impact Changes

1. **Teacher Permissions Clarification**
   - Already correctly documented
   - May need stronger UI messaging

## Conflicts and Inconsistencies Found

### Conflict 1: Social Event Registration Model
**Location:** Business Requirements lines 196-201 vs User Clarification

**Current Documentation Says:**
```markdown
2. **Social Events**: RSVP system with optional ticket purchases
3. **RSVP vs Tickets**: Social events show both RSVP table AND tickets sold table
```

**User Clarification Says:**
- Social events: Users can RSVP (free) AND purchase tickets (separate actions)
- Both ahead of time OR tickets at door during check-in

**Resolution:**
Update to explicitly state these are separate, parallel actions, not "RSVP with optional upgrade"

### Conflict 2: Check-in System Scope and Priority
**Location:** Implementation Plan Phase 5 (lines 381-444) vs User Clarification

**Current Documentation Says:**
- Phase 5 (last phase): 10-14 hours
- "Kiosk Mode Check-In System"
- Focus on security tokens and browser lockdown

**User Clarification Says:**
- IN SCOPE for Phase 1
- Staff-assisted check-in (NOT kiosk self-service)
- Required feature, not optional

**Resolution:**
- Retitle as "Staff-Assisted Check-In System"
- Move to Phase 1 or early Phase 2
- Simplify security requirements (staff login, not kiosk tokens)
- Update time estimates and dependencies

### Conflict 3: Event Images Scope
**Location:** TDD Implementation Plan Line 499 vs User Clarification

**Current Documentation Says:**
- Listed in UI/UX questions: "Event Images: Support for event photos/banners in this phase?"

**User Clarification Says:**
- Defer event image upload functionality to future phase

**Resolution:**
Remove from current scope, mark as deferred in constraints

## Next Steps

1. **Update Business Requirements Document**
   - Fix RSVP+Tickets language for social events
   - Update Story 8 for staff-assisted check-in
   - Add event images to deferred features
   - Verify teacher permissions language

2. **Update Implementation Plan**
   - Reorganize phases to prioritize check-in
   - Answer outstanding questions
   - Update time estimates
   - Revise dependencies

3. **Update File Registry**
   - Log all modified files
   - Track requirement change dates
   - Document cleanup dates for temporary files

4. **Create Summary Report**
   - List all files modified
   - Summarize key changes
   - Confirm alignment with user needs

## Validation Checklist

- [ ] Business requirements document updated
- [ ] Implementation plan reorganized
- [ ] All conflicts resolved
- [ ] File registry updated
- [ ] Summary report created
- [ ] User confirmation obtained

---

**Document Status:** ACTIVE - Requirements Clarification Analysis
**Created:** 2025-10-05
**Author:** Business Requirements Agent
**Next Review:** After requirements update completion
