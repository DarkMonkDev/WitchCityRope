# Backend Developer Lessons Learned - Part 4

<!-- STRICT FORMAT: Only prevention patterns and mistakes. NO status reports, NO project history, NO celebrations. See LESSONS-LEARNED-TEMPLATE.md -->

## 📚 MULTI-FILE LESSONS LEARNED
**Files**: 4 total
**Part 1**: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned.md` (STARTUP + LESSONS)
**Part 2**: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned-2.md` (LESSONS FILE)
**Part 3**: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned-3.md` (LESSONS FILE)
**Part 4**: `/home/chad/repos/witchcityrope/docs/lessons-learned/backend-developer-lessons-learned-4.md` (THIS FILE - CURRENT WRITE TARGET)
**Read ALL**: Part 1, Part 2, Part 3, AND Part 4 are MANDATORY
**Write to**: Part 4 ONLY - **NEVER ADD NEW LESSONS TO PART 1, 2, OR 3**
**Maximum file size**: 2,000 lines (to stay under token limits). All parts can be up to 2,000 lines each
**IF READ FAILS**: STOP and use lessons-learned-validator skill to fix immediately

---

## 🚨 CRITICAL: AUTHENTICATION ENDPOINT CSRF PROTECTION - ALWAYS REQUIRED 🚨

### ⚠️ PROBLEM: Logout endpoint failing with "CSRF Validation Failed" after CSRF implementation
**DISCOVERED**: 2025-11-23 - User clicks logout, appears to log out briefly, then page refreshes showing user still logged in
**ROOT CAUSE**: Logout endpoint missing `.RequireAntiforgeryToken()` after comprehensive CSRF protection rollout

### 🛑 ROOT CAUSE ANALYSIS:

During comprehensive CSRF protection implementation (November 2025), ~38 endpoints were updated to use `.RequireAntiforgeryToken()`. The logout endpoint was missed because:

1. **Architecture Confusion**: Frontend had 3 different authentication patterns active simultaneously:
   - **Pattern A**: TanStack Query mutations (`/features/auth/api/mutations.ts`) - Used for login/register forms
   - **Pattern B**: React Context + authService (`/contexts/AuthContext.tsx` + `/services/authService.ts`) - Used for logout
   - **Pattern C**: Alternative hooks (`/lib/api/hooks/useAuth.ts`) - Mostly dead code with duplicate implementations

2. **CSRF Fix Applied to Wrong File**: Initial fix updated `/features/auth/api/mutations.ts` useLogout() mutation, but actual logout flow used Pattern B (authService.ts)

3. **Duplicate Implementations Caused Wasted Effort**: Fixed BOTH wrong file (mutations.ts) AND correct file (authService.ts), only worked because we accidentally fixed both

### 🔥 CRITICAL ARCHITECTURE PROBLEM:

**Multiple auth patterns = systematic bugs during infrastructure changes**

Found during post-mortem analysis:
- Login used Pattern A (TanStack Query mutations)
- Logout used Pattern B (AuthContext + authService)
- 6 duplicate auth hooks in Pattern C (never used, just sitting there)
- CSRF fix required updating 2 different files to work
- No single source of truth for authentication operations

### ✅ SOLUTION - STANDARD AUTHENTICATION PATTERN:

**DECISION**: Migrated to single authentication pattern after comprehensive research
**PATTERN**: TanStack Query Mutations + Zustand Store
**RESEARCH**: See `/docs/functional-areas/authentication/research/2025-11-23-authentication-pattern-research.md`
**GUIDE**: See `/docs/standards-processes/frontend/authentication-pattern-guide.md`

#### Backend Requirements for Authentication Endpoints:

```csharp
// ✅ CORRECT: Logout endpoint with CSRF protection
app.MapPost("/api/auth/logout", async (
    SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
})
.RequireAuthorization()         // MUST be authenticated
.RequireAntiforgeryToken();     // MUST have CSRF token

// ✅ CORRECT: Login endpoint (no CSRF needed - public endpoint)
app.MapPost("/api/auth/login", async (
    LoginRequest request,
    SignInManager<ApplicationUser> signInManager) =>
{
    // Login logic...
    await signInManager.SignInAsync(user, isPersistent: false);
    return Results.Ok(new { user = userDto });
})
.AllowAnonymous(); // No CSRF for public endpoints

// ✅ CORRECT: Register endpoint (no CSRF needed - public endpoint)
app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager) =>
{
    // Registration logic...
    return Results.Ok(userDto);
})
.AllowAnonymous();

// ❌ WRONG: Logout without CSRF protection
app.MapPost("/api/auth/logout", async (signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
})
.RequireAuthorization(); // ← Missing .RequireAntiforgeryToken()
```

#### CSRF Protection Rules for Auth Endpoints:

| Endpoint | HTTP Method | Auth Required | CSRF Required | Reason |
|----------|-------------|---------------|---------------|--------|
| /api/auth/login | POST | No (public) | No | Creates session, no state change risk |
| /api/auth/register | POST | No (public) | No | Creates user, no state change risk |
| /api/auth/logout | POST | **YES** | **YES** | Destroys session = state change |
| /api/auth/csrf-token | GET | No | No | Provides token, no state change |
| /api/auth/user | GET | YES | No | Read-only operation |
| /api/auth/verify-email | POST | No | No | Public verification, uses secure token |
| /api/auth/forgot-password | POST | No | No | Public endpoint, sends email only |
| /api/auth/reset-password | POST | No | No | Uses secure token from email |

**CRITICAL RULE**: Any authenticated POST/PUT/DELETE endpoint MUST use `.RequireAntiforgeryToken()`

### 🧪 TESTING AUTHENTICATION ENDPOINTS:

```bash
# 1. Test logout with CSRF token (should succeed)
# First get CSRF token
curl -c cookies.txt -b cookies.txt http://localhost:5655/api/auth/csrf-token

# Then logout with token
curl -X POST http://localhost:5655/api/auth/logout \
  -H "X-XSRF-TOKEN: <token-from-cookie>" \
  -b cookies.txt

# Expected: 200 OK

