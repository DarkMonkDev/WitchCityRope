---
name: quality-gate-calculator
description: Calculates context-appropriate quality gate thresholds based on work type (Feature/Bug/Hotfix/Docs/Refactor). Ensures rigorous standards for features, pragmatic standards for hotfixes, and 100% test pass rate for all work.
---

# Quality Gate Calculator Skill

**Purpose**: Calculate appropriate quality gates based on work type and phase.

**When to Use**: At workflow start to set expectations, and at phase transitions to validate progress.

## Quality Gate Philosophy

**Not all work is equal:**
- **Features**: Highest rigor (new functionality, high risk)
- **Bug Fixes**: Moderate rigor (fixing existing code)
- **Hotfixes**: Minimal rigor (production emergency)
- **Documentation**: High completion, moderate validation
- **Refactoring**: High quality, no behavior change

**Universal Rule**: ALL work types require 100% test pass rate in Phase 4.

## Quality Gate Matrix

### Phase 1: Requirements

| Work Type | Required % | Score Target | Rationale |
|-----------|------------|--------------|-----------|
| Feature | 95% | 24/25 points | Complete requirements critical for feature success |
| Bug Fix | 80% | 20/25 points | Focus on problem and fix, less ceremony |
| Hotfix | 70% | 18/25 points | Emergency - just enough to understand issue |
| Documentation | 85% | 21/25 points | Clear purpose and scope needed |
| Refactoring | 90% | 23/25 points | Understand current state and goals |

### Phase 2: Design

| Work Type | Required % | Score Target | Rationale |
|-----------|------------|--------------|-----------|
| Feature | 90% | 32/35 points | Thorough design prevents implementation issues |
| Bug Fix | 70% | 25/35 points | Focus on fix design, skip elaborate specs |
| Hotfix | 60% | 21/35 points | Quick design - enough to implement safely |
| Documentation | 80% | 28/35 points | Structure and organization plan |
| Refactoring | 85% | 30/35 points | Detailed refactoring plan with safety checks |

### Phase 3: Implementation

| Work Type | Required % | Score Target | Rationale |
|-----------|------------|--------------|-----------|
| Feature | 85% | 43/50 points | High code quality for new features |
| Bug Fix | 75% | 38/50 points | Focus on fix, tests, and no regressions |
| Hotfix | 70% | 35/50 points | Working fix with minimal tests |
| Documentation | 80% | 40/50 points | Complete documentation with examples |
| Refactoring | 90% | 45/50 points | Highest quality - no behavior changes allowed |

### Phase 4: Testing

| Work Type | Required % | Test Pass Rate | Rationale |
|-----------|------------|----------------|-----------|
| Feature | 100% | 100% | **ALL tests must pass** |
| Bug Fix | 100% | 100% | **ALL tests must pass** |
| Hotfix | 100% | 100% | **ALL tests must pass** |
| Documentation | 100% | 100% | **ALL tests must pass** |
| Refactoring | 100% | 100% | **ALL tests must pass** |

**CRITICAL**: Phase 4 is ZERO TOLERANCE for all work types. One failing test = cannot advance.

### Phase 5: Finalization

| Work Type | Required % | Score Target | Rationale |
|-----------|------------|--------------|-----------|
| Feature | 80% | 80/100 points | Complete documentation and cleanup |
| Bug Fix | 75% | 75/100 points | Document fix and lessons learned |
| Hotfix | 70% | 70/100 points | Basic documentation, commit, deploy |
| Documentation | 90% | 90/100 points | Comprehensive docs and cross-references |
| Refactoring | 85% | 85/100 points | Document changes and performance impact |

## Automated Calculator Script

