# Blazor Architecture Design: User Management Admin Screen Redesign
<!-- Last Updated: 2025-08-12 -->
<!-- Version: 1.0 -->
<!-- Owner: Blazor Developer Agent -->
<!-- Status: Draft -->

## Executive Summary

This architecture design transforms the user management admin screen into a modern, desktop-focused Blazor Server application using Syncfusion components. The design prioritizes performance, maintainability, and role-based access while supporting polling-based updates and comprehensive vetting workflow management. The architecture follows WitchCityRope's established patterns with vertical slice organization and pure Blazor Server implementation.

## Component Hierarchy Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Admin Layout (Existing)                         │
└─────────────────────┬───────────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────────┐
│               UserManagementIndex.razor                            │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────┐   │
│  │   UserStats     │ │   UserFilters   │ │  UserDataGrid      │   │
│  │   Dashboard     │ │   Advanced      │ │  Enhanced          │   │
│  └─────────────────┘ └─────────────────┘ └─────────────────────┘   │
└─────────────────────┬───────────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────────┐
│                UserDetailPage.razor                                │
│  ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────────┐   │
│  │ UserDetailTabs  │ │VettingWorkflow  │ │   UserNotesPanel   │   │
│  │   Overview      │ │     Panel       │ │    Management      │   │
│  │   Events        │ │                 │ │                    │   │
│  │   Vetting       │ │                 │ │                    │   │
│  │   Notes         │ │                 │ │                    │   │
│  │   Actions       │ │                 │ │                    │   │
│  └─────────────────┘ └─────────────────┘ └─────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

## File Structure & Organization

### Vertical Slice Structure
```
/src/WitchCityRope.Web/Features/Admin/UserManagement/
├── Pages/
│   ├── UserManagementIndex.razor           # Main list page (enhanced)
│   ├── UserManagementIndex.razor.cs        # Code-behind with polling logic
│   ├── UserDetailPage.razor                # Detailed user view
│   └── UserDetailPage.razor.cs             # Code-behind with tab management
├── Components/
│   ├── Shared/
│   │   ├── UserStatsCard.razor             # Reusable stat cards
│   │   ├── UserStatusBadge.razor           # Status display component
│   │   └── LoadingSpinner.razor            # Loading states
│   ├── Grid/
│   │   ├── UserDataGrid.razor              # Enhanced Syncfusion grid
│   │   ├── UserDataGrid.razor.cs           # Grid logic and events
│   │   ├── UserFilters.razor               # Advanced filter panel
│   │   ├── UserFilters.razor.cs            # Filter state management
│   │   └── UserRowActions.razor            # Row action buttons
│   ├── Detail/
│   │   ├── UserDetailTabs.razor            # Main tabbed interface
│   │   ├── UserDetailTabs.razor.cs         # Tab state management
│   │   ├── UserOverviewTab.razor           # Profile overview tab
│   │   ├── UserEventsTab.razor             # Events history tab
│   │   ├── UserVettingTab.razor            # Vetting management tab
│   │   ├── UserNotesTab.razor              # Admin notes tab
│   │   └── UserActionsTab.razor            # Admin actions tab
│   ├── Vetting/
│   │   ├── VettingWorkflowPanel.razor      # Vetting status management
│   │   ├── VettingWorkflowPanel.razor.cs   # Workflow logic
│   │   ├── VettingStatusHistory.razor      # Status change history
│   │   └── VettingStatusUpdateModal.razor  # Status change modal
│   ├── Notes/
│   │   ├── UserNotesPanel.razor            # Notes management
│   │   ├── UserNotesPanel.razor.cs         # Notes logic
│   │   ├── AddNoteModal.razor              # Add note modal
│   │   └── NotesList.razor                 # Notes display list
│   └── Modals/
│       ├── UserEditModal.razor             # Enhanced edit modal
│       ├── UserEditModal.razor.cs          # Edit logic
│       ├── ConfirmActionModal.razor        # Confirmation dialogs
│       └── BulkActionModal.razor           # Future bulk operations
├── Services/
│   ├── IUserManagementService.cs           # Enhanced service interface
│   ├── UserManagementService.cs            # Service implementation
│   ├── IPollingService.cs                  # Polling service interface
│   ├── PollingService.cs                   # Polling implementation
│   └── UserManagementCacheService.cs       # Caching layer
├── Models/
│   ├── ViewModels/
│   │   ├── UserManagementViewModel.cs      # Main page view model
│   │   ├── UserDetailViewModel.cs          # Detail page view model
│   │   ├── UserFilterState.cs              # Filter state model
│   │   └── UserEditModel.cs                # Edit form model
│   ├── DTOs/
│   │   ├── EnhancedAdminUserDto.cs         # Enhanced user DTO
│   │   ├── UserEventsHistoryDto.cs         # Events history DTO
│   │   ├── UserNoteDto.cs                  # Admin notes DTO
│   │   └── VettingStatusUpdateDto.cs       # Vetting update DTO
│   └── Enums/
│       ├── UserFilterType.cs               # Filter enumeration
│       ├── VettingStatusTransition.cs      # Vetting transitions
│       └── NoteCategory.cs                 # Note categories
├── Validators/
│   ├── UserEditModelValidator.cs           # Edit form validation
│   ├── VettingUpdateValidator.cs           # Vetting validation
│   └── UserNoteValidator.cs                # Notes validation
└── Extensions/
    ├── UserManagementExtensions.cs         # Utility extensions
    └── SyncfusionExtensions.cs             # Syncfusion helpers
```

## Component Specifications

### 1. UserManagementIndex.razor (Main Page)
**Path**: `/admin/users`  
**Authorization**: `[Authorize(Roles = "Administrator,EventOrganizer")]`  
**Render Mode**: `@rendermode="InteractiveServer"`

