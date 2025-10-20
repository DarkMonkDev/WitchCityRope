namespace WitchCityRope.Api.Services;

/// <summary>
/// Service interface for managing database seed data operations.
/// Provides comprehensive test data population for development and staging environments
/// using EF Core patterns and idempotent operations.
/// 
/// Implementation follows service layer architecture patterns with:
/// - Result pattern for consistent error handling
/// - CancellationToken support for long-running operations  
/// - Transaction management for data consistency
/// - Structured logging for operational visibility
/// 
/// Seed data includes:
/// - Test user accounts with proper authentication setup
/// - Sample events with realistic data and proper UTC handling
/// - Vetting status configuration for development workflows
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

    /// <summary>
    /// Creates all required ASP.NET Core Identity roles for the application.
    /// Ensures roles exist before user creation and assignment.
    ///
    /// Roles created: Administrator, Teacher, Member, Attendee
    /// Implements idempotent behavior (safe to run multiple times).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task SeedRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates test user accounts for development and testing scenarios.
    /// Includes comprehensive account types: Admin, Teacher, Member, Guest, Organizer.
    ///
    /// Follows existing ApplicationUser patterns and ASP.NET Core Identity integration.
    /// All accounts use documented secure password: "Test123!"
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task SeedUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates sample events for testing event management functionality.
    /// Includes variety of event types, dates, and capacity scenarios.
    /// 
    /// Events follow existing Event entity structure with proper UTC DateTime handling
    /// and realistic pricing, capacity, and scheduling information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task SeedEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Populates vetting status configuration data for testing vetting workflows.
    /// Creates baseline vetting status records needed for user management.
    ///
    /// Supports development testing of vetting application and approval processes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task SeedVettingStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates sample vetting applications for testing the vetting system UI.
    /// Includes variety of application statuses, realistic user data, and proper audit trails.
    ///
    /// Applications include different statuses (UnderReview, InterviewApproved, PendingInterview,
    /// Approved, OnHold, Denied) with realistic names, scene names, FetLife handles, and
    /// application text that matches what's shown in the vetting wireframes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task SeedVettingApplicationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates EventParticipation records for RSVPs and ticket purchases.
    /// This provides proper data for testing RSVP/ticket functionality.
    ///
    /// Creates realistic participation data for both social events (RSVPs) and
    /// class events (ticket purchases) using multiple test users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task SeedEventParticipationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates sample safety incident reports for testing the safety management system.
    /// Includes variety of incident statuses, assignments, and realistic scenarios.
    ///
    /// Incidents include different statuses (ReportSubmitted, InformationGathering,
    /// ReviewingFinalReport, OnHold, Closed) with both anonymous and non-anonymous reports,
    /// various locations, and assigned coordinators. All sensitive data is properly encrypted.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    Task SeedSafetyIncidentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if seed data population is required by examining existing data.
    /// Used for optimization to avoid unnecessary seeding operations.
    /// 
    /// Implements idempotent checks to ensure seed operations are only
    /// performed when database is empty or missing expected data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>True if seeding is needed, false if data already exists</returns>
    Task<bool> IsSeedDataRequiredAsync(CancellationToken cancellationToken = default);
}