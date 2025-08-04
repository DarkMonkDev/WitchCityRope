# Dashboard Testing Strategy

## Overview

This document outlines the comprehensive testing strategy for the user dashboard feature, including unit tests, integration tests, E2E tests, visual regression tests, and MCP-based testing.

## Test Coverage Goals

- **Unit Tests**: 95% code coverage
- **Integration Tests**: All API endpoints and data flows
- **E2E Tests**: Critical user journeys
- **Visual Tests**: All responsive breakpoints
- **Performance Tests**: Load time < 2s, Lighthouse > 90

## Unit Testing

### Component Tests with bUnit

#### DashboardTests.cs

```csharp
using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WitchCityRope.Web.Features.Members.Pages;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;
using Xunit;

public class DashboardTests : ComponentTestBase
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly DashboardViewModel _testDashboardData;

    public DashboardTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _testDashboardData = CreateTestDashboardData();
        
        Services.AddSingleton(_dashboardServiceMock.Object);
    }

    [Fact]
    public void Dashboard_ShowsLoadingState_Initially()
    {
        // Arrange
        _dashboardServiceMock
            .Setup(x => x.GetDashboardDataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(100);
                return _testDashboardData;
            });

        // Act
        var component = RenderComponent<Dashboard>();

        // Assert
        component.Find(".skeleton-loader").Should().NotBeNull();
        component.FindAll(".dashboard-content").Should().BeEmpty();
    }

    [Fact]
    public async Task Dashboard_ShowsWelcomeMessage_ForAuthenticatedUser()
    {
        // Arrange
        var user = new UserDto { Id = Guid.NewGuid(), SceneName = "RopeMaster" };
        MockAuthService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
        _dashboardServiceMock
            .Setup(x => x.GetDashboardDataAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_testDashboardData);

        // Act
        var component = RenderComponent<Dashboard>();
        await component.WaitForState(() => !component.Instance.IsLoading);

        // Assert
        var welcomeMessage = component.Find(".welcome-message");
        welcomeMessage.TextContent.Should().Contain("Welcome back, RopeMaster!");
    }

    [Fact]
    public async Task Dashboard_ShowsUpcomingEvents_WhenUserHasEvents()
    {
        // Arrange
        SetupAuthenticatedUser();
        _testDashboardData.UpcomingEvents = new List<EventViewModel>
        {
            new() { Title = "Rope Safety 101", Date = DateTime.Now.AddDays(5) },
            new() { Title = "March Rope Jam", Date = DateTime.Now.AddDays(8) },
            new() { Title = "Suspension Workshop", Date = DateTime.Now.AddDays(12) }
        };

        // Act
        var component = RenderComponent<Dashboard>();
        await component.WaitForState(() => !component.Instance.IsLoading);

        // Assert
        var eventCards = component.FindAll(".event-card");
        eventCards.Should().HaveCount(3);
        eventCards[0].TextContent.Should().Contain("Rope Safety 101");
    }

    [Fact]
    public async Task Dashboard_ShowsEmptyState_WhenNoUpcomingEvents()
    {
        // Arrange
        SetupAuthenticatedUser();
        _testDashboardData.UpcomingEvents = new List<EventViewModel>();

        // Act
        var component = RenderComponent<Dashboard>();
        await component.WaitForState(() => !component.Instance.IsLoading);

        // Assert
        var emptyState = component.Find(".upcoming-events .empty-state");
        emptyState.Should().NotBeNull();
        emptyState.TextContent.Should().Contain("No upcoming events");
        
        var browseButton = component.Find(".upcoming-events .browse-events-btn");
        browseButton.Should().NotBeNull();
    }

    [Fact]
    public async Task Dashboard_ShowsVettingPrompt_ForUnvettedUser()
    {
        // Arrange
        SetupAuthenticatedUser();
        _testDashboardData.VettingStatus = VettingStatus.NotApplied;

        // Act
        var component = RenderComponent<Dashboard>();
        await component.WaitForState(() => !component.Instance.IsLoading);

        // Assert
        var vettingPrompt = component.Find(".vetting-prompt");
        vettingPrompt.Should().NotBeNull();
        vettingPrompt.TextContent.Should().Contain("Join our vetted community");
        
        var applyButton = component.Find(".vetting-prompt .apply-btn");
        applyButton.Should().NotBeNull();
    }

    [Fact]
    public async Task Dashboard_ShowsAdminSection_ForAdminUsers()
    {
        // Arrange
        SetupAuthenticatedUser(UserRole.Administrator);
        _testDashboardData.Role = UserRole.Administrator;

        // Act
        var component = RenderComponent<Dashboard>();
        await component.WaitForState(() => !component.Instance.IsLoading);

        // Assert
        var adminSection = component.Find(".admin-quick-access");
        adminSection.Should().NotBeNull();
        adminSection.TextContent.Should().Contain("Admin Tools");
    }

    [Fact]
    public async Task Dashboard_HandlesApiError_Gracefully()
    {
        // Arrange
        SetupAuthenticatedUser();
        _dashboardServiceMock
            .Setup(x => x.GetDashboardDataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException("Service unavailable", 503));

        // Act
        var component = RenderComponent<Dashboard>();
        await component.WaitForState(() => !component.Instance.IsLoading);

        // Assert
        component.Instance.HasError.Should().BeTrue();
        
        var errorMessage = component.Find(".error-message");
        errorMessage.TextContent.Should().Contain("Unable to load dashboard");
        
        var retryButton = component.Find(".retry-button");
        retryButton.Should().NotBeNull();
    }

    [Fact]
    public async Task Dashboard_RefreshesData_WhenRetryClicked()
    {
        // Arrange
        SetupAuthenticatedUser();
        var callCount = 0;
        _dashboardServiceMock
            .Setup(x => x.GetDashboardDataAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1) throw new Exception("First call fails");
                return _testDashboardData;
            });

        // Act
        var component = RenderComponent<Dashboard>();
        await component.WaitForState(() => component.Instance.HasError);
        
        var retryButton = component.Find(".retry-button");
        await retryButton.ClickAsync();
        await component.WaitForState(() => !component.Instance.IsLoading && !component.Instance.HasError);

        // Assert
        callCount.Should().Be(2);
        component.FindAll(".dashboard-content").Should().NotBeEmpty();
    }

    [Fact]
    public async Task Dashboard_NavigatesToEventDetails_WhenEventClicked()
    {
        // Arrange
        SetupAuthenticatedUser();
        var eventId = Guid.NewGuid();
        _testDashboardData.UpcomingEvents = new List<EventViewModel>
        {
            new() { Id = eventId, Title = "Test Event" }
        };

        // Act
        var component = RenderComponent<Dashboard>();
        await component.WaitForState(() => !component.Instance.IsLoading);
        
        var eventCard = component.Find(".event-card");
        await eventCard.ClickAsync();

        // Assert
        NavigationManager.Uri.Should().EndWith($"/events/{eventId}");
    }

    [Fact]
    public async Task Dashboard_UpdatesMembershipStats_Correctly()
    {
        // Arrange
        SetupAuthenticatedUser();
        _testDashboardData.Stats = new MembershipStatsViewModel
        {
            IsVerified = true,
            EventsAttended = 42,
            MonthsAsMember = 18,
            Partners = 3
        };

        // Act
        var component = RenderComponent<Dashboard>();
        await component.WaitForState(() => !component.Instance.IsLoading);

        // Assert
        var statsCard = component.Find(".membership-stats");
        statsCard.Find(".events-attended").TextContent.Should().Contain("42");
        statsCard.Find(".months-member").TextContent.Should().Contain("18");
        statsCard.Find(".partners-count").TextContent.Should().Contain("3");
        
        var verifiedBadge = statsCard.Find(".verified-badge");
        verifiedBadge.Should().NotBeNull();
    }
}
```

