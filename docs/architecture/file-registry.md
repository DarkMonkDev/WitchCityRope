# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
> 
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Active File Registry

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-09-08 | /apps/web/tests/playwright/verify-event-fixes.spec.ts | CREATED | Comprehensive E2E test suite to verify all event-related fixes including real database integration, API endpoints, mock data removal, and CORS resolution | Event Fixes Verification - Test Executor | ACTIVE | 2025-10-08 |
| 2025-09-08 | /test-results/event-fixes-verification-report-2025-09-08.json | CREATED | Complete test execution report documenting successful verification of all event system fixes with 100% pass rate, visual evidence, and performance metrics | Event Fixes Verification - Test Executor | ACTIVE | 2025-10-08 |
| 2025-09-08 | /apps/web/src/lib/api/hooks/useEvents.ts | MODIFIED | Fixed API response structure mismatch - added transformation layer to map API fields (startDateTime→startDate, maxAttendees→capacity) and handle {events:[...]} response format | API Integration Fix - Mock Data Replacement | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/pages/events/EventsListPage.tsx | MODIFIED | Removed all mock event data (58 lines), updated to use real API data only, fixed pricing display to use calculated values | API Integration Fix - Mock Data Replacement | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/pages/events/EventDetailPage.tsx | MODIFIED | Complete rewrite to use real API data instead of mock data, added useEvent hook integration, loading states, error handling | API Integration Fix - Mock Data Replacement | ACTIVE | N/A |
| 2025-09-08 | /apps/web/vite.config.ts | MODIFIED | Fixed proxy target port from 5655 to 5653 to match running API server, enabling API calls through React dev server | API Integration Fix - Proxy Configuration | ACTIVE | N/A |
| 2025-09-08 | /API_INTEGRATION_FIX_SUMMARY.md | CREATED | Comprehensive documentation of the mock data replacement fix including root causes, solutions, field mapping, and verification tests | API Integration Fix - Documentation | ACTIVE | 2025-10-08 |
| 2025-09-08 | /apps/web/test-results/comprehensive-test-validation-2025-09-08.md | CREATED | Final comprehensive test validation report confirming all recent improvements are working correctly - authentication helpers, test IDs, Playwright config - test failures due to maintenance issues not broken functionality | Test Executor - Final Test Validation | ACTIVE | 2025-10-08 |
| 2025-09-08 | /docs/functional-areas/events/user-dashboard/phase5-requirements.md | CREATED | Comprehensive business requirements for Phase 5 User Dashboard & Member Features - includes member dashboard, profile management, event registration tracking, role-based features, and mobile experience | Business Requirements Agent - Phase 5 User Dashboard | ACTIVE | N/A |
| 2025-09-08 | /docs/functional-areas/events/user-dashboard/phase5-ui-design.md | CREATED | Comprehensive UI design specifications for Phase 5 User Dashboard with Mantine v7 components, role-based access patterns, responsive mobile design, and WitchCityRope Design System v7 integration | UI Designer Agent - Phase 5 User Dashboard | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/components/dashboard/DashboardCard.tsx | CREATED | Reusable dashboard card component following WitchCityRope Design System v7 with status indicators, actions, and hover effects | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/components/dashboard/EventsWidget.tsx | CREATED | Dashboard widget displaying upcoming events with real API integration using TanStack Query | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/components/dashboard/ProfileWidget.tsx | CREATED | Profile completion widget with progress tracking and user information display | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/components/dashboard/RegistrationHistory.tsx | CREATED | Registration history widget showing past and upcoming event registrations with status indicators | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/components/dashboard/MembershipWidget.tsx | CREATED | Membership status widget with role-based information, vetting progress, and membership benefits display | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/components/profile/ProfileForm.tsx | CREATED | Comprehensive multi-tab profile management form with personal info, privacy settings, and preferences using React Hook Form + Zod validation | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/pages/dashboard/RegistrationsPage.tsx | CREATED | Complete registration management page with filtering, search, cancellation, and responsive design for mobile and desktop views | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/pages/dashboard/DashboardPage.tsx | MODIFIED | Updated main dashboard to use new card-based widgets in 2x2 responsive grid layout with Events, Profile, Registration, and Membership widgets | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/types/user.types.ts | CREATED | TypeScript type definitions for user roles, profile management, membership status, registration status with strong typing for dashboard features | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/hooks/useDashboard.ts | CREATED | Custom React hook for dashboard data management with caching, loading states, and error handling using TanStack Query | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/hooks/useProfile.ts | CREATED | Custom React hook for profile management operations including updates, validation, and optimistic updates with proper error handling | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/hooks/useRegistrations.ts | CREATED | Custom React hook for event registration management with filtering, cancellation, and status tracking capabilities | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/src/styles/dashboard.module.css | CREATED | CSS module for dashboard-specific styles including card layouts, responsive grids, status indicators, and mobile optimizations | React Developer - Phase 5 Implementation | ACTIVE | N/A |
| 2025-09-08 | /apps/web/tests/playwright/dashboard-Phase5-validation.spec.ts | CREATED | Comprehensive Playwright E2E test suite for Phase 5 dashboard functionality including role-based access, widget interactions, and responsive design | Test Developer - Phase 5 Testing | ACTIVE | N/A |
| 2025-09-08 | /docs/functional-areas/events/user-dashboard/phase5-technical-implementation.md | CREATED | Technical implementation documentation for Phase 5 dashboard components including architecture patterns, state management, testing approach, and deployment considerations | React Developer - Phase 5 Documentation | ACTIVE | N/A |
| 2025-09-08 | /docs/functional-areas/events/user-dashboard/phase5-handoff-react-to-test.md | CREATED | Handoff document from React Developer to Test Developer containing implementation details, component structure, test scenarios, and validation requirements for Phase 5 dashboard | React Developer - Phase 5 Handoff | ACTIVE | N/A |
| 2025-09-08 | /docs/functional-areas/events/user-dashboard/phase5-acceptance-criteria-validation.md | CREATED | Detailed acceptance criteria validation document for Phase 5 User Dashboard with test scenarios, edge cases, performance requirements, and validation checklist | Test Developer - Phase 5 Validation | ACTIVE | N/A |
| 2025-09-08 | /docs/functional-areas/events/user-dashboard/phase5-deployment-readiness.md | CREATED | Deployment readiness checklist and production considerations for Phase 5 User Dashboard including performance validation, security review, and rollout strategy | Test Developer - Phase 5 Deployment | ACTIVE | N/A |
| 2025-09-08 | /docs/functional-areas/events/user-dashboard/phase5-post-deployment-monitoring.md | CREATED | Post-deployment monitoring and validation plan for Phase 5 User Dashboard including metrics tracking, user feedback collection, and performance monitoring | Test Developer - Phase 5 Monitoring | ACTIVE | N/A |
| 2025-01-09 | /SESSION-HANDOFF-2025-01-09.md | CREATED | Comprehensive session handoff documentation for perfect continuity - includes complete session summary, all fixes applied, current system status, environment configuration, test credentials, and continuation instructions | Librarian - Session Handoff | ACTIVE | 2025-02-09 |
| 2025-01-09 | /PROGRESS.md | MODIFIED | Updated with January 9 session completion summary, infrastructure excellence achievements, and handoff status for next agent | Librarian - Session Handoff | ACTIVE | N/A |
| 2025-01-09 | /docs/guides-setup/CONTINUATION-GUIDE.md | CREATED | Comprehensive continuation guide for new Claude Code agents including quick verification steps, essential documentation locations, navigation map, emergency protocols, and success checklist | Librarian - Session Handoff | ACTIVE | N/A |
| 2025-01-09 | /AGENT-CONTINUATION-PROMPT.md | CREATED | Complete agent continuation prompt that user can paste to new Claude Code agent - includes project context, current status, working environment details, essential documentation, and verification checklist | Librarian - Session Handoff | ACTIVE | 2025-02-09 |
| 2025-01-09 | /docs/architecture/functional-area-master-index.md | MODIFIED | Updated with January 9 session status showing comprehensive test infrastructure operational and ready for Phase 6 with all critical issues resolved | Librarian - Session Handoff | ACTIVE | N/A |
| 2025-01-09 | /scripts/verify-api-health.sh | CREATED | Backend health verification script with comprehensive API endpoint testing, database connectivity checks, performance monitoring, and CORS validation for handoff documentation | Git Manager - Session Handoff | ACTIVE | N/A |
| 2025-01-09 | /scripts/verify-frontend.sh | CREATED | Frontend health verification script with development server status checks, page accessibility testing, API integration validation, and process monitoring for handoff documentation | Git Manager - Session Handoff | ACTIVE | N/A |
| 2025-01-09 | /docs/backend-environment-state-2025-01-09.md | CREATED | Complete backend environment state documentation including API configuration, database schema, authentication patterns, and service endpoints for handoff continuity | Librarian - Session Handoff | ACTIVE | 2025-02-09 |
| 2025-01-09 | /docs/functional-areas/frontend-state-handoff/react-developer-2025-01-09-handoff.md | CREATED | Frontend state handoff documentation for React developer including component status, implementation patterns, test coverage, and continuation points for next session | Librarian - Session Handoff | ACTIVE | 2025-02-09 |
| 2025-01-09 | /docs/design/wireframes/test-auth.html | CREATED | Authentication system test wireframe for validating login flows and user experience patterns during handoff testing | Librarian - Session Handoff | TEMPORARY | 2025-01-16 |
| 2025-01-09 | /apps/web/cookies.txt | CREATED | Test cookies file for authentication debugging and session state validation during handoff verification | Librarian - Session Handoff | TEMPORARY | 2025-01-16 |
| 2025-01-09 | /apps/web/tests/temp/jwt-test.js | CREATED | JWT token validation test script for verifying authentication system integrity during handoff process | Librarian - Session Handoff | TEMPORARY | 2025-01-16 |
| 2025-08-25 | /apps/web/.env.production | MODIFIED | TinyMCE API key configuration for production environment security | React Development Session | ACTIVE | N/A |
| 2025-08-23 | /docs/lessons-learned/test-executor-lessons-learned.md | MODIFIED | Updated with file organization enforcement success and comprehensive E2E testing methodology including visual evidence validation patterns | Test Executor Agent | ACTIVE | N/A |
| 2025-08-23 | /docs/lessons-learned/librarian-lessons-learned.md | MODIFIED | Updated with file organization maintenance protocols and architecture documentation standards | Librarian Agent | ACTIVE | N/A |
| 2025-08-23 | /docs/lessons-learned/devops-lessons-learned.md | MODIFIED | Updated with Docker operations and environment management best practices | DevOps Agent | ACTIVE | N/A |
| 2025-08-22 | /apps/web/src/pages/TinyMCETest.tsx | CREATED | TinyMCE editor demonstration page for Event Session Matrix with rich text editing capabilities | React Development Session | ACTIVE | N/A |
| 2025-08-22 | /apps/web/tests/playwright/tinymce-basic-check.spec.ts | CREATED | Basic TinyMCE functionality validation test | Test Development Session | ACTIVE | N/A |
| 2025-08-22 | /apps/web/tests/playwright/tinymce-editor.spec.ts | CREATED | Advanced TinyMCE editor functionality test with content validation | Test Development Session | ACTIVE | N/A |
| 2025-08-22 | /apps/web/tests/playwright/tinymce-visual-verification.spec.ts | CREATED | Visual validation test for TinyMCE integration with screenshot comparison | Test Development Session | ACTIVE | N/A |
| 2025-08-22 | /DEMO_FIXES_SUMMARY.md | CREATED | Summary document of TinyMCE demo fixes and UI improvements for Event Session Matrix | React Development Session | ACTIVE | 2025-09-22 |
| 2025-08-22 | /test-demo-fixes.js | CREATED | Test script for validating TinyMCE demo fixes and functionality | React Development Session | TEMPORARY | 2025-09-22 |

