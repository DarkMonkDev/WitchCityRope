---
name: phase-2-validator
description: Validates Design Phase completion before advancing to Implementation Phase. Checks functional specifications, database design, and UI/UX design for completeness and technical feasibility.
---

# Phase 2 (Design) Validation Skill

**Purpose**: Automate validation of Design Phase completion before advancing to Implementation Phase.

**When to Use**: When orchestrator or agents need to verify design documents are ready for implementation.

## Quick Validation

```bash
# Find design documents in the new-work folder
NEW_WORK_DIR=$(find docs/functional-areas -type d -path "*/new-work/*-*-*" | sort | tail -1)

# Check for required design documents
FUNCTIONAL_SPEC="$NEW_WORK_DIR/requirements/functional-spec.md"
DATABASE_DESIGN="$NEW_WORK_DIR/design/database-design.md"
UI_UX_DESIGN="$NEW_WORK_DIR/design/ui-ux-design.md"

echo "Design Phase Validation"
echo "======================="
echo ""
echo "Checking for required documents:"
[ -f "$FUNCTIONAL_SPEC" ] && echo "✅ Functional Specification" || echo "❌ Functional Specification missing"
[ -f "$DATABASE_DESIGN" ] && echo "✅ Database Design" || echo "❌ Database Design missing"
[ -f "$UI_UX_DESIGN" ] && echo "✅ UI/UX Design" || echo "❌ UI/UX Design missing"
```

## Quality Gate Checklist (90% Required for Features)

### Functional Specification (12 points)
- [ ] Technical Overview section present (1 point)
- [ ] Architecture section with Web+API pattern (2 points)
- [ ] Data Models section with DTOs (2 points)
- [ ] API Specifications table present (2 points)
- [ ] Component Specifications defined (2 points)
- [ ] Security Requirements addressed (2 points)
- [ ] Testing Requirements defined (1 point)

### Database Design (8 points)
- [ ] Entity Relationship Diagram present (2 points)
- [ ] Table schemas with PostgreSQL syntax (2 points)
- [ ] Migration plan defined (1 point)
- [ ] Indexes and constraints specified (1 point)
- [ ] Relationships properly defined (1 point)
- [ ] Data types appropriate for PostgreSQL (1 point)

### UI/UX Design (8 points)
- [ ] Wireframes for key screens (2 points)
- [ ] Component breakdown provided (2 points)
- [ ] Mantine v7 components specified (2 points)
- [ ] Mobile responsive considerations (1 point)
- [ ] Accessibility requirements (1 point)

### Integration & Technical Feasibility (7 points)
- [ ] API endpoints match component needs (2 points)
- [ ] Database schema supports API requirements (2 points)
- [ ] Authentication flow defined (1 point)
- [ ] State management approach specified (1 point)
- [ ] No architectural violations (1 point)

## Automated Validation Script

```bash
#!/bin/bash
# Phase 2 Validation Script

NEW_WORK_DIR="$1"
SCORE=0
MAX_SCORE=35
REQUIRED_PERCENTAGE=90

echo "Phase 2 Design Validation"
echo "========================="
echo ""

# Check for required documents
FUNCTIONAL_SPEC="$NEW_WORK_DIR/requirements/functional-spec.md"
DATABASE_DESIGN="$NEW_WORK_DIR/design/database-design.md"
UI_UX_DESIGN="$NEW_WORK_DIR/design/ui-ux-design.md"

if [ ! -f "$FUNCTIONAL_SPEC" ]; then
    echo "❌ CRITICAL: Functional Specification missing"
    exit 1
fi

if [ ! -f "$DATABASE_DESIGN" ]; then
    echo "❌ CRITICAL: Database Design missing"
    exit 1
fi

if [ ! -f "$UI_UX_DESIGN" ]; then
    echo "❌ CRITICAL: UI/UX Design missing"
    exit 1
fi

echo "✅ All required design documents present"
echo ""

# Validate Functional Specification
echo "Functional Specification Validation"
echo "------------------------------------"

if grep -q "## Technical Overview" "$FUNCTIONAL_SPEC"; then
    echo "✅ Technical Overview found"
    ((SCORE++))
else
    echo "❌ Technical Overview missing"
fi

if grep -q "### Microservices Architecture" "$FUNCTIONAL_SPEC" && \
   grep -q "Web Service" "$FUNCTIONAL_SPEC" && \
   grep -q "API Service" "$FUNCTIONAL_SPEC"; then
    echo "✅ Web+API Architecture documented"
    ((SCORE+=2))
else
    echo "❌ Web+API Architecture not properly documented"
fi

if grep -q "## Data Models" "$FUNCTIONAL_SPEC" || grep -q "### DTOs" "$FUNCTIONAL_SPEC"; then
    echo "✅ Data Models section found"
    ((SCORE+=2))
else
    echo "❌ Data Models section missing"
fi

if grep -q "## API Specifications" "$FUNCTIONAL_SPEC" && \
   grep -q "| Method | Path" "$FUNCTIONAL_SPEC"; then
    echo "✅ API Specifications table found"
    ((SCORE+=2))
else
    echo "❌ API Specifications table missing"
fi

if grep -q "## Component Specifications" "$FUNCTIONAL_SPEC"; then
    echo "✅ Component Specifications found"
    ((SCORE+=2))
else
    echo "❌ Component Specifications missing"
fi

if grep -q "## Security Requirements" "$FUNCTIONAL_SPEC"; then
    echo "✅ Security Requirements found"
    ((SCORE+=2))
else
    echo "❌ Security Requirements missing"
fi

if grep -q "## Testing Requirements" "$FUNCTIONAL_SPEC"; then
    echo "✅ Testing Requirements found"
    ((SCORE++))
else
    echo "❌ Testing Requirements missing"
fi

echo ""

# Validate Database Design
echo "Database Design Validation"
echo "--------------------------"

if grep -q "## Entity Relationship Diagram" "$DATABASE_DESIGN" || \
   grep -q "```mermaid" "$DATABASE_DESIGN"; then
    echo "✅ ER Diagram found"
    ((SCORE+=2))
