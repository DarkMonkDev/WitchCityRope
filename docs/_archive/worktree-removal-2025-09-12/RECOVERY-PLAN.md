# GIT CRISIS RECOVERY PLAN - September 10, 2025

<!-- Last Updated: 2025-09-10 -->
<!-- Version: 1.0 -->
<!-- Owner: Librarian Agent -->
<!-- Status: ACTIVE -->

## 🚨 IMMEDIATE RECOVERY STEPS

**PRIORITY**: Execute in exact order - DO NOT DEVIATE

### Phase 1: Secure Current Work (IMMEDIATE)

#### Step 1.1: Commit Staged Changes Safely
```bash
cd /home/chad/repos/witchcityrope-react

# Verify we're in main and staged files look correct
git status
git branch --show-current  # Should show 'main'

# Create safety commit for today's critical fixes
git commit -m "$(cat <<'EOF'
fix: Critical system fixes - schema, events display, login 400, dashboard RangeError

CRITICAL FIXES:
- Database schema mismatch resolved (ApplicationDbContext.cs)
- Events display rendering fixed (EventsWidget, EventsList)
- Login 400 error authentication flow corrected (authStore, EventEndpoints)
- Dashboard RangeError JavaScript boundary checks added

CONTEXT: Emergency fixes made during git crisis recovery
STATUS: Work scattered across main/worktrees due to Docker limitations

🤖 Generated with [Claude Code](https://claude.ai/code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"
```

#### Step 1.2: Create Recovery Branch
```bash
# Create safety branch from current main
git checkout -b recovery/critical-fixes-2025-09-10

# Push immediately for safety
git push -u origin recovery/critical-fixes-2025-09-10

# Return to main for analysis
git checkout main
```

### Phase 2: Analyze Worktree Content (URGENT)

#### Step 2.1: Inspect Events Worktree (1000+ files diverged)
```bash
cd .worktrees/feature-2025-08-24-events-management

# Check what's different
git log --oneline main..HEAD | head -20
git status --porcelain | wc -l
git diff main --name-only | head -20

# Create summary report
echo "=== EVENTS WORKTREE ANALYSIS ===" > /tmp/worktree-analysis.txt
echo "Commits ahead: $(git rev-list --count main..HEAD)" >> /tmp/worktree-analysis.txt
echo "Files changed: $(git diff main --name-only | wc -l)" >> /tmp/worktree-analysis.txt
echo "Staged files: $(git status --porcelain | wc -l)" >> /tmp/worktree-analysis.txt
```

#### Step 2.2: Inspect User Management Worktree (Detached HEAD)
```bash
cd /home/chad/repos/witchcityrope-worktrees/feature-2025-08-12-user-management-redesign

# Check detached HEAD state
git log --oneline -5
git status --porcelain
git show --name-only

# Document findings
echo "=== USER MGMT WORKTREE ANALYSIS ===" >> /tmp/worktree-analysis.txt
echo "HEAD: $(git rev-parse HEAD)" >> /tmp/worktree-analysis.txt
echo "Files modified: $(git status --porcelain | wc -l)" >> /tmp/worktree-analysis.txt
```

#### Step 2.3: Inspect Stashes
```bash
cd /home/chad/repos/witchcityrope-react

# Analyze each stash
for i in 0 1 2; do
  echo "=== STASH $i ANALYSIS ===" >> /tmp/worktree-analysis.txt
  git stash show stash@{$i} --stat >> /tmp/worktree-analysis.txt
  git stash show stash@{$i} --name-only >> /tmp/worktree-analysis.txt
  echo "" >> /tmp/worktree-analysis.txt
done

# Display full analysis
cat /tmp/worktree-analysis.txt
```

### Phase 3: Data Consolidation (CRITICAL)

#### Step 3.1: Extract Events Worktree Work
```bash
cd .worktrees/feature-2025-08-24-events-management

# Create patch for all changes
git diff main > /tmp/events-worktree-changes.patch

# If there are commits ahead of main, create patch series
if [ $(git rev-list --count main..HEAD) -gt 0 ]; then
  git format-patch main --output-directory /tmp/events-patches/
fi

cd /home/chad/repos/witchcityrope-react
```

#### Step 3.2: Extract User Management Worktree Work
```bash
cd /home/chad/repos/witchcityrope-worktrees/feature-2025-08-12-user-management-redesign

# Create patch from current HEAD
git diff HEAD~1 > /tmp/user-mgmt-detached-changes.patch

# Show what this HEAD contains
git show --name-only > /tmp/user-mgmt-head-files.txt

cd /home/chad/repos/witchcityrope-react
```

#### Step 3.3: Extract Stash Content
```bash
# Extract each stash to patches
git stash show stash@{0} -p > /tmp/stash-0-dashboard-redesign.patch
git stash show stash@{1} -p > /tmp/stash-1-database-init.patch
git stash show stash@{2} -p > /tmp/stash-2-core-pages.patch
```

### Phase 4: Consolidation Strategy (ESSENTIAL)

#### Step 4.1: Create Consolidation Branch
```bash
# Create new branch for consolidating all work
git checkout -b consolidation/all-scattered-work-2025-09-10

# This branch will receive all extracted work
git push -u origin consolidation/all-scattered-work-2025-09-10
```

