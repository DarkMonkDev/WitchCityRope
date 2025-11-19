# Orchestrator Handoff - Vetted Member Import & Email Enhancement
**Date**: 2025-11-18
**Feature**: One-Time Vetted Member Import + Email Segment-Based Sending
**Status**: Requirements Complete, Ready for Implementation

## Project Overview

### Feature 1: One-Time Vetted Member Import Tool
**Purpose**: Import 140+ approved vetted members from Google Sheet into database
**Approach**: Console application with remote database support
**Testing**: Multiple local runs with database reset, then staging/production execution

### Feature 2: Email Admin Enhancement
**Purpose**: Add ability to send emails to user segments from Email Templates Admin UI
**Scope**: Backend segmentation system + Frontend send UI

## Requirements Summary

### Import Tool Requirements
1. Console application targeting .NET 9
2. Must support connection strings for: Local, Staging (DigitalOcean), Production (DigitalOcean)
3. Read Google Sheet "Accepted" tab via MCP tool
4. Import 140+ users with vetting status = Approved
5. Set EmailVerified = false (requires password reset)
6. Create VettingApplication records with historical data
7. Create VettingAuditLog entries from notes
8. Dry-run mode for testing
9. Error reporting (skip duplicates, log errors)
10. No export report needed

### Email Enhancement Requirements
1. Backend: User segmentation enum (AllVettedMembers, AllPreVettedMembers, AllTeachers, AllDMs, AllSafetyTeam, AllAdmins, EmailNotVerified, VettingPending)
2. Backend: GET /api/email-templates/segments (with counts)
3. Backend: GET /api/email-templates/segments/{name}/preview
4. Backend: Enhance POST /api/email-templates/ad-hoc/send to use segments
5. Backend: Add "NewWebsiteUser" email template to seeder
6. Frontend: Add "Send Ad-Hoc Email" section to Ad Hoc panel
7. Frontend: Segment selector with preview
8. Frontend: Send confirmation and notifications

### User Segment Definitions
- **AllVettedMembers**: VettingStatus == 3 (Approved)
- **AllPreVettedMembers**: VettingStatus NOT IN (4=Denied, 5=OnHold) AND IsActive=true
- **AllTeachers**: Role contains "Teacher"
- **AllDMs**: Role contains "DungeonMonitor"
- **AllSafetyTeam**: Role contains "SafetyTeam"
- **AllAdmins**: Role contains "Administrator"
- **EmailNotVerified**: EmailVerified == false
- **VettingPending**: VettingStatus == 0 (UnderReview)

### Password Reset Flow
- Imported users get EmailVerified = false
- Send "NewWebsiteUser" email with password reset link
- When user clicks link and sets password → EmailVerified = true
- No separate email verification step needed

## Implementation Phases

### Phase 1: Import Tool (PARALLEL execution where possible)
**Sub-agents**: database-designer, backend-developer, test-developer

**Phase 1A - Database Designer** (handoff: database-designer-2025-11-18-handoff.md)
- Review ApplicationUser and VettingApplication schemas
- Verify migration compatibility with import data
- Document any constraints that might affect import

**Phase 1B - Backend Developer - Import Tool** (handoff: backend-developer-import-2025-11-18-handoff.md)
- Create console application at /tools/VettedMemberImport/
- Implement Google Sheet reader (MCP tool wrapper)
- Implement import logic with duplicate detection
- Support connection strings for Local/Staging/Production
- Dry-run mode
- Error reporting and logging
- Documentation

**Phase 1C - Test Developer** (handoff: test-developer-2025-11-18-handoff.md)
- Create integration tests for import tool
- Test duplicate detection
- Test error handling
- Test dry-run mode

### Phase 2: Email Segmentation Backend (SEQUENTIAL after Phase 1B)
**Sub-agent**: backend-developer

**Phase 2A - Backend Developer - Email System** (handoff: backend-developer-email-2025-11-18-handoff.md)
- Create UserSegment enum
- Implement GET /api/email-templates/segments
- Implement GET /api/email-templates/segments/{name}/preview
- Enhance SendAdHocEmailAsync with segment support
- Add "NewWebsiteUser" template to EmailTemplateSeeder
- Variable replacement for {{user_name}}, {{reset_url}}
- Integration tests

### Phase 3: Email Admin UI (Can start after Phase 2A begins)
**Sub-agents**: ui-designer, react-developer

**Phase 3A - UI Designer** (handoff: ui-designer-2025-11-18-handoff.md)
- Design "Send Ad-Hoc Email" section for Ad Hoc panel
- Segment selector with recipient count
- Preview recipients list (first 10)
- Send confirmation dialog
- Success/error states

**Phase 3B - React Developer** (handoff: react-developer-2025-11-18-handoff.md)
- Implement SendAdHocEmail component
- Integrate with email template API
- Segment selector dropdown
- Preview recipients
- Send flow with confirmation
- Success/error notifications

### Phase 4: Integration Testing (SEQUENTIAL - after all phases)
**Sub-agent**: test-executor

- Full E2E test: Import → Email Admin → Send → Verify
- Staging database compatibility test
- Production readiness verification

## Agent Coordination

### Parallel Execution Groups
**Group 1** (can run in parallel):
- Phase 1A: Database Designer
- Phase 1B: Backend Developer (Import Tool)
- Phase 3A: UI Designer

**Group 2** (after Group 1, can run in parallel):
- Phase 1C: Test Developer (Import Tests)
- Phase 2A: Backend Developer (Email System)
- Phase 3B: React Developer (after UI design complete)

**Sequential**:
- Phase 4: Test Executor (after all above complete)

## Quality Gates

### Phase 1 Complete When:
- [ ] Console tool builds and runs
- [ ] Dry-run mode works
- [ ] Can connect to staging database
- [ ] Error reporting functional
- [ ] Integration tests pass

### Phase 2 Complete When:
- [ ] All segmentation endpoints implemented
- [ ] "NewWebsiteUser" template added
- [ ] SendAdHocEmail supports segments
- [ ] Integration tests pass
- [ ] API documentation updated

### Phase 3 Complete When:
- [ ] Send Ad-Hoc Email UI renders
- [ ] Can select segments and preview
- [ ] Can send emails successfully
- [ ] Error handling works
- [ ] UI tests pass

### Phase 4 Complete When:
- [ ] E2E test passes
- [ ] Staging database verified
- [ ] Production deployment documented
- [ ] All tests passing

## Handoff Documents
All agent handoffs are in `/docs/functional-areas/member-import/handoffs/`
Each handoff contains specific implementation details for that agent.

## Next Steps
1. Create all agent handoff documents
2. Delegate Phase 1A, 1B, 3A in parallel
3. Wait for Group 1 completion
4. Delegate Group 2 in parallel
5. Wait for Group 2 completion
6. Delegate Phase 4

---

**Orchestrator**: Ready to begin implementation
**Created**: 2025-11-18
**Next Review**: After Phase 1 completion
