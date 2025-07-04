# User Dashboard Technical Design Document

## Overview

This document provides the detailed technical design for implementing the user dashboard feature, including component specifications, data flow, and integration points.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     Dashboard.razor                          │
│  ┌─────────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │ DashboardHeader │  │ Loading State│  │  Error State  │  │
│  └────────┬────────┘  └──────────────┘  └───────────────┘  │
│           │                                                  │
│  ┌────────▼────────────────────────────────────────────┐    │
│  │                  Main Content Area                   │    │
│  │  ┌──────────────────┐  ┌────────────────────────┐  │    │
│  │  │  UpcomingEvents  │  │  MembershipStatus     │  │    │
│  │  └──────────────────┘  └────────────────────────┘  │    │
│  │  ┌────────────────────────┐                          │    │
│  │  │    QuickLinks          │                          │    │
│  │  └────────────────────────┘                          │    │
│  └──────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │  DashboardService    │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │      ApiClient        │
                    └──────────┬───────────┘
                               │
                    ┌──────────▼───────────┐
                    │    Dashboard API      │
                    └──────────────────────┘
```

## Component Specifications

### Dashboard.razor

```razor
@page "/member/dashboard"
@layout MainLayout
@attribute [Authorize]
@inherits DashboardBase
@implements IDisposable

<PageTitle>Dashboard - WitchCity Rope</PageTitle>

@if (IsLoading)
{
    <DashboardSkeleton />
}
else if (HasError)
{
    <DashboardError ErrorMessage="@ErrorMessage" OnRetry="@LoadDashboardData" />
}
else if (DashboardData != null)
{
    <div class="dashboard-container">
        <CascadingValue Value="DashboardData">
            <DashboardHeader />
            
            <div class="dashboard-grid">
                <div class="dashboard-main">
                    <UpcomingEvents Events="@DashboardData.UpcomingEvents" />
                    <QuickLinks Role="@DashboardData.Role" />
                </div>
                
                <div class="dashboard-sidebar">
                    <MembershipStatus Stats="@DashboardData.Stats" />
                </div>
            </div>
            
            @if (DashboardData.Role == UserRole.Administrator)
            {
                <AdminQuickAccess />
            }
        </CascadingValue>
    </div>
}

<style>
    .dashboard-container {
        max-width: var(--wcr-container-xl);
        margin: 0 auto;
        padding: var(--wcr-spacing-lg);
    }
    
    .dashboard-grid {
        display: grid;
        grid-template-columns: 2fr 1fr;
        gap: var(--wcr-spacing-lg);
        margin-top: var(--wcr-spacing-xl);
    }
    
    .dashboard-main {
        display: flex;
        flex-direction: column;
        gap: var(--wcr-spacing-lg);
    }
    
    .dashboard-sidebar {
        display: flex;
        flex-direction: column;
        gap: var(--wcr-spacing-lg);
    }
    
    @media (max-width: 1024px) {
        .dashboard-grid {
            grid-template-columns: 1fr;
        }
    }
</style>
```

### Dashboard.razor.cs (Code-behind)

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;
using System;
using System.Threading.Tasks;

namespace WitchCityRope.Web.Features.Members.Pages
{
    public class DashboardBase : ComponentBase
    {
        [Inject] protected IDashboardService DashboardService { get; set; }
        [Inject] protected IAuthService AuthService { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        [Inject] protected IToastService ToastService { get; set; }
        [Inject] protected ILogger<Dashboard> Logger { get; set; }
        
        protected DashboardViewModel? DashboardData { get; set; }
        protected bool IsLoading { get; set; } = true;
        protected bool HasError { get; set; }
        protected string? ErrorMessage { get; set; }
        
        private CancellationTokenSource? _cancellationTokenSource;
        private Timer? _refreshTimer;
        
        protected override async Task OnInitializedAsync()
        {
            await LoadDashboardData();
            
            // Set up auto-refresh every 5 minutes
            _refreshTimer = new Timer(async _ => await RefreshData(), null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }
        
        protected async Task LoadDashboardData()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                ErrorMessage = null;
                
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                
                var currentUser = await AuthService.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    Navigation.NavigateTo("/auth/login");
                    return;
                }
                
                DashboardData = await DashboardService.GetDashboardDataAsync(
                    currentUser.Id, _cancellationTokenSource.Token);
                    
                Logger.LogInformation("Dashboard loaded for user {UserId}", currentUser.Id);
            }
            catch (OperationCanceledException)
            {
                // Request was cancelled, ignore
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading dashboard");
                HasError = true;
                ErrorMessage = "Unable to load dashboard. Please try again.";
                ToastService.ShowError(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
        
        protected async Task RefreshData()
        {
            await InvokeAsync(async () =>
            {
                await LoadDashboardData();
            });
        }
        
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _refreshTimer?.Dispose();
        }
    }
}
```

