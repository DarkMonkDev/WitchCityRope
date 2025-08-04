# User Dashboard Implementation Plan

## Executive Summary

This document outlines the comprehensive plan for implementing the user dashboard feature for WitchCityRope. The dashboard will serve as the primary landing page for authenticated members, providing personalized content, quick access to key features, and an overview of their community engagement.

## Table of Contents

1. [Requirements Analysis](#requirements-analysis)
2. [Technical Design](#technical-design)
3. [Implementation Phases](#implementation-phases)
4. [Testing Strategy](#testing-strategy)
5. [MCP Visual Testing Plan](#mcp-visual-testing-plan)
6. [CI/CD Integration](#cicd-integration)
7. [Documentation Updates](#documentation-updates)

## Requirements Analysis

### Functional Requirements

Based on wireframe analysis and business requirements:

#### 1. **Core Dashboard Features**
- Personalized welcome message with user's scene name
- Quick action buttons (Browse Classes, View Meetups, Update Profile)
- Responsive layout with main content and sidebar
- Progressive disclosure based on membership status

#### 2. **Information Display**
- **Upcoming Events Section**
  - Next 3 registered events with date badges
  - Registration status (Registered, RSVP'd, Waitlisted)
  - Empty state with browse events CTA
  
- **Membership Status Card**
  - Visual status indicator (Verified/Unverified)
  - Member statistics (events attended, membership duration, partners)
  - Progressive status messages based on vetting stage

- **Quick Links Section**
  - Icon-based navigation to key resources
  - Hover animations and tooltips

#### 3. **Role-Based Features**
- Admin users see additional admin tools section
- Teachers see class management options
- Non-vetted members see vetting application prompts

### Non-Functional Requirements

1. **Performance**
   - Dashboard loads within 2 seconds
   - Lazy loading for event recommendations
   - Cached user statistics

2. **Accessibility**
   - WCAG 2.1 AA compliance
   - Keyboard navigation support
   - Screen reader compatibility

3. **Responsive Design**
   - Mobile-first approach
   - Breakpoint at 1024px
   - Touch-friendly interactions

4. **Security**
   - Scene names only (no legal names)
   - Venue addresses hidden until registered
   - Audit trail for all actions

## Technical Design

### Component Architecture

```
/Features/Members/Pages/
├── Dashboard.razor              # Main dashboard page
├── Dashboard.razor.cs           # Code-behind file
└── Dashboard.razor.css          # Scoped styles

/Features/Members/Components/
├── DashboardHeader.razor        # Welcome section
├── UpcomingEvents.razor         # Events list component
├── MembershipStatus.razor       # Status card
├── QuickLinks.razor             # Quick links grid
└── DashboardSkeleton.razor      # Loading state
```

### Service Layer

```csharp
public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(Guid userId);
    Task<List<EventViewModel>> GetUpcomingEventsAsync(Guid userId, int count = 3);
    Task<MembershipStatsViewModel> GetMembershipStatsAsync(Guid userId);
}
```

### Data Models

```csharp
public class DashboardViewModel
{
    public string SceneName { get; set; }
    public UserRole Role { get; set; }
    public VettingStatus VettingStatus { get; set; }
    public List<EventViewModel> UpcomingEvents { get; set; }
    public MembershipStatsViewModel Stats { get; set; }
}

public class MembershipStatsViewModel
{
    public bool IsVerified { get; set; }
    public int EventsAttended { get; set; }
    public int MonthsAsMember { get; set; }
    public int Partners { get; set; }
    public DateTime? NextInterviewDate { get; set; }
}
```

### API Endpoints

```
GET /api/dashboard/{userId} - Get complete dashboard data
GET /api/users/{userId}/upcoming-events?count=3
GET /api/users/{userId}/stats
```

## Implementation Phases

### Phase 1: Core Dashboard Structure (Day 1-2)

1. **Create Base Components**
   - Dashboard.razor page with authentication
   - Basic layout with responsive grid
   - Welcome header component
   - Loading states with skeleton loader

2. **Service Implementation**
   - Create IDashboardService interface
   - Implement mock service for development
   - Wire up dependency injection

3. **Routing and Navigation**
   - Configure dashboard route (/member/dashboard)
   - Update navigation to highlight active page
   - Implement redirect after login

### Phase 2: Data Integration (Day 3-4)

1. **API Integration**
   - Create dashboard controller endpoints
   - Implement data aggregation queries
   - Add caching for performance

2. **Component Data Binding**
   - Wire up real data to components
   - Implement error handling
   - Add retry mechanisms

3. **State Management**
   - Handle loading states
   - Implement refresh functionality
   - Cache management

### Phase 3: Interactive Features (Day 5-6)

1. **Event Actions**
   - View ticket functionality
   - Quick RSVP for meetups
   - Cancel registration

2. **Progressive Disclosure**
   - Vetting status-based content
   - Role-based sections
   - Dynamic CTAs

3. **Animations and Polish**
   - Hover effects on cards
   - Smooth transitions
   - Loading animations

### Phase 4: Testing and Refinement (Day 7-8)

1. **Unit Testing**
   - Component tests with bUnit
   - Service layer tests
   - API endpoint tests

2. **Integration Testing**
   - Full dashboard flow tests
   - Authentication scenarios
   - Error handling verification

3. **Visual Testing**
   - MCP screenshot tests
   - Responsive layout verification
   - Cross-browser testing

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void Dashboard_ShowsWelcomeMessage_ForAuthenticatedUser()
{
    // Arrange
    var user = new UserDto { SceneName = "RopeMaster" };
    MockAuthService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
    
    // Act
    var component = RenderComponent<Dashboard>();
    
    // Assert
    component.Find(".welcome-message").TextContent
        .Should().Contain("Welcome back, RopeMaster!");
}

[Fact]
public void Dashboard_ShowsVettingPrompt_ForUnvettedUser()
{
    // Arrange
    var user = new UserDto { VettingStatus = VettingStatus.NotApplied };
    MockDashboardService.Setup(x => x.GetDashboardDataAsync(It.IsAny<Guid>()))
        .ReturnsAsync(new DashboardViewModel { VettingStatus = VettingStatus.NotApplied });
    
    // Act
    var component = RenderComponent<Dashboard>();
    
    // Assert
    component.Find(".vetting-prompt").Should().NotBeNull();
    component.Find(".vetting-prompt button").TextContent
        .Should().Contain("Start Vetting Process");
}
```

### Integration Tests

```csharp
[Fact]
public async Task Dashboard_RequiresAuthentication_RedirectsToLogin()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/member/dashboard");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Redirect);
    response.Headers.Location.ToString().Should().Contain("/auth/login");
}

[Fact]
public async Task Dashboard_LoadsUserData_ForAuthenticatedUser()
{
    // Arrange
    var client = _factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test", options => { });
        });
    }).CreateClient();
    
    // Act
    var response = await client.GetAsync("/member/dashboard");
    var content = await response.Content.ReadAsStringAsync();
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    content.Should().Contain("Welcome back");
    content.Should().Contain("Upcoming Events");
}
```

### E2E Tests

```csharp
[Fact]
public async Task Dashboard_CompleteUserFlow_WorksCorrectly()
{
    // Navigate to dashboard
    await Page.GotoAsync("/member/dashboard");
    
    // Verify welcome message
    await Expect(Page.Locator(".welcome-message"))
        .ToContainTextAsync("Welcome back");
    
    // Check upcoming events load
    await Page.WaitForSelectorAsync(".event-card");
    var eventCards = await Page.Locator(".event-card").CountAsync();
    Assert.Equal(3, eventCards);
    
    // Test quick action
    await Page.ClickAsync(".browse-classes-btn");
    await Expect(Page).ToHaveURLAsync("/events");
}
```

## MCP Visual Testing Plan

### Screenshot Capture Points

```javascript
// 1. Full dashboard load
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/member/dashboard",
  fullPage: true,
  waitForSelector: ".dashboard-loaded",
  filename: "dashboard-full-load.png"
});

// 2. Mobile responsive view
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/member/dashboard",
  viewport: { width: 375, height: 667 },
  filename: "dashboard-mobile.png"
});

