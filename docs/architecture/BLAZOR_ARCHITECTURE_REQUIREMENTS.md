# Blazor Server Architecture Requirements

## 🚨 CRITICAL: Pure Blazor Server Architecture

This project has been migrated to **PURE BLAZOR SERVER** architecture. Any new pages MUST follow these requirements.

## Required Architecture Patterns

### 1. Blazor Components (.razor files) NOT Razor Pages (.cshtml)
```csharp
// ✅ CORRECT - Blazor Component
@page "/member/profile"
@layout MainLayout
@attribute [Authorize]
@using Microsoft.AspNetCore.Authorization
@inject IUserService UserService
@inject NavigationManager Navigation

<h1>Member Profile</h1>
// Interactive content here

@code {
    // Component logic here
}
```

```csharp
// ❌ WRONG - Razor Page (DO NOT CREATE)
@page "/member/profile"
@model ProfilePageModel
// This is old Razor Pages - DO NOT USE
```

### 2. Interactive Render Modes for User Input
```csharp
// ✅ CORRECT - Interactive pages that handle user input
@page "/member/settings"
@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())
@attribute [Authorize]

<EditForm Model="@settings" OnValidSubmit="SaveSettings">
    <WcrInputText @bind-Value="settings.Email" />
    <button type="submit">Save</button>
</EditForm>

@code {
    private async Task SaveSettings() {
        // Handle form submission
    }
}
```

### 3. Authentication Using Blazor Patterns
```csharp
// ✅ CORRECT - Use AuthorizeView and [Authorize] attribute
@attribute [Authorize]
@using Microsoft.AspNetCore.Authorization

<AuthorizeView>
    <Authorized>
        <p>Hello @context.User.Identity.Name!</p>
    </Authorized>
    <NotAuthorized>
        <p>Please log in</p>
    </NotAuthorized>
</AuthorizeView>
```

### 4. Service Injection Pattern
```csharp
// ✅ CORRECT - Blazor service injection
@inject IUserService UserService
@inject NavigationManager Navigation
@inject IToastService ToastService
@inject AuthenticationStateProvider AuthenticationStateProvider

@code {
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        // Component initialization
    }
}
```

## CSS Escaping Rules

### 🚨 CRITICAL: Always Use @@ for CSS in Razor
```css
/* ✅ CORRECT - Double @ for CSS rules in Blazor components */
<style>
    @@media (max-width: 768px) {
        .container { padding: 1rem; }
    }
    
    @@keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
    }
</style>
```

```css
/* ❌ WRONG - Single @ causes compilation errors */
<style>
    @media (max-width: 768px) {  /* ERROR: @ conflicts with Razor syntax */
        .container { padding: 1rem; }
    }
</style>
```

## String Escaping in Event Handlers

### Button Click Handlers
```csharp
// ✅ CORRECT - Proper string escaping
<button @onclick='() => SetActiveTab("profile")'>Profile</button>
<button @onclick="() => SetActiveTab(\"settings\")">Settings</button>

// ❌ WRONG - Improper escaping causes compilation errors
<button @onclick="() => SetActiveTab("profile")">Profile</button>
```

## Component Structure Requirements

### File Organization
```
/src/WitchCityRope.Web/
├── Features/
│   ├── Public/Pages/           # Public pages (no auth required)
│   │   ├── About.razor
│   │   ├── Privacy.razor
│   │   └── Terms.razor
│   ├── Members/Pages/          # Member pages (auth required)
│   │   ├── Profile.razor
│   │   ├── Settings.razor
│   │   └── Tickets.razor
│   └── Admin/Pages/            # Admin pages (admin role required)
└── Shared/
    ├── Layouts/
    └── Components/
```

### Page Header Pattern
```csharp
// ✅ CORRECT - Every page should follow this pattern
@page "/member/profile"
@layout MainLayout
@attribute [Authorize]
@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())
@using Microsoft.AspNetCore.Authorization
@inject IUserService UserService
@inject NavigationManager Navigation
@inject IToastService ToastService

<PageTitle>Member Profile - Witch City Rope</PageTitle>

// Page content here

@code {
    // Component logic here
}
```

