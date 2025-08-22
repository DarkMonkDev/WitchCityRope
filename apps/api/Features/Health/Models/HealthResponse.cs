namespace WitchCityRope.Api.Features.Health.Models;

/// <summary>
/// Basic health check response DTO
/// Example of simple response model for NSwag type generation
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// Overall health status of the API
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when health check was performed
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Database connectivity status
    /// </summary>
    public bool DatabaseConnected { get; set; }

    /// <summary>
    /// Total number of users in the system
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// API version
    /// </summary>
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Detailed health check response with additional metrics
/// Example of extended response model showing Entity Framework capabilities
/// </summary>
public class DetailedHealthResponse : HealthResponse
{
    /// <summary>
    /// Database server version information
    /// </summary>
    public string DatabaseVersion { get; set; } = string.Empty;

    /// <summary>
    /// Number of users active in the last 30 days
    /// </summary>
    public int ActiveUserCount { get; set; }

    /// <summary>
    /// Current runtime environment
    /// </summary>
    public string Environment { get; set; } = string.Empty;
}