```razor
@page "/admin/users"
@rendermode InteractiveServer
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize(Roles = "Administrator,EventOrganizer")]
@inject IUserManagementService UserService
@inject IPollingService PollingService
@inject NavigationManager Navigation
@inject ILogger<UserManagementIndex> Logger
@implements IDisposable

<PageTitle>User Management - WitchCityRope Admin</PageTitle>

<div class="user-management-container">
    <!-- Stats Dashboard -->
    <div class="stats-section">
        <UserStatsCard Title="Total Users" 
                      Value="@statsModel.TotalUsers" 
                      Icon="fas fa-users" 
                      Color="primary" />
        <UserStatsCard Title="Vetted Members" 
                      Value="@statsModel.VettedUsers" 
                      Icon="fas fa-user-check" 
                      Color="success" />
        <UserStatsCard Title="Pending Applications" 
                      Value="@statsModel.PendingApplications" 
                      Icon="fas fa-clock" 
                      Color="warning" />
        <UserStatsCard Title="Needs Attention" 
                      Value="@statsModel.NeedsAttention" 
                      Icon="fas fa-exclamation-triangle" 
                      Color="danger" />
    </div>

    <!-- Main Content -->
    <div class="main-content">
        <div class="content-header">
            <h2>User Management</h2>
            <div class="header-actions">
                <SfButton CssClass="e-info" OnClick="RefreshData">
                    <span class="fas fa-sync-alt"></span> Refresh
                </SfButton>
                @if (isPollingActive)
                {
                    <span class="polling-indicator active">
                        <span class="fas fa-circle"></span> Auto-updating
                    </span>
                }
            </div>
        </div>

        <!-- Filters Panel -->
        <div class="filters-section">
            <UserFilters FilterState="@filterState" 
                        OnFiltersChanged="OnFiltersChanged"
                        IsLoading="@isLoading" />
        </div>

        <!-- Data Grid -->
        <div class="grid-section">
            @if (isLoading)
            {
                <div class="loading-overlay">
                    <SfSpinner @bind-Visible="isLoading" Size="SpinnerSize.Large" />
                    <p>Loading user data...</p>
                </div>
            }
            else if (hasError)
            {
                <div class="error-message">
                    <div class="alert alert-danger">
                        <strong>Error:</strong> @errorMessage
                        <SfButton CssClass="e-outline e-danger" OnClick="RefreshData">
                            Retry
                        </SfButton>
                    </div>
                </div>
            }
            else
            {
                <UserDataGrid Users="@filteredUsers"
                             TotalCount="@totalCount"
                             CurrentPage="@currentPage"
                             PageSize="@pageSize"
                             OnUserSelected="NavigateToDetail"
                             OnPageChanged="OnPageChanged"
                             OnSortChanged="OnSortChanged"
                             UserRole="@currentUserRole"
                             IsEventOrganizer="@isEventOrganizer" />
            }
        </div>
    </div>
</div>

@code {
    // Component implementation in code-behind
}
```

#### Code-Behind: UserManagementIndex.razor.cs
```csharp
public partial class UserManagementIndex : ComponentBase, IDisposable
{
    // State Management
    private List<EnhancedAdminUserDto> filteredUsers = new();
    private UserManagementStatsDto statsModel = new();
    private UserFilterState filterState = new();
    private bool isLoading = true;
    private bool hasError = false;
    private string errorMessage = string.Empty;
    private bool isPollingActive = true;
    private int totalCount = 0;
    private int currentPage = 1;
    private int pageSize = 50;
    private UserRole currentUserRole;
    private bool isEventOrganizer;
    
    // Polling
    private Timer? pollingTimer;
    private readonly TimeSpan pollingInterval = TimeSpan.FromSeconds(30);
    
    protected override async Task OnInitializedAsync()
    {
        try
        {
            // Initialize user role context
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            currentUserRole = GetUserRole(authState.User);
            isEventOrganizer = authState.User.IsInRole("EventOrganizer");
            
            // Load initial data
            await LoadData();
            
            // Start polling
            StartPolling();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error initializing user management page");
            hasError = true;
            errorMessage = "Failed to initialize page";
        }
        finally
        {
            isLoading = false;
        }
    }
    
    private async Task LoadData()
    {
        isLoading = true;
        hasError = false;
        
        try
        {
            var searchRequest = new UserSearchRequest
            {
                Filters = filterState.GetActiveFilters(),
                PageNumber = currentPage,
                PageSize = pageSize,
                SortBy = filterState.SortBy,
                SortDescending = filterState.SortDescending
            };
            
            var result = await UserService.GetUsersAsync(searchRequest);
            if (result.IsSuccess)
            {
                filteredUsers = result.Value.Items;
                totalCount = result.Value.TotalCount;
                
                // Load stats
                var statsResult = await UserService.GetUserStatsAsync();
                if (statsResult.IsSuccess)
                {
                    statsModel = statsResult.Value;
                }
            }
            else
            {
                hasError = true;
                errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading user data");
            hasError = true;
            errorMessage = "Failed to load user data";
        }
        finally
        {
            isLoading = false;
        }
    }
    
    private void StartPolling()
    {
        pollingTimer = new Timer(async _ =>
        {
            if (!isLoading)
            {
                await InvokeAsync(async () =>
                {
                    await LoadData();
                    StateHasChanged();
                });
            }
        }, null, pollingInterval, pollingInterval);
    }
    
    private async Task OnFiltersChanged(UserFilterState newFilterState)
    {
        filterState = newFilterState;
        currentPage = 1; // Reset to first page
        await LoadData();
    }
    
    private void NavigateToDetail(Guid userId)
    {
        Navigation.NavigateTo($"/admin/users/{userId}");
    }
    
    public void Dispose()
    {
        pollingTimer?.Dispose();
    }
}
```

### 2. UserDataGrid.razor (Enhanced Grid Component)
**Purpose**: High-performance Syncfusion grid with virtual scrolling and role-based columns

