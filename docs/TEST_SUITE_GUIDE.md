# WitchCityRope Test Suite Guide

## 🚀 Quick Start - Running All Tests

```bash
# 1. Prerequisites
sudo systemctl start docker  # Required for integration tests
cd /home/chad/repos/witchcityrope/WitchCityRope

# 2. Build everything first
dotnet build

# 3. Run all tests (will take several minutes)
dotnet test

# 4. Run E2E tests separately (requires app running)
./dev.sh  # Start the application
# In another terminal:
cd tests/playwright && npm test
```

## 📊 Current Test Health Dashboard

| Test Category | Health | Action Required | Effort |
|---------------|---------|-----------------|---------|
| Core Domain | ✅ 99.5% | None | - |
| API Tests | ✅ 95% | Fix concurrency | Low |
| Integration | 🟡 86% | Fix navigation | Medium |
| Infrastructure | 🟡 59% | Review failures | Medium |
| Web Tests | 🔴 Broken | Major refactor | High |
| E2E Tests | 🟡 83% | Stabilize | Medium |

## 🔥 Priority 1: Fix Web Test Compilation (High Impact)

The Web test project has 318+ compilation errors. Here's how to fix:

### Step 1: Assess the Damage
```bash
# See all compilation errors
dotnet build tests/WitchCityRope.Web.Tests/ 2>&1 | grep "error CS" | wc -l

# Common errors to fix:
# - Missing service registrations
# - Interface changes (IAuthService, IJwtService)
# - Namespace changes
```

### Step 2: Consolidate Test Projects
We have THREE Web test projects! Consolidate them:
```
tests/WitchCityRope.Web.Tests/        # 318+ errors - OLD
tests/WitchCityRope.Web.Tests.New/    # Partially working - NEW
tests/WitchCityRope.Web.Tests.Login/  # Login-specific - MERGE
```

**Recommendation**: Keep `.New`, merge `.Login`, delete old `.Web.Tests`

## 🔧 Priority 2: Fix Integration Test Routes (Medium Impact)

### Current Failures
```bash
# See failing navigation tests
dotnet test tests/WitchCityRope.IntegrationTests/ --filter "NavigateToPage" --logger "console;verbosity=normal"
```

### Missing Routes to Create
- `/admin/users` - Admin user management page
- `/admin/incidents` - Incident reporting page  
- `/member/profile` - Member profile page
- `/member/events` - Member events page

### Quick Fix Template
```csharp
// Create in /src/WitchCityRope.Web/Features/Admin/Pages/Users.razor
@page "/admin/users"
@attribute [Authorize(Roles = "Admin")]
@rendermode @(new InteractiveServerRenderMode())

<h3>User Management</h3>
<p>This page is under construction.</p>
```

## 🔄 Priority 3: Fix API Test Concurrency (Low Impact)

### The Problem
6 tests fail when database operations happen simultaneously.

### Quick Fix
Add to failing test classes:
```csharp
[Collection("Sequential")]  // Forces sequential execution
public class ProblematicTestClass
{
    // ... tests
}
```

### Proper Fix
Use separate databases per test:
```csharp
var dbName = $"TestDb_{Guid.NewGuid()}";
```

## 📝 Test Organization Structure

```
tests/
├── Unit Tests (Fast, No Dependencies)
│   ├── WitchCityRope.Core.Tests/          ✅ Working
│   └── WitchCityRope.Api.Tests/           ✅ Working
│
├── Integration Tests (Database Required)
│   ├── WitchCityRope.IntegrationTests/    🟡 86% passing
│   └── WitchCityRope.Infrastructure.Tests/ 🟡 59% passing
│
├── UI Tests (Component Testing)
│   ├── WitchCityRope.Web.Tests/          🔴 Needs fixing
│   ├── WitchCityRope.Web.Tests.New/      ✅ Use this one
│   └── WitchCityRope.Web.Tests.Login/    🔴 Merge into .New
│
└── E2E Tests (Full Stack)
    └── playwright/                         🟡 83% passing
```

## 🐛 Common Test Failures and Solutions

### 1. "Service not registered" Errors
```csharp
// Add to test setup:
services.AddScoped<IMissingService, MockImplementation>();
```

### 2. "Headers are read-only" in Blazor Tests
```csharp
// Don't use SignInManager in Blazor components
// Use API endpoints for auth operations
```

### 3. "Database already exists" Errors
```csharp
// Use unique database names
var dbName = $"Test_{testName}_{Guid.NewGuid():N}";
```

### 4. "Navigation failed" in Integration Tests
```csharp
// Either create the missing page or update the test
// Check: Does this route actually exist in the app?
```

## 🎯 Success Metrics

Track your progress:

1. **Compilation**: All projects build with 0 errors
2. **Unit Tests**: >95% pass rate
3. **Integration Tests**: >90% pass rate  
4. **E2E Tests**: >80% pass rate
5. **Test Speed**: Full suite runs in <5 minutes

## 🛠️ Helpful Testing Commands

```bash
# Run only failing tests
dotnet test --filter "FullyQualifiedName~FailingTestName"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run in parallel (faster)
dotnet test --parallel

# Generate code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/WitchCityRope.Core.Tests/

# Debug a specific test
dotnet test --filter "TestMethodName" --logger "console;verbosity=detailed" --no-build
```

## 📚 Key Documentation

- **Architecture**: `/ARCHITECTURE.md` - Understand Web→API→Database pattern
- **Auth Guide**: `/docs/JWT_TEST_MIGRATION_GUIDE.md` - Cookie vs JWT auth
- **Docker Guide**: `/DOCKER_DEV_GUIDE.md` - Container setup for tests
- **E2E Guide**: `/tests/playwright/README.md` - Playwright test patterns

## ⚡ Quick Wins (Do These First!)

1. **Delete old test project**: Remove `/tests/WitchCityRope.Web.Tests/` if unfixable
2. **Add missing pages**: Create stub pages for failing navigation tests
3. **Fix service registrations**: Add missing services to test setup
4. **Isolate flaky tests**: Use `[Collection("Sequential")]` attribute
5. **Update namespaces**: Fix UserWithAuth references

## 🎉 Definition of Done

The test suite is "done" when:
- [ ] All projects compile with 0 errors
- [ ] Core tests: 100% pass
- [ ] API tests: >95% pass  
- [ ] Integration tests: >90% pass
- [ ] Web tests: Consolidated and >80% pass
- [ ] E2E tests: >80% pass and stable
- [ ] CI/CD pipeline: All tests run automatically

Good luck! The foundation is solid - these are mostly organizational issues rather than fundamental problems.