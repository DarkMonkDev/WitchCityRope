namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Stores reference form responses with assessment data
/// All sensitive data encrypted for privacy protection
/// </summary>
public class VettingReferenceResponse
{
    public VettingReferenceResponse()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Reference Link
    public Guid ReferenceId { get; set; }

    // Assessment Questions (Encrypted)
    public string EncryptedRelationshipDuration { get; set; } = string.Empty;
    public string EncryptedExperienceAssessment { get; set; } = string.Empty;
    public string? EncryptedSafetyConcerns { get; set; }
    public string EncryptedCommunityReadiness { get; set; } = string.Empty;
    public RecommendationLevel Recommendation { get; set; }
    public string? EncryptedAdditionalComments { get; set; }

    // Response Metadata
    public string ResponseIpAddress { get; set; } = string.Empty;
    public string? ResponseUserAgent { get; set; }
    public bool IsCompleted { get; set; }

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public VettingReference Reference { get; set; } = null!;
}

/// <summary>
/// Reference recommendation levels
/// </summary>
public enum RecommendationLevel
{
    DoNotSupport = 1,
    Neutral = 2,
    Support = 3,
    StronglySupport = 4
}