# Rollback Plan: TinyMCE to @mantine/tiptap Migration
<!-- Last Updated: 2025-10-08 -->
<!-- Version: 1.0 -->
<!-- Owner: Migration Team / DevOps -->
<!-- Status: Active -->

## Purpose
This document provides emergency rollback procedures if the TinyMCE to @mantine/tiptap migration encounters critical issues that cannot be resolved quickly.

## When to Execute Rollback

### Rollback Decision Criteria

Execute rollback immediately if ANY of these conditions occur:

| Condition | Severity | Rollback? |
|-----------|----------|-----------|
| **TypeScript errors > 2 hours unresolved** | Critical | ✅ YES |
| **E2E test pass rate drops > 10%** | Critical | ✅ YES |
| **Variable insertion completely broken** | Critical | ✅ YES |
| **Form save functionality broken** | Critical | ✅ YES |
| **Production deployment < 1 day away** | High | ✅ YES |
| **Customer-reported issues** | High | ✅ YES |
| **Performance degradation > 200ms** | Medium | ⚠️ CONSIDER |
| **Styling issues only** | Low | ❌ NO - Fix forward |
| **Minor test failures (< 5%)** | Low | ❌ NO - Fix forward |

### Rollback Authority

**Who can decide to rollback:**
- Project Lead
- Technical Lead
- Senior Developer (if leads unavailable)

**Who CANNOT rollback without approval:**
- Junior developers
- QA engineers
- Individual contributors

### Rollback Communication

**BEFORE rolling back**:
1. Notify project lead
2. Document reason in rollback log (see template below)
3. Alert team via Slack/email
4. Create rollback issue in issue tracker

**AFTER rolling back**:
1. Post-mortem meeting (within 24 hours)
2. Update lessons learned
3. Document blockers for future attempts
4. Plan resolution timeline

---

## Quick Rollback (< 1 hour)

### Prerequisites

- Git working directory clean (or stash changes)
- Access to main branch
- Docker running (for verification)

### Step-by-Step Rollback

#### Step 1: Switch to Main Branch (2 minutes)

```bash
# Save any work in progress
git stash push -m "WIP migration work before rollback"

# Switch to main branch
git checkout main

# Pull latest (ensure up to date)
git pull origin main

# Verify on main
git branch --show-current
# Output should be: main
```

**Success Criteria**:
- On `main` branch
- Working directory clean
- Latest code pulled

#### Step 2: Verify Main Branch State (5 minutes)

```bash
# Check TinyMCE files exist
ls /home/chad/repos/witchcityrope/apps/web/src/components/forms/TinyMCERichTextEditor.tsx
# Should exist

# Check package.json has TinyMCE
grep "@tinymce/tinymce-react" /home/chad/repos/witchcityrope/apps/web/package.json
# Should show: "@tinymce/tinymce-react": "^6.3.0"

# Check environment.ts has tinyMceApiKey
grep "tinyMceApiKey" /home/chad/repos/witchcityrope/apps/web/src/config/environment.ts
# Should show config property
```

**Success Criteria**:
- All TinyMCE files present
- Configuration intact
- Dependencies correct

#### Step 3: Reinstall Dependencies (5-10 minutes)

```bash
cd /home/chad/repos/witchcityrope/apps/web

# Clean install to ensure package-lock.json matches
rm -rf node_modules package-lock.json
npm install

# Verify TinyMCE installed
npm list @tinymce/tinymce-react
# Should show: @tinymce/tinymce-react@6.3.0
```

**Success Criteria**:
- npm install completes without errors
- @tinymce/tinymce-react installed
- All other dependencies intact

#### Step 4: Verify TypeScript Compilation (2 minutes)

```bash
# Check TypeScript
npx tsc --noEmit

# Expected: 0 errors (or baseline error count)
```

**Success Criteria**:
- TypeScript compiles
- Error count matches pre-migration baseline

#### Step 5: Build Verification (3-5 minutes)

```bash
# Build production bundle
npm run build

# Expected: Build succeeds
```

**Success Criteria**:
- Build completes successfully
- No breaking errors

