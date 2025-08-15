# Playwright Best Practices for Testing Blazor Server Applications

## Executive Summary

Playwright has emerged as the superior choice for end-to-end testing of Blazor Server applications in 2024-2025, offering native C# support, advanced handling of dynamic content, and superior CI/CD integration compared to alternatives like Puppeteer. This document outlines key best practices and recommendations based on current industry standards.

## 1. Blazor Server-Specific Testing Patterns

### Understanding the Testing Approach
- **Key Distinction**: Playwright is an end-to-end testing framework, not a component testing library like bUnit
- **Purpose**: Tests the entire system in a "production-like" environment, simulating real user interactions
- **Architecture**: Blazor Server loads HTML in `OnInitialized` and updates DOM via `OnAfterRenderAsync`, requiring special handling

### Recommended Test Structure
```csharp
// Use WebApplicationFactory for Blazor Server testing
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Custom configuration for test environment
}

// Test example
[Fact]
public async Task BlazorServerApp_LoadsHomePage()
{
    using var playwright = await Playwright.CreateAsync();
    var browser = await playwright.Chromium.LaunchAsync();
    var page = await browser.NewPageAsync();
    
    await page.GotoAsync("https://localhost:5001");
    // Wait for specific element instead of network idle
    await page.WaitForSelectorAsync("[data-testid='main-content']");
    
    var title = await page.TextContentAsync("h1");
    Assert.Equal("Welcome", title);
}
```

## 2. Handling SignalR Connections in Tests

### Key Challenges
- Blazor Server uses SignalR circuits to maintain state
- Tests may be flaky due to asynchronous DOM updates
- Network idle states are unreliable for Blazor Server

### Best Practices
1. **Avoid `WaitForLoadStateAsync(LoadState.NetworkIdle)`** - Often doesn't work reliably with Blazor Server
2. **Wait for Specific Elements**: Use `WaitForSelectorAsync()` with data-testid attributes
3. **Handle Circuit Initialization**: Ensure SignalR circuit is established before interactions
4. **Monitor Circuit State**: Use `ActiveCircuitHandler` patterns when needed

### Example Pattern
```csharp
// Don't do this
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

// Do this instead
await page.WaitForSelectorAsync("[data-testid='component-loaded']", new() 
{ 
    State = WaitForSelectorState.Visible,
    Timeout = 30000 
});
```

## 3. Waiting for Blazor Components to Render

### Problem
Tests fail because Playwright executes too quickly while:
- The DOM is loaded from server prerendering
- The .NET runtime is still loading
- SignalR connection is being established

### Solutions

#### Assert Page Readiness
```csharp
// Use Playwright's Expect functionality
await Expect(page.Locator("[data-testid='user-dashboard']"))
    .ToBeVisibleAsync(new() { Timeout = 10000 });
```

#### Wait for Interactive Elements
```csharp
// Wait for buttons to become interactive
var button = page.Locator("button[data-testid='submit-btn']");
await button.WaitForAsync(new() { State = WaitForSelectorState.Attached });
await button.ClickAsync();
```

#### Handle Render Mode Transitions
- Account for prerendering vs interactive modes
- Add small delays only as last resort
- Focus on waiting for specific indicators of component readiness

## 4. Authentication Testing Patterns for Blazor

### JWT Token Management
```csharp
// Store authentication state for reuse
var storageState = "auth-state.json";
await page.Context.StorageStateAsync(new() { Path = storageState });

// Reuse in subsequent tests
var context = await browser.NewContextAsync(new() 
{ 
    StorageStatePath = storageState 
});
```

### Azure AD B2C Integration
- **Challenge**: MSAL and Azure B2C have reported issues with state persistence
- **Solution**: Implement custom token management for test scenarios
- **Best Practice**: Use Microsoft Playwright Testing Service with REST API for production scenarios

### Authentication Best Practices
1. Load existing authenticated state to avoid re-authentication in every test
2. Use isolated browser contexts for test reproducibility
3. Implement token refresh handling for long-running test suites
4. Consider using test-specific authentication endpoints

## 5. CI/CD Integration Best Practices

### Playwright Advantages
- **Native Integration**: Seamless integration with Jenkins, Azure Pipelines, GitHub Actions, TeamCity, CircleCI
- **Parallel Execution**: Built-in support for running tests simultaneously across browsers
- **Trace Files**: Generate trace files for debugging CI failures

### CI/CD Pipeline Configuration
```yaml
# Example GitHub Actions workflow
- name: Install Playwright
  run: dotnet tool install --global Microsoft.Playwright.CLI

- name: Install Browsers
  run: playwright install

- name: Run E2E Tests
  run: dotnet test --filter Category=E2E
  env:
    ASPNETCORE_ENVIRONMENT: Testing
```

### Best Practices
1. Use headless mode in CI environments
2. Configure appropriate timeouts for CI (longer than local)
3. Generate and store trace files for failed tests
4. Run tests in parallel but consider SignalR connection limits
5. Use container-based testing for consistency

## 6. Performance Considerations vs Puppeteer

### Playwright Advantages for Blazor Server

| Feature | Playwright | Puppeteer |
|---------|------------|-----------|
| **Language Support** | C#, JavaScript, Python, Java | JavaScript only |
| **Browser Support** | Chromium, Firefox, WebKit | Chromium (Firefox experimental) |
| **Auto-wait Features** | Advanced, handles dynamic content | Basic |
| **Parallel Execution** | Built-in support | Manual configuration |
| **Network Interception** | Advanced request handling | Basic |
| **CI/CD Integration** | Native support for major tools | Requires more setup |
| **Blazor Integration** | Native C# aligns with .NET | JavaScript bridge needed |

### Performance Metrics
- **Speed**: Playwright generally faster, especially in headless mode
- **Reliability**: Better handling of dynamic content reduces flaky tests
- **Scalability**: Superior parallel execution capabilities

### Recommendation
**Choose Playwright for Blazor Server** because:
1. Native C# support aligns with .NET ecosystem
2. Superior handling of SignalR-based dynamic updates
3. Better auto-wait features for component rendering
4. Built-in cross-browser testing
5. More robust CI/CD integration

## Key Recommendations Summary

1. **Use Playwright over Puppeteer** for Blazor Server testing
2. **Implement proper wait strategies** using element-specific waits, not network idle
3. **Use data-testid attributes** for reliable element selection
4. **Handle authentication** with stored state for efficiency
5. **Configure CI/CD** with appropriate timeouts and parallel execution
6. **Monitor SignalR connections** and handle circuit initialization
7. **Leverage WebApplicationFactory** for integration testing
8. **Generate trace files** for debugging test failures
9. **Test across browsers** to ensure compatibility
10. **Keep tests isolated** using browser contexts

## Additional Resources

- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [Microsoft Playwright Testing Service](https://learn.microsoft.com/en-us/azure/playwright-testing/)
- [Blazor Testing Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/test)