## File Categories

### Core Application Files
- Component files (`.tsx`, `.ts`) in `/apps/web/src/`
- API endpoints in `/src/WitchCityRope.Api/`
- Database files in `/src/WitchCityRope.Infrastructure/`

### Documentation Files
- Architecture docs in `/docs/architecture/`
- Feature documentation in `/docs/functional-areas/`
- Process documentation in `/docs/standards-processes/`

### Test Files
- Playwright tests in `/apps/web/tests/playwright/`
- Unit tests in `/apps/web/src/**/*.test.ts`
- Test results in `/test-results/`

### Configuration Files
- Build configuration: `package.json`, `vite.config.ts`, `playwright.config.ts`
- Environment: `.env` files, Docker configurations
- CI/CD: GitHub Actions workflows

### Archive Files
- Historical: `/docs/_archive/`
- Deprecated features: `/src/_archive/`

## File Maintenance

### Monthly Review (First Monday of Month)
- [ ] Review TEMPORARY files for cleanup
- [ ] Archive outdated documentation
- [ ] Update file statuses
- [ ] Check for orphaned files

### Session End Checklist
- [ ] Log all created/modified files
- [ ] Set appropriate cleanup dates
- [ ] Update documentation references
- [ ] Clean up temporary files

---

**Remember**: Every file operation must be logged to prevent project clutter and maintain traceability.