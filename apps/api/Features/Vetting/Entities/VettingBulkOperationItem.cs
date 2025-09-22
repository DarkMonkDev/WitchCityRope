namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Individual application processing within bulk operations
/// Tracks success/failure for each application in a bulk operation
/// </summary>
public class VettingBulkOperationItem
{
    public VettingBulkOperationItem()
    {
        Success = false;
        AttemptCount = 1;
    }

    // Primary Key
    public Guid Id { get; set; }

    // References
    public Guid BulkOperationId { get; set; }
    public Guid ApplicationId { get; set; }

    // Processing Results
    public bool Success { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }

    // Retry Logic
    public int AttemptCount { get; set; } = 1;
    public DateTime? RetryAt { get; set; }

    // Navigation Properties
    public VettingBulkOperation BulkOperation { get; set; } = null!;
    public VettingApplication Application { get; set; } = null!;
}