```bash
#!/bin/bash
# Quality Gate Calculator

WORK_TYPE="$1"
PHASE="$2"

if [ -z "$WORK_TYPE" ] || [ -z "$PHASE" ]; then
    echo "Usage: $0 <work-type> <phase>"
    echo ""
    echo "Work Types: Feature, Bug, Hotfix, Documentation, Refactoring"
    echo "Phases: 1, 2, 3, 4, 5"
    exit 1
fi

# Normalize inputs
WORK_TYPE=$(echo "$WORK_TYPE" | tr '[:upper:]' '[:lower:]')
PHASE=$(echo "$PHASE" | tr -d 'Phase ')

echo "Quality Gate Calculator"
echo "======================="
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
            *) echo "❌ Unknown work type: $WORK_TYPE"; exit 1 ;;
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
            *) echo "❌ Unknown work type: $WORK_TYPE"; exit 1 ;;
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
            *) echo "❌ Unknown work type: $WORK_TYPE"; exit 1 ;;
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
            *) echo "❌ Unknown work type: $WORK_TYPE"; exit 1 ;;
        esac
        PHASE_NAME="Finalization"
        ;;

    *)
        echo "❌ Unknown phase: $PHASE"
        echo "Valid phases: 1, 2, 3, 4, 5"
        exit 1
        ;;
esac

# Output quality gate
echo "Phase: $PHASE ($PHASE_NAME)"
echo "Work Type: $(echo $WORK_TYPE | sed 's/\b\(.\)/\u\1/g')"
echo ""
echo "Quality Gate:"
echo "  Required: $REQUIRED%"
echo "  Target Score: $TARGET / $MAX_SCORE"
echo ""

# Provide context-specific guidance
if [ "$PHASE" -eq 4 ]; then
    echo "⚠️  CRITICAL: Phase 4 requires 100% test pass rate"
    echo "   NO EXCEPTIONS for any work type"
    echo "   One failing test = Phase 4 fails"
    echo ""
fi

# Output rationale
echo "Rationale:"
case "$WORK_TYPE" in
    "feature")
        echo "  Features require highest rigor due to:"
        echo "  - New functionality introduction"
        echo "  - Higher risk of bugs"
        echo "  - Long-term maintenance impact"
        ;;
    "bug"|"bugfix")
        echo "  Bug fixes have moderate rigor:"
        echo "  - Focus on problem and solution"
        echo "  - Less ceremony than features"
        echo "  - Still requires thorough testing"
        ;;
    "hotfix")
        echo "  Hotfixes have minimal rigor:"
        echo "  - Production emergency"
        echo "  - Speed is critical"
        echo "  - Still requires 100% test pass in Phase 4"
        ;;
    "documentation"|"docs")
        echo "  Documentation requires high completion:"
        echo "  - Clear structure and organization"
        echo "  - Complete coverage of topic"
        echo "  - Examples and cross-references"
        ;;
    "refactoring"|"refactor")
        echo "  Refactoring requires highest quality:"
        echo "  - No behavior changes allowed"
        echo "  - Comprehensive test coverage"
        echo "  - Performance validation"
        ;;
esac

echo ""
echo "Pass/Fail Example:"
echo "  Score of $TARGET = PASS (exactly $REQUIRED%)"
echo "  Score of $((TARGET - 1)) = FAIL ($((TARGET - 1)) points = $(((TARGET - 1) * 100 / MAX_SCORE))%)"
echo ""

# Export variables for use in validation scripts
cat > /tmp/quality-gate.env <<EOF
WORK_TYPE=$WORK_TYPE
PHASE=$PHASE
REQUIRED_PERCENTAGE=$REQUIRED
TARGET_SCORE=$TARGET
MAX_SCORE=$MAX_SCORE
EOF

echo "✅ Quality gate calculated"
echo "   Exported to: /tmp/quality-gate.env"
```

## Usage Examples

### From Orchestrator (Workflow Start)
```
Use the quality-gate-calculator skill to set quality gates for this feature work
```

### Manual Calculation
```bash
# Calculate quality gate for bug fix in design phase
bash .claude/skills/quality-gate-calculator.md Bug 2

# Calculate for hotfix in implementation
bash .claude/skills/quality-gate-calculator.md Hotfix 3
```

