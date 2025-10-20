namespace WitchCityRope.Api.Features.TestHelpers.Models;

/// <summary>
/// Request model for creating test users programmatically
/// ONLY available in Development/Test environments
/// </summary>
public class CreateTestUserRequest
{
    /// <summary>
    /// User's email address (must be unique)
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's password (will be hashed using ASP.NET Core Identity hasher)
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// User's scene name (display name in the community)
    /// </summary>
    public required string SceneName { get; set; }

    /// <summary>
    /// User's first name (optional)
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name (optional)
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User's role (Guest, Member, VettedMember, Teacher, Admin)
    /// Default: Member
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Date of birth (required for Age verification)
    /// Format: YYYY-MM-DD
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// User's vetting status (0-6 enum value)
    /// 0 = UnderReview, 3 = Approved (vetted)
    /// Default: 0 (UnderReview)
    /// </summary>
    public int VettingStatus { get; set; } = 0;

    /// <summary>
    /// User's bio (optional)
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// User's pronouns (optional)
    /// </summary>
    public string? Pronouns { get; set; }
}
