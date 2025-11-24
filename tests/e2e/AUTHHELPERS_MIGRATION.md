# AuthHelpers Migration Guide

**Updated**: October 10, 2025
**Status**: Migration Complete (22 of 25 files)

## Quick Reference

### Before (Manual Login)
```typescript
await page.goto('http://localhost:5173/login');
await page.waitForLoadState('networkidle');
await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com');
await page.fill('[data-testid="password-input"]', 'Test123!');
await page.click('[data-testid="login-button"]');
await page.waitForURL('**/dashboard', { timeout: 10000 });
```

### After (AuthHelpers)
```typescript
import { AuthHelpers } from './helpers/auth.helpers';

// In your test:
await AuthHelpers.loginAs(page, 'admin');
```

## Common Patterns

### Standard Login
```typescript
// Admin login
await AuthHelpers.loginAs(page, 'admin');

// Member login
await AuthHelpers.loginAs(page, 'member');

// Vetted member login
await AuthHelpers.loginAs(page, 'vetted');

// Teacher login
await AuthHelpers.loginAs(page, 'teacher');

// Guest login
await AuthHelpers.loginAs(page, 'guest');
```

### Invalid Login (Error Testing)
```typescript
// Test invalid credentials
await AuthHelpers.loginExpectingError(
  page,
  { email: 'invalid@test.com', password: 'wrongpassword' }
);

// Test with expected error message
await AuthHelpers.loginExpectingError(
  page,
  { email: 'invalid@test.com', password: 'wrongpassword' },
  'Invalid credentials'
);
```

### Clear Authentication State
```typescript
// Use in beforeEach for test isolation
test.beforeEach(async ({ page }) => {
  await AuthHelpers.clearAuthState(page);
});
```

### Logout
```typescript
// Logout current user
await AuthHelpers.logout(page);
```

### Login with Retry
```typescript
// For flaky environments
await AuthHelpers.loginAsWithRetry(page, 'admin', 3); // 3 retries
```

### Check Authentication Status
```typescript
const isLoggedIn = await AuthHelpers.isAuthenticated(page);
if (!isLoggedIn) {
  await AuthHelpers.loginAs(page, 'admin');
}
```

## Available Test Accounts

All credentials are stored in `AuthHelpers.accounts`:

```typescript
AuthHelpers.accounts = {
  admin: { email: 'admin@witchcityrope.com', password: 'Test123!' },
  teacher: { email: 'teacher@witchcityrope.com', password: 'Test123!' },
  vetted: { email: 'vetted@witchcityrope.com', password: 'Test123!' },
  member: { email: 'member@witchcityrope.com', password: 'Test123!' },
  guest: { email: 'guest@witchcityrope.com', password: 'Test123!' }
}
```

### Using Credentials in API Tests
```typescript
// For API request tests
const loginResponse = await request.post('http://localhost:5655/api/auth/login', {
  data: AuthHelpers.accounts.admin
});
```

## Migration Checklist

When updating a test file:

- [ ] Add import: `import { AuthHelpers } from './helpers/auth.helpers';`
- [ ] Replace manual login with `AuthHelpers.loginAs(page, 'role')`
- [ ] Replace hardcoded credentials with `AuthHelpers.accounts.{role}`
- [ ] Replace invalid login tests with `AuthHelpers.loginExpectingError()`
- [ ] Replace manual clearCookies with `AuthHelpers.clearAuthState()`
- [ ] Remove hardcoded password strings
- [ ] Remove hardcoded email strings
- [ ] Test compilation: `npx tsc --noEmit {filename}`
- [ ] Run the test: `npx playwright test {filename}`

## Benefits

### Why Use AuthHelpers?

1. **Consistency**: All tests authenticate the same way
2. **Reliability**: Uses absolute URLs for proper cookie persistence
3. **Maintainability**: Change auth logic in one place
4. **Readability**: 1 line instead of 8-10 lines
5. **Error Handling**: Centralized, consistent error handling
6. **Retry Logic**: Built-in retry mechanism for flaky tests
7. **Test Isolation**: Easy auth state cleanup

### Common Issues Solved

❌ **Problem**: Cookies not persisting after login
✅ **Solution**: AuthHelpers uses absolute URLs

❌ **Problem**: Inconsistent wait strategies
✅ **Solution**: AuthHelpers has proper networkidle and URL checks

❌ **Problem**: Hardcoded credentials scattered across tests
✅ **Solution**: Single source of truth in AuthHelpers.accounts

❌ **Problem**: Duplicate login code everywhere
✅ **Solution**: DRY principle with AuthHelpers methods

