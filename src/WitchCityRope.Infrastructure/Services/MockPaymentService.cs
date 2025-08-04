using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;

namespace WitchCityRope.Infrastructure.Services
{
    /// <summary>
    /// Mock payment service for development/testing when Stripe/PayPal is not configured
    /// </summary>
    public class MockPaymentService : IPaymentService
    {
        private readonly ILogger<MockPaymentService> _logger;

        public MockPaymentService(ILogger<MockPaymentService> logger)
        {
            _logger = logger;
        }

        public Task<PaymentResult> ProcessPaymentAsync(Registration registration, Money amount, string paymentMethodId)
    {
        return ProcessPaymentForRegistration(registration, amount, paymentMethodId);
    }

    public Task<PaymentResult> ProcessPaymentAsync(Ticket ticket, Money amount, string paymentMethodId)
    {
        return ProcessPaymentForTicket(ticket, amount, paymentMethodId);
    }

    private Task<PaymentResult> ProcessPaymentForRegistration(Registration registration, Money amount, string paymentMethodId)
    {
        _logger.LogInformation($"Mock payment processed for registration:");
        _logger.LogInformation($"  Registration ID: {registration.Id}");
        _logger.LogInformation($"  Amount: {amount.Currency} {amount.Amount}");
        _logger.LogInformation($"  Payment Method: {paymentMethodId}");
        
        var result = new PaymentResult
        {
            Success = true,
            TransactionId = $"MOCK_{Guid.NewGuid():N}",
            ProcessedAt = DateTime.UtcNow,
            Status = PaymentStatus.Completed
        };
        
        return Task.FromResult(result);
    }

    private Task<PaymentResult> ProcessPaymentForTicket(Ticket ticket, Money amount, string paymentMethodId)
        {
            _logger.LogInformation($"Mock payment processed:");
            _logger.LogInformation($"  Ticket ID: {ticket.Id}");
            _logger.LogInformation($"  Amount: {amount.Currency} {amount.Amount}");
            _logger.LogInformation($"  Payment Method: {paymentMethodId}");
            
            var result = new PaymentResult
            {
                Success = true,
                TransactionId = $"MOCK_{Guid.NewGuid():N}",
                ProcessedAt = DateTime.UtcNow,
                Status = PaymentStatus.Completed
            };
            
            return Task.FromResult(result);
        }

        public Task<RefundResult> ProcessRefundAsync(Payment payment, Money? refundAmount = null, string? reason = null)
        {
            var amount = refundAmount ?? payment.Amount;
            
            _logger.LogInformation($"Mock refund processed:");
            _logger.LogInformation($"  Payment ID: {payment.Id}");
            _logger.LogInformation($"  Transaction ID: {payment.TransactionId}");
            _logger.LogInformation($"  Refund Amount: {amount.Currency} {amount.Amount}");
            if (!string.IsNullOrEmpty(reason))
            {
                _logger.LogInformation($"  Reason: {reason}");
            }
            
            var result = new RefundResult
            {
                Success = true,
                RefundTransactionId = $"REFUND_{Guid.NewGuid():N}",
                ProcessedAt = DateTime.UtcNow,
                RefundedAmount = amount
            };
            
            return Task.FromResult(result);
        }

        public Task<bool> ValidatePaymentMethodAsync(string paymentMethodId)
        {
            _logger.LogInformation($"Mock payment method validation: {paymentMethodId}");
            // Always return true in mock mode
            return Task.FromResult(true);
        }

        public Task<PaymentIntent> CreatePaymentIntentAsync(Money amount, PaymentMetadata metadata)
        {
            _logger.LogInformation($"Mock payment intent created:");
            _logger.LogInformation($"  Amount: {amount.Currency} {amount.Amount}");
            _logger.LogInformation($"  Registration ID: {metadata.RegistrationId}");
            _logger.LogInformation($"  User ID: {metadata.UserId}");
            _logger.LogInformation($"  Event ID: {metadata.EventId}");
            
            var intent = new PaymentIntent
            {
                ClientSecret = $"pi_mock_{Guid.NewGuid():N}_secret_{Guid.NewGuid():N}",
                IntentId = $"pi_mock_{Guid.NewGuid():N}",
                Amount = amount,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };
            
            return Task.FromResult(intent);
        }

        public Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
        {
            _logger.LogInformation($"Mock payment status check: {transactionId}");
            // Always return completed for mock payments
            return Task.FromResult(PaymentStatus.Completed);
        }
    }
}