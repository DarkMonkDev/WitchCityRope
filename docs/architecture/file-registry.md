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
| 2025-01-18 | /docs/lessons-learned/backend-developer-lessons-learned.md | MODIFIED | Documented cookie deletion fix and prevention strategies | Knowledge capture | ACTIVE | - |
| 2025-01-13 | /docs/architecture/docs-structure-validator.sh | CREATED | Documentation structure validation | Architecture setup | ACTIVE | - |
| 2025-01-13 | /docs/standards-processes/ | CREATED | Project standards and processes directory | Documentation organization | ACTIVE | - |
| 2025-01-13 | /docs/guides-setup/ | CREATED | Setup and operational guides directory | Documentation organization | ACTIVE | - |
| 2025-01-13 | /docs/lessons-learned/ | CREATED | Lessons learned from all agents | Knowledge management | ACTIVE | - |
| 2025-01-13 | /docs/design/ | CREATED | Design documents and wireframes | Design documentation | ACTIVE | - |
| 2025-01-13 | /docs/_archive/ | CREATED | Archived and outdated documentation | Archive management | ACTIVE | - |
| 2025-01-13 | /docs/functional-areas/ | CREATED | Feature-specific documentation | Feature organization | ACTIVE | - |
| 2025-09-18 | /tests/playwright/final-verification-test.spec.ts | CREATED | E2E test for login and dashboard verification | Final testing verification | EVIDENCE | 2026-03-18 |
| 2025-09-18 | /tests/playwright/corrected-final-verification.spec.ts | CREATED | Corrected E2E test with proper selectors | Final testing verification | EVIDENCE | 2026-03-18 |
| 2025-09-18 | /test-results/ | CREATED | Directory for test execution artifacts | Testing evidence collection | EVIDENCE | 2026-03-18 |
| 2025-09-18 | /tests/playwright/specs/dashboard-navigation.spec.ts | CREATED | Critical E2E tests for dashboard navigation bug prevention | Navigation bug prevention | ACTIVE | - |
| 2025-09-18 | /tests/playwright/specs/admin-events-navigation.spec.ts | CREATED | Critical E2E tests for admin events navigation bug prevention | Navigation bug prevention | ACTIVE | - |
| 2025-09-18 | /tests/playwright/specs/test-analysis-summary.md | CREATED | Analysis and documentation of navigation bug prevention patterns | Testing documentation | ACTIVE | - |
| 2025-09-18 | /tests/playwright/CRITICAL_TESTS_SUMMARY.md | CREATED | Summary of critical E2E tests for navigation bug prevention | Testing documentation | ACTIVE | - |
| 2025-09-19 | /playwright-no-setup.config.ts | CREATED | Temporary Playwright config to bypass global setup false positive detection | Test Executor - Admin Event Testing | TEMPORARY | 2025-10-01 |
| 2025-09-19 | /test-results/admin-event-editing-test-results-2025-09-19.md | CREATED | Comprehensive test results report for admin event editing functionality | Test Executor - Admin Event Testing | ACTIVE | N/A |

## File Statistics
- **Total Files**: 37
- **Active Files**: 34
- **Temporary Files**: 2
- **Evidence Files**: 1

## Recent Activity (September 19, 2025)
- Created comprehensive admin event editing test results report
- Created temporary Playwright config to bypass global setup issue
- Documented test execution findings and evidence-based analysis

## Cleanup Schedule
- **2025-10-01**: Review temporary Playwright config file
- **2025-12-01**: Review quick test file for admin event editing
- **2026-01-01**: Review comprehensive admin event editing test files
- **2026-03-18**: Review archived test verification files

## Legend
- **ACTIVE**: File is part of ongoing project infrastructure
- **TEMPORARY**: File created for specific testing/debugging, requires cleanup
- **EVIDENCE**: File contains historical evidence/results, archived but retained