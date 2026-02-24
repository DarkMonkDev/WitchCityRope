using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Models;

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

    /// <summary>
    /// Global timezone for all events (Salem, MA).
    /// Used to determine "today" for queries in local time.
    /// See: /docs/guides-setup/datetime-handling-guide.md
    /// </summary>
    private static readonly TimeZoneInfo EasternTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

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
    /// Supports multi-session tokens by accepting list of session IDs
    /// Filters attendees to only show those eligible for the specified sessions
    /// </summary>
    public async Task<Result<CheckInAttendeesResponse>> GetEventAttendeesAsync(
        Guid eventId,
        List<Guid> sessionIds,
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

            // Get session names for display (multi-session support)
            var sessions = await _context.Sessions
                .AsNoTracking()
                .Where(s => sessionIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name })
                .ToListAsync(cancellationToken);

            var sessionNameMap = sessions.ToDictionary(s => s.Id, s => s.Name);

            // SERVER-SIDE PROJECTION: Build query without includes
            // CRITICAL: Filter to only show attendees eligible for THESE sessions
            // Eligibility rules:
            // - RSVP attendees (via EventAttendance) can check into ANY session (no ticket required)
            // - Ticket holders: ticket must include ANY OF THESE sessions (via Sessions collection)
            // Since EventAttendee doesn't have direct attendance info, we join with EventAttendance
            var eligibleUserIds = await _context.EventAttendances
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp != null ? tp.TicketType : null)
                        .ThenInclude(tt => tt != null ? tt.Sessions : null!)
                .Where(ea => ea.EventId == eventId &&
                            ea.Status == AttendanceStatus.Active &&
                            (ea.AttendanceType == AttendanceType.RSVP ||
                             ea.TicketPurchase == null ||
                             ea.TicketPurchase.TicketType!.Sessions.Any(s => sessionIds.Contains(s.Id))))
                .Select(ea => ea.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var query = _context.EventAttendees
                .AsNoTracking()
                .Where(ea => ea.EventId == eventId && eligibleUserIds.Contains(ea.UserId));

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.ToLower();
                query = query.Where(ea =>
                    (ea.User.SceneName != null && ea.User.SceneName.ToLower().Contains(searchTerm)) ||
                    (ea.User.Email != null && ea.User.Email.ToLower().Contains(searchTerm)) ||
                    (ea.TicketNumber != null && ea.TicketNumber.ToLower().Contains(searchTerm)));
            }

            // Apply status filter (case-insensitive: query parameter casing shouldn't affect results)
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusLower = status.ToLower();
                query = query.Where(ea => ea.RegistrationStatus.ToLower() == statusLower);
            }
            else
            {
                // By default, exclude cancelled attendees from check-in list
                // Only show confirmed and checked-in attendees
                query = query.Where(ea => ea.RegistrationStatus.ToLower() != "cancelled");
            }

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Get event info and capacity (use single query with projection)
            var eventInfo = await _context.Events
                .AsNoTracking()
                .Where(e => e.Id == eventId)
                .Select(e => new { e.Title, e.StartDate })
                .FirstOrDefaultAsync(cancellationToken);

            if (eventInfo == null)
            {
                return Result<CheckInAttendeesResponse>.Failure("Event not found");
            }

            var capacity = await GetEventCapacityAsync(eventId, cancellationToken);

            // SERVER-SIDE PROJECTION: Project to DTO at database level
            // Benefits: Only loads needed fields, calculates check-in time at database level
            // Fetch data with anonymous projection first (translatable to SQL)
            var rawAttendees = await query
                .OrderBy(ea => ea.RegistrationStatus)
                .ThenBy(ea => ea.WaitlistPosition ?? 0)
                .ThenBy(ea => ea.User.SceneName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ea => new
                {
                    ea.Id,
                    ea.UserId,
                    ea.EventId,
                    SceneName = ea.User.SceneName ?? string.Empty,
                    Email = ea.User.Email ?? string.Empty,
                    RegistrationStatus = ea.RegistrationStatus, // String from database
                    ea.TicketNumber,
                    CheckInTime = ea.CheckIns.OrderByDescending(c => c.CheckInTime)
                                             .Select(c => c.CheckInTime.ToString("O"))
                                             .FirstOrDefault(),
                    ea.IsFirstTime,
                    ea.DietaryRestrictions,
                    ea.AccessibilityNeeds,
                    Pronouns = ea.User.Pronouns,
                    ea.HasCompletedWaiver,
                    ea.WaitlistPosition,
                    // Check if user has a ticket via online purchase (EventAttendance) OR door payment (TicketPurchase)
                    HasTicket = _context.EventAttendances
                        .Any(ea2 => ea2.UserId == ea.UserId &&
                                   ea2.EventId == ea.EventId &&
                                   ea2.AttendanceType == AttendanceType.Ticket &&
                                   ea2.Status == AttendanceStatus.Active)
                        ||
                        _context.TicketPurchases
                        .Any(tp => tp.UserId == ea.UserId &&
                                   tp.TicketType!.EventId == ea.EventId &&
                                   tp.PaymentStatus.ToLower() == "completed")
                })
                .ToListAsync(cancellationToken);

            // Build user-to-sessions mapping for multi-session display
            // Group EventAttendance records by user and collect their session names
            var userSessionsMap = await _context.EventAttendances
                .Include(ea => ea.Session)
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp != null ? tp.TicketType : null)
                        .ThenInclude(tt => tt != null ? tt.Sessions : null!)
                .Where(ea => ea.EventId == eventId &&
                            ea.Status == AttendanceStatus.Active &&
                            eligibleUserIds.Contains(ea.UserId))
                .ToListAsync(cancellationToken);

            // Build dictionary of UserId -> List of session names
            var userSessionNames = new Dictionary<Guid, List<string>>();
            foreach (var attendance in userSessionsMap)
            {
                List<string> sessionsForUser;
                if (!userSessionNames.ContainsKey(attendance.UserId))
                {
                    userSessionNames[attendance.UserId] = new List<string>();
                }
                sessionsForUser = userSessionNames[attendance.UserId];

                // For RSVP attendees, use their direct SessionId
                if (attendance.AttendanceType == AttendanceType.RSVP && attendance.SessionId.HasValue)
                {
                    if (sessionNameMap.TryGetValue(attendance.SessionId.Value, out var sessionName))
                    {
                        if (!sessionsForUser.Contains(sessionName))
                        {
                            sessionsForUser.Add(sessionName);
                        }
                    }
                }
                // For ticket holders, use all sessions from their ticket type
                else if (attendance.AttendanceType == AttendanceType.Ticket &&
                        attendance.TicketPurchase?.TicketType?.Sessions != null)
                {
                    foreach (var session in attendance.TicketPurchase.TicketType.Sessions)
                    {
                        if (sessionIds.Contains(session.Id) && sessionNameMap.TryGetValue(session.Id, out var sessionName))
                        {
                            if (!sessionsForUser.Contains(sessionName))
                            {
                                sessionsForUser.Add(sessionName);
                            }
                        }
                    }
                }
            }

            // Convert to DTOs in memory with enum parsing and session names
            var attendeeResponses = rawAttendees.Select(ea => new AttendeeResponse
            {
                AttendeeId = ea.Id.ToString(),
                UserId = ea.UserId.ToString(),
                SceneName = ea.SceneName,
                Email = ea.Email,
                RegistrationStatus = ParseRegistrationStatus(ea.RegistrationStatus),
                TicketNumber = ea.TicketNumber,
                CheckInTime = ea.CheckInTime,
                IsFirstTime = ea.IsFirstTime,
                DietaryRestrictions = ea.DietaryRestrictions,
                AccessibilityNeeds = ea.AccessibilityNeeds,
                Pronouns = ea.Pronouns,
                HasCompletedWaiver = ea.HasCompletedWaiver,
                WaitlistPosition = ea.WaitlistPosition,
                // Set payment status based on ticket purchase
                // "rsvp" = No ticket, show "Paid at Door" button
                // "paid" = Has ticket, show "Covid Test Complete" button
                PaymentStatus = ea.HasTicket ? "paid" : "rsvp",
                // Add session names for multi-session display
                SessionNames = userSessionNames.TryGetValue(ea.UserId, out var sessions) ? sessions : null
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
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Get session token entity with related sessions (multi-session support)
            var sessionTokenEntity = await _context.CheckInSessionTokens
                .Include(t => t.TokenSessions)
                    .ThenInclude(ts => ts.Session)
                .FirstOrDefaultAsync(t => t.Token == sessionToken, cancellationToken);

            if (sessionTokenEntity == null)
            {
                return Result<CheckInResponse>.Failure("Invalid session token");
            }

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

            // Get session info for validation and response
            // Priority: 1) TokenSessions (multi-session), 2) SessionId (backwards compat)
            WitchCityRope.Api.Models.Session? session = null;
            Guid? resolvedSessionId = null;

            // Check multi-session token first
            if (sessionTokenEntity.TokenSessions?.Count > 0)
            {
                // For multi-session tokens, use the first available session
                // Future enhancement: could match based on attendee's ticket
                var firstTokenSession = sessionTokenEntity.TokenSessions.FirstOrDefault();
                if (firstTokenSession?.Session != null)
                {
                    session = firstTokenSession.Session;
                    resolvedSessionId = session.Id;
                }
            }

            // Fall back to legacy single-session field
            if (session == null && sessionTokenEntity.SessionId.HasValue)
            {
                session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.Id == sessionTokenEntity.SessionId.Value, cancellationToken);
                resolvedSessionId = sessionTokenEntity.SessionId;
            }

            if (session == null || !resolvedSessionId.HasValue)
            {
                _logger.LogWarning("Session not found for token. TokenId={TokenId}, SessionId={SessionId}, TokenSessionsCount={Count}",
                    sessionTokenEntity.Id, sessionTokenEntity.SessionId, sessionTokenEntity.TokenSessions?.Count ?? 0);
                return Result<CheckInResponse>.Failure("Session not found. Token may not be configured for any sessions.");
            }

            // CRITICAL: Validate that attendee's ticket is valid for THIS session
            // Business Rules:
            // - RSVP attendees (via EventAttendance) can check into ANY session (no ticket required)
            // - Ticket holders: ticket must be for THIS session OR a multi-session ticket
            // Check EventAttendance for this user to determine eligibility
            var eventAttendance = await _context.EventAttendances
                .Include(ea => ea.TicketPurchase)
                    .ThenInclude(tp => tp != null ? tp.TicketType : null)
                        .ThenInclude(tt => tt != null ? tt.Sessions : null)
                .FirstOrDefaultAsync(ea => ea.UserId == attendee.UserId &&
                                          ea.EventId == attendee.EventId &&
                                          ea.Status == AttendanceStatus.Active,
                                     cancellationToken);

            if (eventAttendance == null)
            {
                return Result<CheckInResponse>.Failure("No active event attendance found for this attendee");
            }

            var canCheckIntoSession = eventAttendance.AttendanceType == AttendanceType.RSVP ||
                                     eventAttendance.TicketPurchase?.TicketType?.Sessions.Any(s => s.Id == resolvedSessionId) == true;

            if (!canCheckIntoSession)
            {
                var ticketSessionInfo = eventAttendance.TicketPurchase?.TicketType?.Sessions.Any() == true
                    ? "a different session"
                    : "unknown session";
                return Result<CheckInResponse>.Failure(
                    $"Attendee's ticket is not valid for this session. Their ticket is for {ticketSessionInfo}.");
            }

            // Check if already checked in to THIS session (attendee can check into different sessions)
            var alreadyCheckedInToSession = attendee.CheckIns.Any(c => c.SessionId == resolvedSessionId);
            if (alreadyCheckedInToSession)
            {
                return Result<CheckInResponse>.Failure($"Attendee already checked in to {session.Name}");
            }

            // Validate waiver completion
            if (!attendee.HasCompletedWaiver)
            {
                return Result<CheckInResponse>.Failure("Waiver must be completed before check-in");
            }

            // Check capacity unless override is specified
            var capacity = await GetEventCapacityAsync(attendee.EventId, cancellationToken);
            if (!request.OverrideCapacity && capacity.IsAtCapacity && string.Equals(attendee.RegistrationStatus, "waitlist", StringComparison.OrdinalIgnoreCase))
            {
                return Result<CheckInResponse>.Failure("Event at capacity. Override required for waitlist check-in.");
            }

            // Create check-in record - use token creator's user ID for audit trail
            // Use session.Id (non-nullable) since we already validated the session exists
            var checkIn = new Entities.CheckIn(
                attendee.Id,
                attendee.EventId,
                session.Id,
                sessionTokenEntity.CreatedByUserId)
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
            attendee.UpdatedBy = sessionTokenEntity.CreatedByUserId;

            // Create audit log - log token prefix for security audit
            var tokenPrefix = sessionToken.Length > 8 ? sessionToken.Substring(0, 8) : sessionToken;
            var auditLog = new CheckInAuditLog(
                attendee.EventId,
                "check-in",
                $"Check-in completed for {attendee.User.SceneName}",
                sessionTokenEntity.CreatedByUserId)
            {
                EventAttendeeId = attendee.Id,
                NewValues = JsonSerializer.Serialize(new
                {
                    status = "checked-in",
                    checkInTime = checkIn.CheckInTime,
                    sessionToken = tokenPrefix + "...", // First 8 chars for audit
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
                AuditLogId = auditLog.Id.ToString(),
                SessionId = session.Id,
                SessionName = session.Name
            };

            _logger.LogInformation("Successful check-in for attendee {AttendeeId} using session token {TokenPrefix}",
                attendee.Id, tokenPrefix);

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

            // Get staff members who have checked people in today for this event
            // IMPORTANT: Use local "today", not UTC "today"
            // At 11 PM EST on Dec 4, UTC is 4 AM Dec 5 - using UTC.Date would miss today's check-ins
            // See: /docs/guides-setup/datetime-handling-guide.md
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EasternTimeZone);
            var todayLocalStart = localNow.Date;
            var todayUtcStart = TimeZoneInfo.ConvertTimeToUtc(todayLocalStart, EasternTimeZone);
            var staffOnDuty = await _context.CheckIns
                .Where(c => c.EventId == eventId && c.CheckInTime >= todayUtcStart)
                .GroupBy(c => new { c.StaffMemberId, c.StaffMember.SceneName, c.StaffMember.Role })
                .Select(g => new StaffMember
                {
                    UserId = g.Key.StaffMemberId.ToString(),
                    SceneName = g.Key.SceneName,
                    Role = g.Key.Role,
                    LastActivity = g.Max(c => c.CheckInTime).ToString("O")
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Get pending sync count
            var pendingCount = await _context.OfflineSyncQueues
                .Where(q => q.EventId == eventId && q.SyncStatus.ToLower() != "completed")
                .CountAsync(cancellationToken);

            var response = new DashboardResponse
            {
                EventId = eventId.ToString(),
                EventTitle = eventInfo.Title,
                EventDate = eventInfo.StartDate.ToString("O"),
                EventStatus = eventStatus,
                Capacity = capacity,
                RecentCheckIns = recentCheckIns,
                StaffOnDuty = staffOnDuty,
                SyncStatus = new SyncStatus
                {
                    PendingCount = pendingCount,
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
        string sessionToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Get session token entity to retrieve creator's user ID for audit logging
            var sessionTokenEntity = await _context.CheckInSessionTokens
                .Include(t => t.TokenSessions)
                .FirstOrDefaultAsync(t => t.Token == sessionToken, cancellationToken);

            if (sessionTokenEntity == null)
            {
                return Result<CheckInResponse>.Failure("Invalid session token");
            }

            // Get the session ID from the token (handle multi-session tokens)
            var sessionId = sessionTokenEntity.SessionId
                ?? sessionTokenEntity.TokenSessions?.FirstOrDefault()?.SessionId
                ?? Guid.Empty;

            if (sessionId == Guid.Empty)
            {
                return Result<CheckInResponse>.Failure("No valid session found for token");
            }

            // Verify event exists
            var eventInfo = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

            if (eventInfo == null)
            {
                return Result<CheckInResponse>.Failure("Event not found");
            }

            // Check capacity before allowing manual entry
            var capacity = await GetEventCapacityAsync(eventId, cancellationToken);
            if (capacity.IsAtCapacity)
            {
                return Result<CheckInResponse>.Failure("Event at capacity. Cannot add walk-in attendee.");
            }

            // Check if user already exists by email
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            ApplicationUser user;
            bool userCreated = false;

            if (existingUser != null)
            {
                // Check if they're already registered for this event
                var existingAttendee = await _context.EventAttendees
                    .Include(ea => ea.CheckIns)
                    .FirstOrDefaultAsync(ea => ea.EventId == eventId && ea.UserId == existingUser.Id, cancellationToken);

                if (existingAttendee != null)
                {
                    // If already registered but not checked in, allow check-in
                    if (existingAttendee.CheckIns.Any())
                    {
                        return Result<CheckInResponse>.Failure("User is already checked in to this event");
                    }
                    return Result<CheckInResponse>.Failure("User is already registered for this event");
                }

                user = existingUser;
            }
            else
            {
                // Create new user for walk-in
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    NormalizedEmail = request.Email.ToUpperInvariant(),
                    UserName = request.Email,
                    NormalizedUserName = request.Email.ToUpperInvariant(),
                    SceneName = request.Name,
                    PhoneNumber = request.Phone,
                    Role = "", // No special role for walk-in attendees
                    VettingStatus = 0, // Unvetted
                    IsActive = true,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // Required fields with defaults
                    EncryptedLegalName = string.Empty,
                    DateOfBirth = DateTime.UtcNow.AddYears(-18), // Default to minimum age
                    PronouncedName = string.Empty,
                    Pronouns = string.Empty
                };

                _context.Users.Add(user);
                userCreated = true;

                _logger.LogInformation("Created new user {UserId} for walk-in: {Email}", user.Id, user.Email);
            }

            // Generate unique ticket number
            var ticketNumber = $"WALKIN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            // Create EventAttendee record
            var attendee = new EventAttendee(eventId, user.Id, "confirmed")
            {
                TicketNumber = ticketNumber,
                DietaryRestrictions = request.DietaryRestrictions,
                AccessibilityNeeds = request.AccessibilityNeeds,
                HasCompletedWaiver = request.HasCompletedWaiver,
                IsFirstTime = true, // Assume walk-ins are first-timers unless specified otherwise
                CreatedBy = sessionTokenEntity.CreatedByUserId,
                UpdatedBy = sessionTokenEntity.CreatedByUserId
            };

            _context.EventAttendees.Add(attendee);

            // Create CheckIn record - use the sessionId we extracted earlier
            var checkIn = new Entities.CheckIn(
                attendee.Id,
                eventId,
                sessionId,
                sessionTokenEntity.CreatedByUserId)
            {
                CheckInTime = DateTime.UtcNow,
                IsManualEntry = true,
                Notes = $"Walk-in manual entry via kiosk. {(userCreated ? "New user created." : "Existing user.")}",
                ManualEntryData = JsonSerializer.Serialize(new
                {
                    name = request.Name,
                    email = request.Email,
                    phone = request.Phone,
                    dietaryRestrictions = request.DietaryRestrictions,
                    accessibilityNeeds = request.AccessibilityNeeds,
                    hasCompletedWaiver = request.HasCompletedWaiver,
                    userCreated = userCreated
                })
            };

            _context.CheckIns.Add(checkIn);

            // Update attendee status to checked-in
            attendee.RegistrationStatus = "checked-in";

            // Create audit log - log token prefix for security audit
            var tokenPrefix = sessionToken.Length > 8 ? sessionToken.Substring(0, 8) : sessionToken;
            var auditLog = new CheckInAuditLog(
                eventId,
                "manual-entry",
                $"Walk-in manual entry for {request.Name} ({request.Email})",
                sessionTokenEntity.CreatedByUserId)
            {
                EventAttendeeId = attendee.Id,
                NewValues = JsonSerializer.Serialize(new
                {
                    userId = user.Id,
                    attendeeId = attendee.Id,
                    ticketNumber = ticketNumber,
                    status = "checked-in",
                    checkInTime = checkIn.CheckInTime,
                    sessionToken = tokenPrefix + "...", // First 8 chars for audit
                    userCreated = userCreated,
                    name = request.Name,
                    email = request.Email
                })
            };

            _context.CheckInAuditLogs.Add(auditLog);

            // Save all changes
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Update capacity cache
            _cache.Remove($"event_capacity_{eventId}");

            // Get updated capacity info
            var updatedCapacity = await GetEventCapacityAsync(eventId, cancellationToken);

            var response = new CheckInResponse
            {
                Success = true,
                AttendeeId = attendee.Id.ToString(),
                CheckInTime = checkIn.CheckInTime.ToString("O"),
                Message = userCreated
                    ? $"Walk-in successful. New user created and checked in."
                    : $"Walk-in successful. Existing user checked in.",
                CurrentCapacity = updatedCapacity,
                AuditLogId = auditLog.Id.ToString()
            };

            _logger.LogInformation(
                "Manual entry completed for event {EventId}. User: {UserId}, Attendee: {AttendeeId}, Token: {TokenPrefix}, UserCreated: {UserCreated}",
                eventId, user.Id, attendee.Id, tokenPrefix, userCreated);

            return Result<CheckInResponse>.Success(response);
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
                CheckedInCount = _context.CheckIns.Count(c => c.EventId == eventId),
                WaitlistCount = _context.EventAttendees
                    .Count(ea => ea.EventId == eventId && ea.RegistrationStatus.ToLower() == "waitlist"),
                AvailableSpots = e.Capacity - _context.CheckIns.Count(c => c.EventId == eventId),
                IsAtCapacity = _context.CheckIns.Count(c => c.EventId == eventId) >= e.Capacity,
                // CanOverride indicates if system supports capacity override
                // User-specific permission checks should be done in CheckInAttendeeAsync
                // EventOrganizer and Administrator roles can override
                CanOverride = true
            })
            .FirstOrDefaultAsync(cancellationToken) ?? new CapacityInfo();

        // Cache for 2 minutes
        _cache.Set(cacheKey, capacity, TimeSpan.FromMinutes(2));

        return capacity;
    }

    /// <summary>
    /// Record a door cash payment for an attendee
    /// Creates a TicketPurchase record with staff attribution
    /// </summary>
    public async Task<Result<CashPaymentResponse>> RecordCashPaymentAsync(
        Guid eventId,
        CashPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Recording cash payment for event {EventId}, attendee {AttendeeId}, amount {Amount}",
                eventId, request.AttendeeId, request.Amount);

            // Validate event exists
            var eventExists = await _context.Events
                .AsNoTracking()
                .AnyAsync(e => e.Id == eventId, cancellationToken);

            if (!eventExists)
            {
                return Result<CashPaymentResponse>.Failure("Event not found");
            }

            // Validate attendee exists and is registered for this event
            var attendee = await _context.EventAttendees
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    ea => ea.EventId == eventId && ea.UserId == request.AttendeeId,
                    cancellationToken);

            if (attendee == null)
            {
                return Result<CashPaymentResponse>.Failure("Attendee is not registered for this event");
            }

            // Validate attendee doesn't already have a ticket for this event
            var existingTicket = await _context.TicketPurchases
                .AsNoTracking()
                .AnyAsync(
                    tp => tp.UserId == request.AttendeeId &&
                          tp.TicketType!.EventId == eventId,
                    cancellationToken);

            if (existingTicket)
            {
                return Result<CashPaymentResponse>.Failure("Attendee already has a ticket for this event");
            }

            // Validate ticket type exists for this event
            var ticketType = await _context.TicketTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    tt => tt.Id == request.TicketTypeId && tt.EventId == eventId,
                    cancellationToken);

            if (ticketType == null)
            {
                return Result<CashPaymentResponse>.Failure("Ticket type not found for this event");
            }

            // Validate staff member exists
            var staffExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == request.RecordedByStaffId, cancellationToken);

            if (!staffExists)
            {
                return Result<CashPaymentResponse>.Failure("Staff member not found");
            }

            // Create ticket purchase record
            var ticketPurchase = new TicketPurchase
            {
                Id = Guid.NewGuid(),
                TicketTypeId = request.TicketTypeId,
                UserId = request.AttendeeId,
                PurchaseDate = DateTime.UtcNow,
                Quantity = 1,
                TotalPrice = request.Amount,
                PaymentStatus = "Completed",
                PaymentMethod = "Cash",
                PaymentReference = $"DOOR-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Notes = request.Notes ?? string.Empty,
                RecordedByStaffId = request.RecordedByStaffId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TicketPurchases.Add(ticketPurchase);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cash payment recorded successfully: TicketPurchase {TicketPurchaseId}, Amount {Amount}",
                ticketPurchase.Id, request.Amount);

            return Result<CashPaymentResponse>.Success(new CashPaymentResponse
            {
                TicketPurchaseId = ticketPurchase.Id,
                Success = true,
                Message = "Cash payment recorded successfully",
                Amount = request.Amount,
                RecordedAt = ticketPurchase.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error recording cash payment for event {EventId}, attendee {AttendeeId}",
                eventId, request.AttendeeId);
            return Result<CashPaymentResponse>.Failure($"Failed to record cash payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to convert string registration status to enum
    /// Handles case-insensitive mapping of database string values to RegistrationStatus enum
    /// </summary>
    /// <param name="status">String status from database (confirmed, waitlist, checked-in, no-show)</param>
    /// <returns>Corresponding RegistrationStatus enum value, defaults to Confirmed if unknown</returns>
    private static RegistrationStatus ParseRegistrationStatus(string status)
    {
        return status?.ToLowerInvariant() switch
        {
            "confirmed" => RegistrationStatus.Confirmed,
            "waitlist" => RegistrationStatus.Waitlist,
            "checked-in" or "checkedin" => RegistrationStatus.CheckedIn,
            "no-show" or "noshow" => RegistrationStatus.NoShow,
            _ => RegistrationStatus.Confirmed // Default to Confirmed for unknown values
        };
    }
}