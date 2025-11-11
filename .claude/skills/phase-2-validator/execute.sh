#!/bin/bash
# Phase 2 (Design) Validation Script
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# This script validates Design Phase completion before advancing to Implementation Phase

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

# Check if we got the required parameter
if [ "$#" -lt 1 ]; then
    echo "📐 Phase 2 Design Validation"
    echo "============================="
    echo ""
    echo "📋 Purpose: Validate Design Phase completion before Implementation Phase"
    echo ""
    echo "✅ Use when:"
    echo "   • Before Phase 2 → Phase 3 transition"
    echo "   • After functional-spec, database-designer, ui-designer complete work"
    echo "   • When user requests to 'continue to implementation'"
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • Still working on design (not ready for validation)"
    echo "   • Already in Implementation Phase or beyond"
    echo ""
    echo "📊 Quality Gate Thresholds by Work Type:"
    echo "   • Feature: 90% required (32/35 points)"
    echo "   • Bug Fix: 70% required (25/35 points)"
    echo "   • Hotfix: 60% required (21/35 points)"
    echo "   • Documentation: 80% required (28/35 points)"
    echo "   • Refactoring: 85% required (30/35 points)"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. Validates functional specification exists and is complete"
    echo "   2. Checks database design (ER diagram, schemas, migration plan)"
    echo "   3. Validates UI/UX design (wireframes, components, Mantine v7)"
    echo "   4. Verifies integration (API endpoints match component needs)"
    echo "   5. Calculates quality score and determines pass/fail"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <new-work-directory> [work-type] [required-percentage]"
    echo ""
    echo "   new-work-directory: Path to new-work folder with design documents"
    echo "   work-type: (optional) Feature|Bug|Hotfix|Docs|Refactor (default: Feature)"
    echo "   required-percentage: (optional) Override quality gate threshold"
    echo ""
    echo "   Example:"
    echo "   bash execute.sh docs/functional-areas/events/new-work/2025-11-03-feature"
    echo "   bash execute.sh docs/functional-areas/events/new-work/2025-11-03-feature Bug 70"
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

NEW_WORK_DIR="$1"
WORK_TYPE="${2:-Feature}"
REQUIRED_PERCENTAGE="${3:-}"

# Set default required percentage based on work type if not provided
if [ -z "$REQUIRED_PERCENTAGE" ]; then
    case "$WORK_TYPE" in
        "Feature")
            REQUIRED_PERCENTAGE=90
            ;;
        "Bug")
            REQUIRED_PERCENTAGE=70
            ;;
        "Hotfix")
            REQUIRED_PERCENTAGE=60
            ;;
        "Docs"|"Documentation")
            REQUIRED_PERCENTAGE=80
            ;;
        "Refactor"|"Refactoring")
            REQUIRED_PERCENTAGE=85
            ;;
        *)
            REQUIRED_PERCENTAGE=90  # Default to Feature requirements
            ;;
    esac
fi

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "📐 Phase 2 Design Validation"
echo "============================="
echo ""

# Check 1: New-work directory exists
echo "1️⃣  Checking new-work directory..."
if [ ! -d "$NEW_WORK_DIR" ]; then
    echo "   ❌ FAIL: New-work directory not found:"
    echo "   $NEW_WORK_DIR"
    echo ""
    echo "💡 Ensure new-work directory exists at the specified path"
    exit 1
fi
echo "   ✅ New-work directory found"
echo "   Path: $NEW_WORK_DIR"

# Check 2: Required design documents exist
echo ""
echo "2️⃣  Checking for required design documents..."

FUNCTIONAL_SPEC="$NEW_WORK_DIR/requirements/functional-spec.md"
DATABASE_DESIGN="$NEW_WORK_DIR/design/database-design.md"
UI_UX_DESIGN="$NEW_WORK_DIR/design/ui-ux-design.md"

MISSING_DOCS=0

if [ ! -f "$FUNCTIONAL_SPEC" ]; then
    echo "   ❌ CRITICAL: Functional Specification missing"
    echo "   Expected: $FUNCTIONAL_SPEC"
    ((MISSING_DOCS++))
else
    echo "   ✅ Functional Specification found"
fi

if [ ! -f "$DATABASE_DESIGN" ]; then
    echo "   ❌ CRITICAL: Database Design missing"
    echo "   Expected: $DATABASE_DESIGN"
    ((MISSING_DOCS++))
else
    echo "   ✅ Database Design found"
fi

if [ ! -f "$UI_UX_DESIGN" ]; then
    echo "   ❌ CRITICAL: UI/UX Design missing"
    echo "   Expected: $UI_UX_DESIGN"
    ((MISSING_DOCS++))
