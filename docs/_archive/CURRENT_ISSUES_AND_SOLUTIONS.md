# Current Issues and Solutions - Development Priorities

**Last Updated**: July 18, 2025  
**Context**: Post major infrastructure fixes - focusing on polish and E2E test reliability

---

## 🎯 HIGH PRIORITY ISSUES (Next Engineer Should Start Here)

### Issue #1: E2E Test Selector Updates
**Severity**: Low  
**Effort**: 1-2 hours  
**Impact**: Test reliability and development confidence  

**Problem**: 2 out of 12 E2E validation tests failing due to outdated selectors

**Specific Failures**:
```bash
# Failure 1: Login validation test
File: /tests/e2e/validation-standardization-tests.js:22
Error: "No element found for selector: .sign-in-btn"
Current selector: await page.waitForSelector('.sign-in-btn');
Fixed selector: await page.waitForSelector('button[type="submit"]');

# Failure 2: Event management navigation
File: /tests/e2e/validation-standardization-tests.js:89
Error: "Navigation timeout of 30000 ms exceeded"
Investigation needed: Check actual admin event edit route
Likely issue: URL changed from /admin/events/edit to different route
```

**Solution Steps**:
```bash
# 1. Fix login selector
cd /home/chad/repos/witchcityrope/WitchCityRope/tests/e2e
# Edit validation-standardization-tests.js line 22
# Change: '.sign-in-btn' → 'button[type="submit"]'

# 2. Investigate event edit route
curl -s http://localhost:5651/admin/events | grep -i edit
# Update test URL to match actual route

# 3. Verify fix
node validation-standardization-tests.js
# Target: 12/12 tests passing
```

**Files to Update**:
- `/tests/e2e/validation-standardization-tests.js`

---

### Issue #2: Form Validation Display Inconsistency  
**Severity**: Medium  
**Effort**: 2-3 hours  
**Impact**: User experience and form usability

**Problem**: Client-side validation not consistently triggering in forms

**Affected Forms**:
```bash
❌ Login Form - Expected validation errors for empty form
❌ Register Form - Password requirements not shown  
❌ Forgot Password Form - Expected email validation error
❌ Change Password Form - Expected validation errors for all password fields
❌ Manage Email Form - Expected email validation error
❌ Delete Personal Data Form - Expected validation errors
```

**Root Cause Analysis Needed**:
```bash
# Check if validation attributes are properly applied
# Location: /src/WitchCityRope.Web/Shared/Validation/Components/

# Verify client-side validation JavaScript is loading
curl -s http://localhost:5651 | grep -i "validation\|blazor"

# Test form submission without server round-trip
# Use browser dev tools to verify validation fires before HTTP request
```

**Investigation Steps**:
```bash
# 1. Check WCR validation component implementation
ls -la /src/WitchCityRope.Web/Shared/Validation/Components/
# Files: WcrInputText.razor, WcrInputEmail.razor, etc.

# 2. Verify validation attributes are rendering
# Test with browser inspector on login form
# Should see: required, data-val-* attributes

# 3. Check Blazor validation JavaScript
# Ensure Microsoft.AspNetCore.Components.Forms.js is loaded
```

**Solution Approach**:
```bash
# 1. Update validation component behavior
# Ensure EditForm properly triggers client validation
# Check: OnInvalidSubmit vs OnValidSubmit handling

# 2. Add explicit validation triggering
# Use EditContext.Validate() where needed
# Ensure ValidationSummary displays properly

# 3. Test each form individually
cd tests/e2e
node test-blazor-login-basic.js  # Should show validation without server trip
```

---

### Issue #3: Browser Selector Modernization
**Severity**: Low  
**Effort**: 2-3 hours  
**Impact**: Test maintainability and cross-browser compatibility

**Problem**: Multiple tests using `:has-text()` pseudo-selector which isn't supported by browsers

**Affected Files**:
```bash
/tests/e2e/test-all-migrated-forms.js:156
/tests/e2e/test-complete-event-flow.js:108  
/tests/e2e/admin-events-management.test.js:multiple locations
```

**Standard Replacements Needed**:
```javascript
// OLD (not supported):
button:has-text("RSVP"), button:has-text("Register")
a:has-text("Edit") 
div:has-text("Error")

// NEW (standard CSS):
button[type="submit"], .btn-primary, button[aria-label*="rsvp"]
a[href*="edit"], .edit-link, .btn-edit
.alert-danger, .validation-summary, .error-message
```