#### Step 6: Start Development Environment (5 minutes)

```bash
# Start Docker containers
./dev.sh

# Wait for healthy status
docker ps

# Navigate to app
# http://localhost:5174
```

**Success Criteria**:
- Containers start successfully
- App loads in browser
- No console errors

#### Step 7: Manual Verification (5-10 minutes)

**Test Checklist**:

1. **Navigate to Event Creation**
   - [ ] http://localhost:5174/admin/events
   - [ ] Click "Create Event"
   - [ ] TinyMCE editor renders

2. **Test Editor Functionality**
   - [ ] Can type text
   - [ ] Bold/italic buttons work
   - [ ] Can save event with description
   - [ ] Saved content loads correctly

3. **Verify Fallback Pattern Works**
   - [ ] Without API key: Textarea fallback shows
   - [ ] With API key: TinyMCE loads

**Success Criteria**:
- All manual tests pass
- Editor functional
- No regressions

#### Step 8: Run E2E Tests (15-20 minutes)

```bash
# Run full E2E test suite
npm run test:e2e

# Compare to pre-migration baseline
echo "Expected pass rate: X%"
echo "Current pass rate: Y%"
```

**Success Criteria**:
- Pass rate ≥ pre-migration baseline
- No new test failures
- System stable

#### Step 9: Cleanup Migration Branch (2 minutes)

```bash
# Delete local feature branch (if safe)
git branch -D feature/migrate-tinymce-to-tiptap

# Delete remote branch (if pushed)
git push origin --delete feature/migrate-tinymce-to-tiptap
```

**Success Criteria**:
- Migration branch deleted
- No orphaned work
- Clean repository state

---

## Rollback Verification Checklist

**ALL must be TRUE before considering rollback complete**:

### Technical Verification
- [ ] On `main` branch
- [ ] TinyMCERichTextEditor.tsx exists
- [ ] EventForm uses TinyMCE
- [ ] @tinymce/tinymce-react in package.json
- [ ] tinyMceApiKey in environment.ts
- [ ] VITE_TINYMCE_API_KEY in .env files
- [ ] TypeScript compiles (0 errors or baseline)
- [ ] Build succeeds
- [ ] Docker containers healthy

### Functional Verification
- [ ] App loads in browser
- [ ] TinyMCE editor renders
- [ ] Can create event with description
- [ ] Can save and load content
- [ ] Fallback pattern works (no API key = Textarea)
- [ ] E2E tests pass (≥ baseline)

### Operational Verification
- [ ] Team notified of rollback
- [ ] Rollback documented (see template below)
- [ ] Post-mortem scheduled
- [ ] Lessons learned captured

---

## Rollback Documentation Template

### Rollback Log Entry

**Date**: YYYY-MM-DD HH:MM (ISO format)
**Rollback By**: [Name]
**Authority**: [Project Lead / Technical Lead]
**Migration Commit**: [Feature branch last commit hash]
**Rolled Back To**: [Main branch commit hash]

**Reason for Rollback**:
- [ ] TypeScript errors unresolved
- [ ] Test failures unacceptable
- [ ] Critical functionality broken
- [ ] Timeline pressure
- [ ] Other: _______________

**Description**:
[Detailed explanation of what went wrong and why rollback was necessary]

**Attempts to Fix Before Rollback**:
1. [What was tried]
2. [What was tried]
3. [Why fixes failed]

**Blockers Identified**:
- **Technical**: [List technical blockers]
- **Process**: [List process blockers]
- **Resource**: [List resource constraints]

**Impact Assessment**:
- **Development Time Lost**: [Hours]
- **Migration Work Salvageable**: [Yes/No - What can be reused]
- **Team Morale**: [Impact on team]

**Post-Mortem Scheduled**:
- **Date**: YYYY-MM-DD
- **Attendees**: [List]
- **Agenda**: Root cause analysis, prevention measures

**Next Steps**:
1. [Action item 1]
2. [Action item 2]
3. [Timeline for retry or alternative]

**Lessons Learned**:
- [Key lesson 1]
- [Key lesson 2]
- [What to do differently next time]

