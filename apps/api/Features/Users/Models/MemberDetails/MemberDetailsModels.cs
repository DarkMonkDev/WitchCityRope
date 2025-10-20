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
    public string Role { get; set; } = "Member";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Participation summary
    public int TotalEventsAttended { get; set; }
    public int TotalEventsRegistered { get; set; }
    public int ActiveRegistrations { get; set; }
    public DateTime? LastEventAttended { get; set; }

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
    public string? RealName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FetLifeHandle { get; set; }
    public string? Pronouns { get; set; }
    public string? AboutYourself { get; set; }
    public int? ExperienceLevel { get; set; }
    public int? YearsExperience { get; set; }
    public string? ExperienceDescription { get; set; }
    public string? SafetyKnowledge { get; set; }
    public string? ConsentUnderstanding { get; set; }
    public string? WhyJoinCommunity { get; set; }
    public string? SkillsInterests { get; set; }
    public string? ExpectationsGoals { get; set; }
    public bool? AgreesToGuidelines { get; set; }
    public bool? AgreesToTerms { get; set; }

    // Admin notes (from VettingApplication.AdminNotes field)
    public string? AdminNotes { get; set; }
}

/// <summary>
/// Event history record for a user
/// </summary>
public class EventHistoryRecord
{
    public Guid EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
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
    public string Role { get; set; } = "Member"; // "Admin", "Teacher", "VettedMember", "Member", "Guest", "SafetyTeam"
}
