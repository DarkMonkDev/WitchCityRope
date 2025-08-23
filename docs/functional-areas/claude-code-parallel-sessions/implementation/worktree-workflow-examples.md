# Git Worktree Workflow Examples - CORRECT vs WRONG
<!-- Last Updated: 2025-08-23 -->
<!-- Version: 1.0 -->
<!-- Owner: Git Manager Agent -->
<!-- Status: Active -->

## 🔴 FUNDAMENTAL CONCEPT: Worktrees ARE Branches! 🔴

**THIS IS THE KEY TO UNDERSTANDING EVERYTHING:**
- A worktree IS the branch - not a copy of a branch
- The branch exists ONLY in the worktree directory
- Main repo NEVER switches branches - it stays on development/master
- One command creates BOTH the worktree AND the branch

## Complete Workflow Examples

### ✅ CORRECT: Feature Development Workflow

```bash
# 1. Main repo is on development branch (and STAYS there!)
cd /home/chad/repos/witchcityrope-react
git branch --show-current
# Output: development

# 2. Create THE feature branch AS a worktree (one command!)
git worktree add .worktrees/feature-2025-08-23-auth -b feature/2025-08-23-auth
# This creates THE branch feature/2025-08-23-auth that exists ONLY in the worktree

# 3. Work happens in the worktree (where THE branch lives)
cd .worktrees/feature-2025-08-23-auth
git branch --show-current
# Output: feature/2025-08-23-auth (THE branch is HERE)

# 4. Make changes, commit, push
echo "// New auth code" >> auth.js
git add auth.js
git commit -m "feat: Add authentication"
git push -u origin feature/2025-08-23-auth

# 5. After PR is merged, cleanup removes BOTH worktree AND branch
cd /home/chad/repos/witchcityrope-react  # Back to main repo
git worktree remove .worktrees/feature-2025-08-23-auth
# The branch feature/2025-08-23-auth is GONE - it only existed in the worktree!

# 6. Main repo is STILL on development (never changed!)
git branch --show-current
# Output: development
```

### ❌ WRONG: Old Branch-First Workflow

```bash
# ❌ WRONG - Don't do this!
cd /home/chad/repos/witchcityrope-react
git checkout -b feature/auth  # NO! This switches the main repo
git worktree add ../worktrees/auth feature/auth  # NO! Redundant - branch already exists!

# ❌ WRONG - Main repo is now on wrong branch
git branch --show-current
# Output: feature/auth (WRONG! Main should stay on development)

# ❌ WRONG - Now you have the branch in TWO places
# - In main repo (wrong!)
# - In worktree (unnecessary duplication)
```

## Common Scenarios

### Scenario 1: Starting New Feature

#### ✅ CORRECT Way
```bash
# Orchestrator delegates to git-manager
Task: git-manager
Prompt: Create worktree for new authentication feature

# Git-manager executes (creates THE branch AS worktree)
git worktree add .worktrees/feature-2025-08-23-auth -b feature/2025-08-23-auth

# Setup environment
cd .worktrees/feature-2025-08-23-auth
cp ../.env .
npm install

# Report back: "Feature branch feature/2025-08-23-auth created and exists in worktree at .worktrees/feature-2025-08-23-auth"
```

#### ❌ WRONG Way
```bash
# ❌ Creating branch first
git checkout -b feature/auth  # NO! Switches main repo
git worktree add .worktrees/auth feature/auth  # NO! Branch already exists

# ❌ Using wrong mental model
# Thinking: "I need to create a branch, then make a worktree from it"
# Reality: "The worktree IS the branch creation"
```

### Scenario 2: Multiple Parallel Features

#### ✅ CORRECT Way
```bash
# Feature 1: Authentication (creates THE branch in worktree)
git worktree add .worktrees/feature-auth -b feature/auth

# Feature 2: User Profile (creates THE branch in worktree)
git worktree add .worktrees/feature-profile -b feature/profile

# Feature 3: Event System (creates THE branch in worktree)
git worktree add .worktrees/feature-events -b feature/events

# Main repo check - still on development!
cd /home/chad/repos/witchcityrope-react
git branch --show-current
# Output: development (perfect!)

# Each branch exists ONLY in its worktree
git branch -a | grep feature/
# Shows: feature/auth, feature/profile, feature/events (all remote tracking)
```

#### ❌ WRONG Way
```bash
# ❌ Switching branches in main repo
cd /home/chad/repos/witchcityrope-react
git checkout -b feature/auth  # NO! Main repo switched
git checkout -b feature/profile  # NO! Main repo switched again
git checkout -b feature/events  # NO! Main repo switched again

# ❌ Now main repo is on feature/events (WRONG!)
# ❌ Can't work on multiple features in parallel
# ❌ Git context is shared, not isolated
```

### Scenario 3: Cleanup After PR Merge

