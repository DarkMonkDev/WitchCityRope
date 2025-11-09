---
name: phase-5-validator
description: Validates Finalization Phase completion before workflow concludes. Checks documentation updates, file registry, git commits, deployment readiness, and lessons learned contributions.
---

# Phase 5 (Finalization) Validation Skill

**Purpose**: Automate validation that all work is properly documented, committed, and ready for deployment/handoff.

**When to Use**: Final validation before marking workflow complete.

## Quick Validation

```bash
# Check git status
git status --short

# Check file registry
tail -5 docs/architecture/file-registry.md

# Check documentation
find docs/functional-areas -type f -name "*.md" -mtime -1
```

## Quality Gate Checklist

### 🚨 Single Source of Truth (BLOCKING - Not Scored)
- [ ] **ALL skills validated for duplication** (MANDATORY)
- [ ] No bash command duplication in agent definitions
- [ ] No procedural duplication in lessons learned
- [ ] No automation duplication in process docs
- [ ] Skills properly referenced (not duplicated)

**CRITICAL**: This check runs FIRST with BLOCKING AUTHORITY.
**Any violations = immediate failure before scoring begins.**

---

### 🚨 Lessons File Sizes (BLOCKING - Not Scored)
- [ ] **ALL lessons files checked for size** (MANDATORY)
- [ ] No files exceeding 1700 line maximum
- [ ] Files approaching 90% threshold identified
- [ ] Multi-file structure properly maintained
- [ ] Size violations provide split instructions

**CRITICAL**: This check runs SECOND with BLOCKING AUTHORITY.
**Files exceeding 1700 lines = immediate failure before scoring begins.**

---

### Git & Version Control (25 points)
- [ ] All changes committed (10 points)
- [ ] Commit messages follow standards (5 points)
- [ ] Branch is up to date (5 points)
- [ ] No merge conflicts (5 points)

### Documentation (30 points)
- [ ] File registry updated (10 points)
- [ ] PROGRESS.md updated (5 points)
- [ ] Feature documentation complete (10 points)
- [ ] CLAUDE.md updated if needed (5 points)

### Lessons Learned (20 points)
- [ ] New lessons documented (10 points)
- [ ] Lessons follow format (5 points)
- [ ] Cross-references added (5 points)

### Deployment Readiness (15 points)
- [ ] All tests passing (5 points)
- [ ] No blocking issues (5 points)
- [ ] Deployment notes present (5 points)

### Cleanup (10 points)
- [ ] Temporary files removed (3 points)
- [ ] Session work archived (3 points)
- [ ] Orphaned files resolved (4 points)

## Automated Validation Script