**Approved By**: [Project Lead signature/confirmation]

---

## Partial Rollback Scenarios

### Scenario 1: Keep New Component, Revert Tests

**When**: Component works but tests are broken

**Action**:
```bash
# Keep MantineTiptapEditor.tsx but revert test changes
git checkout main -- apps/web/tests/playwright/

# Restore TinyMCE tests
git checkout main -- apps/web/tests/playwright/tinymce*.spec.ts

# Keep using old selectors in events-management-e2e.spec.ts
```

### Scenario 2: Keep Tests, Revert Component

**When**: Tests are fine but component has issues

**Action**:
```bash
# Revert component files
git checkout main -- apps/web/src/components/forms/TinyMCERichTextEditor.tsx
git checkout main -- apps/web/src/components/events/EventForm.tsx

# Delete new component
rm apps/web/src/components/forms/MantineTiptapEditor.tsx
```

### Scenario 3: Keep Everything, Restore TinyMCE Parallel

**When**: Want to try fixing Tiptap but need TinyMCE backup

**Action**:
```bash
# Restore TinyMCE files alongside Tiptap
git checkout main -- apps/web/src/components/forms/TinyMCERichTextEditor.tsx

# Update EventForm to use TinyMCE conditionally
# (requires code change to support both)
```

---

## Git Commands Reference

### View Migration Branch Commits

```bash
# See all commits in feature branch
git log feature/migrate-tinymce-to-tiptap --oneline

# See changes made
git diff main...feature/migrate-tinymce-to-tiptap
```

### Restore Specific Files

```bash
# Restore single file from main
git checkout main -- path/to/file.tsx

# Restore entire directory from main
git checkout main -- path/to/directory/
```

### Delete Feature Branch

```bash
# Local only
git branch -D feature/migrate-tinymce-to-tiptap

# Local + remote
git push origin --delete feature/migrate-tinymce-to-tiptap
```

### Create Rollback Commit

```bash
# If changes made during rollback
git add .
git commit -m "$(cat <<'EOF'
revert: Roll back TinyMCE to Tiptap migration

REASON: [Brief reason for rollback]

Rolled back from feature/migrate-tinymce-to-tiptap
back to main branch stable state.

See rollback documentation for full details.

Rollback approved by: [Name]
EOF
)"
```

---

## Post-Rollback Analysis

### Mandatory Post-Mortem Meeting

**Schedule within 24 hours of rollback**

**Attendees** (minimum):
- Project Lead
- Technical Lead
- Developer who performed migration
- QA Engineer

**Agenda**:
1. **Timeline Review** (15 min)
   - What happened when
   - When was rollback decided
   - How long did rollback take

2. **Root Cause Analysis** (30 min)
   - What specifically went wrong
   - Why wasn't it caught earlier
   - Were there warning signs

3. **Process Review** (15 min)
   - What process failed
   - What should have been done differently
   - What worked well

4. **Prevention Measures** (15 min)
   - How to prevent this specific issue
   - What guardrails to add
   - What documentation to improve

5. **Next Steps** (15 min)
   - Retry timeline (if appropriate)
   - Alternative approaches
   - Resource needs

### Lessons Learned Template

**Title**: TinyMCE to Tiptap Migration Rollback - [Date]

**Context**: [Brief description of migration and rollback]

**What Went Wrong**:
- **Technical Issue**: [Specific technical problem]
- **Process Issue**: [Process that failed]
- **Communication Issue**: [Communication breakdown]

**What We Learned**:
1. [Key lesson 1]
2. [Key lesson 2]
3. [Key lesson 3]

**Action Items**:
- [ ] [Preventive measure 1] - Assigned to: [Name] - Due: [Date]
- [ ] [Preventive measure 2] - Assigned to: [Name] - Due: [Date]
- [ ] [Documentation update] - Assigned to: [Name] - Due: [Date]

**Impact**: [How this lesson affects future work]

**Tags**: #rollback #migration #lessons-learned

---

## Prevention Strategies

### Before Next Migration Attempt