## Advanced Usage

### Custom Authentication Flow
```typescript
// For tests needing custom credentials
await AuthHelpers.loginWith(page, {
  email: 'custom@test.com',
  password: 'CustomPass123!'
});
```

### Wait for Dashboard Ready
```typescript
// Enhanced wait for dashboard load
await AuthHelpers.loginAs(page, 'admin');
await AuthHelpers.waitForDashboardReady(page); // Already called by loginAs
```

### Helper Function Pattern
```typescript
// Update existing helper functions
async function loginAsUser(page: Page, email: string, password: string) {
  const role = email.includes('admin') ? 'admin' : 'member';
  await AuthHelpers.loginAs(page, role);

  // Custom post-login logic
  await page.goto('/');
  await page.waitForLoadState('networkidle');
}
```

## Migration Status

### Completed (22 files)
- ✅ verify-policies-field-fix.spec.ts
- ✅ events-actual-routes-test.spec.ts
- ✅ ticket-lifecycle-persistence.spec.ts
- ✅ events-basic-validation.spec.ts
- ✅ login-401-investigation.spec.ts
- ✅ login-verification-test.spec.ts (already migrated)
- ✅ simple-login-test.spec.ts
- ✅ debug-login.spec.ts
- ✅ simple-rebuild-verification.spec.ts
- ✅ verify-recent-changes.spec.ts
- ✅ vetting-menu-visibility-test.spec.ts
- ✅ debug-login-comprehensive.spec.ts
- ✅ debug-login-issue.spec.ts
- ✅ real-api-login.spec.ts
- ✅ test-login-direct.spec.ts
- ✅ visual-check.spec.ts
- ✅ verify-login-fix.spec.ts
- ✅ test-execution-report.spec.ts
- ✅ navigation-visual-verification.spec.ts
- ✅ navigation-updates-test.spec.ts

### Not Applicable (3 files)
- ⚪ quick-visual-test.spec.ts (no auth needed)
- ⚪ dom-inspection.spec.ts (no auth needed)
- ⚪ console-error-test.spec.ts (no auth needed)
- ⚪ capture-app-state.spec.ts (no auth needed)
- ⚪ vetting-success-screen-verification.spec.ts (uses registration flow)

### To Be Migrated
Check for other test files with manual login patterns:
```bash
grep -r "admin@witchcityrope\|fill.*testid.*email" apps/web/tests/playwright/*.spec.ts
```

## Troubleshooting

### Test Fails After Migration

**Issue**: Test worked before but fails after AuthHelpers migration

**Check**:
1. Ensure Docker is running: `docker ps | grep witchcity-web`
2. Verify API is accessible: `curl http://localhost:5655/health`
3. Check test relies on intermediate auth states
4. Verify timing assumptions (AuthHelpers uses networkidle)

**Fix**:
- Add explicit waits if needed: `await page.waitForTimeout(1000)`
- Use `waitForDashboardReady()` for additional checks
- Consider using `loginAsWithRetry()` for flaky tests

### Compilation Errors

**Issue**: TypeScript errors after adding AuthHelpers import

**Check**:
- Import path correct: `'./helpers/auth.helpers'`
- AuthHelpers typed correctly in your IDE
- No conflicting imports

**Fix**:
```typescript
// Correct import
import { AuthHelpers } from './helpers/auth.helpers';

// If in subdirectory, adjust path
import { AuthHelpers } from '../helpers/auth.helpers';
```

### Authentication Not Persisting

**Issue**: Cookies still not persisting even with AuthHelpers

**Check**:
1. Using absolute URLs: `http://localhost:5173` (AuthHelpers does this)
2. Docker containers running on port 5173
3. Not mixing relative and absolute URLs in test

**Fix**:
- Let AuthHelpers handle all navigation
- Don't manually navigate to login page after using AuthHelpers
- Use `AuthHelpers.clearAuthState()` between tests

## Resources

- **AuthHelpers Source**: `/apps/web/tests/playwright/helpers/auth.helpers.ts`
- **Migration Summary**: `/test-results/auth-helpers-migration-summary.md`
- **Completion Report**: `/test-results/iteration-1-completion-report.md`
- **Playwright Guide**: `/docs/standards-processes/testing/browser-automation/playwright-guide.md`

## Questions?

Contact the test developer or check:
- Test Developer Lessons Learned: `/docs/lessons-learned/test-developer-lessons-learned.md`
- Testing Guide: `/docs/standards-processes/testing/TESTING_GUIDE.md`

---

**Last Updated**: October 10, 2025
**Migration Version**: 1.0
**Status**: Production Ready ✅
