using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Audit trail for decision changes and approvals
/// Maintains complete history of all decision modifications
/// </summary>
public class VettingDecisionAuditLog
{
    public VettingDecisionAuditLog()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // Decision Reference
    public Guid DecisionId { get; set; }
    public Guid ApplicationId { get; set; }

    // Audit Details
    public string ActionType { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public string? PreviousDecision { get; set; }
    public string? NewDecision { get; set; }

    // User Context
    public Guid? UserId { get; set; }

    // Timestamp
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public VettingDecision Decision { get; set; } = null!;
    public ApplicationUser? User { get; set; }
}