# Test Infrastructure Fixes - Phase 1 Complete

**Date**: September 18, 2025
**Agent**: test-developer
**Status**: Phase 1 Complete

## Overview

Successfully completed Phase 1 of test infrastructure fixes, discovering a critical architecture migration that explains why tests were failing. The project migrated from Domain-Driven Design to Vertical Slice Architecture, making legacy tests incompatible.

## Infrastructure Fixes Completed

### 1. Project Reference Updates ✅

**Core.Tests Project** (`/tests/WitchCityRope.Core.Tests/WitchCityRope.Core.Tests.csproj`):
```xml
<!-- BEFORE (Broken) -->
<ProjectReference Include="..\..\src\WitchCityRope.Core\WitchCityRope.Core.csproj" />

<!-- AFTER (Fixed) -->
<ProjectReference Include="..\..\apps\api\WitchCityRope.Api.csproj" />
```

**Integration Tests Project** (`/tests/integration/WitchCityRope.IntegrationTests.csproj`):
```xml
<!-- BEFORE (Broken) -->
<ProjectReference Include="../../src/WitchCityRope.Api/WitchCityRope.Api.csproj" />
<ProjectReference Include="../../src/WitchCityRope.Core/WitchCityRope.Core.csproj" />
<ProjectReference Include="../../src/WitchCityRope.Infrastructure/WitchCityRope.Infrastructure.csproj" />

<!-- AFTER (Fixed) -->
<ProjectReference Include="../../apps/api/WitchCityRope.Api.csproj" />
```

**Tests.Common Project** (`/tests/WitchCityRope.Tests.Common/WitchCityRope.Tests.Common.csproj`):
```xml
<!-- BEFORE (Broken) -->
<ProjectReference Include="..\..\src\WitchCityRope.Core\WitchCityRope.Core.csproj" />
<ProjectReference Include="..\..\src\WitchCityRope.Infrastructure\WitchCityRope.Infrastructure.csproj" />

<!-- AFTER (Fixed) -->
<ProjectReference Include="..\..\apps\api\WitchCityRope.Api.csproj" />
```

### 2. New System Health Check Tests ✅

**Created**: `/tests/WitchCityRope.SystemTests/` - A standalone test project that validates infrastructure without dependencies on archived code.

**Health Check Results**:
```
✅ Docker containers running (witchcity-api, witchcity-postgres)
✅ React dev server accessible (http://localhost:5173)
✅ API service healthy (http://localhost:5655/health)
✅ PostgreSQL accessible (localhost:5433, corrected credentials)
✅ React app title = "Witch City Rope - Salem's Rope Bondage Community"
```

### 3. E2E Test Validation ✅

**Verified**: Playwright tests in `/tests/playwright/` work correctly with current architecture.

**Key Findings**:
- Global setup correctly verifies Docker services before tests
- React app serves correct title (no change needed)
- Tests execute successfully against live services

### 4. PostgreSQL Configuration Fix ✅

**Issue**: Health checks failed due to incorrect PostgreSQL credentials
**Root Cause**: Docker uses `devpass123`, tests expected `postgres`
**Fix**: Updated test connection string to match docker-compose.yml defaults

```csharp
// BEFORE (Failed)
"Host=localhost;Port=5433;Database=WitchCityRope_Dev;Username=postgres;Password=postgres"

// AFTER (Works)
"Host=localhost;Port=5433;Database=witchcityrope_dev;Username=postgres;Password=devpass123"
```

## Critical Architecture Discovery

### The Migration That Broke Tests

**BEFORE (Domain-Driven Design)**:
```csharp
// Domain entities with business logic
public class Event : AggregateRoot
{
    public EventId Id { get; private set; }
    public string Title { get; private set; }
    public Money PricingTier { get; private set; }
    public void Publish() { /* business logic */ }
}

// Tests expected these entities
var event = new EventBuilder().WithTitle("Test").Build();
event.Publish();
```

**AFTER (Vertical Slice Architecture)**:
```csharp
// Simple DTOs and services
public class EventDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public decimal Price { get; set; }
}

// API endpoints and services
public class EventService
{
    public async Task<EventDto> GetEventAsync(string id) { /* service logic */ }
}
```

