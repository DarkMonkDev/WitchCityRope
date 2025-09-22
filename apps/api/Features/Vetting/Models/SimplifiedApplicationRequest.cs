using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Simplified vetting application request matching the React form implementation
/// Used for the streamlined single-page application process
/// </summary>
public class SimplifiedApplicationRequest
{
    /// <summary>
    /// Applicant's real/legal name
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string RealName { get; set; } = string.Empty;

    /// <summary>
    /// Preferred scene name for community use
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string PreferredSceneName { get; set; } = string.Empty;

    /// <summary>
    /// Optional FetLife handle (without @ symbol)
    /// </summary>
    [StringLength(50)]
    public string? FetLifeHandle { get; set; }

    /// <summary>
    /// Email address - will be pre-filled from authenticated user
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Why would you like to join Witch City Rope (required text field)
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 20)]
    public string WhyJoin { get; set; } = string.Empty;

    /// <summary>
    /// Description of rope experience (minimum 50 characters)
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 50)]
    public string ExperienceWithRope { get; set; } = string.Empty;

    /// <summary>
    /// Agreement to community standards - must be true
    /// </summary>
    [Required]
    public bool AgreeToCommunityStandards { get; set; }

    /// <summary>
    /// Optional field for how they found out about the community
    /// </summary>
    [StringLength(500)]
    public string? HowFoundUs { get; set; }

    /// <summary>
    /// Optional pronouns field (e.g., "they/them", "she/her", "he/him")
    /// </summary>
    [StringLength(50)]
    public string? Pronouns { get; set; }

    /// <summary>
    /// Any other names, nicknames, or social media handles you have used in a kinky context
    /// </summary>
    [StringLength(500)]
    public string? OtherNames { get; set; }
}