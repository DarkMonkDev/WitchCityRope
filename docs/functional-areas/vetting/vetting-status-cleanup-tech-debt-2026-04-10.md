# Vetting Status Cleanup — Tech Debt / Known Issues Running List

**Created**: 2026-04-10
**Owner**: WitchCityRope dev team
**Purpose**: Running list of issues discovered during the multi-phase vetting status centralization project. These are things to review and decide on at the END of the project — they are NOT in scope for the individual phases unless explicitly called out.

**How to use this list**:
- When a pre-existing issue is discovered during Phase 1/2/3 work, add it here instead of fixing it inline
- At project end, review the list with the project owner and decide: fix / defer / close as won't-fix
- Each entry should have enough detail that a future agent can pick it up cold

---

## Related Work

This document is paired with the vetting status centralization project. Phase 1 of that project:
- Fixed the dashboard `VettingAlertBox` enum key mismatch bug (silent lookup failure for `UnderReview` / `InterviewApproved`)
- Created single-source config at `apps/web/src/features/vetting/constants/vettingStatusConfig.ts`
- Replaced `VettingStatusBox` on `/join` with `VettingAlertBox` for UI consistency
- Added CMS seeder entry for `vetting-interview-scheduling` slug
- Deleted orphaned `MembershipWidget.tsx` dead code

Phases 2 and 3 will migrate remaining consumers and normalize the backend int/enum split.

---

## Issues Discovered

### 1. Pre-existing TypeScript errors in `EventForm.tsx`

**Discovered**: 2026-04-10 during Phase 1 code quality check

**Location**:
- `apps/web/src/components/events/EventForm.tsx:1933` — `error TS2322: Type 'unknown' is not assignable to type 'ReactNode'`
- `apps/web/src/components/events/EventForm.tsx:2150` — `error TS2322: Type 'unknown' is not assignable to type 'ReactNode'`

**Why it's not in scope for the vetting cleanup**:
- EventForm.tsx is unrelated to vetting status
- Errors exist on `main` prior to Phase 1 — not introduced by this project
- Fixing them would expand commit scope and dilute the Phase 1 intent

**Risk if left**:
- TypeScript strict-mode CI check continues to fail
- Two errors mask any new errors introduced by the same file (hard to tell "did I make it worse?")
- Zero runtime impact AFAIK — the `unknown` likely falls through to String() at runtime

**Suggested fix approach**: Read the lines in context, find the JSX expression rendering an `unknown`-typed value, either narrow the type with a type guard or explicitly cast to string/ReactNode. Estimate: 15-30 min.

