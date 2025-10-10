# Test Recovery Plan - October 9, 2025
## Emergency Plan to Address 218 Failing Tests (36.3% Failure Rate)

---

## Current State - UNACCEPTABLE for TDD Organization

**Total Tests**: 601
**Passing**: 383 (63.7%)
**Failing**: 218 (36.3%) ❌
**Status**: 🔴 **CRITICAL - IMMEDIATE ACTION REQUIRED**

**Core Problem**: We cannot distinguish between:
1. Tests written incorrectly (false failures)
2. Actual bugs in the application (real failures)

This ambiguity makes TDD impossible.

---

## Guiding Principles for Recovery

### 1. **Test Quality First**
- Failing tests must ALWAYS indicate real bugs
- If a test is wrong, fix the test immediately
- Never tolerate "known failing tests"

### 2. **Systematic Approach**
- Fix one category at a time
- Verify each fix with full test run
- Document decisions (test bug vs. app bug)

### 3. **Zero Tolerance for Manual Login**
- ALL E2E tests MUST use AuthHelpers
- No exceptions - standardization is critical
- This eliminates an entire class of test flakiness

### 4. **Mandatory Verification**
- Every fix must be verified with actual test run
- No claiming success without proof
- Update this plan with actual results

---

## Phase 0: Immediate Stabilization (Day 1 - 4 hours)

### Goal: Stop the bleeding - fix the worst offenders

### Task 0.1: Complete AuthHelpers Migration (ALL E2E Tests)
**Priority**: 🔴 CRITICAL
**Estimated Time**: 2 hours
**Agent**: test-developer

**Action**:
1. Find ALL E2E tests with manual login pattern:
   ```bash
   grep -r "emailInput.fill\|passwordInput.fill" apps/web/tests/playwright --include="*.spec.ts"
   ```

2. Convert EVERY match to AuthHelpers pattern:
   ```typescript
   // Replace this everywhere:
   await page.goto('http://localhost:5173/login');
   await emailInput.fill(...);
   await passwordInput.fill(...);

   // With this:
   await AuthHelpers.loginAs(page, 'role');
   ```

3. Run E2E suite and verify NO test is stuck on login

**Success Criteria**:
- ✅ Zero grep matches for manual login pattern
- ✅ All E2E tests proceed past login
- ✅ E2E pass rate improves or stays same (verify no regression)

**Deliverable**:
- List of all files converted (expect ~36 files based on migration report)
- E2E test results showing improved stability

---

### Task 0.2: Find the 2 Lost E2E Tests
**Priority**: 🔴 CRITICAL
**Estimated Time**: 1 hour
**Agent**: test-developer

**Action**:
1. Compare baseline E2E results (207 passing) with current (205 passing)
2. Identify which 2 tests regressed from passing → failing
3. Determine root cause:
   - Did we break them with our changes?
   - Were they false positives before?
4. Fix or skip with clear documentation

**Success Criteria**:
- ✅ 2 regressed tests identified
- ✅ Root cause documented
- ✅ Tests either fixed or skipped with reason

**Deliverable**:
- Report identifying the 2 tests
- Fix applied or skip justification

---

### Task 0.3: Fix ProfilePage Tests (14 failing)
**Priority**: 🔴 CRITICAL
**Estimated Time**: 1 hour
**Agent**: react-developer

**Action**:
1. Run ProfilePage tests in isolation:
   ```bash
   npm test -- ProfilePage.test.tsx --reporter=verbose
   ```

2. For EACH of 14 failures, determine:
   - **Test Bug**: Test expects wrong behavior → Fix test
   - **App Bug**: App actually broken → Fix app
   - **Unknown**: Can't tell → Mark for manual testing

3. Fix test bugs immediately
4. Document app bugs for separate fix
5. Run tests again to verify

**Success Criteria**:
- ✅ All 14 failures categorized (test bug vs. app bug)
- ✅ All test bugs fixed
- ✅ All app bugs documented with reproduction steps
- ✅ ProfilePage pass rate > 80%

**Deliverable**:
- Categorization table (test vs. app bugs)
- Fixed tests (expect at least 10/14 to be test issues)
- Bug tickets for real app bugs

---

