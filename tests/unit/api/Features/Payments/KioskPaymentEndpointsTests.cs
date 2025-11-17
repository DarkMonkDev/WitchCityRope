using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.CheckIn.Entities;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.CheckIn.Services;
using WitchCityRope.Api.Features.Payments.Endpoints;
using WitchCityRope.Api.Features.Payments.Entities;
using WitchCityRope.Api.Features.Payments.Services;
using WitchCityRope.Api.Features.Shared.Models;
using Xunit;
using FluentAssertions;
using NSubstitute;

// Aliases to resolve ambiguity
using KioskCashPaymentRequest = WitchCityRope.Api.Features.Payments.Endpoints.CashPaymentRequest;
using KioskCashPaymentResponse = WitchCityRope.Api.Features.Payments.Endpoints.CashPaymentResponse;

namespace WitchCityRope.UnitTests.Api.Features.Payments;

/// <summary>
/// Unit tests for KioskPaymentEndpoints (Pattern B)
/// Tests cash payment recording, health check endpoints
/// Note: SSE stream testing requires integration tests as it's difficult to unit test SSE properly
/// </summary>
public class KioskPaymentEndpointsTests : IDisposable
{
    private readonly IPaymentNotificationService _mockNotificationService;
    private readonly ISessionTokenService _mockSessionTokenService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<KioskPaymentEndpoints> _mockLogger;
    private readonly KioskPaymentEndpoints _sut;