# 2. Test logout without CSRF token (should fail)
curl -X POST http://localhost:5655/api/auth/logout \
  -b cookies.txt

# Expected: 400 Bad Request with "CSRF Validation Failed"

# 3. Verify user session cleared
curl http://localhost:5655/api/protected/welcome \
  -b cookies.txt

# Expected: 401 Unauthorized (session cleared)
```

### 📋 PREVENTION CHECKLIST:

**For Backend Developers implementing auth endpoints:**
- [ ] **Login/Register** = `.AllowAnonymous()` (no CSRF)
- [ ] **Logout** = `.RequireAuthorization().RequireAntiforgeryToken()` (BOTH required)
- [ ] **Read operations** = `.RequireAuthorization()` only (GET methods)
- [ ] **State changes** = `.RequireAntiforgeryToken()` (POST/PUT/DELETE)
- [ ] **Test with and without CSRF token** to verify protection
- [ ] **Verify httpOnly cookie session** properly created/destroyed
- [ ] **Check CSRF token endpoint** is public and working

**For React Developers using auth operations:**
- [ ] **ONLY use mutations from** `/features/auth/api/mutations.ts`
- [ ] **Read auth state from** Zustand store (`useUser`, `useIsAuthenticated`)
- [ ] **NEVER create new auth patterns** - use existing mutations
- [ ] **CSRF tokens handled automatically** by axios interceptor
- [ ] **Check authentication pattern guide** before implementing auth features

### 💥 CONSEQUENCES OF MULTIPLE AUTH PATTERNS:

**What Happened**:
1. CSRF protection rolled out to ~38 endpoints (November 2025)
2. Logout endpoint missed because using different auth pattern
3. Bug found: logout appears to work but user still logged in
4. Investigation found 3 different authentication patterns in use
5. CSRF fix required updating 2 different files to work
6. Comprehensive migration required to fix properly

**Wasted Effort**:
- Updated wrong file (mutations.ts useLogout) - not used
- Updated correct file (authService.logout) - actually used
- Only worked because accidentally fixed both
- Should have been 1 file update, was 2 files + debugging time

**Solution**:
- Deleted obsolete patterns (AuthContext, authService, duplicate hooks)
- Migrated to single pattern (TanStack Query + Zustand)
- Created comprehensive developer guide
- Updated all components to use standard pattern

### 📁 FILES MODIFIED:

**Backend (CSRF Protection)**:
- `/apps/api/Features/Auth/Endpoints/AuthEndpoints.cs`:
  - Line ~45: Added `.RequireAntiforgeryToken()` to logout endpoint

**Frontend (Migration to Standard Pattern)**:
- **DELETED** (obsolete patterns):
  - `/apps/web/src/contexts/AuthContext.tsx` - React Context pattern
  - `/apps/web/src/services/authService.ts` - Direct fetch calls
  - `/apps/web/src/hooks/useAuth.ts` - Context wrapper
  - `/apps/web/src/examples/LoginFormExample.tsx` - Old example

- **UPDATED** (migrated to standard pattern):
  - `/apps/web/src/features/auth/api/mutations.ts`:
    - Lines 182-246: Complete useLogout() mutation with CSRF support
  - `/apps/web/src/components/layout/UtilityBar.tsx`:
    - Lines 4, 41, 47-49: Updated to use useLogout() mutation
  - `/apps/web/src/components/layout/Navigation.tsx`:
    - Lines 4, 22, 51-54: Updated to use useLogout() mutation
  - `/apps/web/src/main.tsx`:
    - Line 16: Removed AuthProvider import
    - Lines 75-80: Removed AuthProvider wrapper, added comments about new pattern
  - `/apps/web/src/lib/api/index.ts`:
    - Lines 6-8: Removed hooks/useAuth export, added comments
  - `/apps/web/src/test/integration/msw-verification.test.ts`:
    - Lines 2, 10-12, 22, 42, 47, 74: Updated to use api client directly

**Documentation**:
- **CREATED**:
  - `/docs/standards-processes/frontend/authentication-pattern-guide.md` - Comprehensive developer guide
  - `/docs/functional-areas/authentication/research/2025-11-23-authentication-pattern-research.md` - Research and recommendations

### 🔗 RELATED LESSONS:

- **CSRF Protection - Middleware Auto-Validation** (Part 1) - How CSRF validation works
- **CSRF Protection - Two-Cookie Pattern** (Part 1) - Token generation implementation
- **Authentication Pattern Guide** - `/docs/standards-processes/frontend/authentication-pattern-guide.md`
- **Architecture Confusion** - See react-developer-lessons-learned.md for frontend impact

### 🎯 KEY TAKEAWAYS:

1. **Authentication endpoints need CSRF protection** for state-changing operations
2. **Logout is a state change** (destroys session) = requires CSRF token
3. **Multiple auth patterns = systematic bugs** during infrastructure changes
4. **Always use standard pattern** documented in authentication-pattern-guide.md
5. **Test CSRF protection** with and without token to verify security
6. **Delete obsolete code** - don't leave multiple patterns around

**Tags**: #critical #authentication #csrf #antiforgery #logout #architecture #technical-debt #tanstack-query #zustand #httponly-cookies #security #owasp

---

## 🚨 CRITICAL: Vite HMR WebSocket Fails in Test Containers - Must Disable HMR (2025-11-29)

**Problem**: E2E tests in TEST containers had 381 WebSocket errors (`ws://localhost:5173`) causing 1,849 ERR_CONNECTION_REFUSED errors, while DEV containers (559 tests) had zero errors.

**Date Discovered**: November 29, 2025 during test container networking investigation
**Context**: Same tests, same code - only container configuration differed between dev and test environments

**Root Cause**:
- **DEV containers**: Playwright runs on HOST machine → browser's `localhost:5173` connects to port-forwarded web container
- **TEST containers**: Playwright runs INSIDE `test-runner` container → browser's `localhost:5173` = test-runner container itself (NOT web container)
- Vite HMR WebSocket configured to use `ws://localhost:5173` which is correct for host-based testing
- In test containers, web service is at `web:5173` (container DNS), not `localhost:5173`
- HMR WebSocket tries to connect to `localhost:5173` inside test-runner container → nothing listening → ERR_CONNECTION_REFUSED

