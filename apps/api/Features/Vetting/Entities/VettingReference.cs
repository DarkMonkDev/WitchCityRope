using System.Security.Cryptography;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Manages reference collection and verification process
/// All PII fields are encrypted for privacy protection
/// </summary>
public class VettingReference
{
    public VettingReference()
    {
        Id = Guid.NewGuid();
        Status = ReferenceStatus.NotContacted;
        ResponseToken = GenerateSecureToken();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        
        AuditLogs = new List<VettingReferenceAuditLog>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }
    public int ReferenceOrder { get; set; } // 1, 2, 3 for display order

    // Reference Information (Encrypted)
    public string EncryptedName { get; set; } = string.Empty;
    public string EncryptedEmail { get; set; } = string.Empty;
    public string EncryptedRelationship { get; set; } = string.Empty;

    // Reference Process
    public ReferenceStatus Status { get; set; }
    public string ResponseToken { get; set; } = string.Empty; // Secure token for reference form
    public DateTime? ContactedAt { get; set; }
    public DateTime? FirstReminderSentAt { get; set; }
    public DateTime? SecondReminderSentAt { get; set; }
    public DateTime? FinalReminderSentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? FormExpiresAt { get; set; }

    // Manual Follow-up Tracking
    public bool RequiresManualContact { get; set; }
    public string? ManualContactNotes { get; set; }
    public DateTime? ManualContactAttemptAt { get; set; }

    // Standard Audit Fields
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
    public VettingReferenceResponse? Response { get; set; }
    public ICollection<VettingReferenceAuditLog> AuditLogs { get; set; }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[48]; // 384-bit token for references
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

/// <summary>
/// Reference contact and response status
/// </summary>
public enum ReferenceStatus
{
    NotContacted = 1,
    Contacted = 2,
    ReminderSent = 3,
    Responded = 4,
    Expired = 5,
    ManualFollowupRequired = 6
}