# Current Test Suite Status
<!-- Last Updated: 2026-03-07 -->
<!-- Version: 4.0 -->
<!-- Owner: Testing Team -->
<!-- Status: Operational -->

## Overall Test Health

### Latest Baseline: March 7, 2026

| Suite | Passed | Failed | Skipped | Total | Pass Rate |
|-------|--------|--------|---------|-------|-----------|
| Unit (.NET) | 1,013 | 0 | 11 | 1,024 | 100% |
| Core (.NET) | 114 | 0 | 18 | 132 | 100% |
| Integration (.NET) | 200 | 0 | 11 | 211 | 100% |
| E2E (Playwright) | 460 | 0 | 20 | 480 | 100% |
| **TOTAL** | **1,787** | **0** | **60** | **1,847** | **100%** |

### Test Infrastructure

- **Unit tests**: xUnit + Moq/NSubstitute, TestContainers PostgreSQL (migrated from InMemoryDatabase)
- **Core tests**: xUnit + TestContainers PostgreSQL with DatabaseTestFixture
- **Integration tests**: xUnit + TestContainers PostgreSQL + WebApplicationFactory + Respawn
- **E2E tests**: Playwright (Chromium) against Docker dev containers

## Test Execution

### Quick Commands

Use the `test-environment` skill for running tests in isolated containers, or run directly:

- **Unit tests**: `dotnet test tests/unit/api/WitchCityRope.Api.Tests.csproj --verbosity minimal`
- **Core tests**: `dotnet test tests/WitchCityRope.Core.Tests/ --verbosity minimal`
- **Integration tests**: `dotnet test tests/integration/ --verbosity minimal`
- **E2E tests**: Use `test-environment` skill (handles Docker containers and execution)

### Test Result Files

All test artifacts in `/test-results/`:
- `test-results.json` - Full Playwright JSON report
- `html-report/` - Interactive HTML report (`npx playwright show-report test-results/html-report`)
- `artifacts/` - Screenshots, traces, videos from Playwright

## Skipped Tests (60 total)

Skipped tests are intentionally skipped with documented reasons, not broken tests.

Common skip reasons:
- `NoOpAntiforgery` makes CSRF rejection untestable in test factory
- Database transaction rollback test requires specific error injection
- Cancellation token handling tests with timing sensitivity
- Tests requiring external service configuration

## Historical Progress

| Date | Unit | Core | Integration | E2E | Total Pass | Notes |
|------|------|------|-------------|-----|------------|-------|
| 2026-03-07 | 1,013 | 114 | 200 | 460 | 1,787 | Full suite repair, 0 failures |
| 2025-12-11 | ~1,000 | ~110 | ~150 | 617 | ~1,877 | E2E at 78.1%, 146 E2E failures |
| 2025-11-29 | - | - | - | 621 | - | E2E baseline |
| 2025-10-09 | - | - | - | 18 | - | Missing data-testid attributes |

## Major Fixes Applied (March 2026)

### Code Bugs Found by Tests
- VettedMembersOnly not enforced for ticket purchases (AttendanceService)
- Admin removal endpoint not updating TicketPurchase.PaymentStatus after refund

### Test Infrastructure Fixes
- MockEncryptionService registered in test factory
- Shared WebApplicationFactory for high-test-count classes (prevents resource exhaustion)
- InMemoryDatabase tests converted to TestContainers PostgreSQL
- Venue FK added to all test Event entities
- USE_MOCK_PAYMENT_SERVICE enabled in test factory
- JsonStringEnumConverter added for DTO deserialization

## Build Status

- **Solution Build**: Successful (warnings only, 0 errors)
- **Docker Containers**: Operational (dev and test isolated)
- **All Test Suites**: 100% pass rate

---

*This status is updated with each baseline test run. For integration test patterns, see `integration-test-patterns.md`.*
