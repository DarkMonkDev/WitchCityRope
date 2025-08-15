# Git Workflow and GitHub Push Instructions

## Branch Management

### Branch Naming Convention
- Features: `feature/YYYY-MM-DD-description`
- Bugs: `fix/YYYY-MM-DD-description`
- Hotfixes: `hotfix/YYYY-MM-DD-description`
- Experiments: `test/YYYY-MM-DD-description`

### Branch Structure
```
main            # Production-ready code (pushes to GitHub)
├── develop     # Integration branch (if exists)
└── feature/    # Feature branches for isolation
    ├── feature/2025-08-12-user-management
    ├── feature/2025-08-13-event-system
    └── fix/2025-08-14-login-bug
```

## Commit Message Format

### Standard Format
```
[phase]: Brief description

Detailed explanation if needed
- Bullet points for multiple changes
- Reference to scope documentation

Scope: feature/YYYY-MM-DD-description
```

### Required Phase Commits
1. After requirements phase: `requirements: [description]`
2. After design phase: `design: [description]`
3. After implementation phase: `implementation: [description]`
4. After testing phase: `testing: [description]`
5. Before human reviews: `checkpoint: Ready for review`
6. After fixes: `fix: [what was fixed]`
7. Final: `finalization: Feature complete`

## GitHub Repository

### Remote Repository
- **URL**: https://github.com/DarkMonkDev/WitchCityRope.git
- **Main Branch**: `main` (not master)
- **Default Push Target**: `main` branch

### Initial Setup
```bash
# Add remote (if not already added)
git remote add origin https://github.com/DarkMonkDev/WitchCityRope.git

# Verify remote
git remote -v
```

## Standard Workflow

### 1. Starting New Work
```bash
# Check current status
git status
git branch -a

# Branch from main (or develop if exists)
git checkout main
git pull origin main

# Create feature branch
git checkout -b feature/YYYY-MM-DD-description
```

### 2. During Development
```bash
# Regular commits at phase boundaries
git add .
git commit -m "[phase]: Brief description

Detailed explanation
- Changes made
- References

Scope: feature/YYYY-MM-DD-description"
```

### 3. Completing Work
```bash
# Ensure all changes committed
git status
git add .
git commit -m "finalization: Complete feature implementation"

# Switch to main
git checkout main

# Merge feature branch
git merge feature/YYYY-MM-DD-description

# Run tests before pushing
dotnet test

# Push to GitHub if tests pass
git push origin main

# Clean up local feature branch
git branch -d feature/YYYY-MM-DD-description
```

## Push Strategy

### Main Branch Only
```bash
# Only push main branch to GitHub
git push origin main

# Feature branches stay local for solo development
# Only push feature branches for backup of long work
git push origin feature/YYYY-MM-DD-description  # Optional backup only
```

### Authentication
When prompted for credentials:
- **Username**: DarkMonkDev
- **Password**: GitHub Personal Access Token (not account password)

Generate token at: https://github.com/settings/tokens
Required scopes: `repo` (full control)

## Recovery Procedures

### Rollback Options
```bash
# View recent commits
git log --oneline -10

# Rollback to specific commit
git reset --hard [commit-hash]

# Create recovery branch from last good state
git checkout -b recovery/YYYY-MM-DD [commit-hash]

# Cherry-pick specific commits
git cherry-pick [commit-hash]
```

### Conflict Resolution
```bash
# See conflict details
git status
git diff

# Fix conflicts in files, then:
git add [resolved-file]
git commit -m "resolve: Fixed merge conflict in [file]"
```

## Status Reporting

### Check Current State
```bash
# Current branch and status
git branch --show-current
git status

# Recent activity
git log --oneline -5

# Uncommitted changes summary
git diff --stat
```

## Best Practices

1. **Keep It Simple**: Don't overcomplicate with unnecessary branches
2. **Commit Frequently**: Small, logical commits with clear messages
3. **Test Before Push**: Always run tests before pushing to main
4. **Clean History**: Merge feature branches, don't rebase for solo work
5. **Backup Long Work**: Push feature branches only for backup purposes

## Common Commands Reference

```bash
# Start new feature
git checkout -b feature/YYYY-MM-DD-description

# Save work in progress
git add .
git commit -m "wip: [description]"

# See what changed
git diff                    # Unstaged changes
git diff --staged           # Staged changes
git log --oneline -10       # Recent history

# Merge feature to main
git checkout main
git merge feature/YYYY-MM-DD-description

# Push to GitHub
git push origin main

# Clean up branches
git branch -d feature/YYYY-MM-DD-description  # Delete local
git remote prune origin                        # Clean remote refs
```

---

**Note**: This document is the authoritative source for git procedures. For Docker and deployment issues, see [/docs/lessons-learned/devops-engineers.md](/docs/lessons-learned/devops-engineers.md).