## Phase 1: Unit Test Recovery (Day 2-3 - 8 hours)

### Goal: Get unit tests to 90%+ pass rate

### Task 1.1: Dashboard Tests Recovery (24 failing)
**Priority**: 🔴 HIGH
**Estimated Time**: 3 hours
**Agent**: react-developer

**Failing Test Files**:
1. ProfilePage.test.tsx - 14 failing (from Phase 0)
2. SecurityPage.test.tsx - 6 failing
3. Dashboard integration - 7 failing

**Action**:
1. **SecurityPage (6 failures)**:
   - Run: `npm test -- SecurityPage.test.tsx --reporter=verbose`
   - For each failure: Test bug or app bug?
   - Fix immediately
   - Target: 20/22 passing (91%)

2. **Dashboard Integration (7 failures)**:
   - Run: `npm test -- dashboard-integration.test.tsx --reporter=verbose`
   - Check if MSW handlers are correct
   - Check if API expectations match current endpoints
   - Target: 12/14 passing (86%)

**Success Criteria**:
- ✅ Dashboard category: 36/60 → 54/60 passing (90%)
- ✅ All test bugs vs. app bugs documented
- ✅ Full unit test suite run shows improvement

**Deliverable**:
- Dashboard tests at 90%+ pass rate
- Bug report for any real application issues

---

### Task 1.2: Events Tests Recovery (9 failing)
**Priority**: 🟡 MEDIUM
**Estimated Time**: 2 hours
**Agent**: react-developer

**Failing Test Files**:
1. EventsList.test.tsx - 4 failing
2. EventsPage.test.tsx - 4 failing
3. Other events tests - 1 failing

**Action**:
1. Run all events tests:
   ```bash
   npm test -- tests/__tests__/features/events --reporter=verbose
   ```

2. For each failure:
   - Check if MSW handlers match current API
   - Check if component structure changed
   - Check if selectors are correct
   - Categorize: test bug vs. app bug

3. Fix test bugs immediately
4. Document app bugs

**Success Criteria**:
- ✅ Events category: 17/26 → 23/26 passing (88%)
- ✅ All failures categorized
- ✅ MSW handlers verified correct

**Deliverable**:
- Events tests at 88%+ pass rate
- Updated MSW handlers if needed

---

### Task 1.3: Integration Tests Recovery (10 failing)
**Priority**: 🟡 MEDIUM
**Estimated Time**: 3 hours
**Agent**: test-developer

**Failing Tests**: 10/30 integration tests (33% failing)

**Action**:
1. Run integration tests:
   ```bash
   npm test -- integration.test.tsx --reporter=verbose
   ```

2. Common integration test issues:
   - **Timing**: Add proper `waitFor()` calls
   - **State**: Tests interfering with each other
   - **API mocks**: MSW handlers not matching real API
   - **Authentication**: Using manual login instead of helpers

3. For each failure:
   - Identify root cause
   - Fix if test issue
   - Document if app issue

**Success Criteria**:
- ✅ Integration: 20/30 → 27/30 passing (90%)
- ✅ All timing issues resolved
- ✅ Tests properly isolated

**Deliverable**:
- Integration tests at 90%+ pass rate
- Best practices guide for integration tests

---

## Phase 1.5: API Unit Test Recovery (Week 2-5 - 80 hours) 🔴 CRITICAL

### Goal: Establish minimum viable API test coverage

**SHOCKING DISCOVERY**: API has only **39 tests** vs **217 needed** for minimum coverage (82% gap!)

### Current State - UNACCEPTABLE
```
Current Tests:     39 total (13% of endpoints tested)
Needed for MVC:   217 tests minimum
Coverage Gap:     178 tests MISSING
Time Estimate:    80 hours (4-6 weeks with dedicated focus)
```

### Critical Untested Areas
```
❌ Vetting:          0 tests (15 endpoints, core business)
❌ Participation:    0 tests (7 endpoints, user-facing)
❌ Dashboard:        0 tests (5 endpoints, user-facing)
❌ User Management:  0 tests (7 endpoints, core)
❌ Check-In:         0 tests (6 endpoints, attendance)
❌ Safety/Audit:     0 tests (5 endpoints, compliance risk)
⚠️  Payments:        8 tests (MOCK ONLY - real service untested!)
⚠️  Events:          12 tests (basic CRUD, no workflows)
✅ Authentication:  12 tests (GOOD)
✅ Health:           7 tests (adequate)
```

