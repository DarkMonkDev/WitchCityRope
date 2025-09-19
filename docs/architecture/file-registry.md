# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
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
| 2025-09-18 | /tests/playwright/simple-dashboard-check.spec.ts | MODIFIED | Enhanced existing test with API health checks and error monitoring | Test improvement | ACTIVE | - |
| 2025-09-18 | /docs/standards-processes/testing/TEST_CATALOG.md | MODIFIED | Added new critical navigation tests to catalog | Test documentation | ACTIVE | - |
| 2025-09-18 | /docs/lessons-learned/test-developer-lessons-learned.md | MODIFIED | Added critical lesson about E2E error monitoring requirements | Knowledge management | ACTIVE | - |
| 2025-09-18 | /tests/playwright/navigation-verification.spec.ts | CREATED | E2E test for navigation verification after API fix | Navigation testing post-fix | EVIDENCE | 2026-03-18 |
| 2025-09-18 | /tests/playwright/navigation-verification-updated.spec.ts | CREATED | Updated navigation verification test with better selectors | Navigation testing refinement | EVIDENCE | 2026-03-18 |
| 2025-09-18 | /tests/playwright/simple-navigation-check.spec.ts | CREATED | Simple test to verify basic app functionality | Basic functionality verification | EVIDENCE | 2026-03-18 |
| 2025-09-18 | /docs/functional-areas/testing/2025-09-18-navigation-verification.md | CREATED | Navigation verification report after API fix | Test execution documentation | ACTIVE | - |
| 2025-09-18 | /docs/functional-areas/testing/2025-09-18-test-suite-analysis.md | CREATED | Comprehensive test suite analysis with failure categorization | Test execution analysis and development guidance | ACTIVE | - |
| 2025-09-18 | /apps/web/src/contexts/AuthContext.tsx | MODIFIED | Fixed logout bug by clearing Zustand store sessionStorage persistence | React Developer - Logout Bug Fix | ACTIVE | - |

## File Categories

### ACTIVE
Files currently in use and relevant to ongoing development.

### EVIDENCE
Test files and artifacts that serve as evidence but can be cleaned up after verification period.

### ARCHIVED
Historical files moved to archive but kept for reference.

### TEMPORARY
Files created for immediate use that should be cleaned up quickly.

## Cleanup Schedule

### Evidence Files (6 months retention)
- Test specification files: Review and clean up after 6 months
- Test result artifacts: Clean up after 3 months
- Debug screenshots and videos: Clean up after 1 month

### Archive Files (2 years retention)
- Historical documentation: Review annually
- Deprecated standards: Keep for reference but mark clearly

### Temporary Files (1 week retention)
- Debug files: Clean up weekly
- Scratch documents: Clean up at session end
- Draft documents: Convert to active or delete

## Usage Guidelines

### When Creating Files
1. **Always log in this registry** - No exceptions
2. **Use descriptive purposes** - Future developers need context
3. **Choose appropriate status** - Helps with cleanup automation
4. **Set cleanup dates** - Prevents accumulation of stale files

### When Modifying Files
1. **Log modifications** - Track significant changes
2. **Update status if needed** - Files may change category
3. **Adjust cleanup dates** - Modified files may need longer retention

### When Deleting Files
1. **Log deletions** - Track what was removed and why
2. **Archive important content** - Don't lose valuable information
3. **Update references** - Fix broken links in other documents

## File Organization Standards

### Documentation Structure
- `/docs/functional-areas/[area]/` - Feature-specific docs
- `/docs/standards-processes/` - Project standards
- `/docs/architecture/` - Architectural decisions
- `/docs/lessons-learned/` - Knowledge from experience

### Test Structure
- `/tests/unit/` - Unit test files
- `/tests/integration/` - Integration test files
- `/tests/playwright/` - E2E test files
- `/test-results/` - Test execution artifacts

### Development Artifacts
- `/session-work/YYYY-MM-DD/` - Daily work files
- `/scripts/` - Utility scripts
- `/tools/` - Development tools

---

**Last Updated**: 2025-09-18
**Maintained By**: All Claude agents (mandatory)
**Review Schedule**: Monthly cleanup, quarterly deep review