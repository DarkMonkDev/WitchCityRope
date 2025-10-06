# Testing Completion Plan
<!-- Last Updated: 2025-10-06 -->
<!-- Version: 1.0 -->
<!-- Owner: Test Executor Agent -->
<!-- Status: Active -->

## Purpose
This document provides a phased strategy to complete all test suites and achieve production-ready test coverage across React unit, .NET integration, and Playwright E2E tests.

## Current State Summary
**Source**: `/home/chad/repos/witchcityrope/test-results/comprehensive-test-analysis-2025-10-05.md`

### Test Suite Status (as of 2025-10-05)

| Test Suite | Status | Pass Rate | Details |
|------------|--------|-----------|---------|
| **React Unit Tests (Vitest)** | 🟡 Mixed | 56% (155/277) | 100 failed, 22 skipped |
| **Integration Tests (.NET)** | 🟡 Poor | 55% (17/31) | 14 failed (12 vetting-related) |
| **E2E Tests (Playwright)** | 🟡 Variable | 57-83% | Events Comprehensive: 57%, Events Workflow: 83% |
| **.NET Unit Tests** | ⏱️ Timeout | N/A | TestContainers too slow (>3 min) |

### Environment Health
- ✅ Docker containers operational (Web: 5173, API: 5655, DB: 5433)
- ✅ .NET compilation clean (0 errors, 41 deprecation warnings only)
- ✅ All services responding correctly

## Test Failure Categorization

From comprehensive test analysis, failures categorized into 4 types:

### Category A: Legitimate Bugs (54 tests)
**Tests validating IMPLEMENTED features that are broken**

**Backend (.NET Integration)** - 12 tests
- Vetting workflow endpoints (status changes, audit logging)
- Email notification failures
- Access control checks (403 returns)
- RSVP access control integration
- **Agent**: backend-developer
- **Priority**: 🔴 CRITICAL

**Frontend (React Unit)** - ~40-50 tests
- Dashboard hook failures (network timeout, malformed responses)
- Authentication error handling (login/logout failures)
- Query caching behavior
- **Agent**: react-developer
- **Priority**: 🔴 CRITICAL

**E2E Tests** - 2 tests
- Logout navigation failure
- Admin event editing workflow
- **Agent**: react-developer + backend-developer
- **Priority**: 🟠 HIGH

### Category B: Unimplemented Features (15 tests)
**Tests for features NOT YET built - should be marked as test.skip()**

