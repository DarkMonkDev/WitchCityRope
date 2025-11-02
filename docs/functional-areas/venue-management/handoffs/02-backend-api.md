# Backend API Handoff Document

## Phase: Backend API Implementation
## Date: [TO BE COMPLETED]
## Feature: Venue Management

## 🎯 CRITICAL BUSINESS RULES (MUST IMPLEMENT)

1. **[RULE_NAME]**: [Clear, specific description]
   - ✅ Correct: [Example of correct implementation]
   - ❌ Wrong: [Example of incorrect implementation]

2. **[RULE_NAME]**: [Clear, specific description]
   - ✅ Correct: [Example]
   - ❌ Wrong: [Example]

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Business Requirements | `/docs/functional-areas/venue-management/requirements/venue-management-requirements.md` | FR-5 (API Endpoints) |
| Database Design Handoff | `/docs/functional-areas/venue-management/handoffs/01-database-design.md` | Data Model, Critical Rules |
| Vertical Slice Guide | `/docs/standards-processes/backend/vertical-slice-implementation-guide.md` | API endpoint patterns |

## 🚨 KNOWN PITFALLS

1. **[PITFALL_NAME]**: [Description]
   - **Why it happens**: [Reason]
   - **How to avoid**: [Prevention strategy]

## ✅ VALIDATION CHECKLIST

Before proceeding to UI design phase, verify:

- [ ] All 6 API endpoints implemented and working
- [ ] Admin authorization enforced on CUD operations
- [ ] Soft delete uses IsActive flag (not hard delete)
- [ ] Name uniqueness validation working
- [ ] Proper error responses (400, 401, 403, 404)
- [ ] Unit tests written and passing
- [ ] Integration tests cover all endpoints
- [ ] API builds without errors

## 🔄 DISCOVERED CONSTRAINTS

1. **Existing Code**: [Description of what exists]
   - **Impact**: [How this affects implementation]
   - **Required Changes**: [What needs modification]

## 📊 DATA MODEL DECISIONS

[Backend API service patterns, DTO transformations, validation logic]

## 🎯 SUCCESS CRITERIA

1. **Test Case**: [Description]
   - **Input**: [Test input]
   - **Expected Output**: [Expected result]

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT [specific thing to avoid]
- ❌ DO NOT [another thing to avoid]

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| [Term] | [Definition] | [Example] |

## 🔗 NEXT AGENT INSTRUCTIONS

**Next Agent**: UI Designer

1. **FIRST**: Read business requirements (FR-3 Admin Interface)
2. **SECOND**: Review this handoff for API capabilities
3. **THIRD**: Review existing admin UI patterns
4. **THEN**: Begin wireframe design

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Database Designer
**Previous Phase Completed**: [Date]
**Key Finding**: [One-sentence summary]

**Next Agent Should Be**: UI Designer
**Next Phase**: UI Design & Wireframes
**Estimated Effort**: 2-3 hours

---

**NOTE**: This handoff will be completed by the Backend API Developer after implementing the API layer.
