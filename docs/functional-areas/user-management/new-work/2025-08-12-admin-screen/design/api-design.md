# API Design: User Management Admin Screen Redesign
<!-- Last Updated: 2025-08-12 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer Agent -->
<!-- Status: Draft -->

## Executive Summary

This API design provides comprehensive backend support for the user management admin screen redesign, implementing a robust ASP.NET Core Minimal API architecture with consolidated user status management, admin notes, and role-based access control. The design prioritizes performance, security, and maintainability while supporting polling-based updates and simplified status workflows.

### Key Changes in This Update
- **Consolidated Status Field**: Single UserStatus enum replaces separate vetting/status properties
- **Simplified Endpoints**: Single status update endpoint instead of multiple vetting endpoints  
- **Streamlined DTOs**: Removed IsVetted property, using Status enum directly
- **Simplified Dashboard Stats**: Only essential metrics (TotalUsers, PendingVetting, OnHold)
- **Updated Service Layer**: Consolidated status management with simplified validation logic

### UserStatus Enum
```csharp
public enum UserStatus
{
    NoApplication = 0,     // User registered but no vetting application
    PendingReview = 1,     // Application submitted, awaiting review
    Vetted = 2,           // Approved and vetted member
    OnHold = 3,           // Temporarily paused (can be resumed)
    Banned = 4            // Permanently banned
}
```

## Architecture Overview

### Service Pattern
- **Web Service → API Service → Database** (no direct DB access from Web)
- **ASP.NET Core Minimal API** with grouped endpoints
- **Vertical Slice Architecture** per feature area
- **Result Pattern** for consistent error handling
- **JWT Authentication** from existing cookie system
- **Role-Based Authorization** (Administrator, Event Organizer)

### Core Principles
- **Single Responsibility**: Each service handles one domain area
- **Performance Optimized**: Efficient queries with proper caching
- **Security First**: Input validation, output sanitization, audit logging
- **Backwards Compatible**: No breaking changes to existing APIs
- **Testable**: Dependency injection with interface contracts

## API Endpoint Specifications

### Base Configuration
```csharp
// API Base URL: https://localhost:5653/api
// Authentication: JWT from cookies (existing system)
// Content-Type: application/json
// Rate Limiting: 100 requests/minute per user
```

## 1. User Management Endpoints

### 1.1 Enhanced User List
**Enhanced existing endpoint with advanced filtering and search**

```http
GET /api/admin/users
```

#### Request Parameters
```csharp
public class UserSearchRequest
{
    [DefaultValue(1)]
    public int Page { get; set; } = 1;
    
    [Range(10, 200)]
    public int PageSize { get; set; } = 50;
    
    [StringLength(100)]
    public string? SearchTerm { get; set; }
    
    public UserStatus[]? Status { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public bool? EmailConfirmed { get; set; }
    
    // Advanced filters
    public bool ShowNewMembersOnly { get; set; }
    public bool ShowNeedsAttentionOnly { get; set; }
    public bool ShowWithSafetyNotesOnly { get; set; }
    public bool ShowWithAdminNotesOnly { get; set; }
    
    // Sorting
    [DefaultValue("CreatedAt")]
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
    
    // Date ranges
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? LastActiveAfter { get; set; }
    public DateTime? LastActiveBefore { get; set; }
}
```

#### Response
```csharp
public class PagedUserResult
{
    public List<EnhancedAdminUserDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public UserSearchMetadata Metadata { get; set; } = new();
}

public class EnhancedAdminUserDto
{
    // Core Identity
    public Guid Id { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Extended Profile
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FetLifeUsername { get; set; }
    public string? Pronouns { get; set; }
    public string? PronouncedName { get; set; }
    
    // Status & Role
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    
    // Security
    public bool LockoutEnabled { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    
    // Activity Metrics
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int EventsAttended { get; set; }
    public DateTime? VettingDate { get; set; }
    
    // Admin Indicators
    public int AdminNotesCount { get; set; }
    public int SafetyNotesCount { get; set; }
    public bool HasConcerningPatterns { get; set; }
    
    // Computed Properties
    public bool IsNewMember => (DateTime.UtcNow - CreatedAt).Days <= 30;
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd > DateTime.UtcNow;
    public bool IsVetted => Status == UserStatus.Vetted;
    public string DisplayName => !string.IsNullOrEmpty(PronouncedName) ? $"{SceneName} ({PronouncedName})" : SceneName;
}

public class UserSearchMetadata
{
    public int NewMembersCount { get; set; }
    public int NeedsAttentionCount { get; set; }
    public int SafetyNotesCount { get; set; }
    public DateTime LastUpdated { get; set; }
}
```

#### Authorization
- **Administrator**: Full access to all users
- **Event Organizer**: Limited to users with RSVPs/attendance at their events

#### Implementation
```csharp
public class UserManagementEndpoints : IEndpointDefinition
{
    public void DefineEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/admin/users")
            .RequireAuthorization("AdminOrEventOrganizer")
            .WithTags("User Management");

        group.MapGet("/", GetUsersAsync)
            .WithSummary("Get paginated user list with advanced filtering")
            .WithDescription("Returns filtered and sorted list of users with role-based access control")
            .Produces<PagedUserResult>()
            .Produces<ProblemDetails>(400)
            .Produces(401)
            .Produces(403);
    }

    private static async Task<IResult> GetUsersAsync(
        [AsParameters] UserSearchRequest request,
        [FromServices] IUserManagementService userService,
        [FromServices] ICurrentUserService currentUser,
        [FromServices] ILogger<UserManagementEndpoints> logger,
        CancellationToken ct)
    {
        try
        {
            // Validate request
            var validator = new UserSearchRequestValidator();
            var validationResult = await validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            // Apply role-based filtering
            if (!currentUser.IsInRole("Administrator"))
            {
                request = await ApplyEventOrganizerFiltering(request, currentUser, ct);
            }

            // Execute search
            var result = await userService.SearchUsersAsync(request, ct);
            
            if (result.IsSuccess)
            {
                logger.LogInformation("User search completed: {Count} users returned", result.Value.Items.Count);
                return Results.Ok(result.Value);
            }
            else
            {
                logger.LogWarning("User search failed: {Error}", result.Error);
                return Results.Problem(result.Error, statusCode: 400);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing user search request");
            return Results.Problem("An error occurred processing your request", statusCode: 500);
        }
    }
}
```