### DashboardService Implementation

```csharp
using WitchCityRope.Web.Models;
using WitchCityRope.Web.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WitchCityRope.Web.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApiClient _apiClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
        
        public DashboardService(
            ApiClient apiClient, 
            IMemoryCache cache,
            ILogger<DashboardService> logger)
        {
            _apiClient = apiClient;
            _cache = cache;
            _logger = logger;
        }
        
        public async Task<DashboardViewModel> GetDashboardDataAsync(
            Guid userId, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"dashboard_{userId}";
            
            if (_cache.TryGetValue<DashboardViewModel>(cacheKey, out var cachedData))
            {
                _logger.LogDebug("Returning cached dashboard for user {UserId}", userId);
                return cachedData;
            }
            
            try
            {
                // Fetch all data in parallel for performance
                var upcomingEventsTask = GetUpcomingEventsAsync(userId, 3, cancellationToken);
                var statsTask = GetMembershipStatsAsync(userId, cancellationToken);
                var userTask = _apiClient.GetAsync<UserDto>($"users/{userId}", cancellationToken);
                
                await Task.WhenAll(upcomingEventsTask, statsTask, userTask);
                
                var dashboardData = new DashboardViewModel
                {
                    SceneName = userTask.Result.SceneName,
                    Role = userTask.Result.Role,
                    VettingStatus = userTask.Result.VettingStatus,
                    UpcomingEvents = await upcomingEventsTask,
                    Stats = await statsTask
                };
                
                _cache.Set(cacheKey, dashboardData, _cacheExpiration);
                _logger.LogInformation("Dashboard data cached for user {UserId}", userId);
                
                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard data for user {UserId}", userId);
                throw;
            }
        }
        
        public async Task<List<EventViewModel>> GetUpcomingEventsAsync(
            Guid userId, int count = 3, CancellationToken cancellationToken = default)
        {
            var response = await _apiClient.GetAsync<List<EventViewModel>>(
                $"users/{userId}/upcoming-events?count={count}", cancellationToken);
            return response ?? new List<EventViewModel>();
        }
        
        
        public async Task<MembershipStatsViewModel> GetMembershipStatsAsync(
            Guid userId, CancellationToken cancellationToken = default)
        {
            var response = await _apiClient.GetAsync<MembershipStatsViewModel>(
                $"users/{userId}/stats", cancellationToken);
            return response ?? new MembershipStatsViewModel();
        }
    }
}
```

## API Design

### Dashboard Controller

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IUserService _userService;
    private readonly IEventService _eventService;
    private readonly ILogger<DashboardController> _logger;
    
    // ... constructor
    
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDashboard(Guid userId)
    {
        // Verify user can access this dashboard
        if (!await CanAccessDashboard(userId))
            return Forbid();
            
        var dashboard = await _dashboardRepository.GetDashboardDataAsync(userId);
        if (dashboard == null)
            return NotFound();
            
        return Ok(dashboard);
    }
    
    [HttpGet("users/{userId}/upcoming-events")]
    public async Task<IActionResult> GetUpcomingEvents(
        Guid userId, 
        [FromQuery] int count = 3)
    {
        var events = await _eventService.GetUpcomingEventsForUserAsync(userId, count);
        return Ok(events);
    }
    
    
    [HttpGet("users/{userId}/stats")]
    public async Task<IActionResult> GetMembershipStats(Guid userId)
    {
        var stats = await _userService.GetMembershipStatsAsync(userId);
        return Ok(stats);
    }
}
```

## State Management

### Dashboard State Flow

```
┌─────────────┐     ┌──────────────┐     ┌───────────────┐
│   Initial   │────▶│   Loading    │────▶│   Loaded      │
│   State     │     │   State      │     │   State       │
└─────────────┘     └──────┬───────┘     └───────┬───────┘
                           │                      │
                           ▼                      ▼
                    ┌──────────────┐       ┌─────────────┐
                    │ Error State  │       │   Refresh   │
                    └──────────────┘       └─────────────┘
