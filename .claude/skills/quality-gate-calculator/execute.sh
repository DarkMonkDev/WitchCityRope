#!/bin/bash
# Quality Gate Calculator Script
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# Calculates context-appropriate quality gate thresholds based on work type and phase

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

if [ "$#" -lt 2 ]; then
    echo "📊 Quality Gate Calculator"
    echo "=========================="
    echo ""
    echo "📋 Purpose: Calculate quality gate thresholds by work type and phase"
    echo ""
    echo "✅ Use when:"
    echo "   • Before running phase validators"
    echo "   • Need to know required percentage for a work type"
    echo "   • Planning quality gates for new work"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • Already know the quality gate threshold"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. Accepts work type and phase number"
    echo "   2. Calculates required percentage and target score"
    echo "   3. Provides rationale for thresholds"
    echo "   4. Exports quality gate to /tmp/quality-gate.env"
    echo "   5. Shows pass/fail examples"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <work-type> <phase>"
    echo ""
    echo "   work-type: Feature|Bug|Hotfix|Documentation|Refactoring"
    echo "   phase: 1|2|3|4|5"
    echo ""
    echo "   Examples:"
    echo "   bash execute.sh Feature 3"
    echo "   bash execute.sh Hotfix 1"
    echo "   bash execute.sh Bug 4"
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

WORK_TYPE="$1"
PHASE="$2"

# Normalize inputs
WORK_TYPE=$(echo "$WORK_TYPE" | tr '[:upper:]' '[:lower:]')
PHASE=$(echo "$PHASE" | tr -d 'Phase ')

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "📊 Quality Gate Calculator"
echo "=========================="
echo ""

echo "1️⃣  Validating work type..."
if ! echo "$WORK_TYPE" | grep -qE "^(feature|bug|bugfix|hotfix|documentation|docs|refactoring|refactor)$"; then
    echo "   ❌ FAIL: Unknown work type: $WORK_TYPE"
    echo "   Valid: Feature, Bug, Hotfix, Documentation, Refactoring"
    exit 1
fi
echo "   ✅ Work type valid"

echo ""
echo "2️⃣  Validating phase..."
if ! echo "$PHASE" | grep -qE "^[1-5]$"; then
    echo "   ❌ FAIL: Invalid phase: $PHASE"
    echo "   Valid: 1, 2, 3, 4, 5"
    exit 1
fi
echo "   ✅ Phase valid"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - CALCULATION
# ============================================

echo "Work Type: $WORK_TYPE"
echo "Phase: $PHASE"
echo ""

# Define quality gates by phase and work type
case "$PHASE" in
    "1")
        MAX_SCORE=25
        case "$WORK_TYPE" in
            "feature") REQUIRED=95; TARGET=24 ;;
            "bug"|"bugfix") REQUIRED=80; TARGET=20 ;;
            "hotfix") REQUIRED=70; TARGET=18 ;;
            "documentation"|"docs") REQUIRED=85; TARGET=21 ;;
            "refactoring"|"refactor") REQUIRED=90; TARGET=23 ;;
        esac
        PHASE_NAME="Requirements"
        ;;

    "2")
        MAX_SCORE=35
        case "$WORK_TYPE" in
            "feature") REQUIRED=90; TARGET=32 ;;
            "bug"|"bugfix") REQUIRED=70; TARGET=25 ;;
            "hotfix") REQUIRED=60; TARGET=21 ;;
            "documentation"|"docs") REQUIRED=80; TARGET=28 ;;
            "refactoring"|"refactor") REQUIRED=85; TARGET=30 ;;
        esac
        PHASE_NAME="Design"
        ;;

    "3")
        MAX_SCORE=50
        case "$WORK_TYPE" in
            "feature") REQUIRED=85; TARGET=43 ;;
            "bug"|"bugfix") REQUIRED=75; TARGET=38 ;;
            "hotfix") REQUIRED=70; TARGET=35 ;;
            "documentation"|"docs") REQUIRED=80; TARGET=40 ;;
            "refactoring"|"refactor") REQUIRED=90; TARGET=45 ;;
        esac
        PHASE_NAME="Implementation"
        ;;

    "4")
        MAX_SCORE=100
        # ALL work types require 100%
        REQUIRED=100
        TARGET=100
        PHASE_NAME="Testing"
        ;;

    "5")
        MAX_SCORE=100
        case "$WORK_TYPE" in
            "feature") REQUIRED=80; TARGET=80 ;;
            "bug"|"bugfix") REQUIRED=75; TARGET=75 ;;
            "hotfix") REQUIRED=70; TARGET=70 ;;
            "documentation"|"docs") REQUIRED=90; TARGET=90 ;;
            "refactoring"|"refactor") REQUIRED=85; TARGET=85 ;;
        esac
        PHASE_NAME="Finalization"
        ;;
