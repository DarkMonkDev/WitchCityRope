---
name: phase-3-validator
description: Validates Implementation Phase completion before advancing to Testing Phase. Checks code compilation, test coverage, implementation completeness, and code quality standards.
---

# Phase 3 (Implementation) Validation Skill

**Purpose**: Automate validation of Implementation Phase before advancing to Testing Phase.

**When to Use**: When orchestrator needs to verify implementation is ready for comprehensive testing.

## Quick Validation

```bash
# Check compilation
echo "Checking compilation..."
dotnet build apps/api/WitchCityRope.Api/WitchCityRope.Api.csproj --no-restore
API_BUILD=$?

cd apps/web && npm run build
WEB_BUILD=$?

if [ $API_BUILD -eq 0 ] && [ $WEB_BUILD -eq 0 ]; then
    echo "✅ Compilation successful"
else
    echo "❌ Compilation failed"
    exit 1
fi
```

## Quality Gate Checklist (85% Required for Features)

### Code Compilation (10 points)
- [ ] API builds without errors (5 points)
- [ ] Web builds without errors (5 points)

### Implementation Completeness (15 points)
- [ ] All API endpoints implemented (4 points)
- [ ] All React components created (4 points)
- [ ] Database migrations created (3 points)
- [ ] Service layer implemented (2 points)
- [ ] Type definitions match API (2 points)

### Code Quality (10 points)
- [ ] No TypeScript errors (2 points)
- [ ] No C# warnings (2 points)
- [ ] ESLint passes (2 points)
- [ ] Proper error handling (2 points)
- [ ] Code follows project patterns (2 points)

### Testing Infrastructure (10 points)
- [ ] Unit tests created (3 points)
- [ ] Integration test stubs created (3 points)
- [ ] E2E test files created (2 points)
- [ ] Test data/fixtures prepared (2 points)

### Documentation (5 points)
- [ ] Implementation notes documented (2 points)
- [ ] API endpoints documented (1 point)
- [ ] Component usage examples (1 point)
- [ ] Known limitations noted (1 point)

## Automated Validation Script

