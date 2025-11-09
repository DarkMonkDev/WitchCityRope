---
name: handoff-document-generator
description: Generates standardized agent handoff documents for phase transitions. Ensures critical information is preserved and passed between agents during workflow orchestration. Automates handoff creation to prevent implementation failures from missing context.
---

# Handoff Document Generator Skill

**Purpose**: Automate creation of standardized handoff documents between agents and phases.

**When to Use**: At every phase transition to ensure context preservation.

## Why Handoffs Matter

**Critical Finding**: 90%+ of implementation failures traced to missing handoff documents.

**Problem**: Agents starting work without context make wrong assumptions.

**Solution**: Mandatory handoff documents at every transition.

## Handoff Types

### 1. Phase → Phase Handoffs
- Requirements → Design
- Design → Implementation
- Implementation → Testing
- Testing → Finalization

### 2. Agent → Agent Handoffs
- business-requirements → functional-spec
- functional-spec → database-designer
- database-designer → backend-developer
- ui-designer → react-developer

## Handoff Template

Location: `/docs/functional-areas/[feature]/new-work/[date]/handoffs/[source]-to-[target]-handoff.md`

```markdown
# Agent Handoff: [Source] → [Target]

**Date**: YYYY-MM-DD HH:MM
**Source Agent**: [agent-name]
**Target Agent**: [agent-name]
**Phase Transition**: [Phase X] → [Phase Y]
**Feature**: [Feature Name]
**Work Type**: Feature|Bug|Hotfix|Docs|Refactor

---

## Executive Summary
[2-3 sentences: What was completed, what's next, critical decisions]

## Completed Work
### Primary Deliverables
- [ ] [Deliverable 1] - Location: /path/to/file
- [ ] [Deliverable 2] - Location: /path/to/file

### Quality Gate Score
- Score: XX/YY (ZZ%)
- Required: AA%
- Status: PASS|FAIL

## Critical Decisions Made
1. **[Decision Topic]**
   - Decision: [What was decided]
   - Rationale: [Why this was chosen]
   - Impact: [Implications for next phase]
   - Alternatives Considered: [What was rejected and why]

## Context for Next Agent
### Must Read
- [ ] File: /path/to/requirements.md
- [ ] File: /path/to/design.md
- [ ] Lessons: /docs/lessons-learned/[agent]-lessons-learned.md

### Key Insights
- [Critical insight 1]
- [Critical insight 2]

### Assumptions Made
- [Assumption 1 - target agent must validate]
- [Assumption 2 - may need adjustment]

## Technical Specifications
### Architecture Decisions
- [ADR or architectural pattern chosen]
- [Technology stack decisions]

### Data Models
```
[Key schemas, types, or data structures]
```

### API Contracts
| Endpoint | Method | Purpose | Status |
|----------|--------|---------|--------|
| /api/... | GET | ... | Designed |

## Dependencies & Blockers
### Dependencies Required
- [ ] [Dependency 1] - Status: Ready|Pending
- [ ] [Dependency 2] - Status: Ready|Pending

### Known Blockers
- [Blocker if any - NONE is valid]

## Security & Privacy
- [Security requirements]
- [Data privacy considerations]
- [Auth/authz requirements]

## Testing Requirements
### Test Cases Needed
- [ ] [Test case 1]
- [ ] [Test case 2]

### Acceptance Criteria
- [ ] [Criteria 1]
- [ ] [Criteria 2]

## Questions for Target Agent
- [ ] [Question needing clarification]
- [ ] [Decision target agent should make]

## Files Created/Modified
| File | Action | Purpose |
|------|--------|---------|
| /path/to/file.md | CREATED | [purpose] |

## Next Steps
1. [Step 1 for target agent]
2. [Step 2 for target agent]
3. [Step 3 for target agent]

## Validation Checklist
- [ ] All deliverables complete
- [ ] Quality gate passed
- [ ] Decisions documented
- [ ] Context provided
- [ ] Dependencies identified
- [ ] Files logged in registry
- [ ] Next steps clear

---

**Target Agent**: Read this entire handoff before starting work. Validate all assumptions.
```

## Automated Generator Script

