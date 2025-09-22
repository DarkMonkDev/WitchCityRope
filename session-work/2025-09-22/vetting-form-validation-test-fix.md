# Vetting Form Validation Test Fix

**Date**: 2025-09-22
**Task**: Update E2E test to properly fill all required fields in vetting form
**Status**: ✅ COMPLETED

## Problem
The vetting form validation was working correctly (submit button disabled until all required fields filled), but the E2E test was failing because it wasn't filling all required fields properly due to incorrect selectors.

## Root Cause
- Test was using `input[name="fieldName"]` selectors
- Mantine forms don't expose `name` attributes consistently in DOM
- Test was targeting wrong field names (e.g., looking for `preferredSceneName` instead of actual form fields)

## Required Fields Identified
Based on form analysis at line 484 in `VettingApplicationForm.tsx`:
1. `realName` (required)
2. `whyJoin` (required)
3. `experienceWithRope` (required)
4. `agreesToCommunityStandards` (required checkbox)

## Solution Applied
Updated `/tests/e2e/vetting-system.spec.ts` with correct selectors:

```typescript
// ✅ NEW: Semantic selectors based on actual UI
const realNameField = page.getByPlaceholder('Enter your real name');
const whyJoinField = page.getByPlaceholder('Tell us why you would like to join Witch City Rope and what you hope to gain from being part of our community...');
const experienceField = page.getByPlaceholder('Tell us about your experience with rope bondage, BDSM, or kink communities...');
const communityStandardsCheckbox = page.getByRole('checkbox', { name: 'I agree to all of the above items' });
const submitButton = page.getByRole('button', { name: 'Submit Application' });
```

## Validation Testing Added
```typescript
// Test submit button state validation
expect(await submitButton.isDisabled()).toBe(true); // Initially disabled
// ... fill required fields ...
expect(await submitButton.isEnabled()).toBe(true);  // Enabled after required fields
```

## Test Results
✅ **Form validation working perfectly**:
- Submit button correctly disabled initially
- All required fields filled successfully
- Submit button enabled after filling required fields
- Form submission successful
- Both required and optional fields tested

## Files Updated
- `/tests/e2e/vetting-system.spec.ts` - Updated field selectors and validation testing
- `/docs/standards-processes/testing/TEST_CATALOG.md` - Updated test documentation
- `/docs/lessons-learned/test-developer-lessons-learned.md` - Added Mantine form testing pattern

## Key Learning
**Mantine UI forms require semantic selectors** (`getByPlaceholder`, `getByRole`, `getByLabel`) instead of `name` attribute selectors for reliable E2E testing.

## Impact
- Vetting system E2E test now properly validates form validation behavior
- Test accurately reflects real user experience
- Future Mantine form tests can use this pattern
- Form validation working as designed - submit disabled until ALL required fields completed