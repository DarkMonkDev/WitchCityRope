# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
> 
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Active File Registry

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-01-09 | /apps/web/src/components/layout/Navigation.tsx | MODIFIED | Updated navigation for logged-in users: replaced user greeting+logout with Dashboard CTA, added Admin link for administrators | Navigation Updates Implementation | ACTIVE | N/A |
| 2025-01-09 | /apps/web/src/components/layout/UtilityBar.tsx | MODIFIED | Updated utility bar layout: added user greeting on LEFT, logout link on RIGHT, changed justify from flex-end to space-between | Navigation Updates Implementation | ACTIVE | N/A |
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
| 2025-09-11 | Multiple investigation files | READ | Comprehensive investigation of admin dashboard and events management system for implementation planning | Librarian investigation - admin events analysis | INVESTIGATION | N/A |
| 2025-09-10 | /docs/lessons-learned/react-developer-lessons-learned.md | MODIFIED | Added critical lesson on safe date handling from API data to prevent runtime crashes | React developer lessons documentation | ACTIVE | Keep permanent |
| 2025-09-10 | /session-work/2025-09-10/date-handling-test.js | CREATED | Test script to verify date handling fix works for all problematic scenarios | React developer testing | TEMPORARY | 2025-09-17 |
| 2025-09-10 | /docs/design/wireframes/navigation-logged-in-mockup.html | CREATED | HTML mockup showing navigation changes for logged-in users with visual comparison of current vs new states | UI Designer navigation mockup | ACTIVE | Keep permanent |
| 2025-09-11 | Multiple navigation files | READ | Investigation of current navigation structure for navigation update orchestration | Librarian navigation analysis | INVESTIGATION | Analysis complete |
| 2025-09-11 | /docs/standards-processes/workflow-orchestration-process.md | MODIFIED | CRITICAL FIX: Removed UI Design and Functional Spec from Phase 1, moved them to Phase 2 with UI Design FIRST mandate | Librarian workflow documentation fix | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/navigation/requirements/functional-specification-2025-09-11.md | CREATED | Comprehensive functional specification for navigation updates based on approved UI design and business requirements | Functional Spec Agent navigation implementation | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/navigation/requirements/business-requirements-2025-09-11.md | CREATED | Business requirements document for navigation updates for logged-in users | Business Requirements Agent navigation updates | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/navigation/design/ui-design-2025-09-11.md | CREATED | UI design document with wireframes and specifications for navigation changes | UI Designer navigation updates | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/navigation/completion-summary-2025-09-11.md | CREATED | Completion summary documenting successful navigation updates implementation | Librarian documentation completion | ACTIVE | Keep permanent |
| 2025-09-11 | /home/chad/repos/witchcityrope-react/PROGRESS.md | MODIFIED | Updated to reflect completed navigation updates for logged-in users with achievement summary | Librarian progress documentation | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/architecture/file-registry.md | MODIFIED | Added entries for all navigation update documentation files and completion tracking | Librarian file tracking | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/architecture/functional-area-master-index.md | MODIFIED | Added navigation functional area with COMPLETE status for logged-in user updates | Librarian navigation tracking | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/lessons-learned/librarian-lessons-learned.md | MODIFIED | Added documentation completion excellence patterns from navigation updates workflow | Librarian lessons documentation | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/events/admin-activation/business-requirements-2025-09-11.md | CREATED | Focused business requirements for activating existing Admin Dashboard Events Management system - prioritizing connection over creation | Business Requirements Agent admin events activation | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/events/admin-activation/ui-design-2025-09-11.md | CREATED | UI design document for minimal Admin Dashboard activation with component specifications and wireframes | UI Designer admin dashboard activation | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/events/admin-activation/functional-specification-2025-09-11.md | CREATED | Technical functional specification for Admin Dashboard Events Management activation - details minimal implementation to connect existing assets | Functional Spec Agent admin events activation | ACTIVE | Keep permanent |
| 2025-09-11 | /session-work/2025-09-11/event-session-matrix-investigation-report.md | CREATED | Comprehensive investigation report confirming Event Session Matrix system is fully implemented - no work was lost in recent merges | Librarian Event Session Matrix investigation | ACTIVE | Keep permanent |
| 2025-09-11 | /session-work/2025-09-11/authentication-investigation-summary.md | CREATED | Complete analysis of authentication implementation status and methodology - identifies single source of truth for authentication patterns | Librarian authentication investigation | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/dependencies-management/requirements/business-requirements.md | CREATED | Business requirements document for dependencies management functional area - covers NuGet, npm, security vulnerabilities | Librarian dependencies management setup | ACTIVE | Keep permanent |
| 2025-09-12 | /apps/api/Features/Events/Models/UpdateEventRequest.cs | CREATED | Request model for partial event updates with optional fields (Title, Description, StartDate, EndDate, Location, Capacity, PricingTiers, IsPublished) | Backend Developer PUT endpoint implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /apps/api/Features/Events/Services/EventService.cs | MODIFIED | Added UpdateEventAsync method with business rule validation (past events, capacity limits, date ranges, partial updates) | Backend Developer PUT endpoint implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /apps/api/Features/Events/Endpoints/EventEndpoints.cs | MODIFIED | Added PUT /api/events/{id} endpoint with JWT authentication, comprehensive HTTP status codes, and proper error handling | Backend Developer PUT endpoint implementation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/backend-lessons-learned.md | MODIFIED | Added comprehensive documentation of Minimal API Event Update Implementation - full solution for 405 Method Not Allowed error | Backend Developer lessons documentation | ACTIVE | Keep permanent |
| 2025-09-11 | /apps/web/src/components/events/EventsTableView.tsx | MODIFIED | Fixed three critical admin events table issues: Invalid Date display, enhanced date/time formatting with validation, improved Copy button styling with WCR design system | React developer admin events table fixes | ACTIVE | Keep permanent |
| 2025-09-11 | /apps/web/src/components/events/CapacityDisplay.tsx | MODIFIED | Enhanced capacity display with proper fallback handling for missing API data, changed props to optional, added "Capacity TBD" fallback UI | React developer capacity display fixes | ACTIVE | Keep permanent |
| 2025-09-11 | /session-work/2025-09-11/admin-events-table-fixes.md | CREATED | Documentation of all fixes applied to admin events table with problem analysis and solutions | React developer fix documentation | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/lessons-learned/frontend-lessons-learned.md | MODIFIED | Added comprehensive lesson on admin events table fixes: validation patterns, fallback handling, and WCR design system compliance | React developer lessons learned | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/events/admin-dashboard/handoffs/business-to-ui-handoff-2025-09-11.md | CREATED | Comprehensive phase transition handoff from Business Requirements to UI Design phase for Admin Events Dashboard enhancement | Librarian handoff documentation | ACTIVE | Keep permanent |
| 2025-09-11 | Multiple lessons learned files | MODIFIED | CRITICAL: Removed ALL worktree references from project - worktrees are NEVER to be used. Updated 12+ agent lessons learned files to remove worktree compliance sections and references | Librarian worktree removal cleanup | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/standards-processes/workflow-orchestration-process.md | MODIFIED | Removed worktree workflow integration sections and replaced with standard git branch workflow | Librarian worktree removal cleanup | ACTIVE | Keep permanent |
| 2025-09-11 | /.claude/commands/orchestrate.md | MODIFIED | Removed worktree workflow integration and examples, replaced with standard branch creation workflow | Librarian worktree removal cleanup | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/dependencies-management/research/package-analysis.md | CREATED | Package analysis framework for security vulnerability assessment and compatibility testing | Librarian dependencies management setup | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/dependencies-management/implementation/update-procedures.md | CREATED | Comprehensive update procedures for NuGet and npm packages including rollback procedures | Librarian dependencies management setup | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/dependencies-management/testing/compatibility-tests.md | CREATED | Testing strategy and compatibility matrix for validating dependency updates | Librarian dependencies management setup | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/dependencies-management/reviews/update-checklist.md | CREATED | Comprehensive review checklist for dependency updates with sign-off requirements | Librarian dependencies management setup | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/architecture/functional-area-master-index.md | MODIFIED | Added Dependencies Management functional area to master index with appropriate status and description | Librarian master index update | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/dependencies-management/requirements/nuget-update-requirements-2025-09-11.md | CREATED | Comprehensive business requirements for NuGet package updates - addressing version conflicts, security vulnerabilities, and system stability | Business Requirements Agent NuGet updates | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/dependencies-management/research/nuget-versions-analysis-2025-09-11.md | CREATED | Comprehensive technology research document analyzing latest NuGet package versions, security updates, compatibility, and migration strategies for WitchCityRope API | Technology Researcher NuGet analysis | ACTIVE | Keep permanent |
| 2025-09-11 | /test-results/nuget-package-validation-report-2025-09-11.md | CREATED | Comprehensive validation report for NuGet package updates showing 206 compilation errors remain | Test executor NuGet validation | ACTIVE | Keep permanent |
| 2025-09-11 | /test-results/nuget-validation-summary-2025-09-11.json | CREATED | JSON summary of NuGet validation results for orchestrator consumption showing compilation status and required fixes | Test executor NuGet validation | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/dependencies-management/nuget-update-completion-report-2025-09-11.md | CREATED | Comprehensive completion report for NuGet package update project - documents all accomplishments, package versions, and success metrics | Librarian NuGet finalization | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/dependencies-management/test-project-fixes-scope-2025-09-11.md | CREATED | Implementation scope and roadmap for fixing 172 test compilation errors - categorized by type with time estimates and fix strategies | Librarian test fixes planning | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/standards-processes/testing-prerequisites.md | MODIFIED | Added mandatory Phase 0 health checks requirement BEFORE any test execution - prevents false failures from infrastructure issues | Librarian testing documentation update | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/standards-processes/testing/TESTING.md | MODIFIED | Added mandatory pre-flight health checks section at top of overview - requires health checks before ANY test execution | Librarian testing documentation update | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/lessons-learned/test-developer-lessons-learned.md | MODIFIED | Added critical lesson about always running health checks first to prevent debugging false failures from port misconfigurations | Librarian testing documentation update | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/lessons-learned/librarian-lessons-learned.md | MODIFIED | Added testing documentation enhancement patterns from health check requirement update work | Librarian lessons documentation | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/lessons-learned/backend-lessons-learned.md | MODIFIED | Added CRITICAL mandatory health check requirements section for backend developers - prevents false test failures | Librarian testing documentation update | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/lessons-learned/react-developer-lessons-learned.md | MODIFIED | Added CRITICAL mandatory health check requirements section for React developers - prevents port misconfiguration debugging | Librarian testing documentation update | ACTIVE | Keep permanent |
| 2025-09-11 | /home/chad/repos/witchcityrope-react/PROGRESS.md | MODIFIED | Added September 11, 2025 section documenting completion of Test Infrastructure Hardening & NuGet Updates with comprehensive infrastructure improvements | Librarian progress documentation update | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/architecture/functional-area-master-index.md | READ | Investigation of events functional area structure for admin events dashboard analysis | Librarian events investigation | INVESTIGATION | Analysis complete |
| 2025-09-11 | /docs/functional-areas/events/** | READ | Comprehensive investigation of events functional area to map existing wireframes, documentation, and React implementation | Librarian events structure analysis | INVESTIGATION | Analysis complete |
| 2025-09-11 | /docs/architecture/WORKTREE-STRATEGY-DECISION.md | ARCHIVED | Moved obsolete worktree strategy decision document to _archive - worktrees are NEVER to be used in this project | Librarian worktree cleanup | ARCHIVED | Keep in archive permanently |
| 2025-09-11 | /scripts/worktree/ | DELETED | Removed entire worktree scripts directory - includes create-worktree.sh, worktree-status.sh, cleanup-worktrees.sh | Librarian worktree cleanup | DELETED | N/A |
| 2025-09-11 | /docs/guides-setup/ai-agent-worktree-guide.md | DELETED | Removed 260-line AI agent worktree guide - worktrees are forbidden in this project | Librarian worktree cleanup | DELETED | N/A |
| 2025-09-11 | /docs/standards-processes/worktree-environment-setup.md | DELETED | Removed 292-line worktree environment setup guide - obsolete and never to be used | Librarian worktree cleanup | DELETED | N/A |
| 2025-09-11 | /docs/functional-areas/claude-code-parallel-sessions/ | DELETED | Removed entire functional area focused on worktree-based parallel sessions - contains 8+ obsolete documents | Librarian worktree cleanup | DELETED | N/A |
| 2025-09-11 | /.gitignore | MODIFIED | Removed .worktrees/ entry from gitignore - this directory should never exist in the project | Librarian worktree cleanup | ACTIVE | Keep permanent |
| 2025-09-11 | /scripts/claude-preflight-check.sh | MODIFIED | Removed all worktree-related validation checks and warnings from preflight script | Librarian worktree cleanup | ACTIVE | Keep permanent |
| 2025-09-11 | /session-work/2025-09-11/worktree-reference-cleanup-completion-2025-09-11.md | CREATED | Comprehensive completion report documenting successful removal of ALL worktree references from project | Librarian worktree cleanup completion | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/functional-areas/events/admin-dashboard/requirements/business-requirements-2025-09-11.md | CREATED | Comprehensive business requirements for Admin Events Dashboard enhancement - transforms card view to efficient table view with filtering, search, and improved navigation | Business Requirements Agent admin events dashboard | ACTIVE | Keep permanent |
| 2025-09-11 | /docs/lessons-learned/react-developer-lessons-learned.md | MODIFIED | Added CRITICAL Mantine Button Text Cutoff Prevention lesson - documents recurring issue, root cause, and prevention patterns to stop this common problem | Librarian lessons learned documentation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/architecture/critical-issue-duplicate-apis-investigation.md | CREATED | CRITICAL: Comprehensive forensic investigation of duplicate API projects issue - identified two API implementations (apps/api/ and src/WitchCityRope.Api/) and provided resolution plan | Librarian critical architectural investigation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/architecture/api-cleanup-session-prompt.md | CREATED | CRITICAL: Comprehensive session prompt for future Claude Code session to handle complete API cleanup and feature extraction - 6-phase work plan with technical constraints and success criteria | Librarian session prompt creation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/backend-lessons-learned.md | MODIFIED | Added CRITICAL API confusion prevention section - clear instructions for backend developers to ONLY modify /apps/api/ and NEVER touch /src/WitchCityRope.Api/ | Librarian agent protection | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/librarian-lessons-learned.md | MODIFIED | Added critical architecture issue session documentation pattern - how to create comprehensive session prompts for complex architectural cleanup | Librarian lessons documentation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/standards-processes/testing/playwright-standards.md | CREATED | Comprehensive Playwright testing standards with proven 100% success login solution - complete implementation guide with cross-browser validation, performance metrics, troubleshooting | Librarian testing standards documentation | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/standards-processes/testing/TESTING.md | MODIFIED | Updated main testing guide with proven Playwright login pattern, updated E2E commands to use npx playwright test instead of deprecated npm test | Librarian testing documentation update | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/test-executor-lessons-learned.md | MODIFIED | Enhanced major success login solution with documentation integration section - records creation of formal testing standards | Librarian testing documentation integration | ACTIVE | Keep permanent |
| 2025-09-12 | /docs/lessons-learned/librarian-lessons-learned.md | MODIFIED | Added testing standards documentation excellence pattern - how to extract proven solutions from lessons learned into formal team standards | Librarian lessons documentation | ACTIVE | Keep permanent |

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
- **Screenshots**: Usually TEMPORARY (1 week retention)
- **Debug Scripts**: TEMPORARY unless solving recurring issues
- **Session Work**: TEMPORARY (1 week retention)

### Agent Responsibilities
Each agent must:
1. **Log immediately** when creating/modifying/deleting files
2. **Set appropriate status** (ACTIVE/TEMPORARY) 
3. **Set cleanup date** for TEMPORARY files
4. **Use descriptive purpose** explaining why file was created
5. **Include session/task context** for tracking

### File Registry Validation
- Run monthly audits to verify file registry accuracy
- Clean up orphaned files not in registry
- Update status of files that changed in purpose/importance