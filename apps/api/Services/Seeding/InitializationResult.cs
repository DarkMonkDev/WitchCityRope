namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Result object for database initialization operations.
/// Contains success status, metrics, timing information, and error details.
/// Used by both seed data services and database initialization services.
/// Extracted from DatabaseInitializationService.cs for shared use.
/// </summary>
public class InitializationResult
{
    /// <summary>
    /// Indicates if the initialization completed successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Total duration of the initialization operation
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Number of EF Core migrations applied during initialization
    /// </summary>
    public int MigrationsApplied { get; set; }

    /// <summary>
    /// Number of seed records created across all seeding operations
    /// </summary>
    public int SeedRecordsCreated { get; set; }

    /// <summary>
    /// Collection of error messages if initialization failed
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Collection of warning messages for non-critical issues
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Environment name where initialization was performed (Development, Staging, Production)
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when initialization completed
    /// </summary>
    public DateTime CompletedAt { get; set; }
}
