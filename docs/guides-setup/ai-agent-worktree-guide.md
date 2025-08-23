# AI Agent Worktree Quick Reference Guide

## 🚨 CRITICAL: All Agents Must Use Worktrees 🚨

**Working in the main repository directory is FORBIDDEN**
- Causes session conflicts between parallel Claude Code instances
- All development happens in isolated worktree directories

## 🔴 CRITICAL CONCEPT: Worktrees ARE Branches! 🔴

**UNDERSTAND THIS FIRST:**
- A worktree IS the branch - not a copy of a branch
- When you create a worktree, you create THE branch in its own directory
- The branch exists ONLY in the worktree directory
- The main repo NEVER switches branches - it stays on development/master
- Removing the worktree removes the branch entirely

### ❌ WRONG Mental Model:
"Create a branch, then create a worktree from it" - NO! This is redundant!

### ✅ CORRECT Mental Model:
"Create a worktree AS the branch" - The worktree IS where the branch lives!

## Quick Commands for Agents

### For Orchestrator Agent
```bash
# Start new workflow - delegate to git-manager
Task: git-manager
Prompt: Create worktree for feature/2025-08-23-user-authentication
Path: .worktrees/feature-2025-08-23-user-authentication

# Include in ALL delegations
Working Directory: /home/chad/repos/witchcityrope-react/.worktrees/feature-2025-08-23-user-authentication
```

### For Git-Manager Agent
```bash
# Create THE feature branch AS a worktree (one command creates both!)
git worktree add .worktrees/feature-[date]-[name] -b feature/[date]-[name]
# The branch feature/[date]-[name] now exists ONLY in that worktree directory

# List worktrees (shows where each branch lives)
git worktree list

# Remove after PR merge (Phase 5) - removes BOTH worktree AND branch
git worktree remove .worktrees/feature-[date]-[name]
# No need for rm -rf - worktree remove handles it
# The branch is gone - it only existed in the worktree!
git worktree prune
```

### For Development Agents (React/Backend/Test)
```bash
# FIRST: Verify working directory
pwd  # Should show: /home/chad/repos/witchcityrope-react/.worktrees/[feature-name]

# Check for environment files
ls -la .env  # Must exist

# Verify dependencies
ls -la node_modules  # Must exist
```

## Environment Setup Checklist

### Immediate Setup (After Worktree Creation)
1. **Change to worktree directory**
   ```bash
   cd .worktrees/feature-[name]
   ```

2. **Copy environment files**
   ```bash
   cp ../.env .
   ```

3. **Install dependencies**
   ```bash
   npm install
   ```

4. **Verify setup**
   ```bash
   npm run dev  # Should start without errors
   ```

## Common Troubleshooting

### Problem: "Cannot find module" errors
**Solution**: Run `npm install` in worktree directory

### Problem: "Environment variable not found"
**Solution**: Copy .env file from main repository

### Problem: "Port already in use"
**Solution**: Use different port for each worktree:
```bash
# Worktree 1
PORT=5173 npm run dev

# Worktree 2  
PORT=5174 npm run dev
```

### Problem: "Cannot remove worktree"
**Solution**: Check for uncommitted changes:
```bash
git -C .worktrees/[name] status
git -C .worktrees/[name] stash  # If changes exist
git worktree remove .worktrees/[name]
```

## Working Directory Verification

### EVERY Agent Must Verify Before Work
```bash
# Check current directory
pwd

# Expected format:
# /home/chad/repos/witchcityrope-react/.worktrees/[feature-name]

# If in wrong directory:
cd /home/chad/repos/witchcityrope-react/.worktrees/[feature-name]
```

## Cleanup Procedures (MANDATORY Phase 5)

### Git-Manager Cleanup Protocol
1. **Verify PR merged**
   ```bash
   git log --oneline --merges -1
   ```

2. **Remove worktree**
   ```bash
   git worktree remove .worktrees/[feature-name]
   ```

3. **Delete directory if needed**
   ```bash
   rm -rf .worktrees/[feature-name]  # Usually not needed
   ```

4. **Prune references**
   ```bash
   git worktree prune
   ```

5. **Verify cleanup**
   ```bash
   git worktree list  # Should not show removed worktree
   ls .worktrees/  # Directory should be gone
   ```

## Agent-Specific Requirements

### Orchestrator
- MUST delegate worktree creation to git-manager
- MUST include working directory in all delegations
- MUST delegate cleanup in Phase 5

### Git-Manager
- OWNS worktree lifecycle (create, manage, cleanup)
- MUST setup environment after creation
- MUST cleanup after PR merge

### Librarian
- MUST track worktree creation in file registry
- MUST document worktree paths in progress files
- MUST update registry after cleanup

### Development Agents
- MUST verify working directory before any operations
- MUST check environment files exist
- MUST report if in wrong directory

## Complete Workflow Example (CORRECT)

```bash
# 1. Main repo ALWAYS stays on development/master
cd /home/chad/repos/witchcityrope-react
git branch --show-current  # Shows: development (NEVER changes!)

# 2. Create feature branch AS a worktree
git worktree add .worktrees/feature-auth -b feature/auth
# This creates THE branch feature/auth that lives ONLY in the worktree

# 3. Work in the worktree (the branch IS there)
cd .worktrees/feature-auth
git branch --show-current  # Shows: feature/auth
# Make changes, commit, push

# 4. After PR merged, cleanup removes branch AND worktree
cd /home/chad/repos/witchcityrope-react
git worktree remove .worktrees/feature-auth
# Branch feature/auth is GONE - it only existed in the worktree!
```

## What NOT to Do (WRONG)

```bash
# ❌ WRONG - Don't create branch first
git checkout -b feature/auth  # NO! This switches main repo
git worktree add .worktrees/auth feature/auth  # NO! Redundant!

# ❌ WRONG - Don't switch branches in main repo
cd /home/chad/repos/witchcityrope-react
git checkout feature/something  # NO! Main repo stays on development!
```

## Directory Structure

```
/home/chad/repos/witchcityrope-react/
├── .git/                          # Shared git database
├── src/                           # Main repo source (STAYS ON DEVELOPMENT/MASTER)
├── docs/                          # Main repo documentation
├── .claude/                       # Agent definitions
└── .worktrees/                    # Each worktree IS a branch (hidden directory)
    ├── feature-2025-08-23-auth/   # THE feature/auth branch lives HERE
    ├── bugfix-2025-08-23-login/   # THE bugfix/login branch lives HERE
    └── hotfix-2025-08-23-api/     # THE hotfix/api branch lives HERE
```

## Success Indicators

✅ **Good**: Working in `/home/chad/repos/witchcityrope-react/.worktrees/[name]`
❌ **Bad**: Working in `/home/chad/repos/witchcityrope-react` root

✅ **Good**: Each worktree has its own .env and node_modules
❌ **Bad**: Sharing environment files between worktrees

✅ **Good**: Cleanup immediately after PR merge
❌ **Bad**: Accumulating worktree directories

## Emergency Recovery

### If worktree gets corrupted
```bash
# Force remove
rm -rf .worktrees/[name]
git worktree prune

# Recreate if needed
git worktree add .worktrees/[name] -b feature/[name]
```

### If multiple agents in same directory
```bash
# STOP all work immediately
# Each agent should move to their own worktree
# Orchestrator must coordinate separation
```

---
*This guide is MANDATORY reading for all AI agents*
*Violations will cause workflow failures*
*Last Updated: 2025-08-23*