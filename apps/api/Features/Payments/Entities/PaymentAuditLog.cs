using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Features.Payments.Entities;

/// <summary>
/// Comprehensive audit trail for all payment operations
/// Supports compliance requirements and troubleshooting
/// </summary>
public class PaymentAuditLog
{
    /// <summary>
    /// Audit log unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to the payment being audited
    /// </summary>
    public Guid PaymentId { get; set; }
    
    /// <summary>
    /// User who performed the action (null for system actions)
    /// </summary>
    public Guid? UserId { get; set; }
    
    #region Action Details
    
    /// <summary>
    /// Type of action performed
    /// </summary>
    public string ActionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the action
    /// </summary>
    public string ActionDescription { get; set; } = string.Empty;
    
    #endregion
    
    #region Change Tracking
    
    /// <summary>
    /// Old values before the change (stored as JSONB)
    /// </summary>
    public Dictionary<string, object>? OldValues { get; set; }
    
    /// <summary>
    /// New values after the change (stored as JSONB)
    /// </summary>
    public Dictionary<string, object>? NewValues { get; set; }
    
    #endregion
    
    #region Security Tracking
    
    /// <summary>
    /// IP address of the user who performed the action
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// User agent of the client that performed the action
    /// </summary>
    public string? UserAgent { get; set; }
    
    #endregion
    
    #region Timing
    
    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    #endregion
    
    #region Navigation Properties
    
    /// <summary>
    /// Navigation property to the payment being audited
    /// </summary>
    public Payment? Payment { get; set; }
    
    /// <summary>
    /// Navigation property to the user who performed the action
    /// </summary>
    public ApplicationUser? User { get; set; }
    
    #endregion
    
    #region Static Factory Methods
    
    /// <summary>
    /// Create audit log for payment initiation
    /// </summary>
    public static PaymentAuditLog PaymentInitiated(Guid paymentId, Guid userId, string ipAddress, string userAgent)
    {
        return new PaymentAuditLog
        {
            PaymentId = paymentId,
            UserId = userId,
            ActionType = "PaymentInitiated",
            ActionDescription = "Payment processing initiated",
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
    
    /// <summary>
    /// Create audit log for payment completion
    /// </summary>
    public static PaymentAuditLog PaymentCompleted(Guid paymentId, string stripePaymentIntentId, decimal amount)
    {
        return new PaymentAuditLog
        {
            PaymentId = paymentId,
            UserId = null, // System action
            ActionType = "PaymentCompleted",
            ActionDescription = $"Payment successfully processed for ${amount:F2}",
            NewValues = new Dictionary<string, object>
            {
                ["status"] = "Completed",
                ["amount"] = amount,
                ["stripe_payment_intent_id"] = "[ENCRYPTED]",
                ["processed_at"] = DateTime.UtcNow
            }
        };
    }
    
    /// <summary>
    /// Create audit log for payment failure
    /// </summary>
    public static PaymentAuditLog PaymentFailed(Guid paymentId, string failureReason, string errorCode)
    {
        return new PaymentAuditLog
        {
            PaymentId = paymentId,
            UserId = null, // System action
            ActionType = "PaymentFailed",
            ActionDescription = $"Payment processing failed: {failureReason}",
            NewValues = new Dictionary<string, object>
            {
                ["status"] = "Failed",
                ["failure_reason"] = failureReason,
                ["error_code"] = errorCode,
                ["failed_at"] = DateTime.UtcNow
            }
        };
    }
    
    /// <summary>
    /// Create audit log for refund initiation
    /// </summary>
    public static PaymentAuditLog RefundInitiated(Guid paymentId, Guid processedByUserId, decimal refundAmount, string reason, string ipAddress)
    {
        return new PaymentAuditLog
        {
            PaymentId = paymentId,
            UserId = processedByUserId,
            ActionType = "RefundInitiated",
            ActionDescription = $"Refund of ${refundAmount:F2} initiated - {reason}",
            NewValues = new Dictionary<string, object>
            {
                ["refund_amount"] = refundAmount,
                ["refund_reason"] = reason,
                ["refund_initiated_at"] = DateTime.UtcNow
            },
            IpAddress = ipAddress
        };
    }
    
    /// <summary>
    /// Create audit log for refund completion
    /// </summary>
    public static PaymentAuditLog RefundCompleted(Guid paymentId, decimal refundAmount, string stripeRefundId)
    {
        return new PaymentAuditLog
        {
            PaymentId = paymentId,
            UserId = null, // System action
            ActionType = "RefundCompleted",
            ActionDescription = $"Refund of ${refundAmount:F2} successfully processed",
            NewValues = new Dictionary<string, object>
            {
                ["refund_status"] = "Completed",
                ["refund_amount"] = refundAmount,
                ["stripe_refund_id"] = "[ENCRYPTED]",
                ["refunded_at"] = DateTime.UtcNow
            }
        };
    }
    
    /// <summary>
    /// Create audit log for status changes
    /// </summary>
    public static PaymentAuditLog StatusChanged(Guid paymentId, string oldStatus, string newStatus, Guid? userId = null)
    {
        return new PaymentAuditLog
        {
            PaymentId = paymentId,
            UserId = userId,
            ActionType = "StatusChanged",
            ActionDescription = $"Payment status changed from {oldStatus} to {newStatus}",
            OldValues = new Dictionary<string, object> { ["status"] = oldStatus },
            NewValues = new Dictionary<string, object> { ["status"] = newStatus }
        };
    }
    
    /// <summary>
    /// Create audit log for metadata updates
    /// </summary>
    public static PaymentAuditLog MetadataUpdated(Guid paymentId, Dictionary<string, object> oldMetadata, Dictionary<string, object> newMetadata, Guid userId)
    {
        return new PaymentAuditLog
        {
            PaymentId = paymentId,
            UserId = userId,
            ActionType = "MetadataUpdated",
            ActionDescription = "Payment metadata updated",
            OldValues = oldMetadata,
            NewValues = newMetadata
        };
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Check if this is a user-initiated action
    /// </summary>
    public bool IsUserAction()
    {
        return UserId.HasValue;
    }
    
    /// <summary>
    /// Check if this is a system-initiated action
    /// </summary>
    public bool IsSystemAction()
    {
        return !UserId.HasValue;
    }
    
    /// <summary>
    /// Get a summary of changes made
    /// </summary>
    public string GetChangesSummary()
    {
        if (OldValues == null && NewValues == null)
            return "No changes recorded";
            
        var changes = new List<string>();
        
        if (NewValues != null)
        {
            foreach (var kvp in NewValues)
            {
                var oldValue = OldValues?.GetValueOrDefault(kvp.Key, "[Not Set]");
                changes.Add($"{kvp.Key}: {oldValue} → {kvp.Value}");
            }
        }
        
        return string.Join(", ", changes);
    }
    
    #endregion
}