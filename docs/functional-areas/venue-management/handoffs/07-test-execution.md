# Test Execution Handoff Document

## Phase: Test Execution & Reporting
## Date: [TO BE COMPLETED]
## Feature: Venue Management

## 🎯 CRITICAL EXECUTION RULES (MUST FOLLOW)

1. **[RULE_NAME]**: [Clear, specific description]
   - ✅ Correct: [Example of correct execution]
   - ❌ Wrong: [Example of incorrect execution]

2. **[RULE_NAME]**: [Clear, specific description]
   - ✅ Correct: [Example]
   - ❌ Wrong: [Example]

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Backend Tests Handoff | `/docs/functional-areas/venue-management/handoffs/05-backend-tests.md` | Test scenarios |
| Frontend Tests Handoff | `/docs/functional-areas/venue-management/handoffs/06-frontend-tests.md` | Component tests |
| Test Catalog | `/docs/standards-processes/testing/TEST_CATALOG.md` | Execution patterns |

## 🚨 KNOWN PITFALLS

1. **[PITFALL_NAME]**: [Description]
   - **Why it happens**: [Reason]
   - **How to avoid**: [Prevention strategy]

## ✅ VALIDATION CHECKLIST

Before declaring feature complete, verify:

- [ ] All backend unit tests passing
- [ ] All integration tests passing
- [ ] All frontend component tests passing
- [ ] Test coverage report generated
- [ ] Test execution report created
- [ ] Known issues documented
- [ ] TEST_CATALOG.md updated
- [ ] Ready for deployment

## 🔄 DISCOVERED ISSUES

1. **Issue**: [Description]
   - **Impact**: [Severity and scope]
   - **Resolution**: [Fix or workaround]

## 📊 TEST RESULTS

[Test execution metrics, pass/fail counts, coverage percentages]

## 🎯 SUCCESS CRITERIA

1. **Metric**: [Description]
   - **Target**: [Expected value]
   - **Actual**: [Measured value]
   - **Status**: [Pass/Fail]

## ⚠️ DO NOT PROCEED IF

- ❌ Test coverage < 80%
- ❌ Critical tests failing
- ❌ Authorization tests not passing

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| [Term] | [Definition] | [Example] |

## 🔗 NEXT AGENT INSTRUCTIONS

**Next Agent**: Deployment Coordinator (or E2E Test Developer)

1. **FIRST**: Review test execution report
2. **SECOND**: Verify all acceptance criteria met
3. **THIRD**: Review deployment checklist
4. **THEN**: Begin deployment process (or E2E testing if scheduled)

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Frontend Test Developer
**Previous Phase Completed**: [Date]
**Key Finding**: [One-sentence summary of test results]

**Next Agent Should Be**: Deployment Coordinator
**Next Phase**: Production Deployment
**Estimated Effort**: 2-3 hours

---

**NOTE**: This handoff will be completed by the Test Executor after running all tests.
