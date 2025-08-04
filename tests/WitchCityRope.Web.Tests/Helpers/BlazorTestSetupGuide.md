# Blazor Component Test Setup Guide

## Common Issues and Solutions

### 1. Components Not Starting Properly

**Issue**: Blazor components fail to initialize in test environment.

**Root Causes**:
- Missing Blazor services in test setup
- Incorrect render mode configuration
- Missing JavaScript runtime mock
- Navigation manager not properly configured

### 2. Required Test Setup

For basic Blazor component tests, ensure the following services are registered:

```csharp
// In your test setup
Services.AddLogging();
Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
Services.AddSingleton<IJSRuntime>(Mock.Of<IJSRuntime>());
```

### 3. Interactive Server Components

Components using `@rendermode InteractiveServer` require special handling:

```csharp
// Use InteractiveServerTestBase for these components
public class MyTest : InteractiveServerTestBase
{
    // Test implementation
}
```

### 4. Navigation Manager Setup

Always use bunit's `FakeNavigationManager`:

```csharp
var navigationManager = new FakeNavigationManager()
{
    BaseUri = "https://localhost/"
};
Services.AddSingleton<NavigationManager>(navigationManager);
```

### 5. Common Service Mocks

Essential services that should be mocked:
- `IAuthService`
- `ILocalStorageService`
- `INotificationService`
- `IToastService`
- `IJSRuntime`

### 6. Syncfusion Components

If using Syncfusion components, add:
```csharp
Services.AddSyncfusionBlazor(options => { });
```

Note: Comment this out if you encounter ambiguous reference errors.

### 7. Test Inheritance Hierarchy

- `TestContext` (bunit base) - For simple component tests
- `TestBase` - Adds common services
- `ComponentTestBase` - Adds all common mocks
- `InteractiveServerTestBase` - For interactive server components

### 8. Troubleshooting Steps

1. Check if all required services are registered
2. Verify navigation manager is properly set up
3. Ensure IJSRuntime is mocked
4. Check for missing authentication services
5. Verify render mode compatibility

### 9. Example Test Setup

```csharp
public class LoginComponentTests : TestContext
{
    public LoginComponentTests()
    {
        // Setup navigation
        var navigationManager = new FakeNavigationManager();
        Services.AddSingleton<NavigationManager>(navigationManager);
        
        // Setup JS runtime
        Services.AddSingleton(Mock.Of<IJSRuntime>());
        
        // Setup other services
        Services.AddSingleton(Mock.Of<IAuthService>());
        
        // Component can now be rendered
    }
}
```

### 10. Key Points

- bunit runs components in static mode by default
- Interactive render modes are simulated, not actually interactive
- SignalR/Circuits are not needed for unit tests
- Always mock external dependencies