```razor
@using Syncfusion.Blazor.Grids
@using Syncfusion.Blazor.DropDowns

<div class="user-data-grid-container">
    <SfGrid DataSource="@Users" 
            AllowPaging="true" 
            AllowSorting="true" 
            AllowFiltering="true"
            AllowVirtualScrolling="true"
            Height="600px"
            GridLines="GridLine.Both">
        
        <GridPageSettings PageSize="@PageSize" 
                         PageCount="5" 
                         CurrentPage="@CurrentPage" />
        
        <GridSortSettings AllowUnsort="true" />
        
        <GridFilterSettings Type="FilterType.FilterBar" />
        
        <GridEvents TRowSelected="OnRowSelected" 
                   OnActionBegin="OnGridActionBegin"
                   OnActionComplete="OnGridActionComplete"
                   TValue="EnhancedAdminUserDto" />
        
        <GridColumns>
            <!-- Scene Name Column -->
            <GridColumn Field="@nameof(EnhancedAdminUserDto.SceneName)" 
                       HeaderText="Scene Name" 
                       Width="200px"
                       IsPrimaryKey="true">
                <Template>
                    @{
                        var user = (context as EnhancedAdminUserDto);
                        <div class="user-scene-name">
                            <strong>@user.SceneName</strong>
                            @if (user.IsNewMember)
                            {
                                <span class="badge badge-new">NEW</span>
                            }
                        </div>
                    }
                </Template>
            </GridColumn>

            <!-- Status Column -->
            <GridColumn Field="@nameof(EnhancedAdminUserDto.VettingStatus)" 
                       HeaderText="Status" 
                       Width="120px">
                <Template>
                    @{
                        var user = (context as EnhancedAdminUserDto);
                        <UserStatusBadge VettingStatus="@user.VettingStatus" 
                                        IsActive="@user.IsActive" />
                    }
                </Template>
            </GridColumn>

            <!-- Contact Info (Admin Only) -->
            @if (!IsEventOrganizer)
            {
                <GridColumn Field="@nameof(EnhancedAdminUserDto.Email)" 
                           HeaderText="Email" 
                           Width="200px" />
                
                <GridColumn Field="@nameof(EnhancedAdminUserDto.FirstName)" 
                           HeaderText="Real Name" 
                           Width="180px">
                    <Template>
                        @{
                            var user = (context as EnhancedAdminUserDto);
                            <span>@GetDisplayName(user)</span>
                        }
                    </Template>
                </GridColumn>
            }

            <!-- Membership Info -->
            <GridColumn Field="@nameof(EnhancedAdminUserDto.Role)" 
                       HeaderText="Role" 
                       Width="120px">
                <Template>
                    @{
                        var user = (context as EnhancedAdminUserDto);
                        <span class="role-badge role-@user.Role.ToString().ToLower()">
                            @user.Role.GetDisplayName()
                        </span>
                    }
                </Template>
            </GridColumn>

            <!-- Activity Metrics -->
            <GridColumn Field="@nameof(EnhancedAdminUserDto.EventsAttended)" 
                       HeaderText="Events" 
                       Width="100px"
                       TextAlign="TextAlign.Center" />

            <GridColumn Field="@nameof(EnhancedAdminUserDto.LastActivityAt)" 
                       HeaderText="Last Active" 
                       Width="140px"
                       Format="MMM dd, yyyy">
                <Template>
                    @{
                        var user = (context as EnhancedAdminUserDto);
                        if (user.LastActivityAt.HasValue)
                        {
                            <span title="@user.LastActivityAt.Value.ToString("F")">
                                @GetRelativeTime(user.LastActivityAt.Value)
                            </span>
                        }
                        else
                        {
                            <span class="text-muted">Never</span>
                        }
                    }
                </Template>
            </GridColumn>

            <!-- Admin Notes Indicator -->
            <GridColumn HeaderText="Notes" 
                       Width="80px"
                       TextAlign="TextAlign.Center">
                <Template>
                    @{
                        var user = (context as EnhancedAdminUserDto);
                        if (user.AdminNotes.Any())
                        {
                            var safetyNotesCount = user.AdminNotes.Count(n => n.Category == "Safety");
                            <div class="notes-indicator">
                                <span class="fas fa-sticky-note notes-icon" 
                                      title="@user.AdminNotes.Count notes">
                                </span>
                                @if (safetyNotesCount > 0)
                                {
                                    <span class="fas fa-exclamation-triangle safety-icon" 
                                          title="@safetyNotesCount safety notes">
                                    </span>
                                }
                            </div>
                        }
                    }
                </Template>
            </GridColumn>

            <!-- Actions Column -->
            <GridColumn HeaderText="Actions" 
                       Width="120px"
                       AllowSorting="false"
                       AllowFiltering="false">
                <Template>
                    @{
                        var user = (context as EnhancedAdminUserDto);
                        <UserRowActions User="@user"
                                       UserRole="@UserRole"
                                       IsEventOrganizer="@IsEventOrganizer"
                                       OnViewDetails="OnViewDetailsClicked" />
                    }
                </Template>
            </GridColumn>
        </GridColumns>
    </SfGrid>
</div>

@code {
    [Parameter] public List<EnhancedAdminUserDto> Users { get; set; } = new();
    [Parameter] public int TotalCount { get; set; }
    [Parameter] public int CurrentPage { get; set; } = 1;
    [Parameter] public int PageSize { get; set; } = 50;
    [Parameter] public UserRole UserRole { get; set; }
    [Parameter] public bool IsEventOrganizer { get; set; }
    
    [Parameter] public EventCallback<Guid> OnUserSelected { get; set; }
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }
    [Parameter] public EventCallback<GridSortEventArgs> OnSortChanged { get; set; }
    
    private async Task OnRowSelected(RowSelectEventArgs<EnhancedAdminUserDto> args)
    {
        if (args.Data != null)
        {
            await OnUserSelected.InvokeAsync(args.Data.Id);
        }
    }
    
    private async Task OnViewDetailsClicked(EnhancedAdminUserDto user)
    {
        await OnUserSelected.InvokeAsync(user.Id);
    }
    
    private string GetDisplayName(EnhancedAdminUserDto user)
    {
        if (!string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName))
        {
            return $"{user.FirstName} {user.LastName}";
        }
        return "Not provided";
    }
    
    private string GetRelativeTime(DateTime dateTime)
    {
        var diff = DateTime.UtcNow - dateTime;
        return diff.TotalDays switch
        {
            < 1 => "Today",
            < 7 => $"{(int)diff.TotalDays} days ago",
            < 30 => $"{(int)(diff.TotalDays / 7)} weeks ago",
            _ => dateTime.ToString("MMM dd, yyyy")
        };
    }
}
```

