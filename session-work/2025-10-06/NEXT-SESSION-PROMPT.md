# Next Session Startup Prompt - Testing Completion Track

Copy and paste this into Claude Code to continue:

---

We're continuing the testing completion track from the 2025-10-06 session. Here's where we left off:

## Session Context (2025-10-06)

**Major Accomplishments**:
- ✅ Testing completion plan created (4-phase strategy)
- ✅ Pre-launch punch list created (34 items, 38% complete)
- ✅ Phase 1 complete (baseline + quick wins)
- ✅ Phase 2 vetting backend complete (15/15 tests passing, 100%)
- ✅ 2 commits made (clean checkpoints)

**Current Test Status**:
- Integration: 94% (29/31) ✅ Target met
- React Unit: 56% (155/277) - Need >90%
- E2E: Variable - Need attention

## Your Task: Continue Phase 2

**Phase 2 Task 2: Fix React Dashboard Error Handling** (estimated 1-2 days)

**Problem**: 40-50 React unit tests failing due to broken error handling
- Network timeout handling in hooks (useCurrentUser, useEvents)
- Login/logout error state management
- Malformed API response validation
- Query caching behavior

**Target**: Achieve >80% React unit pass rate (220+ of 277 tests)

## Critical Documents (MUST READ)

### 1. Testing Completion Plan (Phase 2 Strategy)
**Location**: `/home/chad/repos/witchcityrope/session-work/2025-10-06/testing-completion-plan.md`

Read the **Phase 2: Critical Path** section for:
- Task 2 details (React dashboard error handling)
- Success criteria (>80% pass rate)
- Effort estimate (1-2 days)
- Next steps after completion

### 2. Baseline Test Results (Current Status)
**Location**: `/home/chad/repos/witchcityrope/session-work/2025-10-06/baseline-test-results-2025-10-06.md`

This shows:
- Current 56% React unit pass rate (155/277 tests)
- Categorization of 100 failing tests
- Specific error patterns to fix

### 3. Session Summary (Complete Context)
**Location**: `/home/chad/repos/witchcityrope/session-work/2025-10-06/SESSION-SUMMARY-TESTING-COMPLETION.md`

Complete session 2025-10-06 accomplishments and context.

### 4. Pre-Launch Punch List (Launch Timeline)
**Location**: `/home/chad/repos/witchcityrope/docs/standards-processes/PRE_LAUNCH_PUNCH_LIST.md`

Shows:
- 34 items tracked
- Launch in 25-33 days
- Current 38% completion

## CRITICAL: Stakeholder Notes (READ THIS!)

### Authentication Testing Issues (Repeated Problem)
**IMPORTANT**: We keep having authentication issues in tests, but **we HAVE working authentication tests as examples**.

**MANDATORY APPROACH**:
1. **ALWAYS examine working tests FIRST** before fixing broken ones
2. **Copy the pattern** from working tests to broken tests
3. **DO NOT reinvent** authentication test patterns
4. **Use existing examples** as reference

**Stakeholder Emphasis**: "USE WORKING TESTS AS REFERENCE" (this has been repeated multiple times)

### Commit Strategy
**Stakeholder Preference**:
- Commit **OFTEN** for backup points
- Stakeholder wants to be able to **manually verify changes**
- Clean checkpoints after each major task
- **DO NOT** accumulate large uncommitted changes

## Recommended Approach

### Option 1: Use Orchestrator Workflow (Recommended)

```bash
/orchestrate Continue Phase 2 testing completion - Fix React dashboard error handling (40-50 failing tests). Follow the testing completion plan at /session-work/2025-10-06/testing-completion-plan.md. Use react-developer agent for fixes. Target >80% React unit pass rate.
```

### Option 2: Manual Approach (More Control)

#### Step 1: Read Testing Plan
```bash
# Read Phase 2 section
cat /home/chad/repos/witchcityrope/session-work/2025-10-06/testing-completion-plan.md
```

#### Step 2: Run React Unit Tests
```bash
cd /home/chad/repos/witchcityrope/apps/web
npm run test 2>&1 | tee /tmp/react-dashboard-errors-$(date +%Y%m%d).log
```

#### Step 3: Examine Working Tests (CRITICAL!)
```bash
# Find working authentication tests
find /home/chad/repos/witchcityrope/apps/web/src -name "*.test.tsx" -o -name "*.test.ts"

# Look for tests with high pass rates
grep -r "useCurrentUser" /home/chad/repos/witchcityrope/apps/web/src/**/*.test.tsx
```

#### Step 4: Fix Dashboard Error Handling

**Focus Areas** (from baseline analysis):
1. **Network timeout handling** in hooks
2. **Login/logout error states**
3. **Malformed API response** validation
4. **Query caching** behavior

**Use react-developer agent**:
- Read working test patterns
- Apply same patterns to broken tests
- Fix error handling in hooks
- Update test assertions

#### Step 5: Verify Improvements
```bash
# Run tests again
cd /home/chad/repos/witchcityrope/apps/web
npm run test

# Check pass rate (target >80%)
# 220+ of 277 tests should pass
```

