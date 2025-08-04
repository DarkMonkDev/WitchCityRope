# Blazor Testing Best Practices - .NET 9

## bUnit Testing Guidelines

### 1. Component Test Structure
```csharp
public class ComponentNameTests : TestContext
{
    private Mock<IService> _serviceMock;
    
    public ComponentNameTests()
    {
        // Setup common mocks
        _serviceMock = new Mock<IService>();
        Services.AddSingleton(_serviceMock.Object);
    }
    
    [Fact]
    public void ComponentName_Action_ExpectedResult()
    {
        // Arrange
        var expectedValue = "test";
        _serviceMock.Setup(x => x.Method()).Returns(expectedValue);
        
        // Act
        var component = RenderComponent<ComponentName>(parameters => parameters
            .Add(p => p.SomeParameter, "value"));
        
        // Assert
        component.Find(".element").TextContent.Should().Be(expectedValue);
    }
}
```

### 2. Testing User Interactions
```csharp
// Click events
await component.Find("button").ClickAsync(new MouseEventArgs());

// Form inputs
await component.Find("input").ChangeAsync(new ChangeEventArgs { Value = "new value" });

// Select dropdowns
await component.Find("select").ChangeAsync(new ChangeEventArgs { Value = "option1" });

// Keyboard events
await component.Find("input").KeyPressAsync(new KeyboardEventArgs { Key = "Enter" });
```

### 3. Testing Async Operations
```csharp
[Fact]
public async Task Component_AsyncOperation_UpdatesCorrectly()
{
    // Arrange
    var tcs = new TaskCompletionSource<string>();
    _serviceMock.Setup(x => x.GetDataAsync()).Returns(tcs.Task);
    
    // Act
    var component = RenderComponent<AsyncComponent>();
    
    // Assert loading state
    component.Find(".loading").Should().NotBeNull();
    
    // Complete async operation
    tcs.SetResult("data");
    
    // Wait for re-render
    component.WaitForAssertion(() =>
    {
        component.Find(".content").TextContent.Should().Be("data");
    });
}
```

### 4. Testing Authorization
```csharp
[Fact]
public void Component_RequiresAuthorization_ShowsCorrectContent()
{
    // Arrange
    var authContext = this.AddTestAuthorization();
    authContext.SetAuthorized("testuser");
    authContext.SetRoles("Admin");
    
    // Act
    var component = RenderComponent<AuthorizedComponent>();
    
    // Assert
    component.Find(".admin-content").Should().NotBeNull();
}
```

### 5. Testing Navigation
```csharp
[Fact]
public void Component_NavigationTrigger_NavigatesToCorrectUrl()
{
    // Arrange
    var navManager = Services.GetRequiredService<NavigationManager>();
    
    // Act
    var component = RenderComponent<NavigationComponent>();
    component.Find("button").Click();
    
    // Assert
    navManager.Uri.Should().EndWith("/expected/path");
}
```

### 6. Testing Component Parameters
```csharp
[Fact]
public void Component_ParameterChange_UpdatesDisplay()
{
    // Arrange & Act
    var component = RenderComponent<ParameterComponent>(parameters => parameters
        .Add(p => p.Title, "Initial"));
    
    // Assert initial state
    component.Find("h1").TextContent.Should().Be("Initial");
    
    // Act - change parameter
    component.SetParametersAndRender(parameters => parameters
        .Add(p => p.Title, "Updated"));
    
    // Assert updated state
    component.Find("h1").TextContent.Should().Be("Updated");
}
```

### 7. Testing Event Callbacks
```csharp
[Fact]
public void Component_EventCallback_InvokesCorrectly()
{
    // Arrange
    var wasInvoked = false;
    var component = RenderComponent<CallbackComponent>(parameters => parameters
        .Add(p => p.OnClick, EventCallback.Factory.Create(this, () => wasInvoked = true)));
    
    // Act
    component.Find("button").Click();
    
    // Assert
    wasInvoked.Should().BeTrue();
}
```

### 8. Testing JavaScript Interop
```csharp
[Fact]
public void Component_JSInterop_CallsCorrectMethod()
{
    // Arrange
    var jsRuntime = Services.GetRequiredService<IJSRuntime>();
    var jsMock = Mock.Get(jsRuntime);
    
    // Act
    var component = RenderComponent<JSComponent>();
    component.Find("button").Click();
    
    // Assert
    jsMock.Verify(x => x.InvokeAsync<object>(
        "jsFunction",
        It.IsAny<object[]>()),
        Times.Once);
}
```

## Testing Anti-Patterns to Avoid

### 1. Don't Test Implementation Details
❌ **Bad**: Testing private methods or internal state
✅ **Good**: Testing observable behavior and rendered output

### 2. Don't Use Time-Based Delays
❌ **Bad**: `await Task.Delay(1000);`
✅ **Good**: `component.WaitForAssertion(() => { ... });`

### 3. Don't Test Framework Code
❌ **Bad**: Testing that Blazor's data binding works
✅ **Good**: Testing your component's specific behavior

### 4. Don't Create Fragile Selectors
❌ **Bad**: `component.Find("div > div > span:nth-child(2)")`
✅ **Good**: `component.Find("[data-testid='user-name']")`

### 5. Don't Share State Between Tests
❌ **Bad**: Using static fields or shared instances
✅ **Good**: Fresh setup for each test

## Performance Tips

1. **Use TestContext per class**: Inherit from `TestContext` rather than creating new instances
2. **Minimize renders**: Use `SetParametersAndRender` sparingly
3. **Mock heavy operations**: Mock database calls, API calls, etc.
4. **Parallel execution**: Use `[Collection]` attributes when needed
5. **Dispose properly**: Implement `IDisposable` when using resources

## Debugging Tips

1. **Use component markup**: `component.Markup` to see rendered HTML
2. **Check render count**: `component.RenderCount` for performance issues
3. **Log service calls**: Add logging to mocked services
4. **Use breakpoints**: Debugger works normally in tests
5. **Capture exceptions**: Use `Should().Throw<>()` for expected exceptions