**Why This is Critical**:
- 381 WebSocket connection failures flood browser console with errors
- 1,849 ERR_CONNECTION_REFUSED errors create noise masking real test failures
- HMR is completely unnecessary for E2E tests (no live development during test runs)
- Test reliability severely impacted by spurious network errors

**Error Manifestation**:
```bash
# TEST containers (BEFORE fix)
- 381 ws://localhost:5173 WebSocket errors (HMR trying to connect)
- 1,849 ERR_CONNECTION_REFUSED errors
- 717 401 Unauthorized errors vs ~200 in dev
- 305 tests passing (out of 559 total)

# DEV containers (working correctly)
- 0 WebSocket errors
- 0 ERR_CONNECTION_REFUSED errors
- ~200 401 Unauthorized errors (expected for unauthenticated tests)
- 559 tests passing
```

**Architecture Difference**:
```
DEV Environment:
  Host Machine → Playwright → Browser
  Browser localhost:5173 → Docker port forwarding → web:5173 container
  ✅ HMR WebSocket works: ws://localhost:5173 reaches web container

TEST Environment:
  test-runner container → Playwright → Browser (inside same container)
  Browser localhost:5173 → test-runner container (NOT web container!)
  ❌ HMR WebSocket fails: ws://localhost:5173 has nothing listening
  ✅ App works: http://web:5173 uses container DNS correctly
```

**Wrong Solution** (Fix HMR to use container DNS):
```yaml
# ❌ WRONG - Trying to make HMR work in test containers
environment:
  VITE_HMR_HOST: "web"  # Browser still uses localhost, not web
```
**Problem**: Browser executes JavaScript that tries `ws://localhost:5173`, not `ws://web:5173`

**Correct Solution** (Disable HMR entirely in test containers):

**File: `/home/chad/repos/witchcityrope/docker-compose.test.yml`**
```yaml
services:
  web:
    environment:
      # Disable HMR for test containers (browser runs inside test-runner, cannot reach web:5173)
      # HMR WebSocket tries ws://localhost:5173 which fails in containers
      VITE_HMR_ENABLED: "false"
```

**File: `/home/chad/repos/witchcityrope/apps/web/vite.config.ts`**
```typescript
export default defineConfig(({ mode }) => {
  return {
    server: {
      // HMR Configuration - disable in test containers, configure for dev containers
      hmr: process.env.VITE_HMR_ENABLED === 'false'
        ? false // Disable HMR entirely (for test containers)
        : process.env.DOCKER_ENV === 'true'
        ? {
            host: 'localhost',
            port: parseInt(process.env.VITE_PORT || '5173'),
            protocol: 'ws',
          }
        : true, // Use default HMR for local dev
    }
  }
})
```

**Prevention Checklist**:
1. **Test containers don't need HMR** - it's a development feature, not needed for E2E testing
2. **Browser context matters** - `localhost` in browser means different things in different container contexts
3. **Container DNS vs localhost** - Services use container names (`web`, `api`), browsers in containers use `localhost`
4. **Disable unnecessary features** in test environments to reduce noise and improve reliability
5. **Environment-specific configuration** - Use environment variables to control behavior (`VITE_HMR_ENABLED`)
6. **Verify test vs dev parity** - If dev works but test fails with same code, investigate container networking

**Verification**:
```bash
# After fix, test containers should have:
# - 0 WebSocket errors (HMR disabled)
# - 0 ERR_CONNECTION_REFUSED errors
# - Same test pass rate as dev containers
SKIP_CONFIRMATION=true bash .claude/skills/test-environment/execute.sh --mode e2e --filter "home-page"
```

**Related Issues**:
- **CORS configuration** already correct (`AllowAnyOrigin()` for development)
- **API connectivity** works fine (uses `http://api:8080` via container DNS)
- **Vite proxy** works correctly (`DOCKER_ENV=true` enables container-aware proxy)

**Impact**:
- ✅ **DEV containers**: Unaffected - no `VITE_HMR_ENABLED` set, HMR works as before
- ✅ **TEST containers**: HMR disabled - eliminates 381 WebSocket errors and 1,849 connection errors
- ✅ **Production builds**: Unaffected - HMR only runs in dev mode

**Tags**: #critical #testing #docker #containers #vite #hmr #websocket #networking #e2e #playwright #test-containers

---

## 🚨 CRITICAL: EventAttendee Does NOT Have TicketPurchase Navigation Property (2025-11-30)

**Problem**: Compilation errors when trying to access `EventAttendee.TicketPurchase` - this navigation property does not exist on EventAttendee entity.

**Date Discovered**: November 30, 2025 during CheckInService compilation
**Error**: `error CS1061: 'EventAttendee' does not contain a definition for 'TicketPurchase'`

**Root Cause**:
- **EventAttendee** entity does NOT have a `TicketPurchase` navigation property
- **EventAttendance** entity DOES have a `TicketPurchase` navigation property
- Code was incorrectly trying to access ticket purchase information through EventAttendee
- Relationship is: EventAttendance → TicketPurchase (NOT EventAttendee → TicketPurchase)

**Entity Structures**:
```csharp
// ❌ EventAttendee - NO TicketPurchase property
public class EventAttendee
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string RegistrationStatus { get; set; }
    // ... NO TicketPurchase navigation property!
}

// ✅ EventAttendance - HAS TicketPurchase property
public class EventAttendance
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public AttendanceType AttendanceType { get; set; }
    public Guid? TicketPurchaseId { get; set; }
    public TicketPurchase? TicketPurchase { get; set; }  // ✅ Navigation property
}
```

**Wrong Implementation**:
```csharp
// ❌ WRONG - EventAttendee doesn't have TicketPurchase property
var eligibleUserIds = _context.EventAttendances
    .Where(ea => ea.EventId == eventId &&
                ea.Status == AttendanceStatus.Active &&
                (ea.AttendanceType == AttendanceType.RSVP ||
                 ea.TicketPurchase == null ||
                 ea.TicketPurchase.TicketType.SessionId == sessionId))
    .Select(ea => ea.UserId)
    .Distinct();  // ← Missing .ToListAsync(), causes deferred execution issues
```