### Service Tests

#### DashboardServiceTests.cs

```csharp
public class DashboardServiceTests
{
    private readonly Mock<ApiClient> _apiClientMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _apiClientMock = new Mock<ApiClient>();
        _cacheMock = new Mock<IMemoryCache>();
        var logger = new Mock<ILogger<DashboardService>>();
        
        _service = new DashboardService(_apiClientMock.Object, _cacheMock.Object, logger.Object);
    }

    [Fact]
    public async Task GetDashboardDataAsync_ReturnsCachedData_WhenAvailable()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedData = new DashboardViewModel { SceneName = "CachedUser" };
        object cacheValue = cachedData;
        
        _cacheMock
            .Setup(x => x.TryGetValue($"dashboard_{userId}", out cacheValue))
            .Returns(true);

        // Act
        var result = await _service.GetDashboardDataAsync(userId);

        // Assert
        result.Should().BeSameAs(cachedData);
        _apiClientMock.Verify(x => x.GetAsync<It.IsAnyType>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetDashboardDataAsync_FetchesAllDataInParallel()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCacheMiss();
        SetupApiResponses(userId);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await _service.GetDashboardDataAsync(userId);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        
        // Verify all API calls were made
        _apiClientMock.Verify(x => x.GetAsync<List<EventViewModel>>(
            $"users/{userId}/upcoming-events?count=3", 
            It.IsAny<CancellationToken>()), Times.Once);
            
        _apiClientMock.Verify(x => x.GetAsync<List<EventViewModel>>(
            $"users/{userId}/recommended-classes?count=4", 
            It.IsAny<CancellationToken>()), Times.Once);
            
        _apiClientMock.Verify(x => x.GetAsync<MembershipStatsViewModel>(
            $"users/{userId}/stats", 
            It.IsAny<CancellationToken>()), Times.Once);
            
        _apiClientMock.Verify(x => x.GetAsync<UserDto>(
            $"users/{userId}", 
            It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify parallel execution (should be faster than sequential)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(400);
    }

    [Fact]
    public async Task GetDashboardDataAsync_CachesResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupCacheMiss();
        SetupApiResponses(userId);
        
        var cacheEntryMock = new Mock<ICacheEntry>();
        _cacheMock
            .Setup(x => x.CreateEntry($"dashboard_{userId}"))
            .Returns(cacheEntryMock.Object);

        // Act
        await _service.GetDashboardDataAsync(userId);

        // Assert
        cacheEntryMock.VerifySet(x => x.Value = It.IsAny<DashboardViewModel>(), Times.Once);
        cacheEntryMock.VerifySet(x => x.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5), Times.Once);
    }
}
```

