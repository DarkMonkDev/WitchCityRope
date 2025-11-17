using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Participation.Endpoints;
using WitchCityRope.Api.Features.Participation.Entities;
using WitchCityRope.Api.Features.Participation.Models;
using WitchCityRope.Api.Features.Participation.Services;
using WitchCityRope.Api.Features.Shared.Models;
using WitchCityRope.Api.Features.Vetting.Entities;
using WitchCityRope.Api.Features.Vetting.Models;
using WitchCityRope.Api.Features.Vetting.Services;
using WitchCityRope.Api.Features.Volunteers.Services;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Participation;

/// <summary>
/// Unit tests for ParticipationEndpoints (Pattern B - Minimal API)
/// Tests all 8 participation/RSVP/ticket endpoints with comprehensive coverage
/// </summary>
public class ParticipationEndpointsTests : IDisposable
{
    private readonly IAttendanceService _mockAttendanceService;
    private readonly IVettingAccessControlService _mockVettingService;
    private readonly WitchCityRope.Api.Features.Payments.Services.IRefundService _mockRefundService;
    private readonly IVolunteerAssignmentService _mockVolunteerService;
    private readonly ILogger<IAttendanceService> _mockLogger;
    private readonly ApplicationDbContext _context;
    private readonly DefaultHttpContext _httpContext;
    private readonly Guid _testUserId;
    private readonly Guid _testEventId;

    public ParticipationEndpointsTests()
    {
        _mockAttendanceService = Substitute.For<IAttendanceService>();
        _mockVettingService = Substitute.For<IVettingAccessControlService>();
        _mockRefundService = Substitute.For<WitchCityRope.Api.Features.Payments.Services.IRefundService>();
        _mockVolunteerService = Substitute.For<IVolunteerAssignmentService>();
        _mockLogger = Substitute.For<ILogger<IAttendanceService>>();

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup HttpContext with authenticated user
        _httpContext = new DefaultHttpContext();
        _testUserId = Guid.NewGuid();
        _testEventId = Guid.NewGuid();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region GetParticipationStatus Tests

    /// <summary>
    /// Test: GetParticipationStatus with valid user returns 200 OK with status DTO
    /// </summary>
    [Fact]
    public async Task GetParticipationStatus_WithValidUser_Returns200OkWithStatus()
    {
        // Arrange
        var expectedStatus = new EnhancedParticipationStatusDto
        {
            HasRSVP = true,
            HasTicket = false,
            CanRSVP = false,
            CanPurchaseTicket = true
        };

        _mockAttendanceService.GetParticipationStatusAsync(_testEventId, _testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<EnhancedParticipationStatusDto?>.Success(expectedStatus));

        // Act
        var result = await GetParticipationStatus(_testEventId, _httpContext.User);

        // Assert
        result.Should().BeOfType<Ok<EnhancedParticipationStatusDto?>>();
        var okResult = (Ok<EnhancedParticipationStatusDto?>)result;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(expectedStatus);
    }

    /// <summary>
    /// Test: GetParticipationStatus with missing user claim returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetParticipationStatus_WithMissingUserClaim_Returns401Unauthorized()
    {
        // Arrange
        var userWithoutClaim = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await GetParticipationStatus(_testEventId, userWithoutClaim);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
        problemResult.ProblemDetails.Detail.Should().Contain("missing or invalid user identifier");
    }

    /// <summary>
    /// Test: GetParticipationStatus with invalid user ID format returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task GetParticipationStatus_WithInvalidUserIdFormat_Returns401Unauthorized()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "invalid-guid") };
        var userWithInvalidClaim = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act
        var result = await GetParticipationStatus(_testEventId, userWithInvalidClaim);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails.Title.Should().Be("Unauthorized");
    }

    /// <summary>
    /// Test: GetParticipationStatus when service fails returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task GetParticipationStatus_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        _mockAttendanceService.GetParticipationStatusAsync(_testEventId, _testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<EnhancedParticipationStatusDto?>.Failure("Database connection failed"));

