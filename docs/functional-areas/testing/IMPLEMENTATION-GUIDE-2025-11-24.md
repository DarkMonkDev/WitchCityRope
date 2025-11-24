# Test System Reorganization - Implementation Guide
**Date**: November 24, 2025
**Status**: IN PROGRESS
**Owner**: Main Agent + Sub-agents
**User Approval**: APPROVED - Proceed with all phases

## Executive Summary

Reorganizing WitchCityRope test system to centralized `/tests/` structure for simplicity and maintainability. User-approved plan eliminates scattered tests and establishes single source of truth.

## User Requirements (MANDATORY)

1. ✅ **ALL tests in `/tests/`** - No tests anywhere else
2. ✅ **DELETE Blazor tests** - Not archive, DELETE
3. ✅ **Archive non-Blazor obsolete tests** - Create list for later review
4. ✅ **Commit between phases** - Use git-manager
5. ✅ **Run all tests at end** - Verify structure works
6. ✅ **Complete all phases** - Don't stop for approval
7. ✅ **Use sub-agents in parallel** - Maximize efficiency
8. ✅ **Document thoroughly** - For context window compaction

## Target Structure (SIMPLE)

```
/tests/
├── e2e/                          # E2E tests (renamed from playwright/)
│   ├── admin/
│   ├── auth/
│   ├── events/
│   └── vetting/
│
├── unit/
│   ├── api/                      # .NET API unit tests (already here)
│   │   ├── Features/
│   │   └── Services/
│   └── web/                      # React unit tests (MOVED from /apps/web/src/)
│       ├── components/
│       ├── hooks/
│       └── pages/
│
├── integration/                  # Full-stack integration (already here)
│
├── system/                       # System tests (already here)
│
└── shared/                       # Shared test utilities
    ├── builders/
    ├── fixtures/
    └── helpers/
```

## Implementation Phases

### Phase 1: Cleanup - Delete Blazor, Archive Obsolete
**Status**: PENDING
**Risk**: LOW
**Time**: 30 minutes
**Commit Message**: "chore(tests): delete Blazor tests and archive obsolete tests"

**Actions:**
1. **DELETE** (not archive):
   - `/src/_archive/WitchCityRope.Web.Tests-blazor-legacy-2025-08-22/` (12 files)
   - All Blazor references in documentation

2. **ARCHIVE** to `/tests/_archive/obsolete-2025-11-24/`:
   - 29 `.disabled` test files (with analysis document)
   - Any duplicate/obsolete tests identified

3. **CREATE** `/tests/_archive/obsolete-2025-11-24/ARCHIVED-TESTS-INVENTORY.md`:
   - List of archived tests
   - Reason for archival
   - Decision: Keep or delete later

**Sub-agent**: librarian (file organization specialist)

---

### Phase 2: Move React Tests to /tests/unit/web/
**Status**: PENDING
**Risk**: MEDIUM (import path updates)
**Time**: 1-2 hours
**Commit Message**: "refactor(tests): move React tests to centralized /tests/unit/web/"

**Actions:**
1. **CREATE** directory structure:
   ```
   /tests/unit/web/
   ├── components/
   ├── hooks/
   ├── pages/
   ├── features/
   └── lib/
   ```

2. **MOVE** 43 React test files from:
   - `/apps/web/src/components/__tests__/` → `/tests/unit/web/components/`
   - `/apps/web/src/hooks/__tests__/` → `/tests/unit/web/hooks/`
   - `/apps/web/src/pages/__tests__/` → `/tests/unit/web/pages/`
   - `/apps/web/src/features/*/__tests__/` → `/tests/unit/web/features/`
   - `/apps/web/src/lib/__tests__/` → `/tests/unit/web/lib/`

3. **UPDATE** import paths in test files:
   - Change `import { Component } from '../Component'`
   - To `import { Component } from '@/components/Component'`
   - Or use proper relative paths from new location

4. **UPDATE** `/apps/web/vitest.config.ts`:
   - Change test include pattern to look in `/tests/unit/web/`

5. **VERIFY** tests still discover and run:
   ```bash
   cd /apps/web && npm run test
   ```

