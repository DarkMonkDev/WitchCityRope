using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.EmailTemplates.Models;

/// <summary>
/// DTO for user preview in segment
/// Provides minimal user information for email recipient preview
/// </summary>
public class UserPreviewDto
{
    /// <summary>
    /// User's scene name
    /// </summary>
    public string SceneName { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's vetting status.
    ///
    /// Phase 3b-1: Type changed from int to VettingStatus enum. Ships as
    /// a JSON string via JsonStringEnumConverter.
    /// </summary>
    public VettingStatus VettingStatus { get; set; }

    /// <summary>
    /// User's vetting status as human-readable string
    /// </summary>
    public string VettingStatusDisplay { get; set; } = string.Empty;

    /// <summary>
    /// User's role(s)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user's email is confirmed
    /// </summary>
    public bool EmailConfirmed { get; set; }
}
