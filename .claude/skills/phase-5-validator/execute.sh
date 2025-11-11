#!/bin/bash
# Phase 5 (Finalization) Validation Script
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script validates Finalization Phase completion before workflow concludes
# HAS BLOCKING AUTHORITY: Can prevent workflow completion for critical violations

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

if [ "$#" -gt 0 ] && [ "$1" == "--help" ]; then
    echo "🏁 Phase 5 Finalization Validation"
    echo "==================================="
    echo ""
    echo "📋 Purpose: Validate Finalization Phase completion before workflow concludes"
    echo ""
    echo "🚨 BLOCKING AUTHORITY: This validator can BLOCK workflow completion for:"
    echo "   • Single source of truth violations (bash command duplication)"
    echo "   • Lessons learned files exceeding 2,000 lines"
    echo "   • Failing tests"
    echo "   • Unresolved blocking issues"
    echo ""
    echo "✅ Use when:"
    echo "   • Before completing workflow"
    echo "   • After all phases (Requirements → Design → Implementation → Testing) complete"
    echo "   • When user requests to 'finalize' or 'complete workflow'"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • Still working on testing or implementation"
    echo "   • Tests are not 100% passing"
    echo "   • Documentation is incomplete"
    echo ""
    echo "📊 Quality Gate: 80% Required (Most Work Types)"
    echo "   • Git & Version Control: 25/100 points"
    echo "   • Documentation: 30/100 points"
    echo "   • Lessons Learned: 20/100 points"
    echo "   • Deployment Readiness: 15/100 points"
    echo "   • Cleanup: 10/100 points"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. 🚨 BLOCKING: Validates single source of truth (no duplication)"
    echo "   2. 🚨 BLOCKING: Checks lessons learned file sizes (max 2,000 lines)"
    echo "   3. Validates git state (commits, messages, branch status)"
    echo "   4. Checks documentation (file registry, PROGRESS.md, feature docs)"
    echo "   5. Validates lessons learned (format, cross-references)"
    echo "   6. Verifies deployment readiness (tests passing, no blocking issues)"
    echo "   7. Confirms cleanup (no temp files, session work organized)"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh"
    echo ""
    echo "   No parameters required - validates current state"
    echo ""
    exit 1
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "🏁 Phase 5 Finalization Validation"
echo "==================================="
echo ""

echo "1️⃣  Checking project directory..."
if [ ! -d "/home/chad/repos/witchcityrope" ]; then
    echo "   ❌ FAIL: Project directory not found"
    exit 1
fi
cd /home/chad/repos/witchcityrope
echo "   ✅ Project directory found"

echo ""
echo "2️⃣  Checking git repository..."
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo "   ❌ FAIL: Not a git repository"
    exit 1
fi
echo "   ✅ Git repository detected"

echo ""
echo "3️⃣  Checking required directories..."
if [ ! -d "docs/lessons-learned" ]; then
    echo "   ❌ FAIL: Lessons learned directory not found"
    exit 1
fi
if [ ! -d ".claude/skills" ]; then
    echo "   ❌ FAIL: Skills directory not found"
    exit 1
fi
echo "   ✅ Required directories found"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# BLOCKING CHECK 1: Single Source of Truth
# ============================================

echo "🚨 BLOCKING CHECK 1: Single Source of Truth Validation"
echo "========================================================"
echo ""
echo "Checking ALL skills for duplication violations..."
echo "Any violations will BLOCK finalization."
echo ""

VIOLATIONS_FOUND=0
SKILLS_DIR=".claude/skills"

# Find all skill directories with SKILL.md files
SKILL_DIRS=$(find "$SKILLS_DIR" -mindepth 1 -maxdepth 1 -type d 2>/dev/null || true)

SKILL_COUNT=0
for SKILL_DIR in $SKILL_DIRS; do
    if [ -f "$SKILL_DIR/SKILL.md" ]; then
        ((SKILL_COUNT++))
    fi
done

