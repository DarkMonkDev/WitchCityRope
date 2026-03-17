namespace WitchCityRope.Api.Features.Reports.Models;

/// <summary>
/// Response DTO for the user summary endpoint.
/// Returns current counts of users by verification and vetting status.
/// </summary>
public class UserSummaryResponse
{
    public int TotalUsers { get; set; }
    public int VerifiedUsers { get; set; }
    public int UnverifiedUsers { get; set; }
    public int VettedMembers { get; set; }
    public int VettingApplicationsSubmitted { get; set; }
}
