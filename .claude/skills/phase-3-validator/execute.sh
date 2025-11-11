#!/bin/bash
# Phase 3 (Implementation) Validation Script
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script validates Implementation Phase completion before advancing to Testing Phase

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

if [ "$#" -lt 1 ]; then
    echo "⚙️  Phase 3 Implementation Validation"
    echo "======================================"
    echo ""
    echo "📋 Purpose: Validate Implementation Phase completion before Testing Phase"
    echo ""
    echo "✅ Use when:"
    echo "   • Before Phase 3 → Phase 4 transition"
    echo "   • After react-developer, backend-developer, database-designer complete work"
    echo "   • When user requests to 'continue to testing'"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • Still implementing code (not ready for validation)"
    echo "   • Already in Testing Phase or beyond"
    echo ""
    echo "📊 Quality Gate Thresholds by Work Type:"
    echo "   • Feature: 85% required (43/50 points)"
    echo "   • Bug Fix: 75% required (38/50 points)"
    echo "   • Hotfix: 70% required (35/50 points)"
    echo "   • Documentation: 80% required (40/50 points)"
    echo "   • Refactoring: 90% required (45/50 points)"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. Validates API and Web builds successfully"
    echo "   2. Checks implementation completeness (endpoints, components, migrations)"
    echo "   3. Validates code quality (TypeScript, C#, ESLint)"
    echo "   4. Checks testing infrastructure (unit, integration, E2E tests)"
    echo "   5. Validates documentation (implementation notes, API docs)"
    echo "   6. Calculates quality score and determines pass/fail"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <feature-name> [work-type] [required-percentage]"
    echo ""
    echo "   feature-name: Name of feature being validated (e.g., 'events' or 'vetting')"
    echo "   work-type: (optional) Feature|Bug|Hotfix|Docs|Refactor (default: Feature)"
    echo "   required-percentage: (optional) Override quality gate threshold"
    echo ""
    echo "   Example:"
    echo "   bash execute.sh events"
    echo "   bash execute.sh vetting Bug 75"
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

FEATURE_NAME="$1"
WORK_TYPE="${2:-Feature}"
REQUIRED_PERCENTAGE="${3:-}"

# Set default required percentage based on work type if not provided
if [ -z "$REQUIRED_PERCENTAGE" ]; then
    case "$WORK_TYPE" in
        "Feature")
            REQUIRED_PERCENTAGE=85
            ;;
        "Bug")
            REQUIRED_PERCENTAGE=75
            ;;
        "Hotfix")
            REQUIRED_PERCENTAGE=70
            ;;
        "Docs"|"Documentation")
            REQUIRED_PERCENTAGE=80
            ;;
        "Refactor"|"Refactoring")
            REQUIRED_PERCENTAGE=90
            ;;
        *)
            REQUIRED_PERCENTAGE=85  # Default to Feature requirements
            ;;
    esac
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "⚙️  Phase 3 Implementation Validation"
echo "======================================"
echo ""

echo "1️⃣  Checking project directories..."
if [ ! -d "/home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api" ]; then
    echo "   ❌ FAIL: API directory not found"
    exit 1
fi
if [ ! -d "/home/chad/repos/witchcityrope/apps/web" ]; then
    echo "   ❌ FAIL: Web directory not found"
    exit 1
fi
echo "   ✅ Project directories found"

echo ""
echo "2️⃣  Checking build tools..."
if ! command -v dotnet &> /dev/null; then
    echo "   ❌ FAIL: dotnet CLI not found"
    exit 1
fi
if ! command -v npm &> /dev/null; then
    echo "   ❌ FAIL: npm not found"
    exit 1
fi
echo "   ✅ Build tools available"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - VALIDATION
# ============================================

SCORE=0
MAX_SCORE=50

echo "📝 Validation Details:"
echo "   Feature: $FEATURE_NAME"
echo "   Work Type: $WORK_TYPE"
echo "   Required Score: ${REQUIRED_PERCENTAGE}%"
echo "   Max Score: $MAX_SCORE points"
echo ""
echo "Running validation checks..."
echo ""

# Build Validation (10 points possible)
echo "🔨 Build Validation:"
echo ""

echo "Building API..."
cd /home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api
if dotnet build --no-restore > /tmp/api-build.log 2>&1; then
    echo "   ✅ API builds successfully (+5 points)"
    ((SCORE+=5))
else
    echo "   ❌ API build failed"
    echo "   See: /tmp/api-build.log"
fi

echo "Building Web..."
cd /home/chad/repos/witchcityrope/apps/web
if npm run build > /tmp/web-build.log 2>&1; then
    echo "   ✅ Web builds successfully (+5 points)"
    ((SCORE+=5))
else
    echo "   ❌ Web build failed"
    echo "   See: /tmp/web-build.log"
fi

echo ""

# Implementation Completeness (15 points possible)
echo "📦 Implementation Completeness:"
echo ""