if [ "$SKILL_COUNT" -eq 0 ]; then
    echo "ℹ️  No skills to validate"
else
    echo "Validating $SKILL_COUNT skills for duplication..."
    echo ""

    # Validate each skill
    for SKILL_DIR in $SKILL_DIRS; do
        if [ -f "$SKILL_DIR/SKILL.md" ]; then
            SKILL_NAME=$(basename "$SKILL_DIR")

            echo "   Checking: $SKILL_NAME"

            # Check if skill has embedded bash scripts (should use execute.sh instead)
            BASH_BLOCKS=$(grep -c '```bash' "$SKILL_DIR/SKILL.md" 2>/dev/null || echo "0")

            # Allow up to 2 bash blocks for usage examples
            if [ "$BASH_BLOCKS" -gt 2 ]; then
                echo "   ❌ VIOLATION: $SKILL_NAME has $BASH_BLOCKS bash code blocks"
                echo "      Skills should use execute.sh for automation"
                ((VIOLATIONS_FOUND++))
            else
                echo "   ✅ $SKILL_NAME maintains single source of truth"
            fi
        fi
    done

    echo ""

    if [ $VIOLATIONS_FOUND -gt 0 ]; then
        echo "❌ ❌ ❌ FINALIZATION BLOCKED ❌ ❌ ❌"
        echo ""
        echo "🚨 CRITICAL: $VIOLATIONS_FOUND skill(s) have duplication violations!"
        echo ""
        echo "Single source of truth violations detected."
        echo "Skills must use execute.sh for automation, not embedded bash scripts."
        echo ""
        echo "Required actions:"
        echo "1. Review violations reported above"
        echo "2. For each violation:"
        echo "   - Extract bash code to execute.sh"
        echo "   - Update SKILL.md with 'How to Use This Skill' section"
        echo "   - Remove embedded bash blocks (keep only usage examples)"
        echo "3. Run validation again to confirm fixes"
        echo ""
        echo "Workflow CANNOT complete until violations resolved."
        echo ""
        exit 1
    fi

    echo "✅ ✅ ✅ SINGLE SOURCE OF TRUTH MAINTAINED ✅ ✅ ✅"
    echo ""
    echo "All $SKILL_COUNT skills validated:"
    echo "• No bash command duplication"
    echo "• Proper skill structure in place"
    echo ""
fi

echo ""

# ============================================
# BLOCKING CHECK 2: Lessons Learned Size
# ============================================

echo "🚨 BLOCKING CHECK 2: Lessons Learned Size Validation"
echo "====================================================="
echo ""
echo "Checking ALL lessons files for size violations..."
echo "Files exceeding 2,000 lines will BLOCK finalization."
echo ""

OVERSIZED_FILES=0
WARNING_FILES=0
LESSONS_DIR="docs/lessons-learned"
MAX_LINES=2000
WARNING_THRESHOLD=1800  # 90% of max

# Find all lessons learned files
LESSONS_FILES=$(find "$LESSONS_DIR" -name "*-lessons-learned*.md" -type f 2>/dev/null || true)

LESSONS_COUNT=$(echo "$LESSONS_FILES" | grep -c ".md" 2>/dev/null || echo "0")

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
        echo "Lessons learned files have a 2,000 line maximum to prevent:"
        echo "• File read errors (Claude's token limits)"
        echo "• Difficulty finding specific lessons"
        echo "• Maintenance overhead"
        echo ""
        echo "Required actions:"
        echo "1. Review oversized files listed above"
        echo "2. Split files exceeding 2,000 lines:"
        echo "   - Create Part 2/3/N: [filename]-2.md, [filename]-3.md"
        echo "   - Add multi-file header to all parts"
        echo "   - Move recent lessons to new part"
        echo "   - Keep each part under 2,000 lines"
        echo "3. Use lessons-learned-validator skill to verify"
        echo ""
        echo "Workflow CANNOT complete until files are split."
        echo ""
        exit 1
    fi

    if [ $WARNING_FILES -gt 0 ]; then
        echo "⚠️  WARNING: $WARNING_FILES file(s) approaching size limit"
        echo ""
        echo "Files over 90% of limit (1,800+ lines) should be split soon."
        echo "Plan file splits before they block workflow."
        echo ""
    fi

    echo "✅ ✅ ✅ LESSONS FILE SIZES OK ✅ ✅ ✅"
    echo ""
    echo "All $LESSONS_COUNT lessons files validated:"
    echo "• No files exceeding 2,000 lines"
    if [ $WARNING_FILES -gt 0 ]; then
        echo "• $WARNING_FILES file(s) approaching limit (warnings issued)"
    else
        echo "• All files below 90% threshold"
    fi
    echo ""
