using Stripe;
using WitchCityRope.Api.Features.Payments.ValueObjects;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// Stripe payment processing service implementation
/// Handles all interactions with Stripe APIs with comprehensive error handling
/// </summary>
public class StripeService : IStripeService
{
    private readonly PaymentIntentService _paymentIntentService;
    private readonly CustomerService _customerService;
    private readonly PaymentMethodService _paymentMethodService;
    private readonly Stripe.RefundService _stripeRefundService;
    private readonly ILogger<StripeService> _logger;
    private readonly string _webhookSecret;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        // Initialize Stripe SDK
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe SecretKey not configured");

        _webhookSecret = configuration["Stripe:WebhookSecret"]
            ?? throw new InvalidOperationException("Stripe WebhookSecret not configured");

        // Initialize Stripe services
        _paymentIntentService = new PaymentIntentService();
        _customerService = new CustomerService();
        _paymentMethodService = new PaymentMethodService();
        _stripeRefundService = new Stripe.RefundService();
        _logger = logger;
    }

    public async Task<Result<PaymentIntent>> CreatePaymentIntentAsync(
        Money amount,
        string currency,
        Guid customerId,
        string? paymentMethodId = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating PaymentIntent for customer {CustomerId}, amount {Amount}",
                customerId, amount.ToDisplayString());

            var options = new PaymentIntentCreateOptions
            {
                Amount = amount.ToStripeAmount(),
                Currency = currency.ToLowerInvariant(),
                Customer = customerId.ToString(),
                PaymentMethod = paymentMethodId,
                Metadata = metadata ?? new Dictionary<string, string>(),
                ConfirmationMethod = "manual",
                Confirm = false,
                SetupFutureUsage = "off_session" // Enable saving payment method
            };

            if (!string.IsNullOrEmpty(paymentMethodId))
            {
                options.PaymentMethodTypes = new List<string> { "card" };
            }

            var paymentIntent = await _paymentIntentService.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "PaymentIntent {PaymentIntentId} created successfully for customer {CustomerId}",
                paymentIntent.Id, customerId);

            return Result<PaymentIntent>.Success(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating PaymentIntent for customer {CustomerId}: {ErrorCode}", 
                customerId, ex.StripeError?.Code);
            return Result<PaymentIntent>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PaymentIntent for customer {CustomerId}", customerId);
            return Result<PaymentIntent>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<PaymentIntent>> GetPaymentIntentAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(
                paymentIntentId,
                cancellationToken: cancellationToken);

            return Result<PaymentIntent>.Success(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error retrieving PaymentIntent {PaymentIntentId}: {ErrorCode}",
                paymentIntentId, ex.StripeError?.Code);
            return Result<PaymentIntent>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PaymentIntent {PaymentIntentId}", paymentIntentId);
            return Result<PaymentIntent>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<Customer>> CreateOrRetrieveCustomerAsync(
        Guid userId,
        string email,
        string name,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // First, try to find existing customer by user ID in metadata
            var searchOptions = new CustomerSearchOptions
            {
                Query = $"metadata['user_id']:'{userId}'"
            };

            var existingCustomers = await _customerService.SearchAsync(searchOptions, cancellationToken: cancellationToken);

            if (existingCustomers.Data.Any())
            {
                var existingCustomer = existingCustomers.Data.First();
                _logger.LogInformation("Found existing Stripe customer {CustomerId} for user {UserId}",
                    existingCustomer.Id, userId);
                return Result<Customer>.Success(existingCustomer);
            }

            // Create new customer
            var createOptions = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(),
                    ["created_by"] = "WitchCityRope API"
                }
            };

            var customer = await _customerService.CreateAsync(createOptions, cancellationToken: cancellationToken);

            _logger.LogInformation("Created new Stripe customer {CustomerId} for user {UserId}",
                customer.Id, userId);

            return Result<Customer>.Success(customer);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating/retrieving customer for user {UserId}: {ErrorCode}",
                userId, ex.StripeError?.Code);
            return Result<Customer>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/retrieving customer for user {UserId}", userId);
            return Result<Customer>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<Stripe.PaymentMethod>> SavePaymentMethodAsync(
        string paymentMethodId,
        string customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new PaymentMethodAttachOptions
            {
                Customer = customerId
            };

            var paymentMethod = await _paymentMethodService.AttachAsync(
                paymentMethodId, options, cancellationToken: cancellationToken);

            _logger.LogInformation("Payment method {PaymentMethodId} attached to customer {CustomerId}",
                paymentMethodId, customerId);

            return Result<Stripe.PaymentMethod>.Success(paymentMethod);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error saving payment method {PaymentMethodId}: {ErrorCode}",
                paymentMethodId, ex.StripeError?.Code);
            return Result<Stripe.PaymentMethod>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payment method {PaymentMethodId}", paymentMethodId);
            return Result<Stripe.PaymentMethod>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<List<Stripe.PaymentMethod>>> GetSavedPaymentMethodsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new PaymentMethodListOptions
            {
                Customer = customerId,
                Type = "card"
            };

            var paymentMethods = await _paymentMethodService.ListAsync(options, cancellationToken: cancellationToken);

            return Result<List<Stripe.PaymentMethod>>.Success(paymentMethods.Data.ToList());
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error retrieving payment methods for customer {CustomerId}: {ErrorCode}",
                customerId, ex.StripeError?.Code);
            return Result<List<Stripe.PaymentMethod>>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment methods for customer {CustomerId}", customerId);
            return Result<List<Stripe.PaymentMethod>>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<Refund>> CreateRefundAsync(
        string paymentIntentId,
        Money refundAmount,
        string reason,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating refund for PaymentIntent {PaymentIntentId}, amount {RefundAmount}",
                paymentIntentId, refundAmount.ToDisplayString());

            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = refundAmount.ToStripeAmount(),
                Reason = "requested_by_customer",
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            // Add custom reason to metadata since Stripe has limited reason options
            options.Metadata["custom_reason"] = reason;

            var refund = await _stripeRefundService.CreateAsync(options, cancellationToken: cancellationToken);

            _logger.LogInformation("Refund {RefundId} created successfully for PaymentIntent {PaymentIntentId}",
                refund.Id, paymentIntentId);

            return Result<Refund>.Success(refund);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating refund for PaymentIntent {PaymentIntentId}: {ErrorCode}",
                paymentIntentId, ex.StripeError?.Code);
            return Result<Refund>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for PaymentIntent {PaymentIntentId}", paymentIntentId);
            return Result<Refund>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<Refund>> GetRefundAsync(
        string refundId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var refund = await _stripeRefundService.GetAsync(refundId, cancellationToken: cancellationToken);
            return Result<Refund>.Success(refund);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error retrieving refund {RefundId}: {ErrorCode}",
                refundId, ex.StripeError?.Code);
            return Result<Refund>.Failure($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refund {RefundId}", refundId);
            return Result<Refund>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public Result<Event> ValidateWebhookSignature(string payload, string signature, string webhookSecret)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);
            return Result<Event>.Success(stripeEvent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Invalid webhook signature: {ErrorCode}", ex.StripeError?.Code);
            return Result<Event>.Failure($"Webhook signature validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return Result<Event>.Failure($"Webhook validation error: {ex.Message}");
        }
    }

    public async Task<Result> ProcessWebhookEventAsync(Event stripeEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Stripe webhook event: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    return await HandlePaymentIntentSucceededAsync(stripeEvent, cancellationToken);
                
                case "payment_intent.payment_failed":
                    return await HandlePaymentIntentFailedAsync(stripeEvent, cancellationToken);

                case "charge.dispute.created":
                    return await HandleChargeDisputeCreatedAsync(stripeEvent, cancellationToken);

                default:
                    _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                    return Result.Success();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventType}", stripeEvent.Type);
            return Result.Failure($"Webhook processing error: {ex.Message}");
        }
    }

    private async Task<Result> HandlePaymentIntentSucceededAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            return Result.Failure("PaymentIntent data not found in webhook");
        }

        _logger.LogInformation("Payment succeeded: {PaymentIntentId}", paymentIntent.Id);
        
        // TODO: Update payment status in database
        // This would involve finding the payment by encrypted PaymentIntent ID
        // and updating its status to Completed

        return Result.Success();
    }

    private async Task<Result> HandlePaymentIntentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            return Result.Failure("PaymentIntent data not found in webhook");
        }

        _logger.LogWarning("Payment failed: {PaymentIntentId}, Error: {LastPaymentError}",
            paymentIntent.Id, paymentIntent.LastPaymentError?.Message);

        // TODO: Update payment status in database and log failure
        
        return Result.Success();
    }

    private async Task<Result> HandleChargeDisputeCreatedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Charge dispute created for event: {EventId}", stripeEvent.Id);
        
        // TODO: Create notification for admin team about dispute
        
        return Result.Success();
    }
}