else
    echo "   ✅ UI/UX Design found"
fi

if [ "$MISSING_DOCS" -gt 0 ]; then
    echo ""
    echo "💡 All three design documents are required for Phase 2 validation"
    exit 1
fi

# Check 3: Documents are readable
echo ""
echo "3️⃣  Checking document permissions..."
if [ ! -r "$FUNCTIONAL_SPEC" ] || [ ! -r "$DATABASE_DESIGN" ] || [ ! -r "$UI_UX_DESIGN" ]; then
    echo "   ❌ FAIL: Cannot read one or more design documents"
    echo ""
    echo "💡 Check file permissions"
    exit 1
fi
echo "   ✅ All documents are readable"
echo ""

echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - VALIDATION
# ============================================

SCORE=0
MAX_SCORE=35

echo "📝 Validation Details:"
echo "   Work Type: $WORK_TYPE"
echo "   Required Score: ${REQUIRED_PERCENTAGE}%"
echo "   Max Score: $MAX_SCORE points"
echo ""
echo "Running validation checks..."
echo ""

# Functional Specification Validation (12 points possible)
echo "📄 Functional Specification Validation:"
echo ""

if grep -q "## Technical Overview" "$FUNCTIONAL_SPEC"; then
    echo "   ✅ Technical Overview found (+1 point)"
    ((SCORE++))
else
    echo "   ❌ Technical Overview missing"
fi

if grep -q "### Microservices Architecture" "$FUNCTIONAL_SPEC" && \
   grep -q "Web Service" "$FUNCTIONAL_SPEC" && \
   grep -q "API Service" "$FUNCTIONAL_SPEC"; then
    echo "   ✅ Web+API Architecture documented (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ Web+API Architecture not properly documented"
fi

if grep -q "## Data Models" "$FUNCTIONAL_SPEC" || grep -q "### DTOs" "$FUNCTIONAL_SPEC"; then
    echo "   ✅ Data Models section found (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ Data Models section missing"
fi

if grep -q "## API Specifications" "$FUNCTIONAL_SPEC" && \
   grep -q "| Method | Path" "$FUNCTIONAL_SPEC"; then
    echo "   ✅ API Specifications table found (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ API Specifications table missing"
fi

if grep -q "## Component Specifications" "$FUNCTIONAL_SPEC"; then
    echo "   ✅ Component Specifications found (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ Component Specifications missing"
fi

if grep -q "## Security Requirements" "$FUNCTIONAL_SPEC"; then
    echo "   ✅ Security Requirements found (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ Security Requirements missing"
fi

if grep -q "## Testing Requirements" "$FUNCTIONAL_SPEC"; then
    echo "   ✅ Testing Requirements found (+1 point)"
    ((SCORE++))
else
    echo "   ❌ Testing Requirements missing"
fi

echo ""

# Database Design Validation (8 points possible)
echo "🗄️  Database Design Validation:"
echo ""

if grep -q "## Entity Relationship Diagram" "$DATABASE_DESIGN" || \
   grep -q "\`\`\`mermaid" "$DATABASE_DESIGN"; then
    echo "   ✅ ER Diagram found (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ ER Diagram missing"
fi

if grep -q "CREATE TABLE" "$DATABASE_DESIGN"; then
    TABLE_COUNT=$(grep -c "CREATE TABLE" "$DATABASE_DESIGN")
    echo "   ✅ Table schemas found ($TABLE_COUNT tables) (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ No table schemas found"
fi

if grep -q "## Migration Plan" "$DATABASE_DESIGN" || grep -q "### Migration" "$DATABASE_DESIGN"; then
    echo "   ✅ Migration plan found (+1 point)"
    ((SCORE++))
else
    echo "   ❌ Migration plan missing"
fi

if grep -q "CREATE INDEX" "$DATABASE_DESIGN" || grep -q "FOREIGN KEY" "$DATABASE_DESIGN"; then
    echo "   ✅ Indexes/constraints found (+1 point)"
    ((SCORE++))
else
    echo "   ⚠️  No indexes or constraints defined"
fi

if grep -q "UUID\|INTEGER\|VARCHAR\|TIMESTAMP" "$DATABASE_DESIGN"; then
    echo "   ✅ PostgreSQL data types used (+1 point)"
    ((SCORE++))
else
    echo "   ❌ PostgreSQL data types not specified"
fi

if grep -q "REFERENCES\|FOREIGN KEY" "$DATABASE_DESIGN"; then
    echo "   ✅ Relationships defined (+1 point)"
    ((SCORE++))