# Count API endpoints
API_ENDPOINT_COUNT=$(grep -r "MapGet\|MapPost\|MapPut\|MapDelete" /home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api/Features --include="*.cs" | wc -l)
if [ "$API_ENDPOINT_COUNT" -ge 3 ]; then
    echo "   ✅ API endpoints implemented ($API_ENDPOINT_COUNT found) (+4 points)"
    ((SCORE+=4))
elif [ "$API_ENDPOINT_COUNT" -gt 0 ]; then
    echo "   ⚠️  Limited API endpoints ($API_ENDPOINT_COUNT found) (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ No API endpoints found"
fi

# Count React components
COMPONENT_COUNT=$(find /home/chad/repos/witchcityrope/apps/web/src/features -name "*.tsx" -type f 2>/dev/null | wc -l)
if [ "$COMPONENT_COUNT" -ge 3 ]; then
    echo "   ✅ React components created ($COMPONENT_COUNT components) (+4 points)"
    ((SCORE+=4))
elif [ "$COMPONENT_COUNT" -gt 0 ]; then
    echo "   ⚠️  Limited components ($COMPONENT_COUNT found) (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ No React components found"
fi

# Check migrations
MIGRATION_COUNT=$(find /home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api/Data/Migrations -name "*.cs" -type f 2>/dev/null | wc -l)
if [ "$MIGRATION_COUNT" -ge 1 ]; then
    echo "   ✅ Database migrations created ($MIGRATION_COUNT migrations) (+3 points)"
    ((SCORE+=3))
else
    echo "   ⚠️  No new migrations found (+1 point)"
    ((SCORE+=1))
fi

# Check service layer
SERVICE_COUNT=$(find /home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api/Features -name "*Service.cs" -type f 2>/dev/null | wc -l)
if [ "$SERVICE_COUNT" -ge 1 ]; then
    echo "   ✅ Service layer implemented ($SERVICE_COUNT services) (+2 points)"
    ((SCORE+=2))
else
    echo "   ⚠️  No services found"
fi

# Check type definitions
if [ -d "/home/chad/repos/witchcityrope/packages/shared-types" ]; then
    TYPE_FILE_COUNT=$(find /home/chad/repos/witchcityrope/packages/shared-types -name "*.d.ts" -type f 2>/dev/null | wc -l)
    if [ "$TYPE_FILE_COUNT" -gt 0 ]; then
        echo "   ✅ Type definitions present (+2 points)"
        ((SCORE+=2))
    else
        echo "   ⚠️  No type definition files found"
    fi
else
    echo "   ⚠️  Shared types package not found"
fi

echo ""

# Code Quality Validation (12 points possible)
echo "✨ Code Quality Validation:"
echo ""

# TypeScript errors
echo "Checking TypeScript..."
cd /home/chad/repos/witchcityrope/apps/web
if npx tsc --noEmit > /tmp/tsc-check.log 2>&1; then
    echo "   ✅ No TypeScript errors (+2 points)"
    ((SCORE+=2))
else
    TS_ERROR_COUNT=$(grep -c "error TS" /tmp/tsc-check.log || echo "0")
    echo "   ❌ TypeScript errors found: $TS_ERROR_COUNT"
    echo "   See: /tmp/tsc-check.log"
fi

# C# warnings
cd /home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api
CS_WARNING_COUNT=$(dotnet build --no-restore 2>&1 | grep -c "warning CS" || echo "0")
if [ "$CS_WARNING_COUNT" -eq 0 ]; then
    echo "   ✅ No C# warnings (+2 points)"
    ((SCORE+=2))
else
    echo "   ⚠️  C# warnings found: $CS_WARNING_COUNT (+1 point)"
    ((SCORE+=1))
fi

# ESLint
echo "Running ESLint..."
cd /home/chad/repos/witchcityrope/apps/web
if npm run lint > /tmp/eslint.log 2>&1; then
    echo "   ✅ ESLint passes (+2 points)"
    ((SCORE+=2))
else
    LINT_ERROR_COUNT=$(grep -c "error" /tmp/eslint.log || echo "0")
    echo "   ⚠️  ESLint errors: $LINT_ERROR_COUNT (+1 point)"
    ((SCORE+=1))
    echo "   See: /tmp/eslint.log"
fi

# Error handling check
ERROR_HANDLING_COUNT=$(grep -r "try\|catch\|throw" /home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api/Features --include="*.cs" | wc -l)
if [ "$ERROR_HANDLING_COUNT" -ge 5 ]; then
    echo "   ✅ Error handling present (+2 points)"
    ((SCORE+=2))
else
    echo "   ⚠️  Limited error handling (+1 point)"
    ((SCORE+=1))
fi

# Pattern compliance (check for architectural violations)
DIRECT_DB_COUNT=$(grep -r "DbContext" /home/chad/repos/witchcityrope/apps/web/src --include="*.tsx" --include="*.ts" 2>/dev/null | wc -l)
if [ "$DIRECT_DB_COUNT" -eq 0 ]; then
    echo "   ✅ No architectural violations (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ CRITICAL: Direct database access in Web service detected (-5 points)"
    ((SCORE-=5))
fi

echo ""

# Testing Infrastructure (10 points possible)
echo "🧪 Testing Infrastructure:"
echo ""