### Task 1.5.1: Critical Payment Tests (Week 2)
**Priority**: 🔴 **HIGHEST - FINANCIAL RISK**
**Estimated Time**: 20 hours
**Agent**: backend-developer
**Tests Needed**: 30 tests

**Action**:
1. Test real `PaymentService.cs` (not mock):
   - Payment processing workflows
   - Refund processing
   - PayPal integration
   - Error handling (declined cards, timeouts)
   - Idempotency (duplicate transactions)

2. Create test files:
   - `PaymentServiceTests.cs` (15 tests)
   - `RefundServiceTests.cs` (10 tests)
   - `PaymentWorkflowIntegrationTests.cs` (5 tests)

3. Use real PayPal sandbox for integration tests

**Success Criteria**:
- ✅ All payment paths tested
- ✅ Error scenarios covered
- ✅ Financial calculations verified
- ✅ Zero untested payment code paths

**Deliverable**:
- 30 payment tests passing
- Payment service confidence: HIGH

---

### Task 1.5.2: Vetting Workflow Tests (Week 3)
**Priority**: 🔴 **CRITICAL - CORE BUSINESS**
**Estimated Time**: 25 hours
**Agent**: backend-developer
**Tests Needed**: 40 tests

**Action**:
1. Test vetting application workflow:
   - Submit application (validation, storage)
   - Review workflow (assignment, status updates)
   - Approve/deny logic
   - Email notifications
   - Vetting status transitions

2. Create test files:
   - `VettingApplicationServiceTests.cs` (15 tests)
   - `VettingReviewServiceTests.cs` (15 tests)
   - `VettingWorkflowIntegrationTests.cs` (10 tests)

3. Test all 15 vetting endpoints

**Success Criteria**:
- ✅ All vetting workflows tested
- ✅ Status transitions validated
- ✅ Email notifications verified
- ✅ Business logic correctness proven

**Deliverable**:
- 40 vetting tests passing
- Vetting workflow confidence: HIGH

---

### Task 1.5.3: Participation & Events Tests (Week 4)
**Priority**: 🟡 **HIGH - USER-FACING**
**Estimated Time**: 20 hours
**Agent**: backend-developer
**Tests Needed**: 40 tests

**Action**:
1. Test participation workflows:
   - Event registration (RSVP, tickets)
   - Waitlist management
   - Attendance tracking
   - Cancellation workflows

2. Test event workflows:
   - Event creation (validation, sessions, tickets)
   - Event publishing workflow
   - Session management
   - Capacity management

3. Create test files:
   - `ParticipationServiceTests.cs` (15 tests)
   - `EventWorkflowServiceTests.cs` (15 tests)
   - `ParticipationIntegrationTests.cs` (10 tests)

**Success Criteria**:
- ✅ Registration workflows tested
- ✅ Cancellation logic validated
- ✅ Capacity management verified
- ✅ Event lifecycle tested

**Deliverable**:
- 40 participation/events tests passing
- User-facing workflows confidence: HIGH

---

### Task 1.5.4: Secondary Features Tests (Week 5)
**Priority**: 🟢 **MEDIUM**
**Estimated Time**: 15 hours
**Agent**: backend-developer
**Tests Needed**: 40 tests

**Action**:
1. Test remaining features:
   - Check-In system (15 tests)
   - User Management (15 tests)
   - Dashboard endpoints (10 tests)

2. Create test files:
   - `CheckInServiceTests.cs`
   - `UserManagementServiceTests.cs`
   - `DashboardServiceTests.cs`

**Success Criteria**:
- ✅ All secondary features tested
- ✅ Edge cases covered
- ✅ Error handling validated

**Deliverable**:
- 40 secondary feature tests passing
- Complete API coverage: 85%+

---

### API Test Standards & Templates
**Priority**: 🔴 **CRITICAL**
**Estimated Time**: 5 hours
**Agent**: backend-developer