**Sub-agent**: react-developer (React specialist)

---

### Phase 3: Rename /tests/playwright/ to /tests/e2e/
**Status**: PENDING
**Risk**: LOW
**Time**: 15 minutes
**Commit Message**: "refactor(tests): rename playwright/ to e2e/ for clarity"

**Actions:**
1. **RENAME** directory:
   ```bash
   mv /tests/playwright/ /tests/e2e/
   ```

2. **UPDATE** `/playwright.config.ts`:
   - Change `testDir: './tests/playwright'`
   - To `testDir: './tests/e2e'`

3. **UPDATE** documentation references:
   - TESTING_GUIDE.md
   - Any scripts referencing playwright directory

4. **VERIFY** E2E tests discover:
   - Use test-catalog-updater skill to list E2E tests

**Sub-agent**: librarian (file organization)

---

### Phase 4: Update TESTING_GUIDE.md
**Status**: PENDING
**Risk**: LOW
**Time**: 30 minutes
**Commit Message**: "docs(tests): update TESTING_GUIDE.md with simplified structure"

**Actions:**
1. **UPDATE** "Test Organization" section with new structure
2. **ADD** "Test Location Rules (MANDATORY)" section:
   ```markdown
   ## Test Location Rules (MANDATORY)

   ### Rule #1: ALL tests go in `/tests/`
   - E2E tests: `/tests/e2e/[feature]/`
   - React unit tests: `/tests/unit/web/[feature]/`
   - .NET unit tests: `/tests/unit/api/[feature]/`
   - Integration tests: `/tests/integration/`

   ### Rule #2: NO tests anywhere else
   - ❌ `/apps/web/src/__tests__/` - WRONG
   - ❌ Co-located tests - WRONG
   - ❌ Anywhere except `/tests/` - WRONG

   ### Rule #3: When in doubt
   - All tests belong in `/tests/`
   ```

3. **REMOVE** all Blazor references
4. **UPDATE** file paths in examples
5. **ADD** rationale for centralized structure

**Sub-agent**: librarian (documentation specialist)

---

### Phase 5: Update Agent Lessons-Learned Files
**Status**: PENDING
**Risk**: LOW
**Time**: 45 minutes
**Commit Message**: "docs(agents): update lessons-learned to reference TESTING_GUIDE.md"

**Files to Update (6 files):**

1. `/docs/lessons-learned/test-developer-lessons-learned.md`
   - **Add MANDATORY reading**: Link to TESTING_GUIDE.md
   - Remove duplicated test location instructions
   - Add: "See TESTING_GUIDE.md for test organization rules"

2. `/docs/lessons-learned/test-developer-lessons-learned-2.md`
   - Same updates as above

3. `/docs/lessons-learned/test-executor-lessons-learned.md`
   - **Add OPTIONAL reading**: Link to TESTING_GUIDE.md
   - Note: Tests now at `/tests/e2e/` not `/tests/playwright/`

4. `/docs/lessons-learned/react-developer-lessons-learned.md`
   - **Add MANDATORY reading**: Link to TESTING_GUIDE.md
   - Update: React tests go in `/tests/unit/web/` not co-located

5. `/docs/lessons-learned/backend-developer-lessons-learned.md`
   - **Add OPTIONAL reading**: Link to TESTING_GUIDE.md
   - Confirm .NET tests at `/tests/unit/api/`

6. `/.claude/agents/orchestrator.txt` (if exists)
   - Add TESTING_GUIDE.md to optional reading for workflow orchestration

**Pattern for Updates:**
```markdown
## Testing - MANDATORY READING (or OPTIONAL)

**Single Source of Truth**: [TESTING_GUIDE.md](/docs/standards-processes/testing/TESTING_GUIDE.md)

### Critical Rules
- ALL tests go in `/tests/` directory
- React tests: `/tests/unit/web/[feature]/`
- E2E tests: `/tests/e2e/[feature]/`
- DO NOT create tests co-located with source code

**See TESTING_GUIDE.md for complete testing standards.**
```

**Sub-agent**: librarian (documentation updates)

