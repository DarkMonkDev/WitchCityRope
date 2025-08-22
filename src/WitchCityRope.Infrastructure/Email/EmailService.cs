using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using WitchCityRope.Core.Interfaces;
using WitchCityRope.Core.ValueObjects;
using CoreEmailAddress = WitchCityRope.Core.ValueObjects.EmailAddress;
using SendGridEmailAddress = SendGrid.Helpers.Mail.EmailAddress;

namespace WitchCityRope.Infrastructure.Email
{
    /// <summary>
    /// Email service implementation using SendGrid
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ISendGridClient? _sendGridClient;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly Dictionary<string, string> _emailTemplates;

        public EmailService(ISendGridClient? sendGridClient, IConfiguration configuration)
        {
            _sendGridClient = sendGridClient;
            _fromEmail = configuration["Email:From"] ?? "noreply@witchcityrope.com";
            _fromName = configuration["Email:FromName"] ?? "Witch City Rope";
            
            // Initialize email templates
            _emailTemplates = new Dictionary<string, string>
            {
                ["registration-confirmation"] = configuration["Email:SendGrid:Templates:RegistrationConfirmation"],
                ["cancellation-confirmation"] = configuration["Email:SendGrid:Templates:CancellationConfirmation"],
                ["vetting-status-update"] = configuration["Email:SendGrid:Templates:VettingStatusUpdate"]
            };
        }

        public async Task<bool> SendEmailAsync(CoreEmailAddress to, string subject, string body, bool isHtml = true)
        {
            if (_sendGridClient == null)
            {
                // SendGrid not configured - log email in development
                Console.WriteLine($"[EMAIL] To: {to.Value}, Subject: {subject}");
                return true; // Simulate success in development
            }

            try
            {
                var from = new SendGridEmailAddress(_fromEmail, _fromName);
                var toAddress = new SendGridEmailAddress(to.Value);
                var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, isHtml ? null : body, isHtml ? body : null);

                var response = await _sendGridClient.SendEmailAsync(msg);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Log the exception (logging should be added)
                return false;
            }
        }

        public async Task<bool> SendBulkEmailAsync(IEnumerable<CoreEmailAddress> to, string subject, string body, bool isHtml = true)
        {
            if (_sendGridClient == null)
            {
                // SendGrid not configured - log email in development
                Console.WriteLine($"[BULK EMAIL] To: {string.Join(", ", to.Select(e => e.Value))}, Subject: {subject}");
                return true; // Simulate success in development
            }

            try
            {
                var from = new SendGridEmailAddress(_fromEmail, _fromName);
                var tos = to.Select(email => new SendGridEmailAddress(email.Value)).ToList();
                
                // SendGrid recommends sending bulk emails in batches
                const int batchSize = 100;
                var batches = tos.Select((email, index) => new { email, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.email));

                var allSuccessful = true;
                foreach (var batch in batches)
                {
                    var msg = MailHelper.CreateSingleEmailToMultipleRecipients(
                        from, 
                        batch.ToList(), 
                        subject, 
                        isHtml ? null : body, 
                        isHtml ? body : null);

                    var response = await _sendGridClient.SendEmailAsync(msg);
                    if (!response.IsSuccessStatusCode)
                    {
                        allSuccessful = false;
                    }
                }

                return allSuccessful;
            }
            catch (Exception)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<bool> SendTemplateEmailAsync(CoreEmailAddress to, string templateName, object templateData)
        {
            if (_sendGridClient == null)
            {
                // SendGrid not configured - log email in development
                Console.WriteLine($"[TEMPLATE EMAIL] To: {to.Value}, Template: {templateName}");
                return true; // Simulate success in development
            }

            try
            {
                if (!_emailTemplates.TryGetValue(templateName, out var templateId))
                {
                    throw new ArgumentException($"Template '{templateName}' not found", nameof(templateName));
                }

                var from = new SendGridEmailAddress(_fromEmail, _fromName);
                var toAddress = new SendGridEmailAddress(to.Value);
                var msg = MailHelper.CreateSingleTemplateEmail(from, toAddress, templateId, templateData);

                var response = await _sendGridClient.SendEmailAsync(msg);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                // Log the exception
                return false;
            }
        }

        public async Task<bool> SendRegistrationConfirmationAsync(CoreEmailAddress to, string sceneName, string eventTitle, DateTime eventDate)
        {
            var templateData = new
            {
                scene_name = sceneName,
                event_title = eventTitle,
                event_date = eventDate.ToString("MMMM dd, yyyy"),
                event_time = eventDate.ToString("h:mm tt")
            };

            return await SendTemplateEmailAsync(to, "registration-confirmation", templateData);
        }

        public async Task<bool> SendCancellationConfirmationAsync(CoreEmailAddress to, string sceneName, string eventTitle, Money? refundAmount = null)
        {
            var templateData = new
            {
                scene_name = sceneName,
                event_title = eventTitle,
                refund_amount = refundAmount?.ToString(),
                has_refund = refundAmount != null
            };

            return await SendTemplateEmailAsync(to, "cancellation-confirmation", templateData);
        }

        public async Task<bool> SendVettingStatusUpdateAsync(CoreEmailAddress to, string sceneName, string status, string? notes = null)
        {
            var templateData = new
            {
                scene_name = sceneName,
                status = status,
                notes = notes,
                has_notes = !string.IsNullOrEmpty(notes)
            };

            return await SendTemplateEmailAsync(to, "vetting-status-update", templateData);
        }
    }
}