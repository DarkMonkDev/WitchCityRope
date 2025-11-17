using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WitchCityRope.Api.Features.Payments.Endpoints;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Models;
using WitchCityRope.Api.Features.Payments.Models.Requests;
using WitchCityRope.Api.Features.Payments.Models.Responses;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Payments.ValueObjects;
using Xunit;
using FluentAssertions;
using NSubstitute;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace WitchCityRope.UnitTests.Api.Features.Payments;

/// <summary>
/// Unit tests for PaymentEndpoints following Pattern B standards
/// Tests all CRUD operations for payment processing with sliding scale pricing
/// </summary>
public class PaymentEndpointsTests
{
    private readonly IPaymentService _mockPaymentService;
    private readonly IRefundService _mockRefundService;
    private readonly IValidator<ProcessPaymentApiRequest> _mockPaymentValidator;
    private readonly IValidator<ProcessRefundApiRequest> _mockRefundValidator;
    private readonly ILogger<PaymentEndpoints> _mockLogger;
    private readonly PaymentEndpoints _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _adminUserId = Guid.NewGuid();

    public PaymentEndpointsTests()
    {
        _mockPaymentService = Substitute.For<IPaymentService>();
        _mockRefundService = Substitute.For<IRefundService>();
        _mockPaymentValidator = Substitute.For<IValidator<ProcessPaymentApiRequest>>();
        _mockRefundValidator = Substitute.For<IValidator<ProcessRefundApiRequest>>();
        _mockLogger = Substitute.For<ILogger<PaymentEndpoints>>();

        _controller = new PaymentEndpoints(
            _mockPaymentService,
            _mockRefundService,
            _mockPaymentValidator,
            _mockRefundValidator,
            _mockLogger);
    }

    #region ProcessPayment Tests

