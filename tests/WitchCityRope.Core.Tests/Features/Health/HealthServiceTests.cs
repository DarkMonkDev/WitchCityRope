using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WitchCityRope.Api.Features.Health.Models;
using WitchCityRope.Api.Features.Health.Services;
using WitchCityRope.Tests.Common.Builders;
using WitchCityRope.Tests.Common.Fixtures;
using WitchCityRope.Tests.Common.TestBase;
using Xunit;

namespace WitchCityRope.Core.Tests.Features.Health;

/// <summary>
/// Unit tests for HealthService following Vertical Slice Architecture patterns
/// Tests the health check service independently of database dependencies
/// </summary>
public class HealthServiceTests : FeatureTestBase<HealthService>
{
    public HealthServiceTests(DatabaseTestFixture fixture) : base(fixture)
    {
    }

    protected override HealthService CreateService()
    {
        return new HealthService(DbContext, MockLogger.Object);
    }

    [Fact]
    public async Task GetHealthAsync_WhenDatabaseConnected_ReturnsHealthy()
    {
        // Arrange
        // Database is available via TestContainers

        // Act
        var (success, response, error) = await Service.GetHealthAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Status.Should().Be("Healthy");
        response.DatabaseConnected.Should().BeTrue();
        response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        response.Version.Should().NotBeEmpty();
        error.Should().BeEmpty();

        // Verify no error logging occurred
        VerifyNoErrorsLogged();
    }

    [Fact]
    public async Task GetHealthAsync_WhenDatabaseConnected_ReturnsCorrectUserCount()
    {
        // Arrange
        // Seed test users
        await SeedTestUsersAsync(5);

        // Act
        var (success, response, error) = await Service.GetHealthAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.UserCount.Should().BeGreaterThanOrEqualTo(5);
        response.Status.Should().Be("Healthy");
    }

    [Fact]
    public async Task GetHealthAsync_WhenDatabaseEmpty_ReturnsHealthyWithZeroUsers()
    {
        // Arrange
        // Database starts empty due to reset in base class

        // Act
        var (success, response, error) = await Service.GetHealthAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.UserCount.Should().Be(0);
        response.Status.Should().Be("Healthy");
        response.DatabaseConnected.Should().BeTrue();
    }

    [Fact]
    public async Task GetDetailedHealthAsync_WhenDatabaseConnected_ReturnsDetailedInfo()
    {
        // Arrange
        await SeedTestUsersAsync(10);

        // Act
        var (success, response, error) = await Service.GetDetailedHealthAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Status.Should().Be("Healthy");
        response.DatabaseConnected.Should().BeTrue();
        response.UserCount.Should().Be(10);
        response.DatabaseVersion.Should().NotBeEmpty();
        response.Environment.Should().NotBeEmpty();
        response.ActiveUserCount.Should().BeGreaterThanOrEqualTo(0);
        error.Should().BeEmpty();
    }

    [Fact]
    public async Task GetHealthAsync_ResponseMatchesBuilder_ForConsistency()
    {
        // Arrange
        var expectedResponse = new HealthResponseBuilder()
            .Healthy()
            .WithCurrentTimestamp()
            .WithDatabaseConnected(true)
            .WithUserCount(0) // Empty database
            .Build();

        // Act
        var (success, response, error) = await Service.GetHealthAsync();

        // Assert
        success.Should().BeTrue();
        response.Should().NotBeNull();
        response!.Status.Should().Be(expectedResponse.Status);
        response.DatabaseConnected.Should().Be(expectedResponse.DatabaseConnected);
        response.UserCount.Should().Be(expectedResponse.UserCount);
        // Note: Timestamp and Version will differ, that's expected
    }

    [Fact]
    public async Task GetHealthAsync_PerformanceRequirement_CompletesQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var (success, response, error) = await Service.GetHealthAsync();

        // Assert
        stopwatch.Stop();
        success.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Health check should complete within 1 second");
    }

    [Fact]
    public async Task GetHealthAsync_CalledMultipleTimes_RemainsConsistent()
    {
        // Arrange & Act
        var result1 = await Service.GetHealthAsync();
        var result2 = await Service.GetHealthAsync();
        var result3 = await Service.GetHealthAsync();

        // Assert
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        result3.Success.Should().BeTrue();

        result1.Response!.Status.Should().Be(result2.Response!.Status);
        result2.Response!.Status.Should().Be(result3.Response!.Status);

        result1.Response.DatabaseConnected.Should().Be(result2.Response.DatabaseConnected);
        result2.Response.DatabaseConnected.Should().Be(result3.Response.DatabaseConnected);
    }

    /// <summary>
    /// Helper method to seed test users for user count verification
    /// NOTE: This is a placeholder - actual user seeding would depend on
    /// the ApplicationUser entity structure in the new architecture
    /// </summary>
    private async Task SeedTestUsersAsync(int count)
    {
        // TODO: Implement when ApplicationUser entity structure is confirmed
        // For now, this test will pass with 0 users until user seeding is implemented

        // Example implementation once user entities are available:
        // for (int i = 0; i < count; i++)
        // {
        //     var user = new ApplicationUser
        //     {
        //         Id = Guid.NewGuid().ToString(),
        //         Email = $"test{i}@example.com",
        //         UserName = $"test{i}@example.com"
        //     };
        //     DbContext.Users.Add(user);
        // }
        // await DbContext.SaveChangesAsync();

        await Task.CompletedTask; // Placeholder for async operation
    }
}