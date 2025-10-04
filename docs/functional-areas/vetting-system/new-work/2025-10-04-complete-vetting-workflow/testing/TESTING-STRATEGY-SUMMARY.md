# Complete Vetting Workflow - Testing Strategy Summary

<!-- Created: 2025-10-04 -->
<!-- Purpose: Executive summary of comprehensive testing plan -->

## Overview

Comprehensive testing plan created for the complete vetting workflow implementation covering all layers from unit tests to E2E workflows.

**Document**: `/docs/functional-areas/vetting-system/new-work/2025-10-04-complete-vetting-workflow/testing/test-plan.md`

---

## Quick Reference

### Test Count: 93 Tests Total

- **Unit Tests**: 68 tests
- **Integration Tests**: 25 tests
- **E2E Tests**: 18 tests

### Implementation Time: 12-16 hours

### Coverage Targets

- **Unit Tests**: 80% minimum code coverage
- **Integration Tests**: All API endpoints
- **E2E Tests**: All critical user journeys

---

## Test Breakdown

### Unit Tests (68 tests)

**VettingAccessControlService** - 23 tests:
- RSVP access control for all vetting statuses (8 tests)
- Ticket purchase access control (8 tests)
- Caching behavior (3 tests)
- Audit logging (2 tests)
- Error handling (2 tests)

**VettingEmailService** - 20 tests:
- Application confirmation emails (5 tests)
- Status update notifications (6 tests)
- Reminder emails (4 tests)
- Email logging (3 tests)
- Error handling (2 tests)

**VettingService** - 25 tests:
- Valid status transitions (8 tests)
- Invalid transitions and terminal states (5 tests)
- Specialized methods (ScheduleInterview, PutOnHold, Approve, Deny) (6 tests)
- Email integration (3 tests)
- Transaction and error handling (3 tests)

### Integration Tests (25 tests)

**ParticipationEndpoints** - 10 tests:
- RSVP access control (5 tests)
- Ticket purchase access control (5 tests)

**VettingEndpoints** - 15 tests:
- Status update endpoints (7 tests)
- Email integration (3 tests)
- Audit logging (2 tests)
- Transaction rollback (3 tests)

### E2E Tests (18 tests)

**Admin Vetting Workflow** - 14 tests:
- Login and navigation (2 tests)
- Grid display and filtering (3 tests)
- Application detail view (2 tests)
- Status change modals (4 tests)
- Sorting and pagination (2 tests)
- Error handling (1 test)

**Access Control** - 4 tests:
- RSVP blocking for denied users (2 tests)
- Ticket purchase blocking (2 tests)

---

## Critical Business Rules Validated

### Access Control
- Users without applications can RSVP (general members)
- OnHold, Denied, and Withdrawn statuses block RSVP and ticket purchases
- All other statuses allow access
- Access denials are logged to VettingAuditLog

### Email Notifications
- Mock mode logs emails to console (development)
- Production mode uses SendGrid
- All emails logged to VettingEmailLog
- Email failures don't block status changes
- Template variable substitution works correctly

### Status Transitions
- Valid transition rules enforced
- Terminal states (Approved, Denied) cannot be changed
- OnHold and Denied require admin notes
- Approval grants VettedMember role
- All changes create audit logs

### Data Integrity
- Database transactions rollback on errors
- Concurrent updates handled with concurrency tokens
- All timestamps are UTC
- Caching improves performance (5-minute TTL)

---

## Test Environment Requirements

### Unit Tests
- xUnit + Moq + FluentAssertions
- In-memory mocking (no database)
- Fast execution (<100ms per test)
- Target: 80% code coverage

### Integration Tests
- WebApplicationFactory
- TestContainers with PostgreSQL 16
- Real database transactions
- Seeded test data
- Target: All API endpoints tested

### E2E Tests
- Playwright against Docker containers
- Port 5173 EXCLUSIVELY (Docker-only)
- Pre-flight verification required
- Screenshot capture on failures
- Target: All critical workflows validated

---

## Test Data Requirements

### Users (7 test accounts)
- admin@witchcityrope.com (Administrator, Approved)
- vetted@witchcityrope.com (VettedMember, Approved)
- denied@witchcityrope.com (Member, Denied)
- onhold@witchcityrope.com (Member, OnHold)
- withdrawn@witchcityrope.com (Member, Withdrawn)
- member@witchcityrope.com (Member, no application)
- reviewing@witchcityrope.com (Member, UnderReview)