// 3. Loading states
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/member/dashboard",
  waitForSelector: ".skeleton-loader",
  filename: "dashboard-loading.png"
});

// 4. Empty states
await mcp_browser_tools.screenshot({
  url: "https://localhost:8281/member/dashboard",
  beforeScreenshot: async (page) => {
    // Simulate no events
    await page.evaluate(() => {
      document.querySelector('.upcoming-events').innerHTML = 
        '<div class="empty-state">No upcoming events</div>';
    });
  },
  filename: "dashboard-empty-state.png"
});
```

### Accessibility Audit

```javascript
// Run comprehensive accessibility audit
const auditResults = await mcp_browser_tools.audit({
  url: "https://localhost:8281/member/dashboard",
  type: "accessibility",
  options: {
    includeWarnings: true,
    wcagLevel: "AA"
  }
});

// Performance audit
const perfResults = await mcp_browser_tools.audit({
  url: "https://localhost:8281/member/dashboard",
  type: "performance",
  options: {
    throttling: "3G",
    clearCache: true
  }
});
```

### Visual Regression Tests

```javascript
// Capture baseline
await mcp_puppeteer.screenshot({
  url: "https://localhost:8281/member/dashboard",
  fullPage: true,
  filename: "dashboard-baseline.png"
});

// After changes, compare
await mcp_browser_tools.compareScreenshots({
  baseline: "dashboard-baseline.png",
  current: "dashboard-current.png",
  threshold: 0.1, // 10% difference threshold
  highlightDifferences: true
});
```

## CI/CD Integration

### GitHub Actions Workflow

```yaml
name: Dashboard Tests

