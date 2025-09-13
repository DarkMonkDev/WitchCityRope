namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Response after successful application submission
/// Contains status tracking information for applicant
/// </summary>
public class ApplicationSubmissionResponse
{
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string StatusToken { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string ConfirmationMessage { get; set; } = string.Empty;
    
    // Expected timeline information
    public int EstimatedReviewDays { get; set; }
    public string NextSteps { get; set; } = string.Empty;
    
    // Reference process status
    public List<ReferenceStatusSummary> ReferenceStatuses { get; set; } = new();
}

/// <summary>
/// Summary of reference contact status
/// </summary>
public class ReferenceStatusSummary
{
    public string Name { get; set; } = string.Empty; // May be masked for privacy
    public string Email { get; set; } = string.Empty; // May be masked for privacy
    public string Status { get; set; } = string.Empty; // "NotContacted", "Contacted", "Responded", etc.
    public DateTime? ContactedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}