using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Dashboard.Models;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Dashboard.Services;

/// <summary>
/// Implementation of user dashboard profile and event management service
/// </summary>
public class UserDashboardProfileService : IUserDashboardProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserDashboardProfileService> _logger;

    public UserDashboardProfileService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<UserDashboardProfileService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<Result<List<UserEventDto>>> GetUserEventsAsync(
        Guid userId,
        bool includePast = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching events for user {UserId}, includePast={IncludePast}", userId, includePast);

            // SERVER-SIDE PROJECTION: Query EventParticipations and project to DTO at database level
            // EventParticipations is the central table for both RSVP and ticket participation types
            // Benefits: Only loads needed event fields, no Include() overhead
            var query = _context.EventParticipations
                .AsNoTracking()
                .Where(ep => ep.UserId == userId)
                .Where(ep => ep.Status == ParticipationStatus.Active) // Only active participations (not cancelled)
                .AsQueryable();

            // Filter by date if not including past events
            if (!includePast)
            {
                query = query.Where(ep => ep.Event.EndDate >= DateTime.UtcNow);
            }

            // SERVER-SIDE PROJECTION: Project to DTO in single database query
            var events = await query
                .OrderBy(ep => ep.Event.StartDate)
                .Select(ep => new UserEventDto
                {
                    // Projected at database level - only loads these event fields
                    Id = ep.Event.Id,
                    Title = ep.Event.Title,
                    StartDate = ep.Event.StartDate,
                    EndDate = ep.Event.EndDate,
                    Location = ep.Event.Location,
                    Description = ep.Event.ShortDescription,
                    IsSocialEvent = ep.Event.EventType.ToLower().Contains("social"),
                    HasTicket = ep.ParticipationType == ParticipationType.Ticket,
                    // Calculate registration status at database level
                    RegistrationStatus = ep.Event.EndDate < DateTime.UtcNow
                        ? "Attended"
                        : ep.ParticipationType == ParticipationType.Ticket
                            ? (ep.Event.EventType.ToLower().Contains("social") ? "Ticket Purchased (Social Event)" : "Ticket Purchased")
                            : "RSVP Confirmed"
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {EventCount} events using server-side projection for user {UserId}", events.Count, userId);

            return Result<List<UserEventDto>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching events for user {UserId}", userId);
            return Result<List<UserEventDto>>.Failure("Failed to fetch user events", ex.Message);
        }
    }

    public async Task<Result<VettingStatusDto>> GetVettingStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching vetting status for user {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<VettingStatusDto>.Failure("User not found");
            }

            // Get the user's vetting status from ApplicationUser
            var vettingStatus = (VettingStatus)user.VettingStatus;

            // Map to DTO with appropriate message
            var dto = new VettingStatusDto
            {
                Status = vettingStatus.ToString(),
                LastUpdatedAt = user.UpdatedAt
            };

            // Set status-specific message and URLs
            switch (vettingStatus)
            {
                case VettingStatus.UnderReview:
                    dto.Message = "Your membership application is currently under review. We'll notify you via email once it's been reviewed.";
                    break;

                case VettingStatus.InterviewApproved:
                    dto.Message = "Great News! Your application has been approved for interview. Schedule your vetting interview to complete your membership.";
                    dto.InterviewScheduleUrl = "/vetting/schedule-interview";
                    break;

                case VettingStatus.FinalReview:
                    dto.Message = "Your interview has been completed and your application is in final review. We'll notify you of the decision soon.";
                    break;

                case VettingStatus.OnHold:
                    dto.Message = "Your membership is currently on hold. Contact us if you'd like to resume your membership.";
                    break;

                case VettingStatus.Denied:
                    dto.Message = "Your membership application was not approved at this time. Learn about reapplying.";
                    dto.ReapplyInfoUrl = "/vetting/reapply";
                    break;

                case VettingStatus.Approved:
                    // This should rarely happen as approved users get IsVetted=true
                    dto.Message = "Your application has been approved! Your vetted member access is being finalized.";
                    break;

                default:
                    dto.Message = "";
                    break;
            }

            return Result<VettingStatusDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching vetting status for user {UserId}", userId);
            return Result<VettingStatusDto>.Failure("Failed to fetch vetting status", ex.Message);
        }
    }

    public async Task<Result<UserProfileDto>> GetUserProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching profile for user {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<UserProfileDto>.Failure("User not found");
            }

            var profile = new UserProfileDto
            {
                UserId = user.Id,
                SceneName = user.SceneName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Pronouns = user.Pronouns,
                Bio = user.Bio,
                DiscordName = user.DiscordName,
                FetLifeName = user.FetLifeName,
                PhoneNumber = user.PhoneNumber,
                VettingStatus = ((VettingStatus)user.VettingStatus).ToString()
            };

            return Result<UserProfileDto>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching profile for user {UserId}", userId);
            return Result<UserProfileDto>.Failure("Failed to fetch user profile", ex.Message);
        }
    }

    public async Task<Result<UserProfileDto>> UpdateUserProfileAsync(
        Guid userId,
        UpdateProfileDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating profile for user {UserId}", userId);

            // Use a retry loop to handle optimistic concurrency conflicts
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                // Fetch fresh user data for each attempt to ensure we have latest ConcurrencyStamp
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return Result<UserProfileDto>.Failure("User not found");
                }

                // Store the original concurrency stamp for logging
                var originalStamp = user.ConcurrencyStamp;

                // Update user properties
                user.SceneName = request.SceneName;
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Email = request.Email;
                user.UserName = request.Email; // Keep UserName in sync with Email
                user.Pronouns = request.Pronouns ?? string.Empty;
                user.Bio = request.Bio;
                user.DiscordName = request.DiscordName;
                user.FetLifeName = request.FetLifeName;
                user.PhoneNumber = request.PhoneNumber;
                user.UpdatedAt = DateTime.UtcNow;

                // UserManager.UpdateAsync handles optimistic concurrency automatically via ConcurrencyStamp
                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    // Success - return updated profile
                    var profile = new UserProfileDto
                    {
                        UserId = user.Id,
                        SceneName = user.SceneName,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email ?? string.Empty,
                        Pronouns = user.Pronouns,
                        Bio = user.Bio,
                        DiscordName = user.DiscordName,
                        FetLifeName = user.FetLifeName,
                        PhoneNumber = user.PhoneNumber,
                        VettingStatus = ((VettingStatus)user.VettingStatus).ToString()
                    };

                    _logger.LogInformation("Successfully updated profile for user {UserId} on attempt {Attempt}", userId, attempt + 1);
                    return Result<UserProfileDto>.Success(profile);
                }

                // Check if the failure is due to concurrency conflict
                var concurrencyError = updateResult.Errors.FirstOrDefault(e =>
                    e.Code == "ConcurrencyFailure" || e.Description.Contains("concurrency", StringComparison.OrdinalIgnoreCase));

                if (concurrencyError != null && attempt < maxRetries - 1)
                {
                    // Concurrency conflict detected - retry with fresh data
                    _logger.LogWarning(
                        "Concurrency conflict updating user {UserId} (attempt {Attempt}/{MaxRetries}). " +
                        "Original stamp: {OriginalStamp}. Retrying...",
                        userId, attempt + 1, maxRetries, originalStamp);

                    // Small delay before retry to reduce contention
                    await Task.Delay(50 * (attempt + 1), cancellationToken);
                    continue;
                }

                // Non-concurrency error or final retry exhausted
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                _logger.LogError(
                    "Failed to update profile for user {UserId} after {Attempt} attempts. Errors: {Errors}",
                    userId, attempt + 1, errors);
                return Result<UserProfileDto>.Failure("Failed to update profile", errors);
            }

            // Should never reach here, but just in case
            return Result<UserProfileDto>.Failure("Failed to update profile after multiple attempts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return Result<UserProfileDto>.Failure("Failed to update profile", ex.Message);
        }
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Changing password for user {UserId}", userId);

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            // Verify current password
            var passwordCheck = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!passwordCheck)
            {
                return Result.Failure("Current password is incorrect");
            }

            // Change password
            var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changeResult.Succeeded)
            {
                var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
                return Result.Failure("Failed to change password", errors);
            }

            _logger.LogInformation("Successfully changed password for user {UserId}", userId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return Result.Failure("Failed to change password", ex.Message);
        }
    }
}