### 3. UserDetailPage.razor (Detail View)
**Path**: `/admin/users/{id:guid}`  
**Authorization**: Role-based access control

```razor
@page "/admin/users/{UserId:guid}"
@rendermode InteractiveServer
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize(Roles = "Administrator,EventOrganizer")]
@inject IUserManagementService UserService
@inject NavigationManager Navigation
@inject ILogger<UserDetailPage> Logger

<PageTitle>@GetPageTitle() - User Management</PageTitle>

<div class="user-detail-container">
    @if (isLoading)
    {
        <div class="loading-container">
            <SfSpinner @bind-Visible="isLoading" Size="SpinnerSize.Large" />
            <p>Loading user details...</p>
        </div>
    }
    else if (hasError)
    {
        <div class="error-container">
            <div class="alert alert-danger">
                <h4>Error Loading User</h4>
                <p>@errorMessage</p>
                <SfButton OnClick="ReturnToList" CssClass="e-outline">
                    Return to List
                </SfButton>
                <SfButton OnClick="RefreshUser" CssClass="e-info">
                    Retry
                </SfButton>
            </div>
        </div>
    }
    else if (user != null)
    {
        <!-- Header Section -->
        <div class="user-detail-header">
            <div class="header-left">
                <SfButton OnClick="ReturnToList" 
                         CssClass="e-outline e-small">
                    <span class="fas fa-arrow-left"></span> Back to List
                </SfButton>
                <div class="user-title">
                    <h1>@user.SceneName</h1>
                    <UserStatusBadge VettingStatus="@user.VettingStatus" 
                                    IsActive="@user.IsActive" />
                </div>
            </div>
            <div class="header-actions">
                @if (CanEditUser())
                {
                    <SfButton OnClick="ShowEditModal" CssClass="e-primary">
                        <span class="fas fa-edit"></span> Edit User
                    </SfButton>
                }
                <SfButton OnClick="RefreshUser" CssClass="e-info">
                    <span class="fas fa-sync-alt"></span> Refresh
                </SfButton>
            </div>
        </div>

        <!-- Tab Container -->
        <div class="tab-container">
            <UserDetailTabs User="@user"
                          UserRole="@currentUserRole"
                          IsEventOrganizer="@isEventOrganizer"
                          OnUserUpdated="OnUserUpdated"
                          OnVettingStatusChanged="OnVettingStatusChanged" />
        </div>
    }
</div>

<!-- Edit Modal -->
@if (showEditModal && user != null)
{
    <UserEditModal User="@user"
                  IsVisible="@showEditModal"
                  OnSave="OnUserSaved"
                  OnCancel="OnEditCancelled" />
}

@code {
    [Parameter] public Guid UserId { get; set; }
    
    // State
    private EnhancedAdminUserDto? user;
    private bool isLoading = true;
    private bool hasError = false;
    private string errorMessage = string.Empty;
    private bool showEditModal = false;
    private UserRole currentUserRole;
    private bool isEventOrganizer;
    
    protected override async Task OnParametersSetAsync()
    {
        if (UserId != Guid.Empty)
        {
            await LoadUser();
        }
    }
    
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        currentUserRole = GetUserRole(authState.User);
        isEventOrganizer = authState.User.IsInRole("EventOrganizer");
    }
    
    private async Task LoadUser()
    {
        isLoading = true;
        hasError = false;
        
        try
        {
            var result = await UserService.GetUserDetailAsync(UserId);
            if (result.IsSuccess)
            {
                user = result.Value;
            }
            else
            {
                hasError = true;
                errorMessage = result.Error;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading user {UserId}", UserId);
            hasError = true;
            errorMessage = "Failed to load user details";
        }
        finally
        {
            isLoading = false;
        }
    }
    
    private bool CanEditUser()
    {
        return currentUserRole == UserRole.Administrator || 
               (isEventOrganizer && user?.EventPermissions?.Any() == true);
    }
    
    private string GetPageTitle()
    {
        return user?.SceneName ?? "User Details";
    }
}
```

## State Management Architecture

### 1. Component State Pattern
Each major component manages its own state with clear boundaries and communication patterns:

```csharp
// Centralized state for main page
public class UserManagementState
{
    // Data State
    public List<EnhancedAdminUserDto> Users { get; set; } = new();
    public UserManagementStatsDto Stats { get; set; } = new();
    public int TotalCount { get; set; }
    
    // UI State  
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    
    // Filter State
    public UserFilterState FilterState { get; set; } = new();
    
    // Polling State
    public bool IsPollingActive { get; set; } = true;
    public DateTime LastRefresh { get; set; }
    
    // Events for state changes
    public event Action? OnStateChanged;
    
    public void NotifyStateChanged() => OnStateChanged?.Invoke();
}

// Filter state model
public class UserFilterState
{
    public string SearchTerm { get; set; } = string.Empty;
    public VettingStatus? VettingStatusFilter { get; set; }
    public UserRole? RoleFilter { get; set; }
    public bool? IsActiveFilter { get; set; }
    public bool? IsVettedFilter { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    
    // Quick filters
    public bool ShowNewMembersOnly { get; set; }
    public bool ShowNeedsAttentionOnly { get; set; }
    public bool ShowSafetyNotesOnly { get; set; }
    
    public Dictionary<string, object> GetActiveFilters()
    {
        var filters = new Dictionary<string, object>();
        
        if (!string.IsNullOrWhiteSpace(SearchTerm))
            filters["search"] = SearchTerm;
            
        if (VettingStatusFilter.HasValue)
            filters["vettingStatus"] = VettingStatusFilter.Value;
            
        if (RoleFilter.HasValue)
            filters["role"] = RoleFilter.Value;
            
        if (IsActiveFilter.HasValue)
            filters["isActive"] = IsActiveFilter.Value;
            
        if (IsVettedFilter.HasValue)
            filters["isVetted"] = IsVettedFilter.Value;
            
        if (ShowNewMembersOnly)
            filters["newMembers"] = true;
            
        if (ShowNeedsAttentionOnly)
            filters["needsAttention"] = true;
            
        if (ShowSafetyNotesOnly)
            filters["safetyNotes"] = true;
        
        return filters;
    }
}
```