## Integration Testing

### API Integration Tests

#### DashboardApiTests.cs

```csharp
public class DashboardApiTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DashboardApiTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateAuthenticatedClient(TestUsers.Member);
    }

    [Fact]
    public async Task GetDashboard_ReturnsOk_ForAuthenticatedUser()
    {
        // Arrange
        var userId = TestUsers.Member.Id;

        // Act
        var response = await _client.GetAsync($"/api/dashboard/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var dashboard = JsonSerializer.Deserialize<DashboardDto>(content);
        
        dashboard.Should().NotBeNull();
        dashboard.SceneName.Should().NotBeEmpty();
        dashboard.UpcomingEvents.Should().NotBeNull();
        dashboard.Stats.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboard_ReturnsForbidden_WhenAccessingOtherUserDashboard()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/dashboard/{otherUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDashboard_ReturnsUnauthorized_ForUnauthenticatedUser()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/dashboard/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUpcomingEvents_ReturnsCorrectCount()
    {
        // Arrange
        var userId = TestUsers.Member.Id;
        const int requestedCount = 5;

        // Act
        var response = await _client.GetAsync($"/api/users/{userId}/upcoming-events?count={requestedCount}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        events.Should().HaveCountLessOrEqualTo(requestedCount);
        events.Should().OnlyContain(e => e.Date > DateTime.UtcNow);
        events.Should().BeInAscendingOrder(e => e.Date);
    }

    [Fact]
    public async Task GetMembershipStats_ReturnsAccurateData()
    {
        // Arrange
        var userId = TestUsers.VettedMember.Id;

        // Act
        var response = await _client.GetAsync($"/api/users/{userId}/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var stats = await response.Content.ReadFromJsonAsync<MembershipStatsDto>();
        stats.IsVerified.Should().BeTrue();
        stats.EventsAttended.Should().BeGreaterOrEqualTo(0);
        stats.MonthsAsMember.Should().BeGreaterThan(0);
    }
}
```

