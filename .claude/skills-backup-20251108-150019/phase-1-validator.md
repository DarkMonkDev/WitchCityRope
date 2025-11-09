---
name: phase-1-validator
description: Validates Requirements Phase completion before advancing to Design Phase. Checks business requirements document for completeness, quality gate compliance, and readiness for design work.
---

# Phase 1 (Requirements) Validation Skill

**Purpose**: Automate validation of Requirements Phase completion before advancing to Design Phase.

**When to Use**: When orchestrator or agents need to verify requirements are ready for design work.

## Quick Validation

Run this checklist against the requirements document:

```bash
# 1. Find the requirements document
find /home/chad/repos/witchcityrope/docs/functional-areas -name "business-requirements.md" -path "*/new-work/*" -type f | tail -1

# 2. Check document exists and is non-empty
if [ -f "$DOC_PATH" ] && [ -s "$DOC_PATH" ]; then
    echo "✅ Requirements document exists"
else
    echo "❌ Requirements document missing or empty"
    exit 1
fi
```

## Quality Gate Checklist (95% Required for Features)

### Document Structure (10 points)
- [ ] Executive Summary present (1 point)
- [ ] Business Context section complete (2 points)
- [ ] Success Metrics defined (2 points)
- [ ] User Stories section present (2 points)
- [ ] Business Rules documented (1 point)
- [ ] Security & Privacy section present (1 point)
- [ ] Quality Gate Checklist at bottom (1 point)

### Content Quality (10 points)
- [ ] At least 3 user stories for different roles (2 points)
- [ ] Each story has acceptance criteria (2 points)
- [ ] Business rules are specific and measurable (2 points)
- [ ] Security requirements address data protection (2 points)
- [ ] Success metrics are measurable (1 point)
- [ ] Examples/scenarios provided (1 point)

### WitchCityRope-Specific (5 points)
- [ ] Safety implications considered (1 point)
- [ ] Consent workflows addressed if applicable (1 point)
- [ ] Mobile experience considered (1 point)
- [ ] Impact on user roles documented (1 point)
- [ ] Community standards alignment verified (1 point)

## Automated Validation Script

```bash
#!/bin/bash
# Phase 1 Validation Script

REQUIREMENTS_DOC="$1"
SCORE=0
MAX_SCORE=25
REQUIRED_PERCENTAGE=95

echo "Phase 1 Requirements Validation"
echo "================================"
echo ""

# Check document structure
if grep -q "## Executive Summary" "$REQUIREMENTS_DOC"; then
    echo "✅ Executive Summary found"
    ((SCORE++))
else
    echo "❌ Executive Summary missing"
fi

if grep -q "## Business Context" "$REQUIREMENTS_DOC"; then
    echo "✅ Business Context found"
    ((SCORE+=2))
else
    echo "❌ Business Context missing"
fi

if grep -q "## Success Metrics" "$REQUIREMENTS_DOC" || grep -q "### Success Metrics" "$REQUIREMENTS_DOC"; then
    echo "✅ Success Metrics found"
    ((SCORE+=2))
else
    echo "❌ Success Metrics missing"
fi

if grep -q "## User Stories" "$REQUIREMENTS_DOC"; then
    STORY_COUNT=$(grep -c "^### Story [0-9]" "$REQUIREMENTS_DOC")
    if [ "$STORY_COUNT" -ge 3 ]; then
        echo "✅ User Stories found ($STORY_COUNT stories)"
        ((SCORE+=4))
    else
        echo "⚠️  Only $STORY_COUNT user stories found (need 3+)"
        ((SCORE+=2))
    fi
else
    echo "❌ User Stories section missing"
fi

if grep -q "## Business Rules" "$REQUIREMENTS_DOC"; then
    echo "✅ Business Rules found"
    ((SCORE++))
else
    echo "❌ Business Rules missing"
fi

if grep -q "## Security & Privacy Requirements" "$REQUIREMENTS_DOC"; then
    echo "✅ Security & Privacy section found"
    ((SCORE+=3))
else
    echo "❌ Security & Privacy section missing"
fi

if grep -q "## Quality Gate Checklist" "$REQUIREMENTS_DOC"; then
    echo "✅ Quality Gate Checklist found"
    ((SCORE++))
else
    echo "❌ Quality Gate Checklist missing"
fi

# WitchCityRope-specific checks
if grep -iq "safety\|safe\|safety first" "$REQUIREMENTS_DOC"; then
    echo "✅ Safety considerations addressed"
    ((SCORE++))
else
    echo "⚠️  Safety considerations not explicitly mentioned"
fi

if grep -iq "mobile\|phone\|responsive" "$REQUIREMENTS_DOC"; then
    echo "✅ Mobile experience considered"
    ((SCORE++))
else
    echo "⚠️  Mobile experience not explicitly mentioned"
fi

# Check acceptance criteria
AC_COUNT=$(grep -c "**Acceptance Criteria:**" "$REQUIREMENTS_DOC")
if [ "$AC_COUNT" -ge 3 ]; then
    echo "✅ Acceptance criteria provided ($AC_COUNT found)"
    ((SCORE+=2))
else
    echo "⚠️  Limited acceptance criteria ($AC_COUNT found, need 3+)"
    ((SCORE+=1))
fi

# Calculate percentage
PERCENTAGE=$((SCORE * 100 / MAX_SCORE))

echo ""
echo "================================"
echo "Final Score: $SCORE / $MAX_SCORE ($PERCENTAGE%)"
echo "Required: ${REQUIRED_PERCENTAGE}%"
echo ""

if [ "$PERCENTAGE" -ge "$REQUIRED_PERCENTAGE" ]; then
    echo "✅ PASS - Requirements Phase complete"
    echo "   Ready to advance to Design Phase"
    exit 0
else
    echo "❌ FAIL - Requirements Phase incomplete"
    echo "   Score: $PERCENTAGE% (need ${REQUIRED_PERCENTAGE}%)"
    echo "   Missing: $((MAX_SCORE - SCORE)) points"
    exit 1
fi
```