```

### State Transitions

1. **Initial → Loading**: On component initialization
2. **Loading → Loaded**: When data fetch succeeds
3. **Loading → Error**: When data fetch fails
4. **Error → Loading**: On retry
5. **Loaded → Refresh**: Auto-refresh timer or manual refresh

## Error Handling

### Service Layer Errors

```csharp
public class DashboardErrorBoundary : ErrorBoundary
{
    [Inject] private ILogger<DashboardErrorBoundary> Logger { get; set; }
    [Inject] private IToastService ToastService { get; set; }
    
    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception, "Dashboard error boundary triggered");
        
        var errorMessage = exception switch
        {
            UnauthorizedException => "You don't have permission to view this dashboard",
            ApiException apiEx when apiEx.StatusCode == 404 => "Dashboard data not found",
            NetworkException => "Unable to connect to server. Please check your connection.",
            _ => "An unexpected error occurred. Please refresh the page."
        };
        
        ToastService.ShowError(errorMessage);
        return base.OnErrorAsync(exception);
    }
}
```

### Component-Level Error Handling

```csharp
// In component
private async Task HandleEventAction(EventViewModel evt, string action)
{
    try
    {
        switch (action)
        {
            case "view-ticket":
                Navigation.NavigateTo($"/tickets/{evt.TicketId}");
                break;
            case "cancel":
                await EventService.CancelRegistrationAsync(evt.Id);
                ToastService.ShowSuccess("Registration cancelled");
                await RefreshData();
                break;
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error handling event action {Action}", action);
        ToastService.ShowError($"Failed to {action}. Please try again.");
    }
}
```

## Performance Optimizations

### 1. Data Caching Strategy

```csharp
public class CachedDashboardService : IDashboardService
{
    private readonly IDashboardService _innerService;
    private readonly IMemoryCache _cache;
    
    public async Task<DashboardViewModel> GetDashboardDataAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(
            $"dashboard_{userId}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                entry.SlidingExpiration = TimeSpan.FromMinutes(2);
                return await _innerService.GetDashboardDataAsync(userId, cancellationToken);
            });
    }
}
```

### 2. Lazy Loading Components

```razor
@* LazyLoadComponent.razor *@
@if (_isLoaded)
{
    @ChildContent
}
else
{
    <div @ref="_element" style="min-height: 200px;">
        <SkeletonLoader Type="@SkeletonType" />
    </div>
}

@code {
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public SkeletonLoaderType SkeletonType { get; set; }
    
    private ElementReference _element;
    private bool _isLoaded;
    private IntersectionObserver? _observer;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _observer = await IntersectionObserver.CreateAsync(
                JSRuntime,
                OnIntersection,
                new IntersectionObserverOptions
                {
                    Threshold = 0.1
                });
                
            await _observer.ObserveAsync(_element);
        }
    }
    
    private async Task OnIntersection(IEnumerable<IntersectionObserverEntry> entries)
    {
        var entry = entries.FirstOrDefault();
        if (entry?.IsIntersecting == true)
        {
            _isLoaded = true;
            StateHasChanged();
            
            if (_observer != null)
            {
                await _observer.UnobserveAsync(_element);
                await _observer.DisposeAsync();
            }
        }
    }
}
```

### 3. Virtual Scrolling for Large Lists

```razor
<SfGrid DataSource="@Events" EnableVirtualization="true" Height="400">
    <GridColumns>
        <GridColumn Field="@nameof(EventViewModel.Title)" HeaderText="Event" />
        <GridColumn Field="@nameof(EventViewModel.Date)" HeaderText="Date" Format="d" />
        <GridColumn Field="@nameof(EventViewModel.Status)" HeaderText="Status" />
    </GridColumns>
</SfGrid>
```

## Security Considerations

### 1. Authorization Checks

```csharp
[Authorize]
public class Dashboard : ComponentBase
{
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        
        if (!user.Identity.IsAuthenticated)
        {
            Navigation.NavigateTo("/auth/login");
            return;
        }
        
