# Archived Test Scripts
<!-- Archived: 2025-08-04 -->
<!-- Reason: Cleanup of root directory test scripts -->

## Overview
This directory contains JavaScript test scripts that were previously located in the project root directory. These scripts were created during development for debugging and testing various features but are no longer needed in the root directory.

## Why These Were Archived
1. **Not part of official test suite**: The official E2E tests are in `/tests/playwright/`
2. **Heavy duplication**: Multiple scripts tested the same functionality
3. **Mixed technologies**: Some used Puppeteer despite project migration to Playwright
4. **Poor organization**: Root directory is not the proper location for test files
5. **Outdated selectors**: Many scripts used old selectors that no longer work

## Categories of Archived Scripts

### Login Test Scripts (16 files)
- Various iterations of login testing and debugging
- Functionality now covered by `/tests/playwright/auth/login-*.spec.ts`

### Auth & Navigation Scripts (14 files)
- Authentication state testing and user menu debugging
- Functionality now covered by official Playwright tests

### Other Test Scripts (9 files)
- Puppeteer scripts (project uses Playwright)
- Admin dashboard screenshots
- Event page testing

## Official Test Locations
All E2E testing should use the official test suite:
- `/tests/playwright/` - Main E2E test directory
- `/tests/playwright/auth/` - Authentication tests
- `/tests/playwright/ui/` - UI component tests
- `/tests/playwright/admin/` - Admin functionality tests

## Useful Scripts Preserved
Two scripts were moved to `/tests/playwright/utilities/`:
- `analyze-login-logs.js` - Login test result analysis
- `test-homepage.js` - Quick homepage smoke test

## Note
These files are archived for historical reference. If you need to create new tests, please use the official test structure and follow the project's testing standards.