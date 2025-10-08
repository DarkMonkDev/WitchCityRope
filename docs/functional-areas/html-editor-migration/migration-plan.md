# TinyMCE to @mantine/tiptap Migration Plan
<!-- Last Updated: 2025-10-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Migration Team -->
<!-- Status: Active -->

## Executive Summary

### Migration Overview
**Objective**: Replace TinyMCE rich text editor with @mantine/tiptap to eliminate testing quota issues and improve development experience.

**Current Pain Points**:
- TinyMCE requires API key with usage quotas
- E2E tests fail when quota exceeded
- Workaround (Textarea fallback) unacceptable for production
- Additional complexity managing API keys across environments

**Solution Benefits**:
- **100% client-side** - no API keys or quotas
- **Already installed** - all dependencies present
- **Mantine-integrated** - automatic theming
- **Smaller bundle** - ~69-91% reduction
- **Test-friendly** - no Playwright issues

**Timeline**: 2-3 days (18-25 hours total effort)

**Risk Level**: **LOW** - Drop-in replacement, full rollback capability

### Quick Reference

| Metric | Value |
|--------|-------|
| **Components to Replace** | 3 (TinyMCERichTextEditor, SimpleTinyMCE, EventForm inline) |
| **Config Files to Update** | 5 (.env files, environment.ts, package.json) |
| **Tests to Delete** | 4 (TinyMCE-specific E2E tests) |
| **Tests to Update** | 1 (events-management-e2e.spec.ts selectors) |
| **New Dependencies** | 0 (all already installed) |
| **Total Duration** | 18-25 hours (2-3 business days) |
| **Rollback Time** | < 1 hour (simple git revert) |

---

## Prerequisites

### Environment Requirements
**MUST BE TRUE before starting**:

1. **Docker Environment Running**
   ```bash
   ./dev.sh
   # Verify containers running
   docker ps | grep witchcityrope
   ```

2. **Tests Passing (Baseline)**
   ```bash
   # Run E2E tests to establish baseline
   npm run test:e2e
   # Document pass rate (e.g., "168/268 passing = 62.7%")
   ```

3. **Clean Git Working Directory**
   ```bash
   git status
   # Should show: "nothing to commit, working tree clean"
   ```

4. **Dependencies Installed**
   ```bash
   npm install
   # Verify @mantine/tiptap and @tiptap/* packages present
   npm list @mantine/tiptap
   npm list @tiptap/react
   ```

5. **Team Communication**
   - [ ] Notify team of migration start
   - [ ] Assign migration owner
   - [ ] Block conflicting work on EventForm component
   - [ ] Schedule 2-3 day migration window

### Documentation to Read First
- [ ] [README.md](./README.md) - Migration overview
- [ ] [component-implementation-guide.md](./component-implementation-guide.md) - Component code
- [ ] [testing-migration-guide.md](./testing-migration-guide.md) - Test updates
- [ ] [rollback-plan.md](./rollback-plan.md) - Safety procedures

---

## Phase 1: Preparation & Setup (30-60 minutes)

### Objective
Establish clean baseline and create feature branch for safe migration.

### Step 1.1: Run Baseline Tests (15 minutes)

```bash
# Start Docker if not running
./dev.sh

# Wait for containers to be healthy
docker ps

# Run E2E tests to establish baseline
npm run test:e2e

# Document results
echo "Baseline: X/268 tests passing (Y%)" > /tmp/migration-baseline.txt
```

**Success Criteria**:
- Tests complete without errors
- Pass rate documented
- Know which tests currently fail

**Troubleshooting**:
- If Docker not running: `./dev.sh` then wait 30 seconds
- If tests hang: `Ctrl+C`, run `npm run test:cleanup`, retry
- If out of memory: Set `NODE_OPTIONS='--max-old-space-size=4096'`

### Step 1.2: Create Feature Branch (5 minutes)

```bash
# Ensure on main branch
git checkout main

# Pull latest changes
git pull origin main

# Create feature branch
git checkout -b feature/migrate-tinymce-to-tiptap

# Verify branch created
git branch --show-current
# Output should be: feature/migrate-tinymce-to-tiptap
```

**Success Criteria**:
- On feature branch
- No uncommitted changes
- Branch name matches convention

### Step 1.3: Document Current State (10 minutes)

Create session document:

```bash
# Create session work directory
mkdir -p /home/chad/repos/witchcityrope/session-work/$(date +%Y-%m-%d)

# Document current state
cat > /home/chad/repos/witchcityrope/session-work/$(date +%Y-%m-%d)/tinymce-migration-baseline.md << 'EOF'
# TinyMCE Migration Baseline - $(date +%Y-%m-%d)

## Test Results Before Migration
- **Total Tests**: 268
- **Passing**: X
- **Failing**: Y
- **Pass Rate**: Z%

## Components Using TinyMCE
1. /apps/web/src/components/forms/TinyMCERichTextEditor.tsx
2. /apps/web/src/components/forms/SimpleTinyMCE.tsx
3. /apps/web/src/components/events/EventForm.tsx (inline)

## Configuration Files with TinyMCE
1. /apps/web/.env.example (lines 14-15)
2. /apps/web/.env.staging (VITE_TINYMCE_API_KEY)
3. /apps/web/.env.template (VITE_TINYMCE_API_KEY)
4. /apps/web/src/config/environment.ts (lines 45, 86)
5. /apps/web/package.json (line 35)

## Tests Using TinyMCE
1. tinymce-visual-verification.spec.ts
2. tinymce-editor.spec.ts
3. tinymce-debug.spec.ts
4. tinymce-basic-check.spec.ts
5. events-management-e2e.spec.ts (uses .tox-tinymce selectors)
EOF
```

