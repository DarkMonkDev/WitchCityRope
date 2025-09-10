# Claude Code Failure Prevention System

## 🚨 MANDATORY FOR ALL SESSIONS

**Created**: 2025-09-10  
**Status**: ACTIVE - Immediate Implementation Required  
**Purpose**: Prevent catastrophic failures like the git/worktree crisis

---

## Executive Summary

On 2025-09-10, we experienced a catastrophic failure where work was scattered across main branch and worktrees, with Docker unable to see worktree changes. This resulted in hours of lost work and confusion. This document establishes mandatory processes to prevent recurrence.

## 🔴 CRITICAL FAILURES THIS PREVENTS

1. **Work Done in Wrong Location** - Changes invisible to Docker
2. **Documentation Drift** - Docs out of sync with reality
3. **Lost Work** - Changes scattered across branches/worktrees
4. **Context Loss** - AI agents forgetting established patterns
5. **Process Violations** - Creating files in wrong locations

## 📋 MANDATORY SESSION START PROTOCOL

### Step 1: Run Pre-flight Check (REQUIRED)
```bash
./scripts/claude-preflight-check.sh
```

**DO NOT PROCEED** if errors are reported. The script checks:
- Current directory correctness
- Git branch and worktree status
- Docker compatibility with current location
- Uncommitted changes
- Documentation structure compliance
- Recent handoff documents
- Stashed work

### Step 2: Review Session Context
1. Check `docs/lessons-learned/` for recent handoffs
2. Review `CONTINUATION-GUIDE.md` for project state
3. Check todo list status with TodoWrite tool
4. Review any stashed work: `git stash list`

### Step 3: Declare Work Location
Before starting ANY work, explicitly state and verify:
- **Branch**: main or feature branch name
- **Location**: main directory or worktree path
- **Docker**: Running from main (required) or stopped

## 🛡️ VALIDATION GATES

### Gate 1: File Creation Validation
**BEFORE** creating any file:
1. Check functional-area-master-index.md for proper location
2. Verify NOT creating in /docs/ root
3. Update file registry immediately

### Gate 2: Git Operations Validation
**BEFORE** any git operation:
1. Verify current branch/worktree
2. Check Docker compatibility
3. Review staged/unstaged changes
4. Ensure no build artifacts in staging

### Gate 3: Docker Operations Validation
**CRITICAL**: Docker ONLY works from main directory
- ✅ Main branch + main directory = Docker works
- ❌ Worktree + Docker running = Changes invisible
- ❌ Feature branch + Docker = May see wrong code

### Gate 4: End of Session Validation
**BEFORE** ending session:
1. Run preflight check again
2. Create handoff document with current state
3. Update file registry
4. Commit or stash all changes
5. Document any worktree states

## 🔧 AUTOMATED ENFORCEMENT

### 1. Pre-flight Script (scripts/claude-preflight-check.sh)
- **Run**: At session start and before major operations
- **Purpose**: Catch problems before they compound
- **Enforcement**: Exits with error code on critical issues

### 2. File Registry Automation
```bash
# Auto-add to registry when creating files
echo "| $(date +%Y-%m-%d) | $FILE_PATH | CREATED | $PURPOSE | $TASK | ACTIVE | - |" >> docs/architecture/file-registry.md
```

### 3. Git Hooks (Planned)
- Pre-commit: Check for build artifacts
- Pre-push: Validate documentation structure
- Post-checkout: Warn about Docker implications

## 📊 WORKTREE VS MAIN STRATEGY

### Current Decision Matrix

| Scenario | Use Main | Use Worktree | Use Feature Branch |
|----------|----------|--------------|-------------------|
| Docker needed | ✅ | ❌ | ⚠️ Push first |
| Parallel work | ❌ | ✅ | ✅ |
| Quick fixes | ✅ | ❌ | ❌ |
| Major features | ❌ | ⚠️ | ✅ |
| Testing | ✅ | ❌ | ⚠️ |

### Recommended Approach
1. **DEFAULT**: Work in main for Docker compatibility
2. **FEATURES**: Create feature branches, push, pull to main
3. **PARALLEL**: Use worktrees ONLY for non-Docker work
4. **TESTING**: Always test from main directory

## 🚦 QUICK DECISION TREE

```
Need Docker/Testing?
├─ YES → Use main directory
│   ├─ Quick fix? → main branch
│   └─ Feature? → feature branch + push/pull
└─ NO → Can use worktree
    ├─ Docs only? → worktree OK
    └─ Code changes? → Consider main anyway
```

## 📈 SUCCESS METRICS

Track these to ensure system effectiveness:
- Zero work lost to wrong locations
- Zero documentation in /docs/ root
- All changes visible to Docker
- Clean git history without scattered commits
- Successful session handoffs

## 🔴 EMERGENCY PROCEDURES

### If Work Is In Wrong Location:
1. STOP immediately
2. Run `git status` in all locations
3. Create patches: `git diff > emergency-patch.diff`
4. Document in CRITICAL-*.md file
5. Follow RECOVERY-PLAN.md

### If Docker Can't See Changes:
1. Check current directory: `pwd`
2. Check if in worktree: `git rev-parse --git-common-dir`
3. If in worktree, switch to main
4. Pull changes or apply patches

### If Documentation Violated:
1. Run validation script
2. Move files to correct locations
3. Update file registry
4. Commit fixes immediately

## 📝 ENFORCEMENT CHECKLIST

Print and follow for EVERY session:

- [ ] Run `./scripts/claude-preflight-check.sh`
- [ ] Review recent handoff documents
- [ ] Declare work location (main/worktree/branch)
- [ ] Verify Docker compatibility
- [ ] Check file creation locations
- [ ] Update file registry for all changes
- [ ] Create session handoff before ending
- [ ] Validate no files in /docs/ root
- [ ] Commit or stash all changes
- [ ] Run preflight check again at end

## 🎯 IMPLEMENTATION TIMELINE

### Immediate (Today)
- ✅ Pre-flight script created and tested
- ✅ This enforcement document
- ⏳ Update CLAUDE.md with mandatory requirements
- ⏳ Test full workflow

### Short-term (This Week)
- [ ] Git hooks for automated checking
- [ ] File registry automation
- [ ] Team training/documentation
- [ ] Worktree decision finalization

### Long-term (This Month)
- [ ] Full automation suite
- [ ] CI/CD integration
- [ ] Metrics dashboard
- [ ] Process refinement based on data

## 📚 REFERENCES

- Research: `/docs/functional-areas/ai-workflow-orchstration/research/2025-09-10-claude-code-project-management-failure-prevention-research.md`
- Crisis Documentation: `/docs/lessons-learned/CRITICAL-GIT-CRISIS-2025-09-10.md`
- Recovery Plan: `/docs/lessons-learned/RECOVERY-PLAN.md`
- Validation Script: `/scripts/claude-preflight-check.sh`

---

**REMEMBER**: These aren't suggestions - they're mandatory safeguards born from painful experience. Following this system prevents the multi-hour recovery sessions we've been experiencing.