**E2E Tests** - ~2-3 tests
- Event detail modal/view (component doesn't exist)
- Event card click interaction (not implemented)
- RSVP button functionality (acknowledged incomplete)

**React Unit Tests** - ~10-15 tests
- VettingApplicationsList "No applications" message
- Event CRUD form validation (UI incomplete)

**Agent**: test-developer
**Action**: Mark with `test.skip()` and TODO comments
**Priority**: 🟡 MEDIUM

### Category C: Outdated Tests (35 tests)
**Tests using OLD selectors/expectations - need updates**

**E2E Tests** - ~3-4 tests
- Public events browsing (wrong endpoint, returns 401)
- Event card selectors (`data-testid="event-title"` missing)
- Large event dataset (0 events rendered - data generation issue)

**React Unit Tests** - ~30-40 tests
- Component structure mismatches (VettingApplicationsList, event forms)
- Text content expectations
- Element selector mismatches

**Agent**: test-developer
**Priority**: 🟡 MEDIUM

### Category D: Infrastructure Issues (2 tests)
**Test setup problems, NOT production bugs**

**.NET Integration Tests** - 2 tests
- Phase2ValidationIntegrationTests.DatabaseContainer_ShouldBeRunning_AndAccessible
- Phase2ValidationIntegrationTests.ServiceProvider_ShouldBeConfigured

**Agent**: test-executor or backend-developer
**Priority**: 🟢 LOW

## Critical Blockers

### 🚨 Blocker 1: Vetting System Backend (12 Integration Tests)
**Impact**: CRITICAL - Blocks event access control, RSVP functionality

**Failing Tests**:
1. `VettingEndpointsIntegrationTests.Approval_CreatesAuditLog`
2. `VettingEndpointsIntegrationTests.Approval_GrantsVettedMemberRole`
3. `VettingEndpointsIntegrationTests.StatusUpdate_AsNonAdmin_Returns403`
4. `VettingEndpointsIntegrationTests.StatusUpdate_WithValidTransition_Succeeds`
5. `VettingEndpointsIntegrationTests.AuditLogCreation_IsTransactional`
6. `VettingEndpointsIntegrationTests.OnHold_RequiresReasonAndActions`
7. `VettingEndpointsIntegrationTests.StatusUpdate_CreatesAuditLog`
8. `VettingEndpointsIntegrationTests.Approval_SendsApprovalEmail`
9. `VettingEndpointsIntegrationTests.StatusUpdate_EmailFailureDoesNotPreventStatusChange`
10. `VettingEndpointsIntegrationTests.StatusUpdate_WithInvalidTransition_Fails`
11. `ParticipationEndpointsAccessControlTests.RsvpEndpoint_WhenUserIsApproved_Returns201`
12. `ParticipationEndpointsAccessControlTests.RsvpEndpoint_WhenUserHasNoApplication_Succeeds`

**Root Cause**: Vetting status transitions, audit logging, email notifications, and access control not working
**Estimated Effort**: 2-3 days
**Priority**: 🔴 CRITICAL - Must fix first

### 🚨 Blocker 2: React Dashboard & Auth Error Handling (40-50 Unit Tests)
**Impact**: HIGH - User experience degraded, error states broken

**Failing Areas**:
- Network timeout handling in `useCurrentUser`/`useEvents` hooks
- Malformed API response validation
- Login failure state management
- Logout error recovery
- Query caching behavior

**Root Cause**: Frontend error handling not robust
**Estimated Effort**: 1-2 days
**Priority**: 🟠 HIGH

### 🚨 Blocker 3: Admin Event Editing (E2E Failures)
**Impact**: HIGH - Admin functionality broken

**Failing Test**: `events-complete-workflow.spec.ts` Step 2
**Error**: Admin cannot find/edit events in admin panel
**Root Cause**: Admin events management UI incomplete or broken
**Estimated Effort**: 1 day
**Priority**: 🟠 HIGH

## Phased Completion Strategy

### Phase 1: Baseline + Quick Wins (1-2 days)
**Goal**: Establish clean baseline and eliminate test noise

**Tasks**:
1. **Mark Unimplemented Features** - Category B (15 tests)
   - Use `test.skip()` with TODO comments
   - Document which features are NOT in current scope
   - **Agent**: test-developer
   - **Files**: React unit tests, E2E tests
   - **Effort**: 4-6 hours

2. **Update Outdated Selectors** - Category C (Easy subset, ~10-15 tests)
   - Fix `data-testid` mismatches in event components
   - Update component structure expectations in simple cases
   - **Agent**: test-developer
   - **Files**: E2E event tests, React component tests
   - **Effort**: 6-8 hours

3. **Fix Test Infrastructure** - Category D (2 tests)
   - Resolve Phase2ValidationIntegrationTests setup issues
   - **Agent**: test-executor or backend-developer
   - **Effort**: 2-4 hours

**Success Criteria**:
- React Unit: ~65-70% pass rate (Category B skipped, easy Category C fixed)
- Integration: 65% pass rate (infrastructure fixed)
- E2E: 70-75% pass rate (selectors updated)
- **Commit**: "test: Phase 1 baseline - skip unimplemented features and fix selectors"

### Phase 2: Critical Blockers (3-4 days)
**Goal**: Fix production-blocking bugs in core systems

**Task 1: Vetting System Backend** (12 tests) - 🔴 CRITICAL
- Fix vetting status transition logic
- Implement audit log creation for status changes
- Fix email notification sending
- Resolve access control (403) checks
- Fix RSVP access control integration
- **Agent**: backend-developer
- **Files**: `/apps/api/Features/Vetting/`, integration tests
- **Effort**: 2-3 days
- **Verification**: Run `dotnet test tests/integration/` - all vetting tests pass

**Task 2: React Dashboard Error Handling** (40-50 tests) - 🟠 HIGH
- Implement network timeout handling in hooks
- Add malformed API response validation
- Fix login/logout error state management
- Resolve query caching behavior
- **Agent**: react-developer
- **Files**: `/apps/web/src/features/dashboard/`, `/apps/web/src/hooks/`, auth hooks
- **Effort**: 1-2 days
- **Verification**: Run `npm run test` - dashboard/auth tests pass

**Success Criteria**:
- React Unit: >80% pass rate (Category A dashboard/auth fixed)
- Integration: >90% pass rate (vetting system fixed)
- E2E: 75-80% pass rate (stability improved)
- **Commit**: "fix: Phase 2 critical blockers - vetting system and dashboard error handling"

### Phase 3: Systematic Bug Fixes (2-3 days)
**Goal**: Fix remaining legitimate bugs (Category A)

**Task 1: Public Events Endpoint** (E2E failures)
- Fix public events returning 401 (should allow anonymous access)
- Review endpoint authentication requirements
- **Agent**: backend-developer
- **Files**: `/apps/api/Features/Events/`
- **Effort**: 4-6 hours
- **Verification**: E2E test "should browse events without authentication" passes

**Task 2: Admin Event Editing UI**
- Fix admin panel events table/list not showing
- Resolve event editing workflow
- **Agent**: react-developer
- **Files**: `/apps/web/src/pages/admin/`, admin events components
- **Effort**: 1 day
- **Verification**: E2E test Step 2 admin editing passes

**Task 3: Remaining Category C Updates** (20-25 tests)
- Update complex component structure expectations
- Fix text content assertions
- Resolve remaining selector mismatches
- **Agent**: test-developer
- **Files**: React unit tests, E2E tests
- **Effort**: 1 day
- **Verification**: Test suite passes increase

**Success Criteria**:
- React Unit: >90% pass rate (all Category A fixed, Category C updated)
- Integration: >95% pass rate (public events fixed)
- E2E: >90% pass rate (admin UI fixed)
- **Commit**: "fix: Phase 3 systematic bug fixes - public events, admin UI, test updates"

### Phase 4: Final Validation and CI/CD (1 day)
**Goal**: Ensure all tests pass reliably in CI/CD environment

**Tasks**:
1. **Run Full Test Suite 3 Times**
   - Verify no flaky tests
   - Check for timing-dependent failures
   - **Agent**: test-executor
   - **Effort**: 2-3 hours

2. **Optimize .NET Unit Test Performance**
   - Address TestContainers 3+ minute timeout
   - Consider test database caching
   - **Agent**: backend-developer or test-executor
   - **Effort**: 4-8 hours

3. **Document Test Coverage**
   - Update `/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md`
   - Create test coverage report
   - **Agent**: test-developer
   - **Effort**: 2-3 hours

4. **CI/CD Pipeline Verification**
   - Run tests in GitHub Actions
   - Verify Docker container test execution
   - **Agent**: test-executor
   - **Effort**: 2-3 hours

**Success Criteria**:
- React Unit: >95% pass rate (stable across 3 runs)
- Integration: >98% pass rate (stable across 3 runs)
- E2E: >95% pass rate (stable across 3 runs)
- .NET Unit: <60 seconds execution time
- **Commit**: "test: Phase 4 final validation - CI/CD ready"

## Success Criteria

### Overall Goals
- **React Unit Tests**: >90% pass rate (250+ of 277 passing)
- **Integration Tests**: >95% pass rate (30+ of 31 passing)
- **E2E Tests**: >90% pass rate (stable across all suites)
- **All Unimplemented Features**: Properly marked with `test.skip()`
- **CI/CD Pipeline**: All tests passing in GitHub Actions
- **Test Execution Time**: Reasonable (<5 minutes total for rapid feedback)

### Quality Metrics
- **Zero Flaky Tests**: All tests pass 3 consecutive runs
- **Clear Test Names**: Every test clearly states what it validates
- **Proper Categorization**: All tests in correct suites (unit/integration/e2e)
- **Documentation**: TEST_CATALOG.md updated with current status

## Rollback Plan

If any phase introduces regressions:

1. **Immediate Rollback**: `git revert <commit-hash>`
2. **Analyze Failure**: Review test output and error logs
3. **Fix in Isolation**: Create branch, fix issue, verify locally
4. **Re-run Phase**: Re-execute phase tasks with fix
5. **Document Issue**: Add to lessons learned

## Progress Tracking

Use this checklist to track completion:

### Phase 1: Baseline + Quick Wins ⏳
- [ ] Mark Category B tests with `test.skip()`
- [ ] Fix easy Category C selector updates
- [ ] Fix Category D infrastructure issues
- [ ] Verify 65-70% React unit pass rate
- [ ] Commit and push

### Phase 2: Critical Blockers ⏳
- [ ] Fix vetting system backend (12 tests)
- [ ] Fix React dashboard error handling (40-50 tests)
- [ ] Verify >80% React unit pass rate
- [ ] Verify >90% integration pass rate
- [ ] Commit and push

### Phase 3: Systematic Bug Fixes ⏳
- [ ] Fix public events endpoint
- [ ] Fix admin event editing UI
- [ ] Update remaining Category C tests
- [ ] Verify >90% all test suites
- [ ] Commit and push

### Phase 4: Final Validation ⏳
- [ ] Run full suite 3 times (verify stability)
- [ ] Optimize .NET unit test performance
- [ ] Update TEST_CATALOG.md
- [ ] Verify CI/CD pipeline
- [ ] Commit and push

## Related Documentation

- **Test Analysis Source**: `/home/chad/repos/witchcityrope/test-results/comprehensive-test-analysis-2025-10-05.md`
- **Test Catalog**: `/home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md`
- **Testing Standards**: `/home/chad/repos/witchcityrope/docs/standards-processes/testing/`
- **Pre-Launch Punch List**: `/home/chad/repos/witchcityrope/docs/standards-processes/PRE_LAUNCH_PUNCH_LIST.md`

## Notes

- **Parallel Work Possible**: Phase 1 (test-developer) can run parallel to Phase 2 Task 1 (backend-developer)
- **Dependencies**: Phase 2 Task 2 (React fixes) should wait for Phase 2 Task 1 (vetting backend) if working on vetting UI
- **Commit Strategy**: Commit after EACH phase completion for easy rollback
- **Test Execution**: Always run full test suite after each phase, not just affected tests