```bash
#!/bin/bash
# Phase 5 Validation Script

SCORE=0
MAX_SCORE=100
REQUIRED_PERCENTAGE=80  # Most work types

echo "Phase 5 Finalization Validation"
echo "================================"
echo ""

# ============================================================================
# CRITICAL: Single Source of Truth Validation (BLOCKING)
# ============================================================================
# This check runs FIRST and has BLOCKING AUTHORITY
# Any duplication violations = immediate failure before other checks

echo "🚨 Single Source of Truth Validation (CRITICAL)"
echo "================================================"
echo ""
echo "Checking ALL skills for duplication violations..."
echo "Any violations will BLOCK finalization."
echo ""

VIOLATIONS_FOUND=0

# Get list of all skills (exclude README and self)
SKILLS_DIR="/.claude/skills"
if [ ! -d "$SKILLS_DIR" ]; then
    echo "⚠️  Skills directory not found: $SKILLS_DIR"
    echo "   Skipping single source of truth validation"
else
    # Find all skill files
    SKILL_FILES=$(find "$SKILLS_DIR" -maxdepth 1 -name "*.md" ! -name "README.md" ! -name "SKILLS-REGISTRY.md" ! -name "single-source-validator.md" 2>/dev/null || true)

    SKILL_COUNT=$(echo "$SKILL_FILES" | grep -c ".md" || echo 0)

    if [ "$SKILL_COUNT" -eq 0 ]; then
        echo "ℹ️  No skills to validate"
    else
        echo "Validating $SKILL_COUNT skills for duplication..."
        echo ""

        # Validate each skill
        for SKILL_FILE in $SKILL_FILES; do
            SKILL_NAME=$(basename "$SKILL_FILE" .md)

            echo "   Checking: $SKILL_NAME"

            # Run validator with CRITICAL severity
            if [ -f "$SKILLS_DIR/single-source-validator.md" ]; then
                VALIDATOR_OUTPUT=$(bash "$SKILLS_DIR/single-source-validator.md" "$SKILL_NAME" CRITICAL 2>&1)
                VALIDATOR_EXIT=$?

                if [ $VALIDATOR_EXIT -ne 0 ]; then
                    echo "   ❌ VIOLATION: Duplication found in $SKILL_NAME"
                    echo ""
                    echo "$VALIDATOR_OUTPUT"
                    echo ""
                    ((VIOLATIONS_FOUND++))
                else
                    echo "   ✅ $SKILL_NAME maintains single source of truth"
                fi
            else
                echo "   ⚠️  Validator not found, skipping"
            fi
        done

        echo ""

        if [ $VIOLATIONS_FOUND -gt 0 ]; then
            echo "❌ ❌ ❌ FINALIZATION BLOCKED ❌ ❌ ❌"
            echo ""
            echo "🚨 CRITICAL: $VIOLATIONS_FOUND skill(s) have duplication violations!"
            echo ""
            echo "Single source of truth violations detected."
            echo "Skills must be the ONLY place where automation is documented."
            echo ""
            echo "Required actions:"
            echo "1. Review violations reported above"
            echo "2. Remove duplicated procedures from:"
            echo "   - Agent definitions (/.claude/agents/*.md)"
            echo "   - Lessons learned (/docs/lessons-learned/*.md)"
            echo "   - Process docs (/docs/standards-processes/*.md)"
            echo "3. Replace with references: See: /.claude/skills/[skill-name].md"
            echo "4. Run single-source-validator again to confirm fixes"
            echo ""
            echo "Workflow CANNOT complete until violations resolved."
            echo ""
            echo "For guidance, see:"
            echo "  - Single source of truth architecture: SKILLS-ARCHITECTURE-PLAN.md"
            echo "  - Proper reference format: .claude/skills/single-source-validator.md"
            echo ""
            exit 1
        fi

        echo "✅ ✅ ✅ SINGLE SOURCE OF TRUTH MAINTAINED ✅ ✅ ✅"
        echo ""
        echo "All $SKILL_COUNT skills validated:"
        echo "• No bash command duplication"
        echo "• No procedural duplication"
        echo "• Proper skill references in place"
        echo ""
        echo "Proceeding with finalization validation..."
        echo ""
    fi
fi

echo ""

# ============================================================================
# CRITICAL: Lessons Learned Size Validation (BLOCKING)
# ============================================================================
# This check runs SECOND (after single-source validation)
# Files exceeding 1700 lines = immediate failure

echo "🚨 Lessons Learned Size Validation (CRITICAL)"
echo "=============================================="
echo ""
echo "Checking ALL lessons files for size violations..."
echo "Files exceeding 1700 lines will BLOCK finalization."
echo ""

OVERSIZED_FILES=0
WARNING_FILES=0

LESSONS_DIR="docs/lessons-learned"
MAX_LINES=2000
WARNING_THRESHOLD=1800  # 90% of max

if [ ! -d "$LESSONS_DIR" ]; then
    echo "⚠️  Lessons directory not found: $LESSONS_DIR"
    echo "   Skipping lessons size validation"
else
    # Find all lessons learned files
    LESSONS_FILES=$(find "$LESSONS_DIR" -name "*-lessons-learned*.md" -type f 2>/dev/null || true)

    LESSONS_COUNT=$(echo "$LESSONS_FILES" | grep -c ".md" || echo 0)

    if [ "$LESSONS_COUNT" -eq 0 ]; then
        echo "ℹ️  No lessons files to validate"
    else
        echo "Validating $LESSONS_COUNT lessons files for size..."
        echo ""

        # Check each file
        for LESSONS_FILE in $LESSONS_FILES; do
            BASENAME=$(basename "$LESSONS_FILE")
            LINE_COUNT=$(wc -l < "$LESSONS_FILE" | tr -d ' ')
            PERCENTAGE=$((LINE_COUNT * 100 / MAX_LINES))

            if [ "$LINE_COUNT" -gt "$MAX_LINES" ]; then
                echo "   ❌ CRITICAL: $BASENAME"
                echo "      Size: $LINE_COUNT / $MAX_LINES lines (${PERCENTAGE}%)"
                echo "      MUST split immediately"
                ((OVERSIZED_FILES++))
            elif [ "$LINE_COUNT" -gt "$WARNING_THRESHOLD" ]; then
                echo "   ⚠️  WARNING: $BASENAME"
                echo "      Size: $LINE_COUNT / $MAX_LINES lines (${PERCENTAGE}%)"
                echo "      Plan split soon (over 90%)"
                ((WARNING_FILES++))
            else
                echo "   ✅ $BASENAME: $LINE_COUNT lines (${PERCENTAGE}%)"
            fi
        done

        echo ""

        if [ $OVERSIZED_FILES -gt 0 ]; then
            echo "❌ ❌ ❌ FINALIZATION BLOCKED ❌ ❌ ❌"
            echo ""
            echo "🚨 CRITICAL: $OVERSIZED_FILES file(s) exceed maximum size!"
            echo ""
            echo "Lessons learned files have a 1700 line maximum to prevent:"
            echo "• File read errors (Claude's token limits)"
            echo "• Difficulty finding specific lessons"
            echo "• Maintenance overhead"
            echo ""
            echo "Required actions:"
            echo "1. Review oversized files listed above"
            echo "2. Split files exceeding 1700 lines:"
            echo "   - Create Part 2/3/N: [filename]-2.md, [filename]-3.md"
            echo "   - Add multi-file header to all parts"
            echo "   - Move recent lessons to new part"
            echo "   - Keep each part under 1700 lines"
            echo "3. Update lessons-learned-validator if needed"
            echo "4. Run lessons-learned-validator on all files to confirm"
            echo ""
            echo "For guidance, see:"
            echo "  - Documentation standards: /docs/standards-processes/documentation-standards.md"
            echo "  - Lessons validator: /.claude/skills/lessons-learned-validator.md"
            echo ""
            echo "Workflow CANNOT complete until files are split."
            echo ""
            exit 1
        fi

        if [ $WARNING_FILES -gt 0 ]; then
            echo "⚠️  WARNING: $WARNING_FILES file(s) approaching size limit"
            echo ""
            echo "Files over 90% of limit (1530+ lines) should be split soon."
            echo "Plan file splits before they block workflow."
            echo ""
        fi

        echo "✅ ✅ ✅ LESSONS FILE SIZES OK ✅ ✅ ✅"
        echo ""
        echo "All $LESSONS_COUNT lessons files validated:"
        echo "• No files exceeding 1700 lines"
        if [ $WARNING_FILES -gt 0 ]; then
            echo "• $WARNING_FILES file(s) approaching limit (warnings issued)"
        else
            echo "• All files below 90% threshold"
        fi
        echo ""
        echo "Proceeding with finalization validation..."
        echo ""
    fi
fi

echo ""

# Git & Version Control
echo "Git & Version Control"
echo "---------------------"

# Uncommitted changes
UNCOMMITTED=$(git status --short | wc -l)
if [ "$UNCOMMITTED" -eq 0 ]; then
    echo "✅ All changes committed"
    ((SCORE+=10))
else
    echo "⚠️  Uncommitted changes: $UNCOMMITTED files"
    echo "   Run: git status"
    ((SCORE+=5))
fi

# Commit message quality
LATEST_COMMIT=$(git log -1 --pretty=%B)
if echo "$LATEST_COMMIT" | grep -q "feat:\|fix:\|refactor:\|docs:\|test:"; then
    echo "✅ Commit message follows standards"
    ((SCORE+=5))
else
    echo "⚠️  Commit message doesn't follow conventional format"
    ((SCORE+=3))
fi

# Branch up to date
BEHIND=$(git rev-list HEAD..@{u} --count 2>/dev/null)
if [ "$BEHIND" -eq 0 ] 2>/dev/null; then
    echo "✅ Branch is up to date"
    ((SCORE+=5))
else
    echo "⚠️  Branch is $BEHIND commits behind"
    ((SCORE+=3))
fi

# Merge conflicts
if git diff --check > /dev/null 2>&1; then
    echo "✅ No merge conflicts"
    ((SCORE+=5))
else
    echo "❌ Merge conflicts detected"
fi

echo ""

# Documentation
echo "Documentation Validation"
echo "------------------------"

# File registry
FILE_REGISTRY="docs/architecture/file-registry.md"
REGISTRY_UPDATED=$(find "$FILE_REGISTRY" -mtime -1 2>/dev/null)
if [ -n "$REGISTRY_UPDATED" ]; then
    # Check for recent entries
    RECENT_ENTRIES=$(tail -20 "$FILE_REGISTRY" | grep -c "$(date +%Y-%m-%d)")
    if [ "$RECENT_ENTRIES" -gt 0 ]; then
        echo "✅ File registry updated ($RECENT_ENTRIES new entries)"
        ((SCORE+=10))
    else
        echo "⚠️  File registry modified but no entries for today"
        ((SCORE+=5))
    fi
else
    echo "❌ File registry not updated"
    echo "   CRITICAL: All file operations must be logged"
fi

# PROGRESS.md
if find PROGRESS.md -mtime -1 2>/dev/null; then
    echo "✅ PROGRESS.md updated"
    ((SCORE+=5))
else
    echo "⚠️  PROGRESS.md not updated"
    ((SCORE+=2))
fi

# Feature documentation
NEW_WORK_DIR=$(find docs/functional-areas -type d -path "*/new-work/*" | sort | tail -1)
if [ -n "$NEW_WORK_DIR" ]; then
    DOC_COUNT=$(find "$NEW_WORK_DIR" -name "*.md" | wc -l)
    if [ "$DOC_COUNT" -ge 3 ]; then
        echo "✅ Feature documentation complete ($DOC_COUNT documents)"
        ((SCORE+=10))
    else
        echo "⚠️  Limited documentation ($DOC_COUNT documents)"
        ((SCORE+=5))
    fi
else
    echo "⚠️  No new-work directory found"
fi

# CLAUDE.md updates
if [ -n "$(git diff HEAD~1 CLAUDE.md 2>/dev/null)" ]; then
    echo "✅ CLAUDE.md updated (project changes)"
    ((SCORE+=5))
else
    echo "ℹ️  CLAUDE.md not updated (may not be needed)"
    ((SCORE+=5))  # Not always needed
fi

echo ""

# Lessons Learned
echo "Lessons Learned Validation"
echo "--------------------------"

# Check for new lessons
LESSONS_MODIFIED=$(find docs/lessons-learned -name "*.md" -mtime -1 | wc -l)
if [ "$LESSONS_MODIFIED" -gt 0 ]; then
    echo "✅ New lessons documented ($LESSONS_MODIFIED files)"
    ((SCORE+=10))
else
    echo "⚠️  No new lessons documented"
    echo "   Did this work generate any learnings?"
    ((SCORE+=5))
fi

# Lesson format check
if [ "$LESSONS_MODIFIED" -gt 0 ]; then
    NEWEST_LESSON=$(find docs/lessons-learned -name "*.md" -mtime -1 | head -1)
    if grep -q "## Problem\|## Solution\|## Example" "$NEWEST_LESSON"; then
        echo "✅ Lessons follow standard format"
        ((SCORE+=5))
    else
        echo "⚠️  Lessons don't follow format"
        ((SCORE+=2))
    fi

    # Cross-references
    if grep -q "See:\|Related:" "$NEWEST_LESSON"; then
        echo "✅ Cross-references present"
        ((SCORE+=5))
    else
        echo "⚠️  No cross-references"
        ((SCORE+=3))
    fi
else
    ((SCORE+=10))  # Skip if no lessons
fi

echo ""

# Deployment Readiness
echo "Deployment Readiness"
echo "--------------------"

# Tests passing (run quick check)
if [ -f "test-results/test-execution-report.md" ]; then
    if grep -q "Status: PASS" "test-results/test-execution-report.md"; then
        echo "✅ All tests passing"
        ((SCORE+=5))
    else
        echo "❌ Tests not passing"
        echo "   Cannot finalize with failing tests"
        exit 1
    fi
else
    echo "⚠️  Test results not found"
    echo "   Run: Use phase-4-validator skill first"
    ((SCORE+=2))
fi

# Blocking issues
if [ -f "$NEW_WORK_DIR/implementation-notes.md" ]; then
    BLOCKING_COUNT=$(grep -c "BLOCKING\|CRITICAL\|MUST FIX" "$NEW_WORK_DIR/implementation-notes.md" 2>/dev/null || echo 0)
    if [ "$BLOCKING_COUNT" -eq 0 ]; then
        echo "✅ No blocking issues"
        ((SCORE+=5))
    else
        echo "❌ $BLOCKING_COUNT blocking issues found"
        echo "   Must resolve before finalizing"
        exit 1
    fi
else
    echo "ℹ️  Implementation notes not found"
    ((SCORE+=3))
fi

# Deployment notes
if [ -f "$NEW_WORK_DIR/deployment-notes.md" ]; then
    echo "✅ Deployment notes present"
    ((SCORE+=5))
else
    echo "⚠️  Deployment notes missing"
    echo "   Should document: migrations, config changes, manual steps"
    ((SCORE+=2))
fi

echo ""

# Cleanup
echo "Cleanup Validation"
echo "------------------"

# Temporary files
TEMP_FILES=$(find . -name "*.tmp" -o -name "*.bak" -o -name "*~" | wc -l)
if [ "$TEMP_FILES" -eq 0 ]; then
    echo "✅ No temporary files"
    ((SCORE+=3))
else
    echo "⚠️  Temporary files found: $TEMP_FILES"
    ((SCORE+=1))
fi

# Session work archived
if [ -d "session-work" ]; then
    RECENT_SESSION=$(find session-work -type d -name "$(date +%Y-%m-%d)" 2>/dev/null)
    if [ -n "$RECENT_SESSION" ]; then
        echo "✅ Session work organized"
        ((SCORE+=3))
    else
        echo "⚠️  Session work not organized for today"
        ((SCORE+=1))
    fi
else
    echo "ℹ️  No session-work directory"
    ((SCORE+=3))
fi

# Orphaned files check
ORPHANED=$(find . -maxdepth 1 -type f -name "*.md" ! -name "README.md" ! -name "PROGRESS.md" ! -name "ARCHITECTURE.md" ! -name "CLAUDE.md" ! -name "DOCKER_DEV_GUIDE.md" ! -name "MARKETPLACE-README.md" | wc -l)
if [ "$ORPHANED" -eq 0 ]; then
    echo "✅ No orphaned files in root"
    ((SCORE+=4))
else
    echo "⚠️  Orphaned files in root: $ORPHANED"
    echo "   Move to appropriate location or session-work/"
    ((SCORE+=1))
fi

echo ""

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo "================================"
echo "Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo ""

if [ "$PERCENTAGE" -ge "$REQUIRED_PERCENTAGE" ]; then
    echo "✅ PASS - Finalization Phase complete"
    echo "   Work is properly documented and ready"
    echo ""
    echo "Next Steps:"
    echo "  - Deploy if approved"
    echo "  - Close workflow"
    echo "  - Update project status"
    exit 0
else
    echo "❌ FAIL - Finalization Phase incomplete"
    echo "   Score: $PERCENTAGE% (need ${REQUIRED_PERCENTAGE}%)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    echo ""
    echo "Common issues:"
    echo "  - File registry not updated"
    echo "  - Changes not committed"
    echo "  - Documentation incomplete"
    exit 1
fi
```

