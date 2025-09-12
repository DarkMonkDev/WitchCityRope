# Test Compilation Fixes - 2025-09-12

## Issues Identified

### 1. EventType Enum Issues
- Tests use old EventType values like `EventType.Workshop` 
- EventType enum now only has `Class` and `Social`
- Need to replace all old references

### 2. Event Entity Property Issues
- Tests reference properties that don't exist or use old names
- Event entity has `Capacity` and `GetCurrentAttendeeCount()` method
- No Slug, MaxAttendees, CurrentAttendees properties needed

### 3. Service Method Signature Changes
- EventService.CreateEventAsync may have changed signature
- Need to check if organizerId parameter is required

### 4. RegistrationStatus Namespace Conflicts
- Core vs API namespace issues

### 5. Moq ReturnsAsync Issues
- Method call failures in test setup

## Files to Fix

### Core Tests
- `/tests/WitchCityRope.Core.Tests/Entities/EventTests.cs` - EventType.Workshop → EventType.Class
- Other test files with old EventType references

### API Tests  
- `/tests/WitchCityRope.Api.Tests/Features/Events/CreateEventCommandTests.cs`
- `/tests/WitchCityRope.Api.Tests/Services/EventServiceTests.cs`
- `/tests/WitchCityRope.Api.Tests/Services/ConcurrencyAndEdgeCaseTests.cs`
- `/tests/WitchCityRope.Api.Tests/Models/RequestModelValidationTests.cs`

## Status - Progress Update

### Fixed Issues ✅
1. **EventType Enum Issues** - Fixed EventType.Workshop → EventType.Class across all Core tests
2. **Event Entity Property Issues** - Fixed MockHelpers to use EventBuilder pattern with proper Event constructor
3. **RegistrationStatus Namespace Conflicts** - Added namespace aliases (CoreEnums = WitchCityRope.Core.Enums)
4. **RegisterForEventRequest Constructor** - Fixed to use proper record constructor parameters
5. **Missing using statements** - Added WitchCityRope.Core for DomainException
6. **MockHelpers SendAsync** - Fixed to use SendEmailAsync method
7. **Core Tests Compilation** - ✅ WitchCityRope.Core.Tests now compiles successfully

### Remaining Issues (API Tests) ❌
1. **EventType conversion issues** - Need to use API EventType vs Core EventType 
2. **CreateEventResponse property mismatches** - Core DTOs vs API Models have different properties
3. **Moq ReturnsAsync issues** - Method signature mismatches in test setup
4. **Missing UserRole references** - Need to add using statements for UserRole
5. **RegistrationStatus.Failed** - API enum may not have Failed value

### Currently Working On
- Fixing API test compilation errors
- API tests have ~10 remaining errors vs the original 200+