**Action**:
1. Create test templates:
   - Service test template
   - Integration test template
   - Error handling test patterns

2. Document standards:
   - Test naming conventions
   - Arrange-Act-Assert pattern
   - Mock vs. real dependencies
   - Database test isolation

3. Create test helpers:
   - Test data builders
   - Database cleanup utilities
   - Mock factories

**Deliverable**:
- Test templates at `/tests/WitchCityRope.Api.Tests/Templates/`
- Standards doc at `/docs/standards-processes/testing/api-test-standards.md`

---

## Phase 2: E2E Test Recovery (Week 1 Days 4-5 - 10 hours)

### Goal: Get E2E tests to 85%+ pass rate

### Task 2.1: Admin Events Tests (Full Triage)
**Priority**: 🟡 MEDIUM
**Estimated Time**: 3 hours
**Agent**: test-developer

**Current Status**: Admin tests pass (from Priority 4)
**Additional Work**: Verify all admin workflows actually work

**Action**:
1. Run all admin E2E tests:
   ```bash
   npx playwright test admin --reporter=list
   ```

2. For each test:
   - Run manually to verify UI actually works
   - If test passes but UI broken → App bug
   - If test fails but UI works → Test bug
   - If both broken → Priority app bug

3. Test critical admin workflows manually:
   - Create event
   - Edit event
   - Delete event
   - View event list

**Success Criteria**:
- ✅ All admin E2E tests verified against real UI
- ✅ Any test/UI mismatches documented
- ✅ Critical admin workflows confirmed working

**Deliverable**:
- Admin workflow verification report
- Bug tickets for any broken workflows

---

### Task 2.2: Profile & Dashboard E2E Tests
**Priority**: 🟡 MEDIUM
**Estimated Time**: 3 hours
**Agent**: test-developer

**Action**:
1. Run all profile/dashboard E2E tests:
   ```bash
   npx playwright test profile dashboard --reporter=list
   ```

2. Verify profile test migrations completed:
   - Are tests using unique users?
   - Are cleanup functions working?
   - Are race conditions eliminated?

3. For each failure:
   - Test bug vs. app bug
   - Fix or document

**Success Criteria**:
- ✅ Profile tests using unique users (no race conditions)
- ✅ Dashboard E2E tests at 85%+ pass rate
- ✅ All failures categorized

**Deliverable**:
- Profile E2E tests stable and reliable
- Dashboard E2E at 85%+ pass rate

---

### Task 2.3: Vetting Workflow E2E Tests
**Priority**: 🟢 LOW (already at 95.5%)
**Estimated Time**: 1 hour
**Agent**: test-developer

**Current Status**: Vetting tests are EXCELLENT (63/66 passing)

**Action**:
1. Fix the 3 remaining failures
2. Get to 100% if possible
3. Document these as the gold standard

**Success Criteria**:
- ✅ Vetting: 63/66 → 66/66 (100%)
- ✅ Documented as best practice example

**Deliverable**:
- Vetting tests at 100%
- Best practices guide based on vetting test patterns

---

### Task 2.4: Events E2E Tests (Full Triage)
**Priority**: 🟡 MEDIUM
**Estimated Time**: 3 hours
**Agent**: test-developer

**Action**:
1. Run all events E2E tests:
   ```bash
   npx playwright test events --reporter=list
   ```

2. Test critical event workflows manually:
   - Browse events (public)
   - View event details
   - RSVP to event
   - Cancel RSVP
   - Purchase ticket
   - Cancel ticket

3. For each E2E test:
   - Verify against manual testing
   - Test bug vs. app bug
   - Fix or document

**Success Criteria**:
- ✅ All event E2E tests verified against real UI
- ✅ Critical workflows confirmed working
- ✅ Events E2E at 85%+ pass rate

**Deliverable**:
- Events workflow verification report
- Events E2E at 85%+ pass rate

---

## Phase 3: Quality Gates & Prevention (Day 6 - 4 hours)

### Goal: Prevent regression and enforce standards

### Task 3.1: Implement Pre-commit Test Gates
**Priority**: 🔴 CRITICAL
**Estimated Time**: 2 hours
**Agent**: test-developer

