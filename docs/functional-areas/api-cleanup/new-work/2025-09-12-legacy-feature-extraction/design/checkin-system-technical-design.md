# CheckIn System Technical Design - WitchCityRope
<!-- Last Updated: 2025-09-13 -->
<!-- Version: 1.0 -->
<!-- Owner: Backend Developer Agent -->
<!-- Status: Technical Design Complete -->

## Executive Summary

This technical design document specifies the implementation of the CheckIn System for WitchCityRope event attendee management. The system follows the established vertical slice architecture pattern and provides mobile-first, offline-capable check-in functionality with <1 second response times.

## Business Context & Requirements

### Problem Statement
Event organizers need efficient check-in management for community events but currently lack:
- Mobile-optimized interface for volunteer staff
- Offline capability for unreliable venue WiFi
- Integration with attendee registration and capacity management
- Real-time attendance tracking with safety information display

### Success Metrics
- **Performance**: Check-in processing time <1 second locally, <3 seconds with sync
- **Reliability**: 99%+ offline sync success rate
- **Usability**: <10 minutes volunteer training time
- **Capacity**: Support 500+ attendee events
- **Offline**: 4+ hours operation without connectivity

## Architecture Overview

### Vertical Slice Structure
Following the established pattern used in Safety and Authentication features:

```
/apps/api/Features/CheckIn/
├── Services/
│   ├── ICheckInService.cs           # Main check-in operations
│   ├── CheckInService.cs            # Business logic implementation
│   ├── ISyncService.cs              # Offline synchronization
│   ├── SyncService.cs               # Sync implementation
│   └── ICapacityService.cs          # Real-time capacity tracking
│   └── CapacityService.cs           # Capacity calculations
├── Endpoints/
│   ├── CheckInEndpoints.cs          # Minimal API endpoints
│   └── SyncEndpoints.cs             # Sync-specific endpoints
├── Models/
│   ├── CheckInRequest.cs            # Check-in operation request
│   ├── CheckInResponse.cs           # Check-in result
│   ├── AttendeeResponse.cs          # Attendee display data
│   ├── DashboardResponse.cs         # Event dashboard data
│   ├── SyncRequest.cs               # Offline sync request
│   ├── SyncResponse.cs              # Sync operation result
│   └── ManualEntryRequest.cs        # Walk-in registration
├── Validation/
│   ├── CheckInRequestValidator.cs   # Input validation
│   ├── ManualEntryValidator.cs      # Walk-in validation
│   └── SyncRequestValidator.cs      # Sync data validation
└── Extensions/
    └── CheckInServiceExtensions.cs  # DI configuration
```

### Technology Stack Alignment
- **Database**: PostgreSQL with Entity Framework Core 9
- **Authentication**: Cookie-based following existing patterns
- **Validation**: FluentValidation for all requests
- **Caching**: In-memory with Redis readiness
- **Real-time**: SignalR for capacity updates
- **Error Handling**: Result<T> pattern throughout

## API Specification

### Authentication & Authorization
All endpoints require authentication via cookie-based session management. Role-based access control:
- **CheckInStaff**: Basic check-in operations
- **EventOrganizer**: Capacity overrides and exports
- **Administrator**: All check-in functions

### Endpoint Definitions

#### 1. GET /api/checkin/events/{eventId}/attendees
**Purpose**: Retrieve attendee list for check-in interface

**Parameters**:
- `eventId` (required): Event GUID
- `search` (optional): Search term for filtering
- `status` (optional): Filter by registration status
- `page` (optional): Pagination page number (default: 1)
- `pageSize` (optional): Items per page (default: 50, max: 100)

**Response**:
```typescript
interface CheckInAttendeesResponse {
  eventId: string;
  eventTitle: string;
  eventDate: string;
  totalCapacity: number;
  checkedInCount: number;
  availableSpots: number;
  attendees: CheckInAttendee[];
  pagination: PaginationInfo;
}

interface CheckInAttendee {
  attendeeId: string;
  userId: string;
  sceneName: string;
  email: string;
  registrationStatus: 'confirmed' | 'waitlist' | 'checked-in' | 'no-show';
  ticketNumber?: string;
  checkInTime?: string;
  isFirstTime: boolean;
  dietaryRestrictions?: string;
  accessibilityNeeds?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  pronouns?: string;
  hasCompletedWaiver: boolean;
  waitlistPosition?: number;
}
```

