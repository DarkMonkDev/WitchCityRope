# Admin Role Authorization Issue - Critical Bug

## Issue Description
The admin dashboard link is not appearing in the user dropdown menu after logging in with the admin account (admin@witchcityrope.com).

## Current Status
- **Severity**: High
- **Impact**: Admin users cannot access admin functionality through the UI
- **Workaround**: Direct navigation to /admin URLs works if logged in

## Investigation Results

### Code Analysis ✅
The code implementation is correct:
1. `UserMenuComponent.razor` checks `_currentUser.IsAdmin` (line 21)
2. `IdentityAuthService.cs` sets `IsAdmin = roles.Contains("Administrator") || roles.Contains("Admin")` (line 74)
3. Database seeding creates "Administrator" role and assigns it to admin user

### Root Cause Suspects
1. **Roles not loading from database** - Most likely issue
2. **Blazor state not updating** - Component may not refresh after auth
3. **Caching issue** - User info cached without roles
4. **Database not properly seeded** - Roles might be missing

## Debugging Steps

### 1. Verify Database State
```bash
# Connect to PostgreSQL
docker exec -it witchcity-postgres psql -U postgres -d witchcityrope

# Check roles exist
SELECT * FROM "AspNetRoles";
# Should show: Administrator, Moderator, Organizer, Member, Attendee

# Check admin user has role
SELECT u."Email", r."Name" 
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON u."Id" = ur."UserId"
JOIN "AspNetRoles" r ON ur."RoleId" = r."Id"
WHERE u."Email" = 'admin@witchcityrope.com';
# Should show: admin@witchcityrope.com | Administrator
```

### 2. Run Database Seeder
```bash
cd /home/chad/repos/witchcityrope/WitchCityRope/tools/DatabaseSeeder
dotnet run
```

### 3. Add Debug Logging
Add to `IdentityAuthService.cs` in `GetCurrentUserAsync()`:
```csharp
var roles = await _userManager.GetRolesAsync(user);
_logger.LogInformation($"User {user.Email} has roles: {string.Join(", ", roles)}");
```

### 4. Force Component Refresh
After login, navigate away and back to force refresh:
```csharp
// In login success handler
NavigationManager.NavigateTo("/", true); // Force reload
```

## Quick Fixes to Try

### Option 1: Force Role Reload
In `UserMenuComponent.razor` `OnInitializedAsync()`:
```csharp
protected override async Task OnInitializedAsync()
{
    // Force fresh load of user data
    _currentUser = await AuthService.GetCurrentUserAsync();
    if (_currentUser.IsAuthenticated)
    {
        // Log for debugging
        Console.WriteLine($"User IsAdmin: {_currentUser.IsAdmin}");
    }
}
```

### Option 2: Add StateHasChanged
After getting user data:
```csharp
_currentUser = await AuthService.GetCurrentUserAsync();
await InvokeAsync(StateHasChanged);
```

### Option 3: Check Identity Configuration
In `Program.cs`, ensure roles are enabled:
```csharp
builder.Services.AddDefaultIdentity<WitchCityRopeUser>(options => {
    // ... existing options
})
.AddRoles<IdentityRole>() // Ensure this is present
.AddEntityFrameworkStores<WitchCityRopeIdentityDbContext>();
```

## Testing the Fix

1. Clear browser cookies/cache
2. Stop and restart the application
3. Login with admin@witchcityrope.com / Test123!
4. Check browser console for any errors
5. Inspect the user dropdown - should show "Admin Dashboard" option

## Related Files
- `/src/WitchCityRope.Web/Shared/Components/UserMenuComponent.razor`
- `/src/WitchCityRope.Web/Services/IdentityAuthService.cs`
- `/src/WitchCityRope.Web/Program.cs`
- `/tools/DatabaseSeeder/Seeders/20250120_InitialSeed.cs`

## E2E Test
The test at `/tests/playwright/ui/user-dropdown.spec.ts` line 165 specifically tests for the admin dashboard link visibility and is currently failing.