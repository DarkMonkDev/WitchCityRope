#!/bin/bash
# Lessons Learned Validator
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# Complete lessons learned standards, validation, and multi-file management.
# Single source of truth for all lessons learned operations including format,
# size limits, split procedures, and quality standards.
# BLOCKING AUTHORITY - can prevent workflow completion for oversized files.

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

if [ "$#" -eq 0 ] || [ "$1" == "--help" ]; then
    echo "📖 Lessons Learned Validator"
    echo "============================="
    echo ""
    echo "📋 Purpose: Validate lessons learned format, size, and quality standards"
    echo ""
    echo "🚨 BLOCKING AUTHORITY: This validator can BLOCK workflow completion for:"
    echo "   • Files exceeding 2,000 lines (max file size)"
    echo "   • Too many outdated lessons (>5 marked as outdated)"
    echo ""
    echo "✅ Use when:"
    echo "   • Before committing lesson updates"
    echo "   • During Phase 5 finalization (MANDATORY)"
    echo "   • When adding new lessons"
    echo "   • When files approach size limits (1,800+ lines)"
    echo "   • When splitting files"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • File doesn't exist yet (create it first)"
    echo "   • Just checking general documentation (use other validators)"
    echo ""
    echo "📏 File Size Limits (CRITICAL):"
    echo "   • Maximum: 2,000 lines per file"
    echo "   • Warning threshold: 1,800 lines (90% of max)"
    echo "   • WHY: Prevents file read errors due to token limits"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. 🚨 BLOCKING: Checks file size (max 2,000 lines)"
    echo "   2. Validates file location and naming"
    echo "   3. Checks multi-file structure headers"
    echo "   4. Validates Problem/Solution/Example format"
    echo "   5. Checks prevention-focused language"
    echo "   6. Validates code blocks and examples"
    echo "   7. Checks cross-references"
    echo "   8. Scores 0-100 (80% required to pass)"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <lessons-file>"
    echo ""
    echo "   lessons-file: Path to lessons learned file"
    echo ""
    echo "   Examples:"
    echo "   bash execute.sh docs/lessons-learned/test-executor-lessons-learned.md"
    echo "   bash execute.sh docs/lessons-learned/react-developer-lessons-learned-2.md"
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

LESSONS_FILE="$1"

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "📖 Lessons Learned Validator"
echo "============================="
echo ""

echo "1️⃣  Checking file exists..."
if [ ! -f "$LESSONS_FILE" ]; then
    echo "   ❌ FAIL: File not found: $LESSONS_FILE"
    exit 1
fi
echo "   ✅ File found: $LESSONS_FILE"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - VALIDATION
# ============================================

SCORE=0
MAX_SCORE=100

echo "File: $LESSONS_FILE"
echo ""

# Structure validation (20 points)
echo "📁 Structure Validation"
echo "--------------------"

# File location
if [[ "$LESSONS_FILE" == *"/docs/lessons-learned/"* ]]; then
    echo "✅ File in correct location (+5 points)"
    ((SCORE+=5))
else
    echo "❌ File not in /docs/lessons-learned/"
fi

# Filename pattern
BASENAME=$(basename "$LESSONS_FILE")
if [[ "$BASENAME" =~ ^[a-z-]+-lessons-learned\.md$ ]] || [[ "$BASENAME" =~ ^[a-z-]+-lessons-learned-[0-9]+\.md$ ]]; then
    echo "✅ Filename follows pattern (+3 points)"
    ((SCORE+=3))
else
    echo "❌ Filename doesn't follow pattern: [role]-lessons-learned.md"
fi

# File size check (LINE COUNT - CRITICAL)
LINE_COUNT=$(wc -l < "$LESSONS_FILE")
MAX_LINES=2000
WARNING_THRESHOLD=1800  # 90% of max

echo "File size: $LINE_COUNT lines (max: $MAX_LINES)"

if [ "$LINE_COUNT" -gt "$MAX_LINES" ]; then
    echo "❌ CRITICAL: File exceeds maximum line count"
    echo "   Current: $LINE_COUNT lines"
    echo "   Maximum: $MAX_LINES lines"
    echo "   MANDATORY: Split file immediately"
    # No points awarded - this is a blocking violation
