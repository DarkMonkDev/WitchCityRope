using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Tracks bulk administrative operations for vetting applications
/// Provides audit trail and progress monitoring for bulk operations
/// </summary>
public class VettingBulkOperation
{
    public VettingBulkOperation()
    {
        Status = BulkOperationStatus.Running;
        SuccessCount = 0;
        FailureCount = 0;
        PerformedAt = DateTime.UtcNow;
        StartedAt = DateTime.UtcNow;
        Parameters = "{}";

        Items = new List<VettingBulkOperationItem>();
        Logs = new List<VettingBulkOperationLog>();
    }

    // Primary Key
    public Guid Id { get; set; }

    // Operation Information
    public BulkOperationType OperationType { get; set; }
    public BulkOperationStatus Status { get; set; } = BulkOperationStatus.Running;
    public Guid PerformedBy { get; set; }

    // Timing
    public DateTime PerformedAt { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Parameters and Configuration
    public string Parameters { get; set; } = "{}"; // JSON configuration

    // Progress Tracking
    public int TotalItems { get; set; }
    public int SuccessCount { get; set; } = 0;
    public int FailureCount { get; set; } = 0;
    public string? ErrorSummary { get; set; }

    // Navigation Properties
    public ApplicationUser PerformedByUser { get; set; } = null!;
    public ICollection<VettingBulkOperationItem> Items { get; set; }
    public ICollection<VettingBulkOperationLog> Logs { get; set; }
}

/// <summary>
/// Types of bulk operations supported
/// </summary>
public enum BulkOperationType
{
    SendReminderEmails = 1,
    ChangeStatusToOnHold = 2,
    AssignReviewer = 3,
    ExportApplications = 4
}

/// <summary>
/// Status of bulk operations
/// </summary>
public enum BulkOperationStatus
{
    Running = 1,
    Completed = 2,
    Failed = 3
}