**Systematic Fix Process**:
```bash
# 1. Find all occurrences
grep -r "has-text" tests/e2e/ --include="*.js"

# 2. Create replacement mapping
echo "Document current selectors and their standard equivalents"

# 3. Update files one by one
# Test each file after updating to ensure functionality

# 4. Verify with cross-browser testing if needed
```

---

## 🔧 MEDIUM PRIORITY ISSUES

### Issue #4: Minor Resource 404 Errors
**Severity**: Very Low  
**Effort**: 30 minutes  
**Impact**: Clean console logs

**Problem**: favicon.ico and some endpoints returning 404

**Console Errors**:
```bash
Failed to load resource: the server responded with a status of 404 (Not Found)
http://localhost:5651/favicon.ico
```

**Simple Fixes**:
```bash
# 1. Add favicon.ico to wwwroot/
cp /path/to/favicon.ico /src/WitchCityRope.Web/wwwroot/

# 2. Check for any hardcoded URLs using wrong ports
grep -r "8280\|8180" tests/e2e/
# Update any found to use 5651

# 3. Verify static file middleware configuration
# Should be properly configured in Program.cs
```

---

### Issue #5: Port Configuration in Some Tests
**Severity**: Low  
**Effort**: 15 minutes  
**Impact**: Test consistency

**Problem**: Some E2E tests trying to use port 8280 instead of 5651

**Affected Tests**:
```bash
test-blazor-validation-comprehensive.js
check-console-errors.js
```

**Fix**:
```bash
# Search and replace in test files
sed -i 's/8280/5651/g' tests/e2e/test-blazor-validation-comprehensive.js
sed -i 's/8280/5651/g' tests/e2e/check-console-errors.js

# Verify application URL consistency
grep -r "localhost:" tests/e2e/ | grep -v "5651"
```

---

## 🎯 SOLUTION TEMPLATES

### Template 1: Fixing E2E Selector Issues
```javascript
// BEFORE (failing):
await page.waitForSelector('.sign-in-btn');
await page.click('button:has-text("Save")');

// AFTER (working):
await page.waitForSelector('button[type="submit"]');
await page.click('button[type="submit"], .btn-primary');

// Best Practice:
// 1. Use standard CSS selectors
// 2. Add fallback selectors for robustness  
// 3. Test with browser dev tools first
```

### Template 2: Validation Display Debugging
```javascript
// Add to test for debugging validation:
await page.evaluate(() => {
    const form = document.querySelector('form');
    const isValid = form?.checkValidity();
    const validationMessages = Array.from(document.querySelectorAll('.validation-message'))
        .map(el => el.textContent);
    console.log('Form valid:', isValid, 'Messages:', validationMessages);
});
```

### Template 3: Update Test URLs
```javascript
// BEFORE:
const BASE_URL = 'http://localhost:8280';

// AFTER:  
const BASE_URL = 'http://localhost:5651';

// Environment-aware approach:
const BASE_URL = process.env.TEST_BASE_URL || 'http://localhost:5651';
```

---

## 📋 PRIORITY QUEUE FOR NEXT ENGINEER

**Recommended Order**:

1. **[30 min] Fix E2E selector issues** - Quick wins for test reliability
2. **[1 hour] Investigate validation display** - Core UX improvement  
3. **[2 hours] Update :has-text() selectors** - Test modernization
4. **[15 min] Fix port configurations** - Consistency cleanup
5. **[15 min] Add missing favicon** - Polish

**Expected Outcome After All Fixes**:
- E2E Tests: 12/12 passing (up from 10/12)
- Form Validation: Consistent client-side validation
- Console: Clean logs with no 404 errors
- Test Suite: Fully modern CSS selectors

---

## 🚀 SUCCESS VERIFICATION

**After implementing fixes, verify with**:
```bash
# 1. Full E2E test suite
cd tests/e2e
node validation-standardization-tests.js  # Should be 12/12 ✅
node test-all-migrated-forms.js          # Higher success rate ✅

# 2. Manual form testing
# Open http://localhost:5651/login
# Submit empty form - should see validation immediately ✅
# No browser console errors ✅

# 3. Clean development experience
# All tests running without selector warnings ✅
# Consistent test results across runs ✅
```

---

**Remember**: These are polish issues on top of a solid, working foundation. The core application functionality is excellent - these fixes will make the development experience even better! 🎯