elif [ "$LINE_COUNT" -gt "$WARNING_THRESHOLD" ]; then
    echo "⚠️  WARNING: Approaching line limit"
    echo "   Current: $LINE_COUNT lines ($((LINE_COUNT * 100 / MAX_LINES))% of limit)"
    echo "   Warning threshold: $WARNING_THRESHOLD lines"
    echo "   Action: Plan file split soon"
    ((SCORE+=2))
else
    echo "✅ Line count within limits ($LINE_COUNT / $MAX_LINES lines) (+4 points)"
    ((SCORE+=4))
fi

# Multi-file structure validation
if [ "$LINE_COUNT" -gt 1000 ] || [[ "$BASENAME" =~ -[0-9]+\.md$ ]]; then
    if grep -q "## 📚 MULTI-FILE LESSONS LEARNED" "$LESSONS_FILE" || grep -q "Part [0-9]" "$LESSONS_FILE"; then
        echo "✅ Multi-file structure documented (+4 points)"
        ((SCORE+=4))
    else
        echo "⚠️  Large file or multi-part without proper header (+2 points)"
        echo "   Add multi-file structure header"
        ((SCORE+=2))
    fi
else
    ((SCORE+=4))  # Not applicable for small files
fi

# Table of contents
if grep -q "## Table of Contents" "$LESSONS_FILE" || grep -q "## Contents" "$LESSONS_FILE"; then
    echo "✅ Table of contents present (+4 points)"
    ((SCORE+=4))
else
    echo "⚠️  No table of contents (+2 points)"
    ((SCORE+=2))
fi

echo ""

# Format compliance (30 points)
echo "📝 Format Compliance"
echo "-----------------"

# Count lessons (## headings excluding ToC, Conclusion, etc.)
LESSON_COUNT=$(grep -c "^## [^#]" "$LESSONS_FILE" | grep -v "Table of Contents\|Conclusion\|Multi-File" || echo 0)
echo "Found $LESSON_COUNT lessons"

if [ "$LESSON_COUNT" -eq 0 ]; then
    echo "❌ No lessons found"
    exit 1
fi

# Check for Problem/Solution/Example pattern
PROBLEM_COUNT=$(grep -c "### Problem\|**Problem:**" "$LESSONS_FILE" || echo 0)
SOLUTION_COUNT=$(grep -c "### Solution\|**Solution:**" "$LESSONS_FILE" || echo 0)
EXAMPLE_COUNT=$(grep -c "### Example\|**Example:**" "$LESSONS_FILE" || echo 0)

if [ "$PROBLEM_COUNT" -ge 3 ]; then
    echo "✅ Problem sections present ($PROBLEM_COUNT) (+7 points)"
    ((SCORE+=7))
elif [ "$PROBLEM_COUNT" -gt 0 ]; then
    echo "⚠️  Limited Problem sections ($PROBLEM_COUNT) (+3 points)"
    ((SCORE+=3))
else
    echo "❌ No Problem sections found"
fi

if [ "$SOLUTION_COUNT" -ge 3 ]; then
    echo "✅ Solution sections present ($SOLUTION_COUNT) (+8 points)"
    ((SCORE+=8))
elif [ "$SOLUTION_COUNT" -gt 0 ]; then
    echo "⚠️  Limited Solution sections ($SOLUTION_COUNT) (+4 points)"
    ((SCORE+=4))
else
    echo "❌ No Solution sections found"
fi

if [ "$EXAMPLE_COUNT" -ge 3 ]; then
    echo "✅ Example sections present ($EXAMPLE_COUNT) (+7 points)"
    ((SCORE+=7))
elif [ "$EXAMPLE_COUNT" -gt 0 ]; then
    echo "⚠️  Limited Example sections ($EXAMPLE_COUNT) (+3 points)"
    ((SCORE+=3))
else
    echo "❌ No Example sections found"
fi

# Prevention-focused language
PREVENTION_KEYWORDS=$(grep -ic "prevent\|avoid\|don't\|never\|must not\|instead of" "$LESSONS_FILE" || echo 0)
if [ "$PREVENTION_KEYWORDS" -ge 5 ]; then
    echo "✅ Prevention-focused language ($PREVENTION_KEYWORDS keywords) (+4 points)"
    ((SCORE+=4))