### 1.2 User Statistics Dashboard
**New endpoint for dashboard metrics**

```http
GET /api/admin/users/stats
```

#### Response
```csharp
public class UserManagementStatsDto
{
    public int TotalUsers { get; set; }
    public int PendingVetting { get; set; }
    public int OnHold { get; set; }
    
    public DateTime LastCalculated { get; set; }
}

```

#### Caching Strategy
```csharp
[ResponseCache(Duration = 120)] // 2 minutes
public async Task<IResult> GetUserStatsAsync(
    [FromServices] IUserManagementService userService,
    [FromServices] IMemoryCache cache,
    CancellationToken ct)
{
    const string cacheKey = "user_management_stats";
    
    if (cache.TryGetValue(cacheKey, out UserManagementStatsDto? cachedStats))
    {
        return Results.Ok(cachedStats);
    }
    
    var result = await userService.CalculateUserStatsAsync(ct);
    if (result.IsSuccess)
    {
        cache.Set(cacheKey, result.Value, TimeSpan.FromMinutes(2));
        return Results.Ok(result.Value);
    }
    
    return Results.Problem(result.Error);
}
```

### 1.3 User Detail
**Enhanced existing endpoint with comprehensive user information**

```http
GET /api/admin/users/{id:guid}
```

#### Response
```csharp
public class UserDetailDto
{
    // All properties from EnhancedAdminUserDto plus:
    
    // Full Profile
    public string? Bio { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public EmergencyContactDto? EmergencyContact { get; set; }
    public string? VettingNotes { get; set; }
    
    // Activity Details
    public UserActivityMetricsDto? ActivityMetrics { get; set; }
    public List<UserNoteDto> RecentAdminNotes { get; set; } = new();
    public List<VettingStatusHistoryDto> VettingHistory { get; set; } = new();
    
    // Event Organizer Permissions (if applicable)
    public List<EventOrganizerPermissionDto> EventPermissions { get; set; } = new();
    
    // Security Details (Admin only)
    public List<LoginAttemptDto> RecentLoginAttempts { get; set; } = new();
    public string? LastKnownIpAddress { get; set; }
    public string? LastUserAgent { get; set; }
}

public class EmergencyContactDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
}

public class UserActivityMetricsDto
{
    public int TotalEventsAttended { get; set; }
    public int TotalRsvps { get; set; }
    public int TotalNoShows { get; set; }
    public int TotalLastMinuteCancellations { get; set; }
    
    public double AttendanceRate { get; set; }
    public double CancellationRate { get; set; }
    public double NoShowRate { get; set; }
    
    public DateTime? LastEventAttendedAt { get; set; }
    public DateTime? LastRsvpAt { get; set; }
    public string? MostFrequentEventType { get; set; }
    
    public List<string> ConcerningPatternFlags { get; set; } = new();
    public bool NeedsAdminAttention { get; set; }
}
```

#### Authorization Logic
```csharp
private static async Task<IResult> GetUserDetailAsync(
    Guid id,
    [FromServices] IUserManagementService userService,
    [FromServices] ICurrentUserService currentUser,
    CancellationToken ct)
{
    // Check if user has permission to view this specific user
    if (!currentUser.IsInRole("Administrator"))
    {
        var hasPermission = await userService.CanEventOrganizerViewUserAsync(
            currentUser.UserId, id, ct);
        if (!hasPermission)
        {
            return Results.Forbid();
        }
    }
    
    var result = await userService.GetUserDetailAsync(id, 
        includeSecurityDetails: currentUser.IsInRole("Administrator"), ct);
        
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error);
}
```

### 1.4 Update User
**Enhanced existing endpoint with comprehensive validation**

```http
PUT /api/admin/users/{id:guid}
```

#### Request Body
```csharp
public class UpdateUserRequest
{
    // Basic Profile
    [StringLength(100)]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    public string? LastName { get; set; }
    
    [StringLength(100)]
    public string? FetLifeUsername { get; set; }
    
    [StringLength(50)]
    public string? Pronouns { get; set; }
    
    [StringLength(100)]
    public string? PronouncedName { get; set; }
    
    [StringLength(2000)]
    public string? Bio { get; set; }
    
    // Status Updates (Admin only)
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    
    // Emergency Contact
    public EmergencyContactUpdateDto? EmergencyContact { get; set; }
    
    // Admin Note (required for significant changes)
    [StringLength(5000)]
    public string? AdminNote { get; set; }
}

public class EmergencyContactUpdateDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required, Phone, StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string Relationship { get; set; } = string.Empty;
}
```

#### Validation & Business Rules
```csharp
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        // FetLife username validation
        When(x => !string.IsNullOrEmpty(x.FetLifeUsername), () =>
        {
            RuleFor(x => x.FetLifeUsername)
                .Must(BeValidFetLifeUsername)
                .WithMessage("FetLife username must be 3-15 characters, alphanumeric and underscores only");
        });
        
        // Emergency contact validation
        When(x => x.EmergencyContact != null, () =>
        {
            RuleFor(x => x.EmergencyContact!.Phone)
                .Must(BeValidPhoneNumber)
                .WithMessage("Please provide a valid phone number");
        });
        
        // Admin note required for role changes
        When(x => x.Role.HasValue, () =>
        {
            RuleFor(x => x.AdminNote)
                .NotEmpty()
                .WithMessage("Admin note is required when changing user role");
        });
    }
}
```

