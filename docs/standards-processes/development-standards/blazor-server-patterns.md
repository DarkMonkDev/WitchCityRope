# Blazor Server Development Standards

## Overview

This document defines the mandatory patterns and standards for Blazor Server development in the WitchCityRope project. These standards ensure consistent, maintainable code and prevent common architectural issues.

## Critical Architecture Requirements

### Pure Blazor Server - NO Razor Pages

**This is a PURE BLAZOR SERVER application. NO Razor Pages are allowed anywhere in the project.**

#### ❌ NEVER Create These:
- Razor Pages (`.cshtml` files in `/Pages/` folder)
- `_Host.cshtml`, `_Layout.cshtml`, or any Razor Pages
- `AddRazorPages()` service registration
- `MapRazorPages()` endpoint mapping
- Hybrid Razor Pages + Blazor architecture

#### ✅ ALWAYS Use These Instead:
- Blazor components (`.razor` files in `/Components/Pages/` or `/Features/`)
- `App.razor` as the root HTML document (contains DOCTYPE, head, body)
- Pure Blazor Server with `MapRazorComponents<App>().AddInteractiveServerRenderMode()`
- .NET 9 Blazor Server patterns with proper antiforgery token integration

### Correct Program.cs Configuration

```csharp
// ✅ CORRECT Pure Blazor Server Configuration
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

app.MapRazorComponents<WitchCityRope.Web.App>()
    .AddInteractiveServerRenderMode();
```

## Render Mode Requirements

### Interactive vs Static Pages

#### Interactive Pages (Forms, User Input)
Pages that require user interaction MUST use interactive server render mode:
```razor
@page "/login"
@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())
```

**Use for:**
- Forms with user input
- Real-time updates
- Interactive UI elements
- Authentication pages

#### Static Pages (Content Display)
Pages that only display content can use default server-side rendering:
```razor
@page "/about"
@* No render mode specified - uses default static rendering *@
```

**Use for:**
- Content-only pages
- Static information display
- Pages without user interaction

### .NET 9 Render Mode Syntax

**✅ REQUIRED Syntax for This Project:**
```razor
@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())
```

**Note**: While `@rendermode InteractiveServer` is valid .NET 9 syntax, this project uses the explicit instantiation syntax for consistency.

### Layout Component Restrictions

**CRITICAL**: Layout components (MainLayout) CANNOT have interactive render modes because they receive RenderFragment parameters that cannot be serialized.

**Solution**: Move interactive behavior to separate components and embed them in the layout.

## New Page Creation Checklist

When creating a new Blazor page, ensure it meets ALL of these requirements:

- [ ] Uses `.razor` extension (not `.cshtml`)
- [ ] Has `@page` directive with proper route
- [ ] Uses `@layout MainLayout` (or appropriate layout)
- [ ] Has `@attribute [Authorize]` for protected pages
- [ ] Uses `@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())` for interactive pages
- [ ] Injects required services with `@inject`
- [ ] Uses `@@media` and `@@keyframes` in CSS (double @)
- [ ] Uses proper string escaping in event handlers
- [ ] Uses WCR validation components in forms
- [ ] Follows feature-based folder structure

## CSS Escape Requirements

Blazor requires escaping @ symbols in CSS:

**❌ WRONG - Causes Compilation Errors:**
```css
<style>
    @media (max-width: 768px) { }
    @keyframes fadeIn { }
</style>
```

**✅ CORRECT - Always Use Double @:**
```css
<style>
    @@media (max-width: 768px) { }
    @@keyframes fadeIn { }
</style>
```

## Authentication State Management

### Use Built-in Components

**❌ WRONG - Custom Authentication Logic:**
```csharp
private UserDto? _currentUser;
@if (_currentUser != null) { ... }
```

**✅ CORRECT - Use AuthorizeView:**
```razor
<AuthorizeView>
    <Authorized>
        <p>Welcome, @context.User.Identity?.Name!</p>
    </Authorized>
    <NotAuthorized>
        <p>Please log in.</p>
    </NotAuthorized>
</AuthorizeView>
```

### Authentication State Provider

Ensure `AuthenticationStateProvider` is properly registered in Program.cs for authentication state to flow correctly between components.

## Form Validation Standards

All forms MUST use WCR validation components for consistency:

```razor
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    
    <WcrInputText @bind-Value="model.Name" 
                  Label="Name" 
                  Placeholder="Enter name" />
    
    <WcrInputNumber @bind-Value="model.Age" 
                    Label="Age" 
                    Min="18" 
                    Max="120" />
    
    <WcrValidationSummary />
    
    <button type="submit">Submit</button>
</EditForm>
```

## Antiforgery Token Handling

**❌ NEVER manually add `<AntiforgeryToken />` to EditForm components**

The framework handles antiforgery tokens automatically when using:
- Proper FormName attribute
- SupplyParameterFromForm attribute
- Correct middleware order: UseAuthentication → UseAuthorization → UseAntiforgery

## Service Communication Pattern

### Web → API Communication

The Web service MUST communicate with the API service for all business operations:

```csharp
// ✅ CORRECT - Web calls API
@inject ApiClient ApiClient

private async Task LoadEvents()
{
    var events = await ApiClient.GetEventsAsync();
}
```

**❌ WRONG - Web directly accessing business data:**
```csharp
// Never inject DbContext or repositories in Web project
@inject WitchCityRopeIdentityDbContext DbContext
```

## File Organization

### Feature-Based Structure

Organize components by feature, not by type:

```
/Features/
├── Admin/
│   ├── Pages/
│   │   ├── Dashboard.razor
│   │   └── UserManagement.razor
│   └── Components/
│       └── UserGrid.razor
├── Events/
│   ├── Pages/
│   │   ├── EventList.razor
│   │   └── EventDetail.razor
│   └── Components/
│       └── EventCard.razor
└── Members/
    └── Pages/
        └── Profile.razor
```

## Common Pitfalls to Avoid

1. **Creating Razor Pages** - This breaks the pure Blazor Server architecture
2. **Using old render mode syntax** - Always use .NET 9 syntax
3. **Adding render modes to layouts** - Layouts cannot be interactive
4. **Direct database access from Web** - Always go through the API
5. **Forgetting CSS escaping** - Double @ is required in Blazor
6. **Manual authentication state** - Use AuthorizeView components
7. **Custom form validation** - Use WCR components for consistency

## Testing Your Implementation

Run the architecture validation script to check for common issues:
```bash
./scripts/validate-blazor-architecture.sh
```

## Additional Resources

- [Microsoft Blazor Server Documentation](https://docs.microsoft.com/blazor)
- [.NET 9 Blazor Updates](https://docs.microsoft.com/dotnet/core/whats-new/dotnet-9)
- [ASP.NET Core Authentication](https://docs.microsoft.com/aspnet/core/security/authentication)