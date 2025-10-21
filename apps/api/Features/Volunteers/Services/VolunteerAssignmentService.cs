using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Volunteers.Models;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Volunteers.Services;

/// <summary>
/// Service for admin/safety team volunteer position assignment management
/// Handles assigning members to positions, viewing assignments, and removing assignments
/// </summary>
public class VolunteerAssignmentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VolunteerAssignmentService> _logger;

    public VolunteerAssignmentService(
        ApplicationDbContext context,
        ILogger<VolunteerAssignmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all member assignments for a volunteer position
    /// Returns list of users currently assigned with their contact information
    /// </summary>
    /// <param name="positionId">Volunteer position ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with list of assignments or error message</returns>
    public async Task<(bool success, List<VolunteerAssignmentDto>? assignments, string? error)> GetPositionSignupsAsync(
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if position exists
            var positionExists = await _context.VolunteerPositions
                .AnyAsync(vp => vp.Id == positionId, cancellationToken);

            if (!positionExists)
            {
                return (false, null, "Volunteer position not found");
            }

            // Get all confirmed signups for this position with user details
            var signups = await _context.VolunteerSignups
                .Include(vs => vs.User)
                .Where(vs => vs.VolunteerPositionId == positionId
                          && vs.Status == VolunteerSignupStatus.Confirmed)
                .OrderBy(vs => vs.SignedUpAt)
                .ToListAsync(cancellationToken);

            // Map to DTOs with user contact information
            var assignmentDtos = signups.Select(vs => new VolunteerAssignmentDto
            {
                SignupId = vs.Id,
                UserId = vs.UserId,
                VolunteerPositionId = vs.VolunteerPositionId,
                SceneName = vs.User?.SceneName ?? string.Empty,
                Email = vs.User?.Email ?? string.Empty,
                FetLifeName = vs.User?.FetLifeName,
                DiscordName = vs.User?.DiscordName,
                Status = vs.Status.ToString(),
                SignedUpAt = vs.SignedUpAt,
                HasCheckedIn = vs.HasCheckedIn,
                CheckedInAt = vs.CheckedInAt
            }).ToList();

            _logger.LogInformation(
                "Retrieved {Count} assignments for volunteer position {PositionId}",
                assignmentDtos.Count, positionId);

            return (true, assignmentDtos, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving assignments for volunteer position {PositionId}", positionId);
            return (false, null, "Failed to retrieve volunteer assignments");
        }
    }

    /// <summary>
    /// Assign a member to a volunteer position
    /// Checks for position capacity and existing participations before assignment
    /// </summary>
    /// <param name="positionId">Volunteer position ID</param>
    /// <param name="userId">User ID to assign</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with assignment details or error message</returns>
    public async Task<(bool success, VolunteerAssignmentDto? assignment, string? error)> AssignMemberToPositionAsync(
        Guid positionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get volunteer position with event details
            var position = await _context.VolunteerPositions
                .Include(vp => vp.Event)
                .FirstOrDefaultAsync(vp => vp.Id == positionId, cancellationToken);

            if (position == null)
            {
                return (false, null, "Volunteer position not found");
            }

            // Get user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return (false, null, "User not found");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return (false, null, "Cannot assign inactive user to volunteer position");
            }

            // Check if position is full
            if (position.IsFullyStaffed)
            {
                return (false, null, "Volunteer position is already fully staffed");
            }

            // Check if user already assigned to this position
            var existingSignup = await _context.VolunteerSignups
                .FirstOrDefaultAsync(vs => vs.VolunteerPositionId == positionId
                    && vs.UserId == userId
                    && vs.Status != VolunteerSignupStatus.Cancelled, cancellationToken);

            if (existingSignup != null)
            {
                return (false, null, "User is already assigned to this volunteer position");
            }

            // Check if user has any active participation for this event (ticket/RSVP/volunteer)
            var eventId = position.EventId;
            var existingParticipation = await _context.EventParticipations
                .FirstOrDefaultAsync(ep => ep.EventId == eventId
                    && ep.UserId == userId
                    && ep.Status == Participation.Entities.ParticipationStatus.Active, cancellationToken);

            // If no participation exists, create an RSVP
            if (existingParticipation == null)
            {
                var participation = new Participation.Entities.EventParticipation
                {
                    Id = Guid.NewGuid(),
                    EventId = eventId,
                    UserId = userId,
                    ParticipationType = Participation.Entities.ParticipationType.RSVP,
                    Status = Participation.Entities.ParticipationStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Notes = "Auto-created when assigned to volunteer position"
                };

                _context.EventParticipations.Add(participation);
                _logger.LogInformation(
                    "Auto-created RSVP for user {UserId} when assigned to volunteer position {PositionId}",
                    userId, positionId);
            }

            // Create the volunteer signup
            var signup = new VolunteerSignup
            {
                Id = Guid.NewGuid(),
                VolunteerPositionId = positionId,
                UserId = userId,
                Status = VolunteerSignupStatus.Confirmed,
                SignedUpAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VolunteerSignups.Add(signup);

            // Update position slots filled
            position.SlotsFilled++;

            // Explicitly mark position as modified to ensure EF Core tracks the change
            _context.VolunteerPositions.Update(position);

            await _context.SaveChangesAsync(cancellationToken);

            // Verify persistence with fresh query
            var saved = await _context.VolunteerSignups
                .AsNoTracking()
                .Include(vs => vs.User)
                .FirstOrDefaultAsync(vs => vs.Id == signup.Id, cancellationToken);

            if (saved == null)
            {
                _logger.LogError(
                    "CRITICAL: Volunteer signup {SignupId} failed to persist to database", signup.Id);
                return (false, null, "Failed to save volunteer assignment to database");
            }

            _logger.LogInformation(
                "Verified volunteer assignment {SignupId} saved for user {UserId} to position {PositionId}",
                saved.Id, userId, positionId);

            // Map to DTO
            var assignmentDto = new VolunteerAssignmentDto
            {
                SignupId = saved.Id,
                UserId = saved.UserId,
                VolunteerPositionId = saved.VolunteerPositionId,
                SceneName = saved.User?.SceneName ?? string.Empty,
                Email = saved.User?.Email ?? string.Empty,
                FetLifeName = saved.User?.FetLifeName,
                DiscordName = saved.User?.DiscordName,
                Status = saved.Status.ToString(),
                SignedUpAt = saved.SignedUpAt,
                HasCheckedIn = saved.HasCheckedIn,
                CheckedInAt = saved.CheckedInAt
            };

            return (true, assignmentDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error assigning user {UserId} to volunteer position {PositionId}", userId, positionId);
            return (false, null, "Failed to assign member to volunteer position");
        }
    }

    /// <summary>
    /// Remove a member assignment from a volunteer position
    /// Only allows removal if user has not checked in yet
    /// </summary>
    /// <param name="signupId">Volunteer signup ID to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with error message if failed</returns>
    public async Task<(bool success, string? error)> RemoveAssignmentAsync(
        Guid signupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get signup with position details
            var signup = await _context.VolunteerSignups
                .Include(vs => vs.VolunteerPosition)
                .FirstOrDefaultAsync(vs => vs.Id == signupId, cancellationToken);

            if (signup == null)
            {
                return (false, "Volunteer signup not found");
            }

            // Check if already cancelled
            if (signup.Status == VolunteerSignupStatus.Cancelled)
            {
                return (false, "Volunteer signup is already cancelled");
            }

            // Check if user has checked in
            if (signup.HasCheckedIn)
            {
                return (false, "Cannot remove assignment after volunteer has checked in");
            }

            // Mark as cancelled
            signup.Status = VolunteerSignupStatus.Cancelled;
            signup.UpdatedAt = DateTime.UtcNow;

            // Explicitly mark signup as modified to ensure EF Core tracks the change
            _context.VolunteerSignups.Update(signup);

            // Update position slots filled count
            var position = signup.VolunteerPosition;
            if (position != null && position.SlotsFilled > 0)
            {
                position.SlotsFilled--;
                _context.VolunteerPositions.Update(position);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Verify cancellation persisted
            var cancelled = await _context.VolunteerSignups
                .AsNoTracking()
                .FirstOrDefaultAsync(vs => vs.Id == signupId, cancellationToken);

            if (cancelled == null)
            {
                _logger.LogError(
                    "CRITICAL: Volunteer signup {SignupId} disappeared after cancellation", signupId);
                return (false, "Failed to verify cancellation in database");
            }

            if (cancelled.Status != VolunteerSignupStatus.Cancelled)
            {
                _logger.LogError(
                    "CRITICAL: Signup status is {Status} instead of Cancelled", cancelled.Status);
                return (false, "Cancellation did not persist to database");
            }

            _logger.LogInformation(
                "Verified volunteer assignment cancellation {SignupId} (Status: {Status})",
                cancelled.Id, cancelled.Status);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing volunteer assignment {SignupId}", signupId);
            return (false, "Failed to remove volunteer assignment");
        }
    }

    /// <summary>
    /// Search for active members by name, email, or Discord name
    /// Excludes inactive users and requires minimum 3 characters
    /// </summary>
    /// <param name="searchQuery">Search term (min 3 characters)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success tuple with list of matching users or error message</returns>
    public async Task<(bool success, List<UserSearchResultDto>? users, string? error)> SearchUsersAsync(
        string searchQuery,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate search query length
            if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Trim().Length < 3)
            {
                return (false, null, "Search query must be at least 3 characters");
            }

            var normalizedQuery = searchQuery.Trim().ToLower();

            // Search across scene name, real name (first/last), email, and Discord name
            // Only include active users
            var users = await _context.Users
                .Where(u => u.IsActive
                    && (u.SceneName.ToLower().Contains(normalizedQuery)
                        || (u.FirstName != null && u.FirstName.ToLower().Contains(normalizedQuery))
                        || (u.LastName != null && u.LastName.ToLower().Contains(normalizedQuery))
                        || (u.Email != null && u.Email.ToLower().Contains(normalizedQuery))
                        || (u.DiscordName != null && u.DiscordName.ToLower().Contains(normalizedQuery))))
                .OrderBy(u => u.SceneName)
                .Take(50) // Limit results to prevent huge result sets
                .ToListAsync(cancellationToken);

            // Map to DTOs
            var userDtos = users.Select(u => new UserSearchResultDto
            {
                UserId = u.Id,
                SceneName = u.SceneName,
                Email = u.Email ?? string.Empty,
                DiscordName = u.DiscordName,
                RealName = !string.IsNullOrEmpty(u.FirstName) || !string.IsNullOrEmpty(u.LastName)
                    ? $"{u.FirstName} {u.LastName}".Trim()
                    : null
            }).ToList();

            _logger.LogInformation(
                "User search for '{Query}' returned {Count} results",
                searchQuery, userDtos.Count);

            return (true, userDtos, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users with query '{Query}'", searchQuery);
            return (false, null, "Failed to search users");
        }
    }
}