#### 2. POST /api/checkin/events/{eventId}/checkin
**Purpose**: Process attendee check-in

**Request Body**:
```typescript
interface CheckInRequest {
  attendeeId: string;
  checkInTime: string; // ISO 8601 timestamp
  staffMemberId: string;
  notes?: string;
  overrideCapacity?: boolean;
  isManualEntry?: boolean;
  manualEntryData?: ManualEntryData;
}

interface ManualEntryData {
  name: string;
  email: string;
  phone: string;
  dietaryRestrictions?: string;
  accessibilityNeeds?: string;
  hasCompletedWaiver: boolean;
}
```

**Response**:
```typescript
interface CheckInResponse {
  success: boolean;
  attendeeId: string;
  checkInTime: string;
  message: string;
  currentCapacity: CapacityInfo;
  auditLogId?: string;
}

interface CapacityInfo {
  totalCapacity: number;
  checkedInCount: number;
  waitlistCount: number;
  availableSpots: number;
  isAtCapacity: boolean;
  canOverride: boolean;
}
```

#### 3. GET /api/checkin/events/{eventId}/dashboard
**Purpose**: Real-time event check-in statistics

**Response**:
```typescript
interface CheckInDashboard {
  eventId: string;
  eventTitle: string;
  eventDate: string;
  eventStatus: 'upcoming' | 'active' | 'ended';
  capacity: CapacityInfo;
  recentCheckIns: RecentCheckIn[];
  staffOnDuty: StaffMember[];
  syncStatus: SyncStatus;
}

interface RecentCheckIn {
  attendeeId: string;
  sceneName: string;
  checkInTime: string;
  staffMemberName: string;
  isManualEntry: boolean;
}

interface SyncStatus {
  pendingCount: number;
  lastSync: string;
  conflictCount: number;
}
```

#### 4. POST /api/checkin/events/{eventId}/sync
**Purpose**: Synchronize offline check-in data

**Request Body**:
```typescript
interface SyncRequest {
  deviceId: string;
  pendingCheckIns: PendingCheckIn[];
  lastSyncTimestamp: string;
}

interface PendingCheckIn {
  localId: string;
  attendeeId: string;
  checkInTime: string;
  staffMemberId: string;
  notes?: string;
  isManualEntry?: boolean;
  manualEntryData?: ManualEntryData;
}
```

**Response**:
```typescript
interface SyncResponse {
  success: boolean;
  processedCount: number;
  conflicts: SyncConflict[];
  updatedAttendees: CheckInAttendee[];
  newSyncTimestamp: string;
}

interface SyncConflict {
  localId: string;
  attendeeId: string;
  conflictType: 'duplicate_checkin' | 'capacity_exceeded' | 'attendee_not_found';
  serverData: any;
  localData: any;
  resolution: 'auto_resolved' | 'manual_required';
  message: string;
}
```

## Service Layer Implementation

### ICheckInService Interface
```csharp
/// <summary>
/// Primary service interface for check-in operations
/// Follows established patterns from SafetyService and AuthenticationService
/// </summary>
public interface ICheckInService
{
    /// <summary>
    /// Get attendees for event with optional filtering
    /// Optimized for mobile display and search
    /// </summary>
    Task<Result<CheckInAttendeesResponse>> GetEventAttendeesAsync(
        Guid eventId, 
        string? search = null, 
        string? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process check-in for attendee
    /// Handles capacity validation and audit logging
    /// </summary>
    Task<Result<CheckInResponse>> CheckInAttendeeAsync(
        CheckInRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time dashboard data for event
    /// Cached for performance with real-time updates
    /// </summary>
    Task<Result<CheckInDashboard>> GetEventDashboardAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create manual entry for walk-in attendee
    /// Includes temporary registration creation
    /// </summary>
    Task<Result<CheckInResponse>> CreateManualEntryAsync(
        Guid eventId,
        ManualEntryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export attendance data for organizers
    /// Role-based access control enforced
    /// </summary>
    Task<Result<AttendanceExport>> ExportAttendanceAsync(
        Guid eventId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);
}
```