## Usage Examples

### From Orchestrator
```
Use the phase-5-validator skill to check if work is ready to finalize
```

### Manual Validation
```bash
# Run finalization validation
bash .claude/skills/phase-5-validator.md
```

## Common Issues

### Issue: File Registry Not Updated
**CRITICAL**: Every file operation must be logged.

**Solution**:
```bash
# Update file registry
echo "| $(date +%Y-%m-%d) | /path/to/file | CREATED | Purpose | Task | ACTIVE | - |" >> docs/architecture/file-registry.md
```

### Issue: Uncommitted Changes
**Solution**:
```bash
# Check status
git status

# Commit changes
git add .
git commit -m "feat: implement user management feature

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>"
```

### Issue: Missing Documentation
**Required documents**:
- business-requirements.md
- functional-spec.md
- database-design.md
- ui-ux-design.md
- implementation-notes.md
- deployment-notes.md

### Issue: Temporary Files
**Solution**:
```bash
# Find and remove
find . -name "*.tmp" -delete
find . -name "*.bak" -delete

# Or move to session-work
mkdir -p session-work/$(date +%Y-%m-%d)
mv status.md session-work/$(date +%Y-%m-%d)/
```

## Mandatory Finalization Checklist

Before marking workflow complete:

0. **🚨 Single Source of Truth (BLOCKING - FIRST CHECK)**
   - [ ] ALL skills validated for duplication (MANDATORY)
   - [ ] No bash commands duplicated in agent definitions
   - [ ] No procedures duplicated in lessons learned
   - [ ] No automation duplicated in process docs
   - [ ] Skills properly referenced from other docs
   - **ANY VIOLATIONS = IMMEDIATE WORKFLOW FAILURE**