### Page Navigation Tests

#### DashboardNavigationTests.cs

```csharp
public class DashboardNavigationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    [Fact]
    public async Task Dashboard_RedirectsToLogin_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/member/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.ToString().Should().Contain("/auth/login");
        response.Headers.Location.ToString().Should().Contain("returnUrl=%2Fmember%2Fdashboard");
    }

    [Fact]
    public async Task Dashboard_LoadsSuccessfully_ForAuthenticatedUser()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(TestUsers.Member);

        // Act
        var response = await client.GetAsync("/member/dashboard");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("Welcome back");
        content.Should().Contain("dashboard-container");
    }

    [Fact]
    public async Task Dashboard_ShowsAdminSection_ForAdminUser()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(TestUsers.Admin);

        // Act
        var response = await client.GetAsync("/member/dashboard");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("admin-quick-access");
        content.Should().Contain("Admin Tools");
    }
}
```

## E2E Testing

### Dashboard E2E Tests with Playwright

#### DashboardE2ETests.cs

```csharp
public class DashboardE2ETests : BaseE2ETest
{
    [Fact]
    public async Task Dashboard_CompleteUserJourney_Works()
    {
        // Arrange - Login
        await LoginAsUser(TestUsers.Member);

        // Act - Navigate to dashboard
        await Page.GotoAsync("/member/dashboard");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Page loads
        await Expect(Page.Locator(".dashboard-container")).ToBeVisibleAsync();
        await Expect(Page.Locator(".welcome-message")).ToContainTextAsync($"Welcome back, {TestUsers.Member.SceneName}!");

        // Act - Check upcoming events
        var eventCards = Page.Locator(".event-card");
        await Expect(eventCards).ToHaveCountAsync(3);

        // Act - Click on first event
        await eventCards.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Navigation to event detail
        await Expect(Page).ToHaveURLAsync(new Regex(@"/events/\w+"));

        // Act - Go back to dashboard
        await Page.GoBackAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act - Test quick action
        await Page.ClickAsync(".browse-classes-btn");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Navigation to events page
        await Expect(Page).ToHaveURLAsync("/events");
    }

    [Fact]
    public async Task Dashboard_ResponsiveLayout_WorksOnMobile()
    {
        // Arrange - Set mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        await LoginAsUser(TestUsers.Member);

        // Act
        await Page.GotoAsync("/member/dashboard");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Mobile layout
        var dashboardGrid = Page.Locator(".dashboard-grid");
        var gridStyles = await dashboardGrid.EvaluateAsync<string>("el => window.getComputedStyle(el).gridTemplateColumns");
        gridStyles.Should().NotContain("2fr 1fr"); // Should be single column

        // Test mobile menu
        await Page.ClickAsync(".mobile-menu-toggle");
        await Expect(Page.Locator(".mobile-menu")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Dashboard_RealtimeUpdates_Work()
    {
        // Arrange
        await LoginAsUser(TestUsers.Member);
        await Page.GotoAsync("/member/dashboard");
        
        // Get initial event count
        var initialCount = await Page.Locator(".event-card").CountAsync();

        // Act - Simulate real-time event registration in another tab
        var newContext = await Browser.NewContextAsync();
        var newPage = await newContext.NewPageAsync();
        await LoginAsUser(TestUsers.Member, newPage);
        await newPage.GotoAsync("/events");
        await newPage.ClickAsync(".register-btn:first-child");
        await newPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for dashboard to update
        await Page.WaitForTimeoutAsync(1000);

        // Assert - Dashboard shows updated count
        var updatedCount = await Page.Locator(".event-card").CountAsync();
        updatedCount.Should().Be(initialCount + 1);

        await newContext.CloseAsync();
    }

    [Fact]
    public async Task Dashboard_ErrorHandling_ShowsRetryOption()
    {
        // Arrange - Intercept API calls to simulate error
        await Page.RouteAsync("**/api/dashboard/**", route =>
        {
            route.FulfillAsync(new()
            {
                Status = 500,
                Body = "Internal Server Error"
            });
        });

        await LoginAsUser(TestUsers.Member);

        // Act
        await Page.GotoAsync("/member/dashboard");
        await Page.WaitForSelectorAsync(".error-message");

        // Assert - Error state
        await Expect(Page.Locator(".error-message")).ToContainTextAsync("Unable to load dashboard");
        await Expect(Page.Locator(".retry-button")).ToBeVisibleAsync();

        // Act - Fix API and retry
        await Page.UnrouteAsync("**/api/dashboard/**");
        await Page.ClickAsync(".retry-button");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Dashboard loads
        await Expect(Page.Locator(".dashboard-container")).ToBeVisibleAsync();
    }
}
```

