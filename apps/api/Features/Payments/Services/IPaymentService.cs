using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Shared.Models;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Payment processing service interface supporting sliding scale pricing and comprehensive audit trails
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Process payment for event registration with sliding scale pricing
    /// </summary>
    Task<Result<Payment>> ProcessPaymentAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment by ID with full details
    /// </summary>
    Task<Result<Payment?>> GetPaymentByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payments for a specific user
    /// </summary>
    Task<Result<List<Payment>>> GetPaymentsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment for event registration
    /// </summary>
    Task<Result<Payment?>> GetPaymentByRegistrationIdAsync(
        Guid eventRegistrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment status for event registration
    /// </summary>
    Task<Result<PaymentStatus?>> GetPaymentStatusByRegistrationIdAsync(
        Guid eventRegistrationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update payment status (typically called by webhooks)
    /// </summary>
    Task<Result<Payment>> UpdatePaymentStatusAsync(
        Guid paymentId,
        PaymentStatus status,
        string? paypalOrderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate final amount after sliding scale discount
    /// </summary>
    ValueObjects.Money CalculateFinalAmount(
        ValueObjects.Money originalAmount,
        decimal slidingScalePercentage);

    /// <summary>
    /// Validate sliding scale percentage (0-75%)
    /// </summary>
    bool IsValidSlidingScalePercentage(decimal percentage);

    /// <summary>
    /// Create audit log entry for payment actions
    /// </summary>
    Task<Result> CreateAuditLogAsync(
        PaymentAuditLog auditLog,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for processing payments
/// </summary>
public class ProcessPaymentRequest
{
    public Guid EventRegistrationId { get; set; }
    public Guid UserId { get; set; }
    public ValueObjects.Money OriginalAmount { get; set; } = null!;
    public decimal SlidingScalePercentage { get; set; }
    public PaymentMethodType PaymentMethodType { get; set; }
    public string? ReturnUrl { get; set; }
    public string? CancelUrl { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

// NOTE: Result class now uses WitchCityRope.Api.Features.Shared.Models.Result (shared implementation)
// Duplicate Result classes removed to prevent namespace conflicts