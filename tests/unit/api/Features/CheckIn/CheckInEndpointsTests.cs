using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;
using WitchCityRope.Api.Features.CheckIn.Models;
using WitchCityRope.Api.Features.CheckIn.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Users.Constants;
using Xunit;

namespace WitchCityRope.UnitTests.Api.Features.CheckIn;

/// <summary>
/// Unit tests for CheckInEndpoints (Pattern B - Minimal API)
/// Tests token-based authentication, check-in operations, session token management
/// </summary>
public class CheckInEndpointsTests
{
    private readonly ICheckInService _mockCheckInService;
    private readonly ISessionTokenService _mockSessionTokenService;
    private readonly ISyncService _mockSyncService;
    private readonly IValidator<CheckInRequest> _mockCheckInValidator;
    private readonly IValidator<SyncRequest> _mockSyncValidator;
    private readonly IValidator<CashPaymentRequest> _mockCashPaymentValidator;
    private readonly IValidator<ManualEntryData> _mockManualEntryValidator;
    private readonly ILogger<Program> _mockLogger;

    private const string ValidToken = "valid-session-token-123";
    private const string InvalidToken = "invalid-token";
    private readonly Guid _testEventId = Guid.NewGuid();
    private readonly Guid _adminUserId = Guid.NewGuid();

    public CheckInEndpointsTests()
    {
        _mockCheckInService = Substitute.For<ICheckInService>();
        _mockSessionTokenService = Substitute.For<ISessionTokenService>();
        _mockSyncService = Substitute.For<ISyncService>();
        _mockCheckInValidator = Substitute.For<IValidator<CheckInRequest>>();
        _mockSyncValidator = Substitute.For<IValidator<SyncRequest>>();
        _mockCashPaymentValidator = Substitute.For<IValidator<CashPaymentRequest>>();
        _mockManualEntryValidator = Substitute.For<IValidator<ManualEntryData>>();
        _mockLogger = Substitute.For<ILogger<Program>>();
    }

    #region GetEventAttendees Tests

    /// <summary>
    /// Test: GetEventAttendees with valid token and event ID returns 200 OK with attendees list
    /// </summary>
    [Fact]
    public async Task GetEventAttendees_WithValidToken_Returns200OkWithAttendees()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var expectedResponse = new CheckInAttendeesResponse
        {
            EventId = _testEventId.ToString(),
            EventTitle = "Test Event",
            Attendees = new List<AttendeeResponse>
            {
                new() { AttendeeId = Guid.NewGuid().ToString(), SceneName = "Test User" }
            }
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCheckInService.GetEventAttendeesAsync(_testEventId, Arg.Any<Guid>(), null, null, 1, 50, Arg.Any<CancellationToken>())
            .Returns(Result<CheckInAttendeesResponse>.Success(expectedResponse));

        // Act
        var result = await GetEventAttendees(_testEventId, ValidToken);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<CheckInAttendeesResponse>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: GetEventAttendees with missing token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetEventAttendees_WithMissingToken_Returns401Unauthorized()
    {
        // Act
        var result = await GetEventAttendees(_testEventId, null);

        // Assert
        result.Should().BeAssignableTo<UnauthorizedHttpResult>();
    }

    /// <summary>
    /// Test: GetEventAttendees with invalid token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetEventAttendees_WithInvalidToken_Returns401Unauthorized()
    {
        // Arrange
        _mockSessionTokenService.ValidateTokenAsync(InvalidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Failure("Invalid token"));

        // Act
        var result = await GetEventAttendees(_testEventId, InvalidToken);

        // Assert
        result.Should().BeAssignableTo<UnauthorizedHttpResult>();
    }

    /// <summary>
    /// Test: GetEventAttendees with token for different event returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GetEventAttendees_WithTokenForDifferentEvent_Returns403Forbidden()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = Guid.NewGuid(), // Different event ID
            CreatedByStaffId = Guid.NewGuid(),
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        // Act
        var result = await GetEventAttendees(_testEventId, ValidToken);

        // Assert
        result.Should().BeAssignableTo<ForbidHttpResult>();
    }