### 2. Polling Service Implementation
```csharp
public interface IPollingService
{
    Task StartPolling(Func<Task> refreshAction, TimeSpan interval);
    Task StopPolling();
    bool IsActive { get; }
}

public class PollingService : IPollingService, IDisposable
{
    private Timer? _timer;
    private Func<Task>? _refreshAction;
    private readonly ILogger<PollingService> _logger;
    
    public bool IsActive => _timer != null;
    
    public PollingService(ILogger<PollingService> logger)
    {
        _logger = logger;
    }
    
    public Task StartPolling(Func<Task> refreshAction, TimeSpan interval)
    {
        _refreshAction = refreshAction;
        
        _timer = new Timer(async _ =>
        {
            try
            {
                if (_refreshAction != null)
                {
                    await _refreshAction();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Polling refresh failed");
            }
        }, null, interval, interval);
        
        return Task.CompletedTask;
    }
    
    public Task StopPolling()
    {
        _timer?.Dispose();
        _timer = null;
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

## Data Flow Architecture

### 1. Service Layer Pattern
```csharp
public interface IUserManagementService
{
    // Read Operations
    Task<Result<PagedResult<EnhancedAdminUserDto>>> GetUsersAsync(UserSearchRequest request);
    Task<Result<EnhancedAdminUserDto>> GetUserDetailAsync(Guid userId);
    Task<Result<UserManagementStatsDto>> GetUserStatsAsync();
    Task<Result<UserEventsHistoryDto>> GetUserEventsHistoryAsync(Guid userId);
    Task<Result<List<UserNoteDto>>> GetUserNotesAsync(Guid userId, string? category = null);
    
    // Write Operations
    Task<Result<EnhancedAdminUserDto>> UpdateUserAsync(Guid userId, UserEditModel model);
    Task<Result<VettingStatusUpdateResult>> UpdateVettingStatusAsync(VettingStatusUpdateDto request);
    Task<Result<UserNoteDto>> AddUserNoteAsync(CreateUserNoteDto request);
    
    // Admin Operations
    Task<Result> DeactivateUserAsync(Guid userId, string reason);
    Task<Result> ReactivateUserAsync(Guid userId, string reason);
    Task<Result> ResetPasswordAsync(Guid userId);
}

public class UserManagementService : IUserManagementService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserManagementService> _logger;
    private readonly IMemoryCache _cache;
    
    public UserManagementService(
        HttpClient httpClient,
        ILogger<UserManagementService> logger,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
    }
    
    public async Task<Result<PagedResult<EnhancedAdminUserDto>>> GetUsersAsync(UserSearchRequest request)
    {
        try
        {
            var queryString = BuildQueryString(request);
            var response = await _httpClient.GetAsync($"/api/admin/users?{queryString}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PagedResult<EnhancedAdminUserDto>>(
                    content, JsonOptions);
                    
                return Result<PagedResult<EnhancedAdminUserDto>>.Success(result);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get users: {Error}", error);
                return Result<PagedResult<EnhancedAdminUserDto>>.Failure("Failed to load users");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return Result<PagedResult<EnhancedAdminUserDto>>.Failure("Network error loading users");
        }
    }
    
    // Caching for frequently accessed data
    public async Task<Result<UserManagementStatsDto>> GetUserStatsAsync()
    {
        const string cacheKey = "user_management_stats";
        
        if (_cache.TryGetValue(cacheKey, out UserManagementStatsDto cachedStats))
        {
            return Result<UserManagementStatsDto>.Success(cachedStats);
        }
        
        try
        {
            var response = await _httpClient.GetAsync("/api/admin/users/stats");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var stats = JsonSerializer.Deserialize<UserManagementStatsDto>(content, JsonOptions);
                
                // Cache for 2 minutes
                _cache.Set(cacheKey, stats, TimeSpan.FromMinutes(2));
                
                return Result<UserManagementStatsDto>.Success(stats);
            }
            else
            {
                return Result<UserManagementStatsDto>.Failure("Failed to load stats");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user stats");
            return Result<UserManagementStatsDto>.Failure("Network error loading stats");
        }
    }
}
```

### 2. Event Communication Pattern
```csharp
// Event-driven communication between components
public class UserManagementEventHub
{
    public event Action<EnhancedAdminUserDto>? UserUpdated;
    public event Action<VettingStatusUpdateResult>? VettingStatusChanged;
    public event Action<UserNoteDto>? NoteAdded;
    public event Action<Guid>? UserDeactivated;
    public event Action? DataRefreshRequested;
    
    public void NotifyUserUpdated(EnhancedAdminUserDto user)
    {
        UserUpdated?.Invoke(user);
    }
    
    public void NotifyVettingStatusChanged(VettingStatusUpdateResult result)
    {
        VettingStatusChanged?.Invoke(result);
    }
    
    public void NotifyNoteAdded(UserNoteDto note)
    {
        NoteAdded?.Invoke(note);
    }
    
    public void RequestDataRefresh()
    {
        DataRefreshRequested?.Invoke();
    }
}

// Component registration in Program.cs
builder.Services.AddScoped<UserManagementEventHub>();
```

## Performance Optimization Strategy

### 1. Virtual Scrolling Configuration
```razor
<!-- Grid with virtual scrolling for large datasets -->
<SfGrid DataSource="@Users" 
        AllowVirtualScrolling="true"
        EnableVirtualization="true"
        Height="600px"
        RowHeight="45">
    
    <!-- Column virtualization for wide grids -->
    <GridColumns>
        @foreach (var column in GetVisibleColumns())
        {
            <GridColumn Field="@column.Field" 
                       HeaderText="@column.HeaderText"
                       Width="@column.Width"
                       Visible="@column.Visible" />
        }
    </GridColumns>