#### Step 4.2: Apply Extracted Work (Manual Review Required)
```bash
# MANUAL STEP: Review each patch before applying
# 1. Examine /tmp/events-worktree-changes.patch
# 2. Examine /tmp/user-mgmt-detached-changes.patch  
# 3. Examine /tmp/stash-*.patch files

# Apply patches one by one with careful review
echo "MANUAL INTERVENTION REQUIRED:"
echo "Review patches in /tmp/ before applying"
echo "Each patch needs manual inspection for conflicts"
```

### Phase 5: System Fix (PREVENTION)

#### Step 5.1: Implement Docker Worktree Solution
```bash
# Option A: Create Docker Compose override for worktrees
cat > docker-compose.worktree.yml << 'EOF'
version: '3.8'
services:
  api:
    volumes:
      - ./:/app
      - ./.worktrees:/app/.worktrees:ro  # Mount worktrees read-only
  web:
    volumes:
      - ./:/app
      - ./.worktrees:/app/.worktrees:ro
EOF

# Option B: Create worktree-specific dev script
cat > dev-worktree.sh << 'EOF'
#!/bin/bash
# Development script for worktree usage
echo "ERROR: Docker development not supported in worktrees"
echo "Use: cd /home/chad/repos/witchcityrope-react && ./dev.sh"
exit 1
EOF
chmod +x dev-worktree.sh
```

#### Step 5.2: Deploy Validation Scripts
```bash
# Copy existing validation script to proper location
cp docs/standards-processes/worktree-validation-script.sh .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit

# Create location validator
cat > validate-location.sh << 'EOF'
#!/bin/bash
if [ "$(pwd)" != "/home/chad/repos/witchcityrope-react" ]; then
    echo "🚨 WARNING: Not in main repository directory!"
    echo "Current: $(pwd)"
    echo "Expected: /home/chad/repos/witchcityrope-react"
    echo "Docker commands will not work from worktrees!"
    read -p "Continue anyway? (y/N): " confirm
    [[ ! "$confirm" =~ ^[Yy]$ ]] && exit 1
fi
EOF
chmod +x validate-location.sh
```

## 🔄 WORKFLOW CHANGES (MANDATORY)

### New Development Pattern
```bash
# OLD (BROKEN):
git worktree add feature/something
cd .worktrees/feature/something
# Work here (Docker can't see changes)

# NEW (CORRECT):
# 1. Create worktree for tracking
git worktree add feature/something

# 2. ALWAYS work in main directory
cd /home/chad/repos/witchcityrope-react  # MANDATORY

# 3. Switch to feature branch in main
git checkout feature/something

# 4. Work normally (Docker works)
./dev.sh  # Now works correctly

# 5. Commit work to feature branch
git add .
git commit -m "Feature work"
```

### Docker Development Rules
1. **NEVER** run `./dev.sh` from worktree directories
2. **ALWAYS** run Docker commands from main repository root
3. **ALWAYS** validate location before Docker commands
4. **USE** `./validate-location.sh` before development work

## 📋 RECOVERY CHECKLIST

### Immediate Actions (TODAY)
- [ ] Commit staged changes to safety branch
- [ ] Extract worktree content to patches
- [ ] Document stash contents
- [ ] Create consolidation branch
- [ ] Implement validation scripts

### Manual Review Required
- [ ] Review events worktree patches for valuable work
- [ ] Review user management detached HEAD content
- [ ] Inspect stash contents for missing work
- [ ] Identify conflicts between different work streams
- [ ] Merge valuable work into consolidation branch

### System Prevention
- [ ] Deploy pre-commit hooks for location validation
- [ ] Update development documentation
- [ ] Train all agents on new workflow
- [ ] Create permanent worktree strategy
- [ ] Implement automated location checks

## 🚨 CRITICAL WARNINGS DURING RECOVERY

### DO NOT
- Delete worktrees before extracting content
- Abandon stashes without inspection
- Work in worktree directories with Docker
- Create new worktrees until system fixed
- Force push without backup branches

### ALWAYS
- Create safety branches before major operations
- Extract content to patches before cleanup
- Validate location before development work
- Document recovery steps for learning
- Test Docker functionality after changes

## 📊 SUCCESS METRICS

### Recovery Complete When:
- [ ] All 26 staged changes preserved
- [ ] Worktree work consolidated or accounted for
- [ ] Stash contents reviewed and preserved
- [ ] Docker development working from main
- [ ] Prevention measures deployed

### System Healthy When:
- [ ] Docker commands work reliably
- [ ] Location validation prevents mistakes
- [ ] Worktree strategy clearly documented
- [ ] All agents understand new workflow
- [ ] No work scattered across locations

## 🔮 LONG-TERM WORKTREE STRATEGY

### Option 1: Abandon Worktrees for Docker Projects
- Use feature branches in main directory
- Rely on Git's branch switching
- Maintain single working directory

### Option 2: Fix Docker/Worktree Integration
- Modify Docker Compose for worktree support
- Create worktree-specific development scripts
- Implement proper path mounting

### Option 3: Hybrid Approach
- Use worktrees for non-Docker work
- Use feature branches for Docker-dependent work
- Clear documentation of when to use each

---

**EXECUTION ORDER**: Follow phases sequentially
**REVIEW POINTS**: Manual inspection required at consolidation
**ROLLBACK**: Safety branches created at each phase