### CheckInService Implementation
```csharp
/// <summary>
/// Main check-in service implementation
/// Direct Entity Framework usage following vertical slice pattern
/// </summary>
public class CheckInService : ICheckInService
{
    private readonly ApplicationDbContext _context;
    private readonly ICapacityService _capacityService;
    private readonly ILogger<CheckInService> _logger;
    private readonly IMemoryCache _cache;

    public CheckInService(
        ApplicationDbContext context,
        ICapacityService capacityService,
        ILogger<CheckInService> logger,
        IMemoryCache cache)
    {
        _context = context;
        _capacityService = capacityService;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Get attendees with optimized queries for mobile performance
    /// </summary>
    public async Task<Result<CheckInAttendeesResponse>> GetEventAttendeesAsync(
        Guid eventId, 
        string? search = null, 
        string? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Input validation
            if (pageSize > 100) pageSize = 100;
            if (page < 1) page = 1;

            // Build query with includes for performance
            var query = _context.EventAttendees
                .Include(ea => ea.User)
                .Include(ea => ea.CheckIns)
                .Where(ea => ea.EventId == eventId);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.ToLower();
                query = query.Where(ea => 
                    ea.User.SceneName.ToLower().Contains(searchTerm) ||
                    ea.User.Email.ToLower().Contains(searchTerm) ||
                    (ea.TicketNumber != null && ea.TicketNumber.ToLower().Contains(searchTerm)));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(ea => ea.RegistrationStatus == status);
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var attendees = await query
                .OrderBy(ea => ea.RegistrationStatus)
                .ThenBy(ea => ea.WaitlistPosition ?? 0)
                .ThenBy(ea => ea.User.SceneName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Get event info and capacity
            var eventInfo = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventInfo == null)
            {
                return Result<CheckInAttendeesResponse>.Failure("Event not found");
            }

            var capacity = await _capacityService.GetEventCapacityAsync(eventId, cancellationToken);

            // Map to response DTOs
            var attendeeResponses = attendees.Select(ea => new CheckInAttendee
            {
                AttendeeId = ea.Id.ToString(),
                UserId = ea.UserId.ToString(),
                SceneName = ea.User.SceneName,
                Email = ea.User.Email,
                RegistrationStatus = ea.RegistrationStatus,
                TicketNumber = ea.TicketNumber,
                CheckInTime = ea.CheckIns.FirstOrDefault()?.CheckInTime.ToString("O"),
                IsFirstTime = ea.IsFirstTime,
                DietaryRestrictions = ea.DietaryRestrictions,
                AccessibilityNeeds = ea.AccessibilityNeeds,
                EmergencyContactName = ea.EmergencyContactName,
                EmergencyContactPhone = ea.EmergencyContactPhone,
                Pronouns = ea.User.Pronouns,
                HasCompletedWaiver = ea.HasCompletedWaiver,
                WaitlistPosition = ea.WaitlistPosition
            }).ToList();

            var response = new CheckInAttendeesResponse
            {
                EventId = eventId.ToString(),
                EventTitle = eventInfo.Title,
                EventDate = eventInfo.StartDate.ToString("O"),
                TotalCapacity = capacity.TotalCapacity,
                CheckedInCount = capacity.CheckedInCount,
                AvailableSpots = capacity.AvailableSpots,
                Attendees = attendeeResponses,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            };

            return Result<CheckInAttendeesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendees for event {EventId}", eventId);
            return Result<CheckInAttendeesResponse>.Failure("Failed to retrieve attendees");
        }
    }

    /// <summary>
    /// Process attendee check-in with capacity validation and audit trail
    /// </summary>
    public async Task<Result<CheckInResponse>> CheckInAttendeeAsync(
        CheckInRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Get attendee with event info
            var attendee = await _context.EventAttendees
                .Include(ea => ea.Event)
                .Include(ea => ea.User)
                .Include(ea => ea.CheckIns)
                .FirstOrDefaultAsync(ea => ea.Id == Guid.Parse(request.AttendeeId), cancellationToken);

            if (attendee == null)
            {
                return Result<CheckInResponse>.Failure("Attendee not found");
            }

            // Check if already checked in
            if (attendee.CheckIns.Any())
            {
                return Result<CheckInResponse>.Failure("Attendee already checked in");
            }

            // Validate waiver completion
            if (!attendee.HasCompletedWaiver)
            {
                return Result<CheckInResponse>.Failure("Waiver must be completed before check-in");
            }

            // Check capacity unless override is specified
            var capacity = await _capacityService.GetEventCapacityAsync(attendee.EventId, cancellationToken);
            if (!request.OverrideCapacity && capacity.IsAtCapacity && attendee.RegistrationStatus == "waitlist")
            {
                return Result<CheckInResponse>.Failure("Event at capacity. Override required for waitlist check-in.");
            }

            // Create check-in record
            var checkIn = new CheckIn(
                attendee.Id,
                attendee.EventId,
                Guid.Parse(request.StaffMemberId))
            {
                CheckInTime = DateTime.Parse(request.CheckInTime).ToUniversalTime(),
                Notes = request.Notes,
                OverrideCapacity = request.OverrideCapacity,
                IsManualEntry = request.IsManualEntry,
                ManualEntryData = request.ManualEntryData != null ? 
                    JsonSerializer.Serialize(request.ManualEntryData) : null
            };

            _context.CheckIns.Add(checkIn);

            // Update attendee status
            attendee.RegistrationStatus = "checked-in";
            attendee.UpdatedAt = DateTime.UtcNow;
            attendee.UpdatedBy = Guid.Parse(request.StaffMemberId);

            // Create audit log
            var auditLog = new CheckInAuditLog(
                attendee.EventId,
                "check-in",
                $"Check-in completed for {attendee.User.SceneName}",
                Guid.Parse(request.StaffMemberId))
            {
                EventAttendeeId = attendee.Id,
                NewValues = JsonSerializer.Serialize(new { 
                    status = "checked-in", 
                    checkInTime = checkIn.CheckInTime,
                    staffMember = request.StaffMemberId,
                    overrideCapacity = request.OverrideCapacity
                })
            };

            _context.CheckInAuditLogs.Add(auditLog);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Update capacity cache
            _cache.Remove($"event_capacity_{attendee.EventId}");

            // Get updated capacity info
            var updatedCapacity = await _capacityService.GetEventCapacityAsync(attendee.EventId, cancellationToken);

            var response = new CheckInResponse
            {
                Success = true,
                AttendeeId = attendee.Id.ToString(),
                CheckInTime = checkIn.CheckInTime.ToString("O"),
                Message = "Check-in successful",
                CurrentCapacity = updatedCapacity,
                AuditLogId = auditLog.Id.ToString()
            };

            _logger.LogInformation("Successful check-in for attendee {AttendeeId} by staff {StaffId}", 
                attendee.Id, request.StaffMemberId);

            return Result<CheckInResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in for attendee {AttendeeId}", request.AttendeeId);
            return Result<CheckInResponse>.Failure("Check-in failed. Please try again.");
        }
    }

    // Additional methods for dashboard, manual entry, export...
}
```