else
    echo "❌ ER Diagram missing"
fi

if grep -q "CREATE TABLE" "$DATABASE_DESIGN"; then
    TABLE_COUNT=$(grep -c "CREATE TABLE" "$DATABASE_DESIGN")
    echo "✅ Table schemas found ($TABLE_COUNT tables)"
    ((SCORE+=2))
else
    echo "❌ No table schemas found"
fi

if grep -q "## Migration Plan" "$DATABASE_DESIGN" || grep -q "### Migration" "$DATABASE_DESIGN"; then
    echo "✅ Migration plan found"
    ((SCORE++))
else
    echo "❌ Migration plan missing"
fi

if grep -q "CREATE INDEX" "$DATABASE_DESIGN" || grep -q "FOREIGN KEY" "$DATABASE_DESIGN"; then
    echo "✅ Indexes/constraints found"
    ((SCORE++))
else
    echo "⚠️  No indexes or constraints defined"
fi

if grep -q "UUID\|INTEGER\|VARCHAR\|TIMESTAMP" "$DATABASE_DESIGN"; then
    echo "✅ PostgreSQL data types used"
    ((SCORE++))
else
    echo "❌ PostgreSQL data types not specified"
fi

if grep -q "REFERENCES\|FOREIGN KEY" "$DATABASE_DESIGN"; then
    echo "✅ Relationships defined"
    ((SCORE++))
else
    echo "⚠️  Relationships not explicitly defined"
fi

echo ""

# Validate UI/UX Design
echo "UI/UX Design Validation"
echo "-----------------------"

if grep -q "## Wireframes" "$UI_UX_DESIGN" || grep -q "### Wireframe" "$UI_UX_DESIGN"; then
    echo "✅ Wireframes section found"
    ((SCORE+=2))
else
    echo "❌ Wireframes missing"
fi

if grep -q "## Component Breakdown" "$UI_UX_DESIGN" || grep -q "### Components" "$UI_UX_DESIGN"; then
    echo "✅ Component breakdown found"
    ((SCORE+=2))
else
    echo "❌ Component breakdown missing"
fi

if grep -iq "mantine\|@mantine/core" "$UI_UX_DESIGN"; then
    echo "✅ Mantine v7 components specified"
    ((SCORE+=2))
else
    echo "❌ Mantine v7 components not specified"
fi

if grep -iq "mobile\|responsive\|viewport" "$UI_UX_DESIGN"; then
    echo "✅ Mobile considerations addressed"
    ((SCORE++))
else
    echo "⚠️  Mobile considerations not mentioned"
fi

if grep -iq "accessibility\|a11y\|aria\|wcag" "$UI_UX_DESIGN"; then
    echo "✅ Accessibility requirements found"
    ((SCORE++))
else
    echo "⚠️  Accessibility not explicitly addressed"
fi

echo ""

# Validate Integration
echo "Integration Validation"
echo "----------------------"

# Check API endpoints are defined
API_ENDPOINT_COUNT=$(grep -c "| GET\|| POST\|| PUT\|| DELETE\|| PATCH" "$FUNCTIONAL_SPEC")
if [ "$API_ENDPOINT_COUNT" -ge 3 ]; then
    echo "✅ API endpoints defined ($API_ENDPOINT_COUNT endpoints)"
    ((SCORE+=2))
