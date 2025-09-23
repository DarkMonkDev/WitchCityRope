# Vetting System Tests - Execution Guide

**Date**: September 22, 2025
**Purpose**: Quick reference for running the comprehensive vetting system test suite

## 🚨 Prerequisites

**MANDATORY**: Ensure Docker environment is running on correct ports:

```bash
# Verify Docker containers
docker ps --format "table {{.Names}}\t{{.Ports}}" | grep witchcity

# Expected output:
# witchcity-web: 0.0.0.0:5173->5173/tcp
# witchcity-api: 0.0.0.0:5655->8080/tcp
# witchcity-postgres: 0.0.0.0:5433->5432/tcp

# If containers not running:
./dev.sh
```

## 🧪 Test Execution Commands

### React Component Unit Tests

```bash
# Navigate to web app directory
cd apps/web

# Run all vetting component tests
npm test -- --run tests/unit/web/features/admin/vetting/

# Run specific component tests
npm test -- --run tests/unit/web/features/admin/vetting/components/VettingApplicationsList.test.tsx
npm test -- --run tests/unit/web/features/admin/vetting/components/VettingApplicationDetail.test.tsx
npm test -- --run tests/unit/web/features/admin/vetting/components/OnHoldModal.test.tsx
npm test -- --run tests/unit/web/features/admin/vetting/components/SendReminderModal.test.tsx

# Run with coverage
npm test -- --run --coverage tests/unit/web/features/admin/vetting/
```

### API Service Unit Tests

```bash
# Run TypeScript API service tests
cd apps/web
npm test -- --run tests/unit/web/features/admin/vetting/services/vettingAdminApi.test.ts
```

### Backend Integration Tests

```bash
# Navigate to test directory
cd tests/unit/api

# Run vetting service integration tests
dotnet test Features/Vetting/VettingServiceTests.cs --logger console

# Run vetting endpoints tests
dotnet test Features/Vetting/VettingEndpointsTests.cs --logger console

# Run both with verbose output
dotnet test Features/Vetting/ --logger console --verbosity normal

# Run with coverage (if configured)
dotnet test Features/Vetting/ --collect:"XPlat Code Coverage"
```

### E2E Workflow Tests

```bash
# From project root
npm run test:e2e:playwright -- tests/e2e/vetting-system-complete-workflows.spec.ts

# Run in headed mode for debugging
npm run test:e2e:playwright -- tests/e2e/vetting-system-complete-workflows.spec.ts --headed

# Run specific test cases
npm run test:e2e:playwright -- tests/e2e/vetting-system-complete-workflows.spec.ts --grep "Put on Hold"
npm run test:e2e:playwright -- tests/e2e/vetting-system-complete-workflows.spec.ts --grep "Send Reminder"
```

## 🏃‍♂️ Quick Test Run (All Vetting Tests)

```bash
# 1. Verify Docker environment
docker ps | grep witchcity && echo "✅ Docker ready" || echo "❌ Start Docker first"

# 2. Run React unit tests
cd apps/web && npm test -- --run tests/unit/web/features/admin/vetting/ && cd ../..

# 3. Run backend tests
cd tests/unit/api && dotnet test Features/Vetting/ && cd ../../..

# 4. Run E2E tests
npm run test:e2e:playwright -- tests/e2e/vetting-system-complete-workflows.spec.ts
```

## 📊 Expected Test Results

### Component Unit Tests (58 test cases)
- **VettingApplicationsList**: 13 tests ✅
- **VettingApplicationDetail**: 15 tests ✅
- **OnHoldModal**: 13 tests ✅
- **SendReminderModal**: 17 tests ✅

### API Unit Tests (25+ test cases)
- **VettingAdminApiService**: All API methods ✅

### Integration Tests (22 test cases)
- **VettingServiceTests**: 12 tests ✅
- **VettingEndpointsTests**: 10 tests ✅

### E2E Tests (12 workflow tests)
- **Complete workflows**: Navigation, modals, error handling ✅

## 🐛 Troubleshooting

### Common Issues

**Tests fail with "Connection refused"**
```bash
# Check Docker containers
docker ps | grep witchcity
# Restart if needed
./dev.sh
```

**React tests fail with import errors**
```bash
# Check if in correct directory
pwd  # Should end with /apps/web
# Install dependencies if needed
npm install
```

**Backend tests fail with database errors**
```bash
# Check TestContainers can access Docker
docker info
# May need to restart Docker daemon
```

**E2E tests fail with timeout**
```bash
# Verify application is accessible
curl http://localhost:5173
curl http://localhost:5655/health
# Check for port conflicts
lsof -i :5173 -i :5655
```

### Debug Commands

```bash
# Check test file syntax
cd apps/web && npm run type-check

# Validate C# test compilation
cd tests/unit/api && dotnet build

# Check Playwright installation
npx playwright --version

# Verify test discovery
cd apps/web && npm test -- --list-tests tests/unit/web/features/admin/vetting/
```

## 📈 Success Criteria

All tests should pass with:
- ✅ **0 failing tests**
- ✅ **80%+ code coverage** (where measurable)
- ✅ **No timeout errors**
- ✅ **Clean test output** (no warnings for critical issues)

## 🚀 Next Steps

After successful test execution:

1. **Review coverage reports** for any gaps
2. **Run in CI environment** to verify consistency
3. **Add new tests** as features are extended
4. **Update test documentation** when patterns change

---

**Last Updated**: September 22, 2025
**Maintainer**: Test Developer Agent