### ISyncService Interface
```csharp
/// <summary>
/// Service for handling offline synchronization
/// Critical for mobile reliability at events
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Process pending offline check-ins
    /// Handles conflict resolution and data integrity
    /// </summary>
    Task<Result<SyncResponse>> ProcessOfflineSyncAsync(
        SyncRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue action for offline processing
    /// Stores actions when connectivity is lost
    /// </summary>
    Task<Result<string>> QueueOfflineActionAsync(
        Guid eventId,
        Guid userId,
        string actionType,
        object actionData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending sync count for user
    /// Used for UI indicators
    /// </summary>
    Task<Result<int>> GetPendingSyncCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve sync conflicts manually
    /// For conflicts requiring human decision
    /// </summary>
    Task<Result<bool>> ResolveSyncConflictAsync(
        Guid conflictId,
        string resolution,
        Guid resolvingUserId,
        CancellationToken cancellationToken = default);
}
```

## Performance Optimization Strategy

### Database Query Optimization
1. **Strategic Indexing**: 
   - EventAttendees: (EventId, RegistrationStatus)
   - CheckIns: (EventId, CheckInTime DESC)
   - EventAttendees: (UserId) for user lookups
   - EventAttendees: Partial indexes on FirstTime and Waiver status

2. **Query Patterns**:
   - Use AsNoTracking() for read-only operations
   - Include navigation properties strategically
   - Paginate large attendee lists
   - Project to DTOs to avoid over-fetching

