# Completed Work Archive
<!-- Last Updated: 2025-08-04 -->
<!-- Version: 1.0 -->
<!-- Owner: Project Management Team -->
<!-- Status: Active -->

## Purpose

This folder temporarily holds documentation from completed work efforts before they are committed to git and removed. This is NOT a permanent archive - git history serves that purpose.

## Process

### 1. When to Archive
Move files here when:
- A feature or enhancement is fully complete
- Current-state documentation has been updated
- Development history has been documented
- All temporary files need to be cleaned up

### 2. How to Archive

```bash
# Create a dated folder for your work
mkdir docs/completed-work-archive/2025-08-04-oauth-implementation

# Move completed work files
mv docs/functional-areas/authentication/new-work/* \
   docs/completed-work-archive/2025-08-04-oauth-implementation/

# Update the new-work/status.md to show no active development
echo "# No Active Development" > docs/functional-areas/authentication/new-work/status.md
echo "Last Updated: 2025-08-04" >> docs/functional-areas/authentication/new-work/status.md
```

### 3. Commit to Git

```bash
# Add all changes including archived files
git add -A

# Commit with clear message
git commit -m "Archive OAuth implementation docs - feature complete

- OAuth with Google implemented and tested
- Updated authentication business requirements
- Updated functional design with OAuth flow
- Archived implementation notes and temp files"
```

### 4. Remove Archived Files

```bash
# After successful commit, remove the archived files
rm -rf docs/completed-work-archive/2025-08-04-oauth-implementation

# Verify removal
git status  # Should show deletions
git commit -m "Remove archived OAuth docs - preserved in git history"
```

## What to Archive

### Always Archive
- Completed new-work/ contents
- Temporary test scripts
- Implementation notes
- Design iterations
- Meeting notes specific to the feature

### Never Archive
- Current state documentation (update in place)
- Lessons learned (add to role files)
- Test files that should be permanent
- Reusable scripts or tools

## Folder Naming Convention

```
YYYY-MM-DD-feature-name/
```

Examples:
- `2025-08-04-oauth-implementation/`
- `2025-08-15-payment-integration/`
- `2025-09-01-vetting-workflow-update/`

## Git Commit Messages

### Archive Commit
```
Archive [feature] docs - feature complete

- Brief summary of what was implemented
- Key files updated in current-state
- Note about what's in the archive
```

### Removal Commit
```
Remove archived [feature] docs - preserved in git history
```

## Recovery from Git

If you need to retrieve archived documentation:

```bash
# Find the commit where files were archived
git log --grep="Archive" --oneline

# Show what was in the archive
git show [commit-hash] --name-only

# Retrieve specific file from history
git show [commit-hash]:path/to/file > recovered-file.md

# Or restore entire folder
git checkout [commit-hash] -- docs/completed-work-archive/[folder-name]
```

## Important Notes

⚠️ **This is a TEMPORARY holding area**
- Files should not stay here more than a day
- Always commit before removing
- Git history is the permanent archive

⚠️ **No Automated Cleanup**
- All archiving is manual and intentional
- Review before archiving
- Verify git commit before deletion

✅ **Benefits**
- Clean working documentation
- Complete git history
- No confusion about what's current
- Easy recovery if needed

---

*Remember: Archive → Commit → Remove. Git remembers everything.*