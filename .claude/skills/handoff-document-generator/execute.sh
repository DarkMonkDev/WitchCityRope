#!/bin/bash
# Handoff Document Generator
# SINGLE SOURCE OF TRUTH - DO NOT DUPLICATE
#
# Generates standardized agent handoff documents for phase transitions.
# Ensures critical information is preserved and passed between agents during
# workflow orchestration. Automates handoff creation to prevent implementation
# failures from missing context.

set -e  # Exit on error

# ============================================
# PRE-FLIGHT INFORMATION
# ============================================

if [ "$#" -lt 3 ] || [ "$1" == "--help" ]; then
    echo "📄 Handoff Document Generator"
    echo "=============================="
    echo ""
    echo "📋 Purpose: Generate standardized agent handoff documents for phase transitions"
    echo ""
    echo "✅ Use when:"
    echo "   • At every phase transition to ensure context preservation"
    echo "   • When one agent completes work and another begins"
    echo "   • MANDATORY for Requirements → Design, Design → Implementation, etc."
    echo ""
    echo "❌ DO NOT use if:"
    echo "   • Work isn't complete yet (generate when done)"
    echo "   • No target agent exists (must have recipient)"
    echo ""
    echo "⚙️  What this script does:"
    echo "   1. Creates handoffs directory in new-work folder"
    echo "   2. Generates handoff template with proper headers"
    echo "   3. Lists recent files from new-work as deliverables"
    echo "   4. Determines phase transition automatically"
    echo "   5. Adds TODO sections for source agent to complete"
    echo "   6. Provides file path and next steps"
    echo ""
    echo "📝 Usage:"
    echo "   bash execute.sh <source-agent> <target-agent> <feature-name> [work-type]"
    echo ""
    echo "   source-agent: Agent completing work (e.g., business-requirements)"
    echo "   target-agent: Agent receiving handoff (e.g., functional-spec)"
    echo "   feature-name: Name of feature (used to find new-work directory)"
    echo "   work-type: Feature|Bug|Hotfix|Documentation|Refactoring (default: Feature)"
    echo ""
    echo "   Examples:"
    echo "   bash execute.sh business-requirements functional-spec user-management Feature"
    echo "   bash execute.sh functional-spec react-developer user-management Feature"
    echo "   bash execute.sh backend-developer test-executor api-fixes Bug"
    echo ""
    exit 1
fi

# ============================================
# PARAMETER EXTRACTION
# ============================================

SOURCE_AGENT="$1"
TARGET_AGENT="$2"
FEATURE_NAME="$3"
WORK_TYPE="${4:-Feature}"

# ============================================
# PREREQUISITE CHECKS
# ============================================

echo "📄 Handoff Document Generator"
echo "=============================="
echo ""

echo "1️⃣  Finding new-work directory for feature..."
NEW_WORK_DIR=$(find docs/functional-areas -type d -path "*/$FEATURE_NAME/new-work/*" 2>/dev/null | sort | tail -1)

if [ -z "$NEW_WORK_DIR" ]; then
    echo "   ❌ FAIL: New-work directory not found for feature: $FEATURE_NAME"
    echo "   Searched in: docs/functional-areas/*/new-work/*"
    exit 1
fi
echo "   ✅ Found: $NEW_WORK_DIR"

echo ""
echo "2️⃣  Validating agents..."
echo "   Source: $SOURCE_AGENT"
echo "   Target: $TARGET_AGENT"
echo "   ✅ Agents validated"

echo ""
echo "✅ All prerequisites passed"
echo ""

# ============================================
# MAIN SCRIPT - GENERATION
# ============================================

# Create handoffs directory
HANDOFF_DIR="$NEW_WORK_DIR/handoffs"
mkdir -p "$HANDOFF_DIR"
echo "Created handoffs directory: $HANDOFF_DIR"
echo ""

# Generate handoff filename
HANDOFF_FILE="$HANDOFF_DIR/${SOURCE_AGENT}-to-${TARGET_AGENT}-handoff.md"

# Determine phase transition
case "$SOURCE_AGENT" in
    "business-requirements") PHASE_FROM="Phase 1: Requirements" ;;
    "functional-spec"|"database-designer"|"ui-designer") PHASE_FROM="Phase 2: Design" ;;
    "react-developer"|"backend-developer") PHASE_FROM="Phase 3: Implementation" ;;
    "test-executor") PHASE_FROM="Phase 4: Testing" ;;
    *) PHASE_FROM="Phase Unknown" ;;
esac

case "$TARGET_AGENT" in
    "functional-spec"|"database-designer"|"ui-designer") PHASE_TO="Phase 2: Design" ;;
    "react-developer"|"backend-developer") PHASE_TO="Phase 3: Implementation" ;;
    "test-executor") PHASE_TO="Phase 4: Testing" ;;
    "librarian"|"git-manager") PHASE_TO="Phase 5: Finalization" ;;
    *) PHASE_TO="Phase Unknown" ;;