fi

echo ""

# ============================================
# MAIN VALIDATION (Non-Blocking)
# ============================================

SCORE=0
MAX_SCORE=100
REQUIRED_PERCENTAGE=80

echo "📝 Validation Details:"
echo "   Required Score: ${REQUIRED_PERCENTAGE}%"
echo "   Max Score: $MAX_SCORE points"
echo ""
echo "Running non-blocking validation checks..."
echo ""

# Git & Version Control (25 points)
echo "📦 Git & Version Control:"
echo ""

# Uncommitted changes
UNCOMMITTED=$(git status --short | wc -l)
if [ "$UNCOMMITTED" -eq 0 ]; then
    echo "   ✅ All changes committed (+10 points)"
    ((SCORE+=10))
else
    echo "   ⚠️  Uncommitted changes: $UNCOMMITTED files (+5 points)"
    echo "      Run: git status"
    ((SCORE+=5))
fi

# Commit message quality
LATEST_COMMIT=$(git log -1 --pretty=%B 2>/dev/null || echo "")
if echo "$LATEST_COMMIT" | grep -q "feat:\|fix:\|refactor:\|docs:\|test:"; then
    echo "   ✅ Commit message follows standards (+5 points)"
    ((SCORE+=5))
else
    echo "   ⚠️  Commit message doesn't follow conventional format (+3 points)"
    ((SCORE+=3))
fi

# Branch up to date
BEHIND=$(git rev-list HEAD..@{u} --count 2>/dev/null || echo "0")
if [ "$BEHIND" -eq 0 ]; then
    echo "   ✅ Branch is up to date (+5 points)"
    ((SCORE+=5))
else
    echo "   ⚠️  Branch is $BEHIND commits behind (+3 points)"
    ((SCORE+=3))
fi

# Merge conflicts
if git diff --check > /dev/null 2>&1; then
    echo "   ✅ No merge conflicts (+5 points)"
    ((SCORE+=5))
else
    echo "   ❌ Merge conflicts detected"
fi

echo ""

# Documentation (30 points)
echo "📚 Documentation Validation:"
echo ""

# File registry
FILE_REGISTRY="docs/architecture/file-registry.md"
if [ -f "$FILE_REGISTRY" ]; then
    REGISTRY_UPDATED=$(find "$FILE_REGISTRY" -mtime -1 2>/dev/null)
    if [ -n "$REGISTRY_UPDATED" ]; then
        RECENT_ENTRIES=$(tail -20 "$FILE_REGISTRY" | grep -c "$(date +%Y-%m-%d)" || echo "0")
        if [ "$RECENT_ENTRIES" -gt 0 ]; then
            echo "   ✅ File registry updated ($RECENT_ENTRIES new entries) (+10 points)"
            ((SCORE+=10))
        else
            echo "   ⚠️  File registry modified but no entries for today (+5 points)"
            ((SCORE+=5))
        fi
    else
        echo "   ❌ File registry not updated"
        echo "      CRITICAL: All file operations must be logged"
    fi
else
    echo "   ❌ File registry not found"
fi