# Unit tests
UNIT_TEST_COUNT=$(find /home/chad/repos/witchcityrope/tests -name "*.test.ts" -o -name "*.test.tsx" -o -name "*Tests.cs" 2>/dev/null | wc -l)
if [ "$UNIT_TEST_COUNT" -ge 3 ]; then
    echo "   ✅ Unit tests created ($UNIT_TEST_COUNT tests) (+3 points)"
    ((SCORE+=3))
elif [ "$UNIT_TEST_COUNT" -gt 0 ]; then
    echo "   ⚠️  Limited unit tests ($UNIT_TEST_COUNT found) (+1 point)"
    ((SCORE+=1))
else
    echo "   ❌ No unit tests found"
fi

# Integration tests
INTEGRATION_TEST_COUNT=$(find /home/chad/repos/witchcityrope/tests -path "*/integration/*" -name "*.cs" 2>/dev/null | wc -l)
if [ "$INTEGRATION_TEST_COUNT" -ge 1 ]; then
    echo "   ✅ Integration tests created ($INTEGRATION_TEST_COUNT tests) (+3 points)"
    ((SCORE+=3))
else
    echo "   ⚠️  No integration tests found (+1 point)"
    ((SCORE+=1))
fi

# E2E tests
E2E_TEST_COUNT=$(find /home/chad/repos/witchcityrope/tests/playwright -name "*.spec.ts" 2>/dev/null | wc -l)
if [ "$E2E_TEST_COUNT" -ge 1 ]; then
    echo "   ✅ E2E tests created ($E2E_TEST_COUNT tests) (+2 points)"
    ((SCORE+=2))
else
    echo "   ⚠️  No E2E tests found"
fi

# Test fixtures
FIXTURE_COUNT=$(find /home/chad/repos/witchcityrope/tests -name "*Fixture*.cs" -o -name "*.fixture.ts" 2>/dev/null | wc -l)
if [ "$FIXTURE_COUNT" -ge 1 ]; then
    echo "   ✅ Test fixtures prepared (+2 points)"
    ((SCORE+=2))
else
    echo "   ⚠️  No test fixtures found (+1 point)"
    ((SCORE+=1))
fi

echo ""

# Documentation Validation (5 points possible)
echo "📚 Documentation Validation:"
echo ""

# Implementation notes
NEW_WORK_DIR=$(find /home/chad/repos/witchcityrope/docs/functional-areas -type d -path "*/new-work/*" 2>/dev/null | sort | tail -1)
if [ -n "$NEW_WORK_DIR" ] && [ -f "$NEW_WORK_DIR/implementation-notes.md" ]; then
    echo "   ✅ Implementation notes documented (+2 points)"
    ((SCORE+=2))
else
    echo "   ⚠️  Implementation notes missing"
fi

# API documentation
if grep -q "/// <summary>" /home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api/Features --include="*.cs" -r 2>/dev/null; then
    echo "   ✅ API endpoints documented (+1 point)"
    ((SCORE+=1))
else
    echo "   ⚠️  API documentation missing"
fi

# Component usage examples
if grep -q "// Example:" /home/chad/repos/witchcityrope/apps/web/src/features --include="*.tsx" -r 2>/dev/null; then
    echo "   ✅ Component examples present (+1 point)"
    ((SCORE+=1))
else
    echo "   ⚠️  Component examples missing"
fi

# Known limitations
if [ -n "$NEW_WORK_DIR" ] && [ -f "$NEW_WORK_DIR/implementation-notes.md" ] && grep -q "## Known Limitations" "$NEW_WORK_DIR/implementation-notes.md"; then
    echo "   ✅ Known limitations documented (+1 point)"
    ((SCORE+=1))
else
    echo "   ⚠️  Known limitations not documented"
fi

echo ""

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo "================================"
echo "📊 Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo "   Required: ${REQUIRED_PERCENTAGE}%"
echo "   Work Type: $WORK_TYPE"
echo ""

if [ "$PERCENTAGE" -ge "$REQUIRED_PERCENTAGE" ]; then
    echo "✅ PASS - Implementation Phase Complete"
    echo "========================================"
    echo ""
    echo "   Ready to advance to Testing Phase"
    echo ""
    echo "🎯 Next Steps:"
    echo "   1. Proceed to Testing Phase (Phase 4)"
    echo "   2. test-developer creates comprehensive tests"
    echo "   3. test-executor runs test suite (MUST achieve 100% pass rate)"
    echo "   4. Validate test coverage and quality"
    echo ""
    exit 0
else
    echo "❌ FAIL - Implementation Phase Incomplete"
    echo "=========================================="
    echo ""
    echo "   Score: $PERCENTAGE% (need ${REQUIRED_PERCENTAGE}%)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    echo ""
    echo "🎯 Next Steps:"
    echo "   1. Review missing sections above"
    echo "   2. Return to react-developer, backend-developer, or database-designer"
    echo "   3. Complete missing implementation elements"
    echo "   4. Re-run validation"
    echo ""
    echo "📖 See: .claude/skills/phase-3-validator/SKILL.md (Common Issues)"
    echo ""
    exit 1
fi
