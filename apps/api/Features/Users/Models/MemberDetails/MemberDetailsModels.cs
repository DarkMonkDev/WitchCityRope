namespace WitchCityRope.Api.Features.Users.Models.MemberDetails;

/// <summary>
/// Comprehensive member details for admin view
/// </summary>
public class MemberDetailsResponse
{
    public Guid UserId { get; set; }
    public string SceneName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DiscordName { get; set; }
    public string? FetLifeHandle { get; set; }
    public string Role { get; set; } = ""; // Empty string = no special role
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Participation summary
    public int TotalEventsAttended { get; set; }
    public DateTime? LastEventAttended { get; set; }
    public int FutureEvents { get; set; }
    public int TotalPastEventsRegistered { get; set; }
    public int CancelledRegistrations { get; set; }
    public int NoShows { get; set; }

    // Vetting status
    public int VettingStatus { get; set; }
    public string VettingStatusDisplay { get; set; } = "Not Started";
    public bool HasVettingApplication { get; set; }
}

/// <summary>
/// Vetting details including questionnaire responses
/// </summary>
public class VettingDetailsResponse
{
    public bool HasApplication { get; set; }
    public Guid? ApplicationId { get; set; }
    public string? ApplicationNumber { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? WorkflowStatus { get; set; }
    public string? WorkflowStatusDisplay { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public DateTime? DecisionMadeAt { get; set; }

    // Questionnaire responses
    public string? SceneName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FetLifeHandle { get; set; }
    public string? OtherNames { get; set; }
    public string? Pronouns { get; set; }
    public string? AboutYourself { get; set; }
    public string? ExperienceDescription { get; set; }
    public string? SafetyKnowledge { get; set; }
    public string? ConsentUnderstanding { get; set; }
    public string? WhyJoinCommunity { get; set; }
    public string? HowDidYouHearAboutUs { get; set; }
    public string? SkillsInterests { get; set; }
    public string? ExpectationsGoals { get; set; }
    public bool? AgreesToGuidelines { get; set; }
    public bool? AgreesToTerms { get; set; }
}

/// <summary>
/// Event history record for a user
/// </summary>
public class EventHistoryRecord
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Whether free RSVPs are enabled for this event
    /// </summary>
    public bool AllowRsvps { get; set; }

    /// <summary>
    /// Whether ticket purchase is mandatory to attend
    /// </summary>
    public bool RequireTicketPurchase { get; set; }

    /// <summary>
    /// Whether only vetted members can attend
    /// </summary>
    public bool VettedMembersOnly { get; set; }

    public DateTime EventDate { get; set; }
    public string RegistrationType { get; set; } = string.Empty; // "RSVP", "Ticket", "Attended"
    public string? ParticipationStatus { get; set; } // "Active", "Cancelled", etc.
    public DateTime? RegisteredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal? AmountPaid { get; set; }
}

/// <summary>
/// Paginated event history response
/// </summary>
public class EventHistoryResponse
{
    public List<EventHistoryRecord> Events { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Safety incident involving a user
/// </summary>
public class MemberIncidentRecord
{
    public Guid IncidentId { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime IncidentDate { get; set; }
    public DateTime ReportedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; // Decrypted for admin
    public string? InvolvedParties { get; set; } // Decrypted for admin
    public string? Witnesses { get; set; } // Decrypted for admin
    public string UserInvolvementType { get; set; } = string.Empty; // "Reporter", "Subject", "Witness"
}

/// <summary>
/// Member incidents response
/// </summary>
public class MemberIncidentsResponse
{
    public List<MemberIncidentRecord> Incidents { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// User note response
/// </summary>
public class UserNoteResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string NoteType { get; set; } = "General"; // "Vetting", "General", "Administrative", "StatusChange"
    public Guid? AuthorId { get; set; }
    public string? AuthorSceneName { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsArchived { get; set; }
}

/// <summary>
/// Unified note history response combining UserNotes and VettingAuditLogs for complete audit trail
/// Provides chronological view of both general member notes AND vetting workflow history
/// </summary>
public class MemberNoteHistoryResponse
{
    /// <summary>
    /// Note ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Source of the note: "UserNote" or "VettingAuditLog"
    /// </summary>
    public string NoteSource { get; set; } = string.Empty;

    /// <summary>
    /// Content of the note
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// For UserNotes: note type (Vetting, General, etc.)
    /// For VettingAuditLogs: action (Status Changed, Reviewer Comment, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Author scene name (null for system-generated actions)
    /// </summary>
    public string? AuthorSceneName { get; set; }

    /// <summary>
    /// Timestamp when note/action occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// For VettingAuditLogs: old value (status changes)
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// For VettingAuditLogs: new value (status changes)
    /// </summary>
    public string? NewValue { get; set; }
}

/// <summary>
/// Request to create a new user note
/// </summary>
public class CreateUserNoteRequest
{
    public string Content { get; set; } = string.Empty;
    public string NoteType { get; set; } = "General"; // "Vetting", "General", "Administrative", "StatusChange"
}

/// <summary>
/// Request to update member status (active/inactive)
/// </summary>
public class UpdateMemberStatusRequest
{
    public bool IsActive { get; set; }
    public string? Reason { get; set; } // Optional reason for status change
}

/// <summary>
/// Request to update member role
/// </summary>
public class UpdateMemberRoleRequest
{
    public string Role { get; set; } = ""; // Valid roles: "Administrator", "Teacher", "SafetyTeam", "EventOrganizer", or "" (no special role)
}

/// <summary>
/// Profile change history record for a user
/// </summary>
public class ProfileChangeHistoryDto
{
    public DateTime ChangedAt { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
