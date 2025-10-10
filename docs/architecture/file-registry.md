# File Registry - WitchCityRope Project

> **CRITICAL**: Every file created, modified, or deleted by Claude or any AI agent MUST be logged here.
>
> **Purpose**: Track all files to prevent orphaned documents and maintain project cleanliness.

## Registry Table

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
| 2025-10-10 | /test-results/api-events-response-format-analysis-20251010.md | CREATED | API events response format analysis - E2E test expects array, API returns ApiResponse wrapper | E2E Test Contract Mismatch | ACTIVE | Keep permanently |
| 2025-10-10 | /docs/lessons-learned/backend-developer-lessons-learned-2.md | MODIFIED | Added lesson on E2E test contract mismatch vs ApiResponse wrapper standard | Backend Lessons Update | ACTIVE | Never |
| 2025-10-10 | /test-results/enum-mapping-fix-2025-10-10.md | CREATED | ENUM FIX VERIFIED - Test infrastructure enum mapping fix complete documentation | Test Infrastructure Enum Fix | ACTIVE | Keep permanently |
| 2025-10-10 | /apps/web/tests/playwright/verify-enum-mapping-fix.spec.ts | CREATED | Verification test for enum mapping fix - confirms numeric values work correctly | Test Infrastructure Enum Fix | ACTIVE | Never |
| 2025-10-10 | /apps/web/tests/playwright/templates/ticket-cancellation-persistence-template.ts | MODIFIED | Updated to use numeric ParticipationStatus enum values (1=Active, 2=Cancelled, etc) | Test Infrastructure Enum Fix | ACTIVE | Never |
| 2025-10-10 | /apps/web/tests/playwright/ticket-lifecycle-persistence.spec.ts | MODIFIED | Updated to use numeric ParticipationStatus enum values matching database | Test Infrastructure Enum Fix | ACTIVE | Never |
| 2025-10-10 | /apps/web/tests/playwright/utils/database-helpers.ts | MODIFIED | CRITICAL FIX - Added numeric enum support and getParticipationStatusName() helper | Test Infrastructure Enum Fix | ACTIVE | Never |
| 2025-10-10 | /test-results/ticket-cancellation-verification-2025-10-10.md | CREATED | Ticket cancellation bug fix verification - FIX VERIFIED, test infrastructure needs update | Ticket Cancellation Verification | ACTIVE | Keep permanently |
| 2025-10-10 | /apps/api/Features/Participation/Services/ParticipationService.cs | MODIFIED | CRITICAL BUG FIX - Added explicit Update() call for ticket cancellation persistence | Ticket Cancellation Bug Fix | ACTIVE | Never |
| 2025-10-10 | /docs/lessons-learned/backend-developer-lessons-learned-2.md | MODIFIED | Added lesson on EF Core change tracking failures with domain methods | Backend Lessons Update | ACTIVE | Never |
| 2025-10-10 | /session-work/2025-10-10/safety-reference-number-column-size-fix.md | CREATED | Column size increase fix for SafetyIncident.ReferenceNumber (20→30 chars) | Phase 1.5.4 Column Fix | ACTIVE | Never |
| 2025-10-10 | /apps/api/Features/Safety/Entities/SafetyIncident.cs | MODIFIED | Increased ReferenceNumber MaxLength from 20 to 30 characters | Phase 1.5.4 Entity Update | ACTIVE | Never |
| 2025-10-10 | /apps/api/Data/ApplicationDbContext.cs | MODIFIED | Updated FluentAPI ReferenceNumber HasMaxLength from 20 to 30 | Phase 1.5.4 DbContext Update | ACTIVE | Never |
| 2025-10-10 | /apps/api/Migrations/20251010064435_IncreaseSafetyReferenceNumberLength.cs | CREATED | EF Core migration to alter ReferenceNumber column to VARCHAR(30) | Phase 1.5.4 Migration | ACTIVE | Never |
| 2025-10-10 | /docs/lessons-learned/backend-developer-lessons-learned-2.md | MODIFIED | Added lesson on database column size vs test helper format mismatch | Backend Lessons Update | ACTIVE | Never |
| 2025-10-10 | /session-work/2025-10-10/safety-reference-number-function-implementation.md | CREATED | Implementation summary for safety reference number function | Phase 1.5.4 Documentation | ACTIVE | Never |
| 2025-10-10 | /apps/api/Migrations/20251010063756_AddSafetyReferenceNumberFunction.cs | CREATED | PostgreSQL function for generating unique safety reference numbers (SR-YYYY-NNNNNN) | Phase 1.5.4 Safety Function | ACTIVE | Never |
| 2025-10-10 | /apps/api/Migrations/20251010063756_AddSafetyReferenceNumberFunction.Designer.cs | CREATED | EF Core migration designer for safety reference number function | Phase 1.5.4 Safety Function | ACTIVE | Never |
| 2025-10-10 | /tests/unit/api/Features/Vetting/Services/VettingPublicServiceTests.cs | CREATED | 15 comprehensive tests for public/user-facing vetting methods | Vetting Public Service Tests | ACTIVE | Never |
| 2025-10-10 | /docs/standards-processes/testing/TEST_CATALOG.md | MODIFIED | Added VettingPublicServiceTests entry with test breakdown | TEST_CATALOG Update | ACTIVE | Never |
| 2025-10-09 | /docs/guides-setup/docker-operations-guide.md | MODIFIED | Condensed from 2,064 to 580 lines (72% reduction) - meets 2,000 line standard | Documentation Condensing | ACTIVE | Never |
| 2025-10-09 | /session-work/2025-10-09/file-registry-audit-report.md | CREATED | Comprehensive file-registry audit - recommends cleanup over splitting (89% token reduction) | File Registry Research | ACTIVE | Never |
| 2025-10-09 | /test-results/PHASE-0.3-PROFILEPAGE-FIX-2025-10-09.md | CREATED | Phase 0.3 ProfilePage test fixes - 80% pass rate achieved (16/20 tests) | ProfilePage Test Fixes | ACTIVE | Keep permanently |
| 2025-10-09 | /test-results/PHASE-0.3-SUMMARY-2025-10-09.md | CREATED | Phase 0.3 summary - ProfilePage 80% pass rate SUCCESS | ProfilePage Summary | ACTIVE | Keep permanently |
| 2025-10-09 | /apps/web/src/pages/dashboard/__tests__/ProfilePage.test.tsx | MODIFIED | ProfilePage test fixes - Mantine getElementById pattern | ProfilePage Tests | ACTIVE | Never |
| 2025-10-09 | /test-results/PHASE-0.2-REGRESSION-ANALYSIS-2025-10-09.md | CREATED | E2E test regression analysis - 9 tests lost during Priority fixes | Regression Investigation | ACTIVE | Keep permanently |
| 2025-10-09 | /test-results/API-TEST-COVERAGE-GAP-2025-10-09.md | CREATED | API unit test coverage gap analysis - 82% gap (39 tests vs 217 needed) | API Coverage Assessment | ACTIVE | Keep permanently |
| 2025-10-09 | `/test-results/UNIT-TESTS-AFTER-FIXES-2025-10-09.md` | CREATED | React unit test results after fixes - baseline comparison report | Unit Test Verification | ACTIVE | Never |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-10-09 | `/backend-bugs-fix-summary-2025-10-09.md` | CREATED | Backend bug fixes - Profile race conditions + RSVP validation | Backend Bug Fixes P1/P2 | ACTIVE | Never |
| 2025-10-09 | `/apps/api/Features/Dashboard/Services/UserDashboardProfileService.cs` | MODIFIED | Profile update race condition fix (BUG #4) - optimistic concurrency control | Backend Bug Fix P1 | ACTIVE | Never |
| 2025-10-09 | `/apps/api/Features/Participation/Services/ParticipationService.cs` | MODIFIED | RSVP validation fix (BUG #5) - removed overly restrictive vetting requirement | Backend Bug Fix P2 | ACTIVE | Never |
| 2025-10-09 | `/docs/lessons-learned/backend-developer-lessons-learned-2.md` | MODIFIED | Added 2 critical lessons - Profile race conditions + RSVP validation | Backend Lessons Learned | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/.husky/pre-commit` | CREATED | Pre-commit git hook for ESLint validation | ESLint Pre-commit Hook | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/package.json` | MODIFIED | Added husky and lint-staged configuration | ESLint Pre-commit Setup | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/api/Services/SeedDataService.cs` | MODIFIED | Add ticket purchases for dashboard E2E testing | User Dashboard E2E Testing | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/web/tests/playwright/e2e/dashboard/user-dashboard-wireframe-validation.spec.ts` | CREATED | User dashboard wireframe validation E2E tests - 39 comprehensive test scenarios | Wireframe Validation E2E | ACTIVE | Never |
| 2025-10-09 | /docs/functional-areas/testing/2025-10-09-silent-failure-patterns-audit.md | CREATED | CRITICAL AUDIT - Silent failure patterns and test coverage gaps | Silent Failure Audit | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/web/tests/playwright/e2e/dashboard/user-dashboard-wireframe-validation.spec.ts` | CREATED | User dashboard wireframe validation E2E tests - acceptance criteria | Wireframe Validation Tests | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/specifications/functional-specifications-v2.md` | CREATED | Functional specifications v2.0 - User dashboard redesign APPROVED FOR IMPLEMENTATION | Functional Specs v2.0 | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/APPROVED-DESIGN.md` | CREATED | Design approval document - User dashboard redesign v4 | Approved Design Document | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/design/dashboard-wireframe-v4-iteration.html` | MODIFIED | USER DASHBOARD VERSION - Critical feedback implementation | Wireframe v4 Iteration | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/design/dashboard-wireframe-simplified.html` | CREATED | SIMPLIFIED dashboard wireframe - based on critical user feedback | Simplified Wireframe | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/design/dashboard-wireframe-variation-a-minimal.html` | CREATED | Dashboard wireframe variation A - Minimal focus | Wireframe Variation A | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/design/dashboard-wireframe-variation-b-information-rich.html` | CREATED | Dashboard wireframe variation B - Information rich | Wireframe Variation B | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/design/dashboard-wireframe-variation-c-balanced-modern.html` | CREATED | Dashboard wireframe variation C - Balanced modern (RECOMMENDED) | Wireframe Variation C | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/research/modern-dashboard-ux-analysis-2025-10-09.md` | CREATED | Modern dashboard UX research analysis - 50+ modern dashboard designs analyzed | UX Research Analysis | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-management/research/2025-10-09-simple-dashboard-navigation-patterns.md` | CREATED | Navigation pattern research for simple dashboards - horizontal tab recommendation | Navigation Pattern Research | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/requirements/business-requirements.md` | MODIFIED | MAJOR UPDATE to version 5.0 - PROFILE CONSOLIDATION | Business Requirements v5.0 | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/design/ui-design-specifications.md` | MODIFIED | Updated UI design specifications to version 2.0 CONSOLIDATION | UI Design Specs v2.0 | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/design/dashboard-landing-page-v7.html` | MODIFIED | Updated dashboard landing page HTML mockup to v2.0 CONSOLIDATION | Dashboard Landing v2.0 | ACTIVE | Never |
| 2025-10-08 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/requirements/business-requirements.md` | MODIFIED | Updated user dashboard redesign to version 4.0 CONSOLIDATION | Business Requirements v4.0 | ACTIVE | Never |
| 2025-10-08 | `/docs/lessons-learned/test-developer-lessons-learned-2.md` | MODIFIED | Condensed file from 1,840 lines to 536 lines (71% reduction) | Test Developer Lessons - Cleanup | ACTIVE | Never |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Models/PublicApplicationSubmissionRequest.cs` | MODIFIED | Emergency contact removal - removed properties | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/api/Features/CheckIn/Entities/EventAttendee.cs` | MODIFIED | Emergency contact removal - removed properties | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/api/Features/CheckIn/Models/AttendeeResponse.cs` | MODIFIED | Emergency contact removal - removed fields | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/api/Features/Vetting/Services/VettingService.cs` | MODIFIED | Emergency contact removal - removed storage logic | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/api/Features/CheckIn/Entities/Configuration/EventAttendeeConfiguration.cs` | MODIFIED | Emergency contact removal - removed EF Core configuration | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/api/Features/CheckIn/Services/CheckInService.cs` | MODIFIED | Emergency contact removal - removed response mapping fields | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/web/src/components/forms/EmergencyContactGroup.tsx` | DELETED | Emergency contact removal - component removed entirely | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/web/src/components/forms/index.ts` | MODIFIED | Emergency contact removal - removed export | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/web/src/components/forms/README.md` | MODIFIED | Emergency contact removal - removed documentation | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/web/src/schemas/formSchemas.ts` | MODIFIED | Emergency contact removal - removed schema | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-09 | `/home/chad/repos/witchcityrope/apps/web/src/types/forms.ts` | MODIFIED | Emergency contact removal - removed interface fields | Emergency Contact Cleanup | ACTIVE | N/A |
| 2025-10-08 | `/session-work/2025-10-08/session-summary-20251008.md` | CREATED | Comprehensive session summary - October 8 vetting workflow improvements | Session Wrap-up Oct 8 | ACTIVE | Never |
| 2025-10-08 | `/session-work/2025-10-08/next-session-prompt.md` | CREATED | Next session starting prompt for October 9 with context from October 8 | Next Session Guidance | ACTIVE | Never |
| 2025-10-08 | `/PROGRESS.md` | MODIFIED | Updated with comprehensive October 8 vetting workflow improvements | PROGRESS.md Oct 8 Update | ACTIVE | Never |
| 2025-10-08 | `/docs/design/button-styling-standards-report.md` | CREATED | Button styling standards investigation report - Save Note button issue | Design Standards Report | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/vetting/documentation-update-report-20251008.md` | CREATED | Vetting documentation review report - status label changes | Documentation Review Report | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/vetting/handoffs/librarian-20251008-status-label-documentation-review.md` | CREATED | Librarian agent handoff document - vetting status label review | Vetting Doc Review Handoff | ACTIVE | Never |
| 2025-10-08 | `/session-work/2025-10-08/tiptap-editor-audit-report.md` | CREATED | Tiptap editor usage audit - NO phantom editors, clean production code | Tiptap Audit Report | ACTIVE | Never |
| 2025-10-08 | `/docs/architecture/CURRENT-TECHNOLOGY-STACK.md` | CREATED | Centralized technology stack reference document | Technology Stack Doc | ACTIVE | Never |
| 2025-10-08 | `/ARCHITECTURE.md` | MODIFIED | Updated React frontend section - added Tiptap rich text editor info | Architecture Tiptap Update | ACTIVE | Never |
| 2025-10-08 | `/docs/_archive/tinymce-decision-2025-08-24/README.md` | CREATED | Archive deprecation notice for TinyMCE decision | TinyMCE Archive Notice | ACTIVE | Never |
| 2025-10-08 | `/docs/architecture/research/2025-08-24-html-editors-research.md` | MOVED | Moved TinyMCE research document to archive | TinyMCE Archive Move | ARCHIVED | Never |
| 2025-10-08 | `/docs/architecture/functional-area-master-index.md` | MODIFIED | Updated HTML editor migration status to IMPLEMENTATION COMPLETE | Master Index Update | ACTIVE | Never |
| 2025-10-08 | `/apps/web/src/components/forms/MantineTiptapEditor.tsx` | CREATED | MantineTiptapEditor component - drop-in replacement for TinyMCE | TinyMCE Migration Phase 2 | ACTIVE | Never |
| 2025-10-08 | `/apps/web/src/pages/admin/events/EventForm.tsx` | MODIFIED | Updated to use MantineTiptapEditor instead of TinyMCE | TinyMCE Migration Phase 2 | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/html-editor-migration/phase3-cleanup-complete-report.md` | CREATED | Phase 3 configuration cleanup completion report | TinyMCE Migration Phase 3 | ACTIVE | Never |
| 2025-10-08 | `/apps/web/.env.example` | MODIFIED | Removed TinyMCE configuration section | TinyMCE Migration Cleanup | ACTIVE | Never |
| 2025-10-08 | `/apps/web/.env.staging` | MODIFIED | Removed TinyMCE API key | TinyMCE Migration Cleanup | ACTIVE | Never |
| 2025-10-08 | `/apps/web/.env.template` | MODIFIED | Removed TinyMCE API key section | TinyMCE Migration Cleanup | ACTIVE | Never |
| 2025-10-08 | `/apps/web/src/config/environment.ts` | MODIFIED | Removed external.tinyMceApiKey property | TinyMCE Migration Cleanup | ACTIVE | Never |
| 2025-10-08 | `/apps/web/package.json` | MODIFIED | Removed TinyMCE dependency | TinyMCE Migration Cleanup | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/html-editor-migration/documentation-verification-report.md` | CREATED | Documentation verification report for TinyMCE migration | TinyMCE Migration Verification | ACTIVE | Never |
| 2025-10-08 | `/docs/guides-setup/tinymce-api-key-setup.md` | ARCHIVE-PREP | TinyMCE API key setup guide - TO BE ARCHIVED when migration completes | Archive Preparation | ARCHIVE-PREP | After migration |
| 2025-10-08 | `/docs/functional-areas/testing/handoffs/session-wrap-up-checklist-20251008.md` | CREATED | Final session verification checklist | E2E Stabilization Checklist | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/testing/handoffs/e2e-stabilization-complete-20251008.md` | CREATED | E2E test stabilization session completion summary | E2E Stabilization Complete | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/testing/handoffs/next-session-prompt-20251008.md` | CREATED | Next session prompt and recommendations document | Next Session Recommendations | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/html-editor-migration/README.md` | CREATED | TinyMCE to Tiptap migration documentation hub | Migration Documentation Hub | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/html-editor-migration/migration-plan.md` | CREATED | Master TinyMCE to Tiptap migration plan | Migration Plan | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/html-editor-migration/component-implementation-guide.md` | CREATED | Complete component implementation guide with copy-paste ready code | Component Implementation | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/html-editor-migration/testing-migration-guide.md` | CREATED | Comprehensive testing migration guide | Testing Migration | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/html-editor-migration/configuration-cleanup-guide.md` | CREATED | Line-by-line configuration cleanup guide | Configuration Cleanup | ACTIVE | Never |
| 2025-10-08 | `/docs/functional-areas/html-editor-migration/rollback-plan.md` | CREATED | Emergency rollback procedures and decision framework | Rollback Plan | ACTIVE | Never |
| 2025-10-08 | `/test-results/authentication-persistence-fix-20251008.md` | CREATED | CRITICAL LAUNCH BLOCKER FIX - authentication persistence bug | Auth Persistence Fix | ACTIVE | Never |
| 2025-10-08 | `/apps/web/src/config/api.ts` | MODIFIED | CRITICAL FIX - Modified getApiBaseUrl() for Vite proxy | Auth Persistence Fix | ACTIVE | Never |
| 2025-10-08 | `/test-results/FINAL-E2E-VERIFICATION-20251008.md` | CREATED | Final E2E verification report - 100% pass rate achievement | E2E Final Verification | ACTIVE | Never |
| 2025-10-07 | `/docs/architecture/research/2025-10-07-tiptap-comparison-deep-dive.md` | CREATED | Tiptap v2 vs @mantine/tiptap deep technical comparison | Tiptap Comparison Research | ACTIVE | Never |
| 2025-10-07 | `/docs/architecture/research/2025-10-07-html-editor-alternatives-testing-focus.md` | CREATED | HTML editor alternatives research with testing focus | HTML Editor Research | ACTIVE | Never |
| 2025-10-07 | `/docs/functional-areas/testing/new-work/2025-10-07-e2e-stabilization/fix-plan.md` | CREATED | E2E test stabilization fix plan - Option A systematic approach | E2E Stabilization Plan | ACTIVE | Never |
| 2025-10-07 | `/test-results/e2e-baseline-summary-20251007.md` | NOTE | File referenced but NOT FOUND - content consolidated into handoff | CONSOLIDATED | CONSOLIDATED | 2025-10-08 |
| 2025-10-07 | `/test-results/e2e-failure-categorization-20251007.md` | NOTE | File referenced but NOT FOUND - content consolidated into handoff | CONSOLIDATED | CONSOLIDATED | 2025-10-08 |
| 2025-10-07 | `/test-results/phase2-bug-fixes-20251007.md` | NOTE | File referenced but NOT FOUND - content consolidated into handoff | CONSOLIDATED | CONSOLIDATED | 2025-10-08 |
| 2025-10-07 | `/docs/standards-processes/testing/TEST_CATALOG.md` | MODIFIED | Added E2E test failure categorization summary entry | TEST_CATALOG Phase 2 Update | ACTIVE | Never |
| 2025-10-06 | `/test-results/component-field-name-updates-20251006.md` | CREATED | Component field name updates summary - align with backend DTOs | Component Field Updates | ACTIVE | Never |
| 2025-10-08 | `/apps/web/src/features/admin/vetting/components/VettingApplicationDetail.tsx` | MODIFIED | Rearranged action buttons - primary CTAs left, tertiary actions right | Button Layout UX Improvement | ACTIVE | Never |
| 2025-10-08 | `/session-work/2025-10-08/vetting-buttons-layout-report.md` | CREATED | Button layout rearrangement report - VettingApplicationDetail component | Vetting Buttons Layout | ACTIVE | Never |
| 2025-10-08 | `/home/chad/repos/witchcityrope/docs/functional-areas/user-dashboard/new-work/2025-08-22-user-dashboard-redesign/implementation-plan.md` | CREATED | Comprehensive implementation plan for user dashboard redesign | Implementation Plan v1.0 | ACTIVE | Never |

## Cleanup Guidelines

- **TEMPORARY** files should be in `/session-work/[date]/`
- **TEST RESULTS** should be cleaned up after issues are resolved
- **SOURCE CODE** tracking should be removed (git handles this)
- **PURPOSE** field should be 100-150 characters maximum
- **DETAILED INFO** belongs in the file itself, not the registry
| 2025-10-10 | /test-results/payment-tests-results-2025-10-10.md | CREATED | Payment API test execution results - 23/30 passing (76.7%), refund issues identified | Payment Test Execution | ACTIVE | Keep permanently |
| 2025-10-10 | /tmp/payment-tests-unblocked.log | CREATED | Detailed payment test execution log - 305KB detailed output | Payment Test Detailed Log | TEMPORARY | Delete after review |
| 2025-10-10 | /test-results/phase-1.5.4-final-execution-report.md | CREATED | Phase 1.5.4 final test execution report - 100% pass rate (48/48 tests) | Phase 1.5.4 Complete | ACTIVE | Keep permanently |
| 2025-10-10 | /test-results/events-e2e-p1-fixes-verification-2025-10-10.md | CREATED | Events E2E P1 fixes verification - 57.7% pass rate (82/142), P2 issues identified | Events E2E P1 Verification | ACTIVE | Keep permanently |
