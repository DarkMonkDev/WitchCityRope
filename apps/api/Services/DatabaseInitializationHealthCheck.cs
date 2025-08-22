using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WitchCityRope.Api.Data;

namespace WitchCityRope.Api.Services;

/// <summary>
/// Health check implementation for monitoring database initialization status.
/// Provides visibility into database readiness and initialization progress
/// through health check endpoints.
/// 
/// Key features:
/// - Integration with DatabaseInitializationService completion status
/// - Database connectivity verification
/// - Detailed status information for troubleshooting
/// - Structured data for monitoring and alerting systems
/// 
/// Health check responses:
/// - Healthy: Database initialization completed and connectivity verified
/// - Unhealthy: Initialization in progress, failed, or database unreachable
/// - Degraded: Not currently used but available for partial failure scenarios
/// 
/// This health check is exposed via /api/health/database endpoint
/// and integrated into overall application health monitoring.
/// </summary>
public class DatabaseInitializationHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializationHealthCheck> _logger;

    public DatabaseInitializationHealthCheck(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializationHealthCheck> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Performs health check by verifying database initialization status and connectivity.
    /// 
    /// Health check process:
    /// 1. Check if DatabaseInitializationService has completed successfully
    /// 2. Verify database connectivity if initialization is complete
    /// 3. Return appropriate status with detailed information
    /// 
    /// Returns structured data including:
    /// - initializationCompleted: Boolean status of initialization
    /// - status: Descriptive status message
    /// - error: Error details if applicable
    /// - timestamp: When the check was performed
    /// </summary>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;
        var data = new Dictionary<string, object>
        {
            ["timestamp"] = timestamp,
            ["initializationCompleted"] = DatabaseInitializationService.IsInitializationCompleted
        };

        try
        {
            // Check initialization completion status
            if (!DatabaseInitializationService.IsInitializationCompleted)
            {
                _logger.LogDebug("Health check: Database initialization in progress");
                data["status"] = "Initializing";
                
                return HealthCheckResult.Unhealthy(
                    "Database initialization in progress",
                    data: data);
            }

            // Verify database connectivity
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Perform simple connectivity check
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                _logger.LogWarning("Health check: Database connection failed despite initialization completion");
                data["status"] = "ConnectionFailed";
                data["error"] = "Database connection unavailable";
                
                return HealthCheckResult.Unhealthy(
                    "Database connection failed",
                    data: data);
            }

            // Optional: Verify sample data exists (basic smoke test)
            var userCount = await dbContext.Users.CountAsync(cancellationToken);
            var eventCount = await dbContext.Events.CountAsync(cancellationToken);
            
            data["status"] = "Ready";
            data["userCount"] = userCount;
            data["eventCount"] = eventCount;

            _logger.LogDebug("Health check: Database initialization healthy (Users: {UserCount}, Events: {EventCount})",
                userCount, eventCount);

            return HealthCheckResult.Healthy(
                "Database initialization completed successfully",
                data: data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Health check cancelled due to timeout or shutdown");
            data["status"] = "Cancelled";
            data["error"] = "Health check cancelled";
            
            return HealthCheckResult.Unhealthy(
                "Database health check cancelled",
                data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with unexpected error");
            data["status"] = "Failed";
            data["error"] = ex.Message;
            data["errorType"] = ex.GetType().Name;
            
            return HealthCheckResult.Unhealthy(
                "Database initialization health check failed",
                ex,
                data: data);
        }
    }
}