# User Menu Integration Tests

This document describes the integration tests for the WitchCityRope user menu functionality.

## Test Files

### 1. UserMenuIntegrationTests.cs
Main test file covering user menu functionality:
- Unauthenticated state (login button visibility)
- Authenticated state (user menu display)
- Admin user features
- Logout functionality
- Accessibility attributes

### 2. ProtectedRouteNavigationTests.cs
Comprehensive route protection tests:
- Public route accessibility
- Authentication requirements for protected routes
- Admin-only route restrictions
- Return URL preservation
- Multi-role access scenarios

### 3. Helpers/AuthenticationTestHelper.cs
Reusable authentication helper for tests:
- User creation and authentication
- Token management
- Role assignment (Admin, Vetted)
- Batch user creation for different test scenarios

## Test Coverage

### 1. Unauthenticated Users
- ✅ Login button is displayed in header
- ✅ Login button is displayed in mobile menu
- ✅ User menu is not visible
- ✅ Protected routes redirect to login
- ✅ Public routes are accessible

### 2. Authenticated Users
- ✅ User menu replaces login button
- ✅ User avatar is displayed
- ✅ Username (scene name) is shown
- ✅ Dropdown menu contains:
  - My Profile
  - My Tickets
  - Settings
  - Logout
- ✅ Mobile menu shows user info
- ✅ Protected routes are accessible

### 3. Admin Users
- ✅ Admin Panel link appears in dropdown
- ✅ Admin Panel link appears in mobile menu
- ✅ Admin routes are accessible
- ✅ Regular users cannot see admin options
- ✅ Regular users cannot access admin routes

### 4. Logout Functionality
- ✅ Logout clears authentication
- ✅ Redirects to home page
- ✅ Login button reappears
- ✅ Protected routes become inaccessible

### 5. Navigation to Protected Routes
- ✅ Unauthenticated users redirect to login
- ✅ Return URL is preserved in redirect
- ✅ After login, users return to requested page
- ✅ Admin routes require admin role
- ✅ Vetted user routes require vetting status

## Running the Tests

### Run all user menu tests:
```bash
dotnet test --filter "FullyQualifiedName~UserMenuIntegrationTests"
```

### Run protected route tests:
```bash
dotnet test --filter "FullyQualifiedName~ProtectedRouteNavigationTests"
```

### Run specific test categories:
```bash
# Unauthenticated state tests
dotnet test --filter "FullyQualifiedName~UnauthenticatedUsers"

# Admin functionality tests
dotnet test --filter "FullyQualifiedName~AdminUsers"

# Logout tests
dotnet test --filter "FullyQualifiedName~LogoutFunctionality"
```

## Test Data

### Default Test Users
- **Regular User**: testuser@example.com / StrongPassword123!
- **Admin User**: admin@example.com / StrongPassword123!
- **Vetted User**: vetted@test.com / StrongPassword123!
- **Multi-role User**: vettedadmin@test.com / StrongPassword123!

## Key Patterns

### 1. Authentication Helper Usage
```csharp
var user = await _authHelper.CreateAndAuthenticateUserAsync(
    email: "test@example.com",
    isAdmin: true
);
_authHelper.SetAuthorizationHeader(user.Token);
```

### 2. HTML Parsing with AngleSharp
```csharp
var document = await GetHtmlDocumentAsync(response);
var userMenu = document.QuerySelector(".user-menu");
```

### 3. Route Testing Pattern
```csharp
[Theory]
[MemberData(nameof(GetProtectedRoutes))]
public async Task ProtectedRoutes_RequireAuthentication(string route)
{
    // Test implementation
}
```

## Accessibility Considerations

All tests verify proper ARIA attributes:
- `aria-haspopup="true"` on dropdown triggers
- `aria-expanded` state management
- `role="menu"` on dropdowns
- `role="menuitem"` on menu items

## Mobile Responsiveness

Tests cover both desktop and mobile views:
- Desktop: User menu dropdown
- Mobile: Hamburger menu with user info section
- Consistent authentication state across viewports

## Future Enhancements

Consider adding tests for:
- [ ] Profile picture upload and display
- [ ] Real-time notification badges
- [ ] User preferences persistence
- [ ] Session timeout handling
- [ ] Remember me functionality
- [ ] Two-factor authentication flows