```bash
#!/bin/bash
# Phase 3 Validation Script

FEATURE_NAME="$1"
SCORE=0
MAX_SCORE=50
REQUIRED_PERCENTAGE=85

echo "Phase 3 Implementation Validation"
echo "=================================="
echo ""

# Check compilation
echo "Build Validation"
echo "----------------"

echo "Building API..."
cd /home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api
dotnet build --no-restore > /tmp/api-build.log 2>&1
if [ $? -eq 0 ]; then
    echo "✅ API builds successfully"
    ((SCORE+=5))
else
    echo "❌ API build failed"
    echo "   See: /tmp/api-build.log"
fi

echo "Building Web..."
cd /home/chad/repos/witchcityrope/apps/web
npm run build > /tmp/web-build.log 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Web builds successfully"
    ((SCORE+=5))
else
    echo "❌ Web build failed"
    echo "   See: /tmp/web-build.log"
fi

echo ""

# Check implementation files
echo "Implementation Completeness"
echo "---------------------------"

# Count API endpoints
API_ENDPOINT_COUNT=$(grep -r "MapGet\|MapPost\|MapPut\|MapDelete" apps/api/WitchCityRope.Api/Features --include="*.cs" | wc -l)
if [ "$API_ENDPOINT_COUNT" -ge 3 ]; then
    echo "✅ API endpoints implemented ($API_ENDPOINT_COUNT found)"
    ((SCORE+=4))
elif [ "$API_ENDPOINT_COUNT" -gt 0 ]; then
    echo "⚠️  Limited API endpoints ($API_ENDPOINT_COUNT found)"
    ((SCORE+=2))
else
    echo "❌ No API endpoints found"
fi

# Count React components
COMPONENT_COUNT=$(find apps/web/src/features -name "*.tsx" -type f | wc -l)
if [ "$COMPONENT_COUNT" -ge 3 ]; then
    echo "✅ React components created ($COMPONENT_COUNT components)"
    ((SCORE+=4))
elif [ "$COMPONENT_COUNT" -gt 0 ]; then
    echo "⚠️  Limited components ($COMPONENT_COUNT found)"
    ((SCORE+=2))
else
    echo "❌ No React components found"
fi

# Check migrations
MIGRATION_COUNT=$(find apps/api/WitchCityRope.Api/Data/Migrations -name "*.cs" -type f 2>/dev/null | wc -l)
if [ "$MIGRATION_COUNT" -ge 1 ]; then
    echo "✅ Database migrations created ($MIGRATION_COUNT migrations)"
    ((SCORE+=3))
else
    echo "⚠️  No new migrations found"
    ((SCORE+=1))
fi

# Check service layer
SERVICE_COUNT=$(find apps/api/WitchCityRope.Api/Features -name "*Service.cs" -type f | wc -l)
if [ "$SERVICE_COUNT" -ge 1 ]; then
    echo "✅ Service layer implemented ($SERVICE_COUNT services)"
    ((SCORE+=2))
else
    echo "⚠️  No services found"
fi

# Check type definitions
if [ -d "packages/shared-types" ]; then
    TYPE_FILE_COUNT=$(find packages/shared-types -name "*.d.ts" -type f | wc -l)
    if [ "$TYPE_FILE_COUNT" -gt 0 ]; then
        echo "✅ Type definitions present"
        ((SCORE+=2))
    else
        echo "⚠️  No type definition files found"
    fi
else
    echo "⚠️  Shared types package not found"
fi

echo ""

# Code quality checks
echo "Code Quality Validation"
echo "-----------------------"

# TypeScript errors
echo "Checking TypeScript..."
cd /home/chad/repos/witchcityrope/apps/web
npx tsc --noEmit > /tmp/tsc-check.log 2>&1
if [ $? -eq 0 ]; then
    echo "✅ No TypeScript errors"
    ((SCORE+=2))
else
    TS_ERROR_COUNT=$(grep -c "error TS" /tmp/tsc-check.log)
    echo "❌ TypeScript errors found: $TS_ERROR_COUNT"
    echo "   See: /tmp/tsc-check.log"
fi

# C# warnings
cd /home/chad/repos/witchcityrope/apps/api/WitchCityRope.Api
CS_WARNING_COUNT=$(dotnet build --no-restore 2>&1 | grep -c "warning CS")
if [ "$CS_WARNING_COUNT" -eq 0 ]; then
    echo "✅ No C# warnings"
    ((SCORE+=2))
else
    echo "⚠️  C# warnings found: $CS_WARNING_COUNT"
    ((SCORE+=1))
fi

# ESLint
echo "Running ESLint..."
cd /home/chad/repos/witchcityrope/apps/web
npm run lint > /tmp/eslint.log 2>&1
if [ $? -eq 0 ]; then
    echo "✅ ESLint passes"
    ((SCORE+=2))
else
    LINT_ERROR_COUNT=$(grep -c "error" /tmp/eslint.log)
    echo "⚠️  ESLint errors: $LINT_ERROR_COUNT"
    ((SCORE+=1))
    echo "   See: /tmp/eslint.log"
fi

# Error handling check
ERROR_HANDLING_COUNT=$(grep -r "try\|catch\|throw" apps/api/WitchCityRope.Api/Features --include="*.cs" | wc -l)
if [ "$ERROR_HANDLING_COUNT" -ge 5 ]; then
    echo "✅ Error handling present"
    ((SCORE+=2))
else
    echo "⚠️  Limited error handling"
    ((SCORE+=1))
fi

# Pattern compliance (check for architectural violations)
DIRECT_DB_COUNT=$(grep -r "DbContext" apps/web/src --include="*.tsx" --include="*.ts" | wc -l)
if [ "$DIRECT_DB_COUNT" -eq 0 ]; then
    echo "✅ No architectural violations"
    ((SCORE+=2))
else
    echo "❌ CRITICAL: Direct database access in Web service detected"
    ((SCORE-=5))
fi

echo ""

# Testing infrastructure
echo "Testing Infrastructure"
echo "----------------------"

# Unit tests
UNIT_TEST_COUNT=$(find tests -name "*.test.ts" -o -name "*.test.tsx" -o -name "*Tests.cs" | wc -l)
if [ "$UNIT_TEST_COUNT" -ge 3 ]; then
    echo "✅ Unit tests created ($UNIT_TEST_COUNT tests)"
    ((SCORE+=3))
elif [ "$UNIT_TEST_COUNT" -gt 0 ]; then
    echo "⚠️  Limited unit tests ($UNIT_TEST_COUNT found)"
    ((SCORE+=1))
else
    echo "❌ No unit tests found"
fi

# Integration tests
INTEGRATION_TEST_COUNT=$(find tests -path "*/integration/*" -name "*.cs" | wc -l)
if [ "$INTEGRATION_TEST_COUNT" -ge 1 ]; then
    echo "✅ Integration tests created ($INTEGRATION_TEST_COUNT tests)"
    ((SCORE+=3))
else
    echo "⚠️  No integration tests found"
    ((SCORE+=1))
fi

# E2E tests
E2E_TEST_COUNT=$(find tests/playwright -name "*.spec.ts" 2>/dev/null | wc -l)
if [ "$E2E_TEST_COUNT" -ge 1 ]; then
    echo "✅ E2E tests created ($E2E_TEST_COUNT tests)"
    ((SCORE+=2))
else
    echo "⚠️  No E2E tests found"
fi

# Test fixtures
FIXTURE_COUNT=$(find tests -name "*Fixture*.cs" -o -name "*.fixture.ts" 2>/dev/null | wc -l)
if [ "$FIXTURE_COUNT" -ge 1 ]; then
    echo "✅ Test fixtures prepared"
    ((SCORE+=2))
else
    echo "⚠️  No test fixtures found"
    ((SCORE+=1))
fi

echo ""

# Documentation
echo "Documentation Validation"
echo "------------------------"

# Implementation notes
if [ -f "docs/functional-areas/$FEATURE_NAME/new-work/*/implementation-notes.md" ]; then
    echo "✅ Implementation notes documented"
    ((SCORE+=2))
else
    echo "⚠️  Implementation notes missing"
fi

# API documentation
if grep -q "/// <summary>" apps/api/WitchCityRope.Api/Features --include="*.cs" -r; then
    echo "✅ API endpoints documented"
    ((SCORE++))
else
    echo "⚠️  API documentation missing"
fi

# Component usage examples
if grep -q "// Example:" apps/web/src/features --include="*.tsx" -r; then
    echo "✅ Component examples present"
    ((SCORE++))
else
    echo "⚠️  Component examples missing"
fi

# Known limitations
NEW_WORK_DIR=$(find docs/functional-areas -type d -path "*/new-work/*" | sort | tail -1)
if [ -f "$NEW_WORK_DIR/implementation-notes.md" ] && grep -q "## Known Limitations" "$NEW_WORK_DIR/implementation-notes.md"; then
    echo "✅ Known limitations documented"
    ((SCORE++))
else
    echo "⚠️  Known limitations not documented"
fi

echo ""

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo "================================"
echo "Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo "Required: ${REQUIRED_PERCENTAGE}%"
echo ""

if [ "$PERCENTAGE" -ge "$REQUIRED_PERCENTAGE" ]; then
    echo "✅ PASS - Implementation Phase complete"
    echo "   Ready to advance to Testing Phase"
    exit 0
else
    echo "❌ FAIL - Implementation Phase incomplete"
    echo "   Score: $PERCENTAGE% (need ${REQUIRED_PERCENTAGE}%)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    exit 1
fi
```

