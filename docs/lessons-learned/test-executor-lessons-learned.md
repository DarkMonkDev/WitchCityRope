# Test Executor Lessons Learned
<!-- Last Updated: 2025-09-12 -->
<!-- Version: 12.0 -->
<!-- Owner: Test Team -->
<!-- Status: Active -->

## 🚨 CRITICAL: Compilation Check is Mandatory First Step

**Problem**: Running tests without checking compilation leads to misleading failures.
**Solution**: Always run `dotnet build` before any test execution.

### Pattern
```bash
# ALWAYS first step before testing
dotnet build
# If fails, report compilation errors to orchestrator
# If passes, proceed with test execution
```

**Evidence**: 208 compilation errors prevented all .NET testing, creating false impression of test failures.

## 🚨 CRITICAL: Environment Pre-Flight Checks Required

**Problem**: Test failures often caused by unhealthy infrastructure, not test issues.
**Solution**: Mandatory health validation before test execution.

### Pre-Flight Checklist Pattern
```bash
# 1. Docker Container Health
docker ps --format "table {{.Names}}\t{{.Status}}" | grep witchcity

# Check specific health status
docker inspect witchcity-web --format='{{.State.Health.Status}}'
docker inspect witchcity-api --format='{{.State.Health.Status}}'
docker inspect witchcity-db --format='{{.State.Health.Status}}'

# 2. Service Endpoints
curl -f http://localhost:5651/health || echo "Web service unhealthy"
curl -f http://localhost:5653/health || echo "API service unhealthy"
curl -f http://localhost:5653/health/database || echo "Database unhealthy"

# 3. Database Seed Data
PGPASSWORD=WitchCity2024! psql -h localhost -p 5433 -U postgres -d witchcityrope_dev \
  -c "SELECT COUNT(*) FROM auth.\"Users\" WHERE \"Email\" LIKE '%@witchcityrope.com';"
```

## 🚨 CRITICAL: React Component Rendering vs Infrastructure Health

**Problem**: Infrastructure can be healthy while React components fail to render.
**Solution**: Always distinguish between environment issues and application issues.

### Diagnostic Pattern
1. **HTML Delivery Check**: ✅ Page loads, title correct, scripts load
2. **Component Rendering Check**: ❌ Body content empty, no DOM elements
3. **Element Detection**: 0 forms, 0 inputs, 0 buttons across all routes
4. **Route Accessibility**: Routes respond but render nothing

### Evidence Required
- **Screenshots**: Capture visual proof of blank pages
- **Element Counts**: Document zero elements found
- **Route Accessibility**: Confirm routes load but don't render
- **Browser Console**: Check for JavaScript errors (if accessible)

## 🚨 CRITICAL: Database Seeding Pattern for WitchCityRope

**Problem**: Attempting manual database seeding breaks the automated C# seeding system.
**Solution**: Database seeding is handled ONLY through C# code in the API.

### Correct Pattern
1. **Start the API container** - This triggers DatabaseInitializationService automatically
2. **Check API logs for initialization** - `docker logs witchcity-api | grep -i "database\|seed\|migration"`  
3. **Verify through API endpoints** - Test `/api/health` and `/api/events` to confirm data exists
4. **If database isn't seeded** - The issue is with the API service, NOT missing scripts