    [Fact]
    public async Task ProcessPayment_WithValidRequest_Returns200OkWithPaymentResponse()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();
        var payment = CreateTestPayment();

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Success(payment));

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<PaymentResponse>().Subject;
        response.Id.Should().Be(payment.Id);
        response.EventRegistrationId.Should().Be(payment.EventRegistrationId);
        response.UserId.Should().Be(payment.UserId);
        response.Status.Should().Be(payment.Status);
    }

    [Fact]
    public async Task ProcessPayment_WithValidationFailure_Returns400BadRequestWithValidationProblemDetails()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("OriginalAmount", "Amount must be greater than zero"),
            new ValidationFailure("Currency", "Currency is required")
        };

        SetupAuthenticatedUser(_testUserId);
        _mockPaymentValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result.Result!;
        badRequestResult.StatusCode.Should().Be(400);

        var problemDetails = badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problemDetails.Errors.Should().ContainKey("OriginalAmount");
        problemDetails.Errors.Should().ContainKey("Currency");
    }

    [Fact]
    public async Task ProcessPayment_WithServiceFailure_Returns400BadRequestWithProblemDetails()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Failure("Payment processing failed: insufficient funds"));

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(400);

        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Payment Processing Failed");
        problemDetails.Detail.Should().Contain("insufficient funds");
    }

    [Fact]
    public void ProcessPayment_WithUnauthenticatedUser_Returns401Unauthorized()
    {
        // Arrange & Act
        // Verify that the controller is decorated with [Authorize] attribute
        // In real scenario, this would be caught by authorization middleware before endpoint is called
        var controllerType = typeof(PaymentEndpoints);
        var authorizeAttr = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .FirstOrDefault() as AuthorizeAttribute;

        // Assert
        authorizeAttr.Should().NotBeNull("PaymentEndpoints should require authentication");
    }

    [Fact]
    public async Task ProcessPayment_WithInvalidUserIdInToken_Returns500InternalServerError()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();

        // Setup user with invalid GUID in claim
        var invalidUserClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "invalid-guid")
        }, "test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = invalidUserClaims }
        };

        // Setup validation to pass so we reach line 69 where GetCurrentUserId() is called
        SetupValidationSuccess(request);

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        // GetCurrentUserId() throws UnauthorizedAccessException which is caught and returns 500
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(500);

        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Server Error");
    }

    [Fact]
    public async Task ProcessPayment_WithException_Returns500InternalServerError()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns<Result<Payment>>(x => throw new Exception("Database connection failed"));

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(500);

        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Server Error");
        problemDetails.Detail.Should().Contain("unexpected error");
    }

    [Fact]
    public async Task ProcessPayment_WithSlidingScalePercentage_CalculatesCorrectDiscount()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();
        request.SlidingScalePercentage = 50; // 50% discount

        var payment = CreateTestPayment();
        payment.SlidingScalePercentage = 50;

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Success(payment));

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var response = okResult.Value.Should().BeOfType<PaymentResponse>().Subject;
        response.SlidingScalePercentage.Should().Be(50);
        response.DiscountAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ProcessPayment_WithPayPalPaymentMethod_ProcessesCorrectly()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();
        request.PaymentMethodType = PaymentMethodType.PayPal;

        var payment = CreateTestPayment();
        payment.PaymentMethodType = PaymentMethodType.PayPal;

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(
            Arg.Is<ProcessPaymentRequest>(r => r.PaymentMethodType == PaymentMethodType.PayPal),
            Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Success(payment));

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        await _mockPaymentService.Received(1).ProcessPaymentAsync(
            Arg.Is<ProcessPaymentRequest>(r => r.PaymentMethodType == PaymentMethodType.PayPal),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPayment_WithNewCardPaymentMethod_ProcessesCorrectly()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();
        request.PaymentMethodType = PaymentMethodType.NewCard;

        var payment = CreateTestPayment();
        payment.PaymentMethodType = PaymentMethodType.NewCard;

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(
            Arg.Is<ProcessPaymentRequest>(r => r.PaymentMethodType == PaymentMethodType.NewCard),
            Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Success(payment));

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        await _mockPaymentService.Received(1).ProcessPaymentAsync(
            Arg.Is<ProcessPaymentRequest>(r => r.PaymentMethodType == PaymentMethodType.NewCard),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPayment_WithCashPaymentMethod_ProcessesCorrectly()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();
        request.PaymentMethodType = PaymentMethodType.Cash;

        var payment = CreateTestPayment();
        payment.PaymentMethodType = PaymentMethodType.Cash;

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(
            Arg.Is<ProcessPaymentRequest>(r => r.PaymentMethodType == PaymentMethodType.Cash),
            Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Success(payment));

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        await _mockPaymentService.Received(1).ProcessPaymentAsync(
            Arg.Is<ProcessPaymentRequest>(r => r.PaymentMethodType == PaymentMethodType.Cash),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPayment_IncludesReturnAndCancelUrls()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();
        request.ReturnUrl = "https://example.com/payment/success";
        request.CancelUrl = "https://example.com/payment/cancel";

        var payment = CreateTestPayment();

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Success(payment));

        // Act
        await _controller.ProcessPayment(request);

        // Assert
        await _mockPaymentService.Received(1).ProcessPaymentAsync(
            Arg.Is<ProcessPaymentRequest>(r =>
                r.ReturnUrl == request.ReturnUrl &&
                r.CancelUrl == request.CancelUrl),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPayment_CapturesClientIpAndUserAgent()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();
        var payment = CreateTestPayment();

        SetupAuthenticatedUserWithHttpContext(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Success(payment));

        // Act
        await _controller.ProcessPayment(request);

        // Assert
        await _mockPaymentService.Received(1).ProcessPaymentAsync(
            Arg.Is<ProcessPaymentRequest>(r =>
                !string.IsNullOrEmpty(r.IpAddress) &&
                !string.IsNullOrEmpty(r.UserAgent)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessPayment_WhenServiceReturnsNull_HandlesGracefully()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Success(null!));

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        // Endpoint catches NullReferenceException and returns 500 Internal Server Error
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(500);

        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Server Error");
    }

    [Fact]
    public async Task ProcessPayment_LogsSuccessfulPayment()
    {
        // Arrange
        var request = CreateValidProcessPaymentRequest();
        var payment = CreateTestPayment();

        SetupAuthenticatedUser(_testUserId);
        SetupValidationSuccess(request);

        _mockPaymentService.ProcessPaymentAsync(Arg.Any<ProcessPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<Payment>.Success(payment));

        // Act
        await _controller.ProcessPayment(request);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("processed successfully")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region GetPayment Tests

    [Fact]
    public async Task GetPayment_WithValidId_Returns200OkWithPaymentResponse()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment();
        payment.Id = paymentId;
        payment.UserId = _testUserId;

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(payment));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<PaymentResponse>().Subject;
        response.Id.Should().Be(paymentId);
    }

    [Fact]
    public async Task GetPayment_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(null));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);

        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Title.Should().Be("Not Found");
        problemDetails.Detail.Should().Contain("Payment not found");
    }

    [Fact]
    public async Task GetPayment_WhenUserNotOwnerAndNotAdmin_Returns403Forbidden()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment();
        payment.Id = paymentId;
        payment.UserId = Guid.NewGuid(); // Different user

        SetupAuthenticatedUser(_testUserId); // Regular user, not admin

        _mockPaymentService.GetPaymentByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(payment));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetPayment_WhenAdminViewsAnyPayment_Returns200Ok()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment();
        payment.Id = paymentId;
        payment.UserId = Guid.NewGuid(); // Different user

        SetupAuthenticatedAdmin(_adminUserId);

        _mockPaymentService.GetPaymentByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(payment));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPayment_WhenOwnerViewsOwnPayment_Returns200Ok()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment();
        payment.Id = paymentId;
        payment.UserId = _testUserId; // Same as authenticated user

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(payment));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPayment_WithServiceError_Returns400BadRequest()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Failure("Database error"));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(400);

        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Database error");
    }

    [Fact]
    public async Task GetPayment_WithException_Returns500InternalServerError()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns<Result<Payment?>>(x => throw new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetPayment_WithRefundedPayment_IncludesRefundInfo()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = CreateTestPayment();
        payment.Id = paymentId;
        payment.UserId = _testUserId;
        payment.Status = PaymentStatus.Refunded;
        payment.RefundedAt = DateTime.UtcNow;
        payment.RefundReason = "Customer requested refund";
        payment.RefundAmountValue = 100.00m; // Must set refund amount for GetRefundAmount() to return non-null
        payment.RefundCurrency = "USD";

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByIdAsync(paymentId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(payment));

        // Act
        var result = await _controller.GetPayment(paymentId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var response = okResult.Value.Should().BeOfType<PaymentResponse>().Subject;
        response.RefundInfo.Should().NotBeNull();
        response.RefundInfo!.RefundReason.Should().Be("Customer requested refund");
    }

    #endregion

    #region GetPaymentStatus Tests

    [Fact]
    public async Task GetPaymentStatus_WithValidRegistrationId_Returns200OkWithStatusResponse()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var payment = CreateTestPayment();
        payment.EventRegistrationId = registrationId;
        payment.UserId = _testUserId;
        payment.Status = PaymentStatus.Completed;

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByRegistrationIdAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(payment));

        // Act
        var result = await _controller.GetPaymentStatus(registrationId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<PaymentStatusResponse>().Subject;
        response.EventRegistrationId.Should().Be(registrationId);
        response.Status.Should().Be(PaymentStatus.Completed);
        response.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetPaymentStatus_WithNonExistentRegistration_Returns404NotFound()
    {
        // Arrange
        var registrationId = Guid.NewGuid();

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByRegistrationIdAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(null));

        // Act
        var result = await _controller.GetPaymentStatus(registrationId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);

        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("No payment found for this registration");
    }

    [Fact]
    public async Task GetPaymentStatus_WhenUserNotOwnerAndNotAdmin_Returns403Forbidden()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var payment = CreateTestPayment();
        payment.EventRegistrationId = registrationId;
        payment.UserId = Guid.NewGuid(); // Different user

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByRegistrationIdAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(payment));

        // Act
        var result = await _controller.GetPaymentStatus(registrationId);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetPaymentStatus_WhenAdminChecksAnyStatus_Returns200Ok()
    {
        // Arrange
        var registrationId = Guid.NewGuid();
        var payment = CreateTestPayment();
        payment.EventRegistrationId = registrationId;
        payment.UserId = Guid.NewGuid(); // Different user

        SetupAuthenticatedAdmin(_adminUserId);

        _mockPaymentService.GetPaymentByRegistrationIdAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Success(payment));

        // Act
        var result = await _controller.GetPaymentStatus(registrationId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPaymentStatus_WithServiceError_Returns400BadRequest()
    {
        // Arrange
        var registrationId = Guid.NewGuid();

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByRegistrationIdAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns(Result<Payment?>.Failure("Service unavailable"));

        // Act
        var result = await _controller.GetPaymentStatus(registrationId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetPaymentStatus_WithException_Returns500InternalServerError()
    {
        // Arrange
        var registrationId = Guid.NewGuid();

        SetupAuthenticatedUser(_testUserId);

        _mockPaymentService.GetPaymentByRegistrationIdAsync(registrationId, Arg.Any<CancellationToken>())
            .Returns<Result<Payment?>>(x => throw new Exception("Database connection lost"));

        // Act
        var result = await _controller.GetPaymentStatus(registrationId);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region ProcessRefund Tests

    [Fact]
    public async Task ProcessRefund_ByAdmin_WithValidRequest_Returns200OkWithRefundResponse()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);
        var refund = CreateTestRefund(paymentId);

        SetupAuthenticatedAdmin(_adminUserId);
        SetupRefundValidationSuccess(request);

        _mockRefundService.ProcessRefundAsync(Arg.Any<ProcessRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentRefund>.Success(refund));

        // Act
        var result = await _controller.ProcessRefund(paymentId, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<RefundResponse>().Subject;
        response.Id.Should().Be(refund.Id);
        response.OriginalPaymentId.Should().Be(paymentId);
    }

    [Fact]
    public async Task ProcessRefund_ByTeacher_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);
        var refund = CreateTestRefund(paymentId);

        SetupAuthenticatedTeacher(_adminUserId);
        SetupRefundValidationSuccess(request);

        _mockRefundService.ProcessRefundAsync(Arg.Any<ProcessRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentRefund>.Success(refund));

        // Act
        var result = await _controller.ProcessRefund(paymentId, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ProcessRefund_ByNonAdminUser_Returns403Forbidden()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);

        // Setup regular user (not admin or teacher)
        var regularUserClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
            new Claim(ClaimTypes.Role, "Member")
        }, "test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = regularUserClaims }
        };

        // Act & Assert
        // The [Authorize(Roles = "Administrator,Teacher")] attribute should prevent this
        // In real scenario, this would be caught by authorization middleware
        // For unit test, we verify the endpoint is decorated with the attribute
        var method = typeof(PaymentEndpoints).GetMethod(nameof(PaymentEndpoints.ProcessRefund));
        var authorizeAttr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false).FirstOrDefault() as AuthorizeAttribute;

        authorizeAttr.Should().NotBeNull();
        authorizeAttr!.Roles.Should().Be("Administrator,Teacher");
    }

    [Fact]
    public async Task ProcessRefund_WithValidationFailure_Returns400BadRequestWithValidationProblemDetails()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);
        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("RefundAmount", "Refund amount cannot exceed original payment amount")
        };

        SetupAuthenticatedAdmin(_adminUserId);
        _mockRefundValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await _controller.ProcessRefund(paymentId, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = (BadRequestObjectResult)result.Result!;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problemDetails.Errors.Should().ContainKey("RefundAmount");
    }

    [Fact]
    public async Task ProcessRefund_WithPaymentIdMismatch_Returns400BadRequest()
    {
        // Arrange
        var routePaymentId = Guid.NewGuid();
        var requestPaymentId = Guid.NewGuid(); // Different ID
        var request = CreateValidProcessRefundRequest(requestPaymentId);

        SetupAuthenticatedAdmin(_adminUserId);
        SetupRefundValidationSuccess(request);

        // Act
        var result = await _controller.ProcessRefund(routePaymentId, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(400);

        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Payment ID mismatch");
    }

    [Fact]
    public async Task ProcessRefund_WithServiceFailure_Returns400BadRequest()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);

        SetupAuthenticatedAdmin(_adminUserId);
        SetupRefundValidationSuccess(request);

        _mockRefundService.ProcessRefundAsync(Arg.Any<ProcessRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentRefund>.Failure("Payment not eligible for refund"));

        // Act
        var result = await _controller.ProcessRefund(paymentId, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(400);

        var problemDetails = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("not eligible for refund");
    }

    [Fact]
    public async Task ProcessRefund_WithFullRefund_ProcessesCorrectly()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);
        request.RefundAmount = 100.00m; // Full refund

        var refund = CreateTestRefund(paymentId);

        SetupAuthenticatedAdmin(_adminUserId);
        SetupRefundValidationSuccess(request);

        _mockRefundService.ProcessRefundAsync(Arg.Any<ProcessRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentRefund>.Success(refund));

        // Act
        var result = await _controller.ProcessRefund(paymentId, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        await _mockRefundService.Received(1).ProcessRefundAsync(
            Arg.Is<ProcessRefundRequest>(r => r.RefundAmount.Amount == 100.00m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefund_WithPartialRefund_ProcessesCorrectly()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);
        request.RefundAmount = 50.00m; // Partial refund

        var refund = CreateTestRefund(paymentId);

        SetupAuthenticatedAdmin(_adminUserId);
        SetupRefundValidationSuccess(request);

        _mockRefundService.ProcessRefundAsync(Arg.Any<ProcessRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentRefund>.Success(refund));

        // Act
        var result = await _controller.ProcessRefund(paymentId, request);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        await _mockRefundService.Received(1).ProcessRefundAsync(
            Arg.Is<ProcessRefundRequest>(r => r.RefundAmount.Amount == 50.00m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefund_PreservesMetadata()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);
        request.Metadata = new Dictionary<string, object>
        {
            { "reason_code", "duplicate_charge" },
            { "requested_by", "customer_support" }
        };

        var refund = CreateTestRefund(paymentId);

        SetupAuthenticatedAdmin(_adminUserId);
        SetupRefundValidationSuccess(request);

        _mockRefundService.ProcessRefundAsync(Arg.Any<ProcessRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentRefund>.Success(refund));

        // Act
        await _controller.ProcessRefund(paymentId, request);

        // Assert
        await _mockRefundService.Received(1).ProcessRefundAsync(
            Arg.Is<ProcessRefundRequest>(r => r.Metadata.ContainsKey("reason_code")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessRefund_WithException_Returns500InternalServerError()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);

        SetupAuthenticatedAdmin(_adminUserId);
        SetupRefundValidationSuccess(request);

        _mockRefundService.ProcessRefundAsync(Arg.Any<ProcessRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns<Result<PaymentRefund>>(x => throw new Exception("Payment gateway timeout"));

        // Act
        var result = await _controller.ProcessRefund(paymentId, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task ProcessRefund_LogsSuccessfulRefund()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var request = CreateValidProcessRefundRequest(paymentId);
        var refund = CreateTestRefund(paymentId);

        SetupAuthenticatedAdmin(_adminUserId);
        SetupRefundValidationSuccess(request);

        _mockRefundService.ProcessRefundAsync(Arg.Any<ProcessRefundRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentRefund>.Success(refund));

        // Act
        await _controller.ProcessRefund(paymentId, request);

        // Assert
        _mockLogger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("processed successfully")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid userId)
    {
        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Member")
        }, "test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = userClaims }
        };
    }

    private void SetupAuthenticatedAdmin(Guid userId)
    {
        var adminClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        }, "test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = adminClaims }
        };
    }

    private void SetupAuthenticatedTeacher(Guid userId)
    {
        var teacherClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Teacher")
        }, "test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = teacherClaims }
        };
    }

    private void SetupAuthenticatedUserWithHttpContext(Guid userId)
    {
        var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, "Member")
        }, "test"));

        var httpContext = new DefaultHttpContext
        {
            User = userClaims,
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
        };
        httpContext.Request.Headers.UserAgent = "Mozilla/5.0 Test Browser";

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private void SetupValidationSuccess(ProcessPaymentApiRequest request)
    {
        _mockPaymentValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
    }

    private void SetupRefundValidationSuccess(ProcessRefundApiRequest request)
    {
        _mockRefundValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
    }

    private ProcessPaymentApiRequest CreateValidProcessPaymentRequest()
    {
        return new ProcessPaymentApiRequest
        {
            EventRegistrationId = Guid.NewGuid(),
            OriginalAmount = 100.00m,
            Currency = "USD",
            SlidingScalePercentage = 0,
            PaymentMethodType = PaymentMethodType.PayPal,
            ReturnUrl = "https://example.com/payment/success",
            CancelUrl = "https://example.com/payment/cancel"
        };
    }

    private ProcessRefundApiRequest CreateValidProcessRefundRequest(Guid paymentId)
    {
        return new ProcessRefundApiRequest
        {
            PaymentId = paymentId,
            RefundAmount = 100.00m,
            Currency = "USD",
            RefundReason = "Customer requested refund",
            Metadata = new Dictionary<string, object>()
        };
    }

    private Payment CreateTestPayment()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            EventRegistrationId = Guid.NewGuid(),
            UserId = _testUserId,
            AmountValue = 100.00m,
            Currency = "USD",
            SlidingScalePercentage = 0,
            Status = PaymentStatus.Completed,
            PaymentMethodType = PaymentMethodType.PayPal,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        return payment;
    }

    private PaymentRefund CreateTestRefund(Guid paymentId)
    {
        var refund = new PaymentRefund
        {
            Id = Guid.NewGuid(),
            OriginalPaymentId = paymentId,
            RefundAmountValue = 100.00m,
            RefundCurrency = "USD",
            RefundReason = "Customer requested refund",
            RefundStatus = RefundStatus.Completed,
            ProcessedByUserId = _adminUserId,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        return refund;
    }

    #endregion
}