else
    echo "⚠️  Limited API endpoints ($API_ENDPOINT_COUNT found)"
    ((SCORE+=1))
fi

# Check database schema supports API
TABLE_COUNT=$(grep -c "CREATE TABLE" "$DATABASE_DESIGN")
if [ "$TABLE_COUNT" -ge 1 ]; then
    echo "✅ Database schema supports API ($TABLE_COUNT tables)"
    ((SCORE+=2))
else
    echo "❌ No database tables defined"
fi

# Check authentication
if grep -iq "auth\|authentication\|login\|cookie" "$FUNCTIONAL_SPEC"; then
    echo "✅ Authentication flow defined"
    ((SCORE++))
else
    echo "⚠️  Authentication flow not explicitly defined"
fi

# Check state management
if grep -iq "zustand\|context\|state management" "$FUNCTIONAL_SPEC" || \
   grep -iq "zustand\|context\|state" "$UI_UX_DESIGN"; then
    echo "✅ State management approach specified"
    ((SCORE++))
else
    echo "⚠️  State management approach not specified"
fi

# Check for architectural violations
if grep -iq "direct database\|web.*database\|razor\|signalr" "$FUNCTIONAL_SPEC"; then
    echo "❌ CRITICAL: Architectural violation detected"
    echo "   Found references to direct database access or deprecated patterns"
    ((SCORE-=5))
else
    echo "✅ No architectural violations detected"
    ((SCORE++))
fi

echo ""

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo "================================"
echo "Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo "Required: ${REQUIRED_PERCENTAGE}%"
echo ""

if [ "$PERCENTAGE" -ge "$REQUIRED_PERCENTAGE" ]; then
    echo "✅ PASS - Design Phase complete"
    echo "   Ready to advance to Implementation Phase"
    exit 0
else
    echo "❌ FAIL - Design Phase incomplete"
    echo "   Score: $PERCENTAGE% (need ${REQUIRED_PERCENTAGE}%)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    exit 1
fi
```

## Usage Examples

### From Orchestrator
```
Use the phase-2-validator skill to check if design is ready for implementation
```

### Manual Validation
```bash
# Find new-work directory
NEW_WORK_DIR=$(find docs/functional-areas -type d -path "*/new-work/*-*-*" | sort | tail -1)

# Run validation
bash .claude/skills/phase-2-validator.md "$NEW_WORK_DIR"
```

## Common Issues

### Issue: Missing Web+API Architecture
**Solution**: Functional spec must explicitly document:
- Web Service (React + Vite) at localhost:5173
- API Service (Minimal API) at localhost:5655
- No direct database access from Web Service

### Issue: Using SQL Server Syntax
**Solution**: All database designs must use PostgreSQL syntax:
- ✅ `UUID` not `UNIQUEIDENTIFIER`
- ✅ `VARCHAR` not `NVARCHAR`
- ✅ `TIMESTAMP` not `DATETIME2`

### Issue: Missing Mantine v7 Specification
**Solution**: UI design must specify Mantine components:
- `Button`, `TextInput`, `Select`, `Modal`, etc.
- No Material-UI or Chakra references

### Issue: Architectural Violations
**Critical violations that fail validation:**
- Direct database access from Web Service
- References to Razor Pages or Blazor
- SignalR (should use HTTP polling or webhooks)
- Repository pattern over EF Core

## Output Format

```json
{
  "phase": "design",
  "status": "pass|fail",
  "score": 32,
  "maxScore": 35,
  "percentage": 91,
  "requiredPercentage": 90,
  "documents": {
    "functionalSpec": "present",
    "databaseDesign": "present",
    "uiUxDesign": "present"
  },
  "missingItems": [
    "Accessibility requirements not addressed",
    "Mobile wireframes not provided"
  ],
  "architecturalViolations": [],
  "readyForNextPhase": true
}
```

## Integration with Quality Gates

This skill enforces the quality gate thresholds by work type:

- **Feature**: 90% required (32/35 points)
- **Bug Fix**: 70% required (25/35 points)
- **Hotfix**: 60% required (21/35 points)
- **Documentation**: 80% required (28/35 points)
- **Refactoring**: 85% required (30/35 points)

## Progressive Disclosure

**Initial Context**: Show quick document checklist
**On Request**: Show full validation script with scoring
**On Failure**: Show specific missing items and architectural violations
**On Pass**: Show concise summary with score

---

**Remember**: This skill automates design validation but doesn't replace agent judgment. If validation fails, orchestrator should loop back to functional-spec, database-designer, or ui-designer agents.
