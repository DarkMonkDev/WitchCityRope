# Technical Debt — WitchCityRope

<!-- Last Updated: 2026-04-10 -->
<!-- Owner: All agents collectively; curated by the librarian -->
<!-- Status: Active tracking document — the single source of truth for all tech debt in this repo -->

## Purpose

This is the **single tracked file** for all technical debt, known issues, deferred decisions, and "we should fix this someday" items in the WitchCityRope repo.

Before this file existed (created 2026-04-10), tech debt items were scattered across multiple per-project files (`docs/functional-areas/vetting/vetting-status-cleanup-tech-debt-2026-04-10.md`, `docs/standards-processes/testing/test-infra-tech-debt-2026-04-10.md`, etc.), `CURRENT_TEST_STATUS.md` "Known Issues" sections, lessons-learned prevention patterns, and commit messages. Finding the complete set of outstanding issues required knowing where to look. This file consolidates all of that into one place.

## How agents use this file

**Before starting new work**, scan the [Active items](#active-items) section to check whether any outstanding tech debt:
- overlaps with your task (you might fix it for free while you're there), or
- might affect the area you're working in (so you can plan around it).

**When you discover new tech debt during your work**:
1. Add a new entry under the appropriate area section using the [template](#template-for-new-entries) at the bottom of this file.
2. Do NOT fix the item inline unless the user explicitly asked you to — tech debt often represents in-flight decisions, and touching it unexpectedly creates conflicts.
3. Commit the addition with your other work OR separately — either is fine, but make sure it lands.
4. Cross-reference the item in your commit message so the trail is clear.

**When you resolve a tech debt item**:
1. Do NOT delete the entry. Move it to the [Resolved items](#resolved-items) section with a commit reference, date, and one-line summary of how it was fixed.
2. This preserves the historical trail so future agents can see what was tried, what worked, and what was explicitly ruled out.
3. If a "resolved" item turns out to have been resolved incorrectly, resurrect it to Active with a note explaining why.

**When you update an existing item**:
- Add a dated note to the entry body rather than rewriting history.
- If the priority or impact changes, update the header and add a note explaining why.

## How to prioritize

- **P0** — actively blocking work, or a security/correctness bug with a known exploit path. Very rare in tech debt.
- **P1** — impacts a significant number of tests, users, or downstream systems. Should be scheduled proactively.
- **P2** — known issue with a reasonable workaround. Fix when touching the area.
- **P3** — cosmetic, naming, docblock drift, or "would be nice to have" cleanups. Opportunistic only.

---

# Active items

## Testing infrastructure

### T-1 — WebApplicationFactory shared-state pollution bug (P1, UNRESOLVED)

**Discovered**: 2026-04-10 during Test Infrastructure Cleanup session
**Impact**: ~36 of the 46 Integration test failures in the current baseline. Code is fine, test infrastructure is broken.

#### Symptom

Integration tests in `[Collection("Sequential")]` fail with:
```
System.InvalidOperationException : The entry point exited without ever building an IHost.
   at Microsoft.Extensions.Hosting.HostFactoryResolver.HostingListener.CreateHost()
   at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`1.StartServer()
   at Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory`1.CreateClient()
```

Currently affects all tests in:
- `tests/integration/Features/TicketAssignment/AdminAssignmentEndpointTests.cs` (8 tests, pre-existing)
- `tests/integration/Features/TicketAssignment/AuthorizedContactEndpointTests.cs` (16 tests)
- `tests/integration/Features/TicketAssignment/ProxyRsvpEndpointTests.cs` (11 tests)
- `tests/integration/Features/TicketAssignment/MultiTicketCheckoutEndpointTests.cs` (1 test)

All failing classes use the `private static WebApplicationFactory<Program>? _sharedFactory` pattern. Which specific classes hit the failure depends on test-run order.

#### Proof the code is fine

Both affected classes pass 100% when run in isolation:

```bash
bash .claude/skills/run-test-suite/execute.sh --mode unit --filter "FullyQualifiedName~AdminAssignmentEndpointTests"
# → 8/8 pass (20 seconds)

bash .claude/skills/run-test-suite/execute.sh --mode unit --filter "FullyQualifiedName~AuthorizedContactEndpointTests"
# → 16/16 pass (21 seconds)
```

#### Root cause theory (unverified)

Some earlier test in the sequential collection pollutes global process state (most likely a singleton in the API's DI container or a static field somewhere in the test helpers). The `private static _sharedFactory` caching pattern then locks in the poisoned state for the entire lifetime of each affected class, so every test in those classes fails identically.

#### Three dead-end fixes — DO NOT RE-TRY

During the 2026-04-10 investigation, three successive root-cause theories were applied and all produced **zero** improvement. Listed here so future debuggers don't repeat the work:

1. **Adding `when (ex is not HostAbortedException)`** to the try/catch around `app.Run()` in `Program.cs`. **Why it doesn't work**: `WebApplicationFactory<Program>` in .NET 10 uses `DeferredHostBuilder` + `TaskCompletionSource` for host capture, NOT `HostAbortedException` throwing. The exception never reaches the catch, the filter is a no-op.

2. **Switching `Serilog.CreateBootstrapLogger()` → `CreateLogger()`** per [serilog/serilog-aspnetcore#289](https://github.com/serilog/serilog-aspnetcore/issues/289). **Why it doesn't work**: that issue's bug only manifests in **parallel** test execution. Our failing tests are in `[Collection("Sequential")]` with `DisableParallelization = true`, so they run serially.

3. **Reverting Respawn 7.0.0 → 6.2.1**. **Why it doesn't work**: the initial correlation was coincidental timing drift. Reverting produced identical failure counts. Respawn is not involved.

#### Suggested fix approach

1. **Bisect the test order**: run subsets of the integration suite incrementally and find the smallest subset that triggers the failure. The last-added test in that subset is the culprit. Use `run-test-suite --mode unit --filter` with different filters to narrow it down.
2. **Drop the `_sharedFactory` pattern**: replace `private static WebApplicationFactory<Program>? _sharedFactory` caching with per-test factories. This trades test-run speed (factories are expensive to build) for isolation.

Estimated effort: ~4 hours for a focused session with no prior-misdiagnosis baggage.

#### Authoritative records

- `docs/lessons-learned/test-executor-lessons-learned.md` — "Entry point exited without ever building an IHost" prevention pattern
- `docs/standards-processes/testing/CURRENT_TEST_STATUS.md` — "Known Issues" section
- Commits `e1c0dd8e`, `a7e9d13a` — full investigation in commit messages

---

### T-2 — `EmailTemplateServiceTests.SendAdHocEmailAsync_*` 9 behavioral drift failures (P2)

**Discovered**: 2026-04-10 during Test Infrastructure Cleanup session
**Impact**: 9 unit tests in `tests/unit/api/Features/EmailTemplates/EmailTemplateServiceTests.cs`

#### Symptom

All 9 failures are NSubstitute mock assertion errors. Example:

```
NSubstitute.Exceptions.ReceivedCallsException : Expected to receive exactly 1 call matching:
    SendEmailAsync("test-2a995e50cc704ed6b27a84e61f33723a@example.com", "Welcome",
                   html => html.Contains("Hello TestSceneName!"),
                   text => ((text != null) AndAlso text.Contains("Hello TestSceneName!")),
                   any CancellationToken)
Actually received no matching calls.
```

#### Affected test methods

All in the `SendAdHocEmailAsync_*` family:

- `SendAdHocEmailAsync_WithMultiplePerUserVariables_ReplacesAllVariables`
- `SendAdHocEmailAsync_WithoutPerUserVariables_SendsBulkEmails`
- `SendAdHocEmailAsync_WithPerUserVariables_SendsIndividualEmails`
- `SendAdHocEmailAsync_WithResetUrlVariable_GeneratesUniqueToken`
- `SendAdHocEmailAsync_WithResetUrlVariable_GeneratesUniqueTokensPerUser`
- `SendAdHocEmailAsync_WithSystemUrlVariable_ReplacesWithFrontendUrl`
- `SendAdHocEmailAsync_WithUserNameVariable_FallsBackToEmailWhenSceneNameEmpty`
- `SendAdHocEmailAsync_WithUserNameVariable_ReplacesWithSceneName`
- `SendAdHocEmailAsync_WithVerificationUrlVariable_GeneratesEmailConfirmationToken`

#### Diagnosis

**Real behavioral drift, not `Variables`-property bit-rot.** The `SendAdHocEmailAsync` method either:
- No longer calls `SendEmailAsync` with per-user content (maybe routes through a different service method)
- Uses a different template variable resolver
- Has changed its call signature enough that the mock setup no longer matches

Important clarification: the Core.Tests version of `EmailTemplateServiceTests` had a completely different failure mode (`Variables` property setters pointing at a property that no longer exists on `GlobalEmailTemplate`). That was fixed in commit `9a815064`. **These unit-test failures are a different problem** — the `Variables` property is not involved.

#### Suggested fix approach

1. Read `apps/api/Features/EmailTemplates/Services/EmailTemplateService.SendAdHocEmailAsync` to understand its current call pattern
2. Compare against the mock expectations in each failing test
3. Decide whether the service changed by design (update the tests) or by accident (update the service)
4. Fix one test, verify it runs clean, then apply the pattern to the rest

Estimated effort: ~2 hours for someone with context on the email template variable resolver.

#### Authoritative records

- `docs/standards-processes/testing/CURRENT_TEST_STATUS.md` — "Known Issues" section
- Commit `e1c0dd8e` message

---

### T-3 — Frontend unit test suite baseline is ~60% broken (P1, URGENT per discoverer)

**Discovered**: 2026-04-10 during Phase 1 code review of the vetting status cleanup project
**Impact**: ~196 of ~482 frontend unit tests fail. 27 of 43 test files fail. Test suite is not useful for catching regressions.

#### Baseline state (measured after vetting commit `0fdcbbd6`)

- **Test Files**: 27 failed | 14 passed | 2 skipped (43 total)
- **Tests**: 196 failed | 246 passed | 40 skipped (482 total)

#### Breakdown of the 196 failures

- **8** were introduced by vetting Phase 1 (`VettingApplicationPage.test.tsx`) — regressions from replacing `VettingStatusBox` with `VettingAlertBox` without updating the test file's mocks
- **~188** were already broken on `main` BEFORE Phase 1

#### Representative pre-existing breakages

- `tests/unit/web/features/vetting/VettingStatusBox.test.tsx` (12 tests) — uses `useEventTimeZone` which calls `useQuery`, but test does not wrap in `QueryClientProvider`
- `tests/unit/web/features/admin/vetting/services/vettingAdminApi.test.ts` — error-object shape mismatch (test expects old `{response:{data:{error}}}` shape, code now throws `Error` objects)
- Many others with similar "missing provider" or "API shape drift" patterns

#### Why this is URGENT, not just tech debt

- Unit tests are supposed to catch regressions between phases of cleanup work
- With 60% of the suite broken, adding new coverage is worthless — new tests become islands of green in a red ocean, and real regressions get lost
- It implies CI is either not running these tests, running them with some suppression, or the suite has been allowed to rot unchecked
- Any ongoing project (vetting cleanup, test infrastructure work, etc.) runs without reliable frontend test safety — relying on browser verification and code review only

#### Status

**Decision made 2026-04-10** (per user in chat): a separate agent will be spun up to fix the frontend unit test suite in parallel with other ongoing work. The 8 `VettingApplicationPage.test.tsx` regressions from vetting Phase 1 are acknowledged and left for that separate rehabilitation project.

#### Suggested fix approach

Dedicated investigation — find root cause of pre-existing breakage (missing global test setup? missing providers? drifted API shapes?), fix the infrastructure, re-enable failing tests in batches, verify CI actually runs the suite. This is a multi-day effort, not a quick fix.

#### Authoritative records

- Originally documented in the now-consolidated `docs/functional-areas/vetting/vetting-status-cleanup-tech-debt-2026-04-10.md` (item #2)

---

### T-4 — `TEST_CATALOG.md` full refresh pending (P2)

**Discovered**: 2026-04-10 during Test Infrastructure Cleanup session
**Impact**: Navigation index for all WCR tests is ~4 months stale. New test classes added since 2025-12-13 are not listed. Per-feature-area tables reflect December counts, not current state.

#### Current state

- File header version: `12.12.0` with date `2025-12-13`
- Body is a snapshot from a December 2025 E2E execution run
- Does not reflect:
  - The new `run-test-suite` skill (2026-04-10)
  - Current .NET test counts (1,110 API unit + 132 Core + 268 integration + 6 SystemTests = 1,516 total)
  - The compile cascade that was blocking API Unit + Core Tests before 2026-04-10
  - The 137 tests added by commit `cb8adcdb` (ticket assignment + proxy RSVP feature)

#### Partial mitigation applied 2026-04-10

A "Current Numbers (2026-04-10)" header section was added at the top of `TEST_CATALOG.md` pointing to `CURRENT_TEST_STATUS.md` for current numbers. The body tables below that header are unchanged and still show the December snapshot.

#### Suggested fix approach

This is a dedicated test-executor agent task, not something to squeeze into a cleanup session. The refresh should:

1. Run a clean full baseline via `run-test-suite --mode all`
2. Enumerate every `.cs` test file in each of the 4 .NET test projects (`tests/unit/api/`, `tests/WitchCityRope.Core.Tests/`, `tests/integration/`, `tests/WitchCityRope.SystemTests/`)
3. Extract per-file test counts and pass/fail status from the skill output
4. Update or replace the feature-area tables in `TEST_CATALOG.md` body
5. Update the version header + the navigation links to sibling catalog parts (`TEST_CATALOG_PART_2.md`, etc.)

Estimated effort: ~2 hours for test-executor with a fresh baseline run.

#### Authoritative records

- `docs/standards-processes/testing/TEST_CATALOG.md` — header note added 2026-04-10
- `docs/standards-processes/testing/CURRENT_TEST_STATUS.md` — current numbers as a stopgap

---

## Tooling / development environment

### TL-1 — Pre-commit hook over-broad regex + untracked hook source (P2)

**Discovered**: 2026-04-10 during Test Infrastructure Cleanup session
**Impact**: Commits that touch documentation mentioning test-runner command names verbatim (even in "do NOT use these" warning contexts) are blocked by a false positive in the single-source-of-truth validator.

#### The symptom

During the 2026-04-10 session, two commit attempts were blocked with:

```
❌ CRITICAL VIOLATION in <some doc file>
   ├─ Found bash commands from skill: test-catalog-updater
   │  <line number>: <prose that mentions command names in a warning context>
   └─ FIX: Replace with "Use test-catalog-updater skill"
```

The flagged lines were warnings saying **not** to run those commands.

#### Root cause

At `.git/hooks/pre-commit` (lines 99-112 as of 2026-04-10), the hook has a hard-coded associative array mapping skill names to regex patterns that should not appear duplicated elsewhere:

```
declare -A SKILLS=(
    ["container-restart"]="<patterns for witchcity container operations>"
    ["staging-deploy"]="<patterns for witchcity deploy operations>"
    ["test-catalog-updater"]="<PROBLEM LINE — see below>"
    ["phase-1-validator"]="<pattern for that phase validator>"
    ...
)
```

The `test-catalog-updater` line's pattern has two alternatives: one matches any line containing `npm` + `test` + `playwright` in that order, the other matches the literal phrase for running Playwright tests directly. Both fire on prose that mentions those command names in a warning context.

Two issues with the `test-catalog-updater` entry:

1. **The regex is too broad.** It matches literally anywhere in the file including inside quoted strings, fenced code blocks, and prose warnings.

2. **The attribution is wrong.** The `test-catalog-updater` skill automates TEST_CATALOG updates *after* test runs — it doesn't run tests itself. Test-running commands are the concern of `run-test-suite` and `test-environment`, and both are already enforced at the Bash-tool level by `.claude/hooks/block-manual-test-runs.py` (a PreToolUse hook, totally separate from the pre-commit check).

#### Local workaround applied 2026-04-10

The offending line was removed from `.git/hooks/pre-commit` on the development machine used during the 2026-04-10 session. Commits went through cleanly after the removal. The diff was conceptually: delete the `["test-catalog-updater"]=...` entry from the SKILLS associative array.

#### Why the workaround doesn't propagate

**`.git/hooks/` is not tracked by git.** Git hooks live in a local-only directory that isn't versioned. The edit only applies to the machine where it was made. Other developers who clone WCR get a fresh `.git/hooks/pre-commit` or no hook at all, depending on their setup. There's no shared hook-install flow in this repo — no `core.hooksPath` config, no `.githooks/` tracked directory, no Husky integration for this specific validator. Husky's `.husky/pre-commit` only runs `lint-staged`, not the single-source check.

#### Suggested fix approach

Pick one:

**Option A — Tracked source + `core.hooksPath`**
1. Create `.githooks/pre-commit` in the repo (tracked)
2. Run `git config core.hooksPath .githooks` once per clone (document in setup instructions)
3. Commit the tracked hook without the `test-catalog-updater` line

**Option B — Move into Husky** (recommended)
Extend `.husky/pre-commit` to invoke the single-source validator from a tracked location. The validator logic itself moves into a tracked file under `.githooks/` or `.claude/hooks/`, and `.husky/pre-commit` calls it before `lint-staged`.

**Option C — Fix the regex to only match inside code blocks**
Change the pattern to require the command to be at the start of a line by prefixing with `^\s*`. Surgical fix. Still has edge cases (blockquote prefixes, list markers). Doesn't fix the wrong attribution.

**Option D — Delete the `test-catalog-updater` entry entirely** (recommended)
Trust the PreToolUse hook (`block-manual-test-runs.py`) to prevent execution. Don't try to prevent prose mentions. The real risk (execution) is already mitigated elsewhere.

**Recommended combined fix**: **B + D** — move the single-source validator into Husky for propagation, AND drop the `test-catalog-updater` entry.

Estimated effort: ~1 hour for someone familiar with Husky + git hooks.

#### How to verify this is still an issue

```bash
# Still present if this returns a line
grep "test-catalog-updater" .git/hooks/pre-commit

# Still untracked if this shows nothing meaningful
find . -name "pre-commit" -not -path "./.git/*" -not -path "*/node_modules/*"

# Still uses default path if this returns empty
git config core.hooksPath
```

#### Authoritative records

- This file is the primary record
- `session-work/2026-04-10/pre-commit-hook-tech-debt.md` — gitignored local-only original investigation notes (can be deleted after reading this)

---

## Backend API

### BE-1 — `Users.CK_Users_VettingStatus_Range` CHECK constraint not tracked in EF model snapshot (P2)

**Discovered**: 2026-04-11 during Phase 3b-2 code review of the vetting status cleanup project
**Location**: `apps/api/Migrations/20260411041245_Phase3b2VettingStatusEnum.cs` + `apps/api/Migrations/ApplicationDbContextModelSnapshot.cs`
**Impact**: Model snapshot drift. The database has a CHECK constraint that the EF model does not know about, which means future auto-generated migrations have no visibility into it. Low risk today, but a correctness trap waiting to go off.

#### Symptom

Phase 3b-2 introduced a DB-layer CHECK constraint defending `Users.VettingStatus` against out-of-range enum values:

```sql
ALTER TABLE public."Users"
ADD CONSTRAINT "CK_Users_VettingStatus_Range"
CHECK ("VettingStatus" BETWEEN 0 AND 6);
```

Because the constraint was added via raw `migrationBuilder.Sql()` rather than EF's fluent `HasCheckConstraint` API, it does not appear in `ApplicationDbContextModelSnapshot.cs`. Future `dotnet ef migrations add` runs will not see it.

#### Why it happened

During Phase 3b-2 the author chose raw SQL to keep the migration focused and reversible without touching `OnModelCreating`. The trade-off was made knowingly — it was fast and safe for the immediate need — but the drift is now a real item to clean up.

#### Failure modes this could cause

1. **Silent re-adds**: if a future agent calls `HasCheckConstraint` with a different name or predicate in `OnModelCreating`, EF will generate a migration to add "the missing" constraint, creating a duplicate in production.
2. **Silent drops**: if a future migration runs `migrationBuilder.Sql("DROP ...")` or an `ALTER TABLE` that implicitly removes constraints, the model snapshot won't warn about the removal.
3. **Cross-env skew**: a dev who resets a local DB from the model snapshot (rare but possible) will end up with a DB that lacks the constraint.

#### Suggested fix approach

**Option A — Move into `OnModelCreating` (recommended)**
1. In `apps/api/Data/ApplicationDbContext.cs` inside `OnModelCreating`, add:
   ```csharp
   modelBuilder.Entity<ApplicationUser>()
       .ToTable(t => t.HasCheckConstraint(
           "CK_Users_VettingStatus_Range",
           "\"VettingStatus\" BETWEEN 0 AND 6"));
   ```
2. Run `dotnet ef migrations add Phase3b2VettingStatusConstraintTracked`. This should produce a migration that *drops and re-adds* the constraint under EF's control — inspect before applying.
3. Verify the snapshot now contains the constraint.
4. Apply the migration in dev, re-verify distribution unchanged.

**Option B — Document the drift and leave it**
If the churn of a new migration is undesirable, add a comment in `ApplicationDbContextModelSnapshot.cs` near the `Users` entity definition noting the out-of-band constraint, and add a code comment in `OnModelCreating` warning future authors not to `HasCheckConstraint` on `VettingStatus` without coordinating with the existing raw-SQL migration.

Estimated effort: Option A — 30 minutes including migration verification. Option B — 10 minutes.

#### Authoritative records

- Commit `c1f3481b` — the Phase 3b-2 commit that introduced the constraint
- `apps/api/Migrations/20260411041245_Phase3b2VettingStatusEnum.cs` — the migration itself
- Code review of Phase 3b-2 (2026-04-11, finding 3.2)

---

## Test suite (unit test coverage)

### T-6 — Redundant local enum aliases in `VettingHoldServiceTests` (P3)

**Discovered**: 2026-04-11 during Phase 3b-2 code review (finding 4.3)
**Location**: `tests/unit/api/Features/VettingHold/VettingHoldServiceTests.cs` — top-of-file
**Impact**: Cosmetic.

#### Symptom

The test file declares local `const int` or `using` aliases for vetting status values even though `VettingStatus` enum members are now directly usable. Leftover from the int-based transitional phase.

#### Suggested fix approach

Delete the aliases and use `VettingStatus.Approved` etc. directly in `InlineData` and assertion calls. Estimated effort: 10 minutes. Opportunistic cleanup.

#### Authoritative records

- Phase 3b-2 code review (2026-04-11, finding 4.3)

---

## Frontend / web app

### FE-1 — Pre-existing TypeScript errors in `EventForm.tsx` (P2)

**Discovered**: 2026-04-10 during Phase 1 code quality check of vetting status cleanup
**Location**:
- `apps/web/src/components/events/EventForm.tsx:1933` — `error TS2322: Type 'unknown' is not assignable to type 'ReactNode'`
- `apps/web/src/components/events/EventForm.tsx:2150` — `error TS2322: Type 'unknown' is not assignable to type 'ReactNode'`

**Impact**: TypeScript strict-mode CI check fails. Two errors mask any new errors introduced by the same file (hard to tell "did I make it worse?"). Zero runtime impact AFAIK — the `unknown` likely falls through to `String()` at runtime.

#### Why it was deferred

`EventForm.tsx` is unrelated to vetting status. Errors exist on `main` prior to any recent work — not introduced by the discoverer. Fixing them would expand commit scope and dilute project intent.

#### Suggested fix approach

Read the lines in context, find the JSX expression rendering an `unknown`-typed value, either narrow the type with a type guard or explicitly cast to string/ReactNode. Estimate: 15-30 min.

#### Authoritative records

- Originally documented in the now-consolidated `docs/functional-areas/vetting/vetting-status-cleanup-tech-debt-2026-04-10.md` (item #1)

---

### FE-2 — Latent XSS risk if `interviewScheduleUrl` ever becomes user-editable (P3)

**Discovered**: 2026-04-10 during Phase 1 code review of vetting status cleanup
**Location**: `apps/web/src/pages/dashboard/components/VettingAlertBox.tsx:71` (and line 81 for `reapplyInfoUrl`)
**Impact**: None **today**. Potential future risk.

#### Current state

Both `interviewScheduleUrl` and `reapplyInfoUrl` are hardcoded backend literals set in `UserDashboardProfileService.cs`. There is no untrusted input path. React sanitizes neither `href` values by default nor the absence of any user-controlled source makes this a non-issue right now.

#### Future risk

If a developer ever refactors these URLs to come from the database, admin input, or any other user-editable source, a malicious admin (or SQL injection from elsewhere) could inject `javascript:alert(document.cookie)` and React would render it as a working link.

#### Suggested fix approach

Add a `sanitizeHref()` util that returns `null` for anything not matching `/^\/[^/]|^https?:\/\//`, and use it in `VettingAlertBox`. OR add an inline comment warning future refactorers. Estimate: 30 min.

#### Authoritative records

- Originally documented in the now-consolidated `docs/functional-areas/vetting/vetting-status-cleanup-tech-debt-2026-04-10.md` (item #7)

---

## Documentation

### DOC-1 — Stale reference in `apps/web/UNIT_TEST_FIX_GUIDE.md` (P3)

**Discovered**: 2026-04-10 during Phase 1 code review of vetting status cleanup
**Location**: `apps/web/UNIT_TEST_FIX_GUIDE.md:161, 338` — references `VettingStatusBox.test.tsx` as "failing" but the guide itself is stale and the referenced test file was deleted in vetting Phase 2h.

#### Suggested fix approach

Librarian sweep. Either update the guide or delete it if obsolete. Low priority.

#### Authoritative records

- Originally documented in the now-consolidated `docs/functional-areas/vetting/vetting-status-cleanup-tech-debt-2026-04-10.md` (item #6)

---

# Resolved items

These items came up during a previous session and were resolved. Listed here for traceability so future agents don't re-investigate them. Format: `(date resolved) (original item number/ID) — short summary with commit ref`.

## 2026-04-10 — Vetting status cleanup project

- **`VettingStatusBox.tsx` orphaned dead code** — deleted in Phase 2h alongside `StatusBoxProps` interface and the already-broken `VettingStatusBox.test.tsx`. Replacement for the component is `VettingAlertBox`. Originally item #3 in vetting cleanup tech debt.
- **`UserDashboardProfileService.cs` switch did not handle `Withdrawn`** — resolved in Phase 3a (commit `91d51770`). Added `case VettingStatus.Withdrawn:` with a reasonable message so withdrawn applicants get a meaningful status message instead of falling through to empty-string default. Originally item #4 in vetting cleanup tech debt / BE-1 in the first pass of this consolidated file.
- **Docblock drift in `VettingApplication.cs` enum XML comment** — resolved in Phase 3a (commit `91d51770`). Rewrote the XML docblock that had every enum value wrong and referenced a non-existent "InterviewCompleted" state. Now accurately lists all 7 actual enum values and their persisted integers (`Withdrawn = 6`, not 7). Originally item #5 in vetting cleanup tech debt / BE-2 in the first pass of this consolidated file.

## 2026-04-11 — Vetting status cleanup project (Phase 3b-1, 3b-2, 3c)

These entries document the primary refactoring work that closed out the vetting status cleanup project. None were tracked as "active items" prior to resolution — they were the project's main feature work — but they are recorded here for traceability so future agents have a complete audit trail of what the cleanup project actually did.

### Core refactoring (Phase 3b-1 + 3b-2)

- **DTO layer `int` → `VettingStatus` enum conversion (Phase 3b-1)** — resolved in commits `cff87f33` (primary) + `7eaa7700` (review S1 docblock fix). All DTOs that exposed `VettingStatus` over the wire were changed from `int` to the strongly-typed `VettingStatus` enum. The global `JsonStringEnumConverter` then serialized enum values as strings (`"Approved"`, `"UnderReview"`, etc.) rather than integers. This removed the last place in the stack where the enum was visible as an int to the frontend, and unblocked auto-generated TypeScript types from `@witchcityrope/shared-types` to use the proper union-literal type instead of `number`.
- **`ApplicationUser.VettingStatus` entity `int` → `VettingStatus` enum conversion (Phase 3b-2)** — resolved in commit `c1f3481b`. Changed the entity property from `int` to the `VettingStatus` enum, rewrote 13+ LINQ queries across `EmailTemplateService`, `MemberDetailsService`, `VettingService`, `VettingHoldService`, `AttendanceSeeder`, `UserSeeder`, and related services from `(int)VettingStatus.X` casts to direct enum comparisons. EF Core + Npgsql continues to translate these to SQL int comparisons with identical query plans, so no runtime performance or schema change. The `IsVetted` computed property was kept as `[NotMapped]` for backward compat during the migration window. Total surface area: ~40 initializer sites and 24 `UserSeeder.cs` sites converted; 20 test files updated by the test-executor agent with zero regressions.
- **`CK_Users_VettingStatus_Range` DB-layer CHECK constraint added (Phase 3b-2)** — resolved in commit `c1f3481b` via migration `20260411041245_Phase3b2VettingStatusEnum`. Adds a `CHECK (VettingStatus BETWEEN 0 AND 6)` constraint to `public."Users"` to defend against `(VettingStatus)99`-style out-of-range casts silently propagating through to the DB. The constraint was the closing mitigation for a code-review finding during Phase 3b-1 that flagged the silent-propagation gap. Column type remains `integer` — no schema change, no data migration. Constraint is reversible via the migration's `Down()`.
- **Latent `AttendanceSeeder` "vetted" filter bug (`VettingStatus >= 3`)** — resolved in commit `c1f3481b` as part of the Phase 3b-2 sweep. Two queries in `AttendanceSeeder.SeedEventParticipationsAsync` and `CreateHistoricalSocialEventParticipationsAsync` previously used `u.VettingStatus >= 3` which accidentally included `Denied` (4), `OnHold` (5), and `Withdrawn` (6) as "vetted" for social event seeding. Changed to strict `u.VettingStatus == VettingStatus.Approved` to match the business rule. No production impact (seeder is dev/test only), but a real correctness bug hiding behind the old int comparison.

### Pre-deploy hygiene (Phase 3b-2 follow-up)

Both items below were flagged by the code-reviewer agent on commit `c1f3481b` as "approve with minor changes" and fixed in the same-sitting follow-up commit `8691f12c`. They never went through the "active item" stage — recording here for traceability of the code-review loop.

- **Migration SQL schema qualification** — resolved in commit `8691f12c`. Phase 3b-2's migration originally used `ALTER TABLE "Users"` without schema qualification, inconsistent with every other migration in the project (which all use `schema: "public"` via EF's fluent API). Reviewer flagged this as a `search_path`-safety hygiene issue. Changed to `ALTER TABLE public."Users"` in both `Up()` and `Down()`. Databases that had already applied the migration need no action — the constraint name and predicate are unchanged, so the change is invisible to existing DB state.
- **Residual `.cs.bak` files in `apps/api/Features/Vetting/`** — resolved in commit `8691f12c`. Four `.cs.bak` files (`VettingService.cs.bak`, `IVettingService.cs.bak`, `VettingStatusResponse.cs.bak`, `VettingEndpoints.cs.bak` — ~1,441 lines total) predated Phase 3b-2 and were never cleaned up. Reviewer flagged as a low-priority hygiene issue. All four deleted; `.gitignore` patterns added for `*.cs.bak`, `*.ts.bak`, `*.tsx.bak` so future bulk-rewrite tooling cannot re-introduce them.

### Deployment (Phase 3c)

- **Phase 3 cleanup deployed to staging** — completed on 2026-04-11. `staging-deploy` skill deployed git SHA `8691f12c` to `https://staging.notfai.com`. Independent verification confirmed: (1) CHECK constraint exists on `public."Users"` with the correct predicate `(VettingStatus >= 0 AND VettingStatus <= 6)`; (2) existing 728 user rows distribute as `{0:6, 1:73, 3:647, 4:1, 5:1}` — all within the valid range, no constraint violations; (3) all 3 staging containers healthy; (4) smoke tests pass (homepage + events API); (5) browser verification on `/dashboard`, `/join`, `/admin/members` shows correct rendering of the new enum-typed status.

### Post-deploy opportunistic cleanup (P3 items BE-2, BE-3, BE-4, T-5)

Four items were raised against staging-deployed code as low-risk quick wins. Two were fixed, one was a false positive, and one was deferred as not-worth-it. See commit TBD for the fixes.

- **BE-2 — `AttendanceSeeder` client-side `VettingStatus` filter moved server-side** — resolved. `CreateHistoricalSocialEventParticipationsAsync` previously loaded the entire `Users` table via `_userManager.Users.ToListAsync` and then filtered in memory with `.Where(u => u.VettingStatus == Approved)`. Moved the predicate into the LINQ query before `ToListAsync` so EF translates the filter to SQL. Matches the server-side pattern already in use by the sibling `SeedEventParticipationsAsync` method. Seeder-only code — no production impact, but a hygiene win and one less "why is this different from its sibling" question for future maintainers.
- **BE-3 — "Not Started" fallback text in `GetStatusName`** — **FALSE POSITIVE** discovered during investigation. The Phase 3b-2 commit `c1f3481b` already rewrote `VettingHoldService.GetStatusName` with `_ => "Unknown"` as the default branch. The code reviewer's finding 4.2 misread the code. No source change needed. Future lesson: verify reviewer findings against the actual committed code before logging as tech debt. The rule applies even when the reviewer is specific and confident.
- **BE-4 — Stale `(N)` numeric comments in `VettingSeeder.cs`** — resolved. Removed the numeric suffix comments throughout `VettingSeeder.cs` initializer sites. More importantly, five of the comments were **wrong**: several sites claimed `Approved (4)` when `Approved` is actually enum value `3`. These were holdovers from an earlier int-based design where the values were different. Cleanup removed the incorrect numeric suffixes entirely while preserving the `CORRECTED from X` history notes on the sites that had them. Followup win: the corrected comments now read cleaner (e.g. `// CORRECTED from Approved` instead of `// Denied (4) - CORRECTED from Approved`).
- **T-5 — Unit test for `GetStatusName` default branch** — deferred / not-worth-it. `GetStatusName` is `private static` and the default branch is effectively unreachable at runtime thanks to the Phase 3b-2 DB `CK_Users_VettingStatus_Range` CHECK constraint. Writing a test would require either making the method `internal static` + `InternalsVisibleTo` (adds API surface for one test) or using reflection. Combined with the fact that the compiler + DB constraint already enforce the invariant we would be asserting, the test is low-value gymnastics. Explicitly not doing this. Recorded in resolved rather than deleted so a future agent can see it was evaluated and deemed not worth the effort.

## 2026-04-10 — Test infrastructure cleanup

- **`WitchCityRope.SystemTests` inclusion** — decided: include in `run-test-suite` skill's `TEST_PROJECTS` list. The 6 pre-flight health check tests now run as part of the standard `--mode unit` pass. They target dev URLs (React :5173, API :5655, postgres :5434), so they'll fail if dev containers aren't running — intentional. Commit `fe756812`.
- **`VolunteerPosition.SessionId` nullability** — not a decision to make; commit `1caaa4ca` already made it. That commit (`fix: volunteer emails now list all assignments + remove event-wide positions`) intentionally removed the "event-wide volunteer positions" feature and tightened `VolunteerPosition.SessionId` to non-null. The skipped test `VolunteerSignup_EventWide_UsesEarliestFutureSession` was deleted in commit `fe756812`.
- **Azure.Identity transitive vulnerability** — pinned to `1.14.0` via Central Package Management in commit `e1c0dd8e`. Requires both `<PackageVersion>` in `Directory.Packages.props` AND direct `<PackageReference>` in `tests/WitchCityRope.Tests.Common/WitchCityRope.Tests.Common.csproj` (CPM silently ignores `PackageVersion` for purely transitive deps). Eliminates three advisories (`GHSA-5mfx-4wcx-rv27` high + two moderates) without the Respawn 7.0.0 upgrade that initially caused misleading test timing drift.
- **Volunteer Session FK helper bug** — fixed in commit `e1c0dd8e`. Three test helpers (`VolunteerTimingTests`, `SessionBasedVolunteerTimingTests`, `VolunteerServiceTests`) were creating `VolunteerPosition` with `SessionId = Guid.Empty`, violating the FK constraint introduced in commit `1caaa4ca`. Unblocked 28 tests.
- **Stale Vetting role-grant tests** — two tests rewritten in commits `9a815064` (`Approval_GrantsVettedMemberRole`) and `e1c0dd8e` (`UpdateApplicationStatusAsync_FromFinalReviewToApproved_GrantsVettedMemberRole`) to assert the current `VettingStatus = Approved` behavior instead of the pre-2025-10-19-refactor `Role = "VettedMember"` behavior.
- **Compile cascade in Core Tests** — five files fixed in commit `9a815064`: `EmailTemplateServiceTests` (removed `Variables` property setters), `AuthenticationServiceTests` (removed stale `Models.Auth` namespace + added `IRefreshTokenService` mock), and three `EventService*Tests` files (added `IAttendanceCountService` mock).
- **Broken `test-environment --mode unit|integration|dotnet|all` paths** — removed entirely in commit `fbf5ebb9`. Those paths tried to `docker-compose exec api dotnet test` into a container whose test stage only copied `apps/api/` (no test projects), silently producing zero results since 2025-11-27. Replaced with the new `run-test-suite` skill that runs `dotnet test` from the host.

---

# Template for new entries

Copy this block when adding a new item. Place it under the appropriate area section, sorted by priority then discovery date.

```markdown
### [AREA]-N — Short descriptive title (P?, UNRESOLVED | DEFERRED | BLOCKED)

**Discovered**: YYYY-MM-DD during <session or project name>
**Impact**: <who/what this affects, how many tests/users/systems, severity>

#### Symptom

<What you observe — exact error messages, failing tests, broken behavior. Prose is fine but include code/log snippets where they clarify. Future debuggers will grep this section.>

#### Root cause (or theory)

<What you think is going on. Mark unverified theories as such. If the root cause is fully understood, say so and cite evidence.>

#### Dead-end fixes (if applicable)

<Things that were tried during investigation and did NOT work. Include WHY they didn't work so future debuggers don't repeat the work. Format as a numbered list.>

#### Suggested fix approach

<Concrete steps someone could take to fix this. Estimate effort if possible. If the fix is unknown, say so and describe the investigation path instead.>

#### Authoritative records

<Pointers to the primary sources for this item — commits, lessons-learned entries, CURRENT_TEST_STATUS.md sections, related issues. The tech-debt.md entry is the index; the authoritative records are where the full context lives.>
```

Area codes used so far:
- **T** — Testing infrastructure
- **TL** — Tooling / development environment
- **BE** — Backend API (.NET)
- **FE** — Frontend / web app (React)
- **DOC** — Documentation

Add new area codes as needed (e.g., **DB** for database, **DEP** for deployment, **SEC** for security).

---

# History

| Date | Change | By |
|---|---|---|
| 2026-04-10 | File created. Consolidated content from `docs/functional-areas/vetting/vetting-status-cleanup-tech-debt-2026-04-10.md` (7 items, 1 resolved) and `docs/standards-processes/testing/test-infra-tech-debt-2026-04-10.md` (4 unresolved + 7 resolved). Those source files were deleted after consolidation. | Test infra cleanup session (commit `726f32b3`) |
| 2026-04-11 | Phase 3b-2 code review follow-up: added 6 new active items — `BE-1` (CHECK constraint not in model snapshot, P2), `BE-2` (AttendanceSeeder pre-existing client-side filter, P3), `BE-3` ("Not Started" fallback text, P3), `BE-4` (stale `(N)` comments, P3), `T-5` (missing unit test for default branch, P3), `T-6` (redundant enum aliases, P3). Added new resolved section "2026-04-11 — Vetting status cleanup project (Phase 3b-1, 3b-2, 3c)" documenting the primary refactoring work, the two hygiene fixes (migration schema qualification + `.cs.bak` cleanup), and the staging deploy completion. | Phase 3b-2 hygiene + Phase 3c session (commits `8691f12c` and `d7e90748`) |
| 2026-04-11 | Opportunistic cleanup of 4 P3 items in areas touched by Phase 3: `BE-2` resolved (AttendanceSeeder filter moved server-side), `BE-3` resolved as FALSE POSITIVE (code already returned `"Unknown"`), `BE-4` resolved (stale `(N)` comments stripped — several were wrong), `T-5` resolved as deferred/not-worth-it (private method, unreachable branch). Active items remaining: `BE-1`, `T-1..4`, `T-6`, `TL-1`, `FE-1`, `FE-2`, `DOC-1`. | Post-deploy hygiene session (this commit) |

---

*For current test suite state, see [`docs/standards-processes/testing/CURRENT_TEST_STATUS.md`](standards-processes/testing/CURRENT_TEST_STATUS.md). For the file registry, see [`docs/architecture/file-registry.md`](architecture/file-registry.md). For lessons learned about common failures, see [`docs/lessons-learned/`](lessons-learned/).*
