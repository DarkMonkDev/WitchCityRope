# AI Agent Worktree Quick Reference Guide

## 🚨 CRITICAL: All Agents Must Use Worktrees 🚨

**Working in the main repository directory is FORBIDDEN**
- Causes session conflicts between parallel Claude Code instances
- All development happens in isolated worktree directories

## Quick Commands for Agents

### For Orchestrator Agent
```bash
# Start new workflow - delegate to git-manager
Task: git-manager
Prompt: Create worktree for feature/2025-08-23-user-authentication
Path: ../witchcityrope-worktrees/feature-2025-08-23-user-authentication

# Include in ALL delegations
Working Directory: /home/chad/repos/witchcityrope-worktrees/feature-2025-08-23-user-authentication
```

### For Git-Manager Agent
```bash
# Create new worktree
git worktree add ../witchcityrope-worktrees/feature-[date]-[name] -b feature/[date]-[name]

# List worktrees
git worktree list

# Remove after PR merge (Phase 5)
git worktree remove ../witchcityrope-worktrees/feature-[date]-[name]
rm -rf ../witchcityrope-worktrees/feature-[date]-[name]
git worktree prune
```

### For Development Agents (React/Backend/Test)
```bash
# FIRST: Verify working directory
pwd  # Should show: /home/chad/repos/witchcityrope-worktrees/[feature-name]

# Check for environment files
ls -la .env  # Must exist

# Verify dependencies
ls -la node_modules  # Must exist
```

## Environment Setup Checklist

### Immediate Setup (After Worktree Creation)
1. **Change to worktree directory**
   ```bash
   cd ../witchcityrope-worktrees/feature-[name]
   ```

2. **Copy environment files**
   ```bash
   cp ../../witchcityrope-react/.env .
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
git -C ../witchcityrope-worktrees/[name] status
git -C ../witchcityrope-worktrees/[name] stash  # If changes exist
git worktree remove ../witchcityrope-worktrees/[name]
```

## Working Directory Verification

### EVERY Agent Must Verify Before Work
```bash
# Check current directory
pwd

# Expected format:
# /home/chad/repos/witchcityrope-worktrees/[feature-name]

# If in wrong directory:
cd /home/chad/repos/witchcityrope-worktrees/[feature-name]
```

## Cleanup Procedures (MANDATORY Phase 5)

### Git-Manager Cleanup Protocol
1. **Verify PR merged**
   ```bash
   git log --oneline --merges -1
   ```

2. **Remove worktree**
   ```bash
   git worktree remove ../witchcityrope-worktrees/[feature-name]
   ```

3. **Delete directory**
   ```bash
   rm -rf ../witchcityrope-worktrees/[feature-name]
   ```

4. **Prune references**
   ```bash
   git worktree prune
   ```

5. **Verify cleanup**
   ```bash
   git worktree list  # Should not show removed worktree
   ls ../witchcityrope-worktrees/  # Directory should be gone
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

## Directory Structure

```
/home/chad/repos/
├── witchcityrope-react/           # Main repository (DO NOT WORK HERE)
└── witchcityrope-worktrees/       # All worktrees go here
    ├── feature-2025-08-23-auth/   # Feature worktree
    ├── bugfix-2025-08-23-login/   # Bugfix worktree
    └── hotfix-2025-08-23-api/     # Hotfix worktree
```

## Success Indicators

✅ **Good**: Working in `/home/chad/repos/witchcityrope-worktrees/[name]`
❌ **Bad**: Working in `/home/chad/repos/witchcityrope-react`

✅ **Good**: Each worktree has its own .env and node_modules
❌ **Bad**: Sharing environment files between worktrees

✅ **Good**: Cleanup immediately after PR merge
❌ **Bad**: Accumulating worktree directories

## Emergency Recovery

### If worktree gets corrupted
```bash
# Force remove
rm -rf ../witchcityrope-worktrees/[name]
git worktree prune

# Recreate if needed
git worktree add ../witchcityrope-worktrees/[name] -b feature/[name]
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