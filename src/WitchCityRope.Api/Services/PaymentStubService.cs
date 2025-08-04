using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Stub implementation of payment service for testing purposes
/// Always returns successful payments without actually processing anything
/// </summary>
public class PaymentStubService : IPaymentService
{
    private readonly ILogger<PaymentStubService> _logger;
    private readonly Dictionary<string, PaymentStatus> _transactionStatuses = new();

    public PaymentStubService(ILogger<PaymentStubService> logger)
    {
        _logger = logger;
    }

    public Task<PaymentResult> ProcessPaymentAsync(Registration registration, Money amount, string paymentMethodId)
    {
        _logger.LogInformation("[STUB] Processing payment for registration {RegistrationId} for {Amount} {Currency}", 
            registration.Id, amount.Amount, amount.Currency);
        
        // Generate a fake transaction ID
        var transactionId = $"STUB_{Guid.NewGuid():N}";
        
        // Store the transaction status
        _transactionStatuses[transactionId] = PaymentStatus.Completed;
        
        // Simulate payment result
        var result = new PaymentResult
        {
            Success = true,
            TransactionId = transactionId,
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };
        
        _logger.LogInformation("[STUB] Payment successful with transaction ID: {TransactionId}", transactionId);
        return Task.FromResult(result);
    }

    public Task<PaymentResult> ProcessPaymentAsync(Ticket ticket, Money amount, string paymentMethodId)
    {
        _logger.LogInformation("[STUB] Processing payment for ticket {TicketId} for {Amount} {Currency}", 
            ticket.Id, amount.Amount, amount.Currency);
        
        // Generate a fake transaction ID
        var transactionId = $"STUB_{Guid.NewGuid():N}";
        
        // Store the transaction status
        _transactionStatuses[transactionId] = PaymentStatus.Completed;
        
        // Simulate payment result
        var result = new PaymentResult
        {
            Success = true,
            TransactionId = transactionId,
            Status = PaymentStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };
        
        _logger.LogInformation("[STUB] Payment successful with transaction ID: {TransactionId}", transactionId);
        return Task.FromResult(result);
    }

    public Task<RefundResult> ProcessRefundAsync(Payment payment, Money? refundAmount = null, string? reason = null)
    {
        _logger.LogInformation("[STUB] Processing refund for payment {PaymentId}", payment.Id);
        
        var actualRefundAmount = refundAmount ?? payment.Amount;
        var refundTransactionId = $"REFUND_{Guid.NewGuid():N}";
        
        var result = new RefundResult
        {
            Success = true,
            RefundTransactionId = refundTransactionId,
            RefundedAmount = actualRefundAmount,
            ProcessedAt = DateTime.UtcNow
        };
        
        _logger.LogInformation("[STUB] Refund successful with refund ID: {RefundId}", refundTransactionId);
        return Task.FromResult(result);
    }

    public Task<bool> ValidatePaymentMethodAsync(string paymentMethodId)
    {
        _logger.LogInformation("[STUB] Validating payment method {PaymentMethodId}", paymentMethodId);
        // Always return true for stub
        return Task.FromResult(true);
    }

    public Task<PaymentIntent> CreatePaymentIntentAsync(Money amount, PaymentMetadata metadata)
    {
        _logger.LogInformation("[STUB] Creating payment intent for {Amount} {Currency}", amount.Amount, amount.Currency);
        
        var intent = new PaymentIntent
        {
            ClientSecret = $"pi_{Guid.NewGuid():N}_secret_{Guid.NewGuid():N}",
            IntentId = $"pi_{Guid.NewGuid():N}",
            Amount = amount,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        };
        
        return Task.FromResult(intent);
    }

    public Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
    {
        _logger.LogInformation("[STUB] Getting payment status for transaction {TransactionId}", transactionId);
        
        if (_transactionStatuses.TryGetValue(transactionId, out var status))
        {
            return Task.FromResult(status);
        }
        
        return Task.FromResult(PaymentStatus.Failed);
    }
}