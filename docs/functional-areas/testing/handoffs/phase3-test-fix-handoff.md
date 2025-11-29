# Phase 3 Handoff: Test Infrastructure Fix

**Date**: 2025-11-29
**From**: Orchestrator
**To**: test-developer agent
**Status**: IN PROGRESS

## 🚨 CRITICAL: MANDATORY TEST EXECUTION PROCEDURE 🚨

**YOU MUST FOLLOW THESE STEPS EXACTLY:**

### Step 1: Use test-environment skill
Use the `test-environment` skill to build and run tests in isolated test containers.
This ensures test containers have the latest code and are separate from dev environment.

### Step 2: Test ONE file first, verify, then expand
1. Fix ONE test file
2. Use `test-environment` skill to run ONLY that file
3. Verify fix works
4. ONLY THEN apply fix to other files
5. Test again after each batch

**NEVER run tests directly from host - always use test-environment skill!**

**FAILURE TO FOLLOW THIS PROCEDURE = INVALID RESULTS**

---

## Context

Previous work made mistakes:
1. Archived 50 test files (127 tests) that should have been FIXED not hidden
2. Added test.skip() calls throughout the codebase to hide failures
3. Resulted in FEWER passing tests despite appearing to improve

## Current State

After reverting the hiding:
- **174 test files** active (restored all archived)
- **0 test.skip() calls** (removed all)
- **800 tests total**
- **494 passed** (61.8%)
- **227 failed** (28.4%)
- **79 didn't run** (beforeAll failures)

## Root Causes of Failures

From analyzing test output:

### 1. beforeAll/beforeEach Failures (79 tests)
Tests that don't run because setup failed. These cascade - one failure blocks many tests.
Files affected: admin-events-sessions.spec.ts and others

### 2. 401 Unauthorized Errors
Many tests showing "Failed to load resource: 401 Unauthorized" in console
Indicates auth session issues during parallel test execution

### 3. Timeout Errors
Tests waiting for elements that don't appear or take too long

### 4. Selector Issues
Tests using selectors that don't match current UI

## Your Mission

**FIX tests, do not skip or archive them.**

### Priority Order:

1. **Fix beforeAll failures first** - These block the most tests
   - Find all files with beforeAll hooks that fail
   - Fix the setup logic

2. **Fix auth issues** - Use AuthHelpers consistently
   - Ensure all tests use AuthHelpers.loginAs()
   - Add proper wait after login

3. **Fix selector issues** - Update to match current UI
   - Use data-testid attributes where available
   - Update stale text selectors

4. **Fix timeout issues** - Add proper waits
   - Replace waitForTimeout with waitForSelector
   - Use network idle where appropriate

## Constraints

- DO NOT use test.skip()
- DO NOT archive tests
- DO NOT hide failures
- FIX the actual problems
- **ALWAYS use test-environment skill to run tests**
- **NEVER run tests directly from host**

## Expected Outcome

- All 800 tests execute (0 skipped)
- Pass rate > 70% (560+ tests)
- Remaining failures documented as actual bugs

## How to Run Tests

Use the `test-environment` skill for all test execution.
The skill handles container setup and test execution automatically.

Report back with:
1. What you fixed
2. New pass/fail counts (from TEST containers)
3. Any real application bugs discovered
