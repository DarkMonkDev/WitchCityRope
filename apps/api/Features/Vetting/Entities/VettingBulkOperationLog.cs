using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Detailed logging for bulk operation processing
/// Tracks each step and action within bulk operations for audit trail
/// </summary>
public class VettingBulkOperationLog
{
    public VettingBulkOperationLog()
    {
        LogLevel = "Info";
        CreatedAt = DateTime.UtcNow;
    }

    // Primary Key
    public Guid Id { get; set; }

    // References
    public Guid BulkOperationId { get; set; }
    public Guid? ApplicationId { get; set; }

    // Log Information
    public string LogLevel { get; set; } = "Info"; // Info, Warning, Error
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }

    // Context Information
    public string? OperationStep { get; set; }
    public string? ErrorCode { get; set; }
    public string? StackTrace { get; set; }

    // Navigation Properties
    public VettingBulkOperation BulkOperation { get; set; } = null!;
    public VettingApplication? Application { get; set; }
}