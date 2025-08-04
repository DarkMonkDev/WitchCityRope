# CRITICAL NOTES - WitchCityRope Test Development

## ⚠️ DO NOT ADD THESE DEPENDENCIES

### UI Frameworks (Project uses SYNCFUSION ONLY)
- ❌ **MudBlazor** - DO NOT ADD
- ❌ **Radzen** - DO NOT ADD  
- ❌ **Telerik** - DO NOT ADD
- ❌ **DevExpress** - DO NOT ADD
- ❌ **Any other UI component library** - DO NOT ADD

✅ **ONLY USE**: Syncfusion Blazor Components (project has subscription)

### Why This Matters
1. Project has PAID Syncfusion subscription
2. Adding other UI frameworks creates conflicts
3. Increases bundle size unnecessarily
4. Creates maintenance burden
5. Violates project architecture decisions

## 🔍 VERIFY BEFORE ASSUMING

### Common Incorrect Assumptions
1. **ApiClient** - It's a CONCRETE CLASS, not an interface
   - ❌ Wrong: `IApiClient`
   - ✅ Right: `ApiClient`

2. **Namespaces** - Check they actually exist
   - ❌ Wrong: `WitchCityRope.Shared.Contracts`
   - ✅ Right: `WitchCityRope.Web.Models`

3. **Service Interfaces** - Verify actual method names
   - ❌ Wrong: `GetProfileAsync()`
   - ✅ Right: `GetCurrentUserProfileAsync()`

### How to Verify
```bash
# Check if a type exists
grep -r "class TypeName" src/
grep -r "interface ITypeName" src/

# Check namespace
grep -r "namespace WitchCityRope.Something" src/

# Check service methods
grep -r "methodName" src/WitchCityRope.Web/Services/
```

## 📋 Actual Project Dependencies

### UI Components
- **Syncfusion Blazor** - Complete UI component suite
- Custom components in `/src/WitchCityRope.Web/Shared/`

### Services (in Web project)
- `ApiClient` - HTTP client for API calls (concrete class)
- `IAuthService` - Authentication service
- `IToastService` - Toast notifications
- `INotificationService` - In-app notifications
- `IEventService` - Event management
- `IUserService` - User profile management

### DTOs and Models
- `UserDto` - in `WitchCityRope.Web.Models`
- `EventListItem`, `EventDetail` - in `WitchCityRope.Web.Services`
- `PagedResult<T>` - in `WitchCityRope.Core.Models`

## 🚨 Test Infrastructure Status

### What Was Fixed (January 11, 2025)
1. Removed all MudBlazor references
2. Fixed ApiClient references (not an interface)
3. Removed non-existent namespace references
4. Verified all remaining types exist

### Current State
- ✅ Clean test infrastructure
- ✅ Only references actual project types
- ✅ No unauthorized dependencies
- ✅ Ready for test development

## 💡 Best Practices

1. **Always check actual code** before making assumptions
2. **Never add UI frameworks** without explicit approval
3. **Verify types exist** before using them
4. **Check method signatures** match actual interfaces
5. **Use grep/search** to find actual implementations

## 🔴 Red Flags in Code Reviews

If you see these, investigate immediately:
- Any `using MudBlazor`
- Any `using Radzen`
- References to `IApiClient` (should be `ApiClient`)
- Unknown namespaces like `*.Shared.Contracts`
- Service methods that don't exist in actual interfaces

## Remember
This project has specific architectural decisions and paid licenses. Respect them.