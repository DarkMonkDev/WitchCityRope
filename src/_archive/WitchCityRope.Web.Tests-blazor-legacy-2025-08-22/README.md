# WitchCityRope Web Tests

This project contains comprehensive Blazor component tests for the WitchCityRope web application using bUnit testing framework.

## Test Structure

### Test Helpers (`/Helpers`)
- **AuthenticationTestContext.cs** - Helper methods for setting up authentication state in tests
- **ServiceMockHelpers.cs** - Common service mock configurations
- **ComponentTestBase.cs** - Base class for component tests with shared setup

### Authentication Tests (`/Features/Auth`)
- **LoginComponentTests.cs** - Tests for login form validation, submission, error handling, and 2FA redirection
- **RegisterComponentTests.cs** - Tests for registration form, password strength indicator, and validation
- **TwoFactorAuthComponentTests.cs** - Tests for 2FA code input, auto-advance, and verification flow

### Event Tests (`/Features/Events`)
- **EventCardComponentTests.cs** - Tests for event card rendering, click behavior, and different states
- **EventListTests.cs** - Tests for event list filtering, search, pagination, and loading states
- **EventDetailPageTests.cs** - Tests for event details display, registration button states, and user interactions

### Member Tests (`/Features/Members`)
- **DashboardPageTests.cs** - Tests for dashboard data loading, stats display, and quick actions
- **ProfilePageTests.cs** - Tests for profile editing, form validation, and save behavior

### Layout Tests (`/Shared/Layouts`)
- **MainLayoutTests.cs** - Tests for authenticated layout, user menu, navigation, and responsive behavior
- **PublicLayoutTests.cs** - Tests for public/unauthenticated layout state

### Navigation Tests (`/Shared/Components/Navigation`)
- **MainNavTests.cs** - Tests for navigation component, mobile menu, and role-based visibility

## Key Testing Patterns

### 1. Component Rendering Tests
```csharp
[Fact]
public void Component_InitialRender_DisplaysCorrectContent()
{
    // Arrange & Act
    var component = RenderComponent<MyComponent>();
    
    // Assert
    component.Find(".element").TextContent.Should().Be("Expected Text");
}
```

### 2. User Interaction Tests
```csharp
[Fact]
public async Task Component_UserClick_TriggersAction()
{
    // Arrange
    var component = RenderComponent<MyComponent>();
    
    // Act
    await component.Find(".button").ClickAsync();
    
    // Assert
    mockService.Verify(x => x.MethodCalled(), Times.Once);
}
```

### 3. Form Validation Tests
```csharp
[Fact]
public void Form_InvalidInput_ShowsValidationErrors()
{
    // Arrange
    var component = RenderComponent<FormComponent>();
    
    // Act
    component.Find("form").Submit();
    
    // Assert
    component.FindAll(".validation-message").Should().NotBeEmpty();
}
```

### 4. Async Loading Tests
```csharp
[Fact]
public async Task Component_AsyncLoad_ShowsLoadingThenData()
{
    // Arrange
    var tcs = new TaskCompletionSource<Data>();
    mockService.Setup(x => x.GetDataAsync()).Returns(tcs.Task);
    
    // Act
    var component = RenderComponent<AsyncComponent>();
    
    // Assert - Loading state
    component.Find(".loading-spinner").Should().NotBeNull();
    
    // Complete loading
    tcs.SetResult(new Data());
    await Task.Delay(50);
    
    // Assert - Data displayed
    component.FindAll(".loading-spinner").Should().BeEmpty();
}
```

### 5. Authorization Tests
```csharp
[Fact]
public void Component_UnauthorizedUser_ShowsLoginPrompt()
{
    // Arrange
    AuthenticationTestContext.SetupAnonymousUser(this);
    
    // Act
    var component = RenderComponent<ProtectedComponent>();
    
    // Assert
    component.Find(".login-prompt").Should().NotBeNull();
}
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test class
dotnet test --filter "FullyQualifiedName~LoginComponentTests"

# Run tests in watch mode
dotnet watch test
```

## Test Coverage Areas

1. **Component Rendering** - Verifies components render with correct initial state
2. **User Interactions** - Tests clicks, form inputs, and other user actions
3. **State Management** - Validates component state changes and updates
4. **Service Integration** - Ensures proper interaction with injected services
5. **Authorization** - Tests role-based visibility and access control
6. **Error Handling** - Validates error display and recovery flows
7. **Loading States** - Tests async operations and loading indicators
8. **Responsive Behavior** - Validates mobile menu and responsive features
9. **Form Validation** - Tests client-side validation and error messages
10. **Navigation** - Validates routing and navigation behavior

## Best Practices

1. **Use ComponentTestBase** - Inherit from this for consistent test setup
2. **Mock External Dependencies** - Always mock services, navigation, etc.
3. **Test User Perspective** - Focus on what users see and interact with
4. **Async-Aware Testing** - Use proper async patterns for component lifecycle
5. **Descriptive Test Names** - Follow pattern: `Component_Scenario_ExpectedResult`
6. **Arrange-Act-Assert** - Keep tests organized and readable
7. **Test Edge Cases** - Include error scenarios and boundary conditions
8. **Verify Accessibility** - Test ARIA attributes and keyboard navigation