**Correct Implementation**:
```csharp
// ✅ CORRECT - Use EventAttendance with proper includes and async execution
var eligibleUserIds = await _context.EventAttendances
    .Include(ea => ea.TicketPurchase)
        .ThenInclude(tp => tp != null ? tp.TicketType : null)
    .Where(ea => ea.EventId == eventId &&
                ea.Status == AttendanceStatus.Active &&
                (ea.AttendanceType == AttendanceType.RSVP ||
                 ea.TicketPurchase == null ||
                 ea.TicketPurchase.TicketType!.SessionId == sessionId ||
                 ea.TicketPurchase.TicketType!.SessionId == null))
    .Select(ea => ea.UserId)
    .Distinct()
    .ToListAsync(cancellationToken);  // ← Execute query immediately
```

**Key Points**:
1. **EventAttendance** is the entity that links to TicketPurchase (not EventAttendee)
2. **Include** both TicketPurchase and TicketType navigation properties
3. **Execute immediately** with `.ToListAsync()` to avoid deferred execution issues
4. **Use null-forgiving operator** (`!`) when accessing nested properties after includes

**Business Logic**:
- **EventAttendance** tracks WHO is attending WHAT event (via RSVP or Ticket)
- **EventAttendee** tracks registration details for check-in kiosk
- **TicketPurchase** tracks financial transaction for refunds
- Check-in eligibility is determined by EventAttendance + TicketPurchase relationship

**File Locations**:
- EventAttendee: `/home/chad/repos/witchcityrope/apps/api/Features/CheckIn/Entities/EventAttendee.cs`
- EventAttendance: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Entities/EventAttendance.cs`
- TicketPurchase: `/home/chad/repos/witchcityrope/apps/api/Models/TicketPurchase.cs`
- CheckInService: `/home/chad/repos/witchcityrope/apps/api/Features/CheckIn/Services/CheckInService.cs`

**Tags**: #critical #entity-framework #navigation-properties #compilation-error #check-in #attendance #ticket-purchase #relationship-mapping

---

## 🚨 CRITICAL: ID Initializer Issue Found in Multiple Entities - Must Fix Before New Feature Work (2025-12-01)

**Problem**: Event, Session, and TicketPurchase entities have problematic ID initializers that will cause INSERT failures for new trigger feature work.

**Location**:
- `/home/chad/repos/witchcityrope/apps/api/Models/Event.cs`, line 16: `public Guid Id { get; set; } = Guid.NewGuid();`
- `/home/chad/repos/witchcityrope/apps/api/Models/Session.cs`, line 16: `public Guid Id { get; set; }` (same pattern)
- `/home/chad/repos/witchcityrope/apps/api/Models/TicketPurchase.cs`, line 15: `public Guid Id { get; set; } = Guid.NewGuid();`

**Critical Issue**: TicketPurchase ID initializer violates EF Core best practices (documented in backend-developer-lessons-learned.md Part 1, lines 134-158). This initializer causes EF to think new entities are existing ones, leading to UPDATE attempts instead of INSERTs.

**How This Will Affect New Feature Work**:
When implementing email trigger feature, if new Event/Session/TicketPurchase records are created for testing:
1. Entity is instantiated with `Id` property populated by `Guid.NewGuid()`
2. EF Core sees `Id` is not default (Guid.Empty) and thinks entity is existing
3. Attempt to INSERT fails → UPDATE attempt fails (no existing record) → `DbUpdateConcurrencyException`
4. Error message: "Database operation expected to affect 1 row(s) but actually affected 0 row(s)"

**Fix Applied**: NONE YET - This requires migration planning

**Recommended Action**:
1. Create migration to remove these initializers from DB configuration
2. Update entity models to remove inline initializers:
```csharp
// ❌ WRONG - Remove this
public Guid Id { get; set; } = Guid.NewGuid();

// ✅ CORRECT - Simple property only
public Guid Id { get; set; }
```
3. Ensure EF Core DbContext configuration generates IDs (default behavior)
4. This fix should be completed BEFORE implementing email trigger feature

**Prevention**: When creating new entities, NEVER add ID initializers. Let EF Core handle ID generation. Always apply: `public Guid Id { get; set; }` (no initializer).

**Tags**: #critical #entity-framework #id-generation #email-triggers #technical-debt

---

## 🚨 CRITICAL: Null Check Required for List Properties Before .Any() - NullReferenceException (2025-12-07)

**Problem**: Adding ticket types in admin event details area failed with "failed to save" error. Ticket type briefly appeared (optimistic update) then disappeared (rollback).

**Date Discovered**: December 7, 2025 during ticket type addition testing
**Context**: Admin clicks "Add Ticket Type", fills form, clicks Save → 500 error

**Root Cause**:
- `UpdateEventTicketTypesAsync` called `.Any()` on `ticketTypeDto.SessionIdentifiers` without null check (line 841, 889)
- Frontend sends partial update with only required fields: `{ id, ticketTypes: [...] }`
- `SessionIdentifiers` property is nullable and was `null` in the request
- `.Any()` throws `NullReferenceException` when called on `null` collection

**Error Manifestation**:
```
User Action: Click "Add Ticket Type" → Fill form → Click Save
Frontend: Optimistic update shows new ticket type
Backend: NullReferenceException at line 841 (existing) or 889 (new)
Response: 500 Internal Server Error
Frontend: Error notification "failed to save", rollback removes ticket type
```

**Investigation Process**:
1. ✅ Endpoint calling service method correctly (PUT /api/events/{id})
2. ✅ Service executing `UpdateEventAsync` with request
3. ✅ Request contained `TicketTypes` array with new ticket type
4. ❌ `SessionIdentifiers` property was `null` (not required for ticket type save)
5. ❌ Code called `.Any()` without checking for null first

**Wrong Implementation**:
```csharp
// ❌ WRONG - NullReferenceException when SessionIdentifiers is null
if (ticketTypeDto.SessionIdentifiers.Any())
{
    var linkedSessions = eventEntity.Sessions
        .Where(s => ticketTypeDto.SessionIdentifiers.Contains(s.SessionCode))
        .ToList();
}
```

**Correct Implementation**:
```csharp
// ✅ CORRECT - Null check before .Any()
if (ticketTypeDto.SessionIdentifiers != null && ticketTypeDto.SessionIdentifiers.Any())
{
    var linkedSessions = eventEntity.Sessions
        .Where(s => ticketTypeDto.SessionIdentifiers.Contains(s.SessionCode))
        .ToList();
}
```

**Locations Fixed**:
- Line 841: Update existing ticket type - session linkage
- Line 889: Add new ticket type - session linkage

**Why This Matters**:
- Partial updates often send `null` for optional fields
- List/collection properties can be `null` even if they're not nullable types in C#
- `.Any()`, `.Contains()`, `.Count()` all throw NullReferenceException on `null` collections
- Frontend sends minimal data for performance - backend must handle null values

**Prevention Checklist**:
1. ✅ **ALWAYS check for null** before calling LINQ methods on collections
2. ✅ **Use pattern**: `collection != null && collection.Any()` NOT just `collection.Any()`
3. ✅ **Test with partial updates** - verify null handling for optional fields
4. ✅ **Apply to all LINQ operations**: `.Any()`, `.Contains()`, `.Where()`, `.Select()`, `.Count()`
5. ✅ **Check both update and create paths** - null handling needed in both

**Common LINQ Operations That Require Null Checks**:
```csharp
// ❌ WRONG - All throw NullReferenceException on null
collection.Any()
collection.Contains(value)
collection.Where(predicate)
collection.Select(selector)
collection.Count()
collection.First()
collection.FirstOrDefault()

