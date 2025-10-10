# Timeout Configuration Standard

**Last Updated**: 2025-10-09
**Status**: Active Standard
**Enforcement**: Mandatory across all tests and API clients

## ⛔ CRITICAL WARNING: NEVER USE 10+ MINUTE TIMEOUTS

**ABSOLUTE RULE**: NO TEST SHOULD EVER TAKE 10 MINUTES

**User's Exact Requirements**:
> "NO TEST should ever take 10 minutes. Most will not take more than 30 seconds, but giving them 1 minute maybe 1.5 at the absolute most is plenty. If it takes longer than that, then something has failed and the test is stalled forever. This is VERY important that you set the bash commands to have this time out limit as well as the other tests."

**This means**:
- Tests taking >90 seconds are **BROKEN/STALLED**, not slow
- Fix the test, don't increase the timeout
- This applies to ALL tests AND bash commands

---

## 🚨 ABSOLUTE MAXIMUM: 90 SECONDS

**NO timeout in any test, API client, or bash command should exceed 90 seconds (90000ms).**

### Realistic Timeout Expectations:
- **Most tests**: 30 seconds or less (this is normal)
- **Complex tests**: 60 seconds (1 minute)
- **Absolute maximum**: 90 seconds (1.5 minutes) - NEVER EXCEED
- **Tests >90s**: STALLED - fix the underlying issue, don't increase timeout

## Why 90 Seconds Maximum?

1. **Realistic Upper Bound**: No test should legitimately need more than 60 seconds
2. **Safety Buffer**: 90 seconds provides a 1.5x safety buffer for edge cases
3. **Problem Detection**: Tests consistently hitting these limits indicate real problems:
   - Slow backend responses
   - Infinite loops in code
   - Network issues
   - Missing data or broken dependencies
   - Inefficient queries or operations
   - **Test is stalled/broken** - fix the test logic

## Timeout Hierarchy

### Playwright E2E Tests

**Configuration File**: `/apps/web/playwright.config.ts`

```typescript
timeout: 90 * 1000,  // 90 seconds ABSOLUTE MAXIMUM per test
expect: {
  timeout: 5000,     // 5 seconds for assertions (reasonable for UI)
}
```

**Use Cases**:
- Global test timeout: 90 seconds (DO NOT INCREASE)
- Assertion timeout: 5 seconds (fast UI checks)
- Action timeout: 5 seconds (clicks, fills, etc.)
- Navigation timeout: 10 seconds (page loads)

### Vitest Unit/Integration Tests

**Configuration File**: `/apps/web/vitest.config.ts`

```typescript
testTimeout: 90000,    // 90 seconds ABSOLUTE MAXIMUM per test
hookTimeout: 30000,    // 30 seconds for setup/teardown hooks
teardownTimeout: 10000 // 10 seconds for cleanup
```

**Use Cases**:
- Unit tests: Should complete in <5 seconds
- Integration tests: Should complete in <30 seconds
- Tests taking >30 seconds: Investigate and optimize
- Tests taking >90 seconds: Test is STALLED/BROKEN - fix it

### Wait Helper Constants

**File**: `/apps/web/tests/playwright/helpers/wait.helpers.ts`

```typescript
export const TIMEOUTS = {
  SHORT: 5000,           // 5 seconds - Quick UI interactions
  MEDIUM: 10000,         // 10 seconds - Standard waits
  LONG: 30000,           // 30 seconds - Complex operations (DO NOT INCREASE)
  NAVIGATION: 30000,     // 30 seconds - Page navigation
  API_RESPONSE: 10000,   // 10 seconds - API calls (should be fast)
  AUTHENTICATION: 15000, // 15 seconds - Auth flows
  FORM_SUBMISSION: 20000,// 20 seconds - Form processing
  PAGE_LOAD: 30000,      // 30 seconds - Full page load
  ABSOLUTE_MAX: 90000    // 90 seconds - NEVER EXCEED THIS
}
```

### Bash Command Timeouts

**CRITICAL**: Bash commands MUST also use the 90-second maximum timeout.

```bash
# ❌ WRONG - 10 minute timeout
timeout 600 npm run test:e2e

# ✅ CORRECT - 90 second maximum
timeout 90 npm run test:e2e
```

**When using Bash tool**: Always specify timeout parameter (max 90000ms = 90 seconds)

### API Client Configuration

**Files**:
- `/apps/web/src/config/api.ts`: `timeout: 90000`
- `/apps/web/src/config/environment.ts`: `timeout: 90000`
- `/apps/web/vite.config.ts`: `proxy.timeout: 90000`

All API timeouts are set to 90 seconds maximum to align with test configuration.

## Timeout Values by Operation Type

