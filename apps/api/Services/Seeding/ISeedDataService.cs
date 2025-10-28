namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Service interface for managing database seed data operations.
/// Provides comprehensive test data population for development and staging environments.
/// Extracted from original Services namespace for better organization.
/// </summary>
public interface ISeedDataService
{
    /// <summary>
    /// Populates all seed data categories in a single transactional operation.
    /// Uses EF Core transaction management for consistency and rollback capability.
    ///
    /// This method coordinates seeding of users, events, and configuration data
    /// while ensuring idempotent behavior (safe to run multiple times).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>
    /// InitializationResult containing:
    /// - Success status and error details
    /// - Count of records created
    /// - Duration and timing information
    /// - Warnings for non-critical issues
    /// </returns>
    Task<InitializationResult> SeedAllDataAsync(CancellationToken cancellationToken = default);
}
