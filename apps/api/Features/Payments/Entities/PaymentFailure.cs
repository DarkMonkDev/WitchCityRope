namespace WitchCityRope.Api.Features.Payments.Entities;

/// <summary>
/// Detailed failure tracking for payment processing issues
/// Supports troubleshooting and retry mechanisms
/// </summary>
public class PaymentFailure
{
    /// <summary>
    /// Failure record unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the failed payment
    /// </summary>
    public Guid PaymentId { get; set; }

    #region Failure Details

    /// <summary>
    /// Error code from payment processor or system
    /// </summary>
    public string FailureCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable failure message
    /// </summary>
    public string FailureMessage { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted detailed error response from Stripe (for PCI compliance)
    /// </summary>
    public string? EncryptedStripeErrorDetails { get; set; }

    #endregion

    #region Retry Tracking

    /// <summary>
    /// Number of times this payment has been retried
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// When this failure occurred
    /// </summary>
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this failure record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Navigation property to the failed payment
    /// </summary>
    public Payment? Payment { get; set; }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Check if this failure allows retries
    /// </summary>
    public bool CanRetry()
    {
        // Common retryable failure codes
        var retryableCodes = new[]
        {
            "processing_error",
            "temporary_failure",
            "rate_limit",
            "network_error",
            "timeout"
        };

        return retryableCodes.Contains(FailureCode.ToLower()) && RetryCount < 3;
    }

    /// <summary>
    /// Check if this is a permanent failure
    /// </summary>
    public bool IsPermanentFailure()
    {
        var permanentCodes = new[]
        {
            "card_declined",
            "insufficient_funds",
            "expired_card",
            "invalid_cvc",
            "invalid_number",
            "incorrect_number"
        };

        return permanentCodes.Contains(FailureCode.ToLower());
    }

    /// <summary>
    /// Increment retry count
    /// </summary>
    public void IncrementRetryCount()
    {
        RetryCount++;
        FailedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get user-friendly error message
    /// </summary>
    public string GetUserFriendlyMessage()
    {
        return FailureCode.ToLower() switch
        {
            "card_declined" => "Your card was declined. Please try a different payment method.",
            "insufficient_funds" => "Your card was declined due to insufficient funds.",
            "expired_card" => "Your card has expired. Please use a different payment method.",
            "incorrect_cvc" => "Your card's security code is incorrect. Please check and try again.",
            "invalid_number" => "The card number is invalid. Please check and try again.",
            "processing_error" => "We're having trouble processing your payment. Please try again.",
            "rate_limit" => "Too many requests. Please wait a moment and try again.",
            "network_error" => "Network error occurred. Please check your connection and try again.",
            "timeout" => "Payment request timed out. Please try again.",
            _ => "There was an issue processing your payment. Please try again or contact support."
        };
    }

    /// <summary>
    /// Get suggested actions for the user
    /// </summary>
    public string[] GetSuggestedActions()
    {
        return FailureCode.ToLower() switch
        {
            "card_declined" or "insufficient_funds" => new[]
            {
                "Try a different payment method",
                "Contact your bank to authorize the payment",
                "Check your account balance"
            },
            "expired_card" => new[]
            {
                "Use a different payment method",
                "Contact your bank for a replacement card"
            },
            "incorrect_cvc" or "invalid_number" => new[]
            {
                "Double-check your card information",
                "Ensure you've entered the correct card number and security code"
            },
            "processing_error" or "network_error" => new[]
            {
                "Wait a moment and try again",
                "Check your internet connection",
                "Try refreshing the page"
            },
            "rate_limit" => new[]
            {
                "Wait 5 minutes before trying again",
                "Avoid multiple rapid payment attempts"
            },
            _ => new[]
            {
                "Try again in a few minutes",
                "Contact support if the problem persists"
            }
        };
    }

    #endregion

    #region Static Factory Methods

    /// <summary>
    /// Create failure record for Stripe errors
    /// </summary>
    public static PaymentFailure FromStripeError(Guid paymentId, string stripeErrorCode, string stripeErrorMessage, string encryptedErrorDetails)
    {
        return new PaymentFailure
        {
            PaymentId = paymentId,
            FailureCode = stripeErrorCode,
            FailureMessage = stripeErrorMessage,
            EncryptedStripeErrorDetails = encryptedErrorDetails
        };
    }

    /// <summary>
    /// Create failure record for system errors
    /// </summary>
    public static PaymentFailure FromSystemError(Guid paymentId, string errorCode, string errorMessage)
    {
        return new PaymentFailure
        {
            PaymentId = paymentId,
            FailureCode = errorCode,
            FailureMessage = errorMessage
        };
    }

    /// <summary>
    /// Create failure record for validation errors
    /// </summary>
    public static PaymentFailure FromValidationError(Guid paymentId, string validationMessage)
    {
        return new PaymentFailure
        {
            PaymentId = paymentId,
            FailureCode = "validation_failed",
            FailureMessage = validationMessage
        };
    }

    #endregion
}