# PROGRESS.md
if [ -f "PROGRESS.md" ]; then
    if find PROGRESS.md -mtime -1 2>/dev/null | grep -q .; then
        echo "   ✅ PROGRESS.md updated (+5 points)"
        ((SCORE+=5))
    else
        echo "   ⚠️  PROGRESS.md not updated (+2 points)"
        ((SCORE+=2))
    fi
else
    echo "   ❌ PROGRESS.md not found"
fi

# Feature documentation
NEW_WORK_DIR=$(find docs/functional-areas -type d -path "*/new-work/*" 2>/dev/null | sort | tail -1)
if [ -n "$NEW_WORK_DIR" ]; then
    DOC_COUNT=$(find "$NEW_WORK_DIR" -name "*.md" 2>/dev/null | wc -l)
    if [ "$DOC_COUNT" -ge 3 ]; then
        echo "   ✅ Feature documentation complete ($DOC_COUNT documents) (+10 points)"
        ((SCORE+=10))
    else
        echo "   ⚠️  Limited documentation ($DOC_COUNT documents) (+5 points)"
        ((SCORE+=5))
    fi
else
    echo "   ⚠️  No new-work directory found"
fi

# CLAUDE.md updates
if [ -n "$(git diff HEAD~1 CLAUDE.md 2>/dev/null)" ]; then
    echo "   ✅ CLAUDE.md updated (project changes) (+5 points)"
    ((SCORE+=5))
else
    echo "   ℹ️  CLAUDE.md not updated (may not be needed) (+5 points)"
    ((SCORE+=5))  # Not always needed
fi

echo ""

# Lessons Learned (20 points)
echo "📖 Lessons Learned Validation:"
echo ""

# Check for new lessons
LESSONS_MODIFIED=$(find docs/lessons-learned -name "*.md" -mtime -1 2>/dev/null | wc -l)
if [ "$LESSONS_MODIFIED" -gt 0 ]; then
    echo "   ✅ New lessons documented ($LESSONS_MODIFIED files) (+10 points)"
    ((SCORE+=10))
else
    echo "   ⚠️  No new lessons documented (+5 points)"
    echo "      Did this work generate any learnings?"
    ((SCORE+=5))
fi

# Lesson format check
if [ "$LESSONS_MODIFIED" -gt 0 ]; then
    NEWEST_LESSON=$(find docs/lessons-learned -name "*.md" -mtime -1 2>/dev/null | head -1)
    if [ -n "$NEWEST_LESSON" ] && grep -q "## Problem\|## Solution\|## Example" "$NEWEST_LESSON"; then
        echo "   ✅ Lessons follow standard format (+5 points)"
        ((SCORE+=5))
    else
        echo "   ⚠️  Lessons don't follow format (+2 points)"
        ((SCORE+=2))
    fi

    # Cross-references
    if [ -n "$NEWEST_LESSON" ] && grep -q "See:\|Related:" "$NEWEST_LESSON"; then
        echo "   ✅ Cross-references present (+5 points)"
        ((SCORE+=5))
    else
        echo "   ⚠️  No cross-references (+3 points)"
        ((SCORE+=3))
    fi
else
    ((SCORE+=10))  # Skip if no lessons
fi

echo ""

# Deployment Readiness (15 points)
echo "🚀 Deployment Readiness:"
echo ""

# Tests passing
if [ -f "test-results/test-execution-report.md" ]; then
    if grep -q "Status: PASS" "test-results/test-execution-report.md"; then
        echo "   ✅ All tests passing (+5 points)"
        ((SCORE+=5))
    else
        echo "   ❌ Tests not passing"
        echo "      Cannot finalize with failing tests"
        exit 1
    fi
else
    echo "   ⚠️  Test results not found (+2 points)"
    echo "      Run: phase-4-validator skill first"
    ((SCORE+=2))
fi

