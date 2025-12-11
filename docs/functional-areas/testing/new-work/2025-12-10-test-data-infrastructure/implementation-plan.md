# Test Data Infrastructure Implementation Plan

**Date**: 2025-12-10
**Status**: Phase 3 Complete - ALL E2E TESTS MIGRATED
**Priority**: Critical - Foundation for all E2E test fixes
**Last Updated**: 2025-12-10

## Executive Summary

This plan implements a centralized test data infrastructure following the **DataFactory Pattern** (Solution B) with design considerations for future **Full Infrastructure** expansion (Solution C). The goal is to enable E2E tests to create their own data programmatically, eliminating reliance on seed data.

**COMPLETED**: All 41 E2E test files have been migrated to use the DataFactory pattern. Tests no longer rely on seed data and create their own isolated test data with automatic cleanup.

## Problem Statement

Current E2E tests suffer from:
1. **Seed data dependency**: Tests rely on pre-populated data that breaks on database reseed
2. **Duplicate helper files**: Two `database-helpers.ts` files causing confusion
3. **Incomplete API endpoints**: TestHelperService cannot create events, sessions, ticket types
4. **Poor discoverability**: AI agents struggle to find correct patterns

## Solution Architecture

### Phase 1: DataFactory Pattern (Solution B)

Create a centralized `/tests/lib/datafactory/` structure:

```
/tests/lib/datafactory/
├── index.ts                    # Main export file
├── types.ts                    # Shared TypeScript types
├── api-client.ts               # HTTP client for test helper API
├── factories/
│   ├── user.factory.ts         # User creation (EXISTS via API)
│   ├── event.factory.ts        # Event creation (NEEDS API endpoint)
│   ├── session.factory.ts      # Session creation (NEEDS API endpoint)
│   ├── ticket-type.factory.ts  # Ticket type creation (NEEDS API endpoint)
│   ├── ticket-purchase.factory.ts  # Purchase creation (EXISTS via API)
│   ├── volunteer.factory.ts    # Volunteer position creation (NEEDS API endpoint)
│   └── vetting.factory.ts      # Vetting application creation (NEEDS API endpoint)
└── README.md                   # Documentation for AI discoverability
```

### Phase 2: Full Infrastructure (Solution C)

Extend with scenario factories and comprehensive documentation:

```
/tests/lib/datafactory/
├── ... (Phase 1 files)
├── scenarios/
│   ├── complete-event.scenario.ts      # Event with sessions, tickets, attendees
│   ├── vetting-workflow.scenario.ts    # Full vetting application flow
│   ├── volunteer-event.scenario.ts     # Event with volunteer positions
│   └── ticketed-event.scenario.ts      # Event with ticket sales
└── fixtures/
    ├── worker.fixture.ts               # Playwright worker-scoped fixtures
    └── test.fixture.ts                 # Playwright test-scoped fixtures
```

## Backend API Endpoints Required

### Missing Endpoints to Add

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/test-helpers/events` | POST | Create test event |
| `/api/test-helpers/events/{id}` | DELETE | Delete test event |
| `/api/test-helpers/sessions` | POST | Create test session |
| `/api/test-helpers/sessions/{id}` | DELETE | Delete test session |
| `/api/test-helpers/ticket-types` | POST | Create test ticket type |
| `/api/test-helpers/ticket-types/{id}` | DELETE | Delete test ticket type |
| `/api/test-helpers/volunteer-positions` | POST | Create volunteer position |
| `/api/test-helpers/volunteer-positions/{id}` | DELETE | Delete volunteer position |
| `/api/test-helpers/vetting-applications` | POST | Create vetting application |
| `/api/test-helpers/vetting-applications/{id}` | DELETE | Delete vetting application |

### Existing Endpoints (Already Work)

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/test-helpers/users` | POST | Create test user |
| `/api/test-helpers/users/{id}` | DELETE | Delete test user |
| `/api/test-helpers/ticket-purchases` | POST | Create ticket purchase |
| `/api/test-helpers/ticket-purchases/{id}` | DELETE | Delete ticket purchase |
| `/api/test-helpers/verify-email` | POST | Verify user email |
| `/api/test-helpers/health` | GET | Health check |

## Implementation Phases

### Phase 1A: Backend Endpoints (backend-developer)

**Priority**: HIGHEST - Blocks all other work

