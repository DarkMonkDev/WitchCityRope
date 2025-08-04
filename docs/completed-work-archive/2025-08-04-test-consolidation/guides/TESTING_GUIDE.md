# WitchCityRope Testing Guide

## Quick Start

### Running All Tests
```bash
# Linux/Mac
./run-tests.sh

# Windows
run-tests.cmd

# Or using dotnet directly
dotnet test
```

### Running Specific Test Categories
```bash
# Unit tests only (fast)
dotnet test --filter "Category=Unit"

# Integration tests only (slower, requires Docker)
dotnet test --filter "Category=Integration"

# Tests for a specific feature
dotnet test --filter "FullyQualifiedName~Event"
```

### Running Tests with Coverage
```bash
# Generate coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Install report generator if needed
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"**/coverage.opencover.xml" -targetdir:"coverage/report"
```

## Test Organization

### Project Structure
```
tests/
├── WitchCityRope.Tests.Common/       # Shared test utilities
├── WitchCityRope.Core.Tests/         # Domain logic tests
├── WitchCityRope.Api.Tests/          # API service tests
├── WitchCityRope.Infrastructure.Tests/ # Database & infrastructure tests
└── WitchCityRope.Web.Tests/          # Blazor component tests
```

### Test Categories
- **Unit**: Fast, isolated tests with no external dependencies
- **Integration**: Tests that use real databases or external services
- **Component**: Blazor component rendering and interaction tests
- **E2E**: End-to-end user journey tests (future)

## Writing New Tests

### 1. Unit Test Example
```csharp
public class UserTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesUser()
    {
        // Arrange
        var email = EmailAddress.Create("test@example.com");
        var sceneName = SceneName.Create("TestUser");

        // Act
        var user = new User(email, sceneName, "hashedPassword");

        // Assert
        user.Should().NotBeNull();
        user.Email.Should().Be(email);
        user.SceneName.Should().Be(sceneName);
    }
}
```

### 2. Integration Test Example
```csharp
public class EventServiceIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateEvent_WithValidData_PersistsToDatabase()
    {
        // Arrange
        using var context = CreateDbContext();
        var service = new EventService(context);
        var request = new CreateEventRequest { Title = "Test Event" };

        // Act
        var result = await service.CreateEventAsync(request);

        // Assert
        result.Should().BeSuccessful();
        var saved = await context.Events.FindAsync(result.EventId);
        saved.Should().NotBeNull();
        saved.Title.Should().Be("Test Event");
    }
}
```

### 3. Component Test Example
```csharp
public class LoginComponentTests : ComponentTestBase
{
    [Fact]
    public void LoginForm_ClickSubmit_CallsAuthService()
    {
        // Arrange
        var authService = new Mock<IAuthService>();
        var component = RenderComponent<Login>(
            builder => builder.Add(p => p.AuthService, authService.Object));

        // Act
        component.Find("input[type='email']").Change("test@example.com");
        component.Find("input[type='password']").Change("password");
        component.Find("button[type='submit']").Click();

        // Assert
        authService.Verify(x => x.LoginAsync(
            It.Is<string>(e => e == "test@example.com"),
            It.IsAny<string>()), Times.Once);
    }
}
```

## Test Data Builders

Use builders for creating test data:

```csharp
// Create a test user
var user = new UserBuilder()
    .WithEmail("test@example.com")
    .WithSceneName("TestUser")
    .AsAdmin()
    .WithVerifiedEmail()
    .Build();

// Create a test event
var event = new EventBuilder()
    .WithTitle("Monthly Rope Jam")
    .WithCapacity(50)
    .WithPrice(25.00m)
    .StartsIn(7.DaysFromNow())
    .Build();
```

## Running Tests in CI/CD

### GitHub Actions
```yaml
- name: Run Tests
  run: dotnet test --logger "trx;LogFileName=test-results.trx"

- name: Publish Test Results
  uses: EnricoMi/publish-unit-test-result-action@v2
  if: always()
  with:
    files: '**/test-results.trx'
```

### Azure DevOps
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run Tests'
  inputs:
    command: 'test'
    projects: '**/*.Tests.csproj'
    arguments: '--collect:"XPlat Code Coverage"'
```

## Debugging Tests

### Visual Studio
1. Right-click on test method → Debug Test
2. Set breakpoints as needed
3. Use Test Explorer for running/debugging

### VS Code
1. Install C# extension
2. Click "Debug Test" above test methods
3. Use .NET Test Explorer extension

### Command Line
```bash
# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName=WitchCityRope.Core.Tests.UserTests.Constructor_WithValidData_CreatesUser"
```

## Performance Guidelines

### Keep Tests Fast
- Use in-memory databases for unit tests
- Mock external services
- Avoid Thread.Sleep - use async/await
- Run slow tests in parallel when possible

### Test Isolation
- Each test should be independent
- Use fresh data for each test
- Clean up after integration tests
- Don't rely on test execution order

## Common Issues

### Issue: Tests fail randomly
**Solution**: Look for shared state, use separate databases per test

### Issue: Integration tests are slow
**Solution**: Use test containers, run in parallel, use in-memory DB where possible

### Issue: Can't mock a dependency
**Solution**: Create an interface, use dependency injection

### Issue: Component tests fail to find elements
**Solution**: Wait for rendering, use `WaitForElement` or `WaitForState`

## Best Practices Checklist

- [ ] Name tests clearly: `Method_Scenario_ExpectedResult`
- [ ] One assertion per test (when reasonable)
- [ ] Use AAA pattern: Arrange, Act, Assert
- [ ] Mock external dependencies
- [ ] Test edge cases and error scenarios
- [ ] Keep tests maintainable and readable
- [ ] Run tests before committing
- [ ] Fix broken tests immediately
- [ ] Review test coverage regularly
- [ ] Remove obsolete tests

## Coverage Goals

- **Unit Tests**: 80% minimum coverage
- **Critical Paths**: 100% coverage
- **UI Components**: 70% coverage
- **Overall Target**: 75% coverage

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Best Practices](https://fluentassertions.com/tips/)
- [bUnit Documentation](https://bunit.dev/)
- [Testcontainers .NET](https://dotnet.testcontainers.org/)
- [Entity Framework Core Testing](https://docs.microsoft.com/en-us/ef/core/testing/)

## Getting Help

- Check test output for detailed error messages
- Review similar tests in the codebase
- Consult team documentation
- Ask in team chat/forums

Remember: Tests are documentation. Write them clearly!