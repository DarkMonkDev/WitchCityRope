# Lessons Learned - UI Developers
<!-- Last Updated: 2025-08-04 -->
<!-- Next Review: 2025-09-04 -->

## Blazor Server Specific

### Authentication in Blazor Components
**Issue**: SignInManager operations fail with "Headers are read-only" error  
**Solution**: Never use SignInManager directly in Blazor components. Create API endpoints for auth operations.
```csharp
// ❌ WRONG - Will fail in Blazor component
await SignInManager.PasswordSignInAsync(email, password, false, false);

// ✅ CORRECT - Call API endpoint
var response = await Http.PostAsJsonAsync("/api/auth/login", loginRequest);
```
**Applies to**: All authentication operations (login, logout, register)

### Render Modes
**Issue**: Forms not submitting, buttons not working  
**Solution**: Interactive pages MUST specify render mode
```razor
@* ❌ WRONG - No interactivity *@
@page "/form"

@* ✅ CORRECT - Interactive server mode *@
@page "/form"
@rendermode InteractiveServer
```
**Applies to**: Any page with forms, buttons, or user input

### Hot Reload in Docker
**Issue**: Changes not reflected when running in Docker  
**Solution**: Restart container when hot reload fails
```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml restart web
# OR use the helper script:
./restart-web.sh
```
**Applies to**: Layout changes, authentication changes, route changes

## Syncfusion Components

### License Key Configuration
**Issue**: "License key not found" errors  
**Solution**: Register in Program.cs before builder.Build()
```csharp
// Must be before builder.Build()
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR_KEY");
```
**Applies to**: Initial setup only

### Data Grid Binding
**Issue**: Grid shows "No records to display" even with data  
**Solution**: Ensure IQueryable or List, not IEnumerable
```csharp
// ❌ WRONG
IEnumerable<Event> events = await GetEvents();

// ✅ CORRECT  
List<Event> events = await GetEvents().ToListAsync();
```
**Applies to**: All Syncfusion data components

## WCR Custom Components

### Validation Components
**Issue**: Validation not triggering on custom inputs  
**Solution**: Use EventCallback<T> with proper type
```razor
@* Component definition *@
@typeparam TValue
[Parameter] public EventCallback<TValue> ValueChanged { get; set; }

@* Usage *@
<WcrInputText @bind-Value="model.Email" />
```
**Applies to**: WcrInputText, WcrInputNumber, WcrInputDate

### Form Submission
**Issue**: EditForm not recognizing WCR components  
**Solution**: Ensure components implement proper binding
```razor
@* Must have both Value and ValueChanged *@
[Parameter] public string Value { get; set; }
[Parameter] public EventCallback<string> ValueChanged { get; set; }
```
**Applies to**: All custom form components

## CSS and Styling

### Scoped CSS Not Applied
**Issue**: Component.razor.css styles not working  
**Solution**: Ensure ::deep for child components
```css
/* ❌ WRONG - Won't apply to child components */
.my-style { }

/* ✅ CORRECT - Applies to nested components */
::deep .my-style { }
```
**Applies to**: Blazor scoped CSS files

### Missing Styles After Migration
**Issue**: Styles lost when moving from Razor Pages  
**Solution**: Import CSS in App.razor head section
```html
<head>
    <link rel="stylesheet" href="css/site.css" />
    <link rel="stylesheet" href="_content/Syncfusion.Blazor/styles/bootstrap5.css" />
</head>
```
**Applies to**: Pure Blazor Server apps

## Common Pitfalls

### Navigation After Form Submit
**Issue**: NavigateTo not working after form submission  
**Solution**: Ensure it's called after async operations complete
```csharp
// ❌ WRONG
var task = AuthService.LoginAsync(model);
NavigationManager.NavigateTo("/dashboard");

// ✅ CORRECT
await AuthService.LoginAsync(model);
NavigationManager.NavigateTo("/dashboard");
```

### Dependency Injection Scope
**Issue**: Scoped services acting like singletons  
**Solution**: Don't inject DbContext directly in Blazor components
```csharp
// ❌ WRONG - DbContext in component
@inject WitchCityRopeDbContext DbContext

// ✅ CORRECT - Use service layer
@inject IEventService EventService
```

## Docker Development Tips

### File Changes Not Detected
**When to restart container:**
- After changing Program.cs
- After modifying _Imports.razor  
- After adding new services
- When authentication logic changes
- After route configuration changes

### Port Conflicts
**Issue**: "Port already in use" errors  
**Solution**: Check for orphaned containers
```bash
docker ps -a | grep witchcity
docker rm -f [container-id]
```

---

## Deprecated Practices (Don't Use These)

1. **MudBlazor** - Project uses Syncfusion exclusively
2. **Razor Pages** - Pure Blazor Server only
3. **SQL Server** - PostgreSQL only
4. **In-memory database for tests** - Use TestContainers

---

*Last major update: Removed all MudBlazor references after full migration to Syncfusion*