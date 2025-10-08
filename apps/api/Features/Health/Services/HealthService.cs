using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.Health.Models;

namespace WitchCityRope.Api.Features.Health.Services;

/// <summary>
/// Simple class for raw SQL query results
/// </summary>
public class VersionResult
{
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Simple health check service using direct Entity Framework access
/// Example of the simplified vertical slice architecture pattern
/// </summary>
public class HealthService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthService> _logger;

    public HealthService(ApplicationDbContext context, ILogger<HealthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get basic health check information
    /// Simple Entity Framework service - NO MediatR complexity
    /// </summary>
    public async Task<(bool Success, HealthResponse? Response, string Error)> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simple database connectivity check using direct Entity Framework
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return (false, null, "Database connection failed");
            }

            // Get basic database statistics
            var userCount = await _context.Users
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var response = new HealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                DatabaseConnected = canConnect,
                UserCount = userCount,
                Version = "1.0.0"
            };

            _logger.LogDebug("Health check completed successfully - Database: {DatabaseConnected}, Users: {UserCount}",
                canConnect, userCount);

            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return (false, null, "Health check failed");
        }
    }

    /// <summary>
    /// Get detailed health check information including database version
    /// Example of more complex Entity Framework query in simple service
    /// </summary>
    public async Task<(bool Success, DetailedHealthResponse? Response, string Error)> GetDetailedHealthAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Direct Entity Framework queries - SIMPLE approach
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return (false, null, "Database connection failed");
            }

            // Get database version - simplified approach
            var dbVersion = "PostgreSQL (Connected)";

            // Get multiple statistics in simple queries
            var userCount = await _context.Users
                .AsNoTracking()
                .CountAsync(cancellationToken);

            var activeUserCount = await _context.Users
                .AsNoTracking()
                .Where(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-30))
                .CountAsync(cancellationToken);

            var response = new DetailedHealthResponse
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                DatabaseConnected = canConnect,
                DatabaseVersion = dbVersion,
                UserCount = userCount,
                ActiveUserCount = activeUserCount,
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            };

            _logger.LogInformation("Detailed health check completed - DB: {DatabaseConnected}, Users: {UserCount}/{ActiveUsers}",
                canConnect, userCount, activeUserCount);

            return (true, response, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Detailed health check failed");
            return (false, null, "Detailed health check failed");
        }
    }
}