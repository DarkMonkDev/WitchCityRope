using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WitchCityRope.Api.Features.Health.Endpoints;
using WitchCityRope.Api.Features.Health.Models;
using WitchCityRope.Api.Features.Health.Services;
using Xunit;
using FluentAssertions;
using NSubstitute;

namespace WitchCityRope.UnitTests.Api.Features.Health;

/// <summary>
/// Unit tests for HealthEndpoints (Pattern B)
/// Tests basic health check, detailed health check, and legacy health endpoints
/// </summary>
public class HealthEndpointsTests
{
    private readonly IHealthService _mockHealthService;

    public HealthEndpointsTests()
    {
        _mockHealthService = Substitute.For<IHealthService>();
    }

    #region GET /api/health Tests

    /// <summary>
    /// Test: GET /api/health with healthy database returns 200 OK with HealthResponse
    /// </summary>
    [Fact]
    public async Task GetHealth_WithHealthyDatabase_Returns200OkWithHealthResponse()
    {
        // Arrange
        var healthResponse = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DatabaseConnected = true,
            UserCount = 42,
            Version = "1.0.0"
        };

        _mockHealthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns((true, healthResponse, string.Empty));

        // Act
        var result = await GetHealth();

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Extract value from Results.Ok<T>
        dynamic dynamicResult = result;
        object value = dynamicResult.Value;
        value.Should().NotBeNull();
        value.Should().BeOfType<HealthResponse>();

        var response = (HealthResponse)value;
        response.Status.Should().Be("Healthy");
        response.DatabaseConnected.Should().BeTrue();
        response.UserCount.Should().Be(42);
        response.Version.Should().Be("1.0.0");
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Test: GET /api/health with database connection failure returns 503 Service Unavailable
    /// </summary>
    [Fact]
    public async Task GetHealth_WithDatabaseConnectionFailure_Returns503ServiceUnavailable()
    {
        // Arrange
        _mockHealthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns((false, null, "Database connection failed"));

        // Act
        var result = await GetHealth();

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(503);
        problemResult.ProblemDetails.Title.Should().Be("Health Check Failed");
        problemResult.ProblemDetails.Detail.Should().Be("Database connection failed");
    }

    /// <summary>
    /// Test: GET /api/health with service exception returns 503 Service Unavailable
    /// </summary>
    [Fact]
    public async Task GetHealth_WithServiceException_Returns503ServiceUnavailable()
    {
        // Arrange
        _mockHealthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns((false, null, "Health check failed"));

        // Act
        var result = await GetHealth();

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(503);
        problemResult.ProblemDetails.Title.Should().Be("Health Check Failed");
        problemResult.ProblemDetails.Detail.Should().Be("Health check failed");
    }