1. Add event creation/deletion endpoints
2. Add session creation/deletion endpoints
3. Add ticket type creation/deletion endpoints
4. Add volunteer position creation/deletion endpoints
5. Add vetting application creation/deletion endpoints

### Phase 1B: DataFactory TypeScript (react-developer or main agent)

**Priority**: HIGH - Can start after 1A completes

1. Create `/tests/lib/datafactory/` structure
2. Create `api-client.ts` with typed HTTP methods
3. Create factory files for each entity type
4. Create `index.ts` with consolidated exports
5. Create `README.md` with AI-friendly documentation

### Phase 1C: Database Helpers Consolidation

**Priority**: MEDIUM - Cleanup task

1. Delete `/tests/e2e/utils/database-helpers.ts` (older version)
2. Keep `/tests/e2e/test-utils/utils/database-helpers.ts` (newer version)
3. Update all imports to use the kept version
4. Eventually deprecate direct database access in favor of API factories

### Phase 2: Scenario Factories (Future)

**Priority**: LOWER - After Phase 1 complete

1. Create scenario factories for common test setups
2. Create Playwright fixtures for lifecycle management
3. Update existing tests to use new infrastructure

## Success Criteria

1. All E2E tests can create their own data via API
2. No reliance on seed data for test execution
3. Single source of truth for test data creation
4. AI agents can easily discover and use factories
5. Test isolation: each test creates and cleans up its own data

## Files to Create/Modify

### New Files (Phase 1)

| File | Responsibility |
|------|----------------|
| `/apps/api/Features/TestHelpers/Models/CreateTestEventRequest.cs` | Event creation DTO |
| `/apps/api/Features/TestHelpers/Models/CreateTestSessionRequest.cs` | Session creation DTO |
| `/apps/api/Features/TestHelpers/Models/CreateTestTicketTypeRequest.cs` | Ticket type creation DTO |
| `/apps/api/Features/TestHelpers/Models/CreateTestVolunteerPositionRequest.cs` | Volunteer position creation DTO |
| `/apps/api/Features/TestHelpers/Models/CreateTestVettingApplicationRequest.cs` | Vetting application creation DTO |
| `/tests/lib/datafactory/index.ts` | Main exports |
| `/tests/lib/datafactory/types.ts` | TypeScript types |
| `/tests/lib/datafactory/api-client.ts` | HTTP client |
| `/tests/lib/datafactory/factories/*.factory.ts` | Entity factories |
| `/tests/lib/datafactory/README.md` | Documentation |

### Modified Files

| File | Change |
|------|--------|
| `/apps/api/Features/TestHelpers/Services/ITestHelperService.cs` | Add new method signatures |
| `/apps/api/Features/TestHelpers/Services/TestHelperService.cs` | Implement new methods |
| `/apps/api/Features/TestHelpers/Endpoints/TestHelperEndpoints.cs` | Add new endpoints |

### Deleted Files

| File | Reason |
|------|--------|
| `/tests/e2e/utils/database-helpers.ts` | Duplicate of newer version |
| `/tests/e2e/ticket-purchase-e2e.spec.ts` | Replaced by datafactory version |

## Sub-Agent Delegation

### backend-developer Agent Task

See: [backend-developer-instructions.md](./backend-developer-instructions.md)

### DataFactory Implementation Task

See: [datafactory-implementation-instructions.md](./datafactory-implementation-instructions.md)

## Implementation Progress

### Phase 1: Complete ✅

| Task | Status | Notes |
|------|--------|-------|
| Backend endpoints (10 endpoints) | ✅ Complete | Events, sessions, ticket-types, volunteer-positions, vetting-applications |
| DataFactory TypeScript structure | ✅ Complete | All factories created with proper types |
| API client with typed methods | ✅ Complete | `/tests/lib/datafactory/api-client.ts` |
| Factory files for entities | ✅ Complete | 7 factory files created |
| README with AI documentation | ✅ Complete | `/tests/lib/datafactory/README.md` |
| Database helpers consolidation | ✅ Complete | Re-export pattern used |
| Lessons learned updates | ✅ Complete | test-developer, backend-developer, test-executor |

### Phase 2: Complete ✅