    public KioskPaymentEndpointsTests()
    {
        _mockNotificationService = Substitute.For<IPaymentNotificationService>();
        _mockSessionTokenService = Substitute.For<ISessionTokenService>();
        _mockLogger = Substitute.For<ILogger<KioskPaymentEndpoints>>();

        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _sut = new KioskPaymentEndpoints(
            _mockNotificationService,
            _mockSessionTokenService,
            _dbContext,
            _mockLogger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region RecordCashPayment Tests

    [Fact]
    public async Task RecordCashPayment_WithValidRequest_Returns200OkWithCashPaymentResponse()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var sessionToken = "valid-session-token";
        var amount = 20.00m;

        var request = new KioskCashPaymentRequest
        {
            AttendeeId = attendeeId,
            Amount = amount,
            Notes = "Test payment",
            SessionToken = null
        };

        // Setup test data
        await SetupTestDataAsync(eventId, attendeeId, sessionToken, staffId);

        // Mock session token validation
        var tokenValidationResult = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = staffId
        };
        _mockSessionTokenService.ValidateTokenAsync(sessionToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Success(tokenValidationResult));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = sessionToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<OkObjectResult>();

        var okResult = (OkObjectResult)actionResult.Result!;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeOfType<KioskCashPaymentResponse>();

        var response = (KioskCashPaymentResponse)okResult.Value!;
        response.Success.Should().BeTrue();
        response.PaymentId.Should().NotBeEmpty();
        response.AttendeeId.Should().Be(attendeeId);
        response.EventId.Should().Be(eventId);
        response.Amount.Should().Be(amount);
        response.Currency.Should().Be("USD");
        response.Message.Should().Be("Cash payment recorded");

        // Verify payment was created in database
        var payment = await _dbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == response.PaymentId);
        payment.Should().NotBeNull();
        payment!.AmountValue.Should().Be(amount);
        payment.UserId.Should().Be(attendeeId);
        payment.Status.Should().Be(WitchCityRope.Api.Features.Payments.Models.PaymentStatus.Completed);
        payment.PaymentMethodType.Should().Be(WitchCityRope.Api.Features.Payments.Models.PaymentMethodType.Cash);
        payment.Metadata["recordedBy"].Should().Be(staffId.ToString());
        payment.Metadata["sessionToken"].Should().Be(sessionToken);
        payment.Metadata["paymentSource"].Should().Be("DoorCash");
    }

    [Fact]
    public async Task RecordCashPayment_MissingCheckInTokenHeader_Returns401Unauthorized()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var request = new KioskCashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            Amount = 20.00m
        };

        var httpContext = new DefaultHttpContext();
        // No X-CheckIn-Token header

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<UnauthorizedObjectResult>();

        var unauthorizedResult = (UnauthorizedObjectResult)actionResult.Result!;
        unauthorizedResult.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().Be("Missing X-CheckIn-Token header");
    }

    [Fact]
    public async Task RecordCashPayment_InvalidSessionToken_Returns401Unauthorized()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var invalidToken = "invalid-token";
        var request = new KioskCashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            Amount = 20.00m
        };

        _mockSessionTokenService.ValidateTokenAsync(invalidToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Failure("Token has expired"));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = invalidToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<UnauthorizedObjectResult>();

        var unauthorizedResult = (UnauthorizedObjectResult)actionResult.Result!;
        unauthorizedResult.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().Be("Token has expired");
    }

    [Fact]
    public async Task RecordCashPayment_SessionTokenEventMismatch_Returns400BadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var differentEventId = Guid.NewGuid();
        var sessionToken = "valid-token";
        var request = new KioskCashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            Amount = 20.00m
        };

        var tokenValidationResult = new TokenValidationResult
        {
            EventId = differentEventId, // Different event
            CreatedByStaffId = Guid.NewGuid()
        };
        _mockSessionTokenService.ValidateTokenAsync(sessionToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Success(tokenValidationResult));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = sessionToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();

        var badRequestResult = (BadRequestObjectResult)actionResult.Result!;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Session token is not valid for this event");
    }

    [Fact]
    public async Task RecordCashPayment_AmountLessThanMinimum_Returns400BadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sessionToken = "valid-token";
        var request = new KioskCashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            Amount = 0.00m // Below minimum
        };

        var tokenValidationResult = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = Guid.NewGuid()
        };
        _mockSessionTokenService.ValidateTokenAsync(sessionToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Success(tokenValidationResult));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = sessionToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();

        var badRequestResult = (BadRequestObjectResult)actionResult.Result!;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Amount must be at least $0.01");
    }

    [Fact]
    public async Task RecordCashPayment_AmountExceedsMaximum_Returns400BadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sessionToken = "valid-token";
        var request = new KioskCashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            Amount = 1001.00m // Exceeds maximum
        };

        var tokenValidationResult = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = Guid.NewGuid()
        };
        _mockSessionTokenService.ValidateTokenAsync(sessionToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Success(tokenValidationResult));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = sessionToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();

        var badRequestResult = (BadRequestObjectResult)actionResult.Result!;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Amount cannot exceed $1,000.00");
    }

    [Fact]
    public async Task RecordCashPayment_SessionTokenNotInDatabase_Returns401Unauthorized()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var sessionToken = "valid-token-not-in-db";
        var request = new KioskCashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            Amount = 20.00m
        };

        var tokenValidationResult = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = Guid.NewGuid()
        };
        _mockSessionTokenService.ValidateTokenAsync(sessionToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Success(tokenValidationResult));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = sessionToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<UnauthorizedObjectResult>();

        var unauthorizedResult = (UnauthorizedObjectResult)actionResult.Result!;
        unauthorizedResult.StatusCode.Should().Be(401);
        unauthorizedResult.Value.Should().Be("Invalid session token");
    }

    [Fact]
    public async Task RecordCashPayment_AttendeeNotFound_Returns404NotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var sessionToken = "valid-session-token";

        var request = new KioskCashPaymentRequest
        {
            AttendeeId = attendeeId,
            Amount = 20.00m
        };

        // Only create session token, not the attendee
        var sessionTokenEntity = new CheckInSessionToken
        {
            Id = Guid.NewGuid(),
            Token = sessionToken,
            EventId = eventId,
            CreatedByUserId = staffId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        };
        _dbContext.CheckInSessionTokens.Add(sessionTokenEntity);
        await _dbContext.SaveChangesAsync();

        var tokenValidationResult = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = staffId
        };
        _mockSessionTokenService.ValidateTokenAsync(sessionToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Success(tokenValidationResult));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = sessionToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<NotFoundObjectResult>();

        var notFoundResult = (NotFoundObjectResult)actionResult.Result!;
        notFoundResult.StatusCode.Should().Be(404);
        notFoundResult.Value.Should().Be("Attendee not found or not registered for this event");
    }

    [Fact]
    public async Task RecordCashPayment_AttendeeNotRegisteredForEvent_Returns404NotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var differentEventId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var sessionToken = "valid-session-token";

        var request = new KioskCashPaymentRequest
        {
            AttendeeId = attendeeId,
            Amount = 20.00m
        };

        // Create attendee for DIFFERENT event
        var attendee = new EventAttendee
        {
            Id = Guid.NewGuid(),
            UserId = attendeeId,
            EventId = differentEventId, // Different event
            RegistrationStatus = "confirmed"
        };
        _dbContext.Set<EventAttendee>().Add(attendee);

        // Create session token
        var sessionTokenEntity = new CheckInSessionToken
        {
            Id = Guid.NewGuid(),
            Token = sessionToken,
            EventId = eventId,
            CreatedByUserId = staffId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        };
        _dbContext.CheckInSessionTokens.Add(sessionTokenEntity);
        await _dbContext.SaveChangesAsync();

        var tokenValidationResult = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = staffId
        };
        _mockSessionTokenService.ValidateTokenAsync(sessionToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Success(tokenValidationResult));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = sessionToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<NotFoundObjectResult>();

        var notFoundResult = (NotFoundObjectResult)actionResult.Result!;
        notFoundResult.StatusCode.Should().Be(404);
        notFoundResult.Value.Should().Be("Attendee not found or not registered for this event");
    }

    [Fact]
    public async Task RecordCashPayment_WithSessionTokenInRequest_SendsSSENotification()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var sessionToken = "valid-session-token";
        var kioskSessionToken = "kiosk-session-token";
        var amount = 20.00m;

        var request = new KioskCashPaymentRequest
        {
            AttendeeId = attendeeId,
            Amount = amount,
            SessionToken = kioskSessionToken // This should trigger SSE notification
        };

        await SetupTestDataAsync(eventId, attendeeId, sessionToken, staffId);

        var tokenValidationResult = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = staffId
        };
        _mockSessionTokenService.ValidateTokenAsync(sessionToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Success(tokenValidationResult));

        _mockNotificationService.NotifyPaymentComplete(
            kioskSessionToken,
            attendeeId,
            eventId,
            Arg.Any<Guid>(),
            amount,
            "Cash")
            .Returns(true);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = sessionToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<OkObjectResult>();

        // Verify SSE notification was sent
        await _mockNotificationService.Received(1).NotifyPaymentComplete(
            kioskSessionToken,
            attendeeId,
            eventId,
            Arg.Any<Guid>(),
            amount,
            "Cash");

        // Verify payment metadata includes kiosk session token
        var okResult = (OkObjectResult)actionResult.Result!;
        var response = (KioskCashPaymentResponse)okResult.Value!;
        var payment = await _dbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == response.PaymentId);
        payment!.Metadata["kioskSessionToken"].Should().Be(kioskSessionToken);
    }

    [Fact]
    public async Task RecordCashPayment_WithNotesInRequest_SavesNotesToMetadata()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var attendeeId = Guid.NewGuid();
        var staffId = Guid.NewGuid();
        var sessionToken = "valid-session-token";
        var notes = "Sliding scale discount applied";

        var request = new KioskCashPaymentRequest
        {
            AttendeeId = attendeeId,
            Amount = 15.00m,
            Notes = notes
        };

        await SetupTestDataAsync(eventId, attendeeId, sessionToken, staffId);

        var tokenValidationResult = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = staffId
        };
        _mockSessionTokenService.ValidateTokenAsync(sessionToken, Arg.Any<CancellationToken>())
            .Returns(WitchCityRope.Api.Features.Shared.Models.Result<TokenValidationResult>.Success(tokenValidationResult));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = sessionToken;

        // Act
        var result = await CallRecordCashPayment(eventId, request, httpContext);

        // Assert
        result.Should().BeOfType<ActionResult<KioskCashPaymentResponse>>();
        var actionResult = (ActionResult<KioskCashPaymentResponse>)result;
        actionResult.Result.Should().BeOfType<OkObjectResult>();

        var okResult = (OkObjectResult)actionResult.Result!;
        var response = (KioskCashPaymentResponse)okResult.Value!;
        var payment = await _dbContext.Set<Payment>()
            .FirstOrDefaultAsync(p => p.Id == response.PaymentId);
        payment.Should().NotBeNull();
        payment!.Metadata.Should().ContainKey("notes");
        payment.Metadata["notes"].Should().Be(notes);
    }

    #endregion

    #region HealthCheck Tests

    [Fact]
    public void HealthCheck_ReturnsOkWithHealthStatus()
    {
        // Arrange
        var activeConnections = 5;
        _mockNotificationService.GetActiveConnectionCount().Returns(activeConnections);

        // Act
        var result = _sut.HealthCheck();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        // Verify response contains expected properties
        var value = okResult.Value;
        value.Should().NotBeNull();

        // Use reflection to check anonymous object properties
        var statusProp = value!.GetType().GetProperty("status");
        statusProp.Should().NotBeNull();
        statusProp!.GetValue(value).Should().Be("healthy");

        var serviceProp = value.GetType().GetProperty("service");
        serviceProp.Should().NotBeNull();
        serviceProp!.GetValue(value).Should().Be("kiosk-payments");

        var activeConnectionsProp = value.GetType().GetProperty("activeConnections");
        activeConnectionsProp.Should().NotBeNull();
        activeConnectionsProp!.GetValue(value).Should().Be(activeConnections);

        var timestampProp = value.GetType().GetProperty("timestamp");
        timestampProp.Should().NotBeNull();
        timestampProp!.GetValue(value).Should().NotBeNull();
    }

    [Fact]
    public void HealthCheck_WithZeroConnections_ReturnsHealthyStatus()
    {
        // Arrange
        _mockNotificationService.GetActiveConnectionCount().Returns(0);

        // Act
        var result = _sut.HealthCheck();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        okResult.StatusCode.Should().Be(200);

        var value = okResult.Value!;
        var activeConnectionsProp = value.GetType().GetProperty("activeConnections");
        activeConnectionsProp!.GetValue(value).Should().Be(0);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Sets up test data including session token and event attendee
    /// </summary>
    private async Task SetupTestDataAsync(Guid eventId, Guid attendeeId, string sessionToken, Guid staffId)
    {
        // Create event attendee (registration)
        var attendee = new EventAttendee
        {
            Id = Guid.NewGuid(),
            UserId = attendeeId,
            EventId = eventId,
            RegistrationStatus = "confirmed"
        };
        _dbContext.Set<EventAttendee>().Add(attendee);

        // Create session token entity
        var sessionTokenEntity = new CheckInSessionToken
        {
            Id = Guid.NewGuid(),
            Token = sessionToken,
            EventId = eventId,
            CreatedByUserId = staffId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        };
        _dbContext.CheckInSessionTokens.Add(sessionTokenEntity);

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Simulates calling RecordCashPayment endpoint
    /// </summary>
    private async Task<ActionResult<KioskCashPaymentResponse>> CallRecordCashPayment(
        Guid eventId,
        KioskCashPaymentRequest request,
        HttpContext httpContext)
    {
        // Set controller context
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return await _sut.RecordCashPayment(eventId, request, CancellationToken.None);
    }

    #endregion
}