**Decision at project end**: __________________ (fix now / defer / won't-fix)

---

### 2. 🚨 URGENT: Unit test suite baseline is 60% broken

**Discovered**: 2026-04-10 during Phase 1 code review verification

**Baseline state** (measured after Phase 1 commit 0fdcbbd6):
- **Test Files**: 27 failed | 14 passed | 2 skipped (43 total)
- **Tests**: 196 failed | 246 passed | 40 skipped (482 total)

**Of the 196 failures**:
- 8 were introduced by Phase 1 (`VettingApplicationPage.test.tsx`) — my regressions from replacing `VettingStatusBox` with `VettingAlertBox` without updating the test file's mocks
- ~188 were already broken on `main` BEFORE Phase 1

**Representative pre-existing breakages**:
- `tests/unit/web/features/vetting/VettingStatusBox.test.tsx` (12 tests) — uses `useEventTimeZone` which calls `useQuery`, but test does not wrap in `QueryClientProvider`
- `tests/unit/web/features/admin/vetting/services/vettingAdminApi.test.ts` — error-object shape mismatch (test expects old `{response:{data:{error}}}` shape, code now throws `Error` objects)
- Many others with similar "missing provider" or "API shape drift" patterns

**Why this is URGENT, not just tech debt**:
- Unit tests are supposed to catch regressions between phases of this cleanup
- With 60% of the suite broken, adding new coverage (reviewer's S4) is worthless — new tests become islands of green in a red ocean, and real regressions get lost
- It implies CI is either not running these tests, running them with some suppression, or the suite has been allowed to rot unchecked
- Phase 2 and Phase 3 of the vetting cleanup will be done without reliable test safety — we are relying on browser verification and code review only

**Decision made 2026-04-10** (per user in chat): Separate agent will be spun up to fix the unit test suite in parallel with vetting Phases 2 and 3. Phase 1 shipped with Option 1 scope (no test file changes, no new tests added). The 8 `VettingApplicationPage.test.tsx` regressions from Phase 1 are acknowledged and left for the separate test-rehabilitation project.

**Suggested fix approach**: Dedicated investigation — find root cause of pre-existing breakage (missing global test setup? missing providers? drifted API shapes?), fix the infrastructure, re-enable failing tests in batches, verify CI actually runs the suite. This is a multi-day effort, not a quick fix.

**Decision at project end**: Tracked in separate project — no action needed here.

---

### 3. `VettingStatusBox.tsx` is now orphaned dead code

**Discovered**: 2026-04-10 during Phase 1 code review (S8)

**Location**:
- `apps/web/src/features/vetting/components/VettingStatusBox.tsx` (137 lines, the component)
- `apps/web/src/features/vetting/types/vettingStatus.ts:67-75` (unused `StatusBoxProps` interface)
- `tests/unit/web/features/vetting/VettingStatusBox.test.tsx` (12 pre-existing broken tests)

**Why it's not in scope for Phase 1**:
- Deleting the component would also require deleting its 12-test file
- Those 12 tests are already broken on main (see issue #2)
- The clean-up pulls in a much larger scope around the broken test suite
- Phase 1 was supposed to be minimum-scope per user's Option 1 choice

**Inconsistency with Phase 1**:
- Phase 1 did delete `MembershipWidget.tsx` for the same "orphaned dead code" reason
- Leaving `VettingStatusBox.tsx` means the commit is inconsistent — one orphan deleted, one left behind

**Suggested fix approach**: Phase 2 candidate. Delete all three files (component, test, unused interface) in a single commit after the test-suite-rehabilitation project restores baseline green.

**Decision at project end**: __________________ (Phase 2 / defer)

---

### 4. Backend `UserDashboardProfileService.cs` switch does not handle `Withdrawn`

**Discovered**: 2026-04-10 during Phase 1 code review (S3)

**Location**:
- `apps/api/Features/Dashboard/Services/UserDashboardProfileService.cs` — the switch on `vettingStatus` starting around line 260

**Current behavior**: `VettingStatus.Withdrawn` falls through to `default: dto.Message = "";`

**Why it's not a Phase 1 user-visible bug**: The frontend `VettingAlertBox` reads its message from the frontend `vettingStatusConfig.ts`, not from `dto.Message`, so withdrawn users still get a displayed message from the config. But any future consumer of `VettingStatusDto.Message` for a withdrawn user will get an empty string.

**Suggested fix approach**: Phase 3 candidate (backend normalization phase). Add `case VettingStatus.Withdrawn:` with a reasonable message. ~2 lines.

**Decision at project end**: __________________ (Phase 3 / defer)

---

### 5. Docblock drift in `VettingApplication.cs` enum XML comment

**Discovered**: 2026-04-10 during Phase 1 code review (S6)

**Location**:
- `apps/api/Features/Vetting/Entities/VettingApplication.cs:16` (XML doc says Withdrawn is value 7, actual value is 6 at line 134)

**Why it's not in scope**: Pre-existing documentation error in a file Phase 1 did not touch. Not a code bug, just wrong doc.

**Suggested fix approach**: Phase 3 candidate. One-line fix in the XML doc.

**Decision at project end**: __________________ (Phase 3 / librarian sweep)

---

### 6. Stale reference in `UNIT_TEST_FIX_GUIDE.md`

**Discovered**: 2026-04-10 during Phase 1 code review (S7)

**Location**:
- `apps/web/UNIT_TEST_FIX_GUIDE.md:161, 338` — references `VettingStatusBox.test.tsx` as "failing" but the guide itself is stale

**Suggested fix approach**: Librarian sweep. Either update the guide or delete it if obsolete. Low priority.

**Decision at project end**: __________________ (librarian / defer)

---

### 7. Latent XSS risk if `interviewScheduleUrl` ever becomes user-editable

**Discovered**: 2026-04-10 during Phase 1 code review (Sec1)

**Location**:
- `apps/web/src/pages/dashboard/components/VettingAlertBox.tsx:71` (and line 81 for `reapplyInfoUrl`)

**Current risk**: None. Both `interviewScheduleUrl` and `reapplyInfoUrl` are hardcoded backend literals set in `UserDashboardProfileService.cs`. There is no untrusted input path.

**Future risk**: If a developer ever refactors these URLs to come from the database, admin input, or any other user-editable source, a malicious admin (or SQL injection from elsewhere) could inject `javascript:alert(document.cookie)` and React would render it as a working link. React does NOT sanitize `href` values by default.

**Suggested fix approach**: Add a `sanitizeHref()` util that returns `null` for anything not matching `/^\/[^/]|^https?:\/\//`, and use it in `VettingAlertBox`. OR add an inline comment warning future refactorers. Estimate: 30 min.

**Decision at project end**: __________________ (add util now / comment only / defer)

---

<!--
TEMPLATE for future entries — copy and fill in:

### N. Short descriptive title

**Discovered**: YYYY-MM-DD during Phase X of vetting cleanup

**Location**:
- path/to/file.ext:line — brief description

**Why it's not in scope**:
-

**Risk if left**:
-

**Suggested fix approach**:

**Decision at project end**: __________________

---
-->
