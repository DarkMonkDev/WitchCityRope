# Git Manager Agent Lessons Learned

## 🚨 CRITICAL: Git Worktree Workflow MANDATORY 🚨

**All development MUST use git worktrees for session isolation**
- Branches in same directory = session conflicts
- Worktrees = complete isolation between parallel sessions
- MANDATORY cleanup after PR merge (Phase 5)

## Worktree Management Procedures

### Worktree Creation (MANDATORY for all new work)
```bash
# Standard worktree creation pattern
git worktree add ../witchcityrope-worktrees/feature-[YYYY-MM-DD]-[description] -b feature/[YYYY-MM-DD]-[description]

# Setup environment IMMEDIATELY after creation
cd ../witchcityrope-worktrees/feature-[YYYY-MM-DD]-[description]
cp ../../witchcityrope-react/.env .
npm install
```

### Environment Setup Checklist
- [ ] Copy .env file from main repository
- [ ] Run npm install for dependencies
- [ ] Verify database connection strings
- [ ] Check Docker configuration if needed
- [ ] Test basic functionality before delegation

### Worktree Status Monitoring
```bash
# List all active worktrees
git worktree list

# Check for stale worktrees (merged branches)
for wt in $(git worktree list --porcelain | grep "worktree" | cut -d' ' -f2); do
  branch=$(git -C "$wt" branch --show-current)
  if git branch --merged main | grep -q "$branch"; then
    echo "STALE: $wt (branch $branch is merged)"
  fi
done
```

### MANDATORY Phase 5 Cleanup
**Workflow CANNOT complete without cleanup verification**
```bash
# After PR merge confirmation
git worktree remove ../witchcityrope-worktrees/[feature-name]
rm -rf ../witchcityrope-worktrees/[feature-name]  # Ensure directory deleted
git worktree prune  # Clean up references
```

## Critical Rules

### NEVER create branches without worktrees
- Old way: `git checkout -b feature/name` ❌
- New way: `git worktree add ../path -b feature/name` ✅

### ALWAYS verify worktree isolation
- Each worktree has separate working directory
- Changes in one worktree don't affect others
- Environment files are worktree-specific

### IMMEDIATE cleanup after merge
- Part of Phase 5 finalization (NOT scheduled)
- Prevents directory accumulation
- Maintains <3 active worktrees target

## Common Issues and Solutions

### Issue: Environment file not found in worktree
**Solution**: Always copy .env after worktree creation

### Issue: Dependencies missing in worktree
**Solution**: Run npm install in worktree directory

### Issue: Worktree directory accumulation
**Solution**: Enforce Phase 5 cleanup, never skip

### Issue: Can't remove worktree (uncommitted changes)
**Solution**: 
```bash
git -C [worktree-path] status  # Check changes
git -C [worktree-path] stash    # Stash if needed
git worktree remove [path]      # Now remove
```

## Worktree Naming Conventions

### Feature Development
`../witchcityrope-worktrees/feature-[YYYY-MM-DD]-[description]`

### Bug Fixes
`../witchcityrope-worktrees/bugfix-[YYYY-MM-DD]-[description]`

### Hotfixes
`../witchcityrope-worktrees/hotfix-[YYYY-MM-DD]-[description]`

### Enhancement
`../witchcityrope-worktrees/enhancement-[YYYY-MM-DD]-[description]`

## Migration from Branches

### For existing unmerged branches
```bash
# Create worktree from existing branch
git worktree add ../witchcityrope-worktrees/[branch-name] [branch-name]

# Setup environment
cd ../witchcityrope-worktrees/[branch-name]
cp ../../witchcityrope-react/.env .
npm install

# After verification, can delete original branch reference
git branch -D [branch-name]  # Only after worktree verified working
```

## Performance Considerations

### Disk Usage
- Each worktree only duplicates modified files
- Shared git objects between worktrees
- Target: <3 active worktrees to minimize disk usage

### Memory/CPU
- Each worktree can run separate dev servers
- Monitor port conflicts (use different ports)
- Consider resource limits with multiple environments

## Delegation Context Requirements

### ALWAYS provide worktree path to other agents
```
Working Directory: /home/chad/repos/witchcityrope-worktrees/feature-[name]
```

### Verify agent acknowledgment of working directory
- Agents must confirm they're in correct worktree
- Check for environment file presence
- Validate dependencies installed

## Success Metrics

- Zero session conflicts between parallel Claude Code instances
- 100% cleanup success rate (no orphaned directories)
- <3 active worktrees maintained
- All agents working in correct isolation

---
*Last Updated: 2025-08-23*
*Critical Knowledge: Worktree workflow is MANDATORY for all development*