# Archived Tests Inventory
**Date**: November 24, 2025
**Archive Reason**: Test reorganization - Phase 1 cleanup of obsolete/disabled tests
**Total Files**: 34 files + 2 directories
**Total Lines of Code**: 8,172 lines

## Summary

This archive contains all `.disabled` test files and directories that were scattered throughout the project. These tests were disabled at various points during development for different reasons (replaced by better tests, obsolete functionality, duplicates, etc.).

## Categorized Inventory

### Category 1: React/Web Test Pages (RECOMMEND: DELETE)
**Reason**: Development/debugging pages, not actual tests

| File | Lines | Original Path | Recommendation |
|------|-------|---------------|----------------|
| ApiValidation.tsx.disabled | 878 | /apps/web/src/pages/ | DELETE - Debug page |
| ApiValidationV2.tsx.disabled | 1,043 | /apps/web/src/pages/ | DELETE - Debug page |

**Subtotal**: 2 files, 1,921 lines

---

### Category 2: React Unit Tests (RECOMMEND: REVIEW THEN DELETE)
**Reason**: Disabled React tests - likely replaced or obsolete

| File | Lines | Original Path | Recommendation |
|------|-------|---------------|----------------|
| auth-flow.test.tsx.disabled | 397 | /apps/web/src/test/integration/ | REVIEW - Auth tests may have value |
| useEvent.test.tsx.disabled | 75 | /apps/web/src/test/hooks/ | REVIEW - Hook tests may have value |

**Subtotal**: 2 files, 472 lines

---

### Category 3: Playwright E2E Duplicate Tests (RECOMMEND: DELETE)
**Reason**: Explicitly marked as duplicates in archived folder

| File | Lines | Original Path | Recommendation |
|------|-------|---------------|----------------|
| events-actual-routes-test.spec.ts.disabled | 277 | /tests/playwright/_archived/duplicate-tests/ | DELETE - Duplicate |
| events-crud-test.spec.ts.disabled | 123 | /tests/playwright/_archived/duplicate-tests/ | DELETE - Duplicate |
| navigation-updates-test.spec.ts.disabled | 197 | /tests/playwright/_archived/duplicate-tests/ | DELETE - Duplicate |
| test-direct-navigation.spec.ts.disabled | 22 | /tests/playwright/_archived/duplicate-tests/ | DELETE - Duplicate |
| test-events-navigation.spec.ts.disabled | 29 | /tests/playwright/_archived/duplicate-tests/ | DELETE - Duplicate |
| verify-policies-field-display.spec.ts.disabled | 135 | /tests/playwright/_archived/duplicate-tests/ | DELETE - Duplicate |
| verify-policies-field-fix.spec.ts.disabled | 215 | /tests/playwright/_archived/duplicate-tests/ | DELETE - Duplicate |

**Subtotal**: 7 files, 998 lines

---

### Category 4: .NET Integration Tests (RECOMMEND: REVIEW)
**Reason**: Disabled during migration - may have historical value

| File | Lines | Original Path | Recommendation |
|------|-------|---------------|----------------|
| DatabaseInitializationIntegrationTests.cs.disabled | 508 | /tests/integration/ | REVIEW - Database init tests |
| EventSessionMatrixIntegrationTests.cs.disabled | 598 | /tests/integration/events/ | REVIEW - Complex event logic tests |

**Subtotal**: 2 files, 1,106 lines

---

### Category 5: Test Common - Builders (RECOMMEND: REVIEW THEN DELETE)
**Reason**: Test data builders - possibly replaced by newer patterns

| File | Lines | Original Path | Recommendation |
|------|-------|---------------|----------------|
| EventBuilder.cs.disabled | 157 | /tests/WitchCityRope.Tests.Common/Builders/ | REVIEW - May have reusable patterns |
| EventSessionBuilder.cs.disabled | 102 | /tests/WitchCityRope.Tests.Common/Builders/ | REVIEW - Session builder patterns |
| EventWithSessionsBuilder.cs.disabled | 236 | /tests/WitchCityRope.Tests.Common/Builders/ | REVIEW - Complex builder patterns |
| PaymentBuilder.cs.disabled | 105 | /tests/WitchCityRope.Tests.Common/Builders/ | REVIEW - Payment test patterns |
| RegistrationBuilder.cs.disabled | 96 | /tests/WitchCityRope.Tests.Common/Builders/ | REVIEW - Registration patterns |
| TicketTypeBuilder.cs.disabled | 191 | /tests/WitchCityRope.Tests.Common/Builders/ | REVIEW - Ticket patterns |
| UserBuilder.cs.disabled | 119 | /tests/WitchCityRope.Tests.Common/Builders/ | REVIEW - User test patterns |

**Subtotal**: 7 files, 1,006 lines

---

### Category 6: Test Common - Infrastructure (RECOMMEND: REVIEW)
**Reason**: Test infrastructure - may have reusable patterns

