# File Registry
<!-- Last Updated: 2025-12-12 -->
<!-- Version: 4.549 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Purpose
This registry tracks all files created, modified, and deleted in the WitchCityRope project. It provides accountability and enables cleanup of temporary files.

## Recent Updates

| Date | File Path | Action | Purpose | Session/Task | Status | Cleanup Date |
|------|-----------|--------|---------|--------------|--------|--------------|
| 2025-12-12 | /apps/api/Migrations/20251212000000_RemoveEventTypeAddEventFlags.cs | CREATED | Database migration to remove EventType enum column and replace with three boolean flags (AllowRsvps, RequireTicketPurchase, VettedMembersOnly) - Implements event simplification architecture by removing rigid event type categorization in favor of flexible configuration | Database Designer: Event Type Simplification Migration | PENDING | Never |
| 2025-12-12 | /apps/web/src/lib/api/hooks/useEvents.ts | MODIFIED | Fixed cache invalidation race condition in useUpdateEvent hook - Changed onSettled to only invalidate event lists, not detail queries, preventing stale data after ticket additions | React Developer: Fix Event Cache Invalidation Bug | ACTIVE | Never |
| 2025-12-12 | /docs/functional-areas/event-simplification/ | CREATED | New functional area folder for event simplification research - Stores research documentation for architectural change to unify Workshop and Social Event concepts into single configurable event type | Librarian: Create Event Simplification Folder | ACTIVE | Never |
| 2025-12-12 | /docs/functional-areas/event-simplification/research/ | CREATED | Research subdirectory for event simplification architectural research | Librarian: Create Event Simplification Folder | ACTIVE | Never |
| 2025-12-11 | /docs/functional-areas/payments/new-work/2025-12-11-per-ticket-cancellation-flags/ | CREATED | Feature work folder for per-ticket-purchase cancellation flags enhancement | Main Agent: Per-Ticket Cancellation Flags Implementation Plan | ACTIVE | Never |
| 2025-12-11 | /docs/functional-areas/payments/new-work/2025-12-11-per-ticket-cancellation-flags/implementation-plan.md | CREATED | Comprehensive implementation plan for per-ticket-purchase cancellation eligibility tracking - Fixes multi-session event bug where cancellation is blocked based on event start date instead of individual session timing. Complete step-by-step guide with backend DTO changes, service logic updates, frontend UI enhancements, testing requirements, rollback plan. | Main Agent: Per-Ticket Cancellation Flags Implementation Plan | ACTIVE | Never |
| 2025-12-11 | /docs/functional-areas/payments/new-work/2025-12-11-per-ticket-cancellation-flags/README.md | CREATED | Overview document for per-ticket-purchase cancellation flags feature - Problem statement, solution summary, key changes, timeline, related work references | Main Agent: Per-Ticket Cancellation Flags Implementation Plan | ACTIVE | Never |
| 2025-12-10 | /tests/e2e/vetting-admin-dashboard.spec.ts | MODIFIED | Migrated from seed data dependency to DataFactory pattern - Removed beforeEach/afterEach hooks, now creates own test users/applications | Test Developer: DataFactory Migration | ACTIVE | Never |
| 2025-12-10 | /docs/standards-processes/testing/TEST_CATALOG.md | MODIFIED | Added vetting-admin-dashboard test migration entry (Version 12.04.0) | Test Developer: DataFactory Migration | ACTIVE | Never |
| 2025-12-10 | /tests/e2e/session-based-timing.spec.ts | MODIFIED | Migrated from seed data dependency to DataFactory pattern - Now creates own test data with df fixture | Test Developer: DataFactory Migration | ACTIVE | Never |
| 2025-12-10 | /docs/standards-processes/testing/TEST_CATALOG.md | MODIFIED | Updated session-based-timing test entry to reflect DataFactory migration (6 tests, no seed data dependency) | Test Developer: DataFactory Migration | ACTIVE | Never |
| 2025-12-10 | /docs/functional-areas/testing/new-work/2025-12-10-test-data-infrastructure/ | CREATED | New folder for test data infrastructure implementation | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /docs/functional-areas/testing/new-work/2025-12-10-test-data-infrastructure/implementation-plan.md | CREATED | Implementation plan for DataFactory pattern and backend endpoints | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /docs/functional-areas/testing/new-work/2025-12-10-test-data-infrastructure/backend-developer-instructions.md | CREATED | Detailed instructions for backend-developer agent to implement TestHelper endpoints | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /docs/functional-areas/testing/new-work/2025-12-10-test-data-infrastructure/datafactory-implementation-instructions.md | CREATED | Detailed instructions for DataFactory TypeScript implementation | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/ | CREATED | DataFactory test data infrastructure - centralized, AI-discoverable test data creation for E2E tests | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/index.ts | CREATED | Main DataFactory export - provides DataFactory class with all factories for test data creation | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/types.ts | CREATED | TypeScript types for all test data entities (users, events, sessions, tickets, etc.) | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/api-client.ts | CREATED | HTTP client for test helper API endpoints | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/factories/user.factory.ts | CREATED | User factory for creating/deleting test users | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/factories/event.factory.ts | CREATED | Event factory for creating/deleting test events | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/factories/session.factory.ts | CREATED | Session factory for creating/deleting test sessions | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/factories/ticket-type.factory.ts | CREATED | Ticket type factory for creating/deleting test ticket types | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/factories/ticket-purchase.factory.ts | CREATED | Ticket purchase factory for creating/deleting test purchases | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/factories/volunteer.factory.ts | CREATED | Volunteer position factory for creating/deleting test volunteer positions | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/factories/vetting.factory.ts | CREATED | Vetting application factory for creating/deleting test vetting applications | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/factories/index.ts | CREATED | Re-export all factory classes | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/scenarios/complete-event.scenario.ts | CREATED | Scenario factory for creating complete events with sessions and tickets | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/scenarios/index.ts | CREATED | Re-export all scenario functions | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /tests/lib/datafactory/README.md | CREATED | Documentation for DataFactory usage - AI-friendly with quick start, API reference, migration guide | Main Agent: Test Data Infrastructure | ACTIVE | Never |
| 2025-12-10 | /apps/api/Features/TestHelpers/Models/CreateTestEventRequest.cs | CREATED | Request model for test event creation | Backend Developer: Test Helper Endpoints | ACTIVE | Never |
| 2025-12-10 | /apps/api/Features/TestHelpers/Models/TestEventResponse.cs | CREATED | Response model for test event creation | Backend Developer: Test Helper Endpoints | ACTIVE | Never |
| 2025-12-10 | /apps/api/Features/TestHelpers/Models/CreateTestSessionRequest.cs | CREATED | Request model for test session creation | Backend Developer: Test Helper Endpoints | ACTIVE | Never |
| 2025-12-10 | /apps/api/Features/TestHelpers/Models/TestSessionResponse.cs | CREATED | Response model for test session creation | Backend Developer: Test Helper Endpoints | ACTIVE | Never |
| 2025-12-10 | /apps/api/Features/TestHelpers/Models/CreateTestTicketTypeRequest.cs | CREATED | Request model for test ticket type creation | Backend Developer: Test Helper Endpoints | ACTIVE | Never |
| 2025-12-10 | /apps/api/Features/TestHelpers/Models/TestTicketTypeResponse.cs | CREATED | Response model for test ticket type creation | Backend Developer: Test Helper Endpoints | ACTIVE | Never |
| 2025-12-10 | /apps/api/Features/TestHelpers/Models/CreateTestVolunteerPositionRequest.cs | CREATED | Request model for test volunteer position creation | Backend Developer: Test Helper Endpoints | ACTIVE | Never |
| 2025-12-10 | /apps/api/Features/TestHelpers/Models/TestVolunteerPositionResponse.cs | CREATED | Response model for test volunteer position creation | Backend Developer: Test Helper Endpoints | ACTIVE | Never |

(Continued in archive sections below...)

## File Organization Standards

### Permanent Files
Location examples:
- Application code: `/apps/api/`, `/apps/web/`
- Documentation: `/docs/functional-areas/`, `/docs/standards-processes/`
- Tests: `/tests/playwright/`, `/tests/integration/`

### Session Work Files
- Use: `/session-work/YYYY-MM-DD/` for temporary session files
- Cleanup: Review at end of session
- Archive: Move permanent findings to proper locations

### Naming Conventions
✅ Good: `authentication-analysis-2025-01-20.md`
✅ Good: `database-schema-migration-plan.md`
❌ Bad: `status.md`, `notes.md`, `temp.txt`

## Maintenance Guidelines

1. **Log ALL file operations** (create, modify, delete)
2. **Include descriptive purpose** (what and why)
3. **Set appropriate status** (ACTIVE, TEMPORARY, ARCHIVED)
4. **Review at session end** (cleanup temporary files)
5. **Update version number** when making changes

---

## Archive: October 2025 - November 2025

[Previous entries archived - see git history for full details]

---

**Last Review**: 2025-12-12
**Next Scheduled Review**: Weekly with Librarian Agent
**Registry Owner**: Librarian Agent