else
    echo "   ⚠️  Relationships not explicitly defined"
fi

echo ""

# UI/UX Design Validation (8 points possible)
echo "🎨 UI/UX Design Validation:"
echo ""

if grep -q "## Wireframes" "$UI_UX_DESIGN" || grep -q "### Wireframe" "$UI_UX_DESIGN"; then
    echo "   ✅ Wireframes section found (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ Wireframes missing"
fi

if grep -q "## Component Breakdown" "$UI_UX_DESIGN" || grep -q "### Components" "$UI_UX_DESIGN"; then
    echo "   ✅ Component breakdown found (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ Component breakdown missing"
fi

if grep -iq "mantine\|@mantine/core" "$UI_UX_DESIGN"; then
    echo "   ✅ Mantine v7 components specified (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ Mantine v7 components not specified"
fi

if grep -iq "mobile\|responsive\|viewport" "$UI_UX_DESIGN"; then
    echo "   ✅ Mobile considerations addressed (+1 point)"
    ((SCORE++))
else
    echo "   ⚠️  Mobile considerations not mentioned"
fi

if grep -iq "accessibility\|a11y\|aria\|wcag" "$UI_UX_DESIGN"; then
    echo "   ✅ Accessibility requirements found (+1 point)"
    ((SCORE++))
else
    echo "   ⚠️  Accessibility not explicitly addressed"
fi

echo ""

# Integration Validation (7 points possible)
echo "🔗 Integration Validation:"
echo ""

# Check API endpoints are defined
API_ENDPOINT_COUNT=$(grep -c "| GET\|| POST\|| PUT\|| DELETE\|| PATCH" "$FUNCTIONAL_SPEC")
if [ "$API_ENDPOINT_COUNT" -ge 3 ]; then
    echo "   ✅ API endpoints defined ($API_ENDPOINT_COUNT endpoints) (+2 points)"
    ((SCORE+=2))
else
    echo "   ⚠️  Limited API endpoints ($API_ENDPOINT_COUNT found) (+1 point)"
    ((SCORE+=1))
fi

# Check database schema supports API
TABLE_COUNT=$(grep -c "CREATE TABLE" "$DATABASE_DESIGN")
if [ "$TABLE_COUNT" -ge 1 ]; then
    echo "   ✅ Database schema supports API ($TABLE_COUNT tables) (+2 points)"
    ((SCORE+=2))
else
    echo "   ❌ No database tables defined"
fi

# Check authentication
if grep -iq "auth\|authentication\|login\|cookie" "$FUNCTIONAL_SPEC"; then
    echo "   ✅ Authentication flow defined (+1 point)"
    ((SCORE++))
else
    echo "   ⚠️  Authentication flow not explicitly defined"
fi

# Check state management
if grep -iq "zustand\|context\|state management" "$FUNCTIONAL_SPEC" || \
   grep -iq "zustand\|context\|state" "$UI_UX_DESIGN"; then
    echo "   ✅ State management approach specified (+1 point)"
    ((SCORE++))
else
    echo "   ⚠️  State management approach not specified"
fi

# Check for architectural violations
if grep -iq "direct database\|web.*database\|razor\|signalr" "$FUNCTIONAL_SPEC"; then
    echo "   ❌ CRITICAL: Architectural violation detected (-5 points)"
    echo "      Found references to direct database access or deprecated patterns"
    ((SCORE-=5))
else
    echo "   ✅ No architectural violations detected (+1 point)"
    ((SCORE++))
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
    echo "✅ PASS - Design Phase Complete"
    echo "======================================"
    echo ""
    echo "   Ready to advance to Implementation Phase"
    echo ""
    echo "🎯 Next Steps:"
    echo "   1. Proceed to Implementation Phase (Phase 3)"
    echo "   2. react-developer implements UI components"
    echo "   3. backend-developer implements API endpoints"
    echo "   4. database-designer creates migrations"
    echo ""
    exit 0
else
    echo "❌ FAIL - Design Phase Incomplete"
    echo "========================================"
    echo ""
    echo "   Score: $PERCENTAGE% (need ${REQUIRED_PERCENTAGE}%)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    echo ""
    echo "🎯 Next Steps:"
    echo "   1. Review missing sections above"
    echo "   2. Return to functional-spec, database-designer, or ui-designer agents"
    echo "   3. Complete missing design elements"
    echo "   4. Re-run validation"
    echo ""
    echo "📖 See: .claude/skills/phase-2-validator/SKILL.md (Common Issues)"
    echo ""
    exit 1
fi