| Task | Status | Notes |
|------|--------|-------|
| Scenario factories | ✅ Complete | complete-event, vetting-workflow, volunteer-event, ticketed-event |
| Playwright fixtures | ✅ Complete | test.fixture.ts (test-scoped), worker.fixture.ts (worker-scoped) |
| Migrate proof-of-concept test | ✅ Complete | ticket-purchase-e2e-datafactory.spec.ts |

### Phase 3: Complete ✅

| Task | Status | Notes |
|------|--------|-------|
| Migrate remaining E2E tests | ✅ Complete | 41 files now use DataFactory pattern |
| Remove seed data dependencies | ✅ Complete | All tests create own data |
| Remove old API patterns | ✅ Complete | No more getCsrfToken/apiRequest helpers |
| TypeScript compilation | ✅ Complete | All files compile without errors |
| Multi-session ticket support | ✅ Complete | Added `sessionIds` array to TicketTypeFactory |

## Verification Checklist

After implementation, verify:

- [x] All new endpoints return 201/204 for success
- [x] All endpoints have proper error handling
- [x] All factories can create and delete entities
- [x] TypeScript compiles without errors
- [x] API health check still works
- [x] Database helpers consolidated (re-export pattern)
- [x] Lessons learned files updated
- [x] Tests can run in isolation (no seed data dependency)
- [x] All imports updated to use DataFactory fixture

## Files Created

### Backend (10 new endpoints)

- `/apps/api/Features/TestHelpers/Models/CreateTestEventRequest.cs`
- `/apps/api/Features/TestHelpers/Models/CreateTestEventResponse.cs`
- `/apps/api/Features/TestHelpers/Models/CreateTestSessionRequest.cs`
- `/apps/api/Features/TestHelpers/Models/CreateTestSessionResponse.cs`
- `/apps/api/Features/TestHelpers/Models/CreateTestTicketTypeRequest.cs`
- `/apps/api/Features/TestHelpers/Models/CreateTestTicketTypeResponse.cs`
- `/apps/api/Features/TestHelpers/Models/CreateTestVolunteerPositionRequest.cs`
- `/apps/api/Features/TestHelpers/Models/CreateTestVolunteerPositionResponse.cs`
- `/apps/api/Features/TestHelpers/Models/CreateTestVettingApplicationRequest.cs`
- `/apps/api/Features/TestHelpers/Models/CreateTestVettingApplicationResponse.cs`

### DataFactory TypeScript

- `/tests/lib/datafactory/index.ts` - Main exports
- `/tests/lib/datafactory/types.ts` - TypeScript types (enhanced with `sessionIds` for multi-session tickets)
- `/tests/lib/datafactory/api-client.ts` - HTTP client
- `/tests/lib/datafactory/README.md` - AI-friendly documentation
- `/tests/lib/datafactory/factories/user.factory.ts`
- `/tests/lib/datafactory/factories/event.factory.ts`
- `/tests/lib/datafactory/factories/session.factory.ts`
- `/tests/lib/datafactory/factories/ticket-type.factory.ts` (enhanced with multi-session support)
- `/tests/lib/datafactory/factories/ticket-purchase.factory.ts`
- `/tests/lib/datafactory/factories/volunteer.factory.ts`
- `/tests/lib/datafactory/factories/vetting.factory.ts`
- `/tests/lib/datafactory/factories/index.ts`

### Scenarios

- `/tests/lib/datafactory/scenarios/complete-event.scenario.ts`
- `/tests/lib/datafactory/scenarios/vetting-workflow.scenario.ts`
- `/tests/lib/datafactory/scenarios/volunteer-event.scenario.ts`
- `/tests/lib/datafactory/scenarios/ticketed-event.scenario.ts`
- `/tests/lib/datafactory/scenarios/index.ts`

### Fixtures

- `/tests/lib/datafactory/fixtures/test.fixture.ts` - Test-scoped with auto-cleanup
- `/tests/lib/datafactory/fixtures/worker.fixture.ts` - Worker-scoped for shared data
- `/tests/lib/datafactory/fixtures/index.ts`

## Migrated E2E Test Files (41 total)

### Ticket/Purchase Tests
- `ticket-purchase-e2e-datafactory.spec.ts` - Original proof-of-concept
- `multi-ticket-purchase.spec.ts`
- `ticket-cancellation-selective.spec.ts`
- `ticket-lifecycle-persistence.spec.ts`
- `ticket-refund-workflow.spec.ts`
- `admin-variable-refund.spec.ts`

