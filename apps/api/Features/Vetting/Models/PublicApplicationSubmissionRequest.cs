using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Simplified public application submission request for E2E testing
/// Minimal required fields for creating vetting applications via public API
/// </summary>
public class PublicApplicationSubmissionRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Valid email address is required")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Scene name is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Scene name must be between 3 and 50 characters")]
    public string SceneName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Real name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Real name must be between 2 and 100 characters")]
    public string RealName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required")]
    [Phone(ErrorMessage = "Valid phone number is required")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Emergency contact name is required")]
    public string EmergencyContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Emergency contact phone is required")]
    [Phone(ErrorMessage = "Valid emergency contact phone is required")]
    public string EmergencyContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Experience level is required")]
    public string Experience { get; set; } = string.Empty;

    [Required(ErrorMessage = "Interests are required")]
    [StringLength(2000, ErrorMessage = "Interests cannot exceed 2000 characters")]
    public string Interests { get; set; } = string.Empty;

    [Required(ErrorMessage = "References are required")]
    public string References { get; set; } = string.Empty;

    [Required(ErrorMessage = "Must agree to community rules")]
    public bool AgreeToRules { get; set; }

    [Required(ErrorMessage = "Must consent to background check")]
    public bool ConsentToBackground { get; set; }

    // Optional fields
    public string? Pronouns { get; set; }
    public string? AdditionalInfo { get; set; }
}
