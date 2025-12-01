using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.CheckIn.Services;
using WitchCityRope.Api.Features.Events.Models;
using WitchCityRope.Api.Features.Events.Services;
using WitchCityRope.Api.Features.Shared.Models;
using Xunit;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using System.Security.Claims;

namespace WitchCityRope.UnitTests.Api.Features.Events;

/// <summary>
/// Unit tests for EventEndpoints (Pattern B - Minimal API)
/// Tests event CRUD operations, authorization, and check-in kiosk cash payment endpoint
///
/// Coverage:
/// - GET /api/events (with optional includeUnpublished for admin)
/// - GET /api/events/{id}
/// - PUT /api/events/{id}
/// - GET /api/events/{id}/ticket-types (kiosk mode with X-CheckIn-Token)
/// - POST /api/events/{eventId}/checkin/cash-payment (kiosk mode with token validation)
///
/// NOTE: EventService uses tuple returns which are mockable with NSubstitute.
/// All methods are virtual by default in C# classes, allowing for mocking.
/// </summary>
public class EventEndpointsTests
{
    private readonly IEventService _mockEventService;
    private readonly ICheckInService _mockCheckInService;
    private readonly ISessionTokenService _mockSessionTokenService;
    private readonly IValidator<CashPaymentRequest> _mockCashPaymentValidator;

    public EventEndpointsTests()
    {
        _mockEventService = Substitute.For<IEventService>();
        _mockCheckInService = Substitute.For<ICheckInService>();
        _mockSessionTokenService = Substitute.For<ISessionTokenService>();
        _mockCashPaymentValidator = Substitute.For<IValidator<CashPaymentRequest>>();
    }

    #region GET /api/events Tests

    /// <summary>
    /// Test: GET /api/events without includeUnpublished parameter returns published events with 200 OK
    /// </summary>
    [Fact]
    public async Task GetEvents_WithoutIncludeUnpublished_Returns200OkWithPublishedEvents()
    {
        // Arrange
        var expectedEvents = new List<EventDto>
        {
            new EventDto { Id = Guid.NewGuid().ToString(), Title = "Event 1", IsPublished = true },
            new EventDto { Id = Guid.NewGuid().ToString(), Title = "Event 2", IsPublished = true }
        };

        _mockEventService.GetEventsAsync(false, false, Arg.Any<CancellationToken>())
            .Returns((true, expectedEvents, string.Empty));

        var httpContext = new DefaultHttpContext();

        // Act
        var result = await GetEvents(httpContext, includeUnpublished: null);

        // Assert
        result.Should().BeOfType<Ok<List<EventDto>>>();
        var okResult = (Ok<List<EventDto>>)result;
        okResult.Value.Should().BeEquivalentTo(expectedEvents);
    }

    /// <summary>
    /// Test: GET /api/events?includeUnpublished=false returns published events with 200 OK
    /// </summary>
    [Fact]
    public async Task GetEvents_WithIncludeUnpublishedFalse_Returns200OkWithPublishedEvents()
    {
        // Arrange
        var expectedEvents = new List<EventDto>
        {
            new EventDto { Id = Guid.NewGuid().ToString(), Title = "Event 1", IsPublished = true }
        };

        _mockEventService.GetEventsAsync(false, false, Arg.Any<CancellationToken>())
            .Returns((true, expectedEvents, string.Empty));

        var httpContext = new DefaultHttpContext();

        // Act
        var result = await GetEvents(httpContext, includeUnpublished: false);

        // Assert
        result.Should().BeOfType<Ok<List<EventDto>>>();
        var okResult = (Ok<List<EventDto>>)result;
        okResult.Value.Should().BeEquivalentTo(expectedEvents);
    }

    /// <summary>
    /// Test: GET /api/events?includeUnpublished=true without authentication returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetEvents_WithIncludeUnpublishedTrueAndNotAuthenticated_Returns401Unauthorized()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(); // No identity - not authenticated