3. **Capacity Calculations**:
   ```sql
   -- Optimized capacity query with single DB hit
   SELECT 
       e."Capacity" as "TotalCapacity",
       COUNT(ci."Id") as "CheckedInCount",
       (e."Capacity" - COUNT(ci."Id")) as "AvailableSpots",
       COUNT(ea."Id") FILTER (WHERE ea."RegistrationStatus" = 'waitlist') as "WaitlistCount"
   FROM "Events" e
   LEFT JOIN "EventAttendees" ea ON e."Id" = ea."EventId"
   LEFT JOIN "CheckIns" ci ON ea."Id" = ci."EventAttendeeId"
   WHERE e."Id" = @eventId
   GROUP BY e."Id", e."Capacity";
   ```

### Caching Strategy
```csharp
/// <summary>
/// Cache configuration for CheckIn system performance
/// </summary>
public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    // Cache keys
    private const string EVENT_CAPACITY_KEY = "event_capacity_{0}";
    private const string ATTENDEE_LIST_KEY = "attendee_list_{0}_{1}_{2}"; // eventId_page_search
    private const string EVENT_DASHBOARD_KEY = "event_dashboard_{0}";

    // Cache durations
    private static readonly TimeSpan CAPACITY_CACHE_DURATION = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan ATTENDEE_CACHE_DURATION = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DASHBOARD_CACHE_DURATION = TimeSpan.FromMinutes(1);

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan duration)
    {
        if (_cache.TryGetValue(key, out T cachedValue))
        {
            return cachedValue;
        }

        var item = await getItem();
        _cache.Set(key, item, duration);
        return item;
    }

    public void InvalidateEventCaches(Guid eventId)
    {
        _cache.Remove(string.Format(EVENT_CAPACITY_KEY, eventId));
        _cache.Remove(string.Format(EVENT_DASHBOARD_KEY, eventId));
        
        // Invalidate attendee list caches (all pages/searches)
        // Note: In production, consider using cache tags for bulk invalidation
    }
}
```

### Response Time Targets
- **Local Operations**: <500ms for attendee search
- **Check-in Processing**: <1 second local, <3 seconds with sync
- **Dashboard Load**: <2 seconds with real-time data
- **Offline Sync**: <5 seconds for 50 pending operations

## Offline Capability Implementation

### Sync Queue Management
```csharp
/// <summary>
/// Offline sync service implementation
/// Handles network interruptions and data conflicts
/// </summary>
public class SyncService : ISyncService
{
    public async Task<Result<SyncResponse>> ProcessOfflineSyncAsync(
        SyncRequest request,
        CancellationToken cancellationToken = default)
    {
        var conflicts = new List<SyncConflict>();
        var processedCount = 0;
        var updatedAttendees = new List<CheckInAttendee>();

        try
        {
            foreach (var pendingCheckIn in request.PendingCheckIns)
            {
                var result = await ProcessPendingCheckInAsync(pendingCheckIn, cancellationToken);
                
                if (result.IsSuccess)
                {
                    processedCount++;
                    // Add to updated attendees list
                }
                else
                {
                    // Check for conflicts
                    var conflict = await AnalyzeConflictAsync(pendingCheckIn, result.Error);
                    if (conflict != null)
                    {
                        conflicts.Add(conflict);
                    }
                }
            }

            return Result<SyncResponse>.Success(new SyncResponse
            {
                Success = true,
                ProcessedCount = processedCount,
                Conflicts = conflicts,
                UpdatedAttendees = updatedAttendees,
                NewSyncTimestamp = DateTime.UtcNow.ToString("O")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync processing");
            return Result<SyncResponse>.Failure("Sync processing failed");
        }
    }

    private async Task<SyncConflict?> AnalyzeConflictAsync(PendingCheckIn pending, string error)
    {
        // Check for duplicate check-in
        var existingCheckIn = await _context.CheckIns
            .Include(ci => ci.EventAttendee)
            .FirstOrDefaultAsync(ci => ci.EventAttendeeId == Guid.Parse(pending.AttendeeId));

        if (existingCheckIn != null)
        {
            return new SyncConflict
            {
                LocalId = pending.LocalId,
                AttendeeId = pending.AttendeeId,
                ConflictType = "duplicate_checkin",
                ServerData = new { 
                    checkInTime = existingCheckIn.CheckInTime,
                    staffMember = existingCheckIn.StaffMemberId
                },
                LocalData = new {
                    checkInTime = pending.CheckInTime,
                    staffMember = pending.StaffMemberId
                },
                Resolution = DateTime.Parse(pending.CheckInTime) < existingCheckIn.CheckInTime ? 
                    "auto_resolved" : "manual_required",
                Message = "Attendee was already checked in online"
            };
        }

        return null;
    }
}
```

