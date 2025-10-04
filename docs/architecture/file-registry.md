# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-10-04 | `/docs/functional-areas/vetting-system/new-work/2025-10-04-how-to-join-conditional-visibility/design/ui-design.md` | CREATED | Comprehensive UI design specifications with ASCII wireframes, status box variants, Mantine component mappings, responsive layouts, and accessibility requirements for conditional "How to Join" menu visibility | Conditional menu visibility UI design | ACTIVE | - |
| 2025-10-04 | `/docs/functional-areas/vetting-system/new-work/2025-10-04-how-to-join-conditional-visibility/technical/functional-spec.md` | CREATED | Comprehensive technical functional specification for conditional "How to Join" menu visibility feature - React hooks, TanStack Query, component architecture, state management, and data flow | Conditional menu visibility functional specification | ACTIVE | - |
| 2025-10-04 | `/docs/functional-areas/vetting-system/new-work/2025-10-04-how-to-join-conditional-visibility/requirements/business-requirements.md` | CREATED | Comprehensive business requirements for conditional "How to Join" menu visibility based on vetting application status | Conditional menu visibility feature requirements | ACTIVE | - |
| 2025-09-23 | `/apps/web/src/pages/admin/AdminVettingApplicationDetailPage.tsx` | MODIFIED | Enhanced error handling and debugging for vetting application detail routing issue | Vetting routing fix | ACTIVE | - |
| 2025-09-23 | `/apps/web/src/features/admin/vetting/components/VettingApplicationDetail.tsx` | MODIFIED | Added comprehensive error handling and debugging logs for API failures | Vetting routing fix | ACTIVE | - |
| 2025-09-23 | `/apps/web/src/routes/loaders/authLoader.ts` | MODIFIED | Enhanced authentication loader with debugging and better error handling | Vetting routing fix | ACTIVE | - |
| 2025-09-23 | `/apps/web/src/features/admin/vetting/services/vettingAdminApi.ts` | MODIFIED | Added detailed error handling and logging to vetting API service | Vetting routing fix | ACTIVE | - |
| 2025-09-23 | `/docs/lessons-learned/react-developer-lessons-learned.md` | MODIFIED | Added critical routing debugging pattern lesson | Vetting routing fix | ACTIVE | - |
| 2025-09-23 | /test-results/vetting-system-verification-report-2025-09-23.md | CREATED | Comprehensive verification report of vetting system functionality after recent fixes | Vetting System Verification | ACTIVE | N/A |
| 2025-09-23 | /test-results/app-state.png | CREATED | Screenshot of React app state for verification | Vetting System Verification | TEMPORARY | 2025-09-30 |
| 2025-09-23 | /test-results/vetting-page.png | CREATED | Screenshot of vetting page with burgundy headers | Vetting System Verification | TEMPORARY | 2025-09-30 |
| 2025-09-23 | /test-results/put-on-hold-modal.png | CREATED | Screenshot of PUT ON HOLD modal functionality | Vetting System Verification | TEMPORARY | 2025-09-30 |
| 2025-09-23 | /test-results/send-reminder-modal.png | CREATED | Screenshot of SEND REMINDER modal functionality | Vetting System Verification | TEMPORARY | 2025-09-30 |
| 2025-09-23 | `/apps/web/src/lib/api/hooks/useEventParticipations.ts` | MODIFIED | Added missing `metadata?: string \| null` property to EventParticipationDto interface | DTO alignment fixes - critical E2E blocking errors | ACTIVE | - |
| 2025-09-23 | `/apps/web/src/features/admin/vetting/types/vetting.types.ts` | MODIFIED | Fixed PagedResult interface: changed `pageNumber` to `page` and added `hasPreviousPage`, `hasNextPage` properties | DTO alignment fixes - critical E2E blocking errors | ACTIVE | - |
| 2025-09-23 | `/apps/web/src/features/admin/vetting/hooks/useVettingApplications.ts` | MODIFIED | Updated empty result object to use `page` instead of `pageNumber` and added missing pagination properties | DTO alignment fixes - critical E2E blocking errors | ACTIVE | - |
| 2025-09-23 | `/apps/web/src/features/admin/vetting/components/__tests__/VettingApplicationsList.test.tsx` | MODIFIED | Fixed all mock objects to include missing properties: `hasPreviousPage`, `hasNextPage`, `isError`, `isSuccess` | DTO alignment fixes - critical E2E blocking errors | ACTIVE | - |
| 2025-09-23 | /test-results/select-all-state.png | CREATED | Screenshot of select-all checkbox functionality | Vetting System Verification | TEMPORARY | 2025-09-30 |
| 2025-09-23 | /tests/playwright/vetting-verification.spec.js | CREATED + DELETED | Temporary test file for vetting verification | Vetting System Verification | CLEANED_UP | N/A |
| 2025-09-23 | /tests/playwright/quick-app-check.spec.js | CREATED + DELETED | Temporary test file for React app health check | Vetting System Verification | CLEANED_UP | N/A |
| 2025-09-23 | /tests/playwright/vetting-system-verification.spec.js | CREATED + DELETED | Temporary test file for comprehensive vetting verification | Vetting System Verification | CLEANED_UP | N/A |
| 2025-09-23 | /tests/playwright/test-bulk-actions.spec.js | CREATED + DELETED | Temporary test file for bulk action modal testing | Vetting System Verification | CLEANED_UP | N/A |
| 2025-09-23 | /docs/lessons-learned/backend-developer-lessons-learned.md | CRITICAL_FIX | URGENT: Fixed conflicting file size limits (500 lines → 1700 lines) to match validation checklist standard | File Size Limit Standardization Emergency Fix | ACTIVE | Never |
| 2025-09-23 | /docs/lessons-learned/test-developer-lessons-learned.md | CRITICAL_FIX | URGENT: Fixed conflicting file size limits (500 lines → 1700 lines) to match validation checklist standard | File Size Limit Standardization Emergency Fix | ACTIVE | Never |
| 2025-09-23 | /docs/lessons-learned/react-developer-lessons-learned.md | CRITICAL_FIX | URGENT: Fixed conflicting file size limits (500 lines → 1700 lines) to match validation checklist standard | File Size Limit Standardization Emergency Fix | ACTIVE | Never |
| 2025-09-23 | /docs/lessons-learned/react-developer-lessons-learned-part-2.md | CRITICAL_FIX | URGENT: Fixed conflicting file size limits (500 lines → 1700 lines) to match validation checklist standard | File Size Limit Standardization Emergency Fix | ACTIVE | Never |
| 2025-09-23 | /docs/lessons-learned/librarian-lessons-learned.md | CRITICAL_FIX | URGENT: Fixed conflicting file size limits + added standardization lesson learned | File Size Limit Standardization Emergency Fix | ACTIVE | Never |
| 2025-09-23 | /docs/lessons-learned/backend-developer-lessons-learned.md | CRITICAL_FIX | Emergency file split - reduced from 2106 to 217 lines for agent readability | Backend Developer File Split Emergency Fix | ACTIVE | Never |
| 2025-09-23 | /docs/lessons-learned/librarian-lessons-learned.md | MODIFIED | Added emergency fix documentation for backend developer file split | Backend Developer File Split Emergency Fix | ACTIVE | Never |
| 2025-09-22 | `/docs/standards-processes/testing/TEST_CATALOG.md` | MODIFIED | CRITICAL: Split 2772-line file into manageable parts (336 lines) for agent accessibility | Librarian - TEST_CATALOG Split Implementation | ACTIVE | Never |
| 2025-09-22 | `/docs/standards-processes/testing/TEST_CATALOG_PART_2.md` | CREATED | Historical test documentation and migration patterns (1513 lines) | Librarian - TEST_CATALOG Split Implementation | ACTIVE | Never |
| 2025-09-22 | `/docs/standards-processes/testing/TEST_CATALOG_PART_3.md` | CREATED | Archived test migration analysis and legacy information (1048 lines) | Librarian - TEST_CATALOG Split Implementation | ACTIVE | Never |
| 2025-09-22 | `/docs/lessons-learned/test-developer-lessons-learned.md` | MODIFIED | Updated TEST_CATALOG references to split structure and added maintenance guidance | Librarian - TEST_CATALOG Split Implementation | ACTIVE | Never |
| 2025-09-22 | `/docs/lessons-learned/test-executor-lessons-learned.md` | MODIFIED | Updated TEST_CATALOG references to split structure and added accessibility lesson | Librarian - TEST_CATALOG Split Implementation | ACTIVE | Never |
| 2025-09-22 | `/apps/web/src/features/admin/vetting/components/VettingApplicationDetail.tsx` | MODIFIED | CRITICAL FIX: Removed Send Reminder button, added Deny modal functionality, fixed Save Note button with gold styling and API integration | Vetting UI fixes | ACTIVE | N/A |
| 2025-09-22 | `/apps/web/src/features/admin/vetting/components/DenyApplicationModal.tsx` | CREATED | New modal component for denying applications with reason text area | Vetting UI fixes | ACTIVE | N/A |
| 2025-09-22 | `/apps/web/src/features/admin/vetting/components/VettingApplicationsList.tsx` | MODIFIED | Fixed checkbox clicking to not navigate to detail page | Vetting UI fixes | ACTIVE | N/A |
| 2025-09-22 | `/apps/web/src/features/admin/vetting/components/VettingStatusBadge.tsx` | MODIFIED | Updated status badge colors to exact specified colors | Vetting UI fixes | ACTIVE | N/A |
| 2025-09-22 | `/apps/web/src/features/admin/vetting/services/vettingAdminApi.ts` | MODIFIED | Added denyApplication method and implemented addApplicationNote API call | Vetting UI fixes | ACTIVE | N/A |
| 2025-09-22 | `/apps/web/src/features/admin/vetting/components/VettingApplicationsList.tsx` | MODIFIED | Fixed APPLICATION DATE header alignment to center | Vetting application list header alignment fix | ACTIVE | N/A |
| 2025-09-22 | `/session-work/2025-09-22/vetting-application-detail-fixes.md` | CREATED | Documentation of all UX fixes applied to vetting application detail page | Vetting application detail fixes documentation | ACTIVE | N/A |
| 2025-09-22 | /docs/lessons-learned/react-developer-lessons-learned.md | MODIFIED | CRITICAL: Moved MANDATORY STARTUP PROCEDURE from Part 2 to TOP of Part 1 | Agent lessons reorganization | ACTIVE | Never |
| 2025-09-23 | /apps/web/src/pages/admin/AdminVettingPage.tsx | MODIFIED | Send Reminder button fix: Removed disabled condition, removed selection count, updated modal rendering | Send Reminder wireframe compliance | ACTIVE | N/A |
| 2025-09-23 | /apps/web/src/features/admin/vetting/components/SendReminderModal.tsx | MODIFIED | Removed email chips and emails state, simplified to just subject/message fields | Send Reminder wireframe compliance | ACTIVE | N/A |
| 2025-09-23 | /docs/lessons-learned/react-developer-lessons-learned.md | MODIFIED | Added lesson: Follow wireframe specifications exactly | Send Reminder wireframe compliance | ACTIVE | Never |
| 2025-09-20 | /session-work/2025-09-20/playwright-debugging-screenshots/ | CREATED | Directory for Playwright debugging screenshots from async/race condition testing | Playwright async test debugging | TEMPORARY | 2025-09-27 |
| 2025-09-20 | /session-work/2025-09-20/PLAYWRIGHT_ASYNC_DEBUGGING.md | CREATED | Documentation of async/race condition debugging findings for RSVP system | Playwright async test debugging | ACTIVE | - |
| 2025-09-20 | /apps/web/tests/playwright/features/events/rsvp-ticketing-comprehensive.spec.ts | MODIFIED | Added retry logic and better wait conditions for async operations | Playwright async test fixes | ACTIVE | - |
| 2025-09-20 | /docs/lessons-learned/test-developer-lessons-learned.md | MODIFIED | Added async operation testing patterns and Playwright best practices | Playwright async test debugging | ACTIVE | Never |
| 2025-09-20 | /docs/functional-areas/payments/new-work/2025-01-19-rsvp-ticketing/testing/test-execution-report-2025-09-20.md | CREATED | Comprehensive test execution report showing 90% functional implementation with minor API integration issues | RSVP/Ticketing test execution | ACTIVE | - |
| 2025-09-14 | /docs/functional-areas/payment-paypal-venmo/new-work/2025-09-14-paypal-webhook-integration/testing/webhook-validation-report.md | CREATED | Comprehensive validation report for PayPal webhook integration showing HTTP 200 responses and successful event processing | PayPal webhook integration testing | ACTIVE | - |
| 2025-09-14 | /apps/api/Features/Payments/Models/PayPalWebhookEvent.cs | CREATED | PayPal webhook event model with strongly-typed event structure and JSON deserialization support | PayPal webhook integration | ACTIVE | - |
| 2025-09-14 | /apps/api/Features/Payments/Extensions/JsonElementExtensions.cs | CREATED | Extension methods for safe JsonElement value extraction with null handling | PayPal webhook integration | ACTIVE | - |
| 2025-09-14 | /apps/api/Features/Payments/Endpoints/PayPalEndpoints.cs | MODIFIED | Implemented webhook endpoint with comprehensive PayPal event processing logic | PayPal webhook integration | ACTIVE | - |
| 2025-09-14 | /apps/api/Services/MockPayPalService.cs | CREATED | Mock PayPal service for CI/CD environments to bypass real webhook validation | PayPal webhook integration | ACTIVE | - |
| 2025-09-13 | /docs/functional-areas/api-cleanup/migration-completion-report.md | CREATED | Documentation of successful legacy API migration and archive process | API cleanup completion | ACTIVE | - |
| 2025-09-13 | /src/_archive/WitchCityRope.Api.Legacy/README.md | CREATED | Archive notice explaining legacy API migration to modern API | API cleanup | ACTIVE | - |
| 2025-09-13 | Multiple files in /src/_archive/ | MOVED | Legacy API projects archived after successful feature migration | API cleanup | ARCHIVED | - |
| 2025-09-12 | /docs/functional-areas/testing-infrastructure/new-work/2025-09-12-containerized-testing/requirements/research-findings.md | CREATED | Comprehensive research on containerized testing infrastructure with Docker, TestContainers, and GitHub Actions | Testing infrastructure research | ACTIVE | - |
| 2025-09-11 | /docs/functional-areas/navigation/implementation-complete-report.md | CREATED | Documentation of completed navigation updates with dashboard button and admin link | Navigation completion | ACTIVE | - |
| 2025-09-11 | Multiple navigation component files | MODIFIED | Implemented logged-in user navigation with dashboard button, admin link, and user greeting | Navigation updates | ACTIVE | - |
| 2025-08-24 | /docs/functional-areas/events/new-work/2025-08-24-events-management/requirements/business-requirements.md | CREATED | Business requirements for Events Management React migration from Blazor | Events Management migration | ACTIVE | - |
| 2025-08-22 | /docs/functional-areas/api-architecture-modernization/new-work/2025-08-22-minimal-api-research/MIGRATION-COMPLETION-SUMMARY.md | CREATED | Comprehensive completion summary of API architecture modernization with 49ms response times and zero breaking changes | API architecture migration | ACTIVE | - |
| 2025-08-20 | /docs/functional-areas/design-refresh/new-work/2025-08-20-modernization/requirements/business-requirements.md | CREATED | Business requirements for design refresh modernization with edgy/modern aesthetic | Design refresh | ACTIVE | - |
| 2025-08-19 | Multiple authentication files | MODIFIED/CREATED | NSwag type generation implementation with automated TypeScript interface generation | Authentication NSwag migration | ACTIVE | - |
| 2025-08-17 | /docs/architecture/react-migration/adrs/ADR-004-mantine-ui-framework.md | CREATED | Architecture decision record selecting Mantine v7 UI framework (89/100 score) | Technology research | ACTIVE | - |

## Maintenance Guidelines

### File Lifecycle
1. **ACTIVE**: Current file in use
2. **TEMPORARY**: Scheduled for cleanup (add cleanup date)
3. **ARCHIVED**: Moved to `/docs/_archive/` or `/session-work/archive/`
4. **CLEANED_UP**: File deleted, logged for historical reference

### Cleanup Process
- Review TEMPORARY files weekly
- Archive completed session-work monthly
- Never delete file registry entries (maintain history)
- Update status when files are archived or deleted

### Required Information
Every entry MUST include:
- Date (YYYY-MM-DD format)
- Full absolute file path
- Action (CREATED/MODIFIED/DELETED/MOVED)
- Purpose (why this file exists)
- Session/Task identifier
- Status (ACTIVE/TEMPORARY/ARCHIVED/CLEANED_UP)
- Cleanup date (if TEMPORARY)

## Archive History