## Usage Examples

### From Orchestrator
```
Use the phase-3-validator skill to check if implementation is ready for testing
```

### Manual Validation
```bash
# Run validation for specific feature
bash .claude/skills/phase-3-validator.md "event-management"
```

## Common Issues

### Issue: Build Failures
**Critical - Must fix before testing:**
- Compilation errors in API
- TypeScript errors in Web
- Missing dependencies

**Solution**: Loop back to react-developer or backend-developer agents.

### Issue: Missing Test Files
**Solution**: Test files should exist even if not fully implemented:
- Unit tests for services and components
- Integration test stubs for API endpoints
- E2E test files for user workflows

### Issue: Architectural Violations
**Critical violations:**
- Direct database access from Web service
- Using Blazor patterns in React code
- Missing error handling in API endpoints

### Issue: No Type Definitions
**Solution**: After creating API endpoints:
```bash
cd packages/shared-types
npm run generate
```

## First Vertical Slice Checkpoint

**MANDATORY**: After implementing the first vertical slice (one complete feature from UI → API → Database):

1. Run phase-3-validator
2. If score ≥ 85%, **PAUSE for human review**
3. Wait for explicit approval before continuing with remaining features

**Why**: This catches architectural issues early before they propagate to all features.

## Output Format

```json
{
  "phase": "implementation",
  "status": "pass|fail",
  "score": 43,
  "maxScore": 50,
  "percentage": 86,
  "requiredPercentage": 85,
  "compilation": {
    "api": "success",
    "web": "success"
  },
  "codeQuality": {
    "typescriptErrors": 0,
    "csharpWarnings": 2,
    "eslintErrors": 0,
    "architecturalViolations": 0
  },
  "testing": {
    "unitTests": 12,
    "integrationTests": 3,
    "e2eTests": 2
  },
  "missingItems": [
    "Component usage examples",
    "Known limitations not documented"
  ],
  "criticalIssues": [],
  "readyForNextPhase": true,
  "requiresHumanReview": false
}
```

## Integration with Quality Gates

This skill enforces the quality gate thresholds by work type:

- **Feature**: 85% required (43/50 points)
- **Bug Fix**: 75% required (38/50 points)
- **Hotfix**: 70% required (35/50 points)
- **Refactoring**: 90% required (45/50 points)

## Progressive Disclosure

**Initial Context**: Show compilation and critical checks only
**On Request**: Show full validation with all scoring
**On Failure**: Show specific failing items with fix suggestions
**On First Vertical Slice**: Show full report + human review prompt

---

**Remember**: This skill validates implementation quality but doesn't test functionality. That's Phase 4's job. This ensures code is test-ready.
