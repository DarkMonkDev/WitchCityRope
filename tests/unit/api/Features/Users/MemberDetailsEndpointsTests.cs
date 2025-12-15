using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Features.Users.Models.MemberDetails;
using WitchCityRope.Api.Features.Users.Services;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Users;

/// <summary>
/// Unit tests for MemberDetailsEndpoints following Pattern B standards.
/// Tests admin-only endpoints for comprehensive member management.
/// All endpoints require Administrator role authorization.
/// </summary>
public class MemberDetailsEndpointsTests
{
    private readonly IMemberDetailsService _mockService;
    private readonly ClaimsPrincipal _authenticatedAdmin;
    private readonly Guid _testUserId;
    private readonly Guid _testAuthorId;

    public MemberDetailsEndpointsTests()
    {
        _mockService = Substitute.For<IMemberDetailsService>();
        _testUserId = Guid.NewGuid();
        _testAuthorId = Guid.NewGuid();

        // Create authenticated admin user
        _authenticatedAdmin = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", _testAuthorId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _testAuthorId.ToString()),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "test"));
    }

    #region GetMemberDetails Tests

    /// <summary>
    /// Test: GET /api/users/{userId}/details with valid user returns 200 OK with MemberDetailsResponse
    /// </summary>
    [Fact]
    public async Task GetMemberDetails_WithValidUserId_Returns200OkWithDetails()
    {
        // Arrange
        var expectedResponse = new MemberDetailsResponse
        {
            UserId = _testUserId,
            SceneName = "Test Member",
            Email = "test@witchcityrope.com",
            Role = "Member",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            TotalEventsAttended = 5,
            FutureEvents = 2,
            VettingStatus = 3,
            VettingStatusDisplay = "Approved",
            HasVettingApplication = true
        };

        _mockService.GetMemberDetailsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((true, expectedResponse, string.Empty));

        // Act
        var result = await CallGetMemberDetails(_testUserId);

        // Assert
        result.Should().BeOfType<Ok<MemberDetailsResponse>>();
        var okResult = (Ok<MemberDetailsResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.UserId.Should().Be(_testUserId);
        okResult.Value!.SceneName.Should().Be("Test Member");
        okResult.Value!.TotalEventsAttended.Should().Be(5);
        okResult.Value!.VettingStatusDisplay.Should().Be("Approved");
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/details when user not found returns 404 NotFound
    /// </summary>
    [Fact]
    public async Task GetMemberDetails_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        _mockService.GetMemberDetailsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallGetMemberDetails(_testUserId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Resource Not Found");
        problemResult.ProblemDetails!.Detail.Should().Contain("User not found");
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/details when service throws error returns 500
    /// </summary>
    [Fact]
    public async Task GetMemberDetails_WhenServiceError_Returns500InternalServerError()
    {
        // Arrange
        _mockService.GetMemberDetailsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Database error occurred"));

        // Act
        var result = await CallGetMemberDetails(_testUserId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails.Should().NotBeNull();
        problemResult.ProblemDetails!.Title.Should().Be("Server Error");
        problemResult.ProblemDetails!.Detail.Should().Contain("Database error");
    }

    #endregion

    #region GetVettingDetails Tests

    /// <summary>
    /// Test: GET /api/users/{userId}/vetting-details with valid user returns 200 OK
    /// </summary>
    [Fact]
    public async Task GetVettingDetails_WithValidUserId_Returns200OkWithVettingDetails()
    {
        // Arrange
        var expectedResponse = new VettingDetailsResponse
        {
            HasApplication = true,
            ApplicationId = Guid.NewGuid(),
            ApplicationNumber = "VET-2025-001",
            SubmittedAt = DateTime.UtcNow.AddDays(-30),
            WorkflowStatus = 3,
            WorkflowStatusDisplay = "Approved",
            SceneName = "Test Member",
            Email = "test@witchcityrope.com",
            AgreesToGuidelines = true,
            AgreesToTerms = true
        };

        _mockService.GetVettingDetailsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((true, expectedResponse, string.Empty));

        // Act
        var result = await CallGetVettingDetails(_testUserId);

        // Assert
        result.Should().BeOfType<Ok<VettingDetailsResponse>>();
        var okResult = (Ok<VettingDetailsResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.HasApplication.Should().BeTrue();
        okResult.Value!.ApplicationNumber.Should().Be("VET-2025-001");
        okResult.Value!.WorkflowStatusDisplay.Should().Be("Approved");
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/vetting-details when user has no application returns 200 OK with empty details
    /// </summary>
    [Fact]
    public async Task GetVettingDetails_WhenNoApplication_Returns200OkWithEmptyDetails()
    {
        // Arrange
        var expectedResponse = new VettingDetailsResponse
        {
            HasApplication = false
        };

        _mockService.GetVettingDetailsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((true, expectedResponse, string.Empty));

        // Act
        var result = await CallGetVettingDetails(_testUserId);

        // Assert
        result.Should().BeOfType<Ok<VettingDetailsResponse>>();
        var okResult = (Ok<VettingDetailsResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.HasApplication.Should().BeFalse();
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/vetting-details when user not found returns 404
    /// </summary>
    [Fact]
    public async Task GetVettingDetails_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        _mockService.GetVettingDetailsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallGetVettingDetails(_testUserId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Title.Should().Be("Resource Not Found");
    }

    #endregion

    #region GetEventHistory Tests

    /// <summary>
    /// Test: GET /api/users/{userId}/event-history with valid parameters returns 200 OK with paginated results
    /// </summary>
    [Fact]
    public async Task GetEventHistory_WithValidParameters_Returns200OkWithPaginatedResults()
    {
        // Arrange
        var expectedResponse = new EventHistoryResponse
        {
            Events = new List<EventHistoryRecord>
            {
                new EventHistoryRecord
                {
                    EventId = Guid.NewGuid(),
                    EventTitle = "Rope Workshop 101",
                    AllowRsvps = false,
                    RequireTicketPurchase = true,
                    VettedMembersOnly = false,
                    EventDate = DateTime.UtcNow.AddDays(-10),
                    RegistrationType = "Ticket",
                    ParticipationStatus = "Active",
                    RegisteredAt = DateTime.UtcNow.AddDays(-20),
                    AmountPaid = 25.00m
                },
                new EventHistoryRecord
                {
                    EventId = Guid.NewGuid(),
                    EventTitle = "Monthly Munch",
                    AllowRsvps = true,
                    RequireTicketPurchase = false,
                    VettedMembersOnly = false,
                    EventDate = DateTime.UtcNow.AddDays(-30),
                    RegistrationType = "RSVP",
                    ParticipationStatus = "Active",
                    RegisteredAt = DateTime.UtcNow.AddDays(-35)
                }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        _mockService.GetEventHistoryAsync(_testUserId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((true, expectedResponse, string.Empty));

        // Act
        var result = await CallGetEventHistory(_testUserId, 1, 20);

        // Assert
        result.Should().BeOfType<Ok<EventHistoryResponse>>();
        var okResult = (Ok<EventHistoryResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Events.Should().HaveCount(2);
        okResult.Value!.TotalCount.Should().Be(2);
        okResult.Value!.Page.Should().Be(1);
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/event-history with page less than 1 returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetEventHistory_WithInvalidPageLessThan1_Returns400BadRequest()
    {
        // Arrange - no service call expected

        // Act
        var result = await CallGetEventHistory(_testUserId, 0, 20);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Validation Failed");
        problemResult.ProblemDetails!.Detail.Should().Contain("Page must be >= 1");
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/event-history with pageSize less than 1 returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetEventHistory_WithPageSizeLessThan1_Returns400BadRequest()
    {
        // Arrange - no service call expected

        // Act
        var result = await CallGetEventHistory(_testUserId, 1, 0);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Validation Failed");
        problemResult.ProblemDetails!.Detail.Should().Contain("Page size must be between 1 and 100");
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/event-history with pageSize greater than 100 returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task GetEventHistory_WithPageSizeGreaterThan100_Returns400BadRequest()
    {
        // Arrange - no service call expected

        // Act
        var result = await CallGetEventHistory(_testUserId, 1, 101);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Validation Failed");
        problemResult.ProblemDetails!.Detail.Should().Contain("Page size must be between 1 and 100");
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/event-history when user not found returns 404
    /// </summary>
    [Fact]
    public async Task GetEventHistory_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        _mockService.GetEventHistoryAsync(_testUserId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallGetEventHistory(_testUserId, 1, 20);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region GetMemberIncidents Tests

    /// <summary>
    /// Test: GET /api/users/{userId}/incidents with valid user returns 200 OK with incidents
    /// </summary>
    [Fact]
    public async Task GetMemberIncidents_WithValidUserId_Returns200OkWithIncidents()
    {
        // Arrange
        var expectedResponse = new MemberIncidentsResponse
        {
            Incidents = new List<MemberIncidentRecord>
            {
                new MemberIncidentRecord
                {
                    IncidentId = Guid.NewGuid(),
                    ReferenceNumber = "INC-2025-001",
                    Title = "Safety Protocol Violation",
                    IncidentDate = DateTime.UtcNow.AddDays(-15),
                    ReportedAt = DateTime.UtcNow.AddDays(-14),
                    Status = "Resolved",
                    Location = "Workshop Room",
                    UserInvolvementType = "Subject"
                }
            },
            TotalCount = 1
        };

        _mockService.GetMemberIncidentsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((true, expectedResponse, string.Empty));

        // Act
        var result = await CallGetMemberIncidents(_testUserId);

        // Assert
        result.Should().BeOfType<Ok<MemberIncidentsResponse>>();
        var okResult = (Ok<MemberIncidentsResponse>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Incidents.Should().HaveCount(1);
        okResult.Value!.TotalCount.Should().Be(1);
        okResult.Value!.Incidents[0].ReferenceNumber.Should().Be("INC-2025-001");
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/incidents when user has no incidents returns 200 OK with empty list
    /// </summary>
    [Fact]
    public async Task GetMemberIncidents_WhenNoIncidents_Returns200OkWithEmptyList()
    {
        // Arrange
        var expectedResponse = new MemberIncidentsResponse
        {
            Incidents = new List<MemberIncidentRecord>(),
            TotalCount = 0
        };

        _mockService.GetMemberIncidentsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((true, expectedResponse, string.Empty));

        // Act
        var result = await CallGetMemberIncidents(_testUserId);

        // Assert
        result.Should().BeOfType<Ok<MemberIncidentsResponse>>();
        var okResult = (Ok<MemberIncidentsResponse>)result;
        okResult.Value!.Incidents.Should().BeEmpty();
        okResult.Value!.TotalCount.Should().Be(0);
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/incidents when user not found returns 404
    /// </summary>
    [Fact]
    public async Task GetMemberIncidents_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        _mockService.GetMemberIncidentsAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallGetMemberIncidents(_testUserId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region GetMemberNotes Tests

    /// <summary>
    /// Test: GET /api/users/{userId}/notes with valid user returns 200 OK with notes
    /// </summary>
    [Fact]
    public async Task GetMemberNotes_WithValidUserId_Returns200OkWithNotes()
    {
        // Arrange
        var expectedResponse = new List<MemberNoteHistoryResponse>
        {
            new MemberNoteHistoryResponse
            {
                Id = Guid.NewGuid(),
                NoteSource = "UserNote",
                Content = "Member attended first workshop",
                Type = "General",
                AuthorSceneName = "Admin User",
                Timestamp = DateTime.UtcNow.AddDays(-5)
            },
            new MemberNoteHistoryResponse
            {
                Id = Guid.NewGuid(),
                NoteSource = "VettingAuditLog",
                Content = "Status changed",
                Type = "Status Changed",
                AuthorSceneName = "Admin User",
                Timestamp = DateTime.UtcNow.AddDays(-30),
                OldValue = "Pending Review",
                NewValue = "Approved"
            }
        };

        _mockService.GetMemberNotesAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((true, expectedResponse, string.Empty));

        // Act
        var result = await CallGetMemberNotes(_testUserId);

        // Assert
        result.Should().BeOfType<Ok<List<MemberNoteHistoryResponse>>>();
        var okResult = (Ok<List<MemberNoteHistoryResponse>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Should().HaveCount(2);
        okResult.Value![0].Content.Should().Contain("first workshop");
        okResult.Value![1].NoteSource.Should().Be("VettingAuditLog");
    }

    /// <summary>
    /// Test: GET /api/users/{userId}/notes when user not found returns 404
    /// </summary>
    [Fact]
    public async Task GetMemberNotes_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        _mockService.GetMemberNotesAsync(_testUserId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallGetMemberNotes(_testUserId);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region CreateMemberNote Tests

    /// <summary>
    /// Test: POST /api/users/{userId}/notes with valid request returns 201 Created
    /// </summary>
    [Fact]
    public async Task CreateMemberNote_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new CreateUserNoteRequest
        {
            Content = "Member completed advanced rope workshop",
            NoteType = "General"
        };

        var expectedResponse = new UserNoteResponse
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Content = request.Content,
            NoteType = request.NoteType,
            AuthorId = _testAuthorId,
            AuthorSceneName = "Admin User",
            CreatedAt = DateTime.UtcNow,
            IsArchived = false
        };

        _mockService.CreateMemberNoteAsync(_testUserId, request, _testAuthorId, Arg.Any<CancellationToken>())
            .Returns((true, expectedResponse, string.Empty));

        // Act
        var result = await CallCreateMemberNote(_testUserId, request, _authenticatedAdmin);

        // Assert
        result.Should().BeOfType<Created<UserNoteResponse>>();
        var createdResult = (Created<UserNoteResponse>)result;
        createdResult.Value.Should().NotBeNull();
        createdResult.Value!.Content.Should().Be(request.Content);
        createdResult.Location.Should().Be($"/api/users/{_testUserId}/notes/{expectedResponse.Id}");
    }

    /// <summary>
    /// Test: POST /api/users/{userId}/notes with empty content returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateMemberNote_WithEmptyContent_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateUserNoteRequest
        {
            Content = "",
            NoteType = "General"
        };

        _mockService.CreateMemberNoteAsync(_testUserId, request, _testAuthorId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Content cannot be empty"));

        // Act
        var result = await CallCreateMemberNote(_testUserId, request, _authenticatedAdmin);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Bad Request");
        problemResult.ProblemDetails!.Detail.Should().Contain("empty");
    }

    /// <summary>
    /// Test: POST /api/users/{userId}/notes with invalid note type returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task CreateMemberNote_WithInvalidNoteType_Returns400BadRequest()
    {
        // Arrange
        var request = new CreateUserNoteRequest
        {
            Content = "Test note",
            NoteType = "InvalidType"
        };

        _mockService.CreateMemberNoteAsync(_testUserId, request, _testAuthorId, Arg.Any<CancellationToken>())
            .Returns((false, null, "Invalid note type"));

        // Act
        var result = await CallCreateMemberNote(_testUserId, request, _authenticatedAdmin);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Detail.Should().Contain("Invalid");
    }

    /// <summary>
    /// Test: POST /api/users/{userId}/notes when user not found returns 404
    /// </summary>
    [Fact]
    public async Task CreateMemberNote_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var request = new CreateUserNoteRequest
        {
            Content = "Test note",
            NoteType = "General"
        };

        _mockService.CreateMemberNoteAsync(_testUserId, request, _testAuthorId, Arg.Any<CancellationToken>())
            .Returns((false, null, "User not found"));

        // Act
        var result = await CallCreateMemberNote(_testUserId, request, _authenticatedAdmin);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: POST /api/users/{userId}/notes without authentication returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task CreateMemberNote_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var request = new CreateUserNoteRequest
        {
            Content = "Test note",
            NoteType = "General"
        };

        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity()); // No claims

        // Act
        var result = await CallCreateMemberNote(_testUserId, request, unauthenticatedUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails!.Title.Should().Be("Unauthorized");
        problemResult.ProblemDetails!.Detail.Should().Contain("missing or invalid user identifier");
    }

    #endregion

    #region UpdateMemberStatus Tests

    /// <summary>
    /// Test: PUT /api/users/{userId}/status with valid request returns 204 NoContent
    /// </summary>
    [Fact]
    public async Task UpdateMemberStatus_WithValidRequest_Returns204NoContent()
    {
        // Arrange
        var request = new UpdateMemberStatusRequest
        {
            IsActive = false,
            Reason = "Member requested account deactivation"
        };

        _mockService.UpdateMemberStatusAsync(_testUserId, request, _testAuthorId, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        var result = await CallUpdateMemberStatus(_testUserId, request, _authenticatedAdmin);

        // Assert
        result.Should().BeOfType<NoContent>();
    }

    /// <summary>
    /// Test: PUT /api/users/{userId}/status when user not found returns 404
    /// </summary>
    [Fact]
    public async Task UpdateMemberStatus_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var request = new UpdateMemberStatusRequest
        {
            IsActive = false,
            Reason = "Test reason"
        };

        _mockService.UpdateMemberStatusAsync(_testUserId, request, _testAuthorId, Arg.Any<CancellationToken>())
            .Returns((false, "User not found"));

        // Act
        var result = await CallUpdateMemberStatus(_testUserId, request, _authenticatedAdmin);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: PUT /api/users/{userId}/status without authentication returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task UpdateMemberStatus_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var request = new UpdateMemberStatusRequest
        {
            IsActive = false,
            Reason = "Test reason"
        };

        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await CallUpdateMemberStatus(_testUserId, request, unauthenticatedUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region UpdateMemberRole Tests

    /// <summary>
    /// Test: PUT /api/users/{userId}/role with valid request returns 204 NoContent
    /// </summary>
    [Fact]
    public async Task UpdateMemberRole_WithValidRequest_Returns204NoContent()
    {
        // Arrange
        var request = new UpdateMemberRoleRequest
        {
            Role = "Teacher"
        };

        _mockService.UpdateMemberRoleAsync(_testUserId, request, _testAuthorId, Arg.Any<CancellationToken>())
            .Returns((true, string.Empty));

        // Act
        var result = await CallUpdateMemberRole(_testUserId, request, _authenticatedAdmin);

        // Assert
        result.Should().BeOfType<NoContent>();
    }

    /// <summary>
    /// Test: PUT /api/users/{userId}/role with invalid role returns 400 Bad Request
    /// </summary>
    [Fact]
    public async Task UpdateMemberRole_WithInvalidRole_Returns400BadRequest()
    {
        // Arrange
        var request = new UpdateMemberRoleRequest
        {
            Role = "InvalidRole"
        };

        _mockService.UpdateMemberRoleAsync(_testUserId, request, _testAuthorId, Arg.Any<CancellationToken>())
            .Returns((false, "Invalid role specified"));

        // Act
        var result = await CallUpdateMemberRole(_testUserId, request, _authenticatedAdmin);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Bad Request");
    }

    /// <summary>
    /// Test: PUT /api/users/{userId}/role when user not found returns 404
    /// </summary>
    [Fact]
    public async Task UpdateMemberRole_WhenUserNotFound_Returns404NotFound()
    {
        // Arrange
        var request = new UpdateMemberRoleRequest
        {
            Role = "Teacher"
        };

        _mockService.UpdateMemberRoleAsync(_testUserId, request, _testAuthorId, Arg.Any<CancellationToken>())
            .Returns((false, "User not found"));

        // Act
        var result = await CallUpdateMemberRole(_testUserId, request, _authenticatedAdmin);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
    }

    /// <summary>
    /// Test: PUT /api/users/{userId}/role without authentication returns 401 Unauthorized
    /// </summary>
    [Fact]
    public async Task UpdateMemberRole_WithoutAuthentication_Returns401Unauthorized()
    {
        // Arrange
        var request = new UpdateMemberRoleRequest
        {
            Role = "Teacher"
        };

        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await CallUpdateMemberRole(_testUserId, request, unauthenticatedUser);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to simulate calling GetMemberDetails endpoint
    /// </summary>
    private async Task<IResult> CallGetMemberDetails(Guid userId)
    {
        var (success, response, error) = await _mockService.GetMemberDetailsAsync(userId, CancellationToken.None);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem(
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem(
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Helper method to simulate calling GetVettingDetails endpoint
    /// </summary>
    private async Task<IResult> CallGetVettingDetails(Guid userId)
    {
        var (success, response, error) = await _mockService.GetVettingDetailsAsync(userId, CancellationToken.None);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem(
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem(
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Helper method to simulate calling GetEventHistory endpoint
    /// </summary>
    private async Task<IResult> CallGetEventHistory(Guid userId, int page, int pageSize)
    {
        // Validate pagination parameters
        if (page < 1)
        {
            return Results.Problem(
                title: "Validation Failed",
                detail: "Page must be >= 1",
                statusCode: 400);
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return Results.Problem(
                title: "Validation Failed",
                detail: "Page size must be between 1 and 100",
                statusCode: 400);
        }

        var (success, response, error) = await _mockService.GetEventHistoryAsync(userId, page, pageSize, CancellationToken.None);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem(
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem(
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Helper method to simulate calling GetMemberIncidents endpoint
    /// </summary>
    private async Task<IResult> CallGetMemberIncidents(Guid userId)
    {
        var (success, response, error) = await _mockService.GetMemberIncidentsAsync(userId, CancellationToken.None);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem(
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem(
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Helper method to simulate calling GetMemberNotes endpoint
    /// </summary>
    private async Task<IResult> CallGetMemberNotes(Guid userId)
    {
        var (success, response, error) = await _mockService.GetMemberNotesAsync(userId, CancellationToken.None);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem(
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem(
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.Ok(response);
    }

    /// <summary>
    /// Helper method to simulate calling CreateMemberNote endpoint
    /// </summary>
    private async Task<IResult> CallCreateMemberNote(Guid userId, CreateUserNoteRequest request, ClaimsPrincipal user)
    {
        // Get current user ID (admin performing the action)
        var authorIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(authorIdClaim) || !Guid.TryParse(authorIdClaim, out var authorId))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var (success, response, error) = await _mockService.CreateMemberNoteAsync(userId, request, authorId, CancellationToken.None);

        if (!success)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404);
            }
            if (error.Contains("Invalid", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("empty", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    title: "Bad Request",
                    detail: error,
                    statusCode: 400);
            }
            return Results.Problem(
                title: "Server Error",
                detail: error,
                statusCode: 500);
        }

        return Results.Created($"/api/users/{userId}/notes/{response!.Id}", response);
    }

    /// <summary>
    /// Helper method to simulate calling UpdateMemberStatus endpoint
    /// </summary>
    private async Task<IResult> CallUpdateMemberStatus(Guid userId, UpdateMemberStatusRequest request, ClaimsPrincipal user)
    {
        // Get current user ID (admin performing the action)
        var performedByIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(performedByIdClaim) || !Guid.TryParse(performedByIdClaim, out var performedById))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var (success, error) = await _mockService.UpdateMemberStatusAsync(userId, request, performedById, CancellationToken.None);

        if (!success)
        {
            return error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                ? Results.Problem(
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404)
                : Results.Problem(
                    title: "Server Error",
                    detail: error,
                    statusCode: 500);
        }

        return Results.NoContent();
    }

    /// <summary>
    /// Helper method to simulate calling UpdateMemberRole endpoint
    /// </summary>
    private async Task<IResult> CallUpdateMemberRole(Guid userId, UpdateMemberRoleRequest request, ClaimsPrincipal user)
    {
        // Get current user ID (admin performing the action)
        var performedByIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(performedByIdClaim) || !Guid.TryParse(performedByIdClaim, out var performedById))
        {
            return Results.Problem(
                title: "Unauthorized",
                detail: "User authentication failed - missing or invalid user identifier",
                statusCode: 401);
        }

        var (success, error) = await _mockService.UpdateMemberRoleAsync(userId, request, performedById, CancellationToken.None);

        if (!success)
        {
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    title: "Resource Not Found",
                    detail: error,
                    statusCode: 404);
            }
            if (error.Contains("Invalid", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Problem(
                    title: "Bad Request",
                    detail: error,
                    statusCode: 400);
            }
            return Results.Problem(
                title: "Server Error",
                detail: error,
                statusCode: 500);
        }

        return Results.NoContent();
    }

    #endregion
}
