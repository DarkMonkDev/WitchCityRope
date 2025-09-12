# Test Cleanup Analysis - September 12, 2025

## Overview
Analysis of tests that are failing due to testing unimplemented features vs. actual bugs.

## Tests for Unimplemented Features (Should be Skipped/Removed)

### 1. Form Design Showcase Tests
**File**: `/tests/e2e/form-designs-check.spec.ts`
**Issue**: Tests routes like `/form-designs/*` that don't exist in React app
**Evidence**: No form-design related files found in React src
**Action**: Skip all tests - these features are not implemented
**Reason**: Routes return 200 but have no content (empty React components)

### 2. Tests with Wrong Routes
**Files**: 
- `/tests/e2e/admin-events-dashboard-final.spec.ts`
- `/tests/e2e/admin-events-dashboard-fixed.spec.ts`
- `/tests/e2e/admin-events-dashboard.spec.ts`

**Issue**: Tests navigate to `/admin/events-table` which doesn't exist
**Correct Route**: Should be `/admin/events`
**Evidence**: React routes show `admin/events`, not `admin/events-table`
**Action**: Fix route in tests
**Note**: The components being tested DO exist (filter chips, events table)

### 3. Tests for Future API Endpoints
**Pattern**: Tests calling API endpoints that return 404 or aren't implemented
**Example**: Tests expecting specific API response structures that don't match actual API
**Action**: Skip until APIs are implemented

## Tests That Need Fixing (Real Bugs)

### 1. Login Selector Issues (Partially Fixed)
**Evidence**: Some tests still use wrong selectors
**Status**: Recent fixes completed for data-testid selectors
**Remaining**: Some files may still have old selectors

### 2. Admin Events Dashboard Tests
**Issue**: Wrong route (`/admin/events-table` → `/admin/events`)
**Components**: Filter chips and events table DO exist
**Action**: Fix routes, keep component tests

## Tests That Are Valid But Failing Due to Bugs

### 1. Authentication Flow Tests
**Issue**: Tests are working but may find real authentication bugs
**Example**: RangeError in dashboard after login
**Action**: Keep these - they're finding real issues

### 2. API Integration Tests
**Issue**: May be testing real API integration problems
**Action**: Keep and investigate failures

## Recommended Actions

### Immediate Fixes
1. Fix routes in admin events dashboard tests (`/admin/events-table` → `/admin/events`)
2. Skip form design tests entirely (features not implemented)
3. Verify login selectors are using correct data-testid patterns

### Skip/Remove Tests
1. All form design showcase tests
2. Tests for admin features not yet implemented
3. Tests for API endpoints returning 404

### Keep and Debug
1. Authentication flow tests (they find real bugs)
2. Component interaction tests (when routes are fixed)
3. API integration tests for existing endpoints

## Implementation Plan

### Phase 1: Quick Wins (Fix Wrong Routes/Selectors)
- [ ] Fix `/admin/events-table` → `/admin/events` in admin dashboard tests
- [ ] Verify all login tests use data-testid selectors
- [ ] Remove or skip obvious unimplemented feature tests

### Phase 2: Feature Detection
- [ ] Add feature detection to tests (skip if feature not implemented)
- [ ] Create utility to check if routes exist before testing
- [ ] Add graceful handling for missing components

### Phase 3: Real Bug Investigation
- [ ] Run fixed tests to identify actual bugs
- [ ] Document and report real issues found
- [ ] Keep tests that provide value in finding regressions

## Test Categories Summary

| Category | Count | Action | Priority |
|----------|--------|---------|----------|
| Wrong Routes | 3-4 tests | Fix routes | High |
| Unimplemented Features | 5-10 tests | Skip/Remove | High |
| Valid Bug Detection | 10-15 tests | Keep & Debug | Medium |
| Login Issues | 2-3 tests | Already Fixed | Low |
