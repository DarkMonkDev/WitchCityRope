# Complete Vetting Workflow Tests - Quick Start Guide

<!-- Created: 2025-10-04 -->
<!-- Purpose: Fast-track test implementation guide -->

## For Test Developers: Getting Started

This guide provides quick commands and file paths to implement the complete vetting workflow test suite.

---

## Prerequisites

Before starting ANY test implementation:

```bash
# 1. Read the full test plan
cat /home/chad/repos/witchcityrope/docs/functional-areas/vetting-system/new-work/2025-10-04-complete-vetting-workflow/testing/test-plan.md

# 2. Verify Docker environment (MANDATORY for E2E tests)
docker ps | grep witchcity
./scripts/kill-local-dev-servers.sh
curl -f http://localhost:5173/ | grep "Witch City Rope"

# 3. Check test catalog for existing tests
cat /home/chad/repos/witchcityrope/docs/standards-processes/testing/TEST_CATALOG.md
```

---

## Phase 1: Unit Tests (Start Here)

### Step 1: Create VettingAccessControlServiceTests

**File**: `/tests/WitchCityRope.Api.Tests/Features/Vetting/Services/VettingAccessControlServiceTests.cs`

**Template**:
```csharp
using Xunit;
using Moq;
using FluentAssertions;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace WitchCityRope.Api.Tests.Features.Vetting.Services;

public class VettingAccessControlServiceTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger<VettingAccessControlService>> _mockLogger;
    private readonly VettingAccessControlService _sut;

    public VettingAccessControlServiceTests()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<VettingAccessControlService>>();
        _sut = new VettingAccessControlService(_mockContext.Object, _mockLogger.Object, _mockCache.Object);
    }

    [Fact]
    public async Task CanUserRsvpAsync_WithNoApplication_ReturnsAllowed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Mock: No vetting application in database
        _mockContext.Setup(x => x.VettingApplications)
            .Returns(MockDbSet<VettingApplication>(new List<VettingApplication>()));

        // Act
        var result = await _sut.CanUserRsvpAsync(userId, eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsAllowed.Should().BeTrue();
        result.Value.DenialReason.Should().BeNull();
    }

    // Add 22 more tests from test plan...
}
```

**Tests to Implement**: 23 tests
- See test plan Section "VettingAccessControlService Tests" for complete list

**Run Tests**:
```bash
dotnet test tests/WitchCityRope.Api.Tests/Features/Vetting/Services/VettingAccessControlServiceTests.cs
```

---

### Step 2: Create VettingEmailServiceTests

**File**: `/tests/WitchCityRope.Api.Tests/Features/Vetting/Services/VettingEmailServiceTests.cs`

**Template**:
```csharp
using Xunit;
using Moq;
using FluentAssertions;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WitchCityRope.Api.Tests.Features.Vetting.Services;

public class VettingEmailServiceTests
{
    private readonly Mock<ApplicationDbContext> _mockContext;
    private readonly Mock<ILogger<VettingEmailService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly VettingEmailService _sut;

    public VettingEmailServiceTests()
    {
        _mockContext = new Mock<ApplicationDbContext>();
        _mockLogger = new Mock<ILogger<VettingEmailService>>();
        _mockConfig = SetupMockConfiguration(emailEnabled: false); // Mock mode
        _sut = new VettingEmailService(_mockContext.Object, _mockLogger.Object, _mockConfig.Object);
    }

    private Mock<IConfiguration> SetupMockConfiguration(bool emailEnabled, string? apiKey = null)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Vetting:EmailEnabled"]).Returns(emailEnabled.ToString());
        config.Setup(c => c["Vetting:SendGridApiKey"]).Returns(apiKey ?? "");
        config.Setup(c => c["Vetting:FromEmail"]).Returns("noreply@witchcityrope.com");
        config.Setup(c => c["Vetting:FromName"]).Returns("WitchCityRope");
        return config;
    }

    [Fact]
    public async Task SendApplicationConfirmationAsync_InMockMode_LogsEmailContent()
    {
        // Arrange
        var application = new VettingApplicationTestBuilder().Build();
        var email = "test@example.com";
        var name = "Test User";

        // Act
        var result = await _sut.SendApplicationConfirmationAsync(application, email, name);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("MOCK EMAIL")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Add 19 more tests from test plan...
}
```

**Tests to Implement**: 20 tests
- See test plan Section "VettingEmailService Tests" for complete list

**Run Tests**:
```bash
dotnet test tests/WitchCityRope.Api.Tests/Features/Vetting/Services/VettingEmailServiceTests.cs
```

---

### Step 3: Create VettingServiceStatusChangeTests

