namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Request model for creating test vetting applications programmatically
/// ONLY available in Development/Test environments
/// </summary>
public class CreateTestVettingApplicationRequest
{
    /// <summary>
    /// User ID for the applicant
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Vetting workflow status
    /// UnderReview = 0, InterviewApproved = 1, FinalReview = 2, Approved = 3, Denied = 4, OnHold = 5, Withdrawn = 6
    /// Default: UnderReview
    /// </summary>
    public int WorkflowStatus { get; set; } = 0;

    /// <summary>
    /// Experience description (optional)
    /// </summary>
    public string? ExperienceDescription { get; set; }

    /// <summary>
    /// Why applicant wants to join (optional)
    /// </summary>
    public string? WhyJoinCommunity { get; set; }

    /// <summary>
    /// How applicant heard about the community (optional)
    /// </summary>
    public string? HowDidYouHearAboutUs { get; set; }
}
