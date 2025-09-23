# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
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
| 2025-09-23 | /apps/web/src/features/admin/vetting/components/SendReminderModal.tsx | MODIFIED | Complete rewrite: Added application selection UI, checkboxes, backwards compatibility, per wireframes | Send Reminder wireframe compliance | ACTIVE | N/A |
| 2025-09-23 | /home/chad/repos/witchcityrope-react/PROGRESS.md | MODIFIED | Added September 23 session summary: vetting fixes, test improvements, authentication issues | End-of-session cleanup | ACTIVE | Never |
| 2025-09-23 | /session-work/2025-09-23/lessons-learned-cleanup-report.md | ACTIVE | Documentation of lessons learned file cleanup and standardization | Session documentation | TEMPORARY | 2025-09-30 |
| 2025-09-23 | /session-work/2025-09-23/send-reminder-wireframe-fix.md | ACTIVE | Documentation of Send Reminder modal wireframe compliance fixes | Session documentation | TEMPORARY | 2025-09-30 |
| 2025-09-23 | /session-work/2025-09-23/vetting-seed-data-enhancement.md | ACTIVE | Documentation of test database seeding and infrastructure improvements | Session documentation | TEMPORARY | 2025-09-30 |
| 2025-09-23 | /docs/functional-areas/vetting-system/handoffs/2025-09-23-session-handoff.md | CREATED | Session handoff documenting vetting fixes, test improvements, and remaining authentication issues | End-of-session handoff | ACTIVE | Never |
| 2025-09-23 | test-results/vetting-system-* | DELETED | Multiple test result directories and videos cleaned up | Test cleanup | CLEANED_UP | N/A |
| 2025-09-23 | tests/e2e/test-results/vetting-* | DELETED | E2E test result files and screenshots cleaned up | Test cleanup | CLEANED_UP | N/A |
| 2025-09-23 | Multiple .txt result files | TO_DELETE | Temporary test result files (api-unit-test-results.txt, etc.) | Test cleanup | TEMPORARY | 2025-09-23 |