**File**: `/tests/WitchCityRope.Api.Tests/Features/Vetting/Services/VettingServiceTests.cs`

**Tests to Implement**: 25 tests
- See test plan Section "VettingService Status Change Tests" for complete list

**Run Tests**:
```bash
dotnet test tests/WitchCityRope.Api.Tests/Features/Vetting/Services/VettingServiceTests.cs
```

---

### Verify Phase 1 Complete

```bash
# Run all unit tests
dotnet test --filter "Category=Unit"

# Check coverage (target: 80%)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
reportgenerator -reports:"**/coverage.opencover.xml" -targetdir:"coverage"
open coverage/index.html
```

**Success Criteria**:
- All 68 unit tests passing
- 80% code coverage achieved
- Tests execute in <10 seconds

---

## Phase 2: Integration Tests

### Step 1: Create ParticipationEndpointsTests

**File**: `/tests/WitchCityRope.IntegrationTests/Features/Participation/ParticipationEndpointsTests.cs`

**Base Class**:
```csharp
public class VettingIntegrationTestBase : IAsyncLifetime
{
    protected readonly PostgreSqlContainer _postgresContainer;
    protected ApplicationDbContext _dbContext;
    protected HttpClient _client;

    public VettingIntegrationTestBase()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithDatabase("vetting_test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        var connectionString = _postgresContainer.GetConnectionString();
        // Setup DbContext and HttpClient...
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
```

**Tests to Implement**: 10 tests
- RSVP access control (5 tests)
- Ticket purchase access control (5 tests)

**Run Tests**:
```bash
# IMPORTANT: Run health checks first
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"

# Then run integration tests
dotnet test tests/WitchCityRope.IntegrationTests/Features/Participation/ParticipationEndpointsTests.cs
```

---

### Step 2: Create VettingEndpointsTests

**File**: `/tests/WitchCityRope.IntegrationTests/Features/Vetting/VettingEndpointsTests.cs`

**Tests to Implement**: 15 tests
- Status update endpoints (7 tests)
- Email integration (3 tests)
- Audit logging (2 tests)
- Transaction rollback (3 tests)

**Run Tests**:
```bash
dotnet test tests/WitchCityRope.IntegrationTests/Features/Vetting/VettingEndpointsTests.cs
```

---

### Verify Phase 2 Complete

```bash
# Run all integration tests
dotnet test --filter "Category=Integration"
```

**Success Criteria**:
- All 25 integration tests passing
- All API endpoints tested
- Tests execute in <2 minutes

---

## Phase 3: E2E Tests

### MANDATORY Pre-Flight Checklist

**BEFORE creating ANY E2E test**:

```bash
# 1. Verify Docker containers running (CRITICAL)
docker ps --format "table {{.Names}}\t{{.Ports}}" | grep witchcity
# MUST show: witchcity-web on port 5173

# 2. Kill any rogue local dev servers
./scripts/kill-local-dev-servers.sh

# 3. Verify correct port
curl -f http://localhost:5173/ | grep "Witch City Rope"

# 4. Check for port conflicts
lsof -i :5174 -i :5175 -i :3000 | grep node && echo "❌ CONFLICT" || echo "✅ Clear"
```

---

### Step 1: Create Admin Vetting Workflow Tests

**File**: `/apps/web/tests/e2e/admin/vetting-workflow.spec.ts`

**Template**:
```typescript
import { test, expect } from '@playwright/test';

test.describe('Admin Vetting Workflow', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to Docker port ONLY
    await page.goto('http://localhost:5173');
  });

  test('Admin can login and navigate to vetting grid', async ({ page }) => {
    // Arrange
    const adminEmail = 'admin@witchcityrope.com';
    const adminPassword = 'Test123!'; // NO escaping

    // Act
    await page.click('[data-testid="login-link"]');
    await page.fill('[data-testid="email-input"]', adminEmail);
    await page.fill('[data-testid="password-input"]', adminPassword);
    await page.click('[data-testid="login-button"]');

    await page.goto('http://localhost:5173/admin/vetting');

    // Assert
    await expect(page).toHaveURL(/\/admin\/vetting/);
    await expect(page.locator('h1')).toContainText('Vetting Applications');

    // Screenshot on success
    await page.screenshot({ path: 'test-results/vetting-grid.png' });
  });

  // Add 13 more tests from test plan...
});
```

**Tests to Implement**: 14 tests
- See test plan Section "Admin Vetting Workflow E2E Tests"

**Run Tests**:
```bash
cd apps/web
npm run test:e2e -- tests/e2e/admin/vetting-workflow.spec.ts
```

---

### Step 2: Create Access Control E2E Tests

**File**: `/apps/web/tests/e2e/access-control/vetting-restrictions.spec.ts`

