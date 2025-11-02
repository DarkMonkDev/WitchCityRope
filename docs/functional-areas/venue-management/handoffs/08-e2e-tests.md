# E2E Tests Handoff Document (Future Phase)

## Phase: End-to-End Testing
## Date: [TO BE SCHEDULED]
## Feature: Venue Management

## 🎯 CRITICAL E2E RULES (MUST FOLLOW)

1. **Complete User Workflows**: Test entire workflows from login to completion
   - ✅ Correct: Test login → navigate to admin → create venue → verify in event form
   - ❌ Wrong: Test only venue creation in isolation

2. **Real Environment**: Use Docker containers with real database
   - ✅ Correct: Run against Docker API + PostgreSQL
   - ❌ Wrong: Mock all API calls in E2E tests

## 📍 KEY DOCUMENTS TO READ

| Document | Path | Critical Sections |
|----------|------|-------------------|
| Business Requirements | `/docs/functional-areas/venue-management/requirements/venue-management-requirements.md` | User stories, workflows |
| E2E Testing Guide | `/docs/standards-processes/testing/e2e-testing-guide.md` | Playwright patterns |
| Test Execution Handoff | `/docs/functional-areas/venue-management/handoffs/07-test-execution.md` | Known issues |

## 🚨 KNOWN PITFALLS

1. **Test Data Cleanup**: E2E tests create real database records
   - **Why it happens**: Tests don't clean up after themselves
   - **How to avoid**: Use unique test data names, implement cleanup in afterEach

2. **Race Conditions**: UI may not be ready when test expects
   - **Why it happens**: API calls complete at unpredictable times
   - **How to avoid**: Use Playwright's built-in waiting mechanisms

## ✅ E2E TEST SCENARIOS

### Admin Workflow Tests
- [ ] Admin can create new venue
- [ ] Admin can edit existing venue
- [ ] Admin can soft delete venue
- [ ] Admin can reactivate venue
- [ ] Non-admin users cannot access venue management

### Event Integration Tests
- [ ] New venue appears in event creation dropdown
- [ ] Event displays venue information correctly
- [ ] Inactive venues don't appear in dropdown
- [ ] Events preserve venue info after venue deactivation

### Validation Tests
- [ ] Cannot create venue with duplicate name
- [ ] Cannot create venue without name
- [ ] Form shows validation errors
- [ ] Success notifications display

## 🔄 TEST ENVIRONMENT SETUP

1. **Docker Environment**: Ensure containers running
2. **Test User**: Admin account with proper permissions
3. **Clean Database**: Fresh database or cleanup script
4. **Test Data**: Predictable test venue names

## 📊 E2E TEST STRUCTURE

```typescript
// Example E2E test structure
describe('Venue Management - Admin Workflows', () => {
  test('Admin can create new venue', async ({ page }) => {
    // 1. Login as admin
    await loginAsAdmin(page);

    // 2. Navigate to admin settings
    await page.goto('/admin/settings');

    // 3. Select "Create New Venue"
    await page.selectOption('[data-testid="venue-dropdown"]', 'create-new');

    // 4. Fill form
    await page.fill('[data-testid="venue-name"]', 'Test Venue');
    await page.fill('[data-testid="venue-directions"]', 'Test directions');

    // 5. Submit
    await page.click('[data-testid="save-venue"]');

    // 6. Verify success
    await expect(page.locator('[data-testid="success-notification"]'))
      .toContainText('Venue created');

    // 7. Verify in dropdown
    const dropdown = page.locator('[data-testid="venue-dropdown"]');
    await expect(dropdown).toContainText('Test Venue');
  });
});
```

## 🎯 SUCCESS CRITERIA

1. **Admin Workflows**: All 5 admin workflow tests passing
2. **Event Integration**: All 4 integration tests passing
3. **Validation**: All 4 validation tests passing
4. **Coverage**: 100% of user stories covered
5. **Reliability**: Tests pass 3 consecutive times

## ⚠️ DO NOT IMPLEMENT

- ❌ DO NOT create E2E tests until unit/integration tests pass
- ❌ DO NOT mock API calls in E2E tests
- ❌ DO NOT skip cleanup (leaves test data in database)

## 📝 TERMINOLOGY DICTIONARY

| Term | Definition | Example |
|------|------------|---------|
| E2E Test | End-to-end test simulating real user workflow | Login → create venue → verify |
| Playwright | E2E testing framework used in project | `test('scenario', async ({ page }) => {})` |
| Test Data | Data created specifically for testing | Venue named "E2E Test Venue 123" |

## 🔗 NEXT AGENT INSTRUCTIONS

**Next Agent**: E2E Test Developer

1. **FIRST**: Ensure all unit/integration tests passing
2. **SECOND**: Review user stories and acceptance criteria
3. **THIRD**: Set up Docker test environment
4. **FOURTH**: Review existing E2E test patterns
5. **THEN**: Implement E2E tests following scenarios above

## 🤝 HANDOFF CONFIRMATION

**Previous Agent**: Test Executor
**Previous Phase Completed**: [Date]
**Key Finding**: Unit and integration tests complete, ready for E2E validation

**Next Agent Should Be**: E2E Test Developer
**Next Phase**: E2E Test Implementation
**Estimated Effort**: 3-4 hours

---

**NOTE**: This E2E testing phase is optional for MVP but recommended before production deployment. It can be scheduled after initial deployment if time-constrained.

## When to Schedule E2E Testing

### Immediate (Before MVP)
- If venue management is critical path for launch
- If no manual testing capacity available
- If deployment is fully automated

### After Initial Deployment
- If manual testing can validate workflows
- If feature is non-critical enhancement
- If time-to-production is priority

### Never Skip
- Authorization/security workflows
- Data integrity scenarios
- Multi-step workflows with complex state
