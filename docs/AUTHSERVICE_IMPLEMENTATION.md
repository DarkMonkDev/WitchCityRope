# AuthService Implementation Summary

## Overview
Fixed the `GetCurrentUserAsync()` method in the AuthService and added the AuthenticationStateChanged event to notify components when authentication state changes.

## Changes Made

### 1. IAuthService Interface (`/src/WitchCityRope.Web/Services/IAuthService.cs`)
- Added `AuthenticationStateChanged` event:
  ```csharp
  event EventHandler<bool>? AuthenticationStateChanged;
  ```

### 2. AuthService Implementation (`/src/WitchCityRope.Web/Services/AuthService.cs`)

#### Added AuthenticationStateChanged Event
- Added the event declaration
- Subscribe to the underlying AuthenticationService's event in the constructor
- Forward events from AuthenticationService to AuthService consumers
- Raise the event on successful login and logout

#### Implemented GetCurrentUserAsync()
The method now:
1. Gets the authentication state from AuthenticationService
2. Returns null if user is not authenticated
3. Calls AuthenticationService.GetCurrentUserAsync() to get UserInfo
4. Maps UserInfo to UserDto with proper field mappings:
   - Maps FirstName to SceneName (temporary mapping)
   - Combines FirstName and LastName for DisplayName
   - Sets IsAdmin based on "Admin" role
   - Sets IsVetted based on "VettedMember", "Admin", or "Teacher" roles
   - Includes all roles from UserInfo
   - Sets placeholder values for dates (to be retrieved from API later)

### 3. MainLayout.razor Updates
- Implemented IDisposable interface
- Subscribe to AuthenticationStateChanged event in OnInitializedAsync
- Added event handler to refresh user data when authentication changes
- Properly unsubscribe from the event in Dispose method

### 4. Test Page Updates (`/src/WitchCityRope.Web/Pages/TestAuth.razor`)
- Added UserDto display section to show the result of GetCurrentUserAsync()
- Subscribe to AuthenticationStateChanged event
- Added refresh button to manually test GetCurrentUserAsync()
- Implemented IDisposable to properly clean up event subscriptions

## Usage Example

```csharp
// Get current user
var user = await AuthService.GetCurrentUserAsync();
if (user != null)
{
    Console.WriteLine($"User: {user.Email}, Admin: {user.IsAdmin}");
}

// Subscribe to authentication changes
AuthService.AuthenticationStateChanged += (sender, isAuthenticated) =>
{
    if (isAuthenticated)
    {
        // User logged in
    }
    else
    {
        // User logged out
    }
};
```

## Future Improvements

1. **API Integration**: The current implementation uses placeholder values for some fields. These should be retrieved from the API:
   - TwoFactorEnabled
   - CreatedAt
   - LastLoginAt

2. **SceneName Mapping**: Currently using FirstName as SceneName. This should be properly mapped from the user's actual scene name field when available.

3. **Error Handling**: The current implementation logs errors but returns null. Consider adding more detailed error information if needed.

## Testing

Created unit tests in `/tests/WitchCityRope.Web.Tests/Services/AuthServiceTests.cs` to verify:
- GetCurrentUserAsync returns null when not authenticated
- GetCurrentUserAsync returns proper UserDto when authenticated
- AuthenticationStateChanged event is raised on login
- AuthenticationStateChanged event is raised on logout

The test page at `/test-auth` can be used to manually verify the implementation.