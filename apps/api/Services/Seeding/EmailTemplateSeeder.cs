using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WitchCityRope.Api.Data;
using WitchCityRope.Api.Features.EmailTemplates.Entities;

namespace WitchCityRope.Api.Services.Seeding;

/// <summary>
/// Seeds default email templates for all 5 categories: Vetting, Events, Admin, Incident, and Ad Hoc.
/// Total: 26 templates (Vetting: 6, Events: 10, Admin: 5, Incident: 4, Ad Hoc: 1)
/// Vetting templates are migrated from the legacy VettingEmailTemplates table.
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
    /// Seeds 25 default email templates across 5 categories.
    /// This includes 6 Vetting templates migrated from VettingEmailTemplates table,
    /// plus 20 templates for Events (10), Admin (5), Incident (4), and Ad Hoc (1).
    /// </summary>
    /// <param name="adminUserEmail">Email of admin user for UpdatedBy field. Defaults to admin@witchcityrope.com for dev/staging. Production uses ropemaster@witchcityrope.com.</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    public async Task SeedAsync(string adminUserEmail = "admin@witchcityrope.com", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting email template seeding for admin user: {AdminEmail}...", adminUserEmail);

        // Get admin user ID for UpdatedBy field (accepts custom email for production)
        var adminUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == adminUserEmail, cancellationToken);

        if (adminUser == null)
        {
            _logger.LogError("Admin user not found - cannot seed email templates");
            return;
        }

        var adminUserId = adminUser.Id;

        // Seed Vetting templates (6) - migrated from old VettingEmailTemplates table
        await SeedVettingTemplatesAsync(adminUserId, cancellationToken);

        // Seed Events templates (7)
        await SeedEventsTemplatesAsync(adminUserId, cancellationToken);

        // Seed Admin templates (4)
        await SeedAdminTemplatesAsync(adminUserId, cancellationToken);

        // Seed Incident templates (4)
        await SeedIncidentTemplatesAsync(adminUserId, cancellationToken);

        // Seed Ad Hoc template (1)
        await SeedAdHocTemplateAsync(adminUserId, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // Update trigger configs on existing templates that may not have them set
        await UpdateTriggerConfigsAsync(cancellationToken);

        _logger.LogInformation("Email template seeding completed");
    }

    private async Task SeedVettingTemplatesAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding Vetting templates (6)...");

        var templates = new[]
        {
            new GlobalEmailTemplate
            {
                Id = Guid.Parse("80808080-8080-8080-8080-808080808080"),
                Category = EmailCategory.Vetting,
                TemplateType = "ApplicationReceived",
                Title = "Application Received - {{scene_name}}",
                Subject = "Application Received - {{scene_name}}",
                HtmlBody = "<p style=\"margin-bottom: 16px;\">Hi {{scene_name}},</p><p style=\"margin-bottom: 16px;\">Thank you for submitting your vetting application to WitchCityRope. We have received your application and it is now under review.</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Application Details</h2><p style=\"margin-bottom: 16px;\"><strong>Application Number:</strong> {{application_number}}<br><strong>Submission Date:</strong> {{submission_date}}</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Next Steps</h2><p style=\"margin-bottom: 16px;\">Our vetting team will review your application and contact you within the next 7-10 business days with updates on your status.</p><p style=\"margin-bottom: 16px;\">If you have any questions, please don't hesitate to contact us.</p><p style=\"margin-bottom: 16px;\">Best regards,<br>The Witch City Rope Vetting Team</p><p style=\"margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;\">Questions? Contact us at <a href=\"mailto:info@witchcityrope.com\" style=\"color: #880124;\">info@witchcityrope.com</a></p>",
                PlainTextBody = "Dear {{scene_name}},\n\nThank you for submitting your vetting application to WitchCityRope. We have received your application and it is now under review.\n\nApplication Number: {{application_number}}\nSubmission Date: {{submission_date}}\n\nOur vetting team will review your application and contact you within the next 7-10 business days with updates on your status.\n\nIf you have any questions, please don't hesitate to contact us.\n\nBest regards,\nThe WitchCityRope Vetting Team",
                Variables = JsonSerializer.Serialize(new[] { "{{scene_name}}", "{{application_number}}", "{{submission_date}}", "{{application_date}}", "{{status_change_date}}", "{{current_status}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Id = Guid.Parse("80808080-8080-8080-8080-808080808081"),
                Category = EmailCategory.Vetting,
                TemplateType = "InterviewApproved",
                Title = "Interview Approved - {{scene_name}}",
                Subject = "Interview Approved - {{scene_name}}",
                HtmlBody = "<p style=\"margin-bottom: 16px;\">Hi {{scene_name}},</p><p style=\"margin-bottom: 16px;\">Congratulations! Your vetting application has been approved for the interview stage.</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Application Status</h2><p style=\"margin-bottom: 16px;\"><strong>Application Number:</strong> {{application_number}}<br><strong>Status:</strong> Approved for Interview</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Schedule Your Interview</h2><p style=\"margin-bottom: 16px;\">Please schedule your interview using the link below:</p><p style=\"margin-bottom: 16px;\"><a href=\"{{interview_link}}\" style=\"color: #880124; text-decoration: underline;\">Schedule Interview</a></p><p style=\"margin-bottom: 16px;\">During your interview, we will discuss your experience, interests, and answer any questions you may have about our community.</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Important Information</h2><ul style=\"margin-bottom: 16px; padding-left: 20px;\"><li>Please schedule your interview within the next 14 days</li><li>Prepare questions about WitchCityRope and our community</li><li>Be ready to discuss your rope bondage experience</li></ul><p style=\"margin-bottom: 16px;\">Best regards,<br>The Witch City Rope Vetting Team</p><p style=\"margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;\">Questions? Contact us at <a href=\"mailto:info@witchcityrope.com\" style=\"color: #880124;\">info@witchcityrope.com</a></p>",
                PlainTextBody = "Dear {{scene_name}},\n\nCongratulations! Your vetting application has been approved for the interview stage.\n\nApplication Number: {{application_number}}\nNext Steps: Please schedule your interview using the link below\nInterview Scheduling: {{interview_link}}\n\nDuring your interview, we will discuss your experience, interests, and answer any questions you may have about our community.\n\nPlease schedule your interview within the next 14 days.\n\nBest regards,\nThe WitchCityRope Vetting Team",
                Variables = JsonSerializer.Serialize(new[] { "{{scene_name}}", "{{application_number}}", "{{interview_link}}", "{{submission_date}}", "{{application_date}}", "{{status_change_date}}", "{{current_status}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Id = Guid.Parse("80808080-8080-8080-8080-808080808082"),
                Category = EmailCategory.Vetting,
                TemplateType = "VettingApproved",
                Title = "Welcome to WitchCityRope - {{scene_name}}",
                Subject = "Welcome to WitchCityRope - {{scene_name}}",
                HtmlBody = "<p style=\"margin-bottom: 16px;\">Hi {{scene_name}},</p><p style=\"margin-bottom: 16px;\">Congratulations! Your application has been approved and you are now a <strong>vetted member</strong> of WitchCityRope.</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Application Approved</h2><p style=\"margin-bottom: 16px;\"><strong>Application Number:</strong> {{application_number}}<br><strong>Approval Date:</strong> {{approval_date}}</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Welcome to Our Community!</h2><p style=\"margin-bottom: 16px;\">You now have access to:</p><ul style=\"margin-bottom: 16px; padding-left: 20px;\"><li>All member events and workshops</li><li>Our private community forums</li><li>Advanced classes and demonstrations</li><li>Volunteer opportunities</li></ul><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Next Steps</h2><p style=\"margin-bottom: 16px;\">Your member profile has been activated and you can now register for upcoming events. We look forward to seeing you at our next gathering!</p><p style=\"margin-bottom: 16px;\">Welcome aboard!</p><p style=\"margin-bottom: 16px;\">Best regards,<br>The Witch City Rope Team</p><p style=\"margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;\">Questions? Contact us at <a href=\"mailto:info@witchcityrope.com\" style=\"color: #880124;\">info@witchcityrope.com</a></p>",
                PlainTextBody = "Dear {{scene_name}},\n\nCongratulations! Your application has been approved and you are now a vetted member of WitchCityRope.\n\nApplication Number: {{application_number}}\nApproval Date: {{approval_date}}\n\nWelcome to our community! You now have access to:\n- All member events and workshops\n- Our private community forums\n- Advanced classes and demonstrations\n- Volunteer opportunities\n\nYour member profile has been activated and you can now register for upcoming events.\n\nWelcome aboard!\n\nBest regards,\nThe WitchCityRope Team",
                Variables = JsonSerializer.Serialize(new[] { "{{scene_name}}", "{{application_number}}", "{{approval_date}}", "{{submission_date}}", "{{application_date}}", "{{status_change_date}}", "{{current_status}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Id = Guid.Parse("80808080-8080-8080-8080-808080808083"),
                Category = EmailCategory.Vetting,
                TemplateType = "ApplicationOnHold",
                Title = "Application On Hold - Additional Information Needed - {{scene_name}}",
                Subject = "Application On Hold - Additional Information Needed - {{scene_name}}",
                HtmlBody = "<p style=\"margin-bottom: 16px;\">Hi {{scene_name}},</p><p style=\"margin-bottom: 16px;\">Your vetting application is currently on hold as we need some additional information to proceed.</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Application Status</h2><p style=\"margin-bottom: 16px;\"><strong>Application Number:</strong> {{application_number}}<br><strong>Status:</strong> On Hold<br><strong>Reason:</strong> {{hold_reason}}</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Required Actions</h2><p style=\"margin-bottom: 16px;\">{{required_actions}}</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Important Deadline</h2><p style=\"margin-bottom: 16px;\">Please provide the requested information within <strong>30 days</strong> to avoid application expiration.</p><p style=\"margin-bottom: 16px;\">If you have any questions about what's needed, please don't hesitate to contact us.</p><p style=\"margin-bottom: 16px;\">Best regards,<br>The Witch City Rope Vetting Team</p><p style=\"margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;\">Questions? Contact us at <a href=\"mailto:info@witchcityrope.com\" style=\"color: #880124;\">info@witchcityrope.com</a></p>",
                PlainTextBody = "Dear {{scene_name}},\n\nYour vetting application is currently on hold as we need some additional information to proceed.\n\nApplication Number: {{application_number}}\nReason: {{hold_reason}}\n\nRequired Actions:\n{{required_actions}}\n\nPlease provide the requested information within 30 days to avoid application expiration.\n\nIf you have any questions about what's needed, please contact us.\n\nBest regards,\nThe WitchCityRope Vetting Team",
                Variables = JsonSerializer.Serialize(new[] { "{{scene_name}}", "{{application_number}}", "{{hold_reason}}", "{{required_actions}}", "{{submission_date}}", "{{application_date}}", "{{status_change_date}}", "{{current_status}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Id = Guid.Parse("80808080-8080-8080-8080-808080808084"),
                Category = EmailCategory.Vetting,
                TemplateType = "ApplicationStatusUpdate",
                Title = "Application Status Update - {{scene_name}}",
                Subject = "Application Status Update - {{scene_name}}",
                HtmlBody = "<p style=\"margin-bottom: 16px;\">Hi {{scene_name}},</p><p style=\"margin-bottom: 16px;\">Thank you for your interest in WitchCityRope. After careful review, we are unable to approve your application at this time.</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Application Decision</h2><p style=\"margin-bottom: 16px;\"><strong>Application Number:</strong> {{application_number}}<br><strong>Review Date:</strong> {{review_date}}<br><strong>Status:</strong> Not Approved</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Next Steps</h2><p style=\"margin-bottom: 16px;\">This decision is final for this application cycle. You are welcome to reapply in the future if your circumstances change.</p><p style=\"margin-bottom: 16px;\">We appreciate your interest in our community.</p><p style=\"margin-bottom: 16px;\">Best regards,<br>The Witch City Rope Vetting Team</p><p style=\"margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;\">Questions? Contact us at <a href=\"mailto:info@witchcityrope.com\" style=\"color: #880124;\">info@witchcityrope.com</a></p>",
                PlainTextBody = "Dear {{scene_name}},\n\nThank you for your interest in WitchCityRope. After careful review, we are unable to approve your application at this time.\n\nApplication Number: {{application_number}}\nReview Date: {{review_date}}\n\nThis decision is final for this application cycle. You are welcome to reapply in the future if your circumstances change.\n\nWe appreciate your interest in our community.\n\nBest regards,\nThe WitchCityRope Vetting Team",
                Variables = JsonSerializer.Serialize(new[] { "{{scene_name}}", "{{application_number}}", "{{review_date}}", "{{submission_date}}", "{{application_date}}", "{{status_change_date}}", "{{current_status}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Id = Guid.Parse("80808080-8080-8080-8080-808080808085"),
                Category = EmailCategory.Vetting,
                TemplateType = "InterviewReminder",
                Title = "Interview Reminder - {{scene_name}}",
                Subject = "Interview Reminder - {{scene_name}}",
                HtmlBody = "<p style=\"margin-bottom: 16px;\">Hi {{scene_name}},</p><p style=\"margin-bottom: 16px;\">This is a friendly reminder about your upcoming vetting interview.</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Interview Information</h2><p style=\"margin-bottom: 16px;\"><strong>Application Number:</strong> {{application_number}}</p><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Preparation Checklist</h2><ul style=\"margin-bottom: 16px; padding-left: 20px;\"><li>Review your application details</li><li>Prepare questions about WitchCityRope</li><li>Be ready to discuss your rope bondage experience</li><li>Ensure you have a quiet, private space for the interview</li></ul><h2 style=\"color: #880124; margin-top: 24px; margin-bottom: 16px;\">Need to Reschedule?</h2><p style=\"margin-bottom: 16px;\">If you need to reschedule, please contact us at least 24 hours in advance.</p><p style=\"margin-bottom: 16px;\">We look forward to meeting with you!</p><p style=\"margin-bottom: 16px;\">Best regards,<br>The Witch City Rope Vetting Team</p><p style=\"margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;\">Questions? Contact us at <a href=\"mailto:info@witchcityrope.com\" style=\"color: #880124;\">info@witchcityrope.com</a></p>",
                PlainTextBody = "Dear {{scene_name}},\n\nThis is a friendly reminder about your upcoming vetting interview.\n\nApplication Number: {{application_number}}\n\nIf you need to reschedule, please contact us at least 24 hours in advance.\n\nWe look forward to meeting with you!\n\nBest regards,\nThe WitchCityRope Vetting Team",
                Variables = JsonSerializer.Serialize(new[] { "{{scene_name}}", "{{application_number}}", "{{submission_date}}", "{{application_date}}", "{{status_change_date}}", "{{current_status}}" }),
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
                _logger.LogInformation("Added Vetting template: {TemplateType}", template.TemplateType);
            }
            else
            {
                _logger.LogInformation("Vetting template already exists: {TemplateType}", template.TemplateType);
            }
        }
    }

    private async Task SeedEventsTemplatesAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding Events templates (10)...");

        var templates = new[]
        {
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "Confirmation",
                Title = "Your ticket for {{event_title}}",
                Subject = "Your ticket for {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>Thank you for registering for <strong>{{event_title}}</strong>!</p><p><strong>Event Details:</strong><br>Venue: {{venue_name}}<br>Address: {{venue_address}}</p><h3>Your Sessions</h3>{{ticket_sessions_list}}<p><strong>Total Paid:</strong> {{total_paid}}<br><strong>Confirmation Number:</strong> {{confirmation_number}}</p><p>We look forward to seeing you!</p><p>Questions? Email events@witchcityrope.com</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nThank you for registering for {{event_title}}!\n\nEvent Details:\nVenue: {{venue_name}}\nAddress: {{venue_address}}\n\nYour Sessions:\n{{ticket_sessions_list_text}}\n\nTotal Paid: {{total_paid}}\nConfirmation Number: {{confirmation_number}}\n\nWe look forward to seeing you!\n\nQuestions? Email events@witchcityrope.com",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}", "{{ticket_type}}", "{{total_paid}}", "{{confirmation_number}}", "{{ticket_sessions_list}}", "{{ticket_sessions_list_text}}" }),
                TriggerType = TemplateTriggerType.FixedEvent,
                RecipientGroup = EventRecipientGroup.RSVPTicketHolders,
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
                Title = "One week until {{event_title}}",
                Subject = "One week until {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>Just a friendly reminder that <strong>{{event_title}}</strong> is coming up in one week!</p><p><strong>Event Details:</strong><br>Date: {{event_date}}<br>Time: {{event_time}}<br>Venue: {{venue_name}}<br>Address: {{venue_address}}</p><p>We're looking forward to seeing you there!</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nJust a friendly reminder that {{event_title}} is coming up in one week!\n\nEvent Details:\nDate: {{event_date}}\nTime: {{event_time}}\nVenue: {{venue_name}}\nAddress: {{venue_address}}\n\nWe're looking forward to seeing you there!",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}" }),
                TriggerType = TemplateTriggerType.TimeBased,
                TimingOffsetDays = 7,
                RecipientGroup = EventRecipientGroup.RSVPTicketHolders,
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
                Title = "Tomorrow: {{event_title}}",
                Subject = "Tomorrow: {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p><strong>{{event_title}}</strong> is tomorrow!</p><p><strong>When:</strong> {{event_date}} at {{event_time}}<br><strong>Where:</strong> {{venue_name}}<br>{{venue_address}}</p><p>See you there!</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\n{{event_title}} is tomorrow!\n\nWhen: {{event_date}} at {{event_time}}\nWhere: {{venue_name}}\n{{venue_address}}\n\nSee you there!",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}" }),
                TriggerType = TemplateTriggerType.TimeBased,
                TimingOffsetDays = 1,
                RecipientGroup = EventRecipientGroup.RSVPTicketHolders,
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
                Title = "Starting soon: {{event_title}}",
                Subject = "Starting soon: {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p><strong>{{event_title}}</strong> starts in 2 hours!</p><p><strong>Time:</strong> {{event_time}}<br><strong>Location:</strong> {{venue_name}}</p><p>See you soon!</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\n{{event_title}} starts in 2 hours!\n\nTime: {{event_time}}\nLocation: {{venue_name}}\n\nSee you soon!",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_time}}", "{{venue_name}}" }),
                TriggerType = TemplateTriggerType.TimeBased,
                TimingOffsetDays = 0,
                TimingOffsetHours = 2,
                RecipientGroup = EventRecipientGroup.RSVPTicketHolders,
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
                Title = "Cancellation Confirmation: {{event_title}}",
                Subject = "Cancellation Confirmation: {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>Your registration for <strong>{{event_title}}</strong> has been cancelled.</p><h3>Cancelled Sessions</h3>{{cancelled_sessions_list}}<p>{{custom_message}}</p><p>If you have any questions, please contact events@witchcityrope.com</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nYour registration for {{event_title}} has been cancelled.\n\nCancelled Sessions:\n{{cancelled_sessions_list_text}}\n\n{{custom_message}}\n\nIf you have any questions, please contact events@witchcityrope.com",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{custom_message}}", "{{cancelled_sessions_list}}", "{{cancelled_sessions_list_text}}" }),
                TriggerType = TemplateTriggerType.FixedEvent,
                RecipientGroup = EventRecipientGroup.RSVPTicketHolders,
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "RSVPCancellation",
                Title = "RSVP Cancelled: {{event_title}}",
                Subject = "RSVP Cancelled: {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>Your RSVP for <strong>{{event_title}}</strong> on {{event_date}} has been cancelled.</p><p>{{custom_message}}</p><p>If you'd like to attend, you can RSVP again from the event page.</p><p>Questions? Email events@witchcityrope.com</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nYour RSVP for {{event_title}} on {{event_date}} has been cancelled.\n\n{{custom_message}}\n\nIf you'd like to attend, you can RSVP again from the event page.\n\nQuestions? Email events@witchcityrope.com",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}", "{{event_time}}", "{{venue_name}}", "{{venue_address}}", "{{custom_message}}" }),
                TriggerType = TemplateTriggerType.FixedEvent,
                RecipientGroup = EventRecipientGroup.RSVPTicketHolders,
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
                Title = "Session Update: {{event_title}}",
                Subject = "Session Update: {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>There has been an update to the session <strong>{{session_name}}</strong> for <strong>{{event_title}}</strong>.</p><p><strong>New Date/Time:</strong> {{event_date}} at {{event_time}}</p><p>{{custom_message}}</p><p>Thank you for your understanding.</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nThere has been an update to the session {{session_name}} for {{event_title}}.\n\nNew Date/Time: {{event_date}} at {{event_time}}\n\n{{custom_message}}\n\nThank you for your understanding.",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{session_name}}", "{{event_date}}", "{{event_time}}", "{{custom_message}}" }),
                TriggerType = TemplateTriggerType.FixedEvent,
                RecipientGroup = EventRecipientGroup.RSVPTicketHolders,
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
                Title = "Thank you for attending {{event_title}}",
                Subject = "Thank you for attending {{event_title}}",
                HtmlBody = "<p>Hi {{attendee_name}},</p><p>Thank you for attending <strong>{{event_title}}</strong> on {{event_date}}!</p><p>We hope you had a wonderful experience and learned new skills. If you have any feedback or questions, please don't hesitate to reach out to events@witchcityrope.com</p><p>We look forward to seeing you at future events!</p>",
                PlainTextBody = "Hi {{attendee_name}},\n\nThank you for attending {{event_title}} on {{event_date}}!\n\nWe hope you had a wonderful experience and learned new skills. If you have any feedback or questions, please don't hesitate to reach out to events@witchcityrope.com\n\nWe look forward to seeing you at future events!",
                Variables = JsonSerializer.Serialize(new[] { "{{attendee_name}}", "{{event_title}}", "{{event_date}}" }),
                TriggerType = TemplateTriggerType.TimeBased,
                TimingOffsetDays = -1,
                RecipientGroup = EventRecipientGroup.RSVPTicketHolders,
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            // ============================================================================
            // VOLUNTEER TEMPLATES
            // ============================================================================
            // These target the SessionVolunteers recipient group. The EmailSchedulerJob
            // populates volunteer-specific variables (volunteer_role, shift_start, shift_end)
            // from the volunteer's VolunteerPosition via RecipientInfo.
            // Event-wide volunteers (VolunteerPosition.SessionId == null) receive this email
            // for EVERY session's trigger time. Session-specific volunteers only receive it
            // for their session's trigger time.
            // ============================================================================
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "VolunteerReminder",
                Title = "Volunteer Reminder: {{event_title}}",
                Subject = "Volunteer Reminder: {{event_title}} - {{session_name}}",
                HtmlBody = "<p>Hi {{volunteer_name}},</p><p>This is a reminder that you are volunteering for <strong>{{event_title}}</strong>!</p><h3>Your Volunteer Details</h3><p><strong>Role:</strong> {{volunteer_role}}<br><strong>Session:</strong> {{session_name}}<br><strong>Date:</strong> {{event_date}}<br><strong>Time:</strong> {{event_time}}<br><strong>Shift:</strong> {{shift_start}} - {{shift_end}}</p><p><strong>Location:</strong><br>{{venue_name}}<br>{{venue_address}}</p><p>Thank you for volunteering! If you can no longer make it, please update your status as soon as possible so we can find a replacement.</p><p>Questions? Email events@witchcityrope.com</p>",
                PlainTextBody = "Hi {{volunteer_name}},\n\nThis is a reminder that you are volunteering for {{event_title}}!\n\nYour Volunteer Details:\nRole: {{volunteer_role}}\nSession: {{session_name}}\nDate: {{event_date}}\nTime: {{event_time}}\nShift: {{shift_start}} - {{shift_end}}\n\nLocation:\n{{venue_name}}\n{{venue_address}}\n\nThank you for volunteering! If you can no longer make it, please update your status as soon as possible so we can find a replacement.\n\nQuestions? Email events@witchcityrope.com",
                Variables = JsonSerializer.Serialize(new[] { "{{volunteer_name}}", "{{event_title}}", "{{session_name}}", "{{event_date}}", "{{event_time}}", "{{volunteer_role}}", "{{shift_start}}", "{{shift_end}}", "{{venue_name}}", "{{venue_address}}" }),
                TriggerType = TemplateTriggerType.TimeBased,
                TimingOffsetDays = 2,
                RecipientGroup = EventRecipientGroup.SessionVolunteers,
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Events,
                TemplateType = "VolunteerThankYou",
                Title = "Thank you for volunteering: {{event_title}}",
                Subject = "Thank you for volunteering at {{event_title}}!",
                HtmlBody = "<p>Hi {{volunteer_name}},</p><p>Thank you so much for volunteering at <strong>{{event_title}}</strong>!</p><p><strong>Your Role:</strong> {{volunteer_role}}<br><strong>Session:</strong> {{session_name}}<br><strong>Date:</strong> {{event_date}}</p><p>Our events wouldn't be possible without dedicated volunteers like you. We truly appreciate your time and effort!</p><p>We hope to see you at future events. If you have any feedback about your volunteer experience, please reach out to events@witchcityrope.com</p><p>With gratitude,<br>The Witch City Rope Team</p>",
                PlainTextBody = "Hi {{volunteer_name}},\n\nThank you so much for volunteering at {{event_title}}!\n\nYour Role: {{volunteer_role}}\nSession: {{session_name}}\nDate: {{event_date}}\n\nOur events wouldn't be possible without dedicated volunteers like you. We truly appreciate your time and effort!\n\nWe hope to see you at future events. If you have any feedback about your volunteer experience, please reach out to events@witchcityrope.com\n\nWith gratitude,\nThe Witch City Rope Team",
                Variables = JsonSerializer.Serialize(new[] { "{{volunteer_name}}", "{{event_title}}", "{{session_name}}", "{{event_date}}", "{{volunteer_role}}" }),
                TriggerType = TemplateTriggerType.TimeBased,
                TimingOffsetDays = -1,
                RecipientGroup = EventRecipientGroup.SessionVolunteers,
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
        _logger.LogInformation("Seeding Admin templates (6)..."); // Updated count to include NewWebsiteUser

        var templates = new[]
        {
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Admin,
                TemplateType = "AccountCreated",
                Title = "Welcome to WitchCityRope - Account Created",
                Subject = "Welcome to WitchCityRope - Account Created",
                HtmlBody = "<p>Hi {{user_name}},</p><p>Your WitchCityRope account has been created!</p><p><strong>Email:</strong> {{account_email}}</p><p>You can log in at https://witchcityrope.com</p><p>If you have any questions, contact us at support@witchcityrope.com</p>",
                PlainTextBody = "Hi {{user_name}},\n\nYour WitchCityRope account has been created!\n\nEmail: {{account_email}}\n\nYou can log in at https://witchcityrope.com\n\nIf you have any questions, contact us at support@witchcityrope.com",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{account_email}}" }),
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
                Title = "Password Reset Request - WitchCityRope",
                Subject = "Password Reset Request - WitchCityRope",
                HtmlBody = "<p>Hi {{user_name}},</p><p>A password reset has been requested for your account.</p><p>If you made this request, please click the link below to reset your password:</p><p><a href=\"{{reset_url}}\">Reset My Password</a></p><p>This link will expire in 24 hours.</p><p>If you did not request this, please ignore this email and contact support@witchcityrope.com</p>",
                PlainTextBody = "Hi {{user_name}},\n\nA password reset has been requested for your account.\n\nIf you made this request, please use the link below to reset your password:\n\n{{reset_url}}\n\nThis link will expire in 24 hours.\n\nIf you did not request this, please ignore this email and contact support@witchcityrope.com",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{reset_url}}" }),
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
                Title = "Your WitchCityRope Role Has Been Updated",
                Subject = "Your WitchCityRope Role Has Been Updated",
                HtmlBody = "<p>Hi {{user_name}},</p><p>Your role in the WitchCityRope system has been updated.</p><p>{{action_required}}</p><p>If you have questions about this change, please contact support@witchcityrope.com</p>",
                PlainTextBody = "Hi {{user_name}},\n\nYour role in the WitchCityRope system has been updated.\n\n{{action_required}}\n\nIf you have questions about this change, please contact support@witchcityrope.com",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{action_required}}" }),
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
                Title = "WitchCityRope System Notification",
                Subject = "WitchCityRope System Notification",
                HtmlBody = "<p>Hi {{user_name}},</p><p>{{action_required}}</p><p><strong>Deadline:</strong> {{deadline_date}}</p><p>For assistance, contact support@witchcityrope.com</p>",
                PlainTextBody = "Hi {{user_name}},\n\n{{action_required}}\n\nDeadline: {{deadline_date}}\n\nFor assistance, contact support@witchcityrope.com",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{action_required}}", "{{deadline_date}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Admin,
                TemplateType = "EmailVerification",
                Title = "Verify Your WitchCityRope Email Address",
                Subject = "Verify Your WitchCityRope Email Address",
                HtmlBody = "<p style=\"margin-bottom: 16px;\">Hi {{user_name}},</p><p style=\"margin-bottom: 16px;\">Welcome to WitchCityRope! Please verify your email address by clicking the button below:</p><p style=\"margin-bottom: 24px; text-align: center;\"><a href=\"{{verification_url}}\" style=\"display: inline-block; padding: 12px 24px; background: #614B79; color: white; text-decoration: none; border-radius: 4px; font-weight: bold;\">Verify Email Address</a></p><p style=\"margin-bottom: 16px;\">This link will expire in 3 days. You must verify your email before you can log in.</p><p style=\"margin-bottom: 16px;\">If you did not create this account, please ignore this email.</p><p style=\"margin-bottom: 16px;\">Need help? Contact us at <a href=\"mailto:support@witchcityrope.com\" style=\"color: #880124;\">support@witchcityrope.com</a></p><p style=\"margin-top: 24px; padding-top: 16px; border-top: 1px solid #e5e5e5; color: #666; font-size: 12px;\">This is an automated message from WitchCityRope. Please do not reply to this email.<br>WitchCityRope • Salem, MA • witchcityrope.com</p>",
                PlainTextBody = "Hi {{user_name}},\n\nWelcome to WitchCityRope! Please verify your email address by clicking the link below:\n\n{{verification_url}}\n\nThis link will expire in 3 days. You must verify your email before you can log in.\n\nIf you did not create this account, please ignore this email.\n\nNeed help? Contact us at support@witchcityrope.com\n\n---\nThis is an automated message from WitchCityRope. Please do not reply to this email.\nWitchCityRope • Salem, MA • witchcityrope.com",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{verification_url}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Admin,
                TemplateType = "RefundConfirmation",
                Title = "Refund Confirmation - WitchCityRope",
                Subject = "Refund Confirmation - WitchCityRope",
                HtmlBody = @"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <h1 style=""color: #2c3e50; margin-top: 0;"">Refund Confirmation</h1>
    <p>Hi {{user_name}},</p>
    <p>Your refund has been processed successfully.</p>
    <div style=""background-color: #ffffff; border-left: 4px solid #4CAF50; padding: 15px; margin: 20px 0;"">
        <h2 style=""margin-top: 0; color: #4CAF50; font-size: 18px;"">Refund Details</h2>
        <table style=""width: 100%; border-collapse: collapse;"">
            <tr>
                <td style=""padding: 8px 0; font-weight: bold;"">Refund Amount:</td>
                <td style=""padding: 8px 0; text-align: right;"">{{refund_amount}}</td>
            </tr>
            <tr>
                <td style=""padding: 8px 0; font-weight: bold;"">Original Payment:</td>
                <td style=""padding: 8px 0; text-align: right;"">{{original_amount}}</td>
            </tr>
            <tr>
                <td style=""padding: 8px 0; font-weight: bold;"">Payment Method:</td>
                <td style=""padding: 8px 0; text-align: right;"">{{payment_method}}</td>
            </tr>
            <tr>
                <td style=""padding: 8px 0; font-weight: bold;"">Refund ID:</td>
                <td style=""padding: 8px 0; text-align: right; font-family: monospace; font-size: 12px;"">{{refund_id}}</td>
            </tr>
        </table>
    </div>
    <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0;"">
        <h3 style=""margin-top: 0; color: #856404; font-size: 16px;"">When will I receive my refund?</h3>
        <p style=""margin: 0; color: #856404;"">{{timing_message}}</p>
    </div>
    <div style=""margin: 20px 0;"">
        <h3 style=""font-size: 16px; color: #2c3e50;"">Refund Reason</h3>
        <p style=""background-color: #ffffff; padding: 15px; border-radius: 4px; margin: 10px 0;"">{{refund_reason}}</p>
    </div>
    <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;"">
    <p style=""font-size: 14px; color: #6c757d;"">
        If you have any questions about this refund, please contact us at
        <a href=""mailto:support@witchcityrope.com"" style=""color: #007bff; text-decoration: none;"">support@witchcityrope.com</a>.
    </p>
    <p style=""font-size: 14px; color: #6c757d; margin-top: 20px;"">
        Thank you,<br>
        <strong>WitchCityRope Team</strong>
    </p>
    <div style=""text-align: center; font-size: 12px; color: #adb5bd; margin-top: 20px;"">
        <p>This is an automated email. Please do not reply to this message.</p>
    </div>
</div>",
                PlainTextBody = @"Hi {{user_name}},

Your refund has been processed successfully.

REFUND DETAILS
================
Refund Amount: {{refund_amount}}
Original Payment: {{original_amount}}
Payment Method: {{payment_method}}
Refund ID: {{refund_id}}

WHEN WILL I RECEIVE MY REFUND?
{{timing_message}}

REFUND REASON
{{refund_reason}}

If you have any questions about this refund, please contact us at support@witchcityrope.com.

Thank you,
WitchCityRope Team

---
This is an automated email. Please do not reply to this message.",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{refund_amount}}", "{{original_amount}}", "{{payment_method}}", "{{timing_message}}", "{{refund_reason}}", "{{refund_id}}" }),
                IsActive = true,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = adminUserId
            },
            new GlobalEmailTemplate
            {
                Category = EmailCategory.Admin,
                TemplateType = "NewWebsiteUser",
                Title = "Welcome to WitchCityRope - Set Your Password",
                Subject = "Welcome to WitchCityRope - Set Your Password",
                HtmlBody = @"<div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <h1 style=""color: #614B79; margin-top: 0;"">Welcome to WitchCityRope!</h1>
    <p>Hello {{user_name}},</p>
    <p>Your WitchCityRope account has been created as part of our member import process. We're excited to have you join our community!</p>
    <div style=""background-color: #f8f9fa; border-left: 4px solid #614B79; padding: 15px; margin: 20px 0;"">
        <h2 style=""margin-top: 0; color: #614B79; font-size: 18px;"">Next Step: Set Your Password</h2>
        <p style=""margin-bottom: 10px;"">To activate your account and log in, you need to set your password:</p>
        <p style=""text-align: center; margin: 20px 0;"">
            <a href=""{{reset_url}}"" style=""display: inline-block; padding: 12px 24px; background: #614B79; color: white; text-decoration: none; border-radius: 4px; font-weight: bold;"">Set Your Password</a>
        </p>
        <p style=""margin-top: 10px; color: #6c757d; font-size: 14px;""><strong>Important:</strong> This link will expire in 72 hours for security reasons.</p>
    </div>
    <div style=""margin: 20px 0;"">
        <h3 style=""font-size: 16px; color: #2c3e50;"">What's Next?</h3>
        <ul style=""color: #495057;"">
            <li>Click the button above to set your password</li>
            <li>Complete your profile at <a href=""{{system_url}}"" style=""color: #614B79;"">{{system_url}}</a></li>
            <li>Browse upcoming events and workshops</li>
            <li>Connect with the WitchCityRope community</li>
        </ul>
    </div>
    <hr style=""border: none; border-top: 1px solid #dee2e6; margin: 30px 0;"">
    <p style=""font-size: 14px; color: #6c757d;"">
        If you have any questions or need assistance, please contact us at
        <a href=""mailto:support@witchcityrope.com"" style=""color: #614B79; text-decoration: none;"">support@witchcityrope.com</a>.
    </p>
    <p style=""font-size: 14px; color: #6c757d; margin-top: 20px;"">
        Welcome to the community!<br>
        <strong>The WitchCityRope Team</strong>
    </p>
    <div style=""text-align: center; font-size: 12px; color: #adb5bd; margin-top: 20px;"">
        <p>WitchCityRope • Salem, MA • <a href=""{{system_url}}"" style=""color: #adb5bd;"">{{system_url}}</a></p>
    </div>
</div>",
                PlainTextBody = @"Welcome to WitchCityRope!

Hello {{user_name}},

Your WitchCityRope account has been created as part of our member import process. We're excited to have you join our community!

NEXT STEP: SET YOUR PASSWORD
============================
To activate your account and log in, you need to set your password using the link below:

{{reset_url}}

IMPORTANT: This link will expire in 72 hours for security reasons.

WHAT'S NEXT?
============
• Click the link above to set your password
• Complete your profile at {{system_url}}
• Browse upcoming events and workshops
• Connect with the WitchCityRope community

If you have any questions or need assistance, please contact us at support@witchcityrope.com.

Welcome to the community!
The WitchCityRope Team

---
WitchCityRope • Salem, MA • {{system_url}}",
                Variables = JsonSerializer.Serialize(new[] { "{{user_name}}", "{{reset_url}}", "{{system_url}}" }),
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
                Title = "Incident Report Received - #{{incident_number}}",
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
                Title = "Incident #{{incident_number}} Status Update",
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
                Title = "You've been assigned to Incident #{{incident_number}}",
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
                Title = "Incident #{{incident_number}} Resolved",
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
            Title = "Message from WitchCityRope",
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

    private async Task UpdateTriggerConfigsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating trigger configs for existing Events templates...");

        var triggerConfigs = new Dictionary<string, (TemplateTriggerType TriggerType, int? OffsetDays, int? OffsetHours, EventRecipientGroup Group)>
        {
            ["Confirmation"] = (TemplateTriggerType.FixedEvent, null, null, EventRecipientGroup.RSVPTicketHolders),
            ["Reminder1Week"] = (TemplateTriggerType.TimeBased, 7, null, EventRecipientGroup.RSVPTicketHolders),
            ["Reminder1Day"] = (TemplateTriggerType.TimeBased, 1, null, EventRecipientGroup.RSVPTicketHolders),
            ["Reminder2Hours"] = (TemplateTriggerType.TimeBased, 0, 2, EventRecipientGroup.RSVPTicketHolders),
            ["ThankYou"] = (TemplateTriggerType.TimeBased, -1, null, EventRecipientGroup.RSVPTicketHolders),
            ["Cancellation"] = (TemplateTriggerType.FixedEvent, null, null, EventRecipientGroup.RSVPTicketHolders),
            ["SessionChange"] = (TemplateTriggerType.FixedEvent, null, null, EventRecipientGroup.RSVPTicketHolders),
        };

        var templates = await _context.GlobalEmailTemplates
            .Where(t => t.Category == EmailCategory.Events && t.IsActive)
            .ToListAsync(cancellationToken);

        var updated = 0;
        foreach (var template in templates)
        {
            if (!triggerConfigs.TryGetValue(template.TemplateType, out var config))
                continue;

            // Only update if RecipientGroup is not yet set (avoids overwriting admin changes)
            if (template.RecipientGroup.HasValue)
                continue;

            template.TriggerType = config.TriggerType;
            template.TimingOffsetDays = config.OffsetDays;
            template.TimingOffsetHours = config.OffsetHours;
            template.RecipientGroup = config.Group;
            updated++;
        }

        if (updated > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Updated trigger configs for {Count} Events templates", updated);
        }
        else
        {
            _logger.LogInformation("All Events templates already have trigger configs");
        }
    }
}
