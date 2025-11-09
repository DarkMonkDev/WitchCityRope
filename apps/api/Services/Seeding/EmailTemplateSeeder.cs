using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Seeds default email templates for Events, Admin, Incident, and Ad Hoc categories.
/// Vetting templates are migrated automatically from VettingEmailTemplates table.
/// </summary>
public class EmailTemplateSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailTemplateSeeder> _logger;

    public EmailTemplateSeeder(ApplicationDbContext context, ILogger<EmailTemplateSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds 22 default email templates across 5 categories.
    /// Vetting templates (6) are migrated by database migration.
    /// This method seeds the remaining 16 templates.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting email template seeding...");

        // Get admin user ID for UpdatedBy field
        var adminUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "admin@witchcityrope.com", cancellationToken);

        if (adminUser == null)
        {
            _logger.LogError("Admin user not found - cannot seed email templates");
            return;
        }

        var adminUserId = adminUser.Id;

        // Check if Vetting templates were migrated
        var existingVettingTemplates = await _context.GlobalEmailTemplates
            .Where(t => t.Category == EmailCategory.Vetting)
            .CountAsync(cancellationToken);

        if (existingVettingTemplates > 0)
        {
            _logger.LogInformation("Vetting templates already migrated ({Count} templates), skipping...", existingVettingTemplates);
        }
        else
        {
            _logger.LogWarning("No Vetting templates found - migration may not have run");
        }

        // Seed Events templates (7)
        await SeedEventsTemplatesAsync(adminUserId, cancellationToken);

        // Seed Admin templates (4)
        await SeedAdminTemplatesAsync(adminUserId, cancellationToken);

        // Seed Incident templates (4)
        await SeedIncidentTemplatesAsync(adminUserId, cancellationToken);

        // Seed Ad Hoc template (1)
        await SeedAdHocTemplateAsync(adminUserId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email template seeding completed");
    }

    private async Task SeedEventsTemplatesAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding Events templates (7)...");

        var templates = new[]
        {
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "Confirmation",
                Subject = "Your ticket for {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>Thank you for registering for <strong>{{event_title}}</strong>!</p><p><strong>Event Details:</strong><br>Date: {{event_date}}<br>Time: {{event_time}}<br>Venue: {{venue_name}}<br>Address: {{venue_address}}</p><p><strong>Ticket Type:</strong> {{ticket_type}}<br><strong>Total Paid:</strong> {{total_paid}}<br><strong>Confirmation Number:</strong> {{confirmation_number}}</p><p>We look forward to seeing you!</p><p>Questions? Email {{organizer_email}}</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nThank you for registering for {{event_title}}!\n\nEvent Details:\nDate: {{event_date}}\nTime: {{event_time}}\nVenue: {{venue_name}}\nAddress: {{venue_address}}\n\nTicket Type: {{ticket_type}}\nTotal Paid: {{total_paid}}\nConfirmation Number: {{confirmation_number}}\n\nWe look forward to seeing you!\n\nQuestions? Email {{organizer_email}}",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}", "{{ticket_type}}", "{{total_paid}}", "{{confirmation_number}}", "{{organizer_email}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "Reminder1Week",
                Subject = "One week until {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>Just a friendly reminder that <strong>{{event_title}}</strong> is coming up in one week!</p><p><strong>Event Details:</strong><br>Date: {{event_date}}<br>Time: {{event_time}}<br>Venue: {{venue_name}}<br>Address: {{venue_address}}</p><p>We're looking forward to seeing you there!</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nJust a friendly reminder that {{event_title}} is coming up in one week!\n\nEvent Details:\nDate: {{event_date}}\nTime: {{event_time}}\nVenue: {{venue_name}}\nAddress: {{venue_address}}\n\nWe're looking forward to seeing you there!",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "Reminder1Day",
                Subject = "Tomorrow: {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p><strong>{{event_title}}</strong> is tomorrow!</p><p><strong>When:</strong> {{event_date}} at {{event_time}}<br><strong>Where:</strong> {{venue_name}}<br>{{venue_address}}</p><p>See you there!</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\n{{event_title}} is tomorrow!\n\nWhen: {{event_date}} at {{event_time}}\nWhere: {{venue_name}}\n{{venue_address}}\n\nSee you there!",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "Reminder2Hours",
                Subject = "Starting soon: {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p><strong>{{event_title}}</strong> starts in 2 hours!</p><p><strong>Time:</strong> {{event_time}}<br><strong>Location:</strong> {{venue_name}}</p><p>See you soon!</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\n{{event_title}} starts in 2 hours!\n\nTime: {{event_time}}\nLocation: {{venue_name}}\n\nSee you soon!",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_time}}", "{{venue_name}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "Cancellation",
                Subject = "Event Cancelled: {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>We regret to inform you that <strong>{{event_title}}</strong> scheduled for {{event_date}} has been cancelled.</p><p>{{custom_message}}</p><p>If you have any questions, please contact {{organizer_email}}</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nWe regret to inform you that {{event_title}} scheduled for {{event_date}} has been cancelled.\n\n{{custom_message}}\n\nIf you have any questions, please contact {{organizer_email}}",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{organizer_email}}", "{{custom_message}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "SessionChange",
                Subject = "Session Update: {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>There has been an update to the session <strong>{{session_name}}</strong> for <strong>{{event_title}}</strong>.</p><p><strong>New Date/Time:</strong> {{event_date}} at {{event_time}}</p><p>{{custom_message}}</p><p>Thank you for your understanding.</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nThere has been an update to the session {{session_name}} for {{event_title}}.\n\nNew Date/Time: {{event_date}} at {{event_time}}\n\n{{custom_message}}\n\nThank you for your understanding.",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{session_name}}", "{{event_date}}", "{{event_time}}", "{{custom_message}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "ThankYou",
                Subject = "Thank you for attending {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>Thank you for attending <strong>{{event_title}}</strong> on {{event_date}}!</p><p>We hope you had a wonderful experience and learned new skills. If you have any feedback or questions, please don't hesitate to reach out to {{organizer_email}}</p><p>We look forward to seeing you at future events!</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nThank you for attending {{event_title}} on {{event_date}}!\n\nWe hope you had a wonderful experience and learned new skills. If you have any feedback or questions, please don't hesitate to reach out to {{organizer_email}}\n\nWe look forward to seeing you at future events!",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{organizer_email}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            }
        };

        foreach (var template in templates)
        {
            var exists = await _context.GlobalEmailTemplates
                .AnyAsync(t => t.Category == template.Category && t.TemplateType == template.TemplateType, cancellationToken);

            if (!exists)
            {
                await _context.GlobalEmailTemplates.AddAsync(template, cancellationToken);
                _logger.LogInformation("Added Events template: {TemplateType}", template.TemplateType);
            }
            else
            {
                _logger.LogInformation("Events template already exists: {TemplateType}", template.TemplateType);
            }
        }
    }

    private async Task SeedAdminTemplatesAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding Admin templates (4)...");

        var templates = new[]
        {
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Admin,
                TemplateType = "AccountCreated",
                Subject = "Welcome to WitchCityRope - Account Created",
                HtmlBody = "<p>Hi {{user_name}},</p><p>Your WitchCityRope account has been created!</p><p><strong>Email:</strong> {{account_email}}</p><p>You can log in at {{system_url}}</p><p>If you have any questions, contact us at {{support_email}}</p>",
                PlainTextBody = "Hi {{user_name}},\n\nYour WitchCityRope account has been created!\n\nEmail: {{account_email}}\n\nYou can log in at {{system_url}}\n\nIf you have any questions, contact us at {{support_email}}",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{account_email}}", "{{system_url}}", "{{support_email}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Admin,
                TemplateType = "PasswordReset",
                Subject = "Password Reset Request - WitchCityRope",
                HtmlBody = "<p>Hi {{user_name}},</p><p>A password reset has been requested for your account.</p><p>If you made this request, please reset your password at {{system_url}}</p><p>If you did not request this, please ignore this email and contact {{support_email}}</p>",
                PlainTextBody = "Hi {{user_name}},\n\nA password reset has been requested for your account.\n\nIf you made this request, please reset your password at {{system_url}}\n\nIf you did not request this, please ignore this email and contact {{support_email}}",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{system_url}}", "{{support_email}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Admin,
                TemplateType = "RoleChanged",
                Subject = "Your WitchCityRope Role Has Been Updated",
                HtmlBody = "<p>Hi {{user_name}},</p><p>Your role in the WitchCityRope system has been updated.</p><p>{{action_required}}</p><p>If you have questions about this change, please contact {{support_email}}</p>",
                PlainTextBody = "Hi {{user_name}},\n\nYour role in the WitchCityRope system has been updated.\n\n{{action_required}}\n\nIf you have questions about this change, please contact {{support_email}}",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{action_required}}", "{{support_email}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Admin,
                TemplateType = "SystemNotification",
                Subject = "WitchCityRope System Notification",
                HtmlBody = "<p>Hi {{user_name}},</p><p>{{action_required}}</p><p><strong>Deadline:</strong> {{deadline_date}}</p><p>For assistance, contact {{support_email}}</p>",
                PlainTextBody = "Hi {{user_name}},\n\n{{action_required}}\n\nDeadline: {{deadline_date}}\n\nFor assistance, contact {{support_email}}",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{action_required}}", "{{deadline_date}}", "{{support_email}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            }
        };

        foreach (var template in templates)
        {
            var exists = await _context.GlobalEmailTemplates
                .AnyAsync(t => t.Category == template.Category && t.TemplateType == template.TemplateType, cancellationToken);

            if (!exists)
            {
                await _context.GlobalEmailTemplates.AddAsync(template, cancellationToken);
                _logger.LogInformation("Added Admin template: {TemplateType}", template.TemplateType);
            }
            else
            {
                _logger.LogInformation("Admin template already exists: {TemplateType}", template.TemplateType);
            }
        }
    }

    private async Task SeedIncidentTemplatesAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding Incident templates (4)...");

        var templates = new[]
        {
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Incident,
                TemplateType = "ReportReceived",
                Subject = "Incident Report Received - #{{incident_number}}",
                HtmlBody = "<p>Hi {{reporter_name}},</p><p>We have received your incident report #{{incident_number}} from {{incident_date}}.</p><p>Your assigned coordinator is {{coordinator_name}}.</p><p><strong>Next Steps:</strong><br>{{next_steps}}</p><p>Thank you for reporting this.</p>",
                PlainTextBody = "Hi {{reporter_name}},\n\nWe have received your incident report #{{incident_number}} from {{incident_date}}.\n\nYour assigned coordinator is {{coordinator_name}}.\n\nNext Steps:\n{{next_steps}}\n\nThank you for reporting this.",
                Variables = JsonSerializer.Serialize(new[] { "{{reporter_name}}", "{{incident_number}}", "{{incident_date}}", "{{coordinator_name}}", "{{next_steps}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Incident,
                TemplateType = "StatusUpdate",
                Subject = "Incident #{{incident_number}} Status Update",
                HtmlBody = "<p>Hi {{reporter_name}},</p><p>There is an update on incident #{{incident_number}}.</p><p><strong>Status:</strong> {{status}}</p><p><strong>Next Steps:</strong><br>{{next_steps}}</p>",
                PlainTextBody = "Hi {{reporter_name}},\n\nThere is an update on incident #{{incident_number}}.\n\nStatus: {{status}}\n\nNext Steps:\n{{next_steps}}",
                Variables = JsonSerializer.Serialize(new[] { "{{reporter_name}}", "{{incident_number}}", "{{status}}", "{{next_steps}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Incident,
                TemplateType = "AssignmentNotification",
                Subject = "You've been assigned to Incident #{{incident_number}}",
                HtmlBody = "<p>Hi {{coordinator_name}},</p><p>You have been assigned to incident #{{incident_number}} from {{incident_date}}.</p><p><strong>Next Steps:</strong><br>{{next_steps}}</p><p>Please review and take appropriate action.</p>",
                PlainTextBody = "Hi {{coordinator_name}},\n\nYou have been assigned to incident #{{incident_number}} from {{incident_date}}.\n\nNext Steps:\n{{next_steps}}\n\nPlease review and take appropriate action.",
                Variables = JsonSerializer.Serialize(new[] { "{{coordinator_name}}", "{{incident_number}}", "{{incident_date}}", "{{next_steps}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Incident,
                TemplateType = "Resolved",
                Subject = "Incident #{{incident_number}} Resolved",
                HtmlBody = "<p>Hi {{reporter_name}},</p><p>Incident #{{incident_number}} from {{incident_date}} has been resolved.</p><p><strong>Next Steps:</strong><br>{{next_steps}}</p><p>Thank you for your patience.</p>",
                PlainTextBody = "Hi {{reporter_name}},\n\nIncident #{{incident_number}} from {{incident_date}} has been resolved.\n\nNext Steps:\n{{next_steps}}\n\nThank you for your patience.",
                Variables = JsonSerializer.Serialize(new[] { "{{reporter_name}}", "{{incident_number}}", "{{incident_date}}", "{{next_steps}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            }
        };

        foreach (var template in templates)
        {
            var exists = await _context.GlobalEmailTemplates
                .AnyAsync(t => t.Category == template.Category && t.TemplateType == template.TemplateType, cancellationToken);

            if (!exists)
            {
                await _context.GlobalEmailTemplates.AddAsync(template, cancellationToken);
                _logger.LogInformation("Added Incident template: {TemplateType}", template.TemplateType);
            }
            else
            {
                _logger.LogInformation("Incident template already exists: {TemplateType}", template.TemplateType);
            }
        }
    }

    private async Task SeedAdHocTemplateAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding Ad Hoc template (1)...");

        var template = new GlobalEmailTemplate
        {
            Category = EmailCategory.AdHoc,
            TemplateType = "General",
            Subject = "Message from WitchCityRope",
            HtmlBody = "<p>Hi {{recipient_name}},</p><p>{{custom_content}}</p>",
            PlainTextBody = "Hi {{recipient_name}},\n\n{{custom_content}}",
            Variables = JsonSerializer.Serialize(new[] { "{{recipient_name}}", "{{custom_content}}" }),
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = adminUserId
        };

        var exists = await _context.GlobalEmailTemplates
            .AnyAsync(t => t.Category == template.Category && t.TemplateType == template.TemplateType, cancellationToken);

        if (!exists)
        {
            await _context.GlobalEmailTemplates.AddAsync(template, cancellationToken);
            _logger.LogInformation("Added Ad Hoc template: {TemplateType}", template.TemplateType);
        }
        else
        {
            _logger.LogInformation("Ad Hoc template already exists: {TemplateType}", template.TemplateType);
        }
    }
}