else
    echo "⚠️  Limited prevention focus (+2 points)"
    ((SCORE+=2))
fi

# Tags check
TAGS_COUNT=$(grep -c "#critical\|#process\|#tooling\|#debugging" "$LESSONS_FILE" || echo 0)
if [ "$TAGS_COUNT" -ge 3 ]; then
    echo "✅ Tags present ($TAGS_COUNT) (+4 points)"
    ((SCORE+=4))
else
    echo "⚠️  Limited tags (+2 points)"
    ((SCORE+=2))
fi

echo ""

# Content quality (30 points)
echo "📊 Content Quality"
echo "---------------"

# Problem specificity (check for code examples, error messages, file paths)
SPECIFIC_INDICATORS=$(grep -c "Error:\|file:\|line:\|\`\`\`\|TypeError\|Exception" "$LESSONS_FILE" || echo 0)
if [ "$SPECIFIC_INDICATORS" -ge 5 ]; then
    echo "✅ Problems are specific ($SPECIFIC_INDICATORS specific indicators) (+7 points)"
    ((SCORE+=7))
else
    echo "⚠️  Problems could be more specific (+4 points)"
    ((SCORE+=4))
fi

# Solution actionability (check for concrete actions)
ACTION_KEYWORDS=$(grep -ic "run\|execute\|add\|remove\|change\|update\|create\|delete\|fix" "$LESSONS_FILE" || echo 0)
if [ "$ACTION_KEYWORDS" -ge 10 ]; then
    echo "✅ Solutions are actionable ($ACTION_KEYWORDS action verbs) (+8 points)"
    ((SCORE+=8))
else
    echo "⚠️  Solutions could be more actionable (+5 points)"
    ((SCORE+=5))
fi

# Example concreteness (code blocks, commands, file paths)
CODE_BLOCKS=$(grep -c "^\`\`\`" "$LESSONS_FILE" || echo 0)
COMMAND_EXAMPLES=$(grep -c "^\$ \|^# \|bash" "$LESSONS_FILE" || echo 0)
EXAMPLE_SCORE=$((CODE_BLOCKS + COMMAND_EXAMPLES))

if [ "$EXAMPLE_SCORE" -ge 5 ]; then
    echo "✅ Examples are concrete ($CODE_BLOCKS code blocks, $COMMAND_EXAMPLES commands) (+7 points)"
    ((SCORE+=7))
else
    echo "⚠️  Examples could be more concrete (+4 points)"
    ((SCORE+=4))
fi

# Maintainability (date references, version numbers)
DATE_REFERENCES=$(grep -c "2025\|2024\|Date:" "$LESSONS_FILE" || echo 0)
if [ "$DATE_REFERENCES" -gt 0 ]; then
    echo "✅ Lessons have timestamps (+4 points)"
    ((SCORE+=4))
else
    echo "⚠️  No timestamps found (+2 points)"
    ((SCORE+=2))
fi

# Cross-references
CROSS_REFS=$(grep -c "See:\|Related:\|docs/\|Also:" "$LESSONS_FILE" || echo 0)
if [ "$CROSS_REFS" -ge 3 ]; then
    echo "✅ Cross-references present ($CROSS_REFS) (+4 points)"
    ((SCORE+=4))
else
    echo "⚠️  Limited cross-references (+2 points)"
    ((SCORE+=2))
fi

echo ""

# Maintenance (20 points)
echo "🔧 Maintenance Validation"
echo "----------------------"

# Recent updates
LAST_MODIFIED=$(find "$LESSONS_FILE" -mtime -30 2>/dev/null)
if [ -n "$LAST_MODIFIED" ]; then
    echo "✅ Recently updated (within 30 days) (+5 points)"
    ((SCORE+=5))
else
    echo "⚠️  Not updated recently (>30 days) (+2 points)"
    ((SCORE+=2))
fi

# Outdated markers
OUTDATED_COUNT=$(grep -ic "outdated\|deprecated\|no longer\|obsolete" "$LESSONS_FILE" || echo 0)
if [ "$OUTDATED_COUNT" -eq 0 ]; then
    echo "✅ No outdated lessons marked (+5 points)"
    ((SCORE+=5))
