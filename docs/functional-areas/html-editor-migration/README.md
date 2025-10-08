# TinyMCE to @mantine/tiptap Migration Documentation
<!-- Last Updated: 2025-10-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: Active -->

## Purpose
This functional area contains comprehensive documentation for migrating WitchCityRope from TinyMCE to @mantine/tiptap rich text editor. This migration eliminates testing quota issues while maintaining all functionality.

## Problem Statement
TinyMCE requires an API key and enforces usage quotas that cause test failures during E2E testing. The current workaround (fallback to Textarea when no API key) is not sustainable for production.

## Solution
Migrate to @mantine/tiptap, which is:
- **100% client-side** (no API keys or quotas)
- **Mantine-integrated** (automatic theming and styling)
- **Feature-complete** (supports all current TinyMCE features)
- **Test-friendly** (works seamlessly with Playwright)
- **Smaller bundle** (~69-91% smaller than TinyMCE)

## Migration Documents

### 🎯 Start Here: Master Migration Plan
**File**: [migration-plan.md](./migration-plan.md)

**What it contains**:
- Executive summary (one-page overview)
- Complete 5-phase migration breakdown
- Prerequisites and preparation steps
- Success criteria and verification
- Estimated timeline (2-3 days)
- Risk mitigation strategies
- Rollback procedures

**Who needs this**: Project managers, team leads, anyone executing the migration

---

### 💻 Component Implementation Guide
**File**: [component-implementation-guide.md](./component-implementation-guide.md)

**What it contains**:
- Complete `MantineTiptapEditor.tsx` component code (copy-paste ready)
- Props interface matching existing TinyMCERichTextEditor
- Variable insertion custom extension ({{fieldName}} syntax)
- Usage examples and integration patterns
- Testing instructions and verification
- Troubleshooting common issues

**Who needs this**: React developers, frontend implementers

---

### 🧪 Testing Migration Guide
**File**: [testing-migration-guide.md](./testing-migration-guide.md)

**What it contains**:
- 4 TinyMCE-specific E2E tests to delete (with file paths)
- Selector updates for events-management-e2e.spec.ts
- New Tiptap editor E2E test suite code
- Test data and verification steps
- Before/after baseline comparison
- Old vs new selector mappings

**Who needs this**: Test developers, QA engineers

---

### ⚙️ Configuration Cleanup Guide
**File**: [configuration-cleanup-guide.md](./configuration-cleanup-guide.md)

**What it contains**:
- Line-by-line changes for 5 configuration files
- Environment variable removal instructions
- Package.json dependency updates
- npm install procedures
- Verification steps for each change
- Before/after configuration comparisons

**Who needs this**: DevOps engineers, configuration managers

---

### 🔄 Rollback Plan
**File**: [rollback-plan.md](./rollback-plan.md)

**What it contains**:
- When to abort migration (decision criteria)
- Git commands to revert all changes
- Package restoration procedures
- Test verification after rollback
- Lessons learned documentation template

**Who needs this**: Project managers, anyone executing migration (safety net)

---

## Migration Timeline Summary

| Phase | Description | Duration | Deliverables |
|-------|-------------|----------|--------------|
| **Phase 1** | Preparation & Setup | 30-60 min | Baseline tests, feature branch |
| **Phase 2** | Component Migration | 4-6 hours | New component, EventForm updated |
| **Phase 3** | Configuration Cleanup | 1-2 hours | Config files cleaned, dependencies updated |
| **Phase 4** | Test Suite Updates | 2-3 hours | New tests, old tests deleted |
| **Phase 5** | Documentation & Cleanup | 1-2 hours | Docs archived, PROGRESS.md updated |
| **TOTAL** | | **2-3 days** | Production-ready migration |

## Current State Analysis

### Components Using TinyMCE
1. `/apps/web/src/components/forms/TinyMCERichTextEditor.tsx` - Primary reusable component
2. `/apps/web/src/components/forms/SimpleTinyMCE.tsx` - Test/demo component
3. `/apps/web/src/components/events/EventForm.tsx` - Inline TinyMCE usage (lines 22, 92-93)