#### Step 6: Commit Phase 2 Task 2
```bash
cd /home/chad/repos/witchcityrope
git add .
git commit -m "test: Phase 2 dashboard error handling - >80% React unit pass rate

PROBLEM:
- 40-50 React unit tests failing due to broken error handling
- Network timeout handling broken in useCurrentUser/useEvents hooks
- Login/logout error state management not working

ROOT CAUSE:
[Describe what you found]

SOLUTION:
- Fixed network timeout handling in dashboard hooks
- Updated error state management for login/logout
- Improved malformed API response validation
- Fixed query caching behavior

VERIFICATION:
- React unit tests: 56% → [NEW %] (+[X]% improvement)
- [X] of 277 tests now passing
- No regressions in currently passing tests
- Used working auth tests as reference

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

## Success Criteria

### Phase 2 Task 2 Complete When:
- ✅ React unit tests >80% pass rate (220+ of 277)
- ✅ Dashboard hooks handle network timeouts properly
- ✅ Login/logout error states working
- ✅ No regressions in currently passing tests
- ✅ Handoff document created
- ✅ Committed with comprehensive message

### Phase 2 Complete When:
- ✅ React unit >80% pass rate
- ✅ Integration >90% pass rate (already achieved at 94%)
- ✅ All Phase 2 work committed

## After Phase 2 Dashboard Complete

### Create Phase 2 Completion Report
```bash
# Document Phase 2 final results
# Location: /session-work/2025-10-06/phase2-completion-report.md
# Include:
# - React unit pass rate improvement
# - Integration test status (maintained 94%)
# - All fixes implemented
# - Handoff documents created
# - Commits made
# - Next steps (Phase 3)
```

### Decide: Continue to Phase 3?

**If Time Allows**: Phase 3 (Events Feature Stabilization)
- **Focus**: Address 15+ events-related test failures
- **Estimated**: 2-3 days
- **Target**: >80% events test pass rate

**If Time Limited**: Wrap Up
- Create comprehensive session summary
- Update file registry
- Document next session priorities

## Additional Context

### Git Status (Session End 2025-10-06)
- **Branch**: main
- **Ahead of origin**: 11 commits (not pushed yet - includes earlier vetting UI work)
- **Last commit**: Phase 2 vetting backend (testing work)

### Environment Requirements
- **Docker**: Must be running (`./dev.sh`)
- **Test endpoints**: web:5173, api:5655, db:5433
- **React tests**: Run from `/apps/web` directory
- **Integration tests**: Run from `/tests/integration/api` directory

### Test Command Reference
```bash
# React unit tests
cd /home/chad/repos/witchcityrope/apps/web
npm run test

# Integration tests
cd /home/chad/repos/witchcityrope/tests/integration/api
dotnet test

# E2E tests (Playwright)
cd /home/chad/repos/witchcityrope/apps/web
npm run test:e2e:playwright
```

### Key File Locations

**Testing Plan**:
- `/home/chad/repos/witchcityrope/session-work/2025-10-06/testing-completion-plan.md`

**Baseline Results**:
- `/home/chad/repos/witchcityrope/session-work/2025-10-06/baseline-test-results-2025-10-06.md`

**Phase 1 Report**:
- `/home/chad/repos/witchcityrope/session-work/2025-10-06/phase1-completion-report.md`

**Punch List**:
- `/home/chad/repos/witchcityrope/docs/standards-processes/PRE_LAUNCH_PUNCH_LIST.md`

**Handoff Documents**:
- `/home/chad/repos/witchcityrope/docs/functional-areas/vetting-system/handoffs/`

## Important Reminders

### 🚨 USE WORKING TESTS AS REFERENCE
This has been emphasized multiple times:
- We HAVE working authentication tests
- ALWAYS examine them FIRST
- Copy their patterns
- Don't reinvent authentication testing

### ✅ Commit Strategy
- Commit after each major fix
- Small, focused commits
- Comprehensive commit messages
- Allow stakeholder manual verification

### 📊 Pass Rate Targets
- **Current**: 56% (155/277)
- **Phase 2 Target**: >80% (220/277)
- **Final Target**: >90% (249/277)

### 🎯 Focus Areas (Priority Order)
1. Network timeout handling (highest impact)
2. Login/logout error states (user-facing)
3. API response validation (data integrity)
4. Query caching behavior (performance)

## Expected Outcomes

### After Completing Phase 2 Task 2:
- React unit tests >80% pass rate
- Dashboard error handling robust
- No authentication test failures
- Clean commit with comprehensive message
- Handoff document created

### After Completing Full Phase 2:
- Integration tests at 94% (maintained)
- React unit tests >80%
- All critical path tests passing
- Clear path to Phase 3 or production

## Need Help?

### If Tests Are Still Failing:
1. Check working test examples again
2. Verify Docker containers running
3. Check API endpoints responding
4. Review baseline analysis for patterns
5. Create detailed investigation report

### If Stuck on Specific Pattern:
1. Search codebase for similar working tests
2. Check lessons learned files for patterns
3. Review testing plan for guidance
4. Document the issue for next session

Ready to continue! 🚀

---

**Start by reading**:
1. Testing completion plan (Phase 2 section)
2. Baseline test results (React unit failures)
3. Working authentication test examples

**Then**:
- Fix dashboard error handling
- Target >80% React unit pass rate
- Commit frequently
- Create handoff document

**Remember**: USE WORKING TESTS AS REFERENCE
