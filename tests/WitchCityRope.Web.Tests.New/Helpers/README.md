# Test Infrastructure Documentation

This folder contains the base test infrastructure for WitchCityRope.Web.Tests.

## Core Components

### TestBase.cs
Base class for all Blazor component tests that extends bUnit's TestContext. Provides:
- Common service setup (logging, navigation)
- Helper methods for rendering components
- Assertion helpers for common test scenarios
- Element finding utilities
- Async assertion support with retry logic

Example usage:
```csharp
public class MyComponentTests : TestBase
{
    [Fact]
    public void Component_Should_Render_Correctly()
    {
        // Arrange & Act
        var component = RenderComponent<MyComponent>(
            ComponentParameter.CreateParameter("Title", "Test Title")
        );

        // Assert
        AssertContainsText(component, "Test Title");
        var button = FindElement(component, "button.submit-btn");
        button.Click();
    }
}
```

### AuthenticationTestHelper.cs
Provides utilities for setting up authentication in tests:
- Creating test users with different roles (Admin, Member)
- Setting up authentication state providers
- Creating mock IAuthService instances
- Extension methods for TestContext

Example usage:
```csharp
public class AuthenticatedComponentTests : TestBase
{
    [Fact]
    public void Should_Show_Admin_Content()
    {
        // Arrange
        this.AddAdminAuthentication();
        
        // Act
        var component = RenderComponent<AdminDashboard>();
        
        // Assert
        AssertContainsText(component, "Admin Dashboard");
    }
}
```

### ServiceMockHelper.cs
Helper methods for creating common service mocks:
- IApiClient with configurable responses
- INotificationService with verifiable calls
- HttpClient with mock responses
- Helper methods for creating test data (PagedResult, ApiResponse)

Example usage:
```csharp
public class ApiComponentTests : TestBase
{
    [Fact]
    public async Task Should_Load_Data_From_Api()
    {
        // Arrange
        var apiMock = ServiceMockHelper.CreateApiClientMock();
        var testData = new List<EventDto> { new() { Id = 1, Title = "Test Event" } };
        ServiceMockHelper.SetupSuccessfulGet(apiMock, "/api/events", testData);
        
        Services.AddSingleton(apiMock.Object);
        
        // Act
        var component = RenderComponent<EventList>();
        await component.InvokeAsync(() => component.Instance.LoadEvents());
        
        // Assert
        AssertContainsText(component, "Test Event");
    }
}
```

### NavigationTestHelper.cs
Utilities for testing navigation scenarios:
- Common route constants
- Navigation assertion helpers
- Query parameter handling
- Extension methods for clicking links and verifying navigation

Example usage:
```csharp
public class NavigationTests : TestBase
{
    [Fact]
    public void Should_Navigate_To_Event_Details()
    {
        // Arrange
        var component = RenderComponent<EventCard>(
            ComponentParameter.CreateParameter("EventId", 123)
        );
        
        // Act
        component.ClickLinkAndVerifyNavigation(".event-link", "/events/123");
        
        // Assert - handled by extension method
    }
}
```

## Best Practices

1. **Always inherit from TestBase** for component tests to get all the helper functionality
2. **Use the helper methods** instead of writing boilerplate code
3. **Set up authentication** when testing components that require it
4. **Mock external dependencies** using ServiceMockHelper
5. **Use meaningful test names** that describe what is being tested
6. **Keep tests focused** - test one thing at a time
7. **Use the navigation helpers** for testing routing scenarios

## Folder Structure

- **Helpers/** - Test infrastructure and utilities (this folder)
- **Auth/** - Authentication-related component tests
- **Components/** - Shared component tests
- **Features/** - Feature-specific tests organized by area
  - **Admin/** - Admin feature tests
  - **Events/** - Event management tests
  - **Members/** - Member-related tests
  - **Public/** - Public-facing component tests
- **Services/** - Service layer tests
- **TestData/** - Test data builders and fixtures