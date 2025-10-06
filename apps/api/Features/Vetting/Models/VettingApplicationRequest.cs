using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Vetting application request matching simplified functional specification
/// </summary>
public class VettingApplicationRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string RealName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string SceneName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? FetLifeHandle { get; set; }

    [StringLength(50)]
    public string? Pronouns { get; set; }

    [StringLength(500)]
    public string? OtherNames { get; set; }

    [Required]
    [StringLength(2000, MinimumLength = 50)]
    public string AboutYourself { get; set; } = string.Empty;

    [Required]
    public bool AgreeToCommunityStandards { get; set; }
}