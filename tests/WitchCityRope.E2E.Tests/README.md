# WitchCityRope E2E Tests

This project contains end-to-end tests for the WitchCityRope application using Playwright.

## Prerequisites

- .NET 9.0 SDK
- SQL Server (LocalDB or full instance)
- Node.js (for Playwright)

## Setup

1. Install Playwright browsers:
```bash
dotnet build
```

2. Configure test settings in `appsettings.json` or environment-specific files.

3. Ensure the application is running on the configured `BaseUrl`.

## Running Tests

### Run all tests:
```bash
dotnet test
```

### Run specific test category:
```bash
dotnet test --filter "Category=UserJourneys"
```

### Run with specific browser:
```bash
dotnet test -- MSTest.Parallelize.Workers=1 -- BrowserType=firefox
```

### Run in headed mode (see browser):
```bash
dotnet test -- Headless=false
```

## Test Structure

- **Infrastructure/** - Base test classes and configuration
- **PageObjects/** - Page Object Model implementations
- **Tests/** - Test classes organized by feature area:
  - **UserJourneys/** - Complete user flow tests
  - **Authentication/** - Auth-specific tests
  - **Events/** - Event management tests
  - **Vetting/** - Vetting process tests
  - **Visual/** - Visual regression tests
- **Fixtures/** - Test data management and database setup
- **Helpers/** - Utility functions

## CI/CD Configuration

For CI/CD pipelines, use the `appsettings.CI.json` configuration:

```bash
ASPNETCORE_ENVIRONMENT=CI dotnet test
```

## Visual Testing

Visual regression tests create baseline images on first run. Subsequent runs compare against these baselines.

Baselines are stored in `visual-baselines/`.
Differences are saved to `visual-diffs/`.

## Test Data Management

- Tests create their own test data using `TestDataManager`
- Data is cleaned up after each test (configurable)
- Use the `TestUserPrefix` to identify test users

## Debugging Failed Tests

1. Screenshots are automatically taken on failure
2. Check `screenshots/` directory
3. Enable trace recording in settings for detailed debugging
4. Run tests in headed mode to see the browser

## Best Practices

1. Keep tests independent - each test should set up its own data
2. Use Page Object Model for maintainability
3. Wait for specific elements rather than arbitrary timeouts
4. Clean up test data to avoid conflicts
5. Use meaningful test names that describe the scenario

## Troubleshooting

### Tests fail with "element not found"
- Check if selectors have changed in the application
- Ensure the application is running and accessible
- Verify test data setup is correct

### Database connection errors
- Check connection string in appsettings
- Ensure database server is running
- Verify migrations are applied

### Browser launch failures
- Run `dotnet build` to ensure Playwright browsers are installed
- Check system requirements for the browser
- Try running with a different browser type