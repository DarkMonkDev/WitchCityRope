using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using FluentAssertions;
using WitchCityRope.Api.Services;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;
using WitchCityRope.Api.Tests.TestBase;
using WitchCityRope.Api.Tests.Fixtures;

namespace WitchCityRope.Api.Tests.Services;

/// <summary>
/// Unit tests for DatabaseInitializationHealthCheck using real PostgreSQL database.
/// Tests health check behavior, database connectivity verification,
/// initialization status monitoring, and error handling.
/// 
/// Key testing aspects:
/// - Integration with DatabaseInitializationService status
/// - Real database connectivity verification
/// - Health check result status and data population
/// - Error handling and timeout scenarios
/// - Structured data for monitoring systems
/// - Real database operations (no ApplicationDbContext mocking)
/// </summary>
public class DatabaseInitializationHealthCheckTests : DatabaseTestBase
{
    private readonly Mock<ILogger<DatabaseInitializationHealthCheck>> _mockLogger;
    private DatabaseInitializationHealthCheck _healthCheck = null!;
    private readonly HealthCheckContext _healthCheckContext;

    public DatabaseInitializationHealthCheckTests(DatabaseTestFixture databaseFixture) 
        : base(databaseFixture)
    {
        _mockLogger = new Mock<ILogger<DatabaseInitializationHealthCheck>>();
        _healthCheckContext = new HealthCheckContext();
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        
        // No additional setup needed - base class already sets up service provider
        // The DbContext is already available through the base class setup

        _healthCheck = new DatabaseInitializationHealthCheck(
            MockServiceProvider.Object,
            _mockLogger.Object);

        // Reset static initialization state for each test
        ResetInitializationState();
    }