</SfGrid>

@code {
    // Dynamic column visibility based on screen space
    private List<GridColumnDefinition> GetVisibleColumns()
    {
        var columns = new List<GridColumnDefinition>
        {
            new() { Field = "SceneName", HeaderText = "Scene Name", Width = "200px", Visible = true },
            new() { Field = "VettingStatus", HeaderText = "Status", Width = "120px", Visible = true },
            new() { Field = "Email", HeaderText = "Email", Width = "200px", Visible = !IsEventOrganizer },
            new() { Field = "Role", HeaderText = "Role", Width = "120px", Visible = true },
            new() { Field = "EventsAttended", HeaderText = "Events", Width = "100px", Visible = true },
            new() { Field = "LastActivityAt", HeaderText = "Last Active", Width = "140px", Visible = true }
        };
        
        return columns.Where(c => c.Visible).ToList();
    }
}
```

### 2. Lazy Loading Components
```csharp
// Lazy load heavy components
public partial class UserDetailTabs
{
    [Parameter] public bool IsActive { get; set; }
    
    // Only render tab content when active
    private RenderFragment RenderTabContent(string tabName) => builder =>
    {
        if (ActiveTab == tabName)
        {
            switch (tabName)
            {
                case "overview":
                    builder.OpenComponent<UserOverviewTab>(0);
                    builder.AddAttribute(1, "User", User);
                    builder.CloseComponent();
                    break;
                    
                case "events":
                    builder.OpenComponent<UserEventsTab>(0);
                    builder.AddAttribute(1, "UserId", User.Id);
                    builder.AddAttribute(2, "LazyLoad", true);
                    builder.CloseComponent();
                    break;
                    
                case "vetting":
                    if (CanAccessVetting())
                    {
                        builder.OpenComponent<UserVettingTab>(0);
                        builder.AddAttribute(1, "User", User);
                        builder.CloseComponent();
                    }
                    break;
            }
        }
    };
}
```

### 3. Debounced Search
```csharp
public partial class UserFilters
{
    private Timer? searchDebounceTimer;
    private readonly TimeSpan searchDebounceDelay = TimeSpan.FromMilliseconds(500);
    
    private void OnSearchTermChanged(string searchTerm)
    {
        FilterState.SearchTerm = searchTerm;
        
        // Debounce search to avoid excessive API calls
        searchDebounceTimer?.Dispose();
        searchDebounceTimer = new Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                await OnFiltersChanged.InvokeAsync(FilterState);
                StateHasChanged();
            });
        }, null, searchDebounceDelay, Timeout.InfiniteTimeSpan);
    }
    
    public void Dispose()
    {
        searchDebounceTimer?.Dispose();
    }
}
```

## Security & Authorization Patterns

### 1. Role-Based Component Rendering
```razor
<!-- Admin-only sections -->
<AuthorizeView Roles="Administrator">
    <Authorized>
        <div class="admin-only-actions">
            <SfButton OnClick="ShowVettingActions" CssClass="e-primary">
                Vetting Actions
            </SfButton>
            <SfButton OnClick="ShowUserEdit" CssClass="e-info">
                Edit Profile
            </SfButton>
        </div>
    </Authorized>
</AuthorizeView>

<!-- Event Organizer accessible sections -->
<AuthorizeView Roles="Administrator,EventOrganizer">
    <Authorized>
        <div class="organizer-actions">
            @if (context.User.IsInRole("EventOrganizer") && !context.User.IsInRole("Administrator"))
            {
                <!-- Limited event organizer view -->
                <UserEventsSummary UserId="@UserId" 
                                  ShowOnlyOrganizerEvents="true" />
            }
            else
            {
                <!-- Full admin view -->
                <UserEventsSummary UserId="@UserId" 
                                  ShowAllEvents="true" />
            }
        </div>
    </Authorized>
    <NotAuthorized>
        <div class="unauthorized-message">
            <p>You don't have permission to view this information.</p>
        </div>
    </NotAuthorized>
</AuthorizeView>
```

### 2. Data Sanitization Pattern
```csharp
public class UserInputSanitizer
{
    public static string SanitizeAdminNote(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
            
        // Remove potentially dangerous content but preserve basic formatting
        var sanitized = input
            .Replace("<script", "&lt;script", StringComparison.OrdinalIgnoreCase)
            .Replace("javascript:", "")
            .Replace("vbscript:", "")
            .Replace("onload=", "")
            .Replace("onerror=", "");
            
        // Limit length
        if (sanitized.Length > 5000)
        {
            sanitized = sanitized.Substring(0, 5000);
        }
        
        return sanitized.Trim();
    }
    
    public static UserSearchRequest SanitizeSearchRequest(UserSearchRequest request)
    {
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            request.SearchTerm = SanitizeSearchTerm(request.SearchTerm);
        }
        
        // Validate page parameters
        request.PageNumber = Math.Max(1, request.PageNumber);
        request.PageSize = Math.Min(200, Math.Max(10, request.PageSize));
        
        return request;
    }
}
```

## Error Handling & Loading States

### 1. Centralized Error Handling
```csharp
public class ErrorStateManager
{
    public bool HasError { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public Exception? LastException { get; private set; }
    public DateTime? ErrorTime { get; private set; }
    
    public void SetError(string message, Exception? ex = null)
    {
        HasError = true;
        ErrorMessage = message;
        LastException = ex;
        ErrorTime = DateTime.UtcNow;
        OnErrorChanged?.Invoke();
    }
    
    public void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
        LastException = null;
        ErrorTime = null;
        OnErrorChanged?.Invoke();
    }
    
    public event Action? OnErrorChanged;
}

