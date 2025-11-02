# Backend Tests Handoff Document

## Phase: Backend Unit & Integration Tests
## Date: [TO BE COMPLETED]
## Feature: Venue Management

## 🎯 CRITICAL TESTING RULES (MUST FOLLOW)

1. **[RULE_NAME]**: [Clear, specific description]
   - ✅ Correct: [Example of correct test]
   - ❌ Wrong: [Example of incorrect test]

2. **[RULE_NAME]**: [Clear, specific description]
   - ✅ Correct: [Example]
   - ❌ Wrong: [Example]

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Backend API Handoff | `/docs/functional-areas/venue-management/handoffs/02-backend-api.md` | Service logic, validation |
| Database Design Handoff | `/docs/functional-areas/venue-management/handoffs/01-database-design.md` | Business rules |
| Testing Guide | `/docs/standards-processes/testing/backend-testing-guide.md` | Test patterns |
| Test Catalog | `/docs/standards-processes/testing/TEST_CATALOG.md` | Test structure |

## 🚨 KNOWN PITFALLS

1. **[PITFALL_NAME]**: [Description]
   - **Why it happens**: [Reason]
   - **How to avoid**: [Prevention strategy]

## ✅ VALIDATION CHECKLIST

Before proceeding to frontend testing, verify:

- [ ] Unit tests for VenueService CRUD operations
- [ ] Integration tests for all 6 API endpoints
- [ ] Soft delete behavior tested
- [ ] Name uniqueness validation tested
- [ ] Authorization tests (admin-only endpoints)
- [ ] Error handling tests
- [ ] Test coverage > 80%
- [ ] All tests passing
- [ ] TEST_CATALOG.md updated

## 🔄 DISCOVERED CONSTRAINTS

1. **Existing Test Infrastructure**: [Description]
   - **Impact**: [How this affects tests]
   - **Required Changes**: [Test adjustments]

## 📊 TEST SCENARIOS

[Unit test cases, integration test cases, edge cases]

## 🎯 SUCCESS CRITERIA

1. **Test**: [Description]
   - **Given**: [Initial state]
   - **When**: [Action]
   - **Then**: [Expected result]

## ⚠️ DO NOT TEST

- ❌ DO NOT skip authorization tests
- ❌ DO NOT skip soft delete validation
- ❌ DO NOT skip name uniqueness tests

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| [Term] | [Definition] | [Example] |

## 🔗 NEXT AGENT INSTRUCTIONS

**Next Agent**: Frontend Test Developer

1. **FIRST**: Read frontend implementation handoff
2. **SECOND**: Review UI design handoff for user workflows
3. **THIRD**: Review component structure
4. **THEN**: Begin frontend component test implementation

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: React Frontend Developer
**Previous Phase Completed**: [Date]
**Key Finding**: [One-sentence summary]

**Next Agent Should Be**: Frontend Test Developer
**Next Phase**: Frontend Component Tests
**Estimated Effort**: 2-3 hours

---

**NOTE**: This handoff will be completed by the Backend Test Developer after implementing tests.
