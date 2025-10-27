using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Simplified vetting application request matching the React form implementation
/// Used for the streamlined single-page application process
/// </summary>
public class SimplifiedApplicationRequest
{
    /// <summary>
    /// Applicant's first name
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Applicant's last name
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

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
    [StringLength(2000)]
    public string WhyJoin { get; set; } = string.Empty;

    /// <summary>
    /// Description of rope experience
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string ExperienceWithRope { get; set; } = string.Empty;

    /// <summary>
    /// Agreement to community standards - must be true
    /// </summary>
    [Required]
    public bool AgreeToCommunityStandards { get; set; }

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