## Common Mistakes to Avoid

### 1. Don't Mix Razor Pages with Blazor Components
```csharp
// ❌ WRONG - Don't use Razor Pages patterns
@page "/profile"
@model ProfileModel
@{
    ViewData["Title"] = "Profile";
}

// ✅ CORRECT - Use Blazor component patterns
@page "/profile"
@code {
    private string title = "Profile";
}
```

### 2. Don't Use Server-Side Rendering for Interactive Pages
```csharp
// ❌ WRONG - No render mode for interactive pages
@page "/settings"
<form> // Server-side form - no interactivity
</form>

// ✅ CORRECT - Interactive render mode for user input
@page "/settings"
@rendermode @(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode())
<EditForm Model="@model" OnValidSubmit="HandleSubmit">
    // Interactive form
</EditForm>
```

### 3. Don't Use Manual Authorization Checks
```csharp
// ❌ WRONG - Manual authorization checks
@code {
    protected override async Task OnInitializedAsync()
    {
        var user = await UserService.GetCurrentUserAsync();
        if (user == null) {
            Navigation.NavigateTo("/login");
        }
    }
}

// ✅ CORRECT - Use [Authorize] attribute
@attribute [Authorize]
@code {
    // Component automatically handles authorization
}
```

## Validation and Form Patterns

### Use WCR Validation Components
```csharp
// ✅ CORRECT - Use project's validation components
<EditForm Model="@model" OnValidSubmit="HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />
    
    <WcrInputText @bind-Value="model.Email" Label="Email" IsRequired="true" />
    <WcrInputPassword @bind-Value="model.Password" Label="Password" IsRequired="true" />
    <WcrInputSelect @bind-Value="model.Country" Label="Country">
        <option value="US">United States</option>
        <option value="CA">Canada</option>
    </WcrInputSelect>
    
    <button type="submit" class="btn btn-primary">Submit</button>
</EditForm>
```

## Testing Requirements

### Integration Tests Expect Blazor Components
```csharp
// Integration tests expect:
// - Proper route handling (/member/profile returns 200 OK)
// - Authentication redirects for protected routes
// - Interactive components respond to user input
```

## Migration Checklist

When creating new pages, ensure:
- [ ] Uses `.razor` extension (not `.cshtml`)
- [ ] Has `@page` directive with proper route
- [ ] Uses `@layout MainLayout`
- [ ] Has `@attribute [Authorize]` for protected pages
- [ ] Uses `@rendermode` for interactive pages
- [ ] Injects required services with `@inject`
- [ ] Uses `@@media` and `@@keyframes` in CSS
- [ ] Uses proper string escaping in event handlers
- [ ] Uses WCR validation components in forms
- [ ] Follows feature-based folder structure

## Prevention Tools

### 1. Build-Time Validation
Add to CI/CD pipeline:
```bash
# Check for CSS escaping issues
grep -r "@media\|@keyframes" src/**.razor && echo "ERROR: Use @@media and @@keyframes in Razor files"

# Check for Razor Pages patterns
grep -r "@model\|@{" src/**.razor && echo "ERROR: Don't use Razor Pages patterns in Blazor components"
```

### 2. Code Review Checklist
- [ ] No single `@` in CSS rules
- [ ] No `@model` or `@{}` blocks
- [ ] Uses `@attribute [Authorize]` not manual auth checks
- [ ] Interactive pages have render modes
- [ ] Proper service injection patterns

## Why This Matters

1. **Authentication**: Blazor Server handles auth state properly
2. **Interactivity**: Only Blazor components support true interactivity
3. **Performance**: Server-side rendering with client-side interactivity
4. **Consistency**: All pages follow the same patterns
5. **Testing**: Integration tests expect Blazor routing behavior

## Quick Reference

- **Public Pages**: No auth, can be static or interactive
- **Member Pages**: Require `[Authorize]`, usually interactive
- **Admin Pages**: Require admin role, always interactive
- **CSS**: Always use `@@media`, `@@keyframes`
- **Forms**: Use WCR validation components
- **Navigation**: Use `NavigationManager.NavigateTo()`