## Visual Testing with MCP

### MCP Screenshot Tests

```javascript
// dashboard-visual-tests.js
const runDashboardVisualTests = async () => {
    // Test 1: Full dashboard screenshot
    await mcp_puppeteer.screenshot({
        url: "https://localhost:8281/member/dashboard",
        fullPage: true,
        waitForSelector: ".dashboard-loaded",
        cookies: [{
            name: "auth-token",
            value: TEST_USER_TOKEN
        }],
        filename: "dashboard-full.png"
    });

    // Test 2: Loading state
    await mcp_puppeteer.screenshot({
        url: "https://localhost:8281/member/dashboard",
        waitForSelector: ".skeleton-loader",
        interceptRequests: [{
            url: "**/api/dashboard/**",
            delay: 5000
        }],
        filename: "dashboard-loading.png"
    });

    // Test 3: Empty states
    await mcp_browser_tools.screenshot({
        url: "https://localhost:8281/member/dashboard",
        beforeScreenshot: async (page) => {
            // Override API response for empty state
            await page.route("**/api/users/**/upcoming-events", route => {
                route.fulfill({
                    status: 200,
                    body: JSON.stringify([])
                });
            });
        },
        filename: "dashboard-empty-events.png"
    });

    // Test 4: Mobile responsive
    const viewports = [
        { name: "mobile", width: 375, height: 667 },
        { name: "tablet", width: 768, height: 1024 },
        { name: "desktop", width: 1920, height: 1080 }
    ];

    for (const viewport of viewports) {
        await mcp_puppeteer.screenshot({
            url: "https://localhost:8281/member/dashboard",
            viewport: viewport,
            fullPage: true,
            filename: `dashboard-${viewport.name}.png`
        });
    }

    // Test 5: Dark mode (if implemented)
    await mcp_browser_tools.screenshot({
        url: "https://localhost:8281/member/dashboard",
        beforeScreenshot: async (page) => {
            await page.evaluate(() => {
                document.body.classList.add('dark-mode');
            });
        },
        filename: "dashboard-dark-mode.png"
    });

    // Test 6: Hover states
    await mcp_browser_tools.screenshot({
        url: "https://localhost:8281/member/dashboard",
        beforeScreenshot: async (page) => {
            await page.hover('.event-card:first-child');
        },
        filename: "dashboard-hover-state.png"
    });
};
```

### MCP Accessibility Testing