// ✅ CORRECT - Safe null checks
collection != null && collection.Any()
collection != null && collection.Contains(value)
collection?.Where(predicate) ?? Enumerable.Empty<T>()
collection?.Select(selector) ?? Enumerable.Empty<T>()
collection?.Count() ?? 0
collection?.FirstOrDefault()
```

**Files Modified**:
- `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/EventService.cs`
  - Line 841: Added null check for existing ticket type session linkage
  - Line 889: Added null check for new ticket type session linkage

**Build Verification**:
```bash
cd /home/chad/repos/witchcityrope/apps/api
dotnet build --no-restore
# Result: Build succeeded. 0 Error(s), 110 Warning(s)
```

**Success Criteria**:
- ✅ API builds with 0 errors
- ✅ Adding ticket types succeeds without error
- ✅ Ticket types persist to database
- ✅ No NullReferenceException when SessionIdentifiers is null
- ✅ Session linking works when SessionIdentifiers provided

**Related Patterns**:
- **Defensive Programming** (Part 2, lines 115-265): Verify data and handle edge cases
- **Nullable Field Null Value Persistence** (Part 1, lines 591-716): Handling null values in partial updates
- Similar to null checks required throughout codebase for navigation properties

**Pattern**: When working with collection properties in partial updates, ALWAYS check for null before calling LINQ methods. Partial updates send only changed fields, leaving optional fields null.

**Tags**: #critical #null-reference #linq #any-method #partial-updates #ticket-types #defensive-programming #collection-null-check

---

## 🚨 CRITICAL: EF Core Many-to-Many Relationship Updates - Use Differential Updates, NOT Clear() (2025-12-07)

**Problem**: Using `collection.Clear()` followed by `collection.Add()` in many-to-many relationships causes "duplicate key value violates unique constraint" errors in the join table.

**Date Discovered**: December 7, 2025 during ticket type session linkage updates
**Error**: `Npgsql.PostgresException (0x80004005): 23505: duplicate key value violates unique constraint "PK_TicketTypeSessions"`
**Location**: `/apps/api/Features/Events/Services/EventService.cs` - `UpdateEventTicketTypesAsync` method

**Root Cause**:
- EF Core batches operations and doesn't immediately execute `Clear()` on collections
- When you call `Add()` after `Clear()`, EF tries to insert new records BEFORE deleting old ones
- If any sessions are the same (re-adding existing links), it attempts to insert duplicates into the join table
- PostgreSQL rejects the duplicate key violation in `TicketTypeSessions` (or any join table)

**Wrong Implementation** (Causes Duplicate Key Errors):
```csharp
// ❌ WRONG - Clear() + Add() pattern causes duplicate key violations
existingTicketType.Sessions.Clear();  // Batched for later
if (ticketTypeDto.SessionIdentifiers != null && ticketTypeDto.SessionIdentifiers.Any())
{
    var linkedSessions = eventEntity.Sessions
        .Where(s => ticketTypeDto.SessionIdentifiers.Contains(s.SessionCode))
        .ToList();
    foreach (var session in linkedSessions)
    {
        existingTicketType.Sessions.Add(session);  // Tries to insert before Clear() executes
    }
}
// Result: 23505 duplicate key constraint violation in TicketTypeSessions
```

**Correct Implementation** (Differential Update):
```csharp
// ✅ CORRECT - Only remove/add what actually changed
var currentSessionCodes = existingTicketType.Sessions.Select(s => s.SessionCode).ToHashSet();
var newSessionCodes = (ticketTypeDto.SessionIdentifiers ?? Enumerable.Empty<string>()).ToHashSet();

// Remove sessions that are no longer linked
var sessionsToRemove = existingTicketType.Sessions
    .Where(s => !newSessionCodes.Contains(s.SessionCode))
    .ToList();
foreach (var session in sessionsToRemove)
{
    existingTicketType.Sessions.Remove(session);
}

// Add new sessions that aren't already linked
var sessionsToAdd = eventEntity.Sessions
    .Where(s => newSessionCodes.Contains(s.SessionCode) && !currentSessionCodes.Contains(s.SessionCode))
    .ToList();
