using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Simple audit log for tracking changes to vetting applications
/// Replaces the complex audit system with a single, straightforward audit log
/// </summary>
public class VettingAuditLog
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid PerformedBy { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public VettingApplication Application { get; set; } = null!;
    public ApplicationUser PerformedByUser { get; set; } = null!;
}