```bash
#!/bin/bash
# Handoff Document Generator

SOURCE_AGENT="$1"
TARGET_AGENT="$2"
FEATURE_NAME="$3"
WORK_TYPE="${4:-Feature}"

if [ -z "$SOURCE_AGENT" ] || [ -z "$TARGET_AGENT" ] || [ -z "$FEATURE_NAME" ]; then
    echo "Usage: $0 <source-agent> <target-agent> <feature-name> [work-type]"
    echo "Example: $0 business-requirements functional-spec user-management Feature"
    exit 1
fi

# Find new-work directory
NEW_WORK_DIR=$(find docs/functional-areas -type d -path "*/$FEATURE_NAME/new-work/*" | sort | tail -1)

if [ -z "$NEW_WORK_DIR" ]; then
    echo "Error: New-work directory not found for feature: $FEATURE_NAME"
    exit 1
fi

# Create handoffs directory
HANDOFF_DIR="$NEW_WORK_DIR/handoffs"
mkdir -p "$HANDOFF_DIR"

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
find "$NEW_WORK_DIR" -type f -name "*.md" -mtime -1 | while read file; do
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
git diff --name-status HEAD~1 | while read status file; do
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
echo "✅ Handoff document created:"
echo "   $HANDOFF_FILE"
echo ""
echo "📝 Next: $SOURCE_AGENT must complete all TODO sections"
echo "📖 Then: $TARGET_AGENT must read before starting work"
```

## Usage Examples

### From Orchestrator (Automated)
```
Use the handoff-document-generator skill to create handoff from business-requirements to functional-spec
```

### Manual Generation
```bash
# Create handoff document
bash .claude/skills/handoff-document-generator.md \
  business-requirements \
  functional-spec \
  user-management \
  Feature
```

### From Agent (Self-Service)
```
Before completing my work, I'll use the handoff-document-generator skill to create a handoff document for the next agent.
```

## Handoff Scenarios

### Requirements → Design
**Source**: business-requirements
**Target**: functional-spec, database-designer, ui-designer
**Critical Context**: User stories, business rules, success metrics
**Decisions**: Feature scope, user roles affected, priority

### Design → Implementation
**Source**: functional-spec, database-designer, ui-designer
**Target**: react-developer, backend-developer
**Critical Context**: Architecture, data models, API contracts
**Decisions**: Tech stack, component structure, database schema

### Implementation → Testing
**Source**: react-developer, backend-developer
**Target**: test-executor
**Critical Context**: What was implemented, where, how to test
**Decisions**: Test approach, fixtures needed, edge cases

### Testing → Finalization
**Source**: test-executor
**Target**: librarian, git-manager
**Critical Context**: Test results, coverage, blockers
**Decisions**: Deployment readiness, rollback plan

## Enforcement Rules

### Mandatory Handoffs
**REQUIRED at these transitions:**
- business-requirements → functional-spec (MANDATORY)
- functional-spec → database-designer (if DB changes)
- functional-spec → react-developer (MANDATORY)
- database-designer → backend-developer (MANDATORY)
- ui-designer → react-developer (if new UI)
- backend-developer → test-executor (MANDATORY)
- react-developer → test-executor (MANDATORY)
- test-executor → git-manager (MANDATORY)

### Orchestrator Enforcement
```
Before transitioning from Phase X to Phase Y:
1. Check if handoff exists
2. Verify handoff is complete (no TODO sections)
3. Require target agent to acknowledge reading handoff
4. Block transition if handoff missing
```

## Output Format

```json
{
  "handoff": {
    "source": "business-requirements",
    "target": "functional-spec",
    "feature": "user-management",
    "workType": "Feature",
    "phaseTransition": "Phase 1 → Phase 2",
    "file": "/docs/functional-areas/.../handoffs/business-requirements-to-functional-spec-handoff.md",
    "status": "created",
    "todoSectionsRemaining": 8,
    "complete": false
  },
  "nextSteps": [
    "business-requirements agent: Complete all TODO sections",
    "Run phase-1-validator to get quality gate score",
    "functional-spec agent: Read handoff before starting"
  ]
}
```

## Common Issues

### Issue: Handoff Not Created
**Symptom**: Agent completes work but creates no handoff
**Impact**: Next agent starts with zero context
**Solution**: Orchestrator must REQUIRE handoff creation

### Issue: Incomplete Handoff
**Symptom**: TODO sections not filled in
**Impact**: Next agent has template but no real information
**Solution**: Phase validators check for complete handoffs

### Issue: Handoff Not Read
**Symptom**: Target agent asks questions already answered in handoff
**Impact**: Wasted time, repeated mistakes
**Solution**: Require agents to acknowledge reading handoff

## Integration with Lessons Learned

**Handoffs are NOT lessons learned:**
- Handoffs = Context for THIS feature NOW
- Lessons = Patterns for FUTURE features ALWAYS

**When to promote handoff content to lessons:**
- Decision pattern used repeatedly
- Common pitfall discovered
- Successful approach worth standardizing

## Progressive Disclosure

**Initial Context**: Show handoff location and whether it exists
**On Request**: Generate template with TODOs
**On Completion**: Show next agent what to read
**On Failure**: Show which sections are incomplete

---

**Remember**: Handoff documents are the memory system for multi-agent workflows. Without them, agents work in isolation and fail. With them, agents build on each other's work successfully.
