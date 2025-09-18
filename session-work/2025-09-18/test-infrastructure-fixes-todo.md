# Test Infrastructure Fixes - Phase 1
**Date**: 2025-09-18
**Agent**: test-developer
**Objective**: Fix systematic test infrastructure issues

## Phase 1: Infrastructure Repair

### 1. Fix Unit Test Project References ⚠️
**Location**: `/tests/WitchCityRope.Core.Tests/`
- [x] **ANALYZE**: Core.Tests references archived `..\..\src\WitchCityRope.Core\` path
- [x] **FIX**: Update project references to point to `/apps/api/` ✅
- [x] **ISSUE DISCOVERED**: Tests expect domain entities that no longer exist (migrated to DTOs/services)
- [x] **DECISION**: Architecture changed from DDD to vertical slices - tests need rewrite, not just references

### 2. Fix Integration Test Project References ⚠️
**Location**: `/tests/integration/WitchCityRope.IntegrationTests.csproj`
- [x] **ANALYZE**: References archived paths in `/src/` instead of `/apps/api/`
- [x] **FIX**: Point to `/apps/api/WitchCityRope.Api.csproj` ✅
- [x] **ISSUE DISCOVERED**: Same domain entity dependency issues
- [x] **DECISION**: Architecture mismatch requires test rewrite

### 3. Fix Common Test Project ⚠️
**Location**: `/tests/WitchCityRope.Tests.Common/`
- [x] **ANALYZE**: References archived Core/Infrastructure projects
- [x] **FIX**: Update project references to active code ✅
- [x] **ISSUE DISCOVERED**: Test builders use domain entities that don't exist
- [x] **DECISION**: Need to rebuild for DTO/service architecture

### 4. Create Working System Tests ✅
**Location**: `/tests/WitchCityRope.SystemTests/` (NEW)
- [x] **CREATE**: New standalone health check project
- [x] **VERIFY**: React dev server (port 5173) ✅
- [x] **VERIFY**: API service (port 5655) ✅
- [x] **VERIFY**: PostgreSQL (port 5433) ✅
- [x] **VERIFY**: Docker containers ✅
- [x] **VERIFY**: React app title = "Witch City Rope - Salem's Rope Bondage Community" ✅

### 5. Verify E2E Test Infrastructure ✅
**Location**: `/tests/playwright/`
- [x] **ANALYZE**: Playwright tests work with current architecture ✅
- [x] **VERIFY**: Title expectation matches actual React app ✅
- [x] **VERIFY**: Global setup works with Docker services ✅
- [x] **TEST**: Quick E2E test runs successfully ✅

## Success Criteria
- [x] ✅ **Health checks work**: Created `/tests/WitchCityRope.SystemTests/` with comprehensive infrastructure validation
- [x] ✅ **E2E tests work**: Playwright tests execute successfully against Docker services
- [x] ✅ **React title verified**: App displays "Witch City Rope - Salem's Rope Bondage Community"
- [x] ✅ **Project references updated**: All test projects now reference `/apps/api/` correctly
- [x] ⚠️ **Architecture mismatch discovered**: Domain-driven tests incompatible with vertical slice API

## Critical Discovery: Architecture Migration Impact
**FINDING**: The project migrated from Domain-Driven Design (with entities like `Event`, `User`, `Money`) to Vertical Slice Architecture (with DTOs and services). The existing unit/integration tests expect domain entities that no longer exist.

**IMPACT**:
- ❌ `WitchCityRope.Core.Tests` - Tests domain entities (now archived)
- ❌ `WitchCityRope.Tests.Common` - Builders for domain entities (obsolete)
- ❌ `WitchCityRope.Infrastructure.Tests` - Tests infrastructure layer (now vertical slices)
- ✅ `WitchCityRope.SystemTests` - Works with current architecture
- ✅ `tests/playwright/` - Works with current architecture

## Phase 2 Recommendations
1. **Keep working tests**: SystemTests and Playwright E2E tests
2. **Rewrite unit tests**: Target DTO validation, service logic, API endpoints
3. **Archive legacy tests**: Mark old domain tests as obsolete
4. **Focus on API testing**: Test current vertical slice endpoints
5. **Document patterns**: Create new test patterns for vertical slice architecture