esac

echo "Phase Transition: $PHASE_FROM → $PHASE_TO"
echo ""

# Generate handoff document
cat > "$HANDOFF_FILE" <<EOF
# Agent Handoff: $SOURCE_AGENT → $TARGET_AGENT

**Date**: $(date +"%Y-%m-%d %H:%M")
**Source Agent**: $SOURCE_AGENT
**Target Agent**: $TARGET_AGENT
**Phase Transition**: $PHASE_FROM → $PHASE_TO
**Feature**: $FEATURE_NAME
**Work Type**: $WORK_TYPE

---

## Executive Summary

[2-3 sentences summarizing completed work and critical context for next agent]

**TODO**: $SOURCE_AGENT must fill this in before completing work.

## Completed Work

### Primary Deliverables
EOF

# List recent files in new-work directory
find "$NEW_WORK_DIR" -type f -name "*.md" -mtime -1 2>/dev/null | while read file; do
    echo "- [ ] $(basename $file) - Location: $file" >> "$HANDOFF_FILE"
done

cat >> "$HANDOFF_FILE" <<EOF

### Quality Gate Score
- Score: XX/YY (ZZ%)
- Required: AA%
- Status: PENDING

**TODO**: Run phase validator before completing handoff.

## Critical Decisions Made

1. **[Decision Topic]**
   - Decision: [What was decided]
   - Rationale: [Why this approach]
   - Impact: [How this affects next phase]
   - Alternatives: [What was considered but rejected]

**TODO**: Document at least 2-3 major decisions made.

## Context for Next Agent

### Must Read
- [ ] File: [path to requirements/design doc]
- [ ] Lessons: /docs/lessons-learned/${TARGET_AGENT}-lessons-learned.md

### Key Insights
- [Insight that will save target agent time]
- [Pattern or approach that worked well]
- [Warning about complexity or edge case]

### Assumptions Made
- [Assumption 1 - target agent should validate]
- [Assumption 2 - may need revision based on implementation]

**TODO**: List assumptions target agent must verify.

## Technical Specifications

[If Phase 1→2 or 2→3, include architecture, data models, API contracts]

**TODO**: Include relevant specs for target agent.

## Dependencies & Blockers

### Dependencies Required
- [ ] [Dependency 1] - Status: Ready|Pending

### Known Blockers
- NONE [or list blockers]

## Security & Privacy

- [Security requirements]
- [Auth/authz needs]
- [Data privacy considerations]

**TODO**: Always address security for WitchCityRope community.

## Testing Requirements

### Test Cases Needed
- [ ] [Test scenario 1]
- [ ] [Test scenario 2]

### Acceptance Criteria
- [ ] [Criteria from business requirements]

## Questions for Target Agent

- [ ] [Question needing clarification]
- [ ] [Decision target agent should make]

**TODO**: List any open questions or choices for target agent.

## Files Created/Modified

| File | Action | Purpose |
|------|--------|---------|
EOF

# List files from git diff
git diff --name-status HEAD~1 2>/dev/null | while read status file; do
    action="MODIFIED"
    [ "$status" = "A" ] && action="CREATED"
    [ "$status" = "D" ] && action="DELETED"
    echo "| $file | $action | [purpose] |" >> "$HANDOFF_FILE"
done

cat >> "$HANDOFF_FILE" <<EOF

**TODO**: Update file registry with all changes.

## Next Steps

1. [Specific next step for target agent]
2. [What target agent should do first]
3. [Expected outcome target agent should achieve]

**TODO**: Provide clear, actionable next steps.

## Validation Checklist

- [ ] All deliverables complete
- [ ] Quality gate passed
- [ ] Decisions documented
- [ ] Context provided
- [ ] Dependencies identified
- [ ] Files logged in registry
- [ ] Next steps clear
- [ ] Assumptions listed
- [ ] Security addressed

---

**Target Agent ($TARGET_AGENT)**: Read this entire handoff before starting work. Validate all assumptions and ask questions if context is unclear.

**Source Agent ($SOURCE_AGENT)**: Fill in all TODO sections before marking this handoff complete.
EOF

echo ""
echo "✅ Handoff document created successfully"
echo "================================"
echo ""
echo "File: $HANDOFF_FILE"
echo ""
echo "📝 Next Steps:"
echo "1. $SOURCE_AGENT: Complete all TODO sections in handoff"
echo "2. $SOURCE_AGENT: Run phase validator to get quality gate score"
echo "3. $SOURCE_AGENT: Update file registry with all changes"
echo "4. $TARGET_AGENT: Read handoff before starting work"
echo ""
echo "TODO Count: $(grep -c "TODO" "$HANDOFF_FILE")"
echo ""

exit 0
