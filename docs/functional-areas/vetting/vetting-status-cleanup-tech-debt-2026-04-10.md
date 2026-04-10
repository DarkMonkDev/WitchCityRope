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
