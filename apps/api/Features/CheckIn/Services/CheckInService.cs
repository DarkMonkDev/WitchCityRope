using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.CheckIn.Services;

/// <summary>
/// Main check-in service implementation
/// Direct Entity Framework usage following vertical slice pattern
/// </summary>
public class CheckInService : ICheckInService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CheckInService> _logger;
    private readonly IMemoryCache _cache;

    public CheckInService(
        ApplicationDbContext context,
        ILogger<CheckInService> logger,
        IMemoryCache cache)
    {
        _context = context;
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
                    (ea.User.SceneName != null && ea.User.SceneName.ToLower().Contains(searchTerm)) ||
                    (ea.User.Email != null && ea.User.Email.ToLower().Contains(searchTerm)) ||
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

            var capacity = await GetEventCapacityAsync(eventId, cancellationToken);

            // Map to response DTOs
            var attendeeResponses = attendees.Select(ea => new AttendeeResponse
            {
                AttendeeId = ea.Id.ToString(),
                UserId = ea.UserId.ToString(),
                SceneName = ea.User.SceneName ?? string.Empty,
                Email = ea.User.Email ?? string.Empty,
                RegistrationStatus = ea.RegistrationStatus,
                TicketNumber = ea.TicketNumber,
                CheckInTime = ea.CheckIns.FirstOrDefault()?.CheckInTime.ToString("O"),
                IsFirstTime = ea.IsFirstTime,
                DietaryRestrictions = ea.DietaryRestrictions,
                AccessibilityNeeds = ea.AccessibilityNeeds,
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
            var capacity = await GetEventCapacityAsync(attendee.EventId, cancellationToken);
            if (!request.OverrideCapacity && capacity.IsAtCapacity && attendee.RegistrationStatus == "waitlist")
            {
                return Result<CheckInResponse>.Failure("Event at capacity. Override required for waitlist check-in.");
            }

            // Create check-in record
            var checkIn = new Entities.CheckIn(
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
                NewValues = JsonSerializer.Serialize(new
                {
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
            var updatedCapacity = await GetEventCapacityAsync(attendee.EventId, cancellationToken);

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

    /// <summary>
    /// Get real-time dashboard data for event
    /// </summary>
    public async Task<Result<DashboardResponse>> GetEventDashboardAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get event info
            var eventInfo = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventInfo == null)
            {
                return Result<DashboardResponse>.Failure("Event not found");
            }

            // Get capacity info
            var capacity = await GetEventCapacityAsync(eventId, cancellationToken);

            // Get recent check-ins
            var recentCheckIns = await _context.CheckIns
                .Include(c => c.EventAttendee)
                .ThenInclude(ea => ea.User)
                .Include(c => c.StaffMember)
                .Where(c => c.EventId == eventId)
                .OrderByDescending(c => c.CheckInTime)
                .Take(5)
                .AsNoTracking()
                .Select(c => new RecentCheckIn
                {
                    AttendeeId = c.EventAttendeeId.ToString(),
                    SceneName = c.EventAttendee.User.SceneName,
                    CheckInTime = c.CheckInTime.ToString("O"),
                    StaffMemberName = c.StaffMember.SceneName,
                    IsManualEntry = c.IsManualEntry
                })
                .ToListAsync(cancellationToken);

            // Determine event status
            var now = DateTime.UtcNow;
            var eventStatus = now < eventInfo.StartDate ? "upcoming" :
                             now > eventInfo.EndDate ? "ended" : "active";

            var response = new DashboardResponse
            {
                EventId = eventId.ToString(),
                EventTitle = eventInfo.Title,
                EventDate = eventInfo.StartDate.ToString("O"),
                EventStatus = eventStatus,
                Capacity = capacity,
                RecentCheckIns = recentCheckIns,
                StaffOnDuty = new List<StaffMember>(), // TODO: Implement staff tracking
                SyncStatus = new SyncStatus
                {
                    PendingCount = 0, // TODO: Implement from sync service
                    LastSync = DateTime.UtcNow.ToString("O"),
                    ConflictCount = 0
                }
            };

            return Result<DashboardResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard for event {EventId}", eventId);
            return Result<DashboardResponse>.Failure("Failed to retrieve dashboard data");
        }
    }

    /// <summary>
    /// Create manual entry for walk-in attendee
    /// </summary>
    public async Task<Result<CheckInResponse>> CreateManualEntryAsync(
        Guid eventId,
        ManualEntryData request,
        Guid staffMemberId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Check if user already exists by email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (existingUser != null)
            {
                // Check if they're already registered for this event
                var existingAttendee = await _context.EventAttendees
                    .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == existingUser.Id, cancellationToken);

                if (existingAttendee != null)
                {
                    return Result<CheckInResponse>.Failure("User is already registered for this event");
                }
            }

            // For now, we'll create a manual entry record without creating a full user
            // In a full implementation, this would create a temporary user or work with the registration system

            // TODO: Implement proper manual entry user creation
            // This is a simplified implementation for the initial version

            return Result<CheckInResponse>.Failure("Manual entry not yet fully implemented");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating manual entry for event {EventId}", eventId);
            return Result<CheckInResponse>.Failure("Failed to create manual entry");
        }
    }

    /// <summary>
    /// Get event capacity information with caching
    /// </summary>
    private async Task<CapacityInfo> GetEventCapacityAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var cacheKey = $"event_capacity_{eventId}";

        if (_cache.TryGetValue(cacheKey, out CapacityInfo? cachedCapacity) && cachedCapacity != null)
        {
            return cachedCapacity;
        }

        var capacity = await _context.Events
            .Where(e => e.Id == eventId)
            .Select(e => new CapacityInfo
            {
                TotalCapacity = e.Capacity,
                CheckedInCount = e.Sessions.SelectMany(s => s.TicketTypes)
                    .SelectMany(tt => tt.Purchases)
                    .Count(p => _context.CheckIns.Any(c => c.EventId == eventId)),
                WaitlistCount = _context.EventAttendees
                    .Count(ea => ea.EventId == eventId && ea.RegistrationStatus == "waitlist"),
                AvailableSpots = e.Capacity - _context.CheckIns.Count(c => c.EventId == eventId),
                IsAtCapacity = _context.CheckIns.Count(c => c.EventId == eventId) >= e.Capacity,
                CanOverride = true // TODO: Implement based on user permissions
            })
            .FirstOrDefaultAsync(cancellationToken) ?? new CapacityInfo();

        // Cache for 2 minutes
        _cache.Set(cacheKey, capacity, TimeSpan.FromMinutes(2));

        return capacity;
    }
}