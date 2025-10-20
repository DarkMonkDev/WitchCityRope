using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Volunteers.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Volunteers.Services;

/// <summary>
/// Service for managing volunteer positions and signups
/// </summary>
public class VolunteerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VolunteerService> _logger;

    public VolunteerService(
        ApplicationDbContext context,
        ILogger<VolunteerService> logger)
    {
        _context = context;
        _logger = logger;
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

            // Check if event exists
            var eventExists = await _context.Events.AnyAsync(e => e.Id == eventGuid, cancellationToken);
            if (!eventExists)
            {
                return (false, null, "Event not found");
            }

            // Get volunteer positions
            var query = _context.VolunteerPositions
                .Include(vp => vp.Session)
                .Where(vp => vp.EventId == eventGuid);

            // Only show public-facing positions to non-authenticated users
            if (string.IsNullOrEmpty(userId))
            {
                query = query.Where(vp => vp.IsPublicFacing);
            }

            var positions = await query.ToListAsync(cancellationToken);

            // Get user's existing signups if authenticated
            List<VolunteerSignup>? userSignups = null;
            if (!string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userGuid))
            {
                userSignups = await _context.VolunteerSignups
                    .Where(vs => vs.UserId == userGuid && vs.Status == VolunteerSignupStatus.Confirmed)
                    .ToListAsync(cancellationToken);
            }

            // Map to DTOs
            var positionDtos = positions.Select(vp =>
            {
                var userSignup = userSignups?.FirstOrDefault(us => us.VolunteerPositionId == vp.Id);

                return new VolunteerPositionDto
                {
                    Id = vp.Id,
                    EventId = vp.EventId,
                    SessionId = vp.SessionId,
                    Title = vp.Title,
                    Description = vp.Description,
                    SlotsNeeded = vp.SlotsNeeded,
                    SlotsFilled = vp.SlotsFilled,
                    SlotsRemaining = vp.SlotsRemaining,
                    RequiresExperience = vp.RequiresExperience,
                    Requirements = vp.Requirements,
                    IsPublicFacing = vp.IsPublicFacing,
                    IsFullyStaffed = vp.IsFullyStaffed,
                    SessionName = vp.Session?.Name,
                    SessionStartTime = vp.Session?.StartTime,
                    SessionEndTime = vp.Session?.EndTime,
                    HasUserSignedUp = userSignup != null,
                    UserSignupId = userSignup?.Id
                };
            }).ToList();

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

            // Get the volunteer position with event details
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
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VolunteerSignups.Add(signup);

            // Update slots filled count
            position.SlotsFilled++;

            // Auto-RSVP user to the event if not already registered
            var eventId = position.EventId;
            var existingParticipation = await _context.EventParticipations
                .FirstOrDefaultAsync(ep => ep.EventId == eventId
                    && ep.UserId == userGuid
                    && ep.ParticipationType == ParticipationType.RSVP, cancellationToken);

            if (existingParticipation == null)
            {
                var participation = new EventParticipation
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = userGuid,
                    ParticipationType = ParticipationType.RSVP,
                    Status = ParticipationStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                _context.EventParticipations.Add(participation);
                _logger.LogInformation("Auto-RSVPed user {UserId} to event {EventId} after volunteer signup", userId, eventId);
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
                Notes = signup.Notes,
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
}