foreach (var session in sessionsToAdd)
{
    existingTicketType.Sessions.Add(session);
}
```

**Why This Matters**:
- **Clear() is NOT immediate**: EF Core queues operations for batch execution
- **Order matters**: Insert attempts happen before delete execution in some cases
- **Differential updates are safer**: Only change what needs changing
- **Performance benefit**: Fewer database operations (don't delete/re-insert unchanged records)

**Pattern**: For ALL many-to-many relationship updates in EF Core:
1. **Build HashSets** of current and new identifiers for O(1) lookups
2. **Calculate removals**: Items in current but not in new
3. **Calculate additions**: Items in new but not in current
4. **Execute individually**: Remove items first, then add items
5. **Never use Clear()**: Unless you genuinely want to remove ALL items (rare)

**Applies To**:
- TicketType.Sessions (TicketTypeSessions join table)
- Event.Organizers (EventOrganizers join table)
- Any other many-to-many relationships in the codebase

**Related Patterns**:
- See `UpdateEventOrganizersAsync` method in same file for identical pattern
- Same approach used successfully for organizer updates

**Tags**: #critical #entity-framework #many-to-many #join-table #duplicate-key #postgresql #batch-operations #differential-update #collections

---

## 🚨 CRITICAL: Event Filtering Must Use Session Times, Not Event.StartDate (2025-12-07)

**Problem**: Event filtering was using `Event.StartDate` field to determine if events are "past" or "future", causing events with upcoming sessions to be hidden when `Event.StartDate` becomes stale.

**Date Discovered**: December 7, 2025 during event timing investigation
**Context**: Events named "Timing Test - 300hr Close", "Timing Test - 6hr Close", "Timing Test - 48hr Close" had:
- Event.StartDate = 2025-12-07 18:00:00 UTC (IN THE PAST)
- Session.StartTime = 2025-12-08 23:30:00 UTC (IN THE FUTURE)
- Result: Events were hidden from public view despite having upcoming sessions

**Root Cause**:
- Event.StartDate is a denormalized/cached field that can become stale
- Events are multi-session entities - sessions are the source of truth for timing
- Filtering logic was checking `e.StartDate > DateTime.UtcNow` instead of checking session times
- An event should be "upcoming" if it has ANY session with `StartTime > DateTime.UtcNow`
- An event should be "past" only if ALL sessions have `EndTime < DateTime.UtcNow`

**Why This is Critical**:
- Events with future sessions disappear from public view prematurely
- Users can't register for events that are still open
- Breaks user expectations ("Why is this event hidden when registration is still open?")
- StartDate is unreliable as a filter criterion for multi-session events
- Sessions are the single source of truth for event timing

**Wrong Implementation** (StartDate-based filtering):
```csharp
// ❌ WRONG - Uses stale Event.StartDate field
// Default: Only published future events
query = query.Where(e => e.IsPublished && e.StartDate > DateTime.UtcNow);

// Result: Events with past StartDate but future sessions are hidden
// Example: StartDate = Dec 7 (past), Session.StartTime = Dec 8 (future) → HIDDEN
```

**Correct Implementation** (Session-based filtering):
```csharp
// ✅ CORRECT - Uses actual session times as source of truth
var now = DateTime.UtcNow;

// Default: Only published future events
// An event is "future" if it has ANY session with StartTime > now
// An event is "past" only if ALL sessions have ended (EndTime < now)
query = query.Where(e => e.IsPublished && (
    e.Sessions.Any(s => s.StartTime > now) || // Has at least one upcoming session
    (!e.Sessions.Any() && e.StartDate > now))); // Fallback: no sessions but StartDate is future
```

**Complete Fix Pattern** (All three filter modes):
```csharp
var now = DateTime.UtcNow;

if (includeUnpublished)
{
    // Admin access: Show all events (both published and draft), including future and past
    // Filter based on sessions: show events with sessions in last 30 days OR with future sessions
    query = query.Where(e =>
        e.Sessions.Any(s => s.EndTime > now.AddDays(-30)) || // Has session that ended within last 30 days
        e.Sessions.Any(s => s.StartTime > now) || // Has upcoming session
        e.StartDate > now.AddDays(-30)); // Fallback to StartDate for events without sessions
}
else
{
    // Public access: Only published events
    if (includePastEvents)
    {
        // Show published events including past ones (last 90 days)
        // Filter based on sessions: show events with sessions in last 90 days OR with future sessions
        query = query.Where(e => e.IsPublished && (
            e.Sessions.Any(s => s.EndTime > now.AddDays(-90)) || // Has session that ended within last 90 days
            e.Sessions.Any(s => s.StartTime > now) || // Has upcoming session
            e.StartDate > now.AddDays(-90))); // Fallback to StartDate for events without sessions
    }
    else
    {
        // Default: Only published future events
        // An event is "future" if it has ANY session with StartTime > now
        // An event is "past" only if ALL sessions have ended (EndTime < now)
        query = query.Where(e => e.IsPublished && (
            e.Sessions.Any(s => s.StartTime > now) || // Has at least one upcoming session
            (!e.Sessions.Any() && e.StartDate > now))); // Fallback: no sessions but StartDate is future
    }
}
```

**Why Sessions Are Source of Truth**:
1. **Session.StartTime/EndTime are set by users** during event creation/editing
2. **Event.StartDate is calculated/cached** and can become stale if sessions change
3. **Multi-session events** have multiple start/end times - which StartDate to use?
4. **Business logic uses session times** for registration windows, capacity, etc.
5. **Timing windows are session-relative** (e.g., "24 hours before session start")

**Fallback Logic**:
- For events WITHOUT sessions: Use `Event.StartDate` as fallback
- This handles edge cases where events are being created/edited
- Most production events will have sessions, making this rare

**Location of Fix**:
- File: `/home/chad/repos/witchcityrope/apps/api/Features/Events/Services/EventService.cs`
- Method: `GetEventsAsync`
- Lines: ~72-105 (filter application logic)

**Testing Verification**:
After fix, events with:
- Past Event.StartDate (Dec 7 18:00 UTC)
- Future Session.StartTime (Dec 8 23:30 UTC)

Should appear on `/events` page WITHOUT requiring "Show Past Events" toggle.

**Performance Note**:
- Session filtering requires loading Sessions navigation property (already done via `.Include(e => e.Sessions)` at line 58)
- No additional database queries needed
- Filtering happens in-database via EF Core query translation

**Pattern**: For ALL event time-based queries:
1. **PRIMARY**: Check session times (`Sessions.Any(s => s.StartTime > now)`)
2. **FALLBACK**: Check Event.StartDate only for events without sessions
3. **NEVER**: Filter solely on Event.StartDate for multi-session events
4. **REMEMBER**: Sessions are the single source of truth for event timing

**Related Code**:
- RecalculateEventStartDate/EndDate methods (lines 654-716) update Event.StartDate based on sessions
- These should be called after session changes to keep StartDate in sync
- But filtering should still use sessions as primary criterion

**Tags**: #critical #event-filtering #session-timing #startdate #multi-session-events #source-of-truth #linq #entity-framework #business-logic

---

## 🚨 CRITICAL: TicketPurchaseId Missing from EventAttendance Records - Selective Cancellation Broken (2025-12-09)

**Problem**: Frontend selective ticket cancellation feature broken - `ticketPurchaseSessionMap` empty despite users having tickets. Admin user shows `hasTicket: true` but cannot cancel specific sessions because the map linking purchases to sessions is empty.

**Date Discovered**: December 9, 2025 during ticket purchase flow investigation
**Context**: `GetParticipationStatusAsync` builds `ticketPurchaseSessionMap` by grouping EventAttendance records by `TicketPurchaseId`, but all TicketPurchaseId fields were `null`

**Root Cause**:
- `CreateTicketPurchaseAsync` in `AttendanceService.cs` created EventAttendance records (lines 736-754) without creating a TicketPurchase record
- EventAttendance records had `TicketPurchaseId = null` because no TicketPurchase existed to link to
- The method only created attendance tracking records, not the financial transaction record
- This broke the selective ticket cancellation feature which relies on the TicketPurchase → EventAttendance linkage

**Evidence**:
```
Admin User Data:
- hasTicket: true
- ticket.id exists
- BUT ticketPurchaseSessionMap: {} (empty!)