## 2. Admin Notes Endpoints

### 2.1 Get User Notes
```http
GET /api/admin/users/{userId:guid}/notes
```

#### Query Parameters
```csharp
public class GetUserNotesRequest
{
    public string? Category { get; set; } // General, Vetting, Safety, etc.
    public bool SafetyNotesOnly { get; set; } // Event Organizer access
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool IncludeDeleted { get; set; } = false; // Admin only
}
```

#### Response
```csharp
public class PagedNotesResult
{
    public List<UserNoteDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class UserNoteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal"; // Low, Normal, High, Critical
    public bool IsSafetyRelated { get; set; }
    
    // Metadata
    public string CreatedByName { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    
    // Display helpers
    public string CategoryDisplay => GetCategoryDisplayName(Category);
    public string RelativeTime => GetRelativeTime(CreatedAt);
    public bool CanEdit { get; set; } // Based on current user permissions
    public bool CanDelete { get; set; }
}
```

### 2.2 Create Admin Note
```http
POST /api/admin/users/{userId:guid}/notes
```

#### Request Body
```csharp
public class CreateUserNoteRequest
{
    [Required, StringLength(5000, MinimumLength = 10)]
    public string NoteText { get; set; } = string.Empty;
    
    [Required]
    public string Category { get; set; } = "General";
    
    public string Priority { get; set; } = "Normal";
    public bool IsSafetyRelated { get; set; }
    
    // Optional context
    public string? EventContext { get; set; } // Associated event if relevant
    public string? ActionTaken { get; set; } // For incident reports
}
```

#### Validation
```csharp
public class CreateUserNoteRequestValidator : AbstractValidator<CreateUserNoteRequest>
{
    public CreateUserNoteRequestValidator()
    {
        RuleFor(x => x.NoteText)
            .NotEmpty()
            .Length(10, 5000)
            .Must(NotContainHtmlTags)
            .WithMessage("Note text must be plain text only");
            
        RuleFor(x => x.Category)
            .Must(BeValidCategory)
            .WithMessage("Invalid note category");
            
        RuleFor(x => x.Priority)
            .Must(BeValidPriority)
            .WithMessage("Priority must be Low, Normal, High, or Critical");
            
        // Safety notes require admin role
        When(x => x.IsSafetyRelated, () =>
        {
            RuleFor(x => x.Priority)
                .Must(p => p == "High" || p == "Critical")
                .WithMessage("Safety-related notes must be High or Critical priority");
        });
    }
}
```

#### Implementation
```csharp
private static async Task<IResult> CreateUserNoteAsync(
    Guid userId,
    CreateUserNoteRequest request,
    [FromServices] IUserManagementService userService,
    [FromServices] ICurrentUserService currentUser,
    [FromServices] IValidator<CreateUserNoteRequest> validator,
    CancellationToken ct)
{
    // Validate request
    var validationResult = await validator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        return Results.ValidationProblem(validationResult.ToDictionary());
    }
    
    // Check permissions
    if (!await userService.CanUserCreateNoteAsync(currentUser.UserId, userId, request.Category, ct))
    {
        return Results.Forbid();
    }
    
    // Create note
    var createDto = new CreateUserNoteDto
    {
        UserId = userId,
        NoteText = request.NoteText.Trim(),
        Category = request.Category,
        Priority = request.Priority,
        IsSafetyRelated = request.IsSafetyRelated,
        CreatedById = currentUser.UserId,
        EventContext = request.EventContext,
        ActionTaken = request.ActionTaken
    };
    
    var result = await userService.CreateUserNoteAsync(createDto, ct);
    
    if (result.IsSuccess)
    {
        // Log audit entry
        await userService.LogAuditEntryAsync(new UserManagementAuditEntry
        {
            TargetUserId = userId,
            ActionType = "NoteAdded",
            ActionCategory = "Notes",
            Details = new { Category = request.Category, Priority = request.Priority },
            PerformedById = currentUser.UserId
        }, ct);
        
        return Results.Created($"/api/admin/users/{userId}/notes/{result.Value.Id}", result.Value);
    }
    
    return Results.Problem(result.Error);
}
```

### 2.3 Search Notes Across All Users
```http
GET /api/admin/users/notes/search
```

#### Query Parameters
```csharp
public class SearchNotesRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string SearchTerm { get; set; } = string.Empty;
    
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public bool SafetyNotesOnly { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

#### Authorization
- **Administrator**: Can search all notes
- **Event Organizer**: Can search only safety-related notes for users attending their events

## 3. Vetting Management Endpoints

### 3.1 Update Vetting Status
```http
PUT /api/admin/users/{userId:guid}/vetting-status
```

#### Request Body
```csharp
public class UpdateVettingStatusRequest
{
    [Required]
    public VettingStatus NewStatus { get; set; }
    
    [Required, StringLength(5000, MinimumLength = 20)]
    public string AdminNote { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? ReasonCode { get; set; }
    
    public bool SendNotificationEmail { get; set; } = true;
    public string? CustomEmailMessage { get; set; }
    
    // For interview scheduling
    public DateTime? InterviewScheduledAt { get; set; }
    public string? InterviewNotes { get; set; }
}
```

#### Business Logic Validation
```csharp
public class UpdateVettingStatusRequestValidator : AbstractValidator<UpdateVettingStatusRequest>
{
    private readonly IVettingWorkflowService _workflowService;
    