| Operation Type | Recommended Timeout | Maximum Allowed |
|----------------|---------------------|-----------------|
| Button click | 5 seconds | 10 seconds |
| Form fill | 5 seconds | 10 seconds |
| Page navigation | 10 seconds | 30 seconds |
| API response | 10 seconds | 30 seconds |
| Authentication | 15 seconds | 30 seconds |
| Form submission | 20 seconds | 30 seconds |
| Full page load | 30 seconds | 60 seconds |
| Complete test | 30 seconds | 90 seconds |
| Bash commands | 60 seconds | 90 seconds |

## When Tests Exceed Limits

⚠️ **CRITICAL**: If a test is consistently timing out or approaching the 90-second limit, **DO NOT simply increase the timeout**.

### The Problem is NOT the Timeout

Tests taking >90 seconds are **stalled or broken**:
- Infinite loops waiting for conditions that never happen
- Missing data or broken test setup
- Backend services not running
- Network configuration issues
- Race conditions or deadlocks

### Immediate Actions
1. **Check logs**: Look for actual errors before timeout
2. **Verify services**: Ensure Docker containers are running
3. **Check database**: Verify test data exists
4. **Network issues**: Check for proxy or connectivity problems

### Investigation Steps
1. **Profile the test**: Add timestamps to see where time is spent
2. **Check API responses**: Are endpoints responding slowly?
3. **Database queries**: Are they optimized?
4. **Infinite loops**: Check for missing await, broken conditions
5. **Test isolation**: Are tests conflicting with each other?

### Fix the Root Cause - DO NOT Increase Timeout
- Increasing timeout masks the real problem
- Fix the root cause instead
- Only increase if legitimately necessary (rare - and NEVER above 90s)

## Common Anti-Patterns to Avoid

### ❌ BAD: Arbitrary large timeouts
```typescript
await page.waitForTimeout(600000); // 10 minutes - ABSOLUTELY NO!
test.setTimeout(300000); // 5 minutes - NO!
test.setTimeout(120000); // 2 minutes - NO! (exceeds 90s max)
```

### ✅ GOOD: Reasonable timeouts with explanation
```typescript
await page.waitForTimeout(1000); // 1 second - Allow state update
test.setTimeout(60000); // 1 minute - Complex auth flow test
```

### ❌ BAD: Polling without timeout
```typescript
while (!(await element.isVisible())) {
  await page.waitForTimeout(100); // Infinite loop if never visible!
}
```

### ✅ GOOD: Polling with explicit timeout
```typescript
await expect(element).toBeVisible({ timeout: 10000 }); // 10 second max
```

### ❌ BAD: Long bash command timeouts
```bash
# In Bash tool with timeout parameter
timeout: 600000  // 10 minutes - NO!
```

### ✅ GOOD: Reasonable bash command timeouts
```bash
# In Bash tool with timeout parameter
timeout: 90000  // 90 seconds - ABSOLUTE MAX
```

## Enforcement

### Pre-commit Checks
The following will be checked in CI:
- No timeout > 90000ms in any config file
- No test.setTimeout() > 90000ms
- No page.setDefaultTimeout() > 90000ms
- All API clients use ≤ 90000ms timeout
- Bash tool calls use ≤ 90000ms timeout

### Code Review Checklist
- [ ] All timeouts documented with reason
- [ ] No timeout exceeds 90 seconds
- [ ] Timeouts appropriate for operation type
- [ ] No arbitrary large timeouts without justification
- [ ] Bash commands include timeout limits ≤ 90s

## Configuration Locations

All timeout configurations have been centralized and documented:

1. **Playwright**: `/apps/web/playwright.config.ts`
2. **Vitest**: `/apps/web/vitest.config.ts`
3. **Wait Helpers**: `/apps/web/tests/playwright/helpers/wait.helpers.ts`
4. **API Config**: `/apps/web/src/config/api.ts`
5. **Environment**: `/apps/web/src/config/environment.ts`
6. **Vite Proxy**: `/apps/web/vite.config.ts`

## Migration Notes

**October 9, 2025**: Updated all timeout configurations from 30 seconds to 90 seconds maximum.

**Rationale**: Provide safety buffer while preventing excessively long test runs. The previous 30-second limit was too aggressive for some integration tests, but 90 seconds is the absolute maximum.

**CRITICAL UPDATE - October 9, 2025**: Added explicit warnings against 10+ minute timeouts after user feedback that we kept suggesting incorrect timeout values.

**Changes Made**:
- Updated Playwright global test timeout: 30s → 90s
- Updated Vitest test timeout: 30s → 90s
- Updated Vitest hook timeout: 10s → 30s
- Updated all API client timeouts: 30s → 90s
- Added ABSOLUTE_MAX constant to wait helpers
- Added comprehensive documentation and comments
- **Added critical warnings against 10+ minute timeouts**
- **Documented bash command timeout requirements**

## Related Documentation

- [Playwright Testing Guide](/docs/standards-processes/testing/browser-automation/playwright-guide.md)
- [Testing Guide](/docs/standards-processes/testing/TESTING_GUIDE.md)
- [Test Catalog](/docs/standards-processes/testing/TEST_CATALOG.md)