### Conflict Resolution Strategy
1. **Duplicate Check-ins**: Preserve earliest timestamp, log both actions
2. **Capacity Conflicts**: Require manual organizer approval
3. **Attendee Not Found**: Flag for manual review and possible registration
4. **Data Inconsistency**: Quarantine conflicted records for resolution

## Security Implementation

### Authentication & Authorization
```csharp
/// <summary>
/// Authorization requirements for check-in endpoints
/// </summary>
public static class CheckInEndpoints
{
    public static void MapCheckInEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/checkin")
            .WithTags("CheckIn")
            .RequireAuthorization(); // All endpoints require authentication

        // Basic check-in operations - CheckInStaff role
        group.MapGet("/events/{eventId}/attendees", GetEventAttendees)
            .RequireAuthorization(policy => policy.RequireRole("CheckInStaff", "EventOrganizer", "Administrator"));

        group.MapPost("/events/{eventId}/checkin", ProcessCheckIn)
            .RequireAuthorization(policy => policy.RequireRole("CheckInStaff", "EventOrganizer", "Administrator"));

        // Capacity override - Organizer+ role
        group.MapPost("/events/{eventId}/checkin-override", ProcessCheckInOverride)
            .RequireAuthorization(policy => policy.RequireRole("EventOrganizer", "Administrator"));

        // Data export - Organizer+ role
        group.MapGet("/events/{eventId}/export", ExportAttendance)
            .RequireAuthorization(policy => policy.RequireRole("EventOrganizer", "Administrator"));

        // Dashboard access - All check-in roles
        group.MapGet("/events/{eventId}/dashboard", GetEventDashboard)
            .RequireAuthorization(policy => policy.RequireRole("CheckInStaff", "EventOrganizer", "Administrator"));
    }
}
```

### Data Privacy & Audit
1. **Personal Information Protection**: 
   - Limited display of sensitive data
   - Audit logging for all data access
   - Automatic data purge after events

2. **Audit Trail**: 
   - Complete action logging with user identification
   - IP address and user agent tracking
   - Immutable audit log design

3. **Session Security**: 
   - Session timeout after 2 hours
   - Automatic logout on inactivity
   - Secure cookie configuration

## Integration Points

### Events System Integration
- Event details and capacity information
- Attendee registration status validation
- Real-time capacity updates

### User Management Integration
- Role-based permission validation
- Staff member identification and authorization
- User profile information for check-in display

### Notification System Integration
- Email confirmations for check-ins
- Capacity alert notifications
- Staff activity notifications

### Real-Time Updates (SignalR)
```csharp
/// <summary>
/// Real-time updates for check-in interface
/// Broadcasts capacity and recent check-ins
/// </summary>
public class CheckInHub : Hub
{
    public async Task JoinEventGroup(string eventId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"event_{eventId}");
    }

    public async Task LeaveEventGroup(string eventId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event_{eventId}");
    }
}

// Usage in CheckInService
public async Task BroadcastCapacityUpdate(Guid eventId, CapacityInfo capacity)
{
    await _hubContext.Clients.Group($"event_{eventId}")
        .SendAsync("CapacityUpdated", capacity);
}

public async Task BroadcastRecentCheckIn(Guid eventId, RecentCheckIn checkIn)
{
    await _hubContext.Clients.Group($"event_{eventId}")
        .SendAsync("NewCheckIn", checkIn);
}
```

