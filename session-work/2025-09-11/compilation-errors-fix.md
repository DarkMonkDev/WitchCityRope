# WitchCityRope Compilation Errors Fix - 2025-09-11

## Overview
185 compilation errors need to be fixed after NuGet package updates. Keeping Swashbuckle.AspNetCore (not migrating to native OpenAPI).

## Error Categories

### 1. Enum Namespace Conflicts (~50+ errors)
**Issue**: Duplicate enums between Core.Enums and Api.Features.Events.Models namespaces
- `EventType`: WitchCityRope.Core.Enums vs WitchCityRope.Api.Features.Events.Models  
- `RegistrationStatus`: WitchCityRope.Core.Enums vs WitchCityRope.Api.Features.Events.Models
- `PaymentMethod`: WitchCityRope.Core.Enums vs WitchCityRope.Api.Features.Events.Models

**Solution**: Use explicit namespace references in test files and conflicting code

### 2. Method Signature Issues (~90+ errors)
**Issue**: Method calls missing required parameters
- `CreateEventAsync` calls missing `organizerId` parameter (interface requires `Guid organizerId`)
- `RegisterForEventRequest` constructor issues
- Test constructors using old signatures

**CreateEventRequest Record Constructor Requires**:
```csharp
CreateEventRequest(
    string Title,              // ← Missing in many calls
    string Description,
    EventType Type,
    DateTime StartDateTime,
    DateTime EndDateTime, 
    string Location,
    int MaxAttendees,
    decimal Price,
    string[] RequiredSkillLevels,
    string[] Tags,
    bool RequiresVetting,
    string SafetyNotes,
    string EquipmentProvided,
    string EquipmentRequired,
    Guid OrganizerId          // ← Missing in many calls
)
```

### 3. Entity Property Issues (~30+ errors)
**Issue**: Code references properties that don't exist on Event entity
- `Event.Slug` - Property doesn't exist on entity
- `Event.MaxAttendees` - Should be `Event.Capacity`
- `Event.CurrentAttendees` - Should use `Event.GetCurrentAttendeeCount()` method

**Actual Event Entity Properties**:
- ✅ `Event.Capacity` (not MaxAttendees)
- ✅ `Event.StartDate` and `Event.EndDate` (not StartDateTime/EndDateTime)
- ✅ `Event.GetCurrentAttendeeCount()` method (not CurrentAttendees property)

### 4. Authentication Method Return Types (~15+ errors)
**Issue**: Method signature mismatches in auth services
- `IUserRepository.GetUserAsync` return type conflicts
- Mock setup issues with `ReturnsAsync`

## Files Affected (High Priority)

### Test Files (Can Fix - No Test File Modification Restriction Here)
- `/tests/WitchCityRope.Api.Tests/Services/ConcurrencyAndEdgeCaseTests.cs`
- `/tests/WitchCityRope.Api.Tests/Services/AuthServiceTests.cs`
- Other test files with similar patterns

### Service Files  
- Service files referencing Event entity properties
- Files with CreateEventAsync calls
- Authentication-related service files

## Fix Strategy

1. **Phase 1**: Fix enum namespace conflicts with explicit using statements
2. **Phase 2**: Fix method signatures in CreateEventAsync calls  
3. **Phase 3**: Fix entity property references (Slug, MaxAttendees, CurrentAttendees)
4. **Phase 4**: Fix authentication return type mismatches
5. **Phase 5**: Validate all fixes with full build

## Progress Tracking

- [ ] Phase 1: Enum conflicts resolved
- [ ] Phase 2: Method signatures fixed
- [ ] Phase 3: Entity properties fixed  
- [ ] Phase 4: Auth return types fixed
- [ ] Phase 5: Full build success (0 errors)

## Notes
- Solution should build with 0 compilation errors
- API is currently running from previous successful build
- Core functionality is working, need clean compilation for validation