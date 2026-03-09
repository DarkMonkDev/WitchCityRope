using Microsoft.EntityFrameworkCore;
using WitchCityRope.Api.Core.Entities;
using WitchCityRope.Api.Data;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Handles seeding of application settings.
/// Extracted from SeedDataService.cs for better maintainability.
/// Responsible for creating system configuration settings.
/// </summary>
public class SettingsSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsSeeder> _logger;

    public SettingsSeeder(
        ApplicationDbContext context,
        ILogger<SettingsSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds application settings including timezone configuration.
    /// Idempotent operation - skips if settings already exist.
    ///
    /// Settings created:
    /// - EventTimeZone: IANA timezone ID for event scheduling (America/New_York)
    /// </summary>
    public async Task SeedSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Settings.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Settings already exist, skipping seed");
            return;
        }

        var now = DateTime.UtcNow;
        var settings = new List<Setting>
        {
            new Setting
            {
                Id = Guid.NewGuid(),
                Key = "EventTimeZone",
                Value = "America/New_York",
                Description = "IANA timezone ID for event scheduling (Eastern Time)",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        await _context.Settings.AddRangeAsync(settings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created {Count} default settings", settings.Count);
    }

    /// <summary>
    /// Seeds default test data values for email template testing.
    /// Only creates settings that don't already exist (preserves user customizations).
    /// </summary>
    public async Task SeedEmailTestDataAsync(CancellationToken cancellationToken = default)
    {
        var testDataDefaults = new Dictionary<string, string>
        {
            // ── Global Variables ──
            ["EmailTestData:user_name"] = "Jane Doe",
            ["EmailTestData:system_url"] = "https://witchcityrope.com",
            ["EmailTestData:custom_message"] = "This is a test custom message for preview purposes.",
            ["EmailTestData:custom_content"] = "This is test custom content for preview purposes.",

            // ── Vetting Variables ──
            ["EmailTestData:scene_name"] = "Dark Phoenix",
            ["EmailTestData:application_number"] = "APP-2026-0042",
            ["EmailTestData:submission_date"] = "March 1, 2026",
            ["EmailTestData:application_date"] = "March 1, 2026",
            ["EmailTestData:status_change_date"] = "March 5, 2026",
            ["EmailTestData:current_status"] = "Under Review",
            ["EmailTestData:interview_link"] = "https://witchcityrope.com/vetting/interview/sample-link",
            ["EmailTestData:approval_date"] = "March 8, 2026",
            ["EmailTestData:hold_reason"] = "Additional references needed",
            ["EmailTestData:required_actions"] = "Please provide two community references",
            ["EmailTestData:review_date"] = "March 10, 2026",

            // ── Events Variables ──
            ["EmailTestData:attendee_name"] = "Jane Doe",
            ["EmailTestData:event_title"] = "Introduction to Shibari",
            ["EmailTestData:event_date"] = "Saturday, March 15, 2026",
            ["EmailTestData:event_time"] = "7:00 PM EST",
            ["EmailTestData:venue_name"] = "The Witch City Studio",
            ["EmailTestData:venue_address"] = "123 Essex Street, Salem, MA 01970",
            ["EmailTestData:ticket_type"] = "General Admission",
            ["EmailTestData:total_paid"] = "$35.00",
            ["EmailTestData:confirmation_number"] = "WCR-2026-1234",
            ["EmailTestData:session_name"] = "Beginner Ties - Session A",

            // ── Admin Variables ──
            ["EmailTestData:account_email"] = "testuser@example.com",
            ["EmailTestData:reset_url"] = "https://witchcityrope.com/auth/reset-password?token=sample-token",
            ["EmailTestData:action_required"] = "Please update your profile information",
            ["EmailTestData:deadline_date"] = "March 20, 2026",
            ["EmailTestData:verification_url"] = "https://witchcityrope.com/auth/verify-email?token=sample-token",
            ["EmailTestData:refund_amount"] = "$35.00",
            ["EmailTestData:original_amount"] = "$45.00",
            ["EmailTestData:payment_method"] = "PayPal",
            ["EmailTestData:timing_message"] = "Your refund will be processed within 5-10 business days.",
            ["EmailTestData:refund_reason"] = "Event cancelled by organizer",
            ["EmailTestData:refund_id"] = "REF-2026-5678",

            // ── Incident Variables ──
            ["EmailTestData:reporter_name"] = "Jane Doe",
            ["EmailTestData:incident_number"] = "INC-2026-0001",
            ["EmailTestData:incident_date"] = "March 8, 2026",
            ["EmailTestData:coordinator_name"] = "Safety Coordinator",
            ["EmailTestData:next_steps"] = "An investigation has been initiated. You will be contacted within 48 hours.",
            ["EmailTestData:status"] = "Under Investigation",

            // ── Ad Hoc Variables ──
            ["EmailTestData:recipient_name"] = "Community Member",
        };

        // Only seed settings that don't already exist (preserve user customizations)
        var existingKeys = await _context.Settings
            .Where(s => s.Key.StartsWith("EmailTestData:"))
            .Select(s => s.Key)
            .ToListAsync(cancellationToken);

        var newSettings = testDataDefaults
            .Where(kvp => !existingKeys.Contains(kvp.Key))
            .Select(kvp => new Setting
            {
                Id = Guid.NewGuid(),
                Key = kvp.Key,
                Value = kvp.Value,
                Description = $"Email template test data: {kvp.Key.Replace("EmailTestData:", "")}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            })
            .ToList();

        if (newSettings.Count > 0)
        {
            _context.Settings.AddRange(newSettings);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} email test data defaults", newSettings.Count);
        }
        else
        {
            _logger.LogInformation("Email test data already seeded - skipping");
        }
    }
}
