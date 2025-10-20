using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Safety.Entities;
using WitchCityRope.Api.Features.Safety.Services;
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

            // Get participation counts
            var participations = await _context.EventParticipations
                .AsNoTracking()
                .Where(ep => ep.UserId == userId)
                .ToListAsync(cancellationToken);

            var activeRegistrations = participations
                .Count(p => p.Status == WitchCityRope.Api.Features.Participation.Entities.ParticipationStatus.Active);

            var totalRegistered = participations.Count;

            // Get past events (attended events are those in the past with active participation)
            var pastEvents = await _context.EventParticipations
                .AsNoTracking()
                .Include(ep => ep.Event)
                .Where(ep => ep.UserId == userId
                          && ep.Status == WitchCityRope.Api.Features.Participation.Entities.ParticipationStatus.Active
                          && ep.Event.EndDate < DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            var totalAttended = pastEvents.Count;
            var lastEventAttended = pastEvents
                .OrderByDescending(ep => ep.Event.EndDate)
                .FirstOrDefault()?.Event.EndDate;

            // Map vetting status to display string
            var vettingStatusDisplay = user.VettingStatus switch
            {
                0 => "Under Review",
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
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                TotalEventsAttended = totalAttended,
                TotalEventsRegistered = totalRegistered,
                ActiveRegistrations = activeRegistrations,
                LastEventAttended = lastEventAttended,
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
            var application = await _context.VettingApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(va => va.UserId == userId, cancellationToken);

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
                RealName = application.RealName,
                Email = application.Email,
                Phone = application.Phone,
                FetLifeHandle = application.FetLifeHandle,
                Pronouns = application.Pronouns,
                AboutYourself = application.AboutYourself,
                ExperienceLevel = application.ExperienceLevel,
                YearsExperience = application.YearsExperience,
                ExperienceDescription = application.ExperienceDescription,
                SafetyKnowledge = application.SafetyKnowledge,
                ConsentUnderstanding = application.ConsentUnderstanding,
                WhyJoinCommunity = application.WhyJoinCommunity,
                SkillsInterests = application.SkillsInterests,
                ExpectationsGoals = application.ExpectationsGoals,
                AgreesToGuidelines = application.AgreesToGuidelines,
                AgreesToTerms = application.AgreesToTerms,

                // Admin notes from vetting application
                AdminNotes = application.AdminNotes
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
            // Get all participations with event details
            var query = _context.EventParticipations
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
                var registrationType = ep.ParticipationType == WitchCityRope.Api.Features.Participation.Entities.ParticipationType.RSVP
                    ? "RSVP"
                    : "Ticket";

                var participationStatus = ep.Status switch
                {
                    WitchCityRope.Api.Features.Participation.Entities.ParticipationStatus.Active => "Active",
                    WitchCityRope.Api.Features.Participation.Entities.ParticipationStatus.Cancelled => "Cancelled",
                    WitchCityRope.Api.Features.Participation.Entities.ParticipationStatus.Refunded => "Refunded",
                    WitchCityRope.Api.Features.Participation.Entities.ParticipationStatus.Waitlisted => "Waitlisted",
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
                    EventType = ep.Event.EventType,
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
            // Get all incidents where user is reporter, subject, or mentioned in encrypted fields
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
    /// Get all notes for a member (unified notes system)
    /// Endpoint 5: GET /api/users/{id}/notes
    /// Returns ALL note types together
    /// </summary>
    public async Task<(bool Success, List<UserNoteResponse>? Response, string Error)> GetMemberNotesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var notes = await _context.UserNotes
                .AsNoTracking()
                .Include(un => un.Author)
                .Where(un => un.UserId == userId && !un.IsArchived)
                .OrderByDescending(un => un.CreatedAt)
                .ToListAsync(cancellationToken);

            var response = notes.Select(note => new UserNoteResponse
            {
                Id = note.Id,
                UserId = note.UserId,
                Content = note.Content,
                NoteType = note.NoteType,
                AuthorId = note.AuthorId,
                AuthorSceneName = note.Author?.SceneName,
                CreatedAt = note.CreatedAt,
                IsArchived = note.IsArchived
            }).ToList();

            _logger.LogInformation("Retrieved {Count} notes for user {UserId}", response.Count, userId);
            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notes for user {UserId}", userId);
            return (false, null, "Failed to retrieve notes");
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

            // Validate role
            var validRoles = new[] { "Admin", "Teacher", "VettedMember", "Member", "Guest", "SafetyTeam" };
            if (!validRoles.Contains(request.Role))
            {
                _logger.LogWarning("Invalid role: {Role}", request.Role);
                return (false, $"Invalid role. Must be one of: {string.Join(", ", validRoles)}");
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
}