**Action**:
1. Update `.husky/pre-commit` to run affected tests:
   ```bash
   # Run unit tests for changed files
   npm test -- --findRelatedTests --passWithNoTests
   ```

2. Block commits if:
   - Any test fails
   - Manual login pattern detected
   - Test file has no assertions

3. Add pre-push hook:
   ```bash
   # Run full test suite
   npm test && npm run test:e2e
   ```

**Success Criteria**:
- ✅ Pre-commit hook blocks bad code
- ✅ Pre-push hook runs full suite
- ✅ Manual login pattern detection works

**Deliverable**:
- Pre-commit hooks configured
- Developer guide for working with hooks

---

### Task 3.2: Create Test Quality Dashboard
**Priority**: 🟡 MEDIUM
**Estimated Time**: 2 hours
**Agent**: test-developer

**Action**:
1. Create script to generate test quality report:
   ```bash
   #!/bin/bash
   # test-quality-report.sh

   echo "Running all tests..."
   npm test -- --json --outputFile=unit-results.json
   npm run test:e2e -- --reporter=json > e2e-results.json

   echo "Generating quality report..."
   node scripts/generate-test-report.js
   ```

2. Report should show:
   - Pass rate by category
   - Trend over time (improving or declining)
   - Top 10 flakiest tests
   - Tests with no assertions
   - Tests using deprecated patterns

3. Run weekly and commit to repo

**Success Criteria**:
- ✅ Automated report generation works
- ✅ Report identifies quality issues
- ✅ Scheduled to run weekly

**Deliverable**:
- Test quality dashboard script
- First quality report baseline

---

## Phase 4: Documentation & Standards (Day 7 - 2 hours)

### Task 4.1: Update Test Standards Documentation
**Priority**: 🟡 MEDIUM
**Estimated Time**: 1 hour
**Agent**: librarian

**Action**:
1. Update `/docs/standards-processes/testing/` with:
   - Mandatory AuthHelpers usage
   - Unique user pattern for persistence tests
   - MSW pattern for API mocking
   - Test isolation requirements

2. Create quick reference guide:
   - Common test patterns
   - How to determine test bug vs. app bug
   - When to skip vs. fix a test

**Success Criteria**:
- ✅ All test standards documented
- ✅ Quick reference guide created
- ✅ Examples for each pattern

**Deliverable**:
- Updated test standards
- Quick reference guide

---

### Task 4.2: Update Lessons Learned
**Priority**: 🟡 MEDIUM
**Estimated Time**: 1 hour
**Agent**: librarian

**Action**:
1. Document this recovery process in lessons learned
2. Key lessons:
   - Never claim fixes without verification
   - Always run full suite after changes
   - Manual login is a critical anti-pattern
   - Test quality as important as code quality

2. Update agent lessons learned files:
   - react-developer: Form validation debugging process
   - test-developer: Test vs. app bug determination
   - test-executor: Mandatory verification process

**Success Criteria**:
- ✅ Recovery process documented
- ✅ Key lessons captured
- ✅ Agent lessons learned updated

**Deliverable**:
- Updated lessons learned files
- Post-mortem report

---

## Success Metrics

### Target State (End of Recovery Plan)

| Metric | Current | Target | Must-Have |
|--------|---------|--------|-----------|
| **Frontend Unit Tests** | 178/262 (67.9%) | 235/262 (90%+) | YES ✅ |
| **API Unit Tests** | 39 tests (13%) | 217 tests (85%+) | YES ✅ |
| **E2E Tests** | 205/339 (60.5%) | 288/339 (85%+) | YES ✅ |
| **Overall Pass Rate** | 383/601 (63.7%) | 740/818 (90%+) | YES ✅ |
| **Manual Login** | ✅ 0 files | 0 files | YES ✅ |
| **API Coverage** | 13% endpoints | 85%+ endpoints | YES ✅ |
| **Payment Tests** | 8 (mock only) | 30 (real service) | YES ✅ |
| **Vetting Tests** | 0 tests | 40 tests | YES ✅ |
| **Pre-commit Hooks** | Partial | Enforcing | YES ✅ |
| **Test/App Bug Log** | None | Complete | YES ✅ |

