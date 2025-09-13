using System.ComponentModel.DataAnnotations;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Request model for vetting application submission
/// Contains all form data for 5-step application process
/// </summary>
public class CreateApplicationRequest
{
    // Personal Information (Step 1)
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string SceneName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Pronouns { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    // Experience & Knowledge (Step 2)
    [Required]
    public int ExperienceLevel { get; set; } // Enum: Beginner=1, Intermediate=2, Advanced=3, Expert=4

    [Required]
    [Range(0, 50)]
    public int YearsExperience { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 50)]
    public string ExperienceDescription { get; set; } = string.Empty;

    [Required]
    [StringLength(300, MinimumLength = 30)]
    public string SafetyKnowledge { get; set; } = string.Empty;

    [Required]
    [StringLength(300, MinimumLength = 30)]
    public string ConsentUnderstanding { get; set; } = string.Empty;

    // Community Understanding (Step 3)
    [Required]
    [StringLength(400, MinimumLength = 50)]
    public string WhyJoinCommunity { get; set; } = string.Empty;

    [Required]
    public List<string> SkillsInterests { get; set; } = new();

    [Required]
    [StringLength(300, MinimumLength = 30)]
    public string ExpectationsGoals { get; set; } = string.Empty;

    [Required]
    public bool AgreesToGuidelines { get; set; }

    // References (Step 4)
    [Required]
    public List<ReferenceRequest> References { get; set; } = new();

    // Review & Submit (Step 5)
    [Required]
    public bool AgreesToTerms { get; set; }

    [Required]
    public bool IsAnonymous { get; set; }

    [Required]
    public bool ConsentToContact { get; set; }
}

/// <summary>
/// Reference information for application
/// </summary>
public class ReferenceRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Relationship { get; set; } = string.Empty;

    public int Order { get; set; } // 1, 2, or 3
}