### From Validator
```bash
# Phase validator loads quality gate
source /tmp/quality-gate.env
echo "Required: $REQUIRED_PERCENTAGE%"
echo "Target: $TARGET_SCORE / $MAX_SCORE"
```

## Decision Flowchart

```
Start Workflow
     |
     v
Identify Work Type
     |
     +-- Is it production emergency? --> Hotfix (70-100%)
     |
     +-- Is it fixing existing bug? --> Bug Fix (75-100%)
     |
     +-- Is it new functionality? --> Feature (85-100%)
     |
     +-- Is it improving code? --> Refactoring (85-100%)
     |
     +-- Is it documentation? --> Documentation (80-100%)
     v
Calculate Quality Gates for Each Phase
     v
Store in Workflow Context
     v
Apply at Each Phase Validation
```

## Work Type Classification Guide

### Feature
**Characteristics**:
- Adds new functionality
- Creates new user-facing capabilities
- Introduces new APIs or components

**Examples**:
- "Add event registration system"
- "Implement user dashboard"
- "Create teacher profile pages"

### Bug Fix
**Characteristics**:
- Fixes existing functionality
- Addresses reported issues
- Resolves unexpected behavior

**Examples**:
- "Fix login button not working"
- "Resolve event date display issue"
- "Correct user role assignment"

### Hotfix
**Characteristics**:
- Production emergency
- Blocking critical functionality
- Requires immediate deployment

**Examples**:
- "Fix payment processing failure"
- "Resolve database connection timeout"
- "Patch security vulnerability"

### Documentation
**Characteristics**:
- Creates or updates documentation
- No code changes (or minimal)
- Improves developer/user understanding

**Examples**:
- "Document API endpoints"
- "Create onboarding guide"
- "Update architecture diagrams"

### Refactoring
**Characteristics**:
- Improves code quality
- No behavior changes
- Performance or maintainability focus

**Examples**:
- "Extract service layer"
- "Optimize database queries"
- "Simplify component structure"

## Integration with Validators

Each phase validator loads work type-specific quality gates:

```bash
#!/bin/bash
# In phase-1-validator.md

# Load quality gate for this work type
source /tmp/quality-gate.env

# Use in validation
if [ "$PERCENTAGE" -ge "$REQUIRED_PERCENTAGE" ]; then
    echo "✅ PASS - Meets quality gate"
else
    echo "❌ FAIL - Below quality gate"
    echo "   Required: $REQUIRED_PERCENTAGE%"
    echo "   Actual: $PERCENTAGE%"
fi
```

## Common Issues

### Issue: Work Type Misclassification
**Problem**: Feature classified as bug fix to lower quality gates
**Impact**: Lower quality work slips through
**Solution**: Orchestrator validates work type at workflow start

### Issue: Quality Gate Gaming
**Problem**: Developer tries to lower requirements
**Impact**: System integrity compromised
**Solution**: Quality gates are non-negotiable per work type

### Issue: Hotfix Abuse
**Problem**: Everything labeled "hotfix" to skip rigor
**Impact**: Production quality degrades
**Solution**: Hotfix requires prod issue ticket + approval

## Output Format

```json
{
  "qualityGate": {
    "workType": "Feature",
    "phase": 3,
    "phaseName": "Implementation",
    "required": {
      "percentage": 85,
      "score": 43,
      "maxScore": 50
    },
    "rationale": "Features require high code quality for new features",
    "criticalRules": [
      "Phase 4 requires 100% test pass rate regardless of work type"
    ],
    "exported": "/tmp/quality-gate.env"
  }
}
```

## Progressive Disclosure

**Initial Context**: Show work type and required percentage only
**On Request**: Show full matrix with rationale
**During Validation**: Show pass/fail threshold
**On Failure**: Show how far from target and what's needed

---

**Remember**: Quality gates ensure appropriate rigor for the work being done. Features get highest scrutiny, hotfixes get pragmatic validation, but ALL work requires 100% test pass rate. This balances quality with velocity.
