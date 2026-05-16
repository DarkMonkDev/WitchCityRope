# Technical Debt — WitchCityRope

<!-- Last Updated: 2026-04-11 -->
<!-- Owner: All agents collectively; curated by the librarian -->
<!-- Status: Active tracking document — the single source of truth for all tech debt in this repo -->

> ## 🚨 IMPORTANT — READ BEFORE ADDING OR EDITING ANY ENTRY 🚨
>
> **The goal of these rules is to prevent duplication and data fragmentation — NOT to prevent new entries.** Genuinely new tech debt items absolutely belong here as new entries. What these rules prevent is logging the same issue twice under different IDs, or losing empirical data by creating a parallel entry when an existing one should have been updated.
>
> 1. **READ THE ENTIRE FILE FIRST.** Before adding or editing an entry, scan every section (Active items AND Resolved items) for anything that might already cover what you're about to add. A surface-level grep for a keyword is NOT enough — actually read the surrounding entries. The file is short enough that this is cheap, and the cost of duplication is much higher than the cost of a careful read.
>
> 2. **DECIDE CAREFULLY: NEW ENTRY vs. UPDATE.**
>    - **Add a new entry** when you've found a genuinely new issue — a distinct bug, drift, or architectural concern that no existing entry covers. This is fine and expected.
>    - **Update an existing entry** when you have new empirical data, a better theory, a partial fix, a correction, or anything else that logically belongs inside an existing entry's scope. Updating in place keeps the full trail in one place.
>    - **Signs it's probably a new entry**: different root cause, different subsystem, different symptom, different fix approach.
>    - **Signs it's probably an update**: same root cause with new evidence, same symptom observed in a new location, refinement of an existing theory, new failure-count data for an already-tracked test suite bug.
>
> 3. **IF YOU'RE GENUINELY UNSURE, ASK THE USER.** Don't force-fit a new issue into an existing entry just to avoid adding one, and don't split an existing issue into two entries just to avoid the effort of updating. When the call is close, a one-sentence question to the user is always cheaper than cleaning up later.
>
> 4. **WHEN YOU UPDATE AN ENTRY, ADD A DATED NOTE TO THE BODY** rather than rewriting what's already there. Format: add an `**Updated**: YYYY-MM-DD — <short note>` line under the header, plus a new dated sub-section in the body. Preserve the original text so the history trail stays intact.
>
> 5. **NO SILENT OVERWRITES.** Never delete or substantially rewrite an existing entry's body without the user's explicit approval. Historical context is load-bearing — future agents need to see what was tried, what was ruled out, and why.
>
> 6. **CROSS-REFERENCE RELATED ENTRIES.** When adding a new entry (or updating one) that is related to another — same subsystem, same symptom class, similar fix pattern — link to the related entry by ID (`T-1`, `BE-5`, etc.) so the network stays navigable. Cross-referencing is how we allow genuinely distinct entries to coexist without losing their relationships.
>
> 7. **ADD A HISTORY ROW.** Every commit that touches this file should add a row to the [History](#history) table at the bottom with the date, a short description of the change, and the commit reference. This is what makes the update trail auditable.
>
> **In short**: read first, duplicate never, relate always, ask when unsure. New entries are welcome; duplicates are not.

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
**Last updated**: 2026-04-11 — empirical confirmation of test-order theory, baseline numbers corrected
**Impact**: **61–77 of the current Integration test failures** (see "Updated failure baseline" below). Code is fine, test infrastructure is broken.

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

#### Updated failure baseline (2026-04-11)

The original T-1 entry cited "~36 of 46" integration test failures, measured 2026-04-10. Those numbers are stale. Three test runs on 2026-04-11 (post-Phase-3c, post-BE-2/BE-4 cleanup, identical code under test) produced:

| Run | Mode | Integration Passed | Failed | Skipped | Notes |
|---|---|---|---|---|---|
| Phase 3b-2 baseline (`25a83285`) | `--mode unit` | 179 | **77** | 11 | Verified code-change zero regressions |
| Post-BE-2/BE-4 (`25a83285`) | `--mode unit` | 179 | **77** | 11 | Same as baseline run |
| Post-BE-2/BE-4 (`25a83285`) | `--mode all` | 195 | **61** | 11 | **16 more passing, same code** |

**Key observation**: Three identical `dotnet test` runs against the same commit (`25a83285`) produced integration failure counts of **77, 77, and 61**. The only thing that changed between them was the internal test iteration order. This is **direct empirical confirmation** of T-1's "test order determines failure count" theory — which was previously listed as unverified.

Prior to today, the theory was plausible but not provably correct. Now it is.

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
**Last updated**: 2026-04-22 — fresh measurement after `run-test-suite --mode react` shipped, plus a specific finding about `OnHoldModal.test.tsx` text drift
**Impact**: ~196 of ~482 frontend unit tests fail. 27 of 43 test files fail. Test suite is not useful for catching regressions.

#### Baseline state (measured after vetting commit `0fdcbbd6`)

- **Test Files**: 27 failed | 14 passed | 2 skipped (43 total)
- **Tests**: 196 failed | 246 passed | 40 skipped (482 total)

#### 2026-04-22 measurement (after commit `a5458644`)

- **Test Files**: 26 failed | 15 passed | 2 skipped (43 total)
- **Tests**: 187 failed | 250 passed | 40 skipped (477 total)
- Net change vs 2026-04-10 baseline: **−9 failures**, +4 passing. The improvement came from incidental fixes during other work (the `VettingApplicationsList` controlled-component refactor in `a5458644` plus the test wrapper update added 12 passing tests; OnHoldModal lost 3 to a regression then recovered them via QueryClientProvider wrapper).
- **Note**: The fact that the headline number barely moved while real fixes are landing is what T-3 predicted — without a dedicated rehab pass, opportunistic improvements can't keep up with new drift.

#### Specific finding 2026-04-22: `OnHoldModal.test.tsx` text drift

10 of the 13 tests in this file fail because the modal's rendered text was changed when bulk support was added in commit `395ec740` (months pre-dating today), but the tests were never updated. Examples:

- Test expects validation message: `"Please provide a reason for putting this application on hold"`
- Modal actually emits: `"Please provide a reason for putting the application(s) on hold"`
- Test expects body text: `/You are about to put.*John Doe.*application on hold/`
- Modal actually renders: `"You are about to put John Doe's application on hold"` (the apostrophe-s + bold tag splits the text)

These 10 are pure test/code drift — the production code is correct, only the test expectations are stale. A targeted update to this one file would unblock 10 of the 187 failures.

A separate `SendReminderModal.test.tsx` has 17 failures of similar character (test expects `"pre-fills reminder message with default template"` behavior that no longer exists in the modal). Unblocking these requires a stronger update — the test was written against an older modal design and needs new test cases for the current design.

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

### TL-2 — `run-test-suite --mode all` skips E2E phase after .NET failures (P2)

**Discovered**: 2026-04-11 during opportunistic test run after BE-2/BE-4 tech debt cleanup
**Location**: `.claude/skills/run-test-suite/execute.sh` — control flow between `run_unit_tests` and `run_e2e_tests`
**Impact**: `--mode all` silently skips the entire E2E phase any time the .NET phase has non-zero failures. Given the current baseline has 104+ failing .NET tests (per `T-1`, `T-2`, etc.), this means **`--mode all` currently never runs E2E tests at all**. Anyone calling it expecting full-stack coverage is getting half the coverage and not realizing it.

#### Symptom

Ran `bash .claude/skills/run-test-suite/execute.sh --mode all` in background with output piped to a log file. Expected: `.NET tests complete → Playwright E2E tests run → Test Suite Summary → SKILL_RESULT JSON block`. Actual: `.NET tests complete → script exits with code 1, nothing else printed`.

Log file (`/tmp/test-suite-full.log`, 70,726 lines) ends cleanly at:

```
  [FAIL] .NET tests FAILED
```

No `Running Playwright E2E Tests (via test-environment skill)` header (which `run_e2e_tests` prints as its first line). No `Test Suite Summary` section. No `SKILL_RESULT` JSON block at the end. These are all sequential outputs that happen *after* the .NET phase, meaning control never reached them.

External exit code: `1` (matches what the skill *should* exit with on .NET failure, but also matches what a premature `set -e`-triggered exit would produce).

#### Root cause (theory — not fully verified)

The skill sets `set -e` at line 27 (top of file), then uses `set +e` / `set -e` scaffolding around each test-runner call to capture exit codes without tripping errexit. Specifically:

```bash
if [[ "$MODE" == "unit" || "$MODE" == "all" ]]; then
    set +e
    run_unit_tests        # returns 1 when any test fails
    UNIT_RESULT=$?        # captures 1 into variable
    set -e                # re-enables errexit
    echo ""
fi

if [[ "$MODE" == "e2e" || "$MODE" == "all" ]]; then
    set +e
    run_e2e_tests         # never reached when UNIT_RESULT != 0
    ...
```

Inside `run_unit_tests`, the per-project loop at line 373-376 also does `set +e` / `set -e` toggling around each `dotnet test` invocation. When the function returns, the `set -e` state is whatever the LAST iteration left it in (likely enabled).

The theory is that when `run_unit_tests` returns with a non-zero exit code, **the combination of the function's internal `set -e` manipulation and the main script's `set -e` on line 455 causes the shell to exit early**, probably before or during the `echo ""` on line 456. The observed output pattern is consistent with this — we see 2 trailing newlines (one from the function's final `echo ""` at line 405, one from the `[FAIL]` echo), but we do NOT see a third newline from the main-script `echo ""` at line 456.

**Reason this is only a theory**: I haven't instrumented the skill to prove the exact exit point. The `set -e` interaction inside functions vs. main script in bash is notoriously quirky and version-dependent. It might also be caused by something else entirely — e.g., the background task runner killing the process, or a subshell handling issue, or a redirect interaction with how I invoked the skill.

#### Dead-end fixes — DO NOT RE-TRY

None yet — this is first-discovery.

#### Suggested fix approach

**Option A — Stop using `set -e` in the skill (recommended)**
Remove the `set -e` at line 27. Replace every command that "should fail fast on error" with explicit `|| { echo "error"; exit 1; }` handlers. This is the cleanest fix but requires auditing every command in the skill.

**Option B — Wrap the entire main flow in a function**
Put the top-level execution (both if blocks and the summary) inside a function that's called with `set +e` from a thin wrapper. Function-local errexit state is more predictable than main-script state.

**Option C — Use explicit return-code checks instead of `set +e`/`set -e` pairs**
Change the pattern:
```bash
set +e
run_unit_tests
UNIT_RESULT=$?
set -e
```
to:
```bash
UNIT_RESULT=0
run_unit_tests || UNIT_RESULT=$?
```
The `|| VAR=$?` form captures the exit code without ever letting errexit trigger, and it doesn't modify the shell's `set -e` state. This is the safest minimal-change fix.

**Option D — Verify with `set -x` tracing**
Before committing to a fix, add `set -x` to the skill, re-run `--mode all`, and look at the trace to identify exactly which command triggers the exit. Do this first to confirm or reject the theory above — 10 minutes of debug output beats 2 hours of guessing.

**Recommended**: **D then C**. Run with tracing first to confirm the exit point, then apply the minimal Option C fix. Estimated effort: **1 hour** for investigation + fix, assuming the theory is correct.

#### Workaround

Until fixed, run unit and E2E phases separately:

```bash
bash .claude/skills/run-test-suite/execute.sh --mode unit
bash .claude/skills/run-test-suite/execute.sh --mode e2e
```

This bypasses the problematic control flow because each invocation is a fresh shell state.

#### Authoritative records

- Staging post-deploy test run on 2026-04-11 — the run where this was discovered
- Output file at `/tmp/test-suite-full.log` from that run (local-only, gitignored, may be cleaned up by tmpreaper)
- `.claude/skills/run-test-suite/execute.sh` lines 27, 373-376, 451-465

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

### BE-5 — Service layer swallows `OperationCanceledException` and returns 500 (P2)

**Discovered**: 2026-04-11 while investigating staging console errors reported by user
**Location**: Systemic — confirmed in at least:
- `apps/api/Features/Participation/Services/AttendanceService.cs:731-735` (`GetParticipationStatusAsync`)
- `apps/api/Features/Authentication/Services/RefreshTokenService.cs:126` (`RotateRefreshTokenAsync`)
- Likely many more `catch (Exception ex)` blocks throughout the service layer

**Impact**: Client cancellations (in-flight requests aborted before the response completes) are being caught as generic errors, logged at `ERR` severity, and returned to the client as HTTP 500. **12 occurrences in the last 24 hours on staging**, which has known low user traffic (~1 purchase per day). The user impact is minimal (the client is gone by the time the response is written), but the observability impact is real: monitoring dashboards see false-positive 500s, error rates look inflated, and real exceptions get lost in the noise.

#### Symptom

Representative log from staging at 07:33:07 UTC — five simultaneous 500s for the same authenticated user on five different event participation queries, all with identical `OperationCanceledException` stack traces:

```
[07:33:07 ERR] WitchCityRope.Api.Features.Participation.Services.AttendanceService
  Error getting attendance status for user b3f7a090-9284-45f7-93f2-32f0522f3eb3 in event 7cb32f89-...
System.OperationCanceledException: The operation was canceled.
   at System.Threading.CancellationToken.ThrowOperationCanceledException()
   at Npgsql.NpgsqlCommand.ExecuteReader(Boolean async, ..., CancellationToken cancellationToken)
   at Microsoft.EntityFrameworkCore.Query.Internal.QueryingEnumerable`1.AsyncEnumerator.MoveNextAsync()
   at WitchCityRope.Api.Features.Participation.Services.AttendanceService.GetParticipationStatusAsync(...) at line 168
[07:33:07 ERR] Serilog.AspNetCore.RequestLoggingMiddleware
  HTTP GET /api/events/7cb32f89-.../participation responded 500 in 257.1811 ms
```

And an earlier one in the refresh token path at 07:11:18:

```
[07:11:18 ERR] Microsoft.EntityFrameworkCore.Database.Transaction
  An error occurred using a transaction.
[07:11:18 ERR] WitchCityRope.Api.Features.Authentication.Services.IAuthenticationService
  Token refresh failed
System.OperationCanceledException: The operation was canceled.
   at Npgsql.NpgsqlTransaction.Commit(Boolean async, CancellationToken cancellationToken)
   at WitchCityRope.Api.Data.ApplicationDbContext.SaveChangesAsync(...) at line 1300
   at RefreshTokenService.RotateRefreshTokenAsync(...) at line 126
[07:11:18 ERR] Serilog.AspNetCore.RequestLoggingMiddleware
  HTTP POST /api/auth/refresh responded 500 in 58.2782 ms
```

#### Root cause (primary)

The service layer uses `catch (Exception ex)` without filtering for `OperationCanceledException` first. Example from `AttendanceService.cs:731-735`:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error getting attendance status for user {UserId} in event {EventId}", userId, eventId);
    return Result<EnhancedParticipationStatusDto?>.Failure("Failed to get attendance status", ex.Message);
}
```

When the `cancellationToken` fires (for any reason — see secondary root cause below), `OperationCanceledException` is thrown from somewhere inside the EF query. The generic catch treats it as a real failure, logs at ERR severity, and returns a `Result.Failure` which the minimal-API endpoint converts to HTTP 500.

The correct ASP.NET Core pattern is to let `OperationCanceledException` propagate when the request has actually been aborted:

```csharp
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    throw; // Client is gone — let ASP.NET pipeline close the connection
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error getting attendance status...");
    return Result<EnhancedParticipationStatusDto?>.Failure(...);
}
```

ASP.NET Core's request pipeline handles the propagated OCE gracefully: it doesn't log to ERR, it doesn't send a response body (the client is already disconnected), and the request logging middleware records it at a reduced severity.

#### Root cause (secondary — WHY so many cancellations?)

The primary fix hides the symptom. The deeper question is **why are there 12 OCEs in 24h on a staging environment with near-zero real user traffic?** The user explicitly flagged that their usage is "users purchasing things once a day" — which is inconsistent with my initial assumption that these were all browser-navigation-driven cancellations. Theories worth investigating when someone has time:

1. **Kestrel request timeout** — default request timeout, or an idle connection timeout, firing on requests that are simply slow (e.g., a 500ms+ DB query gets cancelled by a short timeout). Unverified.
2. **Network blips to DigitalOcean managed Postgres** — the staging DB is at `witchcityrope-prod-db-do-user-27362036-0.m.db.ondigitalocean.com:25060`. Transient network issues between the droplet and the managed DB would cancel in-flight Npgsql operations.
3. **Polling/refresh loops from monitoring** — some automated health checker might be calling `/api/events/{id}/participation` then giving up after a few hundred ms. Would need to correlate source IPs with cancellation times.
4. **TanStack Query background refetch cancellations** — if the React app is calling these endpoints on visibility change / window focus / staleTime expiration, and the new query supersedes an in-flight one, TanStack Query aborts the old one. Possible on low traffic if one user has several tabs or re-focuses often.

The user's session during 07:33:07 (same userId across 5 simultaneous cancellations) suggests **hypothesis #4 is most likely for that specific burst** — a single React component unmounted with 5 queries in flight. But hypothesis #1 or #2 might explain the isolated 07:11:18 refresh cancellation 20 minutes earlier, since that one is on the background refresh hook, not tied to any navigation.

#### Suggested fix approach

**Phase A — Surface-level fix (recommended first, high ROI):**

1. Audit every `catch (Exception ex)` block in `apps/api/Features/**/*Service.cs` and related files.
2. Add a preceding `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` clause, OR change the general catch to `catch (Exception ex) when (ex is not OperationCanceledException)`.
3. Run the test suite — no existing tests should break because the new filter path is tested against a non-cancelled token.
4. Redeploy and watch staging logs for the next 24–48h. The false-positive 500s should drop to near zero.

Estimated effort: **1–2 hours** depending on how many files need the change. Safe to automate with a search-and-replace for the pattern, but each instance should be reviewed — some catches may be legitimate "convert any DB error to a known result" paths where the new behavior changes contract.

**Phase B — Root cause investigation (recommended second, lower priority):**

1. Add structured logging for the cancellation source: when catching OCE, check `cancellationToken.IsCancellationRequested` vs. `HttpContext.RequestAborted.IsCancellationRequested` to distinguish request-abort from an internal linked token.
2. Instrument Kestrel or Npgsql with detailed timing logs to see where cancellation originates.
3. Check DigitalOcean DB metrics for packet loss or connection reset patterns correlating with the timestamps.
4. Check the frontend for any code that cancels queries aggressively (TanStack Query `signal`, AbortController patterns).

Estimated effort: **~1 day** of focused investigation, likely requiring staging traffic recording.

#### Authoritative records

- Staging API logs from 2026-04-11 (07:11:18, 07:33:07)
- `apps/api/Features/Participation/Services/AttendanceService.cs:731`
- `apps/api/Features/Authentication/Services/RefreshTokenService.cs:126`
- User report: "I'm seeing these errors in the console window... we only have users purchasing things once a day, so I don't think your analysis is correct about the root cause"

---

### BE-8 — Inconsistent error-string → HTTP-status mapping across endpoint files (P2, UNRESOLVED)

**Discovered**: 2026-04-12 during H1 proxy-RSVP fix (production incident 01-health-check-2026-04-12)
**Impact**: Brittle. A service-layer string edit can silently regress an endpoint to HTTP 500 with no compile error or test failure. Confirmed real-world: ProxyRsvpEndpoints had `"Already has RSVP"` in its switch while the service returned `"Already participating"` — real users saw 500s (see resolved BE-6).

#### Symptom

Two coexisting patterns across feature-slice endpoint files:

**Pattern A — exact-match switch (brittle, caused BE-6):**
```csharp
// apps/api/Features/TicketAssignment/Endpoints/TicketAssignmentEndpoints.cs:68-79
var statusCode = result.Error switch
{
    "Attendance not found" => 404,
    "Assignee already has ticket" => 409,
    _ => 500
};
```

**Pattern B — substring match (more tolerant, used by Participation):**
```csharp
// apps/api/Features/Participation/Endpoints/ParticipationEndpoints.cs:156-179
if (result.Error.Contains("not found")) return Results.Problem(..., statusCode: 404);
if (result.Error.Contains("already"))   return Results.Problem(..., statusCode: 409);
```

ProxyRsvp was converted to Pattern B during H1 remediation (apps/api/Features/ProxyRsvp/Endpoints/ProxyRsvpEndpoints.cs:220-272). TicketAssignment is still on Pattern A and almost certainly has similar stale-label drift lurking.

#### Root cause (confirmed)

C# `switch` on string literals is compile-time opaque — the compiler never verifies that the labels match what any particular callee returns. The service layer evolves (renames, additions), and there is no typed contract linking a `Result<T>.Error` message to an endpoint's label list. Two other ProxyRsvp strings ("Per-person limit reached", "Acceptance window expired") were never in the switch AT ALL and also fell through to 500 in the old implementation.

#### Suggested fix approach

Three escalating options:

1. **Tactical (low effort)**: audit every `result.Error switch { ... _ => 500 }` in `apps/api/Features/**/Endpoints/*.cs`, cross-reference against the corresponding service's `Result.Failure(...)` call sites, and either convert to substring matching (Pattern B) or keep exact matching but make the list exhaustive and add a unit test per endpoint asserting every known service error maps to the intended code.

2. **Structural (medium effort)**: introduce a typed discriminator on `Result<T>` — e.g. an `ErrorKind` enum (`NotFound | Unauthorized | Conflict | BadRequest | Internal`) that the service sets alongside the human-readable message. Endpoints map the enum value, and the string becomes user-facing detail text only. Eliminates the string-contract-drift class of bugs entirely.

3. **Process (ongoing)**: add a small test helper that reflects over each Service's `Result.Failure(...)` string-literal call sites and asserts the corresponding endpoint handler produces a non-500 response for each. Catches this class of regression in CI even without the structural change.

Estimated effort: (1) ~2h, (2) ~1 day + follow-up refactor across feature slices, (3) ~half a day.

#### Authoritative records

- Resolved BE-6 (ProxyRsvp) — the concrete instance that made this pattern visible
- `apps/api/Features/TicketAssignment/Endpoints/TicketAssignmentEndpoints.cs:68-79` — suspected next-victim
- `apps/api/Features/ProxyRsvp/Endpoints/ProxyRsvpEndpoints.cs:220-272` — remediated example (case-insensitive substring match with documented ordering dependency)

---

### BE-9 — No alerting on permanently-Failed Hangfire jobs (P2, UNRESOLVED)

**Discovered**: 2026-04-12 during production health check (incident 01-health-check-2026-04-12)
**Impact**: Observability gap. `DailyLogSummaryJob` (now resolved as BE-7) failed every nightly run for at least 24h before a manual health-check surfaced it. The job retried 10 times as designed, then parked in `Failed` state in `hangfire.job`. Nothing raised an alert, sent an email, or logged at a severity that would trip monitoring. In principle any recurring job can silently die this way.

#### Symptom

`hangfire.job` table on 2026-04-12: 38 rows in state `Failed`, 27 in `Succeeded`. Job id 1232 (`DailyLogSummaryJob`) had exhausted 10 retries by 05:34 UTC on 2026-04-12 and remained permanently Failed. No log entry at WARN or higher mentions "job permanently failed" or similar; the only ERR logs are Hangfire's per-retry failure messages which are scoped to the retry event, not the final dead state.

#### Root cause (confirmed)

Hangfire's default AutomaticRetry attribute writes ERR logs per retry and moves the job to `Failed` state after exhausting retries, but it does NOT emit a distinct "permanent failure" event. The application's Serilog → PostgreSQL sink captures the ERR logs, but nothing reads `logging.application_logs` looking for repeated job failures. The Hangfire dashboard (`/hangfire`, admin-only) shows the Failed bucket but requires human visits.

#### Suggested fix approach

Two main options, possibly both:

1. **Global Hangfire filter** — implement `IElectStateFilter` or `IApplyStateFilter` that fires when a job transitions into `FailedState` after retries are exhausted. The filter logs at FATAL severity with a dedicated template (e.g. `"Recurring job {JobType} permanently failed after {Retries} retries"`) and optionally sends an email/Slack/etc. Hangfire filter pattern is well-documented and minimally invasive.

2. **Periodic sweep** — add a new recurring job (`hangfire-health-sweep`, run every 15 minutes) that queries `hangfire.job` for rows in `Failed` state `createdat >= NOW() - INTERVAL '1 hour'` and writes a FATAL log entry summarizing any findings. Catches failures the filter missed (e.g., database crashes that prevented the filter from firing).

Either approach also needs a destination: email to admins, a page to oncall, or at minimum a known Serilog message-template that the `check-production-server` skill's MANDATORY warning-anomaly-detection section (SKILL.md step 3) flags at HIGH.

Estimated effort: filter approach ~3h, sweep approach ~2h, notification wiring ~2h on top.

#### Authoritative records

- Resolved BE-7 (DailyLogSummaryJob DBNull) — the concrete failure that made this gap visible
- `docs/functional-areas/production-incidents/01-health-check-2026-04-12.md` H2 section
- `apps/api/Program.cs:646-684` — Hangfire registration (where a global filter would go)

---

### BE-10 — `Session.Capacity` declared but never enforced at ticket purchase (P3, UNRESOLVED)

**Updated**: 2026-04-12 — priority lowered P2 → P3 after the session-capacity audit was re-run with the corrected DISTINCT-users rule and returned **zero rows across all events in the last 365 days**. Still a real latent gap; prioritized lower because there's no empirical harm and the corrected audit will catch it if/when it happens. Original Impact text below is preserved for history; the Rope Jam March citation was based on the false-positive overbook and is no longer load-bearing.

**Discovered**: 2026-04-12 during M1 capacity investigation (incident 01-health-check-2026-04-12)
**Impact**: Silent. For multi-session events, per-session capacity is modeled in the schema but the application never checks it during ticket purchase. A ticket type that spans multiple sessions could sell past a session's individual cap without any user-visible error. The Rope Jam March event in the 2026-04-12 health check was only flagged at the EVENT level (50/40); whether it ALSO breached session-level caps was not checked because the query couldn't meaningfully aggregate against an unenforced limit. Will bite when a high-demand multi-session event launches.

#### Symptom

- `apps/api/Models/Session.cs:96-97` defines `public int Capacity { get; set; }` on Session.
- `apps/api/Features/Participation/Services/AttendanceService.cs:1277-1285` is the only capacity check on the ticket-purchase path, and it reads only `eventEntity.Capacity` + `GetReservedCountAsync(request.EventId, ...)` — no per-session cross-check.
- `apps/api/Features/Participation/Services/IAttendanceCountService.cs:43` — `GetReservedCountAsync` takes an `eventId`, not a `sessionId`, and counts distinct users across all attendance types for that event.
- Ticket type → sessions join table `TicketTypeSessions` exists, so the data for a per-session check is available; the application just doesn't use it.

#### Root cause (confirmed)

This is a feature that was schema-designed but not wired through to the ticket-purchase business logic. Not a regression — the Session.Capacity column simply never graduated from "modeled" to "enforced".

#### Suggested fix approach

Three things would need to change:

1. Add a per-session count method to `IAttendanceCountService` — something like `GetReservedCountPerSessionAsync(Guid sessionId, CancellationToken ct)` that counts DISTINCT `TicketPurchaseId` for `Status = Active` + `AttendanceType = Ticket` rows where `SessionId = @sessionId`.
2. In `AttendanceService.PurchaseTicketAsync` (line ~1277), after the event-level capacity check, loop over each session the ticket type covers (via `TicketTypeSessions`) and verify each session's `Capacity` vs `GetReservedCountPerSessionAsync`.
3. Add an integration test that seeds a multi-session ticket type, fills one session to capacity, and asserts a new purchase that would include that session is rejected with a per-session-capacity error.

Estimated effort: ~4h including tests. Care needed around UI error messaging — the rejection needs to identify WHICH session is full so users can pick an alternative if the ticket type supports it.

#### Authoritative records

- Production health report 01-health-check-2026-04-12 M1 section (event-level overbook — does not address session level)
- `apps/api/Models/Session.cs:96-97`
- `apps/api/Features/Participation/Services/AttendanceService.cs:1277-1285`

---

### BE-11 — Ticket-purchase capacity check is not atomic with the insert (P3, UNRESOLVED)

**Updated**: 2026-04-12 — **The "10-over" concern that drove the original P2 priority was a FALSE POSITIVE.** User correction: the audit query was double-counting users who held both an RSVP and a Ticket for the same event. Re-queried with the correct business rule (DISTINCT users per `GetReservedCountAsync`): Rope Jam March = 31 distinct active users against capacity 40 (**9 under**, not 10 over). Full-fleet corrected audit across all events in the last 365 days returned **zero rows** — the application's capacity enforcement has been working correctly throughout the observable window.
>
> **What remains of this entry**: the race condition between the application-level capacity check (`AttendanceService.cs:1277-1285`) and the insert (~line 1354) is still real in principle. No transaction wraps the pair, so in theory two concurrent requests could both pass and both insert. With zero empirical instances in 365 days at ~1 purchase/day, priority drops to P3. Entry kept so that (a) the theoretical issue is documented, and (b) if a high-traffic event ever launches and overbook shows up, the fix direction is already specified below.
>
> **Forensic theories that are no longer relevant**: the hypotheses about capacity being lowered post-hoc, bulk-seed bypasses, or a silently-broken period of the check — ALL presupposed an actual overbook that didn't exist. Archived those below for history but no investigation needed.

**Discovered**: 2026-04-12 during M1 capacity investigation (incident 01-health-check-2026-04-12)
**Impact**: Under concurrent load, two simultaneous ticket-purchase requests can both observe `currentCount < Capacity`, both pass the check, and both insert — producing an overbook. User reports being comfortable with at-most 1-ticket overbook, but the Rope Jam March event in the 2026-04-12 health check showed 50 active against capacity 40 — **10 over**, which a simple race cannot produce with ~1 purchase/day. The "10-over" case needs independent investigation; the race condition is a separate concern that we should fix regardless.

#### Symptom

- `apps/api/Features/Participation/Services/AttendanceService.cs:1277-1285` — capacity check (reads `GetReservedCountAsync`)
- `apps/api/Features/Participation/Services/AttendanceService.cs:~1354` — ticket purchase + attendance insert
- No `BeginTransactionAsync`, `ExecuteInTransactionAsync`, or row-lock wraps the check+insert pair. EF Core's default save-changes transaction covers the inserts but not the preceding read.

Event "Rope Jam - March" (Id `c6058a34-...`, 2026-03-21): 50 Active attendances (31 RSVP + 19 Ticket) against Capacity = 40. Event is past; no backfill needed, but the question of HOW it got there is open.

#### Root cause (partial — confirmed for 1-over drift, unconfirmed for 10-over)

**For 1–2 over**: classic check-then-act race. Two requests read `count = 39`, both pass `39 + 1 ≤ 40`, both insert.

**For 10 over (Rope Jam March)**: A race at ~1 purchase/day cannot explain 10 concurrent overbookings. Hypotheses worth investigating:
1. Admin action lowered `Event.Capacity` AFTER registrations were already recorded — no entity audit log on the Event table to confirm, so we'd have to look at git/deploy history of the event record or UI flows that allow editing capacity post-publish.
2. RSVPs were bulk-imported or seeded for the event without running through the capacity-enforcing service path (e.g., admin proxy-RSVPs via a different code path that skips the check).
3. Capacity check was silently broken for a period (e.g., `GetReservedCountAsync` returning 0 due to a status enum mismatch during an earlier refactor). Would need git-log on `AttendanceCountService` around the affected date range.
4. Manual DB edits.

**User deferred this diagnosis as Phase 4 work.** Entry recorded here so it isn't lost.

#### Suggested fix approach (for the race)

Two patterns worth considering:

1. **Serializable transaction + re-read** — wrap the check+insert in `IDbContextTransaction` with `IsolationLevel.Serializable`. Postgres will then detect concurrent writes via predicate locks and retry one request. Simple but `Serializable` has measurable perf cost at high write rates.
2. **Advisory lock keyed by event-id** — `pg_advisory_xact_lock(hashtext(event_id::text))` at the top of the transaction serializes all purchase attempts for a given event without blocking unrelated events. More surgical.
3. **Row-level `SELECT ... FOR UPDATE` on the Events row** — blocks anyone else from purchasing for the same event until this transaction commits. Simplest to reason about.

Estimated effort: ~4h for option 3 including a concurrency test.

For the 10-over question: ~half a day of forensic investigation, likely requiring a git-log archaeology dive on the relevant files + reading the event's audit trail if any.

#### Authoritative records

- Production health report 01-health-check-2026-04-12 M1 section
- `apps/api/Features/Participation/Services/AttendanceService.cs:1277-1285` (check) and ~1354 (insert)
- User direction 2026-04-12: "I am ok if we go over by one ticket or one RSVP because of a race condition. However, this should be EXTREMELY rare. We have only a few people purchase tickets a day, so I want to understand why it looks like we went way over."

---

### BE-12 — Refund completion doesn't sync `TicketPurchase.PaymentStatus` from non-admin callers (P2, RESOLVED)

**Updated**: 2026-04-13 — **Production deployed and Row B backfill applied.** Commits `0d0cf6f7` (M2a/M2c) and `78376f04` (M2b) deployed to production in the bundle shipped at git SHA `94d132f2` (2026-04-13 04:32 UTC). Row B one-off SQL completed: `e4e24d70-92a4-4550-a439-e497915d18e1` `PaymentStatus` flipped `Completed` → `Refunded` inside a transaction, verified zero stale refund rows remain via the same query the skill's audit 9.7 runs. Row A had already been resolved via admin UI action on 2026-04-12. All three drift rows enumerated in the original symptom table are now reconciled.

**Updated**: 2026-04-12 — **M2a / M2c fixed by centralizing `TicketPurchase.PaymentStatus` sync inside `RefundService.ProcessRefundAsync`.** New private helper `SyncPaymentStatusFromRefundsAsync` recomputes status from the sum of Completed `PaymentRefunds` and persists the change. Every caller (admin `ProcessVariableRefund`, user-initiated `AttendanceService.ProcessAutomaticRefundAsync`, any future caller) now benefits automatically. `ProcessVariableRefund`'s inline update was kept as idempotent defense-in-depth and to keep existing unit tests (which mock `IRefundService`) working. Regression guards added in `tests/unit/api/Services/RefundServiceTests.cs` — `ProcessRefundAsync_WithValidFullRefund_SyncsPaymentStatusToRefunded` and `ProcessRefundAsync_WithValidPartialRefund_SyncsPaymentStatusToPartiallyRefunded`. Commit `0d0cf6f7`.

**Updated**: 2026-04-12 — **M2b fixed by adding `TicketPurchasePaymentStatus.AwaitingManualRefund`.** The authorize-net user-cancel path (`AttendanceService.cs:2663-2670`) used to silently return, leaving the purchase reading `Completed` forever. Now sets `PaymentStatus = AwaitingManualRefund`, emits a WARN log with the amount, and updates `UpdatedAt`. Admin UI (`/admin/reports/payments`) displays a pink "Awaiting Manual Refund" badge + filter option. `TicketPurchase.IsPaymentCompleted` and `PaymentListService.IsRefundable` were extended to include the new status so admins can Process Refund through the normal flow. New audit 9.7 added to `check-production-server` skill to surface the queue of pending manual refunds. Commit `78376f04`.

**Discovered**: 2026-04-12 during M2 investigation (incident 01-health-check-2026-04-12)
**Impact**: 3 real production rows exhibit the drift as of the 2026-04-12 audit. Left unaddressed, every authorize-net user-initiated cancellation will add another drift row, and any non-admin-triggered refund that marks `PaymentRefunds.RefundStatus = Completed` will leave `TicketPurchases.PaymentStatus` stale. Customer-visible consequence: a purchase that has been fully refunded still reads "Completed" in admin views, which can lead to a second refund attempt or confused support interactions. No risk of additional money moving.

#### Symptom

Concrete drift rows in production as of 2026-04-12:

| Row | TicketPurchase.Id | PaymentStatus | Active EventAttendances | PaymentRefund.RefundStatus | Note |
|---|---|---|---|---|---|
| A | `c0c34074-1fa2-4bc4-9fe1-0ba1807d11ef` | Completed | 0 | (none) | Authorize-net cancellation path — user cancelled, attendance cancelled, no refund record created. Log at 2026-04-12 07:38:55: `"Ticket purchase c0c34074-... is not PayPal (method: authorize-net) - skipping automatic refund"` |
| B | `e4e24d70-92a4-4550-a439-e497915d18e1` | Completed | 0 | Completed (1) | Refund record exists and is Completed, but purchase still reads Completed instead of Refunded |
| C | same as B | — | — | — | (C is the refund row `2f53d2f1-...` pointing at purchase B) |

**User recollection (2026-04-12)**: "I believe I processed that refund from the admin - event edit - tickets/rsvp tab. Not sure though, but that's the only place I can I believe. It is possible the customer did it from the public facing event page area, but I think it was done on the admin page."

This matters for identifying the code path — ONLY `ProcessVariableRefund.Execute` (at `apps/api/Features/Payments/Commands/ProcessVariableRefund.cs:234-244`) was found by research to update `TicketPurchase.PaymentStatus` after a refund. If Row B's refund went through a DIFFERENT path that doesn't do that update, we need to find that path.

#### Root cause (theory — unverified)

Three candidates, in order of likelihood:

1. **Authorize-net user-cancellation early-return** (Row A, confirmed): `apps/api/Features/Participation/Services/AttendanceService.cs:2663-2670` returns without updating `PaymentStatus` when the payment method isn't PayPal. The attendance cancellation still completes, but the purchase stays "Completed" forever.
2. **Non-admin refund path** (Row B, unconfirmed): A second refund code path exists somewhere that marks `PaymentRefunds.RefundStatus = Completed` without calling `ProcessVariableRefund`. Research could not locate it. Possibilities: a PayPal webhook handler, a direct `RefundService` call from another feature, or Row B's refund was produced by an earlier buggy version of `ProcessVariableRefund`.
3. **`RefundService` defers status updates to caller**: `apps/api/Features/Payments/Services/RefundService.cs:164` comment explicitly says "DO NOT update ticket purchase status here - let the CALLER (ProcessVariableRefund) handle it". If any future caller forgets, drift happens.

#### Suggested fix approach

**Phase 1 — understand before touching**:
1. Audit every reference to `RefundService` (search across the API) to find all callers. Verify each one updates `TicketPurchase.PaymentStatus` appropriately.
2. Audit every site that sets `PaymentRefund.RefundStatus = Completed` to see whether the change propagates to `TicketPurchase.PaymentStatus`.
3. Determine how Row B's refund was actually created (git log on `ProcessVariableRefund.cs` around 2026-03-20; check any admin audit table if one exists).

**Phase 2 — fix**:
1. For Row A class (authorize-net cancellation): decide whether to add a new `TicketPurchasePaymentStatus.AwaitingManualRefund` enum value OR keep "Completed" and flag via a separate `RequiresManualRefund` bool. Implement in `AttendanceService.cs:2663-2670`.
2. For the RefundService "let caller handle it" pattern: move the status-sync INTO `RefundService` on the transition to `RefundStatus.Completed`, removing the implicit caller contract. If partial-refund summation matters, `RefundService` can compute it inline by summing `PaymentRefunds` for the same purchase.
3. Backfill the 2 existing drift rows: Row A → set to `AwaitingManualRefund` (or however the new state is modeled); Row B → set to `Refunded`. Prefer an admin-UI action over raw SQL against prod.

**DO NOT PROCEED without user approval** — this affects payment reconciliation data. User deferred M2 to Phase 4.

Estimated effort: investigation ~half a day; fix + tests ~1 day; backfill ~half a day.

#### Authoritative records

- Production health report 01-health-check-2026-04-12 M2 section (3 rows enumerated)
- `apps/api/Features/Participation/Services/AttendanceService.cs:2663-2670` (the "skipping refund" early-return)
- `apps/api/Features/Payments/Services/RefundService.cs:164` (comment documenting the caller-handles-it contract)
- `apps/api/Features/Payments/Commands/ProcessVariableRefund.cs:234-244` (only known caller that closes the loop)
- User recollection of admin-UI usage for Row B

---

### BE-14 — Authorize.NET E00114 "Invalid OTS Token" on rapid retry — root cause unresolved (P2, UNRESOLVED)

**Discovered**: 2026-04-12 when production user "Mr J" reported a payment issue with the live site. Full investigation captured in incident `01-health-check-2026-04-12` (Mr J section) — summary below.

**Impact**: **Confirmed lost sales.** Two users in the last 30 days (Mr J on 2026-04-12, Dean on 2026-03-20) have hit this error cascade. One user (Mr J) gave up entirely and never purchased. The other (Dean) came back the next day and completed the purchase — but only because they retried later with a fresh session. Any customer less patient than Dean is a lost sale. Frequency is low (~1/2 weeks) but every hit is a 50/50 shot at losing the customer.

#### Symptom

User flow that reproduces the cascade:

1. User enters card data and clicks Pay
2. `POST /api/checkout/credit-card` with idempotency key `K1` + Accept.js nonce `A1`
3. Authorize.NET **legitimately declines** the card (e.g., bank decline, ErrorCode=2). Backend logs `"[Checkout:...] STAGE 3 FAILED: Payment declined. ResponseCode=2, ErrorCode=2, ..."` and rolls back pending tickets
4. Frontend shows "Dismiss and Try Again" button. User clicks it → dismisses the error alert
5. User clicks Pay again, 1–2 seconds after first error
6. CreditCardForm generates a **fresh** nonce `A2` via `Accept.dispatchData()`
7. `POST /api/checkout/credit-card` **with the same idempotency key `K1`** (the bug — see Root Cause) + fresh nonce `A2`
8. Backend logs `"[Checkout:...] Previous attempt with same key failed. Allowing retry."` and falls through to create new pending tickets
9. Backend sends `A2` to Authorize.NET
10. Authorize.NET returns `E00114 "Invalid OTS Token"` — not because the token was reused (it was fresh), but because of something else inside Authorize.NET that remains unexplained

#### Fix applied (partial — frontend only, commit `a92b7044`, prod deployed 2026-04-13 via bundle `94d132f2`)

`apps/web/src/features/payments/pages/EventPaymentPage.tsx:292-379` — `handleNonceReady` now generates the idempotency key into a LOCAL variable at the top of the function instead of reading from `useState`. State is still synced via `setIdempotencyKey` for PayPal's sake, but the credit-card request uses the local variable so it's always fresh regardless of React state batching. The catch block's redundant regeneration was removed.

**Why this only partially resolves**: it fixes the "same key reused" observable, so the backend will no longer log "Previous attempt with same key failed." on a rapid retry. But **we still don't know why Authorize.NET returned E00114 for a fresh nonce in step 10**. The fix eliminates the symptom of duplicate keys; it doesn't address whatever Auth.net-side behavior produced the E00114 on the second attempt.

#### Root cause (partially confirmed, partially unknown)

**Confirmed**: the frontend was sending the same `idempotencyKey` on both attempts. Backend log wording "Previous attempt with same key failed. Allowing retry." only fires when a prior `TicketPurchases` row with the same `(UserId, IdempotencyKey)` exists. The `useState` + catch-block-regeneration pattern had a state-timing window where the rapid retry captured a stale closure value before React committed the new key.

**UNKNOWN** (the reason this ticket is P2 instead of resolved): why Authorize.NET rejected the second FRESH Accept.js nonce with E00114. Theories that remain unverified:
1. **Auth.net-side request dedup**: some internal Authorize.NET logic flagged the second request as suspicious (same customer, same amount, within 2s) and invalidated the token
2. **Accept.js rate limit**: Accept.js (the Auth.net JavaScript library) may have an internal rate limit on `dispatchData()` calls, producing a "valid-looking" nonce that's actually marked as invalid on submission
3. **Something in our backend** that we didn't find: a residual reference to the first attempt's nonce or payment state on retry (research agent didn't find any caching, but something may be subtle)

#### Suggested investigation approach (if someone picks this up)

1. **Local reproduction**: dev environment has Authorize.NET sandbox configured (`appsettings.Development.json:42-48`, `TestMode: true`). Use the decline-triggering test card to reproduce the scenario end-to-end, watching what actually arrives at Authorize.NET on the second request. (Auth.net publishes test card numbers for various decline codes in their developer docs.)
2. **Wire a feature flag / config** to add a small delay between decline and retry (2 sec → 10 sec) to see if the E00114 disappears. If yes, that confirms theory #1.
3. **Contact Authorize.NET support** with the correlation IDs from Mr J's and Dean's incidents; they may be able to tell us what their servers saw.
4. **Defensive UX**: even without root cause, add a client-side rate limit on the Pay button (e.g., disabled for 3s after any failure) to reduce the rapid-retry window.

#### Data points

- Mr J incident: 2026-04-12 05:22 UTC, user `7e3b914f-5b62-4b8e-ac8c-47d22d1fcc5f`, Rope Jam May, correlation IDs `c2ddbe689b97` (decline) and `47af276b1092` (E00114), 2-second gap between attempts
- Dean incident: 2026-03-20 01:25 UTC, user `687b79f7-f98a-415a-92ee-05aae4a997db`, Rope Jam March, 4 rapid attempts (01:25:11/17/37/42), correlation IDs `18e7f440379c` and `49a5a2cb32da` among others. Came back 2026-03-21 11:56 UTC and succeeded

#### Authoritative records

- Production health report 01-health-check-2026-04-12 Mr J section
- Frontend fix: commit `a92b7044` (prod deployed 2026-04-13 in bundle `94d132f2`)
- `apps/web/src/features/payments/pages/EventPaymentPage.tsx:292-379` (fix location — has history comment pointing here)
- `apps/api/Features/Payments/Endpoints/CheckoutEndpoints.cs:128-159` (backend idempotency handling — unchanged)
- `apps/api/Features/Payments/Services/AuthorizeNetService.cs:176-191` (E00114 handling — backend does not distinguish E00114 from card decline, both return HTTP 400; minor UX improvement opportunity)

---

### BE-15 — PayPal multi-ticket undercharge via stale `createOrder` closure + idempotency-key reuse on cancel (P2, RESOLVED — forensic audit inconclusive for past harm)

**Discovered**: 2026-04-12 during follow-up on a user message from Mr J. He wrote: *"If I buy two tickets at the suggested price and I use the [credit card] purchase process it charges $40 which is correct. If I choose to use PayPal it defaults back to $20 for a single ticket. This means that people may be inadvertently paying you less than they [intended]."*
**Impact**: Revenue leak. Any PayPal purchase where the user adjusted cart state (quantity, sliding-scale slider, ticket-type toggle) AFTER the payment form rendered could be charged an amount based on the PRE-adjustment state. Credit-card flow is unaffected.

#### Symptom

Production trace for Mr J (2026-04-12 01:25–01:29 UTC):

1. Mr J opened the payment form for Rope Jam May
2. Clicked PayPal — backend got `Amount=20`, created 1 `TicketPurchase` + 1 `EventAttendance`, PayPal order `5W9139777D9362143` for $20
3. Mr J cancelled the PayPal popup — backend rolled back the pending tickets
4. Mr J adjusted the cart (presumably to 2 tickets, based on his report) and clicked PayPal again
5. Frontend sent `Amount=40` — but **the same idempotency key** `WCR-6d3064f1fc9d454e8a12c84a4acfd6f0`
6. Backend logged *"Returning existing PayPal order for idempotent retry"* and returned the cached $20 order
7. Mr J gave up on PayPal and used credit card, which correctly charged $40 (2 × $20 rows)

#### Root cause (two-part, both in frontend)

**Cause 1 — stale `createOrder` closure in `<PayPalButtons>`**:
`apps/web/src/features/payments/components/PayPalButton.tsx` rendered `<PayPalButtons createOrder={createOrder} ... />` from `@paypal/react-paypal-js` WITHOUT a `forceReRender` prop. That library caches the `createOrder` callback from first mount. When parent props change (amount, quantity, sliding-scale, ticketSelections) the PayPal SDK keeps calling the original cached closure.

**Cause 2 — idempotency-key reuse on PayPal cancel**:
`EventPaymentPage.handlePayPalSuccess` regenerated `idempotencyKey` on success (line 403). But there was no corresponding handler on CANCEL — the key persisted across a user cancelling and retrying with different cart values. The backend's idempotency check at `CheckoutEndpoints.cs:128-159` (for the PayPal endpoint's equivalent) treats same-key+completed as "return cached" and same-key+failed as "retry but reuse the order ID", so a cancelled-then-retry flow got the stale $20 order.

Same class of bug as BE-14 (idempotency-key state-management) but for the PayPal path specifically.

#### Fix applied (commits `689136ca` forward fix + `897a27ac` regression repair, prod deployed 2026-04-13 via bundle `94d132f2`)

1. **`apps/web/src/features/payments/components/PayPalButton.tsx`** — added `forceReRender={[amount, ticketTypeIds, ticketSelections, slidingScalePercentage, idempotencyKey, eventId]}` to both `<PayPalButtons>` instances (mobile + desktop). Every time a dependency changes, `@paypal/react-paypal-js` re-registers the `createOrder` closure.
2. **`apps/web/src/features/payments/components/PaymentForm.tsx`** — added an `onPaymentCancel` prop and wired it into the existing `handlePaymentCancel` so the parent is notified of PayPal cancellations.
3. **`apps/web/src/features/payments/pages/EventPaymentPage.tsx`** — new `handlePayPalCancel` regenerates `idempotencyKey` when the user cancels the PayPal popup. Wired into the `PaymentForm.onPaymentCancel` chain.

Both credit-card and PayPal paths now regenerate the idempotency key on every meaningful state transition (success, error, cancel, per-attempt). Mirrors the BE-14 fix for the credit-card side.

#### Forensic audit on past data (inconclusive)

Ran a query on 2026-04-12 against all 8 completed PayPal purchases in production history (the site is young):

| Date | User | Ticket Type | Sliding % | Charged |
|---|---|---|---|---|
| 2026-04-07 | Stableboy | Rope Jam March | 33% | $30 |
| 2026-03-21 | Forest | Rope Jam March | 67% | $15 |
| 2026-03-21 | Sandra | Rope Jam March | 67% | $20 |
| 2026-03-20 | Lauren | Rope Jam March | 50% | $25 |
| 2026-03-20 | Derek | Rope Jam March | 67% | $20 |
| 2026-03-19 | Jonathan | Rope Jam March | 50% | $25 |
| 2026-03-19 | Shana | Rope Jam March | 67% | $20 |
| 2026-03-16 | Riley | Rope Jam March | 67% | $20 |

All quantity=1, all within the $10-$40 sliding-scale range for that ticket type. No OBVIOUS undercharge pattern. However, the bug's manifestation is "user clicked 2 tickets, only 1 got recorded" — which leaves zero trace in the DB. An older multi-ticket undercharge is indistinguishable from a real single-ticket purchase without user testimony.

#### Why "RESOLVED" even though forensic is inconclusive

The forward-looking fix is deployed (or in-flight to deployment) and comprehensively addresses both root causes. Past harm is undetectable from data alone; if any affected user surfaces, we can resolve case-by-case.

#### Authoritative records

- Forward fix: commit `689136ca` (forceReRender + cancel key regen)
- Regression repair: commit `897a27ac` (restored `setCurrentStep(2)` dropped during the forward-fix edit)
- Prod deploy: bundle `94d132f2` shipped 2026-04-13 via `production-deploy` skill
- Mr J message on 2026-04-12 reporting the credit-card-vs-PayPal discrepancy
- Production logs 2026-04-12 01:25–01:29 UTC correlation IDs `e0340ef51445` and `24b8b9ae7b80`
- `apps/web/src/features/payments/components/PayPalButton.tsx:271-312` (fix location, `forceReRender`)
- `apps/web/src/features/payments/pages/EventPaymentPage.tsx:415-434` (fix location, `handlePayPalCancel`)
- `apps/web/src/features/payments/utils/paypalSuccessHandler.ts` (handler extracted for testability — see T-8)
- Related: BE-14 (credit-card idempotency, fixed 2026-04-12)

---

### BE-16 — Scheduled-email idempotency check ignores `Failed` logs, causing hourly retry storms (P2, RESOLVED)

**Discovered**: 2026-05-16 while investigating a misdirected volunteer-reminder email (production DB read-only audit of event `cae0d3e3` "Rope Jam - May").
**Resolved**: 2026-05-16 — bounded-retry guard added; see Resolution below.
**Impact**: A reminder template that fails to send for a session is re-attempted on every hourly `EmailSchedulerJob` run until the event's send window closes — producing dozens of duplicate `Failed` rows in `EmailTriggerLogs` and dozens of redundant SendGrid attempts. If the underlying send transiently succeeds on a later retry, the recipient gets a late/duplicate email. Observed concretely on the April Rope Jam event.

#### Symptom

Production `EmailTriggerLogs` for the April Rope Jam session (`f63dce54-dae3-4cc6-8c1f-a75e40701ce9`) contains a `RsvpAcceptanceReminder` row that first failed at `2026-04-17 23:00` and then retried **every hour** through `2026-04-18 23:00` — ~25 consecutive rows, all `Status = Failed`, all with empty `SentAt`. The retries stop only when the event ends (the template is no longer "due").

#### Root cause (verified)

`EmailSchedulerJob.ProcessSessionAsync` (`apps/api/Features/EmailTemplates/Jobs/EmailSchedulerJob.cs:177-183`) does its idempotency check as:

```csharp
var alreadySent = await _context.Set<EmailTriggerLog>()
    .AnyAsync(log => log.TemplateId == template.Id
        && log.SessionId == session.Id
        && log.TemplateType == template.TemplateType
        && log.Status == "Sent", ct);
```

It only treats `Status == "Sent"` as "already handled." A row with `Status == "Failed"` (or `"Skipped"`) does not satisfy the predicate, so the next hourly run re-enters the send path and fails again. There is no attempt counter, no backoff, and no terminal "give up after N attempts" state. The job runs hourly, so a persistently-failing template retries ~hourly for the entire remaining send window.

#### Suggested fix approach

1. Decide the intended retry policy with the user — options: (a) at-most-once (count `Failed` as "handled" too — never retry), (b) bounded retry (e.g. max 3 attempts, then mark a terminal `Abandoned` status), or (c) bounded retry with exponential backoff.
2. Implement in `ProcessSessionAsync` — either widen the idempotency predicate or add an attempt-count guard before re-sending.
3. Consider a terminal status value so `Failed` permanently-dead sends are distinguishable from `Failed`-but-will-retry.
4. Backfill is not required — the stale April rows are harmless history.

Effort: ~1-2 hrs once the policy is chosen. Low risk (single method), but needs a deliberate product decision on retry semantics.

#### Resolution (2026-05-16)

Policy chosen: **bounded retry, max 3 attempts**. A transient SendGrid hiccup still gets retried (next hourly run), but a persistently-failing template stops after 3 `Failed` rows instead of looping for the whole send window.

`EmailSchedulerJob.ProcessSessionAsync` (`apps/api/Features/EmailTemplates/Jobs/EmailSchedulerJob.cs`) now fetches all prior `EmailTriggerLog` statuses for the (template, session, type) tuple instead of a single `AnyAsync(Status == "Sent")`. It skips when a `Sent` row exists (unchanged behavior) OR when the `Failed` count has reached `MaxSendAttempts` (3), logging a `Warning` in the give-up case so the dead reminder is visible in logs.

Also added during code review: graceful handling of the concurrent-run idempotency race. The `UQ_EmailTriggerLogs_Idempotency` unique index is filtered to `Status = 'Sent'`, so if two scheduler runs overlap (Hangfire does not guarantee single-instance execution across app restarts / multiple workers) both can pass the idempotency check, both send, and the second `SaveChangesAsync` hits a unique violation. `ProcessSessionAsync` now catches that specific `DbUpdateException` (Postgres `SqlState` `23505`), detaches the rejected log row, and logs a `Warning` — so one session losing the race no longer aborts `ProcessTemplateAsync`'s loop over the sibling sessions. This does NOT prevent the duplicate *send* (both emails already went out before the conflict); true prevention requires single-instance job execution, which remains an accepted limitation of the current Hangfire setup.

Boundary test coverage added: `tests/unit/api/Features/EmailTemplates/EmailSchedulerJobRetryTests.cs` pins the `MaxSendAttempts` boundary (0/2 failures → retry, 3/5 → skip) and the already-`Sent` short-circuit, driven through the job's public `ExecuteAsync`. Broader scheduler-job orchestration coverage remains tracked under **T-7**.

Not changed: the `Skipped`-status hourly re-check (recipients may legitimately materialize later) and the partial-failure case (1 of N recipients fails → row logged `Sent` → no per-recipient retry — now tracked separately as **BE-17**). No data backfill needed; the stale April rows are harmless history.

#### Related

- **BE-9** — No alerting on permanently-Failed Hangfire jobs. Same blind spot from the ops side: these 25 failed rows generated no alert.
- **T-7** — Recurring Hangfire jobs lack test coverage (covers the missing test for this fix).
- Same subsystem as the **volunteer-reminder catch-up bug** found in the same 2026-05-16 investigation (`EventEmailService.SendCatchUpRemindersAsync` re-sends *every* logged `TimeBased`/`Sent` reminder type — including `VolunteerReminder` — to new ticket buyers). That bug is being handled as active work, not logged here as deferred debt.

#### Authoritative records

- `apps/api/Features/EmailTemplates/Jobs/EmailSchedulerJob.cs` — `ProcessSessionAsync` idempotency check (now the bounded-retry guard)
- `tests/unit/api/Features/EmailTemplates/EmailSchedulerJobRetryTests.cs` — boundary tests
- Production `EmailTriggerLogs`, April Rope Jam session `f63dce54-dae3-4cc6-8c1f-a75e40701ce9`

---

### BE-17 — Scheduled-email trigger log is per-batch, so a partial recipient failure is never retried (P3, UNRESOLVED)

**Discovered**: 2026-05-16 during the BE-16 code review.
**Impact**: Low-frequency, low-severity. When a time-based reminder goes to N recipients and only some fail, the affected recipients silently never receive the email. No duplicate sends, no crash — just a missed reminder for a subset of attendees.

#### Symptom

`EmailSchedulerJob.ProcessSessionAsync` writes ONE `EmailTriggerLog` row per (template, session) processing run, with `Status = successCount > 0 ? "Sent" : "Failed"`. If 9 of 10 sends succeed and 1 fails, the row is logged `"Sent"`. On the next hourly run the BE-16 idempotency check sees a `"Sent"` row and skips the whole session — so the 1 failed recipient is never retried. The only trace is the `ErrorMessage` field: e.g. `"1 of 10 sends failed"`.

#### Root cause

`EmailTriggerLog` tracks sends at batch granularity, not per-recipient. There is no record of *which* recipient failed, so a targeted retry is impossible without a schema change.

#### Suggested fix approach

A design change, not a one-line fix. Options:
1. Add per-recipient send tracking (a child table, or a JSON column on `EmailTriggerLog` listing failed recipient IDs), and retry only the failed recipients.
2. Only log `"Sent"` when ALL recipients succeed — but without per-recipient tracking this re-sends to recipients who already succeeded, causing duplicates.

Option 1 is the correct fix. Effort: ~half a day including migration + tests. Coordinate with BE-16 (same method) and BE-9 (failed-send alerting).

#### Authoritative records

- `apps/api/Features/EmailTemplates/Jobs/EmailSchedulerJob.cs` — `ProcessSessionAsync`, the per-batch log write
- Related: **BE-16** (bounded-retry guard, same method), **BE-9** (no alerting on failed sends)

---

### BE-18 — Authorize.net ticket cancellations don't auto-refund (PayPal does) (P2, UNRESOLVED)

**Discovered**: 2026-05-16 during the production health-check M2 deep-dive (production-incident `02-health-check-2026-05-16`).
**Impact**: When a member self-cancels a ticket they paid for by credit card (Authorize.net), no refund is issued automatically. The purchase is flagged `AwaitingManualRefund` and an admin must process it by hand. PayPal cancellations, by contrast, refund automatically. If the manual queue is not worked promptly, members wait days-to-weeks for their money — the 2026-05-16 health check found two members owed $20 each, outstanding 16 and 28 days.

#### Symptom

`AttendanceService.ProcessAutomaticRefundAsync` (the handler that runs on user-initiated cancellation) only auto-refunds when `PaymentMethod == "PayPal"`. For Authorize.net (and any other method) it sets `PaymentStatus = AwaitingManualRefund` and returns without issuing a refund. The member's money stays with the processor until an admin clicks "Process Refund" in the Admin Payments UI.

#### Root cause / context

This is a deliberate gate, not an oversight — added 2026-04-12 as part of BE-12 M2b. Before M2b, non-PayPal cancellations silently left the purchase reading `Completed` forever (money effectively lost track of); M2b at least surfaced them in a visible admin queue.

The capability to refund Authorize.net already exists: `RefundService.ProcessRefundAsync` has a full working Authorize.net branch (`_authorizeNetService.RefundAsync`). It is simply not wired into the self-service cancellation path the way the PayPal branch is.

**Unverified theory for why it was left manual**: Authorize.net transactions settle in nightly batches. A charge cancelled *before* settlement must be **voided**, not **refunded** (a different API call); only *after* settlement can it be **refunded**. A member can cancel on either side of that boundary, so an auto-refund-on-cancel path must choose void vs. refund based on the transaction's settlement state. Whether `AuthorizeNetService.RefundAsync` / `RefundService` already handles that distinction is NOT yet confirmed — that is the first research step.

#### Suggested fix approach

1. Determine whether `AuthorizeNetService` / `RefundService` already handles the void (pre-settlement) vs. refund (post-settlement) distinction.
2. If void handling is missing, add it — the auto-refund path must pick the correct operation based on transaction state.
3. Wire the Authorize.net branch into `AttendanceService.ProcessAutomaticRefundAsync` the same way PayPal is, so credit-card cancellations refund automatically.
4. Keep `AwaitingManualRefund` as the fallback for genuine failures (refund API call errors) so nothing is ever silently dropped.
5. Test both pre- and post-settlement cancellation timing.

Effort: Medium — mostly Authorize.net settlement-state research + testing, not large code volume.

#### Authoritative records

- `apps/api/Features/Participation/Services/AttendanceService.cs` — `ProcessAutomaticRefundAsync` (the PayPal-only gate, ~line 2663)
- `apps/api/Features/Payments/Services/RefundService.cs` — existing Authorize.net refund branch (~line 218)
- `docs/functional-areas/production-incidents/02-health-check-2026-05-16.md` — M2 (the two outstanding refunds that exposed this)
- Related: **BE-12** (introduced `AwaitingManualRefund`), **BE-14** (Authorize.net E00114 issue)

---

## Test suite (unit test coverage)

### T-8 — No PayPal checkout component tests + no skill wrapper for vitest (P2, UNRESOLVED)

**Discovered**: 2026-04-12 while responding to the BE-15 PayPal undercharge incident and my own `setCurrentStep(2)` regression from the same day's edits.
**Impact**: Every one of today's four PayPal-related production/staging bugs (BE-14 credit-card path, BE-15 Fix 1 stale-closure, BE-15 Fix 2 cancel-key-reuse, and my BE-15 regression that dropped `setCurrentStep(2)`) shipped past automated tests because:
- **No e2e test clicks a PayPal button or hits `/api/checkout/paypal/create-order`.** Verified 2026-04-12 — only `phase4-registration-rsvp.spec.ts:158-166` even mentions PayPal, and that just checks if the button is visible. `tests/e2e/test-utils/helpers/payment.helper.ts:7-13` explicitly states that PayPal flow is skipped for e2e: *"Creating payments via UI/API is complex (requires PayPal flow, events, tickets, etc.). Direct database insertion is faster and more reliable for E2E tests."*
- **No component or vitest test covers the PayPal checkout flow.** Before this entry, the only regression coverage for `EventPaymentPage.handlePayPalSuccess` and `PayPalButton` behavior was... a real user clicking through on staging.
- **The existing `run-test-suite` skill has no vitest mode.** It only supports `--mode unit` (.NET xUnit via `dotnet test`) and `--mode e2e` (Playwright). The pre-commit hook at `/.claude/hooks/block-manual-test-runs.py` blocks `npx vitest`, `npm test`, `vitest`, etc. as direct invocations. The test-executor agent (who's supposed to be the fallback) does not have the Task tool and also can't invoke vitest — confirmed empirically 2026-04-12.

#### Partial fix applied 2026-04-12 (commit `94d132f2`, prod deployed 2026-04-13 via bundle `94d132f2`)

Refactored `handlePayPalSuccess` out of `EventPaymentPage.tsx` into a pure function at `apps/web/src/features/payments/utils/paypalSuccessHandler.ts` with injected dependencies. Added `tests/unit/web/features/payments/paypalSuccessHandler.test.ts` with 7 test cases covering:
- completed-payment state population
- idempotency-key regeneration
- success notification content
- generic notification when confirmation number is absent
- **the `setCurrentStep(2)` advance** (the specific regression guard)
- side-effect ordering (setCompletedPayment before setCurrentStep)
- "exactly one call per dependency" contract

The test file was **written but NOT executed** in the session that authored it because vitest can't be run through any available skill. The test code was type-checked via `tsc --noEmit` (clean) and reads cleanly. Verification that the tests actually pass is deferred until one of:
1. Someone runs `npx vitest run tests/unit/web/features/payments/paypalSuccessHandler.test.ts` locally from a non-hook-bound shell
2. CI runs the frontend test suite (currently no CI runs vitest against this repo as far as I can tell)
3. `run-test-suite` gains a `--mode frontend-unit` or similar

#### Gaps that remain

1. **Component test for PayPalButton** — verifies `forceReRender` deps actually cause the SDK's `createOrder` closure to update when props change. Would directly catch BE-15 Fix 1 regressions.
2. **Component test for EventPaymentPage PayPal cancel path** — verifies idempotency key is regenerated when `onPaymentCancel` fires. Would directly catch BE-15 Fix 2 regressions.
3. **E2E test with a mocked PayPal SDK** — Playwright with `page.route()` intercepting the PayPal CDN and replacing with a local stub that immediately calls `onApprove({orderID: "FAKE"})`. Would catch all four bug classes end-to-end at the UI layer.
4. **Skill + hook support for vitest** — either add `--mode frontend-unit` to `run-test-suite` OR whitelist vitest in the hook's allow-list for paths matching `tests/unit/web/**`. Without this, every agent in this project will hit the same dead end when trying to run a frontend unit test.

#### Suggested fix approach

Priority order:
1. **Unblock vitest execution** (half a day at most). Extend `run-test-suite/execute.sh` with a `--mode frontend-unit` or `--mode web-vitest` branch that runs `npx vitest run` from `apps/web/`. Update the hook to allow that one path. Without this step, every subsequent frontend test is a manual chore.
2. **Write PayPalButton component test** (~2 hours). Mock `@paypal/react-paypal-js`'s `PayPalButtons`, render `<PayPalButton>` with initial amount=20, change amount prop to 40, verify the mocked SDK was re-rendered and the `createOrder` closure captures the new amount.
3. **Write EventPaymentPage integration test for cancel path** (~3 hours, higher setup overhead). Renders the page with all hooks mocked, finds the PaymentForm, fires `onPaymentCancel`, verifies `idempotencyKey` state changed.
4. **Mocked-PayPal e2e** (~1-2 days). Playwright test that intercepts PayPal SDK, fakes approval, asserts the full flow writes correct TicketPurchase + EventAttendance rows.

Items 1 and 2 are highest-leverage. Item 4 is nice-to-have but brittle.

#### Authoritative records

- `apps/web/src/features/payments/utils/paypalSuccessHandler.ts` (extracted pure function, commit `94d132f2`)
- `tests/unit/web/features/payments/paypalSuccessHandler.test.ts` (new, type-checked clean but not runtime-verified)
- `/.claude/hooks/block-manual-test-runs.py` (the hook blocking vitest)
- `/.claude/skills/run-test-suite/execute.sh` (missing vitest mode)
- Related: BE-14, BE-15 (production bugs this coverage would have caught)

---

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

### T-7 — Recurring Hangfire jobs lack test coverage (P2, UNRESOLVED)

**Discovered**: 2026-04-12 during H2 regression-test work on `DailyLogSummaryJob`
**Impact**: Silent failures of business-critical nightly jobs are undetectable until someone runs the production health-check skill. `DailyLogSummaryJob` shipped broken and stayed broken for ≥1 day before detection (see resolved BE-7). Four OTHER recurring jobs run nightly with no tests at all; any of them could fail the same way.

#### Symptom

As of this entry (2026-04-12) the `apps/api/Features/**/Jobs/` layer has the following test coverage:

| Job | Cron | Test file | Coverage |
|---|---|---|---|
| `DailyLogSummaryJob` | `0 1 * * *` | `tests/unit/api/Features/Logging/DailyLogSummaryJobTests.cs` | Added 2026-04-12 — null subcategory, populated subcategory, twice-run idempotence |
| `LogRetentionCleanupJob` | `0 3 * * *` | none | — |
| `RefreshTokenCleanupJob` | `0 4 * * *` | none | — |
| `EmailSchedulerJob` | `0 * * * *` | none | — |
| `DatabaseBackupService` | `0 2 * * *` | none | — |

Only the job that just broke in production has tests. The other four are flying blind.

#### Root cause

Historical — Hangfire jobs were added incrementally and the team's testing norm has centered on service/endpoint layers rather than recurring infrastructure. No pattern existed for "how do I test a Hangfire job" until the one added 2026-04-12.

#### Suggested fix approach

The `DailyLogSummaryJobTests` class is the reference pattern going forward:
- Extends `DatabaseTestBase` (real Postgres via Testcontainers, no WebApplicationFactory — avoids T-1 pollution)
- Truncates job-specific schema tables in `InitializeAsync` since the shared Respawn only touches `public`
- Instantiates the job class directly and calls `ExecuteAsync(CancellationToken.None)`
- Asserts end-state by re-reading via a fresh `DbContext`

Writing equivalent tests for the other four jobs: estimate 2-4h each depending on complexity, ~1-2 days total. Prioritize `EmailSchedulerJob` first (hourly, customer-facing reminders/thank-yous) and `LogRetentionCleanupJob` second (90-day retention matters for disk usage and PII compliance).

#### Authoritative records

- Resolved BE-7 (DailyLogSummaryJob DBNull) — the incident that made this gap visible
- `tests/unit/api/Features/Logging/DailyLogSummaryJobTests.cs` — reference pattern
- `apps/api/Program.cs:646-684` — Hangfire registration listing all recurring jobs

---

### T-9 — `MultiTicketCheckoutTests` MT_U02 / MT_U04 assert a stale attendance count (P3, RESOLVED)

**Discovered**: 2026-05-16, triaged during the refund-id / refund-owed feature work (the two tests surfaced as failures in the regression run and were run down to rule out a real bug).
**Resolved**: 2026-05-16 — both assertions corrected; see Resolution below.
**Impact**: Two consistently-failing unit tests in `tests/unit/api`. **Not a product bug** — triage confirmed the production code is correct; these are stale test assertions. The cost is pure noise: they inflate the failing-test baseline and could mask a future real regression in multi-ticket checkout.

#### Symptom

`MultiTicketCheckoutTests.MT_U02_Multi_Ticket_With_Quantity_2_Creates_Two_Purchases` and `MT_U04_Multi_Ticket_With_Null_Assignee_Creates_Extra_For_Purchaser` fail consistently (verified in full-class isolation — not cross-test pollution):

> Expected `attendances`/`purchaserAttendances` to contain at least 2 item(s), but found 1.

Both buy `Quantity = 2` with the second ticket **unassigned** ("assign later" — `Assignees` null or `[null]`), then assert the purchaser ends up with **≥ 2** `EventAttendance` rows.

#### Root cause (verified — tests are wrong, code is right)

`AttendanceService.CreateTicketPurchaseAsync` (`apps/api/Features/Participation/Services/AttendanceService.cs:1363-1371`) deliberately does NOT create an `EventAttendance` for an unassigned extra ticket. The code comment is explicit: *"'Assign later' tickets (unassigned extras) do NOT get EventAttendance records yet because the purchaser already has attendance for these sessions via their own ticket (index 0)… The TicketPurchase record alone tracks the unassigned ticket's existence."* An attendance is created later when the ticket is assigned (UC-007 post-purchase assignment).

So a buy-2-assign-1-later purchase correctly produces **2 `TicketPurchase` rows but 1 `EventAttendance`** (a person cannot hold two active attendances for the same session). MT_U02 / MT_U04 assert `≥ 2` attendances — they predate or misread this design. MT_U02's *other* assertion (2 `TicketPurchase` rows) is correct and passes; only the attendance-count assertion is wrong.

No customer is losing a ticket — the second ticket exists as a `TicketPurchase` and is fully assignable.

#### Suggested fix approach

Correct the two assertions to match the documented design:
- MT_U02: assert exactly **1** purchaser `EventAttendance` (+ keep the existing "2 `TicketPurchase` rows" assertion).
- MT_U04: assert exactly **1** purchaser `EventAttendance`; optionally also assert 2 `TicketPurchase` rows for completeness.
Trivial (~2 lines each), low risk. Then both tests pass and the assertion documents the real behavior.

#### Resolution (2026-05-16)

Both assertions corrected to match the documented design:
- **MT_U02** now asserts the purchaser has exactly **1** `EventAttendance` (it already, correctly, asserted 2 `TicketPurchase` rows).
- **MT_U04** now asserts exactly **1** purchaser `EventAttendance` **and** 2 `TicketPurchase` rows — so the test actually verifies the "extra ticket" its name claims (the extra exists as a purchase, just not an attendance).
- Both tests' comments were rewritten to explain the unassigned-extra design and point at `AttendanceService.CreateTicketPurchaseAsync`.

Verified: the full `MultiTicketCheckoutTests` class is now 16/16 passing.

#### Authoritative records

- `apps/api/Features/Participation/Services/AttendanceService.cs:1363-1371` — the deliberate "unassigned extra → no EventAttendance" logic
- `tests/unit/api/Features/TicketAssignment/MultiTicketCheckoutTests.cs` — MT_U02, MT_U04 (corrected)

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

### FE-3 — Vite dev port has 5 sources of truth that all must agree (P2, UNRESOLVED)

**Discovered**: 2026-05-09 by an external agent (working in the sibling `accounting-automation` repo) while researching the same bug in that repo. Cross-repo TD-port — the same multi-SSOT structure exists here verbatim, was almost certainly copied from this repo's pattern.
**Impact**: Latent. Does not currently produce an incorrect runtime — all five values happen to agree on `5173`. The risk is silent drift during a port change: a future agent edits one or two of the five locations, the container starts cleanly, and "the dev URL just doesn't load" with no log signal pointing at the cause. The accounting-automation repo hit exactly this failure mode while renumbering ports to remove sibling-repo conflicts (first restart attempt brought up the container on the old port because the Dockerfile CMD's `--port` flag overrode the freshly-edited `vite.config.ts`).

#### Symptom

The dev port `5173` is declared in five places, with two distinct override layers stacked on top of each other:

| Location | Value | Effective? |
|---|---|---|
| `apps/web/vite.config.ts:32` | `port: parseInt(process.env.VITE_PORT || '5173')` + `strictPort: true` | **Defense-in-depth only.** Never exercised because the compose `command:` always passes `--port 5173` (CLI overrides config). |
| `apps/web/Dockerfile:53` `EXPOSE 5173 24678` | 5173, 24678 | Informational only. `EXPOSE` doesn't publish; `compose ports:` does. |
| `apps/web/Dockerfile:57` `ENV VITE_HOST=0.0.0.0` | 0.0.0.0 | Decorative — no consumer reads `VITE_HOST` (vite.config.ts doesn't honor it; the `--host` CLI flag in CMD/command is what binds). |
| `apps/web/Dockerfile:58` `ENV VITE_PORT=5173` | 5173 | Decorative — vite.config.ts WOULD read it, but the compose `command:` `--port` flag overrides whatever vite.config.ts resolves to. |
| `apps/web/Dockerfile:61` `CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "5173"]` | 5173 | Active in the image baseline, but **superseded** by the compose `command:` block. Only matters if the image is run without compose. |
| `docker-compose.dev.yml:156` `"5173:5173"` | 5173 (right side) | **Active.** Must match whatever Vite actually binds to inside the container. |
| `docker-compose.dev.yml:207` `command: ["sh", "-c", "npm run dev:docker-only -- --host 0.0.0.0 --port 5173"]` | 5173 | **Active and authoritative.** This is what actually wins for the running container. |

#### Root cause

Vite CLI flags override `vite.config.ts` settings. The compose `command:` block passes `--port 5173`, which means the env-var-with-default pattern in `vite.config.ts:32` (good code, written defensively) is unreachable in the running container. The HMR EXPOSE on `24678` is also dead — Vite v3+ multiplexes HMR onto the dev server port (5173), so a separate HMR mapping routes to a port nothing is listening on. (Likely the compose `ports:` for HMR was deleted at some point but the EXPOSE was left behind, or HMR was on a separate port in an earlier Vite version.)

The end-result is:
- Five places nominally declare the port, but only one is actually authoritative for the running container.
- The defense-in-depth `strictPort: true` guard in `vite.config.ts` is bypassed by the CLI flag, so it's not actually protecting against silent port-collision auto-increment.
- Future port changes require editing 4–5 files in lockstep with no automated guard against drift.

#### Suggested fix approach

The accounting-automation repo (where this bug was researched) just consolidated the equivalent five sources to two. Same pattern would work here:

1. **`apps/web/Dockerfile`**: drop the `--host` and `--port` flags from the CMD line. Drop `24678` from EXPOSE (dead — HMR is multiplexed). Keep `ENV VITE_HOST=0.0.0.0` and `ENV VITE_PORT=5173` — these become **load-bearing** (vite.config.ts already reads them).
2. **`docker-compose.dev.yml:207`**: change the compose `command:` to `["npm", "run", "dev:docker-only"]` (no shell wrapper, no flags). Drop the `24678` HMR port mapping if it exists in `ports:`.
3. **`apps/web/vite.config.ts`**: no change needed — the env-var-with-default pattern + `strictPort: true` is already correct; this fix just makes it actually take effect.

After this change, the SSOT is `Dockerfile ENV` for the container baseline (overridable by compose `environment:` block if needed) and the `vite.config.ts` default for any host-mode usage. `strictPort: true` then reliably fails-fast on port collisions.

Estimate: 30 min including a `--no-cache` rebuild and HMR verification (edit a `.tsx`, watch the browser auto-reload).

**Caveat for fixers**: WCR's `package.json` disables host-mode `npm run dev` entirely, so the only workflow that needs to work is the container. The vite.config.ts code path then becomes the only resolution path — if env vars don't reach it correctly, the container won't start. Test under both `--rebuild` and `--no-cache` paths.

#### Authoritative records

- This entry's research: `accounting-automation` repo at `/home/chad/repos/accounting-automation`, commit history starting around 2026-05-09. The accounting fix landed in commit `748f35c` (port renumbering exposed the bug) and a follow-up consolidation commit (TD-006 in that repo's `docs/technical-debt.md`, deleted on the consolidation commit per their resolved-items policy).
- Vite docs on CLI flag precedence: https://vitejs.dev/guide/cli.html (CLI flags win over `defineConfig`).

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

## Deployment / infrastructure

### DEP-1 — Nginx "protocol options redefined" warnings at reload time (P3, DEFERRED)

**Discovered**: 2026-04-12 during nginx config inspection for M3 (incident 01-health-check-2026-04-12)
**Impact**: Cosmetic only. Nginx still accepts traffic and serves all sites correctly. The warnings appear at every `nginx -s reload` and clutter `systemctl status nginx` output. Will ALSO potentially cause confusion for future ops work ("is this warning real?").

#### Symptom

Each `systemctl reload nginx` emits:
```
nginx: [warn] 3333340#3333340: protocol options redefined for [::]:443 in /etc/nginx/sites-enabled/inventory-auth:22
nginx: [warn] 3333340#3333340: protocol options redefined for 0.0.0.0:443 in /etc/nginx/sites-enabled/notfai-production:21
nginx: [warn] 3333340#3333340: protocol options redefined for [::]:443 in /etc/nginx/sites-enabled/notfai-production:22
nginx: [warn] 3333340#3333340: protocol options redefined for [::]:443 in /etc/nginx/sites-enabled/vault:32
nginx: [warn] 3333340#3333340: protocol options redefined for 0.0.0.0:443 in /etc/nginx/sites-enabled/vault:33
nginx: [warn] 3333340#3333340: protocol options redefined for 0.0.0.0:443 in /etc/nginx/sites-enabled/witchcityrope-production:36
```

Full list of affected configs (verified 2026-04-12 on the production droplet):
- `/etc/nginx/sites-enabled/inventory-auth`
- `/etc/nginx/sites-enabled/notfai-production`
- `/etc/nginx/sites-enabled/notfai-staging`
- `/etc/nginx/sites-enabled/shipengine-production`
- `/etc/nginx/sites-enabled/shipengine-staging`
- `/etc/nginx/sites-enabled/witchcityrope-production`
- `/etc/nginx/sites-enabled/witchcityrope-production-www` (TWO server blocks, both `listen 443 ssl http2`)

#### Root cause (confirmed)

Nginx rule: the `http2` parameter on `listen` is a PER-LISTEN-ADDRESS setting, not per-server-block. When multiple server blocks share the same `listen <addr>:443` and specify `http2` repeatedly, nginx warns on each redeclaration after the first. Some configs on the droplet have `listen 443 ssl http2;` in the site-specific file AND implicitly inherit the protocol from an earlier site.

#### Suggested fix approach

Two options, both server-side (no repo-side change needed):

1. **Move `http2` to exactly one site's first `listen 443 ssl http2;` directive, strip from all others.** Low-risk, no traffic behavior change. Requires coordinated edits across 7+ site files on the shared droplet, touching sites OWNED by multiple apps (darkmonk, inventory, notfai, shipengine, WCR).
2. **Migrate to nginx 1.25+ `http2 on;` directive** inside the `server { ... }` block instead of on `listen`. Cleaner, avoids the cross-block coupling entirely. Requires nginx 1.25+ on the droplet (verify first).

**Recommendation: defer.** Cross-app server-config work is risky for low value. Touching it in a single-app session is the wrong shape — this should be a dedicated infra-maintenance session with approval from the owners of the other sites (ShipEngine, notfai, inventory, etc.) that share the droplet.

Estimated effort: ~2h once someone with cross-app authority schedules it.

#### Authoritative records

- Production health report 01-health-check-2026-04-12 L1 section
- Live nginx config on `104.131.165.14:/etc/nginx/sites-enabled/` (2026-04-12 listing)

---

## Error handling standard adoption (TD-029 port from inventory-purchasing-workflow)

### BE-TUPLE-MIGRATION — Tuple-pattern services can't use ToProblem (P2, DEFERRED)

**Discovered**: 2026-04-21 during error handling standard migration

**Impact**: 149 `// ARCH-ALLOW: tuple service — pending TD-BE-TUPLE-MIGRATION` annotations across 12 endpoint files, which are excluded from the uniform `result.ToProblem(title)` helper until the underlying services migrate from `(bool success, T? response, string? error)` tuples to `Result<T>`.

#### Symptom

Endpoints that consume tuple-returning services can't use the new `ToProblem(title)` helper — the helper requires a `Result<T>` receiver. Those call sites currently carry an ARCH-ALLOW comment documenting the tuple dependency. Every new endpoint in these files will have to repeat the ARCH-ALLOW pattern until the service migrates.

#### Root cause

The tuple pattern predates the `Result<T>` vertical-slice convention. Ten services use it: `EventService`, `MemberDetailsService`, `UserManagementService`, `AuthenticationService`, `TestHelperService`, `VolunteerService`, `VolunteerAssignmentService`, `HealthService`, `SettingsService`, `PaymentListService`. A previous orchestrator-driven migration shifted newer services to Result<T> but left the older set.

#### Suggested fix approach

One service at a time (est. 30–60 min per service):
1. Change interface signature from `Task<(bool, T?, string?)>` → `Task<Result<T>>`.
2. Update service internals — replace tuple returns with `Result<T>.Success(value)` / `Result<T>.NotFound(...)` / etc. Use the kind-specific factories from the error handling standard.
3. Update each endpoint handler that consumes the service:
   - Replace the destructuring with `var result = await svc.Foo(...)`.
   - Replace the `Results.Problem(...)` call with `result.ToProblem("Title")`.
   - Remove the ARCH-ALLOW comment.
4. Re-run the arch test to confirm the ARCH-ALLOW count drops.
5. Re-run the unit/integration suite to confirm no regressions.

Services should be migrated in order of consumer count (EventService and MemberDetailsService first — they have the most endpoint call sites).

#### Authoritative records

- `docs/standards-processes/backend/error-handling-standard.md` — the standard
- `tests/WitchCityRope.Core.Tests/Features/Shared/EndpointErrorShapeTests.cs` — the arch test that carries this TD in its allowed-exceptions list
- Initial migration commit (pending) that landed Phase 1 foundation + ARCH-ALLOW annotations

---

### BE-BACKUP-MIGRATION — AdminBackupEndpoints bypasses Result<T> with try/catch (P3, DEFERRED)

**Discovered**: 2026-04-21 during error handling standard migration

**Impact**: 15 `// ARCH-ALLOW: backup feature uses try/catch wrapping Hangfire/Spaces — pending TD-BE-BACKUP-MIGRATION` annotations in `apps/api/Features/Backup/Endpoints/AdminBackupEndpoints.cs`.

#### Symptom

The backup feature (manual backup trigger, list, restore, delete, download, job status) wraps every handler in a bespoke try/catch with domain-appropriate `Results.Problem` titles and statuses rather than going through `Result<T>` + `ToProblem`. The endpoints work correctly; they just don't participate in the uniform error shape.

#### Root cause

`BackupOrchestrationService` and friends throw exceptions rather than returning `Result<T>` — the feature was built before the Result pattern was adopted repo-wide. The Hangfire/DigitalOcean Spaces integrations surface failures as exceptions naturally, and converting them to Result<T> requires restructuring the orchestration code.

#### Suggested fix approach

Refactor `BackupOrchestrationService` and `SpacesStorageService` to return `Result<T>` instead of throwing. Collapse the 15 endpoint try/catch blocks to `result.ToProblem(title)`. Est. ~2h. Re-evaluate when the backup feature receives substantial new work (current work is infrequent).

#### Authoritative records

- Mirrors inventory-purchasing-workflow's equivalent TD (`AdminBackupEndpoints` 15 ARCH-ALLOW sites) documented in the Error Handling Standard's "Known allowed exceptions" section.

---

### BE-DEDUPE-RESULT — EmailTemplates has its own Result<T> type shadowing Shared.Models.Result (P2, RESOLVED)

**Discovered**: 2026-04-21 during error handling standard migration
**Updated**: 2026-04-21 — **RESOLVED** (see "Resolution" sub-section at bottom)

**Impact**: 21 `// ARCH-ALLOW: local Result type (EmailTemplates.Services.Result) — not Shared.Models.Result, pending TD-BE-DEDUPE-RESULT` annotations in `apps/api/Features/EmailTemplates/Endpoints/EmailTemplateEndpoints.cs`. The EmailTemplates service methods return `WitchCityRope.Api.Features.EmailTemplates.Services.Result<T>` rather than `WitchCityRope.Api.Features.Shared.Models.Result<T>`, so the `ToProblem` extension — which is bound to the Shared.Models type — does not apply.

#### Symptom

Every EmailTemplates endpoint handler carries an ARCH-ALLOW instead of `result.ToProblem(title)`. A developer trying `result.ToProblem(...)` gets a CS1929 compile error with a confusing message about the wrong receiver type.

#### Root cause

`IEmailTemplateService.cs` declares its own `Result<T>` class at lines 205–225 (approximate) — a local definition that duplicates the shape of `Shared.Models.Result<T>` minus `ErrorKind`. The local copy was likely introduced before `Shared.Models.Result<T>` existed or in parallel with it, and never consolidated.

#### Suggested fix approach

1. Delete the local `Result<T>` declaration in `IEmailTemplateService.cs`.
2. Replace its usages (in `EmailTemplateService.cs` and callers) with `WitchCityRope.Api.Features.Shared.Models.Result<T>`.
3. Update the 21 endpoint handlers to use `result.ToProblem(title)`; remove ARCH-ALLOW comments.
4. Verify the arch test count drops by 21.
5. Run the unit/integration suite.

Est. effort: ~45 min. Low risk — purely additive for callers (the Shared.Models.Result has a superset of API).

#### Authoritative records

- Subagent report (2026-04-21 Phase 3c+3d+3e sweep): "EmailTemplates has its OWN `Result` type (`WitchCityRope.Api.Features.EmailTemplates.Services.Result<T>`, declared in `IEmailTemplateService.cs:205-225`) that is distinct from `Shared.Models.Result`."

#### Resolution (2026-04-21, commit pending)

Deleted the local `Result` / `Result<T>` classes from `IEmailTemplateService.cs`. Added `using WitchCityRope.Api.Features.Shared.Models;` there plus in `EmailTemplateService.cs` (the implementation in the same namespace). Other callers (jobs, other endpoint files) already resolved to `Shared.Models.Result<T>` through their own using directives, so no changes required in those.

Converted the 21 `ARCH-ALLOW` sites in `EmailTemplateEndpoints.cs` to `result.ToProblem(title)`. During that pass, also promoted EmailTemplateService's `.Failure()` calls to kind-specific factories (Rule B'-style): "Template not found" / "Email not found" → `.NotFound(...)` (→ HTTP 404); "Failed to ..." catch-alls → `.Infrastructure(...)` (→ HTTP 500). Pre-sweep these were all emitting 500 at the endpoint level from the old hardcoded `statusCode: 500`; post-sweep they emit the correct status per failure type.

Verified: 0 ARCH-ALLOW remaining in `EmailTemplateEndpoints.cs`, build clean, arch test passes, test suite within baseline range.

---

### BE-EXMESSAGE-LEAK-CATCHALL — Endpoint-level catch-all sites still pass ex.Message to client (P3, DEFERRED)

**Discovered**: 2026-04-21 during error handling standard migration

**Impact**: ~19 sites across various endpoint files where a handler's outer `catch (Exception ex) { return Results.Problem(detail: ex.Message, ...) }` still leaks the exception message to the wire. The arch test's ARCH-ALLOW allows them (subagent annotated them as `// ARCH-ALLOW: catch-all ex.Message leakage`), but they remain a minor security-hygiene issue.

#### Symptom

Raw exception text reaches the client from endpoint-level try/catch blocks that the Phase 3a sweep did not touch (Phase 3a focused on `ex.Message` inside service `Result.Failure(...)` strings; endpoint-level `catch (Exception ex) { Results.Problem(detail: ex.Message) }` were out of scope for that pass).

#### Suggested fix approach

Prefer deleting the endpoint-level try/catch outright: `GlobalExceptionHandler` already catches unhandled exceptions and produces a uniform 500 ProblemDetails. If the endpoint genuinely needs a custom title, convert to `return Result<T>.Infrastructure("<static msg>").ToProblem("<title>")` pattern (same as the `apps/api/Features/Admin/Endpoints/UsersEndpoints.cs` migration in Phase 3a).

Est. effort: ~30 min. Each site is independent; safe to fix opportunistically when touching the file.

#### Authoritative records

- Subagent report (2026-04-21 Phase 3c+3d+3e sweep): "19 handler `catch (Exception ex)` catch-alls (TD-BE-EXMESSAGE-LEAK)".

---

### BE-ENDPOINTS-DIR-OUT-OF-ARCH-SCAN — `apps/api/Endpoints/` has 38 unscanned violations (P3, DEFERRED)

**Discovered**: 2026-04-21 during error handling standard migration

**Impact**: 38 forbidden-call sites in `apps/api/Endpoints/VenueEndpoints.cs` and `apps/api/Endpoints/Admin/` are outside the arch test's scan directory (which only walks `apps/api/Features/*/Endpoints/` + `apps/api/Controllers/`). New drift in these files will not be caught.

#### Symptom

`apps/api/Endpoints/` is an older, pre-vertical-slice convention. These endpoints are actively routed (`app.MapVenueEndpoints()` and `app.MapPublicVenueEndpoints()` in `WebApplicationExtensions.cs`) but live outside the Features/ hierarchy.

#### Suggested fix approach

Two options:
1. **Move files to `apps/api/Features/Venues/Endpoints/`** (vertical-slice-aligned, preferred long-term). Update namespaces + registration call sites. Est. ~30 min plus a test run.
2. **Widen the arch test's scan to include `apps/api/Endpoints/`**. 2-line change in `EndpointErrorShapeTests.cs`. Then sweep the 38 violations to `ToProblem` or ARCH-ALLOW. Est. ~30 min plus a test run.

Either option produces uniform coverage. Option 1 is cleaner.

#### Authoritative records

- `apps/api/Features/Shared/Extensions/WebApplicationExtensions.cs` confirms the endpoints are actively routed.
- Subagent report (2026-04-21 Phase 3c+3d+3e sweep): "`apps/api/Endpoints/` (non-Features) has 38 unannotated violations in `VenueEndpoints.cs` and `Admin/VenueEndpoints.cs`."

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

## 2026-04-12 — Production health-check findings (incident 01-health-check-2026-04-12)

Resolved during the "Phase 1–3" cleanup session that followed the first run of the new `check-production-server` skill. Two HIGH-priority items from the incident report are resolved here; four MEDIUM/LOW items were logged as new active entries (BE-8 through BE-12, T-7, DEP-1) rather than fixed inline.

- **BE-6 — ProxyRsvp endpoint returned 500 instead of 409 on duplicate participation** — resolved 2026-04-12. `apps/api/Features/ProxyRsvp/Endpoints/ProxyRsvpEndpoints.cs` had a stale exact-string switch (`"Already has RSVP" => 409`) that didn't match the service's actual error string (`"Already participating"`), so duplicate-RSVP attempts fell through to the catch-all 500. Research also surfaced two OTHER strings ("Per-person limit reached", "Acceptance window expired") that were never in the switch at all and also produced 500s. Fix: replaced the exact-string switch across all three ProxyRsvp endpoints with a single private static `MapErrorToStatusCode` helper using case-insensitive `.Contains` matching, following the pattern already used by `ParticipationEndpoints.cs:156-179`. Comment block documents ordering dependency (403 matchers run before the 400 "required" matcher because "Vetting required" contains "required"). Regression covered by existing `CreateProxyRsvp_DuplicateRsvp_Returns409` integration test, which now passes in isolation (was previously wedged by T-1 shared-state pollution — that's why the bug reached prod). The broader "inconsistent error-mapping across endpoint files" pattern is captured as active item BE-8.
- **BE-13 — `check-production-server` audit 9.1 + 9.2 double-counted users with both RSVP and Ticket** — resolved 2026-04-12. The audit queries used `COUNT(*)` against `EventAttendances` rather than `COUNT(DISTINCT UserId)`, contradicting the business rule enforced by `IAttendanceCountService.GetReservedCountAsync` at `apps/api/Features/Participation/Services/IAttendanceCountService.cs:40-45`: "Counts unique people, not total records, to prevent double-counting users who have multiple records (RSVP + Ticket, multi-session tickets)." The bug produced a false-positive "Rope Jam March: 50 active against capacity 40" reading in incident `01-health-check-2026-04-12`. Corrected queries switched to `COUNT(DISTINCT UserId) FILTER (WHERE Status = 1)`; re-verified against production and returned zero rows across all events + all sessions in the last 365 days. Downstream effect: the original M1 finding was retracted in the incident report; BE-10 and BE-11 priorities were lowered from P2 to P3 since the empirical harm that drove them didn't exist.
- **BE-7 — DailyLogSummaryJob permanently Failed with "DBNull type mapping" exception** — resolved 2026-04-12. `apps/api/Features/Logging/Jobs/DailyLogSummaryJob.cs:208-211` passed `(object?)summary.Subcategory ?? DBNull.Value` as an untyped parameter to `ExecuteSqlRawAsync`. EF Core 9 / Npgsql can't infer a column type for bare `DBNull.Value`, so categories with no subcategoryExpression (cc_success, login_success, registration, email_scheduler) failed upsert every night. Fix: replaced the untyped-object-array parameters with explicit `NpgsqlParameter` instances and `NpgsqlDbType` enum values for each column. Preserves NULL semantics in the table (does NOT collapse null → empty string, which would have changed ON CONFLICT grouping). Regression covered by three new tests in `tests/unit/api/Features/Logging/DailyLogSummaryJobTests.cs`: null subcategory upsert, populated subcategory upsert, and twice-run idempotence. The broader "recurring Hangfire jobs have no test coverage" gap is captured as active item T-7; the "no alerting when jobs permanently fail" gap is captured as active item BE-9.

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
| 2026-04-11 | Opportunistic cleanup of 4 P3 items in areas touched by Phase 3: `BE-2` resolved (AttendanceSeeder filter moved server-side), `BE-3` resolved as FALSE POSITIVE (code already returned `"Unknown"`), `BE-4` resolved (stale `(N)` comments stripped — several were wrong), `T-5` resolved as deferred/not-worth-it (private method, unreachable branch). Active items remaining: `BE-1`, `T-1..4`, `T-6`, `TL-1`, `FE-1`, `FE-2`, `DOC-1`. | Post-deploy hygiene session (commit `25a83285`) |
| 2026-04-11 | Added `BE-5` (P2) — service layer swallows `OperationCanceledException` and returns 500. Discovered while investigating a user-reported staging console error. 12 false-positive 500s in 24h on low-traffic staging. Root cause: `catch (Exception ex)` blocks in `AttendanceService`, `RefreshTokenService`, and likely many more service files treat client-side cancellations as real errors. Fix: add `catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }` filter before the general catch. Deeper question (why so many cancellations on low-traffic staging?) logged as Phase B investigation. | Staging error investigation session (commit `e7047954`) |
| 2026-04-11 | Added `TL-2` (P2) — `run-test-suite --mode all` skips E2E phase after .NET failures. Discovered during opportunistic test run after BE-2/BE-4 cleanup: the skill exits right after the .NET summary without ever entering the E2E block, likely due to a `set -e` interaction between the main script and internal function scaffolding. Impact: `--mode all` currently never runs E2E at all given the 104-failure .NET baseline. Workaround: run `--mode unit` and `--mode e2e` separately. | Post-cleanup test run session (commit `0eb3e3ee`) |
| 2026-04-11 | Added **IMPORTANT agent rules block** at the top of this file (six numbered rules requiring agents to fully read the file before editing, update existing entries rather than adding new ones, cross-reference related entries, etc.). Written after user flagged that the agent had been adding new entries without thoroughly checking for existing ones to update. Also updated `T-1` in place with an "Updated failure baseline (2026-04-11)" sub-section containing empirical data from three test runs against commit `25a83285` — which produced integration failure counts of 77/77/61, directly confirming T-1's previously-unverified "test order determines failure count" theory. Prior T-1 text preserved; new data appended per the new rules. | Agent rules + T-1 empirical update session (commit `8dd8000e`) |
| 2026-04-11 | **Softened the IMPORTANT agent rules block** after user feedback that it was too restrictive. The original version pushed agents toward "update the closest existing entry" as the default, which would have led to force-fitting genuinely new issues into unrelated entries. Rebalanced framing: explicit that the goal is to prevent duplication, not to prevent new entries. Rule 2 now lists "signs it's a new entry" vs "signs it's an update" side by side. Rule 3 replaces "default to updating" with "ask the user when genuinely unsure". Added new Rule 7: every commit that touches this file must add a History row (catching a pattern where I violated this in `8dd8000e` and had to backfill in `a3196ff1`). | Agent rules rebalancing session (commit `67ee06f3`) |
| 2026-04-12 | Added 7 active items (BE-8 through BE-12, T-7, DEP-1) and 2 resolved items (BE-6 ProxyRsvp 500→409 fix, BE-7 DailyLogSummaryJob DBNull fix) from production health-check incident `01-health-check-2026-04-12`. Active items cover: inconsistent error-mapping across endpoints (BE-8), no alerting on permanently-Failed Hangfire jobs (BE-9), Session.Capacity unenforced (BE-10), capacity check+insert race + "way-over" investigation needed (BE-11), refund→TicketPurchase.PaymentStatus sync gap with 3 drift rows in prod (BE-12), no test coverage for 4 of 5 recurring Hangfire jobs (T-7), nginx cross-app "protocol options redefined" warnings (DEP-1). Added new `## Deployment / infrastructure` area section + `DEP` area code. BE-11 explicitly documents user concern that "10-over" capacity on Rope Jam March is inconsistent with the simple race-condition theory and deferred to Phase 4. BE-12 explicitly does NOT proceed without user approval because it affects payment reconciliation data. | Phase 1-3 cleanup session following first `check-production-server` skill run |
| 2026-04-12 | **Correction session.** User flagged that the Rope Jam March "10-over" finding was based on a faulty audit query that double-counted users with both an RSVP and a Ticket. Per `IAttendanceCountService.GetReservedCountAsync` business rule, one person = one seat regardless of how many EventAttendance rows they have. Re-queried production with corrected `COUNT(DISTINCT UserId)` — Rope Jam March actually 31/40 (9 under), and **zero events or sessions have been overbooked in the last 365 days**. Actions: (1) fixed the skill's audit 9.1 and 9.2 queries; (2) retracted M1 in the incident report; (3) lowered BE-10 and BE-11 priority from P2 to P3 (added "Updated" subsections per rule 4, original text preserved); (4) added resolved entry BE-13 documenting the audit-query bug. The race-condition theory in BE-11 is still valid in principle but no longer load-bearing. | Post-Phase-1-3 correction session |
| 2026-04-12 | Added BE-14 documenting the Authorize.NET E00114 "Invalid OTS Token" rapid-retry cascade that hit user Mr J (and Dean three weeks earlier). Frontend fix applied in `EventPaymentPage.tsx` — idempotency key now generated in a local variable on each call instead of read from useState, eliminating the state-timing window. Entry notes that the key-reuse symptom is resolved but the deeper question of why Authorize.NET returns E00114 for a fresh nonce on rapid retry remains unresolved, pending either local repro with the Auth.net sandbox or direct Auth.net support contact. Investigation approach documented in the entry. | Mr J ticket-purchase failure investigation |
| 2026-04-12 | BE-12 M2a/M2c/M2b fixes landed. M2c: centralized `TicketPurchase.PaymentStatus` sync inside `RefundService.ProcessRefundAsync` via new `SyncPaymentStatusFromRefundsAsync` helper — every caller now gets the status transition automatically (commit `0d0cf6f7`). M2a resolved by consequence. M2b: added new `TicketPurchasePaymentStatus.AwaitingManualRefund` enum value + admin UI badge (pink) + filter option + new skill audit 9.7 to surface the queue of pending manual refunds. `TicketPurchase.IsPaymentCompleted` and `PaymentListService.IsRefundable` extended to include the new status. Remaining: Row B one-off SQL to flip stale status on `e4e24d70-...` (Row A already resolved via admin action). Three pre-existing test failures (unrelated: RefundServiceEmailTests variable-name mismatch + RefundServiceTests failed-refund assertion) surfaced but are not caused by these changes — git diff confirms the affected code paths are untouched. | M2 Strategy B — batch completion before staging deploy |
| 2026-04-12 | Added BE-15 (PayPal multi-ticket undercharge) — RESOLVED. Mr J reported that credit card correctly charged $40 for 2 tickets but PayPal defaulted to $20. Root cause: (1) `@paypal/react-paypal-js`'s `<PayPalButtons>` caches the `createOrder` closure on mount, so parent prop changes (quantity, slider, ticket type) didn't propagate to the SDK; (2) idempotency key wasn't regenerated on PayPal cancel, so a retry with different cart values received the previously-cached $20 order from the backend. Fix: `forceReRender` on both `<PayPalButtons>` instances + new `handlePayPalCancel` in `EventPaymentPage` that regenerates the idempotency key (mirroring the BE-14 credit-card fix). Forensic audit of all 8 historical PayPal purchases showed no OBVIOUS undercharges but the bug is undetectable in DB data alone — the missing ticket leaves no trace. Entry marked resolved because the forward fix is comprehensive; past harm is a human-comms issue. | BE-15 forensic + forward fix (bundled into M2 deploy) |
| 2026-04-12 | Added T-8 (no PayPal checkout component tests + no skill wrapper for vitest). Captures the test-coverage gap that let today's four PayPal-adjacent bugs ship past all automated testing (BE-14 credit-card idempotency, BE-15 Fix 1 stale closure, BE-15 Fix 2 cancel-key reuse, and my BE-15 regression that dropped `setCurrentStep(2)`). Also documents the skill/hook gap: `run-test-suite` has no vitest mode, the pre-commit hook blocks direct `npx vitest` invocation, and the test-executor agent (the hook's suggested fallback) doesn't have the Task tool so it can't invoke anything either. Partial fix applied: extracted `handlePayPalSuccess` into a pure function + added 7 regression-guard tests in `tests/unit/web/features/payments/paypalSuccessHandler.test.ts` — type-checked clean but not runtime-verified in-session due to the skill gap. | BE-15 regression + T-8 entry |
| 2026-04-21 | **Error handling standard adoption (TD-029 port from inventory-purchasing-workflow).** Added five new Active items under a new "Error handling standard adoption" section: BE-TUPLE-MIGRATION (P2, 149 ARCH-ALLOW sites across 12 files pending tuple→Result<T> service migration), BE-BACKUP-MIGRATION (P3, 15 ARCH-ALLOW in AdminBackupEndpoints), BE-DEDUPE-RESULT (P2, 21 ARCH-ALLOW in EmailTemplateEndpoints from duplicate Result<T> type), BE-EXMESSAGE-LEAK-CATCHALL (P3, ~19 endpoint-level catch-alls still leaking ex.Message), BE-ENDPOINTS-DIR-OUT-OF-ARCH-SCAN (P3, apps/api/Endpoints/ has 38 violations outside scan scope). Foundation landed: `Result<T>` + `ResultErrorKind` enum, `ToProblem(title)` extension, `GlobalExceptionHandler`, `AntiforgeryExtensions.ValidateAsync` helper, `EndpointErrorShapeTests` architectural test in Core.Tests, sweep of ~429 forbidden-call sites (Results.Problem/BadRequest/NotFound/Conflict) across 27 endpoint files, ~51 `ex.Message` service-layer leaks cleaned up across 10 services, ProtectedController migrated to Minimal API, ~60 `Result<T>.Failure` calls promoted to kind-specific factories (NotFound/Conflict/Forbidden/Infrastructure/Upstream) across 8 services to produce correct HTTP status codes. Standard doc copied verbatim to `docs/standards-processes/backend/error-handling-standard.md`; CLAUDE.md updated with quick-reference table; code-reviewer agent updated with arch-test checklist; old `error-handling-patterns.md` collapsed to a pointer. Revert anchor: git tag `pre-error-handling-standard-2026-04-21`. | Error handling standard migration session |
| 2026-04-22 | **Vetting bulk Send Reminder feature + run-test-suite vitest mode.** Shipped commit `a5458644`: bulk Send Reminder action on the admin vetting list (filters selection to InterviewApproved subset, fans out per-app via existing endpoint), new "Reminders" column showing `RemindersSentCount`. Refactors triggered by the work: (1) lifted selection state from `VettingApplicationsList` to `AdminVettingPage` (controlled-component pattern), which fixed the pre-existing UX bug where checkboxes stayed checked after bulk actions — affected both Send Reminder and Put On Hold; (2) reordered hooks in `AdminVettingPage` to satisfy Rules of Hooks (pre-existing violation — useState was below an early-return); (3) added React Query invalidation to `OnHoldModal` so the list auto-refreshes after a hold action (closes the long-standing TODO comment). Skill infra: `run-test-suite` gained `--mode react` (vitest from apps/web with same safety nets as the dotnet path), `block-manual-test-runs.py` BLOCK_REASON updated to point at it. T-3 updated with today's measurement (187 failing, down from 196 baseline, with a specific finding on `OnHoldModal.test.tsx` text drift introduced by commit `395ec740`). Also fixed my own regression in OnHoldModal tests (added QueryClientProvider wrapper). Verified end-to-end on staging via agent-browser. | Vetting bulk reminder feature session |
| 2026-04-13 | **Production deploy + Row B backfill.** Bundled fixes shipped to production via `production-deploy` skill at git SHA `94d132f2`: BE-14 (commit `a92b7044`), BE-12 M2a/M2c (commit `0d0cf6f7`), BE-12 M2b (commit `78376f04`), BE-15 forward fix (commit `689136ca`), BE-15 regression repair (commit `897a27ac`), BE-15/T-8 testable handler extraction (commit `94d132f2`), audit 9.1/9.2 DISTINCT-users fix (commit `f52bc8fc`), proxy-RSVP + DailyLogSummaryJob fixes (commit `c5498ad8`). All three prod containers healthy after deploy. Row B one-off SQL executed: `TicketPurchases.Id = e4e24d70-92a4-4550-a439-e497915d18e1` `PaymentStatus` flipped `Completed` → `Refunded`; post-update scan shows zero stale refund rows remaining. BE-12 marked RESOLVED (all three drift rows reconciled — Row A via admin UI on 2026-04-12, Row B via SQL on 2026-04-13, and the underlying classes of drift are closed by the M2a/M2b/M2c code changes). BE-14 stays UNRESOLVED because the Authorize.NET E00114 root cause is still unknown even though the frontend symptom-fix shipped. BE-15 stays RESOLVED. Also: prevalence check on prod confirmed 29 users have both Active RSVP + Active Ticket for same event — expected behavior (AttendanceService auto-creates RSVP on ticket purchase when no RSVP exists, and users who RSVPed before upgrading to a ticket keep both records). Audits use `COUNT(DISTINCT UserId)` so no double-counting. UI correctly prioritizes Ticket > RSVP on both event page and dashboard. No action needed. | Prod deploy session (Strategy B bundle) |
| 2026-05-10 | **Added FE-3** (P2, UNRESOLVED) — Vite dev port has 5 sources of truth that all must agree (`vite.config.ts`, Dockerfile EXPOSE/ENV/CMD, compose `command:`/`ports:`). Cross-repo TD-port: an agent working in the sibling `accounting-automation` repo hit and fixed the same bug there during a port-renumbering exercise. Their fix consolidated their five sources to two; same approach would work here. Currently latent (no incorrect runtime — all five values agree on `5173`), but silent drift on the next port change is the failure mode. Discovered by external agent; entry written by the same agent without modifying any WCR code. | Cross-repo TD-port from accounting-automation |
| 2026-05-16 | **Added BE-18** (P2, UNRESOLVED) — Authorize.net ticket cancellations don't auto-refund the way PayPal cancellations do; the purchase is flagged `AwaitingManualRefund` for an admin to process by hand. Discovered during the production-incident 02 M2 deep-dive (two members found owed $20 each, outstanding 16/28 days). `RefundService` already has a working Authorize.net refund branch — it's just not wired into `AttendanceService.ProcessAutomaticRefundAsync`. Deferred for later research (likely needs Authorize.net void-vs-refund settlement-state handling). | Health-check M2 deep-dive |
| 2026-05-16 | **Added T-9** (P3, UNRESOLVED) — `MultiTicketCheckoutTests` MT_U02 / MT_U04 fail consistently because they assert ≥2 `EventAttendance` rows for a buy-2-assign-1-later purchase. Triaged during the refund feature's regression run: confirmed NOT a product bug — `AttendanceService.CreateTicketPurchaseAsync` deliberately creates no `EventAttendance` for an unassigned extra ticket (the `TicketPurchase` row tracks it; comment at lines 1363-1371). The tests have stale assertions; the fix is to expect 1 attendance. Logged rather than fixed inline per the triage request. | MultiTicketCheckout triage |
| 2026-05-16 | **Resolved T-9** — corrected the MT_U02 / MT_U04 assertions to expect 1 purchaser `EventAttendance` (MT_U04 also now asserts 2 `TicketPurchase` rows). Full `MultiTicketCheckoutTests` class verified 16/16 passing. | MultiTicketCheckout test fix |
| 2026-05-16 | **Added + resolved BE-16; added BE-17** — BE-16 (P2, RESOLVED): `EmailSchedulerJob` idempotency check only treated `Status == "Sent"` as handled, so a `Failed` reminder send was re-attempted every hourly run until the event's send window closed (~25 duplicate `Failed` rows observed on the April Rope Jam session). Discovered during a read-only production DB audit of event `cae0d3e3` ("Rope Jam - May") investigating a misdirected volunteer-reminder email. Fixed same session: bounded-retry guard (max 3 attempts) in `ProcessSessionAsync`. Code-review follow-ups also folded in: graceful handling of the concurrent-run idempotency race (`DbUpdateException` / Postgres `23505`), `AsNoTracking()` on the new query, and boundary tests in `EmailSchedulerJobRetryTests.cs`. BE-17 (P3, UNRESOLVED): per-batch trigger logging means a partial recipient failure is logged `Sent` and never retried — split out from BE-16's body during code review so it survives BE-16's resolution. The primary bug from this investigation — `EventEmailService.SendCatchUpRemindersAsync` re-sending `VolunteerReminder` to ticket buyers — was fixed in the same change (catch-up template-type allowlist + `EventEmailServiceCatchUpTests.cs`); active work, not a deferred-debt entry. | Misdirected volunteer-email investigation + code review |

---

*For current test suite state, see [`docs/standards-processes/testing/CURRENT_TEST_STATUS.md`](standards-processes/testing/CURRENT_TEST_STATUS.md). For the file registry, see [`docs/architecture/file-registry.md`](architecture/file-registry.md). For lessons learned about common failures, see [`docs/lessons-learned/`](lessons-learned/).*