1. **Git**
   - [ ] All changes committed
   - [ ] Commit messages follow standards
   - [ ] Branch clean (no conflicts)

2. **Documentation**
   - [ ] File registry current
   - [ ] Feature docs complete
   - [ ] PROGRESS.md updated
   - [ ] Deployment notes written

3. **Lessons Learned**
   - [ ] New patterns documented
   - [ ] Problems and solutions recorded
   - [ ] Cross-references added

4. **Quality**
   - [ ] All tests passing (100%)
   - [ ] No blocking issues
   - [ ] Code reviewed

5. **Cleanup**
   - [ ] Temporary files removed
   - [ ] Session work organized
   - [ ] No orphaned files

## Output Format

```json
{
  "phase": "finalization",
  "status": "pass|fail",
  "singleSourceOfTruth": {
    "validated": true,
    "skillsChecked": 13,
    "violations": 0,
    "blocking": false,
    "note": "CRITICAL: This check runs FIRST. Any violations = immediate failure."
  },
  "score": 87,
  "maxScore": 100,
  "percentage": 87,
  "requiredPercentage": 80,
  "git": {
    "uncommittedChanges": 0,
    "commitMessage": "standards-compliant",
    "upToDate": true,
    "conflicts": false
  },
  "documentation": {
    "fileRegistry": "updated",
    "progress": "updated",
    "featureDocs": "complete",
    "claude": "not-needed"
  },
  "lessonsLearned": {
    "newLessons": 2,
    "formatCompliant": true,
    "crossReferences": true
  },
  "deployment": {
    "testsPass": true,
    "blockingIssues": 0,
    "notesPresent": true
  },
  "cleanup": {
    "tempFiles": 0,
    "sessionWork": "organized",
    "orphanedFiles": 0
  },
  "readyToFinalize": true,
  "nextSteps": [
    "Deploy to staging (if approved)",
    "Close workflow",
    "Update project status"
  ]
}
```

## Integration with Quality Gates

Quality gate thresholds by work type:

- **Feature**: 80% required
- **Bug Fix**: 75% required
- **Hotfix**: 70% required
- **Documentation**: 90% required
- **Refactoring**: 85% required

## Progressive Disclosure

**Initial Context**: Show quick status check (committed, documented, tested)
**On Request**: Show full scoring breakdown
**On Failure**: Show specific missing items with actionable fixes
**On Pass**: Show next steps and finalization instructions

---

**Remember**: Finalization is about ensuring nothing is forgotten. Documentation, commits, and lessons learned ensure the work is sustainable and maintainable long-term.
