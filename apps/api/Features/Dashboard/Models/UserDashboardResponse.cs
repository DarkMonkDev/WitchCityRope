namespace WitchCityRope.Api.Features.Dashboard.Models;

/// <summary>
/// Response model for user dashboard data
/// Contains basic user profile information and vetting status
/// </summary>
public class UserDashboardResponse
{
    /// <summary>
    /// User's scene name for the rope bondage community
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// User's role in the system (Member, VettedMember, Teacher, Organizer, Administrator)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Current vetting status (0 = Submitted, 1 = InReview, 2 = Approved, 3 = Rejected)
    /// </summary>
    public int VettingStatus { get; set; }

    /// <summary>
    /// Whether the user is currently vetted
    /// </summary>
    public bool IsVetted { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// When the user joined the community
    /// </summary>
    public DateTime JoinDate { get; set; }

    /// <summary>
    /// User's pronouns
    /// </summary>
    public string Pronouns { get; set; } = string.Empty;
}