    public UpdateVettingStatusRequestValidator(IVettingWorkflowService workflowService)
    {
        _workflowService = workflowService;
        
        RuleFor(x => x.AdminNote)
            .NotEmpty()
            .Length(20, 5000)
            .WithMessage("Detailed admin note is required for vetting status changes");
            
        RuleFor(x => x)
            .MustAsync(async (request, ct) =>
            {
                var currentStatus = await _workflowService.GetCurrentVettingStatusAsync(
                    request.UserId, ct);
                return _workflowService.IsValidStatusTransition(currentStatus, request.NewStatus);
            })
            .WithMessage("Invalid vetting status transition");
            
        // Interview scheduling validation
        When(x => x.NewStatus == VettingStatus.InterviewScheduled, () =>
        {
            RuleFor(x => x.InterviewScheduledAt)
                .NotNull()
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Interview must be scheduled for a future date");
        });
    }
}
```

#### Response
```csharp
public class VettingStatusUpdateResult
{
    public Guid UserId { get; set; }
    public VettingStatus PreviousStatus { get; set; }
    public VettingStatus NewStatus { get; set; }
    public string AdminNote { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedByName { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
    public string? EmailError { get; set; }
}
```

### 3.2 Get Vetting Status History
```http
GET /api/admin/users/{userId:guid}/vetting-history
```

#### Response
```csharp
public class VettingStatusHistoryDto
{
    public Guid Id { get; set; }
    public VettingStatus FromStatus { get; set; }
    public VettingStatus ToStatus { get; set; }
    public string AdminNotes { get; set; } = string.Empty;
    public string? ReasonCode { get; set; }
    public bool Automated { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public bool NotificationSent { get; set; }
    public DateTime? NotificationSentAt { get; set; }
}
```

## 4. User Events History Endpoints

### 4.1 Get User Events History
```http
GET /api/admin/users/{userId:guid}/events
```

#### Query Parameters
```csharp
public class UserEventsHistoryRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? EventType { get; set; } // Class, Social, Workshop
    public string? AttendanceStatus { get; set; } // Attended, NoShow, Cancelled
    public bool IncludeCancelled { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

#### Response
```csharp
public class UserEventsHistoryDto
{
    public List<UserEventAttendanceDto> Events { get; set; } = new();
    public UserAttendanceMetricsDto Metrics { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class UserEventAttendanceDto
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public DateTime EventEndDate { get; set; }
    public string Venue { get; set; } = string.Empty;
    
    // RSVP Details
    public string RsvpStatus { get; set; } = string.Empty; // Confirmed, Waitlisted, Cancelled
    public DateTime RsvpDate { get; set; }
    public DateTime? CancellationDate { get; set; }
    public string? CancellationReason { get; set; }
    
    // Attendance
    public bool Attended { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public bool NoShow { get; set; }
    public bool LastMinuteCancellation { get; set; }
    
    // Event Context
    public string? OrganizerName { get; set; }
    public decimal? TicketPrice { get; set; }
    public bool RequiresVetting { get; set; }
}

public class UserAttendanceMetricsDto
{
    // Overall Stats
    public int TotalEventsAttended { get; set; }
    public int TotalRsvps { get; set; }
    public int TotalNoShows { get; set; }
    public int TotalLastMinuteCancellations { get; set; }
    
    // Rates
    public double AttendanceRate { get; set; }
    public double CancellationRate { get; set; }
    public double NoShowRate { get; set; }
    public double ReliabilityScore { get; set; }
    
    // Patterns
    public string? MostFrequentEventType { get; set; }
    public string? PreferredTimeSlot { get; set; }
    public List<string> ConcerningPatterns { get; set; } = new();
    
    // Time-based metrics
    public int EventsLast30Days { get; set; }
    public int EventsLast90Days { get; set; }
    public int EventsLastYear { get; set; }
    
    // Engagement indicators
    public DateTime? FirstEventDate { get; set; }
    public DateTime? LastEventDate { get; set; }
    public int MonthsActive { get; set; }
    public bool IsActiveAttendee { get; set; }
}
```

## 5. Event Organizer Limited Access Endpoints

### 5.1 Get Users for Event
```http
GET /api/admin/users/for-event/{eventId:guid}
```

#### Authorization
- **Event Organizers**: Only for events they organize
- **Administrators**: All events

#### Response
```csharp
public class EventUserListDto
{
    public List<EventAttendeeDto> Attendees { get; set; } = new();
    public EventBasicInfoDto Event { get; set; } = new();
    public int TotalAttendees { get; set; }
    public int CheckedInCount { get; set; }
    public int NoShowCount { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class EventAttendeeDto
{
    public Guid UserId { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public VettingStatus VettingStatus { get; set; }
    public string RsvpStatus { get; set; } = string.Empty;
    public bool CheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    
    // Limited profile info
    public string? Pronouns { get; set; }
    public bool HasSafetyNotes { get; set; }
    public int SafetyNotesCount { get; set; }
    
    // Emergency contact (if event organizer has permission)
    public EmergencyContactDto? EmergencyContact { get; set; }
    
    // Attendance history summary
    public int PreviousEventsAttended { get; set; }
    public double AttendanceReliability { get; set; }
}
```

### 5.2 Get User Safety Notes (Event Organizer Access)
```http
GET /api/admin/users/{userId:guid}/safety-notes
```

#### Authorization Logic
```csharp
private static async Task<IResult> GetUserSafetyNotesAsync(
    Guid userId,
    [FromServices] IUserManagementService userService,
    [FromServices] ICurrentUserService currentUser,
    CancellationToken ct)
{
    // Verify event organizer has permission to view this user's safety notes
    if (!currentUser.IsInRole("Administrator"))
    {
        var hasPermission = await userService.CanEventOrganizerViewSafetyNotesAsync(
            currentUser.UserId, userId, ct);
        if (!hasPermission)
        {
            return Results.Forbid();
        }
    }
    
    var result = await userService.GetUserSafetyNotesAsync(userId, ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error);
}
```

## 6. Audit Trail Endpoints

### 6.1 Get User Audit History
```http
GET /api/admin/users/{userId:guid}/audit
```

#### Query Parameters
```csharp
public class UserAuditRequest
{
    public string? ActionCategory { get; set; } // Profile, Role, Status, Vetting, etc.
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
```

#### Response
```csharp
public class UserAuditHistoryDto
{
    public List<UserAuditEntryDto> Entries { get; set; } = new();
    public int TotalCount { get; set; }
    public DateTime OldestEntry { get; set; }
    public DateTime NewestEntry { get; set; }
}

public class UserAuditEntryDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string ActionCategory { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? AdminNote { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; }
    public string? IpAddress { get; set; }
    
    // Display helpers
    public string ActionDisplay => GetActionDisplayName(ActionType);
    public string ChangeDescription => GenerateChangeDescription();
    public string RelativeTime => GetRelativeTime(PerformedAt);
}
```

## Service Layer Implementation

### Core Service Interface
```csharp
public interface IUserManagementService
{
    // User Search & Retrieval
    Task<Result<PagedUserResult>> SearchUsersAsync(UserSearchRequest request, CancellationToken ct = default);
    Task<Result<UserDetailDto>> GetUserDetailAsync(Guid userId, bool includeSecurityDetails = false, CancellationToken ct = default);
    Task<Result<UserManagementStatsDto>> CalculateUserStatsAsync(CancellationToken ct = default);
    
    // User Management
    Task<Result<UserDetailDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default);
    Task<Result> DeactivateUserAsync(Guid userId, string reason, CancellationToken ct = default);
    Task<Result> ReactivateUserAsync(Guid userId, string reason, CancellationToken ct = default);
    
    // Admin Notes
    Task<Result<PagedNotesResult>> GetUserNotesAsync(Guid userId, GetUserNotesRequest request, CancellationToken ct = default);
    Task<Result<UserNoteDto>> CreateUserNoteAsync(CreateUserNoteDto request, CancellationToken ct = default);
    Task<Result<PagedNotesResult>> SearchNotesAsync(SearchNotesRequest request, CancellationToken ct = default);
    Task<Result<List<UserNoteDto>>> GetUserSafetyNotesAsync(Guid userId, CancellationToken ct = default);
    
    // Status Management
    Task<Result<UserStatusUpdateResult>> UpdateUserStatusAsync(Guid userId, UpdateUserStatusRequest request, CancellationToken ct = default);
    Task<Result<List<UserStatusHistoryDto>>> GetStatusHistoryAsync(Guid userId, CancellationToken ct = default);
    
    // Events & Activity
    Task<Result<UserEventsHistoryDto>> GetUserEventsHistoryAsync(Guid userId, UserEventsHistoryRequest request, CancellationToken ct = default);
    Task<Result<EventUserListDto>> GetEventAttendeesAsync(Guid eventId, CancellationToken ct = default);
    
    // Authorization Helpers
    Task<bool> CanEventOrganizerViewUserAsync(Guid organizerId, Guid userId, CancellationToken ct = default);
    Task<bool> CanEventOrganizerViewSafetyNotesAsync(Guid organizerId, Guid userId, CancellationToken ct = default);
    Task<bool> CanUserCreateNoteAsync(Guid adminId, Guid userId, string category, CancellationToken ct = default);
    
    // Audit
    Task LogAuditEntryAsync(UserManagementAuditEntry entry, CancellationToken ct = default);
    Task<Result<UserAuditHistoryDto>> GetUserAuditHistoryAsync(Guid userId, UserAuditRequest request, CancellationToken ct = default);
}
```

### Implementation Pattern
```csharp
public class UserManagementService : IUserManagementService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserManagementService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUser;

    public UserManagementService(
        HttpClient httpClient,
        ILogger<UserManagementService> logger,
        IMemoryCache cache,
        IEmailService emailService,
        ICurrentUserService currentUser)
    {
        _httpClient = httpClient;
        _logger = logger;
        _cache = cache;
        _emailService = emailService;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedUserResult>> SearchUsersAsync(
        UserSearchRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Searching users with filters: {@Filters}", request);

            var queryParameters = new Dictionary<string, object?>
            {
                ["page"] = request.Page,
                ["pageSize"] = request.PageSize,
                ["searchTerm"] = request.SearchTerm,
                ["status"] = request.Status != null ? string.Join(",", request.Status.Select(s => s.ToString())) : null,
                ["role"] = request.Role?.ToString(),
                ["isActive"] = request.IsActive,
                ["sortBy"] = request.SortBy,
                ["sortDescending"] = request.SortDescending
            };

            // Add advanced filters
            if (request.ShowNewMembersOnly)
                queryParameters["newMembersOnly"] = true;
                
            if (request.ShowNeedsAttentionOnly)
                queryParameters["needsAttention"] = true;
                
            if (request.ShowWithSafetyNotesOnly)
                queryParameters["hasSafetyNotes"] = true;

            var queryString = BuildQueryString(queryParameters);
            var response = await _httpClient.GetAsync($"admin/users?{queryString}", ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<PagedUserResult>(content, JsonOptions);
                
                _logger.LogInformation("User search completed: {Count} users returned", result?.Items.Count ?? 0);
                return Result<PagedUserResult>.Success(result!);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("User search failed: {StatusCode} - {Error}", response.StatusCode, error);
                return Result<PagedUserResult>.Failure($"Search failed: {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return Result<PagedUserResult>.Failure("An error occurred while searching users");
        }
    }

    public async Task<Result<UserStatusUpdateResult>> UpdateUserStatusAsync(
        Guid userId, 
        UpdateUserStatusRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Updating user status for user {UserId} to {Status}", 
                userId, request.NewStatus);

            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"admin/users/{userId}/status", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<UserStatusUpdateResult>(responseContent, JsonOptions);
                
                // Send notification email if requested
                if (request.SendNotificationEmail && result!.EmailSent)
                {
                    await SendUserStatusEmailAsync(userId, result, request.CustomEmailMessage, ct);
                }
                
                _logger.LogInformation("User status updated successfully for user {UserId}", userId);
                return Result<UserStatusUpdateResult>.Success(result!);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("User status update failed: {StatusCode} - {Error}", response.StatusCode, error);
                return Result<UserStatusUpdateResult>.Failure($"Update failed: {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status for user {UserId}", userId);
            return Result<UserStatusUpdateResult>.Failure("An error occurred while updating user status");
        }
    }

    private async Task SendUserStatusEmailAsync(
        Guid userId, 
        UserStatusUpdateResult result, 
        string? customMessage,
        CancellationToken ct)
    {
        try
        {
            var user = await GetUserBasicInfoAsync(userId, ct);
            if (user?.Email != null)
            {
                var templateId = GetEmailTemplateForStatus(result.NewStatus);
                var templateData = new
                {
                    scene_name = user.SceneName,
                    status = result.NewStatus.GetDisplayName(),
                    admin_note = result.AdminNote,
                    custom_message = customMessage,
                    updated_at = result.UpdatedAt.ToString("F"),
                    updated_by = result.UpdatedByName
                };

                await _emailService.SendTemplateEmailAsync(
                    user.Email,
                    templateId,
                    templateData,
                    ct);
                    
                _logger.LogInformation("User status notification email sent to {Email}", user.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send user status notification email for user {UserId}", userId);
        }
    }
}
```

## Email Integration (SendGrid)

### Email Templates
```csharp
public class UserStatusEmailTemplateService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<UserStatusEmailTemplateService> _logger;
    private readonly UserStatusEmailOptions _options;

    public async Task SendUserStatusUpdateAsync(
        string recipientEmail,
        string sceneName,
        UserStatus newStatus,
        string adminNote,
        string? customMessage = null,
        CancellationToken ct = default)
    {
        var templateId = GetTemplateIdForStatus(newStatus);
        var templateData = new UserStatusUpdateTemplateData
        {
            SceneName = sceneName,
            Status = newStatus.GetDisplayName(),
            StatusMessage = GetStatusMessage(newStatus),
            AdminNote = adminNote,
            CustomMessage = customMessage,
            ContactEmail = _options.ContactEmail,
            WebsiteUrl = _options.WebsiteUrl,
            UpdatedAt = DateTime.UtcNow.ToString("F")
        };

        var msg = MailHelper.CreateSingleTemplateEmail(
            new EmailAddress(_options.FromEmail, _options.FromName),
            new EmailAddress(recipientEmail, sceneName),
            templateId,
            templateData);

        var response = await _sendGridClient.SendEmailAsync(msg, ct);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Vetting status email sent successfully to {Email}", recipientEmail);
        }
        else
        {
            var errorBody = await response.Body.ReadAsStringAsync();
            _logger.LogError("Failed to send vetting status email: {StatusCode} - {Error}", 
                response.StatusCode, errorBody);
        }
    }

    private string GetTemplateIdForStatus(UserStatus status) => status switch
    {
        UserStatus.Vetted => _options.Templates.StatusApproved,
        UserStatus.Banned => _options.Templates.StatusRejected,
        UserStatus.OnHold => _options.Templates.StatusOnHold,
        UserStatus.PendingReview => _options.Templates.StatusPendingReview,
        _ => _options.Templates.GeneralUpdate
    };
}

public class UserStatusUpdateTemplateData
{
    [JsonPropertyName("scene_name")]
    public string SceneName { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("status_message")]
    public string StatusMessage { get; set; } = string.Empty;
    
    [JsonPropertyName("admin_note")]
    public string AdminNote { get; set; } = string.Empty;
    
    [JsonPropertyName("custom_message")]
    public string? CustomMessage { get; set; }
    
    [JsonPropertyName("contact_email")]
    public string ContactEmail { get; set; } = string.Empty;
    
    [JsonPropertyName("website_url")]
    public string WebsiteUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;
}
```

## Performance Optimization Strategies

### 1. Caching Implementation
```csharp
public class UserManagementCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserManagementCacheService> _logger;
    
    // Cache keys
    private const string USER_STATS_KEY = "user_management_stats";
    private const string USER_DETAIL_KEY = "user_detail_{0}";
    private const string USER_NOTES_KEY = "user_notes_{0}_{1}"; // userId, page
    private const string EVENT_ATTENDEES_KEY = "event_attendees_{0}";
    
    // Cache durations
    private static readonly TimeSpan StatsExpiry = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan UserDetailExpiry = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan UserNotesExpiry = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan EventAttendeesExpiry = TimeSpan.FromMinutes(1);

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiry,
        CancellationToken ct = default) where T : class
    {
        if (_cache.TryGetValue(key, out T? cached))
        {
            return cached;
        }

        var item = await factory();
        if (item != null)
        {
            _cache.Set(key, item, expiry);
        }

        return item;
    }

    public void InvalidateUserCache(Guid userId)
    {
        var patterns = new[]
        {
            string.Format(USER_DETAIL_KEY, userId),
            USER_STATS_KEY // Stats may change when user data changes
        };

        foreach (var pattern in patterns)
        {
            _cache.Remove(pattern);
        }
        
        _logger.LogDebug("Invalidated cache for user {UserId}", userId);
    }

    public void InvalidateNotesCache(Guid userId)
    {
        // Remove all cached note pages for this user
        for (int page = 1; page <= 10; page++) // Assume max 10 pages
        {
            var key = string.Format(USER_NOTES_KEY, userId, page);
            _cache.Remove(key);
        }
    }
}
```

### 2. Database Query Optimization
```sql
-- Optimized user search query with proper indexing
WITH filtered_users AS (
    SELECT DISTINCT u."Id"
    FROM "auth"."Users" u
    INNER JOIN "users_extended" ue ON u."Id" = ue.user_id
    LEFT JOIN "user_admin_notes" notes ON u."Id" = notes.user_id AND notes.deleted_at IS NULL
    WHERE 
        u."IsActive" = true
        AND ($1 IS NULL OR u."SceneName" ILIKE $1 OR u."Email" ILIKE $1 OR ue.first_name ILIKE $1 OR ue.last_name ILIKE $1)
        AND ($2 IS NULL OR ue.status = ANY($2))
        AND ($3 IS NULL OR u."Role" = $3)
        AND ($4 IS NULL OR u."EmailConfirmed" = $4)
        AND ($5 = false OR u."CreatedAt" >= CURRENT_DATE - INTERVAL '30 days')
        AND ($6 = false OR notes.is_safety_related = true)
    ORDER BY u."CreatedAt" DESC
    LIMIT $7 OFFSET $8
),
user_details AS (
    SELECT 
        u.*,
        ue.*,
        COUNT(notes.id) as admin_notes_count,
        COUNT(CASE WHEN notes.is_safety_related THEN 1 END) as safety_notes_count,
        am.attendance_rate,
        am.cancellation_rate,
        am.needs_admin_attention
    FROM filtered_users f
    INNER JOIN "auth"."Users" u ON f."Id" = u."Id"
    INNER JOIN "users_extended" ue ON u."Id" = ue.user_id
    LEFT JOIN "user_admin_notes" notes ON u."Id" = notes.user_id AND notes.deleted_at IS NULL
    LEFT JOIN "user_activity_metrics" am ON u."Id" = am.user_id AND am.calculation_date = CURRENT_DATE
    GROUP BY u."Id", ue.id, am.id
)
SELECT * FROM user_details;
```

### 3. Efficient Polling Support
```csharp
public class PollingOptimizedUserService
{
    public async Task<Result<PagedUserResult>> GetUsersWithChangeTrackingAsync(
        UserSearchRequest request,
        DateTime? lastPolledAt = null,
        CancellationToken ct = default)
    {
        try
        {
            var cacheKey = GenerateCacheKey(request);
            var cached = await _cache.GetAsync<CachedUserResult>(cacheKey, ct);
            
            // If we have cached data and it's recent enough for polling
            if (cached != null && lastPolledAt.HasValue)
            {
                // Check if any users have been updated since last poll
                var hasUpdates = await _dbContext.Users
                    .AnyAsync(u => u.UpdatedAt > lastPolledAt.Value || 
                                  u.UserExtended.UpdatedAt > lastPolledAt.Value, ct);
                
                if (!hasUpdates)
                {
                    // No changes, return cached data with updated timestamp
                    cached.Result.Metadata.LastUpdated = DateTime.UtcNow;
                    return Result<PagedUserResult>.Success(cached.Result);
                }
            }
            
            // Fetch fresh data
            var result = await ExecuteUserSearchQuery(request, ct);
            
            // Cache for future polls
            if (result.IsSuccess)
            {
                await _cache.SetAsync(cacheKey, new CachedUserResult 
                { 
                    Result = result.Value, 
                    CachedAt = DateTime.UtcNow 
                }, TimeSpan.FromSeconds(30), ct);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in polling-optimized user search");
            return Result<PagedUserResult>.Failure("Search failed");
        }
    }
}
```

## Error Handling Patterns

### Global Exception Handling
```csharp
public class UserManagementExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UserManagementExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationException(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleUnauthorizedAccess(context, ex);
        }
        catch (UserManagementException ex)
        {
            await HandleUserManagementException(context, ex);
        }
        catch (Exception ex)
        {
            await HandleGenericException(context, ex);
        }
    }

    private async Task HandleUserManagementException(HttpContext context, UserManagementException ex)
    {
        _logger.LogWarning(ex, "User management business logic error: {Message}", ex.Message);
        
        var problemDetails = new ProblemDetails
        {
            Status = 400,
            Title = "Business Logic Error",
            Detail = ex.Message,
            Type = "https://witchcityrope.com/errors/business-logic"
        };

        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}

public class UserManagementException : Exception
{
    public string ErrorCode { get; }
    
    public UserManagementException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public UserManagementException(string errorCode, string message, Exception innerException) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
```

### Result Pattern Implementation
```csharp
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string Error { get; private set; } = string.Empty;
    public string? ErrorCode { get; private set; }
    public Dictionary<string, string[]>? ValidationErrors { get; private set; }

    private Result(bool isSuccess, T? value, string error, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, string.Empty);
    }

    public static Result<T> Failure(string error, string? errorCode = null)
    {
        return new Result<T>(false, default, error, errorCode);
    }

    public static Result<T> ValidationFailure(Dictionary<string, string[]> validationErrors)
    {
        var result = new Result<T>(false, default, "Validation failed");
        result.ValidationErrors = validationErrors;
        return result;
    }
}

public static class Result
{
    public static Result Success()
    {
        return new Result(true, string.Empty);
    }

    public static Result Failure(string error, string? errorCode = null)
    {
        return new Result(false, error, errorCode);
    }
}
```

## Security Implementation

### Input Validation & Sanitization
```csharp
public class UserManagementValidator
{
    public static class Rules
    {
        public static IRuleBuilder<T, string> SafeText<T>(IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .Must(text => !ContainsDangerousContent(text))
                .WithMessage("Text contains potentially dangerous content")
                .Must(text => !ContainsExcessiveHtml(text))
                .WithMessage("Excessive HTML markup is not allowed");
        }

        public static IRuleBuilder<T, string> SceneName<T>(IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .NotEmpty()
                .Length(2, 50)
                .Matches(@"^[a-zA-Z0-9\s\-_'.]+$")
                .WithMessage("Scene name can only contain letters, numbers, spaces, hyphens, underscores, apostrophes, and periods");
        }

        public static IRuleBuilder<T, string> FetLifeUsername<T>(IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder
                .Length(3, 15)
                .Matches(@"^[a-zA-Z0-9_]+$")
                .WithMessage("FetLife username can only contain letters, numbers, and underscores");
        }
    }

    private static bool ContainsDangerousContent(string? text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        var dangerousPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"onload\s*=",
            @"onerror\s*=",
            @"onclick\s*=",
            @"<iframe[^>]*>",
            @"<object[^>]*>",
            @"<embed[^>]*>"
        };

        return dangerousPatterns.Any(pattern => 
            Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase));
    }
}
```

### Authorization Policies
```csharp
public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string AdminOrEventOrganizer = "AdminOrEventOrganizer";
    public const string CanAccessUserManagement = "CanAccessUserManagement";
    public const string CanModifyVettingStatus = "CanModifyVettingStatus";
    
    public static void AddUserManagementPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy =>
            policy.RequireRole("Administrator"));
            
        options.AddPolicy(AdminOrEventOrganizer, policy =>
            policy.RequireRole("Administrator", "EventOrganizer"));
            
        options.AddPolicy(CanAccessUserManagement, policy =>
            policy.RequireAssertion(context =>
            {
                var user = context.User;
                return user.IsInRole("Administrator") || 
                       (user.IsInRole("EventOrganizer") && 
                        user.HasClaim("Permission", "ViewUserManagement"));
            }));
            
        options.AddPolicy(CanModifyVettingStatus, policy =>
            policy.RequireRole("Administrator")
                  .RequireClaim("Permission", "ModifyVettingStatus"));
    }
}
```

## Configuration & Deployment

### Service Registration
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserManagementServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Core services
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<UserManagementCacheService>();
        
        // Validators
        services.AddTransient<IValidator<UserSearchRequest>, UserSearchRequestValidator>();
        services.AddTransient<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
        services.AddTransient<IValidator<CreateUserNoteRequest>, CreateUserNoteRequestValidator>();
        services.AddTransient<IValidator<UpdateUserStatusRequest>, UpdateUserStatusRequestValidator>();
        
        // Configuration
        services.Configure<UserManagementOptions>(
            configuration.GetSection("UserManagement"));
        services.Configure<UserStatusEmailOptions>(
            configuration.GetSection("UserStatusEmails"));
        
        // HTTP client for API calls
        services.AddHttpClient<IUserManagementService, UserManagementService>(client =>
        {
            client.BaseAddress = new Uri(configuration["ApiSettings:BaseUrl"]!);
            client.DefaultRequestHeaders.Add("User-Agent", "WitchCityRope-Web/1.0");
        });
        
        // Email services
        services.AddScoped<UserStatusEmailTemplateService>();
        
        return services;
    }
}
```

### Configuration Schema
```json
{
  "UserManagement": {
    "PollingIntervalSeconds": 30,
    "CacheExpiryMinutes": 5,
    "DefaultPageSize": 50,
    "MaxPageSize": 200,
    "MaxSearchTermLength": 100,
    "EnableAdvancedSearch": true,
    "LogAuditActions": true,
    "RequireAdminNoteForCriticalActions": true
  },
  "UserStatusEmails": {
    "FromEmail": "admin@witchcityrope.com",
    "FromName": "WitchCityRope Admin",
    "ContactEmail": "support@witchcityrope.com",
    "WebsiteUrl": "https://witchcityrope.com",
    "Templates": {
      "StatusApproved": "d-12345678901234567890123456789012",
      "StatusRejected": "d-09876543210987654321098765432109",
      "StatusOnHold": "d-11111111111111111111111111111111",
      "StatusPendingReview": "d-22222222222222222222222222222222",
      "GeneralUpdate": "d-44444444444444444444444444444444"
    }
  }
}
```

## Quality Assurance Checklist

- [x] **RESTful Design**: Proper HTTP methods and status codes
- [x] **Security**: Input validation, authorization, audit logging  
- [x] **Performance**: Caching, optimized queries, polling support
- [x] **Error Handling**: Result pattern, global exception handling
- [x] **Validation**: Comprehensive input validation with FluentValidation
- [x] **Documentation**: Complete endpoint specifications with examples
- [x] **Role-Based Access**: Admin vs Event Organizer differentiation
- [x] **Email Integration**: SendGrid templates for notifications
- [x] **Audit Trail**: Complete action logging with retention
- [x] **Backward Compatibility**: No breaking changes to existing APIs
- [x] **Testing**: Testable service interfaces with dependency injection
- [x] **Configuration**: Externalized configuration with validation
- [x] **Monitoring**: Logging and health checks integration
- [x] **Data Protection**: PII handling and sanitization
- [x] **Scalability**: Efficient database queries and caching strategy

---

**Document Status**: ✅ UPDATED FOR SIMPLIFIED STATUS  
**Implementation Ready**: ✅ YES  
**Estimated Effort**: 2-3 weeks (single developer)  
**Risk Level**: LOW-MEDIUM (simplified design, consolidated status)  
**Dependencies**: Database schema changes (UserStatus enum), SendGrid template updates