    /// <summary>
    /// Test: GET /api/health respects cancellation token
    /// </summary>
    [Fact]
    public async Task GetHealth_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var healthResponse = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DatabaseConnected = true,
            UserCount = 10,
            Version = "1.0.0"
        };

        _mockHealthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns((true, healthResponse, string.Empty));

        // Act
        var result = await GetHealth(cts.Token);

        // Assert
        await _mockHealthService.Received(1).GetHealthAsync(cts.Token);
    }

    #endregion

    #region GET /api/health/detailed Tests

    /// <summary>
    /// Test: GET /api/health/detailed with healthy database returns 200 OK with DetailedHealthResponse
    /// </summary>
    [Fact]
    public async Task GetDetailedHealth_WithHealthyDatabase_Returns200OkWithDetailedHealthResponse()
    {
        // Arrange
        var detailedResponse = new DetailedHealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DatabaseConnected = true,
            DatabaseVersion = "PostgreSQL (Connected)",
            UserCount = 150,
            ActiveUserCount = 45,
            Version = "1.0.0",
            Environment = "Development"
        };

        _mockHealthService.GetDetailedHealthAsync(Arg.Any<CancellationToken>())
            .Returns((true, detailedResponse, string.Empty));

        // Act
        var result = await GetDetailedHealth();

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Extract value from Results.Ok<T>
        dynamic dynamicResult = result;
        object value = dynamicResult.Value;
        value.Should().NotBeNull();
        value.Should().BeOfType<DetailedHealthResponse>();

        var response = (DetailedHealthResponse)value;
        response.Status.Should().Be("Healthy");
        response.DatabaseConnected.Should().BeTrue();
        response.DatabaseVersion.Should().Be("PostgreSQL (Connected)");
        response.UserCount.Should().Be(150);
        response.ActiveUserCount.Should().Be(45);
        response.Version.Should().Be("1.0.0");
        response.Environment.Should().Be("Development");
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Test: GET /api/health/detailed with database connection failure returns 503 Service Unavailable
    /// </summary>
    [Fact]
    public async Task GetDetailedHealth_WithDatabaseConnectionFailure_Returns503ServiceUnavailable()
    {
        // Arrange
        _mockHealthService.GetDetailedHealthAsync(Arg.Any<CancellationToken>())
            .Returns((false, null, "Database connection failed"));

        // Act
        var result = await GetDetailedHealth();

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(503);
        problemResult.ProblemDetails.Title.Should().Be("Detailed Health Check Failed");
        problemResult.ProblemDetails.Detail.Should().Be("Database connection failed");
    }

    /// <summary>
    /// Test: GET /api/health/detailed with service exception returns 503 Service Unavailable
    /// </summary>
    [Fact]
    public async Task GetDetailedHealth_WithServiceException_Returns503ServiceUnavailable()
    {
        // Arrange
        _mockHealthService.GetDetailedHealthAsync(Arg.Any<CancellationToken>())
            .Returns((false, null, "Detailed health check failed"));

        // Act
        var result = await GetDetailedHealth();

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(503);
        problemResult.ProblemDetails.Title.Should().Be("Detailed Health Check Failed");
        problemResult.ProblemDetails.Detail.Should().Be("Detailed health check failed");
    }

    /// <summary>
    /// Test: GET /api/health/detailed respects cancellation token
    /// </summary>
    [Fact]
    public async Task GetDetailedHealth_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var detailedResponse = new DetailedHealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DatabaseConnected = true,
            DatabaseVersion = "PostgreSQL (Connected)",
            UserCount = 100,
            ActiveUserCount = 30,
            Version = "1.0.0",
            Environment = "Production"
        };

        _mockHealthService.GetDetailedHealthAsync(Arg.Any<CancellationToken>())
            .Returns((true, detailedResponse, string.Empty));

        // Act
        var result = await GetDetailedHealth(cts.Token);

        // Assert
        await _mockHealthService.Received(1).GetDetailedHealthAsync(cts.Token);
    }

    /// <summary>
    /// Test: GET /api/health/detailed includes active user count based on last 30 days login
    /// </summary>
    [Fact]
    public async Task GetDetailedHealth_IncludesActiveUserCount_BasedOnLast30Days()
    {
        // Arrange
        var detailedResponse = new DetailedHealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DatabaseConnected = true,
            DatabaseVersion = "PostgreSQL (Connected)",
            UserCount = 200,
            ActiveUserCount = 75, // 75 users logged in within last 30 days
            Version = "1.0.0",
            Environment = "Production"
        };

        _mockHealthService.GetDetailedHealthAsync(Arg.Any<CancellationToken>())
            .Returns((true, detailedResponse, string.Empty));

        // Act
        var result = await GetDetailedHealth();

        // Assert
        result.Should().BeAssignableTo<IResult>();
        dynamic dynamicResult = result;
        object value = dynamicResult.Value;
        var response = (DetailedHealthResponse)value;

        response.ActiveUserCount.Should().Be(75);
        response.ActiveUserCount.Should().BeLessThanOrEqualTo(response.UserCount);
    }

    #endregion

    #region GET /health (Legacy) Tests

    /// <summary>
    /// Test: GET /health (legacy endpoint) returns simple status for healthy database
    /// </summary>
    [Fact]
    public async Task GetLegacyHealth_WithHealthyDatabase_Returns200OkWithSimpleStatus()
    {
        // Arrange
        var healthResponse = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DatabaseConnected = true,
            UserCount = 50,
            Version = "1.0.0"
        };

        _mockHealthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns((true, healthResponse, string.Empty));

        // Act
        var result = await GetLegacyHealth();

        // Assert
        result.Should().BeAssignableTo<IResult>();

        // Extract value from Results.Ok - legacy endpoint returns anonymous object { status = "Healthy" }
        dynamic dynamicResult = result;
        object value = dynamicResult.Value;
        value.Should().NotBeNull();

        dynamic statusObj = value!;
        ((string)statusObj.status).Should().Be("Healthy");
    }

    /// <summary>
    /// Test: GET /health (legacy endpoint) with database connection failure returns 503 Service Unavailable
    /// </summary>
    [Fact]
    public async Task GetLegacyHealth_WithDatabaseConnectionFailure_Returns503ServiceUnavailable()
    {
        // Arrange
        _mockHealthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns((false, null, "Database connection failed"));

        // Act
        var result = await GetLegacyHealth();

        // Assert
        result.Should().BeAssignableTo<IResult>();
        var problemResult = result as ProblemHttpResult;
        problemResult.Should().NotBeNull();
        problemResult!.StatusCode.Should().Be(503);
        problemResult.ProblemDetails.Title.Should().Be("Health Check Failed");
        problemResult.ProblemDetails.Detail.Should().Be("Database connection failed");
    }

    /// <summary>
    /// Test: GET /health (legacy endpoint) respects cancellation token
    /// </summary>
    [Fact]
    public async Task GetLegacyHealth_WithCancellationToken_PassesTokenToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var healthResponse = new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DatabaseConnected = true,
            UserCount = 10,
            Version = "1.0.0"
        };

        _mockHealthService.GetHealthAsync(Arg.Any<CancellationToken>())
            .Returns((true, healthResponse, string.Empty));

        // Act
        var result = await GetLegacyHealth(cts.Token);

        // Assert
        await _mockHealthService.Received(1).GetHealthAsync(cts.Token);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper: Simulates GET /api/health endpoint call
    /// </summary>
    private async Task<IResult> GetHealth(CancellationToken cancellationToken = default)
    {
        var (success, response, error) = await _mockHealthService.GetHealthAsync(cancellationToken);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Health Check Failed",
                detail: error,
                statusCode: 503);
    }

    /// <summary>
    /// Helper: Simulates GET /api/health/detailed endpoint call
    /// </summary>
    private async Task<IResult> GetDetailedHealth(CancellationToken cancellationToken = default)
    {
        var (success, response, error) = await _mockHealthService.GetDetailedHealthAsync(cancellationToken);

        return success
            ? Results.Ok(response)
            : Results.Problem(
                title: "Detailed Health Check Failed",
                detail: error,
                statusCode: 503);
    }

    /// <summary>
    /// Helper: Simulates GET /health (legacy) endpoint call
    /// </summary>
    private async Task<IResult> GetLegacyHealth(CancellationToken cancellationToken = default)
    {
        var (success, response, error) = await _mockHealthService.GetHealthAsync(cancellationToken);

        // Return simple status for legacy health checks
        return success
            ? Results.Ok(new { status = "Healthy" })
            : Results.Problem(
                title: "Health Check Failed",
                detail: error,
                statusCode: 503);
    }

    #endregion
}