## Error Handling & Resilience

### Result Pattern Implementation
Following the established Result<T> pattern from Safety system:

```csharp
/// <summary>
/// Standardized error handling for check-in operations
/// </summary>
public static class CheckInErrors
{
    public static readonly string ATTENDEE_NOT_FOUND = "ATTENDEE_NOT_FOUND";
    public static readonly string ALREADY_CHECKED_IN = "ALREADY_CHECKED_IN";
    public static readonly string WAIVER_REQUIRED = "WAIVER_REQUIRED";
    public static readonly string CAPACITY_EXCEEDED = "CAPACITY_EXCEEDED";
    public static readonly string OVERRIDE_REQUIRED = "OVERRIDE_REQUIRED";
    public static readonly string SYNC_CONFLICT = "SYNC_CONFLICT";
    public static readonly string VALIDATION_FAILED = "VALIDATION_FAILED";
}

// Usage in service methods
if (!attendee.HasCompletedWaiver)
{
    return Result<CheckInResponse>.Failure(
        CheckInErrors.WAIVER_REQUIRED,
        "Waiver must be completed before check-in");
}
```

### Network Resilience
1. **Timeout Configuration**: 30-second timeouts for API calls
2. **Retry Policies**: Exponential backoff for failed sync operations
3. **Circuit Breaker**: Temporary failure handling for external services
4. **Graceful Degradation**: Core functionality continues during service outages

## Testing Strategy

### Unit Testing Requirements
```csharp
[TestFixture]
public class CheckInServiceTests
{
    [Test]
    public async Task CheckInAttendeeAsync_ValidRequest_ShouldSucceed()
    {
        // Arrange
        var service = CreateCheckInService();
        var request = CreateValidCheckInRequest();

        // Act
        var result = await service.CheckInAttendeeAsync(request);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Check-in successful", result.Value.Message);
    }

    [Test]
    public async Task CheckInAttendeeAsync_AlreadyCheckedIn_ShouldReturnError()
    {
        // Test duplicate check-in prevention
    }

    [Test]
    public async Task CheckInAttendeeAsync_WaiverNotCompleted_ShouldReturnError()
    {
        // Test waiver validation
    }

    [Test]
    public async Task CheckInAttendeeAsync_CapacityExceeded_ShouldReturnError()
    {
        // Test capacity enforcement
    }
}
```

### Integration Testing
1. **Database Integration**: Entity Framework operations with PostgreSQL
2. **API Integration**: Full endpoint testing with authentication
3. **Sync Testing**: Offline queue processing and conflict resolution
4. **Performance Testing**: Load testing with realistic data volumes

### Offline Testing Strategy
1. **Network Simulation**: Airplane mode during operations
2. **Sync Validation**: Data integrity after connectivity restoration
3. **Conflict Scenarios**: Duplicate operations and resolution testing
4. **Data Recovery**: Application restart with pending operations

## Deployment & Migration Strategy

### Database Migration
```csharp
/// <summary>
/// CheckIn system database migration
/// </summary>
public partial class AddCheckInSystem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create EventAttendees table
        migrationBuilder.CreateTable(
            name: "EventAttendees",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                RegistrationStatus = table.Column<string>(type: "text", nullable: false),
                // Additional columns...
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EventAttendees", x => x.Id);
                table.ForeignKey("FK_EventAttendees_Events", x => x.EventId, "Events", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_EventAttendees_Users", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            });

        // Create indexes for performance
        migrationBuilder.CreateIndex("IX_EventAttendees_EventId_Status", "EventAttendees", new[] { "EventId", "RegistrationStatus" });
        
        // Additional tables: CheckIns, CheckInAuditLog, OfflineSyncQueue
    }
}
```

### Phased Rollout
1. **Phase 1**: Database schema deployment
2. **Phase 2**: API endpoints with feature flags
3. **Phase 3**: Mobile interface deployment
4. **Phase 4**: Real-time features and analytics
5. **Phase 5**: Advanced features (QR codes, bulk operations)

