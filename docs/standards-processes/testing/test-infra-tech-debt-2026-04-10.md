# Test Infrastructure Tech Debt — 2026-04-10

<!-- Created: 2026-04-10 -->
<!-- Owner: Test Team -->
<!-- Status: Active issues from the 2026-04-10 Test Infrastructure Cleanup session -->
<!-- Related commits: fbf5ebb9, 9a815064, e1c0dd8e, a7e9d13a, fe756812 -->

## Purpose

Consolidated tech debt index from the 2026-04-10 Test Infrastructure Cleanup session. All items below are either:
- **Unresolved** — known issues that need future investigation
- **Deferred** — known to need work, but out of scope for the originating session
- **Documented but local-only** — fixes applied to untracked files that won't propagate to other developers

This document is the single source of truth for where these items are tracked. Most items cross-reference authoritative details in other tracked files (lessons learned, test status doc, commit messages); this file is the index, not the documentation itself.

For the session's broader context and current test baseline, see [`CURRENT_TEST_STATUS.md`](CURRENT_TEST_STATUS.md).

---

## 🚨 P1: WebApplicationFactory shared-state pollution bug (UNRESOLVED)

**Impact**: ~36 of the 46 Integration test failures in the current baseline. Code is fine, test infrastructure is broken.

### Symptom

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

### Proof the code is fine

Both affected classes pass 100% when run in isolation:

```bash
bash .claude/skills/run-test-suite/execute.sh --mode unit --filter "FullyQualifiedName~AdminAssignmentEndpointTests"
# → 8/8 pass (20 seconds)

bash .claude/skills/run-test-suite/execute.sh --mode unit --filter "FullyQualifiedName~AuthorizedContactEndpointTests"
# → 16/16 pass (21 seconds)
```

### Root cause theory (unverified)