on:
  push:
    paths:
      - 'src/WitchCityRope.Web/Features/Members/**'
      - 'tests/**/*Dashboard*'
  pull_request:
    paths:
      - 'src/WitchCityRope.Web/Features/Members/**'

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Run Unit Tests
      run: |
        dotnet test tests/WitchCityRope.Web.Tests \
          --filter "FullyQualifiedName~Dashboard" \
          --logger "trx;LogFileName=dashboard-tests.trx"
    
    - name: Run Integration Tests
      run: |
        dotnet test tests/WitchCityRope.IntegrationTests \
          --filter "Category=Dashboard"
    
    - name: Run E2E Tests
      run: |
        dotnet test tests/WitchCityRope.E2E.Tests \
          --filter "Feature=Dashboard"
    
    - name: Upload Test Results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: |
          **/*.trx
          **/screenshots/**
```

### Pre-commit Hooks

```bash
#!/bin/bash
# .git/hooks/pre-commit

# Run dashboard tests before commit
dotnet test tests/WitchCityRope.Web.Tests \
  --filter "FullyQualifiedName~Dashboard" \
  --no-build

if [ $? -ne 0 ]; then
  echo "Dashboard tests failed. Commit aborted."
  exit 1
fi
```

## Documentation Updates

### 1. After Design Phase
- Update `/docs/design/features/dashboard.md` with final design decisions
- Add component interaction diagrams
- Document state management approach

### 2. After Initial Implementation
- Update `/docs/architecture/components.md` with dashboard components
- Add dashboard service to `/docs/architecture/services.md`
- Update API documentation with new endpoints

### 3. After Testing Complete
- Add test coverage report to `/docs/testing/coverage.md`
- Document visual regression test baselines
- Update `/docs/testing/e2e-scenarios.md` with dashboard flows

### 4. Project Documentation Updates
- Update README.md with dashboard feature description
- Add dashboard to feature list in CLAUDE.md
- Update deployment guide with dashboard-specific configurations

## Success Criteria

1. **Functional**
   - All wireframe features implemented
   - Role-based content working correctly
   - Data loads within 2 seconds

2. **Quality**
   - 90%+ unit test coverage
   - All integration tests passing
   - Zero accessibility violations

3. **Visual**
   - Matches wireframe design
   - Responsive on all devices
   - Smooth animations and transitions

4. **Performance**
   - Lighthouse score > 90
   - First contentful paint < 1.5s
   - Time to interactive < 3s

## Timeline

- **Day 1-2**: Core structure and components
- **Day 3-4**: Data integration and API
- **Day 5-6**: Interactive features and polish
- **Day 7-8**: Testing and refinement
- **Day 9**: Documentation and deployment

Total estimated time: 9 working days

## Risk Mitigation

1. **Performance Issues**
   - Implement pagination for event lists
   - Use virtual scrolling for large datasets
   - Cache dashboard data for 5 minutes

2. **Authentication Complexity**
   - Reuse existing auth patterns
   - Implement proper error boundaries
   - Add retry logic for token refresh

3. **Cross-browser Compatibility**
   - Test on Chrome, Firefox, Safari, Edge
   - Use CSS prefixes where needed
   - Implement polyfills for older browsers

## Next Steps

1. Review and approve this plan
2. Create feature branch `feature/user-dashboard`
3. Begin Phase 1 implementation
4. Schedule daily check-ins for progress updates