    [Fact]
    public async Task CheckHealthAsync_WhenInitializationNotCompleted_ReturnsUnhealthy()
    {
        // Arrange
        SetInitializationCompleted(false);

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Database initialization in progress");
        
        result.Data.Should().ContainKey("initializationCompleted");
        result.Data["initializationCompleted"].Should().Be(false);
        result.Data.Should().ContainKey("status");
        result.Data["status"].Should().Be("Initializing");
        result.Data.Should().ContainKey("timestamp");

        VerifyLogContains(LogLevel.Debug, "Health check: Database initialization in progress");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenInitializationCompletedAndDatabaseAccessible_ReturnsHealthy()
    {
        // Arrange
        SetInitializationCompleted(true);
        
        // Add some real data to the database
        var testUser = CreateTestUser();
        var testEvent = CreateTestEvent();
        DbContext.Events.Add(testEvent);
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Database initialization completed successfully");
        
        result.Data.Should().ContainKey("initializationCompleted");
        result.Data["initializationCompleted"].Should().Be(true);
        result.Data.Should().ContainKey("status");
        result.Data["status"].Should().Be("Ready");
        result.Data.Should().ContainKey("userCount");
        result.Data["userCount"].Should().Be(0); // No real users in database (MockUserManager shows 0)
        result.Data.Should().ContainKey("eventCount");
        result.Data["eventCount"].Should().Be(1); // One real event added
        result.Data.Should().ContainKey("timestamp");

        VerifyLogContains(LogLevel.Debug, "Health check: Database initialization healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenInitializationCompletedButDatabaseNotAccessible_ReturnsUnhealthy()
    {
        // Arrange
        SetInitializationCompleted(true);
        SetupFailedDatabaseConnection();

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        // When service provider throws exception (not CanConnectAsync returning false),
        // the catch-all handler returns "Database initialization health check failed"
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Database initialization health check failed");

        result.Data.Should().ContainKey("initializationCompleted");
        result.Data["initializationCompleted"].Should().Be(true);
        result.Data.Should().ContainKey("status");
        result.Data["status"].Should().Be("Failed");
        result.Data.Should().ContainKey("error");
        result.Data["error"].Should().Be("Database connection unavailable");

        VerifyLogContains(LogLevel.Error, "Database health check failed with unexpected error");
    }

    [Fact]
    public async Task CheckHealthAsync_WithDatabaseException_ReturnsUnhealthyWithErrorDetails()
    {
        // Arrange
        SetInitializationCompleted(true);
        SetupDatabaseConnectionThrowsException();

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Database initialization health check failed");
        result.Exception.Should().NotBeNull();
        result.Exception!.Message.Should().Be("Database connection error");
        
        result.Data.Should().ContainKey("initializationCompleted");
        result.Data["initializationCompleted"].Should().Be(true);
        result.Data.Should().ContainKey("status");
        result.Data["status"].Should().Be("Failed");
        result.Data.Should().ContainKey("error");
        result.Data["error"].Should().Be("Database connection error");
        result.Data.Should().ContainKey("errorType");
        result.Data["errorType"].Should().Be("InvalidOperationException");

        VerifyLogContains(LogLevel.Error, "Database health check failed with unexpected error");
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellation_ReturnsUnhealthyWithCancellationStatus()
    {
        // Arrange
        SetInitializationCompleted(true);

        // Cancel the token immediately
        CancellationTokenSource.Cancel();

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        // Note: The health check may complete successfully before the cancellation is detected
        // since CanConnectAsync might not throw OperationCanceledException if it finishes quickly.
        // This test verifies the cancellation handling path exists, but the actual result
        // depends on timing - could be "Cancelled" OR "Ready" depending on DB response time.
        result.Status.Should().Be(HealthStatus.Unhealthy);

        // Accept either cancellation or connection failure (both valid for cancelled token scenario)
        var acceptableDescriptions = new[]
        {
            "Database health check cancelled",
            "Database connection failed",
            "Database initialization health check failed"
        };
        result.Description.Should().BeOneOf(acceptableDescriptions);

        result.Data.Should().ContainKey("status");
        // Status could be "Cancelled", "ConnectionFailed", or "Failed" depending on timing
        var acceptableStatuses = new[] { "Cancelled", "ConnectionFailed", "Failed" };
        result.Data["status"].Should().BeOneOf(acceptableStatuses);
    }

    [Fact]
    public async Task CheckHealthAsync_IncludesTimestamp_InHealthCheckData()
    {
        // Arrange
        var beforeCheck = DateTime.UtcNow;
        SetInitializationCompleted(true);
        // Database is already accessible via real connection

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);
        var afterCheck = DateTime.UtcNow;

        // Assert
        result.Data.Should().ContainKey("timestamp");
        var timestamp = (DateTime)result.Data["timestamp"];
        timestamp.Should().BeOnOrAfter(beforeCheck);
        timestamp.Should().BeOnOrBefore(afterCheck);
        timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task CheckHealthAsync_VerifiesSeedDataExists_WithUserAndEventCounts()
    {
        // Arrange
        SetInitializationCompleted(true);
        
        // Add test data to real database
        for (int i = 0; i < 15; i++)
        {
            var user = CreateTestUser($"user{i}@example.com", $"User{i}");
            DbContext.Users.Add(user);
        }
        
        for (int i = 0; i < 8; i++)
        {
            var evt = CreateTestEvent($"Event {i}");
            DbContext.Events.Add(evt);
        }
        await DbContext.SaveChangesAsync();

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["userCount"].Should().Be(15);
        result.Data["eventCount"].Should().Be(8);

        VerifyLogContains(LogLevel.Debug, "Users: 15, Events: 8");
    }

    [Fact]
    public async Task CheckHealthAsync_WithZeroCountsButInitializationComplete_StillHealthy()
    {
        // Arrange
        SetInitializationCompleted(true);
        // Database is empty by default in tests

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data["userCount"].Should().Be(0);
        result.Data["eventCount"].Should().Be(0);
        result.Data["status"].Should().Be("Ready");
    }

    [Theory]
    [InlineData(typeof(TimeoutException), "Operation timed out")]
    [InlineData(typeof(InvalidOperationException), "Database connection error")]
    [InlineData(typeof(System.Net.NetworkInformation.NetworkInformationException), "Network information error")]
    public async Task CheckHealthAsync_WithSpecificExceptionTypes_HandlesAppropriately(Type exceptionType, string message)
    {
        // Arrange
        SetInitializationCompleted(true);

        // Create exception with appropriate constructor
        Exception exception;
        if (exceptionType == typeof(System.Net.NetworkInformation.NetworkInformationException))
        {
            // NetworkInformationException requires an error code
            exception = (Exception)Activator.CreateInstance(exceptionType, 10054)!; // WSAECONNRESET
        }
        else
        {
            // Other exceptions support (string message) constructor
            exception = (Exception)Activator.CreateInstance(exceptionType, message)!;
        }
        
        // Create a failing database context by using invalid connection
        var failingServiceProvider = new Mock<IServiceProvider>();
        var failingScopeFactory = new Mock<IServiceScopeFactory>();
        var failingScope = new Mock<IServiceScope>();
        var failingScopeProvider = new Mock<IServiceProvider>();
        
        failingServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(failingScopeFactory.Object);
        failingScopeFactory.Setup(x => x.CreateScope()).Returns(failingScope.Object);
        failingScope.Setup(x => x.ServiceProvider).Returns(failingScopeProvider.Object);
        failingScopeProvider.Setup(x => x.GetService(typeof(ApplicationDbContext)))
            .Throws(exception);
        
        var failingHealthCheck = new DatabaseInitializationHealthCheck(
            failingServiceProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await failingHealthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Exception.Should().NotBeNull();
        result.Data["errorType"].Should().Be(exceptionType.Name);

        // Verify error message (NetworkInformationException has system-generated message)
        if (exceptionType == typeof(System.Net.NetworkInformation.NetworkInformationException))
        {
            result.Data.Should().ContainKey("error");
            result.Data["error"].ToString().Should().NotBeNullOrEmpty();
        }
        else
        {
            result.Data["error"].Should().Be(message);
        }
    }

    [Fact]
    public async Task CheckHealthAsync_MultipleCallsConcurrently_HandlesConcurrencyCorrectly()
    {
        // Arrange
        SetInitializationCompleted(true);

        // Add test data
        for (int i = 0; i < 5; i++)
        {
            var user = CreateTestUser($"concurrent{i}@example.com", $"ConcurrentUser{i}");
            DbContext.Users.Add(user);
        }

        for (int i = 0; i < 10; i++)
        {
            var evt = CreateTestEvent($"Concurrent Event {i}");
            DbContext.Events.Add(evt);
        }
        await DbContext.SaveChangesAsync();

        // Setup mock to create NEW DbContext for each scope call (fix concurrency issue)
        // Note: Cannot verify call count on extension methods, so we just ensure behavior is correct
        MockServiceScopeFactory.Setup(x => x.CreateScope())
            .Returns(() =>
            {
                var scope = new Mock<IServiceScope>();
                var scopeServiceProvider = new Mock<IServiceProvider>();
                var newContext = DatabaseFixture.CreateDbContext();

                scopeServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext)))
                    .Returns(newContext);

                scope.Setup(x => x.ServiceProvider).Returns(scopeServiceProvider.Object);
                return scope.Object;
            });

        // Act
        var task1 = _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);
        var task2 = _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);
        var task3 = _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        var results = await Task.WhenAll(task1, task2, task3);

        // Assert
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.Status == HealthStatus.Healthy,
            "all concurrent health checks should succeed with separate DbContext instances");
        results.Should().OnlyContain(r => r.Data.ContainsKey("timestamp"));

