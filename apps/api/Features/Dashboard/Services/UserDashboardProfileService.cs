using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Dashboard.Models;
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

            // Get user's registered events from TicketPurchases
            var query = _context.TicketPurchases
                .Include(tp => tp.TicketType)
                    .ThenInclude(tt => tt!.Event)
                .Where(tp => tp.UserId == userId)
                .AsQueryable();

            // Filter by date if not including past events
            if (!includePast)
            {
                query = query.Where(tp => tp.TicketType!.Event!.EndDate >= DateTime.UtcNow);
            }

            var tickets = await query
                .OrderBy(tp => tp.TicketType!.Event!.StartDate)
                .ToListAsync(cancellationToken);

            var events = tickets
                .Where(tp => tp.TicketType?.Event != null)
                .GroupBy(tp => tp.TicketType!.Event!.Id)
                .Select(g =>
                {
                    var evt = g.First().TicketType!.Event!;
                    var hasTicket = g.Any(tp => tp.TicketType != null && tp.TicketType.Price > 0);
                    var isSocialEvent = evt.EventType.ToLower().Contains("social");

                    // Determine registration status
                    string registrationStatus;
                    if (evt.EndDate < DateTime.UtcNow)
                    {
                        registrationStatus = "Attended";
                    }
                    else if (hasTicket)
                    {
                        registrationStatus = isSocialEvent ? "Ticket Purchased (Social Event)" : "Ticket Purchased";
                    }
                    else
                    {
                        registrationStatus = "RSVP Confirmed";
                    }

                    return new UserEventDto
                    {
                        Id = evt.Id,
                        Title = evt.Title,
                        StartDate = evt.StartDate,
                        EndDate = evt.EndDate,
                        Location = evt.Location,
                        Description = evt.ShortDescription,
                        RegistrationStatus = registrationStatus,
                        IsSocialEvent = isSocialEvent,
                        HasTicket = hasTicket
                    };
                })
                .ToList();

            _logger.LogInformation("Found {EventCount} events for user {UserId}", new object[] { events.Count, userId });

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
                Email = user.Email ?? string.Empty,
                Pronouns = user.Pronouns,
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

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<UserProfileDto>.Failure("User not found");
            }

            // Update user properties
            user.SceneName = request.SceneName;
            user.Email = request.Email;
            user.UserName = request.Email; // Keep UserName in sync with Email
            user.Pronouns = request.Pronouns ?? string.Empty;
            user.PhoneNumber = request.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return Result<UserProfileDto>.Failure("Failed to update profile", errors);
            }

            // Return updated profile
            var profile = new UserProfileDto
            {
                UserId = user.Id,
                SceneName = user.SceneName,
                Email = user.Email ?? string.Empty,
                Pronouns = user.Pronouns,
                PhoneNumber = user.PhoneNumber,
                VettingStatus = ((VettingStatus)user.VettingStatus).ToString()
            };

            _logger.LogInformation("Successfully updated profile for user {UserId}", userId);

            return Result<UserProfileDto>.Success(profile);
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
