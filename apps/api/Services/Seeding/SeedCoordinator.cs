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
    private readonly AttendanceSeeder _attendanceSeeder;
    private readonly SessionTicketSeeder _sessionTicketSeeder;
    private readonly VolunteerSeeder _volunteerSeeder;
    private readonly TicketPurchaseSeeder _ticketPurchaseSeeder;
    private readonly VettingSeeder _vettingSeeder;
    private readonly EventSeeder _eventSeeder;
    private readonly VenueSeeder _venueSeeder;
    private readonly EmailTemplateSeeder _emailTemplateSeeder;
    private readonly ILogger<SeedCoordinator> _logger;

    public SeedCoordinator(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        UserSeeder userSeeder,
        SettingsSeeder settingsSeeder,
        CmsSeeder cmsSeeder,
        SafetySeeder safetySeeder,
        AttendanceSeeder attendanceSeeder,
        SessionTicketSeeder sessionTicketSeeder,
        VolunteerSeeder volunteerSeeder,
        TicketPurchaseSeeder ticketPurchaseSeeder,
        VettingSeeder vettingSeeder,
        EventSeeder eventSeeder,
        VenueSeeder venueSeeder,
        EmailTemplateSeeder emailTemplateSeeder,
        ILogger<SeedCoordinator> logger)
    {
        _context = context;
        _userManager = userManager;
        _userSeeder = userSeeder;
        _settingsSeeder = settingsSeeder;
        _cmsSeeder = cmsSeeder;
        _safetySeeder = safetySeeder;
        _attendanceSeeder = attendanceSeeder;
        _sessionTicketSeeder = sessionTicketSeeder;
        _volunteerSeeder = volunteerSeeder;
        _ticketPurchaseSeeder = ticketPurchaseSeeder;
        _vettingSeeder = vettingSeeder;
        _eventSeeder = eventSeeder;
        _venueSeeder = venueSeeder;
        _emailTemplateSeeder = emailTemplateSeeder;
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

            _logger.LogDebug("Seeding email test data defaults...");
            await _settingsSeeder.SeedEmailTestDataAsync(cancellationToken);

            _logger.LogDebug("Seeding CMS content...");
            await _cmsSeeder.SeedCmsContentAsync(cancellationToken);

            _logger.LogDebug("Seeding venues...");
            await _venueSeeder.SeedVenuesAsync(cancellationToken);

            _logger.LogDebug("Seeding events...");
            await _eventSeeder.SeedEventsAsync(cancellationToken);

            _logger.LogDebug("Seeding sessions and tickets...");
            await _sessionTicketSeeder.SeedSessionsAndTicketsAsync(cancellationToken);

            // NOTE: SeedTicketPurchasesAsync is now replaced by specific methods below
            // All class events are handled by SeedSpecificClassEventTicketsAsync
            // All social event donations are handled by SeedSocialEventDonationTicketsAsync
            // Historical workshops are handled by SeedHistoricalWorkshopTicketsAsync

            _logger.LogDebug("Seeding historical workshop tickets with check-ins and cancellations...");
            await _ticketPurchaseSeeder.SeedHistoricalWorkshopTicketsAsync(_eventSeeder, cancellationToken);

            _logger.LogDebug("Seeding specific class event tickets (Suspension Basics, etc.)...");
            await _ticketPurchaseSeeder.SeedSpecificClassEventTicketsAsync(cancellationToken);

            _logger.LogDebug("Seeding social event donation tickets...");
            await _ticketPurchaseSeeder.SeedSocialEventDonationTicketsAsync(cancellationToken);

            _logger.LogDebug("Seeding RSVP attendances for social events...");
            await _attendanceSeeder.SeedEventParticipationsAsync(cancellationToken);

            _logger.LogDebug("Seeding historical social event RSVPs with check-ins and cancellations...");
            await _attendanceSeeder.SeedHistoricalSocialEventRSVPs(_eventSeeder, cancellationToken);

            _logger.LogDebug("Seeding volunteer positions...");
            await _volunteerSeeder.SeedVolunteerPositionsAsync(cancellationToken);

            _logger.LogDebug("Seeding historical volunteer positions and assignments...");
            await _volunteerSeeder.SeedHistoricalVolunteerPositionsAsync(_eventSeeder, cancellationToken);

            _logger.LogDebug("Seeding vetting statuses...");
            await _vettingSeeder.SeedVettingStatusesAsync(cancellationToken);

            _logger.LogDebug("Seeding vetting applications...");
            await _vettingSeeder.SeedVettingApplicationsAsync(cancellationToken);

            // Dev/staging uses admin@witchcityrope.com (default), production uses ropemaster@witchcityrope.com
            // Note: Vetting email templates are now seeded by EmailTemplateSeeder.SeedVettingTemplatesAsync()
            _logger.LogDebug("Seeding email templates (Events, Admin, Incident, Ad Hoc)...");
            await _emailTemplateSeeder.SeedAsync(adminUserEmail: "admin@witchcityrope.com", cancellationToken);

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
        var globalEmailTemplateCount = await _context.GlobalEmailTemplates.CountAsync(cancellationToken);

        var isRequired = userCount == 0 || eventCount == 0 || vettingApplicationCount == 0 || ticketPurchaseCount == 0 || safetyIncidentCount == 0 || globalEmailTemplateCount == 0;

        _logger.LogDebug("Seed data check: Users={UserCount}, Events={EventCount}, VettingApplications={VettingApplicationCount}, TicketPurchases={TicketPurchaseCount}, SafetyIncidents={SafetyIncidentCount}, GlobalEmailTemplates={GlobalEmailTemplateCount}, Required={IsRequired}",
            userCount, eventCount, vettingApplicationCount, ticketPurchaseCount, safetyIncidentCount, globalEmailTemplateCount, isRequired);

        return isRequired;
    }

    /// <summary>
    /// Seeds only production-essential data: roles, admin user, CMS content, and email templates.
    /// This is for initial production setup only - does not seed test users or event data.
    ///
    /// Production essentials include:
    /// - Roles (Administrator, Teacher, SafetyTeam, etc.) via UserSeeder
    /// - Single admin user (ropemaster@witchcityrope.com, password: Test123!) via UserSeeder
    /// - CMS content (pages, menus) via CmsSeeder
    /// - Email templates (all 23 templates) via EmailTemplateSeeder
    ///
    /// Triggered automatically by DatabaseInitializationService when Production environment
    /// database is empty (no admin user exists).
    /// Idempotent - safe to run multiple times (skips if admin user already exists).
    /// </summary>
    public async Task<InitializationResult> SeedProductionEssentialsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new InitializationResult
        {
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        _logger.LogInformation("Starting production essentials seed data population");

        // Check if admin user already exists
        var adminExists = await _userManager.Users.AnyAsync(u => u.Email == "ropemaster@witchcityrope.com", cancellationToken);
        if (adminExists)
        {
            _logger.LogInformation("Production essentials already seeded (admin user exists), skipping");
            result.Success = true;
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var initialRecordCount = await _userManager.Users.CountAsync(cancellationToken);

            // Seed only production essentials in dependency order
            _logger.LogDebug("Seeding roles...");
            await _userSeeder.SeedRolesAsync(cancellationToken);

            _logger.LogDebug("Seeding admin user (ropemaster)...");
            await _userSeeder.SeedAdminUserAsync(cancellationToken);

            _logger.LogDebug("Seeding CMS content...");
            await _cmsSeeder.SeedCmsContentAsync(cancellationToken);

            // Production uses ropemaster@witchcityrope.com admin user, not admin@witchcityrope.com
            _logger.LogDebug("Seeding email templates...");
            await _emailTemplateSeeder.SeedAsync("ropemaster@witchcityrope.com", cancellationToken);

            // Calculate records created
            var finalRecordCount = await _userManager.Users.CountAsync(cancellationToken);
            result.SeedRecordsCreated = finalRecordCount - initialRecordCount;

            await transaction.CommitAsync(cancellationToken);

            result.Success = true;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation("Production essentials seed completed successfully in {Duration}ms. " +
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

            _logger.LogError(ex, "Production essentials seed failed after {Duration}ms",
                stopwatch.Elapsed.TotalMilliseconds);

            throw;
        }
    }
}
