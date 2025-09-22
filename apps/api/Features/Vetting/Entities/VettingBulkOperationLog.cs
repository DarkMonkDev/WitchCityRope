namespace WitchCityRope.Api.Features.Vetting.Entities;

/// <summary>
/// Detailed logging for bulk operation debugging and monitoring
/// Provides comprehensive audit trail for bulk operation execution
/// </summary>
public class VettingBulkOperationLog
{
    public VettingBulkOperationLog()
    {
        CreatedAt = DateTime.UtcNow;
        Context = "{}";
    }

    // Primary Key
    public Guid Id { get; set; }

    // References
    public Guid BulkOperationId { get; set; }
    public Guid? ApplicationId { get; set; } // Optional reference to specific application

    // Log Information
    public string LogLevel { get; set; } = string.Empty; // Debug, Info, Warning, Error, Critical
    public string Message { get; set; } = string.Empty;
    public string Context { get; set; } = "{}"; // JSON context information
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public VettingBulkOperation BulkOperation { get; set; } = null!;
    public VettingApplication? Application { get; set; }
}