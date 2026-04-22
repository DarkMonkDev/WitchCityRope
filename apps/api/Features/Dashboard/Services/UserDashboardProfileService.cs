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

            // STEP 1: SERVER-SIDE PROJECTION - Query EventAttendances and project to DTO at database level
            // EventAttendances is the central table for both RSVP and ticket attendance types
            // Benefits: Only loads needed event fields, no Include() overhead
            var query = _context.EventAttendances
                .AsNoTracking()
                .Where(ea => ea.UserId == userId)
                .Where(ea => ea.Status == AttendanceStatus.Active) // Only active attendances (not cancelled)
                .AsQueryable();

            // Filter by date if not including past events
            if (!includePast)
            {
                query = query.Where(ea => ea.Event.EndDate >= DateTime.UtcNow);
            }

            // STEP 2: Group by event and aggregate basic attendance data (without sessions)
            // This ensures one UserEventDto per event even if user has multiple attendance types (RSVP + Ticket)
            var events = await query
                .GroupBy(ea => new {
                    ea.Event.Id,
                    ea.Event.Title,
                    ea.Event.StartDate,
                    ea.Event.EndDate,
                    VenueName = ea.Event.Venue != null ? ea.Event.Venue.Name : string.Empty,
                    ea.Event.ShortDescription,
                    ea.Event.AllowRsvps,
                    ea.Event.RequireTicketPurchase,
                    ea.Event.VettedMembersOnly
                })
                .Select(g => new UserEventDto
                {
                    // Projected at database level - only loads these event fields
                    Id = g.Key.Id,
                    Title = g.Key.Title,
                    StartDate = g.Key.StartDate,
                    EndDate = g.Key.EndDate,
                    Location = g.Key.VenueName,
                    Description = g.Key.ShortDescription,
                    IsSocialEvent = g.Key.AllowRsvps && !g.Key.RequireTicketPurchase,
                    // HasTicket is true if ANY attendance for this event is a Ticket
                    HasTicket = g.Any(ea => ea.AttendanceType == AttendanceType.Ticket),
                    // Calculate registration status at database level
                    // Priority: Attended > Ticket Purchased > RSVP Confirmed
                    RegistrationStatus = g.Key.EndDate < DateTime.UtcNow
                        ? "Attended"
                        : g.Any(ea => ea.AttendanceType == AttendanceType.Ticket)
                            ? (g.Key.AllowRsvps && !g.Key.RequireTicketPurchase ? "Ticket Purchased (Social Event)" : "Ticket Purchased")
                            : "RSVP Confirmed"
                })
                .OrderBy(e => e.StartDate)
                .ToListAsync(cancellationToken);

            // STEP 3: Find "purchaser-only" events — events where the user purchased tickets
            // for others but has no personal attendance (no own RSVP or ticket).
            // These events are missing from the attendance-based query above.
            var attendedEventIds = events.Select(e => e.Id).ToHashSet();

            var purchaserOnlyQuery = _context.TicketPurchases
                .AsNoTracking()
                .Where(tp => tp.UserId == userId)
                // Inline IsPaymentCompleted check — computed properties can't be
                // translated to SQL by EF Core (causes InvalidOperationException)
                .Where(tp => tp.PaymentStatus == TicketPurchasePaymentStatus.Completed
                          || tp.PaymentStatus == TicketPurchasePaymentStatus.Confirmed
                          || tp.PaymentStatus == TicketPurchasePaymentStatus.PartiallyRefunded)
                .Where(tp => tp.TicketType != null && tp.TicketType.Event != null)
                // Navigate TicketPurchase -> TicketType -> Event, exclude already-attended events
                .Select(tp => tp.TicketType!.Event!)
                .Where(e => !attendedEventIds.Contains(e.Id))
                .AsQueryable();

            if (!includePast)
            {
                purchaserOnlyQuery = purchaserOnlyQuery.Where(e => e.EndDate >= DateTime.UtcNow);
            }

            var purchaserOnlyEvents = await purchaserOnlyQuery
                .Select(e => new { e.Id, e.Title, e.StartDate, e.EndDate,
                    VenueName = e.Venue != null ? e.Venue.Name : string.Empty,
                    e.ShortDescription, e.AllowRsvps, e.RequireTicketPurchase })
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var pe in purchaserOnlyEvents)
            {
                events.Add(new UserEventDto
                {
                    Id = pe.Id,
                    Title = pe.Title,
                    StartDate = pe.StartDate,
                    EndDate = pe.EndDate,
                    Location = pe.VenueName,
                    Description = pe.ShortDescription,
                    IsSocialEvent = pe.AllowRsvps && !pe.RequireTicketPurchase,
                    HasTicket = false,
                    RegistrationStatus = "Purchased for Others",
                    IsPurchaserOnly = true
                });
            }

            // Re-sort after adding purchaser-only events
            events = events.OrderBy(e => e.StartDate).ToList();

            // STEP 4: Populate session and ticket data for each event (post-query in-memory)
            // This is necessary because EF Core doesn't support complex navigation in GroupBy
            foreach (var eventDto in events)
            {
                // Get user's registered sessions for this event
                var registeredSessions = await _context.EventAttendances
                    .AsNoTracking()
                    .Where(ea => ea.UserId == userId
                        && ea.EventId == eventDto.Id
                        && ea.Status == AttendanceStatus.Active
                        && ea.AttendanceType == AttendanceType.Ticket
                        && ea.SessionId != null)
                    .Select(ea => new UserSessionDto
                    {
                        Id = ea.Session!.Id,
                        Name = ea.Session.Name,
                        StartTime = ea.Session.StartTime,
                        EndTime = ea.Session.EndTime
                    })
                    .Distinct()
                    .ToListAsync(cancellationToken);

                eventDto.RegisteredSessions = registeredSessions;

                // Get ALL sessions for this event (regardless of user's tickets).
                // This ensures RSVP-only users still see session dates/times on their dashboard card,
                // instead of "Date and Time coming soon" which only showed when registeredSessions was empty.
                var allSessions = await _context.Sessions
                    .AsNoTracking()
                    .Where(s => s.EventId == eventDto.Id)
                    .Select(s => new UserSessionDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    })
                    .ToListAsync(cancellationToken);

                eventDto.EventSessions = allSessions;

                // Calculate additional sessions available (sessions user doesn't have tickets for)
                eventDto.AdditionalSessionsAvailable = allSessions.Count - registeredSessions.Count;

                // Check if the event has any ticket types available (paid or donation/free).
                // Used by frontend to show "Purchase Ticket" button for RSVP users without tickets,
                // without needing a separate API call to fetch full event details.
                eventDto.HasAvailableTickets = await _context.TicketTypes
                    .AsNoTracking()
                    .Where(tt => tt.EventId == eventDto.Id)
                    .AnyAsync(cancellationToken);

                // Get user's purchased tickets for this event.
                // Joins through EventAttendance → TicketPurchase → TicketType to get the ticket type name,
                // and optionally includes the session name/times for display below ticket name.
                // Also includes AssignedByUser scene name so the dashboard can show a
                // "From: [Name]" badge for tickets assigned to the user by someone else.
                eventDto.Tickets = await _context.EventAttendances
                    .AsNoTracking()
                    .Where(ea => ea.UserId == userId
                        && ea.EventId == eventDto.Id
                        && ea.Status == AttendanceStatus.Active
                        && ea.AttendanceType == AttendanceType.Ticket
                        && ea.TicketPurchaseId != null)
                    .Select(ea => new UserTicketDto
                    {
                        TicketTypeName = ea.TicketPurchase!.TicketType != null
                            ? ea.TicketPurchase.TicketType.Name
                            : "Ticket",
                        SessionName = ea.Session != null ? ea.Session.Name : null,
                        SessionStartTime = ea.Session != null ? ea.Session.StartTime : null,
                        SessionEndTime = ea.Session != null ? ea.Session.EndTime : null,
                        // Resolve assigner's scene name via navigation property.
                        // Null for self-purchased tickets (AssignedByUserId is null).
                        AssignedBySceneName = ea.AssignedByUser != null
                            ? ea.AssignedByUser.SceneName
                            : null
                    })
                    .ToListAsync(cancellationToken);
            }

            _logger.LogInformation("Retrieved {EventCount} events using server-side projection for user {UserId}", events.Count, userId);

            return Result<List<UserEventDto>>.Success(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching events for user {UserId}", userId);
            return Result<List<UserEventDto>>.Infrastructure("Failed to fetch user events. See server logs for details.");
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
                return Result<VettingStatusDto>.NotFound("User not found");
            }

            // Get the user's vetting status from ApplicationUser
            var vettingStatus = user.VettingStatus;

            // Map to DTO with appropriate message
            var dto = new VettingStatusDto
            {
                Status = vettingStatus,
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
                    // Points to the CMS-managed page with slug "vetting-interview-scheduling".
                    // The page is rendered by CmsDynamicPage via the catch-all /:slug route
                    // in apps/web/src/routes/router.tsx, and its content is editable by
                    // admins via the CMS UI. The dashboard and /join page alerts both link
                    // here via VettingStatusDto.InterviewScheduleUrl → VettingAlertBox.
                    dto.InterviewScheduleUrl = "/vetting-interview-scheduling";
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

                case VettingStatus.Withdrawn:
                    // Phase 3a (tech debt #4): previously fell through to the default
                    // empty-string message, which gave consumers of VettingStatusDto.Message
                    // a blank string for withdrawn applications. This case provides a
                    // sensible status-specific message so frontend and downstream
                    // consumers can display something meaningful.
                    dto.Message = "Your application has been withdrawn. Contact us if you'd like to reapply.";
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
            return Result<VettingStatusDto>.Infrastructure("Failed to fetch vetting status. See server logs for details.");
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
                return Result<UserProfileDto>.NotFound("User not found");
            }

            // Check if user has a non-deleted vetting application by querying the database
            // This is the source of truth - more reliable than user.HasVettingApplication flag
            // Exclude soft-deleted applications so users see "no application" and can reapply
            var application = await _context.VettingApplications
                .AsNoTracking()
                .Where(v => !v.IsDeleted)
                .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);

            var hasApplication = application != null;
            var vettingStatus = hasApplication
                ? application!.WorkflowStatus
                : VettingStatus.UnderReview; // Default for users without application

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
                OtherNames = user.OtherNames,
                VettingStatus = vettingStatus,
                HasVettingApplication = hasApplication
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
                    return Result<UserProfileDto>.NotFound("User not found");
                }

                // Store the original concurrency stamp for logging
                var originalStamp = user.ConcurrencyStamp;

                // Track changed fields for audit logging
                var changedFields = new List<(string FieldName, string? OldValue, string? NewValue)>();

                // Helper method for change detection (normalize null and empty string)
                bool HasChanged(string? oldValue, string? newValue)
                {
                    var normalizedOld = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue.Trim();
                    var normalizedNew = string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim();
                    return !string.Equals(normalizedOld, normalizedNew, StringComparison.Ordinal);
                }

                // Detect changes for each field
                if (HasChanged(user.SceneName, request.SceneName))
                    changedFields.Add(("Scene Name", user.SceneName, request.SceneName));

                if (HasChanged(user.FirstName, request.FirstName))
                    changedFields.Add(("First Name", user.FirstName, request.FirstName));

                if (HasChanged(user.LastName, request.LastName))
                    changedFields.Add(("Last Name", user.LastName, request.LastName));

                if (HasChanged(user.Email, request.Email))
                    changedFields.Add(("Email", user.Email, request.Email));

                if (HasChanged(user.Pronouns, request.Pronouns))
                    changedFields.Add(("Pronouns", user.Pronouns, request.Pronouns));

                if (HasChanged(user.Bio, request.Bio))
                    changedFields.Add(("Bio", user.Bio, request.Bio));

                if (HasChanged(user.DiscordName, request.DiscordName))
                    changedFields.Add(("Discord Name", user.DiscordName, request.DiscordName));

                if (HasChanged(user.FetLifeName, request.FetLifeName))
                    changedFields.Add(("FetLife Name", user.FetLifeName, request.FetLifeName));

                if (HasChanged(user.PhoneNumber, request.PhoneNumber))
                    changedFields.Add(("Phone Number", user.PhoneNumber, request.PhoneNumber));

                if (HasChanged(user.OtherNames, request.OtherNames))
                    changedFields.Add(("Other Names", user.OtherNames, request.OtherNames));

                _logger.LogInformation("Change detection complete: {Count} changes found for user {UserId}", changedFields.Count, userId);
                foreach (var (fieldName, oldValue, newValue) in changedFields)
                {
                    _logger.LogInformation("  Change: {FieldName} | Old: {OldValue} | New: {NewValue}",
                        fieldName, oldValue ?? "(empty)", newValue ?? "(empty)");
                }

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
                user.OtherNames = request.OtherNames;
                user.UpdatedAt = DateTime.UtcNow;

                // UserManager.UpdateAsync handles optimistic concurrency automatically via ConcurrencyStamp
                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    // Create UserNote entries for each changed field
                    if (changedFields.Count > 0)
                    {
                        _logger.LogInformation("Creating {Count} UserNote entries for profile changes (user {UserId})", changedFields.Count, userId);

                        foreach (var (fieldName, oldValue, newValue) in changedFields)
                        {
                            var note = new WitchCityRope.Api.Data.Entities.UserNote
                            {
                                UserId = userId,
                                NoteType = "ProfileChange",
                                Content = $"{fieldName} changed from \"{oldValue ?? "(empty)"}\" to \"{newValue ?? "(empty)"}\"",
                                AuthorId = userId, // User changed their own profile
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.UserNotes.Add(note);
                        }

                        // Save UserNote entries to database
                        var savedCount = await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Successfully saved {Count} UserNote entries to database", savedCount);
                    }
                    else
                    {
                        _logger.LogInformation("No changes detected - skipping UserNote creation");
                    }

                    // Success - check for non-deleted vetting application and return updated profile
                    var application = await _context.VettingApplications
                        .AsNoTracking()
                        .Where(v => !v.IsDeleted)
                        .FirstOrDefaultAsync(v => v.UserId == userId, cancellationToken);

                    var hasApplication = application != null;
                    var vettingStatus = hasApplication
                        ? application!.WorkflowStatus
                        : VettingStatus.UnderReview;

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
                        VettingStatus = vettingStatus,
                        HasVettingApplication = hasApplication
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