        // Additional role checks
        if (user.IsInRole("Banned") || user.IsInRole("Suspended"))
        {
            Navigation.NavigateTo("/account/suspended");
            return;
        }
    }
}
```

### 2. Data Sanitization

```csharp
public class EventViewModel
{
    private string _title;
    
    public string Title 
    { 
        get => _title;
        set => _title = HtmlEncoder.Default.Encode(value ?? string.Empty);
    }
    
    // Venue address hidden until registered
    public string Location => IsRegistered ? VenueAddress : "Address available after registration";
}
```

### 3. CSRF Protection

```razor
@* Automatically included in forms *@
<EditForm Model="@model" OnValidSubmit="@HandleSubmit">
    <AntiforgeryToken />
    @* Form fields *@
</EditForm>
```

## Accessibility Requirements

### WCAG 2.1 AA Compliance

```razor
@* Semantic HTML *@
<main role="main" aria-label="Dashboard">
    <h1 id="dashboard-title">Welcome back, @SceneName!</h1>
    
    <section aria-labelledby="upcoming-events-title">
        <h2 id="upcoming-events-title">Upcoming Events</h2>
        @* Content *@
    </section>
</main>

@* Keyboard navigation *@
<button @onclick="HandleAction" 
        @onkeydown="@(e => HandleKeyDown(e))"
        tabindex="0"
        aria-label="View ticket for @eventTitle">
    View Ticket
</button>

@* Screen reader announcements *@
<div class="sr-only" aria-live="polite" aria-atomic="true">
    @LoadingAnnouncement
</div>
```

## Responsive Design Implementation

### Breakpoints

```css
/* Base styles - mobile first */
.dashboard-grid {
    display: grid;
    grid-template-columns: 1fr;
    gap: var(--wcr-spacing-md);
}

/* Tablet - 768px */
@media (min-width: 768px) {
    .dashboard-grid {
        gap: var(--wcr-spacing-lg);
    }
}

/* Desktop - 1024px */
@media (min-width: 1024px) {
    .dashboard-grid {
        grid-template-columns: 2fr 1fr;
    }
}

/* Large desktop - 1440px */
@media (min-width: 1440px) {
    .dashboard-container {
        max-width: 1320px;
    }
}
```

### Touch-Friendly Interactions

```css
/* Minimum touch target size */
.dashboard-button {
    min-height: 44px;
    min-width: 44px;
    padding: var(--wcr-spacing-sm) var(--wcr-spacing-md);
}

/* Remove hover effects on touch devices */
@media (hover: none) {
    .dashboard-card:hover {
        transform: none;
    }
}
```

## Integration Points

### 1. Navigation Service

```csharp
public interface INavigationService
{
    void NavigateToDashboard();
    void NavigateToEvent(Guid eventId);
    void NavigateToProfile();
    Task<bool> NavigateWithReturnUrl(string url);
}
```

### 2. Notification Hub

```csharp
// Real-time updates via SignalR
hubConnection.On<DashboardUpdateDto>("DashboardUpdated", async (update) =>
{
    if (update.UserId == CurrentUserId)
    {
        await RefreshSection(update.Section);
    }
});
```

### 3. Analytics Tracking

```csharp
// Track dashboard interactions
await Analytics.TrackEvent("Dashboard_Viewed", new
{
    UserId = currentUser.Id,
    Role = currentUser.Role,
    Device = GetDeviceType()
});

await Analytics.TrackEvent("Dashboard_QuickAction", new
{
    Action = actionName,
    Success = result
});
```

## Deployment Considerations

### 1. Feature Flags

```csharp
if (await FeatureFlags.IsEnabledAsync("NewDashboard"))
{
    Navigation.NavigateTo("/member/dashboard");
}
else
{
    Navigation.NavigateTo("/member/home"); // Old dashboard
}
```

### 2. A/B Testing

```csharp
var variant = await AbTesting.GetVariantAsync("DashboardLayout");
var layoutComponent = variant switch
{
    "A" => typeof(DashboardLayoutA),
    "B" => typeof(DashboardLayoutB),
    _ => typeof(DashboardLayoutA)
};
```

### 3. Monitoring

```csharp
// Performance monitoring
using (var activity = Activity.StartActivity("Dashboard.Load"))
{
    activity?.SetTag("user.id", userId);
    activity?.SetTag("user.role", userRole);
    
    // Load dashboard
    
    activity?.SetTag("load.duration", stopwatch.ElapsedMilliseconds);
}
```