| File | Lines | Original Path | Recommendation |
|------|-------|---------------|----------------|
| TestConfiguration.cs.disabled | 152 | /tests/WitchCityRope.Tests.Common/Configuration/ | REVIEW - Config patterns |
| TestDataSeeder.cs.disabled | 137 | /tests/WitchCityRope.Tests.Common/Database/ | REVIEW - Seed data patterns |
| DatabaseTestBase.cs.disabled | 79 | /tests/WitchCityRope.Tests.Common/Fixtures/ | REVIEW - Base class patterns |
| PostgreSqlIntegrationFixture.cs.disabled | 101 | /tests/WitchCityRope.Tests.Common/Fixtures/ | REVIEW - DB fixture patterns |
| ServiceTestBase.cs.disabled | 108 | /tests/WitchCityRope.Tests.Common/Fixtures/ | REVIEW - Service test patterns |
| TestInterfaces.cs.disabled | 186 | /tests/WitchCityRope.Tests.Common/Interfaces/ | REVIEW - Interface patterns |

**Subtotal**: 6 files, 763 lines

---

### Category 7: Test Common - Test Doubles (RECOMMEND: REVIEW)
**Reason**: Mock/stub implementations - possibly replaced by newer mocks

| File | Lines | Original Path | Recommendation |
|------|-------|---------------|----------------|
| InMemoryUserRepository.cs.disabled | 189 | /tests/WitchCityRope.Tests.Common/TestDoubles/ | REVIEW - Mock repository |
| TestEmailService.cs.disabled | 92 | /tests/WitchCityRope.Tests.Common/TestDoubles/ | REVIEW - Email mock |
| TestEncryptionService.cs.disabled | 98 | /tests/WitchCityRope.Tests.Common/TestDoubles/ | REVIEW - Encryption mock |
| TestJwtService.cs.disabled | 126 | /tests/WitchCityRope.Tests.Common/TestDoubles/ | REVIEW - JWT mock |
| TestPaymentService.cs.disabled | 133 | /tests/WitchCityRope.Tests.Common/TestDoubles/ | REVIEW - Payment mock |

**Subtotal**: 5 files, 638 lines

---

### Category 8: Core Tests - Entity Tests (RECOMMEND: REVIEW)
**Reason**: Domain entity tests - may contain valuable business logic tests

| Directory/File | Lines | Original Path | Recommendation |
|----------------|-------|---------------|----------------|
| **Entities.disabled/** (directory) | - | /tests/WitchCityRope.Core.Tests/ | REVIEW DIRECTORY |
| └─ EventTests.cs | 444 | /tests/WitchCityRope.Core.Tests/Entities.disabled/ | REVIEW - Event entity tests |
| └─ RegistrationTests.cs | 505 | /tests/WitchCityRope.Core.Tests/Entities.disabled/ | REVIEW - Registration tests |
| └─ UserTests.cs | 319 | /tests/WitchCityRope.Core.Tests/Entities.disabled/ | REVIEW - User entity tests |

**Subtotal**: 1 directory, 3 files, 1,268 lines

---

### Category 9: Core Tests - Value Object Tests (NOT FOUND IN ARCHIVE)
**Reason**: Value object tests directory

| Directory/File | Lines | Original Path | Recommendation |
|----------------|-------|---------------|----------------|
| **ValueObjects.disabled/** (directory) | - | /tests/WitchCityRope.Core.Tests/ | REVIEW DIRECTORY |
| └─ EmailAddressTests.cs | ~200 | /tests/WitchCityRope.Core.Tests/ValueObjects.disabled/ | REVIEW - Email validation |
| └─ MoneyTests.cs | ~300 | /tests/WitchCityRope.Core.Tests/ValueObjects.disabled/ | REVIEW - Money calculations |
| └─ SceneNameTests.cs | ~200 | /tests/WitchCityRope.Core.Tests/ValueObjects.disabled/ | REVIEW - Scene name validation |

**Note**: ValueObjects.disabled directory files not included in line count above.

---

## Recommendations Summary

### Immediate Deletion Candidates (1,921 lines)
- 2 debug pages (ApiValidation files)

### Probable Deletion After Brief Review (998 lines)
- 7 duplicate E2E tests (explicitly marked as duplicates)

### Requires Careful Review (4,781 lines)
- 2 React unit tests (auth and hooks)
- 2 .NET integration tests (database and event matrix)
- 7 test builders (may have reusable patterns)
- 6 test infrastructure files (base classes, fixtures)
- 5 test doubles (mock services)
- 3 entity test files (Event, Registration, User)
- 3 value object test files (EmailAddress, Money, SceneName)

### Estimated Total Cleanup Potential
- **Safe to delete now**: 1,921 lines (debug pages)
- **Probably safe to delete**: 998 lines (duplicates)
- **Requires review**: 4,781 lines (potentially valuable test patterns)
- **Total archived**: 8,172 lines

## Next Steps

1. **Phase 1 (Now)**: Archive completed ✅
2. **Phase 2 (After test reorganization)**: Review "REVIEW" items for salvageable patterns
3. **Phase 3 (After salvage)**: Delete files marked "DELETE"
4. **Phase 4 (6 months)**: If no one has looked at reviewed files, consider bulk deletion

## Historical Context

These tests were disabled during:
- React migration from Blazor (August-September 2025)
- Database initialization refactoring
- Test infrastructure modernization
- Duplicate test cleanup efforts

Many represent earlier testing approaches that were replaced by better patterns in the current test suite.
