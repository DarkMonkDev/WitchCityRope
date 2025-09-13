namespace WitchCityRope.Api.Features.Payments.Models;

/// <summary>
/// Refund processing status enumeration
/// </summary>
public enum RefundStatus
{
    /// <summary>
    /// Refund is being processed
    /// </summary>
    Processing = 0,
    
    /// <summary>
    /// Refund has been completed successfully
    /// </summary>
    Completed = 1,
    
    /// <summary>
    /// Refund processing failed
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Refund was cancelled
    /// </summary>
    Cancelled = 3
}