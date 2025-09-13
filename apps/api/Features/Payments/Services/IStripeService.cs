using Stripe;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Stripe payment processing service interface
/// Handles all interactions with Stripe APIs
/// </summary>
public interface IStripeService
{
    /// <summary>
    /// Create a PaymentIntent for processing payment
    /// </summary>
    Task<Result<PaymentIntent>> CreatePaymentIntentAsync(
        Money amount,
        string currency,
        Guid customerId,
        string? paymentMethodId = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve PaymentIntent by ID
    /// </summary>
    Task<Result<PaymentIntent>> GetPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or retrieve Stripe customer
    /// </summary>
    Task<Result<Customer>> CreateOrRetrieveCustomerAsync(
        Guid userId,
        string email,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save payment method for future use
    /// </summary>
    Task<Result<Stripe.PaymentMethod>> SavePaymentMethodAsync(
        string paymentMethodId,
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve saved payment methods for customer
    /// </summary>
    Task<Result<List<Stripe.PaymentMethod>>> GetSavedPaymentMethodsAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create refund for a payment
    /// </summary>
    Task<Result<Refund>> CreateRefundAsync(
        string paymentIntentId,
        Money refundAmount,
        string reason,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve refund by ID
    /// </summary>
    Task<Result<Refund>> GetRefundAsync(
        string refundId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate webhook signature
    /// </summary>
    Result<Event> ValidateWebhookSignature(
        string payload,
        string signature,
        string webhookSecret);

    /// <summary>
    /// Process webhook event
    /// </summary>
    Task<Result> ProcessWebhookEventAsync(
        Event stripeEvent,
        CancellationToken cancellationToken = default);
}