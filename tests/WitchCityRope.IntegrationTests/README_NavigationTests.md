# Navigation Link Integration Tests

This directory contains comprehensive integration tests to verify all navigation links work correctly throughout the WitchCityRope application.

## Test Coverage

### 1. NavigationLinksTests.cs
Tests basic navigation functionality:
- Main navigation bar links (Home, Events, Dashboard, etc.)
- Authentication-required links
- Admin section links
- Profile section links
- Landing page CTA buttons and links
- Event page navigation

### 2. BlazorNavigationTests.cs
Tests Blazor-specific navigation features:
- Client-side routing
- Navigation component structure
- Route accessibility
- Navigation consistency across pages
- Blazor-specific click handlers

### 3. DeepLinkValidationTests.cs
Advanced recursive link validation:
- Crawls links up to 3 levels deep
- Validates all discovered links
- Detects broken links (404s, 500s)
- Distinguishes between broken links and auth-required links
- Generates comprehensive reports

### 4. HtmlNavigationTests.cs
Precise HTML parsing using HtmlAgilityPack:
- Validates HTML structure
- Checks accessibility attributes
- Verifies semantic HTML usage
- Tests responsive navigation elements
- Validates image links

## Running the Tests

### Run all navigation tests:
```bash
dotnet test --filter "FullyQualifiedName~NavigationLinksTests|FullyQualifiedName~BlazorNavigationTests|FullyQualifiedName~DeepLinkValidationTests|FullyQualifiedName~HtmlNavigationTests"
```

### Run specific test class:
```bash
# Basic navigation tests
dotnet test --filter "FullyQualifiedName~NavigationLinksTests"

# Blazor navigation tests
dotnet test --filter "FullyQualifiedName~BlazorNavigationTests"

# Deep link validation
dotnet test --filter "FullyQualifiedName~DeepLinkValidationTests"

# HTML parsing tests
dotnet test --filter "FullyQualifiedName~HtmlNavigationTests"
```

### Run with detailed output:
```bash
dotnet test --logger "console;verbosity=detailed" --filter "FullyQualifiedName~NavigationTests"
```

## Test Requirements

The tests require:
1. The application to be running (tests use `WebApplicationFactory`)
2. Test data may be needed for event-related tests
3. Authentication is mocked for protected routes

## What These Tests Catch

1. **Broken Links**: Any link returning 404 or 5xx errors
2. **Malformed URLs**: Links with invalid formats
3. **Missing Navigation Elements**: Required navigation items not present
4. **Inconsistent Navigation**: Navigation that changes unexpectedly between pages
5. **Accessibility Issues**: Missing ARIA labels, non-semantic HTML
6. **Mobile Navigation Issues**: Problems with responsive menu elements
7. **Event Card Navigation**: Issues with dynamic event links
8. **Authentication Flow**: Problems with login/protected route redirects

## Interpreting Test Results

### Success
All links are accessible and return expected status codes:
- 200 OK for public pages
- 302 Redirect or 401 Unauthorized for protected pages
- Proper HTML structure with all required elements

### Failures
Common failure scenarios:
- **404 Not Found**: Route doesn't exist or is misconfigured
- **500 Internal Server Error**: Server-side error when accessing route
- **Missing Elements**: Required navigation elements not found in HTML
- **Broken Event Links**: Dynamic event IDs not properly handled

## Adding New Tests

When adding new navigation features:
1. Add test cases to verify the new links work
2. Update deep link validation if new sections are added
3. Ensure HTML structure tests cover new navigation elements
4. Test both authenticated and unauthenticated scenarios

## CI/CD Integration

These tests should run in your CI/CD pipeline to catch navigation issues before deployment:

```yaml
# Example GitHub Actions step
- name: Run Navigation Tests
  run: dotnet test --filter "Category=Navigation" --logger "trx;LogFileName=navigation-tests.trx"
```

## Troubleshooting

### Tests fail with "Connection refused"
- Ensure the test server is starting correctly
- Check if all required services are registered

### Event links show as broken
- Ensure test data includes sample events
- Check if event IDs are properly formatted

### Authentication tests fail
- Verify authentication middleware is configured for test environment
- Check if test authentication provider is working

## Best Practices

1. **Keep tests independent**: Each test should work in isolation
2. **Use descriptive names**: Test names should clearly indicate what they verify
3. **Log useful information**: Use `ITestOutputHelper` to provide context
4. **Test edge cases**: Empty states, error conditions, etc.
5. **Maintain test data**: Keep test data realistic and up-to-date