**Success Criteria**:
- Baseline document created
- All current state documented
- Ready to proceed to implementation

### Phase 1 Completion Checklist
- [ ] Docker containers running
- [ ] Baseline tests completed and documented
- [ ] Feature branch created and checked out
- [ ] Current state documented in session-work
- [ ] No blocking issues identified
- [ ] Ready to proceed to Phase 2

**Estimated Time**: 30-60 minutes
**Next Phase**: [Phase 2: Component Migration](#phase-2-component-migration-4-6-hours)

---

## Phase 2: Component Migration (4-6 hours)

### Objective
Create MantineTiptapEditor component and replace all TinyMCE usage.

### Step 2.1: Create MantineTiptapEditor Component (2-3 hours)

**CRITICAL**: See [component-implementation-guide.md](./component-implementation-guide.md) for complete code.

```bash
# Create new component file
touch /home/chad/repos/witchcityrope/apps/web/src/components/forms/MantineTiptapEditor.tsx
```

**Component Requirements**:
- Drop-in replacement for TinyMCERichTextEditor
- Same props interface: `value`, `onChange`, `placeholder`, `minRows`
- Variable insertion extension for {{fieldName}} syntax
- Mantine theme integration
- Form integration support

**Implementation Steps**:

1. **Copy Complete Component Code**
   - Open [component-implementation-guide.md](./component-implementation-guide.md)
   - Copy entire MantineTiptapEditor.tsx code
   - Paste into new file
   - Save

2. **Create Variable Insertion Extension**
   ```typescript
   // Already included in component code from guide
   // Supports {{fieldName}} syntax with autocomplete menu
   ```

3. **Test Component in Isolation**
   ```bash
   # Component should compile without errors
   npm run build

   # Check TypeScript compilation
   npx tsc --noEmit
   # Should output: "No TypeScript errors"
   ```

**Success Criteria**:
- MantineTiptapEditor.tsx file created
- TypeScript compiles without errors
- Component exports correctly
- Variable insertion extension included

**Troubleshooting**:
- **Import errors**: Verify @mantine/tiptap installed (`npm list @mantine/tiptap`)
- **Type errors**: Ensure @tiptap/react types installed
- **Extension errors**: Check custom extension code matches guide

### Step 2.2: Update EventForm Component (1-2 hours)

Replace inline TinyMCE usage in EventForm.

**Current Code** (lines 22, 92-93, 102-130):
```typescript
import { Editor } from '@tinymce/tinymce-react';

const tinyMCEApiKey = import.meta.env.VITE_TINYMCE_API_KEY;
const shouldUseTinyMCE = !!tinyMCEApiKey;

const RichTextEditor: React.FC<{...}> = ({ value, onChange, height, placeholder }) => {
  if (!shouldUseTinyMCE) {
    return <Textarea ... />
  }
  return <Editor apiKey={tinyMCEApiKey} ... />
};
```

**New Code**:
```typescript
import { MantineTiptapEditor } from '../forms/MantineTiptapEditor';

// Remove: tinyMCEApiKey, shouldUseTinyMCE variables
// Remove: entire RichTextEditor component

// Replace all <RichTextEditor /> instances with:
<MantineTiptapEditor
  value={form.values.fullDescription}
  onChange={(content) => form.setFieldValue('fullDescription', content)}
  placeholder="Describe the event..."
  minRows={6}
/>
```

**Files to Update**:
1. `/apps/web/src/components/events/EventForm.tsx` - Remove TinyMCE import and inline component

**Verification Steps**:
```bash
# 1. TypeScript compilation
npx tsc --noEmit

# 2. Build check
npm run build

# 3. Start dev server (in Docker)
./dev.sh

# 4. Manual testing
# Navigate to: http://localhost:5174/admin/events
# Click "Create Event"
# Verify rich text editor renders
# Type text and verify formatting works
# Test variable insertion: type "{{" and verify autocomplete
```

**Success Criteria**:
- EventForm imports MantineTiptapEditor
- All TinyMCE references removed from EventForm
- TypeScript compiles without errors
- Build succeeds
- Manual testing confirms editor works

### Step 2.3: Delete Old TinyMCE Components (30 minutes)

```bash
# Delete primary TinyMCE component
rm /home/chad/repos/witchcityrope/apps/web/src/components/forms/TinyMCERichTextEditor.tsx

# Delete test component
rm /home/chad/repos/witchcityrope/apps/web/src/components/forms/SimpleTinyMCE.tsx

# Delete test pages
rm /home/chad/repos/witchcityrope/apps/web/src/pages/TestTinyMCE.tsx
rm /home/chad/repos/witchcityrope/apps/web/src/pages/TinyMCETest.tsx

# Verify files deleted
ls /home/chad/repos/witchcityrope/apps/web/src/components/forms/Tiny*
# Should output: "No such file or directory"
```

**Success Criteria**:
- 4 files deleted
- No import errors after deletion
- TypeScript compilation clean

### Step 2.4: Commit Component Migration (15 minutes)

```bash
# Stage all changes
git add .

# Commit with descriptive message
git commit -m "$(cat <<'EOF'
feat: Migrate from TinyMCE to @mantine/tiptap rich text editor

BREAKING CHANGE: Removes TinyMCE dependency and API key requirement

Changes:
- Add MantineTiptapEditor component with variable insertion support
- Update EventForm to use MantineTiptapEditor
- Remove TinyMCERichTextEditor component
- Remove SimpleTinyMCE test component
- Delete TinyMCE test pages

Benefits:
- No API key management required
- No testing quota issues
- Smaller bundle size (~70% reduction)
- Better Mantine theme integration
- Same functionality preserved

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"

# Verify commit
git log -1 --oneline
```

**Success Criteria**:
- All changes committed
- Commit message detailed
- Git log shows commit

### Phase 2 Completion Checklist
- [ ] MantineTiptapEditor component created and tested
- [ ] Variable insertion extension working
- [ ] EventForm updated to use new component
- [ ] Old TinyMCE components deleted
- [ ] TypeScript compilation: 0 errors
- [ ] Build succeeds
- [ ] Manual testing confirms editor works
- [ ] Changes committed to feature branch

**Estimated Time**: 4-6 hours
**Next Phase**: [Phase 3: Configuration Cleanup](#phase-3-configuration-cleanup-1-2-hours)

---

## Phase 3: Configuration Cleanup (1-2 hours)

### Objective
Remove all TinyMCE configuration references and package dependencies.

**CRITICAL**: See [configuration-cleanup-guide.md](./configuration-cleanup-guide.md) for detailed instructions.

### Step 3.1: Update Environment Files (30 minutes)

**File 1**: `/apps/web/.env.example`
```bash
# Remove lines 14-15
# OLD:
# # TinyMCE API Key (for rich text editor)
# VITE_TINYMCE_API_KEY=your-api-key-here

# Verification
grep -n "TINYMCE" /home/chad/repos/witchcityrope/apps/web/.env.example
# Should output: (nothing)
```

**File 2**: `/apps/web/.env.staging`
```bash
# Remove VITE_TINYMCE_API_KEY line
# Verification
grep "TINYMCE" /home/chad/repos/witchcityrope/apps/web/.env.staging
# Should output: (nothing)
```

**File 3**: `/apps/web/.env.template`
```bash
# Remove VITE_TINYMCE_API_KEY line
# Verification
grep "TINYMCE" /home/chad/repos/witchcityrope/apps/web/.env.template
# Should output: (nothing)
```

**Success Criteria**:
- No TINYMCE references in any .env file
- grep searches return empty

### Step 3.2: Update environment.ts Config (15 minutes)

**File**: `/apps/web/src/config/environment.ts`

Remove lines 45 and 86:
```typescript
// REMOVE line 45:
tinyMceApiKey: import.meta.env.VITE_TINYMCE_API_KEY,

// REMOVE line 86 (config property definition)
```

**Verification**:
```bash
grep -n "tinyMce" /home/chad/repos/witchcityrope/apps/web/src/config/environment.ts
# Should output: (nothing)

# Test TypeScript compilation
npx tsc --noEmit
```

**Success Criteria**:
- No tinyMce references in environment.ts
- TypeScript compiles without errors

### Step 3.3: Update package.json (15 minutes)

**File**: `/apps/web/package.json`

Remove line 35:
```json
// REMOVE:
"@tinymce/tinymce-react": "^6.3.0",
```

**Verification**:
```bash
grep -n "tinymce" /home/chad/repos/witchcityrope/apps/web/package.json
# Should output: (nothing)
```

**Success Criteria**:
- No tinymce references in package.json
- File still valid JSON

### Step 3.4: Update Lock Files (15 minutes)

```bash
# Remove TinyMCE from node_modules
cd /home/chad/repos/witchcityrope/apps/web
npm uninstall @tinymce/tinymce-react

# Verify removal
npm list @tinymce/tinymce-react
# Should output: "(empty)" or error "not found"

# Clean install to update lock files
rm -rf node_modules package-lock.json
npm install

# Verify installation successful
npm list @mantine/tiptap
# Should show: @mantine/tiptap@7.17.8
```

**Success Criteria**:
- TinyMCE package removed
- Lock files updated
- All Mantine/Tiptap packages present
- npm install completes successfully

### Step 3.5: Commit Configuration Cleanup (15 minutes)

```bash
# Stage all changes
git add .

# Commit
git commit -m "$(cat <<'EOF'
chore: Remove TinyMCE configuration and dependencies

Changes:
- Remove VITE_TINYMCE_API_KEY from .env files
- Remove tinyMceApiKey from environment.ts
- Uninstall @tinymce/tinymce-react package
- Update package-lock.json

Migration to @mantine/tiptap eliminates need for API key
configuration and external service dependencies.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"

# Verify commit
git log -1 --oneline
```

**Success Criteria**:
- All config changes committed
- Commit message clear
- Git log shows commit

### Phase 3 Completion Checklist
- [ ] All .env files cleaned (no TINYMCE references)
- [ ] environment.ts cleaned (no tinyMce references)
- [ ] package.json cleaned (no @tinymce dependencies)
- [ ] Lock files updated (npm install successful)
- [ ] TypeScript compilation: 0 errors
- [ ] Build succeeds
- [ ] Changes committed to feature branch

**Estimated Time**: 1-2 hours
**Next Phase**: [Phase 4: Test Suite Updates](#phase-4-test-suite-updates-2-3-hours)

---

## Phase 4: Test Suite Updates (2-3 hours)

### Objective
Delete TinyMCE-specific tests and update selectors for new editor.

**CRITICAL**: See [testing-migration-guide.md](./testing-migration-guide.md) for complete details.

### Step 4.1: Delete TinyMCE-Specific E2E Tests (15 minutes)

```bash
# Delete 4 TinyMCE-specific tests
rm /home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-visual-verification.spec.ts
rm /home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-editor.spec.ts
rm /home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-debug.spec.ts
rm /home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce-basic-check.spec.ts

# Verify deletion
ls /home/chad/repos/witchcityrope/apps/web/tests/playwright/tinymce*.spec.ts
# Should output: "No such file or directory"
```

**Success Criteria**:
- 4 test files deleted
- No remaining tinymce*.spec.ts files
- Test suite still compiles

### Step 4.2: Update events-management-e2e.spec.ts Selectors (1-2 hours)

**File**: `/apps/web/tests/playwright/events-management-e2e.spec.ts`

**Selector Changes**:

| Old Selector (TinyMCE) | New Selector (Tiptap) | Purpose |
|------------------------|----------------------|---------|
| `.tox-tinymce` | `.mantine-RichTextEditor-root` | Editor container |
| `.tox-edit-area` | `.ProseMirror` | Content area |
| `.tox-toolbar` | `.mantine-RichTextEditor-toolbar` | Toolbar |
| `.tox-toolbar-button` | `.mantine-RichTextEditor-control` | Toolbar buttons |

**Example Update**:
```typescript
// OLD:
await page.locator('.tox-tinymce').waitFor();
await page.locator('.tox-edit-area').fill('Event description');

// NEW:
await page.locator('.mantine-RichTextEditor-root').waitFor();
await page.locator('.ProseMirror').fill('Event description');
```

**Verification**:
```bash
# Search for old selectors
grep -r "tox-" /home/chad/repos/witchcityrope/apps/web/tests/playwright/
# Should output: (nothing)

# Test compilation
npx tsc --noEmit

# Run specific test
npm run test:e2e -- events-management-e2e.spec.ts
```

**Success Criteria**:
- No .tox- selectors remaining
- Tests compile without errors
- events-management-e2e.spec.ts passes

### Step 4.3: Create New Tiptap Editor E2E Tests (1-2 hours)

**See [testing-migration-guide.md](./testing-migration-guide.md) for complete test code**.

Create: `/apps/web/tests/playwright/tiptap-editor.spec.ts`

**Test Coverage Required**:
1. ✅ Editor renders correctly
2. ✅ Text input and formatting works
3. ✅ Variable insertion autocomplete ({{fieldName}})
4. ✅ Form integration and value updates
5. ✅ Toolbar buttons functional
6. ✅ Programmatic content updates

**Verification**:
```bash
# Run new test
npm run test:e2e -- tiptap-editor.spec.ts

# Should see:
# ✓ Editor renders with correct classes
# ✓ Text input and formatting works
# ✓ Variable insertion autocomplete appears
# ✓ Form integration updates values
# ✓ Toolbar buttons apply formatting
# ✓ Programmatic updates work
```

**Success Criteria**:
- tiptap-editor.spec.ts created
- All 6 tests passing
- No console errors during tests

### Step 4.4: Run Full Test Suite (30 minutes)

```bash
# Run all E2E tests
npm run test:e2e

# Compare results to baseline
echo "Baseline: X/268 tests (Y%)"
echo "Current:  A/268 tests (B%)"

# Document any new failures
# (Should be same or better pass rate)
```

**Expected Results**:
- TinyMCE tests removed: -4 tests
- New Tiptap tests added: +1 test file (6 tests)
- Net change: +2 tests
- Pass rate: ≥ baseline (should be better due to no quota issues)

**Success Criteria**:
- All tests complete without crashes
- Pass rate ≥ baseline
- No TinyMCE-related failures
- Tiptap tests all passing

### Step 4.5: Commit Test Updates (15 minutes)

```bash
# Stage all changes
git add .

# Commit
git commit -m "$(cat <<'EOF'
test: Update E2E tests for @mantine/tiptap migration

Changes:
- Delete 4 TinyMCE-specific E2E tests
- Update selectors in events-management-e2e.spec.ts
- Add comprehensive Tiptap editor test suite

New test coverage:
- Editor rendering and initialization
- Text input and formatting
- Variable insertion autocomplete
- Form integration
- Toolbar functionality
- Programmatic content updates

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"

# Verify commit
git log -1 --oneline
```

**Success Criteria**:
- All test changes committed
- Commit message descriptive
- Git log shows commit

### Phase 4 Completion Checklist
- [ ] 4 TinyMCE tests deleted
- [ ] events-management-e2e.spec.ts selectors updated
- [ ] New tiptap-editor.spec.ts created
- [ ] All new tests passing
- [ ] Full test suite run (pass rate ≥ baseline)
- [ ] No TinyMCE-related failures
- [ ] Changes committed to feature branch

**Estimated Time**: 2-3 hours
**Next Phase**: [Phase 5: Documentation & Cleanup](#phase-5-documentation--cleanup-1-2-hours)

---

## Phase 5: Documentation & Cleanup (1-2 hours)

### Objective
Archive old documentation, update project files, and prepare for merge.

### Step 5.1: Archive TinyMCE Documentation (15 minutes)

```bash
# Create archive directory
mkdir -p /home/chad/repos/witchcityrope/docs/_archive/tinymce-legacy-2025-10-08

# Move TinyMCE setup guide
mv /home/chad/repos/witchcityrope/docs/guides-setup/tinymce-api-key-setup.md \
   /home/chad/repos/witchcityrope/docs/_archive/tinymce-legacy-2025-10-08/

# Create archive README
cat > /home/chad/repos/witchcityrope/docs/_archive/tinymce-legacy-2025-10-08/README.md << 'EOF'
# TinyMCE Legacy Documentation
<!-- Archived: 2025-10-08 -->
<!-- Reason: Migrated to @mantine/tiptap -->

## Archive Notice
This documentation is preserved for historical reference only.

**Migration Date**: October 8, 2025
**Replaced By**: @mantine/tiptap rich text editor
**Migration Docs**: `/docs/functional-areas/html-editor-migration/`

## Why Archived
TinyMCE was replaced due to:
- API key management complexity
- Testing quota issues causing E2E test failures
- Fallback workaround unacceptable for production
- Bundle size (TinyMCE larger than alternatives)

## Replacement Solution
@mantine/tiptap provides:
- 100% client-side (no API keys)
- Mantine theme integration
- Same feature set
- Better testing compatibility
- ~70% smaller bundle size

See migration documentation for complete details.
EOF

# Verify archive
ls -la /home/chad/repos/witchcityrope/docs/_archive/tinymce-legacy-2025-10-08/
```

**Success Criteria**:
- Archive directory created
- tinymce-api-key-setup.md moved
- Archive README.md created
- Original location empty

### Step 5.2: Update PROGRESS.md (30 minutes)

**File**: `/home/chad/repos/witchcityrope/PROGRESS.md`

Add migration summary:

```markdown
## 🎉 MIGRATION COMPLETE (2025-10-08): TinyMCE to @mantine/tiptap

**EDITOR MIGRATION**: Successfully migrated rich text editor from TinyMCE to @mantine/tiptap

**Migration Achievements**:
- ✅ MantineTiptapEditor component created with variable insertion support
- ✅ EventForm updated to use new component
- ✅ All TinyMCE components and tests removed
- ✅ Configuration cleaned (no API keys required)
- ✅ E2E tests updated and passing
- ✅ Bundle size reduced ~70%

**Technical Benefits**:
- **No API Keys**: 100% client-side implementation
- **No Quotas**: Eliminated testing quota failures
- **Mantine Integration**: Automatic theme and styling
- **Smaller Bundle**: ~155KB vs ~500KB+ (TinyMCE)
- **Test-Friendly**: Works seamlessly with Playwright

**Business Benefits**:
- No API key management overhead
- No testing infrastructure issues
- Same rich text editing functionality
- Better developer experience

**Migration Timeline**: 2.5 days (October 6-8, 2025)
**Commits**: feature/migrate-tinymce-to-tiptap (4 commits)
**Documentation**: `/docs/functional-areas/html-editor-migration/`

**Status**: ✅ **COMPLETE** - Production ready
```

**Verification**:
```bash
# Check PROGRESS.md syntax
grep -A 20 "TinyMCE to @mantine/tiptap" /home/chad/repos/witchcityrope/PROGRESS.md
```

**Success Criteria**:
- PROGRESS.md updated with migration summary
- Achievements and benefits documented
- Timeline and commits referenced

### Step 5.3: Update File Registry (15 minutes)

**File**: `/docs/architecture/file-registry.md`

Add registry entries:

```markdown
| 2025-10-08 | /apps/web/src/components/forms/MantineTiptapEditor.tsx | CREATED | Drop-in replacement for TinyMCERichTextEditor with variable insertion support | TinyMCE to Tiptap migration | ACTIVE | Never |
| 2025-10-08 | /apps/web/src/components/forms/TinyMCERichTextEditor.tsx | DELETED | Replaced by MantineTiptapEditor component | TinyMCE to Tiptap migration | DELETED | N/A |
| 2025-10-08 | /apps/web/src/components/forms/SimpleTinyMCE.tsx | DELETED | Test component no longer needed | TinyMCE to Tiptap migration | DELETED | N/A |
| 2025-10-08 | /apps/web/src/pages/TestTinyMCE.tsx | DELETED | Demo page no longer needed | TinyMCE to Tiptap migration | DELETED | N/A |
| 2025-10-08 | /apps/web/src/pages/TinyMCETest.tsx | DELETED | Demo page no longer needed | TinyMCE to Tiptap migration | DELETED | N/A |
| 2025-10-08 | /apps/web/tests/playwright/tinymce-visual-verification.spec.ts | DELETED | TinyMCE-specific test no longer relevant | TinyMCE to Tiptap migration | DELETED | N/A |
| 2025-10-08 | /apps/web/tests/playwright/tinymce-editor.spec.ts | DELETED | TinyMCE-specific test no longer relevant | TinyMCE to Tiptap migration | DELETED | N/A |
| 2025-10-08 | /apps/web/tests/playwright/tinymce-debug.spec.ts | DELETED | TinyMCE-specific test no longer relevant | TinyMCE to Tiptap migration | DELETED | N/A |
| 2025-10-08 | /apps/web/tests/playwright/tinymce-basic-check.spec.ts | DELETED | TinyMCE-specific test no longer relevant | TinyMCE to Tiptap migration | DELETED | N/A |
| 2025-10-08 | /apps/web/tests/playwright/tiptap-editor.spec.ts | CREATED | Comprehensive Tiptap editor E2E tests | TinyMCE to Tiptap migration | ACTIVE | Never |
| 2025-10-08 | /docs/guides-setup/tinymce-api-key-setup.md | ARCHIVED | Moved to /docs/_archive/tinymce-legacy-2025-10-08/ | TinyMCE to Tiptap migration | ARCHIVED | N/A |
| 2025-10-08 | /docs/functional-areas/html-editor-migration/README.md | CREATED | Migration documentation hub | TinyMCE to Tiptap migration | ACTIVE | Never |
| 2025-10-08 | /docs/functional-areas/html-editor-migration/migration-plan.md | CREATED | Master migration plan with 5-phase breakdown | TinyMCE to Tiptap migration | ACTIVE | Never |
| 2025-10-08 | /docs/functional-areas/html-editor-migration/component-implementation-guide.md | CREATED | Complete component implementation code and guide | TinyMCE to Tiptap migration | ACTIVE | Never |
| 2025-10-08 | /docs/functional-areas/html-editor-migration/testing-migration-guide.md | CREATED | Test suite updates and selector mappings | TinyMCE to Tiptap migration | ACTIVE | Never |
| 2025-10-08 | /docs/functional-areas/html-editor-migration/configuration-cleanup-guide.md | CREATED | Configuration file cleanup instructions | TinyMCE to Tiptap migration | ACTIVE | Never |
| 2025-10-08 | /docs/functional-areas/html-editor-migration/rollback-plan.md | CREATED | Emergency rollback procedures | TinyMCE to Tiptap migration | ACTIVE | Never |
```

**Success Criteria**:
- All file changes logged
- Registry entries complete
- Status and cleanup dates accurate

### Step 5.4: Update Functional Area Master Index (15 minutes)

**File**: `/docs/architecture/functional-area-master-index.md`

Add entry:

```markdown
| **HTML Editor Migration** | `/docs/functional-areas/html-editor-migration/` | **MIGRATION COMPLETE** ✅ | **TinyMCE to @mantine/tiptap migration** - Comprehensive documentation for migrating from TinyMCE to @mantine/tiptap, eliminating API key requirements and testing quota issues. Includes 5-phase migration plan, component implementation guide, testing migration guide, configuration cleanup guide, and rollback procedures. | **COMPLETE** | 2025-10-08 |
```

**Success Criteria**:
- Master index updated
- Entry follows standard format
- Status marked COMPLETE

### Step 5.5: Final Commit (15 minutes)

```bash
# Stage all changes
git add .

# Final commit
git commit -m "$(cat <<'EOF'
docs: Complete TinyMCE to @mantine/tiptap migration

Final cleanup and documentation:
- Archive tinymce-api-key-setup.md to _archive/
- Update PROGRESS.md with migration summary
- Update file registry with all changes
- Update functional area master index

Migration successfully completed:
- 0 API keys required
- 0 testing quota issues
- ~70% smaller bundle size
- Same functionality preserved
- All tests passing

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"

# Verify commit
git log --oneline -5
```

**Success Criteria**:
- All documentation changes committed
- Commit message complete
- Ready for merge

### Step 5.6: Push and Create Pull Request (15 minutes)

```bash
# Push feature branch
git push -u origin feature/migrate-tinymce-to-tiptap

# Create PR using GitHub CLI (if available)
gh pr create \
  --title "feat: Migrate from TinyMCE to @mantine/tiptap" \
  --body "$(cat <<'EOF'
## Summary
Migrates WitchCityRope from TinyMCE rich text editor to @mantine/tiptap, eliminating API key management and testing quota issues.

## Changes
- ✅ Created MantineTiptapEditor component with variable insertion support
- ✅ Updated EventForm to use new component
- ✅ Removed all TinyMCE components and configuration
- ✅ Updated E2E tests with new selectors
- ✅ Archived legacy documentation

## Benefits
- **No API Keys**: 100% client-side implementation
- **No Quotas**: Eliminated testing failures
- **Smaller Bundle**: ~70% size reduction
- **Mantine Integration**: Automatic theming
- **Test-Friendly**: Works seamlessly with Playwright

## Testing
- TypeScript compilation: 0 errors
- Build: ✅ Successful
- E2E tests: ≥ baseline pass rate
- Manual testing: ✅ Verified

## Documentation
Complete migration documentation: `/docs/functional-areas/html-editor-migration/`

## Rollback Plan
If issues arise, see: `/docs/functional-areas/html-editor-migration/rollback-plan.md`

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)" \
  --base main

# OR create PR manually on GitHub
echo "PR URL: https://github.com/DarkMonkDev/WitchCityRope/compare/main...feature/migrate-tinymce-to-tiptap"
```

**Success Criteria**:
- Branch pushed to remote
- Pull request created
- PR description complete

### Phase 5 Completion Checklist
- [ ] TinyMCE documentation archived
- [ ] PROGRESS.md updated with migration summary
- [ ] File registry updated with all changes
- [ ] Functional area master index updated
- [ ] Final commit completed
- [ ] Branch pushed to remote
- [ ] Pull request created

**Estimated Time**: 1-2 hours
**Status**: **MIGRATION COMPLETE** 🎉

---

## Success Criteria

### Technical Success Metrics
- [ ] TypeScript compilation: 0 errors
- [ ] Build completes successfully
- [ ] E2E test pass rate ≥ baseline
- [ ] No TinyMCE dependencies in package.json
- [ ] No TinyMCE configuration in .env files
- [ ] No .tox- selectors in test files
- [ ] Manual testing confirms editor works

### Business Success Metrics
- [ ] No API key management required
- [ ] No testing quota failures
- [ ] Same user experience maintained
- [ ] Variable insertion {{fieldName}} working
- [ ] Production deployment ready
- [ ] Team documentation complete

### Code Quality Metrics
- [ ] All TypeScript types correct
- [ ] No console errors in browser
- [ ] No unused imports
- [ ] Proper error handling
- [ ] Mantine theme integration working
- [ ] Form integration functional

### Documentation Success Metrics
- [ ] Migration plan complete
- [ ] Component guide complete
- [ ] Testing guide complete
- [ ] Configuration guide complete
- [ ] Rollback plan complete
- [ ] File registry updated
- [ ] PROGRESS.md updated

---

## Risk Mitigation

### Identified Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Variable insertion breaks** | Low | High | Comprehensive testing before deployment |
| **Form integration issues** | Low | Medium | Test with @mantine/form before rollout |
| **TypeScript type errors** | Low | Medium | Use generated Tiptap types |
| **E2E test failures** | Low | Medium | Update selectors systematically |
| **Bundle size increase** | Very Low | Low | Tiptap is smaller than TinyMCE |
| **User experience regression** | Low | High | Manual testing before production |

### Mitigation Strategies

1. **Comprehensive Testing**
   - Unit tests for component
   - E2E tests for integration
   - Manual testing before merge

2. **Gradual Rollout**
   - Feature branch development
   - Staging environment testing
   - Production deployment after verification

3. **Full Rollback Capability**
   - Complete rollback plan documented
   - Git revert procedures tested
   - Team trained on rollback process

4. **Team Communication**
   - Daily standup updates
   - Documentation shared early
   - Questions addressed promptly

---

## Troubleshooting Guide

### Common Issues and Solutions

#### Issue: TypeScript Errors After Component Creation
**Symptoms**: `Cannot find module '@mantine/tiptap'`

**Solution**:
```bash
# Verify package installed
npm list @mantine/tiptap

# If not installed
npm install @mantine/tiptap @tiptap/react @tiptap/starter-kit

# Clear cache and rebuild
rm -rf node_modules package-lock.json
npm install
```

#### Issue: Variable Insertion Not Working
**Symptoms**: Typing `{{` doesn't show autocomplete

**Solution**:
- Check custom extension code matches guide
- Verify suggestion list implementation
- Test with simple variable array first
- Check browser console for errors

#### Issue: Editor Not Rendering
**Symptoms**: Blank space where editor should be

**Solution**:
```bash
# Check import paths
grep "MantineTiptapEditor" apps/web/src/components/events/EventForm.tsx

# Verify component exports
grep "export" apps/web/src/components/forms/MantineTiptapEditor.tsx

# Check browser console for errors
# Open DevTools → Console
```

#### Issue: Tests Failing After Selector Updates
**Symptoms**: E2E tests timeout waiting for elements

**Solution**:
- Verify new selectors in browser DevTools
- Add explicit waits: `await page.waitForSelector('.mantine-RichTextEditor-root')`
- Check element visibility: `await page.locator('.ProseMirror').isVisible()`
- Increase timeout if needed: `{ timeout: 10000 }`

#### Issue: Form Values Not Updating
**Symptoms**: Editor changes don't save

**Solution**:
```typescript
// Verify onChange prop connected
<MantineTiptapEditor
  value={form.values.fullDescription}
  onChange={(content) => {
    console.log('Editor changed:', content); // Debug log
    form.setFieldValue('fullDescription', content);
  }}
/>
```

#### Issue: Build Fails After Package Removal
**Symptoms**: `Module not found: @tinymce/tinymce-react`

**Solution**:
```bash
# Find all TinyMCE imports
grep -r "@tinymce" apps/web/src/

# Remove or update imports
# Then rebuild
npm run build
```

---

## Rollback Procedures

**CRITICAL**: If migration fails, see [rollback-plan.md](./rollback-plan.md) for complete procedures.

### Quick Rollback (< 1 hour)

```bash
# 1. Switch to main branch
git checkout main

# 2. Verify tests pass
npm run test:e2e

# 3. Delete feature branch (if needed)
git branch -D feature/migrate-tinymce-to-tiptap

# 4. Notify team
echo "Migration rolled back - using TinyMCE until further notice"
```

### When to Rollback

Abort migration if:
- [ ] TypeScript errors can't be resolved within 2 hours
- [ ] E2E test pass rate drops > 10% from baseline
- [ ] Critical functionality breaks (variable insertion, form save)
- [ ] Production deadline < 1 day away
- [ ] Unexpected technical blockers discovered

---

## Timeline Summary

| Phase | Duration | Cumulative |
|-------|----------|------------|
| **Phase 1: Preparation** | 30-60 min | 1 hour |
| **Phase 2: Component Migration** | 4-6 hours | 7 hours |
| **Phase 3: Configuration Cleanup** | 1-2 hours | 9 hours |
| **Phase 4: Test Suite Updates** | 2-3 hours | 12 hours |
| **Phase 5: Documentation & Cleanup** | 1-2 hours | 14 hours |
| **TOTAL** | **18-25 hours** | **2-3 business days** |

**Recommended Schedule**:
- **Day 1**: Phases 1-2 (Preparation + Component Migration)
- **Day 2**: Phases 3-4 (Configuration + Testing)
- **Day 3**: Phase 5 (Documentation + PR)

---

## Next Steps After Migration

### Immediate (Week 1)
1. **Monitor Production**: Watch for any editor-related issues
2. **Gather Feedback**: Ask users about rich text editing experience
3. **Performance Testing**: Verify bundle size reduction in production
4. **Documentation Review**: Ensure all docs accurate

### Short-term (Month 1)
1. **Enhancement Opportunities**: Identify additional Tiptap features to use
2. **Custom Extensions**: Consider building more custom extensions
3. **Team Training**: Share lessons learned with team
4. **Process Improvement**: Update migration templates based on experience

### Long-term (Quarter 1)
1. **Feature Exploration**: Investigate Tiptap collaboration features
2. **Performance Optimization**: Fine-tune editor configuration
3. **Accessibility Audit**: Verify WCAG compliance
4. **Documentation Maintenance**: Keep migration docs updated

---

## Related Documentation

- **README.md**: [Migration overview and navigation](./README.md)
- **Component Guide**: [component-implementation-guide.md](./component-implementation-guide.md)
- **Testing Guide**: [testing-migration-guide.md](./testing-migration-guide.md)
- **Configuration Guide**: [configuration-cleanup-guide.md](./configuration-cleanup-guide.md)
- **Rollback Plan**: [rollback-plan.md](./rollback-plan.md)

- **TinyMCE Alternatives Research**: `/docs/architecture/research/2025-10-07-html-editor-alternatives-testing-focus.md`
- **Tiptap Comparison**: `/docs/architecture/research/2025-10-07-tiptap-comparison-deep-dive.md`

---

## Version History
- **v1.0** (2025-10-08): Initial migration plan created

---

**IMPORTANT**: This migration plan is comprehensive but flexible. Adjust timeline and procedures based on your team's specific needs and constraints. The rollback plan provides safety if issues arise.

**Questions or Issues**: Consult [rollback-plan.md](./rollback-plan.md) or escalate to project lead.
