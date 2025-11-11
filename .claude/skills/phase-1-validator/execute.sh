#!/bin/bash
# Phase 1 (Requirements) Validation Script
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script validates Requirements Phase completion before advancing to Design Phase

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

# Check if we got the required parameter
if [ "$#" -lt 1 ]; then
    echo "📋 Phase 1 Requirements Validation"
    echo "==================================="
    echo ""
    echo "📋 Purpose: Validate Requirements Phase completion before Design Phase"
    echo ""
    echo "✅ Use when:"
    echo "   • Before Phase 1 → Phase 2 transition"
    echo "   • After business-requirements agent completes work"
    echo "   • When user requests to 'continue to design'"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • Still working on requirements (not ready for validation)"
    echo "   • Already in Design Phase or beyond"
    echo ""
    echo "📊 Quality Gate Thresholds by Work Type:"
    echo "   • Feature: 95% required (24/25 points)"
    echo "   • Bug Fix: 80% required (20/25 points)"
    echo "   • Hotfix: 70% required (18/25 points)"
    echo "   • Documentation: 85% required (21/25 points)"
    echo "   • Refactoring: 90% required (23/25 points)"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. Validates business requirements document exists"
    echo "   2. Checks document structure (Executive Summary, Business Context, etc.)"
    echo "   3. Validates content quality (user stories, acceptance criteria)"
    echo "   4. Checks WitchCityRope-specific requirements (safety, mobile)"
    echo "   5. Calculates quality score and determines pass/fail"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <requirements-document-path> [work-type] [required-percentage]"
    echo ""
    echo "   requirements-document-path: Path to business-requirements.md"
    echo "   work-type: (optional) Feature|Bug|Hotfix|Docs|Refactor (default: Feature)"
    echo "   required-percentage: (optional) Override quality gate threshold"
    echo ""
    echo "   Example:"
    echo "   bash execute.sh docs/functional-areas/events/new-work/business-requirements.md"
    echo "   bash execute.sh docs/functional-areas/events/new-work/business-requirements.md Bug 80"
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

REQUIREMENTS_DOC="$1"
WORK_TYPE="${2:-Feature}"
REQUIRED_PERCENTAGE="${3:-}"

# Set default required percentage based on work type if not provided
if [ -z "$REQUIRED_PERCENTAGE" ]; then
    case "$WORK_TYPE" in
        "Feature")
            REQUIRED_PERCENTAGE=95
            ;;
        "Bug")
            REQUIRED_PERCENTAGE=80
            ;;
        "Hotfix")
            REQUIRED_PERCENTAGE=70
            ;;
        "Docs"|"Documentation")
            REQUIRED_PERCENTAGE=85
            ;;
        "Refactor"|"Refactoring")
            REQUIRED_PERCENTAGE=90
            ;;
        *)
            REQUIRED_PERCENTAGE=95  # Default to Feature requirements
            ;;
    esac
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "📋 Phase 1 Requirements Validation"
echo "==================================="
echo ""

# Check 1: Requirements document exists
echo "1️⃣  Checking requirements document..."
if [ ! -f "$REQUIREMENTS_DOC" ]; then
    echo "   ❌ FAIL: Requirements document not found:"
    echo "   $REQUIREMENTS_DOC"
    echo ""
    echo "💡 Ensure business-requirements.md exists at the specified path"
    exit 1
fi
echo "   ✅ Requirements document found"
echo "   Path: $REQUIREMENTS_DOC"

# Check 2: Document is not empty
echo ""
echo "2️⃣  Checking document size..."
if [ ! -s "$REQUIREMENTS_DOC" ]; then
    echo "   ❌ FAIL: Requirements document is empty"
    echo ""
    echo "💡 Document must have content before validation"
    exit 1
fi
FILE_SIZE=$(wc -c < "$REQUIREMENTS_DOC")
echo "   ✅ Document has content ($FILE_SIZE bytes)"

# Check 3: Document is readable
echo ""
echo "3️⃣  Checking permissions..."
if [ ! -r "$REQUIREMENTS_DOC" ]; then
    echo "   ❌ FAIL: Cannot read requirements document"
    echo ""
    echo "💡 Check file permissions"
    exit 1
fi
echo "   ✅ Document is readable"
echo ""

echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - VALIDATION
# ============================================

SCORE=0
MAX_SCORE=25

echo "📝 Validation Details:"
echo "   Work Type: $WORK_TYPE"
echo "   Required Score: ${REQUIRED_PERCENTAGE}%"
echo "   Max Score: $MAX_SCORE points"
echo ""
echo "Running validation checks..."
echo ""

# Document Structure Checks (10 points possible)
echo "📄 Document Structure:"
echo ""

