# Worktree vs Main Branch Strategy Decision

**Date**: 2025-09-10  
**Status**: RECOMMENDATION READY  
**Decision Required By**: User

## Executive Summary

After the catastrophic worktree/Docker incompatibility discovered today, we must decide on our branch strategy going forward. This document provides a clear recommendation based on our specific needs.

## The Problem

1. **Docker runs from main directory** and cannot see worktree changes
2. **Work got scattered** across 3 locations (main + 2 worktrees)
3. **1000+ file divergence** in events worktree
4. **Detached HEAD** in user-management worktree
5. **Hours lost** to confusion and recovery

## Options Analysis

### Option 1: Abandon Worktrees Completely ✅ RECOMMENDED

**Approach**: 
- Use main branch for all work
- Create feature branches as needed
- Push/pull for parallel work
- No worktrees at all

**Pros**:
- ✅ Docker always works
- ✅ Simple mental model
- ✅ No confusion about location
- ✅ All tools work correctly
- ✅ Easy handoffs between sessions

**Cons**:
- ❌ Can't work on multiple features simultaneously
- ❌ Must commit/stash when switching contexts
- ❌ Slightly slower context switching

**Implementation**:
```bash
# Remove all worktrees
git worktree remove .worktrees/feature-2025-08-24-events-management
git worktree remove ../witchcityrope-worktrees/feature-2025-08-12-user-management-redesign

# Work pattern
git checkout -b feature/new-feature
# ... work ...
git commit
git checkout main
```

### Option 2: Worktrees for Documentation Only

**Approach**:
- Main for all code/Docker work
- Worktrees ONLY for documentation

**Pros**:
- ✅ Can update docs while code runs
- ✅ Clear separation of concerns

**Cons**:
- ❌ Still risk confusion
- ❌ Complexity for marginal benefit
- ❌ Docs often need code context

### Option 3: Fix Docker to Support Worktrees

**Approach**:
- Modify docker-compose to mount worktree paths
- Complex scripting to detect location

**Pros**:
- ✅ Could enable parallel development

**Cons**:
- ❌ HIGH complexity
- ❌ Fragile solution
- ❌ May break with Docker updates
- ❌ Time investment not justified

## 🎯 RECOMMENDATION: Option 1 - Abandon Worktrees

### Why This Is Best For WitchCityRope

1. **Simplicity Wins**: As a solo developer with AI assistance, simplicity prevents errors
2. **Docker Critical**: Our app requires Docker for database, API, and frontend
3. **Limited Benefit**: We rarely need true parallel development
4. **AI Context**: Claude Code works better with simple, clear patterns
5. **Time Saved**: No more debugging worktree/Docker issues

### Migration Plan

#### Phase 1: Extract Worktree Content (TODAY)
```bash
# Save work from events worktree
cd .worktrees/feature-2025-08-24-events-management
git diff main > ~/events-worktree-changes.patch

# Save work from user-management worktree  
cd /home/chad/repos/witchcityrope-worktrees/feature-2025-08-12-user-management-redesign
git diff main > ~/user-management-changes.patch
```

#### Phase 2: Remove Worktrees
```bash
cd /home/chad/repos/witchcityrope-react
git worktree remove .worktrees/feature-2025-08-24-events-management --force
git worktree remove ../witchcityrope-worktrees/feature-2025-08-12-user-management-redesign --force
```

#### Phase 3: Apply Relevant Changes
```bash
# Review and selectively apply patches
git apply --check ~/events-worktree-changes.patch
git apply ~/events-worktree-changes.patch

# Create feature branches as needed
git checkout -b feature/events-management
git add -p  # Selective staging
git commit -m "feat: Consolidated events management work from worktree"
```

#### Phase 4: Update Documentation
- Remove worktree references from CLAUDE.md
- Update development guides
- Document new workflow

### New Workflow Pattern

```bash
# Starting new feature
git checkout main
git pull
git checkout -b feature/new-feature

# Work on feature
# ... make changes ...
npm run test
./scripts/claude-preflight-check.sh

# Save work
git add -A
git commit -m "feat: Description"
git push -u origin feature/new-feature

# Switch contexts
git checkout main  # or another feature branch

# Merge when ready
git checkout main
git merge feature/new-feature
git push
```

## Decision Impact

### What Changes:
- No more worktree commands
- All work in main directory
- Feature branches for isolation
- More frequent commits/pushes

### What Stays Same:
- Docker workflow unchanged
- Testing processes unchanged
- Development tools unchanged
- File structure unchanged

## Metrics for Success

- Zero worktree-related issues
- Reduced session startup time
- Cleaner git history
- Successful Docker builds every time
- No lost work due to location confusion

## User Action Required

**Please confirm decision**:

1. **[ ] Approve Option 1**: Remove worktrees, use feature branches
2. **[ ] Request Alternative**: Propose different approach
3. **[ ] Defer Decision**: Continue with current chaos (not recommended)

Once decided, we will:
1. Execute migration plan
2. Update all documentation
3. Remove worktree references
4. Implement new workflow

---

**Note**: This decision is reversible. We can always add worktrees back if needed, but the current evidence strongly suggests simplicity is better for this project.