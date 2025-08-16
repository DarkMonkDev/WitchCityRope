# Lessons Learned: UI Developers

> **⚠️ ARCHIVED**: This Blazor-specific document has been consolidated into `/docs/lessons-learned/frontend-lessons-learned.md` with React equivalents. 
> 
> **Archive Date**: 2025-08-16  
> **Reason**: Migrated to React - Blazor patterns no longer applicable
> 
> This file is kept for historical reference during the migration period.

This document contains critical lessons learned from the WitchCityRope project that UI/Blazor developers need to know to avoid hours or days of debugging.

## 🚨 CRITICAL: Pure Blazor Server Architecture

### No Razor Pages Allowed

**Issue**: Hybrid Razor Pages + Blazor architecture caused render mode conflicts

**Solution**: This is now a PURE BLAZOR SERVER application
```razor
@* ❌ NEVER CREATE THESE AGAIN: *@
- NO `_Host.cshtml`, `_Layout.cshtml`, or any Razor Pages
- NO `AddRazorPages()` or `MapRazorPages()`
- NO hybrid Razor Pages + Blazor architecture

@* ✅ ALWAYS USE: *@
- `App.razor` as the root HTML document
- ALL pages must be Blazor components (.razor files)
- `@rendermode="InteractiveServer"` for interactive pages
- Pure Blazor Server with `MapRazorComponents<App>()`
```

**Why This Matters**:
- Hybrid architecture caused render mode conflicts → ELIMINATED
- Razor Pages caused layout/CSS issues → FIXED
- Interactive modes now work properly → WORKING
- Authentication and antiforgery tokens integrated → SECURED

## .NET 9 Render Mode Syntax

**Problem**: Used old `@rendermode InteractiveServer` syntax causing compilation errors

**Solution**: Use .NET 9 syntax and follow layout restrictions

**Reference**: See `/docs/standards-processes/development-standards/blazor-server-patterns.md` for complete render mode documentation and patterns.

## Authentication State Management

### Single Source of Truth Pattern

**Problem**: "My Dashboard" showing for unauthenticated users while login button also showing

**Root Cause**: Duplicate authentication logic in MainLayout AND UserMenuComponent competing with each other

**Solution**: Use `AuthorizeView` consistently everywhere instead of custom authentication state management

**Reference**: See `/docs/standards-processes/development-standards/blazor-server-patterns.md` for complete authentication patterns.

## CSS Escape Sequences in Razor

**Issue**: CSS @media and @keyframes cause Razor compilation errors

**Solution**: Always use double @ in CSS within Razor files

**Reference**: See `/docs/standards-processes/development-standards/blazor-server-patterns.md` for CSS requirements and examples.

## User Menu Dropdown Issues

### Event Handler Binding with AuthorizeView

**Issue**: User menu dropdown wouldn't open when clicked

**Root Cause**: Blazor event handlers weren't binding due to AuthorizeView re-rendering

**Solution**: Use native HTML `<details>/<summary>` elements
```html
@* ❌ WRONG - Complex JavaScript/Blazor event handling *@
<div @onclick="ToggleMenu">...</div>

@* ✅ CORRECT - Native HTML elements *@
<details class="user-menu-dropdown">
    <summary class="user-menu-trigger">
        <span class="user-name">@context.User.Identity?.Name</span>
    </summary>
    <div class="user-menu-content">
        <!-- Menu items -->
    </div>
</details>
```

**Benefits**:
- No dependency on JavaScript initialization
- Works reliably with pure HTML/CSS
- Properly shows user info and menu options
- Click-outside behavior to close dropdown

## Form Validation Components

**Standard**: ALL forms must use WCR validation components for consistency

**Reference**: See `/docs/standards-processes/form-fields-and-validation-standards.md` and `/docs/standards-processes/validation-standardization/VALIDATION_COMPONENT_LIBRARY.md` for complete component documentation and usage patterns.

## Antiforgery Token Handling

**Rule**: NEVER manually add `<AntiforgeryToken />` to EditForm components

**Solution**: Use `FormName` + `SupplyParameterFromForm` for automatic handling

**Reference**: See `/docs/standards-processes/development-standards/blazor-server-patterns.md` for antiforgery token patterns.

## Docker Hot Reload Issues

### Container Restart Required

**Problem**: Made UI fixes but user reported "nothing has changed"

**Solution**: Hot reload is unreliable - always restart containers
```bash
# When changes aren't showing:
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web

# Or use helper script:
./restart-web.sh
```

**When to Restart**:
- Authentication/authorization changes
- Layout component modifications
- Render mode changes
- CSS changes (especially in layouts)

## Common UI Pitfalls

### Interactive vs Static Pages

**Rule**: Know when to use interactive render mode

```razor
@* Interactive pages (forms, buttons, user input): *@
@page "/login"
@rendermode @(new InteractiveServerRenderMode())

@* Static pages (content display only): *@
@page "/about"
@* No rendermode needed *@
```

### Component State During Re-renders

**Issue**: State lost during re-renders