### Updated Test Counts
```
Current Total: 601 tests (383 passing, 218 failing)
After Frontend: ~523 tests (87%+ passing)
After API Tests: ~818 tests (90%+ passing)

Breakdown:
- Frontend Unit: 262 tests → 235 passing (90%)
- API Unit: 39 → 217 tests (85%+)
- E2E: 339 tests → 288 passing (85%)
- Integration: 178 tests (included in counts)
```

### Weekly Monitoring

After recovery, track weekly:
- Overall pass rate (must stay > 85%)
- New failures this week
- Tests skipped this week
- Flaky tests (pass/fail inconsistently)

**Rule**: Pass rate drops below 85% → STOP all feature work, fix tests

---

## Risk Mitigation

### Risk 1: "Tests are wrong, app is fine"
**Mitigation**:
- Every failing test requires manual verification
- If test wrong → Fix immediately
- If app wrong → Create bug ticket
- Document decision in test file

### Risk 2: "Fixing tests takes too long"
**Mitigation**:
- This IS the highest priority
- TDD organization cannot function with 36% failure rate
- All feature work STOPS until tests at 85%+

### Risk 3: "We break tests while fixing them"
**Mitigation**:
- Run full suite after EVERY change
- Use test-executor agent (mandatory)
- Never claim success without proof

### Risk 4: "Developers bypass pre-commit hooks"
**Mitigation**:
- CI/CD also runs tests
- Block merge if tests fail
- Code review checks test quality

---

## Timeline & Resource Allocation

### Week 1 (Days 1-3): Emergency Frontend Stabilization
- **Day 1**: Phase 0 (4 hours)
  - ✅ COMPLETE: AuthHelpers migration (all E2E tests)
  - Phase 0.2: Find 2 lost E2E tests
  - Phase 0.3: Fix ProfilePage tests

- **Day 2**: Phase 1.1 (3 hours) - Dashboard tests recovery
- **Day 3**: Phase 1.2-1.3 (5 hours) - Events & Integration tests

**Checkpoint**: Must achieve 80%+ frontend test pass rate by end of Day 3

### Week 1 (Days 4-5): E2E Recovery
- **Day 4**: Phase 2.1-2.2 (6 hours) - Admin & Profile E2E tests
- **Day 5**: Phase 2.3-2.4 (4 hours) - Vetting & Events E2E tests

**Checkpoint**: Must achieve 85%+ E2E pass rate by end of Day 5

### Week 1 (Day 6): Quality Gates Setup
- **Day 6**: Phase 3 (4 hours) - Pre-commit hooks & quality dashboard

**Checkpoint**: Quality gates enforced, ready for API test phase

---

### Week 2: Critical API Tests (Payment & Vetting) 🔴
**BLOCKING**: No new features until payment/vetting tests complete

- **Week 2** (20 hours): Phase 1.5.1 - Payment Tests
  - 30 tests for payment processing
  - Financial risk mitigation
  - **HIGHEST PRIORITY**

**Checkpoint**: Payment processing fully tested

### Week 3: Core Business API Tests 🔴
- **Week 3** (25 hours): Phase 1.5.2 - Vetting Workflow Tests
  - 40 tests for vetting workflows
  - Core business logic validation
  - **CRITICAL PRIORITY**

**Checkpoint**: Vetting workflow fully tested

### Week 4: User-Facing API Tests 🟡
- **Week 4** (20 hours): Phase 1.5.3 - Participation & Events Tests
  - 40 tests for user-facing workflows
  - Registration and event management
  - **HIGH PRIORITY**

**Checkpoint**: User-facing workflows fully tested

### Week 5: Secondary Features & Standards 🟢
- **Week 5 Days 1-3** (15 hours): Phase 1.5.4 - Secondary Features
  - Check-In, User Management, Dashboard
  - **MEDIUM PRIORITY**

- **Week 5 Day 4** (5 hours): API Test Standards & Templates
  - Test templates and documentation
  - Standards enforcement

- **Week 5 Day 5** (2 hours): Phase 4 - Final Documentation
  - Update all documentation
  - Post-mortem and lessons learned

**Final Checkpoint**:
- Frontend tests: 87%+ pass rate ✅
- API tests: 217+ tests, 85%+ coverage ✅
- Quality gates: Enforced ✅
- TDD compliance: ACHIEVED ✅

