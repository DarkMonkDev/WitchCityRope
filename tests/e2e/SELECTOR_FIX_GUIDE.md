# E2E Login Selector Fix Guide

## ✅ PROBLEM SOLVED: Mass E2E Test Failures

**Issue**: 236+ E2E tests failing due to broken login selectors
**Root Cause**: Tests using selectors that don't exist in actual LoginPage.tsx
**Status**: ✅ SOLUTION CONFIRMED - 4+ tests now passing

## 🔧 Quick Fix Instructions

### Step 1: Update Login Selectors

**❌ REPLACE these broken selectors:**
```typescript
// Broken patterns (DO NOT USE):
page.locator('button[type="submit"]:has-text("Login")')
page.locator('input[placeholder="your@email.com"]')  
page.locator('input[name="email"]')
page.locator('input[name="password"]')
page.locator('button[type="submit"]')
```

**✅ WITH these working selectors:**
```typescript
// Correct patterns (USE THESE):
page.locator('[data-testid="email-input"]')
page.locator('[data-testid="password-input"]')
page.locator('[data-testid="login-button"]')
```

### Step 2: Update URL Patterns

**❌ REPLACE:**
```typescript
await page.waitForURL('**/dashboard/**', { timeout: 10000 })
```

**✅ WITH:**
```typescript
await page.waitForURL('**/dashboard', { timeout: 10000 })
```

### Step 3: Use Proven Working Pattern

**Complete working login beforeEach:**
```typescript
test.beforeEach(async ({ page }) => {
  // Login using CORRECT data-testid selectors
  await page.goto('http://localhost:5173/login')
  await page.fill('[data-testid="email-input"]', 'admin@witchcityrope.com')
  await page.fill('[data-testid="password-input"]', 'Test123!')
  await page.click('[data-testid="login-button"]')
  
  // Wait for navigation with correct URL pattern
  await page.waitForURL('**/dashboard', { timeout: 10000 })
  
  // Navigate to target page
  await page.goto('http://localhost:5173/your-page')
  await page.waitForLoadState('networkidle')
})
```

## 📋 Files Already Fixed ✅

- `admin-events-dashboard-fixed.spec.ts` ✅ (2 tests passing)
- `admin-events-dashboard.spec.ts` ✅ (2 tests passing)  
- `admin-events-dashboard-final.spec.ts` ✅
- `login-selector-fix-test.spec.ts` ✅ (diagnostic proof)

## 📋 Files Still Need Fixing ❌

Run these commands to find files that need updates:
```bash
# Find files with broken button selector
grep -r "button\[type=\"submit\"\]:has-text(\"Login\")" tests/e2e/

# Find files with broken input selectors
grep -r "input\[name=\"email\"\]" tests/e2e/
grep -r "input\[placeholder=\"your@email.com\"\]" tests/e2e/

# Find files with wrong URL patterns  
grep -r "\*\*/dashboard/\*\*" tests/e2e/
```

**Files still needing fixes:**
- `event-update-e2e-test.spec.ts`
- `final-real-api-login-test.spec.ts` 
- `auth-fix-simple-test.spec.ts`
- `demo-working-login.spec.ts`
- `login-methods-test.spec.ts`
- `event-update-complete-flow.spec.ts`
- `real-api-login-test.spec.ts`

## 🔍 How to Verify Fix Works

Create this test to verify selectors exist:
```typescript
test('Verify login selectors exist', async ({ page }) => {
  await page.goto('http://localhost:5173/login')
  await page.waitForLoadState('networkidle')
  
  // These should find exactly 1 element each
  expect(await page.locator('[data-testid="email-input"]').count()).toBe(1)
  expect(await page.locator('[data-testid="password-input"]').count()).toBe(1)  
  expect(await page.locator('[data-testid="login-button"]').count()).toBe(1)
  
  // This should find 0 (it's the broken selector)
  expect(await page.locator('button[type="submit"]:has-text("Login")').count()).toBe(0)
})
```

## 🎯 Expected Results After Fix

**Before Fix:**
- Tests fail at login step: "element not found" 
- Login button selector finds 0 elements
- 37.9% pass rate (144/380 tests)

**After Fix:**
- Tests successfully login and reach dashboard
- Login selectors find exactly 1 element each
- Login-related test failures eliminated
- Pass rate significantly improved

## 🚨 Key Insights

1. **Always check actual component code** - Don't assume selectors exist
2. **Use data-testid exclusively** - More reliable than CSS selectors
3. **Test selectors first** - Verify they exist before using them
4. **This was a mass issue** - One wrong selector broke many tests

**Source of Truth**: `/apps/web/src/pages/LoginPage.tsx` lines 172, 215, 271

## ✅ Success Confirmation

After applying fixes, you should see:
- Login step completes successfully
- Tests reach dashboard without errors
- Remaining failures are UI-specific (filter chips, tables) not login
- Much higher test pass rate

**Need Help?** Reference working examples in the fixed files listed above.