if grep -q "## Executive Summary" "$REQUIREMENTS_DOC"; then
    echo "   ✅ Executive Summary found (+1 point)"
    ((SCORE++))
else
    echo "   ❌ Executive Summary missing"
fi

if grep -q "## Business Context" "$REQUIREMENTS_DOC"; then
    echo "   ✅ Business Context found (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ Business Context missing"
fi

if grep -q "## Success Metrics" "$REQUIREMENTS_DOC" || grep -q "### Success Metrics" "$REQUIREMENTS_DOC"; then
    echo "   ✅ Success Metrics found (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ Success Metrics missing"
fi

if grep -q "## User Stories" "$REQUIREMENTS_DOC"; then
    STORY_COUNT=$(grep -c "^### Story [0-9]" "$REQUIREMENTS_DOC" || echo "0")
    if [ "$STORY_COUNT" -ge 3 ]; then
        echo "   ✅ User Stories found ($STORY_COUNT stories) (+4 points)"
        ((SCORE+=4))
    else
        echo "   ⚠️  Only $STORY_COUNT user stories found (need 3+) (+2 points)"
        ((SCORE+=2))
    fi
else
    echo "   ❌ User Stories section missing"
fi

if grep -q "## Business Rules" "$REQUIREMENTS_DOC"; then
    echo "   ✅ Business Rules found (+1 point)"
    ((SCORE++))
else
    echo "   ❌ Business Rules missing"
fi

if grep -q "## Security & Privacy Requirements" "$REQUIREMENTS_DOC"; then
    echo "   ✅ Security & Privacy section found (+3 points)"
    ((SCORE+=3))
else
    echo "   ❌ Security & Privacy section missing"
fi

if grep -q "## Quality Gate Checklist" "$REQUIREMENTS_DOC"; then
    echo "   ✅ Quality Gate Checklist found (+1 point)"
    ((SCORE++))
else
    echo "   ❌ Quality Gate Checklist missing"
fi

echo ""
echo "🎯 Content Quality:"
echo ""

# Check acceptance criteria
AC_COUNT=$(grep -c "**Acceptance Criteria:**" "$REQUIREMENTS_DOC" || echo "0")
if [ "$AC_COUNT" -ge 3 ]; then
    echo "   ✅ Acceptance criteria provided ($AC_COUNT found) (+2 points)"
    ((SCORE+=2))
else
    echo "   ⚠️  Limited acceptance criteria ($AC_COUNT found, need 3+) (+1 point)"
    ((SCORE+=1))
fi

echo ""
echo "🏛️  WitchCityRope-Specific:"
echo ""

# WitchCityRope-specific checks (5 points possible)
if grep -iq "safety\|safe\|safety first" "$REQUIREMENTS_DOC"; then
    echo "   ✅ Safety considerations addressed (+1 point)"
    ((SCORE++))
else
    echo "   ⚠️  Safety considerations not explicitly mentioned"
fi

if grep -iq "mobile\|phone\|responsive" "$REQUIREMENTS_DOC"; then
    echo "   ✅ Mobile experience considered (+1 point)"
    ((SCORE++))
else
    echo "   ⚠️  Mobile experience not explicitly mentioned"
fi

if grep -iq "consent" "$REQUIREMENTS_DOC"; then
    echo "   ✅ Consent workflows mentioned (+1 point)"
    ((SCORE++))
else
    echo "   ⚠️  Consent workflows not mentioned"
fi

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo ""
echo "================================"
echo "📊 Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo "   Required: ${REQUIRED_PERCENTAGE}%"
echo "   Work Type: $WORK_TYPE"
echo ""

if [ "$PERCENTAGE" -ge "$REQUIRED_PERCENTAGE" ]; then
    echo "✅ PASS - Requirements Phase Complete"
    echo "======================================"
    echo ""
    echo "   Ready to advance to Design Phase"
    echo ""
    echo "🎯 Next Steps:"
    echo "   1. Proceed to Design Phase (Phase 2)"
    echo "   2. Create functional specifications"
    echo "   3. Design database schema"
    echo "   4. Create UI/UX wireframes"
    echo ""
    exit 0
else
    echo "❌ FAIL - Requirements Phase Incomplete"
    echo "========================================"
    echo ""
    echo "   Score: $PERCENTAGE% (need ${REQUIRED_PERCENTAGE}%)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    echo ""
    echo "🎯 Next Steps:"
    echo "   1. Review missing sections above"
    echo "   2. Return to business-requirements agent"
    echo "   3. Complete missing requirements"
    echo "   4. Re-run validation"
    echo ""
    echo "📖 See: .claude/skills/phase-1-validator/SKILL.md (Common Issues)"
    echo ""
    exit 1
fi
