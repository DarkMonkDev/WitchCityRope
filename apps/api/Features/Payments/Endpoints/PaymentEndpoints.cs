using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FluentValidation;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Models.Responses;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using WitchCityRope.Api.Features.Payments.Entities;

namespace WitchCityRope.Api.Features.Payments.Endpoints;

/// <summary>
/// Payment processing endpoints with sliding scale pricing support
/// Implements MVP functionality for event registration payments
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize] // All payment endpoints require authentication
public class PaymentEndpoints : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IRefundService _refundService;
    private readonly IValidator<ProcessPaymentApiRequest> _paymentValidator;
    private readonly IValidator<ProcessRefundApiRequest> _refundValidator;
    private readonly ILogger<PaymentEndpoints> _logger;

    public PaymentEndpoints(
        IPaymentService paymentService,
        IRefundService refundService,
        IValidator<ProcessPaymentApiRequest> paymentValidator,
        IValidator<ProcessRefundApiRequest> refundValidator,
        ILogger<PaymentEndpoints> logger)
    {
        _paymentService = paymentService;
        _refundService = refundService;
        _paymentValidator = paymentValidator;
        _refundValidator = refundValidator;
        _logger = logger;
    }

    /// <summary>
    /// Process payment for event registration with sliding scale pricing
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> ProcessPayment(
        [FromBody] ProcessPaymentApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing payment request for registration {RegistrationId} by user {UserId}",
                request.EventRegistrationId, GetCurrentUserId());

            // Validate request
            var validationResult = await _paymentValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var problemDetails = CreateValidationProblemDetails(validationResult);
                return BadRequest(problemDetails);
            }

            // Get current user information
            var currentUserId = GetCurrentUserId();
            var clientInfo = GetClientInfo();

            // Map to service request
            var serviceRequest = new ProcessPaymentRequest
            {
                EventRegistrationId = request.EventRegistrationId,
                UserId = currentUserId,
                OriginalAmount = Money.Create(request.OriginalAmount, request.Currency),
                SlidingScalePercentage = request.SlidingScalePercentage,
                PaymentMethodType = request.PaymentMethodType,
                ReturnUrl = request.ReturnUrl,
                CancelUrl = request.CancelUrl,
                IpAddress = clientInfo.IpAddress,
                UserAgent = clientInfo.UserAgent
            };

            // Process payment
            var result = await _paymentService.ProcessPaymentAsync(serviceRequest, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Payment processing failed for registration {RegistrationId}: {Error}",
                    request.EventRegistrationId, result.ErrorMessage);

                return BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
            }

            // Map to response
            var response = MapToPaymentResponse(result.Value!);

            _logger.LogInformation(
                "Payment {PaymentId} processed successfully for registration {RegistrationId}",
                response.Id, request.EventRegistrationId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment for registration {RegistrationId}",
                request.EventRegistrationId);
            return StatusCode(500, new { error = "An unexpected error occurred while processing your payment" });
        }
    }

    /// <summary>
    /// Get payment details by ID
    /// </summary>
    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType<PaymentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaymentResponse>> GetPayment(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _paymentService.GetPaymentByIdAsync(paymentId, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Error retrieving payment {PaymentId}: {Error}", paymentId, result.ErrorMessage);
                return BadRequest(new { error = result.ErrorMessage });
            }

            if (result.Value == null)
            {
                return NotFound(new { error = "Payment not found" });
            }

            // Check if user has permission to view this payment
            var currentUserId = GetCurrentUserId();
            if (result.Value.UserId != currentUserId && !IsAdmin())
            {
                return Forbid();
            }

            var response = MapToPaymentResponse(result.Value);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving payment {PaymentId}", paymentId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get payment status for event registration
    /// </summary>
    [HttpGet("registration/{eventRegistrationId:guid}/status")]
    [ProducesResponseType<PaymentStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaymentStatusResponse>> GetPaymentStatus(
        Guid eventRegistrationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentResult = await _paymentService.GetPaymentByRegistrationIdAsync(eventRegistrationId, cancellationToken);

            if (!paymentResult.IsSuccess)
            {
                return BadRequest(new { error = paymentResult.ErrorMessage });
            }

            if (paymentResult.Value == null)
            {
                return NotFound(new { error = "No payment found for this registration" });
            }

            // Check permissions
            var currentUserId = GetCurrentUserId();
            if (paymentResult.Value.UserId != currentUserId && !IsAdmin())
            {
                return Forbid();
            }

            var response = MapToPaymentStatusResponse(paymentResult.Value);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving payment status for registration {RegistrationId}",
                eventRegistrationId);
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Process refund for a completed payment (Admin/Teacher only)
    /// </summary>
    [HttpPost("{paymentId:guid}/refund")]
    [Authorize(Roles = "Administrator,Teacher")]
    [ProducesResponseType<RefundResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RefundResponse>> ProcessRefund(
        Guid paymentId,
        [FromBody] ProcessRefundApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing refund request for payment {PaymentId} by user {UserId}",
                paymentId, GetCurrentUserId());

            // Validate request
            var validationResult = await _refundValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var problemDetails = CreateValidationProblemDetails(validationResult);
                return BadRequest(problemDetails);
            }

            // Ensure payment ID matches
            if (request.PaymentId != paymentId)
            {
                return BadRequest(new { error = "Payment ID mismatch" });
            }

            // Get current user and client info
            var currentUserId = GetCurrentUserId();
            var clientInfo = GetClientInfo();

            // Map to service request
            var serviceRequest = new ProcessRefundRequest
            {
                PaymentId = paymentId,
                RefundAmount = Money.Create(request.RefundAmount, request.Currency),
                RefundReason = request.RefundReason,
                ProcessedByUserId = currentUserId,
                IpAddress = clientInfo.IpAddress,
                UserAgent = clientInfo.UserAgent,
                Metadata = request.Metadata
            };

            // Process refund
            var result = await _refundService.ProcessRefundAsync(serviceRequest, cancellationToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Refund processing failed for payment {PaymentId}: {Error}",
                    paymentId, result.ErrorMessage);

                return BadRequest(new { error = result.ErrorMessage, errors = result.Errors });
            }

            // Map to response
            var response = MapToRefundResponse(result.Value!);

            _logger.LogInformation("Refund {RefundId} processed successfully for payment {PaymentId}",
                response.Id, paymentId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing refund for payment {PaymentId}", paymentId);
            return StatusCode(500, new { error = "An unexpected error occurred while processing the refund" });
        }
    }

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }

        return userId;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin") || User.IsInRole("Teacher");
    }

    private ClientInfo GetClientInfo()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        return new ClientInfo(ipAddress, userAgent);
    }

    private PaymentResponse MapToPaymentResponse(Payment payment)
    {
        var amount = payment.GetAmount();
        var originalAmount = amount * (1 + payment.SlidingScalePercentage / 100);
        var discountAmount = originalAmount.Amount - amount.Amount;

        var response = new PaymentResponse
        {
            Id = payment.Id,
            EventRegistrationId = payment.EventRegistrationId,
            UserId = payment.UserId,
            Amount = amount.Amount,
            Currency = amount.Currency,
            DisplayAmount = amount.ToDisplayString(),
            OriginalAmount = originalAmount.Amount,
            SlidingScalePercentage = payment.SlidingScalePercentage,
            DiscountAmount = discountAmount,
            Status = payment.Status,
            StatusDescription = GetStatusDescription(payment.Status),
            PaymentMethodType = payment.PaymentMethodType,
            ProcessedAt = payment.ProcessedAt,
            CreatedAt = payment.CreatedAt
        };

        // Add refund info if payment has been refunded
        if (payment.IsRefunded() && payment.GetRefundAmount() != null)
        {
            var refundAmount = payment.GetRefundAmount()!;
            response.RefundInfo = new RefundInfoResponse
            {
                RefundedAmount = refundAmount.Amount,
                Currency = refundAmount.Currency,
                DisplayAmount = refundAmount.ToDisplayString(),
                RefundedAt = payment.RefundedAt,
                RefundReason = payment.RefundReason,
                RefundCount = payment.Refunds?.Count ?? 0,
                IsPartialRefund = payment.Status == Models.PaymentStatus.PartiallyRefunded
            };
        }

        return response;
    }

    private PaymentStatusResponse MapToPaymentStatusResponse(Payment payment)
    {
        var amount = payment.GetAmount();

        return new PaymentStatusResponse
        {
            PaymentId = payment.Id,
            EventRegistrationId = payment.EventRegistrationId,
            Status = payment.Status,
            StatusDescription = GetStatusDescription(payment.Status),
            IsCompleted = payment.Status == Models.PaymentStatus.Completed,
            IsRefunded = payment.IsRefunded(),
            Amount = amount.Amount,
            Currency = amount.Currency,
            ProcessedAt = payment.ProcessedAt
        };
    }

    private RefundResponse MapToRefundResponse(PaymentRefund refund)
    {
        var amount = refund.GetRefundAmount();

        return new RefundResponse
        {
            Id = refund.Id,
            OriginalPaymentId = refund.OriginalPaymentId,
            RefundAmount = amount.Amount,
            Currency = amount.Currency,
            DisplayAmount = amount.ToDisplayString(),
            RefundReason = refund.RefundReason,
            RefundStatus = refund.RefundStatus,
            StatusDescription = GetRefundStatusDescription(refund.RefundStatus),
            ProcessedByUserId = refund.ProcessedByUserId,
            ProcessedByUserName = refund.ProcessedByUser?.SceneName ?? "Unknown",
            ProcessedAt = refund.ProcessedAt,
            CreatedAt = refund.CreatedAt
        };
    }

    private static string GetStatusDescription(Models.PaymentStatus status)
    {
        return status switch
        {
            Models.PaymentStatus.Pending => "Payment is being processed",
            Models.PaymentStatus.Completed => "Payment completed successfully",
            Models.PaymentStatus.Failed => "Payment failed",
            Models.PaymentStatus.Refunded => "Payment has been fully refunded",
            Models.PaymentStatus.PartiallyRefunded => "Payment has been partially refunded",
            _ => "Unknown status"
        };
    }

    private static string GetRefundStatusDescription(Models.RefundStatus status)
    {
        return status switch
        {
            Models.RefundStatus.Processing => "Refund is being processed",
            Models.RefundStatus.Completed => "Refund completed successfully",
            Models.RefundStatus.Failed => "Refund processing failed",
            Models.RefundStatus.Cancelled => "Refund was cancelled",
            _ => "Unknown refund status"
        };
    }

    private ValidationProblemDetails CreateValidationProblemDetails(FluentValidation.Results.ValidationResult validationResult)
    {
        var problemDetails = new ValidationProblemDetails();

        foreach (var error in validationResult.Errors)
        {
            if (problemDetails.Errors.ContainsKey(error.PropertyName))
            {
                problemDetails.Errors[error.PropertyName] = problemDetails.Errors[error.PropertyName]
                    .Concat(new[] { error.ErrorMessage }).ToArray();
            }
            else
            {
                problemDetails.Errors.Add(error.PropertyName, new[] { error.ErrorMessage });
            }
        }

        return problemDetails;
    }

    #endregion

    #region Data Transfer Objects

    private record ClientInfo(string IpAddress, string UserAgent);

    #endregion
}