# Vetting Application 404 Console Error Suppression Fix

**Date**: 2025-10-05
**Type**: Bug Fix - Console Noise
**Priority**: Low (UX polish)
**Status**: Complete

## Problem Summary

Console errors appeared when loading the vetting application form at `/join` for users without existing applications. The API correctly returns 404 (meaning "no application found"), but this was logged as an error, creating unnecessary console noise.

## Root Cause Analysis

**Location**: `/apps/web/src/lib/api/client.ts` (axios response interceptor)

**Issue**: The error interceptor logged ALL API errors, including expected 404 responses:

```typescript
// OLD CODE - Logged all errors
async (error) => {
  const { response, config } = error

  console.error(`API Error: ${config?.method?.toUpperCase()} ${config?.url}`, {
    status: response?.status,
    statusText: response?.statusText,
    data: response?.data
  })
  // ... rest of error handling
}
```

**Why 404 is Expected Behavior**:
1. User visits `/join` to submit vetting application
2. Frontend calls `GET /api/vetting/my-application` to check for existing application
3. If user has NO application (expected for new applicants), API returns 404
4. Frontend correctly handles 404 by showing the application form (NOT an error state)
5. Console shows error even though everything works correctly

## Solution Implemented

**File Modified**: `/apps/web/src/lib/api/client.ts` (lines 43-57)

**Fix**: Added suppression logic for expected 404 responses on the `/my-application` endpoint:

```typescript
// NEW CODE - Suppress expected 404s
async (error) => {
  const { response, config } = error

  // Check if this is an expected 404 for vetting application check
  // When a user visits /join without an existing application, the API returns 404
  // This is EXPECTED behavior - it means "no application found, show the form"
  const is404 = response?.status === 404
  const isMyApplicationEndpoint = config?.url?.includes('/my-application')
  const shouldSuppressLog = is404 && isMyApplicationEndpoint

  // Only log actual errors, not expected 404s for application checks
  if (!shouldSuppressLog) {
    console.error(`API Error: ${config?.method?.toUpperCase()} ${config?.url}`, {
      status: response?.status,
      statusText: response?.statusText,
      data: response?.data
    })
  }

  // ... rest of error handling (401s, etc.)
}
```

## Key Design Decisions

### Why This Approach?

1. **Minimal Impact**: Only suppresses ONE specific endpoint's expected 404s
2. **Clear Intent**: Inline comments explain WHY 404s are expected
3. **Still Throws Error**: The error is still thrown for the query to handle - we only suppress the console log
4. **No Functional Change**: Form behavior unchanged, just removes console noise

### What's Still Logged?

- All other 404 errors (unexpected ones)
- All 401, 403, 500 errors
- All errors on other endpoints
- Network errors, timeouts, etc.

### What's Suppressed?

ONLY: `404 GET /api/vetting/my-application` errors

## Testing Verification

### Test Scenario 1: New User Visits Application Form
**Expected Behavior**:
1. Load `/join` as new user (no existing application)
2. Console shows NO 404 errors
3. Form displays correctly (showing application form, not "you already applied" message)
4. Submit functionality works normally

### Test Scenario 2: Existing Applicant Visits Form
**Expected Behavior**:
1. Load `/join` as user with existing application
2. API returns 200 with application data
3. UI shows "You have already submitted an application" message
4. No console errors

### Test Scenario 3: Other 404 Errors Still Log
**Expected Behavior**:
1. Try to access non-existent API endpoint (e.g., `/api/invalid`)
2. Console shows 404 error (NOT suppressed)
3. Only `/my-application` 404s are suppressed

## Impact Assessment

### Before Fix
- Console shows red error: `API Error: GET /api/vetting/my-application` with 404 status
- Confusing for developers debugging
- Looks like something is broken (even though it's working correctly)

### After Fix
- Clean console when loading vetting form
- No errors shown for expected "no application found" case
- Actual errors still visible (401s, unexpected 404s, etc.)

## Important Notes

1. **This is NOT a bug** - The 404 is EXPECTED BEHAVIOR
2. **The error is still thrown** - React Query handles it normally
3. **Only console logging is suppressed** - No functional change
4. **Pattern is reusable** - Can apply to other "expected 404" scenarios

## Related Code

**Vetting Application Service**: `/apps/web/src/lib/api/vettingApi.ts`
```typescript
export const vettingApi = {
  async getMyApplication(): Promise<VettingApplicationDto | null> {
    try {
      const response = await apiClient.get('/api/vetting/my-application');
      return response.data;
    } catch (error: any) {
      if (error.response?.status === 404) {
        return null; // Expected - no application found
      }
      throw error;
    }
  }
};
```

**Vetting Form Component**: `/apps/web/src/pages/VettingApplicationPage.tsx`
```typescript
const { data: existingApplication } = useQuery({
  queryKey: ['vetting', 'my-application'],
  queryFn: vettingApi.getMyApplication
  // If 404, returns null, component shows application form
});
```

## Documentation Updates

- **File Registry**: Entry added documenting this change
- **Lessons Learned**: Consider adding pattern for "expected 404s" if recurring

## Future Considerations

### Potential Additional Endpoints to Suppress

If other endpoints follow the "404 means resource doesn't exist" pattern for user-facing checks, consider adding similar suppression:

- User profile checks: `/api/users/me/profile` (404 = no profile yet)
- Payment method checks: `/api/users/me/payment-method` (404 = no payment method)
- Preferences checks: `/api/users/me/preferences` (404 = default preferences)

### Alternative Approaches Considered

1. **React Query `throwOnError: false`**: Would hide errors from DevTools, not just console
2. **Backend returns 200 with null**: Changes API contract, affects other consumers
3. **Frontend doesn't call endpoint**: Loses ability to detect existing applications
4. **Separate "check exists" endpoint**: Adds unnecessary API endpoints

## Completion Checklist

- [x] Code change implemented
- [x] Inline documentation added
- [x] File registry updated
- [x] Summary document created
- [x] Testing approach documented
- [ ] Manual testing verification (pending deployment)

---

**Result**: Clean console, same functionality, better developer experience.