## Usage Examples

### From Orchestrator
```
Use the phase-1-validator skill to check if requirements are ready for design
```

### Manual Validation
```bash
# Find requirements document
REQ_DOC=$(find docs/functional-areas -name "business-requirements.md" -path "*/new-work/*" -type f | tail -1)

# Run validation
bash .claude/skills/phase-1-validator.md "$REQ_DOC"
```

### Integration with Workflow
The orchestrator should automatically invoke this skill:
- Before Phase 1 → Phase 2 transition
- After business-requirements agent completes work
- When user requests to "continue to design"

## Common Issues

### Issue: Missing Success Metrics
**Solution**: Business requirements agent should add specific, measurable metrics like:
- "Reduce event registration time by 50%"
- "Support 100+ concurrent users"
- "95% of users complete registration in 3 minutes"

### Issue: Generic User Stories
**Solution**: User stories should be specific to WitchCityRope context:
- ❌ "As a user, I want to see events"
- ✅ "As a Vetted Member, I want to browse members-only workshops"

### Issue: Missing Safety Considerations
**Solution**: Every feature should address safety implications:
- Data privacy for sensitive community
- Consent requirements for features
- Anonymous reporting options

## Output Format

The skill should output a validation report:

```json
{
  "phase": "requirements",
  "status": "pass|fail",
  "score": 23,
  "maxScore": 25,
  "percentage": 92,
  "requiredPercentage": 95,
  "missingItems": [
    "Mobile experience not mentioned",
    "Only 2 user stories (need 3+)"
  ],
  "recommendations": [
    "Add user story for mobile registration flow",
    "Add user story for admin monitoring"
  ],
  "readyForNextPhase": true
}
```

## Integration with Quality Gates

This skill enforces the quality gate thresholds by work type:

- **Feature**: 95% required (24/25 points)
- **Bug Fix**: 80% required (20/25 points)
- **Hotfix**: 70% required (18/25 points)
- **Documentation**: 85% required (21/25 points)
- **Refactoring**: 90% required (23/25 points)

## Progressive Disclosure

**Initial Context**: Show quick checklist
**On Request**: Show full validation script
**On Failure**: Show specific missing items and recommendations
**On Pass**: Show concise summary only

---

**Remember**: This skill automates validation, it doesn't replace the business-requirements agent's judgment. If validation fails, the orchestrator should loop back to business-requirements agent for completion.