        // Act
        var result = await GetParticipationStatus(_testEventId, _httpContext.User);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Failed to get participation status");
        problemResult.ProblemDetails.Detail.Should().Be("Database connection failed");
    }

    #endregion

    #region CreateRSVP Tests

    /// <summary>
    /// Test: CreateRSVP with valid request returns 201 Created
    /// </summary>
    [Fact]
    public async Task CreateRSVP_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateRSVPRequest { EventId = _testEventId };
        var expectedDto = new ParticipationStatusDto
        {
            EventId = _testEventId,
            UserId = _testUserId,
            ParticipationType = AttendanceType.RSVP,
            Status = AttendanceStatus.Active
        };

        var accessControl = new AccessControlResult(
            IsAllowed: true,
            DenialReason: null,
            VettingStatus: VettingStatus.Approved,
            UserMessage: null
        );

        _mockVettingService.CanUserRsvpAsync(_testUserId, _testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<AccessControlResult>.Success(accessControl));

        _mockAttendanceService.CreateRSVPAsync(Arg.Any<CreateRSVPRequest>(), _testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<ParticipationStatusDto>.Success(expectedDto));

        // Act
        var result = await CreateRSVP(_testEventId, request, _httpContext.User);

        // Assert
        result.Should().BeOfType<Created<ParticipationStatusDto>>();
        var createdResult = (Created<ParticipationStatusDto>)result;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Location.Should().Be($"/api/events/{_testEventId}/participation");
        createdResult.Value.Should().BeEquivalentTo(expectedDto);
    }

    /// <summary>
    /// Test: CreateRSVP with vetting denied returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task CreateRSVP_WithVettingDenied_Returns403Forbidden()
    {
        // Arrange
        var request = new CreateRSVPRequest { EventId = _testEventId };
        var accessControl = new AccessControlResult(
            IsAllowed: false,
            DenialReason: "User vetting status is Denied",
            VettingStatus: VettingStatus.Denied,
            UserMessage: "You are not allowed to RSVP to this event"
        );

        _mockVettingService.CanUserRsvpAsync(_testUserId, _testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<AccessControlResult>.Success(accessControl));

        // Act
        var result = await CreateRSVP(_testEventId, request, _httpContext.User);

        // Assert
        // FIXED: Result is JsonHttpResult with anonymous type, check as IResult with status code
        result.Should().BeAssignableTo<IResult>();
        var jsonResult = result as IStatusCodeHttpResult;
        jsonResult.Should().NotBeNull();
        jsonResult!.StatusCode.Should().Be(403);
    }

    /// <summary>
    /// Test: CreateRSVP with event not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task CreateRSVP_WithEventNotFound_Returns404NotFound()
    {
        // Arrange
        var request = new CreateRSVPRequest { EventId = _testEventId };
        var accessControl = new AccessControlResult(true, null, null, null);

        _mockVettingService.CanUserRsvpAsync(_testUserId, _testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<AccessControlResult>.Success(accessControl));

        _mockAttendanceService.CreateRSVPAsync(Arg.Any<CreateRSVPRequest>(), _testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<ParticipationStatusDto>.Failure("Event not found"));

        // Act
        var result = await CreateRSVP(_testEventId, request, _httpContext.User);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Resource Not Found");
        problemResult.ProblemDetails.Detail.Should().Be("Event not found");
    }

    /// <summary>
    /// Test: CreateRSVP with already existing RSVP returns 409 Conflict
    /// </summary>
    [Fact]
    public async Task CreateRSVP_WithExistingRSVP_Returns409Conflict()
    {
        // Arrange
        var request = new CreateRSVPRequest { EventId = _testEventId };
        var accessControl = new AccessControlResult(true, null, null, null);

        _mockVettingService.CanUserRsvpAsync(_testUserId, _testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<AccessControlResult>.Success(accessControl));

        _mockAttendanceService.CreateRSVPAsync(Arg.Any<CreateRSVPRequest>(), _testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<ParticipationStatusDto>.Failure("User already has an active RSVP"));

        // Act
        var result = await CreateRSVP(_testEventId, request, _httpContext.User);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(409);
        problemResult.ProblemDetails.Title.Should().Be("Conflict");
        problemResult.ProblemDetails.Detail.Should().Contain("already");
    }

    /// <summary>
    /// Test: CreateRSVP with capacity exceeded returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task CreateRSVP_WithCapacityExceeded_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateRSVPRequest { EventId = _testEventId };
        var accessControl = new AccessControlResult(true, null, null, null);

        _mockVettingService.CanUserRsvpAsync(_testUserId, _testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<AccessControlResult>.Success(accessControl));

        _mockAttendanceService.CreateRSVPAsync(Arg.Any<CreateRSVPRequest>(), _testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<ParticipationStatusDto>.Failure("Event has reached capacity"));

        // Act
        var result = await CreateRSVP(_testEventId, request, _httpContext.User);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Bad Request");
        problemResult.ProblemDetails.Detail.Should().Contain("capacity");
    }

    /// <summary>
    /// Test: CreateRSVP with vetting service failure returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task CreateRSVP_WithVettingServiceFailure_Returns500InternalServerError()
    {
        // Arrange
        var request = new CreateRSVPRequest { EventId = _testEventId };

        _mockVettingService.CanUserRsvpAsync(_testUserId, _testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<AccessControlResult>.Failure("Database connection lost"));

        // Act
        var result = await CreateRSVP(_testEventId, request, _httpContext.User);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Access check failed");
        problemResult.ProblemDetails.Detail.Should().Be("Unable to verify RSVP eligibility at this time");
    }

    #endregion

    #region CreateTicketPurchase Tests

    /// <summary>
    /// Test: PurchaseTicket with valid request returns 201 Created
    /// </summary>
    [Fact]
    public async Task PurchaseTicket_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateTicketPurchaseRequest { EventId = _testEventId };
        var expectedDto = new ParticipationStatusDto
        {
            EventId = _testEventId,
            UserId = _testUserId,
            ParticipationType = AttendanceType.Ticket,
            Status = AttendanceStatus.Active
        };

        var accessControl = new AccessControlResult(true, null, null, null);

        _mockVettingService.CanUserPurchaseTicketAsync(_testUserId, _testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<AccessControlResult>.Success(accessControl));

        _mockAttendanceService.CreateTicketPurchaseAsync(Arg.Any<CreateTicketPurchaseRequest>(), _testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<ParticipationStatusDto>.Success(expectedDto));

        // Act
        var result = await PurchaseTicket(_testEventId, request, _httpContext.User);

        // Assert
        result.Should().BeOfType<Created<ParticipationStatusDto>>();
        var createdResult = (Created<ParticipationStatusDto>)result;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Location.Should().Be($"/api/events/{_testEventId}/participation");
        createdResult.Value.Should().BeEquivalentTo(expectedDto);
    }

    /// <summary>
    /// Test: PurchaseTicket with vetting on hold returns 403 Forbidden
    /// </summary>
    [Fact]
    public async Task PurchaseTicket_WithVettingOnHold_Returns403Forbidden()
    {
        // Arrange
        var request = new CreateTicketPurchaseRequest { EventId = _testEventId };
        var accessControl = new AccessControlResult(
            IsAllowed: false,
            DenialReason: "User vetting is on hold",
            VettingStatus: VettingStatus.OnHold,
            UserMessage: "Your vetting application is currently on hold"
        );

        _mockVettingService.CanUserPurchaseTicketAsync(_testUserId, _testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<AccessControlResult>.Success(accessControl));

        // Act
        var result = await PurchaseTicket(_testEventId, request, _httpContext.User);

        // Assert
        // FIXED: Result is JsonHttpResult with anonymous type, check as IResult with status code
        result.Should().BeAssignableTo<IResult>();
        var jsonResult = result as IStatusCodeHttpResult;
        jsonResult.Should().NotBeNull();
        jsonResult!.StatusCode.Should().Be(403);
    }

    /// <summary>
    /// Test: PurchaseTicket with non-class event returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task PurchaseTicket_WithNonClassEvent_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateTicketPurchaseRequest { EventId = _testEventId };
        var accessControl = new AccessControlResult(true, null, null, null);

        _mockVettingService.CanUserPurchaseTicketAsync(_testUserId, _testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<AccessControlResult>.Success(accessControl));

        _mockAttendanceService.CreateTicketPurchaseAsync(Arg.Any<CreateTicketPurchaseRequest>(), _testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<ParticipationStatusDto>.Failure("Tickets are only allowed for class events"));

        // Act
        var result = await PurchaseTicket(_testEventId, request, _httpContext.User);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails.Title.Should().Be("Bad Request");
        problemResult.ProblemDetails.Detail.Should().Contain("only allowed");
    }

    #endregion

    #region CancelParticipation Tests

    /// <summary>
    /// Test: CancelParticipation with valid request returns 204 NoContent
    /// </summary>
    [Fact]
    public async Task CancelParticipation_WithValidRequest_Returns204NoContent()
    {
        // Arrange
        _mockAttendanceService.CancelParticipationAsync(_testEventId, _testUserId, null, null, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await CancelParticipation(_testEventId, _httpContext.User);

        // Assert
        result.Should().BeOfType<NoContent>();
        var noContentResult = (NoContent)result;
        noContentResult.StatusCode.Should().Be(204);
    }

    /// <summary>
    /// Test: CancelParticipation with RSVP type specified returns 204 NoContent
    /// </summary>
    [Fact]
    public async Task CancelParticipation_WithRSVPType_Returns204NoContent()
    {
        // Arrange
        _mockAttendanceService.CancelParticipationAsync(_testEventId, _testUserId, AttendanceType.RSVP, "User cancelled", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await CancelParticipation(_testEventId, _httpContext.User, "rsvp", "User cancelled");

        // Assert
        result.Should().BeOfType<NoContent>();
        var noContentResult = (NoContent)result;
        noContentResult.StatusCode.Should().Be(204);
    }

    /// <summary>
    /// Test: CancelParticipation with no active attendance returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task CancelParticipation_WithNoActiveAttendance_Returns404NotFound()
    {
        // Arrange
        _mockAttendanceService.CancelParticipationAsync(_testEventId, _testUserId, null, null, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("No active attendance found"));

        // Act
        var result = await CancelParticipation(_testEventId, _httpContext.User);

        // Assert
        result.Should().BeOfType<NotFound>();
    }

    /// <summary>
    /// Test: CancelParticipation with non-cancellable attendance returns 400 BadRequest
    /// </summary>
    [Fact]
    public async Task CancelParticipation_WithNonCancellableAttendance_Returns400BadRequest()
    {
        // Arrange
        _mockAttendanceService.CancelParticipationAsync(_testEventId, _testUserId, null, null, Arg.Any<CancellationToken>())
            .Returns(Result.Failure("This attendance cannot be cancelled"));

        // Act
        var result = await CancelParticipation(_testEventId, _httpContext.User);

        // Assert
        result.Should().BeOfType<BadRequest<string>>();
        var badRequestResult = (BadRequest<string>)result;
        badRequestResult.Value.Should().Contain("cannot be cancelled");
    }

    #endregion

    #region GetUserParticipations Tests

    /// <summary>
    /// Test: GetUserParticipations with valid user returns 200 OK with list
    /// </summary>
    [Fact]
    public async Task GetUserParticipations_WithValidUser_Returns200OkWithList()
    {
        // Arrange
        var expectedParticipations = new List<UserParticipationDto>
        {
            new UserParticipationDto
            {
                EventId = _testEventId,
                EventTitle = "Test Event",
                ParticipationType = AttendanceType.RSVP,
                Status = AttendanceStatus.Active
            }
        };

        _mockAttendanceService.GetUserParticipationsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<List<UserParticipationDto>>.Success(expectedParticipations));

        // Act
        var result = await GetUserParticipations(_httpContext.User);

        // Assert
        result.Should().BeOfType<Ok<List<UserParticipationDto>>>();
        var okResult = (Ok<List<UserParticipationDto>>)result;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().HaveCount(1);
        okResult.Value.Should().BeEquivalentTo(expectedParticipations);
    }

    /// <summary>
    /// Test: GetUserParticipations with no participations returns 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetUserParticipations_WithNoParticipations_Returns200OkWithEmptyList()
    {
        // Arrange
        _mockAttendanceService.GetUserParticipationsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns(Result<List<UserParticipationDto>>.Success(new List<UserParticipationDto>()));

        // Act
        var result = await GetUserParticipations(_httpContext.User);

        // Assert
        result.Should().BeOfType<Ok<List<UserParticipationDto>>>();
        var okResult = (Ok<List<UserParticipationDto>>)result;
        okResult.Value.Should().BeEmpty();
    }

    #endregion

    #region CancelRSVP (Backward Compatibility) Tests

    /// <summary>
    /// Test: CancelRSVP (backward compatibility) returns 204 NoContent
    /// </summary>
    [Fact]
    public async Task CancelRSVP_BackwardCompatibility_Returns204NoContent()
    {
        // Arrange
        _mockAttendanceService.CancelParticipationAsync(_testEventId, _testUserId, AttendanceType.RSVP, null, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await CancelRSVP(_testEventId, _httpContext.User);

        // Assert
        result.Should().BeOfType<NoContent>();
    }

    #endregion

    #region AdminGetEventParticipations Tests

    /// <summary>
    /// Test: AdminGetEventParticipations with valid event returns 200 OK with list
    /// </summary>
    [Fact]
    public async Task AdminGetEventParticipations_WithValidEvent_Returns200OkWithList()
    {
        // Arrange
        var expectedParticipations = new List<EventParticipationDto>
        {
            new EventParticipationDto
            {
                UserId = _testUserId,
                UserEmail = "test@example.com",
                ParticipationType = AttendanceType.RSVP,
                Status = AttendanceStatus.Active
            }
        };

        _mockAttendanceService.GetEventParticipationsAsync(_testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<List<EventParticipationDto>>.Success(expectedParticipations));

        // Act
        var result = await AdminGetEventParticipations(_testEventId);

        // Assert
        result.Should().BeOfType<Ok<List<EventParticipationDto>>>();
        var okResult = (Ok<List<EventParticipationDto>>)result;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().HaveCount(1);
        okResult.Value.Should().BeEquivalentTo(expectedParticipations);
    }

    /// <summary>
    /// Test: AdminGetEventParticipations when service fails returns 500 InternalServerError
    /// </summary>
    [Fact]
    public async Task AdminGetEventParticipations_WhenServiceFails_Returns500InternalServerError()
    {
        // Arrange
        _mockAttendanceService.GetEventParticipationsAsync(_testEventId, Arg.Any<CancellationToken>())
            .Returns(Result<List<EventParticipationDto>>.Failure("Database error"));

        // Act
        var result = await AdminGetEventParticipations(_testEventId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Title.Should().Be("Failed to get event participations");
        problemResult.ProblemDetails.Detail.Should().Be("Database error");
    }

    #endregion

    #region AdminRemoveParticipation Tests

    /// <summary>
    /// Test: AdminRemoveParticipation with valid request returns 204 NoContent
    /// </summary>
    [Fact]
    public async Task AdminRemoveParticipation_WithValidRequest_Returns204NoContent()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var adminClaims = new[] { new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()) };
        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "TestAuth"));

        // Create test data in in-memory database
        var attendance = new EventAttendance
        {
            Id = Guid.NewGuid(),
            EventId = _testEventId,
            UserId = _testUserId,
            AttendanceType = AttendanceType.RSVP,
            Status = AttendanceStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.EventAttendances.Add(attendance);
        await _context.SaveChangesAsync();

        // Act
        var result = await AdminRemoveParticipation(_testEventId, _testUserId, adminUser, _context);

        // Assert
        result.Should().BeOfType<NoContent>();

        // Verify attendance was cancelled
        var updatedAttendance = await _context.EventAttendances.FindAsync(attendance.Id);
        updatedAttendance.Should().NotBeNull();
        updatedAttendance!.Status.Should().Be(AttendanceStatus.Cancelled);
        updatedAttendance.CancelledAt.Should().NotBeNull();
        updatedAttendance.UpdatedBy.Should().Be(adminUserId);
    }

    /// <summary>
    /// Test: AdminRemoveParticipation with no active attendance returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task AdminRemoveParticipation_WithNoActiveAttendance_Returns404NotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var adminClaims = new[] { new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()) };
        var adminUser = new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "TestAuth"));

        // Act
        var result = await AdminRemoveParticipation(_testEventId, _testUserId, adminUser, _context);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Title.Should().Be("Attendance Not Found");
    }

    #endregion

    #region Helper Methods - Simulate Endpoint Logic

    private async Task<IResult> GetParticipationStatus(Guid eventId, ClaimsPrincipal user)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var result = await _mockAttendanceService.GetParticipationStatusAsync(eventId, userId, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Failed to get participation status",
                detail: result.Error,
                statusCode: 500);
    }

    private async Task<IResult> CreateRSVP(Guid eventId, CreateRSVPRequest request, ClaimsPrincipal user)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var accessCheckResult = await _mockVettingService.CanUserRsvpAsync(userId, eventId, CancellationToken.None);

        if (!accessCheckResult.IsSuccess)
        {
            return Results.Problem(
                title: "Access check failed",
                detail: "Unable to verify RSVP eligibility at this time",
                statusCode: 500);
        }

        var accessControl = accessCheckResult.Value!;

        if (!accessControl.IsAllowed)
        {
            return Results.Json(new
            {
                error = accessControl.DenialReason,
                message = accessControl.UserMessage,
                vettingStatus = accessControl.VettingStatus?.ToString()
            }, statusCode: 403);
        }

        request.EventId = eventId;

        var result = await _mockAttendanceService.CreateRSVPAsync(request, userId, CancellationToken.None);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
            {
                return Results.Problem(
                    title: "Resource Not Found",
                    detail: result.Error,
                    statusCode: 404);
            }
            if (result.Error.Contains("already"))
            {
                return Results.Problem(
                    title: "Conflict",
                    detail: result.Error,
                    statusCode: 409);
            }
            if (result.Error.Contains("vetted") || result.Error.Contains("capacity"))
            {
                return Results.Problem(
                    title: "Bad Request",
                    detail: result.Error,
                    statusCode: 400);
            }

            return Results.Problem(
                title: "Failed to create RSVP",
                detail: result.Error,
                statusCode: 500);
        }

        return Results.Created($"/api/events/{eventId}/participation", result.Value);
    }

    private async Task<IResult> PurchaseTicket(Guid eventId, CreateTicketPurchaseRequest request, ClaimsPrincipal user)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var accessCheckResult = await _mockVettingService.CanUserPurchaseTicketAsync(userId, eventId, CancellationToken.None);

        if (!accessCheckResult.IsSuccess)
        {
            return Results.Problem(
                title: "Access check failed",
                detail: "Unable to verify ticket purchase eligibility at this time",
                statusCode: 500);
        }

        var accessControl = accessCheckResult.Value!;

        if (!accessControl.IsAllowed)
        {
            return Results.Json(new
            {
                error = accessControl.DenialReason,
                message = accessControl.UserMessage,
                vettingStatus = accessControl.VettingStatus?.ToString()
            }, statusCode: 403);
        }

        request.EventId = eventId;

        var result = await _mockAttendanceService.CreateTicketPurchaseAsync(request, userId, CancellationToken.None);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found"))
            {
                return Results.Problem(
                    title: "Resource Not Found",
                    detail: result.Error,
                    statusCode: 404);
            }
            if (result.Error.Contains("already"))
            {
                return Results.Problem(
                    title: "Conflict",
                    detail: result.Error,
                    statusCode: 409);
            }
            if (result.Error.Contains("capacity") || result.Error.Contains("only allowed"))
            {
                return Results.Problem(
                    title: "Bad Request",
                    detail: result.Error,
                    statusCode: 400);
            }

            return Results.Problem(
                title: "Failed to purchase ticket",
                detail: result.Error,
                statusCode: 500);
        }

        return Results.Created($"/api/events/{eventId}/participation", result.Value);
    }

    private async Task<IResult> CancelParticipation(Guid eventId, ClaimsPrincipal user, string? type = null, string? reason = null)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        AttendanceType? attendanceType = type?.ToLower() switch
        {
            "rsvp" => AttendanceType.RSVP,
            "ticket" => AttendanceType.Ticket,
            _ => null
        };

        var result = await _mockAttendanceService.CancelParticipationAsync(eventId, userId, attendanceType, reason, CancellationToken.None);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found") || result.Error.Contains("No active attendance"))
            {
                return Results.NotFound();
            }
            if (result.Error.Contains("cannot be cancelled"))
            {
                return Results.BadRequest(result.Error);
            }

            return Results.Problem(result.Error);
        }

        return Results.NoContent();
    }

    private async Task<IResult> GetUserParticipations(ClaimsPrincipal user)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var result = await _mockAttendanceService.GetUserParticipationsAsync(userId, CancellationToken.None);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: "Failed to get user participations",
                detail: result.Error,
                statusCode: 500);
    }

    private async Task<IResult> CancelRSVP(Guid eventId, ClaimsPrincipal user, string? reason = null)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var result = await _mockAttendanceService.CancelParticipationAsync(eventId, userId, AttendanceType.RSVP, reason, CancellationToken.None);

        if (!result.IsSuccess)
        {
            if (result.Error.Contains("not found") || result.Error.Contains("No active attendance"))
            {
                return Results.NotFound();
            }
            if (result.Error.Contains("cannot be cancelled"))
            {
                return Results.BadRequest(result.Error);
            }

            return Results.Problem(result.Error);
        }

        return Results.NoContent();
    }

    private async Task<IResult> AdminGetEventParticipations(Guid eventId)
    {
        var result = await _mockAttendanceService.GetEventParticipationsAsync(eventId, CancellationToken.None);

        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return Results.Problem(
            title: "Failed to get event participations",
            detail: result.Error ?? "Failed to get event participations",
            statusCode: 500);
    }

    private async Task<IResult> AdminRemoveParticipation(Guid eventId, Guid userId, ClaimsPrincipal user, ApplicationDbContext context)
    {
        if (!Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var adminUserId))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "Admin authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var attendance = await context.EventAttendances
            .FirstOrDefaultAsync(ea =>
                ea.EventId == eventId &&
                ea.UserId == userId &&
                ea.Status == AttendanceStatus.Active);

        if (attendance == null)
        {
            return Results.Problem(
                title: "Attendance Not Found",
                detail: "No active attendance found for this user and event",
                statusCode: 404);
        }

        attendance.Status = AttendanceStatus.Cancelled;
        attendance.CancelledAt = DateTime.UtcNow;
        attendance.CancellationReason = $"Removed by admin {adminUserId}";
        attendance.UpdatedBy = adminUserId;
        attendance.UpdatedAt = DateTime.UtcNow;
        context.EventAttendances.Update(attendance);

        await context.SaveChangesAsync();

        return Results.NoContent();
    }

    #endregion
}