# Blocking issues
if [ -n "$NEW_WORK_DIR" ] && [ -f "$NEW_WORK_DIR/implementation-notes.md" ]; then
    BLOCKING_COUNT=$(grep -c "BLOCKING\|CRITICAL\|MUST FIX" "$NEW_WORK_DIR/implementation-notes.md" 2>/dev/null || echo "0")
    if [ "$BLOCKING_COUNT" -eq 0 ]; then
        echo "   ✅ No blocking issues (+5 points)"
        ((SCORE+=5))
    else
        echo "   ❌ $BLOCKING_COUNT blocking issues found"
        echo "      Must resolve before finalizing"
        exit 1
    fi
else
    echo "   ℹ️  Implementation notes not found (+3 points)"
    ((SCORE+=3))
fi

# Deployment notes
if [ -n "$NEW_WORK_DIR" ] && [ -f "$NEW_WORK_DIR/deployment-notes.md" ]; then
    echo "   ✅ Deployment notes present (+5 points)"
    ((SCORE+=5))
else
    echo "   ⚠️  Deployment notes missing (+2 points)"
    echo "      Should document: migrations, config changes, manual steps"
    ((SCORE+=2))
fi

echo ""

# Cleanup (10 points)
echo "🧹 Cleanup Validation:"
echo ""

# Temporary files
TEMP_FILES=$(find . -name "*.tmp" -o -name "*.bak" -o -name "*~" 2>/dev/null | wc -l)
if [ "$TEMP_FILES" -eq 0 ]; then
    echo "   ✅ No temporary files (+3 points)"
    ((SCORE+=3))
else
    echo "   ⚠️  Temporary files found: $TEMP_FILES (+1 point)"
    ((SCORE+=1))
fi

# Session work archived
if [ -d "session-work" ]; then
    RECENT_SESSION=$(find session-work -type d -name "$(date +%Y-%m-%d)" 2>/dev/null)
    if [ -n "$RECENT_SESSION" ]; then
        echo "   ✅ Session work organized (+3 points)"
        ((SCORE+=3))
    else
        echo "   ⚠️  Session work not organized for today (+1 point)"
        ((SCORE+=1))
    fi
else
    echo "   ℹ️  No session-work directory (+3 points)"
    ((SCORE+=3))
fi

# Orphaned files check
ORPHANED=$(find . -maxdepth 1 -type f -name "*.md" ! -name "README.md" ! -name "PROGRESS.md" ! -name "ARCHITECTURE.md" ! -name "CLAUDE.md" ! -name "DOCKER_DEV_GUIDE.md" ! -name "MARKETPLACE-README.md" 2>/dev/null | wc -l)
if [ "$ORPHANED" -eq 0 ]; then
    echo "   ✅ No orphaned files in root (+4 points)"
    ((SCORE+=4))
else
    echo "   ⚠️  Orphaned files in root: $ORPHANED (+1 point)"
    echo "      Move to appropriate location or session-work/"
    ((SCORE+=1))
fi

echo ""

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo "================================"
echo "📊 Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo "   Required: ${REQUIRED_PERCENTAGE}%"
echo ""

if [ "$PERCENTAGE" -ge "$REQUIRED_PERCENTAGE" ]; then
    echo "✅ PASS - Finalization Phase Complete"
    echo "======================================"
    echo ""
    echo "   ✅ Single source of truth maintained"
    echo "   ✅ Lessons learned files under size limit"
    echo "   ✅ Work is properly documented and ready"
    echo ""
    echo "🎯 Next Steps:"
    echo "   1. Deploy to staging if approved"
    echo "   2. Close workflow"
    echo "   3. Update project status"
    echo "   4. Archive session work"
    echo ""
    exit 0
else
    echo "❌ FAIL - Finalization Phase Incomplete"
    echo "========================================"
    echo ""
    echo "   Score: $PERCENTAGE% (need ${REQUIRED_PERCENTAGE}%)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    echo ""
    echo "🎯 Common issues:"
    echo "   • File registry not updated"
    echo "   • Changes not committed"
    echo "   • Documentation incomplete"
    echo "   • Lessons learned missing"
    echo ""
    echo "📖 See: .claude/skills/phase-5-validator/SKILL.md (Common Issues)"
    echo ""
    exit 1
fi
