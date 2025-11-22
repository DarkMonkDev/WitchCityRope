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

    /// <summary>
    /// Seeds only production-essential data: roles, admin user, CMS content, and email templates.
    /// Used for initial production environment setup only.
    ///
    /// Production essentials include:
    /// - All system roles (Administrator, Teacher, SafetyTeam, etc.)
    /// - Single admin user (ropemaster@witchcityrope.com, password: Test123!)
    /// - All CMS content pages
    /// - All 23 email templates across 5 categories
    ///
    /// Triggered automatically by DatabaseInitializationService when Production environment
    /// database is empty (no admin user exists).
    /// Idempotent - safe to run multiple times (skips if admin user already exists).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>
    /// InitializationResult containing:
    /// - Success status and error details
    /// - Count of records created (roles + admin user + CMS + templates)
    /// - Duration and timing information
    /// - Environment name
    /// </returns>
    Task<InitializationResult> SeedProductionEssentialsAsync(CancellationToken cancellationToken = default);
}
