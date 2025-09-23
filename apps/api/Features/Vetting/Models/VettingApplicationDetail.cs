namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Detailed view of vetting application for admin review
/// </summary>
public class VettingApplicationDetail
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public DateTime LastUpdated { get; set; }

    // Decrypted applicant information
    public string RealName { get; set; } = string.Empty;
    public string SceneName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FetLifeHandle { get; set; }
    public string? Pronouns { get; set; }
    public string? OtherNames { get; set; }
    public string AboutYourself { get; set; } = string.Empty;
    public string HowFoundUs { get; set; } = string.Empty;
    public bool AgreeToCommunityStandards { get; set; }

    // Admin information
    public string? AdminNotes { get; set; }
    public List<VettingAuditEntry> AuditLog { get; set; } = new();
}

/// <summary>
/// Audit log entry for application history
/// </summary>
public class VettingAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Notes { get; set; }
}