### Feature Flags
```csharp
/// <summary>
/// Feature flag configuration for gradual rollout
/// </summary>
public static class CheckInFeatureFlags
{
    public const string BASIC_CHECKIN = "checkin.basic";
    public const string OFFLINE_SYNC = "checkin.offline_sync";
    public const string REALTIME_UPDATES = "checkin.realtime";
    public const string CAPACITY_OVERRIDE = "checkin.capacity_override";
    public const string MANUAL_ENTRY = "checkin.manual_entry";
    public const string DATA_EXPORT = "checkin.data_export";
}
```

## Monitoring & Health Checks

### Health Check Implementation
```csharp
/// <summary>
/// CheckIn system health monitoring
/// </summary>
public class CheckInHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test database connectivity
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Database connection failed");
            }

            // Check for failed sync operations
            var failedSyncCount = await _context.OfflineSyncQueues
                .CountAsync(q => q.SyncStatus == "failed" && q.RetryCount >= 5, cancellationToken);

            // Check cache availability
            var cacheWorking = _cache.Get("health_check_key") != null;

            var data = new Dictionary<string, object>
            {
                ["database_connected"] = canConnect,
                ["failed_sync_operations"] = failedSyncCount,
                ["cache_available"] = cacheWorking
            };

            if (failedSyncCount > 10)
            {
                return HealthCheckResult.Degraded("High number of failed sync operations", null, data);
            }

            return HealthCheckResult.Healthy("CheckIn system operational", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("CheckIn health check failed", ex);
        }
    }
}
```

### Performance Monitoring
1. **Metrics Collection**: Response times, throughput, error rates
2. **Alerting**: Performance degradation notifications
3. **Dashboard**: Real-time system status monitoring
4. **Logging**: Structured logging for troubleshooting

## Implementation Roadmap

### Sprint 1: Core Infrastructure (1 week)
- [ ] Database entities and migrations
- [ ] Basic service interfaces and DI setup
- [ ] Minimal API endpoint scaffolding
- [ ] Basic check-in operation (no offline)

### Sprint 2: Business Logic (1 week)
- [ ] Complete CheckInService implementation
- [ ] Capacity validation and calculations
- [ ] Audit logging system
- [ ] Input validation and error handling

### Sprint 3: Offline Capability (1 week)
- [ ] SyncService implementation
- [ ] Offline queue management
- [ ] Conflict detection and resolution
- [ ] Data synchronization endpoints

### Sprint 4: Performance & Real-time (1 week)
- [ ] Caching implementation
- [ ] SignalR real-time updates
- [ ] Query optimization
- [ ] Load testing and tuning

### Sprint 5: Advanced Features (1 week)
- [ ] Manual entry system
- [ ] Data export functionality
- [ ] Advanced dashboard features
- [ ] Security hardening

### Sprint 6: Integration & Polish (1 week)
- [ ] Frontend integration testing
- [ ] Mobile optimization validation
- [ ] Documentation completion
- [ ] Production deployment preparation

## Validation Checklist

### Technical Standards Compliance
- [ ] Follows vertical slice architecture pattern
- [ ] Uses Result<T> pattern for error handling
- [ ] Implements FluentValidation for all inputs
- [ ] Direct Entity Framework usage (no MediatR)
- [ ] Cookie-based authentication integration
- [ ] Async/await throughout with CancellationToken support
- [ ] Structured logging with correlation IDs
- [ ] UTC DateTime handling for PostgreSQL

### Performance Requirements
- [ ] <1 second check-in processing locally
- [ ] <3 seconds check-in with server sync
- [ ] <500ms attendee search response
- [ ] <2 seconds dashboard load time
- [ ] Supports 500+ attendee events
- [ ] 4+ hours offline operation capability

### Security Requirements
- [ ] Role-based access control enforced
- [ ] Complete audit trail implementation
- [ ] Personal data protection measures
- [ ] Session timeout and security
- [ ] Input validation and sanitization
- [ ] SQL injection prevention

### Integration Requirements
- [ ] Events system integration
- [ ] User management integration
- [ ] Real-time update broadcasting
- [ ] Notification system hooks
- [ ] Export functionality for organizers

This comprehensive technical design provides a complete implementation roadmap for the CheckIn System following established patterns and meeting all performance, security, and functionality requirements for mobile-first event management.