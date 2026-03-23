using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Safety.Services;
using WitchCityRope.Api.Features.Users.Constants;
using WitchCityRope.Api.Features.Users.Models.MemberDetails;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Users.Services;

/// <summary>
/// Service for comprehensive member details in admin member management
/// Implements unified notes system and aggregated member information
/// </summary>
public class MemberDetailsService : IMemberDetailsService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<MemberDetailsService> _logger;
    private readonly IEncryptionService _encryptionService;

    public MemberDetailsService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<MemberDetailsService> logger,
        IEncryptionService encryptionService)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Get comprehensive member details including participation summary
    /// Endpoint 1: GET /api/users/{id}/details
    /// </summary>
    public async Task<(bool Success, MemberDetailsResponse? Response, string Error)> GetMemberDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found for member details: {UserId}", userId);
                return (false, null, "User not found");
            }

            // SERVER-SIDE PROJECTION: Calculate participation counts at database level
            // Benefits: Eliminates loading full participations, more efficient counting

            // TotalEventsAttended: Active registrations for past events (event ended)
            var totalAttended = await _context.EventAttendances
                .AsNoTracking()
                .Where(ep => ep.UserId == userId
                          && ep.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active
                          && ep.Event.EndDate < DateTime.UtcNow)
                .CountAsync(cancellationToken);

            // LastEventAttended: Most recent past event with Active status
            var lastEventAttended = await _context.EventAttendances
                .AsNoTracking()
                .Where(ep => ep.UserId == userId
                          && ep.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active
                          && ep.Event.EndDate < DateTime.UtcNow)
                .OrderByDescending(ep => ep.Event.EndDate)
                .Select(ep => ep.Event.EndDate)
                .FirstOrDefaultAsync(cancellationToken);

            // FutureEvents: Active registrations where event hasn't started yet
            // CRITICAL: Filter based on sessions - event is "future" if it has ANY session with StartTime > now
            var now = DateTime.UtcNow;
            var futureEvents = await _context.EventAttendances
                .AsNoTracking()
                .Include(ep => ep.Event)
                    .ThenInclude(e => e.Sessions) // Load sessions for session-based filtering
                .Where(ep => ep.UserId == userId
                          && ep.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active
                          && (ep.Event.Sessions.Any(s => s.StartTime > now) ||
                              (!ep.Event.Sessions.Any() && ep.Event.StartDate > now)))
                .CountAsync(cancellationToken);

            // TotalPastEventsRegistered: ALL registrations (any status) for past events
            var totalPastEventsRegistered = await _context.EventAttendances
                .AsNoTracking()
                .Where(ep => ep.UserId == userId
                          && ep.Event.EndDate < DateTime.UtcNow)
                .CountAsync(cancellationToken);

            // CancelledRegistrations: Count of Cancelled OR Refunded status
            var cancelledRegistrations = await _context.EventAttendances
                .AsNoTracking()
                .Where(ep => ep.UserId == userId
                          && (ep.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Cancelled
                           || ep.Status == WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Refunded))
                .CountAsync(cancellationToken);

            // NoShows: Calculated as TotalPastEventsRegistered - TotalEventsAttended
            var noShows = totalPastEventsRegistered - totalAttended;

            // Map vetting status to display string
            // Map vetting status int to display string
            // VettingStatus defaults to 0 (UnderReview) for all users, so we must check
            // HasVettingApplication to distinguish "hasn't applied" from "actually under review"
            // This matches the same logic used in MembersList.tsx getVettingStatusBadge()
            var vettingStatusDisplay = user.VettingStatus switch
            {
                0 => user.HasVettingApplication ? "Under Review" : "Not Applied",
                1 => "Interview Approved",
                2 => "Final Review",
                3 => "Approved",
                4 => "Denied",
                5 => "On Hold",
                6 => "Withdrawn",
                _ => "Not Started"
            };

            var response = new MemberDetailsResponse
            {
                UserId = user.Id,
                SceneName = user.SceneName,
                Email = user.Email,
                DiscordName = user.DiscordName,
                FetLifeHandle = user.FetLifeName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                OtherNames = user.OtherNames,
                Pronouns = user.Pronouns,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                TotalEventsAttended = totalAttended,
                LastEventAttended = lastEventAttended,
                FutureEvents = futureEvents,
                TotalPastEventsRegistered = totalPastEventsRegistered,
                CancelledRegistrations = cancelledRegistrations,
                NoShows = noShows,
                VettingStatus = user.VettingStatus,
                VettingStatusDisplay = vettingStatusDisplay,
                HasVettingApplication = user.HasVettingApplication
            };

            _logger.LogInformation("Retrieved member details for user {UserId}", userId);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get member details for user {UserId}", userId);
            return (false, null, "Failed to retrieve member details");
        }
    }

    /// <summary>
    /// Get vetting details including questionnaire responses
    /// Endpoint 2: GET /api/users/{id}/vetting-details
    /// </summary>
    public async Task<(bool Success, VettingDetailsResponse? Response, string Error)> GetVettingDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Exclude soft-deleted applications — deleted apps should not appear on member vetting tab
            var application = await _context.VettingApplications
                .AsNoTracking()
                .Where(va => !va.IsDeleted)
                .FirstOrDefaultAsync(va => va.UserId == userId, cancellationToken);

            // Load user to get current profile OtherNames
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (application == null)
            {
                // No application exists - return empty response
                var emptyResponse = new VettingDetailsResponse
                {
                    HasApplication = false
                };
                return (true, emptyResponse, string.Empty);
            }

            // Map workflow status to display string
            var workflowStatusDisplay = application.WorkflowStatus switch
            {
                WitchCityRope.Api.Features.Vetting.Entities.VettingStatus.UnderReview => "Under Review",
                WitchCityRope.Api.Features.Vetting.Entities.VettingStatus.InterviewApproved => "Interview Approved",
                WitchCityRope.Api.Features.Vetting.Entities.VettingStatus.FinalReview => "Final Review",
                WitchCityRope.Api.Features.Vetting.Entities.VettingStatus.Approved => "Approved",
                WitchCityRope.Api.Features.Vetting.Entities.VettingStatus.Denied => "Denied",
                WitchCityRope.Api.Features.Vetting.Entities.VettingStatus.OnHold => "On Hold",
                WitchCityRope.Api.Features.Vetting.Entities.VettingStatus.Withdrawn => "Withdrawn",
                _ => "Unknown"
            };

            var response = new VettingDetailsResponse
            {
                HasApplication = true,
                ApplicationId = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                SubmittedAt = application.SubmittedAt,
                WorkflowStatus = (int)application.WorkflowStatus,
                WorkflowStatusDisplay = workflowStatusDisplay,
                LastReviewedAt = application.LastReviewedAt,
                DecisionMadeAt = application.DecisionMadeAt,

                // Questionnaire responses
                SceneName = application.SceneName,
                FirstName = application.FirstName,
                LastName = application.LastName,
                Email = application.Email,
                Phone = null, // Field removed from entity
                FetLifeHandle = application.FetLifeHandle,
                OtherNames = user?.OtherNames,  // Use current profile value, not vetting application
                Pronouns = application.Pronouns,
                AboutYourself = null, // Field removed from entity
                ExperienceDescription = application.ExperienceDescription,
                SafetyKnowledge = null, // Field removed from entity
                ConsentUnderstanding = null, // Field removed from entity
                WhyJoinCommunity = application.WhyJoinCommunity,
                HowDidYouHearAboutUs = application.HowDidYouHearAboutUs,
                SkillsInterests = null, // Field removed from entity
                ExpectationsGoals = null, // Field removed from entity
                AgreesToGuidelines = application.AgreesToGuidelines,
                AgreesToTerms = application.AgreesToTerms
            };

            _logger.LogInformation("Retrieved vetting details for user {UserId}", userId);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vetting details for user {UserId}", userId);
            return (false, null, "Failed to retrieve vetting details");
        }
    }

    /// <summary>
    /// Get paginated event history for a member
    /// Endpoint 3: GET /api/users/{id}/event-history
    /// </summary>
    public async Task<(bool Success, EventHistoryResponse? Response, string Error)> GetEventHistoryAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all attendances with event details
            var query = _context.EventAttendances
                .AsNoTracking()
                .Include(ep => ep.Event)
                .Where(ep => ep.UserId == userId)
                .OrderByDescending(ep => ep.Event.StartDate);

            var totalCount = await query.CountAsync(cancellationToken);

            var participations = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var events = participations.Select(ep =>
            {
                var registrationType = ep.AttendanceType == WitchCityRope.Api.Features.Participation.Entities.AttendanceType.RSVP
                    ? "RSVP"
                    : "Ticket";

                var participationStatus = ep.Status switch
                {
                    WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Active => "Active",
                    WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Cancelled => "Cancelled",
                    WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Refunded => "Refunded",
                    WitchCityRope.Api.Features.Participation.Entities.AttendanceStatus.Waitlisted => "Waitlisted",
                    _ => "Unknown"
                };

                // Extract amount from metadata JSON if available
                decimal? amountPaid = null;
                if (!string.IsNullOrEmpty(ep.Metadata))
                {
                    try
                    {
                        var metadata = System.Text.Json.JsonDocument.Parse(ep.Metadata);
                        if (metadata.RootElement.TryGetProperty("amount", out var amountElement))
                        {
                            amountPaid = amountElement.GetDecimal();
                        }
                    }
                    catch
                    {
                        // Ignore JSON parsing errors
                    }
                }

                return new EventHistoryRecord
                {
                    EventId = ep.Event.Id,
                    EventTitle = ep.Event.Title,
                    AllowRsvps = ep.Event.AllowRsvps,
                    RequireTicketPurchase = ep.Event.RequireTicketPurchase,
                    VettedMembersOnly = ep.Event.VettedMembersOnly,
                    EventDate = ep.Event.StartDate,
                    RegistrationType = registrationType,
                    ParticipationStatus = participationStatus,
                    RegisteredAt = ep.CreatedAt,
                    CancelledAt = ep.CancelledAt,
                    AmountPaid = amountPaid
                };
            }).ToList();

            var response = new EventHistoryResponse
            {
                Events = events,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };

            _logger.LogInformation("Retrieved event history for user {UserId}: {TotalCount} events", userId, totalCount);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get event history for user {UserId}", userId);
            return (false, null, "Failed to retrieve event history");
        }
    }

    /// <summary>
    /// Get safety incidents involving a member
    /// Endpoint 4: GET /api/users/{id}/incidents
    /// Admin is authorized to see all encrypted fields
    /// </summary>
    public async Task<(bool Success, MemberIncidentsResponse? Response, string Error)> GetMemberIncidentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // OPTIMIZATION: Already uses AsNoTracking for read-only queries
            // Good pattern - loads all incidents in single query
            // Decryption loop is necessary for encrypted data
            var incidents = await _context.SafetyIncidents
                .AsNoTracking()
                .Where(si => si.ReporterId == userId || si.AssignedTo == userId || si.CoordinatorId == userId)
                .OrderByDescending(si => si.IncidentDate)
                .ToListAsync(cancellationToken);

            var incidentRecords = new List<MemberIncidentRecord>();

            foreach (var incident in incidents)
            {
                // Determine user's involvement type
                var involvementType = incident.ReporterId == userId ? "Reporter" :
                                     incident.CoordinatorId == userId ? "Coordinator" :
                                     incident.AssignedTo == userId ? "Assigned" :
                                     "Related";

                var statusDisplay = incident.Status switch
                {
                    IncidentStatus.ReportSubmitted => "Report Submitted",
                    IncidentStatus.InformationGathering => "Information Gathering",
                    IncidentStatus.ReviewingFinalReport => "Reviewing Final Report",
                    IncidentStatus.OnHold => "On Hold",
                    IncidentStatus.Closed => "Closed",
                    _ => "Unknown"
                };

                // Decrypt sensitive fields for admin view
                string? decryptedInvolvedParties = null;
                if (!string.IsNullOrEmpty(incident.EncryptedInvolvedParties))
                {
                    try
                    {
                        decryptedInvolvedParties = await _encryptionService.DecryptAsync(incident.EncryptedInvolvedParties);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to decrypt involved parties for incident {IncidentId}", incident.Id);
                        decryptedInvolvedParties = "[Decryption Failed]";
                    }
                }

                incidentRecords.Add(new MemberIncidentRecord
                {
                    IncidentId = incident.Id,
                    ReferenceNumber = incident.ReferenceNumber,
                    Title = incident.Title,
                    IncidentDate = incident.IncidentDate,
                    ReportedAt = incident.ReportedAt,
                    Status = statusDisplay,
                    Location = incident.Location,
                    Description = incident.EncryptedDescription, // Keep encrypted for now, decrypt on detail view
                    InvolvedParties = decryptedInvolvedParties, // Decrypted for admin
                    Witnesses = incident.EncryptedWitnesses, // Keep encrypted for now
                    UserInvolvementType = involvementType
                });
            }

            var response = new MemberIncidentsResponse
            {
                Incidents = incidentRecords,
                TotalCount = incidentRecords.Count
            };

            _logger.LogInformation("Retrieved {Count} incidents for user {UserId}", incidentRecords.Count, userId);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get incidents for user {UserId}", userId);
            return (false, null, "Failed to retrieve incidents");
        }
    }

    /// <summary>
    /// Converts vetting audit log actions to simplified, user-friendly descriptions
    /// EXACT COPY from VettingService.GetSimplifiedActionDescription()
    /// </summary>
    private static string GetSimplifiedActionDescription(string action, string? oldValue, string? newValue)
    {
        // Map status transitions to extremely simple past tense descriptions (2-4 words max)
        if (action.Contains("Status Changed") && !string.IsNullOrWhiteSpace(newValue))
        {
            return newValue switch
            {
                "InterviewApproved" => "Approved for interview",
                "FinalReview" => "Interview completed",
                "Approved" => "Application approved",
                "Denied" => "Application denied",
                "OnHold" => "Application placed on hold",
                "UnderReview" => "Returned to review",
                "Withdrawn" => "Application withdrawn",
                _ => $"Status changed to {newValue}"
            };
        }

        if (action.Contains("Approval"))
        {
            return "Application approved";
        }

        if (action.Contains("Denied"))
        {
            return "Application denied";
        }

        // Default fallback
        return action;
    }

    /// <summary>
    /// Build appropriate content for VettingAuditLog notes based on action type
    /// </summary>
    private static string GetNoteContent(string action, string? oldValue, string? newValue, string? notes)
    {
        // For note/comment actions, just show the content directly (no prefix, no "Reason:")
        if (action.Contains("Note Added") || action == "Note Added" ||
            action.Contains("Reviewer Comment") || action == "Reviewer Comment" ||
            action.Contains("Comment"))
        {
            return notes ?? string.Empty;
        }

        // For status change actions, show simplified description + optional reason
        if (action.Contains("Status Changed") || action.Contains("Status"))
        {
            var simplifiedDescription = GetSimplifiedActionDescription(action, oldValue, newValue);

            if (string.IsNullOrWhiteSpace(notes))
            {
                return simplifiedDescription;
            }

            return $"{simplifiedDescription}\n\nReason: {notes}";
        }

        // For other actions (Approval, Denied, etc.), use simplified description + optional reason
        var description = GetSimplifiedActionDescription(action, oldValue, newValue);

        if (string.IsNullOrWhiteSpace(notes))
        {
            return description;
        }

        return $"{description}\n\nReason: {notes}";
    }

    /// <summary>
    /// Get all notes for a member (unified notes system)
    /// Endpoint 5: GET /api/users/{id}/notes
    /// Returns ALL note types together including UserNotes AND VettingAuditLogs
    /// </summary>
    public async Task<(bool Success, List<MemberNoteHistoryResponse>? Response, string Error)> GetMemberNotesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching complete note history for user {UserId}", userId);

            // Fetch UserNotes (general notes)
            var userNotes = await _context.UserNotes
                .AsNoTracking()
                .Include(un => un.Author)
                .Where(un => un.UserId == userId && !un.IsArchived)
                .Select(un => new MemberNoteHistoryResponse
                {
                    Id = un.Id,
                    NoteSource = "UserNote",
                    Content = un.Content,
                    Type = un.NoteType,
                    AuthorSceneName = un.Author != null ? un.Author.SceneName : null,
                    Timestamp = un.CreatedAt,
                    OldValue = null,
                    NewValue = null
                })
                .ToListAsync(cancellationToken);

            // Fetch VettingAuditLogs for all user's non-deleted vetting applications
            // Load raw data first, then apply transformation in memory
            var vettingAuditLogsRaw = await _context.VettingAuditLogs
                .AsNoTracking()
                .Include(val => val.PerformedByUser)
                .Where(val => _context.VettingApplications
                    .Any(va => va.UserId == userId && !va.IsDeleted && va.Id == val.ApplicationId))
                .ToListAsync(cancellationToken);

            // Transform to response DTOs with simplified descriptions
            var vettingAuditLogs = vettingAuditLogsRaw
                .Select(val => new MemberNoteHistoryResponse
                {
                    Id = val.Id,
                    NoteSource = "VettingAuditLog",
                    Content = GetNoteContent(val.Action, val.OldValue, val.NewValue, val.Notes),
                    Type = val.Action,
                    AuthorSceneName = val.PerformedByUser.SceneName,
                    Timestamp = val.PerformedAt,
                    OldValue = val.OldValue,
                    NewValue = val.NewValue
                })
                .ToList();

            // Combine and sort by timestamp (newest first)
            var combinedNotes = userNotes
                .Concat(vettingAuditLogs)
                .OrderByDescending(n => n.Timestamp)
                .ToList();

            _logger.LogInformation(
                "Retrieved {UserNotesCount} user notes and {AuditLogCount} audit logs for user {UserId}",
                userNotes.Count, vettingAuditLogs.Count, userId);

            return (true, combinedNotes, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get complete note history for user {UserId}", userId);
            return (false, null, "Failed to retrieve note history");
        }
    }

    /// <summary>
    /// Create a new note for a member
    /// Endpoint 6: POST /api/users/{id}/notes
    /// </summary>
    public async Task<(bool Success, UserNoteResponse? Response, string Error)> CreateMemberNoteAsync(
        Guid userId,
        CreateUserNoteRequest request,
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                _logger.LogWarning("Cannot create note - user not found: {UserId}", userId);
                return (false, null, "User not found");
            }

            // Validate note type
            var validNoteTypes = new[] { "Vetting", "General", "Administrative", "StatusChange" };
            if (!validNoteTypes.Contains(request.NoteType))
            {
                _logger.LogWarning("Invalid note type: {NoteType}", request.NoteType);
                return (false, null, $"Invalid note type. Must be one of: {string.Join(", ", validNoteTypes)}");
            }

            // Validate content
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return (false, null, "Note content cannot be empty");
            }

            var note = new WitchCityRope.Api.Data.Entities.UserNote(
                userId: userId,
                content: request.Content.Trim(),
                noteType: request.NoteType,
                authorId: authorId
            );

            _context.UserNotes.Add(note);
            await _context.SaveChangesAsync(cancellationToken);

            // Load author for response
            var author = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == authorId, cancellationToken);

            var response = new UserNoteResponse
            {
                Id = note.Id,
                UserId = note.UserId,
                Content = note.Content,
                NoteType = note.NoteType,
                AuthorId = note.AuthorId,
                AuthorSceneName = author?.SceneName,
                CreatedAt = note.CreatedAt,
                IsArchived = note.IsArchived
            };

            _logger.LogInformation("Created {NoteType} note for user {UserId} by author {AuthorId}",
                request.NoteType, userId, authorId);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create note for user {UserId}", userId);
            return (false, null, "Failed to create note");
        }
    }

    /// <summary>
    /// Update member status (active/inactive) and auto-create status change note
    /// Endpoint 7: PUT /api/users/{id}/status
    /// </summary>
    public async Task<(bool Success, string Error)> UpdateMemberStatusAsync(
        Guid userId,
        UpdateMemberStatusRequest request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Cannot update status - user not found: {UserId}", userId);
                return (false, "User not found");
            }

            var oldStatus = user.IsActive;
            user.IsActive = request.IsActive;

            // Explicitly mark as modified (defensive persistence pattern)
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Auto-create status change note
            var statusChangeMessage = request.IsActive
                ? $"Member status changed to ACTIVE. {(string.IsNullOrEmpty(request.Reason) ? "" : $"Reason: {request.Reason}")}"
                : $"Member status changed to INACTIVE. {(string.IsNullOrEmpty(request.Reason) ? "" : $"Reason: {request.Reason}")}";

            var statusNote = new WitchCityRope.Api.Data.Entities.UserNote(
                userId: userId,
                content: statusChangeMessage.Trim(),
                noteType: "StatusChange",
                authorId: performedByUserId
            );

            _context.UserNotes.Add(statusNote);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated status for user {UserId} from {OldStatus} to {NewStatus} by {PerformedBy}",
                userId, oldStatus, request.IsActive, performedByUserId);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for user {UserId}", userId);
            return (false, "Failed to update member status");
        }
    }

    /// <summary>
    /// Update member role
    /// Endpoint 8: PUT /api/users/{id}/roles
    /// Note: Role is a single string value, not an array
    /// </summary>
    public async Task<(bool Success, string Error)> UpdateMemberRoleAsync(
        Guid userId,
        UpdateMemberRoleRequest request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Cannot update role - user not found: {UserId}", userId);
                return (false, "User not found");
            }

            // Validate role - empty string is allowed (means "no special role")
            // Valid assignable roles come from UserRoleConstants (single source of truth)
            if (!string.IsNullOrEmpty(request.Role) && !UserRoleConstants.ValidRoles.Contains(request.Role))
            {
                _logger.LogWarning("Invalid role: {Role}. Valid roles: {ValidRoles}", request.Role, string.Join(", ", UserRoleConstants.ValidRoles));
                return (false, $"Invalid role. Must be one of: {string.Join(", ", UserRoleConstants.ValidRoles)}, or empty to remove special role");
            }

            var oldRole = user.Role;
            user.Role = request.Role;

            // Explicitly mark as modified (defensive persistence pattern)
            _context.Users.Update(user);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated role for user {UserId} from {OldRole} to {NewRole} by {PerformedBy}",
                userId, oldRole, request.Role, performedByUserId);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update role for user {UserId}", userId);
            return (false, "Failed to update member role");
        }
    }

    /// <summary>
    /// Get profile change history for a member
    /// Endpoint 9: GET /api/users/{id}/profile-change-history
    /// </summary>
    public async Task<(bool Success, List<ProfileChangeHistoryDto>? Response, string Error)> GetProfileChangeHistoryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verify user exists
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
            if (!userExists)
            {
                _logger.LogWarning("Cannot get profile change history - user not found: {UserId}", userId);
                return (false, null, "User not found");
            }

            // Query UserNotes for ProfileChange entries
            var notes = await _context.UserNotes
                .AsNoTracking()
                .Where(n => n.UserId == userId && n.NoteType == "ProfileChange")
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            // Transform in memory (can't use helper methods in EF query)
            var profileChanges = notes.Select(n => new ProfileChangeHistoryDto
            {
                ChangedAt = n.CreatedAt,
                FieldName = ExtractFieldName(n.Content),
                OldValue = ExtractOldValue(n.Content),
                NewValue = ExtractNewValue(n.Content)
            }).ToList();

            _logger.LogInformation("Retrieved {Count} profile changes for user {UserId}", profileChanges.Count, userId);
            return (true, profileChanges, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profile change history for user {UserId}", userId);
            return (false, null, "Failed to retrieve profile change history");
        }
    }

    /// <summary>
    /// Update member contact information (admin only)
    /// Endpoint 10: PUT /api/users/{id}/contact-info
    /// Creates audit notes for all changed fields
    /// </summary>
    public async Task<(bool Success, string Error)> UpdateMemberContactInfoAsync(
        Guid userId,
        AdminUpdateContactInfoDto request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Admin {AdminId} updating contact info for user {UserId}", performedByUserId, userId);

            // Use a retry loop to handle optimistic concurrency conflicts
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                // Fetch fresh user data for each attempt to ensure we have latest ConcurrencyStamp
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("Cannot update contact info - user not found: {UserId}", userId);
                    return (false, "User not found");
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

                if (HasChanged(user.DiscordName, request.DiscordName))
                    changedFields.Add(("Discord Name", user.DiscordName, request.DiscordName));

                if (HasChanged(user.FetLifeName, request.FetLifeName))
                    changedFields.Add(("FetLife Name", user.FetLifeName, request.FetLifeName));

                if (HasChanged(user.PhoneNumber, request.PhoneNumber))
                    changedFields.Add(("Phone Number", user.PhoneNumber, request.PhoneNumber));

                if (HasChanged(user.OtherNames, request.OtherNames))
                    changedFields.Add(("Other Names", user.OtherNames, request.OtherNames));

                // Apply field updates to the user entity
                user.SceneName = request.SceneName;
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Email = request.Email;
                user.UserName = request.Email; // Keep UserName in sync with Email
                user.Pronouns = request.Pronouns ?? string.Empty;
                user.DiscordName = request.DiscordName;
                user.FetLifeName = request.FetLifeName;
                user.PhoneNumber = request.PhoneNumber;
                user.OtherNames = request.OtherNames;
                user.UpdatedAt = DateTime.UtcNow;

                // UserManager.UpdateAsync handles optimistic concurrency automatically via ConcurrencyStamp
                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    // Create audit note for changed fields
                    if (changedFields.Count > 0)
                    {
                        // Fetch admin's scene name for the audit note
                        var adminUser = await _context.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Id == performedByUserId, cancellationToken);
                        var adminSceneName = adminUser?.SceneName ?? "Unknown Admin";

                        // Build a single audit note summarizing all changes
                        var changeDescriptions = changedFields.Select(cf =>
                            $"{cf.FieldName} from \"{cf.OldValue ?? "(empty)"}\" to \"{cf.NewValue ?? "(empty)"}\"");
                        var noteContent = $"[Admin Edit by {adminSceneName}] Changed: {string.Join(", ", changeDescriptions)}";

                        var auditNote = new WitchCityRope.Api.Data.Entities.UserNote(
                            userId: userId,
                            content: noteContent,
                            noteType: "Administrative",
                            authorId: performedByUserId
                        );

                        _context.UserNotes.Add(auditNote);
                        await _context.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation(
                            "Admin {AdminId} updated {ChangeCount} fields for user {UserId}. Changes: {Changes}",
                            performedByUserId, changedFields.Count, userId,
                            string.Join(", ", changedFields.Select(cf => cf.FieldName)));
                    }
                    else
                    {
                        _logger.LogInformation("No changes detected for user {UserId} contact info update", userId);
                    }

                    return (true, string.Empty);
                }

                // Check if the failure is due to concurrency conflict
                var concurrencyError = updateResult.Errors.FirstOrDefault(e =>
                    e.Code == "ConcurrencyFailure" || e.Description.Contains("concurrency", StringComparison.OrdinalIgnoreCase));

                if (concurrencyError != null && attempt < maxRetries - 1)
                {
                    // Concurrency conflict detected - retry with fresh data
                    _logger.LogWarning(
                        "Concurrency conflict updating contact info for user {UserId} (attempt {Attempt}/{MaxRetries}). " +
                        "Original stamp: {OriginalStamp}. Retrying...",
                        userId, attempt + 1, maxRetries, originalStamp);

                    // Small delay before retry to reduce contention
                    await Task.Delay(50 * (attempt + 1), cancellationToken);
                    continue;
                }

                // Non-concurrency error or final retry exhausted
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                _logger.LogError(
                    "Failed to update contact info for user {UserId} after {Attempt} attempts. Errors: {Errors}",
                    userId, attempt + 1, errors);
                return (false, $"Failed to update contact information: {errors}");
            }

            // Should never reach here, but just in case
            return (false, "Failed to update contact information after multiple attempts");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update contact info for user {UserId}", userId);
            return (false, "Failed to update contact information");
        }
    }

    /// <summary>
    /// Admin-initiated password reset for a member.
    /// Generates a reset token and immediately uses it to set the new password,
    /// bypassing the need for the member's current password or email verification.
    /// Creates an audit note documenting that the admin reset the password.
    /// Endpoint 11: POST /api/users/{id}/reset-password
    /// </summary>
    public async Task<(bool Success, string Error)> AdminResetPasswordAsync(
        Guid userId,
        AdminResetPasswordRequest request,
        Guid performedByUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("Admin password reset failed - user not found: {UserId}", userId);
                return (false, "User not found");
            }

            // Generate a reset token and immediately use it to set the new password.
            // This approach uses ASP.NET Identity's built-in token validation pipeline,
            // ensuring password policy enforcement and proper security token updates.
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Admin password reset failed for user {UserId}: {Errors}", userId, errors);
                return (false, errors);
            }

            // Create an audit note documenting the password reset
            var auditNote = new WitchCityRope.Api.Data.Entities.UserNote(
                userId: userId,
                content: "Password was reset by an administrator.",
                noteType: "Administrative",
                authorId: performedByUserId
            );

            _context.UserNotes.Add(auditNote);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Admin {AdminId} reset password for user {UserId}",
                performedByUserId,
                userId);

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during admin password reset for user {UserId}", userId);
            return (false, "An unexpected error occurred while resetting the password");
        }
    }

    /// <summary>
    /// Extract field name from profile change content
    /// Format: "Field Name changed from \"old\" to \"new\""
    /// </summary>
    private string ExtractFieldName(string content)
    {
        var changedIndex = content.IndexOf(" changed from", StringComparison.Ordinal);
        return changedIndex > 0 ? content.Substring(0, changedIndex) : content;
    }

    /// <summary>
    /// Extract old value from profile change content
    /// Format: "Field Name changed from \"old\" to \"new\""
    /// </summary>
    private string? ExtractOldValue(string content)
    {
        var fromIndex = content.IndexOf("from \"", StringComparison.Ordinal);
        if (fromIndex < 0) return null;

        var startIndex = fromIndex + 6; // Length of "from \""
        var endIndex = content.IndexOf("\" to \"", startIndex, StringComparison.Ordinal);
        if (endIndex < 0) return null;

        var value = content.Substring(startIndex, endIndex - startIndex);
        return value == "(empty)" ? null : value;
    }

    /// <summary>
    /// Extract new value from profile change content
    /// Format: "Field Name changed from \"old\" to \"new\""
    /// </summary>
    private string? ExtractNewValue(string content)
    {
        var toIndex = content.IndexOf("\" to \"", StringComparison.Ordinal);
        if (toIndex < 0) return null;

        var startIndex = toIndex + 6; // Length of "\" to \""
        var endIndex = content.LastIndexOf("\"", StringComparison.Ordinal);
        if (endIndex <= startIndex) return null;

        var value = content.Substring(startIndex, endIndex - startIndex);
        return value == "(empty)" ? null : value;
    }
}