// Usage in components
@if (errorManager.HasError)
{
    <div class="error-container">
        <div class="alert alert-danger">
            <div class="error-header">
                <span class="fas fa-exclamation-triangle"></span>
                <strong>Error</strong>
                @if (errorManager.ErrorTime.HasValue)
                {
                    <small class="text-muted">(@errorManager.ErrorTime.Value.ToString("HH:mm:ss"))</small>
                }
            </div>
            <p>@errorManager.ErrorMessage</p>
            <div class="error-actions">
                <SfButton OnClick="RetryOperation" CssClass="e-outline e-danger">
                    <span class="fas fa-redo"></span> Retry
                </SfButton>
                <SfButton OnClick="@errorManager.ClearError" CssClass="e-outline">
                    Dismiss
                </SfButton>
            </div>
        </div>
    </div>
}
```

### 2. Loading State Components
```razor
@* LoadingSpinner.razor *@
<div class="loading-container @CssClass">
    @if (ShowSpinner)
    {
        <SfSpinner @bind-Visible="ShowSpinner" 
                  Size="@SpinnerSize" 
                  Type="@SpinnerType" />
    }
    
    @if (!string.IsNullOrEmpty(Message))
    {
        <div class="loading-message">
            <p>@Message</p>
            @if (ShowProgress && TotalItems > 0)
            {
                <div class="loading-progress">
                    <SfProgressBar Value="@ProgressPercentage" 
                                  Height="4px" 
                                  ShowProgressValue="false" />
                    <small>@LoadedItems of @TotalItems items loaded</small>
                </div>
            }
        </div>
    }
    
    @if (ShowCancelButton)
    {
        <div class="loading-actions">
            <SfButton OnClick="OnCancel" 
                     CssClass="e-outline e-small">
                Cancel
            </SfButton>
        </div>
    }
</div>

@code {
    [Parameter] public bool ShowSpinner { get; set; } = true;
    [Parameter] public SpinnerSize SpinnerSize { get; set; } = SpinnerSize.Medium;
    [Parameter] public string Message { get; set; } = string.Empty;
    [Parameter] public string CssClass { get; set; } = string.Empty;
    [Parameter] public bool ShowProgress { get; set; }
    [Parameter] public int LoadedItems { get; set; }
    [Parameter] public int TotalItems { get; set; }
    [Parameter] public bool ShowCancelButton { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    
    private double ProgressPercentage => TotalItems > 0 ? (double)LoadedItems / TotalItems * 100 : 0;
}
```

## CSS Architecture & Styling

### 1. Component-Scoped Styling
```scss
// UserManagementIndex.razor.css
.user-management-container {
    padding: 1.5rem;
    min-height: calc(100vh - 200px);
    
    .stats-section {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
        gap: 1rem;
        margin-bottom: 2rem;
    }
    
    .main-content {
        background: white;
        border-radius: 8px;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        overflow: hidden;
        
        .content-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 1.5rem;
            border-bottom: 1px solid #e0e0e0;
            background: #f8f9fa;
            
            h2 {
                margin: 0;
                color: #2c3e50;
                font-weight: 600;
            }
            
            .header-actions {
                display: flex;
                gap: 0.5rem;
                align-items: center;
                
                .polling-indicator {
                    display: flex;
                    align-items: center;
                    gap: 0.25rem;
                    font-size: 0.875rem;
                    color: #6c757d;
                    
                    &.active {
                        color: #28a745;
                        
                        .fas.fa-circle {
                            animation: pulse 2s infinite;
                        }
                    }
                }
            }
        }
        
        .filters-section {
            padding: 1rem 1.5rem;
            border-bottom: 1px solid #e0e0e0;
            background: #fafbfc;
        }
        
        .grid-section {
            position: relative;
            min-height: 400px;
            
            .loading-overlay {
                position: absolute;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
                background: rgba(255, 255, 255, 0.9);
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                z-index: 10;
            }
        }
    }
}

// Animations
@keyframes pulse {
    0% { opacity: 1; }
    50% { opacity: 0.5; }
    100% { opacity: 1; }
}

// Status badges
.role-badge {
    padding: 0.25rem 0.5rem;
    border-radius: 12px;
    font-size: 0.75rem;
    font-weight: 500;
    text-transform: uppercase;
    
    &.role-administrator {
        background: #dc3545;
        color: white;
    }
    
    &.role-eventorganizer {
        background: #fd7e14;
        color: white;
    }
    
    &.role-member {
        background: #6f42c1;
        color: white;
    }
    
    &.role-guest {
        background: #6c757d;
        color: white;
    }
}

.badge-new {
    background: #17a2b8;
    color: white;
    font-size: 0.65rem;
    padding: 0.125rem 0.375rem;
    border-radius: 8px;
    margin-left: 0.5rem;
}

// Notes indicators
.notes-indicator {
    display: flex;
    gap: 0.25rem;
    align-items: center;
    
    .notes-icon {
        color: #6c757d;
    }
    
    .safety-icon {
        color: #dc3545;
    }
}
```

### 2. Responsive Design Considerations
```scss
// Mobile-first approach for future phases
@media (max-width: 1200px) {
    .user-management-container {
        .stats-section {
            grid-template-columns: repeat(2, 1fr);
        }
    }
}

@media (max-width: 768px) {
    // Mobile styles for future implementation
    .user-management-container {
        padding: 1rem;
        
        .stats-section {
            grid-template-columns: 1fr;
        }
        
        .content-header {
            flex-direction: column;
            gap: 1rem;
            
            .header-actions {
                width: 100%;
                justify-content: space-between;
            }
        }
    }
}
```

## Testing Architecture

### 1. Component Testing Structure
```csharp
// UserDataGridTests.cs
[TestFixture]
public class UserDataGridTests : TestContext
{
    private Mock<IUserManagementService> mockUserService;
    private List<EnhancedAdminUserDto> testUsers;
    
    [SetUp]
    public void Setup()
    {
        mockUserService = new Mock<IUserManagementService>();
        testUsers = CreateTestUsers();
        
        Services.AddScoped(_ => mockUserService.Object);
        Services.AddSyncfusionBlazor();
    }
    
