# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
> 
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Active File Registry

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-09-10 | /tests/playwright/login-and-events-verification.spec.ts | CREATED | Comprehensive E2E test for login and events verification per user requirements | Test executor login/events testing | ACTIVE | Keep permanent |
| 2025-09-10 | /test-results/login-and-events-test-report-2025-09-10.md | CREATED | Detailed test execution report showing successful login and events functionality | Test executor comprehensive testing | ACTIVE | Keep permanent |
| 2025-09-10 | /test-results/01-login-page-loaded.png | CREATED | Screenshot evidence of login page functionality | Test executor login testing | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /test-results/02-login-form-filled.png | CREATED | Screenshot evidence of login form with credentials filled | Test executor login testing | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /test-results/03-after-login-submission.png | CREATED | Screenshot evidence of successful login redirect to dashboard | Test executor login testing | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /test-results/04-events-page-loaded.png | CREATED | Screenshot evidence of events page displaying all 10 events correctly | Test executor events testing | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /test-results/06-journey-after-login.png | CREATED | Screenshot evidence of user journey - post login state | Test executor journey testing | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /test-results/07-journey-events-page.png | CREATED | Screenshot evidence of complete user journey - final events page | Test executor journey testing | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /tests/playwright/events-diagnostic.spec.ts | CREATED | Focused diagnostic test to capture actual events page behavior and API connectivity issues | Test executor events diagnosis | ACTIVE | Keep permanent |
| 2025-09-10 | /docs/functional-areas/events/handoffs/test-executor-2025-09-10-events-diagnosis.md | CREATED | Critical handoff documenting that events are NOT displaying due to API connection issues, with visual evidence | Test executor events diagnosis | ACTIVE | Keep permanent |
| 2025-09-10 | /tests/playwright/dashboard.spec.ts | MODIFIED | CRITICAL FIX: Added mandatory JavaScript and console error monitoring to catch RangeError crashes | Test developer dashboard error detection | ACTIVE | Keep permanent |
| 2025-09-10 | /docs/standards-processes/testing/TEST_CATALOG.md | MODIFIED | Documented critical fix for E2E test false positive (test passed while dashboard crashed with RangeError) | Test developer error detection fix | ACTIVE | Keep permanent |
| 2025-09-10 | /docs/lessons-learned/test-developer-lessons-learned.md | MODIFIED | Added critical lesson about E2E tests without error monitoring being worse than no tests | Test developer lessons learned | ACTIVE | Keep permanent |
| 2025-09-10 | /tests/run-dashboard-error-test.sh | CREATED | Verification script to test improved dashboard error detection | Test developer verification script | ACTIVE | Keep permanent |
| 2025-09-10 | /test-results/events-page-actual-diagnosis.png | CREATED | Screenshot evidence showing loading skeleton state when events fail to load | Test executor events diagnosis | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /test-results/events-page-after-wait.png | CREATED | Screenshot evidence showing error message "Failed to Load Events" that users actually see | Test executor events diagnosis | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /test-results/events-main-content.png | CREATED | Screenshot of main content area detail for diagnostic analysis | Test executor events diagnosis | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /apps/web/src/components/dashboard/EventsWidget.tsx | MODIFIED | Fixed critical RangeError: Invalid time value when formatting null/undefined dates from API responses | React developer date handling fix | ACTIVE | Keep permanent |
| 2025-09-10 | /docs/lessons-learned/react-developer-lessons-learned.md | MODIFIED | Added critical lesson on safe date handling from API data to prevent runtime crashes | React developer lessons documentation | ACTIVE | Keep permanent |
| 2025-09-10 | /session-work/2025-09-10/date-handling-test.js | CREATED | Test script to verify date handling fix works for all problematic scenarios | React developer testing | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /docs/design/wireframes/navigation-logged-in-mockup.html | CREATED | HTML mockup showing navigation changes for logged-in users with visual comparison of current vs new states | UI Designer navigation mockup | ACTIVE | Keep permanent |

## File Management Guidelines

### File Status Definitions
- **ACTIVE**: Permanently useful files that should be kept
- **TEMPORARY**: Files that can be cleaned up after the specified date  
- **ARCHIVED**: Files moved to archive location
- **DELETED**: Files that have been removed

### Cleanup Schedule
- **Weekly**: Review TEMPORARY files and clean up expired ones
- **Monthly**: Review ACTIVE files for continued relevance
- **Quarterly**: Archive old but useful files

### File Categories
- **Tests**: Keep permanently (ACTIVE)
- **Documentation**: Keep permanently (ACTIVE)  
- **Screenshots**: Clean up after 1 week (TEMPORARY)
- **Reports**: Keep permanently (ACTIVE)
- **Diagnostic Output**: Clean up after 1 week (TEMPORARY)

---

*Last Updated: 2025-09-10 by UI Designer Agent*