#### ✅ CORRECT Way
```bash
# After PR is merged to main
cd /home/chad/repos/witchcityrope-react

# Remove the worktree (this removes THE branch too!)
git worktree remove .worktrees/feature-auth

# Verify branch is gone (it only existed in the worktree)
git branch -a | grep feature/auth
# Output: (nothing - branch is completely gone)

# Clean up any git references
git worktree prune
```

#### ❌ WRONG Way
```bash
# ❌ Trying to delete branch separately
git branch -d feature/auth  # NO! Branch doesn't exist here
# Error: branch 'feature/auth' not found

# ❌ Forgetting that removing worktree removes the branch
rm -rf .worktrees/feature-auth  # NO! Use git worktree remove
# Now you have orphaned git references

# ❌ Trying to checkout the branch to delete it
git checkout feature/auth  # NO! Don't switch main repo
git checkout development
git branch -d feature/auth  # Unnecessary - branch is in worktree
```

## Agent Delegation Examples

### Orchestrator → Git-Manager

#### ✅ CORRECT Delegation
```yaml
Task: git-manager
Description: Create feature branch for authentication
Prompt: |
  Create THE feature branch AS a worktree for authentication work:
  - Branch name: feature/2025-08-23-authentication
  - Worktree location: .worktrees/feature-2025-08-23-authentication
  - Use single command: git worktree add [path] -b [branch]
  - This creates THE branch that exists ONLY in the worktree
  - Setup environment files and dependencies
  - Report worktree path for other agents
```

#### ❌ WRONG Delegation
```yaml
# ❌ WRONG - Asks to create branch first
Task: git-manager
Prompt: |
  1. Create branch feature/authentication
  2. Create worktree from the branch
  3. Setup environment
  
# NO! This shows misunderstanding - worktree IS the branch!
```

### Orchestrator → Development Agent

#### ✅ CORRECT Delegation
```yaml
Task: react-developer
Description: Implement authentication UI
Prompt: |
  Implement authentication UI components.
  
  Working Directory: /home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-23-auth
  Branch: feature/2025-08-23-auth (exists ONLY in this worktree)
  
  First verify:
  - cd to working directory
  - Confirm you're on feature/2025-08-23-auth branch
  - Check .env exists
  - Verify node_modules installed
```

#### ❌ WRONG Delegation
```yaml
# ❌ WRONG - No worktree context
Task: react-developer
Prompt: |
  Switch to feature/authentication branch and implement UI
  
# NO! Agent shouldn't switch branches - should work in worktree!
```

## Quick Sanity Checks

### How to Verify Correct Setup

```bash
# 1. Main repo should ALWAYS be on development/master
cd /home/chad/repos/witchcityrope-react
git branch --show-current
# Expected: development or master (NEVER a feature branch)

# 2. Each worktree has its own branch
cd .worktrees/feature-auth
git branch --show-current
# Expected: feature/auth (THE branch lives here)

# 3. Branches exist ONLY in worktrees
cd /home/chad/repos/witchcityrope-react
git branch | grep feature/
# Expected: (nothing - features are in worktrees, not main)

# 4. List all worktrees and their branches
git worktree list
# Expected:
# /home/chad/repos/witchcityrope-react              abc123 [development]
# /home/chad/repos/witchcityrope-react/.worktrees/feature-auth  def456 [feature/auth]
# /home/chad/repos/witchcityrope-react/.worktrees/feature-profile  ghi789 [feature/profile]
```

## Common Mistakes and Fixes

### Mistake 1: "I created a branch but can't find it"
**Cause**: Created branch in main repo, looking in worktree
**Fix**: Branches created in worktrees exist ONLY there. Use `git worktree list` to find it.

### Mistake 2: "Main repo is on wrong branch"
**Cause**: Used `git checkout` in main repo
**Fix**: 
```bash
cd /home/chad/repos/witchcityrope-react
git checkout development  # Reset main to development
# From now on, ONLY create branches via worktrees
```

### Mistake 3: "Can't delete the branch"
**Cause**: Trying to delete branch that exists in worktree
**Fix**: Remove the worktree - this removes the branch too!
```bash
git worktree remove .worktrees/[name]
```

### Mistake 4: "Worktree add says branch already exists"
**Cause**: Previously created branch without worktree
**Fix**: 
```bash
# If branch exists, create worktree WITHOUT -b flag
git worktree add .worktrees/existing-branch existing-branch
# Then NEVER use git checkout in main repo again
```

## The One Rule to Remember

### 🎯 THE GOLDEN RULE 🎯

**"Worktrees ARE branches, not copies of branches"**

When you run:
```bash
git worktree add ../path -b branch-name
```

You are creating THE branch `branch-name` that exists ONLY in that worktree directory.

The main repository NEVER switches branches - it's just the shared git database.

---

*This document demonstrates the CORRECT mental model for git worktrees in the AI agent workflow.*
*Remember: Worktrees ARE branches, not copies!*