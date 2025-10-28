using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Models;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Coordinates all seed data operations by orchestrating specialized seeder components.
/// Implements ISeedDataService interface and provides transaction management for consistency.
/// Extracted from monolithic SeedDataService.cs (3,800+ lines) for better maintainability.
///
/// Key responsibilities:
/// - Orchestrate 10 specialized seeders in correct dependency order
/// - Provide transaction management with rollback capability
/// - Check if seeding is required to avoid unnecessary work
/// - Calculate metrics and return comprehensive initialization results
/// - Implement fail-fast error handling with structured logging
///
/// Seeding order (critical for foreign key dependencies):
/// 1. Roles (UserSeeder) - Required before users
/// 2. Users (UserSeeder) - Required before events, vetting, etc.
/// 3. Settings (SettingsSeeder) - Application configuration
/// 4. CMS Content (CmsSeeder) - Static pages
/// 5. Events (EventSeeder) - Base events required for sessions
/// 6. Sessions and Tickets (SessionTicketSeeder) - Requires events
/// 7. Ticket Purchases (TicketPurchaseSeeder) - Requires tickets and users
/// 8. Event Participations (ParticipationSeeder) - Requires events and users
/// 9. Volunteer Positions (VolunteerSeeder) - Requires events
/// 10. Vetting (VettingSeeder) - Statuses, applications, email templates
/// 11. Safety Incidents (SafetySeeder) - Requires users
///
/// Implementation follows existing service patterns with proper UTC datetime handling,
/// structured logging, and result pattern for error reporting.
/// </summary>
public class SeedCoordinator : ISeedDataService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserSeeder _userSeeder;
    private readonly SettingsSeeder _settingsSeeder;
    private readonly CmsSeeder _cmsSeeder;
    private readonly SafetySeeder _safetySeeder;
    private readonly ParticipationSeeder _participationSeeder;
    private readonly SessionTicketSeeder _sessionTicketSeeder;
    private readonly VolunteerSeeder _volunteerSeeder;
    private readonly TicketPurchaseSeeder _ticketPurchaseSeeder;
    private readonly VettingSeeder _vettingSeeder;
    private readonly EventSeeder _eventSeeder;
    private readonly ILogger<SeedCoordinator> _logger;

    public SeedCoordinator(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        UserSeeder userSeeder,
        SettingsSeeder settingsSeeder,
        CmsSeeder cmsSeeder,
        SafetySeeder safetySeeder,
        ParticipationSeeder participationSeeder,
        SessionTicketSeeder sessionTicketSeeder,
        VolunteerSeeder volunteerSeeder,
        TicketPurchaseSeeder ticketPurchaseSeeder,
        VettingSeeder vettingSeeder,
        EventSeeder eventSeeder,
        ILogger<SeedCoordinator> logger)
    {
        _context = context;
        _userManager = userManager;
        _userSeeder = userSeeder;
        _settingsSeeder = settingsSeeder;
        _cmsSeeder = cmsSeeder;
        _safetySeeder = safetySeeder;
        _participationSeeder = participationSeeder;
        _sessionTicketSeeder = sessionTicketSeeder;
        _volunteerSeeder = volunteerSeeder;
        _ticketPurchaseSeeder = ticketPurchaseSeeder;
        _vettingSeeder = vettingSeeder;
        _eventSeeder = eventSeeder;
        _logger = logger;
    }

    /// <summary>
    /// Coordinates all seed data operations in a single transaction.
    /// Provides comprehensive error handling and rollback capability.
    ///
    /// Uses EF Core transaction management to ensure data consistency
    /// and follows result pattern for error reporting.
    ///
    /// Implements idempotent behavior - safe to run multiple times.
    /// Checks if seeding is required before executing to avoid unnecessary work.
    /// </summary>
    public async Task<InitializationResult> SeedAllDataAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new InitializationResult
        {
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        _logger.LogInformation("Starting comprehensive seed data population");

        // Check if seeding is required to avoid unnecessary work
        if (!await IsSeedDataRequiredAsync(cancellationToken))
        {
            _logger.LogInformation("Seed data already exists, skipping population");
            result.Success = true;
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var initialUserCount = await _userManager.Users.CountAsync(cancellationToken);
            var initialEventCount = await _context.Events.CountAsync(cancellationToken);

            // Seed data operations in logical order following dependency hierarchy
            _logger.LogDebug("Seeding roles...");
            await _userSeeder.SeedRolesAsync(cancellationToken);

            _logger.LogDebug("Seeding users...");
            await _userSeeder.SeedUsersAsync(cancellationToken);

            _logger.LogDebug("Seeding settings...");
            await _settingsSeeder.SeedSettingsAsync(cancellationToken);

            _logger.LogDebug("Seeding CMS content...");
            await _cmsSeeder.SeedCmsContentAsync(cancellationToken);

            _logger.LogDebug("Seeding events...");
            await _eventSeeder.SeedEventsAsync(cancellationToken);

            _logger.LogDebug("Seeding sessions and tickets...");
            await _sessionTicketSeeder.SeedSessionsAndTicketsAsync(cancellationToken);

            _logger.LogDebug("Seeding ticket purchases...");
            await _ticketPurchaseSeeder.SeedTicketPurchasesAsync(cancellationToken);

            _logger.LogDebug("Seeding historical workshop tickets with check-ins and cancellations...");
            await _ticketPurchaseSeeder.SeedHistoricalWorkshopTicketsAsync(_eventSeeder, cancellationToken);

            _logger.LogDebug("Seeding event participations...");
            await _participationSeeder.SeedEventParticipationsAsync(cancellationToken);

            _logger.LogDebug("Seeding historical social event RSVPs with check-ins and cancellations...");
            await _participationSeeder.SeedHistoricalSocialEventRSVPs(_eventSeeder, cancellationToken);

            _logger.LogDebug("Seeding volunteer positions...");
            await _volunteerSeeder.SeedVolunteerPositionsAsync(cancellationToken);

            _logger.LogDebug("Seeding historical volunteer positions and assignments...");
            await _volunteerSeeder.SeedHistoricalVolunteerPositionsAsync(_eventSeeder, cancellationToken);

            _logger.LogDebug("Seeding vetting statuses...");
            await _vettingSeeder.SeedVettingStatusesAsync(cancellationToken);

            _logger.LogDebug("Seeding vetting applications...");
            await _vettingSeeder.SeedVettingApplicationsAsync(cancellationToken);

            _logger.LogDebug("Seeding vetting email templates...");
            await _vettingSeeder.SeedVettingEmailTemplatesAsync(cancellationToken);

            _logger.LogDebug("Seeding safety incidents...");
            await _safetySeeder.SeedSafetyIncidentsAsync(cancellationToken);

            // Calculate records created for metrics
            var finalUserCount = await _userManager.Users.CountAsync(cancellationToken);
            var finalEventCount = await _context.Events.CountAsync(cancellationToken);

            result.SeedRecordsCreated = (finalUserCount - initialUserCount) + (finalEventCount - initialEventCount);

            await transaction.CommitAsync(cancellationToken);

            result.Success = true;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Seed data population completed successfully in {Duration}ms. " +
                "Records created: {RecordCount}",
                result.Duration.TotalMilliseconds, result.SeedRecordsCreated);

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);

            result.Success = false;
            result.Errors.Add(ex.Message);
            result.Duration = stopwatch.Elapsed;

            _logger.LogError(ex, "Seed data population failed after {Duration}ms",
                stopwatch.Elapsed.TotalMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// Checks if seed data is required by verifying if key entities exist.
    /// Uses multiple entity checks to determine if database is empty and needs seeding.
    ///
    /// Checks for:
    /// - Users (authentication required for most operations)
    /// - Events (core business entity)
    /// - Vetting Applications (member vetting workflow)
    /// - Ticket Purchases (event registration data)
    /// - Safety Incidents (safety monitoring data)
    ///
    /// Returns true if ANY of these entity types are missing,
    /// indicating database needs comprehensive seed data.
    /// </summary>
    private async Task<bool> IsSeedDataRequiredAsync(CancellationToken cancellationToken)
    {
        var userCount = await _userManager.Users.CountAsync(cancellationToken);
        var eventCount = await _context.Events.CountAsync(cancellationToken);
        var vettingApplicationCount = await _context.VettingApplications.CountAsync(cancellationToken);
        var ticketPurchaseCount = await _context.TicketPurchases.CountAsync(cancellationToken);
        var safetyIncidentCount = await _context.SafetyIncidents.CountAsync(cancellationToken);

        var isRequired = userCount == 0 || eventCount == 0 || vettingApplicationCount == 0 || ticketPurchaseCount == 0 || safetyIncidentCount == 0;

        _logger.LogDebug("Seed data check: Users={UserCount}, Events={EventCount}, VettingApplications={VettingApplicationCount}, TicketPurchases={TicketPurchaseCount}, SafetyIncidents={SafetyIncidentCount}, Required={IsRequired}",
            userCount, eventCount, vettingApplicationCount, ticketPurchaseCount, safetyIncidentCount, isRequired);

        return isRequired;
    }
}
