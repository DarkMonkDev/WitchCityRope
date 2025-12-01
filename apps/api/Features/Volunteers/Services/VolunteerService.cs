using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Volunteers.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Events;
using WitchCityRope.Api.Features.Events.Interfaces;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Volunteers.Services;

/// <summary>
/// Service for managing volunteer positions and signups
/// </summary>
public class VolunteerService : IVolunteerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VolunteerService> _logger;
    private readonly ITimeZoneService _timeZoneService;

    public VolunteerService(
        ApplicationDbContext context,
        ILogger<VolunteerService> logger,
        ITimeZoneService timeZoneService)
    {
        _context = context;
        _logger = logger;
        _timeZoneService = timeZoneService;
    }

    /// <summary>
    /// Get volunteer positions for an event
    /// </summary>
    public async Task<(bool success, List<VolunteerPositionDto>? positions, string? error)> GetEventVolunteerPositionsAsync(
        string eventId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(eventId, out var eventGuid))
            {
                return (false, null, "Invalid event ID format");
            }

            // Get the event for timing validation
            var eventEntity = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventGuid, cancellationToken);
            if (eventEntity == null)
            {
                return (false, null, "Event not found");
            }

            // OPTIMIZATION: Already optimized - uses Include for Session relationship
            // Batches queries appropriately (3 queries instead of 1+N+M)
            // Could be optimized further with single query if needed, but current approach is good

            // Get volunteer positions - only show public-facing positions on event page
            var positions = await _context.VolunteerPositions
                .AsNoTracking()
                .Include(vp => vp.Session)
                .Where(vp => vp.EventId == eventGuid && vp.IsPublicFacing)
                .ToListAsync(cancellationToken);

            // Get event sessions to handle event-wide positions
            var eventSessions = await _context.Sessions
                .AsNoTracking()
                .Where(s => s.EventId == eventGuid)
                .ToListAsync(cancellationToken);

            // Get user's existing signups if authenticated
            List<VolunteerSignup>? userSignups = null;
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
            {
                userSignups = await _context.VolunteerSignups
                    .AsNoTracking()
                    .Where(vs => vs.UserId == userGuid && vs.Status == VolunteerSignupStatus.Confirmed)
                    .ToListAsync(cancellationToken);
            }

            // Map to DTOs with async cancellation permission checks
            var positionDtos = new List<VolunteerPositionDto>();
            foreach (var vp in positions)
            {
                var userSignup = userSignups?.FirstOrDefault(us => us.VolunteerPositionId == vp.Id);

                // For event-wide positions (no SessionId), use the event's session if only one exists
                Session? sessionToUse = vp.Session;
                if (sessionToUse == null && eventSessions.Count == 1)
                {
                    sessionToUse = eventSessions[0];
                }

                // Calculate cancellation permission based on event timing rules
                var canCancel = false;
                if (userSignup != null)
                {
                    canCancel = await _timeZoneService.IsActionAllowedAsync(
                        eventEntity,
                        EventActionType.CancelVolunteer,
                        cancellationToken);
                }

                // Calculate signup permission based on event timing rules and position availability
                // Users cannot sign up if: timing window closed, position full, or already signed up
                var canSignUp = await _timeZoneService.IsActionAllowedAsync(
                    eventEntity,
                    EventActionType.GetVolunteer,
                    cancellationToken);

                // Additional constraints: position must have available slots and user not already signed up
                if (canSignUp)
                {
                    canSignUp = vp.SlotsRemaining > 0 && userSignup == null;
                }

                positionDtos.Add(new VolunteerPositionDto
                {
                    Id = vp.Id,
                    EventId = vp.EventId,
                    SessionId = vp.SessionId,
                    Title = vp.Title,
                    Description = vp.Description,
                    SlotsNeeded = vp.SlotsNeeded,
                    SlotsFilled = vp.SlotsFilled,
                    SlotsRemaining = vp.SlotsRemaining,
                    IsPublicFacing = vp.IsPublicFacing,
                    IsFullyStaffed = vp.IsFullyStaffed,
                    // Don't show session name if event has only one session (redundant)
                    SessionName = eventSessions.Count > 1 ? sessionToUse?.Name : null,
                    SessionStartTime = sessionToUse?.StartTime,
                    SessionEndTime = sessionToUse?.EndTime,
                    HasUserSignedUp = userSignup != null,
                    UserSignupId = userSignup?.Id,
                    CanCancel = canCancel,
                    CanSignUp = canSignUp
                });
            }

            return (true, positionDtos, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving volunteer positions for event {EventId}", eventId);
            return (false, null, "Failed to retrieve volunteer positions");
        }
    }

    /// <summary>
    /// Sign up a user for a volunteer position
    /// </summary>
    public async Task<(bool success, VolunteerSignupDto? signup, string? error)> SignupForPositionAsync(
        string positionId,
        string userId,
        VolunteerSignupRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(positionId, out var positionGuid))
            {
                return (false, null, "Invalid position ID format");
            }

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return (false, null, "Invalid user ID format");
            }

            // Get the volunteer position with event details FIRST (need event for timing check)
            var position = await _context.VolunteerPositions
                .Include(vp => vp.Event)
                .FirstOrDefaultAsync(vp => vp.Id == positionGuid, cancellationToken);

            if (position == null)
            {
                return (false, null, "Volunteer position not found");
            }

            // Check if position is public-facing
            if (!position.IsPublicFacing)
            {
                return (false, null, "This volunteer position is not open for public signups");
            }

            // TIMING VALIDATION FIRST - Check before all other business rules
            // This ensures users get timing errors BEFORE other validation errors
            if (position.Event != null)
            {
                var isAllowed = await _timeZoneService.IsActionAllowedAsync(
                    position.Event,
                    EventActionType.GetVolunteer,
                    cancellationToken);

                if (!isAllowed)
                {
                    _logger.LogWarning("Volunteer signup attempt for event {EventId} outside allowed timing window", position.EventId);
                    return (false, null, "Volunteer registration window has closed for this event");
                }
            }

            // Validate event waiver acceptance
            if (!request.EventWaiverAccepted)
            {
                return (false, null, "You must accept the Event Waiver to volunteer");
            }

            // Check if position is already full
            if (position.IsFullyStaffed)
            {
                return (false, null, "This volunteer position is already fully staffed");
            }

            // Check if user already signed up
            var existingSignup = await _context.VolunteerSignups
                .FirstOrDefaultAsync(vs => vs.VolunteerPositionId == positionGuid
                    && vs.UserId == userGuid
                    && vs.Status != VolunteerSignupStatus.Cancelled, cancellationToken);

            if (existingSignup != null)
            {
                return (false, null, "You have already signed up for this volunteer position");
            }

            // Create the signup
            var signup = new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = positionGuid,
                UserId = userGuid,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VolunteerSignups.Add(signup);

            // Update slots filled count
            position.SlotsFilled++;

            // Auto-RSVP user to the event if it's a social event and they're not already registered
            // Only social events use RSVPs - class/workshop events require ticket purchases
            var eventId = position.EventId;

            if (position.Event?.EventType == Enums.EventType.Social)
            {
                var existingAttendance = await _context.EventAttendances
                    .FirstOrDefaultAsync(ea => ea.EventId == eventId
                        && ea.UserId == userGuid
                        && ea.Status == AttendanceStatus.Active, cancellationToken);

                if (existingAttendance == null)
                {
                    var attendance = new EventAttendance
                    {
                        Id = Guid.NewGuid(),
                        EventId = eventId,
                        UserId = userGuid,
                        AttendanceType = AttendanceType.RSVP,
                        Status = AttendanceStatus.Active,
                        EventWaiverAccepted = request.EventWaiverAccepted,
                        EventWaiverAcceptedAt = request.EventWaiverAccepted ? DateTime.UtcNow : null,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.EventAttendances.Add(attendance);
                    _logger.LogInformation("Auto-RSVPed user {UserId} to social event {EventId} after volunteer signup with event waiver accepted", userId, eventId);
                }
                else
                {
                    _logger.LogInformation("User {UserId} already has active attendance for event {EventId}, skipping auto-RSVP", userId, eventId);
                }
            }
            else
            {
                _logger.LogInformation("Skipping auto-RSVP for user {UserId} - Event {EventId} is not a social event (type: {EventType})",
                    userId, eventId, position.Event?.EventType);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Map to DTO
            var signupDto = new VolunteerSignupDto
            {
                Id = signup.Id,
                VolunteerPositionId = signup.VolunteerPositionId,
                UserId = signup.UserId,
                Status = signup.Status.ToString(),
                SignedUpAt = signup.SignedUpAt,
                HasCheckedIn = signup.HasCheckedIn,
                CheckedInAt = signup.CheckedInAt,
                HasCompleted = signup.HasCompleted,
                CompletedAt = signup.CompletedAt,
                PositionTitle = position.Title,
                EventTitle = position.Event?.Title ?? string.Empty,
                EventStartDate = position.Event?.StartDate ?? DateTime.MinValue
            };

            _logger.LogInformation("User {UserId} signed up for volunteer position {PositionId}", userId, positionId);

            return (true, signupDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing up user {UserId} for volunteer position {PositionId}", userId, positionId);
            return (false, null, "Failed to sign up for volunteer position");
        }
    }

    /// <summary>
    /// Get user's upcoming volunteer shifts for dashboard
    /// </summary>
    public async Task<(bool success, List<UserVolunteerShiftDto>? shifts, string? error)> GetUserVolunteerShiftsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return (false, null, "Invalid user ID format");
            }

            // Get user's confirmed volunteer signups for upcoming events
            var shifts = await _context.VolunteerSignups
                .AsNoTracking()
                .Include(vs => vs.VolunteerPosition)
                    .ThenInclude(vp => vp!.Event)
                        .ThenInclude(e => e!.Venue)
                .Include(vs => vs.VolunteerPosition)
                    .ThenInclude(vp => vp!.Session)
                .Where(vs => vs.UserId == userGuid
                    && vs.Status == VolunteerSignupStatus.Confirmed
                    && vs.VolunteerPosition!.Event!.StartDate >= DateTime.UtcNow) // Only upcoming events
                .OrderBy(vs => vs.VolunteerPosition!.Event!.StartDate)
                .ToListAsync(cancellationToken);

            // Map to DTOs with async cancellation permission checks
            var shiftDtos = new List<UserVolunteerShiftDto>();
            foreach (var vs in shifts)
            {
                var position = vs.VolunteerPosition!;
                var eventEntity = position.Event!;
                var session = position.Session;

                // Calculate cancellation permission based on event timing rules
                // User cannot cancel if already checked in (checked in service method CancelVolunteerSignupAsync)
                // Here we only check if timing allows cancellation
                var canCancel = await _timeZoneService.IsActionAllowedAsync(
                    eventEntity,
                    EventActionType.CancelVolunteer,
                    cancellationToken);

                shiftDtos.Add(new UserVolunteerShiftDto
                {
                    SignupId = vs.Id,
                    EventTitle = eventEntity.Title,
                    EventLocation = eventEntity.Venue?.Name ?? string.Empty,
                    EventDate = eventEntity.StartDate,
                    PositionTitle = position.Title,
                    SessionName = session?.Name,
                    ShiftStartTime = session?.StartTime,
                    ShiftEndTime = session?.EndTime,
                    CanCancel = canCancel
                });
            }

            _logger.LogInformation("Retrieved {Count} upcoming volunteer shifts for user {UserId}", shiftDtos.Count, userId);

            return (true, shiftDtos, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving volunteer shifts for user {UserId}", userId);
            return (false, null, "Failed to retrieve volunteer shifts");
        }
    }

    /// <summary>
    /// Cancel a volunteer signup
    /// User can only cancel their own signups
    /// Timing enforcement via VolunteerCancellationCloseHours
    /// Cannot cancel if already checked in
    /// </summary>
    public async Task<(bool success, string? error)> CancelVolunteerSignupAsync(
        string signupId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Guid.TryParse(signupId, out var signupGuid))
            {
                return (false, "Invalid signup ID format");
            }

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return (false, "Invalid user ID format");
            }

            // Get the signup with position and event details
            var signup = await _context.VolunteerSignups
                .Include(vs => vs.VolunteerPosition)
                    .ThenInclude(vp => vp!.Event)
                .FirstOrDefaultAsync(vs => vs.Id == signupGuid, cancellationToken);

            if (signup == null)
            {
                return (false, "Volunteer signup not found");
            }

            // Verify ownership - user can only cancel their own signups
            if (signup.UserId != userGuid)
            {
                _logger.LogWarning("User {UserId} attempted to cancel signup {SignupId} belonging to user {OwnerId}",
                    userId, signupId, signup.UserId);
                return (false, "You can only cancel your own volunteer signups");
            }

            // Check if already cancelled
            if (signup.Status == VolunteerSignupStatus.Cancelled)
            {
                return (false, "This volunteer signup is already cancelled");
            }

            // Cannot cancel if already checked in
            if (signup.HasCheckedIn)
            {
                _logger.LogWarning("User {UserId} attempted to cancel checked-in signup {SignupId}", userId, signupId);
                return (false, "Cannot cancel volunteer signup after checking in");
            }

            // Check if cancellation is allowed based on granular timing controls
            if (signup.VolunteerPosition?.Event != null)
            {
                var isAllowed = await _timeZoneService.IsActionAllowedAsync(
                    signup.VolunteerPosition.Event,
                    EventActionType.CancelVolunteer,
                    cancellationToken);

                if (!isAllowed)
                {
                    _logger.LogWarning("Volunteer cancellation attempt for signup {SignupId} outside allowed timing window", signupId);
                    return (false, "Volunteer cancellation window has closed for this event");
                }
            }

            // Cancel the signup
            signup.Status = VolunteerSignupStatus.Cancelled;
            signup.UpdatedAt = DateTime.UtcNow;

            // Update slots filled count on position
            if (signup.VolunteerPosition != null)
            {
                signup.VolunteerPosition.SlotsFilled = Math.Max(0, signup.VolunteerPosition.SlotsFilled - 1);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {UserId} cancelled volunteer signup {SignupId}", userId, signupId);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling volunteer signup {SignupId} for user {UserId}", signupId, userId);
            return (false, "Failed to cancel volunteer signup");
        }
    }
}
