# Playwright Scripts Cleanup Plan
<!-- Created: 2025-08-04 -->
<!-- Purpose: Plan for cleaning up test scripts in root directory -->

## Summary
Found 40+ JavaScript test files in the root directory that appear to be development/debugging artifacts. These files:
- Are NOT part of the official test suite (`/tests/playwright/`)
- Have significant duplication (8+ files test login functionality)
- Mix Playwright and Puppeteer (project uses Playwright only)
- Are not referenced in any active workflows
- Clutter the root directory

## Files to Delete (36 files)

### Login Test Scripts (16 files)
All functionality exists in `/tests/playwright/auth/`:
- `debug-login-page.js` - Debug script
- `debug-login-detailed.js` - Debug script
- `test-login-simple.js` - Duplicate functionality
- `debug-login-flow.js` - Debug script
- `debug-form-submit.js` - Debug script
- `debug-syncfusion-elements.js` - Debug script
- `simple-login-test.js` - Duplicate of test-login-simple.js
- `test-username-login.js` - Not needed (email login only)
- `test-both-login-methods.js` - Not needed
- `test-email-only-login.js` - Duplicate
- `test-simple-login.js` - Duplicate
- `test-identity-login.js` - Old Razor Pages auth
- `test-login-functionality.js` - Duplicate
- `debug-login-selectors.js` - Debug script
- `check-login-page.js` - Duplicate
- `test-playwright-login-direct.js` - Duplicate

### Auth & Navigation Scripts (14 files)
All functionality exists in `/tests/playwright/auth/` and `/tests/playwright/ui/`:
- `test-auth-functionality.js` - Covered in auth tests
- `test-razor-pages-auth.js` - Old Razor Pages
- `test-auth-complete.js` - Covered in auth tests
- `test-auth-state-propagation.js` - Covered
- `test-simple-auth-check.js` - Duplicate
- `check-auth-navigation.js` - Covered in navigation tests
- `test-dashboard-access.js` - Covered
- `test-user-menu.js` - Covered in ui tests
- `test-user-menu-js.js` - Duplicate
- `test-dropdown-debug.js` - Debug script
- `test-hover-menu.js` - Debug script
- `check-rendered-html.js` - Debug script
- `test-checkbox-dropdown.js` - Debug script
- `test-details-dropdown.js` - Debug script

### Other Test Scripts (9 files)
- `test-browser-automation.js` - Puppeteer (project uses Playwright)
- `test-event-flow.js` - Puppeteer with outdated selectors
- `test-playwright-selectors.js` - Debug script
- `test-actual-login.js` - Duplicate login test
- `test-events-page.js` - Covered in events tests
- `take_admin_screenshot.js` - Outdated selectors
- `check-admin-dashboard.js` - Outdated selectors
- `check-admin-events.js` - Outdated selectors
- `capture-admin-dashboard.js` - Has syntax error

## Files to Keep (3 files)

### 1. `analyze-login-logs.js`
- **Reason**: Listed as "main" in package.json
- **Action**: Review if still needed for monitoring

### 2. `test-homepage.js`
- **Reason**: Simple smoke test that could be useful
- **Action**: Move to `/tests/playwright/utilities/` if keeping

### 3. `playwright.config.ts` & `playwright.config.ci.ts`
- **Reason**: Official Playwright configuration files
- **Action**: Keep in root (standard location)

## Cleanup Process

1. **Create archive directory**: `/session-work/2025-08-04/archived-test-scripts/`
2. **Move all files to archive** (in case we need to recover anything)
3. **Update file registry** with all moves
4. **Update package.json** if analyze-login-logs.js is removed
5. **Clean up any generated screenshots** in root directory
6. **Update documentation** that references non-existent files

## Documentation Updates Needed
- Remove references to non-existent test files in:
  - `/docs/CLEANUP_SUMMARY.md`
  - `/docs/LOGIN_MONITORING_GUIDE.md`
  - `/docs/USER-DROPDOWN-TEST-README.md`
  - Other docs that reference old test scripts

## Benefits of Cleanup
- Cleaner root directory
- Clear separation between dev scripts and production tests
- Easier to understand which tests to run
- Reduced confusion for new developers
- Follows project standards (all tests in `/tests/`)

## Questions for User
1. Is `analyze-login-logs.js` still actively used for monitoring?
2. Should we keep `test-homepage.js` as a quick smoke test?
3. Do you want to archive the files or delete them completely?