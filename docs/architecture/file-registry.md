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
| 2025-09-19 | `/apps/web/src/pages/admin/AdminEventDetailsPage.tsx` | MODIFIED | Added debug logging for teacher selection (temporary) | Teacher selection persistence debugging | TEMPORARY | Should remove debug logs |
| 2025-09-19 | `/apps/web/src/components/events/EventForm.tsx` | MODIFIED | Added debug logging for form submission (temporary) | Teacher selection persistence debugging | TEMPORARY | Should remove debug logs |
| 2025-09-19 | `/apps/web/src/utils/eventDataTransformation.ts` | MODIFIED | Added debug logging for data transformation (temporary) | Teacher selection persistence debugging | TEMPORARY | Should remove debug logs |
| 2025-01-18 | /package.json | MODIFIED | Disabled npm run dev script to prevent local dev servers | Docker-only development fix | ACTIVE | Never |
| 2025-01-18 | /apps/web/package.json | MODIFIED | Disabled npm run dev script, added dev:docker-only script | Docker-only development fix | ACTIVE | Never |
| 2025-01-18 | /apps/web/vite.config.ts | MODIFIED | Set strictPort: true to enforce port 5173 | Docker-only development fix | ACTIVE | Never |
| 2025-01-18 | /docker-compose.dev.yml | MODIFIED | Updated command to use dev:docker-only script | Docker-only development fix | ACTIVE | Never |
| 2025-01-18 | /scripts/kill-local-dev-servers.sh | CREATED | Script to kill local Node/npm processes that conflict with Docker | Docker-only development fix | ACTIVE | Never |
| 2025-01-18 | /tests/e2e/global-setup.ts | MODIFIED | Enhanced to detect and prevent local dev server conflicts | Docker-only development fix | ACTIVE | Never |
| 2025-01-18 | /playwright.config.ts | MODIFIED | Added Docker-only enforcement comments | Docker-only development fix | ACTIVE | Never |
| 2025-01-18 | /DOCKER_ONLY_DEVELOPMENT.md | CREATED | Comprehensive documentation of Docker-only development approach | Docker-only development fix | ACTIVE | Never |
| 2025-01-18 | /CLAUDE.md | MODIFIED | Updated to reference Docker-only development and renumbered sections | Docker-only development fix | ACTIVE | Never |
| 2025-09-19 | /apps/api/Services/ITokenBlacklistService.cs | CREATED | Interface for JWT token blacklisting to fix logout security vulnerability | Backend Developer - Logout Fix | ACTIVE | N/A |
| 2025-09-19 | /apps/api/Enums/EventType.cs | MODIFIED | Changed Workshop to Class enum value to fix admin events filter bug | React Developer - Events Filter Fix | ACTIVE | N/A |
| 2025-09-19 | /apps/api/Services/SeedDataService.cs | MODIFIED | Updated all EventType.Workshop references to EventType.Class to fix filter | React Developer - Events Filter Fix | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/events/EventForm.tsx | MODIFIED | Implemented environment-aware TinyMCE with Textarea fallback to prevent API usage costs in development | React Developer - TinyMCE Cost Control | ACTIVE | N/A |
| 2025-09-19 | /apps/web/.env.development | MODIFIED | Removed/commented out VITE_TINYMCE_API_KEY to disable TinyMCE in development environment | React Developer - TinyMCE Cost Control | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/forms/TinyMCERichTextEditor.tsx | MODIFIED | Updated to use environment variable and fallback to Textarea when no API key | React Developer - TinyMCE Cost Control | ACTIVE | N/A |
| 2025-09-19 | /apps/web/src/components/forms/SimpleTinyMCE.tsx | MODIFIED | Removed hardcoded API key and implemented environment-aware fallback | React Developer - TinyMCE Cost Control | ACTIVE | N/A |
| 2025-09-19 | /docs/lessons-learned/react-developer-lessons-learned.md | MODIFIED | Added critical TinyMCE development cost prevention pattern | React Developer - TinyMCE Cost Control | ACTIVE | N/A |
| 2025-09-19 | /tests/playwright/admin-event-editing-comprehensive.spec.ts | CREATED | Comprehensive E2E test suite for admin event editing critical issues | Test Developer - Admin Event E2E | ACTIVE | 2026-01-01 |
| 2025-09-19 | /tests/playwright/admin-event-editing-focused.spec.ts | CREATED | Streamlined E2E test for 4 critical admin event editing issues | Test Developer - Admin Event E2E | ACTIVE | 2026-01-01 |
| 2025-09-19 | /tests/playwright/admin-event-editing-quick-test.spec.ts | CREATED | Quick validation test for admin event editing infrastructure | Test Developer - Admin Event E2E | TEMPORARY | 2025-12-01 |
| 2025-09-19 | /docs/functional-areas/events/handoffs/test-developer-2025-09-19-handoff.md | CREATED | Handoff document for admin event editing E2E test results and findings | Test Developer - Admin Event E2E | ACTIVE | N/A |
| 2025-09-19 | /apps/api/Services/TokenBlacklistService.cs | CREATED | In-memory token blacklist implementation for logout security | Backend Developer - Logout Fix | ACTIVE | N/A |
| 2025-09-19 | /apps/api/Services/JwtService.cs | MODIFIED | Added blacklist checking to ValidateToken method and ExtractJti method | Backend Developer - Logout Fix | ACTIVE | N/A |
| 2025-09-19 | /apps/api/Services/IJwtService.cs | MODIFIED | Added ExtractJti method to interface for token blacklisting | Backend Developer - Logout Fix | ACTIVE | N/A |
| 2025-09-19 | /apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs | MODIFIED | Enhanced logout endpoint to blacklist tokens server-side | Backend Developer - Logout Fix | ACTIVE | N/A |
| 2025-09-19 | /apps/api/Program.cs | MODIFIED | Registered ITokenBlacklistService as singleton in DI container | Backend Developer - Logout Fix | ACTIVE | N/A |
| 2025-01-18 | /apps/api/Features/Authentication/Endpoints/AuthenticationEndpoints.cs | MODIFIED | Fixed httpOnly cookie deletion for logout functionality | Backend authentication fix | ACTIVE | - |
| 2025-09-19 | /tests/playwright/verify-fixes.spec.ts | CREATED | E2E tests to verify logout and teacher persistence issues | Test Executor - Issue Verification | TEMPORARY | 2025-09-25 |
| 2025-09-19 | /tests/playwright/debug-ui.spec.ts | CREATED | UI debugging test to analyze React app state | Test Executor - Issue Verification | TEMPORARY | 2025-09-25 |
| 2025-09-19 | /tests/playwright/login-investigation.spec.ts | CREATED | Detailed login flow investigation test | Test Executor - Issue Verification | TEMPORARY | 2025-09-25 |
| 2025-09-19 | /tests/playwright/actual-verification.spec.ts | CREATED | Behavior verification tests for logout and admin access | Test Executor - Issue Verification | TEMPORARY | 2025-09-25 |
| 2025-09-19 | /tests/playwright/final-verification.spec.ts | CREATED | Final confirmation tests for both reported issues | Test Executor - Issue Verification | TEMPORARY | 2025-09-25 |
| 2025-09-19 | /test-results/verification-report-2025-09-19.md | CREATED | Comprehensive test report confirming logout persistence bug and teacher selection not implemented | Test Executor - Issue Verification | ACTIVE | N/A |
| 2025-09-19 | /docs/functional-areas/events/admin/teacher-selection-investigation-2025-09-19.md | CREATED | Investigation notes for teacher selection feature | Authentication session | ACTIVE | 2025-09-20 |
| 2025-09-19 | /docs/functional-areas/authentication/handoffs/2025-09-19-logout-persistence-fix.md | CREATED | Authentication fix handoff documentation | Authentication bug fix | ACTIVE | Permanent |
| 2025-09-19 | /tests/e2e/auth-fix-simple-test.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |
| 2025-09-19 | /tests/playwright/logout-fix-verification-final.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |
| 2025-09-19 | /tests/playwright/simple-logout-test.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |
| 2025-09-19 | /tests/playwright/logout-fix-verification.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |
| 2025-09-19 | /apps/web/tests/playwright/auth-flow-improved.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |
| 2025-09-19 | /apps/web/tests/playwright/auth-fixed.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |
| 2025-09-19 | /apps/web/tests/playwright/auth-test-with-correct-selectors.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |
| 2025-09-19 | /apps/web/tests/playwright/working-auth-test.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |
| 2025-09-19 | /apps/web/tests/playwright/jwt-auth-comprehensive.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |
| 2025-09-19 | /apps/web/tests/playwright/real-api-authentication-test.spec.ts | DELETED | Temporary auth test | Authentication debugging | DELETED | N/A |

## File Status Key
- **ACTIVE**: Permanent file in production use
- **TEMPORARY**: Test/debug file to be removed by cleanup date
- **ARCHIVED**: Historical file for reference only

## Cleanup Schedule
- **2025-09-25**: Remove temporary verification test files
- **2025-12-01**: Remove temporary debugging scripts and logs
- **2026-01-01**: Archive old E2E test files if superseded

## Session Summary - 2025-09-19
- Fixed critical logout persistence bug through proper React Context implementation
- Implemented AuthProvider pattern with comprehensive documentation
- Added DO NOT CHANGE warnings to prevent regression
- Cleaned up 10 temporary test files created during debugging
- Created handoff documentation for next session on event details admin
- Stable checkpoint at commit 721050a

## Notes
- All test executor verification files are temporary for debugging purposes
- Main verification report is permanent for tracking confirmed issues
- Debug logging added to components should be removed after fixes