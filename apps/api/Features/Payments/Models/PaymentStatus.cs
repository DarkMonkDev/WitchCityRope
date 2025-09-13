namespace WitchCityRope.Api.Features.Payments.Models;

/// <summary>
/// Payment processing status enumeration
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment has been initiated but not yet processed
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Payment has been successfully completed
    /// </summary>
    Completed = 1,
    
    /// <summary>
    /// Payment processing failed
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Payment has been fully refunded
    /// </summary>
    Refunded = 3,
    
    /// <summary>
    /// Payment has been partially refunded
    /// </summary>
    PartiallyRefunded = 4
}