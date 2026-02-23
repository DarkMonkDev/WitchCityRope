using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Payments.Models.PayPal;
using PaypalServerSdk.Standard;
using PaypalServerSdk.Standard.Controllers;
using PaypalServerSdk.Standard.Models;
using PaypalServerSdk.Standard.Exceptions;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WcrMoney = WitchCityRope.Api.Features.Payments.ValueObjects.Money;
using PayPalMoney = PaypalServerSdk.Standard.Models.Money;

namespace WitchCityRope.Api.Features.Payments.Services;

/// <summary>
/// PayPal payment processing service using PayPalServerSDK 2.x.
/// Handles order creation, capture, refunds, and webhook processing.
/// Includes automatic Venmo support through PayPal SDK.
/// </summary>
public class PayPalService : IPayPalService
{
    private readonly PaypalServerSdkClient _client;
    private readonly OrdersController _ordersController;
    private readonly PaymentsController _paymentsController;
    private readonly ILogger<PayPalService> _logger;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    private static DateTime ParseCreateTime(string? createTime)
    {
        if (string.IsNullOrEmpty(createTime)) return DateTime.UtcNow;
        return DateTime.TryParse(createTime, out var result) ? result : DateTime.UtcNow;
    }

    private static string GetMethodString(LinkHttpMethod? method)
    {
        return method?.ToString() ?? "GET";
    }

    public PayPalService(
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ILogger<PayPalService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var clientId = configuration["PayPal:ClientId"]
            ?? throw new InvalidOperationException("PayPal ClientId not configured");

        var clientSecret = configuration["PayPal:Secret"]
            ?? throw new InvalidOperationException("PayPal Secret not configured");

        var mode = configuration["PayPal:Mode"] ?? "sandbox";
        var environment = mode.Equals("live", StringComparison.OrdinalIgnoreCase)
            ? PaypalServerSdk.Standard.Environment.Production
            : PaypalServerSdk.Standard.Environment.Sandbox;

        _client = new PaypalServerSdkClient.Builder()
            .ClientCredentialsAuth(
                new PaypalServerSdk.Standard.Authentication.ClientCredentialsAuthModel.Builder(
                    clientId,
                    clientSecret
                ).Build()
            )
            .Environment(environment)
            .Build();

        _ordersController = _client.OrdersController;
        _paymentsController = _client.PaymentsController;
    }

    public async Task<Result<PayPalOrderResponse>> CreateOrderAsync(
        WcrMoney amount,
        Guid customerId,
        int slidingScalePercentage,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating PayPal order for customer {CustomerId}, amount {Amount}, sliding scale {SlidingScale}%",
                customerId, amount.ToDisplayString(), slidingScalePercentage);

            var returnUrl = _configuration["PayPal:ReturnUrl"] ?? "https://witchcityrope.com/payment/success";
            var cancelUrl = _configuration["PayPal:CancelUrl"] ?? "https://witchcityrope.com/payment/cancel";

            var orderRequest = new OrderRequest
            {
                Intent = CheckoutPaymentIntent.Capture,
                PurchaseUnits = new List<PurchaseUnitRequest>
                {
                    new PurchaseUnitRequest
                    {
                        Amount = new AmountWithBreakdown
                        {
                            CurrencyCode = amount.Currency,
                            MValue = amount.ToPayPalAmount()
                        },
                        CustomId = customerId.ToString(),
                        Description = $"WitchCityRope Event Registration - {slidingScalePercentage}% sliding scale discount applied",
                        InvoiceId = Guid.NewGuid().ToString()
                    }
                },
                PaymentSource = new PaymentSource
                {
                    Paypal = new PaypalWallet
                    {
                        ExperienceContext = new PaypalWalletExperienceContext
                        {
                            ReturnUrl = returnUrl,
                            CancelUrl = cancelUrl,
                            BrandName = "WitchCityRope",
                            LandingPage = PaypalExperienceLandingPage.Login,
                            UserAction = PaypalExperienceUserAction.PayNow,
                            ShippingPreference = PaypalWalletContextShippingPreference.NoShipping
                        }
                    }
                }
            };

            var createInput = new CreateOrderInput
            {
                Body = orderRequest,
                Prefer = "return=representation"
            };

            var response = await _ordersController.CreateOrderAsync(createInput);
            var order = response.Data;