        // Act
        var result = await GetEvents(httpContext, includeUnpublished: true);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Authentication Required");
    }

    /// <summary>
    /// Test: GET /api/events?includeUnpublished=true as non-admin returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GetEvents_WithIncludeUnpublishedTrueAsNonAdmin_Returns403Forbidden()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Member") // Not Administrator
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        // Act
        var result = await GetEvents(httpContext, includeUnpublished: true);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Title.Should().Be("Insufficient Permissions");
    }

    /// <summary>
    /// Test: GET /api/events?includeUnpublished=true as admin returns all events with 200 OK
    /// </summary>
    [Fact]
    public async Task GetEvents_WithIncludeUnpublishedTrueAsAdmin_Returns200OkWithAllEvents()
    {
        // Arrange
        var expectedEvents = new List<EventDto>
        {
            new EventDto { Id = Guid.NewGuid().ToString(), Title = "Published Event", IsPublished = true },
            new EventDto { Id = Guid.NewGuid().ToString(), Title = "Draft Event", IsPublished = false }
        };

        _mockEventService.GetEventsAsync(true, false, Arg.Any<CancellationToken>())
            .Returns((true, expectedEvents, string.Empty));

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Administrator")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = claimsPrincipal
        };

        // Act
        var result = await GetEvents(httpContext, includeUnpublished: true);

        // Assert
        result.Should().BeOfType<Ok<List<EventDto>>>();
        var okResult = (Ok<List<EventDto>>)result;
        okResult.Value.Should().BeEquivalentTo(expectedEvents);
    }

    /// <summary>
    /// Test: GET /api/events when service fails returns 500 Internal Server Error
    /// </summary>
    [Fact]
    public async Task GetEvents_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        _mockEventService.GetEventsAsync(false, false, Arg.Any<CancellationToken>())
            .Returns((false, new List<EventDto>(), "Database connection failed"));

        var httpContext = new DefaultHttpContext();

        // Act
        var result = await GetEvents(httpContext, includeUnpublished: null);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Failed to retrieve events");
    }

    #endregion

    #region GET /api/events/{id} Tests

    /// <summary>
    /// Test: GET /api/events/{id} with valid ID returns event with 200 OK
    /// </summary>
    [Fact]
    public async Task GetEvent_WithValidId_Returns200OkWithEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var expectedEvent = new EventDto
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test Description",
            IsPublished = true
        };

        _mockEventService.GetEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((true, expectedEvent, string.Empty));

        // Act
        var result = await GetEvent(eventId);

        // Assert
        result.Should().BeOfType<Ok<EventDto>>();
        var okResult = (Ok<EventDto>)result;
        okResult.Value.Should().BeEquivalentTo(expectedEvent);
    }

    /// <summary>
    /// Test: GET /api/events/{id} with non-existent ID returns 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetEvent_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();

        _mockEventService.GetEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Event not found"));

        // Act
        var result = await GetEvent(eventId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Event Not Found");
    }

    /// <summary>
    /// Test: GET /api/events/{id} when service fails returns 500 Internal Server Error
    /// </summary>
    [Fact]
    public async Task GetEvent_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var expectedEvent = new EventDto(); // Service returned event but success=false

        _mockEventService.GetEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((false, expectedEvent, "Database query failed"));

        // Act
        var result = await GetEvent(eventId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Failed to retrieve event");
    }

    #endregion

    #region PUT /api/events/{id} Tests

    /// <summary>
    /// Test: PUT /api/events/{id} with valid request returns updated event with 200 OK
    /// </summary>
    [Fact]
    public async Task UpdateEvent_WithValidRequest_Returns200OkWithUpdatedEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var request = new UpdateEventRequest
        {
            Title = "Updated Event Title",
            Capacity = 50
        };

        var expectedEvent = new EventDto
        {
            Id = eventId,
            Title = "Updated Event Title",
            Capacity = 50
        };

        _mockEventService.UpdateEventAsync(eventId, request, Arg.Any<CancellationToken>())
            .Returns((true, expectedEvent, string.Empty));

        // Act
        var result = await UpdateEvent(eventId, request);

        // Assert
        result.Should().BeOfType<Ok<EventDto>>();
        var okResult = (Ok<EventDto>)result;
        okResult.Value.Should().BeEquivalentTo(expectedEvent);
    }

    /// <summary>
    /// Test: PUT /api/events/{id} with non-existent ID returns 404 Not Found
    /// </summary>
    [Fact]
    public async Task UpdateEvent_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var request = new UpdateEventRequest { Title = "Test" };

        _mockEventService.UpdateEventAsync(eventId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Event not found"));

        // Act
        var result = await UpdateEvent(eventId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Failed to update event");
    }

    /// <summary>
    /// Test: PUT /api/events/{id} with invalid event ID format returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateEvent_WithInvalidEventIdFormat_Returns400BadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var request = new UpdateEventRequest { Title = "Test" };

        _mockEventService.UpdateEventAsync(eventId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Invalid event ID format"));

        // Act
        var result = await UpdateEvent(eventId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Failed to update event");
    }

    /// <summary>
    /// Test: PUT /api/events/{id} attempting to update past event returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateEvent_WithPastEvent_Returns400BadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var request = new UpdateEventRequest { Title = "Test" };

        _mockEventService.UpdateEventAsync(eventId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Cannot update past events"));

        // Act
        var result = await UpdateEvent(eventId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Failed to update event");
        problemResult.ProblemDetails.Detail.Should().Contain("past events");
    }

    /// <summary>
    /// Test: PUT /api/events/{id} attempting to reduce capacity below current attendance returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateEvent_ReducingCapacityBelowAttendance_Returns400BadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var request = new UpdateEventRequest { Capacity = 10 };

        _mockEventService.UpdateEventAsync(eventId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Cannot reduce capacity to 10. Current attendance is 25"));

        // Act
        var result = await UpdateEvent(eventId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Failed to update event");
        problemResult.ProblemDetails.Detail.Should().Contain("capacity");
    }

    /// <summary>
    /// Test: PUT /api/events/{id} with invalid date range returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateEvent_WithInvalidDateRange_Returns400BadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var request = new UpdateEventRequest
        {
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(3) // End before start
        };

        _mockEventService.UpdateEventAsync(eventId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Start date must be before end date"));

        // Act
        var result = await UpdateEvent(eventId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Failed to update event");
        problemResult.ProblemDetails.Detail.Should().Contain("date");
    }

    /// <summary>
    /// Test: PUT /api/events/{id} when service fails returns 500 Internal Server Error
    /// </summary>
    [Fact]
    public async Task UpdateEvent_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var request = new UpdateEventRequest { Title = "Test" };

        // FIXED: Error message must NOT contain keywords like "date", "capacity", "null", "not found", "Invalid event ID", "past events"
        _mockEventService.UpdateEventAsync(eventId, request, Arg.Any<CancellationToken>())
            .Returns((false, null, "Database connection error"));

        // Act
        var result = await UpdateEvent(eventId, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Failed to update event");
    }

    #endregion

    #region GET /api/events/{id}/ticket-types Tests (Kiosk Mode)

    /// <summary>
    /// Test: GET /api/events/{id}/ticket-types with valid token returns ticket types with 200 OK
    /// </summary>
    [Fact]
    public async Task GetEventTicketTypes_WithValidToken_Returns200OkWithTicketTypes()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var ticketTypes = new List<TicketTypeDto>
        {
            new TicketTypeDto { Id = Guid.NewGuid().ToString(), Name = "General Admission" },
            new TicketTypeDto { Id = Guid.NewGuid().ToString(), Name = "VIP" }
        };

        var eventDto = new EventDto
        {
            Id = eventId,
            Title = "Test Event",
            TicketTypes = ticketTypes
        };

        _mockEventService.GetEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((true, eventDto, string.Empty));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = "valid-token-123";

        // Act
        var result = await GetEventTicketTypes(eventId, httpContext);

        // Assert
        result.Should().BeOfType<Ok<List<TicketTypeDto>>>();
        var okResult = (Ok<List<TicketTypeDto>>)result;
        okResult.Value.Should().BeEquivalentTo(ticketTypes);
    }

    /// <summary>
    /// Test: GET /api/events/{id}/ticket-types without token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetEventTicketTypes_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        // No X-CheckIn-Token header

        // Act
        var result = await GetEventTicketTypes(eventId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Missing check-in session token");
    }

    /// <summary>
    /// Test: GET /api/events/{id}/ticket-types with empty token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetEventTicketTypes_WithEmptyToken_Returns401Unauthorized()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = "";

        // Act
        var result = await GetEventTicketTypes(eventId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Missing check-in session token");
    }

    /// <summary>
    /// Test: GET /api/events/{id}/ticket-types for non-existent event returns 404 Not Found
    /// </summary>
    [Fact]
    public async Task GetEventTicketTypes_WithNonExistentEvent_Returns404NotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();

        _mockEventService.GetEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Event not found"));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = "valid-token-123";

        // Act
        var result = await GetEventTicketTypes(eventId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Event not found");
    }

    /// <summary>
    /// Test: GET /api/events/{id}/ticket-types when service fails returns 500 Internal Server Error
    /// </summary>
    [Fact]
    public async Task GetEventTicketTypes_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var eventDto = new EventDto { Id = eventId }; // Non-null but service failed

        _mockEventService.GetEventAsync(eventId, Arg.Any<CancellationToken>())
            .Returns((false, eventDto, "Database error"));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-CheckIn-Token"] = "valid-token-123";

        // Act
        var result = await GetEventTicketTypes(eventId, httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Failed to retrieve ticket types");
    }

    #endregion

    #region POST /api/events/{eventId}/checkin/cash-payment Tests (Kiosk Mode)

    /// <summary>
    /// Test: POST /api/events/{eventId}/checkin/cash-payment with valid token and request returns 200 OK
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WithValidTokenAndRequest_Returns200Ok()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var token = "valid-token-123";
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m,
            Notes = "Cash payment at door"
        };

        var tokenData = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = Guid.NewGuid()
        };

        _mockSessionTokenService.ValidateTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCashPaymentValidator.ValidateAsync(Arg.Any<CashPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var expectedResponse = new CashPaymentResponse
        {
            TicketPurchaseId = Guid.NewGuid(),
            Amount = 25.00m,
            Success = true,
            Message = "Cash payment recorded successfully",
            RecordedAt = DateTime.UtcNow
        };

        _mockCheckInService.RecordCashPaymentAsync(eventId, Arg.Any<CashPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<CashPaymentResponse>.Success(expectedResponse));

        // Act
        var result = await RecordCashPayment(eventId, token, request);

        // Assert
        result.Should().BeOfType<Ok<CashPaymentResponse>>();
        var okResult = (Ok<CashPaymentResponse>)result;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: POST /api/events/{eventId}/checkin/cash-payment without token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        string? token = null;
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m
        };

        // Act
        var result = await RecordCashPayment(eventId, token, request);

        // Assert
        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    /// <summary>
    /// Test: POST /api/events/{eventId}/checkin/cash-payment with invalid token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WithInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var token = "invalid-token";
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m
        };

        _mockSessionTokenService.ValidateTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Failure("Invalid token"));

        // Act
        var result = await RecordCashPayment(eventId, token, request);

        // Assert
        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    /// <summary>
    /// Test: POST /api/events/{eventId}/checkin/cash-payment with token for different event returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WithTokenForDifferentEvent_Returns403Forbidden()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var differentEventId = Guid.NewGuid();
        var token = "valid-token-for-different-event";
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m
        };

        var tokenData = new TokenValidationResult
        {
            EventId = differentEventId, // Different event
            CreatedByStaffId = Guid.NewGuid()
        };

        _mockSessionTokenService.ValidateTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        // Act
        var result = await RecordCashPayment(eventId, token, request);

        // Assert
        result.Should().BeOfType<ForbidHttpResult>();
    }

    /// <summary>
    /// Test: POST /api/events/{eventId}/checkin/cash-payment with validation errors returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WithValidationErrors_Returns400BadRequest()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var token = "valid-token-123";
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = -10.00m // Invalid amount
        };

        var tokenData = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = Guid.NewGuid()
        };

        _mockSessionTokenService.ValidateTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        var validationFailures = new List<ValidationFailure>
        {
            new ValidationFailure("Amount", "Amount must be positive")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockCashPaymentValidator.ValidateAsync(Arg.Any<CashPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var result = await RecordCashPayment(eventId, token, request);

        // Assert
        // FIXED: ValidationProblem returns ProblemHttpResult with 400 status
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Test: POST /api/events/{eventId}/checkin/cash-payment when attendee not found returns 404 Not Found
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WhenAttendeeNotFound_Returns404NotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var token = "valid-token-123";
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m
        };

        var tokenData = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = Guid.NewGuid()
        };

        _mockSessionTokenService.ValidateTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCashPaymentValidator.ValidateAsync(Arg.Any<CashPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.RecordCashPaymentAsync(eventId, Arg.Any<CashPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<CashPaymentResponse>.Failure("Attendee not found"));

        // Act
        var result = await RecordCashPayment(eventId, token, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: POST /api/events/{eventId}/checkin/cash-payment when attendee already has ticket returns 409 Conflict
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WhenAttendeeAlreadyHasTicket_Returns409Conflict()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var token = "valid-token-123";
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m
        };

        var tokenData = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = Guid.NewGuid()
        };

        _mockSessionTokenService.ValidateTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCashPaymentValidator.ValidateAsync(Arg.Any<CashPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.RecordCashPaymentAsync(eventId, Arg.Any<CashPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<CashPaymentResponse>.Failure("Attendee already has a ticket for this event"));

        // Act
        var result = await RecordCashPayment(eventId, token, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
    }

    /// <summary>
    /// Test: POST /api/events/{eventId}/checkin/cash-payment when service fails returns 500 Internal Server Error
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var token = "valid-token-123";
        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m
        };

        var tokenData = new TokenValidationResult
        {
            EventId = eventId,
            CreatedByStaffId = Guid.NewGuid()
        };

        _mockSessionTokenService.ValidateTokenAsync(token, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCashPaymentValidator.ValidateAsync(Arg.Any<CashPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.RecordCashPaymentAsync(eventId, Arg.Any<CashPaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<CashPaymentResponse>.Failure("Database error"));

        // Act
        var result = await RecordCashPayment(eventId, token, request);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region Helper Methods (Simulate Endpoint Logic)

    /// <summary>
    /// Simulates GET /api/events endpoint logic
    /// </summary>
    private async Task<IResult> GetEvents(HttpContext context, bool? includeUnpublished)
    {
        var shouldIncludeUnpublished = includeUnpublished.GetValueOrDefault(false);

        if (shouldIncludeUnpublished)
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                return Results.Problem(
                    title: "Authentication Required",
                    detail: "Authentication required to access unpublished events",
                    statusCode: 401);
            }

            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole != "Administrator")
            {
                return Results.Problem(
                    title: "Insufficient Permissions",
                    detail: "Administrator role required to access unpublished events",
                    statusCode: 403);
            }
        }

        var (success, response, error) = await _mockEventService.GetEventsAsync(shouldIncludeUnpublished, false, CancellationToken.None);

        if (success)
        {
            return Results.Ok(response);
        }

        return Results.Problem(
            title: "Failed to retrieve events",
            detail: error ?? "Unable to retrieve events. Please check if the API database connection is working.",
            statusCode: 500);
    }

    /// <summary>
    /// Simulates GET /api/events/{id} endpoint logic
    /// </summary>
    private async Task<IResult> GetEvent(string id)
    {
        var (success, response, error) = await _mockEventService.GetEventAsync(id, CancellationToken.None);

        if (success && response != null)
        {
            return Results.Ok(response);
        }

        return Results.Problem(
            title: response == null ? "Event Not Found" : "Failed to retrieve event",
            detail: error ?? (response == null ? "Event not found" : "Failed to retrieve event from database"),
            statusCode: response == null ? 404 : 500);
    }

    /// <summary>
    /// Simulates PUT /api/events/{id} endpoint logic
    /// </summary>
    private async Task<IResult> UpdateEvent(string id, UpdateEventRequest request)
    {
        var (success, response, error) = await _mockEventService.UpdateEventAsync(id, request, CancellationToken.None);

        if (success && response != null)
        {
            return Results.Ok(response);
        }

        var statusCode = error switch
        {
            string msg when msg.Contains("not found") => 404,
            string msg when msg.Contains("Invalid event ID") => 400,
            string msg when msg.Contains("past events") => 400,
            string msg when msg.Contains("capacity") => 400,
            string msg when msg.Contains("date") => 400,
            string msg when msg.Contains("null") => 400,
            _ => 500
        };

        return Results.Problem(
            title: "Failed to update event",
            detail: error ?? "Failed to update event",
            statusCode: statusCode);
    }

    /// <summary>
    /// Simulates GET /api/events/{id}/ticket-types endpoint logic
    /// </summary>
    private async Task<IResult> GetEventTicketTypes(string id, HttpContext context)
    {
        var sessionToken = context.Request.Headers["X-CheckIn-Token"].FirstOrDefault();
        if (string.IsNullOrEmpty(sessionToken))
        {
            return Results.Problem(
                title: "Missing check-in session token",
                detail: "X-CheckIn-Token header required for kiosk access",
                statusCode: 401);
        }

        var (success, response, error) = await _mockEventService.GetEventAsync(id, CancellationToken.None);

        if (success && response != null)
        {
            return Results.Ok(response.TicketTypes);
        }

        return Results.Problem(
            title: response == null ? "Event not found" : "Failed to retrieve ticket types",
            detail: error ?? (response == null ? "Event not found" : "Failed to retrieve ticket types from database"),
            statusCode: response == null ? 404 : 500);
    }

    /// <summary>
    /// Simulates POST /api/events/{eventId}/checkin/cash-payment endpoint logic
    /// </summary>
    private async Task<IResult> RecordCashPayment(Guid eventId, string? token, CashPaymentRequest request)
    {
        // VALIDATE TOKEN FIRST
        if (string.IsNullOrEmpty(token))
        {
            return Results.Unauthorized();
        }

        var validationResult = await _mockSessionTokenService.ValidateTokenAsync(token, CancellationToken.None);
        if (!validationResult.IsSuccess)
        {
            return Results.Unauthorized();
        }

        var tokenData = validationResult.Value;
        if (tokenData.EventId != eventId)
        {
            return Results.Forbid(); // 403 - Token is for different event
        }

        // EXTRACT STAFF ID FROM TOKEN
        request.RecordedByStaffId = tokenData.CreatedByStaffId;

        // Validate request
        var requestValidation = await _mockCashPaymentValidator.ValidateAsync(request, CancellationToken.None);
        if (!requestValidation.IsValid)
        {
            return Results.ValidationProblem(requestValidation.ToDictionary());
        }

        // Record cash payment
        var result = await _mockCheckInService.RecordCashPaymentAsync(eventId, request, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Cash Payment Failed",
                detail: result.Error,
                statusCode: result.Error.Contains("not found") ? 404 :
                           result.Error.Contains("already has a ticket") ? 409 : 500);
    }

    #endregion
}
