using System;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Core.Entities
{
    /// <summary>
    /// Represents a payment transaction in the system
    /// Business Rules:
    /// - Payments support sliding scale pricing
    /// - Refunds are allowed within the cancellation window
    /// - Payment details are encrypted for security
    /// </summary>
    public class Payment
    {
        // Private constructor for EF Core
        private Payment() { }

        // Constructor for Ticket (new approach)
        public Payment(
            Ticket ticket,
            Money amount,
            string paymentMethod,
            string transactionId)
        {
            if (ticket == null)
                throw new ArgumentNullException(nameof(ticket));

            if (amount == null)
                throw new ArgumentNullException(nameof(amount));

            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required", nameof(paymentMethod));

            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Transaction ID is required", nameof(transactionId));

            Id = Guid.NewGuid();
            TicketId = ticket.Id;
            Ticket = ticket;
            Amount = amount;
            PaymentMethod = paymentMethod;
            TransactionId = transactionId;
            Status = PaymentStatus.Pending;
            ProcessedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        // Constructor for Registration (legacy support during migration)
        [Obsolete("Use the constructor with Ticket parameter instead. Registration is being phased out.")]
        public Payment(
            Registration registration,
            Money amount,
            string paymentMethod,
            string transactionId)
        {
            if (registration == null)
                throw new ArgumentNullException(nameof(registration));

            if (amount == null)
                throw new ArgumentNullException(nameof(amount));

            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new ArgumentException("Payment method is required", nameof(paymentMethod));

            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Transaction ID is required", nameof(transactionId));

            Id = Guid.NewGuid();
            RegistrationId = registration.Id;
            Registration = registration;
            Amount = amount;
            PaymentMethod = paymentMethod;
            TransactionId = transactionId;
            Status = PaymentStatus.Pending;
            ProcessedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        
        // Ticket relationship (new approach)
        public Guid? TicketId { get; private set; }
        public Ticket? Ticket { get; private set; }
        
        // Registration relationship (legacy support during migration)
        [Obsolete("Use TicketId instead. Registration is being phased out.")]
        public Guid? RegistrationId { get; private set; }
        
        [Obsolete("Use Ticket instead. Registration is being phased out.")]
        public Registration? Registration { get; private set; }
        
        /// <summary>
        /// The amount paid (from sliding scale pricing)
        /// </summary>
        public Money Amount { get; private set; }
        
        public PaymentStatus Status { get; private set; }
        
        public string PaymentMethod { get; private set; }
        
        /// <summary>
        /// External payment processor transaction ID
        /// </summary>
        public string TransactionId { get; private set; }
        
        public DateTime ProcessedAt { get; private set; }
        
        public DateTime UpdatedAt { get; private set; }
        
        /// <summary>
        /// Refund details if applicable
        /// </summary>
        public Money RefundAmount { get; private set; }
        
        public DateTime? RefundedAt { get; private set; }
        
        public string RefundTransactionId { get; private set; }
        
        public string RefundReason { get; private set; }

        /// <summary>
        /// Marks the payment as completed
        /// </summary>
        public void MarkAsCompleted()
        {
            if (Status != PaymentStatus.Pending)
                throw new DomainException("Only pending payments can be marked as completed");

            Status = PaymentStatus.Completed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Marks the payment as failed
        /// </summary>
        public void MarkAsFailed(string? failureReason = null)
        {
            if (Status != PaymentStatus.Pending)
                throw new DomainException("Only pending payments can be marked as failed");

            Status = PaymentStatus.Failed;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Initiates a refund for the payment
        /// </summary>
        public void InitiateRefund(Money? refundAmount = null, string? reason = null)
        {
            if (Status != PaymentStatus.Completed)
                throw new DomainException("Only completed payments can be refunded");

            if (RefundedAt.HasValue)
                throw new DomainException("Payment has already been refunded");

            RefundAmount = refundAmount ?? Amount;
            
            if (RefundAmount.Amount > Amount.Amount)
                throw new DomainException("Refund amount cannot exceed original payment amount");

            Status = PaymentStatus.Refunded;
            RefundedAt = DateTime.UtcNow;
            RefundReason = reason ?? "Registration cancelled within refund window";
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Completes the refund process with transaction details
        /// </summary>
        public void CompleteRefund(string refundTransactionId)
        {
            if (Status != PaymentStatus.Refunded)
                throw new DomainException("Payment must be in refunded status");

            if (string.IsNullOrWhiteSpace(refundTransactionId))
                throw new ArgumentException("Refund transaction ID is required", nameof(refundTransactionId));

            RefundTransactionId = refundTransactionId;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if the payment is eligible for refund
        /// </summary>
        public bool IsEligibleForRefund()
        {
            return Status == PaymentStatus.Completed && !RefundedAt.HasValue;
        }

        /// <summary>
        /// Gets the net amount after any refunds
        /// </summary>
        public Money GetNetAmount()
        {
            if (RefundAmount == null)
                return Amount;

            return Money.Create(Amount.Amount - RefundAmount.Amount, Amount.Currency);
        }
    }
}