else
    echo "⚠️  $OUTDATED_COUNT lessons marked as outdated (+2 points)"
    echo "   Remove or update these lessons"
    ((SCORE+=2))
fi

# Duplicate detection (simple check for repeated ## headings)
DUPLICATE_HEADINGS=$(grep "^## " "$LESSONS_FILE" | sort | uniq -d | wc -l)
if [ "$DUPLICATE_HEADINGS" -eq 0 ]; then
    echo "✅ No duplicate lesson titles (+5 points)"
    ((SCORE+=5))
else
    echo "⚠️  $DUPLICATE_HEADINGS duplicate lesson titles (+2 points)"
    echo "   Consolidate duplicate lessons"
    ((SCORE+=2))
fi

# File size monitoring (already calculated)
PERCENTAGE_USED=$((LINE_COUNT * 100 / MAX_LINES))

if [ "$LINE_COUNT" -gt "$MAX_LINES" ]; then
    echo "❌ File exceeds maximum ($LINE_COUNT / $MAX_LINES lines - ${PERCENTAGE_USED}%)"
    echo "   CRITICAL: Must split immediately"
    # No points - blocking violation
elif [ "$LINE_COUNT" -gt "$WARNING_THRESHOLD" ]; then
    echo "⚠️  File approaching limit ($LINE_COUNT / $MAX_LINES lines - ${PERCENTAGE_USED}%) (+3 points)"
    echo "   Action: Plan split soon (over 90%)"
    ((SCORE+=3))
else
    echo "✅ File size healthy ($LINE_COUNT / $MAX_LINES lines - ${PERCENTAGE_USED}%) (+5 points)"
    ((SCORE+=5))
fi

echo ""

# BLOCKING VALIDATION: Check for critical violations BEFORE scoring
echo "🚨 Critical Violations Check"
echo "============================="

BLOCKING_VIOLATIONS=0

# Check if file exceeds maximum line count
if [ "$LINE_COUNT" -gt "$MAX_LINES" ]; then
    echo "❌ BLOCKING: File exceeds maximum line count ($LINE_COUNT > $MAX_LINES)"
    ((BLOCKING_VIOLATIONS++))
fi

# Check if outdated lessons are present
if [ "$OUTDATED_COUNT" -gt 5 ]; then
    echo "❌ BLOCKING: Too many outdated lessons ($OUTDATED_COUNT)"
    ((BLOCKING_VIOLATIONS++))
fi

if [ "$BLOCKING_VIOLATIONS" -gt 0 ]; then
    echo ""
    echo "❌ ❌ ❌ VALIDATION BLOCKED ❌ ❌ ❌"
    echo ""
    echo "Critical violations found: $BLOCKING_VIOLATIONS"
    echo ""
    echo "Actions required:"
    if [ "$LINE_COUNT" -gt "$MAX_LINES" ]; then
        echo "1. Split file immediately (exceeds $MAX_LINES line limit)"
        echo "   See 'Split Procedure' section in SKILL.md for instructions"
        echo ""
        echo "   Quick steps:"
        echo "   - Create next part: ${LESSONS_FILE%.md}-N.md"
        echo "   - Add multi-file header to new part"
        echo "   - Move recent $((LINE_COUNT - 1800)) lines to new part"
        echo "   - Update Part 1 header with new file count"
        echo "   - Verify all parts under 2,000 lines"
    fi
    if [ "$OUTDATED_COUNT" -gt 5 ]; then
        echo "2. Remove or update outdated lessons"
    fi
    echo ""
    echo "Cannot proceed with workflow until resolved."
    exit 1
fi

echo "✅ No blocking violations"
echo ""

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo "================================"
echo "📊 Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo ""

if [ "$PERCENTAGE" -ge 80 ]; then
    echo "✅ PASS - Lessons learned file meets quality standards"
    echo ""
    if [ "$LINE_COUNT" -gt "$WARNING_THRESHOLD" ]; then
        echo "⚠️  Note: File approaching size limit ($LINE_COUNT / $MAX_LINES lines)"
        echo "   Consider planning file split soon"
    fi
    exit 0
else
    echo "❌ FAIL - Lessons learned file needs improvement"
    echo "   Score: $PERCENTAGE% (need 80%+)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    exit 1
fi