### Session/Timing Tests
- `session-ticket-availability.spec.ts` - Migrated from "Session Timing Test Event" seed data
- `session-based-timing.spec.ts` - Migrated from seed data
- `session-availability-counts.spec.ts`
- `session-based-ticket-timing.spec.ts`
- `session-based-volunteer-timing.spec.ts`
- `comprehensive-timing-tests.spec.ts`

### Event Admin Tests
- `admin-events-workflow.spec.ts`
- `admin-events-dependencies.spec.ts` - Migrated from seed data
- `admin-events-sessions.spec.ts`
- `admin-events-volunteers.spec.ts`
- `admin-session-deletion.spec.ts`
- `admin-tickettype-deletion.spec.ts`
- `events/admin-event-copy.spec.ts` - Migrated from seed data

### Check-in Tests
- `checkin-staff-authentication.spec.ts` - Removed beforeAll, now creates data
- `checkin-dashboard.spec.ts`
- `checkin-attendee-workflow.spec.ts`
- `admin-checkin-sessions.spec.ts`

### Vetting Tests
- `vetting-workflow.spec.ts`
- `vetting-workflow-integration.spec.ts`
- `vetting-admin-dashboard.spec.ts` - Migrated to fixture pattern
- `vetting-application-detail.spec.ts` - Migrated to fixture pattern
- `vetting-profile-update.spec.ts`
- `vetting-complete-flow.spec.ts`

### Volunteer Tests
- `volunteer-session-validation.spec.ts`
- `volunteer-auto-cancel.spec.ts`

### Email Template Tests
- `vetting-email-templates.spec.ts` - Migrated to fixture pattern
- `admin-email-templates-triggers.spec.ts` - Migrated to fixture pattern

### Other Tests
- `venue-display.spec.ts` - Migrated from seed data
- `event-update-complete-flow.spec.ts` - Migrated to fixture pattern
- `profile-update-persistence.spec.ts`
- `profile-update-full-persistence.spec.ts`
- `rsvp-lifecycle-persistence.spec.ts`
- `comprehensive-rsvp-verification.spec.ts`
- `registration-tos.spec.ts`
- `admin-member-history.spec.ts`

### UI-Only Tests (Not Migrated - Don't Create Data)
- `home-page.spec.ts` - Simple UI verification
- `events-basic-validation.spec.ts` - UI verification only

## Key Enhancements Made During Migration

### Multi-Session Ticket Support

Added support for tickets that span multiple sessions:

```typescript
// types.ts - Added sessionIds array
export interface CreateTicketTypeParams {
  sessionId?: string;        // Single session (backward compatible)
  sessionIds?: string[];     // Multiple sessions (new)
  eventId?: string;          // Required by backend
  name: string;
  price: number;
  quantityAvailable: number;
}

// Usage in tests:
await df.ticketTypes.create({
  sessionIds: [session1.id, session2.id],  // Multi-session
  eventId: event.id,
  name: 'Both Sessions Ticket',
  price: 40,
  quantityAvailable: 20,
});
```

### Removed Old Patterns

- ❌ `getCsrfToken()` helper function - No longer needed
- ❌ `apiRequest()` helper function - Replaced by DataFactory
- ❌ `test.beforeAll` for data setup - Each test creates own data
- ❌ Seed data dependencies - Tests are fully isolated
- ❌ `let testEventId` shared variables - Tests use created event IDs

### New Pattern

```typescript
import { expect } from '@playwright/test';
import { test } from '../lib/datafactory/fixtures/test.fixture';

test('test name', async ({ page, df }) => {
  // Create isolated test data
  const event = await df.events.createPublished(`Test Event ${Date.now()}`);
  const session = await df.sessions.create({ eventId: event.id, ... });
  const ticketType = await df.ticketTypes.create({ sessionId: session.id, ... });

  // Run test
  await page.goto(`/events/${event.id}`);

  // Automatic cleanup - no manual cleanup needed
});
```

## Related Documentation

- Research Document: `/docs/test-baselines/test-data-infrastructure-research.md`
- Existing TestHelpers: `/apps/api/Features/TestHelpers/`
- E2E Test Utils: `/tests/e2e/test-utils/`
- DataFactory README: `/tests/lib/datafactory/README.md`