---

### Phase 6: Clean Up Empty Directories
**Status**: PENDING
**Risk**: LOW
**Time**: 10 minutes
**Commit Message**: "chore(tests): remove empty test directories from apps/"

**Actions:**
1. **REMOVE** empty directories after moving React tests:
   ```bash
   find /apps/web/src -type d -name "__tests__" -empty -delete
   ```

2. **VERIFY** no empty test directories remain:
   ```bash
   find /apps/web/src -type d -name "__tests__"
   ```

**Sub-agent**: librarian (cleanup)

---

### Phase 7: Final Verification - Run All Tests
**Status**: PENDING
**Risk**: N/A (verification only)
**Time**: 30-60 minutes (test execution)
**Commit Message**: N/A (verification only, commit results document)

**Actions:**
1. **Run .NET Unit Tests**:
   ```bash
   cd /home/chad/repos/witchcityrope
   dotnet test tests/WitchCityRope.Core.Tests/
   dotnet test tests/unit/api/
   ```

2. **Run React Unit Tests**:
   ```bash
   cd /apps/web
   npm run test
   ```

3. **Run Integration Tests**:
   ```bash
   dotnet test tests/integration/
   ```

4. **Run E2E Tests** (list only, don't run full suite):
   - Use test-catalog-updater skill to list E2E tests

5. **CREATE** verification report:
   `/test-results/reorganization-verification-2025-11-24.md`

6. **COMMIT** verification report

**Sub-agent**: test-executor (test execution specialist)

---

## Git Commit Strategy

**Use git-manager agent for all commits.**

**Commit after each phase:**
1. Phase 1: "chore(tests): delete Blazor tests and archive obsolete tests"
2. Phase 2: "refactor(tests): move React tests to centralized /tests/unit/web/"
3. Phase 3: "refactor(tests): rename playwright/ to e2e/ for clarity"
4. Phase 4: "docs(tests): update TESTING_GUIDE.md with simplified structure"
5. Phase 5: "docs(agents): update lessons-learned to reference TESTING_GUIDE.md"
6. Phase 6: "chore(tests): remove empty test directories from apps/"
7. Phase 7: "docs(tests): add reorganization verification report"

**IMPORTANT**: Do NOT push to remote. Only commit locally.

---

## Rollback Plan

If anything goes wrong:
1. All changes are in local commits only (not pushed)
2. Can use `git reset --hard` to rollback to specific phase
3. Each phase is independent - can rollback individual phases

---

## Success Criteria

- ✅ All tests in `/tests/` directory
- ✅ No tests in `/apps/web/src/`
- ✅ Blazor tests DELETED
- ✅ Obsolete tests archived with inventory
- ✅ TESTING_GUIDE.md updated
- ✅ 6 agent lessons-learned files updated
- ✅ All test suites run successfully
- ✅ Local commits for each phase
- ✅ Verification report created

---

## Current Progress Tracking

**Phase 1**: ⏳ PENDING
**Phase 2**: ⏳ PENDING
**Phase 3**: ⏳ PENDING
**Phase 4**: ⏳ PENDING
**Phase 5**: ⏳ PENDING
**Phase 6**: ⏳ PENDING
**Phase 7**: ⏳ PENDING

**Last Updated**: 2025-11-24 (Start)

---

## Notes for Context Window Compaction

**If context window resets, read this guide and continue from last completed phase.**

**To check progress:**
```bash
git log --oneline | head -10
```

Look for commit messages matching phase commit patterns.

**To verify current state:**
```bash
ls -la /tests/e2e/          # Should exist if Phase 3 done
ls -la /tests/unit/web/     # Should exist if Phase 2 done
ls -la /apps/web/src/__tests__/  # Should NOT exist if Phase 2 done
```

**Reference documents:**
- This guide: `/docs/functional-areas/testing/IMPLEMENTATION-GUIDE-2025-11-24.md`
- Analysis: `/docs/functional-areas/testing/test-system-analysis-2025-11-24.md`
- Testing Guide: `/docs/standards-processes/testing/TESTING_GUIDE.md`
