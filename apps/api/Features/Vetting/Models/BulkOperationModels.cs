using WitchCityRope.Api.Features.Vetting.Entities;

namespace WitchCityRope.Api.Features.Vetting.Models;

/// <summary>
/// Request to send bulk reminder emails
/// </summary>
public class BulkReminderRequest
{
    public List<Guid> ApplicationIds { get; set; } = new();
    public string? CustomMessage { get; set; }
}

/// <summary>
/// Request to change status for multiple applications
/// </summary>
public class BulkStatusChangeRequest
{
    public List<Guid> ApplicationIds { get; set; } = new();
    public VettingStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Result of bulk operation
/// </summary>
public class BulkOperationResult
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