### NEVER DO
- ❌ Write SQL scripts to insert test data
- ❌ Use psql or database tools to manually insert data
- ❌ Create bash scripts for database seeding
- ❌ Look for seed scripts (they don't exist by design)
- ❌ Bypass the C# seeding mechanism

## Compilation Error Patterns and Solutions

### EventService Signature Changes
**Problem**: API signature changes not reflected in test code.
**Solution**: Update test calls with new parameters.

```csharp
// ❌ OLD - in tests
EventService.CreateEventAsync(CreateEventRequest)

// ✅ NEW - missing organizerId parameter
EventService.CreateEventAsync(CreateEventRequest, Guid organizerId)
```

### Missing Entity Properties
**Problem**: Tests expect properties that don't exist on entities.
**Solution**: Either implement properties or update tests.

```csharp
// ❌ Properties don't exist
event.Slug // Property doesn't exist
event.MaxAttendees // Property doesn't exist  
event.CurrentAttendees // Property doesn't exist
```

### Namespace Conflicts
**Problem**: Ambiguous enum references cause compilation errors.
**Solution**: Use fully qualified names.

```csharp
// ❌ Ambiguous
RegistrationStatus // Ambiguous between Core.Enums and Api.Features.Events.Models

// ✅ Fully qualified
WitchCityRope.Core.Enums.RegistrationStatus
```

### Constructor Parameter Changes
**Problem**: Constructor signatures change but tests not updated.
**Solution**: Update test instantiation.

```csharp
// ❌ OLD - in tests
new RegisterForEventRequest(Guid userId, ...)

// ✅ NEW - requires eventId parameter
new RegisterForEventRequest(Guid eventId, ...)
```

## React Unit Test Issues

### Multiple Elements with Same Text
**Problem**: Tests fail when multiple elements have identical text content.
**Solution**: Use more specific selectors.

```jsx
// ❌ Problem
Found multiple elements with the text: "Profile"
<p class="mantine-Text-root">Profile</p>  // Navigation link
<h1 class="mantine-Title-root">Profile</h1> // Page title

// ✅ Solution - Use specific selectors
screen.getByRole('heading', { name: 'Profile' })  // For h1
screen.getByRole('link', { name: 'Profile' })     // For navigation
```

### Test Timeouts
**Problem**: React tests timing out after 30 seconds.
**Solution**: Investigate performance issues and optimize selectors.

## E2E Playwright Test Patterns

### Login Solution Pattern
**Problem**: Mantine UI login tests failing with selector issues.
**Solution**: Use data-testid selectors with direct page.fill() approach.

```typescript
// ✅ WORKING APPROACH - Use data-testid selectors
const emailInput = page.locator('[data-testid="email-input"]')
const passwordInput = page.locator('[data-testid="password-input"]') 
const loginButton = page.locator('[data-testid="login-button"]')

// Fill form with page.fill() - works reliably with Mantine
await emailInput.fill('admin@witchcityrope.com')
await passwordInput.fill('Test123!')
await loginButton.click()

// ❌ DOES NOT WORK - name attribute selectors
const emailInput = page.locator('input[name="email"]') // 0 elements found
const passwordInput = page.locator('input[name="password"]') // 0 elements found
```

### Complete Login Test Pattern
```typescript
test('Login and navigate', async ({ page }) => {
  // Navigate to login
  await page.goto('http://localhost:5173/login')
  await page.waitForLoadState('networkidle')
  
  // Wait for form
  await page.waitForSelector('[data-testid="login-form"]', { timeout: 10000 })
  
  // Fill credentials using data-testid selectors
  await page.locator('[data-testid="email-input"]').fill('admin@witchcityrope.com')
  await page.locator('[data-testid="password-input"]').fill('Test123!')
  
  // Submit and wait for navigation
  await page.locator('[data-testid="login-button"]').click()
  await page.waitForURL('**/dashboard', { timeout: 15000 })
  
  // Continue with test...
})
```

### CSS Warning vs Critical Error Distinction
**Problem**: Mantine CSS warnings treated as test failures.
**Solution**: Filter console errors by actual impact on functionality.

```javascript
// ✅ Non-blocking - ignore these
Warning: Unsupported style property @media (max-width: 768px)
Warning: Unsupported style property &:focus-visible

// 🚨 Critical - these block functionality  
Element not found: [data-testid="login-button"]
```

## Parallel Worker Configuration

### 15 Parallel Workers Pattern
**Problem**: Single worker execution takes 50+ minutes for 380 tests.
**Solution**: Configure 15 workers for development, 1 for CI.

```typescript
// ✅ Optimal configuration
workers: process.env.CI ? 1 : 15,
fullyParallel: true,

// Performance improvement: 380 tests in ~5-7 minutes vs 50+ minutes
```

**Evidence**: "Running 380 tests using 15 workers" confirmed working.

## Test Failure Categorization

### Distinguish Development vs Bugs
**Problem**: Test failures during active development treated as bugs.
**Solution**: Categorize failures correctly.

#### ✅ EXPECTED - DEVELOPMENT IN PROGRESS
- Missing entity properties not implemented yet
- UI component evolution requires selector updates
- Missing API endpoints not implemented
- Test maintenance lag during rapid development

#### 🚨 ACTUAL BUGS NEEDING FIXES
- Namespace conflicts in compilation
- API signature mismatches in tests
- Moq setup issues with ReturnsAsync
- Test performance timeouts

## Environment Troubleshooting Patterns

### Container Health vs Service Health
**Problem**: Containers show "Up" but services are unhealthy.
**Solution**: Check multiple layers of health.

```bash
# Layer 1: Container status
docker ps | grep witchcity

# Layer 2: Health check status
docker inspect witchcity-web --format='{{.State.Health.Status}}'

# Layer 3: Service endpoints
curl -f http://localhost:5173/health

# Layer 4: Functional validation
curl -f http://localhost:5653/api/events | jq length
```

### Compilation Errors in Container Logs
**Problem**: Container appears running but has compilation errors.
**Solution**: Always check container logs for build errors.

```bash
# Check for compilation errors
docker logs witchcity-web --tail 50 | grep -i error

# If found, restart to rebuild
./dev.sh  # Preferred method
```

## Test Result Reporting Pattern

### Failure Report Format
```json
{
  "error_type": "compilation",
  "details": "CS0246: Type 'LoginRequest' not found",
  "file": "AuthService.cs:45",
  "suggested_fix": "backend-developer needed"
}
```

### Environment Issue Report Format
```json
{
  "error_type": "environment",
  "issue": "Database not seeded",
  "action_taken": "Ran seed script - resolved",
  "status": "fixed"
}
```

## Critical Testing Methodology Lessons

### 1. Selector Strategy Must Match Component Implementation
**Problem**: Using selectors that components don't implement.
**Solution**: Verify component implementation before writing selectors.

### 2. Environment Health Prevents Misdiagnosis
**Problem**: Attributing failures to tests when infrastructure is broken.
**Solution**: Multi-layer environment validation before testing.

### 3. Visual Evidence Prevents False Conclusions
**Problem**: Assuming test failures without seeing actual application state.
**Solution**: Always capture screenshots for diagnostic evidence.

### 4. Performance Monitoring During Testing
**Problem**: Not tracking response times during test execution.
**Solution**: Monitor and report performance metrics.

| Operation | Target | Status |
|-----------|---------|---------|
| Login | <5s | Monitor |
| API Calls | <2s | Monitor |
| Page Load | <3s | Monitor |

## Mandatory Test Execution Workflow

### Phase 1: Pre-Flight Validation
1. ✅ Check Docker containers health
2. ✅ Verify service endpoints respond
3. ✅ Confirm database seeded
4. ✅ Run compilation check

### Phase 2: Test Execution Order
1. **Compilation Check** - `dotnet build`
2. **Unit Tests** - Backend and React separately
3. **Integration Tests** - With health check first
4. **E2E Tests** - With environment validation

### Phase 3: Result Analysis
1. **Categorize failures** (compilation vs bugs vs expected)
2. **Capture evidence** (screenshots, logs)
3. **Report to orchestrator** with specific agent recommendations

## Common Pitfall Prevention

### Don't Mix Infrastructure and Application Issues
**Problem**: Environment problems blamed on application or vice versa.
**Solution**: Layer validation to isolate root cause.

### Don't Skip Compilation Validation
**Problem**: Wasting time on tests when code doesn't compile.
**Solution**: Always build first, test second.

### Don't Assume Selector Availability
**Problem**: Using selectors without verifying they exist in components.
**Solution**: Inspect component implementation or use browser dev tools.

### Don't Ignore CSS Warnings in Mantine
**Problem**: Treating CSS warnings as critical test failures.
**Solution**: Filter Mantine warnings as non-blocking noise.

## Tags
#compilation-check #environment-health #react-rendering #database-seeding #playwright-testing #parallel-workers #test-categorization #selector-patterns #mantine-ui #css-warnings #performance-monitoring #failure-analysis