using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WitchCityRope.Core.Entities;
using WitchCityRope.Core.Enums;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;
using WitchCityRope.Infrastructure.Data;

namespace WitchCityRope.Infrastructure.Services
{
    public class EventEmailService : IEventEmailService
    {
        private readonly WitchCityRopeIdentityDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<EventEmailService> _logger;

        public EventEmailService(
            WitchCityRopeIdentityDbContext context,
            IEmailService emailService,
            ILogger<EventEmailService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // Template Management

        public async Task<List<EventEmailTemplate>> GetEventEmailTemplatesAsync(Guid eventId)
        {
            return await _context.EventEmailTemplates
                .Where(t => t.EventId == eventId)
                .OrderBy(t => t.Type)
                .ThenBy(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<EventEmailTemplate> GetEmailTemplateByIdAsync(Guid templateId)
        {
            return await _context.EventEmailTemplates
                .Include(t => t.Event)
                .FirstOrDefaultAsync(t => t.Id == templateId);
        }

        public async Task<EventEmailTemplate> CreateEmailTemplateAsync(EventEmailTemplate template)
        {
            _context.EventEmailTemplates.Add(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created email template {TemplateId} for event {EventId}", template.Id, template.EventId);
            
            return template;
        }

        public async Task<EventEmailTemplate> UpdateEmailTemplateAsync(EventEmailTemplate template)
        {
            _context.EventEmailTemplates.Update(template);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated email template {TemplateId}", template.Id);
            
            return template;
        }

        public async Task DeleteEmailTemplateAsync(Guid templateId)
        {
            var template = await _context.EventEmailTemplates.FindAsync(templateId);
            if (template != null)
            {
                _context.EventEmailTemplates.Remove(template);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted email template {TemplateId}", templateId);
            }
        }

        // Email Sending

        public async Task<bool> SendEventEmailAsync(Guid eventId, Guid templateId)
        {
            try
            {
                var template = await GetEmailTemplateByIdAsync(templateId);
                if (template == null || template.EventId != eventId)
                {
                    _logger.LogWarning("Template {TemplateId} not found or doesn't belong to event {EventId}", templateId, eventId);
                    return false;
                }

                var recipients = await GetEventEmailRecipientsAsync(eventId, template.Type);
                if (!recipients.Any())
                {
                    _logger.LogWarning("No recipients found for event {EventId} with template type {TemplateType}", eventId, template.Type);
                    return true; // Not an error, just no recipients
                }

                // Get event details for variable replacement
                var evt = await _context.Events
                    .Include(e => e.Tickets)
                    .FirstOrDefaultAsync(e => e.Id == eventId);
                
                if (evt == null)
                {
                    _logger.LogError("Event {EventId} not found", eventId);
                    return false;
                }

                // Send emails to all recipients
                var success = true;
                foreach (var recipientEmail in recipients)
                {
                    try
                    {
                        var user = await _context.Users
                            .FirstOrDefaultAsync(u => u.Email == recipientEmail);
                        
                        var variables = new EventEmailVariables
                        {
                            AttendeeName = user?.DisplayName ?? "Guest",
                            EventName = evt.Title,
                            EventDate = evt.StartDate,
                            EventTime = new TimeOnly(evt.StartDate.Hour, evt.StartDate.Minute),
                            VenueName = evt.Location,
                            VenueAddress = evt.Location
                        };

                        var (subject, body) = template.RenderTemplate(variables);
                        
                        var emailAddress = EmailAddress.Create(recipientEmail);
                        await _emailService.SendEmailAsync(emailAddress, subject, body);
                        
                        _logger.LogInformation("Sent email to {Email} for event {EventId} using template {TemplateId}", 
                            recipientEmail, eventId, templateId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email} for event {EventId}", recipientEmail, eventId);
                        success = false;
                    }
                }

                // Record email history
                await RecordEmailHistory(eventId, templateId, template.Subject, template.Body, recipients, success);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event email for event {EventId} with template {TemplateId}", eventId, templateId);
                return false;
            }
        }

        public async Task<bool> SendCustomEventEmailAsync(Guid eventId, string subject, string body, List<string> recipientEmails = null)
        {
            try
            {
                var recipients = recipientEmails ?? await GetEventEmailRecipientsAsync(eventId, EmailTemplateType.Custom);
                
                if (!recipients.Any())
                {
                    _logger.LogWarning("No recipients specified for custom email for event {EventId}", eventId);
                    return true; // Not an error
                }

                var success = true;
                foreach (var recipientEmail in recipients)
                {
                    try
                    {
                        var emailAddress = EmailAddress.Create(recipientEmail);
                        await _emailService.SendEmailAsync(emailAddress, subject, body);
                        
                        _logger.LogInformation("Sent custom email to {Email} for event {EventId}", recipientEmail, eventId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send custom email to {Email} for event {EventId}", recipientEmail, eventId);
                        success = false;
                    }
                }

                // Record email history
                await RecordEmailHistory(eventId, null, subject, body, recipients, success);

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending custom event email for event {EventId}", eventId);
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync(Guid templateId, string testEmail)
        {
            try
            {
                var template = await GetEmailTemplateByIdAsync(templateId);
                if (template == null)
                {
                    _logger.LogWarning("Template {TemplateId} not found", templateId);
                    return false;
                }

                var variables = new EventEmailVariables
                {
                    AttendeeName = "Test User",
                    EventName = "Test Event",
                    EventDate = DateTime.Now.AddDays(7),
                    EventTime = new TimeOnly(19, 0),
                    VenueName = "Test Venue",
                    VenueAddress = "123 Test Street, Test City"
                };

                var (subject, body) = template.RenderTemplate(variables);
                
                var emailAddress = EmailAddress.Create(testEmail);
                await _emailService.SendEmailAsync(emailAddress, $"[TEST] {subject}", body);
                
                _logger.LogInformation("Sent test email to {Email} for template {TemplateId}", testEmail, templateId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email for template {TemplateId}", templateId);
                return false;
            }
        }

        // Template Operations

        public async Task<string> PreviewEmailTemplateAsync(Guid templateId, Guid? sampleUserId = null)
        {
            var template = await GetEmailTemplateByIdAsync(templateId);
            if (template == null)
                return string.Empty;

            var user = sampleUserId.HasValue 
                ? await _context.Users.FindAsync(sampleUserId.Value)
                : null;

            var variables = new EventEmailVariables
            {
                AttendeeName = user?.DisplayName ?? "Sample User",
                EventName = template.Event?.Title ?? "Sample Event",
                EventDate = template.Event?.StartDate ?? DateTime.Now.AddDays(7),
                EventTime = template.Event != null 
                    ? new TimeOnly(template.Event.StartDate.Hour, template.Event.StartDate.Minute)
                    : new TimeOnly(19, 0),
                VenueName = template.Event?.Location ?? "Sample Venue",
                VenueAddress = template.Event?.Location ?? "123 Sample Street"
            };

            var (subject, body) = template.RenderTemplate(variables);
            
            return $"{subject}\n{body}";
        }

        public async Task<EventEmailTemplate> CloneTemplateAsync(Guid templateId, Guid targetEventId)
        {
            var sourceTemplate = await GetEmailTemplateByIdAsync(templateId);
            if (sourceTemplate == null)
                throw new ArgumentException("Source template not found", nameof(templateId));

            var clonedTemplate = new EventEmailTemplate(
                eventId: targetEventId,
                type: sourceTemplate.Type,
                subject: sourceTemplate.Subject,
                body: sourceTemplate.Body);

            return await CreateEmailTemplateAsync(clonedTemplate);
        }

        public async Task<List<EventEmailTemplate>> GetSystemTemplatesAsync()
        {
            // Return system templates (templates with no specific event ID)
            // For now, return empty list as we don't have system templates yet
            return new List<EventEmailTemplate>();
        }

        // Recipient Management

        public async Task<List<string>> GetEventEmailRecipientsAsync(Guid eventId, EmailTemplateType templateType)
        {
            var registrations = await _context.Tickets
                .Where(r => r.EventId == eventId)
                .ToListAsync();
            
            var userIds = registrations.Select(r => r.UserId).Distinct();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();
            
            var userEmailMap = users.ToDictionary(u => u.Id, u => u.Email);
            var recipients = new List<string>();

            switch (templateType)
            {
                case EmailTemplateType.RegistrationConfirmation:
                    // All confirmed users
                    var confirmedRegs = registrations.Where(r => r.Status == TicketStatus.Confirmed);
                    foreach (var reg in confirmedRegs)
                    {
                        if (userEmailMap.TryGetValue(reg.UserId, out var email))
                            recipients.Add(email);
                    }
                    break;
                
                case EmailTemplateType.EventReminder:
                    // Only confirmed attendees
                    var reminderRegs = registrations.Where(r => r.Status == TicketStatus.Confirmed);
                    foreach (var reg in reminderRegs)
                    {
                        if (userEmailMap.TryGetValue(reg.UserId, out var email))
                            recipients.Add(email);
                    }
                    break;
                
                case EmailTemplateType.WaitlistNotification:
                    // Only waitlisted users
                    var waitlistRegs = registrations.Where(r => r.Status == TicketStatus.Waitlisted);
                    foreach (var reg in waitlistRegs)
                    {
                        if (userEmailMap.TryGetValue(reg.UserId, out var email))
                            recipients.Add(email);
                    }
                    break;
                
                case EmailTemplateType.CancellationNotice:
                    // All registered and waitlisted users
                    var cancelRegs = registrations.Where(r => r.Status == TicketStatus.Confirmed || 
                                          r.Status == TicketStatus.Waitlisted);
                    foreach (var reg in cancelRegs)
                    {
                        if (userEmailMap.TryGetValue(reg.UserId, out var email))
                            recipients.Add(email);
                    }
                    break;
                
                case EmailTemplateType.Custom:
                default:
                    // All confirmed users by default
                    var defaultRegs = registrations.Where(r => r.Status == TicketStatus.Confirmed);
                    foreach (var reg in defaultRegs)
                    {
                        if (userEmailMap.TryGetValue(reg.UserId, out var email))
                            recipients.Add(email);
                    }
                    break;
            }

            return recipients.Distinct().ToList();
        }

        public async Task<int> GetRecipientCountAsync(Guid eventId, EmailTemplateType templateType)
        {
            var recipients = await GetEventEmailRecipientsAsync(eventId, templateType);
            return recipients.Count;
        }

        // Email History

        public async Task<List<EmailSendHistory>> GetEmailHistoryAsync(Guid eventId)
        {
            // TODO: Implement email history tracking in database
            // For now, return empty list
            return new List<EmailSendHistory>();
        }

        public async Task<bool> ResendEmailAsync(Guid emailHistoryId)
        {
            // TODO: Implement email history resending
            _logger.LogWarning("ResendEmailAsync not yet implemented");
            return false;
        }

        // Bulk Operations

        public async Task<bool> SendBulkEventEmailsAsync(List<Guid> eventIds, Guid templateId)
        {
            var success = true;
            
            foreach (var eventId in eventIds)
            {
                try
                {
                    var result = await SendEventEmailAsync(eventId, templateId);
                    if (!result)
                        success = false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending bulk email for event {EventId}", eventId);
                    success = false;
                }
            }

            return success;
        }

        public async Task<bool> ScheduleEmailAsync(Guid templateId, DateTime scheduledTime)
        {
            // TODO: Implement email scheduling
            _logger.LogWarning("ScheduleEmailAsync not yet implemented");
            return false;
        }

        public async Task<bool> CancelScheduledEmailAsync(Guid scheduledEmailId)
        {
            // TODO: Implement scheduled email cancellation
            _logger.LogWarning("CancelScheduledEmailAsync not yet implemented");
            return false;
        }

        // Private helper methods

        private async Task RecordEmailHistory(Guid eventId, Guid? templateId, string subject, string body, 
            List<string> recipients, bool isSuccessful, string errorMessage = null)
        {
            // TODO: Implement email history recording in database
            _logger.LogInformation("Email sent for event {EventId}: Subject='{Subject}', Recipients={RecipientCount}, Success={IsSuccessful}",
                eventId, subject, recipients.Count, isSuccessful);
        }
    }
}