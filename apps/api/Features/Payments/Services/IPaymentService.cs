using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;

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
        string? stripePaymentIntentId = null,
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
    public string? SavedPaymentMethodId { get; set; }
    public string? StripePaymentMethodId { get; set; }
    public bool SavePaymentMethod { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

/// <summary>
/// Result pattern for error handling
/// </summary>
public class Result
{
    public bool IsSuccess { get; protected set; }
    public string ErrorMessage { get; protected set; } = string.Empty;
    public List<string> Errors { get; protected set; } = new();

    protected Result(bool isSuccess, string errorMessage = "", List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Errors = errors ?? new List<string>();
    }

    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(List<string> errors) => new(false, "", errors);
}

/// <summary>
/// Generic result pattern for returning values
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; private set; }

    private Result(bool isSuccess, T? value = default, string errorMessage = "", List<string>? errors = null)
        : base(isSuccess, errorMessage, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value);
    public static new Result<T> Failure(string error) => new(false, default, error);
    public static new Result<T> Failure(List<string> errors) => new(false, default, "", errors);
}