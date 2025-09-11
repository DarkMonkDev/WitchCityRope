# Agent Handoff Documentation Template

## Purpose
Ensure critical information discovered by early-phase agents is explicitly passed to implementation agents, preventing the mismatch between requirements and implementation.

## When to Use
Create this document at the END of each phase before starting the next:
- After Business Requirements → Before Functional Design
- After Functional Design → Before Implementation
- After Implementation → Before Testing

---

# AGENT HANDOFF DOCUMENT

## Phase: [PHASE_NAME]
## Date: [YYYY-MM-DD]
## Feature: [FEATURE_NAME]

## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

List the TOP 5 business rules that MUST be implemented correctly:

1. **[RULE_NAME]**: [Clear, specific description]
   - ✅ Correct: [Example of correct implementation]
   - ❌ Wrong: [Example of incorrect implementation]

2. **[RULE_NAME]**: [Clear, specific description]
   - ✅ Correct: [Example]
   - ❌ Wrong: [Example]

## 📍 KEY DOCUMENTS TO READ

List documents the next agent MUST read before starting:

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Business Requirements | `/docs/functional-areas/[feature]/requirements/business-requirements.md` | Lines 196-200: RSVP vs Ticket distinction |
| Functional Design | `/docs/functional-areas/[feature]/functional-design.md` | Data model section |
| User Flows | `/docs/functional-areas/[feature]/user-flows.md` | Social event RSVP flow |

## 🚨 KNOWN PITFALLS

List mistakes that are likely to happen:

1. **[PITFALL_NAME]**: [Description]
   - **Why it happens**: [Reason]
   - **How to avoid**: [Prevention strategy]

## ✅ VALIDATION CHECKLIST

Before proceeding to next phase, verify:

- [ ] Implementation matches business rule #1: [RULE_NAME]
- [ ] Implementation matches business rule #2: [RULE_NAME]
- [ ] No hardcoded assumptions that contradict requirements
- [ ] Domain model supports all use cases
- [ ] API endpoints align with user flows

## 🔄 DISCOVERED CONSTRAINTS

List any technical constraints or existing code that affects implementation:

1. **Existing Code**: [Description of what exists]
   - **Impact**: [How this affects new implementation]
   - **Required Changes**: [What needs modification]

## 📊 DATA MODEL DECISIONS

Explicit data model requirements:

```
Entity: [NAME]
- Field1: Type (Required/Optional) - Purpose
- Field2: Type (Required/Optional) - Purpose

Business Logic:
- Rule1: [Description]
- Rule2: [Description]
```

## 🎯 SUCCESS CRITERIA

How to know implementation is correct:

1. **Test Case**: [Description]
   - **Input**: [Test input]
   - **Expected Output**: [Expected result]

2. **Test Case**: [Description]
   - **Input**: [Test input]
   - **Expected Output**: [Expected result]

## ⚠️ DO NOT IMPLEMENT

Explicitly list what should NOT be done:

- ❌ DO NOT [specific thing to avoid]
- ❌ DO NOT [another thing to avoid]
- ❌ DO NOT assume [specific assumption to avoid]

## 📝 TERMINOLOGY DICTIONARY

Define key terms to prevent confusion:

| Term | Definition | Example |
|------|------------|---------|
| RSVP | Free reservation for social events | User clicks "RSVP" → No payment required |
| Registration | Paid ticket purchase | User clicks "Buy Ticket" → Payment required |
| Social Event | Community gathering with optional payment | Allows both RSVP and ticket purchase |
| Class | Educational event requiring payment | Only ticket purchase, no RSVP option |

## 🔗 NEXT AGENT INSTRUCTIONS

Specific instructions for the next agent:

1. **FIRST**: Read documents listed in Key Documents section
2. **SECOND**: Review existing code at: [paths]
3. **THIRD**: Validate your understanding against Success Criteria
4. **THEN**: Begin implementation following the constraints

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: [Name/Type]
**Previous Phase Completed**: [Date]
**Key Finding**: [One-sentence summary of most important discovery]

**Next Agent Should Be**: [Recommended agent type]
**Next Phase**: [Phase name]
**Estimated Effort**: [Hours/Days]

---

## Example Usage for Events Management

### After Business Requirements Phase:

```markdown
## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

1. **Social Events Have Dual Options**: Social events MUST show both RSVP (free) and Purchase Ticket buttons
   - ✅ Correct: Two separate buttons, user can do either or both
   - ❌ Wrong: Single "Register" button that requires payment

2. **Classes Require Payment**: Classes/Workshops MUST only show Purchase Ticket (no RSVP)
   - ✅ Correct: Only ticket purchase flow for EventType.Class
   - ❌ Wrong: Allowing free registration for classes

3. **RSVP Plus Ticket Allowed**: Users who RSVP can still purchase tickets later
   - ✅ Correct: Check for RSVP, still show ticket purchase option
   - ❌ Wrong: Hide ticket button after RSVP

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Business Requirements | `/docs/functional-areas/events/requirements/business-requirements.md` | Lines 196-200: Event type rules |
| User Flows | `/docs/functional-areas/events/user-flows.md` | RSVP vs Ticket flowchart |

## 🚨 KNOWN PITFALLS

1. **Registration Entity Confusion**: Existing Registration entity assumes payment
   - **Why it happens**: Legacy code from payment-only system
   - **How to avoid**: Create separate RSVP entity or extend Registration

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT use Registration entity for free RSVPs without modification
- ❌ DO NOT create a generic "registration" flow
- ❌ DO NOT assume all events work the same way
```

---

## Implementation Notes

1. **Create at Phase Boundaries**: Generate this document when switching between major phases
2. **Keep It Focused**: Maximum 2 pages - only critical information
3. **Use Examples**: Show correct vs incorrect implementations
4. **Be Explicit**: Don't assume knowledge - state everything clearly
5. **Version Control**: Commit immediately after creation

## File Naming Convention

```
/docs/functional-areas/[feature]/handoffs/
  - requirements-to-design-handoff.md
  - design-to-implementation-handoff.md
  - implementation-to-testing-handoff.md
```

## Automation Suggestion

Consider adding a git hook or CI check that requires a handoff document when:
- Moving from one phase to another
- Switching between agent types
- After completing requirements gathering

This template ensures critical business rules and discovered constraints are explicitly communicated between agents, preventing the "telephone game" effect where information gets lost or distorted.