```javascript
// dashboard-accessibility-tests.js
const runAccessibilityTests = async () => {
    // Comprehensive accessibility audit
    const results = await mcp_browser_tools.audit({
        url: "https://localhost:8281/member/dashboard",
        type: "accessibility",
        options: {
            includeWarnings: true,
            wcagLevel: "AA",
            categories: [
                "aria",
                "color-contrast",
                "forms",
                "keyboard",
                "labels",
                "navigation"
            ]
        }
    });

    // Check specific accessibility requirements
    const accessibilityChecks = [
        {
            name: "Keyboard Navigation",
            test: async (page) => {
                // Tab through all interactive elements
                const tabbableElements = await page.evaluate(() => {
                    const elements = [];
                    let activeElement = document.activeElement;
                    
                    // Tab through page
                    for (let i = 0; i < 50; i++) {
                        document.activeElement.dispatchEvent(
                            new KeyboardEvent('keydown', { key: 'Tab' })
                        );
                        if (document.activeElement === activeElement) break;
                        elements.push({
                            tag: document.activeElement.tagName,
                            text: document.activeElement.textContent,
                            visible: document.activeElement.offsetParent !== null
                        });
                        activeElement = document.activeElement;
                    }
                    return elements;
                });
                
                return tabbableElements.every(el => el.visible);
            }
        },
        {
            name: "Screen Reader Labels",
            test: async (page) => {
                const missingLabels = await page.evaluate(() => {
                    const inputs = document.querySelectorAll('input, select, textarea');
                    const missing = [];
                    inputs.forEach(input => {
                        if (!input.getAttribute('aria-label') && 
                            !input.getAttribute('aria-labelledby') &&
                            !document.querySelector(`label[for="${input.id}"]`)) {
                            missing.push(input.outerHTML);
                        }
                    });
                    return missing;
                });
                
                return missing.length === 0;
            }
        }
    ];

    for (const check of accessibilityChecks) {
        const passed = await check.test(page);
        console.log(`${check.name}: ${passed ? 'PASSED' : 'FAILED'}`);
    }
};
```

### MCP Performance Testing

```javascript
// dashboard-performance-tests.js
const runPerformanceTests = async () => {
    // Performance audit with specific metrics
    const perfResults = await mcp_browser_tools.audit({
        url: "https://localhost:8281/member/dashboard",
        type: "performance",
        options: {
            throttling: {
                rttMs: 150,
                throughputKbps: 1638.4,
                cpuSlowdownMultiplier: 4
            },
            categories: [
                "performance",
                "best-practices"
            ]
        }
    });

    // Measure specific metrics
    const metrics = await mcp_browser_tools.evaluate({
        url: "https://localhost:8281/member/dashboard",
        script: `
            // Wait for dashboard to fully load
            await new Promise(resolve => {
                if (document.querySelector('.dashboard-loaded')) {
                    resolve();
                } else {
                    const observer = new MutationObserver(() => {
                        if (document.querySelector('.dashboard-loaded')) {
                            observer.disconnect();
                            resolve();
                        }
                    });
                    observer.observe(document.body, { childList: true, subtree: true });
                }
            });

            // Get performance metrics
            const paintTiming = performance.getEntriesByType('paint');
            const navigationTiming = performance.getEntriesByType('navigation')[0];
            
            return {
                firstContentfulPaint: paintTiming.find(p => p.name === 'first-contentful-paint')?.startTime,
                largestContentfulPaint: performance.getEntriesByType('largest-contentful-paint')[0]?.startTime,
                timeToInteractive: navigationTiming.loadEventEnd - navigationTiming.fetchStart,
                totalBlockingTime: performance.measure('blocking-time').duration,
                cumulativeLayoutShift: performance.getEntriesByType('layout-shift')
                    .reduce((sum, entry) => sum + entry.value, 0)
            };
        `
    });

    // Assert performance budgets
    expect(metrics.firstContentfulPaint).toBeLessThan(1500);
    expect(metrics.largestContentfulPaint).toBeLessThan(2500);
    expect(metrics.timeToInteractive).toBeLessThan(3000);
    expect(metrics.totalBlockingTime).toBeLessThan(300);
    expect(metrics.cumulativeLayoutShift).toBeLessThan(0.1);
};
```

## Test Data Management

### Test Data Builders