            var paypalResponse = new PayPalOrderResponse
            {
                OrderId = order.Id,
                Status = order.Status?.ToString() ?? "CREATED",
                CreateTime = ParseCreateTime(order.CreateTime),
                Links = order.Links?.Select(link => new PayPalLink
                {
                    Href = link.Href,
                    Rel = link.Rel,
                    Method = GetMethodString(link.Method)
                }).ToList() ?? new List<PayPalLink>()
            };

            _logger.LogInformation(
                "PayPal order {OrderId} created successfully for customer {CustomerId}",
                order.Id, customerId);

            return Result<PayPalOrderResponse>.Success(paypalResponse);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "PayPal API error creating order for customer {CustomerId}: {StatusCode}",
                customerId, ex.ResponseCode);
            return Result<PayPalOrderResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal order for customer {CustomerId}", customerId);
            return Result<PayPalOrderResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<PayPalCaptureResponse>> CaptureOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Capturing PayPal order {OrderId}", orderId);

            var captureInput = new CaptureOrderInput
            {
                Id = orderId,
                Prefer = "return=representation"
            };

            var response = await _ordersController.CaptureOrderAsync(captureInput);
            var order = response.Data;

            // Extract capture details from the first purchase unit
            var capture = order.PurchaseUnits?.FirstOrDefault()?.Payments?.Captures?.FirstOrDefault();
            if (capture == null)
            {
                return Result<PayPalCaptureResponse>.Failure("No capture found in PayPal response");
            }

            var captureResponse = new PayPalCaptureResponse
            {
                CaptureId = capture.Id,
                Status = capture.Status?.ToString() ?? "UNKNOWN",
                Amount = new PayPalAmount
                {
                    CurrencyCode = capture.Amount?.CurrencyCode ?? "USD",
                    Value = capture.Amount?.MValue ?? "0.00"
                },
                FinalCapture = capture.FinalCapture ?? true,
                CreateTime = ParseCreateTime(capture.CreateTime),
                UpdateTime = ParseCreateTime(capture.UpdateTime),
                TransactionId = capture.Id,
                PayerId = order.Payer?.PayerId,
                OrderId = orderId,
                CaptureTime = DateTime.UtcNow,
                PayerEmail = order.Payer?.EmailAddress,
                PayerName = order.Payer?.Name != null
                    ? $"{order.Payer.Name.GivenName} {order.Payer.Name.Surname}".Trim()
                    : null
            };

            _logger.LogInformation(
                "PayPal order {OrderId} captured successfully with capture ID {CaptureId}",
                orderId, capture.Id);

            return Result<PayPalCaptureResponse>.Success(captureResponse);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "PayPal API error capturing order {OrderId}: {StatusCode}",
                orderId, ex.ResponseCode);
            return Result<PayPalCaptureResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal order {OrderId}", orderId);
            return Result<PayPalCaptureResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<PayPalOrderResponse>> GetOrderAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var getInput = new GetOrderInput
            {
                Id = orderId
            };

            var response = await _ordersController.GetOrderAsync(getInput);
            var order = response.Data;

            var paypalResponse = new PayPalOrderResponse
            {
                OrderId = order.Id,
                Status = order.Status?.ToString() ?? "UNKNOWN",
                CreateTime = ParseCreateTime(order.CreateTime),
                Links = order.Links?.Select(link => new PayPalLink
                {
                    Href = link.Href,
                    Rel = link.Rel,
                    Method = GetMethodString(link.Method)
                }).ToList() ?? new List<PayPalLink>()
            };

            return Result<PayPalOrderResponse>.Success(paypalResponse);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "PayPal API error retrieving order {OrderId}: {StatusCode}",
                orderId, ex.ResponseCode);
            return Result<PayPalOrderResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PayPal order {OrderId}", orderId);
            return Result<PayPalOrderResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<PayPalRefundResponse>> RefundCaptureAsync(
        string captureId,
        WcrMoney refundAmount,
        string reason,
        string? idempotencyKey = null,
        string? noteToPayer = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Creating refund for PayPal capture {CaptureId}, amount {RefundAmount}, idempotency key {IdempotencyKey}",
                captureId, refundAmount.ToDisplayString(), idempotencyKey ?? "none");

            var refundRequest = new RefundRequest
            {
                Amount = new PayPalMoney
                {
                    CurrencyCode = refundAmount.Currency,
                    MValue = refundAmount.ToPayPalAmount()
                },
                InvoiceId = Guid.NewGuid().ToString(),
                NoteToPayer = noteToPayer ?? $"Refund for WitchCityRope event registration: {reason}"
            };

            var refundInput = new RefundCapturedPaymentInput
            {
                CaptureId = captureId,
                Body = refundRequest,
                Prefer = "return=representation",
                PaypalRequestId = idempotencyKey
            };

            var response = await _paymentsController.RefundCapturedPaymentAsync(refundInput);
            var refund = response.Data;

            var refundResponse = new PayPalRefundResponse
            {
                RefundId = refund.Id,
                Status = refund.Status?.ToString() ?? "UNKNOWN",
                Amount = new PayPalAmount
                {
                    CurrencyCode = refund.Amount?.CurrencyCode ?? "USD",
                    Value = refund.Amount?.MValue ?? "0.00"
                },
                CreateTime = ParseCreateTime(refund.CreateTime),
                UpdateTime = ParseCreateTime(refund.UpdateTime),
                Reason = reason,
                InvoiceId = refund.InvoiceId,
                NoteToPayer = refund.NoteToPayer,
                CaptureId = captureId,
                RefundTime = DateTime.UtcNow
            };

            _logger.LogInformation("PayPal refund {RefundId} created successfully for capture {CaptureId}",
                refund.Id, captureId);

            return Result<PayPalRefundResponse>.Success(refundResponse);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "PayPal API error creating refund for capture {CaptureId}: {StatusCode}",
                captureId, ex.ResponseCode);
            return Result<PayPalRefundResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for PayPal capture {CaptureId}", captureId);
            return Result<PayPalRefundResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<PayPalRefundResponse>> GetRefundAsync(
        string refundId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var getInput = new GetRefundInput
            {
                RefundId = refundId
            };

            var response = await _paymentsController.GetRefundAsync(getInput);
            var refund = response.Data;

            var refundResponse = new PayPalRefundResponse
            {
                RefundId = refund.Id,
                Status = refund.Status?.ToString() ?? "UNKNOWN",
                Amount = new PayPalAmount
                {
                    CurrencyCode = refund.Amount?.CurrencyCode ?? "USD",
                    Value = refund.Amount?.MValue ?? "0.00"
                },
                CreateTime = ParseCreateTime(refund.CreateTime),
                UpdateTime = ParseCreateTime(refund.UpdateTime),
                InvoiceId = refund.InvoiceId,
                NoteToPayer = refund.NoteToPayer
            };

            return Result<PayPalRefundResponse>.Success(refundResponse);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "PayPal API error retrieving refund {RefundId}: {StatusCode}",
                refundId, ex.ResponseCode);
            return Result<PayPalRefundResponse>.Failure($"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PayPal refund {RefundId}", refundId);
            return Result<PayPalRefundResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public Result<Dictionary<string, object>> ValidateWebhookSignature(string payload, string signature, string webhookId)
    {
        try
        {
            var webhookEvent = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);

            if (webhookEvent == null)
            {
                return Result<Dictionary<string, object>>.Failure("Invalid webhook payload");
            }

            _logger.LogInformation("PayPal webhook payload parsed (signature verification delegated to PayPalWebhookVerificationService)");
            return Result<Dictionary<string, object>>.Success(webhookEvent);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid PayPal webhook JSON payload");
            return Result<Dictionary<string, object>>.Failure($"Webhook JSON parsing failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing PayPal webhook payload");
            return Result<Dictionary<string, object>>.Failure($"Webhook validation error: {ex.Message}");
        }
    }

    public Result<PayPalWebhookEvent> ValidateWebhookSignatureTyped(string payload, string signature, string webhookId)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var webhookEvent = JsonSerializer.Deserialize<PayPalWebhookEvent>(payload, jsonOptions);

            if (webhookEvent == null)
            {
                return Result<PayPalWebhookEvent>.Failure("Invalid webhook payload");
            }

            if (string.IsNullOrEmpty(webhookEvent.EventType))
            {
                return Result<PayPalWebhookEvent>.Failure("Webhook event is missing event_type");
            }

            _logger.LogInformation("PayPal webhook parsed for event type: {EventType}", webhookEvent.EventType);
            return Result<PayPalWebhookEvent>.Success(webhookEvent);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid PayPal webhook JSON payload");
            return Result<PayPalWebhookEvent>.Failure($"Webhook JSON parsing failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing PayPal webhook payload");
            return Result<PayPalWebhookEvent>.Failure($"Webhook validation error: {ex.Message}");
        }
    }

}