esac

# Output quality gate
echo "📋 Quality Gate for Phase $PHASE ($PHASE_NAME)"
echo "=============================================="
echo ""
echo "Work Type: $(echo $WORK_TYPE | sed 's/\b\(.\)/\u\1/g')"
echo "Required: $REQUIRED%"
echo "Target Score: $TARGET / $MAX_SCORE points"
echo ""

# Provide context-specific guidance
if [ "$PHASE" -eq 4 ]; then
    echo "🚨 CRITICAL: Phase 4 requires 100% test pass rate"
    echo "   NO EXCEPTIONS for any work type"
    echo "   One failing test = Phase 4 fails"
    echo ""
fi

# Output rationale
echo "📖 Rationale:"
case "$WORK_TYPE" in
    "feature")
        echo "   Features require highest rigor due to:"
        echo "   • New functionality introduction"
        echo "   • Higher risk of bugs"
        echo "   • Long-term maintenance impact"
        ;;
    "bug"|"bugfix")
        echo "   Bug fixes have moderate rigor:"
        echo "   • Focus on problem and solution"
        echo "   • Less ceremony than features"
        echo "   • Still requires thorough testing"
        ;;
    "hotfix")
        echo "   Hotfixes have minimal rigor:"
        echo "   • Production emergency"
        echo "   • Speed is critical"
        echo "   • Still requires 100% test pass in Phase 4"
        ;;
    "documentation"|"docs")
        echo "   Documentation requires high completion:"
        echo "   • Clear structure and organization"
        echo "   • Complete coverage of topic"
        echo "   • Examples and cross-references"
        ;;
    "refactoring"|"refactor")
        echo "   Refactoring requires highest quality:"
        echo "   • No behavior changes allowed"
        echo "   • Comprehensive test coverage"
        echo "   • Performance validation"
        ;;
esac

echo ""
echo "✅ Pass/Fail Example:"
echo "   Score of $TARGET = PASS (exactly $REQUIRED%)"
if [ "$TARGET" -gt 1 ]; then
    FAIL_SCORE=$((TARGET - 1))
    FAIL_PERCENTAGE=$(( FAIL_SCORE * 100 / MAX_SCORE))
    echo "   Score of $FAIL_SCORE = FAIL ($FAIL_PERCENTAGE%)"
fi
echo ""

# Export variables for use in validation scripts
cat > /tmp/quality-gate.env <<EOF
WORK_TYPE=$WORK_TYPE
PHASE=$PHASE
REQUIRED_PERCENTAGE=$REQUIRED
TARGET_SCORE=$TARGET
MAX_SCORE=$MAX_SCORE
PHASE_NAME=$PHASE_NAME
EOF

echo "✅ Quality gate calculated and exported"
echo "   File: /tmp/quality-gate.env"
echo ""
echo "   Use in validators:"
echo "   source /tmp/quality-gate.env"
echo "   echo \$REQUIRED_PERCENTAGE"
echo ""

exit 0
