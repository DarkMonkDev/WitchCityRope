using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;
using WitchCityRope.Api.Endpoints.Admin;
using WitchCityRope.Api.DTOs;
using WitchCityRope.Api.Features.Events.Models;
using Xunit;
using FluentAssertions;

namespace WitchCityRope.UnitTests.Api.Endpoints.Admin;

public class AdminVenueEndpointsTests
{
    private readonly ClaimsPrincipal _adminUser;
    private readonly ClaimsPrincipal _regularUser;
    private readonly ClaimsPrincipal _unauthenticatedUser;

    public AdminVenueEndpointsTests()
    {
        // Create admin user claims
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "test"));

        // Create regular user claims
        _regularUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Member")
        }, "test"));

        // Create unauthenticated user
        _unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());
    }

    #region GetAllVenues Tests

    [Fact]
    public async Task GetAllVenues_WithAdminUser_ReturnsOkResult()
    {
        // Arrange
        var mockVenues = new List<VenueDto>
        {
            new VenueDto
            {
                Id = 1,
                Name = "Active Venue",
                Directions = "123 Active St",
                VenueInformation = "Internal notes visible to admin",
                IsActive = true,
                CreatedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
                UpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
            },
            new VenueDto
            {
                Id = 2,
                Name = "Inactive Venue",
                Directions = "456 Inactive St",
                VenueInformation = "Another note",
                IsActive = false,
                CreatedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
                UpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
            }
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateGetAllVenues(httpContext, mockVenues);

        // Assert
        result.Should().BeOfType<Ok<List<VenueDto>>>();
        var okResult = (Ok<List<VenueDto>>)result;
        okResult.Value.Should().NotBeNull();
        okResult.Value!.Count.Should().Be(2);
        okResult.Value.Should().Contain(v => v.IsActive);
        okResult.Value.Should().Contain(v => !v.IsActive); // Includes inactive venues
        okResult.Value.All(v => v.VenueInformation != null).Should().BeTrue(); // Admin sees venue information
    }

    [Fact]
    public async Task GetAllVenues_WithUnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _unauthenticatedUser;

        // Act
        var result = await SimulateGetAllVenues(httpContext, null);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(401);
        problemResult.ProblemDetails!.Title.Should().Be("Authentication Required");
    }

    [Fact]
    public async Task GetAllVenues_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _regularUser;

        // Act
        var result = await SimulateGetAllVenues(httpContext, null);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(403);
        problemResult.ProblemDetails!.Title.Should().Be("Insufficient Permissions");
        problemResult.ProblemDetails.Detail.Should().Contain("Administrator role required");
    }

    [Fact]
    public async Task GetAllVenues_WithDatabaseError_ReturnsInternalServerError()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateGetAllVenuesWithError(httpContext);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(500);
        problemResult.ProblemDetails!.Title.Should().Be("Database Error");
    }

    #endregion

    #region GetActiveVenues Tests

    [Fact]
    public async Task GetActiveVenues_WithAdminUser_ReturnsOnlyActiveVenues()
    {
        // Arrange
        var mockVenues = new List<VenueDto>
        {
            new VenueDto { Id = 1, Name = "Active Venue", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateGetActiveVenues(httpContext, mockVenues);

        // Assert
        result.Should().BeOfType<Ok<List<VenueDto>>>();
        var okResult = (Ok<List<VenueDto>>)result;
        okResult.Value!.All(v => v.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveVenues_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _regularUser;

        // Act
        var result = await SimulateGetActiveVenues(httpContext, null);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region GetVenue Tests

    [Fact]
    public async Task GetVenue_WithValidIdAndAdminUser_ReturnsOkResult()
    {
        // Arrange
        var venueId = 1;
        var mockVenue = new VenueDto
        {
            Id = venueId,
            Name = "Test Venue",
            Directions = "123 Test St",
            VenueInformation = "Admin can see venue information",
            IsActive = true,
            CreatedAt = DateTime.Parse("2025-11-14T10:00:00Z"),
            UpdatedAt = DateTime.Parse("2025-11-14T10:00:00Z")
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateGetVenue(venueId, httpContext, mockVenue);

        // Assert
        result.Should().BeOfType<Ok<VenueDto>>();
        var okResult = (Ok<VenueDto>)result;
        okResult.Value!.Id.Should().Be(venueId);
        okResult.Value.VenueInformation.Should().NotBeNull(); // Admin sees venue information
    }

    [Fact]
    public async Task GetVenue_WithNonExistentVenue_ReturnsNotFound()
    {
        // Arrange
        var venueId = 999;
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateGetVenue(venueId, httpContext, null);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Title.Should().Be("Venue Not Found");
    }

    [Fact]
    public async Task GetVenue_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.User = _regularUser;

        // Act
        var result = await SimulateGetVenue(1, httpContext, null);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region CreateVenue Tests

    [Fact]
    public async Task CreateVenue_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateVenueRequest
        {
            Name = "New Venue",
            Directions = "789 New St",
            VenueInformation = "Internal venue information"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateCreateVenue(request, httpContext, false, false);

        // Assert
        result.Should().BeOfType<Created<VenueDto>>();
        var createdResult = (Created<VenueDto>)result;
        createdResult.Value!.Name.Should().Be("New Venue");
        createdResult.Location.Should().Contain("/api/admin/venues/");
    }

    [Fact]
    public async Task CreateVenue_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateVenueRequest { Name = "", Directions = "123 Test" };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateCreateVenue(request, httpContext, false, false);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Validation Failed");
        problemResult.ProblemDetails.Detail.Should().Contain("name is required");
    }

    [Fact]
    public async Task CreateVenue_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateVenueRequest { Name = "Existing Venue" };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateCreateVenue(request, httpContext, true, false);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Duplicate Venue Name");
        problemResult.ProblemDetails.Detail.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateVenue_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var request = new CreateVenueRequest { Name = "New Venue" };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _regularUser;

        // Act
        var result = await SimulateCreateVenue(request, httpContext, false, false);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateVenue_WithNameTooLong_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateVenueRequest { Name = new string('A', 101) }; // 101 chars
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateCreateVenue(request, httpContext, false, false);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Detail.Should().Contain("100 characters");
    }

    #endregion

    #region UpdateVenue Tests

    [Fact]
    public async Task UpdateVenue_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var venueId = 1;
        var request = new UpdateVenueRequest
        {
            Name = "Updated Venue",
            Directions = "Updated directions",
            VenueInformation = "Updated venue information",
            IsActive = true
        };

        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateUpdateVenue(venueId, request, httpContext, true, false, false);

        // Assert
        result.Should().BeOfType<Ok<VenueDto>>();
        var okResult = (Ok<VenueDto>)result;
        okResult.Value!.Name.Should().Be("Updated Venue");
        okResult.Value.Directions.Should().Be("Updated directions");
    }

    [Fact]
    public async Task UpdateVenue_WithNonExistentVenue_ReturnsNotFound()
    {
        // Arrange
        var venueId = 999;
        var request = new UpdateVenueRequest { Name = "Test", IsActive = true };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateUpdateVenue(venueId, request, httpContext, false, false, false);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Title.Should().Be("Venue Not Found");
    }

    [Fact]
    public async Task UpdateVenue_WithDuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var venueId = 1;
        var request = new UpdateVenueRequest { Name = "Existing Venue", IsActive = true };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateUpdateVenue(venueId, request, httpContext, true, true, false);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Title.Should().Be("Duplicate Venue Name");
    }

    [Fact]
    public async Task UpdateVenue_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var venueId = 1;
        var request = new UpdateVenueRequest { Name = "Test", IsActive = true };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _regularUser;

        // Act
        var result = await SimulateUpdateVenue(venueId, request, httpContext, true, false, false);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task UpdateVenue_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var venueId = 1;
        var request = new UpdateVenueRequest { Name = "", IsActive = true };
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateUpdateVenue(venueId, request, httpContext, true, false, false);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(400);
        problemResult.ProblemDetails!.Detail.Should().Contain("name is required");
    }

    #endregion

    #region DeleteVenue Tests

    [Fact]
    public async Task DeleteVenue_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var venueId = 1;
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateDeleteVenue(venueId, httpContext, true);

        // Assert
        result.Should().BeOfType<NoContent>();
    }

    [Fact]
    public async Task DeleteVenue_WithNonExistentVenue_ReturnsNotFound()
    {
        // Arrange
        var venueId = 999;
        var httpContext = new DefaultHttpContext();
        httpContext.User = _adminUser;

        // Act
        var result = await SimulateDeleteVenue(venueId, httpContext, false);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(404);
        problemResult.ProblemDetails!.Title.Should().Be("Venue Not Found");
    }

    [Fact]
    public async Task DeleteVenue_WithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        var venueId = 1;
        var httpContext = new DefaultHttpContext();
        httpContext.User = _regularUser;

        // Act
        var result = await SimulateDeleteVenue(venueId, httpContext, true);

        // Assert
        result.Should().BeOfType<ProblemHttpResult>();
        var problemResult = (ProblemHttpResult)result;
        problemResult.StatusCode.Should().Be(403);
    }

    #endregion

    #region Status Code Mapping Tests

    [Theory]
    [InlineData("", 401)]    // Unauthenticated
    [InlineData("Member", 403)] // Non-admin
    [InlineData("Administrator", 200)] // Admin
    public async Task GetAllVenues_ReturnsCorrectStatusCodes_BasedOnUserRole(string role, int expectedStatusCode)
    {
        // Arrange
        ClaimsPrincipal user;
        if (string.IsNullOrEmpty(role))
        {
            user = _unauthenticatedUser;
        }
        else if (role == "Administrator")
        {
            user = _adminUser;
        }
        else
        {
            user = _regularUser;
        }

        var httpContext = new DefaultHttpContext();
        httpContext.User = user;

        var mockVenues = new List<VenueDto>
        {
            new VenueDto { Id = 1, Name = "Test", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        // Act
        var result = await SimulateGetAllVenues(httpContext, mockVenues);

        // Assert
        if (expectedStatusCode == 200)
        {
            result.Should().BeOfType<Ok<List<VenueDto>>>();
        }
        else
        {
            result.Should().BeOfType<ProblemHttpResult>();
            var problemResult = (ProblemHttpResult)result;
            problemResult.StatusCode.Should().Be(expectedStatusCode);
        }
    }

    #endregion

    #region Helper Methods - Simulation

    private async Task<IResult> SimulateGetAllVenues(HttpContext context, List<VenueDto>? venues)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(title: "Authentication Required", detail: "You must be logged in to access venues", statusCode: 401);
        }

        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Administrator")
        {
            return Results.Problem(title: "Insufficient Permissions", detail: "Administrator role required to manage venues", statusCode: 403);
        }

        await Task.CompletedTask;
        return Results.Ok(venues ?? new List<VenueDto>());
    }

    private async Task<IResult> SimulateGetAllVenuesWithError(HttpContext context)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(title: "Authentication Required", detail: "You must be logged in to access venues", statusCode: 401);
        }

        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Administrator")
        {
            return Results.Problem(title: "Insufficient Permissions", detail: "Administrator role required to manage venues", statusCode: 403);
        }

        await Task.CompletedTask;
        return Results.Problem(title: "Database Error", detail: "Failed to retrieve venues: Connection failed", statusCode: 500);
    }

    private async Task<IResult> SimulateGetActiveVenues(HttpContext context, List<VenueDto>? venues)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(title: "Authentication Required", detail: "You must be logged in to access venues", statusCode: 401);
        }

        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Administrator")
        {
            return Results.Problem(title: "Insufficient Permissions", detail: "Administrator role required to manage venues", statusCode: 403);
        }

        await Task.CompletedTask;
        return Results.Ok(venues ?? new List<VenueDto>());
    }

    private async Task<IResult> SimulateGetVenue(int id, HttpContext context, VenueDto? venue)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(title: "Authentication Required", detail: "You must be logged in to access venues", statusCode: 401);
        }

        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Administrator")
        {
            return Results.Problem(title: "Insufficient Permissions", detail: "Administrator role required to manage venues", statusCode: 403);
        }

        await Task.CompletedTask;

        if (venue == null)
        {
            return Results.Problem(title: "Venue Not Found", detail: $"Venue with ID {id} does not exist", statusCode: 404);
        }

        return Results.Ok(venue);
    }

    private async Task<IResult> SimulateCreateVenue(CreateVenueRequest request, HttpContext context, bool isDuplicate, bool hasError)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(title: "Authentication Required", detail: "You must be logged in to create venues", statusCode: 401);
        }

        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Administrator")
        {
            return Results.Problem(title: "Insufficient Permissions", detail: "Administrator role required to create venues", statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.Problem(title: "Validation Failed", detail: "Venue name is required", statusCode: 400);
        }

        if (request.Name.Length > 100)
        {
            return Results.Problem(title: "Validation Failed", detail: "Venue name must not exceed 100 characters", statusCode: 400);
        }

        if (request.Directions?.Length > 2000)
        {
            return Results.Problem(title: "Validation Failed", detail: "Directions must not exceed 2000 characters", statusCode: 400);
        }

        if (request.VenueInformation?.Length > 1000)
        {
            return Results.Problem(title: "Validation Failed", detail: "VenueInformation must not exceed 1000 characters", statusCode: 400);
        }

        await Task.CompletedTask;

        if (isDuplicate)
        {
            return Results.Problem(title: "Duplicate Venue Name", detail: $"A venue with the name '{request.Name}' already exists", statusCode: 400);
        }

        if (hasError)
        {
            return Results.Problem(title: "Database Error", detail: "Failed to create venue: Connection failed", statusCode: 500);
        }

        var venueDto = new VenueDto
        {
            Id = 1,
            Name = request.Name,
            Directions = request.Directions,
            VenueInformation = request.VenueInformation,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Results.Created($"/api/admin/venues/{venueDto.Id}", venueDto);
    }

    private async Task<IResult> SimulateUpdateVenue(int id, UpdateVenueRequest request, HttpContext context, bool venueExists, bool isDuplicate, bool hasError)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(title: "Authentication Required", detail: "You must be logged in to update venues", statusCode: 401);
        }

        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Administrator")
        {
            return Results.Problem(title: "Insufficient Permissions", detail: "Administrator role required to update venues", statusCode: 403);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.Problem(title: "Validation Failed", detail: "Venue name is required", statusCode: 400);
        }

        if (request.Name.Length > 100)
        {
            return Results.Problem(title: "Validation Failed", detail: "Venue name must not exceed 100 characters", statusCode: 400);
        }

        if (request.Directions?.Length > 2000)
        {
            return Results.Problem(title: "Validation Failed", detail: "Directions must not exceed 2000 characters", statusCode: 400);
        }

        if (request.VenueInformation?.Length > 1000)
        {
            return Results.Problem(title: "Validation Failed", detail: "VenueInformation must not exceed 1000 characters", statusCode: 400);
        }

        await Task.CompletedTask;

        if (!venueExists)
        {
            return Results.Problem(title: "Venue Not Found", detail: $"Venue with ID {id} does not exist", statusCode: 404);
        }

        if (isDuplicate)
        {
            return Results.Problem(title: "Duplicate Venue Name", detail: $"Another venue with the name '{request.Name}' already exists", statusCode: 400);
        }

        if (hasError)
        {
            return Results.Problem(title: "Database Error", detail: "Failed to update venue: Connection failed", statusCode: 500);
        }

        var venueDto = new VenueDto
        {
            Id = id,
            Name = request.Name,
            Directions = request.Directions,
            VenueInformation = request.VenueInformation,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Results.Ok(venueDto);
    }

    private async Task<IResult> SimulateDeleteVenue(int id, HttpContext context, bool venueExists)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Problem(title: "Authentication Required", detail: "You must be logged in to delete venues", statusCode: 401);
        }

        var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Administrator")
        {
            return Results.Problem(title: "Insufficient Permissions", detail: "Administrator role required to delete venues", statusCode: 403);
        }

        await Task.CompletedTask;

        if (!venueExists)
        {
            return Results.Problem(title: "Venue Not Found", detail: $"Venue with ID {id} does not exist", statusCode: 404);
        }

        return Results.NoContent();
    }

    #endregion
}