### Vetting Applications
- 10+ applications in various statuses for grid testing
- Applications with status change history for audit log testing

### Events
- Public social event (capacity < max)
- Paid class event (capacity < max)
- Past event (for validation testing)
- Sold out event (capacity = currentAttendees)

### Email Templates (Optional)
- ApplicationReceived, Approved, Denied, OnHold, InterviewReminder
- Fallback templates work if database templates not present

---

## Implementation Phases

### Phase 1: Unit Tests (CRITICAL)
- Create 3 test files (VettingAccessControlServiceTests, VettingEmailServiceTests, VettingServiceStatusChangeTests)
- Verify 80% code coverage
- All tests pass in <10 seconds
- **Estimated Time**: 6-8 hours

### Phase 2: Integration Tests (HIGH)
- Create 2 test files (ParticipationEndpointsTests, VettingEndpointsTests)
- Setup TestContainers with PostgreSQL
- Seed test data
- All tests pass with real database
- **Estimated Time**: 3-4 hours

### Phase 3: E2E Tests (HIGH)
- Create 2 test files (vetting-workflow.spec.ts, vetting-restrictions.spec.ts)
- Verify Docker-only environment (port 5173)
- Implement test data helpers
- Screenshot capture on failures
- **Estimated Time**: 2-3 hours

### Phase 4: CI/CD Integration (MEDIUM)
- Create GitHub Actions workflow
- Configure test environment variables
- Setup coverage reporting
- **Estimated Time**: 1 hour

### Phase 5: Documentation (MEDIUM)
- Update TEST_CATALOG.md (already done)
- Document test data requirements
- Create troubleshooting guide
- Add lessons learned
- **Estimated Time**: 30 minutes

---

## Execution Time Estimates

**Unit Tests**: <10 seconds
**Integration Tests**: <2 minutes
**E2E Tests**: <3 minutes
**Total Suite**: ~3.5 minutes

**Parallel Execution**:
- Unit tests can run in parallel (all isolated)
- Integration tests can run in parallel with unique database names
- E2E tests sequential (avoid race conditions on Docker)

---

## Success Criteria

**Definition of Done**:
- All 93 tests implemented and passing
- 80% code coverage achieved
- All tests run in CI/CD successfully
- No flaky tests (100% pass rate over 10 runs)
- Documentation updated in TEST_CATALOG.md
- Handoff document created for test-executor

**Quality Gates**:
- Unit tests execute in <10 seconds
- Integration tests execute in <2 minutes
- E2E tests execute in <3 minutes
- All critical workflows validated end-to-end
- Access control properly enforced and tested

---

## Known Risks & Mitigation

### Risk 1: Email Service Failures
**Mitigation**: Email failures do NOT block status changes, comprehensive logging for manual follow-up

### Risk 2: Concurrent Status Updates
**Mitigation**: Database concurrency tokens, integration tests simulate conflicts

### Risk 3: Role Grant Failures
**Mitigation**: Dedicated tests for ApproveApplicationAsync role grant logic

### Risk 4: Cache Staleness
**Mitigation**: 5-minute cache expiration, cache invalidation on status changes (TODO)

### Risk 5: Docker Environment Issues
**Mitigation**: Mandatory pre-flight checklist, kill-local-dev-servers.sh script, health checks

---

## Next Steps

1. **Review test plan** with team
2. **Begin Phase 1** (Unit Tests)
3. **Create handoff document** for test-executor after implementation
4. **Monitor CI/CD** for test failures

---

## Related Documents

- **Full Test Plan**: `/docs/functional-areas/vetting-system/new-work/2025-10-04-complete-vetting-workflow/testing/test-plan.md`
- **TEST_CATALOG**: `/docs/standards-processes/testing/TEST_CATALOG.md`
- **Docker Testing Standard**: `/docs/standards-processes/testing/docker-only-testing-standard.md`
- **Playwright Guide**: `/docs/standards-processes/testing/browser-automation/playwright-guide.md`

---

**Status**: READY FOR IMPLEMENTATION
**Created**: 2025-10-04
**Test Developer**: Ready to begin implementation
