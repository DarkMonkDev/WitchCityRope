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
using WitchCityRope.Api.Features.EmailTemplates.Services;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.Fixtures;
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
/// Uses TestContainers PostgreSQL via DatabaseTestFixture (NOT InMemoryDatabase)
/// </summary>
[Collection("Database")]
public class KioskPaymentEndpointsTests : IAsyncLifetime
{
    private readonly DatabaseTestFixture _fixture;
    private readonly IPaymentNotificationService _mockNotificationService;
    private readonly ISessionTokenService _mockSessionTokenService;
    private ApplicationDbContext _dbContext = null!;
    private readonly IEventEmailService _mockEventEmailService;
    private readonly ILogger<KioskPaymentEndpoints> _mockLogger;
    private KioskPaymentEndpoints _sut = null!;

    public KioskPaymentEndpointsTests(DatabaseTestFixture fixture)
    {
        _fixture = fixture;
        _mockNotificationService = Substitute.For<IPaymentNotificationService>();
        _mockSessionTokenService = Substitute.For<ISessionTokenService>();
        _mockEventEmailService = Substitute.For<IEventEmailService>();
        _mockLogger = Substitute.For<ILogger<KioskPaymentEndpoints>>();
    }

    public async Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();

        _sut = new KioskPaymentEndpoints(
            _mockNotificationService,
            _mockSessionTokenService,
            _dbContext,
            _mockEventEmailService,
            _mockLogger);
    }

    public async Task DisposeAsync()
    {
        _dbContext?.Dispose();
        await _fixture.ResetDatabaseAsync();
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
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeOfType<KioskCashPaymentResponse>();

        var response = (KioskCashPaymentResponse)okResult.Value!;
        response.Success.Should().BeTrue();
        response.TicketPurchaseId.Should().NotBeEmpty();
        response.AttendeeId.Should().Be(attendeeId);
        response.EventId.Should().Be(eventId);
        response.Amount.Should().Be(amount);
        response.Currency.Should().Be("USD");
        response.Message.Should().Be("Cash payment recorded");

        // Verify payment was created in database
        var payment = await _dbContext.TicketPurchases
            .FirstOrDefaultAsync(p => p.Id == response.TicketPurchaseId);
        payment.Should().NotBeNull();
        payment!.TotalPrice.Should().Be(amount);
        payment.UserId.Should().Be(attendeeId);
        payment.PaymentStatus.Should().Be(TicketPurchasePaymentStatus.Completed);
        payment.PaymentMethod.Should().Be("Cash");
        payment.RecordedByStaffId.Should().Be(staffId);
        payment.PaymentReference.Should().StartWith("DOOR-");
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
            EventId = differentEventId,
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
            Amount = 0.00m
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
            Amount = 1001.00m
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

        // Create event infrastructure for FK constraints
        await CreateEventInfrastructureAsync(eventId, staffId);

        // Only create session token, not the attendee
        var session = await _dbContext.Sessions.FirstAsync(s => s.EventId == eventId);
        var sessionTokenEntity = new CheckInSessionToken
        {
            Id = Guid.NewGuid(),
            Token = sessionToken,
            EventId = eventId,
            SessionId = session.Id,
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

        // Create event infrastructure for FK constraints
        await CreateEventInfrastructureAsync(eventId, staffId);
        await CreateEventInfrastructureAsync(differentEventId);

        // Create attendee user
        var attendeeUser = new ApplicationUser
        {
            Id = attendeeId,
            Email = $"attendee-{attendeeId:N}@test.com",
            UserName = $"attendee-{attendeeId:N}@test.com",
            SceneName = $"Attendee",
            EmailConfirmed = true,
            Role = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(attendeeUser);

        // Create attendee for DIFFERENT event
        var attendee = new EventAttendee
        {
            Id = Guid.NewGuid(),
            UserId = attendeeId,
            EventId = differentEventId,
            RegistrationStatus = "confirmed"
        };
        _dbContext.Set<EventAttendee>().Add(attendee);

        // Create session token
        var session = await _dbContext.Sessions.FirstAsync(s => s.EventId == eventId);
        var sessionTokenEntity = new CheckInSessionToken
        {
            Id = Guid.NewGuid(),
            Token = sessionToken,
            EventId = eventId,
            SessionId = session.Id,
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
            SessionToken = kioskSessionToken
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

        await _mockNotificationService.Received(1).NotifyPaymentComplete(
            kioskSessionToken,
            attendeeId,
            eventId,
            Arg.Any<Guid>(),
            amount,
            "Cash");

        var okResult = (OkObjectResult)actionResult.Result!;
        var response = (KioskCashPaymentResponse)okResult.Value!;
        var payment = await _dbContext.TicketPurchases
            .FirstOrDefaultAsync(p => p.Id == response.TicketPurchaseId);
        payment.Should().NotBeNull();
        payment!.PaymentMethod.Should().Be("Cash");
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
        var payment = await _dbContext.TicketPurchases
            .FirstOrDefaultAsync(p => p.Id == response.TicketPurchaseId);
        payment.Should().NotBeNull();
        payment!.Notes.Should().Be(notes);
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

        var value = okResult.Value;
        value.Should().NotBeNull();

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

    private async Task SetupTestDataAsync(Guid eventId, Guid attendeeId, string sessionToken, Guid staffId)
    {
        // Create users for FK constraints
        var attendeeUser = new ApplicationUser
        {
            Id = attendeeId,
            Email = $"attendee-{attendeeId:N}@test.com",
            UserName = $"attendee-{attendeeId:N}@test.com",
            SceneName = $"Attendee{attendeeId.ToString()[..8]}",
            EmailConfirmed = true,
            Role = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var staffUser = new ApplicationUser
        {
            Id = staffId,
            Email = $"staff-{staffId:N}@test.com",
            UserName = $"staff-{staffId:N}@test.com",
            SceneName = $"Staff{staffId.ToString()[..8]}",
            EmailConfirmed = true,
            Role = "Administrator",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.AddRange(attendeeUser, staffUser);

        // Create Venue → Event → Session chain for FK constraints
        var venue = new Venue
        {
            Name = $"KioskTestVenue-{Guid.NewGuid():N}"[..30],
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Venues.Add(venue);
        await _dbContext.SaveChangesAsync();

        var testEvent = new Event
        {
            Id = eventId,
            Title = "Kiosk Payment Test Event",
            Description = "Test",
            VenueId = venue.Id,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(3),
            Capacity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Events.Add(testEvent);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            StartTime = testEvent.StartDate,
            EndTime = testEvent.EndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Sessions.Add(session);

        var attendee = new EventAttendee
        {
            Id = Guid.NewGuid(),
            UserId = attendeeId,
            EventId = eventId,
            RegistrationStatus = "confirmed"
        };
        _dbContext.Set<EventAttendee>().Add(attendee);

        var sessionTokenEntity = new CheckInSessionToken
        {
            Id = Guid.NewGuid(),
            Token = sessionToken,
            EventId = eventId,
            SessionId = session.Id,
            CreatedByUserId = staffId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        };
        _dbContext.CheckInSessionTokens.Add(sessionTokenEntity);

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Creates minimum event infrastructure (Venue → Event → Session) for tests that don't need full attendee setup
    /// </summary>
    private async Task CreateEventInfrastructureAsync(Guid eventId, Guid? staffId = null)
    {
        // Create staff user if provided
        if (staffId.HasValue)
        {
            var existingUser = await _dbContext.Users.FindAsync(staffId.Value);
            if (existingUser == null)
            {
                var staff = new ApplicationUser
                {
                    Id = staffId.Value,
                    Email = $"staff-{staffId.Value:N}@test.com",
                    UserName = $"staff-{staffId.Value:N}@test.com",
                    SceneName = $"Staff{staffId.Value.ToString()[..8]}",
                    EmailConfirmed = true,
                    Role = "Administrator",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Users.Add(staff);
            }
        }

        var venue = new Venue
        {
            Name = $"Venue-{Guid.NewGuid():N}"[..30],
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Venues.Add(venue);
        await _dbContext.SaveChangesAsync();

        var testEvent = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test",
            VenueId = venue.Id,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(1).AddHours(3),
            Capacity = 50,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Events.Add(testEvent);

        var session = new Session
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            StartTime = testEvent.StartDate,
            EndTime = testEvent.EndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<ActionResult<KioskCashPaymentResponse>> CallRecordCashPayment(
        Guid eventId,
        KioskCashPaymentRequest request,
        HttpContext httpContext)
    {
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return await _sut.RecordCashPayment(eventId, request, CancellationToken.None);
    }

    #endregion
}
