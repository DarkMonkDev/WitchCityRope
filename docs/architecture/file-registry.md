# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
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
- Clean up temporary test files after verification complete
- Remove debug scripts once issues resolved

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
Files moved to archive folders but kept for historical reference.

---

**Last Updated**: 2025-09-19
**Maintained By**: All Claude agents
**Review Schedule**: Weekly