---

## Updated Timeline Summary

```
Week 1:  Frontend test recovery (25 hours)
Week 2:  Payment API tests (20 hours)        🔴 CRITICAL
Week 3:  Vetting API tests (25 hours)        🔴 CRITICAL
Week 4:  Participation API tests (20 hours)  🟡 HIGH
Week 5:  Secondary + Docs (22 hours)         🟢 MEDIUM

Total: 112 hours over 5 weeks (vs original 25-30 hours)
```

**Reality Check**: Original plan missed 82% of necessary work (API tests)

---

## Agent Assignment & Parallel Work

### Parallel Track 1: Unit Tests (react-developer)
- Phase 0.3: ProfilePage
- Phase 1.1: Dashboard
- Phase 1.2: Events

### Parallel Track 2: E2E Tests (test-developer)
- Phase 0.1: AuthHelpers migration
- Phase 0.2: Find lost tests
- Phase 1.3: Integration tests
- Phase 2.1-2.4: All E2E recovery

### Sequential: Quality & Docs
- Phase 3: test-developer (depends on tests stable)
- Phase 4: librarian (depends on Phase 3)

**Can run Track 1 & 2 in parallel** to save time

---

## Communication Plan

### Daily Standup (During Recovery)
1. What tests were fixed yesterday?
2. What's the current pass rate?
3. Any blockers?
4. What tests are we fixing today?

### Weekly Report (After Recovery)
1. Current pass rate
2. Tests fixed this week
3. Tests regressed this week
4. Action items

### Escalation Path
- Pass rate drops below 80% → Notify immediately
- Critical workflow broken → Stop all work
- Can't determine test vs. app bug → Manual testing session

---

## Commit Strategy

### DO NOT commit until:
- ✅ All tests in that category passing (or skipped with reason)
- ✅ Full test suite run shows no regression
- ✅ Test-executor verification complete

### Commit Message Format:
```
test(recovery): [Category] - Achieve X% pass rate (was Y%)

- Fixed Z test bugs (list)
- Fixed N app bugs (list)
- Skipped M tests (list with reasons)

Verified with full test suite run:
- Unit: A/B passing (X%)
- E2E: C/D passing (Y%)
- Overall: E/F passing (Z%)

[Link to test-executor report]
```

---

## Test Bug vs. App Bug Decision Tree

```
Is the test failing?
├─ YES
│  ├─ Run the workflow manually
│  │  ├─ Does the UI work correctly?
│  │  │  ├─ YES → TEST BUG (fix test)
│  │  │  └─ NO → APP BUG (create ticket)
│  │  └─ Can't manually test? → REVIEW with team
│  └─ Document decision in test file
└─ NO → Continue
```

**Example**:
```typescript
// ProfilePage.test.tsx
test('should display user email', async () => {
  // BUG FIX 2025-10-09: Test was expecting wrong selector
  // Manual verification: Email DOES display at [data-testid="user-email"]
  // Previous selector was incorrect, not an app bug

  expect(screen.getByTestId('user-email')).toHaveText('user@example.com');
});
```

---

## Accountability

### This Recovery Plan Is:
- ✅ Mandatory - Not optional
- ✅ Blocking - No features until complete
- ✅ Verified - Every claim requires proof
- ✅ Tracked - Daily progress reports

### Success Criteria:
- ✅ 87%+ overall pass rate
- ✅ Zero manual login in E2E tests
- ✅ Pre-commit hooks enforcing quality
- ✅ All test/app bugs documented

**No excuses. This is TDD. Tests are the foundation.**

---

## Next Steps (Immediate)

1. **Review this plan** - Get approval
2. **Start Phase 0** - AuthHelpers migration (2 hours)
3. **First checkpoint** - Verify AuthHelpers complete
4. **Continue Phase 0** - Fix ProfilePage and find lost tests
5. **Daily updates** - Progress and blockers

---

**Created**: October 9, 2025
**Status**: 🔴 PENDING APPROVAL
**Estimated Total Time**: 25-30 hours over 2 weeks
**Critical Success Factor**: NO SHORTCUTS - Verify everything

Let's fix this properly.