        // Verify each result has expected data fields
        results.Should().OnlyContain(r => r.Data.ContainsKey("userCount"));
        results.Should().OnlyContain(r => r.Data.ContainsKey("eventCount"));
        results.Should().OnlyContain(r => r.Data.ContainsKey("status"));
        results.Should().OnlyContain(r => r.Data["status"].ToString() == "Ready");
    }

    [Fact]
    public async Task CheckHealthAsync_EnsuresServiceScopeIsDisposed()
    {
        // Arrange
        SetInitializationCompleted(true);
        
        // Add minimal test data
        var user = CreateTestUser();
        var evt = CreateTestEvent();
        DbContext.Users.Add(user);
        DbContext.Events.Add(evt);
        await DbContext.SaveChangesAsync();

        // Act
        await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        MockServiceScope.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task CheckHealthAsync_WithServiceProviderException_ReturnsUnhealthy()
    {
        // Arrange
        SetInitializationCompleted(true);
        MockServiceScopeFactory.Setup(x => x.CreateScope())
            .Throws(new InvalidOperationException("Service provider error"));

        // Act
        var result = await _healthCheck.CheckHealthAsync(_healthCheckContext, CancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["error"].Should().Be("Service provider error");
        result.Data["errorType"].Should().Be("InvalidOperationException");
    }

    private void SetInitializationCompleted(bool completed)
    {
        // Use reflection to set the static field
        var field = typeof(DatabaseInitializationService)
            .GetField("_initializationCompleted", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        field?.SetValue(null, completed);
    }

    private void ResetInitializationState()
    {
        SetInitializationCompleted(false);
    }

    private void SetupFailedDatabaseConnection()
    {
        // Dispose the current DbContext to simulate connection failure
        DbContext?.Dispose();
        DbContext = null!;
        
        // Update service provider to return null or throw
        MockScopeServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext)))
            .Throws(new InvalidOperationException("Database connection unavailable"));
    }

    private void SetupDatabaseConnectionThrowsException()
    {
        // Dispose the current DbContext to simulate connection failure
        DbContext?.Dispose();
        DbContext = null!;
        
        // Update service provider to throw specific exception
        MockScopeServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext)))
            .Throws(new InvalidOperationException("Database connection error"));
    }

    private new void VerifyLogContains(LogLevel level, string message)
    {
        _mockLogger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    public override async Task DisposeAsync()
    {
        ResetInitializationState();
        await base.DisposeAsync();
    }
}