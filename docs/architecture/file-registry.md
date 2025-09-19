# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-09-19 | /tests/playwright/events-admin-add-buttons-verification.spec.ts | CREATED | Comprehensive E2E test for verifying Add buttons fixes | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/focused-add-buttons-test.spec.ts | CREATED | Focused test for Add buttons with error monitoring | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/complete-add-buttons-verification.spec.ts | CREATED | Complete verification test for all three Add buttons | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/volunteers-tab-test.spec.ts | CREATED | Specific test for Volunteers tab and Add New Position button | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/test-add-new-position-button.spec.ts | CREATED | Final verification test for Add New Position modal functionality | Events admin Add buttons verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /test-results/events-admin-add-buttons-verification-report.md | CREATED | Comprehensive verification report documenting all Add buttons fixes are working | Events admin Add buttons verification | ACTIVE | - |
| 2025-09-19 | /apps/web/src/components/events/SessionFormModal.tsx | MODIFIED | Fixed undefined property errors in Add Session modal | Events admin page bug fixes | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/events/TicketTypeFormModal.tsx | MODIFIED | Fixed undefined property errors in Add Ticket Type modal | Events admin page bug fixes | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/events/VolunteerPositionFormModal.tsx | MODIFIED | Fixed undefined property errors in Add Volunteer Position modal | Events admin page bug fixes | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/events/EventForm.tsx | MODIFIED | Added safety checks for undefined arrays in modal props | Events admin page bug fixes | ACTIVE | N/A |
| 2025-09-19 | /test-add-buttons.js | DELETED | Test script to verify Add button functionality (cleaned up) | Events admin page testing | CLEANED | 2025-09-19 |
| 2025-01-19 | /apps/api/Program.cs | MODIFIED | Fixed logout middleware conflict - removed simple middleware that intercepted logout requests | Logout authorization fix | ACTIVE | - |
| 2025-01-19 | /docs/lessons-learned/backend-developer-lessons-learned.md | MODIFIED | Updated logout conflict from URGENT to RESOLVED with solution details | Logout authorization fix | ACTIVE | - |
| 2025-09-19 | /scripts/debug/teacher-selection-debug.js | CREATED | Teacher selection debugging script | Teacher selection issue investigation | TEMPORARY | 2025-09-25 |
| 2025-09-19 | /apps/api/Features/Users/Models/UserOptionDto.cs | CREATED | DTO for user dropdown options | Teacher selection API endpoint | ACTIVE | - |
| 2025-09-19 | /apps/api/Features/Users/Endpoints/UserEndpoints.cs | MODIFIED | Added GetUsersByRole endpoint | Teacher selection API endpoint | ACTIVE | - |
| 2025-09-19 | /apps/api/Features/Users/Services/UserManagementService.cs | MODIFIED | Added GetUsersByRoleAsync method | Teacher selection API endpoint | ACTIVE | - |
| 2025-09-19 | /apps/web/src/lib/api/hooks/useTeachers.ts | CREATED | React hook for fetching teachers | Teacher selection with API integration | ACTIVE | - |
| 2025-09-19 | /apps/web/src/lib/api/hooks/useTeachers.ts | MODIFIED | Fixed import path from '../apiClient' to '../client' | Fix critical import error crashing app | ACTIVE | - |
| 2025-09-19 | /apps/web/src/components/events/EventForm.tsx | MODIFIED | Added real teacher API integration with fallback | Teacher selection bug fix | ACTIVE | - |
| 2025-09-19 | `/dev.sh` | MODIFIED | Fixed Docker Compose command to include PostgreSQL database in development environment | Teacher selection persistence bug fix | ACTIVE | N/A |
| 2025-09-19 | /test-results/events-admin-persistence-test-suite.js | CREATED | Comprehensive Playwright test suite for verifying events admin persistence fixes | Events admin persistence testing | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /tests/playwright/events-admin-persistence-verification.spec.ts | CREATED | Manual verification test suite for events admin persistence | Events admin persistence verification | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /test-results/playwright-bypass.config.ts | CREATED | Bypass configuration for Playwright to avoid environment checks | Testing infrastructure workaround | TEMPORARY | 2025-09-26 |
| 2025-09-19 | /test-results/events-admin-persistence-verification-report.md | CREATED | Comprehensive test execution report for events admin persistence fixes | Test execution documentation | ACTIVE | - |

## Cleanup Schedule

### Immediate (Next 7 Days)
- Clean up temporary test files after verification complete (2025-09-26)
- Remove debug scripts once issues resolved
- Archive Playwright test files created for specific bug verification

### Monthly Review
- Review all TEMPORARY files for cleanup
- Archive completed session work
- Update status of ACTIVE files

## File Categories

### ACTIVE
Files that are part of the permanent codebase and should be maintained.

### TEMPORARY
Files created for debugging, testing, or analysis that should be cleaned up after use.

### ARCHIVED
Files that were active but are no longer needed and have been moved to archive.

### CLEANED
Files that have been successfully removed from the project.

## Notes

- All test files created for bug verification should be marked TEMPORARY
- Verification reports should be marked ACTIVE as they serve as documentation
- Component fixes and API modifications are ACTIVE permanent changes
- Debug scripts should be cleaned up after issues are resolved