namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Service interface for managing database seed data operations.
/// Provides comprehensive test data population for development and staging environments.
/// Implemented by SeedCoordinator which orchestrates specialized seeder components.
/// </summary>
public interface ISeedDataService
{
    /// <summary>
    /// Seeds all data for development and testing in a transactional manner.
    /// Coordinates specialized seeders for users, events, vetting, safety, and more.
    ///
    /// Implementation (SeedCoordinator) ensures:
    /// - Idempotent operations (safe to run multiple times)
    /// - Transaction-based consistency with rollback capability
    /// - Proper dependency order (roles before users, events before sessions, etc.)
    /// - Comprehensive error handling and logging
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>
    /// InitializationResult containing:
    /// - Success status and error details
    /// - Count of records created
    /// - Duration and timing information
    /// - Environment name
    /// - Warnings for non-critical issues
    /// </returns>
    Task<InitializationResult> SeedAllDataAsync(CancellationToken cancellationToken = default);
}