### Configuration Files Affected
1. `/apps/web/src/config/environment.ts` - Lines 45, 86 (tinyMceApiKey)
2. `/apps/web/.env.example` - Lines 14-15 (VITE_TINYMCE_API_KEY)
3. `/apps/web/.env.staging` - Contains VITE_TINYMCE_API_KEY
4. `/apps/web/.env.template` - Contains VITE_TINYMCE_API_KEY
5. `/apps/web/package.json` - Line 35: "@tinymce/tinymce-react": "^6.3.0"

### Tests to Update/Delete
**Delete** (4 TinyMCE-specific E2E tests):
1. `/apps/web/tests/playwright/tinymce-visual-verification.spec.ts`
2. `/apps/web/tests/playwright/tinymce-editor.spec.ts`
3. `/apps/web/tests/playwright/tinymce-debug.spec.ts`
4. `/apps/web/tests/playwright/tinymce-basic-check.spec.ts`

**Update** (selector changes):
1. `/apps/web/tests/playwright/events-management-e2e.spec.ts`

### Test/Demo Pages to Delete
1. `/apps/web/src/pages/TestTinyMCE.tsx`
2. `/apps/web/src/pages/TinyMCETest.tsx`

### Documentation to Archive
1. `/docs/guides-setup/tinymce-api-key-setup.md` → Move to `/docs/_archive/`

## Package Dependencies

**Current TinyMCE**:
- `"@tinymce/tinymce-react": "^6.3.0"` - **REMOVE**

**Already Installed** (@mantine/tiptap ecosystem):
- `"@mantine/tiptap": "^7.17.8"` ✅
- `"@tiptap/extension-highlight": "^3.3.0"` ✅
- `"@tiptap/extension-link": "^3.3.0"` ✅
- `"@tiptap/extension-subscript": "^3.3.0"` ✅
- `"@tiptap/extension-superscript": "^3.3.0"` ✅
- `"@tiptap/extension-text-align": "^3.3.0"` ✅
- `"@tiptap/extension-underline": "^3.3.0"` ✅
- `"@tiptap/react": "^3.3.0"` ✅
- `"@tiptap/starter-kit": "^3.3.0"` ✅

**Status**: All Tiptap dependencies already installed - no new packages needed!

## Success Criteria

### Technical Success
- [ ] MantineTiptapEditor component created and working
- [ ] Variable insertion custom extension functional
- [ ] EventForm using new component
- [ ] All TinyMCE components deleted
- [ ] All TinyMCE tests deleted/updated
- [ ] All config files cleaned
- [ ] TypeScript compilation: 0 errors
- [ ] E2E tests passing (baseline or better)

### Business Success
- [ ] No API key management required
- [ ] No testing quota issues
- [ ] No fallback workarounds needed
- [ ] Same or better user experience
- [ ] Production deployment ready

## Migration Readiness Checklist

Before starting migration, verify:
- [ ] Docker environment running (`./dev.sh`)
- [ ] Current tests passing (establish baseline)
- [ ] Git working directory clean
- [ ] All team members aware of migration
- [ ] Backup branch created
- [ ] Time allocated (2-3 days minimum)

## Questions and Support

### Common Questions
- **Will this break existing content?** No - HTML content is preserved
- **Do we lose any features?** No - all features maintained
- **Can we rollback?** Yes - comprehensive rollback plan included
- **How long will it take?** 2-3 days for complete migration
- **Will tests pass immediately?** Most tests yes, some require selector updates

### Getting Help
- **Migration issues**: See [rollback-plan.md](./rollback-plan.md)
- **Component issues**: See [component-implementation-guide.md](./component-implementation-guide.md)
- **Test issues**: See [testing-migration-guide.md](./testing-migration-guide.md)
- **Config issues**: See [configuration-cleanup-guide.md](./configuration-cleanup-guide.md)

## Related Documentation
- **TinyMCE Alternatives Research**: `/docs/architecture/research/2025-10-07-html-editor-alternatives-testing-focus.md`
- **Tiptap Deep Dive Comparison**: `/docs/architecture/research/2025-10-07-tiptap-comparison-deep-dive.md`
- **Original TinyMCE Setup Guide**: `/docs/guides-setup/tinymce-api-key-setup.md` (to be archived)

## Version History
- **v1.0** (2025-10-08): Initial migration documentation created
