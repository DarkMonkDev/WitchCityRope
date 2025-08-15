# Build Resolution Strategy

## Current Situation
The Web test project has 318+ compilation errors due to:
- Interface method mismatches after ASP.NET Core Identity migration
- Obsolete test patterns
- Changed DTOs and service contracts
- Type ambiguities

## Options Analysis

### Option 1: Create New Test Project ✅ (Recommended)
**Pros:**
- Clean start without legacy code
- Can run tests immediately
- No time wasted fixing obsolete code
- Clear separation between old and new

**Cons:**
- Temporary duplication
- Need to update solution file
- Eventually need to migrate or remove old project

**Implementation:**
1. Create `WitchCityRope.Web.Tests.New` project
2. Copy over new test infrastructure
3. Continue writing tests
4. Once coverage is achieved, replace old project

### Option 2: Comment Out Old Tests
**Pros:**
- Keep same project structure
- Gradual migration possible
- No solution file changes

**Cons:**
- Lots of manual work
- Risk of missing tests
- Cluttered codebase
- Still need to fix project-level issues

**Implementation:**
1. Comment out all test files except new ones
2. Fix remaining build errors
3. Gradually uncomment and fix tests

### Option 3: Fix All Compilation Errors
**Pros:**
- No lost tests
- Everything working

**Cons:**
- 318+ errors to fix
- Many tests may be obsolete anyway
- Time-consuming (est. 4-6 hours)
- Tests still won't match current architecture

**Time Estimate:** 4-6 hours of fixing for potentially obsolete tests

## Recommendation: Option 1 - New Test Project

### Migration Plan
1. **Create new project** with same structure
2. **Move new tests** to new project
3. **Continue development** in new project
4. **Achieve 80% coverage** target
5. **Archive old project** once complete

### Benefits
- Immediate productivity
- Clean, maintainable codebase
- Focus on current architecture
- No time wasted on obsolete code

## Implementation Steps

```bash
# 1. Create new test project
dotnet new xunit -n WitchCityRope.Web.Tests.New -o tests/WitchCityRope.Web.Tests.New

# 2. Add to solution
dotnet sln add tests/WitchCityRope.Web.Tests.New/WitchCityRope.Web.Tests.New.csproj

# 3. Add required packages
cd tests/WitchCityRope.Web.Tests.New
dotnet add package bunit --version 1.25.3
dotnet add package Moq --version 4.20.72
dotnet add package FluentAssertions --version 8.4.0
dotnet add package Microsoft.AspNetCore.Components --version 9.0.6

# 4. Add project references
dotnet add reference ../../src/WitchCityRope.Web/WitchCityRope.Web.csproj
dotnet add reference ../../src/WitchCityRope.Core/WitchCityRope.Core.csproj
dotnet add reference ../WitchCityRope.Tests.Common/WitchCityRope.Tests.Common.csproj

# 5. Copy new files
cp -r ../WitchCityRope.Web.Tests/Helpers ./
cp -r ../WitchCityRope.Web.Tests/Auth ./
cp -r ../WitchCityRope.Web.Tests/TestData ./
# etc.

# 6. Run tests
dotnet test
```

## Success Criteria
- All new tests pass
- No compilation errors
- Can add new tests easily
- Coverage reporting works
- CI/CD integration successful

## Timeline
- Project setup: 30 minutes
- File migration: 30 minutes
- Verification: 30 minutes
- Total: 1.5 hours

This approach allows immediate progress on test development without getting bogged down in fixing obsolete code.