    [Test]
    public void UserDataGrid_RendersCorrectly_WithUsers()
    {
        // Arrange
        var component = RenderComponent<UserDataGrid>(parameters => parameters
            .Add(p => p.Users, testUsers)
            .Add(p => p.UserRole, UserRole.Administrator)
            .Add(p => p.IsEventOrganizer, false));
        
        // Assert
        Assert.That(component.Find(".user-data-grid-container"), Is.Not.Null);
        Assert.That(component.FindAll(".e-row").Count, Is.EqualTo(testUsers.Count));
    }
    
    [Test]
    public async Task UserDataGrid_FiresOnUserSelected_WhenRowClicked()
    {
        // Arrange
        var selectedUserId = Guid.Empty;
        var component = RenderComponent<UserDataGrid>(parameters => parameters
            .Add(p => p.Users, testUsers)
            .Add(p => p.OnUserSelected, EventCallback.Factory.Create<Guid>(
                this, id => selectedUserId = id)));
        
        // Act
        var firstRow = component.Find(".e-row");
        await firstRow.ClickAsync();
        
        // Assert
        Assert.That(selectedUserId, Is.EqualTo(testUsers[0].Id));
    }
    
    private List<EnhancedAdminUserDto> CreateTestUsers()
    {
        return new List<EnhancedAdminUserDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                SceneName = "TestUser1",
                Email = "test1@example.com",
                VettingStatus = VettingStatus.Approved,
                Role = UserRole.Member,
                IsActive = true,
                EventsAttended = 5
            },
            new()
            {
                Id = Guid.NewGuid(),
                SceneName = "TestUser2",
                Email = "test2@example.com",
                VettingStatus = VettingStatus.Pending,
                Role = UserRole.Guest,
                IsActive = true,
                EventsAttended = 0
            }
        };
    }
}
```

### 2. Integration Testing Pattern
```csharp
// UserManagementIntegrationTests.cs
[TestFixture]
public class UserManagementIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task UserManagement_AdminCanAccessAllFeatures()
    {
        // Arrange
        await AuthenticateAsAdmin();
        
        // Act
        var response = await TestClient.GetAsync("/admin/users");
        
        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Contains.Substring("User Management"));
        Assert.That(content, Contains.Substring("Total Users"));
    }
    
    [Test]
    public async Task UserManagement_EventOrganizerHasLimitedAccess()
    {
        // Arrange
        await AuthenticateAsEventOrganizer();
        
        // Act & Assert
        var response = await TestClient.GetAsync("/admin/users");
        response.EnsureSuccessStatusCode();
        
        // Should not see admin-only features
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Not.Contain("Vetting Actions"));
        Assert.That(content, Does.Not.Contain("Delete User"));
    }
}
```

## Deployment Configuration

### 1. Service Registration
```csharp
// Program.cs additions
public static void ConfigureUserManagementServices(this IServiceCollection services)
{
    // Core services
    services.AddScoped<IUserManagementService, UserManagementService>();
    services.AddScoped<IPollingService, PollingService>();
    services.AddScoped<UserManagementCacheService>();
    services.AddScoped<UserManagementEventHub>();
    
    // Validation
    services.AddScoped<IValidator<UserEditModel>, UserEditModelValidator>();
    services.AddScoped<IValidator<VettingStatusUpdateDto>, VettingUpdateValidator>();
    services.AddScoped<IValidator<CreateUserNoteDto>, UserNoteValidator>();
    
    // Configuration
    services.Configure<UserManagementOptions>(
        configuration.GetSection("UserManagement"));
    services.Configure<VettingWorkflowOptions>(
        configuration.GetSection("VettingWorkflow"));
}
```

### 2. Configuration Options
```json
{
  "UserManagement": {
    "PollingIntervalSeconds": 30,
    "DetailPageRefreshSeconds": 60,
    "DefaultPageSize": 50,
    "MaxPageSize": 200,
    "EnableEventOrganizerAccess": true,
    "CacheExpiryMinutes": 5,
    "MaxSearchTermLength": 100
  },
  "VettingWorkflow": {
    "RequireAdminNoteForStatusChange": true,
    "SendEmailOnStatusChange": true,
    "MinAdminNoteLength": 10,
    "AllowedStatusTransitions": {
      "NotStarted": ["Submitted"],
      "Submitted": ["UnderReview", "MoreInfoRequested"],
      "UnderReview": ["Approved", "Rejected", "OnHold"],
      "MoreInfoRequested": ["Submitted", "Rejected"],
      "OnHold": ["UnderReview", "Approved", "Rejected"],
      "Approved": ["OnHold"],
      "Rejected": []
    }
  }
}
```

## Quality Assurance Checklist

- [x] **Component Architecture**: Clear hierarchy with single responsibility
- [x] **State Management**: Centralized state with event-driven updates  
- [x] **Performance**: Virtual scrolling, lazy loading, debounced search
- [x] **Security**: Role-based rendering, input sanitization, authorization
- [x] **Error Handling**: Centralized error management with user feedback
- [x] **Loading States**: Comprehensive loading indicators and progress
- [x] **Accessibility**: WCAG 2.1 AA compliance considerations
- [x] **Testing**: Unit and integration test patterns defined
- [x] **Polling**: Non-intrusive background updates every 30 seconds
- [x] **Desktop Focus**: Optimized for desktop admin workflows
- [x] **Syncfusion Integration**: Proper usage of grid, navigation, inputs
- [x] **Pure Blazor Server**: No Razor Pages, proper render modes
- [x] **Vertical Slice**: Features organized by domain boundaries
- [x] **API Integration**: Proper HTTP client usage with error handling
- [x] **Role-Based Access**: Admin vs Event Organizer differentiation

---

**Document Status**: ✅ COMPLETE  
**Implementation Ready**: ✅ YES  
**Estimated Complexity**: HIGH (comprehensive redesign)  
**Development Timeline**: 3-4 weeks (single developer)  
**Risk Level**: MEDIUM (new UI patterns, role-based access complexity)