```csharp
public class DashboardTestDataBuilder
{
    private readonly Faker _faker = new();

    public DashboardViewModel BuildDashboard(Action<DashboardViewModel>? customize = null)
    {
        var dashboard = new DashboardViewModel
        {
            SceneName = _faker.Internet.UserName(),
            Role = _faker.PickRandom<UserRole>(),
            VettingStatus = _faker.PickRandom<VettingStatus>(),
            UpcomingEvents = BuildEvents(3),
            Stats = BuildStats()
        };

        customize?.Invoke(dashboard);
        return dashboard;
    }

    public List<EventViewModel> BuildEvents(int count, EventType? type = null)
    {
        return _faker.Make(count, () => new EventViewModel
        {
            Id = Guid.NewGuid(),
            Title = _faker.Lorem.Sentence(3),
            Description = _faker.Lorem.Paragraph(),
            Date = _faker.Date.Future(30),
            EventType = type ?? _faker.PickRandom<EventType>(),
            Location = _faker.Address.StreetAddress(),
            Price = _faker.Random.Decimal(25, 125),
            Capacity = _faker.Random.Int(10, 60),
            RegisteredCount = _faker.Random.Int(0, 30),
            IsUserRegistered = _faker.Random.Bool()
        });
    }

    public MembershipStatsViewModel BuildStats()
    {
        return new MembershipStatsViewModel
        {
            IsVerified = _faker.Random.Bool(0.7f),
            EventsAttended = _faker.Random.Int(0, 100),
            MonthsAsMember = _faker.Random.Int(1, 48),
            Partners = _faker.Random.Int(0, 10)
        };
    }
}
```

### Test Scenarios

```csharp
public static class DashboardTestScenarios
{
    public static IEnumerable<object[]> UserRoleScenarios()
    {
        yield return new object[] { UserRole.Guest, false, false };
        yield return new object[] { UserRole.Member, true, false };
        yield return new object[] { UserRole.Administrator, true, true };
        yield return new object[] { UserRole.Teacher, true, false };
    }

    public static IEnumerable<object[]> VettingStatusScenarios()
    {
        yield return new object[] { VettingStatus.NotApplied, "Start your vetting journey" };
        yield return new object[] { VettingStatus.ApplicationSubmitted, "Application under review" };
        yield return new object[] { VettingStatus.InterviewScheduled, "Interview scheduled" };
        yield return new object[] { VettingStatus.Approved, "Welcome to the community!" };
        yield return new object[] { VettingStatus.Rejected, "We're sorry, your application was not approved" };
    }

    public static IEnumerable<object[]> LoadingStateScenarios()
    {
        yield return new object[] { 0, "Instant load" };
        yield return new object[] { 100, "Fast load" };
        yield return new object[] { 500, "Normal load" };
        yield return new object[] { 2000, "Slow load" };
        yield return new object[] { 5000, "Very slow load" };
    }
}
```

## Continuous Integration

### GitHub Actions Test Workflow