**Solution**: Use proper state management
```csharp
@* ❌ WRONG - State in local variables *@
private bool isOpen = false;

@* ✅ CORRECT - State in component *@
[Parameter] public bool IsOpen { get; set; }
[Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
```

## Blazor Circuit Errors and Unhandled Exceptions

### Problem: "An error has occurred. This application may no longer respond until reloaded"

**Root Cause**: Unhandled exceptions in Blazor Server components break the SignalR circuit

**Common Causes**:
- Null reference exceptions during component initialization
- Missing dependency injection services (especially ILogger)
- Unsafe property access on API responses
- Missing try-catch blocks in lifecycle methods

**Solution**: Comprehensive defensive coding patterns

```csharp
@inject ILogger<ComponentName> Logger

@code {
    protected override async Task OnInitializedAsync()
    {
        try
        {
            Logger?.LogInformation("Component initializing...");
            
            // Always check for authentication before API calls
            await EnsureAuthenticationAsync();
            
            // Load data with error handling
            await LoadData();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error during component initialization");
            errorMessage = "Failed to initialize component. Please refresh the page.";
            StateHasChanged(); // Ensure UI updates
        }
    }
}
```

**Critical Patterns**:
- Always inject ILogger and use null-conditional operator: `Logger?.LogError(...)`
- Wrap all async operations in try-catch blocks
- Use null-conditional operators on all property access: `user?.SceneName ?? "Unknown"`
- Call `StateHasChanged()` after setting error messages
- Implement IDisposable for cleanup

### JWT Authentication 401 Errors

**Problem**: API calls return 401 Unauthorized even when user is logged in

**Root Cause**: JWT tokens not being acquired or attached to API requests

**Solution**: Simplified AuthenticationDelegatingHandler that only checks for existing tokens

```csharp
// FIXED: Simplified handler that prevents Blazor circuit breaks
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    // Only checks for existing JWT tokens - no service resolution or token acquisition
    // Token acquisition happens during login via AuthenticationEventHandler
}
```

### AuthenticationDelegatingHandler Circuit Breaking

**Problem**: Complex service resolution and token acquisition in DelegatingHandler causing Blazor circuits to break

**Root Cause**: 
- Excessive logging in hot path (every HTTP request)
- Service resolution using IServiceProvider.CreateScope() in transient handler
- UserManager resolution and usage in DelegatingHandler
- Complex token acquisition logic in the wrong place

**Solution**: Simplified handler with single responsibility
```csharp
// ❌ WRONG - Complex handler that breaks circuits
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    // Excessive logging
    // Service resolution with CreateScope()
    // UserManager access
    // Token acquisition logic
}

// ✅ CORRECT - Simple handler that won't break circuits
public class AuthenticationDelegatingHandler : DelegatingHandler
{
    // Only checks authentication status
    // Gets existing token from IJwtTokenService
    // Adds Authorization header if token exists
    // Minimal logging with null-conditional operators
}
```

**Key Fixes**:
- Remove IServiceProvider dependency and service resolution
- Remove UserManager access
- Move token acquisition to AuthenticationEventHandler (during login)
- Use null-conditional operators on ILogger: `_logger?.LogDebug(...)` in ALL services
- Wrap token retrieval in try-catch to prevent circuit breaks
- Minimal logging only for debugging purposes
- NEVER try to acquire tokens in Blazor components - causes circuit breaks
- Components should only check for existing tokens and show friendly error messages

**Component Pattern**:
```csharp
private async Task LoadData()
{
    try
    {
        // Ensure authentication before API calls
        await EnsureAuthenticationAsync();
        
        var result = await ApiClient.GetDataAsync();
        // Handle result safely...
    }
    catch (HttpRequestException ex) when (ex.Message.Contains("401"))
    {
        // Refresh authentication and retry once
        await RefreshAuthenticationAsync();
        var retryResult = await ApiClient.GetDataAsync();
        // Handle retry result...
    }
}
```

## Key Learnings Summary

1. **Pure Blazor Server only** - No Razor Pages allowed
2. **Use native HTML when possible** - Avoids Blazor re-render issues (e.g., `<details>/<summary>` for dropdowns)
3. **Restart containers for UI changes** - Hot reload is unreliable in Docker
4. **Know when to use interactive mode** - Not all pages need it
5. **State management matters** - Use proper component patterns
6. **Always inject ILogger** - Essential for debugging Blazor circuit errors
7. **Use defensive coding** - Null checks, try-catch blocks, StateHasChanged() calls
8. **Simplified DelegatingHandler** - No service resolution or complex logic to prevent circuit breaks
9. **JWT token acquisition** - Happens during login via AuthenticationEventHandler, not in DelegatingHandler

**For complete patterns and syntax, see:**
- **Blazor patterns**: `/docs/standards-processes/development-standards/blazor-server-patterns.md`
- **Validation components**: `/docs/standards-processes/form-fields-and-validation-standards.md`
- **Component library**: `/docs/standards-processes/validation-standardization/VALIDATION_COMPONENT_LIBRARY.md`