**Tests to Implement**: 4 tests
- RSVP blocking (2 tests)
- Ticket purchase blocking (2 tests)

**Run Tests**:
```bash
cd apps/web
npm run test:e2e -- tests/e2e/access-control/vetting-restrictions.spec.ts
```

---

### Verify Phase 3 Complete

```bash
# Run all E2E tests
cd apps/web
npm run test:e2e
```

**Success Criteria**:
- All 18 E2E tests passing
- All critical workflows validated
- Tests execute in <3 minutes
- Screenshots captured on failures

---

## Phase 4: CI/CD Integration

### Create GitHub Actions Workflow

**File**: `.github/workflows/vetting-tests.yml`

**Template**: See test plan Section "CI/CD Integration"

**Test CI/CD**:
```bash
# Push to branch
git add .
git commit -m "Add vetting workflow tests"
git push

# Verify GitHub Actions run successfully
gh workflow view vetting-tests
```

---

## Phase 5: Documentation

### Update TEST_CATALOG.md

Already done! Entry added at:
`/docs/standards-processes/testing/TEST_CATALOG.md`

### Add Lessons Learned

If you discover new patterns during implementation:

**File**: `/docs/lessons-learned/test-developer-lessons-learned-2.md`

---

## Troubleshooting

### Unit Tests Failing

**Problem**: Mock setup incorrect
**Solution**: Check test plan for exact mock configuration

### Integration Tests Failing

**Problem**: TestContainers not starting
**Solution**:
```bash
docker ps | grep postgres
# If no containers, Docker may not be running
sudo systemctl start docker
```

### E2E Tests Failing

**Problem**: Port conflicts or wrong port
**Solution**:
```bash
./scripts/kill-local-dev-servers.sh
docker ps | grep witchcity-web
# Should show port 5173, NOT 5174 or 5175
```

---

## Quick Commands Reference

```bash
# Phase 1: Unit Tests
dotnet test --filter "Category=Unit"

# Phase 2: Integration Tests (with health check)
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "Category=HealthCheck"
dotnet test --filter "Category=Integration"

# Phase 3: E2E Tests (Docker MUST be running)
./dev.sh  # Start Docker if not running
cd apps/web && npm run test:e2e

# Full Test Suite
npm run test:all

# Coverage Report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
reportgenerator -reports:"**/coverage.opencover.xml" -targetdir:"coverage"
open coverage/index.html
```

---

## Success Checklist

**Phase 1 Complete**:
- [ ] VettingAccessControlServiceTests.cs created (23 tests)
- [ ] VettingEmailServiceTests.cs created (20 tests)
- [ ] VettingServiceTests.cs created (25 tests)
- [ ] All 68 unit tests passing
- [ ] 80% code coverage achieved
- [ ] Tests execute in <10 seconds

**Phase 2 Complete**:
- [ ] ParticipationEndpointsTests.cs created (10 tests)
- [ ] VettingEndpointsTests.cs created (15 tests)
- [ ] All 25 integration tests passing
- [ ] Tests execute in <2 minutes

**Phase 3 Complete**:
- [ ] vetting-workflow.spec.ts created (14 tests)
- [ ] vetting-restrictions.spec.ts created (4 tests)
- [ ] All 18 E2E tests passing
- [ ] Tests execute in <3 minutes
- [ ] Docker-only environment verified

**Phase 4 Complete**:
- [ ] GitHub Actions workflow created
- [ ] All tests pass in CI/CD

**Phase 5 Complete**:
- [ ] TEST_CATALOG.md updated (done)
- [ ] Lessons learned documented
- [ ] Handoff document created

---

## Total Progress Tracking

**Tests Implemented**: 0 / 93
**Estimated Time Remaining**: 12-16 hours
**Current Phase**: Phase 1 - Unit Tests

---

## Help & Resources

**Full Test Plan**: `/docs/functional-areas/vetting-system/new-work/2025-10-04-complete-vetting-workflow/testing/test-plan.md`

**Testing Standards**:
- Docker-Only: `/docs/standards-processes/testing/docker-only-testing-standard.md`
- Playwright: `/docs/standards-processes/testing/browser-automation/playwright-guide.md`
- Testing Guide: `/docs/standards-processes/testing/TESTING_GUIDE.md`

**Lessons Learned**:
- Part 1: `/docs/lessons-learned/test-developer-lessons-learned.md`
- Part 2: `/docs/lessons-learned/test-developer-lessons-learned-2.md`

**Test Catalog**: `/docs/standards-processes/testing/TEST_CATALOG.md`

---

**Ready to Start**: Begin with Phase 1, Step 1 - VettingAccessControlServiceTests!
