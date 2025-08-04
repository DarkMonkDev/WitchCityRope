# Obsolete Tests Explanation

## Summary
The following test files were removed as they tested Blazor components that no longer exist in the Web project:

### Removed Test Files:
1. `Features/Auth/LoginComponentTests.cs` - Tested Login.razor component
2. `Features/Auth/RegisterComponentTests.cs` - Tested Register.razor component  
3. `Features/Auth/TwoFactorAuthComponentTests.cs` - Tested TwoFactorAuth.razor component
4. `Shared/Components/Navigation/MainNavTests.cs` - Tested MainNav.razor component

## Reason for Removal
These Blazor components have been replaced by ASP.NET Core Identity pages located in `Areas/Identity/Pages/Account/`. The authentication flow is now handled by:

- **Login functionality**: `/Areas/Identity/Pages/Account/Login.cshtml`
- **Registration functionality**: `/Areas/Identity/Pages/Account/Register.cshtml`
- **Two-factor authentication**: `/Areas/Identity/Pages/Account/Manage/TwoFactorAuthentication.cshtml`
- **Navigation**: Handled by Razor partial views rather than Blazor components

## What Should Replace These Tests

### Integration Tests
The authentication flow should now be tested through integration tests that:
- Test the full HTTP request/response cycle
- Verify Identity page behavior
- Test authentication cookies and tokens
- Validate form submissions and redirects

These tests already exist in:
- `WitchCityRope.IntegrationTests/AuthenticationTests.cs`
- `WitchCityRope.E2E.Tests/Tests/Authentication/AuthenticationFlowTests.cs`
- `WitchCityRope.E2E.Tests/Tests/Authentication/TwoFactorAuthTests.cs`

### UI/E2E Tests
End-to-end tests using Playwright or similar tools should verify:
- User registration flow
- Login/logout functionality
- Two-factor authentication setup and usage
- Navigation menu behavior based on authentication state

These tests are located in:
- `WitchCityRope.E2E.Tests/Tests/UserJourneys/UserRegistrationFlowTests.cs`

### Component Tests
If any Blazor components still exist in the Web project that require testing, they should have corresponding test files in the `WitchCityRope.Web.Tests` project following the same pattern as the removed tests.

## Migration Notes
When Identity pages need testing, consider:
1. Using the ASP.NET Core Test Framework with `WebApplicationFactory`
2. Testing at the HTTP level rather than component level
3. Verifying authentication state through cookies/claims
4. Testing form anti-forgery tokens
5. Validating Identity-specific features like account lockout, email confirmation, etc.

## References
- [ASP.NET Core Identity Testing Documentation](https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [Testing Razor Pages](https://docs.microsoft.com/en-us/aspnet/core/test/razor-pages-tests)