Some earlier test in the sequential collection pollutes global process state (most likely a singleton in the API's DI container or a static field somewhere in the test helpers). The `private static _sharedFactory` caching pattern then locks in the poisoned state for the entire lifetime of each affected class, so every test in those classes fails identically.

### Three dead-end fixes — DO NOT RE-TRY

During the 2026-04-10 investigation, three successive root-cause theories were applied and all produced **zero** improvement in the failure count. Documenting them here so future debuggers don't repeat the work:

1. **Adding `when (ex is not HostAbortedException)`** to the try/catch around `app.Run()` in `Program.cs`. **Why it doesn't work**: `WebApplicationFactory<Program>` in .NET 10 uses `DeferredHostBuilder` + `TaskCompletionSource` for host capture, NOT `HostAbortedException` throwing. Verified by reading the dotnet/aspnetcore source. The exception never reaches the catch, the filter is a no-op.

2. **Switching `Serilog.CreateBootstrapLogger()` → `CreateLogger()`** per [serilog/serilog-aspnetcore#289](https://github.com/serilog/serilog-aspnetcore/issues/289). **Why it doesn't work**: that issue's bug only manifests in **parallel** test execution. Our failing tests are in `[Collection("Sequential")]` with `DisableParallelization = true`, so they run serially. Fix doesn't apply.

3. **Reverting Respawn 7.0.0 → 6.2.1**. **Why it doesn't work**: the initial correlation was coincidental timing drift. Reverting produced identical failure counts (verified by run-4 vs run-5 baseline diff). Respawn is not involved.

### Where the authoritative record lives

- [`docs/lessons-learned/test-executor-lessons-learned.md`](../../../docs/lessons-learned/test-executor-lessons-learned.md) — the "Entry point exited without ever building an IHost" prevention pattern has the full diagnostic procedure and the dead-end fix list
- [`CURRENT_TEST_STATUS.md`](CURRENT_TEST_STATUS.md) — "Known Issues" section with the current failing class list
- Commit `e1c0dd8e` — full investigation notes in the commit message
- Commit `a7e9d13a` — added the lessons-learned prevention pattern

### What the real fix probably looks like

Identify which earlier test corrupts which global state. Two approaches:

1. **Bisect the test order**: run subsets of the integration suite incrementally and find the smallest subset that triggers the failure. The last-added test in that subset is the culprit. Use `run-test-suite --mode unit --filter` with different filters to narrow it down.
2. **Drop the `_sharedFactory` pattern**: replace the `private static WebApplicationFactory<Program>? _sharedFactory` caching with per-test factories. This trades test-run speed (factories are expensive to build) for isolation. If the failures disappear, the shared factory was the problem.

Estimated effort: **~4 hours** for a focused session with no prior-misdiagnosis baggage.

---

## 🟡 P2: `EmailTemplateServiceTests.SendAdHocEmailAsync_*` — 9 behavioral drift failures

**Impact**: 9 unit tests in `tests/unit/api/Features/EmailTemplates/EmailTemplateServiceTests.cs` (under the `WitchCityRope.Api.Tests.Services` namespace).

### Symptom

All 9 failures are NSubstitute mock assertion errors. Example:

```
NSubstitute.Exceptions.ReceivedCallsException : Expected to receive exactly 1 call matching:
    SendEmailAsync("test-2a995e50cc704ed6b27a84e61f33723a@example.com", "Welcome",
                   html => html.Contains("Hello TestSceneName!"),
                   text => ((text != null) AndAlso text.Contains("Hello TestSceneName!")),
                   any CancellationToken)
Actually received no matching calls.
```

### Affected test methods

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

### Diagnosis

**Real behavioral drift, not `Variables`-property bit-rot.** The `SendAdHocEmailAsync` method either:
- No longer calls `SendEmailAsync` with per-user content (maybe routes through a different service method)
- Uses a different template variable resolver
- Has changed its call signature enough that the mock setup no longer matches

Important clarification: the Core.Tests version of `EmailTemplateServiceTests` had a completely different failure mode (`Variables` property setters pointing at a property that no longer exists on `GlobalEmailTemplate`). That was fixed in commit `9a815064`. **These unit-test failures are a different problem** — the `Variables` property is not involved.

### What a fix would look like

1. Read `apps/api/Features/EmailTemplates/Services/EmailTemplateService.SendAdHocEmailAsync` to understand its current call pattern
2. Compare against the mock expectations in each failing test
3. Decide whether the service changed by design (update the tests) or by accident (update the service)
4. Fix one test, verify it runs clean, then apply the pattern to the rest

Estimated effort: **~2 hours** for someone with context on the email template variable resolver.

### Where the authoritative record lives

- [`CURRENT_TEST_STATUS.md`](CURRENT_TEST_STATUS.md) — "Known Issues" section has the full list of test names + sample error signature
- Commit `e1c0dd8e` message — flagged as deferred during the session with an explanation

---

## 🟡 P2: `TEST_CATALOG.md` full refresh pending

**Impact**: Navigation index for all WCR tests is ~4 months stale. New test classes added since 2025-12-13 are not listed. Per-feature-area tables reflect December counts, not current state.

### Current state

- File header version: `12.12.0` with date `2025-12-13`
- Body is a snapshot from a December 2025 E2E execution run
- Does not reflect:
  - The new `run-test-suite` skill (2026-04-10)
  - Current .NET test counts (1,110 API unit + 132 Core + 268 integration + 6 SystemTests = 1,516 total)
  - The compile cascade that was blocking API Unit + Core Tests before 2026-04-10
  - The 137 tests added by commit `cb8adcdb` (ticket assignment + proxy RSVP feature)

### Partial mitigation applied 2026-04-10

I added a "Current Numbers (2026-04-10)" header section at the top of the file pointing to `CURRENT_TEST_STATUS.md` for current numbers. The body tables below that header are unchanged and still show the December snapshot.

### What a proper refresh looks like

This is a dedicated test-executor agent task, not something to squeeze into a cleanup session. The refresh should:

1. Run a clean full baseline via `run-test-suite --mode all`
2. Enumerate every `.cs` test file in each of the 4 .NET test projects (`tests/unit/api/`, `tests/WitchCityRope.Core.Tests/`, `tests/integration/`, `tests/WitchCityRope.SystemTests/`)
3. Extract per-file test counts and pass/fail status from the skill output
4. Update or replace the feature-area tables in `TEST_CATALOG.md` body
5. Update the version header + the navigation links to sibling catalog parts (`TEST_CATALOG_PART_2.md`, etc.)

Estimated effort: **~2 hours** for test-executor with a fresh baseline run.

### Where the authoritative record lives

- [`TEST_CATALOG.md`](TEST_CATALOG.md) — header note added 2026-04-10
- [`CURRENT_TEST_STATUS.md`](CURRENT_TEST_STATUS.md) — current numbers (as a stopgap until the catalog catches up)

---

## 🟡 P2: Pre-commit hook over-broad regex + untracked hook source

**Impact**: Commits that touch documentation mentioning test-runner command names verbatim (even in "do NOT use these" warning contexts) are blocked by a false positive in the single-source-of-truth validator. Affects any doc commit that lists `dotnet test`, `npm test`, `npx playwright test`, etc. as examples of blocked commands.

### The symptom

During the 2026-04-10 session, two commit attempts were blocked with:

```
❌ CRITICAL VIOLATION in <some doc file>
   ├─ Found bash commands from skill: test-catalog-updater
   │  <line number>: <prose that mentions "npx playwright test" in a warning context>
   └─ FIX: Replace with "Use test-catalog-updater skill"
```

The flagged lines were warnings saying **not** to run those commands, e.g.:

> "NEVER run `dotnet test`, `npm test`, `npx playwright test`, or similar directly via Bash"

### Root cause

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

The `test-catalog-updater` line's pattern has two alternatives: one matches any line containing `npm` + `test` + `playwright` in that order, the other matches the literal phrase `npx playwright test`. Both fire on prose that mentions those command names in a warning context.

Two issues with the `test-catalog-updater` entry:

1. **The regex is too broad.** `npx playwright test` matches literally anywhere in the file including inside quoted strings, fenced code blocks, and prose. `npm.*test.*playwright` matches any line with those three words in that order, which catches warnings like *"never run npm test or playwright test directly"*.

2. **The attribution is wrong.** The `test-catalog-updater` skill automates TEST_CATALOG updates *after* test runs — it doesn't run tests itself. Test-running commands are the concern of `run-test-suite` and `test-environment`, and both are already enforced at the Bash tool level by `.claude/hooks/block-manual-test-runs.py` (a PreToolUse hook, totally separate from the pre-commit check).

### Local workaround applied 2026-04-10

The offending line was removed from `.git/hooks/pre-commit` on the development machine used during the 2026-04-10 session. Commits went through cleanly after the removal. The diff is conceptually:

> Delete the `["test-catalog-updater"]=...` entry from the `SKILLS` associative array in `.git/hooks/pre-commit` (one line removal). Leave all other entries in place.

### Why the workaround doesn't propagate

**`.git/hooks/` is not tracked by git.** Git hooks live in a local-only directory that isn't versioned. The edit only applies to the machine where it was made. Other developers who clone WCR get:
- A fresh `.git/hooks/pre-commit` (if they have a hook-install script — but I couldn't find one)
- OR no hook at all (if they don't)

There's no shared hook-install flow in this repo — no `core.hooksPath` config, no `.githooks/` tracked directory, no Husky integration for this specific validator. Husky's `.husky/pre-commit` only runs `lint-staged`, not the single-source check.

### Proper fix options

Pick one:

#### Option A — Tracked source + `core.hooksPath`

1. Create `.githooks/pre-commit` in the repo (tracked)
2. Run `git config core.hooksPath .githooks` once per clone (document in setup instructions)
3. Commit the tracked hook without the `test-catalog-updater` line

**Pro**: Single source of truth, everyone gets the same hook.
**Con**: Requires a one-time setup step per clone.

#### Option B — Move into Husky (**recommended**)

Husky is already set up (`.husky/pre-commit` currently only calls `lint-staged`). Extend it to invoke the single-source validator from a tracked location as well. The validator logic itself would move into a tracked file under `.githooks/` or `.claude/hooks/`, and `.husky/pre-commit` would call it before calling `lint-staged`.

**Pro**: Uses existing infrastructure, no new `core.hooksPath` config needed, installs automatically via `npm install`.
**Con**: Developers must run `npm install` for Husky to set up the `.husky/_` shim.

#### Option C — Fix the regex to only match inside code blocks

Change the pattern to require the command to be at the start of a line (typical of code blocks, unusual in prose) by prefixing the current alternation with a `^\s*` anchor. The pattern becomes "match only when the command appears at the start of a line after optional whitespace".

**Pro**: Surgical fix, minimal change.
**Con**: Still has edge cases (blockquote prefixes, list markers). Doesn't fix the wrong attribution.

#### Option D — Delete the `test-catalog-updater` entry entirely (**recommended**)

Trust the PreToolUse hook (`block-manual-test-runs.py`) to prevent execution. Don't try to prevent prose mentions. The risk (execution) is already mitigated elsewhere.

**Pro**: Zero false positives. The real risk is already handled by the other hook.
**Con**: Legitimate duplication of `npx playwright test` bash commands in docs would go undetected — but since both skills (`run-test-suite`, `test-environment`) wrap these commands, and the PreToolUse hook prevents bypass, the documentation-level duplication check is low-value.

### Recommended combined fix

**B + D**: Move the single-source validator into Husky for propagation, AND drop the `test-catalog-updater` entry since it's false-positive-prone and redundant with the PreToolUse hook.

Estimated effort: **~1 hour** for someone familiar with Husky + git hooks.

### How to verify this is still an issue in a future session

```bash
# Still present if this returns a line
grep "test-catalog-updater" .git/hooks/pre-commit

# Still untracked if nothing shows
find . -name "pre-commit" -not -path "./.git/*" -not -path "*/node_modules/*"
# (should eventually show a tracked .githooks/pre-commit or similar)

# Still uses default path if this returns empty
git config core.hooksPath
```

### Where the authoritative record lives

- `session-work/2026-04-10/pre-commit-hook-tech-debt.md` — **local only, gitignored**, full original investigation notes
- This file — the consolidated tracked version you are reading

---

## ✅ Resolved during 2026-04-10 session (historical context)

These items came up during the session and were resolved. Listed here for traceability and so future agents don't re-investigate them.

### `WitchCityRope.SystemTests` inclusion

**Resolution**: Added to the `run-test-suite` skill's `TEST_PROJECTS` array in commit `fe756812`. The 6 pre-flight health check tests now run as part of the standard `--mode unit` pass. They target dev URLs (React :5173, API :5655, postgres :5434), so they'll fail if dev containers aren't running — intentional. To skip them without running the dev stack, filter with `--filter "Category!=HealthCheck"`.

### `VolunteerPosition.SessionId` nullability

**Resolution**: Not a decision to make — commit `1caaa4ca` already made it. That commit (`fix: volunteer emails now list all assignments + remove event-wide positions`) intentionally removed the "event-wide volunteer positions" feature and tightened `VolunteerPosition.SessionId` to non-null. Per the commit message: *"Event-wide volunteer logic removed from recipient service, volunteer service, and assignment service. Migration safely cleans up any null-SessionId rows before applying the constraint."*

The test I had initially skipped pending this decision (`VolunteerSignup_EventWide_UsesEarliestFutureSession`) was deleted in commit `fe756812` with a comment pointing back to `1caaa4ca`.

### Azure.Identity transitive vulnerability

**Resolution**: Pinned to `1.14.0` via Central Package Management in commit `e1c0dd8e`. Requires:
1. `<PackageVersion Include="Azure.Identity" Version="1.14.0" />` in `Directory.Packages.props`
2. `<PackageReference Include="Azure.Identity" />` in `tests/WitchCityRope.Tests.Common/WitchCityRope.Tests.Common.csproj` (required for CPM to apply the pin to transitive deps)

This eliminates three advisories (`GHSA-5mfx-4wcx-rv27` high + two moderates) without the Respawn 7.0.0 upgrade that initially caused misleading test timing drift.

### Volunteer Session FK helper bug

**Resolution**: Fixed in commit `e1c0dd8e`. Three test helpers were creating `VolunteerPosition` with `SessionId = Guid.Empty`, violating the FK constraint introduced in commit `1caaa4ca`. Fixed by setting `SessionId = session.Id` in each helper. Unblocked 28 tests across VolunteerServiceTests + VolunteerTimingTests.

### Stale Vetting role-grant tests

**Resolution**: Two tests rewritten in commits `9a815064` and `e1c0dd8e` to assert the current `VettingStatus = Approved` behavior instead of the pre-2025-10-19-refactor `Role = "VettedMember"` behavior.

### Compile cascade in Core Tests

**Resolution**: Five files fixed in commit `9a815064` — `EmailTemplateServiceTests` (removed `Variables` property setters), `AuthenticationServiceTests` (removed stale `Models.Auth` namespace + added `IRefreshTokenService` mock), and three `EventService*Tests` files (added `IAttendanceCountService` mock).

### Broken `test-environment --mode unit|integration|dotnet|all` paths

**Resolution**: Removed entirely in commit `fbf5ebb9`. Those paths tried to `docker-compose exec api dotnet test` into a container whose test stage only copied `apps/api/` (no test projects), silently producing zero results since 2025-11-27. Replaced with the new `run-test-suite` skill that runs `dotnet test` from the host.

---

## How to use this document

**When picking up one of these items**:

1. Read the relevant item's section above (symptom, diagnosis, dead-end fixes, what a fix would look like)
2. Cross-reference the "Where the authoritative record lives" pointers for full details
3. Before starting a fix, verify the current state hasn't changed since this doc was written — check the authoritative records, not just this index

**When adding new tech debt**:

1. Add a new section following the same structure (Impact, Symptom, Root cause, Fix options, Where the record lives)
2. Update the item count in the section headers
3. If the item is resolved, move it to the "Resolved" section with a commit reference

**When items are resolved**:

Don't delete them — move to the "Resolved during 2026-04-10 session (historical context)" section. This preserves the trail for debugging if a resolved item turns out to have been resolved incorrectly.

---

*For the current test baseline, see [`CURRENT_TEST_STATUS.md`](CURRENT_TEST_STATUS.md). For the skills that run tests, see `.claude/skills/run-test-suite/SKILL.md` and `.claude/skills/test-environment/SKILL.md`. For lessons learned about running tests, see [`docs/lessons-learned/test-executor-lessons-learned.md`](../../../docs/lessons-learned/test-executor-lessons-learned.md).*
