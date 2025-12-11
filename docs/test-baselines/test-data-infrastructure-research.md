# Test Data Infrastructure Research Report

**Date**: 2025-12-10
**Purpose**: Comprehensive analysis of test data creation patterns across all test types
**Goal**: Propose consolidation solutions optimized for AI agent development

---

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Current State Analysis](#current-state-analysis)
3. [E2E Test Data Patterns](#e2e-test-data-patterns)
4. [Integration/Unit Test Patterns](#integrationunit-test-patterns)
5. [Backend TestHelpers Service](#backend-testhelpers-service)
6. [Existing Documentation Analysis](#existing-documentation-analysis)
7. [Industry Best Practices](#industry-best-practices)
8. [Critical Issues Identified](#critical-issues-identified)
9. [Proposed Solutions](#proposed-solutions)
10. [Recommendations](#recommendations)

---

## Executive Summary

### Key Findings

1. **Duplicate database-helpers.ts files** - Two versions exist with overlapping code
2. **Missing test helper endpoints** - Cannot create events, sessions, ticket types, or volunteer positions via API
3. **Heavy seed data dependency** - Many tests assume specific data exists rather than creating it
4. **Inconsistent patterns** - Different tests use different approaches (API, direct DB, UI-based)
5. **Poor discoverability** - AI agents cannot easily find what helpers exist

### Current Test Data Creation Methods

| Method | E2E Tests | Integration Tests | Unit Tests |
|--------|-----------|-------------------|------------|
| API Test Helpers | Users, Tickets | N/A | N/A |
| Direct Database | Verification only | Primary method | Primary method |
| UI-based creation | Some tests | N/A | N/A |
| Seed data assumption | Heavy reliance | Light (5 users) | Light |
| Builder pattern | N/A | DTOs only | DTOs only |

### What Can Be Created Programmatically Today

| Entity | E2E (API) | Integration (DbContext) | Missing Capability |
|--------|-----------|------------------------|-------------------|
| Users | YES | YES | None |
| Events | NO | YES | E2E API endpoint |
| Sessions | NO | YES | E2E API endpoint |
| Ticket Types | NO | YES | E2E API endpoint |
| Ticket Purchases | YES | YES | None |
| Volunteer Positions | NO | YES | E2E API endpoint |
| Vetting Applications | NO | NO | Both environments |

---

## Current State Analysis

### Test Directory Structure

```
tests/
├── e2e/                          # Playwright E2E tests
│   ├── test-utils/               # NEWER helpers location
│   │   ├── helpers/              # Auth, form, payment helpers
│   │   └── utils/                # Database helpers (NEWER)
│   ├── utils/                    # OLDER database helpers (DUPLICATE)
│   └── *.spec.ts                 # Test files
├── integration/                  # C# integration tests
│   ├── IntegrationTestBase.cs    # Base class with helpers
│   └── Features/                 # Feature-specific tests
├── unit/                         # C# unit tests
│   └── api/
│       └── TestBase/             # DatabaseTestBase
└── WitchCityRope.Tests.Common/   # Shared test infrastructure
    ├── Builders/                 # DTO builders (fluent pattern)
    └── Fixtures/                 # Database fixtures
```

---

## E2E Test Data Patterns

### Helper Files That Create Test Data

| File | Creates | Method | Issues |
|------|---------|--------|--------|
| `/e2e/utils/database-helpers.ts` | Users (via API) | POST /api/test-helpers/users | DUPLICATE - older version |
| `/e2e/test-utils/utils/database-helpers.ts` | Users (via API) | POST /api/test-helpers/users | DUPLICATE - newer, has session helpers |
| `/e2e/test-utils/helpers/payment.helper.ts` | Ticket Purchases | POST /api/test-helpers/ticket-purchases | Works correctly |
| `/e2e/test-utils/helpers/auth.helpers.ts` | N/A (login only) | Uses seed data | Relies on 5 hardcoded accounts |
| `/e2e/test-utils/helpers/event.helpers.ts` | N/A (fetch only) | GET /api/events | Cannot create events |

### API Test Helper Endpoints Used

```typescript
// User Creation
POST /api/test-helpers/users
Body: { email, password, sceneName, firstName, lastName, role, dateOfBirth }
Response: { id, email, sceneName, role, createdAt }

// User Deletion (cleanup)
DELETE /api/test-helpers/users/{userId}

// Ticket Purchase Creation
POST /api/test-helpers/ticket-purchases
Body: { totalPrice, paymentMethod, paymentStatus, userId?, ticketTypeId?, quantity }
Response: { id, paymentReference, totalPrice, paymentMethod, paymentStatus }

// Ticket Purchase Deletion (cleanup)
DELETE /api/test-helpers/ticket-purchases/{ticketPurchaseId}

// Email Verification
POST /api/test-helpers/verify-email
Body: { email }
```

### Direct Database Access Functions

```typescript
// Database-helpers.ts functions (both versions)

// QUERIES (verification, not creation)
verifyProfileFields(userId, expectedFields)
getUserIdFromEmail(email)
verifyEventParticipation(userId, eventId, expectedStatus, expectedType)
verifyNoEventParticipation(userId, eventId)
verifyEventExists(eventId)
getTestableEvents()
getFirstRsvpEvent()
getFirstTicketEvent()
verifyVettingApplicationStatus(userId, expectedStatus)
verifyAuditLogExists(tableName, entityId, action)

// DATA CREATION
createTestUser(options)              // Via API, not direct DB
generateUniqueTestEmail(prefix)      // Helper for uniqueness

// MANIPULATION (newer version only)
updateSessionStartTime(sessionId, newStartTime)
getEventSessions(eventId)

// CLEANUP
cleanupTestUser(emailOrId)
cleanupTestData(tableName, ids)
closeDatabaseConnections()
```

### Tests Relying on Seed Data

**Hardcoded Accounts** (from `auth.helpers.ts`):
- `admin@witchcityrope.com` / Test123!
- `teacher@witchcityrope.com` / Test123!
- `vetted@witchcityrope.com` / Test123!
- `member@witchcityrope.com` / Test123!
- `guest@witchcityrope.com` / Test123!

**Tests Using Seed Events**:
- `session-ticket-availability.spec.ts` - Expects "Session Timing Test Event"
- `session-based-timing.spec.ts` - Expects events with specific timing configs
- `comprehensive-timing-tests.spec.ts` - Expects "Timing Test - *" events
- All tests using `getFirstRsvpEvent()` or `getFirstTicketEvent()`

---

## Integration/Unit Test Patterns

### Base Test Classes

| Class | Location | Purpose | Key Methods |
|-------|----------|---------|-------------|
| `IntegrationTestBase` | `/tests/integration/IntegrationTestBase.cs` | HTTP integration tests | `CreateTestVenueAsync()`, `GenerateJwtToken()`, `CreateAuthenticatedClientWithCsrfAsync()` |
| `DtoMappingTestBase` | `/tests/integration/DtoMappingTestBase.cs` | DTO validation tests | `AssertDtoPropertiesExistOnEntity<TDto, TEntity>()` |
| `DatabaseTestBase` | `/tests/WitchCityRope.Tests.Common/TestBase/DatabaseTestBase.cs` | Unit tests with DB | `CreateNewDbContext()`, `VerifyEntitySaved<T>()` |

### Builder Pattern (DTOs Only)

```csharp
// Available Builders (in /tests/WitchCityRope.Tests.Common/Builders/)
EventDtoBuilder          // Fluent EventDto creation
UserDtoBuilder           // Fluent UserDto creation
LoginRequestBuilder      // Login request creation
RegisterUserRequestBuilder  // Registration request creation
CreateEventRequestBuilder   // Event creation API request

// Example Usage:
var eventDto = new EventDtoBuilder()
    .WithTitle("Test Workshop")
    .WithStartDate(DateTime.UtcNow.AddDays(7))
    .AsClassEvent()
    .WithCapacity(20)
    .Build();
```

### Direct DbContext Entity Creation Patterns

```csharp
// Pattern 1: Simple Entity
var venue = new Venue { Name = "Test Venue", Location = "123 Test St", IsActive = true };
context.Venues.Add(venue);
await context.SaveChangesAsync();

// Pattern 2: Event with Sessions
var evt = new Event { Title = "Test", VenueId = venueId, StartDate = DateTime.UtcNow.AddDays(7) };
evt.Sessions.Add(new Session { EventId = evt.Id, SessionCode = "S1", StartTime = evt.StartDate });
context.Events.Add(evt);
await context.SaveChangesAsync();

// Pattern 3: Event with Session-Ticket Mapping (Many-to-Many)
var ticket = new TicketType { EventId = evt.Id, Name = "Regular", Price = 25m };
ticket.Sessions.Add(session);  // Link to session
evt.TicketTypes.Add(ticket);
await context.SaveChangesAsync();

// Pattern 4: Ticket Purchase with Attendance
var purchase = new TicketPurchase { TicketTypeId = ticketType.Id, UserId = userId };
var attendance = new EventAttendance {
    EventId = eventId, UserId = userId,
    AttendanceType = AttendanceType.Ticket,
    TicketPurchaseId = purchase.Id
};
context.TicketPurchases.Add(purchase);
context.EventAttendances.Add(attendance);
await context.SaveChangesAsync();
```

### Helper Methods Found in Test Classes

```csharp
// Common patterns repeated across multiple test files:
CreateTestVenueAsync(string? name = null)
CreateTestSessionAsync(Guid eventId, DateTime startTime, string name)
CreateTestTicketTypeAsync(Guid eventId, Guid? sessionId, string name)
CreateTestTicketAsync(Guid eventId, Guid userId, Guid ticketTypeId)
CreateAuthenticatedUserAsync(string email) // Returns (HttpClient, Guid userId)
CreateTestVolunteerPositionAsync(DateTime startDateTime, decimal? closeHours, ...)
```

---

## Backend TestHelpers Service

### Available Endpoints

| Endpoint | Method | Creates | Environment |
|----------|--------|---------|-------------|
| `/api/test-helpers/users` | POST | ApplicationUser | Dev/Test only |
| `/api/test-helpers/users/{userId}` | DELETE | - | Dev/Test only |
| `/api/test-helpers/ticket-purchases` | POST | TicketPurchase + EventAttendance | Dev/Test only |
| `/api/test-helpers/ticket-purchases/{id}` | DELETE | - | Dev/Test only |
| `/api/test-helpers/verify-email` | POST | - (updates EmailConfirmed) | Dev/Test only |
| `/api/test-helpers/health` | GET | - | Dev/Test only |

### Environment Restriction Implementation

```csharp
// In TestHelperEndpoints.cs (lines 18-24)
var environment = app.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
if (!environment.IsDevelopment() && environment.EnvironmentName != "Test")
{
    return; // Endpoints not registered in production
}
```

### Missing Endpoints (Cannot Create)

| Entity | Needed For | Workaround Today |
|--------|------------|------------------|
| Events | Session timing tests, check-in tests | Rely on seed data |
| Sessions | Multi-session ticket tests | Rely on seed data |
| Ticket Types | Ticket purchase tests | Uses first available |
| Volunteer Positions | Volunteer workflow tests | Rely on seed data |
| Vetting Applications | Vetting workflow tests | None - tests blocked |

---

## Existing Documentation Analysis

### Well-Documented

| Document | Coverage | Quality |
|----------|----------|---------|
| `integration-test-patterns.md` | PostgreSQL patterns, health checks, transactions | Comprehensive |
| `TEST-CREATION-GUIDE.md` | TestHelperService, uniqueness, seeded users | Good overview |
| `test-developer-lessons-learned.md` | Common mistakes, solutions | Practical |

### Gaps in Documentation

1. **No seeded data catalog** - What exactly exists after seeding?
2. **No E2E test data patterns** - How to create data from E2E tests
3. **No builder patterns for entities** - Only DTO builders exist
4. **No factory pattern documentation** - No factory classes exist
5. **No cleanup strategy documentation** - Respawn partially mentioned
6. **Missing: Complex entity creation** - Events with sessions, tickets

---

## Industry Best Practices

### Playwright-Recommended Patterns

1. **DataFactory Pattern** (Playwright Solutions)
   - Centralized `lib/datafactory/` with factory functions
   - Functions like `createUser()`, `createEvent()` that return complete entities
   - API-based creation, not direct DB

2. **Playwright Fixtures Pattern**
   - Worker-scoped for expensive setup (auth, DB init)
   - Test-scoped for test-specific data
   - Automatic cleanup via fixture teardown

3. **Parallel Test Safety**
   - Unique identifiers (GUIDs, timestamps) for all test data
   - Worker-isolated data or data prefixed with worker ID
   - `expect().toPass()` for retry-safe assertions

### .NET Integration Test Patterns

1. **WebApplicationFactory + IAsyncLifetime**
   - Factory creates test server
   - IAsyncLifetime manages setup/teardown

2. **Test Data Builders (Entity Level)**
   - Fluent builders for entities, not just DTOs
   - Optional overrides with sensible defaults

3. **Database Per Test Class**
   - Respawn for fast cleanup between tests
   - Transaction rollback for simpler cases

### AI-Agent Discoverability Patterns

1. **Central README at `/tests/lib/datafactory/README.md`**
2. **Comprehensive JSDoc/XML comments with `@example` tags**
3. **Naming conventions**: `create{Entity}`, `create{Entity}Batch`
4. **Factory registry file** listing all available factories

---

## Critical Issues Identified

### Issue 1: Duplicate database-helpers.ts (CRITICAL)

**Impact**: Maintenance nightmare, inconsistent behavior
**Files**:
- `/tests/e2e/utils/database-helpers.ts` (748 lines, older)
- `/tests/e2e/test-utils/utils/database-helpers.ts` (810 lines, newer with session helpers)

**Evidence**: Some tests import from `./utils/`, others from `./test-utils/utils/`

### Issue 2: Cannot Create Events via API (CRITICAL)

**Impact**: Tests relying on seed data break when database is reset
**Blocked Tests**:
- session-ticket-availability.spec.ts (6 tests)
- session-based-timing.spec.ts (5 tests)
- admin-checkin-sessions.spec.ts (8 tests)
- And more...

### Issue 3: Cannot Create Vetting Applications (HIGH)

**Impact**: 13 vetting workflow tests cannot be fixed
**Blocker**: No backend endpoint to programmatically create vetting applications

### Issue 4: Seed Data Dependency (HIGH)

**Impact**: Tests fail when seed data missing or stale
**Examples**:
- "Session Timing Test Event" must exist with specific session dates
- 5 hardcoded user accounts must exist
- Events must have future dates (seed data goes stale over time)

### Issue 5: Poor AI Discoverability (MEDIUM)

**Impact**: AI agents create duplicate patterns, miss existing helpers
**Evidence**:
- No central registry of test helpers
- Helpers spread across multiple directories
- Inconsistent naming conventions

---

## Proposed Solutions

### Solution A: Minimal Fix - Consolidate Existing

**Effort**: Low (1-2 days)
**Scope**: Fix immediate duplication issues

1. **Delete older database-helpers.ts** (`/tests/e2e/utils/`)
2. **Update all imports** to use `/tests/e2e/test-utils/utils/database-helpers.ts`
3. **Add README.md** to `/tests/e2e/test-utils/` documenting available helpers
4. **Add missing TestHelper endpoints** for events, sessions, ticket types

**Pros**: Quick, minimal disruption
**Cons**: Doesn't address architectural issues, still seed-data dependent

### Solution B: DataFactory Pattern (Playwright-Aligned)

**Effort**: Medium (1 week)
**Scope**: Create unified data creation layer

1. **Create `/tests/lib/datafactory/`** structure:
   ```
   tests/lib/datafactory/
   ├── README.md              # Central discovery doc
   ├── index.ts               # Export all factories
   ├── user.factory.ts        # createUser(), createUserBatch()
   ├── event.factory.ts       # createEvent(), createEventWithSessions()
   ├── ticket.factory.ts      # createTicketType(), createTicketPurchase()
   └── volunteer.factory.ts   # createVolunteerPosition()
   ```

2. **Backend endpoints** for each entity type
3. **TypeScript interfaces** aligned with API responses
4. **Fixtures integration** for lifecycle management
5. **Migration guide** for converting existing tests

**Pros**: AI-discoverable, consistent patterns, proper isolation
**Cons**: Significant refactoring effort, learning curve

### Solution C: Full Test Data Infrastructure (Comprehensive)

**Effort**: High (2-3 weeks)
**Scope**: Complete redesign

1. **Everything in Solution B**, plus:
2. **Scenario Factories** for common test setups:
   ```typescript
   // Creates event + sessions + ticket types + venue in one call
   createCompleteEventScenario({
     sessions: 2,
     ticketTypes: 3,
     registrationCloseHours: 12
   })
   ```

3. **Seeded Data Catalog** documenting what exists
4. **Test Data Fixtures** (Playwright fixtures for common setups)
5. **Cleanup Automation** (automatic rollback/deletion)
6. **Documentation overhaul** with examples

**Pros**: Future-proof, highly AI-discoverable, reduces test flakiness
**Cons**: Large investment, potential for over-engineering

---

## Recommendations

### Immediate Actions (This Week)

1. **Fix duplicate database-helpers.ts** - Delete older file, update imports
2. **Add event creation endpoint** to TestHelperService
3. **Add session creation endpoint** to TestHelperService
4. **Add ticket type creation endpoint** to TestHelperService
5. **Create README.md** in test-utils/ documenting available helpers

### Short-Term (Next 2 Weeks)

1. **Implement Solution B** (DataFactory pattern)
2. **Add vetting application endpoint** (unblocks 13 tests)
3. **Convert 5-10 seed-dependent tests** to use factories
4. **Document new patterns** in TEST-CREATION-GUIDE.md

### Long-Term (Next Month)

1. **Complete migration** to DataFactory pattern
2. **Add scenario factories** for common test setups
3. **Remove seed data dependencies** from all tests
4. **Performance optimization** (batch creation, caching)

---

## Appendix: Files to Update

### Priority 1: Delete/Consolidate
- [ ] DELETE: `/tests/e2e/utils/database-helpers.ts` (keep backup)
- [ ] UPDATE: All files importing from `./utils/database-helpers`

### Priority 2: Add Endpoints
- [ ] ADD: `/api/test-helpers/events` (POST, DELETE)
- [ ] ADD: `/api/test-helpers/sessions` (POST, DELETE)
- [ ] ADD: `/api/test-helpers/ticket-types` (POST, DELETE)
- [ ] ADD: `/api/test-helpers/volunteer-positions` (POST, DELETE)
- [ ] ADD: `/api/test-helpers/vetting-applications` (POST, DELETE)

### Priority 3: Documentation
- [ ] CREATE: `/tests/e2e/test-utils/README.md`
- [ ] UPDATE: `TEST-CREATION-GUIDE.md` with E2E patterns
- [ ] CREATE: Seeded data catalog document

---

## References

### Codebase Files Analyzed
- `/tests/e2e/utils/database-helpers.ts`
- `/tests/e2e/test-utils/utils/database-helpers.ts`
- `/tests/e2e/test-utils/helpers/*.ts`
- `/apps/api/Features/TestHelpers/**/*`
- `/tests/integration/IntegrationTestBase.cs`
- `/tests/WitchCityRope.Tests.Common/**/*`

### Documentation Reviewed
- `/docs/standards-processes/testing/TEST-CREATION-GUIDE.md`
- `/docs/standards-processes/testing/integration-test-patterns.md`
- `/docs/lessons-learned/test-developer-lessons-learned.md`

### External Sources
- Playwright Solutions: DataFactory Pattern
- Microsoft Learn: ASP.NET Core Integration Testing
- CircleCI: Playwright Fixtures Deep Dive
- Fishery: TypeScript Factory Library
