# Compilation Error Fixes TODO - September 12, 2025

**GOAL**: Fix ALL 334 compilation errors to achieve ZERO errors

## Priority Order (Based on Root Cause Analysis)

### 1. **Remove ALL WitchCityRope.Web References** ⚡ HIGHEST PRIORITY
**Impact**: Fixes 36+ errors - Test projects reference non-existent Blazor project

**Files with WitchCityRope.Web references:**
- tests/WitchCityRope.IntegrationTests/WitchCityRope.IntegrationTests.csproj ❌
- tests/WitchCityRope.IntegrationTests.disabled/WitchCityRope.IntegrationTests.csproj ❌
- Various integration test .cs files (16 total files) ❌

**Action**: Remove all project references and using statements for WitchCityRope.Web

### 2. **Fix EventType.Workshop → EventType.Class References** 🔄 HIGH PRIORITY
**Impact**: Fixes 34+ errors - Old enum values no longer exist

**Files with EventType.Workshop references:**
- tests/WitchCityRope.E2E.Tests/Fixtures/TestDataManager.cs ❌
- Various test builders and mock setups ❌

**Action**: Global find/replace EventType.Workshop → EventType.Class

### 3. **Fix EventType Enum Conflicts** 🔧 HIGH PRIORITY
**Impact**: Fixes type conversion errors - Conflicts between Core and Api enums

**Errors Found:**
- cannot convert from 'WitchCityRope.Core.Enums.EventType' to 'WitchCityRope.Api.Features.Events.Models.EventType'
- 'EventType' is an ambiguous reference between namespaces

**Action**: Use explicit namespace references or consolidate enum usage

### 4. **Fix FluentAssertions Issues** 📚 MEDIUM PRIORITY  
**Impact**: Fixes 78+ errors - Method name/package version issues

**Errors Found:**
- 'BeLessOrEqualTo' method not found
- Method signature mismatches

**Action**: Check FluentAssertions package version and fix method calls

### 5. **Fix Model Property/Constructor Issues** 🏗️ MEDIUM PRIORITY
**Impact**: Fixes 46+ errors - Constructor signature changes

**Errors Found:**
- CreateEventRequest constructor parameter missing 'Title'
- CreateEventResponse missing 'Title', 'Slug' properties
- Cannot convert List<string> to string[]

**Action**: Fix constructor calls and property access

### 6. **Fix Enum Status Reference Issues** 🎯 LOW PRIORITY
**Impact**: Fixes remaining errors - Status enum mismatches

**Errors Found:**
- 'RegistrationStatus' does not contain a definition for 'Failed'
- Ambiguous RegistrationStatus references
- 'UserRole' does not exist

**Action**: Use correct enum values and resolve namespace conflicts

## Progress Tracking

### ✅ Completed
- [x] Diagnosed error categories and root causes
- [x] Created systematic fix plan
- [x] Identified all files requiring changes

### 🔄 In Progress
- [ ] **CURRENT**: Removing WitchCityRope.Web references

### ⏳ Pending
- [ ] Fix EventType.Workshop references
- [ ] Resolve EventType enum conflicts  
- [ ] Fix FluentAssertions method calls
- [ ] Fix constructor/property mismatches
- [ ] Fix enum status references
- [ ] **FINAL**: Verify ZERO compilation errors

## Build Status After Each Fix Category
- **Before fixes**: 334 errors
- **After Web refs removed**: [TBD]
- **After EventType.Workshop fixed**: [TBD] 
- **After enum conflicts resolved**: [TBD]
- **After FluentAssertions fixed**: [TBD]
- **After model fixes**: [TBD]
- **After status enum fixes**: [TBD]
- **FINAL TARGET**: 0 errors ✅

## Files Modified Log
- Will update as each file is fixed
- Commit after each major category for progress tracking

## Notes
- Following TDD principles - these fixes will enable proper test execution
- Integration tests using WitchCityRope.Web are obsolete after React migration
- Consider deleting obsolete integration tests rather than fixing if they can't be adapted