Teacher User Data (from seed data):
- ticketPurchaseSessionMap: {"6ce042b5...":["019b01c5..."]} (correct!)

Difference: Seed data correctly creates TicketPurchase + links EventAttendance
UI flow: Only created EventAttendance without TicketPurchase link
```

**Database Schema**:
```csharp
// EventAttendance entity
public class EventAttendance
{
    public Guid? TicketPurchaseId { get; set; }  // Link to financial transaction
    public TicketPurchase? TicketPurchase { get; set; }  // Navigation property
    // ... other fields
}
```

**Why This Pattern Failed**:
1. EventAttendance was created for attendance tracking (who's attending which sessions)
2. TicketPurchase was never created for financial tracking (who paid for what ticket type)
3. Without TicketPurchase, there was no record to link EventAttendance records to
4. `GetParticipationStatusAsync` grouped by `TicketPurchaseId` → all null → empty map
5. Frontend couldn't build UI for selective cancellation (didn't know which sessions belonged to which purchase)

**Correct Pattern** (from TestHelperService.cs lines 207-247 and TicketPurchaseSeeder.cs):
```csharp
// STEP 1: Create TicketPurchase record FIRST
var ticketPurchase = new TicketPurchase
{
    Id = Guid.NewGuid(),
    TicketTypeId = request.TicketTypeId,
    UserId = userId,
    Quantity = 1,
    TotalPrice = ticketType.Price,
    PaymentStatus = "Pending",
    PaymentMethod = request.PaymentMethodId ?? "Unknown",
    PaymentReference = $"WCR-{Guid.NewGuid().ToString()[..8].ToUpper()}",
    Notes = request.Notes ?? $"Ticket purchase - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
    EventWaiverAccepted = request.EventWaiverAccepted,
    EventWaiverAcceptedAt = DateTime.UtcNow,
    PurchaseDate = DateTime.UtcNow,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

_context.TicketPurchases.Add(ticketPurchase);
await _context.SaveChangesAsync(cancellationToken);

// STEP 2: Create EventAttendance records linked to TicketPurchase
foreach (var session in ticketTypeWithSessions.Sessions)
{
    var attendance = new EventAttendance(request.EventId, userId, AttendanceType.Ticket)
    {
        SessionId = session.Id,
        TicketPurchaseId = ticketPurchase.Id,  // CRITICAL: Link to TicketPurchase
        Notes = request.Notes,
        EventWaiverAccepted = true,
        EventWaiverAcceptedAt = DateTime.UtcNow,
        CreatedBy = userId
    };

    _context.EventAttendances.Add(attendance);
}

await _context.SaveChangesAsync(cancellationToken);
```

**Why Two Separate Records**:
- **TicketPurchase**: Financial transaction record (payment tracking, refunds, purchase history)
- **EventAttendance**: Attendance tracking record (capacity, check-in, roster)
- **Link**: `EventAttendance.TicketPurchaseId` → `TicketPurchase.Id`
- **Purpose**: Enables selective cancellation (cancel specific sessions within a multi-session ticket)

**Multi-Session Ticket Support**:
```
One TicketPurchase → Multiple EventAttendance records
Example: Weekend Pass ticket
- 1 TicketPurchase (financial record)
- 3 EventAttendance records (one per session: Fri, Sat, Sun)
- All 3 attendance records link to same TicketPurchaseId
- Frontend can show: "Weekend Pass - 3 sessions" with individual cancel buttons
```

**Payment Status Field**:
- Initially "Pending" when EventAttendance created
- Updated to "Completed" after PayPal payment processes
- Separate payment processing step updates TicketPurchase record
- EventAttendance records remain linked regardless of payment status changes

**Prevention Rules**:
1. ✅ **ALWAYS create TicketPurchase BEFORE EventAttendance** when user purchases a ticket
2. ✅ **ALWAYS set EventAttendance.TicketPurchaseId** to link to the financial record
3. ✅ **Use TicketPurchaseId for grouping** in APIs that need to identify which sessions belong to same purchase
4. ✅ **Test with multi-session tickets** to verify cancellation works for individual sessions
5. ✅ **Compare with seed data patterns** - TestHelperService and seeders show correct implementation

**Testing Verification**:
```bash
# 1. User purchases ticket through UI
curl -X POST 'http://localhost:5173/api/events/{eventId}/tickets' \
  -H 'Content-Type: application/json' \
  -b cookies.txt \
  -d '{"ticketTypeId":"...","notes":"Test purchase","eventWaiverAccepted":true}'

# 2. Query database - verify TicketPurchase created
docker exec witchcity-postgres psql -U postgres -d witchcitydb \
  -c "SELECT \"Id\", \"TicketTypeId\", \"PaymentStatus\" FROM \"TicketPurchases\" WHERE \"UserId\" = '{userId}' ORDER BY \"CreatedAt\" DESC LIMIT 1;"

# 3. Query database - verify EventAttendance links to TicketPurchase
docker exec witchcity-postgres psql -U postgres -d witchcitydb \
  -c "SELECT \"Id\", \"TicketPurchaseId\", \"SessionId\", \"Status\" FROM \"EventAttendances\" WHERE \"UserId\" = '{userId}' AND \"AttendanceType\" = 2 ORDER BY \"CreatedAt\" DESC LIMIT 3;"

# 4. Get participation status - verify ticketPurchaseSessionMap populated
curl 'http://localhost:5173/api/events/{eventId}/participation' -b cookies.txt | jq '.data.ticketPurchaseSessionMap'
# Expected: {"<ticketPurchaseId>":["<sessionId1>","<sessionId2>",...]}
```

**File Modified**: `/home/chad/repos/witchcityrope/apps/api/Features/Participation/Services/AttendanceService.cs`
- Lines 736-790: Added TicketPurchase creation before EventAttendance records
- Added extensive comments explaining the two-step process
- Pattern matches TestHelperService.cs (lines 208-247)

**Related Patterns**:
- **Event Participation Architecture** (AttendanceService.cs lines 18-51): Documents ticket vs RSVP separation
- **TestHelperService Pattern** (lines 207-247): Shows correct TicketPurchase → EventAttendance linking
- **TicketPurchaseSeeder Pattern** (lines 653-794): Production seed data creates both records correctly

**Success Criteria**:
- ✅ API builds with 0 errors (warnings only)
- ✅ Every ticket purchase creates TicketPurchase record first
- ✅ Every EventAttendance for tickets has TicketPurchaseId set
- ✅ `ticketPurchaseSessionMap` populated for all users with tickets
- ✅ Frontend selective cancellation UI works for multi-session tickets
- ✅ Payment processing can update TicketPurchase.PaymentStatus independently

**Pattern**: Ticket purchases require TWO records: TicketPurchase (financial) + EventAttendance (attendance). Create TicketPurchase FIRST, then create EventAttendance with TicketPurchaseId link. Never create EventAttendance alone for ticket purchases.

**Tags**: #critical #ticketpurchase #eventattendance #foreign-key #selective-cancellation #multi-session-tickets #payment-tracking #financial-records #attendance-tracking #database-relationships

---

## Serilog PostgreSQL Sink: 3 Staging Deployment Issues (2026-03-02)

### PgBouncer Transaction Pooling Incompatibility with COPY Protocol
**Problem**: Setting `useCopy: true` on the Serilog PostgreSQL sink causes silent failures in staging/production because PgBouncer transaction-mode pooling does not support the streaming COPY protocol.
**Solution**: Always use `useCopy: false`; the sink uses batch INSERTs instead which are PgBouncer-compatible.

### UUID Column Type Mismatch for LogContext Properties
**Problem**: Pushing a string value via `LogContext.PushProperty("UserId", userId.ToString())` causes `InvalidCastException` at the sink because the `user_id` column is PostgreSQL UUID and the sink uses `NpgsqlDbType.Uuid`.
**Solution**: Always call `Guid.TryParse()` before pushing UUID-typed properties, then push the `Guid` value, not the string. See `CorrelationIdMiddleware.cs` and `UserContextMiddleware.cs` for the correct pattern.

### Sink Fails Silently if Table Does Not Pre-Exist
**Problem**: Setting `needAutoCreateTable: true` causes the sink to attempt table creation at startup, which either fails (wrong permissions) or creates a table with a different schema than the EF Core migration expects.
**Solution**: Always use `needAutoCreateTable: false` and ensure the `logging.application_logs` table is created by EF Core migration before deploying.

**Reference**: `/docs/standards-processes/backend/serilog-logging-guide.md`

**Tags**: #serilog #postgresql #pgbouncer #logging #uuid #deployment #staging

---

## Backward-Compatible DTO Extension Pattern for Checkout Flow Modifications (2026-03-18)

### Problem
Modifying the atomic checkout flow (validate -> create pending -> charge -> finalize) is high-risk. Adding multi-ticket support with assignees must not break existing single-ticket purchases.

### Solution: Optional Field + Normalization
1. Add optional `TicketSelections` field alongside existing `TicketTypeIds`
2. At the start of the service method, normalize both formats into a unified `List<TicketSelectionItem>`
3. All processing uses the normalized list -- no branching logic deeper in the method

```csharp
var selections = request.TicketSelections?.Any() == true
    ? request.TicketSelections
    : request.TicketTypeIds.Select(id => new TicketSelectionItem
    {
        TicketTypeId = id,
        Quantity = 1,
        Assignees = null
    }).ToList();
```

### Key Decisions
- **One TicketPurchase per individual ticket** (not per selection) -- enables independent refund/assignment
- **Assigned tickets transition: PendingPayment -> PendingAcceptance** (not Active) after payment
- **Purchaser's own tickets transition: PendingPayment -> Active** as before
- **GetReservedCountAsync must include PendingAcceptance** for capacity enforcement (BR-013)
- **Auto-RSVP only for purchaser's own tickets** -- assignee gets RSVP when they accept

### Mistakes to Avoid
- DO NOT forget to update `GetReservedCountAsync` to include PendingAcceptance -- causes overselling
- DO NOT create EventAttendee check-in records for PendingAcceptance tickets -- only for Active
- DO NOT accept waiver on behalf of assignees at checkout (BR-030, BR-033)
- DO NOT skip authorization check (BR-020) -- purchaser must be an authorized delegate for each assignee

**Tags**: #checkout #multi-ticket #backward-compatibility #assignment #capacity