    /// <summary>
    /// Test: GetEventAttendees when service fails returns 500 InternalServerError with Problem Details
    /// </summary>
    [Fact]
    public async Task GetEventAttendees_WhenServiceFails_Returns500WithProblemDetails()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCheckInService.GetEventAttendeesAsync(_testEventId, Arg.Any<Guid>(), null, null, 1, 50, Arg.Any<CancellationToken>())
            .Returns(Result<CheckInAttendeesResponse>.Failure("Database connection failed"));

        // Act
        var result = await GetEventAttendees(_testEventId, ValidToken);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Get Attendees Failed");
        problemResult.ProblemDetails.Detail.Should().Be("Database connection failed");
    }

    #endregion

    #region ProcessCheckIn Tests

    /// <summary>
    /// Test: ProcessCheckIn with valid request returns 200 OK with CheckInResponse
    /// </summary>
    [Fact]
    public async Task ProcessCheckIn_WithValidRequest_Returns200OkWithCheckInResponse()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var request = new CheckInRequest
        {
            AttendeeId = Guid.NewGuid().ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O")
        };

        var expectedResponse = new CheckInResponse
        {
            Success = true,
            AttendeeId = request.AttendeeId,
            Message = "Check-in successful"
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCheckInValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.CheckInAttendeeAsync(request, ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<CheckInResponse>.Success(expectedResponse));

        // Act
        var result = await ProcessCheckIn(_testEventId, ValidToken, request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<CheckInResponse>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: ProcessCheckIn with missing token returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task ProcessCheckIn_WithMissingToken_Returns401Unauthorized()
    {
        // Arrange
        var request = new CheckInRequest
        {
            AttendeeId = Guid.NewGuid().ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O")
        };

        // Act
        var result = await ProcessCheckIn(_testEventId, null, request);

        // Assert
        result.Should().BeAssignableTo<UnauthorizedHttpResult>();
    }

    /// <summary>
    /// Test: ProcessCheckIn with validation failure returns 400 BadRequest with validation errors
    /// </summary>
    [Fact]
    public async Task ProcessCheckIn_WithValidationFailure_Returns400ValidationProblem()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var request = new CheckInRequest
        {
            AttendeeId = string.Empty, // Invalid
            CheckInTime = DateTime.UtcNow.ToString("O")
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("AttendeeId", "AttendeeId is required")
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCheckInValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await ProcessCheckIn(_testEventId, ValidToken, request);

        // Assert
        // FIXED: ValidationProblem returns ProblemHttpResult with 400 status
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Test: ProcessCheckIn with attendee not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task ProcessCheckIn_WithAttendeeNotFound_Returns404NotFound()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var request = new CheckInRequest
        {
            AttendeeId = Guid.NewGuid().ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O")
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCheckInValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.CheckInAttendeeAsync(request, ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<CheckInResponse>.Failure("Attendee not found"));

        // Act
        var result = await ProcessCheckIn(_testEventId, ValidToken, request);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: ProcessCheckIn with capacity exceeded returns 409 Conflict
    /// </summary>
    [Fact]
    public async Task ProcessCheckIn_WithCapacityExceeded_Returns409Conflict()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var request = new CheckInRequest
        {
            AttendeeId = Guid.NewGuid().ToString(),
            CheckInTime = DateTime.UtcNow.ToString("O")
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCheckInValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.CheckInAttendeeAsync(request, ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<CheckInResponse>.Failure("Event is at capacity"));

        // Act
        var result = await ProcessCheckIn(_testEventId, ValidToken, request);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(409);
        problemResult.ProblemDetails.Detail.Should().Contain("capacity");
    }

    #endregion

    #region GetEventDashboard Tests

    /// <summary>
    /// Test: GetEventDashboard with valid token returns 200 OK with dashboard data
    /// </summary>
    [Fact]
    public async Task GetEventDashboard_WithValidToken_Returns200OkWithDashboard()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var expectedResponse = new DashboardResponse
        {
            EventId = _testEventId.ToString(),
            EventTitle = "Test Event",
            Capacity = new CapacityInfo { TotalCapacity = 50, CheckedInCount = 25 }
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCheckInService.GetEventDashboardAsync(_testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<DashboardResponse>.Success(expectedResponse));

        // Act
        var result = await GetEventDashboard(_testEventId, ValidToken);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<DashboardResponse>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: GetEventDashboard with event not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetEventDashboard_WithEventNotFound_Returns404NotFound()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCheckInService.GetEventDashboardAsync(_testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<DashboardResponse>.Failure("Event not found"));

        // Act
        var result = await GetEventDashboard(_testEventId, ValidToken);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(404);
    }

    #endregion

    #region SyncOfflineCheckIns Tests

    /// <summary>
    /// Test: SyncOfflineCheckIns with valid request returns 200 OK with sync response
    /// </summary>
    [Fact]
    public async Task SyncOfflineCheckIns_WithValidRequest_Returns200OkWithSyncResponse()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var request = new SyncRequest
        {
            DeviceId = "test-device",
            PendingCheckIns = new List<PendingCheckIn>(),
            LastSyncTimestamp = DateTime.UtcNow.AddHours(-1).ToString("O")
        };

        var expectedResponse = new SyncResponse
        {
            ProcessedCount = 5,
            Success = true
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockSyncValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockSyncService.ProcessOfflineSyncAsync(request, ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<SyncResponse>.Success(expectedResponse));

        // Act
        var result = await SyncOfflineCheckIns(_testEventId, ValidToken, request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<SyncResponse>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: SyncOfflineCheckIns with validation failure returns 400 ValidationProblem
    /// </summary>
    [Fact]
    public async Task SyncOfflineCheckIns_WithValidationFailure_Returns400ValidationProblem()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var request = new SyncRequest
        {
            DeviceId = "test-device",
            PendingCheckIns = new List<PendingCheckIn>(),
            LastSyncTimestamp = DateTime.UtcNow.AddHours(-1).ToString("O")
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("DeviceId", "DeviceId is required")
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockSyncValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(validationFailures));

        // Act
        var result = await SyncOfflineCheckIns(_testEventId, ValidToken, request);

        // Assert
        // FIXED: ValidationProblem returns ProblemHttpResult with 400 status
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
    }

    #endregion

    #region RecordCashPayment Tests

    /// <summary>
    /// Test: RecordCashPayment with valid request returns 200 OK with payment response
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WithValidRequest_Returns200OkWithPaymentResponse()
    {
        // Arrange
        var staffId = Guid.NewGuid();
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId, // FIXED: Must match eventId for authorization
            CreatedByStaffId = staffId,
        };

        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m,
            RecordedByStaffId = staffId
        };

        var expectedResponse = new CashPaymentResponse
        {
            Success = true,
            TicketPurchaseId = Guid.NewGuid(),
            Message = "Cash payment recorded"
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCashPaymentValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.RecordCashPaymentAsync(_testEventId, request, Arg.Any<CancellationToken>())
            .Returns(Result<CashPaymentResponse>.Success(expectedResponse));

        // Act
        var result = await RecordCashPayment(_testEventId, ValidToken, request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<CashPaymentResponse>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: RecordCashPayment extracts staff ID from token and sets on request
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_ExtractsStaffIdFromToken_SetsOnRequest()
    {
        // Arrange
        var staffId = Guid.NewGuid();
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId, // FIXED: Must match eventId for authorization
            CreatedByStaffId = staffId,
        };

        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m
            // RecordedByStaffId NOT set - should be populated from token
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCashPaymentValidator.ValidateAsync(
            Arg.Is<CashPaymentRequest>(r => r.RecordedByStaffId == staffId),
            Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.RecordCashPaymentAsync(
            _testEventId,
            Arg.Is<CashPaymentRequest>(r => r.RecordedByStaffId == staffId),
            Arg.Any<CancellationToken>())
            .Returns(Result<CashPaymentResponse>.Success(new CashPaymentResponse { Success = true }));

        // Act
        var result = await RecordCashPayment(_testEventId, ValidToken, request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        await _mockCheckInService.Received(1).RecordCashPaymentAsync(
            _testEventId,
            Arg.Is<CashPaymentRequest>(r => r.RecordedByStaffId == staffId),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: RecordCashPayment with attendee already has ticket returns 409 Conflict
    /// </summary>
    [Fact]
    public async Task RecordCashPayment_WithAttendeeAlreadyHasTicket_Returns409Conflict()
    {
        // Arrange
        var staffId = Guid.NewGuid();
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId, // FIXED: Must match eventId for authorization
            CreatedByStaffId = staffId,
        };

        var request = new CashPaymentRequest
        {
            AttendeeId = Guid.NewGuid(),
            TicketTypeId = Guid.NewGuid(),
            Amount = 25.00m,
            RecordedByStaffId = staffId
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockCashPaymentValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.RecordCashPaymentAsync(_testEventId, request, Arg.Any<CancellationToken>())
            .Returns(Result<CashPaymentResponse>.Failure("Attendee already has a ticket for this event"));

        // Act
        var result = await RecordCashPayment(_testEventId, ValidToken, request);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(409);
        problemResult.ProblemDetails.Detail.Should().Contain("already has a ticket");
    }

    #endregion

    #region CreateManualEntry Tests

    /// <summary>
    /// Test: CreateManualEntry with valid request returns 200 OK with check-in response
    /// </summary>
    [Fact]
    public async Task CreateManualEntry_WithValidRequest_Returns200OkWithCheckInResponse()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var request = new ManualEntryData
        {
            Name = "Walk-In User",
            Email = "walkin@example.com",
            Phone = "555-1234"
        };

        var expectedResponse = new CheckInResponse
        {
            Success = true,
            Message = "Manual entry created and checked in"
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockManualEntryValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.CreateManualEntryAsync(_testEventId, request, ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<CheckInResponse>.Success(expectedResponse));

        // Act
        var result = await CreateManualEntry(_testEventId, ValidToken, request);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<CheckInResponse>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: CreateManualEntry with user already registered returns 409 Conflict
    /// </summary>
    [Fact]
    public async Task CreateManualEntry_WithUserAlreadyRegistered_Returns409Conflict()
    {
        // Arrange
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        var request = new ManualEntryData
        {
            Name = "Duplicate User",
            Email = "duplicate@example.com",
            Phone = "555-1234"
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        _mockManualEntryValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        _mockCheckInService.CreateManualEntryAsync(_testEventId, request, ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<CheckInResponse>.Failure("User already registered for this event"));

        // Act
        var result = await CreateManualEntry(_testEventId, ValidToken, request);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(409);
        problemResult.ProblemDetails.Detail.Should().Contain("already registered");
    }

    #endregion

    #region Session Token Management Tests (Admin)

    /// <summary>
    /// Test: GenerateCheckInToken with valid admin user returns 200 OK with session token
    /// </summary>
    [Fact]
    public async Task GenerateCheckInToken_WithValidAdminUser_Returns200OkWithToken()
    {
        // Arrange
        var request = new GenerateTokenRequest
        {
            EventId = _testEventId,
            ExpirationHours = 12
        };

        var expectedResponse = new SessionTokenResponse
        {
            Token = "generated-token-abc123",
            EventId = _testEventId,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        };

        var user = CreateAdminClaimsPrincipal(_adminUserId);

        _mockSessionTokenService.GenerateTokenAsync(_testEventId, Arg.Any<Guid>(), _adminUserId, 12, Arg.Any<CancellationToken>())
            .Returns(Result<SessionTokenResponse>.Success(expectedResponse));

        // Act
        var result = await GenerateCheckInToken(request, user);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<SessionTokenResponse>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    /// <summary>
    /// Test: GenerateCheckInToken with missing user claim returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task GenerateCheckInToken_WithMissingUserClaim_Returns403Forbidden()
    {
        // Arrange
        var request = new GenerateTokenRequest
        {
            EventId = _testEventId,
            ExpirationHours = 12
        };

        var user = new ClaimsPrincipal(); // No claims

        // Act
        var result = await GenerateCheckInToken(request, user);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(403);
        problemResult.ProblemDetails.Title.Should().Be("Authorization Failed");
    }

    /// <summary>
    /// Test: GenerateCheckInToken with null expiration hours defaults to 12 hours
    /// </summary>
    [Fact]
    public async Task GenerateCheckInToken_WithNullExpirationHours_DefaultsTo12Hours()
    {
        // Arrange
        var request = new GenerateTokenRequest
        {
            EventId = _testEventId, // FIXED: Must include EventId
            ExpirationHours = null // Should default to 12
        };

        var user = CreateAdminClaimsPrincipal(_adminUserId);

        var expectedResponse = new SessionTokenResponse
        {
            Token = "generated-token-abc123",
            EventId = _testEventId,
            ExpiresAt = DateTime.UtcNow.AddHours(12)
        };

        _mockSessionTokenService.GenerateTokenAsync(_testEventId, Arg.Any<Guid>(), _adminUserId, 12, Arg.Any<CancellationToken>())
            .Returns(Result<SessionTokenResponse>.Success(expectedResponse));

        // Act
        var result = await GenerateCheckInToken(request, user);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        await _mockSessionTokenService.Received(1).GenerateTokenAsync(_testEventId, Arg.Any<Guid>(), _adminUserId, 12, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: RevokeCheckInToken with valid token returns 204 NoContent
    /// </summary>
    [Fact]
    public async Task RevokeCheckInToken_WithValidToken_Returns204NoContent()
    {
        // Arrange
        var request = new RevokeTokenRequest
        {
            Token = ValidToken
        };

        var user = CreateAdminClaimsPrincipal(_adminUserId);

        _mockSessionTokenService.RevokeTokenAsync(ValidToken, _adminUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await RevokeCheckInToken(request, user);

        // Assert
        result.Should().BeAssignableTo<NoContent>();
    }

    /// <summary>
    /// Test: RevokeCheckInToken when token not found returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task RevokeCheckInToken_WhenTokenNotFound_Returns400BadRequest()
    {
        // Arrange
        var request = new RevokeTokenRequest
        {
            Token = "nonexistent-token"
        };

        var user = CreateAdminClaimsPrincipal(_adminUserId);

        _mockSessionTokenService.RevokeTokenAsync("nonexistent-token", _adminUserId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("Token not found"));

        // Act
        var result = await RevokeCheckInToken(request, user);

        // Assert
        result.Should().BeAssignableTo<ProblemHttpResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult!.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Detail.Should().Be("Token not found");
    }

    /// <summary>
    /// Test: GetActiveCheckInTokens returns 200 OK with list of tokens
    /// </summary>
    [Fact]
    public async Task GetActiveCheckInTokens_WithValidEventId_Returns200OkWithTokenList()
    {
        // Arrange
        var expectedTokens = new List<SessionTokenResponse>
        {
            new() { Token = "token1", EventId = _testEventId },
            new() { Token = "token2", EventId = _testEventId }
        };

        _mockSessionTokenService.GetActiveTokensForEventAsync(_testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<List<SessionTokenResponse>>.Success(expectedTokens));

        // Act
        var result = await GetActiveCheckInTokens(_testEventId);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var okResult = result as Ok<List<SessionTokenResponse>>;
        okResult.Should().NotBeNull();
        okResult!.Value.Should().HaveCount(2);
        okResult.Value.Should().BeEquivalentTo(expectedTokens);
    }

    #endregion

    #region GetPendingSyncCount Tests

    /// <summary>
    /// Test: GetPendingSyncCount with valid token returns 200 OK with count
    /// </summary>
    [Fact]
    public async Task GetPendingSyncCount_WithValidToken_Returns200OkWithCount()
    {
        // Arrange
        var sessionId = Guid.NewGuid(); // FIXED: Store sessionId for verification
        var tokenData = new TokenValidationResult
        {
            EventId = _testEventId,
            CreatedByStaffId = Guid.NewGuid(),
        };

        _mockSessionTokenService.ValidateTokenAsync(ValidToken, Arg.Any<CancellationToken>())
            .Returns(Result<TokenValidationResult>.Success(tokenData));

        // FIXED: Setup mock to expect any Guid (since helper generates new sessionId)
        _mockSyncService.GetPendingSyncCountAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<int>.Success(5));

        // Act
        var result = await GetPendingSyncCount(ValidToken);

        // Assert
        result.Should().BeAssignableTo<IResult>();
        // FIXED: Result is Ok with anonymous type, need to use IValueHttpResult
        var okResult = result as IValueHttpResult;
        okResult.Should().NotBeNull();

        // Use dynamic to access anonymous object
        dynamic response = okResult!.Value!;
        int pendingCount = response.pendingCount;
        pendingCount.Should().Be(5);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper: Simulates GET /api/checkin/events/{eventId}/attendees endpoint
    /// </summary>
    private async Task<IResult> GetEventAttendees(Guid eventId, string? token, string? search = null, string? status = null, int page = 1, int pageSize = 50)
    {
        // Validate token
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
            return Results.Forbid();
        }

        // Call service - use sessionId from token validation
        var result = await _mockCheckInService.GetEventAttendeesAsync(eventId, tokenData.SessionId, search, status, page, pageSize, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Get Attendees Failed",
                detail: result.Error,
                statusCode: 500);
    }

    /// <summary>
    /// Helper: Simulates POST /api/checkin/events/{eventId}/checkin endpoint
    /// </summary>
    private async Task<IResult> ProcessCheckIn(Guid eventId, string? token, CheckInRequest request)
    {
        // Validate token
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
            return Results.Forbid();
        }

        // Validate request
        var requestValidation = await _mockCheckInValidator.ValidateAsync(request, CancellationToken.None);
        if (!requestValidation.IsValid)
        {
            return Results.ValidationProblem(requestValidation.ToDictionary());
        }

        // Call service
        var result = await _mockCheckInService.CheckInAttendeeAsync(request, token, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Check-in Failed",
                detail: result.Error,
                statusCode: result.Error.Contains("not found") ? 404 :
                           result.Error.Contains("capacity") ? 409 : 500);
    }

    /// <summary>
    /// Helper: Simulates GET /api/checkin/events/{eventId}/dashboard endpoint
    /// </summary>
    private async Task<IResult> GetEventDashboard(Guid eventId, string? token)
    {
        // Validate token
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
            return Results.Forbid();
        }

        // Call service
        var result = await _mockCheckInService.GetEventDashboardAsync(eventId, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Get Dashboard Failed",
                detail: result.Error,
                statusCode: result.Error.Contains("not found") ? 404 : 500);
    }

    /// <summary>
    /// Helper: Simulates POST /api/checkin/events/{eventId}/sync endpoint
    /// </summary>
    private async Task<IResult> SyncOfflineCheckIns(Guid eventId, string? token, SyncRequest request)
    {
        // Validate token
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
            return Results.Forbid();
        }

        // Validate request
        var requestValidation = await _mockSyncValidator.ValidateAsync(request, CancellationToken.None);
        if (!requestValidation.IsValid)
        {
            return Results.ValidationProblem(requestValidation.ToDictionary());
        }

        // Call service
        var result = await _mockSyncService.ProcessOfflineSyncAsync(request, token, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Sync Failed",
                detail: result.Error,
                statusCode: 500);
    }

    /// <summary>
    /// Helper: Simulates POST /api/checkin/events/{eventId}/cash-payment endpoint
    /// </summary>
    private async Task<IResult> RecordCashPayment(Guid eventId, string? token, CashPaymentRequest request)
    {
        // Validate token
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
            return Results.Forbid();
        }

        // Extract staff ID from token
        request.RecordedByStaffId = tokenData.CreatedByStaffId;

        // Validate request
        var requestValidation = await _mockCashPaymentValidator.ValidateAsync(request, CancellationToken.None);
        if (!requestValidation.IsValid)
        {
            return Results.ValidationProblem(requestValidation.ToDictionary());
        }

        // Call service
        var result = await _mockCheckInService.RecordCashPaymentAsync(eventId, request, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Cash Payment Failed",
                detail: result.Error,
                statusCode: result.Error.Contains("not found") ? 404 :
                           result.Error.Contains("already has a ticket") ? 409 : 500);
    }

    /// <summary>
    /// Helper: Simulates POST /api/checkin/events/{eventId}/manual-entry endpoint
    /// </summary>
    private async Task<IResult> CreateManualEntry(Guid eventId, string? token, ManualEntryData request)
    {
        // Validate token
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
            return Results.Forbid();
        }

        // Validate request
        var requestValidation = await _mockManualEntryValidator.ValidateAsync(request, CancellationToken.None);
        if (!requestValidation.IsValid)
        {
            return Results.ValidationProblem(requestValidation.ToDictionary());
        }

        // Call service
        var result = await _mockCheckInService.CreateManualEntryAsync(eventId, request, token, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Manual Entry Failed",
                detail: result.Error,
                statusCode: result.Error.Contains("already registered") ? 409 : 500);
    }

    /// <summary>
    /// Helper: Simulates POST /api/checkin/session-tokens/generate endpoint
    /// </summary>
    private async Task<IResult> GenerateCheckInToken(GenerateTokenRequest request, ClaimsPrincipal user)
    {
        // Get admin user ID from claims
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var adminUserId))
        {
            return Results.Problem(
                title: "Authorization Failed",
                detail: "Unable to identify user from token",
                statusCode: 403);
        }

        var result = await _mockSessionTokenService.GenerateTokenAsync(
            request.EventId,
            request.SessionId,
            adminUserId,
            request.ExpirationHours ?? 12,
            CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Bad Request",
                detail: result.Error,
                statusCode: 400);
    }

    /// <summary>
    /// Helper: Simulates POST /api/checkin/session-tokens/revoke endpoint
    /// </summary>
    private async Task<IResult> RevokeCheckInToken(RevokeTokenRequest request, ClaimsPrincipal user)
    {
        // Get admin user ID from claims
        var userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var adminUserId))
        {
            return Results.Problem(
                title: "Authorization Failed",
                detail: "Unable to identify user from token",
                statusCode: 403);
        }

        var result = await _mockSessionTokenService.RevokeTokenAsync(request.Token, adminUserId, CancellationToken.None);

        return result.IsSuccess
            ? Results.NoContent()
            : Results.Problem(
                title: "Bad Request",
                detail: result.Error,
                statusCode: 400);
    }

    /// <summary>
    /// Helper: Simulates GET /api/checkin/session-tokens/event/{eventId} endpoint
    /// </summary>
    private async Task<IResult> GetActiveCheckInTokens(Guid eventId)
    {
        var result = await _mockSessionTokenService.GetActiveTokensForEventAsync(eventId, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Bad Request",
                detail: result.Error,
                statusCode: 400);
    }

    /// <summary>
    /// Helper: Simulates GET /api/checkin/sync/pending-count endpoint
    /// </summary>
    private async Task<IResult> GetPendingSyncCount(string? token)
    {
        // Validate token
        if (string.IsNullOrEmpty(token))
        {
            return Results.Unauthorized();
        }

        var validationResult = await _mockSessionTokenService.ValidateTokenAsync(token, CancellationToken.None);
        if (!validationResult.IsSuccess)
        {
            return Results.Unauthorized();
        }

        var sessionId = Guid.NewGuid();
        var result = await _mockSyncService.GetPendingSyncCountAsync(sessionId, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(new { pendingCount = result.Value })
            : Results.Problem(
                title: "Get Pending Count Failed",
                detail: result.Error,
                statusCode: 500);
    }

    /// <summary>
    /// Helper: Creates ClaimsPrincipal with Administrator role and user ID
    /// </summary>
    private ClaimsPrincipal CreateAdminClaimsPrincipal(Guid userId)
    {
        var claims = new List<Claim>
        {
            new("sub", userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, UserRole.Administrator.ToRoleString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    #endregion
}