### Impact on Test Projects

| Project | Status | Issue | Solution |
|---------|--------|-------|----------|
| `WitchCityRope.Core.Tests` | ❌ Broken | Tests domain entities that don't exist | Rewrite for DTO/service testing |
| `WitchCityRope.Tests.Common` | ❌ Broken | Builders for archived entities | Rewrite for current architecture |
| `WitchCityRope.Infrastructure.Tests` | ❌ Broken | Tests infrastructure layer (archived) | Focus on API endpoint testing |
| `WitchCityRope.SystemTests` | ✅ Working | New project, no dependencies | Continue using |
| `tests/playwright/` | ✅ Working | Tests UI, not internal architecture | Continue using |

## Namespace Updates Required

All test files referencing these archived namespaces need updates:

```csharp
// REMOVE these imports
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Core.Enums;
using WitchCityRope.Infrastructure.Data;

// REPLACE with current API namespaces
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Authentication.Models;
```

## Configuration Changes Made

### Health Check Test Configuration
- **React URL**: http://localhost:5173 (confirmed working)
- **API URL**: http://localhost:5655 (confirmed working)
- **PostgreSQL**: localhost:5433, database=witchcityrope_dev, password=devpass123
- **Docker containers**: witchcity-api, witchcity-postgres (confirmed running)

### Test Categories
```csharp
[Trait("Category", "HealthCheck")]    // System infrastructure tests
[Trait("Category", "Integration")]    // API integration tests (needs rewrite)
[Trait("Category", "Unit")]          // Business logic tests (needs rewrite)
```

## Next Steps for Phase 2

### 1. Archive Legacy Tests
Mark obsolete test projects to prevent confusion:
```bash
mv tests/WitchCityRope.Core.Tests tests/WitchCityRope.Core.Tests.obsolete-ddd
mv tests/WitchCityRope.Tests.Common tests/WitchCityRope.Tests.Common.obsolete-ddd
```

### 2. Create Vertical Slice Tests
Focus on testing current architecture:

**API Endpoint Tests**:
```csharp
[Test]
public async Task GetEvents_ReturnsEventDtos()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/events");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
    events.Should().NotBeNull();
}
```

**Service Logic Tests**:
```csharp
[Test]
public async Task EventService_GetEvent_ReturnsEventDto()
{
    var service = new EventService(_dbContext);
    var result = await service.GetEventAsync("event-id");

    result.Should().NotBeNull();
    result.Title.Should().NotBeEmpty();
}
```

### 3. Update Test Patterns Documentation
Create new patterns for vertical slice testing in:
- `/docs/standards-processes/testing/vertical-slice-test-patterns.md`
- Update `/docs/standards-processes/testing/TESTING_GUIDE.md`

## Commands to Run Tests

### Working Health Checks
```bash
# Run infrastructure validation
dotnet test tests/WitchCityRope.SystemTests/ --filter "Category=HealthCheck"

# Expected: 6 passed (all infrastructure working)
```

### Working E2E Tests
```bash
# Start services first
./dev.sh

# Run Playwright tests
npx playwright test --reporter=dot

# Expected: E2E tests pass against live services
```

### Broken Legacy Tests (Don't Run)
```bash
# These will fail due to architecture mismatch
dotnet test tests/WitchCityRope.Core.Tests/           # ❌ 108+ compilation errors
dotnet test tests/WitchCityRope.Tests.Common/         # ❌ Missing domain entities
dotnet test tests/WitchCityRope.Infrastructure.Tests/ # ❌ 69+ compilation errors
```

## Summary

**Phase 1 Achievements**:
✅ Fixed all project references to point to current API project
✅ Created working system health checks
✅ Validated E2E test infrastructure
✅ Identified root cause of test failures (architecture migration)
✅ Corrected PostgreSQL credentials
✅ Verified React app title expectations

**Key Discovery**: Test failures were not infrastructure issues but fundamental architecture incompatibility. The project successfully migrated from DDD to vertical slices, but tests were never updated to match.

**Next Priority**: Rewrite tests for vertical slice architecture rather than trying to fix domain-driven tests for entities that no longer exist.