using System;
using System.Threading.Tasks;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Core.Interfaces
{
    /// <summary>
    /// Service for processing payments and refunds
    /// Supports sliding scale pricing model
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Processes a payment for an event registration
        /// </summary>
        /// <param name="registration">The registration to process payment for</param>
        /// <param name="amount">The amount to charge (from sliding scale)</param>
        /// <param name="paymentMethodId">Payment method identifier from payment processor</param>
        /// <returns>Payment result with transaction details</returns>
        Task<PaymentResult> ProcessPaymentAsync(Registration registration, Money amount, string paymentMethodId);

        /// <summary>
        /// Processes a refund for a payment
        /// </summary>
        /// <param name="payment">The payment to refund</param>
        /// <param name="refundAmount">Amount to refund (null for full refund)</param>
        /// <param name="reason">Reason for the refund</param>
        /// <returns>Refund result with transaction details</returns>
        Task<RefundResult> ProcessRefundAsync(Payment payment, Money? refundAmount = null, string? reason = null);

        /// <summary>
        /// Validates a payment method before processing
        /// </summary>
        /// <param name="paymentMethodId">Payment method identifier</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> ValidatePaymentMethodAsync(string paymentMethodId);

        /// <summary>
        /// Creates a payment intent for client-side processing
        /// </summary>
        /// <param name="amount">The amount for the payment intent</param>
        /// <param name="metadata">Additional metadata for the payment</param>
        /// <returns>Payment intent details</returns>
        Task<PaymentIntent> CreatePaymentIntentAsync(Money amount, PaymentMetadata metadata);

        /// <summary>
        /// Retrieves the status of a payment transaction
        /// </summary>
        /// <param name="transactionId">The transaction identifier</param>
        /// <returns>Current payment status</returns>
        Task<PaymentStatus> GetPaymentStatusAsync(string transactionId);
    }

    /// <summary>
    /// Result of a payment processing operation
    /// </summary>
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Result of a refund processing operation
    /// </summary>
    public class RefundResult
    {
        public bool Success { get; set; }
        public string RefundTransactionId { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public Money RefundedAmount { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    /// <summary>
    /// Payment intent for client-side processing
    /// </summary>
    public class PaymentIntent
    {
        public string ClientSecret { get; set; } = string.Empty;
        public string IntentId { get; set; } = string.Empty;
        public Money Amount { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Metadata associated with a payment
    /// </summary>
    public class PaymentMetadata
    {
        public Guid RegistrationId { get; set; }
        public Guid UserId { get; set; }
        public Guid EventId { get; set; }
        public string? Description { get; set; }
    }
}