```yaml
name: Dashboard CI/CD

on:
  push:
    branches: [ main, develop ]
    paths:
      - 'src/WitchCityRope.Web/Features/Members/**'
      - 'tests/**/*Dashboard*'
  pull_request:
    paths:
      - 'src/WitchCityRope.Web/Features/Members/**'

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:16
        env:
          POSTGRES_PASSWORD: testpass
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Unit Tests
      run: |
        dotnet test tests/WitchCityRope.Web.Tests \
          --no-build \
          --verbosity normal \
          --logger "trx;LogFileName=unit-tests.trx" \
          --collect:"XPlat Code Coverage" \
          --filter "FullyQualifiedName~Dashboard"
    
    - name: Integration Tests
      run: |
        dotnet test tests/WitchCityRope.IntegrationTests \
          --no-build \
          --verbosity normal \
          --logger "trx;LogFileName=integration-tests.trx" \
          --filter "Category=Dashboard"
    
    - name: E2E Tests
      run: |
        # Install Playwright
        pwsh tests/WitchCityRope.E2E.Tests/bin/Debug/net9.0/playwright.ps1 install
        
        # Run E2E tests
        dotnet test tests/WitchCityRope.E2E.Tests \
          --no-build \
          --verbosity normal \
          --logger "trx;LogFileName=e2e-tests.trx" \
          --filter "Feature=Dashboard"
    
    - name: Generate Coverage Report
      run: |
        dotnet tool install --global dotnet-reportgenerator-globaltool
        reportgenerator \
          -reports:"tests/**/coverage.cobertura.xml" \
          -targetdir:"coverage" \
          -reporttypes:"HtmlInline_AzurePipelines;Cobertura"
    
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: |
          **/*.trx
          coverage/**
          tests/**/screenshots/**
    
    - name: Comment PR with Coverage
      uses: 5monkeys/cobertura-action@master
      if: github.event_name == 'pull_request'
      with:
        path: coverage/Cobertura.xml
        minimum_coverage: 90
        fail_below_threshold: true
```

## Test Metrics and Reporting

### Coverage Requirements

| Component | Target | Minimum |
|-----------|--------|---------|
| Dashboard.razor | 95% | 90% |
| DashboardService | 95% | 90% |
| DashboardController | 90% | 85% |
| Sub-components | 90% | 85% |
| Overall | 92% | 88% |

### Performance Budgets

| Metric | Target | Maximum |
|--------|--------|---------|
| First Contentful Paint | 1.2s | 1.5s |
| Largest Contentful Paint | 2.0s | 2.5s |
| Time to Interactive | 2.5s | 3.0s |
| Total Blocking Time | 200ms | 300ms |
| Cumulative Layout Shift | 0.05 | 0.1 |

### Accessibility Requirements

- WCAG 2.1 AA compliance
- Keyboard navigation for all interactive elements
- Screen reader compatibility
- Color contrast ratio ≥ 4.5:1 for normal text
- Color contrast ratio ≥ 3:1 for large text
- Focus indicators visible

## Test Execution Strategy

### Local Development

```bash
# Run all dashboard tests
dotnet test --filter "FullyQualifiedName~Dashboard"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --filter "FullyQualifiedName~Dashboard"

# Run specific test category
dotnet test --filter "Category=Dashboard&Category=Unit"

# Watch mode for TDD
dotnet watch test --filter "FullyQualifiedName~Dashboard"
```

### Pre-commit Hooks

```bash
#!/bin/bash
# .git/hooks/pre-commit

# Run dashboard tests
echo "Running dashboard tests..."
dotnet test tests/WitchCityRope.Web.Tests \
  --filter "FullyQualifiedName~Dashboard" \
  --no-build \
  --verbosity quiet

if [ $? -ne 0 ]; then
  echo "❌ Dashboard tests failed. Please fix before committing."
  exit 1
fi

echo "✅ Dashboard tests passed!"
```

### CI/CD Pipeline

1. **Build Stage**: Compile and validate
2. **Unit Test Stage**: Fast feedback
3. **Integration Test Stage**: API verification
4. **E2E Test Stage**: User journey validation
5. **Visual Test Stage**: Screenshot comparison
6. **Deploy Stage**: If all tests pass

## Test Maintenance

### Best Practices

1. **Keep Tests Fast**: Aim for < 100ms per unit test
2. **Use Test Builders**: Reduce test data setup boilerplate
3. **Avoid Brittle Selectors**: Use data attributes for E2E tests
4. **Mock External Dependencies**: Keep tests isolated
5. **Regular Cleanup**: Remove obsolete tests
6. **Document Complex Tests**: Add comments for non-obvious assertions

### Test Review Checklist

- [ ] New feature has corresponding tests
- [ ] Tests cover happy path and edge cases
- [ ] Tests are readable and maintainable
- [ ] No hard-coded test data
- [ ] Appropriate use of mocks
- [ ] Tests run in isolation
- [ ] Performance impact considered
- [ ] Accessibility requirements tested