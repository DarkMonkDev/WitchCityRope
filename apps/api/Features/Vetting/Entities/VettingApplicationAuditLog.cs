using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Comprehensive audit trail for all application changes
/// Tracks all modifications for compliance and debugging
/// </summary>
public class VettingApplicationAuditLog
{
    public VettingApplicationAuditLog()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Application Reference
    public Guid ApplicationId { get; set; }

    // Audit Details
    public string ActionType { get; set; } = string.Empty; // "StatusChange", "AssignmentChange", etc.
    public string ActionDescription { get; set; } = string.Empty;
    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON

    // User Context
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public VettingApplication Application { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}