1. **Better Testing**
   - [ ] Test component in isolation first
   - [ ] Create comprehensive unit tests
   - [ ] Run E2E tests early and often
   - [ ] Set up automated testing in CI/CD

2. **Incremental Approach**
   - [ ] Start with smallest use case
   - [ ] Prove it works completely
   - [ ] Then expand to other areas
   - [ ] Don't migrate everything at once

3. **Better Documentation**
   - [ ] Document assumptions
   - [ ] List all dependencies
   - [ ] Create troubleshooting guides
   - [ ] Have rollback plan ready BEFORE starting

4. **Team Alignment**
   - [ ] Ensure team understands why migrating
   - [ ] Get buy-in from all stakeholders
   - [ ] Allocate sufficient time
   - [ ] Don't rush due to external pressure

5. **Technical Preparation**
   - [ ] Verify all dependencies available
   - [ ] Test in local environment first
   - [ ] Create prototype before full migration
   - [ ] Validate approach with team

---

## Rollback Success Criteria

### Migration is Successfully Rolled Back When:

**Technical**:
- ✅ All TinyMCE code restored
- ✅ All Tiptap code removed
- ✅ Dependencies correct
- ✅ Configuration restored
- ✅ TypeScript compiles
- ✅ Build succeeds
- ✅ Tests pass at baseline

**Functional**:
- ✅ App loads without errors
- ✅ TinyMCE editor works
- ✅ Can create/edit events
- ✅ Content saves and loads
- ✅ No user-facing issues

**Operational**:
- ✅ Team notified
- ✅ Rollback documented
- ✅ Post-mortem scheduled
- ✅ Lessons learned captured
- ✅ Repository clean

---

## Emergency Contacts

### Who to Contact During Rollback

**Technical Issues**:
- Primary: [Technical Lead Name]
- Backup: [Senior Developer Name]

**Process/Approval**:
- Primary: [Project Lead Name]
- Backup: [Product Owner Name]

**Infrastructure**:
- Primary: [DevOps Lead Name]
- Backup: [Infrastructure Engineer Name]

**After Hours**:
- Emergency Hotline: [Number]
- On-Call Rotation: [Link to schedule]

---

## Related Documentation

- **Migration Plan**: [migration-plan.md](./migration-plan.md) - Original migration plan
- **Component Guide**: [component-implementation-guide.md](./component-implementation-guide.md) - Component implementation
- **Testing Guide**: [testing-migration-guide.md](./testing-migration-guide.md) - Test migration steps
- **Configuration Guide**: [configuration-cleanup-guide.md](./configuration-cleanup-guide.md) - Config cleanup

---

## Appendix: Common Rollback Issues

### Issue: Can't Switch to Main (Uncommitted Changes)

**Error**: `error: Your local changes to the following files would be overwritten by checkout`

**Solution**:
```bash
# Stash changes
git stash push -m "Migration work before rollback"

# Now switch
git checkout main

# Later retrieve (if needed)
git stash list
git stash apply stash@{0}
```

### Issue: TinyMCE Files Missing After Checkout

**Error**: Files not restored from main

**Solution**:
```bash
# Force checkout from main
git checkout main -- apps/web/src/components/forms/TinyMCERichTextEditor.tsx

# Verify file exists
ls -la apps/web/src/components/forms/TinyMCERichTextEditor.tsx
```

### Issue: npm install Fails After Rollback

**Error**: Package installation errors

**Solution**:
```bash
# Nuclear option: completely clean and reinstall
rm -rf node_modules package-lock.json
npm cache clean --force
npm install
```

### Issue: Tests Still Failing After Rollback

**Error**: Baseline tests don't pass

**Diagnosis**:
1. Check if database state is corrupt
2. Verify Docker containers healthy
3. Check for environment variable issues

**Solution**:
```bash
# Reset Docker environment
docker-compose down -v  # Remove volumes
docker-compose up -d

# Wait for healthy state
docker ps

# Retry tests
npm run test:e2e
```

---

## Version History
- **v1.0** (2025-10-08): Initial rollback plan created

---

**REMEMBER**: Rollback is a safety mechanism, not a failure. Document, learn, and improve for next time.
